<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FamilySelect.ascx.cs" Inherits="RockWeb.Plugins.cc_newspring.AttendedCheckin.FamilySelect" %>

<script type="text/javascript" src="../plugins/cc_newspring/attendedcheckin/scripts.js"></script>

<asp:UpdatePanel ID="pnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <asp:Panel ID="pnlSelections" runat="server" CssClass="attended">

            <asp:HiddenField ID="newPersonType" runat="server" />

            <div class="row checkin-header">
                <div class="col-xs-2 checkin-actions">
                    <Rock:BootstrapButton ID="lbBack" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbBack_Click" EnableViewState="false">
                        <span class="fa fa-arrow-left" />
                    </Rock:BootstrapButton>
                </div>

                <div class="col-xs-8 text-center">
                    <h1 id="lblFamilyTitle" runat="server">Search Results</h1>
                </div>

                <div class="col-xs-2 checkin-actions text-right">
                    <Rock:BootstrapButton ID="lbNext" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbNext_Click" EnableViewState="false">
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
                                    <asp:LinkButton ID="lbSelectFamily" runat="server" CausesValidation="false" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select family" />
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

                            <asp:ListView ID="lvPerson" runat="server" OnItemDataBound="lvPeople_ItemDataBound" OnPagePropertiesChanging="lvPerson_PagePropertiesChanging">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbSelectPerson" runat="server" data-id='<%# Eval("Person.Id") %>' CssClass="btn btn-primary btn-lg btn-block btn-checkin-select person" />
                                </ItemTemplate>
                                <EmptyDataTemplate>
                                    <div class="text-center large-font">
                                        <asp:Literal ID="lblPersonTitle" runat="server" Text="No family member(s) are eligible for check-in." />
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

                            <asp:ListView ID="lvVisitor" runat="server" OnItemDataBound="lvPeople_ItemDataBound" OnPagePropertiesChanging="lvVisitor_PagePropertiesChanging">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbSelectPerson" runat="server" data-id='<%# Eval("Person.Id") %>' CssClass="btn btn-primary btn-lg btn-block btn-checkin-select visitor" />
                                </ItemTemplate>
                                <EmptyDataTemplate>
                                    <div class="text-center large-font">
                                        <asp:Literal ID="lblPersonTitle" runat="server" Text="No visitor(s) are eligible for check-in." />
                                    </div>
                                </EmptyDataTemplate>
                            </asp:ListView>
                            <asp:DataPager ID="dpVisitorPager" runat="server" PageSize="4" PagedControlID="lvVisitor">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-lg btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <!-- Nothing Found State -->
                <h3 id="divNothingFound" runat="server" class="col-xs-9 centered" visible="false">
                    <asp:Literal ID="lblNothingFound" runat="server" EnableViewState="false" />
                </h3>

                <!-- Add Buttons -->
                <div id="divActions" runat="server" class="col-xs-3">
                    <h3 id="actions" runat="server" class="text-center">Actions</h3>

                    <asp:LinkButton ID="lbAddVisitor" runat="server" Text="Add Visitor" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" OnClick="lbAddVisitor_Click" CausesValidation="false" EnableViewState="false" />
                    <asp:LinkButton ID="lbAddFamilyMember" runat="server" Text="Add Person" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" OnClick="lbAddFamilyMember_Click" CausesValidation="false" EnableViewState="false" />
                    <asp:LinkButton ID="lbNewFamily" runat="server" Text="New Family" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" OnClick="lbNewFamily_Click" CausesValidation="false" EnableViewState="false" />
                </div>
            </div>
        </asp:Panel>

        <!-- ADD PERSON MODAL -->
        <Rock:ModalDialog ID="mdlAddPerson" runat="server">
            <Content>
                <div class="soft-quarter-ends">
                    <!-- Modal Header -->
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

                    <!-- Modal Body -->
                    <div class="checkin-body">
                        <div class="row">
                            <div class="col-xs-2">
                                <Rock:RockTextBox ID="tbPersonFirstName" runat="server" CssClass="col-xs-12" Label="First Name" ValidationGroup="Person" />
                            </div>
                            <div class="col-xs-2">
                                <Rock:RockTextBox ID="tbPersonLastName" runat="server" CssClass="col-xs-12" Label="Last Name" ValidationGroup="Person" />
                            </div>
                            <div class="col-xs-1">
                                <Rock:RockDropDownList ID="ddlPersonSuffix" runat="server" CssClass="col-xs-12" Label="Suffix" />
                            </div>
                            <div class="col-xs-2">
                                <Rock:DatePicker ID="dpPersonDOB" runat="server" Label="Date of Birth" CssClass="col-xs-12 date-picker" ValidationGroup="Person" data-show-age="true" />
                            </div>
                            <div class="col-xs-2">
                                <Rock:RockDropDownList ID="ddlPersonGender" runat="server" Label="Gender" CssClass="col-xs-12" ValidationGroup="Person" />
                            </div>
                            <div class="col-xs-2">
                                <Rock:RockDropDownList ID="ddlPersonAbilityGrade" runat="server" Label="Ability/Grade" CssClass="col-xs-12" />
                            </div>
                            <div class="col-xs-1 shift-up centered">
                                <Rock:RockCheckBox ID="cbPersonSpecialNeeds" Label="Special Needs" runat="server" CssClass="" />
                            </div>

                            <div class="row flush-sides">
                                <div class="grid full-width soft-quarter-sides">
                                    <Rock:Grid ID="rGridPersonResults" runat="server" OnRowCommand="rGridPersonResults_AddExistingPerson" EnableResponsiveTable="true"
                                        OnGridRebind="rGridPersonResults_GridRebind" ShowActionRow="false" PageSize="4" DataKeyNames="Id" AllowSorting="true">
                                        <Columns>
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-2" ItemStyle-CssClass="col-xs-2" HeaderText="First Name" DataField="FirstName" SortExpression="FirstName" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-2" ItemStyle-CssClass="col-xs-2" HeaderText="Last Name" DataField="LastName" SortExpression="LastName" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-1" ItemStyle-CssClass="col-xs-1" HeaderText="Suffix" DataField="SuffixValue" SortExpression="SuffixValue" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-1" ItemStyle-CssClass="col-xs-1" HeaderText="DOB" DataField="BirthDate" DataFormatString="{0:MM/dd/yy}" HtmlEncode="false" SortExpression="BirthDate" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-1" ItemStyle-CssClass="col-xs-1" HeaderText="Age" DataField="Age" SortExpression="Age" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-2" ItemStyle-CssClass="col-xs-2" HeaderText="Gender" DataField="Gender" SortExpression="Gender" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-2" ItemStyle-CssClass="col-xs-2" HeaderText="Ability/Grade" DataField="Attribute" SortExpression="Attribute" />
                                            <Rock:RockBoundField HeaderStyle-CssClass="col-xs-1" ItemStyle-CssClass="col-xs-1" HeaderText="Special Needs" DataField="IsSpecialNeeds" SortExpression="IsSpecialNeeds" />
                                            <asp:TemplateField>
                                                <ItemTemplate>
                                                    <Rock:BootstrapButton ID="lbAdd" runat="server" CssClass="btn btn-lg btn-primary" CommandName="Add"
                                                        Text="Add" CommandArgument="<%# ((GridViewRow) Container).RowIndex %>" CausesValidation="false">
                                                    </Rock:BootstrapButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </Rock:Grid>
                                </div>
                            </div>

                            <div class="row">
                                <div class="soft-quarter-sides">
                                    <div class="col-xs-12 text-right">
                                        <Rock:BootstrapButton ID="lbNewPerson" runat="server" Text="None of these, add a new person" CssClass="btn btn-lg btn-primary btn-checkin-select"
                                            OnClick="lbNewPerson_Click" ValidationGroup="Person">
                                        </Rock:BootstrapButton>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <!-- ADD FAMILY MODAL -->
        <Rock:ModalDialog ID="mdlNewFamily" runat="server" Content-DefaultButton="lbSaveFamily">
            <Content>
                <div class="soft-quarter-ends">
                    <!-- Modal Header -->
                    <div class="row checkin-header">
                        <div class="col-xs-3 checkin-actions">
                            <Rock:BootstrapButton ID="lbCloseFamily" runat="server" Text="Cancel" CssClass="btn btn-lg btn-primary" OnClick="lbCloseFamily_Click" EnableViewState="false" />
                        </div>

                        <div class="col-xs-6 text-center">
                            <h2>New Family</h2>
                        </div>

                        <div class="col-xs-3 checkin-actions text-right">
                            <Rock:BootstrapButton ID="lbSaveFamily" CssClass="btn btn-lg btn-primary" runat="server" Text="Save" OnClick="lbSaveFamily_Click" ValidationGroup="Family" CausesValidation="true" />
                        </div>
                    </div>

                    <!-- Modal Body -->
                    <div class="checkin-body">
                        <asp:ListView ID="lvNewFamily" runat="server" OnPagePropertiesChanging="lvNewFamily_PagePropertiesChanging" OnItemDataBound="lvNewFamily_ItemDataBound">
                            <LayoutTemplate>
                                <div class="row large-font">
                                    <div class="col-xs-2">
                                        <label>First Name</label>
                                    </div>
                                    <div class="col-xs-2">
                                        <label>Last Name</label>
                                    </div>
                                    <div class="col-xs-1">
                                        <label>Suffix</label>
                                    </div>
                                    <div class="col-xs-2">
                                        <label>Date of Birth</label>
                                    </div>
                                    <div class="col-xs-2">
                                        <label>Gender</label>
                                    </div>
                                    <div class="col-xs-2">
                                        <label>Ability/Grade</label>
                                    </div>
                                    <div class="col-xs-1">
                                        <label>Special Needs</label>
                                    </div>
                                </div>
                                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                            </LayoutTemplate>
                            <ItemTemplate>
                                <div class="row expanded">
                                    <div class="col-xs-2">
                                        <Rock:RockTextBox ID="tbFirstName" runat="server" Text='<%# ((SerializedPerson)Container.DataItem).FirstName %>' ValidationGroup="Family" />
                                    </div>
                                    <div class="col-xs-2">
                                        <Rock:RockTextBox ID="tbLastName" runat="server" Text='<%# ((SerializedPerson)Container.DataItem).LastName %>' ValidationGroup="Family" />
                                    </div>
                                    <div class="col-xs-1">
                                        <Rock:RockDropDownList ID="ddlSuffix" runat="server" />
                                    </div>
                                    <div class="col-xs-2">
                                        <Rock:DatePicker ID="dpBirthDate" runat="server" SelectedDate='<%# ((SerializedPerson)Container.DataItem).BirthDate %>' ValidationGroup="Family" CssClass="date-picker" data-show-age="true" />
                                    </div>
                                    <div class="col-xs-2">
                                        <Rock:RockDropDownList ID="ddlGender" runat="server" ValidationGroup="Family" />
                                    </div>
                                    <div class="col-xs-2">
                                        <Rock:RockDropDownList ID="ddlAbilityGrade" runat="server" />
                                    </div>
                                    <div class="col-xs-1">
                                        <Rock:RockCheckBox ID="cbSpecialNeeds" runat="server" />
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
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">

    var setClickEvents = function () {

        $('.family').unbind('click').on('click', function () {
            $(this).toggleClass('active');
            $(this).siblings('.family').removeClass('active');
            if (!$(this).hasClass('btn-loading')) {
                $(this).addClass('btn-loading');
            }
        });

        $('.person').unbind('click').on('click', function (event) {
            event.stopPropagation();
            $(this).toggleClass('active');
            var selectedIds = $('#hfSelectedPerson').val();
            var buttonId = this.getAttribute('data-id');
            if (selectedIds.indexOf(buttonId) >= 0) {
                var buttonIdRegex = new RegExp(buttonId + ',*', "g");
                $('#hfSelectedPerson').val(selectedIds.replace(buttonIdRegex, ''));
            } else {
                $('#hfSelectedPerson').val(buttonId + ',' + selectedIds);
            }
            return false;
        });

        $('.visitor').unbind('click').on('click', function (event) {
            event.stopPropagation();
            $(this).toggleClass('active');
            var selectedIds = $('#hfSelectedVisitor').val();
            var buttonId = this.getAttribute('data-id');
            if (selectedIds.indexOf(buttonId) >= 0) {
                var buttonIdRegex = new RegExp(buttonId + ',*', "g");
                $('#hfSelectedPerson').val(selectedIds.replace(buttonIdRegex, ''));
            } else {
                $('#hfSelectedVisitor').val(buttonId + ',' + selectedIds);
            }
            return false;
        });
    };

    $(document).ready(function () {
        setClickEvents();
    });
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(setClickEvents);
</script>