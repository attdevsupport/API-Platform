<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample Payment Application - Subscription Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/ >
<body>

<div id="container">
<!-- open HEADER --><div id="header">

<div>
    <div id="hcRight">
        <asp:Label runat="server" Text="Label" ID="lblServerTime"></asp:Label>
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

<h1>AT&T Sample Payment Application - Subscription Application</h1>
<h2>Feature 1: Create New Subscription</h2><br/>

</div>
</div>
<form id="form1" runat="server">
<div id="navigation">
<table border="0" width="100%">
  <tbody>
  <tr>
     <asp:RadioButtonList ID="Radio_SubscriptionProductType" runat="server" 
                            RepeatDirection="Vertical" Font-Names="Calibri" Font-Size="Small">
       <asp:ListItem Selected="True">Subscribe for $1.99 per month</asp:ListItem>
       <asp:ListItem>Subscribe for $3.99 per month</asp:ListItem>
     </asp:RadioButtonList>
    </td>
  </tr>
  </tbody></table>
</div>
<div id="extra">
  <table>
  <tbody>
  <tr>
  	<td><br /><br />
          <asp:Button runat="server" Text="Subscribe" ID="newSubscriptionButton" 
            onclick="newSubscriptionButton_Click1"/></td>
  </tr>
  </tbody></table>
</div>
<br clear="all" />
<div align="center">
    <asp:Panel runat="server" ID="newSubscriptionPanel">
    </asp:Panel></div>
    <div class="successWide" id="subscriptionSuccessTable" runat="server" visible="False">
            <strong>SUCCESS:</strong><br/>
            <strong>Merchant Transaction ID:</strong>
                    <asp:Label ID="lblsubsid" runat="server" Text="" />
                        <br/>
            <strong>Transaction Auth Code:</strong>
                    <asp:Label ID="lblsubscode" runat="server" Text="" />
                        <br/>
                        <br />
            <strong></strong>
                    <asp:Button runat="server" Text="View Notary" ID="viewNotaryButton" 
                onclick="viewNotaryButton_Click"/>
                        <br/>
                        &nbsp;</div>
<br clear="all" />
<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Subscription Status</h2>

</div>
</div>
<div id="navigation" align="center">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell" align="left">
         <asp:RadioButtonList ID="Radio_SubscriptionStatus" runat="server" 
                            RepeatDirection="Vertical" Font-Names="Calibri" Font-Size="Small">
       <asp:ListItem ID="GetSubscriptionMerchantSubsID" Selected="True">Merchant Sub. ID: </asp:ListItem>
       <asp:ListItem ID="GetSubscriptionAuthCode">Auth Code: </asp:ListItem>
       <asp:ListItem ID="GetSubscriptionID">Subscription ID: </asp:ListItem>
     </asp:RadioButtonList>
  </tr>
  </tbody></table>
  </div>
<div id="extra">
  <table>
  <tbody>
  <tr>
  	<td><br /><br /><br />
          <asp:Button runat="server" Text="Get Subscription Status" 
            ID="getSubscriptionButton" onclick="getSubscriptionButton_Click"/></td>
  </tr>
  </tbody></table>
</div>
<br clear="all" />
        <div class="successWide" id="subsGetStatusTable" runat="server" visible="False">
            <strong>SUCCESS:</strong><br/>
            <strong>Subscription ID:</strong>
                            <asp:Label ID="lblstatusSubsId" runat="server" Text="" />
                        <br/>
            <strong>Merchant Subscription ID:</strong>
                            <asp:Label ID="lblstatusMerSubsId" runat="server" Text="" />
        </div>
        <div align="center">
    <asp:Panel runat="server" ID="getSubscriptionStatusPanel">
    </asp:Panel></div>
<br clear="all" />

<div id="wrapper">
<div id="content">

<h2><br />Feature 3: Get Subscription Details</h2>

</div>
</div>

<div id="navigation" align="center">
    <asp:Table id="subsDetailsTable" runat="server" width=750px cellpadding="1" cellspacing="1" border="0">
    </asp:Table>
</div>
<br clear="all" />
<div id="extra" >
    <asp:Button runat="server" Text="Get Subscription Details" 
        ID="btnGetSubscriptionDetails" onclick="btnGetSubscriptionDetails_Click"/>
</div>
<br clear="all" />
<br clear="all" />
        <div class="successWide" id="subsDetailsSuccessTable" runat="server" visible="False">
        <strong>SUCCESS:</strong><br />
        <strong>Consumer ID </strong> <asp:Label ID="lblConsId" runat="server" Text="" /><br />
        <strong>Merchant Subscription ID </strong> <asp:Label ID="lblMerSubId" runat="server" Text="" /><br/>
        </div>
        <div align="center">
    <asp:Panel runat="server" ID="subsDetailsPanel">
    </asp:Panel></div>
<br/>
<br clear="all" />

<div id="wrapper">
<div id="content">

<h2><br />Feature 4: Refund Subscription</h2>

</div>
</div>

<div id="navigation" align="center">
    <asp:Table id="subsRefundTable" runat="server" width=750px cellpadding="1" cellspacing="1" border="0">
    </asp:Table>
</div>
<br clear="all" />
<div id="extra" >
    <asp:Button runat="server" Text="Refund" 
        ID="btnGetSubscriptionRefund" onclick="btnGetSubscriptionRefund_Click"/>
</div>
<br clear="all" />
<br clear="all" />
        <div class="successWide" id="subsRefundSuccessTable" runat="server" visible="False">
        <strong>SUCCESS:</strong><br />
        <strong>Transaction ID </strong> <asp:Label ID="lbRefundTranID" runat="server" Text="" /><br />
        <strong>Transaction Status </strong> <asp:Label ID="lbRefundTranStatus" runat="server" Text="" /><br/>
        <strong>IsSuccess </strong> <asp:Label ID="lbRefundIsSuccess" runat="server" Text="" /><br/>
        <strong>Version </strong> <asp:Label ID="lbRefundVersion" runat="server" Text="" /><br/>
        </div>
        <div align="center">
    <asp:Panel runat="server" ID="subsRefundPanel">
    </asp:Panel></div>
<br/>
<br clear="all" />
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
</form>
</body></html>

