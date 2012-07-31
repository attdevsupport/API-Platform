<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
<title>AT&T Sample Payment Application - Subscription
	Application</title>
<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
<link rel="stylesheet" type="text/css" href="style/common.css"/ >
<script type="text/javascript" src="js/helper.js">

</script>
<head>
<script type="text/javascript">
	var _gaq = _gaq || [];
	_gaq.push([ '_setAccount', 'UA-28378273-1' ]);
	_gaq.push([ '_trackPageview' ]);

	(function() {
		var ga = document.createElement('script');
		ga.type = 'text/javascript';
		ga.async = true;
		ga.src = ('https:' == document.location.protocol ? 'https://ssl'
				: 'http://www')
				+ '.google-analytics.com/ga.js';
		var s = document.getElementsByTagName('script')[0];
		s.parentNode.insertBefore(ga, s);
	})();
</script>
</head>
<body>

	<%@ page contentType="text/html; charset=iso-8859-1" language="java"%>
	<%@ page import="org.apache.commons.httpclient.*"%>
	<%@ page import="org.apache.commons.httpclient.methods.*"%>
	<%@ page import="org.json.JSONObject"%>
	<%@ page import="org.json.JSONArray"%>
	<%@ page import="java.io.*"%>
	<%@ page import="java.util.Random"%>
	<%@ include file="getToken.jsp"%>
	<%@ page import="java.util.Arrays"%><%@ page
		import="java.util.Collections"%><%@ page
		import="java.util.Comparator"%>
	<%

String newSubscription = request.getParameter("newSubscription");
String getSubscriptionStatus = request.getParameter("getSubscriptionStatus");
String getSubscriptionDetails = request.getParameter("getSubscriptionDetails");
String refundSubscription = request.getParameter("refundSubscription");
String refundReasonText = "User did not like product";
String refreshNotifications = request.getParameter("refreshNotifications");

String trxId = (String) session.getAttribute("trxId");
if(trxId==null || trxId.equalsIgnoreCase("null"))
    trxId = "";
String subscriptionId = request.getParameter("subscriptionId");
if(subscriptionId==null || subscriptionId.equalsIgnoreCase("null"))
    subscriptionId = "";

String notificationId = request.getParameter("notificationId");
if(notificationId==null || notificationId.equalsIgnoreCase("null"))
    notificationId = "";

String merchantTrxId = request.getParameter("merchantTrxId");
if(merchantTrxId==null || merchantTrxId.equalsIgnoreCase("null"))
    merchantTrxId = (String) session.getAttribute("merchantTrxId");
if(merchantTrxId==null || merchantTrxId.equalsIgnoreCase("null"))
    merchantTrxId = "";
    
String MerchantSubscriptionIdList = request.getParameter("MerchantSubscriptionIdList");
if(MerchantSubscriptionIdList==null || MerchantSubscriptionIdList.equalsIgnoreCase("null"))
    MerchantSubscriptionIdList = (String) session.getAttribute("MerchantSubscriptionIdList");
if(MerchantSubscriptionIdList==null || MerchantSubscriptionIdList.equalsIgnoreCase("null"))
    MerchantSubscriptionIdList = "";
    
String merchantSubscriptionId = request.getParameter("merchantSubscriptionId");
if(merchantSubscriptionId==null || merchantSubscriptionId.equalsIgnoreCase("null"))
    merchantSubscriptionId = (String) session.getAttribute("merchantSubscriptionId");
if(merchantSubscriptionId==null || merchantSubscriptionId.equalsIgnoreCase("null"))
    merchantSubscriptionId = "";    
    
String authCode = request.getParameter("SubscriptionAuthCode");
if(authCode==null || authCode.equalsIgnoreCase("null"))
    authCode = (String) session.getAttribute("authCode");
if(authCode==null || authCode.equalsIgnoreCase("null"))
    authCode = "";
String consumerId = request.getParameter("consumerId");
if(consumerId==null || consumerId.equalsIgnoreCase("null"))
    consumerId = (String) session.getAttribute("consumerId");
if(consumerId==null || consumerId.equalsIgnoreCase("null"))
    consumerId = "";
    
Random randomGenerator = new Random();
int product = 0;
if(request.getParameter("product")!=null)
    product = Integer.parseInt(request.getParameter("product"));
