<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Confirm.ascx.cs" Inherits="cc.newspring.AttendedCheckin.Confirm" %>

<asp:UpdatePanel ID="pnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <asp:Panel ID="pnlConfirm" runat="server" CssClass="attended">

            <Rock:ModalAlert ID="maWarning" runat="server" />

            <div class="row checkin-header">
                <div class="col-xs-2 checkin-actions">
                    <Rock:BootstrapButton ID="lbBack" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbBack_Click" EnableViewState="false">
                    <span class="fa fa-arrow-left"></span>
                    </Rock:BootstrapButton>
                </div>

                <div class="col-xs-8 text-center">
                    <h1>Confirm</h1>
                </div>

                <div class="col-xs-2 checkin-actions text-right">
                    <Rock:BootstrapButton ID="lbDone" CssClass="btn btn-lg btn-primary" runat="server" OnClick="lbDone_Click" EnableViewState="false">
                    <span class="fa fa-arrow-right"></span>
                    </Rock:BootstrapButton>
                </div>
            </div>

            <div class="checkin-body selected-grid">
                <div class="row">
                    <asp:UpdatePanel ID="pnlSelectedGrid" runat="server">
                        <ContentTemplate>
                            <div class="grid in-the-middle">
                                <Rock:Grid ID="gPersonList" runat="server" DataKeyNames="PersonId,GroupId,LocationId,ScheduleId" DisplayType="Light"
                                    CssClass="three-col-with-controls" EnableResponsiveTable="false" ShowFooter="false" EmptyDataText="No People Selected"
                                    OnRowCommand="gPersonList_Print" OnGridRebind="gPersonList_GridRebind">
                                    <Columns>
                                        <asp:BoundField DataField="PersonId" Visible="false" />
                                        <asp:BoundField DataField="Name" HeaderText="Name" />
                                        <asp:BoundField DataField="GroupId" Visible="false" />
                                        <asp:BoundField DataField="Location" HeaderText="Assigned To" />
                                        <asp:BoundField DataField="LocationId" Visible="false" />
                                        <asp:BoundField DataField="Schedule" HeaderText="Time" />
                                        <asp:BoundField DataField="ScheduleId" Visible="false" />
                                        <Rock:EditField HeaderText="Edit" ControlStyle-CssClass="btn btn-lg btn-primary" OnClick="gPersonList_Edit" />
                                        <Rock:DeleteField HeaderText="Delete" ControlStyle-CssClass="btn btn-lg btn-primary" OnClick="gPersonList_Delete" />
                                        <asp:TemplateField HeaderText="Print">
                                            <ItemTemplate>
                                                <asp:LinkButton ID="btnPrint" runat="server" CssClass="btn btn-lg btn-primary" CommandName="Print" CommandArgument="<%# Container.DataItemIndex %>">
                                            <i class="fa fa-print"></i>
                                                </asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
                <div class="row at-the-bottom">
                    <div class="col-xs-9"></div>
                    <div class="col-xs-3">
                        <Rock:BootstrapButton ID="lbPrintAll" CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" runat="server" OnClick="lbPrintAll_Click" Text="Print All" EnableViewState="false" />
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript" src="../plugins/cc_newspring/attendedcheckin/scripts.js"></script>