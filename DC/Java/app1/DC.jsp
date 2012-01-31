<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample DC Application - Get Device Capabilities Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.JSONObject"%>
<%@ page import="java.util.Iterator"%>
<%@ include file="config.jsp" %>
<%
String scope = (String) session.getAttribute("scope");
if(scope==null) scope="";
String postOauth = "DC.jsp?getDeviceInfo=true";
String redirectUri = "";
String address = request.getParameter("address");
if(address==null || address.equalsIgnoreCase("null"))
    address = (String) session.getAttribute("addressDC");
if(address==null || address.equalsIgnoreCase("null"))
	address = "425-802-8620";
Boolean newAddress = false;
if(!address.equalsIgnoreCase((String)session.getAttribute("addressDC"))) {
    newAddress = true;
}
session.setAttribute("addressDC",address);
String getDeviceInfo = request.getParameter("getDeviceInfo");
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

<h1>AT&T Sample DC Application - Get Device Capabilities Application</h1>
<h2>Feature 1: Get Device Capabilities</h2>

</div>
</div>
<form method="post" name="getDeviceInfo" action="DC.jsp">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<%=address%>" style="width: 90%">
    </td>
  </tr>
  </tbody></table>
  
</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="getDeviceInfo">Get Device Capabilities</button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
</form>

<%  
    if(getDeviceInfo!=null) {
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
        String accessToken = request.getParameter("access_token");
    	if(accessToken==null || accessToken=="null"){
    		accessToken = (String) session.getAttribute("accessToken");}
    	if((newAddress==true) || (accessToken==null) || (!scope.equalsIgnoreCase("DC")) && (!scope.equalsIgnoreCase("SMS,MMS,WAP,DC,TL,PAYMENT"))) {
    			session.setAttribute("scope", "DC");
    			session.setAttribute("clientId", clientIdWeb);
    			session.setAttribute("clientSecret", clientSecretWeb);
    			session.setAttribute("address", address);
    			session.setAttribute("postOauth", postOauth);
    			session.setAttribute("redirectUri", redirectUri);
    			response.sendRedirect("oauth.jsp?getExtCode=yes");
    	}
        String url = FQDN + "/1/devices/" + address + "/info";   
        HttpClient client = new HttpClient();
        GetMethod method = new GetMethod(url);  
        method.setQueryString("access_token=" + accessToken);
        method.addRequestHeader("Accept","application/json");
        int statusCode = client.executeMethod(method);    
        if(statusCode==200) {
           	JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
           	JSONObject deviceId = new JSONObject(jsonResponse.getString("deviceId"));
            JSONObject capabilities = new JSONObject(jsonResponse.getString("capabilities"));
            Iterator deviceIdKeys = deviceId.keys();
            Iterator capabilitiesKeys = capabilities.keys();
           	%>
                <div class="successWide">
                <strong>SUCCESS:</strong><br />
                Device parameters listed below.
                </div>
                <br />
                
                <div align="center">
                <table width="500" cellpadding="1" cellspacing="1" border="0">
                    <thead>
                    	<tr>
                        	<th width="50%" class="label">Parameter</th>
                            <th width="50%" class="label">Value</th>
                        </tr>
                    </thead>
                    <tbody>
        	<%
                while(deviceIdKeys.hasNext()) {
                String key = (String) deviceIdKeys.next();
            %>
                        <tr>
                        <td class="cell" align="center"><em><%=key%></em></td>
                    	<td class="cell" align="center"><em><%=deviceId.getString(key)%></em></td>
                       </tr>
            <%
                }
            %>
            <%
                while(capabilitiesKeys.hasNext()) {
                String key = (String) capabilitiesKeys.next();
            %>
                        <tr>
                        <td class="cell" align="center"><em><%=key%></em></td>
                    	<td class="cell" align="center"><em><%=capabilities.getString(key)%></em></td>
                       </tr>
            <%
                }
            %>
                    </tbody>
                </table>
                </div>
                
                <br />
<%
        } 
        else {
        	%>
            <div class="errorWide">
            <strong>ERROR:</strong><br />
            <%=method.getResponseBodyAsString()%>
            </div>
            <br />
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