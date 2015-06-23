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
    /// Calculates and updates the LastCheckIn property on check-in objects
    /// </summary>
    [Description( "Select multiple services this person last checked into" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Select By Multiple Services Attended" )]
    [BooleanField( "Room Balance By Group", "Select the group with the least number of current people. Best for groups having a 1:1 ratio with locations.", false )]
    [BooleanField( "Room Balance By Location", "Select the location with the least number of current people. Best for groups having 1 to many ratio with locations.", false )]
    [IntegerField( "Previous Months Attendance", "Select the number of previous months to look for attendance history.  The default value is 3 months.", false, 3 )]
    [IntegerField( "Max Assignments", "Select the maximum number of assignments based on previous attendance.  The default value is 5.", false, 5 )]
    public class SelectByMultipleAttended : CheckInActionComponent
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
            int previousMonthsNumber = GetAttributeValue( action, "PreviousMonthsAttendance" ).AsIntegerOrNull() ?? 3;
            int maxAssignments = GetAttributeValue( action, "MaxAssignments" ).AsIntegerOrNull() ?? 5;

            var cutoffDate = Rock.RockDateTime.Today.AddMonths( previousMonthsNumber * -1 );
            var attendanceService = new AttendanceService( rockContext );

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                foreach ( var person in family.People.Where( p => p.Selected && !p.FirstTime ).ToList() )
                {
                    var personGroupTypeIds = person.GroupTypes.Select( gt => gt.GroupType.Id ).ToList();

                    var lastDateAttendances = attendanceService.Queryable()
                        .Where( a =>
                            a.PersonAlias.PersonId == person.Person.Id &&
                            personGroupTypeIds.Contains( a.Group.GroupTypeId ) &&
                            a.StartDateTime >= cutoffDate && a.DidAttend == true )
                        .OrderByDescending( a => a.StartDateTime ).Take( maxAssignments )
                        .ToList();

                    if ( lastDateAttendances.Any() )
                    {
                        var isSpecialNeeds = person.Person.GetAttributeValue( "IsSpecialNeeds" ).AsBoolean();

                        var lastAttended = lastDateAttendances.Max( a => a.StartDateTime ).Date;
                        lastDateAttendances = lastDateAttendances.Where( a => a.StartDateTime >= lastAttended ).ToList();

                        foreach ( var groupAttendance in lastDateAttendances )
                        {
                            // Start with unfiltered groups for kids with abnormal age and grade parameters (1%)
                            var groupType = person.GroupTypes.FirstOrDefault( t => t.GroupType.Id == groupAttendance.Group.GroupTypeId && ( !t.ExcludedByFilter || isSpecialNeeds ) );
                            if ( groupType != null )
                            {
                                CheckInGroup group = null;
                                if ( groupType.Groups.Count == 1 )
                                {
                                    // Only a single group is open
                                    group = groupType.Groups.FirstOrDefault( g => !g.ExcludedByFilter || isSpecialNeeds );
                                }
                                else
                                {
                                    // Pick the group they last attended
                                    group = groupType.Groups.FirstOrDefault( g => g.Group.Id == groupAttendance.GroupId && ( !g.ExcludedByFilter || isSpecialNeeds ) );
                                }

                                if ( roomBalanceByGroup && !isSpecialNeeds )
                                {
                                    // Respect filtering when room balancing
                                    var filteredGroups = groupType.Groups.Where( g => !g.ExcludedByFilter ).ToList();
                                    if ( filteredGroups.Any() )
                                    {
                                        group = filteredGroups.OrderBy( g => g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() ).FirstOrDefault();
                                    }
                                }

                                if ( group != null )
                                {
                                    CheckInLocation location = null;
                                    if ( group.Locations.Count == 1 )
                                    {
                                        // Only a single location is open
                                        location = group.Locations.FirstOrDefault( l => !l.ExcludedByFilter || isSpecialNeeds );
                                    }
                                    else
                                    {
                                        // Pick the location they last attended
                                        location = group.Locations.FirstOrDefault( l => l.Location.Id == groupAttendance.LocationId && ( !l.ExcludedByFilter || isSpecialNeeds ) );
                                    }

                                    if ( roomBalanceByLocation && !isSpecialNeeds )
                                    {
                                        // Respect filtering when room balancing
                                        var filteredLocations = group.Locations.Where( l => !l.ExcludedByFilter && l.Schedules.Any( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) ).ToList();
                                        if ( filteredLocations.Any() )
                                        {
                                            location = filteredLocations.OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                        }
                                    }

                                    if ( location != null )
                                    {
                                        CheckInSchedule schedule = null;
                                        if ( location.Schedules.Count == 1 )
                                        {
                                            schedule = location.Schedules.FirstOrDefault( s => !s.ExcludedByFilter || isSpecialNeeds );
                                        }
                                        else if ( groupAttendance.ScheduleId != null )
                                        {
                                            schedule = location.Schedules.FirstOrDefault( s => s.Schedule.Id == groupAttendance.ScheduleId && ( !s.ExcludedByFilter || isSpecialNeeds ) );
                                        }
                                        else
                                        {
                                            // if the schedule doesn't exactly match but everything else does, select it
                                            schedule = location.Schedules.FirstOrDefault( s => ( !s.ExcludedByFilter && !isSpecialNeeds ) );
                                        }

                                        if ( schedule != null )
                                        {
                                            schedule.Selected = true;
                                            schedule.PreSelected = true;
                                            schedule.LastCheckIn = groupAttendance.StartDateTime;
                                            location.Selected = true;
                                            location.PreSelected = true;
                                            location.LastCheckIn = groupAttendance.StartDateTime;
                                            group.Selected = true;
                                            group.PreSelected = true;
                                            group.LastCheckIn = groupAttendance.StartDateTime;
                                            groupType.Selected = true;
                                            groupType.PreSelected = true;
                                            group.LastCheckIn = groupAttendance.StartDateTime;
                                            groupType.LastCheckIn = groupAttendance.StartDateTime;
                                            groupType.Selected = true;
                                            groupType.PreSelected = true;
                                            person.LastCheckIn = groupAttendance.StartDateTime;
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