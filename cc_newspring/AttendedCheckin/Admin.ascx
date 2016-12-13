<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Admin.ascx.cs" Inherits="RockWeb.Plugins.cc_newspring.AttendedCheckin.Admin" %>

<asp:UpdatePanel ID="pnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <asp:PlaceHolder ID="phScript" runat="server"></asp:PlaceHolder>
        <asp:HiddenField ID="hfLatitude" runat="server" />
        <asp:HiddenField ID="hfLongitude" runat="server" />
        <asp:HiddenField ID="hfTheme" runat="server" />
        <asp:HiddenField ID="hfKiosk" runat="server" />
        <asp:HiddenField ID="hfCheckinType" runat="server" />
        <asp:HiddenField ID="hfGroupTypes" runat="server" />

        <Rock:ModalAlert ID="maAlert" runat="server" />

        <div style="display: none">
            <asp:LinkButton ID="lbTestPrint" runat="server" />
            <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click" />
            <asp:LinkButton ID="lbCheckGeoLocation" runat="server" OnClick="lbCheckGeoLocation_Click" />
        </div>

        <asp:Panel ID="pnlAdmin" runat="server" DefaultButton="lbOk" CssClass="attended">
            <asp:UpdatePanel ID="pnlHeader" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <div class="row checkin-header">
                        <div class="col-xs-8 col-xs-offset-2 text-center">
                            <h1>Admin</h1>
                        </div>
                        <div class="col-xs-2 checkin-actions text-right">
                            <Rock:BootstrapButton ID="lbOk" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbOk_Click">
                                <span class="fa fa-arrow-right"></span>
                            </Rock:BootstrapButton>
                        </div>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>

            <div class="row checkin-body">
                <div class="col-xs-12 centered">
                    <asp:Label ID="lblHeader" runat="server" Visible="false"><h3>Checkin Type(s)</h3></asp:Label>
                    <asp:DataList ID="dlMinistry" runat="server" OnItemDataBound="dlMinistry_ItemDataBound" RepeatColumns="3" CssClass="full-width centered">
                        <ItemStyle CssClass="expanded" />
                        <ItemTemplate>
                            <asp:Button ID="btnGroupType" runat="server" data-id='<%# Eval("Id") %>' CssClass="btn btn-primary btn-lg btn-block btn-checkin-select" Text='<%# Eval("Name") %>' OnClientClick="toggleGroupType(this); return false;" />
                        </ItemTemplate>
                    </asp:DataList>
                    <asp:Label ID="lblInfo" runat="server" />
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">

    function toggleGroupType(element) {
        $(element).toggleClass('active').blur();
        var selectedIds = $("input[id$='hfGroupTypes']").val();
        var groupTypeId = element.getAttribute('data-id');
        if (selectedIds.indexOf(groupTypeId) >= 0) { // already selected, remove id
            var selectedIdRegex = new RegExp(groupTypeId + ',*', "g");
            $("input[id$='hfGroupTypes']").val(selectedIds.replace(selectedIdRegex, ''));
        } else { // newly selected, add id
            $("input[id$='hfGroupTypes']").val(groupTypeId + ',' + selectedIds);
        }
    };

    var setKeyboardEvents = function () {
        $(document).unbind('keydown').keydown(function (e) {
            if (e.keyCode === 73 && e.ctrlKey) {

                // Ctrl + Shift + I
                e.stopPropagation();
                __doPostBack($('a[id$="lbTestPrint"]').attr('id'), '');
                return false;
            }
        });
    };

    $(document).ready(function () {
        setKeyboardEvents();
    });
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(setKeyboardEvents);
</script>
