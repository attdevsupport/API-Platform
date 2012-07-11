<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Speech_App1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&amp;T Sample Speech Application - Speech to Text(Generic) Application</title>
    <meta content="text/html; charset=UTF-8" http-equiv="Content-Type" />
    <link rel="stylesheet" type="text/css" href="style/common.css" />
</head>
<body>
    <form id="form1" runat="server">
    <div id="container">
        <!-- open HEADER -->
        <div id="header">
            <div>
                <div class="hcRight">
                    <asp:Label ID="lblServerTime" runat="server"></asp:Label>
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
        <div>
            <div class="content">
                <h1>
                    AT&amp;T Sample Speech Application  - Speech to Text(Generic) Application</h1>
                <h2>
                    Feature 1: Speech to Text(Generic)</h2>
            </div>
        </div>
        <br />
        <br />
        <div class="navigation">
            <table border="0" width="100%">
                <tbody>
                    <tr>
                        <td valign="middle" class="label" align="right">
                            Audio File:
                        </td>
                        <td class="cell">
                            <asp:FileUpload runat="server" ID="fileUpload1" />
                        </td>
                        <td>
                            <asp:Button runat="server" ID="btnSubmit" text="Submit" 
                                onclick="BtnSubmit_Click" />
                        </td>
                    </tr> 
                    <tr>
                    <td />
                    <td>
                    <div id="extraleft">
                        <div class="warning">
                            <strong>Note:</strong><br />
                            If no file is chosen, a <a href="./default.wav">default.wav</a> file will be loaded on submit.<br />
                            <strong>Speech file format constraints:</strong> <br />
                                •	16 bit PCM WAV, single channel, 8 kHz sampling<br />
                                •	AMR (narrowband), 12.2 kbit/s, 8 kHz sampling<br />
                        </div>
                    </div>
                    </td>
                    <td />
                    </tr>
                </tbody>
            </table>            
        </div>
        <br clear="all" />
        <br clear="all" />
        <div align="center">
            <asp:Panel ID="statusPanel" runat="server" Font-Names="Calibri" 
                Font-Size="XX-Small">
            </asp:Panel>
        </div>

        <div align="center">
        <asp:Panel ID="resultsPanel" runat="server" BorderWidth="0" Width="80%">
            <table width="500" cellpadding="1" cellspacing="1" border="0">
                <thead>
                    <tr>
                    	<th width="50%" class="label">Parameter</th>
                        <th width="50%" class="label">Value</th>
                    </tr>
                </thead>
                <tr>
                    <td class="cell" align="center">
                        <i>ResponseId </i>
                    </td>
                    <td class="cell" align="center">
                        <i><asp:Label ID="lblResponseId" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>Hypothesis </i>
                    </td>
                    <td class="cell" align="center">
                        <i><asp:Label ID="lblHypothesis" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>LanguageId </i>
                    </td>
                    <td class="cell" align="center">
                        <i><asp:Label ID="lblLanguageId" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>Confidence </i>
                    </td>
                    <td class="cell" align="center">
                        <i><asp:Label ID="lblConfidence" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>Grade </i>
                    </td>
                    <td class="cell" align="center">
                       <i><asp:Label ID="lblGrade" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>ResultText </i>
                    </td>
                    <td class="cell" align="center">
                        <i><asp:Label ID="lblResultText" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>Words </i>
                    </td>
                    <td class="cell" align="center">
                        <i><asp:Label ID="lblWords" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
                <tr>
                    <td class="cell" align="center">
                        <i>WordScores </i>
                    </td>
                    <td class="cell" align="center">
                       <i><asp:Label ID="lblWordScores" runat="server"></asp:Label>
                        </i>
                    </td>
                </tr>
            </table>               
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
                For dewnload of tools and documentation, please go to <a href="https://devconnect-api.att.com/"
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
