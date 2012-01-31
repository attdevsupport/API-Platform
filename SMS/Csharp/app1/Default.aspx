<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample SMS Application &#8211; Basic SMS Service Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type"/>
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/>
    <style type="text/css">
        .style1
        {
            height: 87px;
        }
        .style2
        {
            font: 12px Arial, Sans-serif;
            height: 87px;
        }
        #form1
        {
            height: 317px;
        }
    </style>
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

    <form id="form1" runat="server">

<div id="wrapper">
<div id="content">

<h1>AT&T Sample SMS Application &#8211; Basic SMS Service Application</h1>
<h2>Feature 1: Send SMS</h2>

</div>
</div>
<div id="navigation">
<table border="0" width="40%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell">
        <asp:TextBox ID="txtmsisdn" runat="server" MaxLength="16"></asp:TextBox>
      </td>
  </tr>
  <tr>
    <td valign="top" class="label">Message:</td>
    <td class="cell">
        <asp:TextBox ID="txtmsg" runat="server" Height="87px" Width="387px" 
            TextMode="MultiLine"></asp:TextBox>
   </td>
   </tr>
  </tbody></table>

</div>
<div id="extra">
<table border="0">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="style1">
            &nbsp;</td>
    <td class="style2" align="left" valign="top">
        <asp:Label ID="smsApp1Label" runat="server" Height="69px" 
            Text="Note: All Messages will be sent from first short code of a registered application" 
            Width="263px" Font-Bold="True" Font-Names="Calibri"></asp:Label>
      </td>
  </tr>
  <tr>
    <td width="20%" valign="bottom">
            <asp:Button ID="Button2" runat="server" onclick="Button1_Click" 
            Text="Send SMS" /></td>
    <td class="cell">
    </td>
  </tr>
  </tbody></table>
  <table>
  <tbody>
  </tbody></table>

</div>
    

<br clear="all" />
<div align="center">
<asp:Panel ID="sendSMSPanel" runat="server" Font-Names="Calibri" Font-Size="XX-Small">
    </asp:Panel>
    </div>
<br clear="all" />

<div id="wrapper">
<div id="content">
<h2><br />
Feature 2: Get Delivery Status</h2>
</div>
</div>
<div id="navigation">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Message ID:</td>
    <td class="cell">
        <asp:TextBox ID="txtSmsId" runat="server" Width="160px"></asp:TextBox>
    </td>
  </tr>
  </tbody>
  </table>
</div>
<div id="extra">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell">
        <asp:Button ID="getDeliveryStatusButton" runat="server" 
            onclick="getDeliveryStatusButton_Click" Text="Get Status" Width="104px" />
    </td>
  </tr>
  </tbody>
  </table>
</div>
<br clear="all" />
<div align="center">
    <asp:Panel ID="getStatusPanel" runat="server" Font-Names="Calibri" Font-Size="XX-Small">
    </asp:Panel>
</div>
<br clear="all" />
<div id="wrapper">
<div id="content">
<h2><br />Feature 3: Get Received Messages</h2>
</div>
</div>
<div id="navigation">
<asp:Panel ID="receiveMessagePanel" runat="server" Font-Names="Calibri">
    </asp:Panel>
</div>
<br clear="all" />
<div align="center">
    <asp:Panel ID="getMessagePanel" runat="server" Font-Names="Calibri" 
        Font-Size="XX-Small">
    </asp:Panel>
</div>
<br clear="all" />
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Virtual Mobile</div>
    <p>© 2011 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>


</form>
</body></html>