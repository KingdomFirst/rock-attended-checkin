// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn
{
    /// <summary>
    /// Assigns a grouptype, group, location and schedule from those available if one hasn't been previously selected
    /// </summary>
    [Description( "Selects the grouptype, group, location and schedule for each person based on their best fit." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Select By Best Fit" )]
    [BooleanField( "Room Balance By Group", "Select the group with the least number of current people. Best for groups having a 1:1 ratio with locations.", false )]
    [BooleanField( "Room Balance By Location", "Select the location with the least number of current people. Best for groups having 1 to many ratio with locations.", false )]
    public class SelectByBestFit : CheckInActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, Rock.Model.WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            bool roomBalanceByGroup = GetAttributeValue( action, "RoomBalanceByGroup" ).AsBoolean();
            bool roomBalanceByLocation = GetAttributeValue( action, "RoomBalanceByLocation" ).AsBoolean();

            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    foreach ( var person in family.People.Where( f => f.Selected ) )
                    {
                        char[] delimiter = { ',' };
                        var validGroupTypes = person.GroupTypes.ToList();
                        if ( validGroupTypes.Any() )
                        {
                            List<CheckInGroup> validGroups;
                            CheckInGroupType bestGroupType = null;
                            if ( validGroupTypes.Count() == 1 || validGroupTypes.Any( gt => gt.Selected ) )
                            {
                                bestGroupType = validGroupTypes.OrderByDescending( gt => gt.Selected ).FirstOrDefault();
                                validGroups = bestGroupType.Groups.Where( g => !g.ExcludedByFilter ).ToList();
                            }
                            else
                            {
                                // start with unfiltered groups for kids with abnormal age and grade parameters (1%)
                                validGroups = validGroupTypes.SelectMany( gt => gt.Groups ).ToList();
                            }

                            if ( validGroups.Any() )
                            {
                                CheckInGroup bestGroup = null;
                                List<CheckInLocation> validLocations;
                                if ( validGroups.Count == 1 || validGroups.Any( g => g.Selected ) )
                                {
                                    bestGroup = validGroups.OrderByDescending( g => g.Selected ).FirstOrDefault();
                                    validLocations = bestGroup.Locations.Where( l => !l.ExcludedByFilter ).ToList();
                                }
                                else
                                {
                                    // Honor group assignments first
                                    var checkInGroupIds = validGroups.Select( g => g.Group.Id ).ToList();
                                    var personAssignments = new GroupMemberService( rockContext ).Queryable()
                                        .Where( m => checkInGroupIds.Contains( m.GroupId ) && m.PersonId == person.Person.Id )
                                        .Select( m => m.GroupId ).ToList();
                                    if ( personAssignments.Any() )
                                    {
                                        bestGroup = validGroups.FirstOrDefault( g => personAssignments.Contains( g.Group.Id ) );
                                    }

                                    // Select group by best fit
                                    if ( bestGroup == null )
                                    {
                                        CheckInGroup closestAbilityGroup = null;
                                        bool useAbilityGroups = true;

                                        // attributes on people already loaded
                                        var personsAbility = person.Person.GetAttributeValue( "AbilityLevel" );
                                        if ( !string.IsNullOrWhiteSpace( personsAbility ) )
                                        {
                                            var abilityGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "AbilityLevel" )
                                                && g.Group.GetAttributeValue( "AbilityLevel" ) == personsAbility ).ToList();
                                            closestAbilityGroup = abilityGroups.FirstOrDefault();
                                        }
                                        else
                                        {
                                            useAbilityGroups = false;
                                        }

                                        CheckInGroup closestGradeGroup = null;
                                        if ( person.Person.GradeOffset != null )
                                        {
                                            // check groups for a grade range
                                            var gradeValues = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.SCHOOL_GRADES ) ).DefinedValues;
                                            var filteredGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "GradeRange" ) )
                                                .Select( g => new
                                                {
                                                    Group = g,
                                                    GradeOffsets = g.Group.GetAttributeValue( "GradeRange" )
                                                        .Split( delimiter, StringSplitOptions.None )
                                                        .Where( av => !string.IsNullOrEmpty( av ) )
                                                        .Select( av => gradeValues.FirstOrDefault( v => v.Guid == new Guid( av ) ) )
                                                        .Select( av => av.Value.AsDecimal() )
                                                        .ToList()
                                                }
                                                ).ToList();

                                            // Only check groups that have valid grade offsets
                                            if ( filteredGroups.Any( g => g.GradeOffsets.Any() ) )
                                            {
                                                decimal baseVariance = 100;
                                                decimal gradeOffset = (decimal)person.Person.GradeOffset.Value;
                                                foreach ( var filtered in filteredGroups )
                                                {
                                                    var minGradeOffset = filtered.GradeOffsets.First();
                                                    var maxGradeOffset = filtered.GradeOffsets.Last();
                                                    var gradeVariance = minGradeOffset - maxGradeOffset;
                                                    if ( minGradeOffset >= gradeOffset && maxGradeOffset <= gradeOffset && gradeVariance < baseVariance )
                                                    {
                                                        closestGradeGroup = filtered.Group;
                                                        baseVariance = gradeVariance;
                                                    }
                                                }
                                            }
                                        }

                                        CheckInGroup closestAgeGroup = null;
                                        if ( person.Person.Age != null )
                                        {
                                            // check groups for an age range
                                            var filteredGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "AgeRange" ) )
                                                .Select( g => new
                                                {
                                                    Group = g,
                                                    AgeRange = g.Group.GetAttributeValue( "AgeRange" )
                                                        .Split( delimiter, StringSplitOptions.None )
                                                        .Where( av => !string.IsNullOrEmpty( av ) )
                                                        .Select( av => av.AsType<decimal>() )
                                                        .ToList()
                                                }
                                                ).ToList();

                                            if ( filteredGroups.Any( g => g.AgeRange.Any() ) )
                                            {
                                                decimal baseVariance = 100;
                                                decimal personAge = (decimal)person.Person.AgePrecise;
                                                foreach ( var filtered in filteredGroups )
                                                {
                                                    var minAge = filtered.AgeRange.First();
                                                    var maxAge = filtered.AgeRange.Last();
                                                    var ageVariance = maxAge - minAge;
                                                    if ( maxAge >= personAge && minAge <= personAge && ageVariance < baseVariance )
                                                    {
                                                        closestAgeGroup = filtered.Group;
                                                        baseVariance = ageVariance;
                                                    }
                                                }
                                            }
                                        }

                                        // assignment priority: Ability, then Grade, then Age, then 1st available
                                        bestGroup = closestAbilityGroup ?? closestGradeGroup ?? closestAgeGroup ?? validGroups.FirstOrDefault( g => !g.ExcludedByFilter );
                                        if ( roomBalanceByGroup )
                                        {
                                            CheckInGroup lowestCountGroup = null;
                                            if ( useAbilityGroups )
                                            {
                                                lowestCountGroup = validGroups.Where( g => !g.ExcludedByFilter )
                                                    .OrderBy( g => g.Locations.Select( l => KioskLocationAttendance
                                                        .Read( l.Location.Id ).CurrentCount ).Sum() )
                                                    .FirstOrDefault();
                                            }
                                            else
                                            {
                                                lowestCountGroup = validGroups.Where( g => !g.ExcludedByFilter && !g.Group.Attributes.ContainsKey( "AgeRange" ) )
                                                    .OrderBy( g => g.Locations.Select( l => KioskLocationAttendance
                                                        .Read( l.Location.Id ).CurrentCount ).Sum() )
                                                    .FirstOrDefault();
                                            }

                                            if ( lowestCountGroup != null )
                                            {   // only one location per group should exist
                                                var lowCount = lowestCountGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                                var bestGroupCount = bestGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                                if ( lowCount < bestGroupCount )
                                                {
                                                    bestGroup = lowestCountGroup;
                                                }
                                            }
                                        }
                                    }

                                    validLocations = bestGroup.Locations.Where( l => !l.ExcludedByFilter ).ToList();
                                }

                                if ( validLocations.Any() )
                                {
                                    CheckInLocation bestLocation = null;
                                    List<CheckInSchedule> validSchedules;
                                    if ( validLocations.Count() == 1 || validLocations.Any( g => g.Selected ) )
                                    {
                                        bestLocation = validLocations.OrderByDescending( g => g.Selected ).FirstOrDefault();
                                        validSchedules = bestLocation.Schedules.Where( l => !l.ExcludedByFilter ).ToList();
                                    }
                                    else
                                    {
                                        if ( roomBalanceByLocation )
                                        {
                                            bestLocation = validLocations.Where( l => l.Schedules.Any( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) )
                                                .OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                            validSchedules = bestLocation.Schedules.Where( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ).ToList();
                                        }
                                        else
                                        {
                                            bestLocation = validLocations.FirstOrDefault( l => l.Schedules.Any( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) );
                                            validSchedules = validLocations.SelectMany( l => l.Schedules.Where( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) ).ToList();
                                        }
                                    }

                                    if ( validSchedules.Any() )
                                    {
                                        var bestSchedule = validSchedules.OrderByDescending( s => s.Selected ).FirstOrDefault();
                                        bestSchedule.Selected = true;
                                        bestSchedule.PreSelected = true;

                                        if ( bestLocation != null )
                                        {
                                            bestLocation.PreSelected = true;
                                            bestLocation.Selected = true;

                                            if ( bestGroup != null )
                                            {
                                                bestGroup.PreSelected = true;
                                                bestGroup.Selected = true;

                                                bestGroupType = validGroupTypes.FirstOrDefault( gt => gt.GroupType.Id == bestGroup.Group.GroupTypeId );
                                                if ( bestGroupType != null )
                                                {
                                                    bestGroupType.Selected = true;
                                                    bestGroupType.PreSelected = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}