String amount = "";
String description = "";
String merchantProductId = "";
String trxIdGetDetails = "";
if(product==1) {
    amount = "1.99";
    description = "Word Game Subscription 1";
    merchantProductId = "WordGameSubscription1";
} else if(product==2) {
    amount = "3.99";
    description = "Number Game Subscription 2";
    merchantProductId = "NumberGameSubscription1";
}
%>

	<div id="container">
		<!-- open HEADER -->
		<div id="header">

			<div>
				<div id="hcRight">
					<%=new java.util.Date()%>
				</div>
				<div id="hcLeft">Server Time:</div>
			</div>
			<div>
				<div id="hcRight">
					<script language="JavaScript" type="text/javascript">
						var myDate = new Date();
						document.write(myDate);
					</script>
				</div>
				<div id="hcLeft">Client Time:</div>
			</div>
			<div>
				<div id="hcRight">
					<script language="JavaScript" type="text/javascript">
						document.write("" + navigator.userAgent);
					</script>
				</div>
				<div id="hcLeft">User Agent:</div>
			</div>
			<br clear="all" />
		</div>
		<!-- close HEADER -->

		<div id="wrapper">
			<div id="content">

				<h1>AT&T Sample Payment Application - Subscription Application</h1>
				<h2>Feature 1: Create New Subscription</h2>
				<br />

			</div>
		</div>
		<form method="post" name="newSubscription">
			<div id="navigation">

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td width="50%" valign="top" class="label"><input
								type="radio" name="product" value="1" checked> Subscribe
								for $1.99 per month:</td>
							</td>
						</tr>
						<tr>
							<td width="50%" valign="top" class="label"><input
								type="radio" name="product" value="2"> Subscribe for
								$3.99 per month:</td>
							</td>
						</tr>
					</tbody>
				</table>

			</div>
			<div id="extra">

				<table>
					<tbody>
						<tr>
							<td><br />
							<br />
							<button type="submit" name="newSubscription">Subscribe</button>
							</td>
						</tr>
					</tbody>
				</table>


			</div>
			<br clear="all" />
			<div align="center"></div>
		</form>

<% 
if(newSubscription!=null) { 
merchantTrxId = "user" + randomGenerator.nextInt(100000) + "subscription" + randomGenerator.nextInt(1000000);
session.setAttribute("merchantTrxId", merchantTrxId);
session.setAttribute("trxId", null);
session.setAttribute("authCode", null);
session.setAttribute("consumerId", null);

MerchantSubscriptionIdList = "SUBLIST" + randomGenerator.nextInt(100000);
session.setAttribute("MerchantSubscriptionIdList", MerchantSubscriptionIdList);

PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/transactionData/" + merchantTrxId + ".txt"))), false);
String toSave = trxId + "\n" + merchantTrxId + "\n" + authCode;
outWrite.write(toSave);
outWrite.close();

String forNotary = "notary.jsp?signPayload=true&return=subscription.jsp&payload="+
"{\"Amount\":" + amount + ","+
"\"Category\":1, \"Channel\":\"MOBILE_WEB\","+
"\"Description\":\"" + description + "\","+
"\"MerchantTransactionId\":\"" + merchantTrxId + "\","+
"\"MerchantProductId\":\"" + merchantProductId + "\","+
"\"MerchantPaymentRedirectUrl\":\"" + subscriptionRedirect + "\","+
"\"MerchantSubscriptionIdList\":\"" + MerchantSubscriptionIdList + "\","+
"\"IsPurchaseOnNoActiveSubscription\":\"false\","+
"\"SubscriptionRecurrences\":99999,"+
"\"SubscriptionPeriod\":\"MONTHLY\","+
"\"SubscriptionPeriodAmount\":1}";
response.sendRedirect(forNotary);
	
}

 %>

		<%
