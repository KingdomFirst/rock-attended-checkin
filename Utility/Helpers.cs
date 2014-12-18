using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
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
        public static List<ListItem> GetAbilityListItems()
        {
            var dtAbility = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_ABILITY_LEVEL_TYPE ) );

            if ( dtAbility == null )
            {
                return new List<ListItem>();
            }

            return dtAbility.DefinedValues.Select( dv => new ListItem( dv.Value, dv.Guid.ToString() ) ).ToList();
        }

        /// <summary>
        /// Gets the grade list items.
        /// </summary>
        /// <returns></returns>
        public static List<ListItem> GetGradeListItems()
        {
            return Enum.GetValues( typeof( GradeLevel ) ).Cast<GradeLevel>().OrderBy( gl => (int)gl )
                .Select( g => new ListItem( g.GetDescription(), g.ConvertToString() ) ).ToList();
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
        public static void LoadAbilityAndGradeItems( this DropDownList thisDDL )
        {
            thisDDL.Items.Clear();
            thisDDL.DataTextField = "Text";
            thisDDL.DataValueField = "Value";
            thisDDL.Items.Add( new ListItem( Rock.Constants.None.Text, Rock.Constants.None.IdValue ) );

            thisDDL.LoadItems( GetAbilityListItems(), "Ability" );
            thisDDL.LoadItems( GetGradeListItems(), "Grade" );
        }
    }
}