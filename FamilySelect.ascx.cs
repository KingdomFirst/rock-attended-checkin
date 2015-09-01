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

namespace RockWeb.Plugins.cc_newspring.AttendedCheckin
{
    /// <summary>
    /// Family Select block for Attended Check-in
    /// </summary>
    [DisplayName( "Family Select" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Family Select Block" )]
    [BooleanField( "Enable Add Buttons", "Show the add people/visitor/family buttons on the family select page?", true )]
    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Default Connection Status", "Select the default connection status for people added in checkin", true, false, "B91BA046-BC1E-400C-B85D-638C1F4E0CE2" )]
    [TextField( "Not Found Text", "What text should display when the nothing is found?", true, "Please add them using one of the buttons on the right" )]
    public partial class FamilySelect : CheckInBlock
    {
        #region Variables

        /// <summary>
        /// Gets the kiosk campus identifier.
        /// </summary>
        /// <value>
        /// The kiosk campus identifier.
        /// </value>
        private int? KioskCampusId
        {
            get
            {
                var campusId = ViewState["CampusId"] as int?;
                if ( campusId != null )
                {
                    return campusId;
                }
                else
                {
                    var kioskCampusId = CurrentCheckInState.Kiosk.KioskGroupTypes
                        .Where( gt => gt.KioskGroups.Any( g => g.KioskLocations.Any( l => l.CampusId.HasValue ) ) )
                        .SelectMany( gt => gt.KioskGroups.SelectMany( g => g.KioskLocations.Select( l => l.CampusId ) ) )
                        .FirstOrDefault();
                    ViewState["CampusId"] = kioskCampusId;
                    return kioskCampusId;
                }
            }
        }

        #endregion Variables

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
                if ( !Page.IsPostBack )
                {
                    if ( CurrentCheckInState.CheckIn.Families.Count > 0 )
                    {
                        // Load the family results
                        ProcessFamily();

                        // Load the person/visitor results
                        ProcessPeople();
                    }
                    else
                    {
                        ShowHideResults( false );
                    }
                }
            }
        }

        #endregion Control Methods

        #region Click Events

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
            var selectedPeopleIds = ( hfSelectedPerson.Value + hfSelectedVisitor.Value )
                .SplitDelimitedValues().Select( int.Parse ).ToList();

            var selectedFamily = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
            if ( selectedFamily == null )
            {
                maWarning.Show( "Please pick or add a family.", ModalAlertType.Warning );
                return;
            }
            else if ( selectedFamily.People.Count == 0 )
            {
                string errorMsg = "No one in this family is eligible to check-in.";
                maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
                return;
            }
            else if ( !selectedPeopleIds.Any() )
            {
                maWarning.Show( "Please pick at least one person.", ModalAlertType.Warning );
                return;
            }
            else
            {
                selectedFamily.People.ForEach( p => p.Selected = selectedPeopleIds.Contains( p.Person.Id ) );

                var errors = new List<string>();
                if ( ProcessActivity( "Activity Search", out errors ) )
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
        }

        /// <summary>
        /// Handles the ItemCommand event of the lvFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewCommandEventArgs"/> instance containing the event data.</param>
        protected void lvFamily_ItemCommand( object sender, ListViewCommandEventArgs e )
        {
            int id = int.Parse( e.CommandArgument.ToString() );
            var family = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Group.Id == id );

            foreach ( ListViewDataItem li in ( (ListView)sender ).Items )
            {
                ( (LinkButton)li.FindControl( "lbSelectFamily" ) ).RemoveCssClass( "active" );
            }

            if ( !family.Selected )
            {
                CurrentCheckInState.CheckIn.Families.ForEach( f => f.Selected = false );
                ( (LinkButton)e.Item.FindControl( "lbSelectFamily" ) ).AddCssClass( "active" );
                family.Selected = true;
                ProcessPeople( family );
            }
            else
            {
                family.Selected = false;
                lvPerson.DataSource = null;
                lvPerson.DataBind();
                dpPersonPager.Visible = false;
                pnlPerson.Update();
                lvVisitor.DataSource = null;
                lvVisitor.DataBind();
                dpVisitorPager.Visible = false;
                pnlVisitor.Update();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbAddVisitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddVisitor_Click( object sender, EventArgs e )
        {
            lblAddPersonHeader.Text = "Add Visitor";
            newPersonType.Value = "Visitor";
            LoadPersonFields();
            mdlAddPerson.Show();
        }

        /// <summary>
        /// Handles the Click event of the lbAddFamilyMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddFamilyMember_Click( object sender, EventArgs e )
        {
            lblAddPersonHeader.Text = "Add Family Member";
            newPersonType.Value = "Person";
            LoadPersonFields();
            mdlAddPerson.Show();
        }

        /// <summary>
        /// Handles the Click event of the lbNewFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNewFamily_Click( object sender, EventArgs e )
        {
            var newFamilyList = new List<SerializedPerson>();
            var familyMembersToAdd = dpNewFamily.PageSize * 2;
            newFamilyList.AddRange( Enumerable.Repeat( new SerializedPerson(), familyMembersToAdd ) );
            ViewState["newFamily"] = newFamilyList;
            lvNewFamily.DataSource = newFamilyList;
            lvNewFamily.DataBind();
            mdlNewFamily.Show();
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the lvFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvFamily_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            dpFamilyPager.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );

            // rebind List View
            lvFamily.DataSource = CurrentCheckInState.CheckIn.Families
                .OrderByDescending( f => f.Group.CampusId == KioskCampusId )
                .ThenBy( f => f.Caption ).Take( 50 ).ToList();
            lvFamily.DataBind();
            pnlFamily.Update();
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the dpPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvPerson_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            dpPersonPager.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
            BindPager( hfSelectedPerson.Value, isFamilyMember: true );
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the dpVisitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvVisitor_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            dpVisitorPager.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
            BindPager( hfSelectedVisitor.Value, isFamilyMember: false );
        }

        /// <summary>
        /// Binds the pager.
        /// </summary>
        /// <param name="selectedPersonIds">The selected person ids.</param>
        /// <param name="isFamilyMember">if set to <c>true</c> [is family member].</param>
        private void BindPager( string selectedPersonIds, bool isFamilyMember )
        {
            var selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( selectedFamily != null )
            {
                var peopleList = selectedFamily.People.Where( f => f.FamilyMember == isFamilyMember && !f.ExcludedByFilter )
                    .OrderByDescending( p => p.Person.AgePrecise ).ToList();

                var selectedPeople = selectedPersonIds.SplitDelimitedValues().Select( int.Parse ).ToList();
                peopleList.ForEach( p => p.Selected = selectedPeople.Contains( p.Person.Id ) );

                // rebind List View
                if ( isFamilyMember )
                {
                    lvPerson.DataSource = peopleList;
                    lvPerson.DataBind();
                    pnlPerson.Update();
                }
                else
                {
                    lvVisitor.DataSource = peopleList;
                    lvVisitor.DataBind();
                    pnlVisitor.Update();
                }
            }
        }

        #endregion Click Events

        #region DataBound Methods

        /// <summary>
        /// Handles the DataBound event of the lvFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lvFamily_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                var family = (CheckInFamily)e.Item.DataItem;
                var lbSelectFamily = (LinkButton)e.Item.FindControl( "lbSelectFamily" );
                lbSelectFamily.CommandArgument = family.Group.Id.ToString();

                lbSelectFamily.Text = string.Format( @"{0}<br />
                    <span class='checkin-sub-title'>
						{1}
				    </span>
                    <div class='fa fa-refresh fa-spin'></div>
                ", family.Caption, family.SubCaption );

                if ( family.Selected )
                {
                    lbSelectFamily.AddCssClass( "active" );
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the lvPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void lvPeople_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                var lbSelectPerson = (LinkButton)e.Item.FindControl( "lbSelectPerson" );
                var person = (CheckInPerson)e.Item.DataItem;

                string ageLabel = "n/a";
                string birthdayLabel = "n/a";
                if ( person.Person.Age != null )
                {
                    birthdayLabel = person.Person.BirthMonth + "/" + person.Person.BirthDay;
                    ageLabel = person.Person.Age <= 18 ? person.Person.Age.ToString() : "Adult";
                }

                lbSelectPerson.Text = string.Format( @"{0}<br />
                    <span class='checkin-sub-title'>
						Birthday: {1} Age: {2}
				    </span>
                ", person.Person.FullName, birthdayLabel, ageLabel );

                if ( person.Selected )
                {
                    lbSelectPerson.AddCssClass( "active" );
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the lvNewFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewItemEventArgs"/> instance containing the event data.</param>
        protected void lvNewFamily_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                SerializedPerson person = ( (ListViewDataItem)e.Item ).DataItem as SerializedPerson;

                var ddlSuffix = (RockDropDownList)e.Item.FindControl( "ddlSuffix" );
                ddlSuffix.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ) ), true );
                if ( person.SuffixValueId.HasValue )
                {
                    ddlSuffix.SelectedValue = person.SuffixValueId.ToString();
                }

                var ddlGender = (RockDropDownList)e.Item.FindControl( "ddlGender" );
                ddlGender.BindToEnum<Gender>();
                if ( person.Gender != Gender.Unknown )
                {
                    ddlGender.SelectedIndex = person.Gender.ConvertToInt();
                }

                var ddlAbilityGrade = (RockDropDownList)e.Item.FindControl( "ddlAbilityGrade" );
                ddlAbilityGrade.LoadAbilityAndGradeItems();
                if ( !string.IsNullOrWhiteSpace( person.Ability ) )
                {
                    ddlAbilityGrade.SelectedValue = person.Ability;
                }
            }
        }

