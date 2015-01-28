<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FamilySelect.ascx.cs" Inherits="cc.newspring.AttendedCheckin.FamilySelect" %>

<asp:UpdatePanel ID="pnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <asp:Panel ID="pnlSelections" runat="server" CssClass="attended">

            <Rock:ModalAlert ID="maWarning" runat="server" />
            <asp:HiddenField ID="newPersonType" runat="server" />

            <div class="row checkin-header">
                <div class="col-xs-2 checkin-actions">
                    <Rock:BootstrapButton ID="lbBack" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbBack_Click" EnableViewState="false" CausesValidation="False">
                        <span class="fa fa-arrow-left" />
                    </Rock:BootstrapButton>
                </div>

                <div class="col-xs-8 text-center">
                    <h1 id="lblFamilyTitle" runat="server">Search Results</h1>
                </div>

                <div class="col-xs-2 checkin-actions text-right">
                    <Rock:BootstrapButton ID="lbNext" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbNext_Click" EnableViewState="false" CausesValidation="False">
                        <span class="fa fa-arrow-right" />
                    </Rock:BootstrapButton>
                </div>
            </div>

            <div class="row checkin-body">
                <div class="col-xs-3">
                    <asp:UpdatePanel ID="pnlFamily" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>

                            <h3 class="text-center">Families</h3>

                            <asp:ListView ID="lvFamily" runat="server" OnPagePropertiesChanging="lvFamily_PagePropertiesChanging"
                                OnItemCommand="lvFamily_ItemCommand" OnItemDataBound="lvFamily_ItemDataBound">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbSelectFamily" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" CausesValidation="false">
						                <%# Eval("Caption") %><br />
                                        <span class='checkin-sub-title'>
                                            <%# Eval("SubCaption") %>
                                        </span>
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:ListView>
                            <asp:DataPager ID="dpFamilyPager" runat="server" PageSize="4" PagedControlID="lvFamily">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-lg btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div class="col-xs-3">
                    <asp:UpdatePanel ID="pnlPerson" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <asp:HiddenField ID="hfSelectedPerson" runat="server" ClientIDMode="Static" />

                            <h3 class="text-center">People</h3>

                            <asp:ListView ID="lvPerson" runat="server" OnItemDataBound="lvPerson_ItemDataBound" OnPagePropertiesChanging="lvPerson_PagePropertiesChanging">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbSelectPerson" runat="server" data-id='<%# Eval("Person.Id") %>' CssClass="btn btn-primary btn-lg btn-block btn-checkin-select person">
						                <%# Eval("Person.FullName") %><br />
						                <span class='checkin-sub-title'>
							                Birthday: <%# Eval("Person.BirthMonth") + "/" + Eval("Person.BirthDay") + " " ?? "N/A " %>
                                            <%# Convert.ToInt32( Eval( "Person.Age" ) ) <= 18 ? "Age: " + Eval( "Person.Age" ) : string.Empty %>
						                </span>
                                    </asp:LinkButton>
                                </ItemTemplate>
                                <EmptyDataTemplate>
                                    <div class="text-center large-font">
                                        <asp:Literal ID="lblPersonTitle" runat="server" Text="No one in this family is eligible to check-in." />
                                    </div>
                                </EmptyDataTemplate>
                            </asp:ListView>
                            <asp:DataPager ID="dpPersonPager" runat="server" PageSize="4" PagedControlID="lvPerson">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-lg btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div class="col-xs-3">
                    <asp:UpdatePanel ID="pnlVisitor" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <asp:HiddenField ID="hfSelectedVisitor" runat="server" ClientIDMode="Static" />

                            <h3 class="text-center">Visitors</h3>

                            <asp:ListView ID="lvVisitor" runat="server" OnItemDataBound="lvVisitor_ItemDataBound" OnPagePropertiesChanging="lvVisitor_PagePropertiesChanging">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbSelectVisitor" runat="server" data-id='<%# Eval("Person.Id") %>' CssClass="btn btn-primary btn-lg btn-block btn-checkin-select visitor">
						                <%# Eval("Person.FullName") %><br />
						                <span class='checkin-sub-title'>Birthday: <%# Eval("Person.BirthMonth") + "/" + Eval("Person.BirthDay") ?? "N/A" %></span>
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:ListView>
                            <asp:DataPager ID="dpVisitorPager" runat="server" PageSize="4" PagedControlID="lvVisitor">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-lg btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div id="divNothingFound" runat="server" class="col-xs-9" visible="false">
                    <div class="col-xs-3"></div>
                    <div class="col-xs-9 nothing-eligible">
                        <asp:Literal ID="lblNothingFound" runat="server" EnableViewState="false" />
                    </div>
                </div>

                <div id="divActions" runat="server" class="col-xs-3">
                    <h3 id="actions" runat="server" class="text-center">Actions</h3>

                    <asp:LinkButton ID="lbAddVisitor" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" OnClick="lbAddVisitor_Click" Text="Add Visitor" CausesValidation="false" EnableViewState="false" />
                    <asp:LinkButton ID="lbAddFamilyMember" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" OnClick="lbAddFamilyMember_Click" Text="Add Family Member" CausesValidation="false" EnableViewState="false" />
                    <asp:LinkButton ID="lbNewFamily" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" OnClick="lbNewFamily_Click" Text="New Family" CausesValidation="false" EnableViewState="false" />
                </div>
            </div>
        </asp:Panel>

        <Rock:ModalDialog ID="mdlAddPerson" runat="server" Content-DefaultButton="lbPersonSearch">
            <Content>
                <div class="row checkin-header">
                    <div class="checkin-actions">
                        <div class="col-xs-3">
                            <Rock:BootstrapButton ID="lbClosePerson" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbClosePerson_Click" Text="Cancel" EnableViewState="false" />
                        </div>

                        <div class="col-xs-6">
                            <h2 class="text-center">
                                <asp:Literal ID="lblAddPersonHeader" runat="server" /></h2>
                        </div>

                        <div class="col-xs-3 text-right">
                            <Rock:BootstrapButton ID="lbPersonSearch" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbPersonSearch_Click" Text="Search" EnableViewState="false" />
                        </div>
                    </div>
                </div>

                <div class="checkin-body">
                    <div class="row">
                        <div class="col-xs-2">
                            <Rock:RockTextBox ID="tbFirstNamePerson" runat="server" CssClass="col-xs-12" Label="First Name" ValidationGroup="Person" />
                        </div>
                        <div class="col-xs-2">
                            <Rock:RockTextBox ID="tbLastNamePerson" runat="server" CssClass="col-xs-12" Label="Last Name" ValidationGroup="Person" />
                        </div>
                        <div class="col-xs-3">
                            <Rock:DatePicker ID="dpDOBPerson" runat="server" Label="DOB" CssClass="col-xs-12 date-picker" ValidationGroup="Person" />
                        </div>
                        <div class="col-xs-2">
                            <Rock:RockDropDownList ID="ddlGenderPerson" runat="server" Label="Gender" CssClass="col-xs-12" ValidationGroup="Person" />
                        </div>
                        <div class="col-xs-3">
                            <Rock:RockDropDownList ID="ddlAbilityPerson" runat="server" Label="Ability/Grade" CssClass="col-xs-12" />
                        </div>
                    </div>

                    <div class="row">
                        <asp:UpdatePanel ID="pnlPersonSearch" runat="server">
                            <ContentTemplate>
                                <div class="grid">
                                    <Rock:Grid ID="rGridPersonResults" runat="server" OnRowCommand="rGridPersonResults_AddExistingPerson" EnableResponsiveTable="false"
                                        OnGridRebind="rGridPersonResults_GridRebind" ShowActionRow="false" PageSize="4" DataKeyNames="Id" AllowSorting="true">
                                        <Columns>
                                            <asp:BoundField DataField="Id" Visible="false" />
                                            <asp:BoundField DataField="FirstName" HeaderText="First Name" SortExpression="FirstName" />
                                            <asp:BoundField DataField="LastName" HeaderText="Last Name" SortExpression="LastName" />
                                            <asp:BoundField DataField="BirthDate" HeaderText="DOB" SortExpression="BirthDate" DataFormatString="{0:MM/dd/yy}" HtmlEncode="false" />
                                            <asp:BoundField DataField="Age" HeaderText="Age" SortExpression="Age" />
                                            <asp:BoundField DataField="Gender" HeaderText="Gender" SortExpression="Gender" />
                                            <asp:BoundField DataField="Attribute" HeaderText="Ability/Grade" SortExpression="Attribute" />
                                            <asp:TemplateField>
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="lbAdd" runat="server" CssClass="btn btn-lg btn-primary" CommandName="Add"
                                                        Text="Add" CommandArgument="<%# ((GridViewRow) Container).RowIndex %>" CausesValidation="false"><i class="fa fa-plus"></i>
                                                    </asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </Rock:Grid>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>

                    <div class="row">
                        <div class="col-xs-12 text-right">
                            <asp:LinkButton ID="lbNewPerson" runat="server" Text="None of these, add a new person" CssClass="btn btn-lg btn-primary btn-checkin-select" ValidationGroup="Person" CausesValidation="true" OnClick="lbNewPerson_Click" />
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mdlNewFamily" runat="server" Content-DefaultButton="lbSaveFamily">
            <Content>
                <div class="row checkin-header">
                    <div class="col-xs-3 checkin-actions">
                        <Rock:BootstrapButton ID="lbCloseFamily" runat="server" Text="Cancel" CssClass="btn btn-lg btn-primary" OnClick="lbCloseFamily_Click" EnableViewState="false" />
                    </div>

                    <div class="col-xs-6 text-center">
                        <h2>New Family</h2>
                    </div>

                    <div class="col-xs-3 checkin-actions text-right">
                        <Rock:BootstrapButton ID="lbSaveFamily" CssClass="btn btn-lg btn-primary" runat="server" Text="Save" OnClick="lbSaveFamily_Click" ValidationGroup="Family" EnableViewState="false" />
                    </div>
                </div>

                <div class="checkin-body">
                    <asp:ListView ID="lvNewFamily" runat="server" OnPagePropertiesChanging="lvNewFamily_PagePropertiesChanging" OnItemDataBound="lvNewFamily_ItemDataBound">
                        <LayoutTemplate>
                            <div class="row">
                                <div class="col-xs-2">
                                    <h4>First Name</h4>
                                </div>
                                <div class="col-xs-2">
                                    <h4>Last Name</h4>
                                </div>
                                <div class="col-xs-3">
                                    <h4>DOB</h4>
                                </div>
                                <div class="col-xs-2">
                                    <h4>Gender</h4>
                                </div>
                                <div class="col-xs-3">
                                    <h4>Ability/Grade</h4>
                                </div>
                            </div>
                            <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                        </LayoutTemplate>
                        <ItemTemplate>
                            <div class="row">
                                <div class="col-xs-2">
                                    <Rock:RockTextBox ID="tbFirstName" runat="server" RequiredErrorMessage="First Name is Required" Text='<%# ((NewPerson)Container.DataItem).FirstName %>' ValidationGroup="Family" />
                                </div>
                                <div class="col-xs-2">
                                    <Rock:RockTextBox ID="tbLastName" runat="server" RequiredErrorMessage="Last Name is Required" Text='<%# ((NewPerson)Container.DataItem).LastName %>' ValidationGroup="Family" />
                                </div>
                                <div class="col-xs-3">
                                    <Rock:DatePicker ID="dpBirthDate" runat="server" RequiredErrorMessage="DOB is Required" SelectedDate='<%# ((NewPerson)Container.DataItem).BirthDate %>' ValidationGroup="Family" CssClass="date-picker" />
                                </div>
                                <div class="col-xs-2">
                                    <Rock:RockDropDownList ID="ddlGender" runat="server" RequiredErrorMessage="Gender is Required" ValidationGroup="Family" />
                                </div>
                                <div class="col-xs-3">
                                    <Rock:RockDropDownList ID="ddlAbilityGrade" runat="server" />
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:ListView>

                    <div class="row">
                        <div class="col-xs-offset-9 col-xs-3 text-right">
                            <asp:DataPager ID="dpNewFamily" runat="server" PageSize="4" PagedControlID="lvNewFamily">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-lg btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript" src="../plugins/cc_newspring/attendedcheckin/scripts.js"></script>

