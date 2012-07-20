<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&amp;T Sample SMS Application - Basic SMS Service Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
</script>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.JSONObject"%>
<%@ page import="org.json.JSONArray"%>
<%@ page import="org.w3c.dom.*" %>
<%@ page import="javax.xml.parsers.*" %>
<%@ page import="javax.xml.transform.*" %>
<%@ page import="javax.xml.transform.stream.*" %>
<%@ page import="javax.xml.transform.dom.*" %>
<%@ page import="java.io.*" %>
<%@ include file="getToken.jsp" %>

<%

    String address = request.getParameter("address");
    if(address==null || address.equalsIgnoreCase("null"))
        address = (String) session.getAttribute("addressSms");
	if(address==null || address.equalsIgnoreCase("null"))
		address = "";
	session.setAttribute("addressSms",address);
	String message = request.getParameter("message");
	if(message==null || message.equalsIgnoreCase("null"))
		message = (String) session.getAttribute("message");
	if(message==null || message.equalsIgnoreCase("null"))
		message = "simple message to myself";
	session.setAttribute("message",message);
	String smsId = request.getParameter("smsId");
	if (smsId==null) smsId = (String) session.getAttribute("smsId");
	if (smsId==null) smsId = "";
	session.setAttribute("smsId",smsId);
	String getSmsDeliveryStatus = request.getParameter("getSmsDeliveryStatus");
	String sendSms = request.getParameter("sendSms");
	String getReceivedSms = request.getParameter("getReceivedSms");
	String print = "";
%>

<div id="container">
<!-- open HEADER --><div id="header">

<div>
	<div id="hcRight">
        <%=new java.util.Date()%>
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

<h1>AT&T Sample SMS Application - Basic SMS Service Application</h1>
<h2>Feature 1: Send SMS</h2>

</div>
</div>
<form method="post" name="sendSms" action="">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<%=address%>" style="width: 90%">
    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Message:</td>
    <td class="cell"><textarea rows="4" name="message" style="width: 90%"><%=message%></textarea>
	</td></tr>
  </tbody></table>

</div>
<div id="extra">

  <table>
  <tbody>
  <tr>
  	<td><br /><br /><br /><br /><br /><button type="submit" name="sendSms">Send SMS Message</button></td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
<div align="center"></div>
</form>

<%
//If Send SMS button was clicked, do this.
if(sendSms!=null) {
    //Check for a few known formats the user could have entered the address, adjust accordingly
    String invalidAddress = null;
    if((address.indexOf("-")==3) && (address.length()==12))
        address = "tel:" + address.substring(0,3) + address.substring(4,7) + address.substring(8,12);
    else if((address.indexOf(":")==3) && (address.length()==14))
        address = address;    
    else if((address.indexOf("-")==-1) && (address.length()==10))
        address = "tel:" + address;
    else if((address.indexOf("-")==-1) && (address.length()==11))
        address = "tel:" + address.substring(1);
    else if((address.indexOf("-")==-1) && (address.indexOf("+")==0) && (address.length()==12))
        address = "tel:" + address.substring(2);
    else 
        invalidAddress = "yes";
if(invalidAddress==null) {
    //Initialize the client
    String url = FQDN + "/rest/sms/2/messaging/outbox";
    HttpClient client = new HttpClient();
    PostMethod method = new PostMethod(url);
    //Build the request body
    JSONObject rpcObject = new JSONObject();
	rpcObject.put("Message", message);
	rpcObject.put("Address", address);
	method.setRequestBody(rpcObject.toString());
	method.addRequestHeader("Content-Type","application/json; charset=UTF-8");
	method.addRequestHeader("Authorization","Bearer " + accessToken);
    method.addRequestHeader("Accept","application/json");
    //Send the request and parse based on HTTP status code
    int statusCode = client.executeMethod(method);
    if(statusCode==201) {
       	JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
       	smsId = jsonResponse.getString("Id");
       	session.setAttribute("smsId",smsId);
       	%>
        <div class="successWide">
        <strong>SUCCESS:</strong><br />
        <strong>Message ID:</strong> <%=smsId%>
        </div><br/>
		<%
    } else {
    	%>
        <div class="errorWide">
        <strong>ERROR:</strong><br />
        <%=method.getResponseBodyAsString()%>
        </div><br/>
		<%
    }
    method.releaseConnection();
    } else { %>
                <div class="errorWide">
                <strong>ERROR:</strong><br />
                Invalid Address Entered
                </div><br/>
<%    }
}
%>

<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Delivery Status</h2>

</div>
</div>
<form method="post" name="getSmsDeliveryStatus" action="">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Message ID:</td>
    <td class="cell"><input size="12" name="smsId" value="<%=smsId%>" style="width: 90%">
    </td>
  </tr>
  </tbody></table>
  