        #endregion DataBound Methods

        #region Modal Events

        /// <summary>
        /// Handles the Click event of the lbClosePerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbClosePerson_Click( object sender, EventArgs e )
        {
            mdlAddPerson.Hide();
        }

        /// <summary>
        /// Handles the Click event of the lbCloseFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCloseFamily_Click( object sender, EventArgs e )
        {
            mdlNewFamily.Hide();
        }

        /// <summary>
        /// Handles the Click event of the lbPersonSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbPersonSearch_Click( object sender, EventArgs e )
        {
            rGridPersonResults.PageIndex = 0;
            rGridPersonResults.Visible = true;
            rGridPersonResults.PageSize = 4;
            lbNewPerson.Visible = true;
            BindPersonGrid();
        }

        /// <summary>
        /// Handles the Click event of the lbNewPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNewPerson_Click( object sender, EventArgs e )
        {
            var selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( selectedFamily != null )
            {
                // CreatePeople only has a single person to validate/create
                var newPerson = new SerializedPerson()
                {
                    FirstName = tbPersonFirstName.Text,
                    LastName = tbPersonLastName.Text,
                    SuffixValueId = ddlPersonSuffix.SelectedValueAsId(),
                    BirthDate = dpPersonDOB.SelectedDate,
                    Gender = ddlPersonGender.SelectedValueAsEnum<Gender>(),
                    Ability = ddlPersonAbilityGrade.SelectedValue,
                    AbilityGroup = ddlPersonAbilityGrade.SelectedItem.Attributes["optiongroup"],
                    IsSpecialNeeds = cbPersonSpecialNeeds.Checked
                };

                if ( newPerson.IsValid() )
                {	// Person passed validation
                    var newPeople = CreatePeople( new List<SerializedPerson>( 1 ) { newPerson } );

                    var checkInPerson = new CheckInPerson();
                    checkInPerson.Person = newPeople.FirstOrDefault();
                    checkInPerson.FirstTime = true;

                    if ( !newPersonType.Value.Equals( "Visitor" ) )
                    {   // Family Member
                        AddGroupMembers( selectedFamily.Group, newPeople );
                        hfSelectedPerson.Value += checkInPerson.Person.Id + ",";
                        checkInPerson.FamilyMember = true;
                    }
                    else
                    {   // Visitor
                        AddVisitorRelationships( selectedFamily, checkInPerson.Person.Id );
                        hfSelectedVisitor.Value += checkInPerson.Person.Id + ",";
                        checkInPerson.FamilyMember = false;

                        // If a child, make the family group explicitly so the child role type can be selected. If no
                        // family group is explicitly made, Rock makes one with Adult role type by default
                        if ( dpPersonDOB.SelectedDate.Age() < 18 )
                        {
                            AddGroupMembers( null, newPeople );
                        }
                    }

                    checkInPerson.Selected = true;
                    selectedFamily.People.Add( checkInPerson );
                    selectedFamily.SubCaption = string.Join( ",", selectedFamily.People.Select( p => p.Person.FirstName ) );

                    ProcessPeople( selectedFamily );
                    mdlAddPerson.Hide();
                }
                else
                {
                    maWarning.Show( "Validation: Name, DOB, and Gender are required.", ModalAlertType.Information );
                }
            }
        }

