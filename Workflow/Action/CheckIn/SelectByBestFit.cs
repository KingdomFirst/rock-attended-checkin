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
            bool roomBalanceByGroup = bool.Parse( GetAttributeValue( action, "RoomBalanceByGroup" ) ?? "false" );
            bool roomBalanceByLocation = bool.Parse( GetAttributeValue( action, "RoomBalanceByLocation" ) ?? "false" );

            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    foreach ( var person in family.People.Where( f => f.Selected ) )
                    {
                        char[] delimiter = { ',' };
                        var validGroupTypes = person.GroupTypes.Where( gt => !gt.ExcludedByFilter );
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
                                validGroups = validGroupTypes.SelectMany( gt => gt.Groups.Where( g => !g.ExcludedByFilter ) ).ToList();
                            }

                            if ( validGroups.Any() )
                            {
                                CheckInGroup bestGroup = null;
                                List<CheckInLocation> validLocations;
                                if ( validGroups.Count() == 1 || validGroups.Any( g => g.Selected ) )
                                {
                                    bestGroup = validGroups.OrderByDescending( g => g.Selected ).FirstOrDefault();
                                    validLocations = bestGroup.Locations.Where( l => !l.ExcludedByFilter ).ToList();
                                }
                                else
                                {
                                    CheckInGroup closestAbilityGroup = null;

                                    // FilterGroupsByAbilityLevel already loads the attributes on people
                                    var personsAbility = person.Person.GetAttributeValue( "AbilityLevel" );

                                    if ( !string.IsNullOrWhiteSpace( personsAbility ) )
                                    {
                                        // check groups for a ability
                                        var newGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "AbilityLevel" ) && g.Group.GetAttributeValue( "AbilityLevel" ) == personsAbility ).ToList();
                                        closestAbilityGroup = newGroups.FirstOrDefault();
                                    }
                                    else
                                    {
                                        validGroups = validGroups.Where( g => !g.Group.Attributes.ContainsKey( "AbilityLevel" ) ).ToList();
                                    }

                                    CheckInGroup closestGradeGroup = null;
                                    if ( person.Person.Grade != null )
                                    {
                                        // check groups for a grade range
                                        var gradeFilteredGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "GradeRange" ) )
                                            .Select( g => new
                                            {
                                                Group = g,
                                                GradeRange = g.Group.GetAttributeValue( "GradeRange" )
                                                    .Split( delimiter, StringSplitOptions.None )
                                                    .Select( av => av.AsType<decimal>() )
                                            }
                                            ).ToList();

                                        if ( gradeFilteredGroups.Count > 0 )
                                        {
                                            decimal grade = (decimal)person.Person.Grade;
                                            closestGradeGroup = gradeFilteredGroups.Aggregate( ( x, y ) => Math.Abs( x.GradeRange.Average() - grade ) < Math.Abs( y.GradeRange.Average() - grade ) ? x : y )
                                                .Group;
                                        }
                                    }

                                    CheckInGroup closestAgeGroup = null;
                                    if ( person.Person.Age != null )
                                    {
                                        // check groups for an age range
                                        var ageFilteredGroups = validGroups.Where( g => g.Group.Attributes.ContainsKey( "AgeRange" ) )
                                            .Select( g => new
                                            {
                                                Group = g,
                                                AgeRange = g.Group.GetAttributeValue( "AgeRange" )
                                                    .Split( delimiter, StringSplitOptions.None )
                                                    .Select( av => av.AsType<decimal>() )
                                            }
                                            ).ToList();

                                        if ( ageFilteredGroups.Count > 0 )
                                        {
                                            decimal age = (decimal)person.Person.AgePrecise;
                                            closestAgeGroup = ageFilteredGroups.Aggregate( ( x, y ) => Math.Abs( x.AgeRange.Average() - age ) < Math.Abs( y.AgeRange.Average() - age ) ? x : y )
                                                .Group;
                                        }
                                    }

                                    bestGroup = closestAbilityGroup ?? closestGradeGroup ?? closestAgeGroup ?? validGroups.FirstOrDefault();
                                    if ( roomBalanceByGroup )
                                    {   // only one location per group should exist
                                        var lowestCountGroup = validGroups.OrderBy( g => g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() ).FirstOrDefault();
                                        var lowCount = lowestCountGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                        var bestGroupCount = bestGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                        if ( lowCount < bestGroupCount )
                                        {
                                            bestGroup = lowestCountGroup;
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