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
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState == null )
            {
                return false;
            }

            bool roomBalanceByGroup = GetAttributeValue( action, "RoomBalanceByGroup" ).AsBoolean();
            bool roomBalanceByLocation = GetAttributeValue( action, "RoomBalanceByLocation" ).AsBoolean();

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                // don't run for people who already have attendance assignments
                foreach ( var person in family.People.Where( f => f.Selected && !f.GroupTypes.Any( gt => gt.Selected ) ) )
                {
                    decimal baseVariance = 100;
                    char[] delimiter = { ',' };

                    // variable must be a string to compare to group attribute value
                    var specialNeeds = person.Person.GetAttributeValue( "IsSpecialNeeds" ).ToStringSafe();

                    var validGroupTypes = person.GroupTypes;
                    if ( validGroupTypes.Count > 0 )
                    {
                        IEnumerable<CheckInGroup> validGroups;
                        CheckInGroupType bestGroupType = null;
                        if ( validGroupTypes.Count == 1 )
                        {
                            bestGroupType = validGroupTypes.OrderByDescending( gt => gt.Selected ).FirstOrDefault();
                            validGroups = bestGroupType.Groups.Where( g => !g.ExcludedByFilter || g.Selected );
                        }
                        else
                        {
                            // start with unfiltered groups for kids with abnormal age and grade parameters (1%)
                            validGroups = validGroupTypes.SelectMany( gt => gt.Groups );
                        }

                        // check how many groups exist without getting the whole list
                        int numValidGroups = validGroups.Take( 2 ).Count();
                        if ( numValidGroups > 0 )
                        {
                            CheckInGroup bestGroup = null;
                            IEnumerable<CheckInLocation> validLocations;
                            if ( numValidGroups == 1 )
                            {
                                bestGroup = validGroups.OrderByDescending( g => g.Selected ).FirstOrDefault();
                                validLocations = bestGroup.Locations.Where( l => !l.ExcludedByFilter || l.Selected );
                            }
                            else
                            {
                                // Select by group assignment first
                                var checkInGroupIds = validGroups.Select( g => g.Group.Id ).ToList();
                                var personAssignments = new GroupMemberService( rockContext ).Queryable()
                                    .Where( m => checkInGroupIds.Contains( m.GroupId ) && m.PersonId == person.Person.Id )
                                    .Select( m => m.GroupId ).ToList();
                                if ( personAssignments.Count > 0 )
                                {
                                    bestGroup = validGroups.FirstOrDefault( g => personAssignments.Contains( g.Group.Id ) );
                                }

                                // Select group by best fit
                                if ( bestGroup == null )
                                {
                                    //var ageGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( "AgeRange" ) && !string.IsNullOrEmpty( g.Group.AttributeValues["AgeRange"].Value ) )
                                    var attributeGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( "AgeRange" )
                                        && !string.IsNullOrEmpty( g.Group.AttributeValues["AgeRange"].Value ) );

                                    var ageGroups = attributeGroups.Select( g => new
                                        {
                                            Group = g,
                                            AgeRange = g.Group.AttributeValues["AgeRange"].Value
                                                .Split( delimiter, StringSplitOptions.None )
                                                .Where( av => !string.IsNullOrEmpty( av ) )
                                                .Select( av => av.AsType<decimal>() )
                                            //.ToList()
                                        } )
                                        //.Where( g => g.AgeRange.Count > 0 )
                                        .ToList();

                                    // Check ages
                                    CheckInGroup closestAgeGroup = null;
                                    if ( person.Person.Age != null && ageGroups.Count > 0 )
                                    {
                                        decimal personAge = (decimal)person.Person.AgePrecise;
                                        foreach ( var ageGroup in ageGroups.Where( g => g.AgeRange.Any() ) )
                                        {
                                            var minAge = ageGroup.AgeRange.First();
                                            var maxAge = ageGroup.AgeRange.Last();
                                            var ageVariance = maxAge - minAge;
                                            if ( maxAge >= personAge && minAge <= personAge && ageVariance < baseVariance )
                                            {
                                                closestAgeGroup = ageGroup.Group;
                                                baseVariance = ageVariance;
                                            }
                                        }
                                    }

                                    // Check grades
                                    CheckInGroup closestGradeGroup = null;
                                    if ( person.Person.GradeOffset != null )
                                    {
                                        var gradeValues = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.SCHOOL_GRADES ) ).DefinedValues;
                                        //attributeGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "GradeRange" ) && !string.IsNullOrEmpty( g.Group.AttributeValues["GradeRange"].Value ) )
                                        attributeGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( "GradeRange" )
                                            && !string.IsNullOrEmpty( g.Group.AttributeValues["GradeRange"].Value ) ).ToList();

                                        var gradeGroups = attributeGroups.Select( g => new
                                            {
                                                Group = g,
                                                GradeOffsets = g.Group.AttributeValues["GradeRange"].Value
                                                    .Split( delimiter, StringSplitOptions.None )
                                                    .Where( av => !string.IsNullOrEmpty( av ) )
                                                    .Select( av => gradeValues.FirstOrDefault( v => v.Guid == new Guid( av ) ) )
                                                    .Select( av => av.Value.AsDecimal() )
                                                //.ToList()
                                            } )
                                            .ToList();

                                        // Only check groups that have valid grade offsets
                                        if ( person.Person.GradeOffset != null && gradeGroups.Count > 0 )
                                        {
                                            decimal gradeOffset = (decimal)person.Person.GradeOffset.Value;
                                            foreach ( var gradeGroup in gradeGroups.Where( g => g.GradeOffsets.Any() ) )
                                            {
                                                var minGradeOffset = gradeGroup.GradeOffsets.First();
                                                var maxGradeOffset = gradeGroup.GradeOffsets.Last();
                                                var gradeVariance = minGradeOffset - maxGradeOffset;
                                                if ( minGradeOffset >= gradeOffset && maxGradeOffset <= gradeOffset && gradeVariance < baseVariance )
                                                {
                                                    closestGradeGroup = gradeGroup.Group;
                                                    baseVariance = gradeVariance;
                                                }
                                            }

                                            /* ======================================================== *
                                                optional scenario: find the next closest grade group
                                            * ========================================================= *
                                                if (grade > max)
                                                    grade - max
                                                else if (grade < min)
                                                    min - grade
                                                else 0;

                                            // add a tiny variance to offset larger groups:
                                                result += ((max - min)/100)
                                            * ========================================================= */
                                        }
                                    }

                                    CheckInGroup closestNeedsGroup = null;
                                    if ( specialNeeds.AsBoolean() )
                                    {
                                        var specialGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( "IsSpecialNeeds" )
                                            && g.Group.AttributeValues["IsSpecialNeeds"].Value == specialNeeds ).ToList();
                                        if ( person.Person.Age != null && specialGroups.Count > 0 )
                                        {
                                            // get the special needs group by closest age
                                            var intersectingGroups = ageGroups.Where( ag => specialGroups.Select( sg => sg.Group.Id ).Contains( ag.Group.Group.Id ) );
                                            decimal personAge = (decimal)person.Person.AgePrecise;
                                            foreach ( var filteredGroup in intersectingGroups )
                                            {
                                                var minAge = filteredGroup.AgeRange.First();
                                                var maxAge = filteredGroup.AgeRange.Last();
                                                var ageVariance = maxAge - minAge;
                                                if ( maxAge >= personAge && minAge <= personAge && ageVariance < baseVariance )
                                                {
                                                    closestNeedsGroup = filteredGroup.Group;
                                                    baseVariance = ageVariance;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            closestNeedsGroup = specialGroups.FirstOrDefault();
                                        }
                                    }

                                    // assignment priority: Ability, then Grade, then Age, then 1st available
                                    bestGroup = closestNeedsGroup ?? closestGradeGroup ?? closestAgeGroup ?? validGroups.FirstOrDefault( g => !g.ExcludedByFilter );

                                    if ( roomBalanceByGroup )
                                    {
                                        CheckInGroup lowestCountGroup = validGroups.Where( g => !g.ExcludedByFilter )
                                            .OrderBy( g => g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() )
                                            .FirstOrDefault();

                                        if ( lowestCountGroup != null )
                                        {   // only one location per group should exist in room balance by group
                                            var lowestGroupCount = lowestCountGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                            var bestGroupCount = bestGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                            if ( lowestGroupCount < bestGroupCount )
                                            {
                                                bestGroup = lowestCountGroup;
                                            }
                                        }
                                    }
                                }

                                validLocations = bestGroup.Locations.Where( l => !l.ExcludedByFilter || l.Selected );
                            }

                            // check how many locations exist without getting the whole list
                            int numValidLocations = validLocations.Take( 2 ).Count();
                            if ( numValidLocations > 0 )
                            {
                                CheckInLocation bestLocation = null;
                                IEnumerable<CheckInSchedule> validSchedules;
                                if ( numValidLocations == 1 )
                                {
                                    bestLocation = validLocations.OrderByDescending( l => l.Selected ).FirstOrDefault();
                                    validSchedules = bestLocation.Schedules.Where( s => !s.ExcludedByFilter || s.Selected );
                                }
                                else
                                {
                                    if ( roomBalanceByLocation )
                                    {
                                        bestLocation = validLocations.Where( l => l.Schedules.Any( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) )
                                            .OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount )
                                            .FirstOrDefault();
                                        validSchedules = bestLocation.Schedules.Where( s => !s.ExcludedByFilter || s.Selected );
                                    }
                                    else
                                    {
                                        bestLocation = validLocations.FirstOrDefault( l => l.Schedules.Any( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) );
                                        validSchedules = validLocations.SelectMany( l => l.Schedules.Where( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) );
                                    }
                                }

                                // check how many schedules exist without getting the whole list
                                int numValidSchedules = validSchedules.Take( 2 ).Count();
                                if ( numValidSchedules > 0 )
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
    }
}