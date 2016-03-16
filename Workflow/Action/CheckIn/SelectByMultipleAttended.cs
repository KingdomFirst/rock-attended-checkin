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
    [BooleanField( "Room Balance", "Auto-assign the location with the least number of current people. This only applies when a person fits into multiple groups or locations.", false, order: 0 )]
    [IntegerField( "Balancing Override", "Enter the maximum difference between two locations before room balancing overrides previous attendance.  The default value is 10.", false, 10, order: 1 )]
    [TextField( "Excluded Locations", "Enter a comma-delimited list of location(s) to manually exclude from room balancing (like a catch-all room).", false, "Base Camp", order: 2 )]
    [IntegerField( "Previous Months Attendance", "Enter the number of previous months to look for attendance history.  The default value is 3 months.", false, 3, order: 3 )]
    [IntegerField( "Max Assignments", "Enter the maximum number of auto-assignments based on previous attendance.  The default value is 5.", false, 5, order: 4 )]
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

            int peopleWithoutAssignments = 0;
            bool roomBalance = GetAttributeValue( action, "RoomBalance" ).AsBoolean();
            int balanceOverride = GetAttributeValue( action, "DifferentialOverride" ).AsIntegerOrNull() ?? 10;
            int previousMonthsNumber = GetAttributeValue( action, "PreviousMonthsAttendance" ).AsIntegerOrNull() ?? 3;
            int maxAssignments = GetAttributeValue( action, "MaxAssignments" ).AsIntegerOrNull() ?? 5;
            var excludedLocations = GetAttributeValue( action, "ExcludedLocations" ).SplitDelimitedValues( false )
                .Select( s => s.Trim() );

            var cutoffDate = Rock.RockDateTime.Today.AddMonths( previousMonthsNumber * -1 );
            var attendanceService = new AttendanceService( rockContext );

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                // get the number of people checking in, including visitors or first-timers
                peopleWithoutAssignments = family.People.Where( p => p.Selected ).Count();

                foreach ( var previousAttender in family.People.Where( p => p.Selected && !p.FirstTime ) )
                {
                    var currentlyConfiguredGroupTypeIds = previousAttender.GroupTypes.Select( gt => gt.GroupType.Id ).ToList();

                    var lastDateAttendances = attendanceService.Queryable()
                        .Where( a =>
                            a.PersonAlias.PersonId == previousAttender.Person.Id &&
                            currentlyConfiguredGroupTypeIds.Contains( a.Group.GroupTypeId ) &&
                            a.StartDateTime >= cutoffDate && a.DidAttend == true )
                        .OrderByDescending( a => a.StartDateTime ).Take( maxAssignments )
                        .ToList();

                    if ( lastDateAttendances.Any() )
                    {
                        bool createdMatchingAssignment = false;
                        var hasSpecialNeeds = previousAttender.Person.GetAttributeValue( "HasSpecialNeeds" ).AsBoolean();

                        var lastAttended = lastDateAttendances.Max( a => a.StartDateTime ).Date;
                        foreach ( var groupAttendance in lastDateAttendances.Where( a => a.StartDateTime >= lastAttended ) )
                        {
                            bool withinServiceWindow = false;
                            var serviceCutoff = groupAttendance.StartDateTime;
                            if ( serviceCutoff > RockDateTime.Now.Date )
                            {
                                // calculate the service window to determine if people are still checked in
                                var serviceTime = groupAttendance.StartDateTime.Date + groupAttendance.Schedule.NextStartDateTime.Value.TimeOfDay;
                                var serviceStart = serviceTime.AddMinutes( ( groupAttendance.Schedule.CheckInStartOffsetMinutes ?? 0 ) * -1.0 );
                                serviceCutoff = serviceTime.AddMinutes( ( groupAttendance.Schedule.CheckInEndOffsetMinutes ?? 0 ) );
                                withinServiceWindow = RockDateTime.Now > serviceStart && RockDateTime.Now < serviceCutoff;
                            }

                            // Start with filtered groups unless they have abnormal age and grade parameters (1%)
                            var groupType = previousAttender.GroupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupAttendance.Group.GroupTypeId && ( !gt.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow ) );
                            if ( groupType != null )
                            {
                                CheckInGroup group = null;
                                if ( groupType.Groups.Count == 1 )
                                {
                                    // Only a single group is open
                                    group = groupType.Groups.FirstOrDefault( g => !g.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow );
                                }
                                else
                                {
                                    // Pick the group they last attended
                                    group = groupType.Groups.FirstOrDefault( g => g.Group.Id == groupAttendance.GroupId && ( !g.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow ) );

                                    if ( group != null && roomBalance && !hasSpecialNeeds )
                                    {
                                        var currentAttendance = group.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum();
                                        var lowestAttendedGroup = groupType.Groups.Where( g => !g.ExcludedByFilter && !excludedLocations.Contains( g.Group.Name ) )
                                            .Select( g => new { Group = g, Attendance = g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() } )
                                            .OrderBy( g => g.Attendance )
                                            .FirstOrDefault();

                                        if ( lowestAttendedGroup != null && lowestAttendedGroup.Attendance < ( currentAttendance - balanceOverride ) )
                                        {
                                            group = lowestAttendedGroup.Group;
                                        }
                                    }
                                }

                                if ( group != null )
                                {
                                    CheckInLocation location = null;
                                    if ( group.Locations.Count == 1 )
                                    {
                                        // Only a single location is open
                                        location = group.Locations.FirstOrDefault( l => !l.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow );
                                    }
                                    else
                                    {
                                        // Pick the location they last attended
                                        location = group.Locations.FirstOrDefault( l => l.Location.Id == groupAttendance.LocationId && ( !l.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow ) );

                                        if ( location != null && roomBalance && !hasSpecialNeeds )
                                        {
                                            var currentAttendance = KioskLocationAttendance.Read( location.Location.Id ).CurrentCount;
                                            var lowestAttendedLocation = group.Locations.Where( l => !l.ExcludedByFilter && !excludedLocations.Contains( l.Location.Name ) )
                                                .Select( l => new { Location = l, Attendance = KioskLocationAttendance.Read( location.Location.Id ).CurrentCount } )
                                                .OrderBy( l => l.Attendance )
                                                .FirstOrDefault();

                                            if ( lowestAttendedLocation != null && lowestAttendedLocation.Attendance < ( currentAttendance - balanceOverride ) )
                                            {
                                                location = lowestAttendedLocation.Location;
                                            }
                                        }
                                    }

                                    if ( location != null )
                                    {
                                        CheckInSchedule schedule = null;
                                        if ( location.Schedules.Count == 1 )
                                        {
                                            schedule = location.Schedules.FirstOrDefault( s => !s.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow );
                                        }
                                        else if ( groupAttendance.ScheduleId != null )
                                        {
                                            schedule = location.Schedules.FirstOrDefault( s => s.Schedule.Id == groupAttendance.ScheduleId && ( !s.ExcludedByFilter || hasSpecialNeeds || withinServiceWindow ) );
                                        }
                                        else
                                        {
                                            // if the schedule doesn't exactly match but everything else does, still select it
                                            // NOTE: this is helpful for a child coming at a different service time than normal
                                            schedule = location.Schedules.FirstOrDefault( s => ( !s.ExcludedByFilter && !hasSpecialNeeds ) );

                                            // if the schedule doesn't match previous attendance, it's impossible to currently be checked in
                                            serviceCutoff = RockDateTime.Now;
                                        }

                                        if ( schedule != null )
                                        {
                                            // set the service end time unless someone checked out already
                                            var attendanceEndDate = groupAttendance.EndDateTime ?? serviceCutoff;

                                            schedule.Selected = true;
                                            schedule.PreSelected = true;
                                            schedule.LastCheckIn = attendanceEndDate;
                                            location.Selected = true;
                                            location.PreSelected = true;
                                            location.LastCheckIn = attendanceEndDate;
                                            group.Selected = true;
                                            group.PreSelected = true;
                                            group.LastCheckIn = attendanceEndDate;
                                            groupType.Selected = true;
                                            groupType.PreSelected = true;
                                            group.LastCheckIn = attendanceEndDate;
                                            groupType.LastCheckIn = attendanceEndDate;
                                            groupType.Selected = true;
                                            groupType.PreSelected = true;
                                            previousAttender.PreSelected = true;
                                            previousAttender.LastCheckIn = attendanceEndDate;
                                            createdMatchingAssignment = true;
                                        }
                                    }
                                }
                            }
                        }

                        if ( createdMatchingAssignment )
                        {
                            peopleWithoutAssignments--;
                        }
                    }
                }
            }

            // true condition will continue to the next auto-assignment
            // false condition will stop processing auto-assignments
            if ( action.Activity.AttributeValues.Any() && action.Activity.AttributeValues.ContainsKey( "ContinueAssignments" ) )
            {
                var continueAssignments = peopleWithoutAssignments > 0;
                action.Activity.AttributeValues["ContinueAssignments"].Value = continueAssignments.ToString();
            }

            return true;
        }
    }
}