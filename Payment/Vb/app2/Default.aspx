<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<%@ Page Language="VB" AutoEventWireup="true" CodeFile="Default.aspx.vb" Inherits="Payment_App2" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&T Sample Payment Application - Subscription Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css" />
</head>
<body>
    <form id="form1" runat="server">
    <div id="container">
        <!-- open HEADER -->
        <div id="header">
            <div>
                <div class="hcRight">
                    <asp:Label runat="server" Text="Label" ID="lblServerTime"></asp:Label>
                </div>
                <div class="hcLeft">
                    Server Time:</div>
            </div>
            <div>
                <div class="hcRight">
                    <script language="JavaScript" type="text/javascript">
                        var myDate = new Date();
                        document.write(myDate);
                    </script>
                </div>
                <div class="hcLeft">
                    Client Time:</div>
            </div>
            <div>
                <div class="hcRight">
                    <script language="JavaScript" type="text/javascript">
                        document.write("" + navigator.userAgent);
                    </script>
                </div>
                <div class="hcLeft">
                    User Agent:</div>
            </div>
            <br clear="all" />
        </div>
        <!-- close HEADER -->
        <div class="wrapper">
            <div class="content">
                <h1>
                    AT&T Sample Payment Application - Subscription Application</h1>
                <h2>
                    Feature 1: Create New Subscription</h2>
                <br />
            </div>
        </div>
        <div class="navigation">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <asp:RadioButtonList ID="Radio_SubscriptionProductType" runat="server" RepeatDirection="Vertical"
                            Font-Names="Calibri" Font-Size="Small">
                            <asp:ListItem Selected="True">Subscribe for $1.99 per month</asp:ListItem>
                            <asp:ListItem>Subscribe for $3.99 per month</asp:ListItem>
                        </asp:RadioButtonList>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class="extra">
            <table>
                <tbody>
                    <tr>
                        <td>
                            <br />
                            <br />
                            <asp:Button runat="server" Text="Subscribe" ID="newSubscriptionButton" OnClick="NewSubscriptionButton_Click1" />
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <br clear="all" />
        <div align="center">
            <asp:Panel runat="server" ID="newSubscriptionPanel">
            </asp:Panel>
        </div>
        <div class="successWide" id="subscriptionSuccessTable" runat="server" visible="False">
            <strong>SUCCESS:</strong><br />
            <strong>Merchant Transaction ID:</strong>
            <asp:Label ID="lblsubsid" runat="server" Text="" />
            <br />
            <strong>Transaction Auth Code:</strong>
            <asp:Label ID="lblsubscode" runat="server" Text="" />
            <br />
            <br />
            <strong></strong>
            <asp:Button runat="server" Text="View Notary" ID="viewNotaryButton" OnClick="ViewNotaryButton_Click" />
            <br />
            &nbsp;</div>
        <br clear="all" />
        <div class="wrapper">
            <div class="content">
                <h2>
                    <br />
                    Feature 2: Get Subscription Status</h2>
            </div>
        </div>
        <div class="navigation" align="center">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td class="cell" align="left">
                            <asp:RadioButtonList ID="Radio_SubscriptionStatus" runat="server" RepeatDirection="Vertical"
                                Font-Names="Calibri" Font-Size="Small">
                                <asp:ListItem ID="GetSubscriptionMerchantSubsID" Selected="True">Merchant Transaction ID: </asp:ListItem>
                                <asp:ListItem ID="GetSubscriptionAuthCode">Auth Code: </asp:ListItem>
                                <asp:ListItem ID="GetSubscriptionID">Subscription ID: </asp:ListItem>
                            </asp:RadioButtonList>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class="extra">
            <table>
                <tbody>
                    <tr>
                        <td>
                            <br />
                            <br />
                            <br />
                            <asp:Button runat="server" Text="Get Subscription Status" ID="getSubscriptionButton"
                                OnClick="GetSubscriptionButton_Click" />
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <br clear="all" />
        <div class="successWide" id="subsGetStatusTable" runat="server" visible="False">
            <strong>SUCCESS</strong><br />
        </div>
        <div>
            <asp:Panel runat="server" ID="getSubscriptionStatusPanel">
            </asp:Panel>
        </div>
        <br clear="all" />
        <div class="wrapper">
            <div class="content">
                <h2>
                    <br />
                    Feature 3: Get Subscription Details</h2>
            </div>
        </div>
        <div class="navigation">
            <asp:Table ID="subsDetailsTable" runat="server" Width="750px" CellPadding="1" CellSpacing="1"
                border="0">
            </asp:Table>
        </div>
        <br clear="all" />
        <div class="extra">
            <asp:Button runat="server" Text="Get Subscription Details" ID="btnGetSubscriptionDetails"
                OnClick="BtnGetSubscriptionDetails_Click" />
        </div>
        <br clear="all" />
        <br clear="all" />
        <div class="successWide" id="subsDetailsSuccessTable" runat="server" visible="False">
            <strong>SUCCESS</strong><br />
        </div>
        <div align="center">
            <asp:Panel runat="server" ID="subsDetailsPanel">
            </asp:Panel>
        </div>
        <br />
        <br clear="all" />
        <div class="wrapper">
            <div class="content">
                <h2>
                    <br />
                    Feature 4: Refund Subscription</h2>
            </div>
        </div>
        <div class="navigation">
            <asp:Table ID="subsRefundTable" runat="server" Width="750px" CellPadding="1" CellSpacing="1"
                border="0">
            </asp:Table>
        </div>
        <br clear="all" />
        <div class="extra">
            <asp:Button runat="server" Text="Refund" ID="btnGetSubscriptionRefund" OnClick="BtnGetSubscriptionRefund_Click" />
        </div>
        <br clear="all" />
        <br clear="all" />
        <div class="successWide" id="subsRefundSuccessTable" runat="server" visible="False">
            <strong>SUCCESS</strong><br />
        </div>
        <div align="center">
            <asp:Panel runat="server" ID="subsRefundPanel">
            </asp:Panel>
        </div>
        <br clear="all" />
        <div class="wrapper">
            <div class="content">
                <h2>
                    Feature 5: Notifications</h2>
            </div>
        </div>
        <div class="navigation">
            <asp:Table ID="notificationTable" runat="server" Width="750px" CellPadding="1" CellSpacing="1"
                border="0">
            </asp:Table>
        </div>
        <br clear="all" />
        <div class="extra">
            <asp:Button ID="btnRefreshNotifications" runat="server" Text="Refresh" 
                onclick="BtnRefreshNotifications_Click" />
        </div>
        <div align="center">
            <asp:Panel runat="server" ID="notificationPanel">
            </asp:Panel>
        </div>
        <br clear="all" />
        <br clear="all" />
        <div id="footer">
            <div style="float: right; width: 20%; font-size: 9px; text-align: right">
                Powered by AT&amp;T Cloud Architecture</div>
            <p>
                &#169; 2012 AT&amp;T Intellectual Property. All rights reserved. <a href="http://developer.att.com/"
                    target="_blank">http://developer.att.com</a>
                <br />
                The Application hosted on this site are working examples intended to be used for
                reference in creating products to consume AT&amp;T Services and not meant to be
                used as part of your product. The data in these pages is for test purposes only
                and intended only for use as a reference in how the services perform.
                <br />
                For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/"
                    target="_blank">https://devconnect-api.att.com</a>
                <br />
                For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a></p>
        </div>
    </div>
    </form>
</body>
</html>