if(request.getParameter("SubscriptionAuthCode")!=null) { 
		
	PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/transactionData/" + merchantTrxId + ".txt"))), false);
	String toSave = trxId + "\n" + merchantTrxId + "\n" + authCode;
	outWrite.write(toSave);
	outWrite.close();    
	session.setAttribute("authCode", authCode);

%>
		<div class="successWide">
			<strong>SUCCESS:</strong><br /> <strong>Merchant
				Subscription ID</strong>
			<%=(String) session.getAttribute("merchantTrxId")%><br /> <strong>Subscription
				Auth Code</strong>
			<%=authCode%><br />
			<br />
			<form name="getNotaryDetails" action="notary.jsp">
				<input type="submit" name="getNotaryDetails"
					value="View Notary Details" />
			</form>
		</div>
		<br />
		<% } %>

		<%
		
		
if(request.getParameter("signedPayload")!=null && request.getParameter("signature")!=null){
    response.sendRedirect(FQDN + "/rest/3/Commerce/Payment/Subscriptions?clientid=" + clientIdAut + "&SignedPaymentDetail=" + request.getParameter("signedPayload") + "&Signature=" + request.getParameter("signature"));
	System.out.println("BODY1: " + response);
	}
	
if(request.getParameter("getTransactionType") != null)
	{
		int getTransactionType = Integer.parseInt(request.getParameter("getTransactionType"));
		String url = "";
		if(getTransactionType==1)
			url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/MerchantTransactionId/" + merchantTrxId;   
		if(getTransactionType==2)
			url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionAuthCode/" + authCode;
		if(getTransactionType==3)
			url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionId/" + trxId;
		HttpClient client = new HttpClient();
		GetMethod method = new GetMethod(url);  
		method.addRequestHeader("Authorization","Bearer " + accessToken);
		method.addRequestHeader("Accept","application/json");
		int statusCode = client.executeMethod(method);   
		JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
		trxId = jsonResponse.getString("SubscriptionId");
		session.setAttribute("trxId", trxId);
	}	
%>

		<div id="wrapper">
			<div id="content">

				<h2>
					<br /> Feature 2: Get Subscription Status
				</h2>

			</div>
		</div>
		<form method="post" name="getSubscriptionStatus"
			action="subscription.jsp">
			<div id="navigation" align="center">

				<table style="width: 750px" cellpadding="1" cellspacing="1"
					border="0">
					<thead>
						<tr>
							<th style="width: 150px" class="cell" align="right"></th>
							<th style="width: 100px" class="cell"></th>
							<th style="width: 240px" class="cell" align="left"></th>
						</tr>
					</thead>
					<tbody>
						<tr>
							<td class="cell" align="left"><input type="radio"
								name="getTransactionType" value="1" checked /> Merchant
								Subscription ID:</td>
							<td></td>
							<td class="cell" align="left"><%=merchantTrxId%></td>
						</tr>
						<tr>
							<td class="cell" align="left"><input type="radio"
								name="getTransactionType" value="2" /> Auth Code:
							<td></td>
							<td class="cell" align="left"><%=authCode%></td>
							</td>
						</tr>

						 <% if(!trxId.equalsIgnoreCase("")) { %>
						<tr>
						   <td class="cell" align="left"><input type="radio"
							name="getTransactionType" value="3" /> Subscription ID:
						   <td></td>
						   <td class="cell" align="left"><%=trxId%></td>
						   </td>
						  </tr>
						  <% } %>

						<tr>
							<td></td>
							<td></td>
							<td></td>
							<td class="cell"><button type="submit"
									name="getSubscriptionStatus">Get Subscription Status</button></td>
						</tr>
					</tbody>
				</table>
		</form>
	</div>
	<br clear="all" />

	<% 

