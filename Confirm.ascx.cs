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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.cc_newspring.AttendedCheckin
{
    /// <summary>
    /// Confirmation block for Attended Check-in
    /// </summary>
    [DisplayName( "Confirmation Block" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Confirmation Block" )]
    [LinkedPage( "Activity Select Page" )]
    [BooleanField( "Display Group Names", "By default location names are shown in the grid.  Check this option to show the group names instead.", false )]
    [BooleanField( "Print Individual Labels", "Select this option to print one label per person's group, location, & schedule.", false )]
    [BinaryFileField( "DE0E5C50-234B-474C-940C-C571F385E65F", "Designated Single Label", "Select a label to print once per print job.  Unselect the label to print it with every print job.", false )]
    public partial class Confirm : CheckInBlock
    {
        #region Fields

        /// <summary>
        /// Gets or sets a value indicating whether the label has already been printed
        /// </summary>
        /// <value>
        /// <c>true</c> if [remove label from server queue]; otherwise, <c>false</c>.
        /// </value>
        private bool RemoveFromQueue = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether [run save attendance].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [run save attendance]; otherwise, <c>false</c>.
        /// </value>
        private bool RunSaveAttendance
        {
            get
            {
                var attendanceCodeSet = ViewState["RunSaveAttendance"].ToStringSafe();
                if ( !string.IsNullOrWhiteSpace( attendanceCodeSet ) )
                {
                    return attendanceCodeSet.AsBoolean();
                }

                return true;
            }
            set
            {
                ViewState["RunSaveAttendance"] = value;
            }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( this.Page, "~/Scripts/CheckinClient/cordova-2.4.0.js", false );
            RockPage.AddScriptLink( this.Page, "~/Scripts/CheckinClient/ZebraPrint.js", false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentWorkflow == null || CurrentCheckInState == null )
            {
                NavigateToHomePage();
            }
            else
            {
                if ( !Page.IsPostBack )
                {
                    gPersonList.UseAccessibleHeader = true;
                    BindGrid();
                }
            }
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        protected void BindGrid()
        {
            var selectedPeopleList = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault()
                .People.Where( p => p.Selected ).OrderBy( p => p.Person.FullNameReversed ).ToList();

            var checkInList = new List<Activity>();
            foreach ( var person in selectedPeopleList )
            {
                var selectedGroupTypes = person.GroupTypes.Where( gt => gt.Selected ).ToList();
                if ( selectedGroupTypes.Any() )
                {
                    foreach ( var group in selectedGroupTypes.SelectMany( gt => gt.Groups.Where( g => g.Selected ) ) )
                    {
                        foreach ( var location in group.Locations.Where( l => l.Selected ) )
                        {
                            foreach ( var schedule in location.Schedules.Where( s => s.Selected ) )
                            {
                                var checkIn = new Activity();

                                checkIn.Name = person.Person.FullName;
                                var showGroup = GetAttributeValue( "DisplayGroupNames" ).AsBoolean();
                                checkIn.Location = showGroup ? group.Group.Name : location.Location.Name;
                                checkIn.Schedule = schedule.Schedule.Name;
                                checkIn.PersonId = person.Person.Id;
                                checkIn.GroupId = group.Group.Id;
                                checkIn.LocationId = location.Location.Id;
                                checkIn.ScheduleId = schedule.Schedule.Id;

                                // are they already checked in?
                                checkIn.CheckedIn = schedule.LastCheckIn != null && schedule.LastCheckIn.Value.Date.Equals( DateTime.Today );
                                checkInList.Add( checkIn );
                            }
                        }
                    }
                }
                else
                {   // auto assignment didn't select anything
                    checkInList.Add( new Activity { PersonId = person.Person.Id, Name = person.Person.FullName, GroupId = 0, LocationId = 0, ScheduleId = 0 } );
                }
            }

            gPersonList.DataSource = checkInList.OrderBy( c => c.Name ).ThenBy( c => c.Schedule );
            gPersonList.DataBind();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gPersonList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gPersonList_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                if ( ( (Activity)e.Row.DataItem ).CheckedIn )
                {
                    e.Row.Cells[3].Text = "<span class=\"fa fa-check\"/>";
                }
            }
        }

        #endregion Control Methods

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        /// <summary>
        /// Handles the Click event of the lbNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNext_Click( object sender, EventArgs e )
        {
            if ( RunSaveAttendance )
            {
                var errors = new List<string>();
                if ( ProcessActivity( "Save Attendance", out errors ) )
                {
                    SaveState();
                }
                else
                {
                    string errorMsg = "<ul><li>" + errors.AsDelimited( "</li><li>" ) + "</li></ul>";
                    maWarning.Show( errorMsg.Replace( "'", @"\'" ), ModalAlertType.Warning );
                    return;
                }

                RunSaveAttendance = false;
            }

            // reset search criteria
            CurrentCheckInState.CheckIn.SearchValue = string.Empty;

            SaveState();
            NavigateToNextPage();
        }

        /// <summary>
        /// Handles the Edit event of the gPersonList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gPersonList_Edit( object sender, RowEventArgs e )
        {
            var dataKeyValues = gPersonList.DataKeys[e.RowIndex].Values;
            var queryParams = new Dictionary<string, string>();
            queryParams.Add( "personId", dataKeyValues["PersonId"].ToString() );
            queryParams.Add( "groupId", dataKeyValues["GroupId"].ToString() );
            queryParams.Add( "locationId", dataKeyValues["LocationId"].ToString() );
            queryParams.Add( "scheduleId", dataKeyValues["ScheduleId"].ToString() );
            NavigateToLinkedPage( "ActivitySelectPage", queryParams );
        }

        /// <summary>
        /// Handles the Delete event of the gPersonList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gPersonList_Delete( object sender, RowEventArgs e )
        {
            var dataKeyValues = gPersonList.DataKeys[e.RowIndex].Values;
            var personId = Convert.ToInt32( dataKeyValues["PersonId"] );
            var groupId = Convert.ToInt32( dataKeyValues["GroupId"] );
            var locationId = Convert.ToInt32( dataKeyValues["LocationId"] );
            var scheduleId = Convert.ToInt32( dataKeyValues["ScheduleId"] );
            var alreadyCheckedIn = dataKeyValues["CheckedIn"].ToString().AsBoolean();

            if ( alreadyCheckedIn )
            {
                var rockContext = new RockContext();
                var personAttendance = new AttendanceService( rockContext ).Get( DateTime.Today, locationId, scheduleId, groupId, personId );
                if ( personAttendance != null )
                {
                    personAttendance.DidAttend = false;
                    rockContext.SaveChanges();
                }
            }

            var selectedPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault()
                .People.FirstOrDefault( p => p.Person.Id == personId );

            if ( groupId == 0 || locationId == 0 || scheduleId == 0 )
            {
                selectedPerson.Selected = false;
                selectedPerson.PreSelected = false;
            }
            else
            {
                var selectedGroups = selectedPerson.GroupTypes.Where( gt => gt.Selected )
                    .SelectMany( gt => gt.Groups.Where( g => g.Selected ) ).ToList();
                var selectedGroup = selectedGroups.FirstOrDefault( g => g.Selected && g.Group.Id == groupId );
                var selectedLocation = selectedGroup.Locations.FirstOrDefault( l => l.Selected && l.Location.Id == locationId );
                var selectedSchedule = selectedLocation.Schedules.FirstOrDefault( s => s.Selected && s.Schedule.Id == scheduleId );

                selectedSchedule.Selected = false;
                selectedSchedule.PreSelected = false;

                // clear checkin rows that aren't selected
                if ( !selectedLocation.Schedules.Any( s => s.Selected ) )
                {
                    selectedLocation.Selected = false;
                    selectedLocation.PreSelected = false;
                }

                if ( !selectedGroup.Locations.Any( l => l.Selected ) )
                {
                    selectedGroup.Selected = false;
                    selectedGroup.PreSelected = false;
                }

                if ( !selectedGroups.Any() )
                {
                    selectedPerson.GroupTypes.ForEach( gt => gt.Selected = false );
                    selectedPerson.Selected = false;
                    selectedPerson.PreSelected = false;
                }
            }

            SaveState();
            BindGrid();
        }

        /// <summary>
        /// Handles the RowCommand event of the gPersonList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewCommandEventArgs"/> instance containing the event data.</param>
        protected void gPersonList_Print( object sender, GridViewCommandEventArgs e )
        {
            if ( e.CommandName == "Print" )
            {
                int labelIndex = Convert.ToInt32( e.CommandArgument );
                var singleLabelDataKey = new ArrayList() { gPersonList.DataKeys[labelIndex] };
                ProcessLabels( new DataKeyArray( singleLabelDataKey ) );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbPrintAll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbPrintAll_Click( object sender, EventArgs e )
        {
            ProcessLabels( gPersonList.DataKeys );
        }

        /// <summary>
        /// Handles the GridRebind event of the gPersonList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gPersonList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion Edit Events

        #region Internal Methods

        /// <summary>
        /// Creates the labels.
        /// </summary>
        /// <param name="dataKeyArray">The data key array.</param>
        /// <returns></returns>
        private void ProcessLabels( DataKeyArray checkinArray )
        {
            // Make sure we can save the attendance and get an attendance code
            if ( RunSaveAttendance )
            {
                var attendanceErrors = new List<string>();
                if ( ProcessActivity( "Save Attendance", out attendanceErrors ) )
                {
                    SaveState();
                }
                else
                {
                    string errorMsg = "<ul><li>" + attendanceErrors.AsDelimited( "</li><li>" ) + "</li></ul>";
                    maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
                    return;
                }

                RunSaveAttendance = false;
            }

            var printQueue = new Dictionary<string, StringBuilder>();
            bool printIndividually = !GetAttributeValue( "PrintIndividualLabels" ).AsBoolean();
            var designatedLabelGuid = GetAttributeValue( "DesignatedSingleLabel" ).AsGuidOrNull();

            foreach ( var selectedFamily in CurrentCheckInState.CheckIn.Families.Where( p => p.Selected ) )
            {
                List<CheckInLabel> labels = new List<CheckInLabel>();
                List<CheckInPerson> selectedPeople = selectedFamily.People.Where( p => p.Selected ).ToList();
                List<CheckInGroupType> selectedGroupTypes = selectedPeople.SelectMany( gt => gt.GroupTypes )
                    .Where( gt => gt.Selected ).ToList();
                List<CheckInGroup> availableGroups = null;
                List<CheckInLocation> availableLocations = null;
                List<CheckInSchedule> availableSchedules = null;

                foreach ( DataKey dataKey in checkinArray )
                {
                    var personId = Convert.ToInt32( dataKey["PersonId"] );
                    var groupId = Convert.ToInt32( dataKey["GroupId"] );
                    var locationId = Convert.ToInt32( dataKey["LocationId"] );
                    var scheduleId = Convert.ToInt32( dataKey["ScheduleId"] );

                    // Make sure only the current item is selected in the merge object
                    
                    if ( printIndividually )
                    {   
                        int groupTypeId = selectedGroupTypes.Where( gt => gt.Groups.Any( g => g.Group.Id == groupId ) )
                            .Select( gt => gt.GroupType.Id ).FirstOrDefault();
                        availableGroups = selectedGroupTypes.SelectMany( gt => gt.Groups ).ToList();
                        availableLocations = availableGroups.SelectMany( l => l.Locations ).ToList();
                        availableSchedules = availableLocations.SelectMany( s => s.Schedules ).ToList();

                        selectedPeople.ForEach( p => p.Selected = (p.Person.Id == personId ) );
                        selectedGroupTypes.ForEach( gt => gt.Selected = ( gt.GroupType.Id == groupTypeId ) );
                        availableGroups.ForEach( g => g.Selected = ( g.Group.Id == groupId ) );
                        availableLocations.ForEach( l => l.Selected = ( l.Location.Id == locationId ) );
                        availableSchedules.ForEach( s => s.Selected = ( s.Schedule.Id == scheduleId ) );
                    }

                    // Create labels for however many items are currently selected
                    // #TODO: Rewrite CreateLabels so it would only resolve a list of ID's
                    var labelErrors = new List<string>();
                    if ( ProcessActivity( "Create Labels", out labelErrors ) )
                    {
                        SaveState();
                    }

                    // Add all labels and exclude one-time label
                    labels.AddRange( selectedGroupTypes.SelectMany( gt => gt.Labels )
                        .Where( l => ( !RemoveFromQueue || l.FileGuid != designatedLabelGuid ) ) );
                    RemoveFromQueue = RemoveFromQueue || labels.Any( l => l.FileGuid == designatedLabelGuid );

                    if ( !printIndividually )
                    {
                        // only iterate once if printing the entire family
                        break;
                    }
				}

                // Print client labels
                if ( labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Client ) )
                {
                    var clientLabels = labels.Where( l => l.PrintFrom == PrintFrom.Client ).ToList();
                    var urlRoot = string.Format( "{0}://{1}", Request.Url.Scheme, Request.Url.Authority );
                    clientLabels.ForEach( l => l.LabelFile = urlRoot + l.LabelFile );
                    AddLabelScript( clientLabels.ToJson() );
                }

                // Print server labels
                if ( labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Server ) )
                {
                    var printerIp = string.Empty;
                    var labelContent = new StringBuilder();

                    // make sure labels have a valid ip
                    foreach ( var label in labels.Where( l => l.PrintFrom == PrintFrom.Server && !string.IsNullOrEmpty( l.PrinterAddress ) ) )
                    {
                        var labelCache = KioskLabel.Read( label.FileGuid );
                        if ( labelCache != null )
                        {
                            if ( printerIp != label.PrinterAddress )
                            {
                                printQueue.AddOrReplace( label.PrinterAddress, labelContent );
                                printerIp = label.PrinterAddress;
                                labelContent = new StringBuilder();
                            }

                            var printContent = labelCache.FileContent;

                            foreach ( var mergeField in label.MergeFields )
                            {   
                                if ( !string.IsNullOrWhiteSpace( mergeField.Value ) )
                                {
                                    printContent = Regex.Replace( printContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", mergeField.Key ), ZebraFormatString( mergeField.Value ) );
                                }
                                else
                                {
                                    printContent = Regex.Replace( printContent, string.Format( @"\^FO.*\^FS\s*(?=\^FT.*\^FD{0}\^FS)", mergeField.Key ), string.Empty );
                                    printContent = Regex.Replace( printContent, string.Format( @"\^FD{0}\^FS", mergeField.Key ), "^FD^FS" );
                                }
                            }

                            labelContent.Append( printContent );
                        }
                    }

                    printQueue.AddOrReplace( printerIp, labelContent );
                }

                if ( printQueue.Any() )
                {
                    PrintLabels( printQueue );
                    //printQueue.Clear();
                }

                if ( printIndividually )
                {
                    // reset selections to what they were before queue
                    selectedPeople.ForEach( p => p.Selected = p.PreSelected );
                    selectedGroupTypes.ForEach( gt => gt.Selected = gt.PreSelected );
                    availableGroups.ForEach( g => g.Selected = g.PreSelected );
                    availableLocations.ForEach( l => l.Selected = l.PreSelected );
                    availableSchedules.ForEach( s => s.Selected = s.PreSelected );
                }
            }
        }

        /// <summary>
        /// Prints the labels.
        /// </summary>
        /// <param name="families">The families.</param>
        private void PrintLabels( Dictionary<string, StringBuilder> printerContent )
        {
            foreach ( var printerIp in printerContent.Keys.Where( k => !string.IsNullOrEmpty( k ) ) )
            {
                StringBuilder labelContent;
                if ( printerContent.TryGetValue( printerIp, out labelContent ) )
                {
                    var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                    var printerIpEndPoint = new IPEndPoint( IPAddress.Parse( printerIp ), 9100 );
                    var result = socket.BeginConnect( printerIpEndPoint, null, null );
                    bool success = result.AsyncWaitHandle.WaitOne( 5000, true );

                    if ( socket.Connected )
                    {
                        var ns = new NetworkStream( socket );
                        labelContent.Append( "~JK" );
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes( labelContent.ToString() );
                        ns.Write( toSend, 0, toSend.Length );
                    }
                    else
                    {
                        maWarning.Show( "Could not connect to printer.", ModalAlertType.Warning );
                    }

                    if ( socket != null && socket.Connected )
                    {
                        socket.Shutdown( SocketShutdown.Both );
                        socket.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Adds the label script.
        /// </summary>
        /// <param name="jsonObject">The json object.</param>
        private void AddLabelScript( string jsonObject )
        {
            string script = string.Format( @"

            // setup deviceready event to wait for cordova
	        if (navigator.userAgent.match(/(iPhone|iPod|iPad)/)) {{
                document.addEventListener('deviceready', onDeviceReady, false);
            }} else {{
                $( document ).ready(function() {{
                    onDeviceReady();
                }});
            }}

	        // label data
            var labelData = {0};

		    function onDeviceReady() {{
			    printLabels();
		    }}

		    function alertDismissed() {{
		        // do something
		    }}

		    function printLabels() {{
		        ZebraPrintPlugin.printTags(
            	    JSON.stringify(labelData),
            	    function(result) {{
			            console.log('Tag printed');
			        }},
			        function(error) {{
				        // error is an array where:
				        // error[0] is the error message
				        // error[1] determines if a re-print is possible (in the case where the JSON is good, but the printer was not connected)
			            console.log('An error occurred: ' + error[0]);
                        navigator.notification.alert(
                            'An error occurred while printing the labels.' + error[0],  // message
                            alertDismissed,         // callback
                            'Error',            // title
                            'Ok'                  // buttonName
                        );
			        }}
                );
	        }}
            ", jsonObject );
            ScriptManager.RegisterStartupScript( this, this.GetType(), "addLabelScript", script, true );
        }

        /// <summary>
        /// Formats the Zebra string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="isJson">if set to <c>true</c> [is json].</param>
        /// <returns></returns>
        private static string ZebraFormatString( string input, bool isJson = false )
        {
            if ( isJson )
            {
                return input.Replace( "é", @"\\82" );  // fix acute e
            }
            else
            {
                return input.Replace( "é", @"\82" );  // fix acute e
            }
        }
        

        #endregion Internal Methods

        #region Classes

        /// <summary>
        /// Check-In information class used to bind the selected grid.
        /// </summary>
        protected class Activity
        {
            public int PersonId { get; set; }

            public string Name { get; set; }

            public int GroupId { get; set; }

            public string Location { get; set; }

            public int LocationId { get; set; }

            public int ScheduleId { get; set; }

            public string Schedule { get; set; }

            public bool CheckedIn { get; set; }

            public Activity()
            {
                PersonId = 0;
                Name = string.Empty;
                GroupId = 0;
                Location = string.Empty;
                LocationId = 0;
                Schedule = string.Empty;
                ScheduleId = 0;
                CheckedIn = false;
            }
        }

        #endregion
    }
}