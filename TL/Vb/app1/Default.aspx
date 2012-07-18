<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<%@ Page Language="VB" AutoEventWireup="true" CodeFile="Default.aspx.vb" Inherits="TL_App1" %>


<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&T Sample Application – TL Service Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type" />
    <link rel="stylesheet" type="text/css" href="style/common.css" />
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
                    <asp:Label ID="serverTimeLabel" runat="server" Text="Label"></asp:Label>
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
        <div>
            <div class="content">
                <h1>
                    AT&T Sample Application – TL</h1>
                <h2>
                    Feature 1: Map of Device Location</h2>
            </div>
        </div>
        <div class="navigation">
            <table>
                <tbody>
                    <tr>
                        <td align="left" class="label">
                            Requested Accuracy:
                        </td>
                        <td align="left" class="cell">
                            <asp:RadioButtonList ID="Radio_RequestedAccuracy" runat="server" RepeatDirection="Horizontal"
                                Font-Names="Calibri" Font-Size="Small">
                                <asp:ListItem>150 m</asp:ListItem>
                                <asp:ListItem Selected="True">1,000 m</asp:ListItem>
                                <asp:ListItem>10,000 m</asp:ListItem>
                            </asp:RadioButtonList>
                        </td>
                    </tr>
                    <tr>
                        <td align="left" class="label">
                            Acceptable Accuracy:
                        </td>
                        <td align="left" class="cell">
                            <asp:RadioButtonList ID="Radio_AcceptedAccuracy" runat="server" RepeatDirection="Horizontal"
                                Font-Names="Calibri" Font-Size="Small">
                                <asp:ListItem>150 m</asp:ListItem>
                                <asp:ListItem>1,000 m</asp:ListItem>
                                <asp:ListItem Selected="True">10,000 m</asp:ListItem>
                            </asp:RadioButtonList>
                        </td>
                    </tr>
                    <tr>
                        <td align="left" class="label">
                            Delay Tolerance:
                        </td>
                        <td align="left" class="cell">
                            <asp:RadioButtonList ID="Radio_DelayTolerance" runat="server" RepeatDirection="Horizontal"
                                Font-Names="Calibri" Font-Size="Small">
                                <asp:ListItem>No Delay</asp:ListItem>
                                <asp:ListItem>Low Delay</asp:ListItem>
                                <asp:ListItem Selected="True">Delay Tolerant
                                </asp:ListItem>
                            </asp:RadioButtonList>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class="extra">
        <br />
        <br />
        <br />
        <br />
            <asp:Button ID="GetDeviceLocationButton" runat="server" Text="Get Phone Location" OnClick="GetDeviceLocation_Click" />
        </div>
        <br clear="all" />
        <div>
            <asp:Panel ID="tlPanel" runat="server" Font-Names="Calibri">
            </asp:Panel>
        </div>
        <br clear="all" />
        <div align="center">
            <div id="map_canvas" align="center" visible="false" runat="server">
                <br />
                <iframe runat="server" id="MapTerminalLocation" width="600" height="400" frameborder="0"
                    scrolling="no" marginheight="0" marginwidth="0"></iframe>
            </div>
            <div id="footer">
                <div style="float: right; width: 20%; font-size: 9px; text-align: right">
                    Powered by AT&amp;T Cloud Architecture</div>
                <p>
                    © 2012 AT&amp;T Intellectual Property. All rights reserved. <a href="http://developer.att.com/"
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
    </div>
    </form>
</body>
</html>
