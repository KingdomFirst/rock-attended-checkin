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

namespace RockWeb.Plugins.cc_newspring.AttendedCheckin
{
    /// <summary>
    /// Family Select block for Attended Check-in
    /// </summary>
    [DisplayName( "Family Select" )]
    [Category( "Check-in > Attended" )]
    [Description( "Attended Check-In Family Select Block" )]
    [BooleanField( "Enable Add Buttons", "Show the add people/visitor/family buttons on the family select page?", true )]
    [TextField( "Not Found Text", "What text should display when the nothing is found?", true, "Please add them using one of the buttons on the right" )]
    public partial class FamilySelect : CheckInBlock
    {
        #region Variables

        private int? KioskCampusId
        {
            get
            {
                var campusId = ViewState["CampusId"] as string;
                if ( campusId != null )
                {
                    return campusId.AsType<int?>();
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
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( CurrentWorkflow == null || CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                if ( CurrentCheckInState.CheckIn.Families.Count > 0 )
                {
                    // Load the family results
                    DisplayFamily();

                    // Load the person/visitor results
                    ProcessFamily();
                }
                else
                {
                    ShowHideResults( false );
                }
            }
        }

        /// <summary>
        /// Refreshes the family.
        /// </summary>
        protected void DisplayFamily()
        {
            // Order families by campus then by caption
            List<CheckInFamily> familyList = CurrentCheckInState.CheckIn.Families;
            if ( familyList.Count > 1 )
            {
                familyList = CurrentCheckInState.CheckIn.Families.OrderByDescending( f => f.Group.CampusId == KioskCampusId )
                    .ThenBy( f => f.Caption ).ToList();
            }

            // Auto process the first family if one not selected
            if ( !familyList.Any( f => f.Selected ) )
            {
                familyList.FirstOrDefault().Selected = true;
            }
            // maybe rebind person/visitor here?

            if ( familyList.Any() )
            {
                dpFamilyPager.Visible = true;
                dpFamilyPager.SetPageProperties( 0, dpFamilyPager.MaximumRows, false );
            }

            lvFamily.DataSource = familyList;
            lvFamily.DataBind();
            pnlFamily.Update();
        }

        /// <summary>
        /// Sets the display to show or hide panels depending on the search results.
        /// </summary>
        /// <param name="hasValidResults">if set to <c>true</c> [has valid results].</param>
        private void ShowHideResults( bool hasValidResults )
        {
            lbNext.Enabled = hasValidResults;
            lbNext.Visible = hasValidResults;
            pnlFamily.Visible = hasValidResults;
            pnlPerson.Visible = hasValidResults;
            pnlVisitor.Visible = hasValidResults;
            actions.Visible = hasValidResults;
            //lbCheckout.Visible = hasValidResults;

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

            // Check if the add buttons can be displayed
            bool showAddButtons = true;
            bool.TryParse( GetAttributeValue( "EnableAddButtons" ), out showAddButtons );

            lbAddFamilyMember.Visible = showAddButtons;
            lbAddVisitor.Visible = showAddButtons;
            lbNewFamily.Visible = showAddButtons;
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

            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
            if ( family == null )
            {
                maWarning.Show( "Please pick or add a family.", ModalAlertType.Warning );
                return;
            }
            else if ( family.People.Count == 0 )
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
                family.People.ForEach( p => p.Selected = selectedPeopleIds.Contains( p.Person.Id ) );
                ProcessSelection( maWarning );
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
            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Group.Id == id ).FirstOrDefault();

            foreach ( ListViewDataItem li in ( (ListView)sender ).Items )
            {
                ( (LinkButton)li.FindControl( "lbSelectFamily" ) ).RemoveCssClass( "active" );
            }

            if ( !family.Selected )
            {
                CurrentCheckInState.CheckIn.Families.ForEach( f => f.Selected = false );
                ( (LinkButton)e.Item.FindControl( "lbSelectFamily" ) ).AddCssClass( "active" );
                family.Selected = true;
                ProcessFamily();
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
            if ( !CurrentCheckInState.CheckIn.Families.Any( f => f.Selected ) )
            {
                maWarning.Show( "No family selected.  Please use the Add Family button.", ModalAlertType.Warning );
                return;
            }

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
            if ( !CurrentCheckInState.CheckIn.Families.Any( f => f.Selected ) )
            {
                maWarning.Show( "No family selected.  Please use the Add Family button.", ModalAlertType.Warning );
                return;
            }

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
        /// Handles the Click event of the lbCheckout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCheckout_Click( object sender, EventArgs e )
        {
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
                .ThenBy( f => f.Caption ).ToList();
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

            var selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( selectedFamily != null )
            {
                var peopleList = selectedFamily.People.Where( f => f.FamilyMember && !f.ExcludedByFilter )
                    .OrderBy( p => p.Person.FullNameReversed ).ToList();

                var selectedPeople = hfSelectedPerson.Value.SplitDelimitedValues().Select( int.Parse ).ToList();
                peopleList.ForEach( p => p.Selected = selectedPeople.Contains( p.Person.Id ) );

                // rebind List View
                lvPerson.DataSource = peopleList;
                lvPerson.DataBind();
                pnlPerson.Update();
            }
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the dpVisitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvVisitor_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            dpVisitorPager.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );

            var selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( selectedFamily != null )
            {
                var visitorList = selectedFamily.People.Where( f => !f.FamilyMember && !f.ExcludedByFilter )
                    .OrderBy( p => p.Person.FullNameReversed ).ToList();

                var selectedVisitors = hfSelectedVisitor.Value.SplitDelimitedValues().Select( int.Parse ).ToList();
                visitorList.ForEach( p => p.Selected = selectedVisitors.Contains( p.Person.Id ) );

                // rebind List View
                lvVisitor.DataSource = visitorList;
                lvVisitor.DataBind();
                pnlVisitor.Update();
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
                if ( family.Selected )
                {
                    var lbSelectFamily = (LinkButton)e.Item.FindControl( "lbSelectFamily" );
                    lbSelectFamily.AddCssClass( "active" );
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the lvPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void lvPerson_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                var person = (CheckInPerson)e.Item.DataItem;
                if ( person.Selected )
                {
                    var lbSelectPerson = (LinkButton)e.Item.FindControl( "lbSelectPerson" );
                    lbSelectPerson.AddCssClass( "active" );
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the lvVisitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void lvVisitor_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            if ( e.Item.ItemType == ListViewItemType.DataItem )
            {
                var person = (CheckInPerson)e.Item.DataItem;
                if ( person.Selected )
                {
                    var lbSelectVisitor = (LinkButton)e.Item.FindControl( "lbSelectVisitor" );
                    lbSelectVisitor.AddCssClass( "active" );
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
            // Make sure all required fields are filled out
            //Page.Validate( "Person" );
            //if ( !Page.IsValid )
            //{
            //    return;
            //}

            var checkInFamily = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
            if ( checkInFamily != null )
            {
                // CreatePeople only has a single person to validate/create
                var newPeople = CreatePeople( new List<SerializedPerson>() {
                    new SerializedPerson() {
                        FirstName = tbFirstNamePerson.Text,
                        LastName = tbLastNamePerson.Text,
                        SuffixValueId = ddlSuffix.SelectedValueAsId(),
                        BirthDate =  dpDOBPerson.SelectedDate,
                        Gender = ddlGenderPerson.SelectedValueAsEnum<Gender>(),
                        Ability =  ddlAbilityPerson.SelectedValue,
                        AbilityGroup = ddlAbilityPerson.SelectedItem.Attributes["optiongroup"]
                    }
                } );

                // Person passed validation
                if ( newPeople.Any() )
                {
                    var checkInPerson = new CheckInPerson();
                    checkInPerson.Person = newPeople.FirstOrDefault();

                    if ( !newPersonType.Value.Equals( "Visitor" ) )
                    {   // Family Member
                        AddGroupMembers( checkInFamily.Group, newPeople );
                        hfSelectedPerson.Value += checkInPerson.Person.Id + ",";
                        checkInPerson.FamilyMember = true;
                    }
                    else
                    {   // Visitor
                        AddVisitorRelationships( checkInFamily, checkInPerson.Person.Id );
                        hfSelectedVisitor.Value += checkInPerson.Person.Id + ",";
                        checkInPerson.FamilyMember = false;

                        // If a child, make the family group explicitly so the child role type can be selected. If no 
                        // family group is explicitly made, Rock makes one with Adult role type by default
                        if ( dpDOBPerson.SelectedDate.Age() < 18 )
                        {
                            AddGroupMembers( null, newPeople );
                        }                        
                    }

                    checkInPerson.Selected = true;
                    checkInFamily.People.Add( checkInPerson );
                    checkInFamily.SubCaption = string.Join( ",", checkInFamily.People.Select( p => p.Person.FirstName ) );

                    //tbFirstNamePerson.Required = false;
                    //tbLastNamePerson.Required = false;
                    //ddlGenderPerson.Required = false;
                    //dpDOBPerson.Required = false;

                    ProcessFamily();
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
                var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    int rowIndex = int.Parse( e.CommandArgument.ToString() );
                    int personId = int.Parse( rGridPersonResults.DataKeys[rowIndex].Value.ToString() );

                    var personAlreadyInFamily = family.People.Any( p => p.Person.Id == personId );
                    if ( !personAlreadyInFamily )
                    {
                        var rockContext = new RockContext();
                        var checkInPerson = new CheckInPerson();
                        checkInPerson.Person = new PersonService( rockContext ).Get( personId ).Clone( false );

                        if ( !newPersonType.Value.Equals( "Visitor" ) )
                        {
                            // Family member
                            var groupMember = new GroupMemberService( rockContext ).GetByPersonId( personId )
                                .FirstOrDefault( gm => gm.Group.GroupType.Guid == new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ) );
                            if ( groupMember != null )
                            {
                                groupMember.GroupId = family.Group.Id;
                                rockContext.SaveChanges();
                            }

                            checkInPerson.FamilyMember = true;
                        }
                        else
                        {
                            // Visitor
                            AddVisitorRelationships( family, personId );
                            checkInPerson.FamilyMember = false;
                        }

                        checkInPerson.Selected = true;
                        family.People.Add( checkInPerson );
                        ProcessFamily();
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
            // Make sure all required fields are filled out
            //Page.Validate( "Family" );
            //if ( !Page.IsValid )
            //{
            //    return;
            //}

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

                if ( currentPage.HasValue )
                {
                    pageOffset = (int)currentPage * lvNewFamily.DataKeys.Count;
                }

                newFamilyList[pageOffset + personOffset] = newPerson;
                personOffset++;
            }

            List<Person> newPeople = CreatePeople( newFamilyList );

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
                DisplayFamily();
                ProcessFamily();
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
            var newFamilyList = new List<SerializedPerson>();
            int currentPage = e.StartRowIndex / e.MaximumRows;
            int? previousPage = ViewState["currentPage"] as int?;
            int personOffset = 0;
            int pageOffset = 0;
            if ( ViewState["newFamily"] != null )
            {
                newFamilyList = (List<SerializedPerson>)ViewState["newFamily"];
            }

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
        /// Loads the person fields.
        /// </summary>
        private void LoadPersonFields()
        {
            tbFirstNamePerson.Text = string.Empty;
            tbLastNamePerson.Text = string.Empty;
            ddlSuffix.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_SUFFIX ) ), true );
            ddlSuffix.SelectedIndex = 0;
            ddlGenderPerson.BindToEnum<Gender>();
            ddlGenderPerson.SelectedIndex = 0;
            ddlAbilityPerson.LoadAbilityAndGradeItems();
            ddlAbilityPerson.SelectedIndex = 0;
            rGridPersonResults.Visible = false;
            lbNewPerson.Visible = false;

            //tbFirstNamePerson.Required = true;
            //tbLastNamePerson.Required = true;
            //ddlGenderPerson.Required = true;
        }

        /// <summary>
        /// Binds the person search results grid on the New Person/Visitor screen.
        /// </summary>
        private void BindPersonGrid()
        {
            var personService = new PersonService( new RockContext() );
            var people = personService.Queryable();

            var firstNameIsEmpty = string.IsNullOrEmpty( tbFirstNamePerson.Text );
            var lastNameIsEmpty = string.IsNullOrEmpty( tbLastNamePerson.Text );
            if ( !firstNameIsEmpty && !lastNameIsEmpty )
            {
                people = personService.GetByFullName( string.Format( "{0} {1}", tbFirstNamePerson.Text, tbLastNamePerson.Text ), false );
            }
            else if ( !lastNameIsEmpty )
            {
                people = people.Where( p => p.LastName.ToLower().StartsWith( tbLastNamePerson.Text ) );
            }
            else if ( !firstNameIsEmpty )
            {
                people = people.Where( p => p.FirstName.ToLower().StartsWith( tbFirstNamePerson.Text ) );
            }

            if ( ddlSuffix.SelectedValueAsInt().HasValue )
            {
                var suffixValueId = ddlSuffix.SelectedValueAsId();
                people = people.Where( p => p.SuffixValueId == suffixValueId );
            }

            if ( !string.IsNullOrEmpty( dpDOBPerson.Text ) )
            {
                DateTime searchDate;
                if ( DateTime.TryParse( dpDOBPerson.Text, out searchDate ) )
                {
                    people = people.Where( p => p.BirthYear == searchDate.Year
                        && p.BirthMonth == searchDate.Month && p.BirthDay == searchDate.Day );
                }
            }

            if ( ddlGenderPerson.SelectedValueAsEnum<Gender>() != 0 )
            {
                var gender = ddlGenderPerson.SelectedValueAsEnum<Gender>();
                people = people.Where( p => p.Gender == gender );
            }

            // Get the list of people so we can filter by grade and ability level
            var peopleList = people.OrderBy( p => p.LastName ).ThenBy( p => p.FirstName ).ToList();

            // Load abilities so we can see them in the result grid
            var abilityLevelValues = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_ABILITY_LEVEL_TYPE ) ).DefinedValues;
            peopleList.ForEach( p => p.LoadAttributes() );

            // Set a filter if an ability/grade was selected
            var optionGroup = ddlAbilityPerson.SelectedItem.Attributes["optiongroup"];
            if ( !string.IsNullOrEmpty( optionGroup ) )
            {
                if ( optionGroup.Equals( "Ability" ) )
                {
                    peopleList = peopleList.Where( p => p.Attributes.ContainsKey( "AbilityLevel" )
                        && p.GetAttributeValue( "AbilityLevel" ) == ddlAbilityPerson.SelectedValue ).ToList();
                }
                else if ( optionGroup.Equals( "Grade" ) )
                {
                    var grade = ddlAbilityPerson.SelectedValueAsId();
                    peopleList = peopleList.Where( p => p.GradeOffset == (int?)grade ).ToList();
                }
            }

            // Load person grid
            var matchingPeople = peopleList.Select( p => new
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
                        .Equals( p.GetAttributeValue( "AbilityLevel" ), StringComparison.OrdinalIgnoreCase ) )
                        .Select( dv => dv.Value ).FirstOrDefault()
            } ).OrderByDescending( p => p.BirthDate ).ToList();

            rGridPersonResults.DataSource = matchingPeople;
            rGridPersonResults.DataBind();
        }

        /// <summary>
        /// Processes the family.
        /// </summary>
        private void ProcessFamily( bool processWorkflow = true )
        {
            var errors = new List<string>();
            if ( ProcessActivity( "Person Search", out errors ) )
            {
                IEnumerable<CheckInPerson> memberDataSource = null;
                IEnumerable<CheckInPerson> visitorDataSource = null;

                var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    if ( family.People.Any( f => !f.ExcludedByFilter ) )
                    {
                        if ( family.People.Where( f => f.FamilyMember ).Any() )
                        {
                            var familyMembers = family.People.Where( f => f.FamilyMember && !f.ExcludedByFilter ).ToList();
                            hfSelectedPerson.Value = string.Join( ",", familyMembers.Select( f => f.Person.Id ).ToList() ) + ",";
                            familyMembers.ForEach( p => p.Selected = true );
                            memberDataSource = familyMembers.OrderBy( p => p.Person.FullNameReversed ).ToList();
                        }

                        if ( family.People.Where( f => !f.FamilyMember ).Any() )
                        {
                            var familyVisitors = family.People.Where( f => !f.FamilyMember && !f.ExcludedByFilter ).ToList();
                            visitorDataSource = familyVisitors.OrderBy( p => p.Person.FullNameReversed ).ToList();
                            if ( familyVisitors.Any( f => f.Selected ) )
                            {
                                hfSelectedVisitor.Value = string.Join( ",", familyVisitors.Where( f => f.Selected )
                                    .Select( f => f.Person.Id ).ToList() ) + ",";
                            }
                        }
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
                maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Creates the people.
        /// </summary>
        /// <param name="serializedPeople">The new people list.</param>
        /// <returns></returns>
        private List<Person> CreatePeople( List<SerializedPerson> serializedPeople )
        {
            var newPeopleList = new List<Person>();
            if ( serializedPeople.Any( p => p.IsValid() ) )
            {
                var rockContext = new RockContext();
                var personService = new PersonService( rockContext );
                var connectionStatus = DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS ) );
                var statusAttendee = connectionStatus.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_ATTENDEE ) ) );

                foreach ( SerializedPerson np in serializedPeople.Where( p => p.IsValid() ) )
                {
                    bool hasAbilityOrGradeValue = !string.IsNullOrWhiteSpace( np.Ability );

                    var person = new Person();
                    person.FirstName = np.FirstName;
                    person.LastName = np.LastName;
                    person.SuffixValueId = np.SuffixValueId;
                    person.Gender = (Gender)np.Gender;

                    if ( np.BirthDate != null )
                    {
                        person.BirthDay = ( (DateTime)np.BirthDate ).Day;
                        person.BirthMonth = ( (DateTime)np.BirthDate ).Month;
                        person.BirthYear = ( (DateTime)np.BirthDate ).Year;
                    }

                    if ( statusAttendee != null )
                    {
                        person.ConnectionStatusValueId = statusAttendee.Id;
                    }

                    if ( hasAbilityOrGradeValue && np.AbilityGroup == "Grade" )
                    {
                        person.GradeOffset = np.Ability.AsIntegerOrNull();
                    }

                    // Add the person so we can assign an ability (if set)
                    personService.Add( person );

                    if ( hasAbilityOrGradeValue && np.AbilityGroup == "Ability" )
                    {
                        person.LoadAttributes( rockContext );
                        person.SetAttributeValue( "AbilityLevel", np.Ability );
                        person.SaveAttributeValues( rockContext );
                    }

                    newPeopleList.Add( person );
                }

                rockContext.SaveChanges();

                // After save, each person should have an alias record pointing to the original person
                foreach ( var person in newPeopleList )
                {
                    if ( !person.Aliases.Any() )
                    {
                        person.Aliases.Add( new PersonAlias { AliasPersonId = person.Id, AliasPersonGuid = person.Guid } );
                    }
                }

                rockContext.SaveChanges();
            }

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
                familyGroup.IsActive = true;

                // Get oldest person's last name
                var familyName = newPeople.Where( p => p.BirthDate.HasValue )
                    .OrderByDescending( p => p.BirthDate )
                    .Select( p => p.LastName ).FirstOrDefault();

                familyGroup.Name = familyName + " Family";
                new GroupService( rockContext ).Add( familyGroup );
            }

            // Add group members
            var newGroupMembers = new List<GroupMember>( 0 );
            foreach ( var person in newPeople )
            {
                var groupMember = new GroupMember();
                groupMember.IsSystem = false;
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
        private void AddVisitorRelationships( CheckInFamily family, int visitorId )
        {
            var rockContext = new RockContext();
            foreach ( var familyMember in family.People.Where( p => p.FamilyMember && p.Person.Age >= 18 ) )
            {
                Person.CreateCheckinRelationship( familyMember.Person.Id, visitorId, CurrentPersonAlias, rockContext );
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

            public bool IsValid()
            {
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
            }
        }

        #endregion NewPerson Class
    }
}