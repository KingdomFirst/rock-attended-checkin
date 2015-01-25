<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Search.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Attended.Search" %>

<asp:UpdatePanel ID="pnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

        <asp:PlaceHolder ID="phScript" runat="server" />
        <Rock:ModalAlert ID="maWarning" runat="server" />

        <asp:Panel ID="pnlSearch" runat="server" DefaultButton="lbSearch" CssClass="attended">

            <div class="row checkin-header">
                <div class="col-xs-2 checkin-actions">
                    <Rock:BootstrapButton ID="lbBack" runat="server" CssClass="btn btn-primary" OnClick="lbBack_Click" EnableViewState="false">
                <span class="fa fa-arrow-left"></span>
                    </Rock:BootstrapButton>
                </div>

                <div class="col-xs-8">
                    <Rock:RockTextBox ID="tbSearchBox" MaxLength="50" CssClass="checkin-phone-entry" runat="server" Label="" TabIndex="0" />
                    <asp:LinkButton runat="server" OnClick="lbSearch_Click">
                    <span class="fa fa-search"></span>
                    </asp:LinkButton>
                </div>

                <div class="col-xs-2 checkin-actions text-right">
                    <Rock:BootstrapButton ID="lbSearch" runat="server" CssClass="btn btn-lg btn-primary" OnClick="lbSearch_Click" EnableViewState="false">
                    <span class="fa fa-arrow-right"></span>
                    </Rock:BootstrapButton>
                </div>
            </div>

            <div class="row checkin-body">
                <div class="col-xs-3"></div>
                <div class="col-xs-6">
                    <asp:Panel ID="pnlKeyPad" runat="server" Visible="false" CssClass="tenkey checkin-phone-entry ">
                        <div>
                            <a href="#" class="btn btn-default btn-lg digit">1</a>
                            <a href="#" class="btn btn-default btn-lg digit">2</a>
                            <a href="#" class="btn btn-default btn-lg digit">3</a>
                        </div>
                        <div>
                            <a href="#" class="btn btn-default btn-lg digit">4</a>
                            <a href="#" class="btn btn-default btn-lg digit">5</a>
                            <a href="#" class="btn btn-default btn-lg digit">6</a>
                        </div>
                        <div>
                            <a href="#" class="btn btn-default btn-lg digit">7</a>
                            <a href="#" class="btn btn-default btn-lg digit">8</a>
                            <a href="#" class="btn btn-default btn-lg digit">9</a>
                        </div>
                        <div>
                            <a href="#" class="btn btn-default btn-lg digit back"><i class='fa fa-arrow-left'></i></a>
                            <a href="#" class="btn btn-default btn-lg digit">0</a>
                            <a href="#" class="btn btn-default btn-lg digit clear">C</a>
                        </div>
                    </asp:Panel>
                </div>
                <div class="col-xs-3"></div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript" src="../plugins/cc_newspring/attendedcheckin/scripts.js"></script>

<script>

    var SetKeyEvents = function () {
        $('.tenkey a.digit').unbind('click').click(function () {
            $name = $("input[id$='tbSearchBox']");
            $name.val($name.val() + $(this).html());
        });
        $('.tenkey a.back').unbind('click').click(function () {
            $name = $("input[id$='tbSearchBox']");
            $name.val($name.val().slice(0, -1));
        });
        $('.tenkey a.clear').unbind('click').click(function () {
            $name = $("input[id$='tbSearchBox']");
            $name.val('');
        });
    };

    $(document).ready(function () {
        SetKeyEvents();
    });

    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(SetKeyEvents);
</script>