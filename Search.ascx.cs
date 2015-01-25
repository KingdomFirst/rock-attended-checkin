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
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Constants;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.CheckIn.Attended
{
    /// <summary>
    /// Search block for Attended Check-in
    /// </summary>
    [DisplayName( "Search Block" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Search block" )]
    [LinkedPage( "Admin Page" )]
    [BooleanField( "Show Key Pad", "Show the number key pad on the search screen", false )]
    [IntegerField( "Minimum Text Length", "Minimum length for text searches (defaults to 1).", false, 1 )]
    [IntegerField( "Maximum Text Length", "Maximum length for text searches (defaults to 20).", false, 20 )]
    public partial class Search : CheckInBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if ( CurrentKioskId == null || CurrentGroupTypeIds == null || CurrentCheckInState.Kiosk == null )
                {
                    NavigateToHomePage();
                }
                else
                {
                    if ( !CurrentCheckInState.Kiosk.HasLocations( CurrentGroupTypeIds ) || !CurrentCheckInState.Kiosk.HasActiveLocations( CurrentGroupTypeIds ) )
                    {
                        DateTimeOffset activeAt = CurrentCheckInState.Kiosk.FilteredGroupTypes( CurrentGroupTypeIds ).Select( g => g.NextActiveTime ).Min();
                        // not active yet, display next active time
                        return;
                    }
                    else if ( CurrentCheckInState != null && !string.IsNullOrWhiteSpace( CurrentCheckInState.CheckIn.SearchValue ) )
                    {
                        tbSearchBox.Text = CurrentCheckInState.CheckIn.SearchValue;
                    }

                    string script = string.Format( @"
                    <script>
                        $(document).ready(function (e) {{
                            if (localStorage) {{
                                localStorage.checkInKiosk = '{0}';
                                localStorage.checkInGroupTypes = '{1}';
                            }}
                        }});
                    </script>
                    ", CurrentKioskId, CurrentGroupTypeIds.AsDelimited( "," ) );
                    phScript.Controls.Add( new LiteralControl( script ) );

                    if ( bool.Parse( GetAttributeValue( "ShowKeyPad" ) ) == true )
                    {
                        pnlKeyPad.Visible = true;
                    }

                    tbSearchBox.Focus();
                }
            }
        }

        #endregion Control Methods

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the lbSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState != null && CurrentCheckInState.Kiosk != null )
            {
                CurrentCheckInState.CheckIn.Families.Clear();
                CurrentCheckInState.CheckIn.UserEnteredSearch = true;
                CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;

                int minLength = int.Parse( GetAttributeValue( "MinimumTextLength" ) );
                int maxLength = int.Parse( GetAttributeValue( "MaximumTextLength" ) );
                if ( tbSearchBox.Text.Length >= minLength && tbSearchBox.Text.Length <= maxLength )
                {
                    int searchNumber;
                    if ( int.TryParse( tbSearchBox.Text, out searchNumber ) )
                    {
                        CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );
                    }
                    else
                    {
                        CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_NAME );
                    }

                    CurrentCheckInState.CheckIn.SearchValue = tbSearchBox.Text;
                    ProcessSelection( maWarning );
                }
                else
                {
                    string errorMsg = ( tbSearchBox.Text.Length > maxLength )
                        ? string.Format( "<ul><li>Please enter no more than {0} characters</li></ul>", maxLength )
                        : string.Format( "<ul><li>Please enter at least {0} characters</li></ul>", minLength );

                    maWarning.Show( errorMsg, ModalAlertType.Warning );
                }
            }
            else
            {
                maWarning.Show( "This kiosk is not currently active.", ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            //bool selectedFamilyExists = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any();
            //if ( !selectedFamilyExists )
            //{
            //    var queryParams = new Dictionary<string, string>();
            //    queryParams.Add( "back", "true" );
            //    NavigateToLinkedPage( "AdminPage", queryParams );
            //}

            NavigateToPreviousPage();
        }

        #endregion Edit Events
    }
}