</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="getSmsDeliveryStatus">Get Status</button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
</form>

   <%  
    //If Check Delivery Status button was clicked, do this.
       if(getSmsDeliveryStatus!=null) {
           //Initialize the client
           String url = FQDN + "/rest/sms/2/messaging/outbox/" + smsId;   
           HttpClient client = new HttpClient();
           GetMethod method = new GetMethod(url);  

	    method.addRequestHeader("Authorization","Bearer " + accessToken);
           method.addRequestHeader("Accept","application/json");
           //Send the request, parse based on HTTP status code
           int statusCode = client.executeMethod(method); 
           if(statusCode==200) {
              	JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
              	JSONObject deliveryInfoList = new JSONObject(jsonResponse.getString("DeliveryInfoList"));
              	JSONArray deliveryInfoArray = new JSONArray(deliveryInfoList.getString("DeliveryInfo"));
              	JSONObject deliveryInfo = new JSONObject(deliveryInfoArray.getString(0));
              	%>
                <div class="successWide">
                <strong>SUCCESS:</strong><br />			
                <strong>Status:</strong> <%=deliveryInfo.getString("DeliveryStatus")%><br />
                <strong>Resource URL:</strong> <%=deliveryInfoList.getString("ResourceUrl")%>
                </div><br/>
       		<%
           } else {
           	%>
                <div class="errorWide">
                <strong>ERROR:</strong><br />
                <strong>Status:</strong> <%=statusCode%><br />    
                <%=method.getResponseBodyAsString()%>
                </div><br/>
       		<%
           }
           method.releaseConnection();
       }
   %>

<div id="wrapper">
<div id="content">

<h2><br />Feature 3: Get Received Messages</h2>

</div>
</div>
<form method="post" name="getReceivedSms" action="">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="getReceivedSms" value="<%=shortCode1%>">Get Messages for Short Code <%=shortCode1%></button>
    <button type="submit" name="getReceivedSms" value="<%=shortCode2%>">Get Messages for Short Code <%=shortCode2%></button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
</form>

   <%  
    //If Get Received Messages button was clicked, do this.
       if(getReceivedSms!=null) {
           //Initialize the client
           String url = FQDN + "/rest/sms/2/messaging/inbox";   
           HttpClient client = new HttpClient();
           GetMethod method = new GetMethod(url);  
           method.setQueryString("access_token=" + accessToken + "&RegistrationID=" + getReceivedSms);
           method.addRequestHeader("Accept","application/json");
           session.setAttribute("registrationID", getReceivedSms);
           //Send the request, parse based on HTTP status code
           int statusCode = client.executeMethod(method);
           if(statusCode==200) {
              		JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
              		JSONObject smsList = new JSONObject(jsonResponse.getString("InboundSmsMessageList"));
              		int numberOfMessagesInBatch = Integer.parseInt(smsList.getString("NumberOfMessagesInThisBatch"));
              		int numberOfMessagesPending = Integer.parseInt(smsList.getString("TotalNumberOfPendingMessages"));
              		JSONArray messages = new JSONArray(smsList.getString("InboundSmsMessage"));
              		%>
                    <div class="successWide">
                    <strong>SUCCESS:</strong><br />
                    <strong>Messages in this batch:</strong> <%=numberOfMessagesInBatch%><br />
                    <strong>Messages pending:</strong> <%=numberOfMessagesPending%>
                    </div>
                    <div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
                    <thead>
                        <tr>
                        	<th style="width: 100px" class="cell"><strong>Message Index</strong></th>
                            <th style="width: 275px" class="cell"><strong>Message Text</strong></th>
                            <th style="width: 125px" class="cell"><strong>Sender Address</strong></th>
                    	</tr>
                    </thead>
                    <tbody>
							<%if(messages.length()!=0) {
                            for(int i=0;i<numberOfMessagesInBatch; i++) {
							JSONObject msg = new JSONObject(messages.getString(i));%>
                            <tr>
                            	<td class="cell"><%=msg.getString("MessageId")%></td>
                                <td align="center" class="cell"><%=msg.getString("Message")%></td>
                                <td align="center" class="cell"><%=msg.getString("SenderAddress")%></td>
                            </tr>
							<%}}%>
					</tbody>
                    </table>
                    </div><br/>
              		<%

              } else {
                 	%>
                    <div class="errorWide">
                    <strong>ERROR:</strong><br />
                    <%=method.getResponseBodyAsString()%>
                    </div><br/>
           		<%
               }
           method.releaseConnection();
       }
   %>

<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p>&#169; 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>

</body></html>