if(getSubscriptionStatus!=null) { 
int getTransactionType = Integer.parseInt(request.getParameter("getTransactionType"));
String url = "";
if(getTransactionType==1)
    url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/MerchantTransactionId/" + merchantTrxId;   
if(getTransactionType==2)
    url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionAuthCode/" + authCode;
if(getTransactionType==3)
    url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionId/" + trxId;
HttpClient client = new HttpClient();
GetMethod method = new GetMethod(url);  
method.addRequestHeader("Authorization","Bearer " + accessToken);
method.addRequestHeader("Accept","application/json");
int statusCode = client.executeMethod(method);  
  
if(statusCode==200) {
    JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
    trxId = jsonResponse.getString("SubscriptionId");
    session.setAttribute("trxId", trxId);
    consumerId = jsonResponse.getString("ConsumerId");
    session.setAttribute("consumerId", consumerId);
    merchantTrxId = jsonResponse.getString("MerchantTransactionId");
    session.setAttribute("merchantTrxId", merchantTrxId);
    merchantSubscriptionId = jsonResponse.getString("MerchantSubscriptionId");
    session.setAttribute("merchantSubscriptionId", merchantSubscriptionId);
    JSONArray parameters = jsonResponse.names();
    PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/transactionData/" + merchantTrxId + ".txt"))), false);
    String toSave = trxId + "\n" + merchantTrxId + "\n" + authCode + "\n" + consumerId + "\n" + MerchantSubscriptionIdList;
    outWrite.write(toSave);
    outWrite.close();
    %>
	<div class="successWide">
		<strong>SUCCESS</strong><br />

	</div>
	<br />
	<div align="center">
		<table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
			<thead>
				<tr>
					<th style="width: 100px" class="cell" align="right"><strong>Parameter</strong>
					</th>
					<th style="width: 100px" class="cell"><strong></strong>
					</th>
					<th style="width: 275px" class="cell" align="left"><strong>Value</strong>
					</th>
				</tr>
			</thead>
			<tbody>
				<% for(int i=0; i<parameters.length(); i++) { %>
				<tr>
					<td align="right" class="cell"><%=parameters.getString(i)%></td>
					<td align="center" class="cell"></td>
					<td align="left" class="cell"><%=jsonResponse.getString(parameters.getString(i))%></td>
				</tr>
				<% } %>
			</tbody>
		</table>
	</div>
	<br />
	<%
} else {
%>
	<div class="errorWide">
		<strong>ERROR:</strong><br />
		<%=method.getResponseBodyAsString()%>
	</div>
	<br />
	<%
}
method.releaseConnection();
} %>

	<div id="wrapper">
		<div id="content">

			<h2>
				<br />Feature 3: Get Subscription Details
			</h2>

		</div>
	</div>
	<br />
	<form method="post" name="getSubscriptionDetails">
		<div id="navigation" align="center">

			<table style="width: 900px" cellpadding="1" cellspacing="1"
				border="0">
				<thead>
					<tr>
						<th style="width: 150px" class="cell" align="right"><strong>Consumer
								ID</strong>
						</th>
						<th style="width: 50px" class="cell"></th>
						<th style="width: 240px" class="cell" align="left"><strong>Merchant
								Subscription ID</strong>
						</th>
						<td><div class="warning">
								<strong>WARNING:</strong><br /> You must use Get Subscription
								Status to get the Consumer ID before you can get details.
							</div>
						</td>
					</tr>
				</thead>
				<tbody>
					<%