        /// <summary>
        /// Handles the RowCommand event of the grdPersonSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewCommandEventArgs"/> instance containing the event data.</param>
        protected void rGridPersonResults_AddExistingPerson( object sender, GridViewCommandEventArgs e )
        {
            if ( e.CommandName.Equals( "Add" ) )
            {
                var selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
                if ( selectedFamily != null )
                {
                    int rowIndex = int.Parse( e.CommandArgument.ToString() );
                    int personId = int.Parse( rGridPersonResults.DataKeys[rowIndex].Value.ToString() );

                    var checkInPerson = selectedFamily.People.FirstOrDefault( p => p.Person.Id == personId );
                    if ( checkInPerson == null )
                    {
                        var rockContext = new RockContext();
                        checkInPerson.Person = new PersonService( rockContext ).Get( personId ).Clone( false );

                        if ( !newPersonType.Value.Equals( "Visitor" ) )
                        {
                            // New family member, add them to the current family if they don't exist
                            var groupMemberService = new GroupMemberService( rockContext );
                            if ( !selectedFamily.Group.Members.Any( gm => gm.PersonId == personId ) )
                            {
                                var familyMember = new GroupMember();
                                familyMember.GroupId = selectedFamily.Group.Id;
                                familyMember.PersonId = personId;
                                familyMember.IsSystem = false;
                                familyMember.GroupMemberStatus = GroupMemberStatus.Active;
                                familyMember.GroupRoleId = (int)selectedFamily.Group.GroupType.DefaultGroupRoleId;

                                groupMemberService.Add( familyMember );
                                rockContext.SaveChanges();
                            }

                            checkInPerson.FamilyMember = true;
                        }
                        else
                        {
                            // Visitor, associate with current family
                            AddVisitorRelationships( selectedFamily, personId, rockContext );
                            checkInPerson.FamilyMember = false;
                        }

                        checkInPerson.Selected = true;
                        selectedFamily.People.Add( checkInPerson );
                        ProcessPeople( selectedFamily );
                    }

                    mdlAddPerson.Hide();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbSaveFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSaveFamily_Click( object sender, EventArgs e )
        {
            var newFamilyList = (List<SerializedPerson>)ViewState["newFamily"] ?? new List<SerializedPerson>();
            int? currentPage = ViewState["currentPage"] as int?;
            int personOffset = 0;
            int pageOffset = 0;

            // add people from the current page
            foreach ( ListViewItem item in lvNewFamily.Items )
            {
                var newPerson = new SerializedPerson();
                newPerson.FirstName = ( (TextBox)item.FindControl( "tbFirstName" ) ).Text;
                newPerson.LastName = ( (TextBox)item.FindControl( "tbLastName" ) ).Text;
                newPerson.SuffixValueId = ( (RockDropDownList)item.FindControl( "ddlSuffix" ) ).SelectedValueAsId();
                newPerson.BirthDate = ( (DatePicker)item.FindControl( "dpBirthDate" ) ).SelectedDate;
                newPerson.Gender = ( (RockDropDownList)item.FindControl( "ddlGender" ) ).SelectedValueAsEnum<Gender>();
                newPerson.Ability = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedValue;
                newPerson.AbilityGroup = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedItem.Attributes["optiongroup"];
                newPerson.IsSpecialNeeds = ( (RockCheckBox)item.FindControl( "cbSpecialNeeds" ) ).Checked;

                if ( currentPage.HasValue )
                {
                    pageOffset = (int)currentPage * lvNewFamily.DataKeys.Count;
                }

                newFamilyList[pageOffset + personOffset] = newPerson;
                personOffset++;
            }

            List<Person> newPeople = CreatePeople( newFamilyList.Where( p => p.IsValid() ).ToList() );

            // People passed validation
            if ( newPeople.Any() )
            {
                // Create family group (by passing null) and add group members
                var familyGroup = AddGroupMembers( null, newPeople );

                var checkInFamily = new CheckInFamily();

                foreach ( var person in newPeople )
                {
                    var checkInPerson = new CheckInPerson();
                    checkInPerson.Person = person;
                    checkInPerson.FirstTime = true;
                    checkInPerson.Selected = true;
                    checkInPerson.FamilyMember = true;
                    checkInFamily.People.Add( checkInPerson );
                }

                checkInFamily.Group = familyGroup;
                checkInFamily.Caption = familyGroup.Name;
                checkInFamily.SubCaption = string.Join( ",", checkInFamily.People.Select( p => p.Person.FirstName ) );
                checkInFamily.Selected = true;

                CurrentCheckInState.CheckIn.Families.Clear();
                CurrentCheckInState.CheckIn.Families.Add( checkInFamily );

                ShowHideResults( checkInFamily.People.Count > 0 );
                ProcessFamily( checkInFamily );
                ProcessPeople( checkInFamily );
                mdlNewFamily.Hide();
            }
            else
            {
                maWarning.Show( "Validation: Name, DOB, and Gender are required.", ModalAlertType.Information );
            }
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the lvNewFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvNewFamily_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            var newFamilyList = (List<SerializedPerson>)ViewState["newFamily"] ?? new List<SerializedPerson>();
            int currentPage = e.StartRowIndex / e.MaximumRows;
            int? previousPage = ViewState["currentPage"] as int?;
            int personOffset = 0;
            int pageOffset = 0;

            foreach ( ListViewItem item in lvNewFamily.Items )
            {
                var newPerson = new SerializedPerson();
                newPerson.FirstName = ( (TextBox)item.FindControl( "tbFirstName" ) ).Text;
                newPerson.LastName = ( (TextBox)item.FindControl( "tbLastName" ) ).Text;
                newPerson.SuffixValueId = ( (RockDropDownList)item.FindControl( "ddlSuffix" ) ).SelectedValueAsId();
                newPerson.BirthDate = ( (DatePicker)item.FindControl( "dpBirthDate" ) ).SelectedDate;
                newPerson.Gender = ( (RockDropDownList)item.FindControl( "ddlGender" ) ).SelectedValueAsEnum<Gender>();
                newPerson.Ability = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedValue;
                newPerson.AbilityGroup = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedItem.Attributes["optiongroup"];

                if ( previousPage.HasValue )
                {
                    pageOffset = (int)previousPage * e.MaximumRows;
                }

                if ( e.StartRowIndex + personOffset + e.MaximumRows >= newFamilyList.Count )
                {
                    newFamilyList.AddRange( Enumerable.Repeat( new SerializedPerson(), e.MaximumRows ) );
                }

                newFamilyList[pageOffset + personOffset] = newPerson;
                personOffset++;
            }

            ViewState["currentPage"] = currentPage;
            ViewState["newFamily"] = newFamilyList;
            dpNewFamily.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
            lvNewFamily.DataSource = newFamilyList;
            lvNewFamily.DataBind();
            mdlNewFamily.Show();
        }

        /// <summary>
        /// Handles the GridRebind event of the rGridPersonResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rGridPersonResults_GridRebind( object sender, EventArgs e )
        {
            BindPersonGrid();
        }

        #endregion Modal Events

        #region Internal Methods

        /// <summary>
        /// Refreshes the family.
        /// </summary>
        private void ProcessFamily( CheckInFamily selectedFamily = null )
        {
            selectedFamily = selectedFamily ?? CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            var familyList = CurrentCheckInState.CheckIn.Families
                .OrderByDescending( f => f.Group.CampusId == KioskCampusId )
                .ThenBy( f => f.Caption ).Take( 50 ).ToList();

            // Order families by campus then by caption
            if ( CurrentCheckInState.CheckIn.Families.Count > 1 )
            {
                dpFamilyPager.Visible = true;
                dpFamilyPager.SetPageProperties( 0, dpFamilyPager.MaximumRows, false );
            }

            if ( selectedFamily != null )
            {
                selectedFamily.Selected = true;
            }
            else
            {
                familyList.FirstOrDefault().Selected = true;
            }

            lvFamily.DataSource = familyList;
            lvFamily.DataBind();
            pnlFamily.Update();
        }

        /// <summary>
        /// Processes the family.
        /// </summary>
        private void ProcessPeople( CheckInFamily selectedFamily = null )
        {
            var errors = new List<string>();
            if ( ProcessActivity( "Person Search", out errors ) )
            {
                List<CheckInPerson> memberDataSource = null;
                List<CheckInPerson> visitorDataSource = null;

                selectedFamily = selectedFamily ?? CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );

                if ( selectedFamily != null && selectedFamily.People.Any( f => !f.ExcludedByFilter ) )
                {
                    memberDataSource = selectedFamily.People.Where( f => f.FamilyMember && !f.ExcludedByFilter )
                        .OrderByDescending( p => p.Person.AgePrecise ).ToList();
                    memberDataSource.ForEach( p => p.Selected = true );

                    hfSelectedPerson.Value = string.Join( ",", memberDataSource.Select( f => f.Person.Id ) ) + ",";

                    visitorDataSource = selectedFamily.People.Where( f => !f.FamilyMember && !f.ExcludedByFilter )
                        .OrderByDescending( p => p.Person.AgePrecise ).ToList();
                    if ( visitorDataSource.Any( f => f.Selected ) )
                    {
                        hfSelectedVisitor.Value = string.Join( ",", visitorDataSource.Where( f => f.Selected )
                            .Select( f => f.Person.Id ).ToList() ) + ",";
                    }
                }

                lvPerson.DataSource = memberDataSource;
                lvPerson.DataBind();
                lvVisitor.DataSource = visitorDataSource;
                lvVisitor.DataBind();

                if ( memberDataSource != null )
                {
                    dpPersonPager.Visible = true;
                    dpPersonPager.SetPageProperties( 0, dpPersonPager.MaximumRows, false );
                }

                if ( visitorDataSource != null )
                {
                    dpVisitorPager.Visible = true;
                    dpVisitorPager.SetPageProperties( 0, dpVisitorPager.MaximumRows, false );
                }

                // Force an update
                pnlPerson.Update();
                pnlVisitor.Update();
            }
            else
            {
                string errorMsg = "<ul><li>" + errors.AsDelimited( "</li><li>" ) + "</li></ul>";
                maWarning.Show( errorMsg.Replace( "'", @"\'" ), ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Sets the display to show or hide panels depending on the search results.
        /// </summary>
        /// <param name="hasValidResults">if set to <c>true</c> [has valid results].</param>
        private void ShowHideResults( bool hasValidResults )
        {
            lbNext.Enabled = hasValidResults;
            lbAddFamilyMember.Enabled = hasValidResults;
            lbAddVisitor.Enabled = hasValidResults;
            lbNext.Visible = hasValidResults;
            pnlFamily.Visible = hasValidResults;
            pnlPerson.Visible = hasValidResults;
            pnlVisitor.Visible = hasValidResults;
            actions.Visible = hasValidResults;

            if ( !hasValidResults )
            {
                // Show a custom message when nothing is found
                string nothingFoundText = GetAttributeValue( "NotFoundText" );
                lblFamilyTitle.InnerText = "No Results";
                divNothingFound.InnerText = nothingFoundText;
                divNothingFound.Visible = true;
            }
            else
            {
                lblFamilyTitle.InnerText = "Search Results";
                divNothingFound.Visible = false;
            }

            // Admin option whether add buttons can be displayed
            bool showAddButtons = GetAttributeValue( "EnableAddButtons" ).AsBoolean();

            lbAddFamilyMember.Visible = showAddButtons;
            lbAddVisitor.Visible = showAddButtons;
            lbNewFamily.Visible = showAddButtons;
        }

        /// <summary>
        /// Loads the person fields.
        /// </summary>
        private void LoadPersonFields()
        {
            tbPersonFirstName.Text = string.Empty;
            tbPersonLastName.Text = string.Empty;
            ddlPersonSuffix.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ) ), true );
            ddlPersonSuffix.SelectedIndex = 0;
            ddlPersonGender.BindToEnum<Gender>();
            ddlPersonGender.SelectedIndex = 0;
            ddlPersonAbilityGrade.LoadAbilityAndGradeItems();
            ddlPersonAbilityGrade.SelectedIndex = 0;
            rGridPersonResults.Visible = false;
            lbNewPerson.Visible = false;
        }

        /// <summary>
        /// Binds the person search results grid on the New Person/Visitor screen.
        /// </summary>
        private void BindPersonGrid()
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var peopleQry = personService.Queryable().AsNoTracking();

            var abilityLevelValues = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_ABILITY_LEVEL_TYPE ), rockContext ).DefinedValues;

