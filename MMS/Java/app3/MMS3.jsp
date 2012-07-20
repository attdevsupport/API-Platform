<%-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
--%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample MMS Application 3 - MMS Gallery Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="java.io.*" %>
<%@ page import="java.util.Arrays" %>
<%@ page import="java.util.Collections" %>
<%@ page import="java.util.Comparator" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.*"%>
<%@ include file="config.jsp" %>

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

<h1>AT&T Sample MMS Application 3 - MMS Gallery Application</h1>
<h2>Feature 1: Web gallery of MMS photos sent to short code</h2>

</div>
</div>

<%
String senderAddress = "";
String date = "";
String text = "";
String url = request.getRequestURL().toString().substring(0,request.getRequestURL().toString().lastIndexOf("/")) + "/getImageData.jsp";
HttpClient client = new HttpClient();
GetMethod method = new GetMethod(url);  
int statusCode = client.executeMethod(method); 
JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
JSONArray imageList = new JSONArray(jsonResponse.getString("imageList"));
String totalNumberOfImagesSent = jsonResponse.getString("totalNumberOfImagesSent");
%>

<br />
<p>Photos sent to short code <%=shortCode1%>: <%=totalNumberOfImagesSent%></p>


<div id="gallerywrapper">
<%
for(int i=0; i<imageList.length(); i++) {
JSONObject image = new JSONObject(imageList.getString(i));
%>
            <div id="gallery"><img src="<%=image.getString("path")%>" width="150" border="0"  /><br /><strong>Sent from:</strong> <%=image.getString("senderAddress")%><br /><strong>On:</strong> <%=image.getString("date")%><div><%=image.getString("text")%></div></div>
<%
if (i==10) {
   break;
}
}
method.releaseConnection();
%>

</div>
<br clear="all" />

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