if(true) {
    String url = request.getRequestURL().toString().substring(0,request.getRequestURL().toString().lastIndexOf("/")) + "/getLatestTransactions.jsp";
    HttpClient client = new HttpClient();
    GetMethod method = new GetMethod(url);  
    int statusCode = client.executeMethod(method); 
    if(statusCode==200) {
        JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
        JSONArray transactionList = new JSONArray(jsonResponse.getString("transactionList"));
        for(int i=0; i<transactionList.length(); i++) {
            if(transactionList.length()>0) {
                JSONObject transaction = new JSONObject(transactionList.getString(i));
                if(consumerId.equalsIgnoreCase(transaction.getString("consumerId")))
                    trxIdGetDetails = transaction.getString("merchantTransactionId");
                if(i==0) {
                    %>
					<tr>
						<td class="cell" align="right">
							<% if(transaction.getString("consumerId").length() > 5)
						{%> <input type="radio" name="trxIdGetDetails"
							value="<%=transaction.getString("consumerId")%>" checked /><%=transaction.getString("consumerId")%>
						</td>
						<td></td>
						<td class="cell" align="left"><%=transaction.getString("MerchantSubscriptionIdList")%></td>
					</tr>
					<% } %>
					<%
                } else {
                    %>
					<tr>
						<td class="cell" align="right">
							<% if(transaction.getString("consumerId").length() > 5)
						{%> <input type="radio" name="trxIdGetDetails"
							value="<%=transaction.getString("consumerId")%>" /><%=transaction.getString("consumerId")%>
						</td>
						<td></td>
						<td class="cell" align="left"><%=transaction.getString("MerchantSubscriptionIdList")%></td>
					</tr>
					<% } %>
					<%
                }
            }
        }
        method.releaseConnection();
    }
}
%>

					<tr>
						<td></td>
						<td></td>
						<td></td>
						<td class="cell"><button type="submit"
								name="getSubscriptionDetails">Get Subscription Details</button>
						</td>
					</tr>
				</tbody>
			</table>
	</form>
	</div>
	<br clear="all" />


	<% if(getSubscriptionDetails!=null) { %>
	<%
    String url = FQDN + "/rest/3/Commerce/Payment/Subscriptions/" + MerchantSubscriptionIdList + "/Detail/" + consumerId;  
    HttpClient client = new HttpClient();
    GetMethod method = new GetMethod(url);  
    method.addRequestHeader("Authorization","Bearer " + accessToken);
    method.addRequestHeader("Accept","application/json");
    int statusCode = client.executeMethod(method);
    if(statusCode==200) {
    JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
    JSONArray parameters = jsonResponse.names();
    %>
	<div class="successWide">
		<strong>SUCCESS</strong><br />

	</div>
	<br />
	<div align="center">
		<table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
			<thead>
				<tr>
					<th style="width: 100px" class="cell" align="right"><strong>Parameter</strong>
					</th>
					<th style="width: 100px" class="cell"><strong></strong>
					</th>
					<th style="width: 275px" class="cell" align="left"><strong>Value</strong>
					</th>
				</tr>
			</thead>
			<tbody>
				<% for(int i=0; i<parameters.length(); i++) { %>
				<tr>
					<td align="right" class="cell"><%=parameters.getString(i)%></td>
					<td align="center" class="cell"></td>
					<td align="left" class="cell"><%=jsonResponse.getString(parameters.getString(i))%></td>
				</tr>
				<% } %>
			</tbody>
		</table>
	</div>
	<br />
	<%
  } else {
  	%>
	<div class="errorWide">
		<strong>ERROR:</strong><br />
		<%=method.getResponseBodyAsString()%>
	</div>
	<br />
	<%
  }
   method.releaseConnection();
}
%>

	<div id="wrapper">
		<div id="content">

			<h2>
				<br />Feature 4: Refund Subscription
			</h2>

		</div>
	</div>
	<form method="post" name="refundSubscription" action="subscription.jsp">
		<div id="navigation" align="center">

			<table style="width: 750px" cellpadding="1" cellspacing="1"
				border="0">
				<thead>
					<tr>
						<th style="width: 150px" class="cell" align="right"><strong>Subscription
								ID</strong>
						</th>
						<th style="width: 100px" class="cell"></th>
						<th style="width: 240px" class="cell" align="left"><strong>Merchant
								Subscription ID</strong>
						</th>
						<td><div class="warning">
								<strong>WARNING:</strong><br /> You must use Get Subscription
								Status to get the Subscription ID before you can refund it.
							</div>
						</td>
					</tr>
				</thead>
				<tbody>
					<%
