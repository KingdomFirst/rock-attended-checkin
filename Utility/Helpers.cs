using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace cc.newspring.AttendedCheckIn.Utility
{
    public static class Helpers
    {
        /// <summary>
        /// Gets the ability list items.
        /// </summary>
        /// <returns></returns>
        public static List<ListItem> GetAbilityItems()
        {
            var abilities = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_ABILITY_LEVEL_TYPE ) ).DefinedValues;

            if ( abilities.Count > 0 )
            {
                return abilities.Select( dv => new ListItem( dv.Value, dv.Guid.ToString() ) ).ToList();
            }

            return new List<ListItem>();
        }

        /// <summary>
        /// Gets the grade items.
        /// </summary>
        /// <returns></returns>
        public static List<ListItem> GetGradeItems()
        {
            var grades = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.SCHOOL_GRADES ) ).DefinedValues;
            if ( grades.Count > 0 )
            {
                return grades.Select( dv => new ListItem( dv.Description, dv.Value.ToString() ) ).ToList();
            }

            return new List<ListItem>();
        }

        /// <summary>
        /// Loads the items.
        /// </summary>
        /// <param name="thisDDL">The this DDL.</param>
        /// <param name="listItems">The list items.</param>
        /// <param name="optionGroupName">Name of the option group.</param>
        public static void LoadItems( this DropDownList thisDDL, List<ListItem> listItems, string optionGroupName = null )
        {
            foreach ( var listItem in listItems )
            {
                if ( optionGroupName != null )
                {
                    listItem.Attributes.Add( "optiongroup", optionGroupName );
                }

                thisDDL.Items.Add( listItem );
            }
        }

        /// <summary>
        /// Loads the ability and grade items.
        /// </summary>
        /// <param name="thisDDL">The this DDL.</param>
        public static void LoadAbilityAndGradeItems( this RockDropDownList thisDDL )
        {
            thisDDL.Items.Clear();
            thisDDL.DataTextField = "Text";
            thisDDL.DataValueField = "Value";
            thisDDL.Items.Add( new ListItem( Rock.Constants.None.Text, Rock.Constants.None.IdValue ) );

            var abilityItems = GetAbilityItems();
            var gradeItems = GetGradeItems();

            thisDDL.LoadItems( abilityItems, "Ability" );
            thisDDL.LoadItems( gradeItems, "Grade" );

            if ( !string.IsNullOrWhiteSpace( thisDDL.Label ) )
            {
                if ( abilityItems.Any() && gradeItems.Any() )
                {
                    thisDDL.Label = "Ability/Grade";
                }
                else if ( abilityItems.Any() )
                {
                    thisDDL.Label = "Ability";
                }
                else
                {
                    thisDDL.Label = "Grade";
                }
            }
        }

        /// <summary>
        /// Reads the attendance cache by schedule.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="scheduleId">The schedule identifier.</param>
        /// <returns></returns>
        public static int ReadAttendanceBySchedule( int locationId, int? scheduleId )
        {
            var attendanceCount = 0;
            var attendanceCache = KioskLocationAttendance.Get( locationId );

            if ( attendanceCache != null )
            {
                if ( scheduleId != null )
                {
                    foreach ( var scheduleAttendance in attendanceCache.Groups.SelectMany( g => g.Schedules ).Where( s => s.ScheduleId == (int)scheduleId ) )
                    {
                        attendanceCount += scheduleAttendance.CurrentCount;
                    }
                }
                else
                {
                    attendanceCount = attendanceCache.CurrentCount;
                }
            }

            return attendanceCount;
        }
    }
}
