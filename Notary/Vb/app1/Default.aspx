<%@ Page Language="VB" AutoEventWireup="true" CodeFile="Default.aspx.vb" Inherits="_Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&amp;T Sample Notary Application - Sign Payload Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/ >
</script>
<body>

<div id="container">
<!-- open HEADER --><div id="header">

<div>
    <div id="hcRight">
        <asp:Label runat="server" Text="Label" ID="serverTimeLabel"></asp:Label>
    </div>
    <div id="hcLeft">Server Time:</div>
</div>
<div>
	<div id="hcRight"><script language="JavaScript" type="text/javascript">
	                      var myDate = new Date();
	                      document.write(myDate);
</script></div>
	<div id="hcLeft">Client Time:</div>
</div>
<div>
	<div id="hcRight"><script language="JavaScript" type="text/javascript">
	                      document.write("" + navigator.userAgent);
</script></div>
	<div id="hcLeft">User Agent:</div>
</div>
<br clear="all" />
</div><!-- close HEADER -->

<div id="wrapper">
<div id="content">

<h1>A&amp;T Sample Notary Application - Sign Payload Application</h1>

</div>
</div>

<div id="wrapper">
  <div id="content">

<h2><br />
Feature 1: Sign Payload</h2>
<br/>
</div>
</div>
<form id="form1" runat="server">
<div id="navigation">

<table border="0" width="950px">
  <tbody>
  <tr>
    <td valign="top" class="label">Request:</td>
    <td class="cell" >
        <asp:TextBox runat="server" ID="requestText" Height="223px" 
            Width="400px" TextMode="MultiLine"></asp:TextBox>
    <td width="50px"></td>
    <td  valign="top" class="label">Signed Payload:</td>
    <td class="cell" width="400px">
        <asp:TextBox runat="server" Height="223px" 
            Width="400px"  TextMode="MultiLine" 
            ID="SignedPayLoadTextBox" BorderStyle="None"></asp:TextBox>
</td>
  </tr>
<tr>
    <td></td>
    <td></td>
    <td width="50px"></td>
    <td valign="top" class="label">Signature:</td>
    <td class="cell">
        <asp:TextBox ID="SignatureTextBox" runat="server" Height="73px" Width="400px" 
             TextMode="MultiLine" BorderStyle="None"></asp:TextBox>
    </td>
</tr>
  <tr>
    <td></td>
    <td class="cell" align="right">
        <asp:Button runat="server" Text="Sign Payload" ID="signPayLoadButton" 
            onclick="signPayLoadButton_Click" /></td>
  </tr>
  </tbody></table>
</div>

<br clear="all" />
<div align="center">
    <asp:Panel runat="server" ID="notaryPanel">
    </asp:Panel></div>
</form>

<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Virtual Mobile</div>
    <p>&#169; 2011 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>

</body></html>
