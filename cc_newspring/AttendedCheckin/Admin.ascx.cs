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
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web;
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
    /// Admin block for Attended Check-in
    /// </summary>
    [DisplayName( "Check-in Administration" )]
    [Category( "Check-in > Attended" )]
    [Description( "Check-In Administration block" )]
    [BooleanField( "Enable Location Sharing", "If enabled, the block will attempt to determine the kiosk's location via location sharing geocode.", false, "Geo Location", 0 )]
    [IntegerField( "Time to Cache Kiosk GeoLocation", "Time in minutes to cache the coordinates of the kiosk. A value of zero (0) means cache forever. Default 20 minutes.", false, 20, "Geo Location", 1 )]
    [MemoField( "Test Label Content", "Enter the label content to use for test print jobs.  Press Ctrl + I on the Admin screen to send a test print.", true, @"﻿CT~~CD,~CC^~CT~^XA
        ^FO150,75^GFA,2546,2546,38,M07FF8,L0KF8,K07LF,J01MFE,J07NF8,J0OFE,I03PF,I07PF8,I0QFCL01FC0FE07IFC3F81FC1FC01FE007FFC007FFE003F83F01FC007FC,003RFL01FC0FE0JFC3FC3FC1FC07FF807IF807IF803F87F81FC01IF,007RFL01FE0FE0JFC1FC3FC1FC1IFC07IFC07IFE03F87FC1FC07IF8,007RF8K01FF0FE0JFC1FC3FE3FC1IFE07IFE07JF03F87FC1FC0JFC,00SFCK01FF0FE0JFC1FC3FE3F83IFE07JF07JF03F87FE1FC0JFE,01SFEK01FF8FE0JFC1FC7FE3F83FCFF07JF07JF83F87FF1FC1JFE,03TFK01FFCFE0FFJ0FE7FE3F83F87F07F07F87F07F83F87FF1FC1FE0FE,07TFK01FFCFE0FFJ0FE7FF3F83FCI07F07F87F03F83F87FF9FC1FC07F,07TF8J01FFEFE0JF00FE7FF3F03FF8007F03F87F03F83F87FFDFC3FC,0UF8J01JFE0JF80FEIF7F03IF807F07F87F03F83F87FFDFC3FC,0UFCJ01JFE0JF80FEFDF7F01IFE07F07F87F07F83F87JFC3FC3FF,1UFCJ01JFE0JF807FFDIF00JF07JF07JF83F87JFC3FC7FF,1JFL07JFCJ01JFE0JF807FF9IF007IF07JF07JF03F87JFC3FC7FF,3JFL01JFEJ01FDFFE0JF807FF9FFE001IF87IFE07JF03F87F7FFC3FC7FF,3JFM0JFEJ01FDFFE0FFJ07FF8FFEJ0FF87IFC07IFE03F87F3FFC3FC7FF,3JFM07IFEJ01FCFFE0FFJ03FF0FFE03F03F87IF807IFC03F87F9FFC1FC3FF,7JFM03JFJ01FCFFE0FFJ03FF0FFE03F83F87FFE007IFC03F87F9FFC1FE0FF,7JFM01JFJ01FC7FE0JFC03FF07FC03FC7F87FJ07F3FC03F87F8FFC1FF1FE,7JFM01JFJ01FC3FE0JFC03FF07FC03JF07FJ07F1FE03F87F8FFC0JFE,7JFN0JFJ01FC3FE0JFC01FE07FC01JF07FJ07F1FF03F87F87FC07IFC,KFI03EI0JFJ01FC1FE0JFC01FE07FC00IFE07FJ07F0FF03F87F83FC03IF8,KFI0FFI0JFJ01FC0FE0JFC01FE03F8007FF807FJ07F07F83F87F83FC01IF,KF001FF800JFJ01F807E07IF800FC03F8001FE007FJ07F07F83F83F01FC007F8,KF001FF800JF,::::::::7JF001FF800JFL0FFC00FE07F07F03F83IF8I0FFC00FE07F,7JF001FF800JFK03FFE00FE07F07F03F83IFE003IF00FE07F,7JF001FF800JFK07IF80FE07F07F03F83JF807IF80FE07F,7JF001FF800JFK0JF80FE07F07F03F83JF80JFC0FE07F,3JF001FF800JFJ01JFC0FE07F07F03F83JFC0JFE0FE07F,3JF001FF800JFJ01JFE0FE07F07F03F83JFC1JFE0FE07F,3JF001FF800JFJ03FC1FE0FE07F07F03F83FC1FE1FE0FE0FE07F,1JF001FF800JFJ03FC0FE0FE07F07F03F83FC1FE1FC07F0FE07F,1JF001FF800JFJ03F80FE0KF07F03F83FC0FE1FC07F0KF,1JF001FF800JFJ03F8J0KF07F03F83FC1FE1FCJ0KF,0VFJ03F8J0KF07F03F83FC1FE1FCJ0KF,0VFJ03F8J0KF07F03F83JFC1FCJ0KF,07UFJ03F8J0KF07F03F83JFC1FCJ0KF,03UFJ03F8J0KF07F03F83JF81FCJ0KF,03UFJ03F80FE0FE2FF07F03F83JF01FC07F0FF47F,01UFJ03FC0FE0FE07F07F03F83IFE01FC0FF0FE07F,00UFJ01FE1FE0FE07F07F87F83JF01FE1FE0FE07F,007TFJ01JFC0FE07F07JF83FCFF01JFE0FE07F,003TFJ01JFC0FE07F03JF03FC7F80JFC0FE07F,001TFK0JF80FE07F01JF03FC7FC07IFC0FE07F,I0TFK07IF00FE07F00IFE03FC3FC03IF80FE07F,I03SFK01FFE00FE07F007FF803FC3FE01FFE00FE07F,I01SFL07FQ0FEJ08002003F8,J07RF,K0RF,K01QF,M0PF,^FS
        ^PW609
        ^FT30,230^A0N,30,30^FH\^FDThe only way to test is in production.^FS
        ^FT400,275^A0N,25,25^FH\^FD- Perry Noble^FS
        ^FO20,300^GB570,0,6^FS
        ^FT20,350^A0N,30,30^FH\^FDDeviceName^FS
        ^FT350,350^A0N,30,30^FH\^FDPrinterIP^FS
        ^XZ", order: 2 )]
    public partial class Admin : CheckInBlock
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
                // Set the check-in state from values passed on query string
                CurrentKioskId = PageParameter( "KioskId" ).AsIntegerOrNull();

                var queryStringConfig = PageParameter( "GroupTypeIds" );
                if ( !string.IsNullOrEmpty( queryStringConfig ) )
                {
                    CurrentGroupTypeIds = queryStringConfig
                        .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                        .ToList()
                        .Select( s => s.AsInteger() )
                        .ToList();
                }

                if ( CurrentCheckInState != null && CurrentCheckInState.Kiosk != null && CurrentGroupTypeIds != null && !UserBackedUp )
                {
                    // Set the local cache if a session is already active
                    CurrentKioskId = CurrentCheckInState.DeviceId;
                    CurrentGroupTypeIds = CurrentCheckInState.ConfiguredGroupTypes;
                    CurrentCheckInState = null;
                    CurrentWorkflow = null;

                    // Save the check-in state
                    SaveState();

                    // Navigate to the next page
                    NavigateToNextPage();
                }
                else
                {
                    RockPage.AddScriptLink( "~/Blocks/CheckIn/Scripts/geo-min.js" );

                    AttemptKioskMatchByIpOrName();

                    string script = string.Format( @"<script>
                        $(document).ready(function (e) {{
                            if (localStorage) {{
                                if (localStorage.checkInKiosk) {{
                                    $('[id$=""hfKiosk""]').val(localStorage.checkInKiosk);
                                    if (localStorage.checkInGroupTypes) {{
                                        $('[id$=""hfGroupTypes""]').val(localStorage.checkInGroupTypes);
                                    }}
                                }}
                                {0};
                            }}
                        }});
                        </script>", this.Page.ClientScript.GetPostBackEventReference( lbRefresh, "" )
                    );
                    phScript.Controls.Add( new LiteralControl( script ) );

                    // Initiate the check-in variables
                    lbOk.Focus();
                    SaveState();
                }
            }
            else if ( Request["__EVENTTARGET"] == lbTestPrint.ClientID )
            {
                SendTestPrint();
            }
            else
            {
                phScript.Controls.Clear();
            }
        }

        /// <summary>
        /// Attempts to match a known kiosk based on the IP address of the client.
        /// </summary>
        private void AttemptKioskMatchByIpOrName()
        {
            // match kiosk by ip/name.
            string hostIp = Request.ServerVariables["REMOTE_ADDR"];
            string forwardedIp = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            string ipAddress = forwardedIp ?? hostIp;
            bool skipDeviceNameLookup = false;

            var rockContext = new RockContext();
            var checkInDeviceTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Id;
            var device = new DeviceService( rockContext ).GetByIPAddress( ipAddress, checkInDeviceTypeId, skipDeviceNameLookup );

            var checkInDeviceTypeGuid = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Guid;
            var deviceList = new DeviceService( rockContext ).GetByDeviceTypeGuid( checkInDeviceTypeGuid ).AsNoTracking().ToList();

            string hostName = string.Empty;
            try
            {
                hostName = System.Net.Dns.GetHostEntry( ipAddress ).HostName ?? ipAddress;
            }
            catch ( System.Net.Sockets.SocketException )
            {
                hostName = "Unknown";
            }

            lblInfo.Text = string.Format( "Device IP: {0}     Name: {1}", ipAddress, hostName );

            if ( device != null )
            {
                ClearMobileCookie();
                CurrentKioskId = device.Id;
            }
            else
            {
                maAlert.Show( "This device has not been set up for check-in.", ModalAlertType.Alert );
                lbOk.Text = @"<span class='fa fa-refresh' />";
                lbOk.Enabled = false;
                pnlContent.Update();
                pnlHeader.Update();
            }
        }

        #endregion Control Methods

        #region Events

        /// <summary>
        /// Handles the Click event of the lbOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbOk_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                maAlert.Show( "Check-in state timed out.  Please refresh the page.", ModalAlertType.Warning );
                pnlContent.Update();
                Response.Redirect( Request.Path, false );
                return;
            }

            if ( CurrentKioskId == null || CurrentKioskId == 0 )
            {
                CurrentKioskId = hfKiosk.ValueAsInt();
            }

            var selectedGroupTypes = hfGroupTypes.Value.SplitDelimitedValues().Select( int.Parse ).Distinct().ToList();
            if ( !selectedGroupTypes.Any() )
            {
                foreach ( DataListItem item in dlMinistry.Items )
                {
                    ( (Button)item.FindControl( "lbMinistry" ) ).RemoveCssClass( "active" );
                }

                hfGroupTypes.Value = string.Empty;
                maAlert.Show( "Please select at least one check-in type.", ModalAlertType.Warning );
                pnlContent.Update();
                return;
            }

            // return if kiosk isn't active
            if ( !CurrentCheckInState.Kiosk.HasActiveLocations( selectedGroupTypes ) )
            {
                maAlert.Show( "There are no active schedules for the selected grouptypes.", ModalAlertType.Information );
                pnlContent.Update();
                return;
            }

            ClearMobileCookie();
            CurrentGroupTypeIds = selectedGroupTypes;
            CurrentCheckInState = null;
            CurrentWorkflow = null;
            SaveState();
            NavigateToNextPage();
        }

        /// <summary>
        /// Used by the local storage script to rebind the group types if they were previously
        /// saved via local storage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            BindGroupTypes();
        }

        /// <summary>
        /// Handles attempting to find a registered Device kiosk by it's latitude and longitude.
        /// This event method is called automatically when the GeoLocation script get's the client's location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbCheckGeoLocation_Click( object sender, EventArgs e )
        {
            var lat = hfLatitude.Value;
            var lon = hfLongitude.Value;
            Device kiosk = null;

            if ( !string.IsNullOrEmpty( lat ) && !string.IsNullOrEmpty( lon ) )
            {
                kiosk = GetCurrentKioskByGeoFencing( lat, lon );
            }

            if ( kiosk != null )
            {
                SetDeviceIdCookie( kiosk );
                CurrentKioskId = kiosk.Id;
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the dlMinistry control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void dlMinistry_ItemDataBound( object sender, DataListItemEventArgs e )
        {
            var selectedGroupTypes = hfGroupTypes.Value.SplitDelimitedValues().Select( int.Parse ).ToList();
            if ( selectedGroupTypes.Count > 0 )
            {
                if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
                {
                    if ( selectedGroupTypes.Contains( ( (GroupType)e.Item.DataItem ).Id ) )
                    {
                        ( (Button)e.Item.FindControl( "lbMinistry" ) ).AddCssClass( "active" );
                    }
                }
            }
        }

        #endregion Events

        #region GeoLocation related

        /// <summary>
        /// Adds GeoLocation script and calls its init() to get client's latitude/longitude before firing
        /// the server side lbCheckGeoLocation_Click click event. Puts the two values into the two corresponding
        /// hidden varialbles, hfLatitude and hfLongitude.
        /// </summary>
        private void AddGeoLocationScript()
        {
            string geoScript = string.Format( @"
            <script>
                $(document).ready(function (e) {{
                    tryGeoLocation();

                    function tryGeoLocation() {{
                        if ( geo_position_js.init() ) {{
                            geo_position_js.getCurrentPosition(success_callback, error_callback, {{ enableHighAccuracy: true }});
                        }}
                        else {{
                            $(""div.checkin-header h1"").html( ""We're Sorry!"" );
                            $(""div.checkin-header h1"").after( ""<p>We don't support that kind of device yet. Please check-in using the on-site kiosks.</p>"" );
                            alert(""We We don't support that kind of device yet. Please check-in using the on-site kiosks."");
                        }}
                    }}

                    function success_callback( p ) {{
                        var latitude = p.coords.latitude.toFixed(4);
                        var longitude = p.coords.longitude.toFixed(4);
                        $(""input[id$='hfLatitude']"").val( latitude );
                        $(""input[id$='hfLongitude']"").val( longitude );
                        $(""div.checkin-header h1"").html( 'Checking Your Location...' );
                        $(""div.checkin-header"").append( ""<p class='muted'>"" + latitude + "" "" + longitude + ""</p>"" );
                        // now perform a postback to fire the check geo location
                        {0};
                    }}

                    function error_callback( p ) {{
                        // TODO: decide what to do in this situation...
                        alert( 'error=' + p.message );
                    }}
                }});
            </script>
            ", this.Page.ClientScript.GetPostBackEventReference( lbCheckGeoLocation, "" ) );
            phScript.Controls.Add( new LiteralControl( geoScript ) );
        }

        /// <summary>
        /// Returns a kiosk based on finding a geo location match for the given latitude and longitude.
        /// </summary>
        /// <param name="sLatitude">latitude as string</param>
        /// <param name="sLongitude">longitude as string</param>
        /// <returns></returns>
        public static Device GetCurrentKioskByGeoFencing( string sLatitude, string sLongitude )
        {
            var rockContext = new RockContext();
            double latitude = double.Parse( sLatitude );
            double longitude = double.Parse( sLongitude );
            var checkInDeviceType = DefinedValueCache.Read( new Guid( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ), rockContext );

            // We need to use the DeviceService until we can get the GeoFence to JSON Serialize/Deserialize.
            Device kiosk = new DeviceService( rockContext ).GetByGeocode( latitude, longitude, checkInDeviceType.Id );

            return kiosk;
        }

        #endregion GeoLocation related

        #region Storage Methods

        /// <summary>
        /// Sets the "DeviceId" cookie to expire after TimeToCacheKioskGeoLocation minutes
        /// if IsMobile is set.
        /// </summary>
        /// <param name="kiosk"></param>
        private void SetDeviceIdCookie( Device kiosk )
        {
            // set an expiration cookie for these coordinates.
            double timeCacheMinutes = double.Parse( GetAttributeValue( "TimetoCacheKioskGeoLocation" ) ?? "0" );

            HttpCookie deviceCookie = Request.Cookies[CheckInCookie.DEVICEID];
            if ( deviceCookie == null )
            {
                deviceCookie = new HttpCookie( CheckInCookie.DEVICEID, kiosk.Id.ToString() );
            }

            deviceCookie.Expires = ( timeCacheMinutes == 0 ) ? DateTime.MaxValue : RockDateTime.Now.AddMinutes( timeCacheMinutes );
            Response.Cookies.Set( deviceCookie );

            HttpCookie isMobileCookie = new HttpCookie( CheckInCookie.ISMOBILE, "true" );
            Response.Cookies.Set( isMobileCookie );
        }

        /// <summary>
        /// Clears the flag cookie that indicates this is a "mobile" device kiosk.
        /// </summary>
        private void ClearMobileCookie()
        {
            HttpCookie isMobileCookie = new HttpCookie( CheckInCookie.ISMOBILE );
            isMobileCookie.Expires = RockDateTime.Now.AddDays( -1d );
            Response.Cookies.Set( isMobileCookie );
        }

        #endregion Storage Methods

        #region Internal Methods

        /// <summary>
        /// Binds the group types.
        /// </summary>
        /// <param name="selectedValues">The selected values.</param>
        private void BindGroupTypes( RockContext rockContext = null )
        {
            if ( CurrentKioskId > 0 )
            {
                dlMinistry.DataSource = GetDeviceGroupTypes( (int)CurrentKioskId, rockContext );
                dlMinistry.DataBind();
                lblHeader.Visible = true;
            }
        }

        /// <summary>
        /// Gets the device group types.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns></returns>
        private List<GroupType> GetDeviceGroupTypes( int deviceId, RockContext rockContext = null )
        {
            rockContext = rockContext ?? new RockContext();
            var groupTypes = new Dictionary<int, GroupType>();

            var locationService = new LocationService( rockContext );

            // Get all locations (and their children) associated with device
            var locationIds = locationService
                .GetByDevice( deviceId, true )
                .Select( l => l.Id )
                .ToList();

            // Requery using EF
            foreach ( var groupType in locationService
                .Queryable().AsNoTracking()
                .Where( l => locationIds.Contains( l.Id ) )
                .SelectMany( l => l.GroupLocations )
                .Where( gl => gl.Group.GroupType.TakesAttendance )
                .Select( gl => gl.Group.GroupType )
                .ToList() )
            {
                groupTypes.AddOrIgnore( groupType.Id, groupType );
            }

            return groupTypes
                .OrderBy( g => g.Key )
                .Select( g => g.Value )
                .ToList();
        }

        /// <summary>
        /// Prints a test label.
        /// </summary>        
        protected void SendTestPrint()
        {
            if ( CurrentKioskId != null )
            {
                CheckInLabel label = new CheckInLabel();

                // get the current kiosk print options
                Device device = null;
                if ( CurrentCheckInState != null )
                {
                    device = CurrentCheckInState.Kiosk.Device;
                }
                else
                {
                    using ( var rockContext = new RockContext() )
                    {
                        device = new DeviceService( rockContext ).Get( (int)CurrentKioskId );
                    }
                }

                if ( device != null )
                {
                    label.PrintFrom = device.PrintFrom;
                    label.PrintTo = device.PrintToOverride;
                    label.PrinterDeviceId = device.PrinterDeviceId;
                    label.PrinterAddress = device.IPAddress;
                }

                // set the label content
                var labelContent = GetAttributeValue( "TestLabelContent" );
                labelContent = Regex.Replace( labelContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", "DeviceName" ), device.Name );
                labelContent = Regex.Replace( labelContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", "PrinterIP" ), label.PrinterAddress );

                // try printing the label
                if ( !string.IsNullOrWhiteSpace( labelContent ) && !string.IsNullOrWhiteSpace( label.PrinterAddress ) )
                {
                    var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                    var printerIpEndPoint = new IPEndPoint( IPAddress.Parse( label.PrinterAddress ), 9100 );
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
                        maAlert.Show( string.Format( "Can't connect to printer {0} from {1}", label.PrinterAddress, device.Name ), ModalAlertType.Alert );
                        pnlContent.Update();
                    }

                    if ( socket != null && socket.Connected )
                    {
                        socket.Shutdown( SocketShutdown.Both );
                        socket.Close();
                    }
                }
            }
            else
            {
                maAlert.Show( "Current check-in state is not instantiated.", ModalAlertType.Alert );
                pnlContent.Update();
            }
        }

        #endregion Internal Methods
    }
}