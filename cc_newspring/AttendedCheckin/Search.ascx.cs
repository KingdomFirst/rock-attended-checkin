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
using System.Text.RegularExpressions;
using System.Web.UI;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.cc_newspring.AttendedCheckin
{
    /// <summary>
    /// Search block for Attended Check-in
    /// </summary>
    [DisplayName( "Search Block" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Search block" )]
    [LinkedPage( "Admin Page" )]
    [BooleanField( "Show Key Pad", "Show the number key pad on the search screen", false )]
    public partial class Search : CheckInBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( CurrentCheckInState == null || CurrentGroupTypeIds == null )
            {
                NavigateToLinkedPage( "AdminPage" );
                return;
            }

            RegisterRefresh();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack && CurrentCheckInState != null )
            {
                if ( CurrentCheckInType.SearchType.Guid.Equals( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER.AsGuid() ) )
                {
                    pnlKeyPad.Visible = true;
                    tbSearchBox.Placeholder = "Search By Phone";
                }
                else if ( CurrentCheckInType.SearchType.Guid.Equals( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_NAME.AsGuid() ) )
                {
                    pnlKeyPad.Visible = false;
                    tbSearchBox.Placeholder = "Search By Last Name, First Name";
                }
                else
                {
                    pnlKeyPad.Visible = false;
                    tbSearchBox.Placeholder = "Enter Last Name, First Name or Phone";
                }

                if ( !string.IsNullOrWhiteSpace( CurrentCheckInState.CheckIn.SearchValue ) )
                {
                    tbSearchBox.Text = CurrentCheckInState.CheckIn.SearchValue;
                }

                string script = string.Format( @"
            <script>
                $(document).ready(function (e) {{
                    if (localStorage) {{
                        localStorage.theme = '{0}';
                        localStorage.checkInKiosk = '{1}';
                        localStorage.checkInType = '{2}';
                        localStorage.checkInGroupTypes = '{3}';
                    }}
                }});
            </script>
            ", CurrentTheme, CurrentKioskId, CurrentCheckinTypeId, CurrentGroupTypeIds.AsDelimited( "," ) );
                phScript.Controls.Add( new LiteralControl( script ) );

                // make sure the checkin type isn't set to name only
                if ( GetAttributeValue( "ShowKeyPad" ).AsBoolean() && !CurrentCheckInType.SearchType.Guid.Equals( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_NAME.AsGuid() ) )
                {
                    pnlKeyPad.Visible = true;
                }

                tbSearchBox.Focus();
            }
        }

        /// <summary>
        /// Registers a view refresh.
        /// </summary>
        private void RegisterRefresh()
        {
            string script = string.Format( @"
            var timeoutSeconds = {0};
            if (timeout) {{
                window.clearTimeout(timeout);
            }}

            var timeout = window.setTimeout(refreshKiosk, timeoutSeconds * 1000);

            function refreshKiosk() {{
                window.clearTimeout(timeout);
                {1};
            }}
            ", ( CurrentCheckInType != null ? CurrentCheckInType.RefreshInterval.ToString() : "60" ), this.Page.ClientScript.GetPostBackEventReference( lbRefresh, "" ) );

            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "RefreshScript", script, true );

            // Set to null so that object will be recreated with a potentially updated group type cache.
            CurrentCheckInState.CheckInType = null;
        }

        #endregion

        #region Click Events

        /// <summary>
        /// Handles the Click event of the lbSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( KioskCurrentlyActive )
            {
                CurrentCheckInState.CheckIn.Families.Clear();
                CurrentCheckInState.CheckIn.UserEnteredSearch = true;
                CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;

                var searchInput = tbSearchBox.Text;

                // run regex expression on input if provided
                if ( CurrentCheckInType != null && !string.IsNullOrWhiteSpace( CurrentCheckInType.RegularExpressionFilter ) )
                {
                    Regex regex = new Regex( CurrentCheckInType.RegularExpressionFilter );
                    Match match = regex.Match( searchInput );
                    if ( match.Success )
                    {
                        if ( match.Groups.Count == 2 )
                        {
                            searchInput = match.Groups[1].ToString();
                        }
                    }
                }

                // check the type of search
                double searchNumber;
                if ( Double.TryParse( searchInput, out searchNumber ) )
                {
                    CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );
                    int minLength = CurrentCheckInType != null ? CurrentCheckInType.MinimumPhoneSearchLength : 4;
                    int maxLength = CurrentCheckInType != null ? CurrentCheckInType.MaximumPhoneSearchLength : 10;

                    if ( searchInput.Length < minLength || searchInput.Length > maxLength )
                    {
                        string errorMsg = ( searchInput.Length > maxLength )
                            ? string.Format( "<ul><li>Please enter no more than {0} character(s)</li></ul>", maxLength )
                            : string.Format( "<ul><li>Please enter at least {0} character(s)</li></ul>", minLength );

                        maWarning.Show( errorMsg, ModalAlertType.Warning );
                        return;
                    }
                }
                else
                {
                    CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_NAME );
                }

                // remember the current search value
                CurrentCheckInState.CheckIn.SearchValue = searchInput;

                var errors = new List<string>();
                if ( ProcessActivity( "Family Search", out errors ) )
                {
                    SaveState();
                    NavigateToNextPage();
                }
                else
                {
                    string errorMsg = "<ul><li>" + errors.AsDelimited( "</li><li>" ) + "</li></ul>";
                    maWarning.Show( errorMsg.Replace( "'", @"\'" ), ModalAlertType.Warning );
                }
            }
            else
            {
                // Display a warning if the check-in state is active but the schedule is not
                maWarning.Show( "Check-in does not have an active schedule.", ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            bool hasFamilyCheckedIn = CurrentCheckInState.CheckIn.Families.Any( f => f.Selected );
            if ( !hasFamilyCheckedIn )
            {
                var queryParams = new Dictionary<string, string>();
                queryParams.Add( "back", "true" );
                NavigateToLinkedPage( "AdminPage", queryParams );
            }
            else
            {
                NavigateToPreviousPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            // Nothing here, we've already checked the CurrentCheckInState (OnInit)
            if ( CurrentCheckInState == null )
            {
                NavigateToLinkedPage( "AdminPage" );
                return;
            }
        }

        #endregion
    }
}