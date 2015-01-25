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
    [BooleanField( "Display Group Names", "By default location names are shown.  Check this option to show the group names instead.", false )]
    public partial class ActivitySelect : CheckInBlock
    {
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
                var person = GetPerson( personId );

                if ( person != null )
                {
                    var first = person.Person.NickName ?? person.Person.FirstName;
                    lblPersonName.Text = string.Format( "{0} {1}", first, person.Person.LastName );
                }

                if ( !Page.IsPostBack )
                {
                    if ( person != null && person.GroupTypes.Any() )
                    {
                        var selectedGroupType = person.GroupTypes.FirstOrDefault( gt => gt.Selected );
                        if ( selectedGroupType != null )
                        {
                            ViewState["groupTypeId"] = selectedGroupType.GroupType.Id.ToString();
                        }

                        ViewState["groupId"] = Request.QueryString["groupId"];
                        ViewState["locationId"] = Request.QueryString["locationId"];
                        ViewState["scheduleId"] = Request.QueryString["scheduleId"];

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
                            .FirstOrDefault( n => n.EntityId == person.Person.Id );

                        if ( checkInNote != null )
                        {
                            tbNoteText.Text = checkInNote.Text;
                        }
                    }
                    else
                    {
                        maWarning.Show( InvalidParameterError, ModalAlertType.Warning );
                        NavigateToPreviousPage();
                    }
                }

                // reload the attribute field.
                var allergyAttribute = new AttributeService( new RockContext() )
                    .GetByEntityTypeId( new Person().TypeId )
                    .FirstOrDefault( a => a.Name.ToUpper() == "ALLERGY" );
                if ( allergyAttribute != null )
                {
                    LoadAttributeControl( allergyAttribute.Id, person.Person.Id );
                }

                bool showGroupNames = bool.Parse( GetAttributeValue( "DisplayGroupNames" ) );
                if ( showGroupNames )
                {
                    hdrLocations.InnerText = "Group";
                }
            }
        }

        #endregion Control Methods

        #region Edit Events

        /// <summary>
        /// Handles the ItemCommand event of the rGroupType control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void lvGroupType_ItemCommand( object source, ListViewCommandEventArgs e )
        {
            var person = GetPerson();
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
                var groupId = ViewState["groupId"].ToString().AsType<int?>();
                var locationId = ViewState["locationId"].ToString().AsType<int?>();
                int scheduleId = Int32.Parse( e.CommandArgument.ToString() );

                // set this selected group, location, and schedule
                var selectedGroupType = person.GroupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                selectedGroupType.Selected = true;
                var selectedGroup = selectedGroupType.Groups.FirstOrDefault( g => g.Group.Id == groupId && g.Locations.Any( l => l.Location.Id == locationId ) );
                if ( selectedGroup == null )
                {
                    selectedGroup = selectedGroupType.Groups.FirstOrDefault( g => g.Locations.Any( l => l.Location.Id == locationId ) );
                }
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
            var selectedGroupTypeId = ViewState["groupTypeId"].ToString().AsType<int?>();
            if ( selectedGroupTypeId != null )
            {
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
            var selectedLocationId = ViewState["locationId"].ToString().AsType<int?>();
            if ( selectedLocationId != null )
            {
                bool showGroupNames = bool.Parse( GetAttributeValue( "DisplayGroupNames" ) ?? "false" );
                if ( e.Item.ItemType == ListViewItemType.DataItem )
                {
                    int locationId = 0;
                    string displayName = string.Empty;
                    bool optionSelected = false;
                    if ( !showGroupNames )
                    {
                        var location = (CheckInLocation)e.Item.DataItem;
                        locationId = location.Location.Id;
                        displayName = location.Location.Name;
                        optionSelected = location.Selected;
                    }
                    else
                    {
                        var group = (CheckInGroup)e.Item.DataItem;
                        displayName = group.Group.Name;
                        optionSelected = group.Selected;
                        locationId = group.Locations.Select( l => l.Location.Id ).FirstOrDefault();
                    }

                    var lbLocation = (LinkButton)e.Item.FindControl( "lbLocation" );
                    lbLocation.Text = string.Format( "{0} ({1})", displayName, KioskLocationAttendance.Read( locationId ).CurrentCount.ToString() );
                    lbLocation.CommandArgument = locationId.ToString();

                    if ( optionSelected )
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
                lbSchedule.Text += string.Format( " ({0})", scheduleAttendance.Select( s => s.AttendanceCount ).FirstOrDefault() );
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
            var person = GetPerson();

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
                var selectedGroup = selectedGroupType.Groups.FirstOrDefault( g => g.Selected && g.Group.Id == groupId
                    && g.Locations.Any( l => l.Location.Id == locationId
                        && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) ) );
                var selectedLocation = selectedGroup.Locations.FirstOrDefault( l => l.Selected
                    && l.Location.Id == locationId && l.Schedules.Any( s => s.Schedule.Id == scheduleId ) );
                var selectedSchedule = selectedLocation.Schedules.FirstOrDefault( s => s.Selected
                    && s.Schedule.Id == scheduleId );
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
            mdlNotes.Show();
        }

        /// <summary>
        /// Handles the Click event of the lbAddNoteSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSaveNotes_Click( object sender, EventArgs e )
        {
            var person = GetPerson();
            var rockContext = new RockContext();
            var checkInNote = new NoteService( rockContext ).GetByNoteTypeId( int.Parse( ViewState["checkInNoteTypeId"].ToString() ) )
                .FirstOrDefault( n => n.EntityId == person.Person.Id );
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
            mdlNotes.Hide();
        }

        /// <summary>
        /// Handles the Click event of the lbCloseNotes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCloseNotes_Click( object sender, EventArgs e )
        {
            mdlNotes.Hide();
        }

        /// <summary>
        /// Handles the Click event of the lbEditInfo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEditInfo_Click( object sender, EventArgs e )
        {
            ResetEditInfo();
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

            var currentPerson = GetPerson();

            var changes = currentPerson.Person.FirstName == tbFirstName.Text;
            changes = changes || currentPerson.Person.LastName == tbLastName.Text;
            changes = changes || currentPerson.Person.NickName == tbNickname.Text;
            changes = changes || currentPerson.Person.BirthDate == dpDOB.SelectedDate;

            if ( changes )
            {
                var rockContext = new RockContext();
                var person = new PersonService( rockContext ).Get( currentPerson.Person.Id );
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

                rockContext.SaveChanges();
            }

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
            int? groupTypeId = ViewState["groupTypeId"].ToString().AsType<int?>();
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
        protected void BindLocations( List<CheckInGroupType> groupTypes )
        {
            int? groupTypeId = ViewState["groupTypeId"].ToString().AsType<int>();
            if ( groupTypeId != null )
            {
                int groupId = ViewState["groupId"].ToString().AsType<int>();
                int locationId = ViewState["locationId"].ToString().AsType<int>();

                var groupType = groupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                if ( groupType == null )
                {
                    groupType = groupTypes.FirstOrDefault();
                }

                int placeInList = 1;
                List<Rock.Lava.ILiquidizable> locationItems = null;
                bool showGroupNames = bool.Parse( GetAttributeValue( "DisplayGroupNames" ) ?? "false" );
                if ( !showGroupNames )
                {
                    var allLocations = groupType.Groups.SelectMany( g => g.Locations ).OrderBy( l => l.Location.Name ).ToList();
                    if ( locationId > 0 )
                    {
                        var selectedLocation = allLocations.FirstOrDefault( l => l.Location.Id == locationId );
                        placeInList = allLocations.IndexOf( selectedLocation ) + 1;
                    }

                    // Show Group Names not set, locationItems is Type <CheckInLocation>
                    locationItems = allLocations.Cast<Rock.Lava.ILiquidizable>().ToList();
                }
                else
                {
                    var allGroups = groupType.Groups.OrderBy( g => g.Group.Name ).ToList();
                    if ( groupId > 0 )
                    {
                        var selectedGroup = allGroups.FirstOrDefault( g => g.Group.Id == groupId && g.Locations.Any( l => l.Location.Id == locationId ) );
                        placeInList = allGroups.IndexOf( selectedGroup ) + 1;
                    }

                    // Show Group Names is set, locationItems is Type <CheckInGroup>
                    locationItems = allGroups.Cast<Rock.Lava.ILiquidizable>().ToList();
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
        protected void BindSchedules( List<CheckInGroupType> groupTypes )
        {
            int? groupTypeId = ViewState["groupTypeId"].ToString().AsType<int?>();
            if ( groupTypeId != null )
            {
                int locationId = ViewState["locationId"].ToString().AsType<int>();

                var groupType = groupTypes.FirstOrDefault( gt => gt.GroupType.Id == groupTypeId );
                if ( groupType == null )
                {
                    groupType = groupTypes.FirstOrDefault();
                }

                var location = groupType.Groups.SelectMany( g => g.Locations ).FirstOrDefault( l => l.Location.Id == locationId );
                if ( location == null )
                {
                    location = groupType.Groups.SelectMany( g => g.Locations ).FirstOrDefault();
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
                            checkIn.Location = location.Location.Name;
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
        /// Gets the person.
        /// </summary>
        /// <returns></returns>
        private CheckInPerson GetPerson( int? parameterPersonId = null )
        {
            var personId = parameterPersonId ?? Request.QueryString["personId"].AsType<int?>();
            var family = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );

            if ( personId == null || personId < 1 || family == null )
            {
                return null;
            }

            return family.People.FirstOrDefault( p => p.Person.Id == personId );
        }

        /// <summary>
        /// Loads the attribute control.
        /// </summary>
        /// <param name="allergyAttributeId">The allergy attribute identifier.</param>
        /// <param name="personId">The person identifier.</param>
        protected void LoadAttributeControl( int allergyAttributeId, int personId )
        {
            var attribute = AttributeCache.Read( allergyAttributeId );
            var person = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected )
                .People.FirstOrDefault( p => p.Person.Id == personId );

            phAttributes.Controls.Clear();
            person.Person.LoadAttributes();
            var attributeValue = person.Person.GetAttributeValue( attribute.Key );
            attribute.AddControl( phAttributes.Controls, attributeValue, "", true, true );
            hfAllergyAttributeId.Value = attribute.Id.ToString();
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
                    ScheduleAttendance sa = new ScheduleAttendance();
                    sa.ScheduleId = schedule.Schedule.Id;
                    sa.AttendanceCount = attendanceQuery.Where( l => l.ScheduleId == sa.ScheduleId ).Count();
                    ScheduleAttendanceList.Add( sa );
                }
            }
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
                var personAbility = person.Person.GetAttributeValue( "AbilityLevel" );
                if ( !string.IsNullOrWhiteSpace( personAbility ) )
                {
                    ddlAbility.SelectedValue = personAbility;
                }
            }
        }

        /// <summary>
        /// Unsets the changes.
        /// </summary>
        private void UnsetChanges()
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
        }

        /// <summary>
        /// Goes to the confirmation page with changes.
        /// </summary>
        private void GoNext()
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

            ProcessSelection( maWarning );
        }

        #endregion Internal Methods
    }
}