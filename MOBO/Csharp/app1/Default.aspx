<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Mobo_App1" validateRequest="false" enableEventValidation="false" viewStateEncryptionMode="Never" enableViewStateMac="false"%>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&amp;T Sample Mobo Application 1- Sample Mobo Service Application</title>
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
            <br clear="all" />
        </div>
        <!-- close HEADER -->
        <div>
            <div class="content">
                <h1>
                    AT&amp;T Sample Mobo Application 1 - Basic Mobo Service Application</h1>
                <h2>
                    Feature 1: Send Message</h2>
            </div>
        </div>
        <br />
        <br />
        <div class="navigation">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td width="20%" valign="top" class="label">
                            Address:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" ID="txtPhone" Width="90%"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td valign="top" class="label">
                            Message:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" TextMode="MultiLine" ID="txtMessage" Width="90%" Rows="4"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td valign="top" class="label">
                            Subject:
                        </td>
                        <td class="cell">
                            <asp:TextBox runat="server" TextMode="MultiLine" ID="txtSubject" Width="90%" Rows="4"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td valign="top" class="label">
                            Group:
                        </td>
                        <td class="cell">
                            <asp:CheckBox runat="server" ID="chkGroup" />
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div id="extraleft">
            <div class="warning">
                <strong>WARNING:</strong><br />
                total size of all attachments cannot exceed 600 KB.
            </div>
        </div>
        <div class="extra">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td valign="bottom" class="style1">
                            Attachment 1:
                        </td>
                        <td class="cell">
                            <asp:FileUpload runat="server" ID="fileUpload1" />
                        </td>
                    </tr>
                    <tr>
                        <td valign="bottom" class="style1">
                            Attachment 2:
                        </td>
                        <td class="cell">
                        <asp:FileUpload runat="server" ID="fileUpload2" />
                        </td>
                    </tr>
                    <tr>
                        <td valign="bottom" class="style1">
                            Attachment 3:
                        </td>
                        <td class="cell">
                        <asp:FileUpload runat="server" ID="fileUpload3" />
                        </td>
                    </tr>
                    <tr>
                        <td valign="bottom" class="style1">
                            Attachment 4:
                        </td>
                        <td class="cell">
                        <asp:FileUpload runat="server" ID="fileUpload4" />
                        </td>
                    </tr>
                    <tr>
                        <td valign="bottom" class="style1">
                            Attachment 5:
                        </td>
                        <td class="cell">
                        <asp:FileUpload runat="server" ID="fileUpload5" />
                        </td>
                    </tr>
                </tbody>
            </table>
            <table>
                <tbody>
                    <tr>
                        <td>
                            <asp:Button runat="server" ID="btnSendMessage" text="Send Message" 
                                onclick="BtnSendMessage_Click"/>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <br clear="all" />
        <div align="center">
            <asp:Panel ID="statusPanel" runat="server" Font-Names="Calibri" Font-Size="XX-Small">
            </asp:Panel>
        </div>
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
    <p>
        &nbsp;</p>
    </form>
</body>
</html>
