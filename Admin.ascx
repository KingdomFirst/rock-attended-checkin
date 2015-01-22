<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Admin.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Attended.Admin" %>

<asp:Panel ID="pnlContent" runat="server">

    <asp:PlaceHolder ID="phScript" runat="server"></asp:PlaceHolder>
    <asp:HiddenField ID="hfLatitude" runat="server" />
    <asp:HiddenField ID="hfLongitude" runat="server" />
    <asp:HiddenField ID="hfKiosk" runat="server" />
    <asp:HiddenField ID="hfGroupTypes" runat="server" />

    <span style="display: none">
        <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click"></asp:LinkButton>
        <asp:LinkButton ID="lbCheckGeoLocation" runat="server" OnClick="lbCheckGeoLocation_Click"></asp:LinkButton>
    </span>

    <Rock:ModalAlert ID="maWarning" runat="server" />

    <asp:Panel ID="pnlAdmin" runat="server" DefaultButton="lbOk" CssClass="attended">
        <asp:UpdatePanel ID="pnlHeader" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <div class="row checkin-header">
                    <div class="col-xs-2 checkin-actions">
                        <a id="lbRetry" runat="server" class="btn btn-lg btn-primary" visible="false" href="javascript:window.location.href=window.location.href">Retry</a>
                    </div>
                    <div class="col-xs-8 text-center">
                        <h1>Admin</h1>
                    </div>
                    <div class="col-xs-2 checkin-actions text-right">
                        <Rock:BootstrapButton ID="lbOk" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbOk_Click" EnableViewState="false">
                    <span class="fa fa-arrow-right"></span>
                        </Rock:BootstrapButton>
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>

        <div class="row checkin-body">
            <div class="col-xs-4"></div>
            <div class="col-xs-4">
                <asp:Label ID="lblHeader" runat="server"><h3>Checkin Type(s)</h3></asp:Label>
                <asp:Repeater ID="repMinistry" runat="server" OnItemDataBound="repMinistry_ItemDataBound">
                    <ItemTemplate>
                        <asp:Button ID="lbMinistry" runat="server" data-id='<%# Eval("Id") %>' CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-primary btn-lg btn-block btn-checkin-select btn-grouptype" Text='<%# Eval("Name") %>' />
                    </ItemTemplate>
                </asp:Repeater>
                <asp:Label ID="lblInfo" runat="server" Style="text-align: center" />
            </div>
            <div class="col-xs-4"></div>
        </div>
    </asp:Panel>
</asp:Panel>

<script type="text/javascript" src="../plugins/cc_newspring/attendedcheckin/scripts.js"></script>

<script type="text/javascript">

    var setClickEvents = function () {
        $('.btn-grouptype').off('click').on('click', function () {
            $(this).toggleClass('active').blur();
            var selectedIds = $('input[id$="hfGroupTypes"]').val();
            var buttonId = this.getAttribute('data-id') + ',';
            if (typeof selectedIds == "string" && (selectedIds.indexOf(buttonId) >= 0)) {
                $('input[id$="hfGroupTypes"]').val(selectedIds.replace(buttonId, ''));
            } else {
                $('input[id$="hfGroupTypes"]').val(buttonId + selectedIds);
            }
            return false;
        });
    };

    $(document).ready(function () { setClickEvents(); });
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(setClickEvents);
</script>