if(true) {
    String url = request.getRequestURL().toString().substring(0,request.getRequestURL().toString().lastIndexOf("/")) + "/getLatestTransactions.jsp";
    HttpClient client = new HttpClient();
    GetMethod method = new GetMethod(url);  
    int statusCode = client.executeMethod(method); 
    if(statusCode==200) {
        JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
        JSONArray transactionList = new JSONArray(jsonResponse.getString("transactionList"));
        for(int i=0; i<transactionList.length(); i++) {
            if(transactionList.length()>0) {
                JSONObject transaction = new JSONObject(transactionList.getString(i));
                if(i==0) {
                    %>
					<tr>
						<td class="cell" align="right">
							<% if(transaction.getString("transactionId").length() > 5)
							{ %> <input type="radio" name="trxIdRefund"
							value="<%=transaction.getString("transactionId")%>" checked /><%=transaction.getString("transactionId")%>
						</td>
						<td></td>
						<td class="cell" align="left"><%=transaction.getString("MerchantSubscriptionIdList")%></td>
					</tr>
					<% } %>
					<%
                } else {
                    %>
					<tr>
						<td class="cell" align="right">
							<% if(transaction.getString("transactionId").length() > 5)
							{ %> <input type="radio" name="trxIdRefund"
							value="<%=transaction.getString("transactionId")%>" /><%=transaction.getString("transactionId")%>
						</td>
						<td></td>
						<td class="cell" align="left"><%=transaction.getString("MerchantSubscriptionIdList")%></td>
					</tr>
					<% } %>
					<%
                }
            }
        }
        method.releaseConnection();
    }
}
%>

					<tr>
						<td></td>
						<td></td>
						<td></td>
						<td class="cell"><button type="submit"
								name="refundSubscription">Refund Subscription</button></td>
					</tr>
				</tbody>
			</table>
	</form>
	</div>
	<br clear="all" />


	<% if(refundSubscription!=null) { 
	trxId = request.getParameter("trxIdRefund"); // Gets value for the checked radio button
    String url = FQDN + "/rest/3/Commerce/Payment/Transactions/" + trxId;  
    HttpClient client = new HttpClient();
    PutMethod method = new PutMethod(url);  
    method.addRequestHeader("Authorization", "Bearer " + accessToken);
    method.addRequestHeader("Content-Type","application/json");
    method.addRequestHeader("Accept","application/json");
    JSONObject bodyObject = new JSONObject();
    String reasonCode = "1";
	String status = "Refunded";
	bodyObject.put("TransactionOperationStatus",status);
    bodyObject.put("RefundReasonCode",Double.parseDouble(reasonCode));
    bodyObject.put("RefundReasonText",refundReasonText);
    method.setRequestBody(bodyObject.toString()); 
    int statusCode = client.executeMethod(method);	
    if(statusCode ==200) {
        JSONObject rpcObject = new JSONObject(method.getResponseBodyAsString());
		JSONArray refundParameters = rpcObject.names();
		%>
	<div class="successWide">
		<strong>SUCCESS</strong><br />

	</div>
	<br />
	<div align="center">
		<table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
			<thead>
				<tr>
					<th style="width: 100px" class="cell" align="right"><strong>Parameter</strong>
					</th>
					<th style="width: 100px" class="cell"><strong></strong>
					</th>
					<th style="width: 275px" class="cell" align="left"><strong>Value</strong>
					</th>
				</tr>
			</thead>
			<tbody>
				<% for(int i=0; i<refundParameters.length(); i++) { %>
				<tr>
					<td align="right" class="cell"><%=refundParameters.getString(i)%></td>
					<td align="center" class="cell"></td>
					<td align="left" class="cell"><%=rpcObject.getString(refundParameters.getString(i))%></td>
				</tr>
				<% } %>
			</tbody>
		</table>
	</div>
	<br />
	<%
  } else {
  	%>
	<div class="errorWide">
		<strong>ERROR:</strong><br />
		<%=method.getResponseBodyAsString()%>
	</div>
	<br />
	<%
  }
   method.releaseConnection();
}
%>

	<div id="wrapper">
		<div id="content">

			<h2>
				<br />Feature 5: Notifications
			</h2>
		</div>
	</div>
	<form method="post" name="refreshNotifications"
		action="subscription.jsp">
		<div id="navigation" align="center">

			<table style="width: 750px" cellpadding="1" cellspacing="1"
				border="0">
				<thead>
					<tr>
						<th class="cell" align="left"><strong>Notification
								ID</strong>
						</th>

						<th class="cell" align="left"><strong>Notification
								Type</strong>
						</th>
						<th style="width: 100px" class="cell"></th>
						<th class="cell" align="left"><strong>Transaction ID</strong>
						</th>

						</td>
					</tr>
				</thead>
				<tbody>
					<%
