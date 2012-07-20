<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample Application - TL Service Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.JSONObject"%>
<%@ include file="config.jsp" %>
<%
String scope = "TL";


String requestedAccuracy = request.getParameter("requestedAccuracy");
String acceptableAccuracy = request.getParameter("acceptableAccuracy");
String tolerance = request.getParameter("tolerance");
String getDeviceLocation = request.getParameter("getDeviceLocation");
String postOauth = "TL.jsp?getDeviceLocation=true&requestedAccuracy=" + requestedAccuracy + "&acceptableAccuracy=" + acceptableAccuracy + "&tolerance=" + tolerance;
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

<h1>AT&T Sample Application - TL</h1>
<h2>Feature 1: Map of Device Location</h2>

</div>
</div>
<form method="post" name="getDeviceLocation" action="TL.jsp">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
  <td valign="top" class="label">Requested Accuracy:</td>
    <td valign="top" class="cell"><input type="radio" name="requestedAccuracy" value="150" /> 150 m  <input type="radio" name="requestedAccuracy" value="1000" checked /> 1,000 m <input type="radio" name="requestedAccuracy" value="10000" /> 10,000 m </td>
  </tr>
  <tr>
  <td valign="top" class="label">Acceptable Accuracy:</td>
    <td valign="top" class="cell"><input type="radio" name="acceptableAccuracy" value="150" /> 150 m <input type="radio" name="acceptableAccuracy" value="1000" /> 1,000 m <input type="radio" name="acceptableAccuracy" value="10000" checked /> 10,000 m </td>
  </tr>
  <tr>
    <td valign="top" class="label">Delay Tolerance:</td>
    <td valign="top" class="cell"><input type="radio" name="tolerance" value="NoDelay" /> No Delay <input type="radio" name="tolerance" value="LowDelay" checked /> Low Delay <input type="radio" name="tolerance" value="DelayTolerant" /> Delay Tolerant  </td>
    </td>
  </tr>
  </tbody></table>

</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><br /><br /><br /><br /><br /><br /><button type="submit" name="getDeviceLocation">Get Phone Location</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />

<div align="center"></div>
</form>

<%  
if(getDeviceLocation!=null) {
    String accessToken = request.getParameter("access_token");
    if(accessToken==null || accessToken == "null"){
accessToken = (String)session.getAttribute("accessToken");
    session.setAttribute("accessToken", accessToken);}
if((accessToken==null) || (!scope.equalsIgnoreCase("TL")) && (!scope.equalsIgnoreCase("SMS,MMS,WAP,DC,TL,PAYMENT"))) {
session.setAttribute("scope", "TL");
session.setAttribute("clientId", clientIdWeb);
session.setAttribute("clientSecret", clientSecretWeb);
session.setAttribute("postOauth", postOauth);
session.setAttribute("redirectUri", redirectUri);
response.sendRedirect("oauth.jsp?getExtCode=yes");
}
    String url = FQDN + "/2/devices/location";    
    HttpClient client = new HttpClient();
    GetMethod method = new GetMethod(url);  
    method.setQueryString("requestedAccuracy=" + requestedAccuracy + "&access_token=" + accessToken + "&acceptableAccuracy=" + acceptableAccuracy + "&tolerance=" + tolerance);
    method.addRequestHeader("Accept","application/json");
    method.addRequestHeader("Authorization","Bearer " + accessToken);
    Long start = System.currentTimeMillis();
    int statusCode = client.executeMethod(method);    
    Long end = System.currentTimeMillis();
    Long elapsed = (end-start)/1000;
    if(statusCode==200 || statusCode==201) {
    JSONObject rpcObject = new JSONObject(method.getResponseBodyAsString());
    %>
            <div class="successWide">
            <strong>SUCCESS:</strong><br />
            <strong>Latitude:</strong> <%=rpcObject.getString("latitude")%><br />
            <strong>Longitude:</strong> <%=rpcObject.getString("longitude")%><br />
            <strong>Accuracy:</strong> <%=rpcObject.getString("accuracy")%><br />
            <strong>Response Time:</strong> <%=elapsed%> seconds
            </div>
            <br /><br />
            
            <div align="center">
            <iframe width="600" height="400" frameborder="0" scrolling="no" marginheight="0" marginwidth="0" 
    src="http://maps.google.com/?q=<%=rpcObject.getString("latitude")%>+<%=rpcObject.getString("longitude")%>&output=embed"></iframe><br /></div><%
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
