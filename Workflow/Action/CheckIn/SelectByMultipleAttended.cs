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
    [BooleanField( "Room Balance", "Auto-assign the location with the least number of current people. This only applies when a person fits into multiple groups or locations.", false, "", 0 )]
    [IntegerField( "Balancing Override", "Enter the maximum difference between two locations before room balancing overrides previous attendance.  The default value is 10.", false, 10, "", 1 )]
    [IntegerField( "Previous Months Attendance", "Enter the number of previous months to look for attendance history.  The default value is 3 months.", false, 3, "", 2 )]
    [IntegerField( "Max Assignments", "Enter the maximum number of auto-assignments based on previous attendance.  The default value is 5.", false, 5, "", 3 )]
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

            var cutoffDate = Rock.RockDateTime.Today.AddMonths( previousMonthsNumber * -1 );
            var attendanceService = new AttendanceService( rockContext );

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                // get the number of people checking in, including visitors or first-timers
                peopleWithoutAssignments = family.People.Where( p => p.Selected ).Count();

                foreach ( var previousAttender in family.People.Where( p => p.Selected && !p.FirstTime ) )
                {
                    var personGroupTypeIds = previousAttender.GroupTypes.Select( gt => gt.GroupType.Id );

                    var lastDateAttendances = attendanceService.Queryable()
                        .Where( a =>
                            a.PersonAlias.PersonId == previousAttender.Person.Id &&
                            personGroupTypeIds.Contains( a.Group.GroupTypeId ) &&
                            a.StartDateTime >= cutoffDate && a.DidAttend == true )
                        .OrderByDescending( a => a.StartDateTime ).Take( maxAssignments )
                        .ToList();

                    if ( lastDateAttendances.Any() )
                    {
                        bool createdMatchingAssignment = false;
                        var isSpecialNeeds = previousAttender.Person.GetAttributeValue( "IsSpecialNeeds" ).AsBoolean();

                        var lastAttended = lastDateAttendances.Max( a => a.StartDateTime ).Date;
                        foreach ( var groupAttendance in lastDateAttendances.Where( a => a.StartDateTime >= lastAttended ) )
                        {
                            // Start with filtered groups unless they have abnormal age and grade parameters (1%)
                            var groupType = previousAttender.GroupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupAttendance.Group.GroupTypeId && ( !gt.ExcludedByFilter || isSpecialNeeds ) );
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

                                    if ( group != null && roomBalance && !isSpecialNeeds )
                                    {
                                        var currentAttendance = group.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum();
                                        var lowestAttendedGroup = groupType.Groups.Where( g => !g.ExcludedByFilter )
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
                                        location = group.Locations.FirstOrDefault( l => !l.ExcludedByFilter || isSpecialNeeds );
                                    }
                                    else
                                    {
                                        // Pick the location they last attended
                                        location = group.Locations.FirstOrDefault( l => l.Location.Id == groupAttendance.LocationId && ( !l.ExcludedByFilter || isSpecialNeeds ) );

                                        if ( location != null && roomBalance && !isSpecialNeeds )
                                        {
                                            var currentAttendance = KioskLocationAttendance.Read( location.Location.Id ).CurrentCount;
                                            var lowestAttendedLocation = group.Locations.Where( l => !l.ExcludedByFilter )
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
                                            previousAttender.LastCheckIn = groupAttendance.StartDateTime;
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