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
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
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
    [BooleanField( "Remove Attendance On Checkout", "By default, the attendance is given a checkout date.  Select this option to completely remove attendance on checkout.", false )]
    [BooleanField( "Display Child Age/Grade", "By default, the person name is the only thing displayed. Select this option to display age and grade to help with child selections.", false, key: "DisplayChildAgeGrade" )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Designated Single Label", "Select a label to print once per print job.  Unselect the label to print it with every print job.", false )]
    public partial class Confirm : CheckInBlock
    {
        #region Fields

        /// <summary>
        /// Gets or sets a value indicating whether the designated label has already been printed
        /// </summary>
        /// <value>
        /// <c>true</c> if ; otherwise, <c>false</c>.
        /// </value>
        private bool RemoveFromQueue = false;

        /// <summary>
        /// Gets or sets a value indicating whether to run save attendance.
        /// </summary>
        /// <value>
        ///   <c>true</c> if save attendance should be run; otherwise, <c>false</c>.
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

            if ( CurrentWorkflow == null || CurrentCheckInState == null )
            {
                NavigateToHomePage();
            }

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

            if ( !Page.IsPostBack && CurrentCheckInState != null )
            {
                if ( CurrentCheckInState.CheckIn.Families.Count > 0 )
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

            if ( GetAttributeValue( "DisplayChildAgeGrade" ).AsBoolean() )
            {
                gPersonList.Columns[1].Visible = true;
                gPersonList.Columns[2].Visible = true;
            }

            var checkInList = new List<Activity>();
            foreach ( var person in selectedPeopleList )
            {
                foreach ( var groupType in person.GroupTypes.Where( gt => gt.Selected ) )
                {
                    foreach ( var group in groupType.Groups.Where( g => g.Selected ) )
                    {
                        foreach ( var location in group.Locations.Where( l => l.Selected ) )
                        {
                            foreach ( var schedule in location.Schedules.Where( s => s.Selected ) )
                            {
                                var checkIn = new Activity();
                                checkIn.Name = person.Person.FullName;
                                checkIn.Age = person.Person.Age < 18 ? person.Person.Age.ToStringSafe() : string.Empty;
                                checkIn.Location = GetAttributeValue( "DisplayGroupNames" ).AsBoolean()
                                    ? group.Group.Name
                                    : location.Location.Name;
                                checkIn.Schedule = schedule.Schedule.Name;
                                checkIn.PersonId = person.Person.Id;
                                checkIn.GroupId = group.Group.Id;
                                checkIn.LocationId = location.Location.Id;
                                checkIn.ScheduleId = schedule.Schedule.Id;

                                // show "K" when under 1st Grade
                                if ( person.Person.GradeOffset != null )
                                {
                                    var grade = 12 - person.Person.GradeOffset;
                                    checkIn.Grade = grade <= 0 ? "K" : grade.ToString();
                                }

                                // LastCheckin is set to the end time of the current service
                                checkIn.CheckedIn = schedule.LastCheckIn != null && schedule.LastCheckIn > RockDateTime.Now;

                                // V6 NOTE: CreateLabels & SaveAttendance workflows depend on SelectedForSchedule fields
                                // Person.SelectedForSchedule is a subset of Person.PossibleSchedules
                                var personSchedule = person.PossibleSchedules.FirstOrDefault( s => s.Schedule.Id == schedule.Schedule.Id );
                                if ( personSchedule != null )
                                {
                                    personSchedule.Selected = true;
                                    personSchedule.PreSelected = true;
                                }

                                // GroupType.SelectedForSchedule is an actual list, separate from GroupType.PossibleSchedules
                                groupType.SelectedForSchedule.Add( schedule.Schedule.Id );

                                checkInList.Add( checkIn );
                            }
                        }
                    }
                }

                if ( !checkInList.Any( c => c.PersonId == person.Person.Id ) )
                {   // auto assignment didn't select anything
                    var personsAge = person.Person.Age < 18 ? person.Person.Age.ToStringSafe() : string.Empty;
                    checkInList.Add( new Activity { PersonId = person.Person.Id, Name = person.Person.FullName, Age = personsAge, GroupId = 0, LocationId = 0, ScheduleId = 0 } );
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
                    e.Row.Cells[5].Text = "<span class=\"fa fa-check\"/>";
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
                    maAlert.Show( errorMsg.Replace( "'", @"\'" ), ModalAlertType.Warning );
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
                bool removeAttendance = GetAttributeValue( "RemoveAttendanceOnCheckout" ).AsBoolean();

                // run task asynchronously so the UI doesn't slow down
                Task.Run( () =>
                {
                    var rockContext = new RockContext();
                    var today = RockDateTime.Now.Date;
                    var tomorrow = today.AddDays( 1 );
                    var personAttendance = rockContext.Attendances.FirstOrDefault( a => a.StartDateTime >= today
                        && a.StartDateTime < tomorrow
                        && a.LocationId == locationId
                        && a.ScheduleId == scheduleId
                        && a.GroupId == groupId
                        && a.PersonAlias.PersonId == personId
                    );

                    if ( personAttendance != null )
                    {
                        if ( removeAttendance )
                        {
                            rockContext.Attendances.Remove( personAttendance );
                        }
                        else
                        {
                            personAttendance.EndDateTime = RockDateTime.Now;
                        }

                        rockContext.SaveChanges();
                    }
                } );
            }

            var selectedPerson = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected )
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

                if ( !selectedGroups.Any( g => g.Selected ) )
                {
                    selectedPerson.GroupTypes.ForEach( gt => gt.SelectedForSchedule.Clear() );
                    selectedPerson.GroupTypes.ForEach( gt => gt.Selected = false );
                    selectedPerson.GroupTypes.ForEach( gt => gt.PreSelected = false );
                    selectedPerson.PossibleSchedules.ForEach( s => s.Selected = false );
                    selectedPerson.PossibleSchedules.ForEach( s => s.PreSelected = false );
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
            // All family members need attendance now so they also get the same code
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
                    maAlert.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
                    return;
                }

                RunSaveAttendance = false;
            }

            var printQueue = new Dictionary<string, StringBuilder>();
            bool printIndividually = GetAttributeValue( "PrintIndividualLabels" ).AsBoolean();
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
                List<CheckInSchedule> possiblePersonSchedules = null;

                foreach ( DataKey dataKey in checkinArray )
                {
                    var personId = Convert.ToInt32( dataKey["PersonId"] );
                    var groupId = Convert.ToInt32( dataKey["GroupId"] );
                    var locationId = Convert.ToInt32( dataKey["LocationId"] );
                    var scheduleId = Convert.ToInt32( dataKey["ScheduleId"] );

                    int groupTypeId = selectedGroupTypes.Where( gt => gt.Groups.Any( g => g.Group.Id == groupId ) )
                        .Select( gt => gt.GroupType.Id ).FirstOrDefault();
                    availableGroups = selectedGroupTypes.SelectMany( gt => gt.Groups ).ToList();
                    availableLocations = availableGroups.SelectMany( l => l.Locations ).ToList();
                    availableSchedules = availableLocations.SelectMany( s => s.Schedules ).ToList();
                    possiblePersonSchedules = selectedPeople.SelectMany( p => p.PossibleSchedules ).ToList();

                    // Only the current item should be selected in the merge object, unselect everything else
                    if ( printIndividually || checkinArray.Count == 1 )
                    {
                        // Note: This depends on PreSelected being set properly to undo changes later
                        selectedPeople.ForEach( p => p.Selected = ( p.Person.Id == personId ) );
                        selectedGroupTypes.ForEach( gt => gt.Selected = ( gt.GroupType.Id == groupTypeId ) );
                        availableGroups.ForEach( g => g.Selected = ( g.Group.Id == groupId ) );
                        availableLocations.ForEach( l => l.Selected = ( l.Location.Id == locationId ) );
                        availableSchedules.ForEach( s => s.Selected = ( s.Schedule.Id == scheduleId ) );

                        // Unselect the SelectedSchedule properties too
                        possiblePersonSchedules.ForEach( s => s.Selected = ( s.Schedule.Id == scheduleId ) );
                    }

                    // Create labels for however many items are currently selected
                    var labelErrors = new List<string>();
                    if ( ProcessActivity( "Create Labels", out labelErrors ) )
                    {
                        SaveState();
                    }

                    // Add valid grouptype labels, excluding the one-time label (if set)
                    if ( printIndividually )
                    {
                        var selectedPerson = selectedPeople.FirstOrDefault( p => p.Person.Id == personId );
                        if ( selectedPerson != null )
                        {
                            labels.AddRange( selectedPerson.GroupTypes.Where( gt => gt.Labels != null )
                                .SelectMany( gt => gt.Labels )
                                .Where( l => ( !RemoveFromQueue || l.FileGuid != designatedLabelGuid ) )
                            );
                        }

                        RemoveFromQueue = RemoveFromQueue || labels.Any( l => l.FileGuid == designatedLabelGuid );
                    }
                    else
                    {
                        labels.AddRange( selectedGroupTypes.Where( gt => gt.Labels != null )
                            .SelectMany( gt => gt.Labels )
                            .Where( l => ( !RemoveFromQueue || l.FileGuid != designatedLabelGuid ) )
                        );

                        // don't continue processing if printing all info on one label
                        break;
                    }
                }

                // Print client labels
                if ( labels.Any( l => l.PrintFrom == PrintFrom.Client ) )
                {
                    var clientLabels = labels.Where( l => l.PrintFrom == PrintFrom.Client ).ToList();
                    var urlRoot = string.Format( "{0}://{1}", Request.Url.Scheme, Request.Url.Authority );
                    clientLabels.ForEach( l => l.LabelFile = urlRoot + l.LabelFile );
                    AddLabelScript( clientLabels.ToJson() );
                    pnlContent.Update();
                }

                // Print server labels
                if ( labels.Any( l => l.PrintFrom == PrintFrom.Server ) )
                {
                    string delayCut = @"^XB";
                    string endingTag = @"^XZ";
                    var printerIp = string.Empty;
                    var labelContent = new StringBuilder();

                    // make sure labels have a valid ip
                    var lastLabel = labels.Last();
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

                            // send a Delay Cut command at the end to prevent cutting intermediary labels
                            if ( label != lastLabel )
                            {
                                printContent = Regex.Replace( printContent.Trim(), @"\" + endingTag + @"$", delayCut + endingTag );
                            }

                            labelContent.Append( printContent );
                        }
                    }

                    printQueue.AddOrReplace( printerIp, labelContent );

                    if ( printQueue.Any() )
                    {
                        PrintLabels( printQueue );
                        printQueue.Clear();
                    }
                    else
                    {   // give the user feedback when no server labels are configured
                        phPrinterStatus.Controls.Add( new LiteralControl( "No labels were created.  Please verify that the grouptype is configured with labels and cache is reset." ) );
                    }
                }

                if ( printIndividually || checkinArray.Count == 1 )
                {
                    // reset selections to what they were before queue
                    selectedPeople.ForEach( p => p.Selected = p.PreSelected );
                    possiblePersonSchedules.ForEach( s => s.Selected = s.PreSelected );
                    selectedGroupTypes.ForEach( gt => gt.Selected = gt.PreSelected );
                    availableGroups.ForEach( g => g.Selected = g.PreSelected );
                    availableLocations.ForEach( l => l.Selected = l.PreSelected );
                    availableSchedules.ForEach( s => s.Selected = s.PreSelected );
                }

                // since Save Attendance already ran, mark everyone as being checked in
                var selectedSchedules = availableLocations.Where( l => l.Selected )
                    .SelectMany( s => s.Schedules ).Where( s => s.Selected ).ToList();
                foreach ( var selectedSchedule in selectedSchedules )
                {
                    var serviceStart = (DateTime)selectedSchedule.StartTime;
                    selectedSchedule.LastCheckIn = serviceStart.AddMinutes( (double)selectedSchedule.Schedule.CheckInEndOffsetMinutes );
                }
            }

            // refresh the currently checked in flag
            BindGrid();
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
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes( labelContent.ToString() );
                        ns.Write( toSend, 0, toSend.Length );
                    }
                    else
                    {
                        phPrinterStatus.Controls.Add( new LiteralControl( string.Format( "Can't connect to printer: {0}", printerIp ) ) );
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
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(onDeviceReady);
        }}

	    // label data
        var labelData = {0};

		function onDeviceReady() {{
            try {{
                printLabels();
            }}
            catch (err) {{
                console.log('An error occurred printing labels: ' + err);
            }}
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
                        'Error',                // title
                        'Ok'                    // buttonName
                    );
			    }}
            );
	    }}
            ", ZebraFormatString( jsonObject ) );
            ScriptManager.RegisterStartupScript( pnlContent, GetType(), "addLabelScript", script, true );
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
        /// Check-in helper class used to bind the selected grid.
        /// </summary>
        public class Activity
        {
            public int PersonId { get; set; }

            public string Name { get; set; }

            public string Age { get; set; }

            public string Grade { get; set; }

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
