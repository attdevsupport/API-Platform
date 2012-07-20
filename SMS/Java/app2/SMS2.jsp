<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&amp;T Sample SMS Application - SMS app 2 - Voting</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.*"%>
<%@ page import="java.io.*" %>
<%@ include file="getToken.jsp" %>

<%

String responseFormat = "json";
String getReceivedSms = request.getParameter("getReceivedSms");
int numberOfMessagesInBatch = 0;
JSONObject jsonResponse = new JSONObject();
JSONObject smsList = new JSONObject();
JSONArray messages = new JSONArray();
String invalidMessagePresent = null;
String url2 = request.getRequestURL().toString().substring(0,request.getRequestURL().toString().lastIndexOf("/")) + "/getVotes.jsp";
HttpClient client2 = new HttpClient();
GetMethod method2 = new GetMethod(url2);  
int statusCode2 = client2.executeMethod(method2); 
JSONObject jsonResponse2 = new JSONObject(method2.getResponseBodyAsString());
Integer totalNumberOfVotes = Integer.parseInt(jsonResponse2.getString("totalNumberOfVotes"));
Integer footballVotes = Integer.parseInt(jsonResponse2.getString("footballVotes"));
Integer baseballVotes = Integer.parseInt(jsonResponse2.getString("baseballVotes"));
Integer basketballVotes = Integer.parseInt(jsonResponse2.getString("basketballVotes"));
method2.releaseConnection();
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

<h1>AT&amp;T Sample SMS Application - SMS app 2 - Voting</h1>
<h2>Feature 1: Calculate Votes sent via SMS to <%=shortCode1%> with text "Football", "Basketball", or "Baseball"</h2>

</div>
</div>
<form method="post" name="getReceivedSms" action="">

   <%  
    //If Update Totals button was clicked, do this.
       if(getReceivedSms!=null) {

           String url = FQDN + "/rest/sms/2/messaging/inbox";   
           HttpClient client = new HttpClient();
           GetMethod method = new GetMethod(url);  
           method.setQueryString("access_token=" + accessToken + "&RegistrationID=" + shortCode1);
           method.addRequestHeader("Accept","application/" + responseFormat);
           session.setAttribute("registrationID", shortCode1);
           int statusCode = client.executeMethod(method); 
           
        if(statusCode==200) {
          	jsonResponse = new JSONObject(method.getResponseBodyAsString());
      		smsList = new JSONObject(jsonResponse.getString("InboundSmsMessageList"));
      		numberOfMessagesInBatch = Integer.parseInt(smsList.getString("NumberOfMessagesInThisBatch"));
      		int numberOfMessagesInBatch1 = 0;
      		int numberOfMessagesInBatch2 = 0;
      		int numberOfMessagesInBatch3 = 0;
      		int numberOfMessagesPending = Integer.parseInt(smsList.getString("TotalNumberOfPendingMessages"));
      		messages = new JSONArray(smsList.getString("InboundSmsMessage"));
      		if(messages.length()!=0) {
				for(int i=0;i<numberOfMessagesInBatch; i++) {
					JSONObject msg = new JSONObject(messages.getString(i));
					String messageText = msg.getString("Message");
					if(messageText.equalsIgnoreCase("football")) {
						numberOfMessagesInBatch1 += 1;
					} else if(messageText.equalsIgnoreCase("baseball")) {
						numberOfMessagesInBatch2 += 1;
					} else if(messageText.equalsIgnoreCase("basketball")) {
						numberOfMessagesInBatch3 += 1;
					} else {
						invalidMessagePresent = "yes";
					}
					
				}
      		}
           
    	   footballVotes = footballVotes + numberOfMessagesInBatch1;
    	   PrintWriter outWrite1 = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/WEB-INF/tally1.txt"))), false);
   		   outWrite1.write(footballVotes.toString());
   		   outWrite1.close();
   		   
    	   baseballVotes = baseballVotes + numberOfMessagesInBatch2;
    	   PrintWriter outWrite2 = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/WEB-INF/tally2.txt"))), false);
   		   outWrite2.write(baseballVotes.toString());
   		   outWrite2.close();
   		   
    	   basketballVotes = basketballVotes + numberOfMessagesInBatch3;
    	   PrintWriter outWrite3 = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/WEB-INF/tally3.txt"))), false);
   		   outWrite3.write(basketballVotes.toString());
   		   outWrite3.close();
            %>
                <div id="navigation">
                <br /><br />
                <div class="success">
                <strong>SUCCESS:</strong><br />
                <strong>Total votes:</strong> <%=totalNumberOfVotes%>
                </div>
            <%
        } else {
        	%><%=method.getResponseBodyAsString()%><%
        }
        method.releaseConnection();
       }
              	%>

<br/>
<table style="width: 300px" cellpadding="1" cellspacing="1" border="0">
<thead>
	<tr>
    	<th style="width: 125px" class="cell"><strong>Favorite Sport</strong></th>
        <th style="width: 125px" class="cell"><strong>Number of Votes</strong></th>
	</tr>
</thead>
<tbody>
	<tr>
        <td align="center" class="cell">Football</td>
        <td align="center" class="cell"><%=footballVotes%></td>
    </tr>
	<tr>
        <td align="center" class="cell">Baseball</td>
        <td align="center" class="cell"><%=baseballVotes%></td>
    </tr>
	<tr>
        <td align="center" class="cell">Basketball</td>
        <td align="center" class="cell"><%=basketballVotes%></td>
    </tr>
</tbody>
</table>

</div>
<div id="extra">

  <table>
  <tbody>
  <tr>
  	<td><br /><br /><br /><br /><br /><br /><br /><br /><button type="submit" name="getReceivedSms">Update Vote Totals</button></td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
<div align="center"></div>
</form>

<%
if(invalidMessagePresent!=null) {
%>
    <br/>
    <div class="errorWide">
    <strong>INVALID TEXT PRESENT:</strong><br />
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
			JSONObject msg = new JSONObject(messages.getString(i));
            if((!msg.getString("Message").equalsIgnoreCase("Football")) && (!msg.getString("Message").equalsIgnoreCase("Basketball")) && (!msg.getString("Message").equalsIgnoreCase("Baseball"))) {
            %>
            <tr>
                <td class="cell"><%=msg.getString("MessageId")%></td>
                <td align="center" class="cell"><%=msg.getString("Message")%></td>
                <td align="center" class="cell"><%=msg.getString("SenderAddress")%></td>
            </tr>
				<%}}}%>
	</tbody>
    </table>
    </div><br/>
<% } %>


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
