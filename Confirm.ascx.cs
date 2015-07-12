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
        /// Gets or sets a value indicating whether [remove label from client queue].
        /// </summary>
        /// <value>
        /// <c>true</c> if [remove label from client queue]; otherwise, <c>false</c>.
        /// </value>
        private bool RemoveLabelFromClientQueue
        {
            get
            {
                var labelAlreadyPrinted = ViewState["RemoveLabelFromClientQueue"].ToStringSafe();
                if ( !string.IsNullOrWhiteSpace( labelAlreadyPrinted ) )
                {
                    return labelAlreadyPrinted.AsBoolean();
                }

                return false;
            }
            set
            {
                ViewState["RemoveLabelFromClientQueue"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [remove label from server queue].
        /// </summary>
        /// <value>
        /// <c>true</c> if [remove label from server queue]; otherwise, <c>false</c>.
        /// </value>
        private bool RemoveLabelFromServerQueue
        {
            get
            {
                var labelAlreadyPrinted = ViewState["RemoveLabelFromServerQueue"].ToStringSafe();
                if ( !string.IsNullOrWhiteSpace( labelAlreadyPrinted ) )
                {
                    return labelAlreadyPrinted.AsBoolean();
                }

                return false;
            }
            set
            {
                ViewState["RemoveLabelFromServerQueue"] = value;
            }
        }

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
                var selectedGroup = selectedGroups.Where( g => g.Selected && g.Group.Id == groupId ).FirstOrDefault();
                var selectedLocation = selectedGroup.Locations.Where( l => l.Selected && l.Location.Id == locationId ).FirstOrDefault();
                var selectedSchedule = selectedLocation.Schedules.Where( s => s.Selected && s.Schedule.Id == scheduleId ).FirstOrDefault();

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

            var rockContext = new RockContext();
            var printQueue = new Dictionary<string, StringBuilder>();
            var globalAttributes = GlobalAttributesCache.Read( rockContext );
            var globalMergeValues = GlobalAttributesCache.GetMergeFields( null );

            bool printAll = !GetAttributeValue( "PrintIndividualLabels" ).AsBoolean();
            var designatedLabelGuid = GetAttributeValue( "DesignatedSingleLabel" ).AsGuidOrNull();

            foreach ( var selectedFamily in CurrentCheckInState.CheckIn.Families.Where( p => p.Selected ) )
            {
                List<CheckInPerson> selectedPeople = selectedFamily.People.Where( p => p.Selected ).ToList();
                List<CheckInGroupType> selectedGroupTypes = selectedPeople.SelectMany( gt => gt.GroupTypes ).Where( gt => gt.Selected ).ToList();
                List<CheckInGroup> selectedGroups = null;
                List<CheckInLocation> selectedLocations = null;
                List<CheckInSchedule> selectedSchedules = null;

                foreach ( DataKey dataKey in checkinArray )
                {
                    var personId = Convert.ToInt32( dataKey["PersonId"] );
                    var groupId = Convert.ToInt32( dataKey["GroupId"] );
                    var locationId = Convert.ToInt32( dataKey["LocationId"] );
                    var scheduleId = Convert.ToInt32( dataKey["ScheduleId"] );
 
                    if ( !printAll )
                    {
                        // unselect everything 
                        selectedPeople.ForEach( p => p.Selected = false );
                        selectedGroupTypes.ForEach( gt => gt.Selected = false );
                        selectedGroups = selectedGroupTypes.SelectMany( gt => gt.Groups ).ToList();
                        selectedGroups.ForEach( g => g.Selected = false );
                        selectedLocations = selectedGroups.SelectMany( g => g.Locations ).ToList();
                        selectedLocations.ForEach( l => l.Selected = false );
                        selectedSchedules = selectedLocations.SelectMany( l => l.Schedules ).ToList();
                        selectedSchedules.ForEach( s => s.Selected = false );

                        // select only the current data key
                        var selectedPerson = selectedFamily.People.FirstOrDefault( p => p.Person.Id == personId );
                        if ( selectedPerson != null )
                        {
                            var selectedGroupType = selectedPerson.GroupTypes.FirstOrDefault( gt => gt.Groups.Any( g => g.Group.Id == groupId ) );
                            if ( selectedGroupType != null )
                            {
                                var selectedGroup = selectedGroupType.Groups.FirstOrDefault( g => g.Group.Id == groupId );
                                if ( selectedGroup != null )
                                {
                                    var selectedLocation = selectedGroup.Locations.FirstOrDefault( l => l.Location.Id == locationId );
                                    if ( selectedLocation != null )
                                    {
                                        var selectedSchedule = selectedLocation.Schedules.FirstOrDefault( s => s.Schedule.Id == scheduleId );
                                        if ( selectedSchedule != null )
                                        {
                                            // make sure selected & preselected are set (to reset later)
                                            selectedPerson.Selected = selectedPerson.PreSelected = true;
                                            selectedGroupType.Selected = selectedGroupType.PreSelected = true;
                                            selectedGroup.Selected = selectedGroup.PreSelected = true;
                                            selectedLocation.Selected = selectedLocation.PreSelected = true;
                                            selectedSchedule.Selected = selectedSchedule.PreSelected = true;

                                            selectedPeople = new List<CheckInPerson>( 1 ) { selectedPerson };
                                            selectedGroupTypes = new List<CheckInGroupType>( 1 ) { selectedGroupType };
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // NOTE: when printing individual labels, this populates the merge
                    // codes for a single person at a time (hence the print queue)
                    var labelErrors = new List<string>();
                    if ( ProcessActivity( "Create Labels", out labelErrors ) )
                    {
                        SaveState();
                    }
                    else
                    {
                        string errorMsg = "<ul><li>" + labelErrors.AsDelimited( "</li><li>" ) + "</li></ul>";
                        maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
                        return;
                    }

                    foreach ( var groupType in selectedGroupTypes )
                    {
                        // create the labels
                        var printFromClient = groupType.Labels.Where( l => l.PrintFrom == Rock.Model.PrintFrom.Client );

                        if ( RemoveLabelFromClientQueue )
                        {
                            printFromClient = printFromClient.Where( l => l.FileGuid != designatedLabelGuid );
                        }

                        if ( printFromClient.Any() )
                        {
                            var urlRoot = string.Format( "{0}://{1}", Request.Url.Scheme, Request.Url.Authority );
                            printFromClient.ToList().ForEach( l => l.LabelFile = urlRoot + l.LabelFile );
                            AddLabelScript( printFromClient.ToJson() );

                            // Remove from future client queues
                            RemoveLabelFromClientQueue = RemoveLabelFromClientQueue || printFromClient.Any( l => l.FileGuid == designatedLabelGuid );
                        }

                        var printFromServer = groupType.Labels.Where( l => l.PrintFrom == Rock.Model.PrintFrom.Server );
                        if ( RemoveLabelFromServerQueue )
                        {
                            printFromServer = printFromServer.Where( l => l.FileGuid != designatedLabelGuid );
                        }

                        if ( printFromServer.Any() )
                        {
                            var labelContent = new StringBuilder();

                            foreach ( var label in printFromServer.Where( p => !string.IsNullOrEmpty( p.PrinterAddress ) ).OrderBy( l => l.PrinterAddress ) )
                            {
                                var labelCache = KioskLabel.Read( label.FileGuid );
                                if ( labelCache != null )
                                {
                                    bool useExistingQueue = printQueue.ContainsKey( label.PrinterAddress );

                                    if ( useExistingQueue )
                                    {
                                        printQueue.TryGetValue( label.PrinterAddress, out labelContent );
                                    }

                                    var printContent = labelCache.FileContent;

                                    foreach ( var mergeField in label.MergeFields )
                                    {
                                        printContent = Regex.Replace( printContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", mergeField.Key ), mergeField.Value );

                                        //if ( !string.IsNullOrWhiteSpace( mergeField.Value ) )
                                        //{
                                        //    printContent = Regex.Replace( printContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", mergeField.Key ), ZebraFormatString( mergeField.Value ) );
                                        //}
                                        //else
                                        //{
                                        //    // Remove the box preceding merge field
                                        //    printContent = Regex.Replace( printContent, string.Format( @"\^FO.*\^FS\s*(?=\^FT.*\^FD{0}\^FS)", mergeField.Key ), string.Empty );
                                        //    // Remove the merge field
                                        //    printContent = Regex.Replace( printContent, string.Format( @"\^FD{0}\^FS", mergeField.Key ), "^FD^FS" );
                                        //}
                                    }

                                    labelContent.Append( printContent );
                                    RemoveLabelFromServerQueue = RemoveLabelFromServerQueue || label.FileGuid == designatedLabelGuid;

                                    if ( useExistingQueue )
                                    {
                                        printQueue[label.PrinterAddress] = labelContent;
                                    }
                                    else
                                    {
                                        printQueue.Add( label.PrinterAddress, labelContent );                                            
                                    }
                                }
                            }
                        }
                    }

                    if ( printAll )
                    {
                        // only loop through datakeys once if printing the entire family
                        break;
                    }

				} // end data keys

                if ( printQueue.Any() )
                {
                    PrintLabels( printQueue );
                    printQueue.Clear();
                }

                if ( !printAll )
                {
                    // reset selections to what they were before queue loop
                    selectedPeople.ForEach( p => p.Selected = p.PreSelected );
                    selectedGroupTypes.ForEach( gt => gt.Selected = gt.PreSelected );
                    selectedGroups.ForEach( g => g.Selected = g.PreSelected );
                    selectedLocations.ForEach( l => l.Selected = l.PreSelected );
                    selectedSchedules.ForEach( s => s.Selected = s.PreSelected );
                }

            } // end family
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