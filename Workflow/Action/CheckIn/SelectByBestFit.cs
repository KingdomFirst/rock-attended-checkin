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
    [BooleanField( "Prioritize Group Membership", "Auto-assign the group and location where the person is a group member.", false, "", 0 )]
    [GroupTypesField( "Room Balance Grouptypes", "Select the grouptype(s) you want to room balance. This will auto-assign the group or location (within a grouptype) with the least number of people.", false, order: 1 )]
    [IntegerField( "Balancing Override", "Enter the maximum difference between two locations before room balancing overrides previous attendance.  The default value is 5.", false, 5, "", 2 )]
    [TextField( "Excluded Locations", "Enter a comma-delimited list of location name(s) to manually exclude from room balancing (like catch-all rooms).", false, "Base Camp", order: 3 )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Person Special Needs Attribute", "Select the attribute used to filter special needs people.", false, false, "", order: 4 )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Group Special Needs Attribute", "Select the attribute used to filter special needs groups.", false, false, "", order: 5 )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Group Age Range Attribute", "Select the attribute used to define the age range of the group", false, false, "43511B8F-71D9-423A-85BF-D1CD08C1998E", order: 6 )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Group Grade Range Attribute", "Select the attribute used to define the grade range of the group", false, false, "C7C028C2-6582-45E8-839D-5C4467C6FDF4", order: 7 )]
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

            var roomBalanceGroupTypes = GetAttributeValue( action, "RoomBalanceGrouptypes" ).SplitDelimitedValues().AsGuidList();
            bool useGroupMembership = GetAttributeValue( action, "PrioritizeGroupMembership" ).AsBoolean();
            int roomBalanceOverride = GetAttributeValue( action, "BalancingOverride" ).AsIntegerOrNull() ?? 5;
            var excludedLocations = GetAttributeValue( action, "ExcludedLocations" ).SplitDelimitedValues( false )
                .Select( s => s.Trim() );

            // get admin-selected attribute keys instead of using a hardcoded key
            var personSpecialNeedsKey = string.Empty;
            var personSpecialNeedsGuid = GetAttributeValue( action, "PersonSpecialNeedsAttribute" ).AsGuid();
            if ( personSpecialNeedsGuid != Guid.Empty )
            {
                personSpecialNeedsKey = AttributeCache.Read( personSpecialNeedsGuid, rockContext ).Key;
            }

            var groupSpecialNeedsKey = string.Empty;
            var groupSpecialNeedsGuid = GetAttributeValue( action, "GroupSpecialNeedsAttribute" ).AsGuid();
            if ( personSpecialNeedsGuid != Guid.Empty )
            {
                groupSpecialNeedsKey = AttributeCache.Read( groupSpecialNeedsGuid, rockContext ).Key;
            }

            var groupAgeRangeKey = string.Empty;
            var groupAgeRangeGuid = GetAttributeValue( action, "GroupAgeRangeAttribute" ).AsGuid();
            if ( personSpecialNeedsGuid != Guid.Empty )
            {
                groupAgeRangeKey = AttributeCache.Read( groupAgeRangeGuid, rockContext ).Key;
            }

            var groupGradeRangeKey = string.Empty;
            var groupGradeRangeGuid = GetAttributeValue( action, "GroupGradeRangeAttribute" ).AsGuid();
            if ( personSpecialNeedsGuid != Guid.Empty )
            {
                groupGradeRangeKey = AttributeCache.Read( groupGradeRangeGuid, rockContext ).Key;
            }

            // log a warning if any of the attributes are missing or invalid
            if ( string.IsNullOrWhiteSpace( personSpecialNeedsKey ) )
            {
                action.AddLogEntry( string.Format( "The Person Special Needs attribute is not selected or invalid for '{0}'.", action.ActionType.Name ) );
            }

            if ( string.IsNullOrWhiteSpace( groupSpecialNeedsKey ) )
            {
                action.AddLogEntry( string.Format( "The Group Special Needs attribute is not selected or invalid for '{0}'.", action.ActionType.Name ) );
            }

            if ( string.IsNullOrWhiteSpace( groupAgeRangeKey ) )
            {
                action.AddLogEntry( string.Format( "The Group Age Range attribute is not selected or invalid for '{0}'.", action.ActionType.Name ) );
            }

            if ( string.IsNullOrWhiteSpace( groupGradeRangeKey ) )
            {
                action.AddLogEntry( string.Format( "The Group Grade Range attribute is not selected or invalid for '{0}'.", action.ActionType.Name ) );
            }

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                // don't process people who already have assignments
                foreach ( var person in family.People.Where( f => f.Selected && !f.GroupTypes.Any( gt => gt.Selected ) ) )
                {
                    decimal baseVariance = 100;
                    char[] delimiter = { ',' };

                    // check if this person has special needs
                    var hasSpecialNeeds = person.Person.GetAttributeValue( personSpecialNeedsKey ).AsBoolean();

                    // get a list of room balanced grouptype ID's since CheckInGroup model is a shallow clone
                    var roomBalanceGroupTypeIds = person.GroupTypes.Where( gt => roomBalanceGroupTypes.Contains( gt.GroupType.Guid ) )
                        .Select( gt => gt.GroupType.Id ).ToList();

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

                                    var ageGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( groupAgeRangeKey )
                                            && g.Group.AttributeValues[groupAgeRangeKey].Value != null
                                            && g.Group.AttributeValues.ContainsKey( personSpecialNeedsKey ) == hasSpecialNeeds
                                        )
                                        .ToList()
                                        .Select( g => new
                                        {
                                            Group = g,
                                            AgeRange = g.Group.AttributeValues[groupAgeRangeKey].Value
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

                                                    if ( hasSpecialNeeds )
                                                    {
                                                        closestNeedsGroup = closestAgeGroup;
                                                    }
                                                }
                                            }
                                        }
                                        else if ( hasSpecialNeeds )
                                        {
                                            // person has special needs but not an age, assign to first special needs group
                                            closestNeedsGroup = ageGroups.FirstOrDefault().Group;
                                        }
                                    }

                                    // Check grade
                                    CheckInGroup closestGradeGroup = null;
                                    if ( person.Person.GradeOffset != null )
                                    {
                                        var gradeValues = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.SCHOOL_GRADES ) ).DefinedValues;
                                        var gradeGroups = validGroups.Where( g => g.Group.AttributeValues.ContainsKey( groupGradeRangeKey ) && g.Group.AttributeValues[groupGradeRangeKey].Value != null )
                                            .ToList()
                                            .Select( g => new
                                            {
                                                Group = g,
                                                GradeOffsets = g.Group.AttributeValues[groupGradeRangeKey].Value
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

                                    // Assignment priority: Group Membership, then Ability, then Grade, then Age, then the first non-excluded group
                                    // NOTE: if group member is prioritized (and membership exists) this section is skipped entirely
                                    bestGroup = closestNeedsGroup ?? closestGradeGroup ?? closestAgeGroup ?? validGroups.FirstOrDefault( g => !g.ExcludedByFilter );

                                    // room balance if they fit into multiple groups
                                    if ( bestGroup != null && roomBalanceGroupTypeIds.Contains( bestGroup.Group.GroupTypeId ) )
                                    {
                                        var currentGroupAttendance = bestGroup.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum();
                                        var lowestGroup = validGroups.Where( g => !g.ExcludedByFilter && !excludedLocations.Contains( g.Group.Name ) )
                                            .Select( g => new { Group = g, Attendance = g.Locations.Select( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).Sum() } )
                                            .OrderBy( g => g.Attendance )
                                            .FirstOrDefault();

                                        if ( lowestGroup != null && lowestGroup.Attendance < ( currentGroupAttendance - roomBalanceOverride ) )
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
                                    var filteredLocations = validLocations.Where( l => !l.ExcludedByFilter && !excludedLocations.Contains( l.Location.Name ) && l.Schedules.Any( s => s.Schedule.IsCheckInActive ) );

                                    // room balance if they fit into multiple locations
                                    if ( roomBalanceGroupTypeIds.Contains( bestGroup.Group.GroupTypeId ) )
                                    {
                                        filteredLocations = filteredLocations.OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount );
                                    }

                                    bestLocation = filteredLocations.FirstOrDefault();
                                    validSchedules = bestLocation.Schedules;
                                }

                                // check how many schedules exist without getting the whole list
                                int numValidSchedules = validSchedules.Take( 2 ).Count();
                                if ( numValidSchedules > 0 )
                                {
                                    // finished finding assignment, verify everything is selected
                                    var bestSchedule = validSchedules.OrderBy( s => s.Schedule.StartTimeOfDay ).FirstOrDefault();
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
                                                person.Selected = true;
                                                person.PreSelected = true;
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