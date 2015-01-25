<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ActivitySelect.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Attended.ActivitySelect" %>

<asp:UpdatePanel ID="pnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <asp:HiddenField ID="hfAllergyAttributeId" runat="server" />

        <asp:Panel ID="pnlActivities" runat="server" CssClass="attended">

            <Rock:ModalAlert ID="maWarning" runat="server" />

            <div class="row checkin-header">
                <div class="col-xs-3 checkin-actions">
                    <Rock:BootstrapButton ID="lbBack" CssClass="btn btn-primary btn-lg" runat="server" OnClick="lbBack_Click" EnableViewState="false">
                    <span class="fa fa-arrow-left"></span>
                    </Rock:BootstrapButton>
                </div>

                <div class="col-xs-6 text-center">
                    <h1>
                        <asp:Literal ID="lblPersonName" runat="server" EnableViewState="false" /></h1>
                </div>

                <div class="col-xs-3 checkin-actions text-right">
                    <Rock:BootstrapButton ID="lbNext" CssClass="btn btn-primary btn-lg" runat="server" OnClick="lbNext_Click" EnableViewState="false">
                     <span class="fa fa-arrow-right"></span>
                    </Rock:BootstrapButton>
                </div>
            </div>

            <div class="row checkin-body">
                <div class="col-xs-3">
                    <asp:UpdatePanel ID="pnlGroupTypes" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <h3>GroupType</h3>
                            <asp:ListView ID="lvGroupType" runat="server" OnItemCommand="lvGroupType_ItemCommand" OnPagePropertiesChanging="lvGroupType_PagePropertiesChanging" OnItemDataBound="lvGroupType_ItemDataBound">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbGroupType" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" CausesValidation="false" />
                                </ItemTemplate>
                            </asp:ListView>
                            <asp:DataPager ID="dpGroupType" runat="server" PageSize="5" PagedControlID="lvGroupType">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div class="col-xs-3">
                    <asp:UpdatePanel ID="pnlLocations" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <h3 id="hdrLocations" runat="server">Location</h3>
                            <asp:ListView ID="lvLocation" runat="server" OnPagePropertiesChanging="lvLocation_PagePropertiesChanging" OnItemCommand="lvLocation_ItemCommand" OnItemDataBound="lvLocation_ItemDataBound">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbLocation" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select"></asp:LinkButton>
                                </ItemTemplate>
                            </asp:ListView>
                            <asp:DataPager ID="dpLocation" runat="server" PageSize="5" PagedControlID="lvLocation">
                                <Fields>
                                    <asp:NextPreviousPagerField ButtonType="Button" ButtonCssClass="pagination btn btn-primary btn-checkin-select" />
                                </Fields>
                            </asp:DataPager>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div class="col-xs-3">
                    <asp:UpdatePanel ID="pnlSchedules" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <h3>Schedule</h3>
                            <asp:Repeater ID="rSchedule" runat="server" OnItemCommand="rSchedule_ItemCommand" OnItemDataBound="rSchedule_ItemDataBound">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lbSchedule" runat="server" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" CausesValidation="false" />
                                </ItemTemplate>
                            </asp:Repeater>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div class="col-xs-3 selected-grid">
                    <h3>Selected</h3>
                    <asp:UpdatePanel ID="pnlSelected" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <div class="grid cozy">
                                <Rock:Grid ID="gSelectedGrid" runat="server" ShowHeader="false" ShowFooter="false" EnableResponsiveTable="false" DisplayType="Light"
                                    DataKeyNames="GroupId, LocationId, ScheduleId" EmptyDataText="No Locations Selected">
                                    <Columns>
                                        <asp:BoundField DataField="Schedule" />
                                        <asp:BoundField DataField="ScheduleId" Visible="false" />
                                        <asp:BoundField DataField="GroupId" Visible="false" />
                                        <asp:BoundField DataField="Location" />
                                        <asp:BoundField DataField="LocationId" Visible="false" />
                                        <Rock:DeleteField OnClick="gSelectedGrid_Delete" ControlStyle-CssClass="btn btn-primary " />
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </div>

            <div class="row checkin-footer at-the-bottom">
                <div class="col-xs-3 col-xs-offset-6">
                    <asp:LinkButton ID="lbEditInfo" runat="server" Text="Edit Info" CssClass="btn btn-primary btn-block btn-checkin-select" OnClick="lbEditInfo_Click" CausesValidation="false" />
                </div>
                <div class="col-xs-3">
                    <asp:LinkButton ID="lbAddNote" runat="server" Text="Add Note" CssClass="btn btn-primary btn-block btn-checkin-select" OnClick="lbAddNote_Click" CausesValidation="false" />
                </div>
            </div>
        </asp:Panel>

        <Rock:ModalDialog ID="mdlNotes" runat="server" Content-DefaultButton="lbSaveNotes">
            <Content>
                <div class="checkin-header row">
                    <div class="col-xs-3 checkin-actions">
                        <Rock:BootstrapButton ID="lbCloseNotes" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbCloseNotes_Click" Text="Cancel" EnableViewState="false" />
                    </div>
                    <div class="col-xs-6 text-center">
                        <h2>Add Notes</h2>
                    </div>
                    <div class="col-xs-3 checkin-actions text-right">
                        <asp:LinkButton ID="lbSaveNotes" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbSaveNotes_Click" Text="Save" EnableViewState="false" />
                    </div>
                </div>

                <div class="checkin-body">
                    <div class="row">
                        <div class="col-xs-12">
                            <asp:PlaceHolder ID="phAttributes" runat="server" EnableViewState="false"></asp:PlaceHolder>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-xs-12">
                            <div class="form-group">
                                <label>Notes</label>
                                <Rock:RockTextBox ID="tbNoteText" runat="server" MaxLength="60" Rows="3" />
                            </div>
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mdlInfo" runat="server" Content-DefaultButton="lbSaveEditInfo">
            <Content>
                <div class="row checkin-header">
                    <div class="col-xs-3 checkin-actions">
                        <Rock:BootstrapButton ID="lbCloseEditInfo" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbCloseEditInfo_Click" Text="Cancel" EnableViewState="false" />
                    </div>

                    <div class="col-xs-6 text-center">
                        <h2>Edit Info</h2>
                    </div>

                    <div class="col-xs-3 checkin-actions text-right">
                        <Rock:BootstrapButton ID="lbSaveEditInfo" ValidationGroup="Person" CausesValidation="true" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbSaveEditInfo_Click" Text="Save" EnableViewState="false" />
                    </div>
                </div>

                <div class="checkin-body">
                    <div class="row">
                        <div class="col-xs-2">
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" ValidationGroup="Person" />
                        </div>
                        <div class="col-xs-2">
                            <Rock:RockTextBox ID="tbNickname" runat="server" ValidationGroup="Person" Label="Nickname" />
                        </div>
                        <div class="col-xs-2">
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" ValidationGroup="Person" />
                        </div>
                        <div class="col-xs-3">
                            <Rock:DatePicker ID="dpDOB" runat="server" Label="DOB" ValidationGroup="Person" CssClass="date-picker" />
                        </div>
                        <div class="col-xs-3">
                            <Rock:RockDropDownList ID="ddlAbility" runat="server" Label="Ability/Grade" />
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript" src="../plugins/cc_newspring/attendedcheckin/scripts.js"></script>

<script type="text/javascript">

    var removeModalOverlay = function () {
        $('.modal:visible').css('z-index', Number($('.modal-backdrop').css('z-index')) + 1);
    };

    $(document).ready(function () {
        removeModalOverlay();
    });
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(removeModalOverlay);
</script>