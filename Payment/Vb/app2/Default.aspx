<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Default.aspx.vb" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample Payment Application - Subscription Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<body>

<div id="container">
<!-- open HEADER --><div id="header">

<div>
    <div id="hcRight">
        Thu Dec 08 00:14:15 UTC 2011
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
<form method="post" name="newSubscription" >
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="50%" valign="top" class="label">Subscribe for $1.99 per month:</td>
    <td class="cell"><input type="radio" name="product" value="1" checked>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top" class="label">Subscribe for $3.99 per month:</td>
    <td class="cell"><input type="radio" name="product" value="2">
	</td></tr>
  </tbody></table>

</div>
<div id="extra">

  <table>
  <tbody>
  <tr>
  	<td><br /><br /><button type="submit" name="newSubscription">Subscribe</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />
<div align="center"></div>
</form>

<div class="successWide">
<strong>SUCCESS:</strong><br />
<strong>Merchant Subscription ID</strong> subscription589<br/>
<strong>Subscription Auth Code</strong> 66574834711<br /><br/>
<form name="getNotaryDetails" action="notary.jsp">
    <input type="submit" name="getNotaryDetails" value="View Notary Details" />
</form>
</div><br/>

<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Subscription Status</h2>

</div>
</div>
<form method="post" name="getTransactionStatus" >
<div id="navigation" align="center">

<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th style="width: 150px" class="cell" align="right"></th>
        <th style="width: 100px" class="cell"></th>
        <th style="width: 240px" class="cell" align="left"></th>
    </tr>
</thead>
  <tbody>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="getTransactionType" value"1" checked /> Merchant Sub. ID:
    </td>
    <td></td>
    <td class="cell" align="left">subscription589</td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="getTransactionType" value"2" /> Auth Code:
    <td></td>
    <td class="cell" align="left">66574834711</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="getTransactionType" value"3" /> Subscription ID:
    <td></td>
    <td class="cell" align="left">trx84775818159911</td>
    </td>
  </tr>
  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button  type="submit" name="getSubscriptionStatus">Get Subscription Status</button>
    </td>
  </tr>
  </tbody></table>


</form>
</div>
<br clear="all" />

<div class="successWide">
<strong>SUCCESS:</strong><br />
<strong>Merchant Subscription ID</strong> subscription589<br/>
<strong>Subscription ID</strong> trx84775818159911<br />
<strong>Merchant Transaction ID</strong> user573transaction1377<br/>
</div><br/>
<div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th style="width: 100px" class="cell" align="right"><strong>Parameter</strong></th>
        <th style="width: 100px" class="cell"><strong></strong></th>
        <th style="width: 275px" class="cell" align="left"><strong>Value</strong></th>
	</tr>
</thead>
<tbody>
	<tr>
    	<td align="right" class="cell">Amount</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">0.99</td>
    </tr>
	<tr>
    	<td align="right" class="cell">Channel</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">MOBILE_WEB</td>
    </tr>
       <td align="right" class="cell">ConsumerId</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">74b8482944h1874</td>
    </tr>
       <td align="right" class="cell">ContentCategory</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">1</td>
    </tr>
       <td align="right" class="cell">Currency</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">USD</td>
    </tr>
       <td align="right" class="cell">Description</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">Product 1 by Merchant</td>
    </tr>
       <td align="right" class="cell">MerchantApplicationId</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">656471783856547181374641</td>
    </tr>
       <td align="right" class="cell">MerchantIdentifier</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">6578483h4g48</td>
    </tr>
       <td align="right" class="cell">MerchantProductId</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">MerchId58777</td>
    </tr>
       <td align="right" class="cell">IsSuccess</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">True</td>
    </tr>
       <td align="right" class="cell">TransactionStatus</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">Successful</td>
    </tr>
</tbody>
</table>
</div><br/>

<div id="wrapper">
<div id="content">

<h2><br />Feature 3: Get Subscription Details</h2>

</div>
</div>
<br/>
<form method="post" name="getSubscriptionDetails" >
<div id="navigation" align="center">

<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th style="width: 150px" class="cell" align="right"><strong>Consumer ID</strong></th>
        <th style="width: 100px" class="cell"></th>
        <th style="width: 240px" class="cell" align="left"><strong>Merchant Subscription ID</strong></th>
    <td><div class="warning">
<strong>WARNING:</strong><br />
You must use Get Subscription Status to get the Consumer ID before you can get details.
</div></td>
	</tr>
</thead>
  <tbody>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"1" checked /> 74b8482944h1874
    </td>
    <td></td>
    <td class="cell" align="left">subscription589</td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"2" /> 74b8482944h1872
    <td></td>
    <td class="cell" align="left">subscription588</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"3" /> 74b8482944h1870
    <td></td>
    <td class="cell" align="left">subscription587</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"4" /> 74b8482944h1854
    <td></td>
    <td class="cell" align="left">subscription586</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"5" /> 74b8482944h1829
    <td></td>
    <td class="cell" align="left">subscription585</td>
    </td>
  </tr>

  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button  type="submit" name="getSubscriptionDetails">Get Subscription Details</button>
    </td>
  </tr>
  </tbody></table>


</form>
</div>
<br clear="all" />


