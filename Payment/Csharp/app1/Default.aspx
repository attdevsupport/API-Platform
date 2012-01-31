<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample Payment Application - Single Pay Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/ >
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

<h1>AT&T Sample Payment Application - Single Pay Application</h1>
<h2>Feature 1: Create New Transaction</h2><br/>

</div>
</div>
<form id="form1" runat="server">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
     <asp:RadioButtonList ID="Radio_TransactionProductType" runat="server" 
                            RepeatDirection="Vertical" Font-Names="Calibri" Font-Size="Small">
       <asp:ListItem Selected="True">Buy product 1 for $0.99</asp:ListItem>
       <asp:ListItem>Buy product 2 for $2.99m</asp:ListItem>
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
          <asp:Button runat="server" Text="Buy Product" ID="newTransactionButton" 
            onclick="newTransactionButton_Click" /></td>
  </tr>
  </tbody></table>
</div>
<br clear="all" />
<div align="center">
    <asp:Panel runat="server" ID="newTransactionPanel">
    </asp:Panel></div>
    <div class="successWide" id="transactionSuccessTable" runat="server" visible="False">
            <strong>SUCCESS:</strong><br/>
            <strong>Merchant Transaction ID:</strong>
                            <asp:Label ID="lbltranid" runat="server" Text="" />
                        <br/>
            <strong>Transaction Auth Code:</strong>
                            <asp:Label ID="lbltrancode" runat="server" Text="" />
                        <br/>
                        <br />
            <strong>   </strong>
                                          <asp:Button runat="server" Text="View Notary" 
                ID="viewNotaryButton" onclick="Unnamed1_Click"/>
                        <br/>
                        &nbsp;</div>

<!--
<div class="successWide">
<strong>SUCCESS:</strong><br />
<strong>Merchant Transaction ID</strong> user573transaction1377<br/>
<strong>Transaction Auth Code</strong> 66574834711<br /><br/>
<form name="getNotaryDetails" action="notary.jsp">
    <input type="submit" name="getNotaryDetails" value="View Notary Details" />
</form>
</div><br/>
-->
<br clear="all" />
<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Transaction Status</h2>

</div>
</div>

<div id="navigation" align="center">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell" align="left">
         <asp:RadioButtonList ID="Radio_TransactionStatus" runat="server" 
                            RepeatDirection="Vertical" Font-Names="Calibri" Font-Size="Small">
       <asp:ListItem ID="GetTransactionMerchantTransID" Selected="True">Merchant Transaction ID: </asp:ListItem>
       <asp:ListItem ID="GetTransactionAuthCode">Auth Code: </asp:ListItem>
       <asp:ListItem ID="GetTransactionTransID">Transaction ID: </asp:ListItem>
     </asp:RadioButtonList>
  </tr>
  </tbody></table>
</div>
<div id="extra">
  <table>
  <tbody>
  <tr>
  	<td><br /><br /><br />
          <asp:Button runat="server" Text="Get Transaction Status" 
            ID="getTransactionButton" onclick="getTransactionButton_Click" 
            /></td>
  </tr>
  </tbody></table>
</div>
<br clear="all" />

        <div class="successWide" id="tranGetStatusTable" runat="server" visible="False">
            <strong>SUCCESS:</strong><br/>
            <strong>Transaction ID:</strong>
                            <asp:Label ID="lblstatusTranId" runat="server" Text="" />
                        <br/>
            <strong>Merchant Transaction ID:</strong>
                            <asp:Label ID="lblstatusMerTranId" runat="server" Text="" />
        </div>
        <div align="center">
    <asp:Panel runat="server" ID="newTransactionStatusPanel">
    </asp:Panel></div>
<br clear="all" />

<div id="wrapper">
<div id="content">

<h2><br />Feature 3: Refund Transaction</h2>

</div>
</div>

<div id="navigation" align="center">
    <asp:Table id="refundTable" runat="server" width=750px cellpadding="1" cellspacing="1" border="0">
    </asp:Table>
</div>
<br clear="all" />
<div id="extra" >
    <asp:Button runat="server" Text="Refund Transaction" 
        onclick="Unnamed1_Click1" />
</div>
<br clear="all" />
<br clear="all" />
        <div class="successWide" id="refundSuccessTable" runat="server" visible="False">
        <strong>SUCCESS:</strong><br />
        <strong>Transaction ID </strong> <asp:Label ID="lbRefundTranID" runat="server" Text="" /><br />
        <strong>Transaction Status </strong> <asp:Label ID="lbRefundTranStatus" runat="server" Text="" /><br/>
        <strong>IsSuccess </strong> <asp:Label ID="lbRefundIsSuccess" runat="server" Text="" /><br/>
        <strong>Version </strong> <asp:Label ID="lbRefundVersion" runat="server" Text="" /><br/>
        </div>
        <div align="center">
    <asp:Panel runat="server" ID="refundPanel">
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