            var firstNameIsEmpty = string.IsNullOrEmpty( tbPersonFirstName.Text );
            var lastNameIsEmpty = string.IsNullOrEmpty( tbPersonLastName.Text );
            if ( !firstNameIsEmpty && !lastNameIsEmpty )
            {
                peopleQry = personService.GetByFullName( string.Format( "{0} {1}", tbPersonFirstName.Text, tbPersonLastName.Text ), false );
            }
            else if ( !lastNameIsEmpty )
            {
                peopleQry = peopleQry.Where( p => p.LastName.Equals( tbPersonLastName.Text ) );
            }
            else if ( !firstNameIsEmpty )
            {
                peopleQry = peopleQry.Where( p => p.FirstName.Equals( tbPersonFirstName.Text ) );
            }

            if ( ddlPersonSuffix.SelectedValueAsInt().HasValue )
            {
                var suffixValueId = ddlPersonSuffix.SelectedValueAsId();
                peopleQry = peopleQry.Where( p => p.SuffixValueId == suffixValueId );
            }

            if ( !string.IsNullOrEmpty( dpPersonDOB.Text ) )
            {
                DateTime searchDate;
                if ( DateTime.TryParse( dpPersonDOB.Text, out searchDate ) )
                {
                    peopleQry = peopleQry.Where( p => p.BirthYear == searchDate.Year
                        && p.BirthMonth == searchDate.Month && p.BirthDay == searchDate.Day );
                }
            }

