<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<%@ Page Language="VB" AutoEventWireup="true" CodeFile="Default.aspx.vb" Inherits="MIM_App1" 
    ValidateRequest="false" EnableEventValidation="false" ViewStateEncryptionMode="Never" EnableViewStateMac="false" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&amp;T Sample MIM Application 1- Sample MIM Service Application</title>
    <meta content="text/html; charset=UTF-8" http-equiv="Content-Type" />
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
                    <asp:Label ID="lblServerTime" runat="server" Text="Label"></asp:Label>
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
            <br style="clear: both;" />
        </div>
        <!-- close HEADER -->
        <div class="wrapper">
            <div class="content">
                <h1>
                    AT&amp;T Sample MIM Application 1 - Basic MIM Service Application</h1>
                <h2>
                    Feature 1: Get Message Header(s)</h2>
            </div>
        </div>
        <br />
        <br />
        <div class="navigation">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td style="width: 20%" valign="middle" class="label">
                            Header Count:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" ID="txtHeaderCount" Width="20%" MaxLength="3"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 20%" valign="middle" class="label">
                            Index Cursor:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" ID="txtIndexCursor" Width="70%"></asp:TextBox>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div id="extraleft">
            <div class="warning">
                <strong>INFORMATION:</strong> Header Count is mandatory(1-500) and Index cursor is optional.
                To Use MIM, mobile number should be registered at messages.att.net
            </div>
        </div>
        <div class="extra">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td class="cell">
                            <asp:Button ID="GetHeaderButton" runat="server" Text="Get Message Headers" OnClick="GetHeaderButton_Click" />
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <br />
        <br style="clear: both;" />
        <div style="text-align: left">
            <asp:Panel ID="statusPanel" runat="server" Font-Names="Calibri" Font-Size="XX-Small">
            </asp:Panel>
        </div>
        <div style="text-align: left">
            <asp:Panel ID="pnlHeader" runat="server">
                <table border="0" width="100%">
                    <tr>
                        <td style="width: 10%" valign="middle" class="label">
                            Header Count:
                        </td>
                        <td class="cell" align="left">
                            <asp:Label ID="lblHeaderCount" runat="server" CssClass="label"></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 10%" valign="middle" class="label">
                            Index Cursor:
                        </td>
                        <td class="cell" align="left">
                            <asp:Label ID="lblIndexCursor" runat="server" CssClass="label"></asp:Label>
                        </td>
                    </tr>
                </table>
                <table border="0" width="100%">
                    <tr>
                        <td colspan="2">
                            <asp:GridView ID="gvMessageHeaders" runat="server" BackColor="White" BorderColor="#CCCCCC"
                                BorderStyle="None" BorderWidth="1px" CellPadding="3" Width="100%">
                                <FooterStyle BackColor="White" ForeColor="#000066" />
                                <HeaderStyle BackColor="#006699" Font-Bold="True" ForeColor="White" HorizontalAlign="Left" />
                                <PagerStyle BackColor="White" ForeColor="#000066" HorizontalAlign="Left" />
                                <RowStyle ForeColor="#000066" HorizontalAlign="Left" />
                                <SelectedRowStyle BackColor="#669999" Font-Bold="True" ForeColor="White" />
                                <SortedAscendingCellStyle BackColor="#F1F1F1" />
                                <SortedAscendingHeaderStyle BackColor="#007DBB" />
                                <SortedDescendingCellStyle BackColor="#CAC9C9" />
                                <SortedDescendingHeaderStyle BackColor="#00547E" />
                            </asp:GridView>
                        </td>
                    </tr>
                </table>
            </asp:Panel>
        </div>
        <br style="clear: both;" />
        <br />
        <div>
            <div class="content">
                <h2>
                    Feature 2: Get Message Content</h2>
            </div>
        </div>
        <br />
        <br />
        <div class="navigation">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td style="width: 20%" valign="middle" class="label">
                            Message ID:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" ID="txtMessageId" Width="90%"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 20%" valign="middle" class="label">
                            Part Number:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" ID="txtPartNumber" Width="90%"></asp:TextBox>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <br />
        <br />
        <div class="extra">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td class="cell">
                            <asp:Button ID="GetMessageContent" runat="server" Text="Get Message Content" OnClick="GetMessageContent_Click" />
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <br style="clear: both;" />
        <div style="text-align: left">
            <asp:Panel ID="ContentPanelStatus" runat="server" Font-Names="Calibri" Font-Size="XX-Small">
            </asp:Panel>
        </div>
        <br style="clear: both;" />
        <br />
        <br />
        <div style="text-align: center">
            <asp:Panel ID="smilpanel" runat="server">
                <asp:TextBox ID="TextBox1" runat="server" Width="500" TextMode="MultiLine" BorderStyle="NotSet"
                    Enabled="False" EnableViewState="False" EnableTheming="False" Height="100"></asp:TextBox>
            </asp:Panel>
        </div>
        <asp:Panel ID="imagePanel" runat="server" HorizontalAlign="Center">
            <img id="imagetoshow" runat="server" alt="Fetched Image" />
        </asp:Panel>
        <br style="clear: both;" />
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
    <p>
        &nbsp;</p>
    </form>
</body>
</html>