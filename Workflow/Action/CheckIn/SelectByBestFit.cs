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
    [BooleanField( "Prioritize Group Membership", "Auto-assign the group and location where the person is a group member. The default value is no.", false, "", 0 )]
    [BooleanField( "Room Balance", "Auto-assign the location with the least number of current people. This only applies when a person fits into multiple groups or locations.", false, "", 1 )]
    [IntegerField( "Balancing Override", "Enter the maximum difference between two locations before room balancing overrides previous attendance.  The default value is 10.", false, 10, "", 2 )]
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
            
            bool roomBalance = GetAttributeValue( action, "RoomBalance" ).AsBoolean();
            bool useGroupMembership = GetAttributeValue( action, "PrioritizeGroupMembership" ).AsBoolean();
            int balanceOverride = GetAttributeValue( action, "DifferentialOverride" ).AsIntegerOrNull() ?? 10;

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                // don't run for people who already have attendance assignments
                foreach ( var person in family.People.Where( f => f.Selected && !f.GroupTypes.Any( gt => gt.Selected ) ) )
                {
                    decimal baseVariance = 100;
                    char[] delimiter = { ',' };

                    // variable must be a string to compare to group attribute value
                    var specialNeedsValue = person.Person.GetAttributeValue( "IsSpecialNeeds" ).ToStringSafe();
                    var isSpecialNeeds = specialNeedsValue.AsBoolean();

                    if ( person.GroupTypes.Count > 0 )
                    {
                        CheckInGroupType bestGroupType = null;
                        IEnumerable<CheckInGroup> validGroups;
                        if ( person.GroupTypes.Count == 1 )
                        {
                            bestGroupType = person.GroupTypes.FirstOrDefault();
                            validGroups = bestGroupType.Groups;
                        }
                        else
                        {
                            // Start with unfiltered groups since one criteria may not match exactly ( SN > Grade > Age )
                            validGroups = person.GroupTypes.SelectMany( gt => gt.Groups );
                        }

                        // check how many groups exist without getting the whole list
                        int numValidGroups = validGroups.Take( 2 ).Count();
                        if ( numValidGroups > 0 )
                        {
                            CheckInGroup bestGroup = null;
                            IEnumerable<CheckInLocation> validLocations;
                            if ( numValidGroups == 1 )
                            {
                                bestGroup = validGroups.FirstOrDefault();
                                validLocations = bestGroup.Locations;
                            }
                            else
                            {
                                // Select by group assignment first
                                if ( useGroupMembership )
                                {
                                    var personAssignments = new GroupMemberService( rockContext ).GetByPersonId( person.Person.Id )
                                        .Select( gm => gm.Group.Id ).ToList();
                                    if ( personAssignments.Count > 0 )
                                    {
                                        bestGroup = validGroups.FirstOrDefault( g => personAssignments.Contains( g.Group.Id ) );
                                    }
                                }

                                // Select group by best fit
                                if ( bestGroup == null )
                                {
                                    // Check age and special needs
                                    CheckInGroup closestAgeGroup = null;
                                    CheckInGroup closestNeedsGroup = null;

                                    var ageGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( "AgeRange" ) &&
                                            g.Group.AttributeValues.ContainsKey( "IsSpecialNeeds" ) == isSpecialNeeds )
                                        .Select( g => new
                                        {
                                            Group = g,
                                            AgeRange = g.Group.AttributeValues["AgeRange"].Value
                                                .Split( delimiter, StringSplitOptions.None )
                                                .Where( av => !string.IsNullOrEmpty( av ) )
                                                .Select( av => av.AsType<decimal>() )
                                        } )
                                        .ToList();

                                    if ( ageGroups.Count > 0 )
                                    {
                                        if ( person.Person.Age != null )
                                        {
                                            baseVariance = 100;
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

                                                    if ( isSpecialNeeds )
                                                    {
                                                        closestNeedsGroup = closestAgeGroup;
                                                    }
                                                }
                                            }
                                        }
                                        else if ( isSpecialNeeds )
                                        {
                                            // special needs was checked but no age, assign to first special needs group
                                            closestNeedsGroup = ageGroups.FirstOrDefault().Group;
                                        }
                                    }

                                    // Check grade
                                    CheckInGroup closestGradeGroup = null;
                                    if ( person.Person.GradeOffset != null )
                                    {
                                        var gradeValues = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.SCHOOL_GRADES ) ).DefinedValues;
                                        var gradeGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( "GradeRange" ) )
                                            .Select( g => new
                                            {
                                                Group = g,
                                                GradeOffsets = g.Group.AttributeValues["GradeRange"].Value
                                                    .Split( delimiter, StringSplitOptions.None )
                                                    .Where( av => !string.IsNullOrEmpty( av ) )
                                                    .Select( av => gradeValues.FirstOrDefault( v => v.Guid == new Guid( av ) ) )
                                                    .Select( av => av.Value.AsDecimal() )
                                            } )
                                            .ToList();

                                        // Only check groups that have valid grade offsets
                                        if ( person.Person.GradeOffset != null && gradeGroups.Count > 0 )
                                        {
                                            baseVariance = 100;
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

                                    // assignment priority: Ability, then Grade, then Age, then 1st available
                                    bestGroup = closestNeedsGroup ?? closestGradeGroup ?? closestAgeGroup ?? validGroups.FirstOrDefault( g => !g.ExcludedByFilter );

                                    // room balance if they fit into multiple groups
                                    if ( bestGroup != null && roomBalance )
                                    {
                                        var currentGroupAttendance = bestGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum();
                                        var lowestGroup = validGroups.Where( g => !g.ExcludedByFilter )
                                            .Select( g => new { Group = g, Attendance = g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() } )
                                            .OrderBy( g => g.Attendance )
                                            .FirstOrDefault();

                                        if ( lowestGroup != null && lowestGroup.Attendance < ( currentGroupAttendance - balanceOverride ) )
                                        {
                                            bestGroup = lowestGroup.Group;
                                        }
                                    }
                                }

                                validLocations = bestGroup.Locations;
                            }

                            // check how many locations exist without getting the whole list
                            int numValidLocations = validLocations.Take( 2 ).Count();
                            if ( numValidLocations > 0 )
                            {
                                CheckInLocation bestLocation = null;
                                IEnumerable<CheckInSchedule> validSchedules;
                                if ( numValidLocations == 1 )
                                {
                                    bestLocation = validLocations.FirstOrDefault();
                                    validSchedules = bestLocation.Schedules;
                                }
                                else
                                {
                                    var orderedLocations = validLocations.Where( l => !l.ExcludedByFilter && l.Schedules.Any( s => s.Schedule.IsCheckInActive ) );

                                    if ( roomBalance )
                                    {
                                        orderedLocations = orderedLocations.OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount );
                                    }

                                    bestLocation = orderedLocations.FirstOrDefault();
                                    validSchedules = bestLocation.Schedules;
                                }

                                // check how many schedules exist without getting the whole list
                                int numValidSchedules = validSchedules.Take( 2 ).Count();
                                if ( numValidSchedules > 0 )
                                {
                                    var bestSchedule = validSchedules.FirstOrDefault();
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

                                            bestGroupType = person.GroupTypes.FirstOrDefault( gt => gt.GroupType.Id == bestGroup.Group.GroupTypeId );
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