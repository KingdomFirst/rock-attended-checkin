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
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.CheckIn.Attended
{
    [DisplayName( "Activity Select" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Activity Select Block" )]
    public partial class ActivitySelect : CheckInBlock
    {
        /// <summary>
        /// Check-In information class used to bind the selected grid.
        /// </summary>
        protected class CheckIn
        {
            public string Location { get; set; }

            public string Schedule { get; set; }

            public DateTime? StartTime { get; set; }

            public int LocationId { get; set; }

            public int ScheduleId { get; set; }

            public CheckIn()
            {
                Location = string.Empty;
                Schedule = string.Empty;
                StartTime = new DateTime?();
                LocationId = 0;
                ScheduleId = 0;
            }
        }

        /// <summary>
        /// Gets the error when a page's parameter string is invalid.
        /// </summary>
        /// <value>
        /// The invalid parameter error.
        /// </value>
        protected string InvalidParameterError
        {
            get
            {
                return "The selected person's check-in information could not be loaded.";
            }
        }

        /// <summary>
        /// The check in note type identifier
        /// </summary>
        protected int CheckInNoteTypeId;

        /// <summary>
        /// A list of attendance counts per schedule
        /// </summary>
        protected class ScheduleAttendance
        {
            public int ScheduleId { get; set; }

            public int AttendanceCount { get; set; }
        }

        protected List<ScheduleAttendance> ScheduleAttendanceList = new List<ScheduleAttendance>();

        #region Control Methods

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
                var personId = Request.QueryString["personId"].AsType<int?>();
                SetHeader();
                if ( !Page.IsPostBack )
                {
                    var person = GetPerson();

                    if ( person != null && person.GroupTypes.Any() )
                    {
                        ViewState["locationId"] = Request.QueryString["locationId"];
                        ViewState["scheduleId"] = Request.QueryString["scheduleId"];
                        var selectedGroupType = person.GroupTypes.Where( gt => gt.Selected ).FirstOrDefault();
                        if ( selectedGroupType != null )
                        {
                            ViewState["groupTypeId"] = selectedGroupType.GroupType.Id.ToString();
                        }

                        BindGroupTypes( person.GroupTypes );
                        BindLocations( person.GroupTypes );
                        BindSchedules( person.GroupTypes );
                        BindSelectedGrid();

                        // look up check-in notes
                        var rockContext = new RockContext();
                        var personTypeId = new Person().TypeId;
                        CheckInNoteTypeId = new NoteTypeService( rockContext ).Queryable()
                            .Where( t => t.Name == "Check-In" && t.EntityTypeId == personTypeId )
                            .Select( t => t.Id ).FirstOrDefault();

                        ViewState["checkInNoteTypeId"] = CheckInNoteTypeId;

                        var checkInNote = new NoteService( rockContext )
                            .GetByNoteTypeId( CheckInNoteTypeId )
                            .Where( n => n.EntityId == person.Person.Id )
                            .FirstOrDefault();

                        if ( checkInNote != null )
                        {
                            tbNoteText.Text = checkInNote.Text;
                        }

                        var allergyAttributeId = new AttributeService( rockContext )
                            .GetByEntityTypeId( new Person().TypeId )
                            .Where( a => a.Name.ToUpper() == "ALLERGY" ).FirstOrDefault().Id;
                        LoadAttributeControl( allergyAttributeId, person.Person.Id );
                    }
                    else
                    {
                        maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
                        GoBack();
                    }
                }

                // reload the attribute field.
                if ( personId > 0 )
                {
                    var allergyAttributeId = new AttributeService( new RockContext() ).GetByEntityTypeId( new Person().TypeId )
                        .Where( a => a.Name.ToUpper() == "ALLERGY" ).FirstOrDefault().Id;
                    LoadAttributeControl( allergyAttributeId, (int)personId );
                }
            }
        }

        #endregion Control Methods

        /// <summary>
        /// Sets the header.
        /// </summary>
        protected void SetHeader()
        {
            var person = GetPerson();

            if ( person != null )
            {
                var first = person.Person.NickName ?? person.Person.FirstName;
                lblPersonName.Text = string.Format( "{0} {1}", first, person.Person.LastName );
            }
        }

        #region Edit Events

        /// <summary>
        /// Handles the ItemCommand event of the rGroupType control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rGroupType_ItemCommand( object source, ListViewCommandEventArgs e )
        {
            var person = GetPerson();

            if ( person != null )
            {
                foreach ( ListViewDataItem item in rGroupType.Items )
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

                ViewState["groupTypeId"] = e.CommandArgument.ToString();
                pnlGroupTypes.Update();
                BindLocations( person.GroupTypes );
                BindSchedules( person.GroupTypes );
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
            var person = GetPerson();

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

                ViewState["locationId"] = e.CommandArgument.ToString();
                pnlLocations.Update();
                BindSchedules( person.GroupTypes );
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
            var person = GetPerson();

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

                var groupTypeId = ViewState["groupTypeId"].ToString().AsType<int?>();
                var locationId = ViewState["locationId"].ToString().AsType<int?>();
                int scheduleId = Int32.Parse( e.CommandArgument.ToString() );

                // prevent a checkin at different groups/locations for the same schedule time
                var groups = person.GroupTypes.SelectMany( gt => gt.Groups ).ToList();
                var locations = groups.SelectMany( g => g.Locations ).ToList();
                var schedules = locations.SelectMany( l => l.Schedules )
                    .Where( s => s.Schedule.Id == scheduleId ).ToList();

                // set this selected group, location, and schedule
                var selectedGroupTypes = person.GroupTypes.Where( gt => gt.GroupType.Id == groupTypeId ).ToList();
                selectedGroupTypes.ForEach( gt => gt.Selected = true );
                var selectedGroups = selectedGroupTypes.SelectMany( gt => gt.Groups ).Where( g => g.Locations.Any( l => l.Location.Id == locationId ) ).ToList();
                selectedGroups.ForEach( g => g.Selected = true );
                var selectedLocations = selectedGroups.SelectMany( g => g.Locations ).Where( l => l.Location.Id == locationId ).ToList();
                selectedLocations.ForEach( l => l.Selected = true );
                var selectedSchedules = selectedLocations.SelectMany( l => l.Schedules ).Where( s => s.Schedule.Id == scheduleId ).ToList();
                selectedSchedules.ForEach( s => s.Selected = true );

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
        protected void rGroupType_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( ViewState["groupTypeId"] != null )
            {
                var selectedGroupTypeId = ViewState["groupTypeId"].ToString().AsType<int?>();

                if ( e.Item.ItemType == ListViewItemType.DataItem )
                {
                    var groupType = (CheckInGroupType)e.Item.DataItem;
                    var lbGroupType = (LinkButton)e.Item.FindControl( "lbGroupType" );
                    lbGroupType.CommandArgument = groupType.GroupType.Id.ToString();
                    lbGroupType.Text = groupType.GroupType.Name;
                    if ( groupType.Selected && groupType.GroupType.Id == selectedGroupTypeId )
                    {
                        lbGroupType.AddCssClass( "active" );
                    }
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
            if ( ViewState["locationId"] != null )
            {
                var selectedLocationId = ViewState["locationId"].ToString().AsType<int?>();
                if ( e.Item.ItemType == ListViewItemType.DataItem )
                {
                    var location = (CheckInLocation)e.Item.DataItem;
                    var lbLocation = (LinkButton)e.Item.FindControl( "lbLocation" );
                    lbLocation.CommandArgument = location.Location.Id.ToString();
                    lbLocation.Text = location.Location.Name + " (" + GetLocationAttendance( location ) + ")";

                    if ( location.Selected && location.Location.Id == selectedLocationId )
                    {
                        lbLocation.AddCssClass( "active" );
                    }
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
                lbSchedule.Text = schedule.Schedule.Name;
                if ( schedule.Selected )
                {
                    lbSchedule.AddCssClass( "active" );
                }

                var scheduleAttendance = ScheduleAttendanceList.Where( s => s.ScheduleId == schedule.Schedule.Id );
                lbSchedule.Text += " (" + scheduleAttendance.Select( s => s.AttendanceCount ).FirstOrDefault() + ")";
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCloseEditInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCloseEditInfo_Click( object sender, EventArgs e )
        {
            ShowOrHideAddModal( "edit-info-modal", false );
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
                ShowOrHideAddModal( "edit-info-modal", true );
                return;
            }

            var currentPerson = GetPerson();

            var changes = currentPerson.Person.FirstName == tbFirstName.Text;
            changes = changes || currentPerson.Person.LastName == tbLastName.Text;
            changes = changes || currentPerson.Person.NickName == tbNickname.Text;
            changes = changes || currentPerson.Person.BirthDate == dpDOB.SelectedDate;

            if ( changes )
            {
                var newContext = new RockContext();
                var person = new PersonService( newContext ).Get( currentPerson.Person.Id );
                person.LoadAttributes();

                person.FirstName = tbFirstName.Text;
                currentPerson.Person.FirstName = tbFirstName.Text;

                person.LastName = tbLastName.Text;
                currentPerson.Person.LastName = tbLastName.Text;

                person.BirthDate = dpDOB.SelectedDate;
                currentPerson.Person.BirthDate = dpDOB.SelectedDate;

                person.NickName = tbNickname.Text.Length > 0 ? tbNickname.Text : tbFirstName.Text;
                currentPerson.Person.NickName = tbNickname.Text.Length > 0 ? tbNickname.Text : tbFirstName.Text;

                var optionGroup = ddlAbility.SelectedItem.Attributes["optiongroup"];
                if ( !string.IsNullOrEmpty( optionGroup ) )
                {
                    // Selected ability level
                    if ( optionGroup == "Ability" )
                    {
                        person.SetAttributeValue( "AbilityLevel", ddlAbility.SelectedValue );
                        currentPerson.Person.SetAttributeValue( "AbilityLevel", ddlAbility.SelectedValue );

                        person.Grade = null;
                        currentPerson.Person.Grade = null;

                        person.SaveAttributeValues();
                    }
                    // Selected a grade
                    else if ( optionGroup == "Grade" )
                    {
                        var grade = ddlAbility.SelectedValueAsEnum<GradeLevel>();
                        person.Grade = (int?)grade;
                        currentPerson.Person.Grade = (int?)grade;

                        person.Attributes.Remove( "AbilityLevel" );
                        currentPerson.Person.Attributes.Remove( "AbilityLevel" );

                        person.SaveAttributeValues();
                    }
                }

                newContext.SaveChanges();
                SetHeader();
            }

            ShowOrHideAddModal( "edit-info-modal", false );
        }

        /// <summary>
        /// Handles the Click event of the lbEditInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEditInfo_Click( object sender, EventArgs e )
        {
            ResetEditInfo();
            ShowOrHideAddModal( "edit-info-modal", true );
        }

        /// <summary>
        /// Handles the Click event of the lbCloseNotes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCloseNotes_Click( object sender, EventArgs e )
        {
            ShowOrHideAddModal( "notes-modal", false );
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the lvLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvLocation_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            Pager.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
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
            var person = GetPerson();

            if ( person != null )
            {
                // Delete an item. Remove the selected attribute from the group, location and schedule
                int index = e.RowIndex;
                var row = gSelectedGrid.Rows[index];
                var dataKeyValues = gSelectedGrid.DataKeys[index].Values;
                var locationId = int.Parse( dataKeyValues["LocationId"].ToString() );
                var scheduleId = int.Parse( dataKeyValues["ScheduleId"].ToString() );

                CheckInGroupType selectedGroupType;
                if ( person.GroupTypes.Count == 1 )
                {
                    selectedGroupType = person.GroupTypes.FirstOrDefault();
                }
                else
                {
                    selectedGroupType = person.GroupTypes.Where( gt => gt.Selected
                        && gt.Groups.Any( g => g.Locations.Any( l => l.Location.Id == locationId
                            && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) ) ) ).FirstOrDefault();
                }
                var selectedGroup = selectedGroupType.Groups.Where( g => g.Selected
                    && g.Locations.Any( l => l.Location.Id == locationId
                        && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) ) ).FirstOrDefault();
                var selectedLocation = selectedGroup.Locations.Where( l => l.Selected
                    && l.Location.Id == locationId && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) ).FirstOrDefault();
                var selectedSchedule = selectedLocation.Schedules.Where( s => s.Selected
                    && s.Schedule.Id == scheduleId ).FirstOrDefault();
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
        /// Handles the Click event of the lbAddNote control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddNote_Click( object sender, EventArgs e )
        {
            //mpeAddNote.Show();
            ShowOrHideAddModal( "notes-modal", true );
        }

        /// <summary>
        /// Shows or hides the modal.
        /// </summary>
        /// <param name="elementId">The element identifier.</param>
        /// <param name="doShow">if set to <c>true</c> [do show].</param>
        protected void ShowOrHideAddModal( string elementId, bool doShow )
        {
            var js = "$('.modal-backdrop').remove();";

            if ( doShow )
            {
                js += "var modal = $('#" + elementId + ":not(:visible)');" +
                    "modal.modal('show');";
            }
            else
            {
                js += "var modal = $('#" + elementId + ":visible');" +
                    "modal.modal('hide');";
            }

            ScriptManager.RegisterStartupScript( Page, Page.GetType(), DateTime.Now.ToString(), js, true );
        }

        /// <summary>
        /// Handles the Click event of the lbAddNoteCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddNoteCancel_Click( object sender, EventArgs e )
        {
            hfAllergyAttributeId.Value = string.Empty;
        }

        /// <summary>
        /// Handles the Click event of the lbAddNoteSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddNoteSave_Click( object sender, EventArgs e )
        {
            var person = GetPerson();
            var rockContext = new RockContext();
            var checkInNote = new NoteService( rockContext ).GetByNoteTypeId( int.Parse( ViewState["checkInNoteTypeId"].ToString() ) )
                .Where( n => n.EntityId == person.Person.Id )
                .FirstOrDefault();
            if ( checkInNote == null )
            {
                checkInNote = new Note();
                checkInNote.IsSystem = false;
                checkInNote.EntityId = person.Person.Id;
                checkInNote.NoteTypeId = int.Parse( ViewState["checkInNoteTypeId"].ToString() );
                rockContext.Notes.Add( checkInNote );
            }

            checkInNote.Text = tbNoteText.Text;

            var allergyAttributeId = new AttributeService( rockContext )
                .GetByEntityTypeId( new Person().TypeId )
                .Where( a => a.Name.ToUpper() == "ALLERGY" )
                .Select( a => (int?)a.Id ).FirstOrDefault();
            if ( allergyAttributeId != null )
            {
                var allergyAttribute = Rock.Web.Cache.AttributeCache.Read( (int)allergyAttributeId );

                var allergyAttributeControl = phAttributes.FindControl( string.Format( "attribute_field_{0}", allergyAttributeId ) );
                if ( allergyAttributeControl != null )
                {
                    person.Person.LoadAttributes();
                    person.Person.SetAttributeValue( "Allergy", allergyAttribute.FieldType.Field
                        .GetEditValue( allergyAttributeControl, allergyAttribute.QualifierValues ) );
                    person.Person.SaveAttributeValues( rockContext );
                    hfAllergyAttributeId.Value = string.Empty;
                }
            }

            rockContext.SaveChanges();
            ShowOrHideAddModal( "notes-modal", false );
        }

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            GoBack();
        }

        /// <summary>
        /// Handles the Click event of the lbNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNext_Click( object sender, EventArgs e )
        {
            GoNext();
        }

        #endregion Edit Events

        #region Internal Methods

        /// <summary>
        /// Binds the group types.
        /// </summary>
        /// <param name="person">The person.</param>
        protected void BindGroupTypes( List<CheckInGroupType> groupTypes )
        {
            rGroupType.DataSource = groupTypes;
            rGroupType.DataBind();
            pnlGroupTypes.Update();
        }

        /// <summary>
        /// Binds the locations.
        /// </summary>
        /// <param name="person">The person.</param>
        protected void BindLocations( List<CheckInGroupType> groupTypes )
        {
            if ( ViewState["groupTypeId"] != null )
            {
                int groupTypeId = ViewState["groupTypeId"].ToString().AsType<int>();
                int locationId = ViewState["locationId"].ToString().AsType<int>();

                CheckInGroupType groupType = null;
                if ( groupTypes.Any( gt => gt.GroupType.Id == groupTypeId ) )
                {
                    groupType = groupTypes.Where( gt => gt.GroupType.Id == groupTypeId ).FirstOrDefault();
                }
                else
                {
                    groupType = groupTypes.FirstOrDefault();
                }

                CheckInLocation location = null;
                var locations = groupType.Groups.SelectMany( g => g.Locations ).ToList();
                if ( locationId > 0 )
                {
                    location = locations.Where( l => l.Location.Id == locationId ).FirstOrDefault();
                    var selectedLocationPlaceInList = locations.IndexOf( location ) + 1;
                    var pageSize = this.Pager.PageSize;
                    var pageToGoTo = selectedLocationPlaceInList / pageSize;
                    if ( selectedLocationPlaceInList % pageSize != 0 || pageToGoTo == 0 )
                    {
                        pageToGoTo++;
                    }

                    this.Pager.SetPageProperties( ( pageToGoTo - 1 ) * this.Pager.PageSize, this.Pager.MaximumRows, false );
                }
                else
                {
                    location = locations.FirstOrDefault();
                }

                Session["locations"] = locations;
                lvLocation.DataSource = locations;
                lvLocation.DataBind();
                pnlLocations.Update();
            }
        }

        /// <summary>
        /// Binds the schedules.
        /// </summary>
        /// <param name="person">The person.</param>
        protected void BindSchedules( List<CheckInGroupType> groupTypes )
        {
            if ( ViewState["groupTypeId"] != null )
            {
                int groupTypeId = ViewState["groupTypeId"].ToString().AsType<int>();
                int locationId = ViewState["locationId"].ToString().AsType<int>();

                CheckInGroupType groupType = null;
                if ( groupTypes.Any( gt => gt.GroupType.Id == groupTypeId ) )
                {
                    groupType = groupTypes.Where( gt => gt.GroupType.Id == groupTypeId ).FirstOrDefault();
                }
                else
                {
                    groupType = groupTypes.FirstOrDefault();
                }

                CheckInLocation location = null;
                var locations = groupType.Groups.SelectMany( g => g.Locations ).ToList();
                if ( locations.Any( l => l.Location.Id == locationId ) )
                {
                    location = locations.Where( l => l.Location.Id == locationId ).FirstOrDefault();
                }
                else
                {
                    location = locations.FirstOrDefault();
                }

                GetScheduleAttendance( location );
                rSchedule.DataSource = location.Schedules.ToList();
                rSchedule.DataBind();
                pnlSchedules.Update();
            }
        }

        /// <summary>
        /// Binds the selected items to the grid.
        /// </summary>
        protected void BindSelectedGrid()
        {
            var person = GetPerson();

            if ( person != null )
            {
                var selectedGroupTypes = person.GroupTypes.Where( gt => gt.Selected ).ToList();
                var selectedGroups = selectedGroupTypes.SelectMany( gt => gt.Groups.Where( g => g.Selected ) ).ToList();
                var selectedLocations = selectedGroups.SelectMany( g => g.Locations.Where( l => l.Selected ) ).ToList();

                var checkInList = new List<CheckIn>();
                foreach ( var location in selectedLocations )
                {
                    foreach ( var schedule in location.Schedules.Where( s => s.Selected ) )
                    {
                        var checkIn = new CheckIn();
                        checkIn.Location = location.Location.Name;
                        checkIn.Schedule = schedule.Schedule.Name;
                        checkIn.StartTime = Convert.ToDateTime( schedule.StartTime );
                        checkIn.LocationId = location.Location.Id;
                        checkIn.ScheduleId = schedule.Schedule.Id;
                        checkInList.Add( checkIn );
                    }
                }

                gSelectedGrid.DataSource = checkInList.OrderBy( c => c.StartTime )
                    .ThenBy( c => c.Schedule ).ToList();
                gSelectedGrid.DataBind();
                pnlSelected.Update();
            }
        }

        /// <summary>
        /// Gets the family.
        /// </summary>
        /// <returns></returns>
        private CheckInFamily GetFamily()
        {
            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
        }

        /// <summary>
        /// Gets the person.
        /// </summary>
        /// <returns></returns>
        private CheckInPerson GetPerson()
        {
            var personId = Request.QueryString["personId"].AsType<int?>();
            var family = GetFamily();

            if ( personId == null || personId < 1 || family == null )
            {
                return null;
            }

            return family.People.Where( p => p.Person.Id == personId ).FirstOrDefault();
        }

        /// <summary>
        /// Resets the edit info modal.
        /// </summary>
        private void ResetEditInfo()
        {
            var person = GetPerson();
            ddlAbility.LoadAbilityAndGradeItems();

            tbFirstName.Text = person.Person.FirstName;
            tbLastName.Text = person.Person.LastName;
            tbNickname.Text = person.Person.NickName;
            dpDOB.SelectedDate = person.Person.BirthDate;

            tbFirstName.Required = true;
            tbLastName.Required = true;
            dpDOB.Required = true;

            if ( person.Person.Grade.HasValue )
            {
                ddlAbility.SelectedValue = ( (GradeLevel)person.Person.Grade.Value ).ToString();
            }
            else if ( person.Person.Attributes.ContainsKey( "AbilityLevel" ) )
            {
                ddlAbility.SelectedValue = person.Person.GetAttributeValue( "AbilityLevel" );
            }
        }

        /// <summary>
        /// Goes back to the confirmation page with no changes.
        /// </summary>
        private new void GoBack()
        {
            var person = GetPerson();

            if ( person != null )
            {
                var groupTypes = person.GroupTypes.ToList();
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

            NavigateToPreviousPage();
        }

        /// <summary>
        /// Goes to the confirmation page with changes.
        /// </summary>
        private void GoNext()
        {
            if ( gSelectedGrid.Rows.Count > 0 )
            {
                var person = GetPerson();

                if ( person != null )
                {
                    var groupTypes = person.GroupTypes.ToList();
                    groupTypes.ForEach( gt => gt.PreSelected = gt.Selected );

                    var groups = groupTypes.SelectMany( gt => gt.Groups ).ToList();
                    groups.ForEach( g => g.PreSelected = g.Selected );

                    var locations = groups.SelectMany( g => g.Locations ).ToList();
                    locations.ForEach( l => l.PreSelected = l.Selected );

                    var schedules = locations.SelectMany( l => l.Schedules ).ToList();
                    schedules.ForEach( s => s.PreSelected = s.Selected );
                }
                else
                {
                    maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
                }

                SaveState();
                NavigateToNextPage();
            }
            else
            {
                string errorMsg = "<ul><li>" + "You must select an activity to continue. Otherwise, click the Back arrow." + "</li></ul>";
                maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Shows the note modal.
        /// </summary>
        /// <param name="attributeId">The attribute id.</param>
        /// <param name="entityId">The entity id.</param>
        protected void LoadAttributeControl( int allergyAttributeId, int personId )
        {
            var attribute = AttributeCache.Read( allergyAttributeId );
            var person = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault()
                .People.Where( p => p.Person.Id == personId ).FirstOrDefault();

            phAttributes.Controls.Clear();
            person.Person.LoadAttributes();
            var attributeValue = person.Person.GetAttributeValue( attribute.Key );
            attribute.AddControl( phAttributes.Controls, attributeValue, "", true, true );
            hfAllergyAttributeId.Value = attribute.Id.ToString();
        }

        /// <summary>
        /// Gets the attendance count for the first available schedule for a location. This will show on the location buttons.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        protected string GetLocationAttendance( CheckInLocation location )
        {
            return KioskLocationAttendance.Read( location.Location.Id ).CurrentCount.ToString();
        }

        /// <summary>
        /// Gets the attendance count for all of the schedules for a location. This will show on the schedule buttons.
        /// </summary>
        /// <param name="location"></param>
        protected void GetScheduleAttendance( CheckInLocation location )
        {
            var rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );
            var attendanceQuery = attendanceService.GetByDateAndLocation( DateTime.Now, location.Location.Id );
            ScheduleAttendanceList.Clear();
            foreach ( var schedule in location.Schedules )
            {
                ScheduleAttendance sa = new ScheduleAttendance();
                sa.ScheduleId = schedule.Schedule.Id;
                sa.AttendanceCount = attendanceQuery.Where( l => l.ScheduleId == sa.ScheduleId ).Count();
                ScheduleAttendanceList.Add( sa );
            }
        }

        #endregion Internal Methods
    }
}