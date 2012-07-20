<%
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample Application - WAPPush</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="com.sun.jersey.multipart.file.*" %>
<%@ page import="com.sun.jersey.multipart.BodyPart" %>
<%@ page import="com.sun.jersey.multipart.MultiPart" %>
<%@ page import="java.io.*" %>
<%@ page import="java.util.List" %>
<%@ page import="com.att.rest.*" %>
<%@ page import="java.net.*" %>
<%@ page import="javax.ws.rs.core.*" %>
<%@ page import="org.apache.commons.fileupload.*"%>
<%@ page import="java.util.List,java.util.Iterator"%>
<%@ page import="org.json.*"%>
<%@ page import="org.w3c.dom.*" %>
<%@ page import="javax.xml.parsers.*" %>
<%@ page import="javax.xml.transform.*" %>
<%@ page import="javax.xml.transform.stream.*" %>
<%@ page import="javax.xml.transform.dom.*" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.apache.commons.codec.binary.Base64" %>
<%@ include file="getToken.jsp" %>

<%

String sendWap = request.getParameter("sendWap");
String contentBodyFormat = "FORM-ENCODED";    
String address = request.getParameter("address");
if(address==null || address.equalsIgnoreCase("null"))
    address = (String) session.getAttribute("addressWap");
if(address==null || address.equalsIgnoreCase("null"))
	address = "";
session.setAttribute("addressWap",address);		
String fileName = "";		
String subject = request.getParameter("subject");
if(subject==null || subject.equalsIgnoreCase("null")) 
    subject = (String) session.getAttribute("subject");
if(subject==null || subject.equalsIgnoreCase("null"))
    subject = "This is a sample WAP Push message.";	
session.setAttribute("subject", subject);
String url = request.getParameter("url");
if(url==null || url.equalsIgnoreCase("null")) 
    url = (String) session.getAttribute("url");
if(url==null || url.equalsIgnoreCase("null"))
    url = "http://developer.att.com";   
session.setAttribute("url", url);
String priority = "High";
String wapId = "";
String endpoint = FQDN + "/1/messages/outbox/wapPush";
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

<h1>AT&T Sample Application - WAPPush</h1>
<h2>Feature 1: Send basic WAP message</h2>

</div>
</div>
<form method="post" name="sendWap" >
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<%=address%>" style="width: 90%">
    </td>
  </tr>
  <tr>
    <td width="20%" valign="top" class="label">URL:</td>
    <td class="cell"><input size="18" name="url" value="<%=url%>" style="width: 90%">
    </td>
  </tr>
  <tr>
  	<td valign="top" class="label">Service Type:</td>
    <td valign="top" class="cell">Service Indication <input type="radio" name="" value="" checked /> Service Loading <input type="radio" name="" value="" disabled /> </td>
  </tr>
  </tbody></table>

<div class="warning">
<strong>WARNING:</strong><br />
At this time, AT&T only supports Service Type: Service Indication due to security concerns.
</div>

</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Alert Text:</td>
    <td class="cell"><textarea rows="4" name="subject" style="width: 90%"><%=subject%></textarea></td>
  </tr>
  </tbody></table>
  <table>
  <tbody>
  <tr>
  	<td><button type="submit" name="sendWap">Send WAP Message</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />
<div align="center"></div>
</form>

<% 
//If Send WAP Push button was clicked, do this.
if(request.getParameter("sendWap")!=null) {    

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
		
		MediaType contentBodyType = null;
		String requestBody = "";
		MultiPart mPart;
			contentBodyType = MediaType.MULTIPART_FORM_DATA_TYPE;
			JSONObject requestObject = new JSONObject();
   			requestObject.put("address", address);
   		 	requestBody += requestObject.toString() + "\r\n";
		

String attachmentBody = "";
attachmentBody += "Content-Disposition: form-data; name=\"PushContent\"\n";
attachmentBody += "Content-Type: text/vnd.wap.si\n";
attachmentBody += "Content-Length: 20\n";
attachmentBody += "X-Wap-Application-Id: x-wap-application:wml.ua\n\n";
attachmentBody += "<?xml version=\"1.0\"?>\n";
attachmentBody += "<!DOCTYPE si PUBLIC \"-//WAPFORUM//DTD SI 1.0//EN\" \"http://www.wapforum.org/DTD/si.dtd\">\n";
attachmentBody += "<si>";
attachmentBody += "<indication href=\"" + url + "\" action=\"signal-medium\" si-id=\"6532\" >\n";
attachmentBody += subject + "\n";
attachmentBody += "</indication>\n";
attachmentBody += "</si>\n";
String encodedAttachment = new String(Base64.encodeBase64(attachmentBody.getBytes()));

   			mPart = new MultiPart().bodyPart(new BodyPart(requestBody,MediaType.APPLICATION_JSON_TYPE)).bodyPart(new BodyPart(encodedAttachment, MediaType.TEXT_PLAIN_TYPE));
		
		mPart.getBodyParts().get(1).getHeaders().add("Content-Transfer-Encoding", "base64");
		mPart.getBodyParts().get(1).getHeaders().add("Content-Disposition","attachment; name=\"\"; filename=\"\"");
		mPart.getBodyParts().get(0).getHeaders().add("Content-Transfer-Encoding", "8bit");
		mPart.getBodyParts().get(0).getHeaders().add("Content-Disposition","form-data; name=\"root-fields\"");
		mPart.getBodyParts().get(0).getHeaders().add("Content-ID", "<startpart>");
		mPart.getBodyParts().get(1).getHeaders().add("Content-ID", "<attachment>");
		// This currently uses a proprietary rest client to assemble the request body that does not follow SMIL standards. It is recommended to follow SMIL standards to ensure attachment delivery.
		RestClient client;
			client = new RestClient(endpoint, contentBodyType, MediaType.APPLICATION_JSON_TYPE);
        
        
		client.addRequestBody(mPart);
		String responze = client.invoke(com.att.rest.HttpMethod.POST, accessToken);
		if (client.getHttpResponseCode() == 200){
			JSONObject rpcObject = new JSONObject(responze);
			wapId = rpcObject.getString("id");
			session.setAttribute("mmsId", wapId);
      	    session.setAttribute("wapId",wapId);
	       	%>
                <div class="successWide">
                <strong>SUCCESS:</strong><br />
                <strong>Message ID:</strong> <%=wapId%>
              
                </div>
			<%
		} else {
	    	%>
                <div class="errorWide">
                <strong>ERROR:</strong><br />
                <%=responze%>
                </div>
			<%
		}
    } else { %>
                <div class="errorWide">
                <strong>ERROR:</strong><br />
                Invalid Address Entered
                </div>
<%    }
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

