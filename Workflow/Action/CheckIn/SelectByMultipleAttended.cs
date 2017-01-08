﻿// <copyright>
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
using cc.newspring.AttendedCheckIn.Utility;

namespace cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn
{
    /// <summary>
    /// Calculates and updates the LastCheckIn property on check-in objects
    /// </summary>
    [Description( "Select multiple services from person's previous attendance" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Select By Multiple Services Attended" )]
    [GroupTypesField( "Room Balance Grouptypes", "Select the grouptype(s) you want to room balance. This will auto-assign the group or location (within a grouptype) with the least number of people.", false, order: 0 )]
    [IntegerField( "Balancing Override", "Enter the maximum difference between two locations before room balancing starts to override previous attendance.  The default value is 5.", false, 5, order: 1 )]
    [TextField( "Excluded Locations", "Enter a comma-delimited list of location name(s) to manually exclude from room balancing (like catch-all rooms).", false, "Base Camp", order: 2 )]
    [IntegerField( "Max Assignments", "Enter the maximum number of auto-assignments based on previous attendance.  The default value is 5.", false, 5, order: 3 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Person Special Needs Attribute", "Select the attribute used to filter special needs people.", false, false, "", order: 4 )]
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

            int selectFromDaysBack = checkInState.CheckInType.AutoSelectDaysBack;
            var roomBalanceGroupTypes = GetAttributeValue( action, "RoomBalanceGrouptypes" ).SplitDelimitedValues().AsGuidList();
            int roomBalanceOverride = GetAttributeValue( action, "BalancingOverride" ).AsIntegerOrNull() ?? 5;
            int maxAssignments = GetAttributeValue( action, "MaxAssignments" ).AsIntegerOrNull() ?? 5;
            var excludedLocations = GetAttributeValue( action, "ExcludedLocations" ).SplitDelimitedValues( whitespace: false )
                .Select( s => s.Trim() ).ToList();

            var personSpecialNeedsKey = string.Empty;
            var personSpecialNeedsGuid = GetAttributeValue( action, "PersonSpecialNeedsAttribute" ).AsGuid();
            if ( personSpecialNeedsGuid != Guid.Empty )
            {
                personSpecialNeedsKey = AttributeCache.Read( personSpecialNeedsGuid, rockContext ).Key;
            }

            // log a warning if the attribute is missing or invalid
            if ( string.IsNullOrWhiteSpace( personSpecialNeedsKey ) )
            {
                action.AddLogEntry( string.Format( "The Person Special Needs attribute is not selected or invalid for '{0}'.", action.ActionType.Name ) );
            }

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                var cutoffDate = RockDateTime.Today.AddDays( selectFromDaysBack * -1 );
                var attendanceService = new AttendanceService( rockContext );

                // only process people who have been here before
                foreach ( var previousAttender in family.People.Where( p => p.Selected && !p.FirstTime ) )
                {
                    // get a list of this person's available grouptypes
                    var availableGroupTypeIds = previousAttender.GroupTypes.Select( gt => gt.GroupType.Id ).ToList();

                    // order by most recent attendance
                    var lastDateAttendances = attendanceService.Queryable().Where( a =>
                            a.PersonAlias.PersonId == previousAttender.Person.Id &&
                            availableGroupTypeIds.Contains( a.Group.GroupTypeId ) &&
                            a.StartDateTime >= cutoffDate && a.DidAttend == true )
                        .OrderByDescending( a => a.StartDateTime ).Take( maxAssignments )
                        .ToList();

                    if ( lastDateAttendances.Any() )
                    {
                        var assignmentsGiven = 0;
                        // get the most recent day, then create assignments starting with the earliest attendance record
                        var lastAttended = lastDateAttendances.Max( a => a.StartDateTime ).Date;
                        var numAttendances = lastDateAttendances.Count( a => a.StartDateTime >= lastAttended );
                        foreach ( var groupAttendance in lastDateAttendances.Where( a => a.StartDateTime >= lastAttended ).OrderBy( a => a.Schedule.StartTimeOfDay ) )
                        {
                            bool currentlyCheckedIn = false;
                            var serviceCutoff = groupAttendance.StartDateTime;
                            if ( serviceCutoff > RockDateTime.Now.Date && groupAttendance.Schedule != null )
                            {
                                // calculate the service window to determine if people are still checked in
                                var serviceTime = groupAttendance.StartDateTime.Date + groupAttendance.Schedule.StartTimeOfDay;
                                var serviceStart = serviceTime.AddMinutes( ( groupAttendance.Schedule.CheckInStartOffsetMinutes ?? 0 ) * -1.0 );
                                serviceCutoff = serviceTime.AddMinutes( ( groupAttendance.Schedule.CheckInEndOffsetMinutes ?? 0 ) );
                                currentlyCheckedIn = RockDateTime.Now > serviceStart && RockDateTime.Now < serviceCutoff;
                            }

                            // override exists in case they are currently checked in or have special needs
                            bool useCheckinOverride = currentlyCheckedIn || previousAttender.Person.GetAttributeValue( personSpecialNeedsKey ).AsBoolean();

                            // get a list of room balanced grouptype ID's since CheckInGroup model is a shallow clone
                            var roomBalanceGroupTypeIds = previousAttender.GroupTypes.Where( gt => roomBalanceGroupTypes.Contains( gt.GroupType.Guid ) )
                                .Select( gt => gt.GroupType.Id ).ToList();

                            // start with filtered groups unless they have abnormal age and grade parameters (1%)
                            var groupType = previousAttender.GroupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupAttendance.Group.GroupTypeId && ( !gt.ExcludedByFilter || useCheckinOverride ) );
                            if ( groupType != null )
                            {
                                // assigning the right schedule depends on prior attendance & currently available schedules being sorted
                                var orderedSchedules = groupType.Groups.SelectMany( g => g.Locations.SelectMany( l => l.Schedules ) )
                                    .DistinctBy( s => s.Schedule.Id ).OrderBy( s => s.Schedule.StartTimeOfDay )
                                    .Select( s => s.Schedule.Id ).ToList();

                                int? currentScheduleId = null;
                                if ( orderedSchedules.Count == 1 )
                                {
                                    currentScheduleId = orderedSchedules.FirstOrDefault();
                                }
                                else if ( currentlyCheckedIn )
                                {
                                    // always pick the schedule they're currently checked into
                                    currentScheduleId = orderedSchedules.Where( s => s == groupAttendance.ScheduleId ).FirstOrDefault();
                                }
                                else
                                {
                                    // sort the earliest schedule for the current grouptype, then skip the number of assignments already given (multiple services)
                                    currentScheduleId = groupType.AvailableForSchedule
                                        .OrderBy( d => orderedSchedules.IndexOf( d ) )
                                        .Skip( assignmentsGiven ).FirstOrDefault();
                                }

                                CheckInGroup group = null;
                                if ( groupType.Groups.Count == 1 )
                                {
                                    // only a single group is open
                                    group = groupType.Groups.FirstOrDefault( g => !g.ExcludedByFilter || useCheckinOverride );
                                }
                                else
                                {
                                    // pick the group they last attended, as long as it's open or what they're currently checked into
                                    group = groupType.Groups.FirstOrDefault( g => g.Group.Id == groupAttendance.GroupId && ( !g.ExcludedByFilter || useCheckinOverride ) );

                                    // room balance only on new check-ins and only for the current service
                                    if ( group != null && currentScheduleId != null && roomBalanceGroupTypeIds.Contains( group.Group.GroupTypeId ) && !excludedLocations.Contains( group.Group.Name ) && !useCheckinOverride )
                                    {
                                        // make sure balanced rooms are open for the current service
                                        var currentAttendance = group.Locations.Where( l => l.AvailableForSchedule.Contains( (int)currentScheduleId ) )
                                            .Select( l => Helpers.ReadAttendanceBySchedule( l.Location.Id, currentScheduleId ) ).Sum();

                                        var lowestAttendedGroup = groupType.Groups.Where( g => g.AvailableForSchedule.Contains( (int)currentScheduleId ) )
                                            .Where( g => !g.ExcludedByFilter && !excludedLocations.Contains( g.Group.Name ) )
                                            .Select( g => new { Group = g, Attendance = g.Locations.Select( l => Helpers.ReadAttendanceBySchedule( l.Location.Id, currentScheduleId ) ).Sum() } )
                                            .OrderBy( g => g.Attendance )
                                            .FirstOrDefault();

                                        if ( lowestAttendedGroup != null && lowestAttendedGroup.Attendance < ( currentAttendance - roomBalanceOverride + 1 ) )
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
                                        // only a single location is open
                                        location = group.Locations.FirstOrDefault( l => !l.ExcludedByFilter || useCheckinOverride );
                                    }
                                    else
                                    {
                                        // pick the location they last attended, as long as it's open or what they're currently checked into
                                        location = group.Locations.FirstOrDefault( l => l.Location.Id == groupAttendance.LocationId && ( !l.ExcludedByFilter || useCheckinOverride ) );

                                        // room balance only on new check-ins and only for the current service
                                        if ( location != null && currentScheduleId != null && roomBalanceGroupTypeIds.Contains( group.Group.GroupTypeId ) && !excludedLocations.Contains( location.Location.Name ) && !useCheckinOverride )
                                        {
                                            var currentAttendance = Helpers.ReadAttendanceBySchedule( location.Location.Id, currentScheduleId );

                                            var lowestAttendedLocation = group.Locations.Where( l => l.AvailableForSchedule.Contains( (int)currentScheduleId ) )
                                                .Where( l => !l.ExcludedByFilter && !excludedLocations.Contains( l.Location.Name ) )
                                                .Select( l => new { Location = l, Attendance = Helpers.ReadAttendanceBySchedule( l.Location.Id, currentScheduleId ) } )
                                                .OrderBy( l => l.Attendance )
                                                .FirstOrDefault();

                                            if ( lowestAttendedLocation != null && lowestAttendedLocation.Attendance < ( currentAttendance - roomBalanceOverride + 1 ) )
                                            {
                                                location = lowestAttendedLocation.Location;
                                            }
                                        }
                                    }

                                    if ( location != null )
                                    {
                                        // the current schedule could exist on multiple locations, so pick the one owned by this location
                                        // if the current schedule just closed, get the first available schedule at this location
                                        CheckInSchedule schedule = location.Schedules.OrderByDescending( s => s.Schedule.Id == currentScheduleId ).FirstOrDefault();
                                        if ( schedule != null )
                                        {
                                            // it's impossible to currently be checked in unless these match exactly
                                            if ( group.Group.Id == groupAttendance.GroupId && location.Location.Id == groupAttendance.LocationId && schedule.Schedule.Id == groupAttendance.ScheduleId )
                                            {
                                                // checkout feature either removes the attendance or sets the EndDateTime
                                                var endOfCheckinWindow = groupAttendance.EndDateTime ?? serviceCutoff;
                                                schedule.LastCheckIn = endOfCheckinWindow;
                                                location.LastCheckIn = endOfCheckinWindow;
                                                group.LastCheckIn = endOfCheckinWindow;
                                                groupType.LastCheckIn = endOfCheckinWindow;
                                                previousAttender.LastCheckIn = endOfCheckinWindow;
                                            }

                                            // finished finding assignment, verify everything is selected
                                            schedule.Selected = true;
                                            schedule.PreSelected = true;
                                            location.Selected = true;
                                            location.PreSelected = true;
                                            group.Selected = true;
                                            group.PreSelected = true;
                                            groupType.Selected = true;
                                            groupType.PreSelected = true;
                                            previousAttender.Selected = true;
                                            previousAttender.PreSelected = true;
                                            assignmentsGiven++;
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