            if ( ddlPersonGender.SelectedValueAsEnum<Gender>() != 0 )
            {
                var gender = ddlPersonGender.SelectedValueAsEnum<Gender>();
                peopleQry = peopleQry.Where( p => p.Gender == gender );
            }

            // Set a filter if an ability/grade was selected
            var optionGroup = ddlPersonAbilityGrade.SelectedItem.Attributes["optiongroup"];
            if ( !string.IsNullOrEmpty( optionGroup ) )
            {
                if ( optionGroup.Equals( "Ability" ) )
                {
                    peopleQry = peopleQry.WhereAttributeValue( rockContext, "AbilityLevel", ddlPersonAbilityGrade.SelectedValue );
                }
                else if ( optionGroup.Equals( "Grade" ) )
                {
                    var grade = ddlPersonAbilityGrade.SelectedValueAsId();
                    peopleQry = peopleQry.WhereGradeOffsetRange( grade, grade, false );
                }
            }

            // Set a filter if special needs was checked
            if ( cbPersonSpecialNeeds.Checked )
            {
                peopleQry = peopleQry.WhereAttributeValue( rockContext, "IsSpecialNeeds", "True" );
            }

            // call list here to get virtual properties not supported in LINQ
            var peopleList = peopleQry.ToList();

            // load attributes to display additional person info
            peopleList.ForEach( p => p.LoadAttributes( rockContext ) );