<script type="text/javascript">

    var setControlEvents = function () {

        $('.modal:visible').css('z-index', Number($('.modal-backdrop').css('z-index')) + 1);

        $('.person').unbind('click').on('click', function () {
            $(this).toggleClass('active');
            var selectedIds = $('#hfSelectedPerson').val();
            var buttonId = this.getAttribute('data-id') + ',';
            if (typeof selectedIds == "string" && (selectedIds.indexOf(buttonId) >= 0)) {
                $('#hfSelectedPerson').val(selectedIds.replace(buttonId, ''));
            } else {
                $('#hfSelectedPerson').val(buttonId + selectedIds);
            }
            return false;
        });

        $('.visitor').unbind('click').on('click', function () {
            $(this).toggleClass('active');
            var selectedIds = $('#hfSelectedVisitor').val();
            var buttonId = this.getAttribute('data-id') + ',';
            if (typeof selectedIds == "string" && (selectedIds.indexOf(buttonId) >= 0)) {
                $('#hfSelectedVisitor').val(selectedIds.replace(buttonId, ''));
            } else {
                $('#hfSelectedVisitor').val(buttonId + selectedIds);
            }
            return false;
        });

        $('#<%= pnlFamily.ClientID %>').on('click', 'a', function () {
            $('.nothing-eligible').html("<i class='fa fa-refresh fa-spin fa-2x'></i>");
        });
    };

    $(document).ready(function () {
        setControlEvents();
    });
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(setControlEvents);
</script>