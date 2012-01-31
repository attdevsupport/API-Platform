<%@ Page Language="VB" AutoEventWireup="true" CodeFile="Default.aspx.vb" Inherits="Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample Application – TL Service Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type"/>
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/>
    </head>
<body>
<div id="container">
<!-- open HEADER --><div id="header">
<div>
    <div id="hcLeft">Server Time:</div>
       	<div id="hcRight">
            <asp:Label ID="serverTimeLabel" runat="server" Text="Label"></asp:Label>
        </div>
    </div>
<div>
    <div id="hcLeft">Client Time:</div>
	<div id="hcRight">
        <script language="JavaScript" type="text/javascript">
            var myDate = new Date();
            document.write(myDate);
        </script>
    </div>
</div>
<div>
    <div id="hcLeft">User Agent:</div>    
	<div id="hcRight">
        <script language="JavaScript" type="text/javascript">
            document.write("" + navigator.userAgent);
        </script>
    </div>
</div>

<br clear="all" />
</div><!-- close HEADER -->

<div id="wrapper">
<div id="content">

<h1>AT&T Sample Application – TL</h1>
<h2>Feature 1: Map of Device Location</h2>

</div>
</div>
<br clear="all" />
<form id="form1" runat="server"> 
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell">
        <asp:TextBox ID="tlTextBox" runat="server" MaxLength="16"></asp:TextBox>
&nbsp;</td>
  </tr>
  <tr>
  	<td valign="top" class="label">Requested Accuracy:</td>
                    <td align="left" class="cell">
                        <asp:RadioButtonList ID="Radio_RequestedAccuracy" runat="server" 
                            RepeatDirection="Horizontal" Font-Names="Calibri" Font-Size="Small">
                            <asp:ListItem>150 m</asp:ListItem>
                            <asp:ListItem Selected="True">1,000 m</asp:ListItem>
                            <asp:ListItem>10,000 m</asp:ListItem>
                        </asp:RadioButtonList>
                                    </td>
  </tr>
  <tr>
  	<td valign="top" class="label">Acceptable Accuracy:</td>
                    <td align="left" class="cell">
                        <asp:RadioButtonList ID="Radio_AcceptedAccuracy" runat="server" 
                            RepeatDirection="Horizontal" Font-Names="Calibri" Font-Size="Small">
                            <asp:ListItem>150 m</asp:ListItem>
                            <asp:ListItem>1,000 m</asp:ListItem>
                            <asp:ListItem Selected="True">10,000 m</asp:ListItem>
                        </asp:RadioButtonList>
                                    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Delay Tolerance:</td>
                    <td align="left" class="cell">
                        <asp:RadioButtonList ID="Radio_DelayTolerance" runat="server" 
                            RepeatDirection="Horizontal" Font-Names="Calibri" Font-Size="Small">
                            <asp:ListItem>No Delay</asp:ListItem>
                            <asp:ListItem>Low Delay</asp:ListItem>
                            <asp:ListItem Selected="True">Delay Tolerant
                            </asp:ListItem>
                        </asp:RadioButtonList>
                                    </td>
    </td>
  </tr>
  </tbody></table>

</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><br /><br /><br /><br /><br /><br />
        <asp:Button ID="tlButton" runat="server" Text="Get Phone Location" 
            onclick="tlButton_Click1" /></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />

<div align="center">
    <asp:Panel ID="tlPanel" runat="server" Font-Names="Calibri">
    </asp:Panel>
</div>
<br clear="all" />

<div align="center">
      <div id="map_canvas" align="center" visible="false" runat="server"><br /> 
                    <iframe runat="server" id="MapTerminalLocation" width="600" height="400" frameborder="0"
                        scrolling="no" marginheight="0" marginwidth="0"></iframe>
                        </div>

</form>
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Virtual Mobile</div>
    <p>© 2011 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>
</body></html>