            // Load person grid
            rGridPersonResults.DataSource = peopleList.Select( p => new
                {
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    p.SuffixValue,
                    p.BirthDate,
                    p.Age,
                    p.Gender,
                    Attribute = p.GradeOffset.HasValue
                        ? p.GradeFormatted
                        : abilityLevelValues.Where( dv => dv.Guid.ToString()
                            .Equals( p.AttributeValues["AbilityLevel"].Value, StringComparison.OrdinalIgnoreCase ) )
                            .Select( dv => dv.Value ).FirstOrDefault(),
                    IsSpecialNeeds = p.AttributeValues.Keys.Contains( "IsSpecialNeeds" )
                         ? p.AttributeValues["IsSpecialNeeds"].Value == "True" ? "Yes" : string.Empty
                         : string.Empty
                } ).OrderByDescending( p => p.BirthDate ).ToList();

            rGridPersonResults.DataBind();
        }

        /// <summary>
        /// Gets the current person.
        /// </summary>
        /// <returns></returns>
        private CheckInPerson GetCurrentPerson( int? parameterPersonId = null )
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
        /// Creates the people.
        /// </summary>
        /// <param name="serializedPeople">The new people list.</param>
        /// <returns></returns>
        private List<Person> CreatePeople( List<SerializedPerson> serializedPeople )
        {
            var newPeopleList = new List<Person>();
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            var defaultStatusGuid = GetAttributeValue( "DefaultConnectionStatus" ).AsGuid();
            var connectionStatus = DefinedValueCache.Read( defaultStatusGuid, rockContext );

            var recordStatus = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS ), rockContext );
            var activeRecord = recordStatus.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE ) ) );

            var recordType = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_RECORD_TYPE ), rockContext );
            var personType = recordType.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON ) ) );

            foreach ( SerializedPerson personData in serializedPeople )
            {
                bool hasAbility = !string.IsNullOrWhiteSpace( personData.Ability ) && personData.AbilityGroup == "Ability";
                bool hasGrade = !string.IsNullOrWhiteSpace( personData.Ability ) && personData.AbilityGroup == "Grade";

                var person = new Person();
                person.FirstName = personData.FirstName;
                person.LastName = personData.LastName;
                person.SuffixValueId = personData.SuffixValueId;
                person.Gender = (Gender)personData.Gender;

                if ( personData.BirthDate != null )
                {
                    person.BirthDay = ( (DateTime)personData.BirthDate ).Day;
                    person.BirthMonth = ( (DateTime)personData.BirthDate ).Month;
                    person.BirthYear = ( (DateTime)personData.BirthDate ).Year;
                }

                if ( connectionStatus != null )
                {
                    person.ConnectionStatusValueId = connectionStatus.Id;
                }

                if ( activeRecord != null )
                {
                    person.RecordStatusValueId = activeRecord.Id;
                }

                if ( personType != null )
                {
                    person.RecordTypeValueId = personType.Id;
                }

                if ( hasGrade )
                {
                    person.GradeOffset = personData.Ability.AsIntegerOrNull();
                }

                // Add the person so we can assign an ability (if set)
                personService.Add( person );

                if ( hasAbility || personData.IsSpecialNeeds )
                {
                    person.LoadAttributes( rockContext );

                    if ( hasAbility )
                    {
                        person.SetAttributeValue( "AbilityLevel", personData.Ability );
                        person.SaveAttributeValues( rockContext );
                    }

                    person.SetAttributeValue( "IsSpecialNeeds", personData.IsSpecialNeeds.ToTrueFalse() );
                    person.SaveAttributeValues( rockContext );
                }

                newPeopleList.Add( person );
            }

            rockContext.SaveChanges();

            return newPeopleList;
        }

        /// <summary>
        /// Adds the group member.
        /// </summary>
        /// <param name="familyGroup">The family group.</param>
        /// <param name="person">The person.</param>
        /// <returns></returns>
        private Group AddGroupMembers( Group familyGroup, List<Person> newPeople )
        {
            var rockContext = new RockContext();
            var familyGroupType = GroupTypeCache.GetFamilyGroupType();

            // Create a new family group if one doesn't exist
            if ( familyGroup == null )
            {
                familyGroup = new Group();
                familyGroup.GroupTypeId = familyGroupType.Id;
                familyGroup.IsSecurityRole = false;
                familyGroup.IsSystem = false;
                familyGroup.IsPublic = true;
                familyGroup.IsActive = true;

                // Get oldest person's last name
                var familyName = newPeople.Where( p => p.BirthDate.HasValue )
                    .OrderByDescending( p => p.BirthDate )
                    .Select( p => p.LastName ).FirstOrDefault();

                familyGroup.Name = familyName + " Family";
                new GroupService( rockContext ).Add( familyGroup );
            }

            // Add group members
            var newGroupMembers = new List<GroupMember>();
            foreach ( var person in newPeople )
            {
                var groupMember = new GroupMember();
                groupMember.IsSystem = false;
                groupMember.IsNotified = false;
                groupMember.GroupId = familyGroup.Id;
                groupMember.PersonId = person.Id;

                if ( person.Age >= 18 )
                {
                    groupMember.GroupRoleId = familyGroupType.Roles.FirstOrDefault( r =>
                        r.Guid == new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT ) ).Id;
                }
                else
                {
                    groupMember.GroupRoleId = familyGroupType.Roles.FirstOrDefault( r =>
                        r.Guid == new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD ) ).Id;
                }

                newGroupMembers.Add( groupMember );
            }

            // New family group, save as part of tracked entity
            if ( familyGroup.Id == 0 )
            {
                familyGroup.Members = newGroupMembers;
            }
            else // use GroupMemberService to save to an existing group
            {
                new GroupMemberService( rockContext ).AddRange( newGroupMembers );
            }

            rockContext.SaveChanges();
            return familyGroup;
        }

        /// <summary>
        /// Adds the visitor group member roles.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="visitorId">The person id.</param>
        private void AddVisitorRelationships( CheckInFamily family, int visitorId, RockContext rockContext = null )
        {
            rockContext = rockContext ?? new RockContext();
            foreach ( var familyMember in family.People.Where( p => p.FamilyMember && p.Person.Age >= 18 ) )
            {
                Person.CreateCheckinRelationship( familyMember.Person.Id, visitorId, rockContext );
            }
        }

        #endregion Internal Methods

        #region NewPerson Class

        /// <summary>
        /// Lightweight Person model to serialize people to viewstate
        /// </summary>
        [Serializable()]
        protected class SerializedPerson
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public int? SuffixValueId { get; set; }

            public DateTime? BirthDate { get; set; }

            public Gender Gender { get; set; }

            public string Ability { get; set; }

            public string AbilityGroup { get; set; }

            public bool IsSpecialNeeds { get; set; }

            public bool IsValid()
            {
                // use OR and negation to immediately return when not valid
                return !( string.IsNullOrWhiteSpace( FirstName ) || string.IsNullOrWhiteSpace( LastName )
                    || !BirthDate.HasValue || Gender == Gender.Unknown );
            }

            public SerializedPerson()
            {
                FirstName = string.Empty;
                LastName = string.Empty;
                BirthDate = new DateTime?();
                Gender = new Gender();
                Ability = string.Empty;
                AbilityGroup = string.Empty;
                IsSpecialNeeds = false;
            }
        }

        #endregion NewPerson Class
    }
}