if(true) 
{
				File directory = new File(application.getRealPath("/CallBack/")); 
				File[] files = directory.listFiles();
				Arrays.sort(files, new Comparator<File>(){
					public int compare(File f1, File f2)
					{
						return Long.valueOf(f1.lastModified()).compareTo(f2.lastModified());
					} });
				Collections.reverse(Arrays.asList(files));

				if(directory.listFiles().length>0) {
					int i = 0;

					String notID="";
					String notType="";
					String transID="";

					for(File callbackFile : files){  
						  String callbackFileName = callbackFile.getName(); 
							RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("CallBack/" + callbackFileName),"r");
							notID = (inFile1.readLine()).trim(); 
							notType=(inFile1.readLine()).trim();
							transID=(inFile1.readLine()).trim();

							inFile1.close();			
							i += 1;	
							 %>
					<tr>
						<td class="cell" align="left"><%=notID%></td>
						<td class="cell" align="left"><%=notType%></td>

						</td>
						<td></td>
						<td class="cell" align="left"><%=transID%></td>
					</tr>

					<%

					    if(i==5 )			//Print latest 5 transactions
						    break;
					}
					%>
					<tr>
						<td></td>
						<td></td>
						<td></td>
						<td class="cell"><button type="submit"
								name="refreshNotifications" value="refreshNotifications">Refresh</button>
						</td
					</tr>
				</tbody>
			</table>
	</form>
	</div>
	<br clear="all" />
	</form>
	</div>

	<% 
				}

String notificationType = "";
String transactionId = "";

		String url1 = request.getRequestURL().toString().substring(0,request.getRequestURL().toString().lastIndexOf("/")) + "/getLatestNotifications.jsp";
		HttpClient client1 = new HttpClient();
		GetMethod method1 = new GetMethod(url1);  
		int statusCode2 = client1.executeMethod(method1); 
		JSONObject jsonResponse1 = new JSONObject(method1.getResponseBodyAsString());
		JSONArray notificationList = new JSONArray(jsonResponse1.getString("notificationList"));
		String totalNumberOfNotifications = jsonResponse1.getString("totalNumberOfNotifications");
        System.out.println("method1.getResponseBodyAsString()"+method1.getResponseBodyAsString());
      
        method1.releaseConnection();


	String not = "";
    if(refreshNotifications!=null) {
            
			for(int i=0; i<notificationList.length(); i++) 
            {
            	JSONObject notification = new JSONObject(notificationList.getString(i));
            	
            	notificationId = notification.getString("notificationId");
                not = notificationId.trim();			
            	
				String url = FQDN + "/rest/3/Commerce/Payment/Notifications/" + not;		//Builds the Get Request
                HttpClient clients = new HttpClient();
				GetMethod methods = new GetMethod(url);
                System.out.println("accessToken"+accessToken);
            	methods.addRequestHeader("Authorization", "Bearer " + accessToken);
                methods.addRequestHeader("Accept", "application/json");
        		int statusCode = clients.executeMethod(methods);
                 
 
                if (statusCode  == 200 || statusCode == 201)
                {
					 JSONObject jsonResponse = new JSONObject(methods.getResponseBodyAsString());
					 JSONObject getNotificationResponse = jsonResponse.getJSONObject("GetNotificationResponse");

					 String noType = getNotificationResponse.getString("NotificationType");
					 String originalTrxId = getNotificationResponse.getString("OriginalTransactionId");

					int random = (int)(Math.random()*10000000);
					PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/CallBack/" + random + ".txt"))), false);		//Print Successfull notification details to a file.
					outWrite.println(not);
					outWrite.println(noType);
					outWrite.println(originalTrxId);

					outWrite.close();

					HttpClient client2 = new HttpClient(); 		//Acknowledge the notification
					PutMethod method2 = new PutMethod(url);
					method2.addRequestHeader("Authorization", "Bearer " + accessToken);
					method2.addRequestHeader("Content-Type", "application/json");
					method2.addRequestHeader("Accept", "application/json"); 
					int statusCode3 = client2.executeMethod(method2);

					if(statusCode3==200 || statusCode3==201) 
					System.out.println("Acknowlegement success ");

					methods.releaseConnection();    
					method2.releaseConnection();
				}
                     			
			}	
    }

}

%>

	<div id="footer">

		<div
			style="float: right; width: 20%; font-size: 9px; text-align: right">Powered
			by AT&amp;T Cloud Architecture</div>
		<p>
			&#169; 2012 AT&amp;T Intellectual Property. All rights reserved. <a
				href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
			<br> The Application hosted on this site are working examples
			intended to be used for reference in creating products to consume
			AT&amp;T Services and not meant to be used as part of your product.
			The data in these pages is for test purposes only and intended only
			for use as a reference in how the services perform. <br> For
			download of tools and documentation, please go to <a
				href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>