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
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.HtmlControls;
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
            else if ( !Page.IsPostBack )
            {
                if ( CurrentCheckInState.CheckIn.Families.Count > 0 )
                {
                    var kioskLocationId = CurrentCheckInState.Kiosk.Device.Locations;
                    //var currentCampusId = CampusCache.All()
                    //    .Where( c => c.LocationId.HasValue && kioskLocationId == c.LocationId )
                    //    .Select( c => c.Id ).FirstOrDefault();

                    // Order families by campus then by caption
                    var familyList = CurrentCheckInState.CheckIn.Families//.OrderByDescending( f => f.Group.CampusId == currentCampusId )
                        .OrderBy( f => f.Caption ).ToList();
                    if ( !UserBackedUp )
                    {
                        familyList.FirstOrDefault().Selected = true;
                    }

                    ProcessFamily();
                    lvFamily.DataSource = familyList;
                    lvFamily.DataBind();
                    //lblFamilyTitle.InnerText = string.Format( "Results for \"{0}\"", CurrentCheckInState.CheckIn.SearchValue );
                }
                else
                {
                    //lblFamilyTitle.InnerText = string.Format( "No Results for \"{0}\"", CurrentCheckInState.CheckIn.SearchValue );
                    lbNext.Enabled = false;
                    lbNext.Visible = false;
                    pnlFamily.Visible = false;
                    pnlPerson.Visible = false;
                    pnlVisitor.Visible = false;
                    actions.Visible = false;

                    string nothingFoundText = GetAttributeValue( "NotFoundText" );
                    divNothingFound.InnerText = nothingFoundText;
                    divNothingFound.Visible = true;

                    bool showAddButtons = true;
                    bool.TryParse( GetAttributeValue( "EnableAddButtons" ), out showAddButtons );

                    lbAddFamilyMember.Visible = showAddButtons;
                    lbAddVisitor.Visible = showAddButtons;
                    lbNewFamily.Visible = showAddButtons;
                }

                rGridPersonResults.PageSize = 4;
            }
        }

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

            // Successful family checkin
            if ( selectedPeopleIds.Count() > 0 )
            {
                family.People.ForEach( p => p.Selected = selectedPeopleIds.Contains( p.Person.Id ) );

                ProcessSelection( maWarning );
            }
            else
            {
                maWarning.Show( "Please pick at least one person.", ModalAlertType.Warning );
                return;
            }
        }

        #endregion Control Methods

        #region Load Methods

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
                    lbSelectFamily.CommandArgument = family.Group.Id.ToString();
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
                    lbSelectPerson.CommandArgument = person.Person.Id.ToString();
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
                    lbSelectVisitor.CommandArgument = person.Person.Id.ToString();
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
            //var tbFirstName = (RockTextBox)e.Item.FindControl( "tbFirstName" );
            //var tbLastName = (RockTextBox)e.Item.FindControl( "tbLastName" );
            //var dpBirthDate = (DatePicker)e.Item.FindControl( "dpBirthDate" );
            var ddlGender = (RockDropDownList)e.Item.FindControl( "ddlGender" );
            ddlGender.BindToEnum<Gender>();
            ( (RockDropDownList)e.Item.FindControl( "ddlAbilityGrade" ) ).LoadAbilityAndGradeItems();
        }

        #endregion Load Methods

        #region Select People Events

        /// <summary>
        /// Handles the ItemCommand event of the lvFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewCommandEventArgs"/> instance containing the event data.</param>
        protected void lvFamily_ItemCommand( object sender, ListViewCommandEventArgs e )
        {
            int id = int.Parse( e.CommandArgument.ToString() );
            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Group.Id == id ).FirstOrDefault();

            foreach ( ListViewDataItem li in lvFamily.Items )
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
                //dpPersonPager.Visible = false;
                lvPerson.DataSource = null;
                lvPerson.DataBind();
                //dpVisitorPager.Visible = false;
                lvVisitor.DataSource = null;
                lvVisitor.DataBind();
                return;
            }

            if ( lvPerson.DataSource != null )
            {
                dpPersonPager.Visible = true;
                dpPersonPager.SetPageProperties( 0, dpPersonPager.MaximumRows, false );
            }

            if ( lvVisitor.DataSource != null )
            {
                dpVisitorPager.Visible = true;
                dpVisitorPager.SetPageProperties( 0, dpVisitorPager.MaximumRows, false );
            }
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
            lvFamily.DataSource = CurrentCheckInState.CheckIn.Families.OrderBy( f => f.Caption ).ToList();
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

        #endregion Select People Events

        #region Add People Events

        /// <summary>
        /// Handles the Click event of the lbAddVisitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddVisitor_Click( object sender, EventArgs e )
        {
            lblAddPersonHeader.Text = "Add Visitor";
            newPersonType.Value = "Visitor";
            SetAddPersonFields();
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
            SetAddPersonFields();
        }

        /// <summary>
        /// Handles the Click event of the lbNewPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNewPerson_Click( object sender, EventArgs e )
        {
            // Make sure all required fields are filled out
            if ( string.IsNullOrEmpty( tbFirstNamePerson.Text ) || string.IsNullOrEmpty( tbLastNamePerson.Text ) || string.IsNullOrEmpty( dpDOBPerson.Text ) || ddlGenderPerson.SelectedValueAsInt() == 0 )
            {
                Page.Validate( "Person" );
                mdlAddPerson.Show();
            }
            else
            {
                var checkInFamily = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( checkInFamily == null )
                {
                    checkInFamily = new CheckInFamily();
                    var familyGroup = CreateFamily( tbLastNamePerson.Text );

                    checkInFamily.Group = familyGroup;
                    checkInFamily.Caption = familyGroup.Name;
                }

                var checkInPerson = new CheckInPerson();
                checkInPerson.Person = CreatePerson( tbFirstNamePerson.Text, tbLastNamePerson.Text, dpDOBPerson.SelectedDate, (int?)ddlGenderPerson.SelectedValueAsEnum<Gender>(),
                    ddlAbilityPerson.SelectedValue, ddlAbilityPerson.SelectedItem.Attributes["optiongroup"] );

                if ( newPersonType.Value != "Visitor" )
                {   // Family Member
                    var groupMember = AddGroupMember( checkInFamily.Group.Id, checkInPerson.Person );
                    hfSelectedPerson.Value += checkInPerson.Person.Id + ",";
                    checkInPerson.FamilyMember = true;
                }
                else
                {   // Visitor
                    AddVisitorGroupMemberRoles( checkInFamily, checkInPerson.Person.Id );
                    hfSelectedVisitor.Value += checkInPerson.Person.Id + ",";
                    checkInPerson.FamilyMember = false;
                }

                checkInPerson.Selected = true;
                checkInFamily.People.Add( checkInPerson );
                checkInFamily.SubCaption = string.Join( ",", checkInFamily.People.Select( p => p.Person.FirstName ) );
                checkInFamily.Selected = true;
                CurrentCheckInState.CheckIn.Families.Add( checkInFamily );

                tbFirstNamePerson.Required = false;
                tbLastNamePerson.Required = false;
                ddlGenderPerson.Required = false;
                dpDOBPerson.Required = false;

                ProcessFamily();
                mdlAddPerson.Hide();
            }
        }

        /// <summary>
        /// Handles the PagePropertiesChanging event of the lvNewFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PagePropertiesChangingEventArgs"/> instance containing the event data.</param>
        protected void lvNewFamily_PagePropertiesChanging( object sender, PagePropertiesChangingEventArgs e )
        {
            var newFamilyList = new List<NewPerson>();
            if ( ViewState["newFamily"] != null )
            {
                newFamilyList = (List<NewPerson>)ViewState["newFamily"];
                int personOffset = 0;
                foreach ( ListViewItem item in lvNewFamily.Items )
                {
                    var rowPerson = new NewPerson();
                    rowPerson.FirstName = ( (TextBox)item.FindControl( "tbFirstName" ) ).Text;
                    rowPerson.LastName = ( (TextBox)item.FindControl( "tbLastName" ) ).Text;
                    rowPerson.BirthDate = ( (DatePicker)item.FindControl( "dpBirthDate" ) ).SelectedDate;
                    rowPerson.Gender = ( (RockDropDownList)item.FindControl( "ddlGender" ) ).SelectedValueAsEnum<Gender>();
                    rowPerson.Ability = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedValue;
                    rowPerson.AbilityGroup = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedItem.Attributes["optiongroup"];
                    newFamilyList[System.Math.Abs( e.StartRowIndex - e.MaximumRows ) + personOffset] = rowPerson;
                    personOffset++;

                    // check if the list should be expanded
                    if ( e.MaximumRows + e.StartRowIndex + personOffset >= newFamilyList.Count )
                    {
                        newFamilyList.AddRange( Enumerable.Repeat( new NewPerson(), e.MaximumRows ) );
                    }
                }
            }

            ViewState["newFamily"] = newFamilyList;
            dpNewFamily.SetPageProperties( e.StartRowIndex, e.MaximumRows, false );
            lvNewFamily.DataSource = newFamilyList;
            lvNewFamily.DataBind();
            mdlNewFamily.Show();
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
            mdlAddPerson.Show();
        }

        /// <summary>
        /// Handles the Click event of the lbNewFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbNewFamily_Click( object sender, EventArgs e )
        {
            var newFamilyList = new List<NewPerson>();
            newFamilyList.AddRange( Enumerable.Repeat( new NewPerson(), 10 ) );
            ViewState["newFamily"] = newFamilyList;
            lvNewFamily.DataSource = newFamilyList;
            lvNewFamily.DataBind();

            mdlNewFamily.Show();
        }

        /// <summary>
        /// Handles the Click event of the lbSaveFamily control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSaveFamily_Click( object sender, EventArgs e )
        {
            var newFamilyList = (List<NewPerson>)ViewState["newFamily"];
            var checkInFamily = new CheckInFamily();
            CheckInPerson checkInPerson;
            NewPerson newPerson;

            // add the new people
            foreach ( ListViewItem item in lvNewFamily.Items )
            {
                newPerson = new NewPerson();
                newPerson.FirstName = ( (TextBox)item.FindControl( "tbFirstName" ) ).Text;
                newPerson.LastName = ( (TextBox)item.FindControl( "tbLastName" ) ).Text;
                newPerson.BirthDate = ( (DatePicker)item.FindControl( "dpBirthDate" ) ).SelectedDate;
                newPerson.Gender = ( (RockDropDownList)item.FindControl( "ddlGender" ) ).SelectedValueAsEnum<Gender>();
                newPerson.Ability = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedValue;
                newPerson.AbilityGroup = ( (RockDropDownList)item.FindControl( "ddlAbilityGrade" ) ).SelectedItem.Attributes["optiongroup"];
                newFamilyList.Add( newPerson );
            }

            var lastName = newFamilyList.Where( p => p.BirthDate.HasValue ).OrderByDescending( p => p.BirthDate ).Select( p => p.LastName ).FirstOrDefault();
            var familyGroup = CreateFamily( lastName );

            // create people and add to checkin
            foreach ( NewPerson np in newFamilyList.Where( np => np.IsValid() ) )
            {
                var person = CreatePerson( np.FirstName, np.LastName, np.BirthDate, (int?)np.Gender, np.Ability, np.AbilityGroup );
                var groupMember = AddGroupMember( familyGroup.Id, person );
                familyGroup.Members.Add( groupMember );
                checkInPerson = new CheckInPerson();
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

            ProcessFamily();
            RefreshFamily();
        }

        /// <summary>
        /// Handles the RowCommand event of the grdPersonSearchResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewCommandEventArgs"/> instance containing the event data.</param>
        protected void rGridPersonResults_AddExistingPerson( object sender, GridViewCommandEventArgs e )
        {
            if ( e.CommandName == "Add" )
            {
                var rockContext = new RockContext();
                var groupMemberService = new GroupMemberService( rockContext );
                int rowIndex = int.Parse( e.CommandArgument.ToString() );
                int personId = int.Parse( rGridPersonResults.DataKeys[rowIndex].Value.ToString() );

                var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    var checkInPerson = new CheckInPerson();
                    checkInPerson.Person = new PersonService( rockContext ).Get( personId ).Clone( false );
                    var isPersonInFamily = family.People.Any( p => p.Person.Id == checkInPerson.Person.Id );
                    if ( !isPersonInFamily )
                    {
                        if ( newPersonType.Value != "Visitor" )
                        {
                            // Add as family member
                            var groupMember = groupMemberService.GetByPersonId( personId ).FirstOrDefault( gm => gm.Group.GroupType.Guid == new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ) );
                            if ( groupMember != null )
                            {
                                groupMember.GroupId = family.Group.Id;
                                rockContext.SaveChanges();
                            }

                            checkInPerson.FamilyMember = true;
                            hfSelectedPerson.Value += personId + ",";
                        }
                        else
                        {
                            // Add as visitor
                            AddVisitorGroupMemberRoles( family, personId );
                            checkInPerson.FamilyMember = false;
                            hfSelectedVisitor.Value += personId + ",";
                        }

                        checkInPerson.Selected = true;
                        family.People.Add( checkInPerson );
                        ProcessFamily();
                    }

                    mdlAddPerson.Hide();
                }
                else
                {
                    string errorMsg = "<ul><li>You have to pick or create a family to add this person to.</li></ul>";
                    maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
                }
            }
            else
            {
                mdlAddPerson.Show();
                BindPersonGrid();
            }
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

        #endregion Add People Events

        #region Internal Methods

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
                    var grade = ddlAbilityPerson.SelectedValueAsEnum<GradeLevel>();
                    peopleList = peopleList.Where( p => p.Grade == (int?)grade ).ToList();
                }
            }

            // Load person grid
            var matchingPeople = peopleList.Select( p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.BirthDate,
                p.Age,
                p.Gender,
                Attribute = p.Grade.HasValue
                    ? ( (GradeLevel)p.Grade ).GetDescription()
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
        private void ProcessFamily()
        {
            var errors = new List<string>();
            if ( ProcessActivity( "Person Search", out errors ) )
            {
                var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    IEnumerable<CheckInPerson> memberDataSource = null;
                    IEnumerable<CheckInPerson> visitorDataSource = null;
                    if ( family.People.Any() )
                    {
                        if ( family.People.Where( f => f.FamilyMember ).Any() )
                        {
                            var familyMembers = family.People.Where( f => f.FamilyMember && !f.ExcludedByFilter ).ToList();
                            hfSelectedPerson.Value = string.Join( ",", familyMembers.Select( f => f.Person.Id ) ) + ",";
                            familyMembers.ForEach( p => p.Selected = true );
                            memberDataSource = familyMembers.OrderBy( p => p.Person.FullNameReversed ).ToList();
                        }

                        if ( family.People.Where( f => !f.FamilyMember ).Any() )
                        {
                            var familyVisitors = family.People.Where( f => !f.FamilyMember && !f.ExcludedByFilter ).ToList();
                            visitorDataSource = familyVisitors.OrderBy( p => p.Person.FullNameReversed ).ToList();
                        }
                    }

                    lvPerson.DataSource = memberDataSource;
                    lvPerson.DataBind();
                    pnlPerson.Update();
                    lvVisitor.DataSource = visitorDataSource;
                    lvVisitor.DataBind();
                    pnlVisitor.Update();
                }
            }
            else
            {
                string errorMsg = "<ul><li>" + errors.AsDelimited( "</li><li>" ) + "</li></ul>";
                maWarning.Show( errorMsg, Rock.Web.UI.Controls.ModalAlertType.Warning );
            }
        }

        /// <summary>
        /// Refreshes the family.
        /// </summary>
        protected void RefreshFamily()
        {
            // Sort by campus first
            lvFamily.DataSource = CurrentCheckInState.CheckIn.Families.OrderBy( f => f.Caption ).ToList();
            lvFamily.DataBind();
            pnlFamily.Update();

            if ( divNothingFound.Visible )
            {
                lblFamilyTitle.InnerText = "Search Results";
                lbNext.Enabled = true;
                lbNext.Visible = true;
                pnlFamily.Visible = true;
                pnlPerson.Visible = true;
                pnlVisitor.Visible = true;
                actions.Visible = true;
                divNothingFound.Visible = false;
            }
        }

        /// <summary>
        /// Sets the add person fields.
        /// </summary>
        protected void SetAddPersonFields()
        {
            ddlGenderPerson.BindToEnum<Gender>();
            ddlGenderPerson.SelectedIndex = 0;
            ddlAbilityPerson.LoadAbilityAndGradeItems();
            ddlAbilityPerson.SelectedIndex = 0;
            rGridPersonResults.Visible = false;
            lbNewPerson.Visible = false;

            tbFirstNamePerson.Required = true;
            tbLastNamePerson.Required = true;
            ddlGenderPerson.Required = true;

            mdlAddPerson.Show();
        }

        /// <summary>
        /// Adds a new person.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="DOB">The DOB.</param>
        /// <param name="gender">The gender</param>
        /// <param name="attribute">The attribute.</param>
        protected Person CreatePerson( string firstName, string lastName, DateTime? DOB, int? gender, string ability, string abilityGroup )
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            var person = new Person();
            person.FirstName = firstName;
            person.LastName = lastName;
            person.BirthDate = DOB;
            personService.Add( person );

            if ( gender != null )
            {
                person.Gender = (Gender)gender;
            }

            if ( !string.IsNullOrWhiteSpace( ability ) && abilityGroup == "Grade" )
            {
                person.Grade = (int?)ability.ConvertToEnum<GradeLevel>();
            }

            rockContext.SaveChanges();

            // Every person should have an alias record pointing to the original person
            if ( person.Aliases.Count < 1 )
            {
                person.Aliases.Add( new PersonAlias { AliasPersonId = person.Id, AliasPersonGuid = person.Guid } );
            }

            if ( !string.IsNullOrWhiteSpace( ability ) && abilityGroup == "Ability" )
            {
                person.LoadAttributes( rockContext );
                person.SetAttributeValue( "AbilityLevel", ability );
                person.SaveAttributeValues( rockContext );
            }

            return person;
        }

        /// <summary>
        /// Creates the family.
        /// </summary>
        /// <param name="FamilyName">Name of the family.</param>
        /// <returns></returns>
        protected Group CreateFamily( string FamilyName )
        {
            var familyGroup = new Group();
            familyGroup.Name = FamilyName + " Family";
            familyGroup.GroupTypeId = GroupTypeCache.GetFamilyGroupType().Id;
            familyGroup.IsSecurityRole = false;
            familyGroup.IsSystem = false;
            familyGroup.IsActive = true;

            var rockContext = new RockContext();
            new GroupService( rockContext ).Add( familyGroup );
            rockContext.SaveChanges();

            return familyGroup;
        }

        /// <summary>
        /// Adds the group member.
        /// </summary>
        /// <param name="familyGroup">The family group.</param>
        /// <param name="person">The person.</param>
        /// <returns></returns>
        protected GroupMember AddGroupMember( int familyGroupId, Person person )
        {
            var rockContext = new RockContext();
            var familyGroupType = GroupTypeCache.GetFamilyGroupType();

            var groupMember = new GroupMember();
            groupMember.IsSystem = false;
            groupMember.GroupId = familyGroupId;
            groupMember.PersonId = person.Id;
            if ( person.Age >= 18 )
            {
                groupMember.GroupRoleId = familyGroupType.Roles.FirstOrDefault( r => r.Guid == new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT ) ).Id;
            }
            else
            {
                groupMember.GroupRoleId = familyGroupType.Roles.FirstOrDefault( r => r.Guid == new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD ) ).Id;
            }

            new GroupMemberService( rockContext ).Add( groupMember );
            rockContext.SaveChanges();

            return groupMember;
        }

        /// <summary>
        /// Adds the visitor group member roles.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="personId">The person id.</param>
        protected void AddVisitorGroupMemberRoles( CheckInFamily family, int personId )
        {
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            var groupMemberService = new GroupMemberService( rockContext );

            var knownRelationshipGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS );
            var ownerRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER ) );
            var canCheckIn = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_CAN_CHECK_IN ) );

            foreach ( var familyMember in family.People )
            {
                if ( familyMember.FamilyMember )
                {
                    var group = groupMemberService.Queryable()
                    .Where( m =>
                        m.PersonId == familyMember.Person.Id &&
                        m.GroupRoleId == ownerRole.Id )
                    .Select( m => m.Group )
                    .FirstOrDefault();

                    if ( group == null && ownerRole != null )
                    {
                        var groupMember = new GroupMember();
                        groupMember.PersonId = familyMember.Person.Id;
                        groupMember.GroupRoleId = ownerRole.Id;

                        group = new Group();
                        group.Name = knownRelationshipGroupType.Name;
                        group.GroupTypeId = knownRelationshipGroupType.Id;
                        group.Members.Add( groupMember );

                        groupService.Add( group );
                    }

                    // add the visitor to this group with CanCheckIn
                    Person.CreateCheckinRelationship( familyMember.Person.Id, personId, CurrentPersonAlias );
                }
            }

            rockContext.SaveChanges();
        }

        #endregion Internal Methods

        #region NewPerson Class

        /// <summary>
        /// Lightweight Person model to quickly add people during Check-in
        /// </summary>
        [Serializable()]
        protected class NewPerson
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public DateTime? BirthDate { get; set; }

            public Gender Gender { get; set; }

            public string Ability { get; set; }

            public string AbilityGroup { get; set; }

            public bool IsValid()
            {
                return !( string.IsNullOrWhiteSpace( FirstName ) || string.IsNullOrWhiteSpace( LastName )
                    || !BirthDate.HasValue || Gender == Gender.Unknown );
            }

            public NewPerson()
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