<div class="successWide">
<strong>SUCCESS:</strong><br />
<strong>Merchant Subscription ID</strong> subscription589<br/>
<strong>Subscription ID</strong> trx84775818159911<br />
<strong>Merchant Transaction ID</strong> user573transaction1377<br/>
</div><br/>
<div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th style="width: 100px" class="cell" align="right"><strong>Parameter</strong></th>
        <th style="width: 100px" class="cell"><strong></strong></th>
        <th style="width: 275px" class="cell" align="left"><strong>Value</strong></th>
    </tr>
</thead>
<tbody>
	<tr>
    	<td align="right" class="cell">creationDate</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">2011-06-13T16:11:16.000+0000</td>
    </tr>
	<tr>
    	<td align="right" class="cell">Currency</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">USD</td>
    </tr>
       <td align="right" class="cell">CurrentEndDate</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">2011-07-13T16:11:16.000+0000</td>
    </tr>
       <td align="right" class="cell">CurrentStartDate</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">2011-06-13T16:11:16.000+0000</td>
    </tr>
       <td align="right" class="cell">GrossAmount</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">0.05</td>
    </tr>
       <td align="right" class="cell">IsActiveSubscription</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">true</td>
    </tr>
       <td align="right" class="cell">IsSuccess</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">true</td>
    </tr>
       <td align="right" class="cell">Recurrences</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">99999</td>
    </tr>
       <td align="right" class="cell">RecurrencesLeft</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">2147483647</td>
    </tr>
       <td align="right" class="cell">Status</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">Active</td>
    </tr>
       <td align="right" class="cell">Version</td>
        <td align="center" class="cell"></td>
        <td align="left" class="cell">2</td>
    </tr>
</tbody>
</table>
</div><br/>

<div id="wrapper">
<div id="content">

<h2><br />Feature 4: Refund Subscription</h2>

</div>
</div>
<form method="post" name="refundSubscription" >
<div id="navigation" align="center">

<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th style="width: 150px" class="cell" align="right"><strong>Subscription ID</strong></th>
        <th style="width: 100px" class="cell"></th>
        <th style="width: 240px" class="cell" align="left"><strong>Merchant Subscription ID</strong></th>
    <td><div class="warning">
<strong>WARNING:</strong><br />
You must use Get Subscription Status to get the Subscription ID before you can refund it.
</div></td>
    </tr>
</thead>
  <tbody>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"1" checked /> trx84775818159911
    </td>
    <td></td>
    <td class="cell" align="left">subscription589</td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"2" /> trx84775818159910
    <td></td>
    <td class="cell" align="left">subscription588</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"3" /> trx84775818159909
    <td></td>
    <td class="cell" align="left">subscription587</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"4" /> trx84775818159908
    <td></td>
    <td class="cell" align="left">subscription586</td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="right">
        <input type="radio" name="trxId" value"5" /> trx84775818159907
    <td></td>
    <td class="cell" align="left">subscription585</td>
    </td>
  </tr>
  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button  type="submit" name="refundSubscription">Refund Subscription</button>
    </td>
  </tr>
  </tbody></table>


</form>
</div>
<br clear="all" />

<div class="successWide">
<strong>SUCCESS:</strong><br />
<strong>Transaction ID</strong> trx84775818159911<br />
<strong>Merchant Transaction ID</strong> user573transaction1377<br/>
</div><br/>

<div id="wrapper">
<div id="content">

<div id="wrapper">
<div id="content">

<h2><br />Feature 5: Notifications</h2>

</div>
</div>
<form method="post" name="refreshNotifications" >
<div id="navigation"><br/>

<div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
    	<th style="width: 100px" class="cell"><strong>Notification ID</strong></th>
        <th style="width: 100px" class="cell"><strong>Notification Type</strong></th>
        <th style="width: 125px" class="cell"><strong>Subscription ID</strong></th>
        <th style="width: 175px" class="cell"><strong>Merchant Subscription ID</strong></th>
	</tr>
</thead>
<tbody>
	<tr>
    	<td align="center" class="cell">7467-77481751-7744</td>
        <td align="center" class="cell">Refund</td>
        <td align="center" class="cell">trx84775818159911</td>
        <td align="center" class="cell">subscription589</td>
    </tr>
	<tr>
    	<td align="center" class="cell">7467-77481751-6154</td>
        <td align="center" class="cell">Stop Subscription</td>
        <td align="center" class="cell">trx65478195757195</td>
        <td align="center" class="cell">myId65758108283650</td>
    </tr>
       <td align="center" class="cell">7467-77481751-6478</td>
        <td align="center" class="cell">Cancel Subscription</td>
        <td align="center" class="cell">trx65478195757196</td>
        <td align="center" class="cell">myId65758108283649</td>
    </tr>
       <td align="center" class="cell">7467-77481751-0024</td>
        <td align="center" class="cell">Refund</td>
        <td align="center" class="cell">trx65478195756123</td>
        <td align="center" class="cell">myId65758108283648</td>
    </tr>
       <td align="center" class="cell">7467-77481751-9612</td>
        <td align="center" class="cell">Stop Subscription</td>
        <td align="center" class="cell">trx65478195751890</td>
        <td align="center" class="cell">myId65758108283647</td>
    </tr>
</tbody>
</table>
</div>
<div id="extra"><br/>

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="refreshNotifications">Refresh</button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
</form></div>

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
