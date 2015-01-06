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
            bool roomBalanceByGroup = bool.Parse( GetAttributeValue( action, "RoomBalanceByGroup" ) ?? "false" );
            bool roomBalanceByLocation = bool.Parse( GetAttributeValue( action, "RoomBalanceByLocation" ) ?? "false" );
            var checkInState = GetCheckInState( entity, out errorMessages );

            if ( checkInState == null )
            {
                return false;
            }

            var sixMonthsAgo = Rock.RockDateTime.Today.AddMonths( -6 );
            var attendanceService = new AttendanceService( rockContext );

            foreach ( var family in checkInState.CheckIn.Families.Where( f => f.Selected ) )
            {
                foreach ( var person in family.People.Where( p => p.Selected ) )
                {
                    var personGroupTypeIds = person.GroupTypes.Select( gt => gt.GroupType.Id );

                    var personAttendances = attendanceService.Queryable()
                        .Where( a =>
                            a.PersonAlias.PersonId == person.Person.Id &&
                            personGroupTypeIds.Contains( a.Group.GroupTypeId ) &&
                            a.StartDateTime >= sixMonthsAgo
                        );

                    if ( personAttendances.Any() )
                    {
                        var lastDate = personAttendances.Max( a => a.StartDateTime ).Date;
                        var lastDateAttendances = personAttendances.Where( a => a.StartDateTime >= lastDate );

                        foreach ( var groupAttendance in lastDateAttendances )
                        {
                            var groupType = person.GroupTypes.FirstOrDefault( t => t.GroupType.Id == groupAttendance.Group.GroupTypeId );

                            if ( groupType != null )
                            {
                                CheckInGroup group = null;

                                if ( groupType.Groups.Count == 1 )
                                {
                                    group = groupType.Groups.FirstOrDefault();
                                }
                                else if ( roomBalanceByGroup )
                                {
                                    group = groupType.Groups.OrderBy( g => g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() ).FirstOrDefault();
                                }
                                else
                                {
                                    group = groupType.Groups.FirstOrDefault( g => g.Group.Id == groupAttendance.GroupId );
                                }

                                if ( group != null )
                                {
                                    CheckInLocation location = null;

                                    if ( group.Locations.Count == 1 )
                                    {
                                        location = group.Locations.FirstOrDefault();
                                    }
                                    else if ( roomBalanceByLocation )
                                    {
                                        location = group.Locations.Where( l => l.Schedules.Any( s => !s.ExcludedByFilter && s.Schedule.IsCheckInActive ) )
                                                .OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                                    }
                                    else
                                    {
                                        location = group.Locations.FirstOrDefault( l => l.Location.Id == groupAttendance.LocationId );
                                    }

                                    if ( location != null )
                                    {
                                        var schedule = location.Schedules.FirstOrDefault( s => s.Schedule.Id == groupAttendance.ScheduleId );

                                        if ( schedule != null )
                                        {
                                            schedule.Selected = true;
                                            location.Selected = true;
                                            group.Selected = true;
                                            groupType.Selected = true;
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