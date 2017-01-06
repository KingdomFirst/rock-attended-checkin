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
using System.Web.UI.WebControls;
using cc.newspring.AttendedCheckIn.Utility;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using System.Threading.Tasks;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.cc_newspring.AttendedCheckin
{
    [DisplayName( "Activity Select" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Activity Select Block" )]
    [BooleanField( "Display Group Names", "By default location names are shown.  Check this option to show the group names instead.", false )]
    [BooleanField( "Remove Attendance On Checkout", "By default, the attendance is given a checkout date.  Select this option to completely remove attendance on checkout.", false )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Person Special Needs Attribute", "Select the person attribute used to filter kids with special needs.", true, false, "8B562561-2F59-4F5F-B7DC-92B2BB7BB7CF" )]
    public partial class ActivitySelect : CheckInBlock
    {
        #region Variables

        /// <summary>
        /// Stores whether or not to display group names
        /// </summary>
        private static bool DisplayGroupNames;

        /// <summary>
        /// Gets the error when a page's parameter string is invalid.
        /// </summary>
        /// <value>
        /// The invalid parameter error.
        /// </value>
        private static string InvalidParameterError
        {
            get
            {
                return "The selected person's check-in information could not be loaded.";
            }
        }

        /// <summary>
        /// Gets the person special needs attribute key.
        /// </summary>
        /// <value>
        /// The special needs key.
        /// </value>
        private string SpecialNeedsKey
        {
            get
            {
                var specialNeedsKey = ViewState["SpecialNeedsKey"] as string;
                if ( !string.IsNullOrWhiteSpace( specialNeedsKey ) )
                {
                    return specialNeedsKey;
                }
                else
                {
                    var personSpecialNeedsGuid = GetAttributeValue( "PersonSpecialNeedsAttribute" ).AsGuid();
                    if ( personSpecialNeedsGuid != Guid.Empty )
                    {
                        specialNeedsKey = new RockContext().Attributes.Where( a => a.Guid == personSpecialNeedsGuid ).Select( a => a.Key ).FirstOrDefault();
                        ViewState["SpecialNeedsKey"] = specialNeedsKey;
                        return specialNeedsKey;
                    }
                    else
                    {
                        throw new Exception( "The selected Person Special Needs attribute is invalid for the FamilySelect page." );
                    }
                }
            }
        }

        /// <summary>
        /// A container for a schedule and attendance count
        /// </summary>
        private class ScheduleAttendance
        {
            public int ScheduleId { get; set; }

            public int AttendanceCount { get; set; }
        }

        /// <summary>
        /// A list of attendance counts per schedule
        /// </summary>
        private List<ScheduleAttendance> ScheduleAttendanceList = new List<ScheduleAttendance>();

        #endregion Variables

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
                var person = GetCurrentPerson();
                if ( person != null )
                {
                    // Set the nickname
                    var nickName = person.Person.NickName ?? person.Person.FirstName;
                    lblPersonName.Text = string.Format( "{0} {1}", nickName, person.Person.LastName );
                }

                DisplayGroupNames = GetAttributeValue( "DisplayGroupNames" ).AsBoolean();

                if ( person != null && person.GroupTypes.Any() )
                {
                    int? selectedGroupTypeId = person.GroupTypes.Where( gt => gt.Selected )
                        .Select( gt => (int?)gt.GroupType.Id ).FirstOrDefault();
                    if ( selectedGroupTypeId != null )
                    {
                        ViewState["groupTypeId"] = selectedGroupTypeId;
                    }

                    int? selectedGroupId = Request.QueryString["groupId"].AsType<int?>();
                    if ( selectedGroupId > 0 )
                    {
                        ViewState["groupId"] = selectedGroupId;
                    }

                    int? selectedLocationId = Request.QueryString["locationId"].AsType<int?>();
                    if ( selectedLocationId > 0 )
                    {
                        ViewState["locationId"] = selectedLocationId;
                    }

                    int? selectedScheduleId = Request.QueryString["scheduleId"].AsType<int?>();
                    if ( selectedScheduleId > 0 )
                    {
                        ViewState["scheduleId"] = selectedScheduleId;
                    }

                    BindGroupTypes( person.GroupTypes, selectedGroupTypeId );
                    BindLocations( person.GroupTypes, selectedGroupTypeId, selectedGroupId, selectedLocationId );
                    BindSchedules( person.GroupTypes, selectedGroupTypeId, selectedGroupId, selectedLocationId );
                    BindSelectedGrid();
                }
                else
                {
                    maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
                    NavigateToPreviousPage();
                }
            }
            else
            {
                var lblAbilityGrade = ViewState["lblAbilityGrade"] as string;
                if ( !string.IsNullOrEmpty( lblAbilityGrade ) )
                {
                    ddlAbilityGrade.Label = lblAbilityGrade;
                }
            }

            // Instantiate the allergy control for reference later
            var allergyControl = AttributeCache.Read( new Guid( Rock.SystemGuid.Attribute.PERSON_ALLERGY ) )
                .AddControl( phAttributes.Controls, string.Empty, "", true, true );
            if ( allergyControl is RockTextBox )
            {
                ( (RockTextBox)allergyControl ).MaxLength = 80;
            }

            if ( DisplayGroupNames )
            {
                hdrLocations.InnerText = "Group";
            }
        }

        /// <summary>
        /// Unsets the changes.
        /// </summary>
        private void UnsetChanges()
        {
            var person = GetCurrentPerson();

            if ( person != null )
            {
                var groupTypes = person.GroupTypes;
                groupTypes.ForEach( gt => gt.Selected = gt.PreSelected );

                var groups = groupTypes.SelectMany( gt => gt.Groups ).ToList();
                groups.ForEach( g => g.Selected = g.PreSelected );

                var locations = groups.SelectMany( g => g.Locations ).ToList();
                locations.ForEach( l => l.Selected = l.PreSelected );

                var schedules = locations.SelectMany( l => l.Schedules ).ToList();
                schedules.ForEach( s => s.Selected = s.PreSelected );
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Goes to the confirmation page with changes.
        /// </summary>
        private void GoNext()
        {
            var person = GetCurrentPerson();
            if ( person != null )
            {
                var changes = new List<string>();
                person.PreSelected = person.Selected;
                var groupTypes = person.GroupTypes.ToList();
                foreach ( var groupType in groupTypes )
                {
                    History.EvaluateChange( changes, string.Format( "{0} Grouptype", groupType ), groupType.PreSelected, groupType.Selected );
                    groupType.PreSelected = groupType.Selected;
                }

                var groups = groupTypes.SelectMany( gt => gt.Groups ).ToList();
                foreach ( var group in groups )
                {
                    History.EvaluateChange( changes, string.Format( "{0} Group", group ), group.PreSelected, group.Selected );
                    group.PreSelected = group.Selected;
                }

                var locations = groups.SelectMany( g => g.Locations ).ToList();
                foreach ( var location in locations )
                {
                    History.EvaluateChange( changes, string.Format( "{0} Location", location ), location.PreSelected, location.Selected );
                    location.PreSelected = location.Selected;
                }

                var schedules = locations.SelectMany( l => l.Schedules ).ToList();
                foreach ( var schedule in schedules )
                {
                    History.EvaluateChange( changes, string.Format( "{0} Schedule", schedule ), schedule.PreSelected, schedule.Selected );
                    schedule.PreSelected = schedule.Selected;
                }

                HistoryService.AddChanges(
                    new RockContext(),
                    typeof( Person ),
                    Rock.SystemGuid.Category.HISTORY_PERSON_ACTIVITY.AsGuid(),
                    person.Person.Id,
                    changes,
                    CurrentPersonAliasId
                );
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }

            SaveState();
            NavigateToNextPage();
        }

        #endregion Control Methods

        #region Click Events

        /// <summary>
        /// Handles the ItemCommand event of the rGroupType control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void lvGroupType_ItemCommand( object source, ListViewCommandEventArgs e )
        {
            var person = GetCurrentPerson();
            if ( person != null )
            {
                foreach ( ListViewDataItem item in lvGroupType.Items )
                {
                    if ( item.ID != e.Item.ID )
                    {
                        ( (LinkButton)item.FindControl( "lbGroupType" ) ).RemoveCssClass( "active" );
                    }
                    else
                    {
                        ( (LinkButton)e.Item.FindControl( "lbGroupType" ) ).AddCssClass( "active" );
                    }
                }

                ViewState["groupTypeId"] = e.CommandArgument.ToStringSafe();
                pnlGroupTypes.Update();
                BindLocations( person.GroupTypes, e.CommandArgument.ToStringSafe().AsType<int?>() );
                BindSchedules( person.GroupTypes, e.CommandArgument.ToStringSafe().AsType<int?>() );
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the ItemCommand event of the lvLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewCommandEventArgs"/> instance containing the event data.</param>
        protected void lvLocation_ItemCommand( object sender, ListViewCommandEventArgs e )
        {
            var person = GetCurrentPerson();
            if ( person != null )
            {
                foreach ( ListViewDataItem item in lvLocation.Items )
                {
                    if ( item.ID != e.Item.ID )
                    {
                        ( (LinkButton)item.FindControl( "lbLocation" ) ).RemoveCssClass( "active" );
                    }
                    else
                    {
                        ( (LinkButton)e.Item.FindControl( "lbLocation" ) ).AddCssClass( "active" );
                    }
                }

                int? selectedLocationId = e.CommandArgument.ToStringSafe().AsType<int?>();
                int? selectedGroupId = null;
                if ( DisplayGroupNames )
                {
                    // multiple groups could use the same location, so update based on the group's name
                    selectedGroupId = person.GroupTypes.SelectMany( gt => gt.Groups )
                        .Where( g => g.Group.Name.Equals( e.CommandName.ToString() ) )
                        .Select( g => (int?)g.Group.Id )
                        .FirstOrDefault();
                }
                else
                {
                    selectedGroupId = person.GroupTypes.SelectMany( gt => gt.Groups )
                        .OrderByDescending( g => !g.ExcludedByFilter )
                        .Where( g => g.Locations.Any( l => l.Location.Id == selectedLocationId ) )
                        .Select( g => (int?)g.Group.Id )
                        .FirstOrDefault();
                }

                ViewState["locationId"] = selectedLocationId;
                if ( selectedGroupId != null )
                {
                    ViewState["groupId"] = selectedGroupId;
                }

                pnlLocations.Update();
                BindSchedules( person.GroupTypes, null, selectedGroupId, selectedLocationId );
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the ItemCommand event of the rSchedule control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rSchedule_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            var person = GetCurrentPerson();
            if ( person != null )
            {
                foreach ( RepeaterItem item in rSchedule.Items )
                {
                    if ( item.ID != e.Item.ID )
                    {
                        ( (LinkButton)item.FindControl( "lbSchedule" ) ).RemoveCssClass( "active" );
                    }
                    else
                    {
                        ( (LinkButton)e.Item.FindControl( "lbSchedule" ) ).AddCssClass( "active" );
                    }
                }

                int? groupTypeId = ViewState["groupTypeId"].ToStringSafe().AsType<int?>();
                int? groupId = ViewState["groupId"].ToStringSafe().AsType<int?>();
                int? locationId = ViewState["locationId"].ToStringSafe().AsType<int?>();
                int? scheduleId = e.CommandArgument.ToStringSafe().AsType<int?>();

                // set this selected group, location, and schedule
                var selectedGroupType = person.GroupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                selectedGroupType.Selected = true;
                var selectedGroup = selectedGroupType.Groups.FirstOrDefault( g => g.Group.Id == groupId && g.Locations.Any( l => l.Location.Id == locationId ) );
                selectedGroup.Selected = true;
                var selectedLocation = selectedGroup.Locations.FirstOrDefault( l => l.Location.Id == locationId );
                selectedLocation.Selected = true;
                var selectedSchedule = selectedLocation.Schedules.FirstOrDefault( s => s.Schedule.Id == scheduleId );
                selectedSchedule.Selected = true;

                pnlSchedules.Update();
                BindSelectedGrid();
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rGroupTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void lvGroupType_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                var groupType = (CheckInGroupType)e.Item.DataItem;
                var lbGroupType = (LinkButton)e.Item.FindControl( "lbGroupType" );
                lbGroupType.CommandArgument = groupType.GroupType.Id.ToString();
                lbGroupType.Text = groupType.GroupType.Name;

                var selectedGroupTypeId = ViewState["groupTypeId"].ToStringSafe().AsType<int?>();
                if ( groupType.Selected && selectedGroupTypeId != null && groupType.GroupType.Id == selectedGroupTypeId )
                {
                    lbGroupType.AddCssClass( "active" );
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the lvLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewItemEventArgs"/> instance containing the event data.</param>
        protected void lvLocation_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                int locationId = 0;
                string displayName = string.Empty;
                bool itemSelected = false;
                if ( DisplayGroupNames )
                {   // parse group items
                    var group = (CheckInGroup)e.Item.DataItem;
                    displayName = group.Group.Name;
                    itemSelected = group.Selected;
                    locationId = group.Locations.Select( l => l.Location.Id ).FirstOrDefault();
                }
                else
                {   // parse location items
                    var location = (CheckInLocation)e.Item.DataItem;
                    locationId = location.Location.Id;
                    displayName = location.Location.Name;
                    itemSelected = location.Selected;
                }

                var lbLocation = (LinkButton)e.Item.FindControl( "lbLocation" );

                lbLocation.Text = displayName;
                lbLocation.CommandName = displayName;
                lbLocation.CommandArgument = locationId.ToString();

                if ( itemSelected )
                {
                    lbLocation.AddCssClass( "active" );
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rSchedule control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rSchedule_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
            {
                var schedule = (CheckInSchedule)e.Item.DataItem;
                var lbSchedule = (LinkButton)e.Item.FindControl( "lbSchedule" );
                lbSchedule.CommandArgument = schedule.Schedule.Id.ToString();
                if ( schedule.Selected )
                {
                    lbSchedule.AddCssClass( "active" );
                }

                if ( CurrentCheckInType != null && CurrentCheckInType.DisplayLocationCount )
                {
                    var scheduleAttendance = ScheduleAttendanceList.Where( s => s.ScheduleId == schedule.Schedule.Id );
                    lbSchedule.Text = string.Format( "{0} ({1})", schedule.Schedule.Name, scheduleAttendance.Select( s => s.AttendanceCount ).FirstOrDefault() );
                }
                else
                {
                    lbSchedule.Text = string.Format( "{0}", schedule.Schedule.Name );
                }
            }
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the lvGroupType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvGroupType_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            dpGroupType.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
            lvGroupType.DataSource = Session["grouptypes"];
            lvGroupType.DataBind();
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the lvLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvLocation_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            dpLocation.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
            lvLocation.DataSource = Session["locations"];
            lvLocation.DataBind();
        }

        /// <summary>
        /// Handles the Delete event of the gCheckInList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gSelectedGrid_Delete( object sender, RowEventArgs e )
        {
            var person = GetCurrentPerson();

            if ( person != null )
            {
                // Delete an item. Remove the selected attribute from the group, location and schedule
                int index = e.RowIndex;
                var row = gSelectedGrid.Rows[index];
                var dataKeyValues = gSelectedGrid.DataKeys[index].Values;
                var groupId = int.Parse( dataKeyValues["GroupId"].ToString() );
                var locationId = int.Parse( dataKeyValues["LocationId"].ToString() );
                var scheduleId = int.Parse( dataKeyValues["ScheduleId"].ToString() );

                CheckInGroupType selectedGroupType;
                if ( person.GroupTypes.Count == 1 )
                {
                    selectedGroupType = person.GroupTypes.FirstOrDefault();
                }
                else
                {
                    selectedGroupType = person.GroupTypes.FirstOrDefault( gt => gt.Selected
                        && gt.Groups.Any( g => g.Group.Id == groupId && g.Locations.Any( l => l.Location.Id == locationId
                            && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) ) ) );
                }

                if ( selectedGroupType != null )
                {
                    var selectedGroup = selectedGroupType.Groups.FirstOrDefault( g => g.Selected && g.Group.Id == groupId
                    && g.Locations.Any( l => l.Location.Id == locationId
                        && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) ) );
                    var selectedLocation = selectedGroup.Locations.FirstOrDefault( l => l.Selected
                        && l.Location.Id == locationId && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) );
                    var selectedSchedule = selectedLocation.Schedules.FirstOrDefault( s => s.Selected
                        && s.Schedule.Id == scheduleId );

                    bool currentlyCheckedIn = selectedSchedule.LastCheckIn != null && selectedSchedule.LastCheckIn > RockDateTime.Now;
                    if ( currentlyCheckedIn )
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
                                && a.PersonAlias.PersonId == person.Person.Id
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

                    // started from the bottom now we here
                    selectedSchedule.Selected = false;

                    // clear checkin rows without anything selected
                    if ( !selectedLocation.Schedules.Any( s => s.Selected ) )
                    {
                        selectedLocation.Selected = false;
                    }

                    if ( !selectedGroup.Locations.Any( l => l.Selected ) )
                    {
                        selectedGroup.Selected = false;
                    }

                    if ( !selectedGroupType.Groups.Any( l => l.Selected ) )
                    {
                        selectedGroupType.Selected = false;
                    }
                }

                BindLocations( person.GroupTypes );
                BindSchedules( person.GroupTypes );
                BindSelectedGrid();
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbEditInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEditInfo_Click( object sender, EventArgs e )
        {
            BindInfo();
            mdlInfo.Show();
        }

        /// <summary>
        /// Handles the Click event of the lbSaveEditInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSaveEditInfo_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrEmpty( tbFirstName.Text ) || string.IsNullOrEmpty( tbLastName.Text ) || string.IsNullOrEmpty( dpDOB.Text ) )
            {
                Page.Validate( "Person" );
                mdlInfo.Show();
                return;
            }

            CheckInPerson currentPerson = GetCurrentPerson();
            var rockContext = new RockContext();
            Person person = new PersonService( rockContext ).Get( currentPerson.Person.Id );
            person.LoadAttributes();

            person.FirstName = tbFirstName.Text;
            currentPerson.Person.FirstName = tbFirstName.Text;

            person.LastName = tbLastName.Text;
            currentPerson.Person.LastName = tbLastName.Text;

            person.SuffixValueId = ddlSuffix.SelectedValueAsId();
            currentPerson.Person.SuffixValueId = ddlSuffix.SelectedValueAsId();

            var DOB = dpDOB.SelectedDate;
            if ( DOB != null )
            {
                person.BirthDay = ( (DateTime)DOB ).Day;
                currentPerson.Person.BirthDay = ( (DateTime)DOB ).Day;
                person.BirthMonth = ( (DateTime)DOB ).Month;
                currentPerson.Person.BirthMonth = ( (DateTime)DOB ).Month;
                person.BirthYear = ( (DateTime)DOB ).Year;
                currentPerson.Person.BirthYear = ( (DateTime)DOB ).Year;
            }

            person.NickName = tbNickname.Text.Length > 0 ? tbNickname.Text : tbFirstName.Text;
            currentPerson.Person.NickName = tbNickname.Text.Length > 0 ? tbNickname.Text : tbFirstName.Text;
            var optionGroup = ddlAbilityGrade.SelectedItem.Attributes["optiongroup"];

            if ( !string.IsNullOrEmpty( optionGroup ) )
            {
                // Selected ability level
                if ( optionGroup == "Ability" )
                {
                    person.SetAttributeValue( "AbilityLevel", ddlAbilityGrade.SelectedValue );
                    currentPerson.Person.SetAttributeValue( "AbilityLevel", ddlAbilityGrade.SelectedValue );

                    person.GradeOffset = null;
                    currentPerson.Person.GradeOffset = null;
                }
                // Selected a grade
                else if ( optionGroup == "Grade" )
                {
                    person.GradeOffset = ddlAbilityGrade.SelectedValueAsId();
                    currentPerson.Person.GradeOffset = ddlAbilityGrade.SelectedValueAsId();

                    person.Attributes.Remove( "AbilityLevel" );
                    currentPerson.Person.Attributes.Remove( "AbilityLevel" );
                }
            }

            // Always save the special needs value
            person.SetAttributeValue( SpecialNeedsKey, cbSpecialNeeds.Checked ? "Yes" : string.Empty );
            currentPerson.Person.SetAttributeValue( SpecialNeedsKey, cbSpecialNeeds.Checked ? "Yes" : string.Empty );

            // store the allergies
            var allergyAttribute = AttributeCache.Read( new Guid( Rock.SystemGuid.Attribute.PERSON_ALLERGY ), rockContext );
            var allergyAttributeControl = phAttributes.FindControl( string.Format( "attribute_field_{0}", allergyAttribute.Id ) );
            if ( allergyAttributeControl != null )
            {
                person.SetAttributeValue( "Allergy", allergyAttribute.FieldType.Field
                    .GetEditValue( allergyAttributeControl, allergyAttribute.QualifierValues ) );
                currentPerson.Person.SetAttributeValue( "Allergy", allergyAttribute.FieldType.Field
                    .GetEditValue( allergyAttributeControl, allergyAttribute.QualifierValues ) );
            }

            // store the check-in notes
            person.SetAttributeValue( "LegalNotes", tbNoteText.Text );
            currentPerson.Person.SetAttributeValue( "LegalNotes", tbNoteText.Text );

            // Save the attribute change to the db (CheckinPerson already tracked)
            person.SaveAttributeValues();
            rockContext.SaveChanges();
            mdlInfo.Hide();
        }

        /// <summary>
        /// Handles the Click event of the lbCloseEditInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCloseEditInfo_Click( object sender, EventArgs e )
        {
            mdlInfo.Hide();
        }

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            UnsetChanges();
            NavigateToPreviousPage();
        }

        /// <summary>
        /// Handles the Click event of the lbNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNext_Click( object sender, EventArgs e )
        {
            if ( gSelectedGrid.Rows.Count == 0 )
            {
                maWarning.Show( "Please select at least one assignment for this person.", ModalAlertType.Warning );
                return;
            }

            GoNext();
        }

        #endregion Click Events

        #region Internal Methods

        /// <summary>
        /// Binds the group types.
        /// </summary>
        /// <param name="person">The person.</param>
        protected void BindGroupTypes( List<CheckInGroupType> groupTypes, int? groupTypeId = null )
        {
            groupTypeId = groupTypeId ?? ViewState["groupTypeId"].ToStringSafe().AsType<int?>();
            if ( groupTypeId != null )
            {
                var groupType = groupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                var placeInList = groupTypes.IndexOf( groupType ) + 1;
                var pageSize = dpGroupType.PageSize;
                var pageToGoTo = placeInList / pageSize;
                if ( placeInList % pageSize != 0 || pageToGoTo == 0 )
                {
                    pageToGoTo++;
                }

                dpGroupType.SetPageProperties( ( pageToGoTo - 1 ) * dpGroupType.PageSize, dpGroupType.MaximumRows, false );
            }

            Session["grouptypes"] = groupTypes;
            lvGroupType.DataSource = groupTypes;
            lvGroupType.DataBind();
            pnlGroupTypes.Update();
        }

        /// <summary>
        /// Binds the locations.
        /// </summary>
        /// <param name="person">The person.</param>
        protected void BindLocations( List<CheckInGroupType> groupTypes, int? groupTypeId = null, int? groupId = null, int? locationId = null )
        {
            groupTypeId = groupTypeId ?? ViewState["groupTypeId"].ToStringSafe().AsType<int?>();
            if ( groupTypeId != null )
            {
                groupId = groupId ?? ViewState["groupId"].ToStringSafe().AsType<int?>();
                locationId = locationId ?? ViewState["locationId"].ToStringSafe().AsType<int?>();

                var groupType = groupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                if ( groupType == null )
                {
                    groupType = groupTypes.FirstOrDefault();
                }

                int placeInList = 1;
                IEnumerable<ILiquidizable> locationItems = null;
                if ( DisplayGroupNames )
                {
                    var allGroups = groupType.Groups.OrderBy( g => g.Group.Name ).ToList();
                    if ( groupId > 0 )
                    {
                        var selectedGroup = allGroups.FirstOrDefault( g => g.Group.Id == groupId && g.Locations.Any( l => l.Location.Id == locationId ) );
                        placeInList = allGroups.IndexOf( selectedGroup ) + 1;
                    }

                    // Show group names; locationItems is Type <CheckInGroup>
                    locationItems = allGroups.Cast<ILiquidizable>();
                }
                else
                {
                    var allLocations = groupType.Groups.SelectMany( g => g.Locations )
                        .OrderBy( l => l.Location.Name ).ThenBy( l => !l.Selected )
                        .DistinctBy( l => l.Location.Id ).ToList();
                    if ( locationId > 0 )
                    {
                        var selectedLocation = allLocations.FirstOrDefault( l => l.Location.Id == locationId );
                        placeInList = allLocations.IndexOf( selectedLocation ) + 1;
                    }

                    // Show location names; locationItems is Type <CheckInLocation>
                    locationItems = allLocations.Cast<ILiquidizable>();
                }

                var pageToGoTo = placeInList / dpLocation.PageSize;
                if ( pageToGoTo == 0 || placeInList % dpLocation.PageSize != 0 )
                {
                    pageToGoTo++;
                }

                dpLocation.SetPageProperties( ( pageToGoTo - 1 ) * dpLocation.PageSize, dpLocation.MaximumRows, false );

                Session["locations"] = locationItems;
                lvLocation.DataSource = locationItems;
                lvLocation.DataBind();
                pnlLocations.Update();
            }
        }

        /// <summary>
        /// Binds the schedules.
        /// </summary>
        /// <param name="person">The person.</param>
        protected void BindSchedules( List<CheckInGroupType> groupTypes, int? groupTypeId = null, int? groupId = null, int? locationId = null )
        {
            groupTypeId = groupTypeId ?? ViewState["groupTypeId"].ToStringSafe().AsType<int?>();
            if ( groupTypeId != null )
            {
                groupId = groupId ?? ViewState["groupId"].ToStringSafe().AsType<int?>();
                locationId = locationId ?? ViewState["locationId"].ToStringSafe().AsType<int>();

                var groupType = groupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                if ( groupType != null )
                {
                    var location = groupType.Groups.Where( g => g.Group.Id == groupId ).SelectMany( g => g.Locations )
                        .FirstOrDefault( l => l.Location.Id == locationId );

                    if ( location != null )
                    {
                        GetScheduleAttendance( location );
                        rSchedule.DataSource = location.Schedules;
                        rSchedule.DataBind();
                        pnlSchedules.Update();
                    }
                }
            }
        }

        /// <summary>
        /// Binds the selected items to the grid.
        /// </summary>
        protected void BindSelectedGrid()
        {
            var person = GetCurrentPerson();

            if ( person != null )
            {
                var selectedGroupTypes = person.GroupTypes.Where( gt => gt.Selected );
                var selectedGroups = selectedGroupTypes.SelectMany( gt => gt.Groups.Where( g => g.Selected ) ).ToList();

                var checkInList = new List<Activity>();
                foreach ( var group in selectedGroups )
                {
                    foreach ( var location in group.Locations.Where( l => l.Selected ) )
                    {
                        foreach ( var schedule in location.Schedules.Where( s => s.Selected ) )
                        {
                            var checkIn = new Activity();
                            checkIn.StartTime = Convert.ToDateTime( schedule.StartTime );
                            checkIn.GroupId = group.Group.Id;
                            checkIn.Location = DisplayGroupNames ? group.Group.Name : location.Location.Name;
                            checkIn.LocationId = location.Location.Id;
                            checkIn.Schedule = schedule.Schedule.Name;
                            checkIn.ScheduleId = schedule.Schedule.Id;
                            checkInList.Add( checkIn );
                        }
                    }
                }

                gSelectedGrid.DataSource = checkInList.OrderBy( c => c.StartTime )
                    .ThenBy( c => c.Schedule ).ToList();
                gSelectedGrid.DataBind();
                pnlSelected.Update();
            }
        }

        /// <summary>
        /// Binds the edit info modal.
        /// </summary>
        protected void BindInfo()
        {
            var person = GetCurrentPerson();
            if ( person != null )
            {
                ddlAbilityGrade.LoadAbilityAndGradeItems();
                ddlSuffix.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ) ), true );

                ViewState["lblAbilityGrade"] = ddlAbilityGrade.Label;
                person.Person.LoadAttributes();

                tbFirstName.Text = person.Person.FirstName;
                tbLastName.Text = person.Person.LastName;
                tbNickname.Text = person.Person.NickName;
                dpDOB.SelectedDate = person.Person.BirthDate;
                cbSpecialNeeds.Checked = person.Person.GetAttributeValue( SpecialNeedsKey ).AsBoolean();

                tbFirstName.Required = true;
                tbLastName.Required = true;
                dpDOB.Required = true;

                if ( person.Person.SuffixValueId.HasValue )
                {
                    ddlSuffix.SelectedValue = person.Person.SuffixValueId.ToString();
                }

                if ( person.Person.GradeOffset.HasValue && person.Person.GradeOffset.Value >= 0 )
                {
                    ddlAbilityGrade.SelectedValue = person.Person.GradeOffset.ToString();
                }
                else if ( person.Person.AttributeValues.ContainsKey( "AbilityLevel" ) )
                {
                    var personAbility = person.Person.GetAttributeValue( "AbilityLevel" );
                    if ( !string.IsNullOrWhiteSpace( personAbility ) )
                    {
                        ddlAbilityGrade.SelectedValue = personAbility;
                    }
                }

                // Note: Allergy control is dynamic and must be initialized on PageLoad
                var personAllergyValues = person.Person.GetAttributeValue( "Allergy" );
                if ( !string.IsNullOrWhiteSpace( personAllergyValues ) )
                {
                    phAttributes.Controls.Clear();
                    var control = AttributeCache.Read( new Guid( Rock.SystemGuid.Attribute.PERSON_ALLERGY ) )
                        .AddControl( phAttributes.Controls, personAllergyValues, "", true, true );

                    if ( control is RockTextBox )
                    {
                        ( (RockTextBox)control ).MaxLength = 80;
                    }
                }

                // load check-in notes
                var notes = person.Person.GetAttributeValue( "LegalNotes" ) ?? string.Empty;
                tbNoteText.Text = notes;
            }
            else
            {
                maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Gets the current person.
        /// </summary>
        /// <returns></returns>
        private CheckInPerson GetCurrentPerson( int? personId = null )
        {
            personId = personId ?? Request.QueryString["personId"].AsType<int?>();
            var family = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );

            if ( personId == null || personId < 1 || family == null )
            {
                return null;
            }

            return family.People.FirstOrDefault( p => p.Person.Id == personId );
        }

        /// <summary>
        /// Gets the attendance count for all of the schedules for a location. This will show on the schedule buttons.
        /// </summary>
        /// <param name="location"></param>
        protected void GetScheduleAttendance( CheckInLocation location )
        {
            if ( location != null )
            {
                var rockContext = new RockContext();
                var attendanceService = new AttendanceService( rockContext );
                var attendanceQuery = attendanceService.GetByDateAndLocation( DateTime.Now, location.Location.Id );

                ScheduleAttendanceList.Clear();
                foreach ( var schedule in location.Schedules )
                {
                    var attendance = new ScheduleAttendance();
                    attendance.ScheduleId = schedule.Schedule.Id;
                    attendance.AttendanceCount = attendanceQuery.Where( l => l.ScheduleId == attendance.ScheduleId ).Count();
                    ScheduleAttendanceList.Add( attendance );
                }
            }
        }

        #endregion Internal Methods

        #region Classes

        /// <summary>
        /// Check-In information class used to bind the selected grid.
        /// </summary>
        protected class Activity
        {
            public DateTime? StartTime { get; set; }

            public int GroupId { get; set; }

            public string Location { get; set; }

            public int LocationId { get; set; }

            public string Schedule { get; set; }

            public int ScheduleId { get; set; }

            public Activity()
            {
                StartTime = new DateTime?();
                GroupId = 0;
                Location = string.Empty;
                LocationId = 0;
                Schedule = string.Empty;
                ScheduleId = 0;
            }
        }

        #endregion Classes
    }
}
