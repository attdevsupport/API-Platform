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
String getReceivedSms = request.getParameter("getReceivedSms");

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
			String url = request.getRequestURL().toString().substring(0,request.getRequestURL().toString().lastIndexOf("/")) + "/getVoteData.jsp";
			HttpClient client = new HttpClient();
			GetMethod method = new GetMethod(url);  
			int statusCode = client.executeMethod(method); 
			//client.executeMethod(method);
			RandomAccessFile inFile2 = new RandomAccessFile(application.getRealPath("VoteTotals/voteTotals.txt"),"r");		
	
			String footTotal = inFile2.readLine();
			String baseTotal = inFile2.readLine();
			String basketTotal = inFile2.readLine();
			String total = inFile2.readLine();
			
			inFile2.close();

            %>
 <br/> 
						<div id = "extraleft">
						<div class="success">
                        <strong>Success:</strong><br />
                        <strong>Total Votes: </strong><%=total%>
                        </div><br/>
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
        <td align="center" class="cell"><%=footTotal%></td>
    </tr>
	<tr>
        <td align="center" class="cell">Baseball</td>
        <td align="center" class="cell"><%=baseTotal%></td>
    </tr>
	<tr>
        <td align="center" class="cell">Basketball</td>
        <td align="center" class="cell"><%=basketTotal%></td>
    </tr>
	
</tbody>
</table>

</div>
<div id="extra">

  <table>
  <tbody>
  <tr>
	<td><br /><button type="submit" name="getReceivedSms">Update Vote Totals</button></td>
  </tr>
  </tbody>
  </table>

</div>
<br></br><br></br><br></br>
<br clear="all" />
<div align="center"></div>

<%

if(getReceivedSms!=null)
{
	String dateTime = "";
	String messageId = "";
	String message = "";
	String senderAdd = "";
	String destinationAdd = "";	
	JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
	JSONArray voteList = new JSONArray(jsonResponse.getString("voteList"));

		
	for(int i = 0;i < voteList.length(); i++){
	JSONObject vote = new JSONObject(voteList.getString(i));
%>
<table width="100%" cellpadding="1" cellspacing="1" border="0">
<%if(i==0){%>
<tbody>
    <tr>
<th class="cell" width="30%" align="left"><strong>Date & Time </strong></th>
<th class="cell" width="10%" align="left"><strong>MessageID</strong></th>
<th class="cell" width="20%" align="left"><strong>Message</strong></th>
<th class="cell" width="20%" align="left"><strong>SenderAddress</strong></th>
<th class="cell" width="20%" align="left"><strong>DestinationAddress</strong></th>
</td>
	</tr>
<%}%>
<tr>
                        <td class="cell" width="30%" align="left">
	                    <%=vote.getString("Date&Time")%>
			            </td>
						 <td class="cell" width="10%" align="left">
                         <%=vote.getString("MessageID")%>
				        </td>
						 <td class="cell" width="20%" align="left">
                         <%=vote.getString("Message")%>
				         </td>
						 <td class="cell" width="20%" align="left">
                         <%=vote.getString("SenderAddress")%>
				         </td>
						 <td class="cell" width="20%" align="left">
	                     <%=vote.getString("DestinationAddress")%>
			             </td>
</tr>  
<% 							
		if (i==10) {
			break;
		}
      }  
 } %>   
  </tbody></table>


</form>
</div>
<br clear="all" />
</div>



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
