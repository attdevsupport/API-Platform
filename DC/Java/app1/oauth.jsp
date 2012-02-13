<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' September 2011
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2011 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML lang=en xml:lang="en" xmlns="http://www.w3.org/1999/xhtml"><HEAD><TITLE>Application 1</TITLE>
<META content="text/html; charset=windows-1252" http-equiv=Content-Type>
<SCRIPT type=text/javascript src="helper.js">
</SCRIPT>
<META name=GENERATOR content="MSHTML 8.00.6001.19046"></HEAD>
<BODY>
<TABLE border=0 width="100%">
  <TBODY>
  <TR>
    <TD rowSpan=2 width="25%" align=left><IMG 
      src="http://developer.att.com/developer/images/att.gif"></TD>
    <TD width="15%" align=right>Server Time:</TD>
    <TD width="60%" align=left>
      <SCRIPT language=JavaScript type=text/javascript>
var myDate = new Date();
document.write(myDate.format('l, F d, Y  H:i') + ' PDT');
</SCRIPT>
</TD></TR>
  <TR>
    <TD width="15%" align=right>Client Time:</TD>
    <TD width="25%" align=left>
      <SCRIPT language=JavaScript type=text/javascript>
var myDate = new Date();
document.write(myDate.format('l, F d, Y  H:i') + ' PDT');
</SCRIPT>
</TD></TR></TBODY></TABLE>

<HR size=px"></HR>
<font size=4px"><B>ATT sample Oauth application</B></font><BR><BR>
<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.JSONObject"%>
<%@ include file="config.jsp" %>
<%
    String clientId = request.getParameter("clientId");
if(clientId!=null)
if(clientId.equalsIgnoreCase("default"))
	clientId = clientIdWeb;
if((clientId==null) || (clientId.equalsIgnoreCase("null"))) 
	clientId = (String) session.getAttribute("clientId");
if(clientId==null)
	clientId = "";
String clientSecret = request.getParameter("clientSecret");
if(clientSecret!=null)
if(clientSecret.equalsIgnoreCase("default"))
	clientSecret = clientSecretWeb;
if((clientSecret==null) || (clientSecret.equalsIgnoreCase("null"))) 
	clientSecret = (String) session.getAttribute("clientSecret");
if(clientSecret==null)
	clientSecret = "";
String redirectUri = request.getParameter("redirectUri");
if(redirectUri==null)
	redirectUri = (String) session.getAttribute("redirectUri");
if(redirectUri==null)
	redirectUri = "https://code-api-att.com/apigee-public/oauth.jsp";
String scope = request.getParameter("scope");
if(scope==null)
	scope = (String) session.getAttribute("scope");
if(scope==null)
	scope = "SMS,MMS";
session.setAttribute("scope", scope);
String code = request.getParameter("code");
if(code==null) code="";
String print = "";
String getExtCode = request.getParameter("getExtCode");
String refreshToken = request.getParameter("refreshToken");
if (refreshToken==null) 
	refreshToken=(String) session.getAttribute("refreshToken");
if (refreshToken==null) 
	refreshToken="";
String getRefreshToken = request.getParameter("getRefreshToken");
if (getRefreshToken==null) getRefreshToken="";
%>

<form name="getExtCode" method="post">
API Key <input type="text" name="clientId" value="default" size=40 /><br>
API Secret <input type="text" name="clientSecret" value="default" size=40 /><br>
{FQDN} <input type="text" name="FQDN" value="<%=FQDN%>" size=60 /><br>
Scope <input type="text" name="scope" value="<%=scope%>" size=40 /><br />
Redirect URI <input type="text" name="redirectUri" value="<%=redirectUri%>" size=60 /><br />
<input type="submit" name="getExtCode" value="Get Access Token" />
</form><br><br>

   <%   
   	   if(getExtCode!=null) {
   		   session.setAttribute("clientId", clientId);
   		   session.setAttribute("clientSecret", clientSecret);
   		   session.setAttribute("FQDN", FQDN);
   		   response.sendRedirect(FQDN + "/oauth/authorize?client_id=" + clientId + "&scope=" + scope + "&redirect_uri=" + redirectUri);
   	   }
   
       if(!code.equalsIgnoreCase("")) {
            String url = FQDN + "/oauth/token";   
            HttpClient client = new HttpClient();
            PostMethod method = new PostMethod(url); 
            String b = "client_id=" + clientId + "&client_secret=" + clientSecret + "&grant_type=authorization_code&code=" + code;
            method.addRequestHeader("Content-Type","application/x-www-form-urlencoded");
            method.setRequestBody(b);
            int statusCode = client.executeMethod(method);    
            print = method.getResponseBodyAsString();
           if(statusCode==200){ 
            	JSONObject rpcObject = new JSONObject(method.getResponseBodyAsString());
            	String accessToken = rpcObject.getString("access_token");
            	refreshToken = rpcObject.getString("refresh_token");
            	session.setAttribute("refreshToken", refreshToken);
               	session.setAttribute("accessToken", accessToken);

           	String postOauth = (String) session.getAttribute("postOauth");
           	if(postOauth!= null) {
           		session.setAttribute("postOauth", null);
           		response.sendRedirect(postOauth);
           	}
           }
           method.releaseConnection();
       }
   %>   

<form name="getRefreshToken" method="post">
API Key <input type="text" name="clientId" value="<%=clientId%>" size=40 /><br />
API Secret <input type="text" name="clientSecret" value="<%=clientSecret%>" size=40 /><br />
Refresh Token <input type="text" name="refreshToken" value="<%=refreshToken%>" size=60 /><br />
<input type="submit" name="getRefreshToken" value="Refresh Access Token" />
</form><br><br>

   <%   
       if(!getRefreshToken.equalsIgnoreCase("")) {
    	   session.setAttribute("clientId",clientId);
    	   session.setAttribute("clientSecret",clientSecret);
            String url = FQDN + "/oauth/token";   
            HttpClient client = new HttpClient();
            PostMethod method = new PostMethod(url); 
            String b = "client_id=" + clientIdAut + "&client_secret=" + clientSecretAut + "&grant_type=refresh_token&refresh_token=" + refreshToken;
            method.addRequestHeader("Content-Type","application/x-www-form-urlencoded");
            method.setRequestBody(b);
           int statusCode = client.executeMethod(method);    
           print = method.getResponseBodyAsString();
           if(statusCode==200){ 
           	String accessToken = print.substring(18,50);
           	session.setAttribute("accessToken", accessToken);
           	String postOauth = (String) session.getAttribute("postOauth");
           	if(postOauth!= null) {
           		session.setAttribute("postOauth", null);
           		response.sendRedirect(postOauth);
           	} else {
           		
           	}
           }
           method.releaseConnection();
       }
   %> 
<br><br><html><body><%=print%></body></html>

<div style="position:absolute;bottom:0px;left:30%;width:40%;font-size:9px;" align="center" >
� 2011 AT&T Intellectual Property. All rights reserved.  <a href="http://developer.att.com" target="_blank">http://developer.att.com</a>
<br/>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&T Services and not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br/>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com" target="_blank">https://devconnect-api.att.com</a>
<br/>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>
<br/><br/>
</div>

