<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<%@ Page Language="VB" AutoEventWireup="true" CodeFile="Default.aspx.vb" Inherits="WapPush_App1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&T Sample Application - WAPPush</title>
    <link type="text/css" rel="stylesheet" href="style/common.css" />
</head>
    <body>
        <form id="form1" runat="server">
        <div id="container">
            <!-- open HEADER -->
            <div id="header">
                <div>
                    <div class="hcLeft">
                        Server Time:</div>
                    <div class="hcRight">
                        <asp:Label ID="serverTimeLabel" runat="server"></asp:Label>
                    </div>
                </div>
                <div>
                    <div class="hcLeft">
                        Client Time:</div>
                    <div class="hcRight">
                        <script language="JavaScript" type="text/javascript">
                            var myDate = new Date();
                            document.write(myDate);
                        </script>
                    </div>
                </div>
                <div>
                    <div class="hcLeft">
                        User Agent:</div>
                    <div class="hcRight">
                        <script language="JavaScript" type="text/javascript">
                            document.write("" + navigator.userAgent);
                        </script>
                    </div>
                </div>
                <br clear="all" />
            </div>
            <!-- close HEADER -->
            <div class="wrapper">
                <div class="content">
                    <h1>
                        AT&T Sample Application - WAPPush</h1>
                    <h2>
                        Feature 1: Send basic WAP message</h2>
                </div>
            </div>
            <div class="navigation">
                <table border="0" width="100%">
                    <tbody>
                        <tr>
                            <td width="20%" valign="top" class="label">
                                Phone:
                            </td>
                            <td class="cell">
                                <asp:TextBox ID="txtAddressWAPPush" runat="server" TabIndex="1" Width="90%" MaxLength="16">
                                </asp:TextBox>
                                &nbsp;
                            </td>
                        </tr>
                        <tr>
                            <td width="20%" valign="top" class="label">
                                URL:
                            </td>
                            <td class="cell">
                                <asp:TextBox ID="txtUrl" runat="server" TabIndex="1" Width="90%">http://developer.att.com</asp:TextBox>
                                &nbsp;
                            </td>
                        </tr>
                        <tr>
                            <td valign="top" class="label">
                                Service Type:
                            </td>
                            <td valign="top" class="cell">
                                <asp:RadioButtonList ID="RadioButtonList_ServiceType" runat="server" RepeatDirection="Horizontal"
                                    TextAlign="Left">
                                    <asp:ListItem Selected="True">Service Indication</asp:ListItem>
                                    <asp:ListItem Enabled="False">ServiceLoading</asp:ListItem>
                                </asp:RadioButtonList>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div class="warning">
                    <strong>WARNING:</strong><br />
                    At this time, AT&T only supports Service Type: Service Indication due to security
                    concerns.
                </div>
            </div>
            <div class="extra">
                <table border="0" width="100%">
                    <tbody>
                        <tr>
                            <td width="20%" valign="top" class="label">
                                Alert Text:
                            </td>
                            <td align="left" class="cell" width="90%">
                                <asp:TextBox ID="txtAlert" runat="server" TextMode="MultiLine" Width="89%" Rows="5"
                                    EnableTheming="True" Height="78px">This is a sample WAP Push message.</asp:TextBox>
                        </tr>
                    </tbody>
                </table>
                <table>
                    <tbody>
                        <tr>
                            <td>
                                <asp:Button runat="server" ID="btnSendWAP" Text="Send WAP message" TabIndex="6" BorderStyle="NotSet"
                                    OnClick="SendWAPButton_Click" style="margin-left: 0px" />
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <br clear="all" />
            <div align="center">
                <asp:Panel ID="wapPanel" runat="server" Font-Names="Calibri">
                </asp:Panel>
            </div>
            <div id="footer" align="center">
                <div style="float: right; width: 20%; font-size: 9px; text-align: right">
                    Powered by AT&amp;T Cloud Architecture</div>
                <p>
                    © 2012 AT&amp;T Intellectual Property. All rights reserved. <a href="http://developer.att.com/"
                        target="_blank">http://developer.att.com</a>
                    <br/>
                    The Application hosted on this site are working examples intended to be used for
                    reference in creating products to consume AT&amp;T Services and not meant to be
                    used as part of your product. The data in these pages is for test purposes only
                    and intended only for use as a reference in how the services perform.
                    <br/>
                    For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/"
                        target="_blank">https://devconnect-api.att.com</a>
                    <br/>
                    For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>
            </div>
            </div>
            <p>
                &nbsp;</p>
        </form>
    </body>
</html>
