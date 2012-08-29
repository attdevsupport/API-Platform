<%
	//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
<title>AT&T Sample Mobo Application 1 - Basic Mobo Service
	Application</title>
<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type" />
<link rel="stylesheet" type="text/css" href="style/common.css" />
<style type="text/css">
.style1 {
	font-style: normal;
	font-variant: normal;
	font-weight: bold;
	font-size: 12px;
	line-height: normal;
	font-family: Arial, Sans-serif;
	width: 92px;
}
</style>
</head>
<body>
	<%@ page language="java" session="true" %>
	<%@ page contentType="text/html; charset=iso-8859-1" language="java"%>
	<%@ page import="com.sun.jersey.multipart.file.*"%>
	<%@ page import="com.sun.jersey.multipart.BodyPart"%>
	<%@ page import="com.sun.jersey.multipart.MultiPart"%>
	<%@ page import="com.sun.jersey.api.client.Client"%>
	<%@ page import="com.sun.jersey.api.client.ClientResponse"%>
	<%@ page import="com.sun.jersey.api.client.WebResource"%>
	<%@ page import="com.sun.jersey.api.client.config.ClientConfig"%>
	<%@ page import="com.sun.jersey.api.client.config.DefaultClientConfig"%>
	<%@ page import="com.sun.jersey.core.util.StringKeyIgnoreCaseMultivaluedMap"%>
	<%@ page import="com.sun.jersey.multipart.MultiPartMediaTypes"%>
	<%@ page import="javax.mail.internet.MimeMultipart"%>
	<%@ page import="javax.mail.internet.MimeBodyPart"%>
	<%@ page import="java.io.*"%>
	<%@ page import="java.util.List"%>
	<%@ page import="com.att.rest.*"%>
	<%@ page import="java.net.*"%>
	<%@ page import="javax.ws.rs.core.*"%>
	<%@ page import="org.apache.commons.fileupload.*"%>
	<%@ page import="java.util.List,java.util.Iterator"%>
	<%@ page import="org.json.*"%>
	<%@ page import="org.w3c.dom.*"%>
	<%@ page import="javax.xml.parsers.*"%>
	<%@ page import="javax.xml.transform.*"%>
	<%@ page import="javax.xml.transform.stream.*"%>
	<%@ page import="javax.xml.transform.dom.*"%>
	<%@ page import="org.apache.commons.httpclient.*"%>
	<%@ page import="org.apache.commons.httpclient.methods.*"%>
	<%@ page import="java.util.HashSet"%>
	<%@page import="java.util.regex.Matcher"%>  
	<%@page import="java.util.regex.Pattern"%>  
	<%@ include file="config.jsp"%>
	<%
		final String scope = "MOBO";
		final String contentBodyFormat = "FORM-ENCODED"; 
		final String responseFormat = "json";
		final String requestFormat = "json";
		final String endpoint = FQDN + "/rest/1/MyMessages";
		final String postOauth = "MOBO.jsp?sendMessageButton=true";
		final String priority = "HIGH";
		String accessToken =  "";
		int statusCode = 0;
		RestClient client;
		String responze = "";
		String ID = "";

		List addresses = new java.util.ArrayList();
		List badAddresses = new java.util.ArrayList();
		int numShort = 0;


		String sendMessageButton = request.getParameter("sendMessageButton");        
		boolean sendMsgBtnClicked = false;
		boolean groupBoxError = false;

		String phoneTextBox = "";
		if (session.getAttribute("phoneTextBox") != null )
		{
			phoneTextBox = (String)session.getAttribute("phoneTextBox") ;	
		}

		String subjectTextBox = session.getAttribute("subjectTextBox") != null  ? (String)session.getAttribute("subjectTextBox") : "";
		String messageTextBox =session.getAttribute("messageTextBox") != null  ? (String)session.getAttribute("messageTextBox") : "";
		String groupCheckBox  = session.getAttribute("groupCheckBox") != null  ? (String)session.getAttribute("groupCheckBox") : "";
		String fileName =session.getAttribute("fileName") != null  ? (String) session.getAttribute("fileName") : "";
		//
		if (phoneTextBox == null)
		{
			phoneTextBox =  request.getParameter("phoneTextBox") != null ? (String)  request.getParameter("phoneTextBox") : "";
			messageTextBox =  request.getParameter("messageTextBox") != null ? (String)  request.getParameter("messageTextBox") : "";
			groupCheckBox =  request.getParameter("groupCheckBox") != null ? (String)  request.getParameter("groupCheckBox") : "";
			fileName =  request.getParameter("fileName") != null ? (String)  request.getParameter("fileName") : "";
		}

		if(sendMessageButton != null) 
		{
		  sendMsgBtnClicked = true;
		}

		//If Send MMS button was clicked, do this to get some parameters from the form.
		if( sendMsgBtnClicked) 
		{ 
			try
			{
		        DiskFileUpload fu = new DiskFileUpload();
		        List fileItems = fu.parseRequest(request);
		        Iterator itr = fileItems.iterator();
		        while(itr.hasNext()) 
		        {
			       FileItem fi = (FileItem)itr.next();
			       if(!fi.isFormField()) 
			 	   {
						if (fi.getName() != "" )
						{
							File fNew= new File(application.getRealPath("/"), fi.getName());
							if (fileName == "") 
							{
								fileName = fi.getName();
							}	 
							else 
							{
						        fileName = fileName + "," + fi.getName();
							}
						    if(!(fi.getName().equalsIgnoreCase("")))
						    {
						    	fi.write(fNew);
						    }
						    session.setAttribute("fileName",fileName);
						}

				   } else {
				       session.setAttribute(fi.getFieldName(),fi.getString().trim());
				   }
				}
		    }
		    catch(Exception e)
		    {
		    } 

			//check for access token		    
		    accessToken = request.getParameter("access_token");
			if(accessToken==null || accessToken == "null"){
				accessToken = (String)session.getAttribute("accessToken");
				session.setAttribute("accessToken", accessToken);
			}
			if((accessToken==null) || (!scope.equalsIgnoreCase("MOBO")) && (!scope.equalsIgnoreCase("SMS,MMS,WAP,DC,TL,PAYMENT,MOBO"))) {
				session.setAttribute("scope", "MOBO");
				session.setAttribute("clientId", clientIdWeb);
				session.setAttribute("clientSecret", clientSecretWeb);
				session.setAttribute("postOauth", postOauth);
				session.setAttribute("redirectUri", redirectUri);

				phoneTextBox = (String) session.getAttribute("phoneTextBox");
				subjectTextBox = (String) session.getAttribute("subjectTextBox");
				messageTextBox = (String) session.getAttribute("messageTextBox");
				groupCheckBox  = (String) session.getAttribute("groupCheckBox");
				fileName = (String) session.getAttribute("fileName");

				String params = "";
				if (subjectTextBox !=  null) params += "&subjectTextBox=" + subjectTextBox;
				if (phoneTextBox !=  null) params += "&phoneTextBox=" + phoneTextBox;
				if (groupCheckBox !=  null) params += "&groupCheckBox="+ groupCheckBox;
				if (messageTextBox !=  null) params += "&messageTextBox="+ messageTextBox;
				if (fileName !=  null) params += "&fileName="+ fileName;
				params = response.encodeURL(params);
				out.println("params:" + params);
				System.out.println("params:" + params);
				response.sendRedirect("oauth.jsp?getExtCode=yes"+ params);
			}   

			phoneTextBox = (String) session.getAttribute("phoneTextBox");
			//if we get redirected input parameters could be null
			if (phoneTextBox != null)
			{
				phoneTextBox = phoneTextBox.trim();
				subjectTextBox = (String) session.getAttribute("subjectTextBox");
				messageTextBox = (String) session.getAttribute("messageTextBox");
				groupCheckBox  = (String) session.getAttribute("groupCheckBox");

				fileName = (String) session.getAttribute("fileName");

				//Parse the phone addresses
				String [] address = phoneTextBox.split(",");
				for(String a : address) 
				{
					if(a.length() >= 10)
					{	
						 String expression = "[+]?[0-15]*[0-9]+$";  
						 CharSequence inputStr = a;  
						 Pattern pattern = Pattern.compile(expression);  
						 Matcher matcher = pattern.matcher(inputStr);  
						 if(matcher.matches()){  
							a = "tel:"+a;
							addresses.add(a);
						 }		  
					}
					else if((a.length()>2) && (a.length()<=8))
					{
						String expression = "[0-15]*[0-9]+$";  
						CharSequence inputStr = a;  
						Pattern pattern = Pattern.compile(expression);  
						Matcher matcher = pattern.matcher(inputStr);  
						if(matcher.matches()){  
							a = "short:"+a;
							addresses.add(a);
						 }
					}
					else if(a.contains("@"))
					{
						a =a;
						addresses.add(a);
					}
					else
					{
						badAddresses.add(a);
					}
				}		
			}
			else
			{
				sendMsgBtnClicked = false;	
				phoneTextBox = "";
				subjectTextBox = "";
				messageTextBox = "";
				groupCheckBox = "";
				fileName = "";
			}
		} 
	%>
	<%
		String requestBody = "";
		MultiPart mPart;
		MediaType contentBodyType = null;
		if( sendMsgBtnClicked ) 
		{
			  JSONArray numbers = new JSONArray(addresses);
			  JSONObject requestObject = new JSONObject();
			  requestObject.put("Addresses", numbers);	//numbers
			  requestObject.put("Text", messageTextBox.trim());
			  requestObject.put("Subject", subjectTextBox.trim());

			  if (groupCheckBox != null && groupCheckBox.equals("on")) 
			  {
				   requestObject.put("Group", "true");
			  }
			  else
			  {
				   requestObject.put("Group", "false");
			  }	
			  requestBody += requestObject.toString();
		  //Check whether attachments are present
		  //if present do multipart/related or else single body part
			if (fileName != null && fileName != "")
			{
				client = new RestClient(endpoint, new MediaType ("multipart","related"), MediaType.APPLICATION_JSON_TYPE);
			}
			else
			{
				client = new RestClient(endpoint, new MediaType ("application", "json"), MediaType.APPLICATION_JSON_TYPE);
			}
			if ((fileName != null && fileName != "")) {
				mPart = new MultiPart(new MediaType ("multipart","related"));
				MultiPart mPart1 = mPart.bodyPart(new BodyPart(requestBody,MediaType.APPLICATION_JSON_TYPE));
				mPart.getBodyParts().get(0).getHeaders().add("Content-Transfer-Encoding", "8bit");
				mPart.getBodyParts().get(0).getHeaders().add("Content-Disposition","form-data; name=\"root-fields\"");
				mPart.getBodyParts().get(0).getHeaders().add("Content-ID", "<startpart>");
				String[] attachments = fileName.split(",");
				MediaType[] medTyp = new MediaType[attachments.length];
				ServletContext context = getServletContext();
				for (int i = 0; i < attachments.length; i++) {
					java.util.Map<String, String> conttypeattr = new java.util.HashMap<String, String>();
					conttypeattr.put("name", attachments[i]);
					//media type
					FileDataBodyPart fIlE = new FileDataBodyPart();
					MediaType media = fIlE.getPredictor().getMediaTypeFromFileName("/" + attachments[i]);
					medTyp[i] = new MediaType(media.getType(),media.getSubtype(), conttypeattr);
					//
					int hdrIndex = i + 1;
					mPart1 = mPart1.bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[i]),medTyp[i]));
					mPart1.getBodyParts().get(hdrIndex).getHeaders().add("Content-Disposition","form-data; name=\"file" + i	+ "\"; filename=\""	+ attachments[i] + "\"");
					mPart1.getBodyParts().get(hdrIndex).getHeaders().add("Content-Transfer-Encoding", "binary");
					mPart1.getBodyParts().get(hdrIndex).getHeaders().add("Content-ID", attachments[i]);
					mPart1.getBodyParts().get(hdrIndex).getHeaders().add("Content-Location", attachments[i]);
				}
				client.addRequestBody(mPart);
			}
			else 
			{
			    client.addRequestBody(requestBody);				 
			}
			responze = client.invoke(com.att.rest.HttpMethod.POST,accessToken);		
			statusCode = client.getHttpResponseCode();
			//clean up input parameters from the session
			session.removeAttribute("phoneTextBox");
			session.removeAttribute("messageTextBox");
			session.removeAttribute("subjectTextBox");
			session.removeAttribute("groupCheckBox");
			session.removeAttribute("fileName");
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
				<h1>AT&T Sample Mobo Application 1 Basic Mobo Service
					Application</h1>
				<h2>Feature 1: Send Message</h2>
			</div>
		</div>
		<br clear="all" />
		<form method="post" name="sendMessageButton"
			enctype="multipart/form-data"
			action="MOBO.jsp?sendMessageButton=true">
			<div id="navigation">
				<table border="0" width="100%">
					<tbody>
						<tr>
							<td width="20%" valign="top" class="label">Address:</td>
							<td class="cell"><input name="phoneTextBox" type="text"
								maxlength="60" value="<%=phoneTextBox%>" style="width: 291px;" />
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Message:</td>
							<td class="cell"><textarea name="messageTextBox" rows="2"
									cols="20" style="height: 99px; width: 291px;"><%=messageTextBox%></textarea>
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Subject:</td>
							<td class="cell"><textarea name="subjectTextBox" rows="2"
									cols="20" style="height: 99px; width: 291px;"><%=subjectTextBox%></textarea>
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Group:</td>
							<td class="cell"><input name="groupCheckBox" type="checkbox" id="phoneTextBox"/></td>
						</tr>
					</tbody>
				</table>
			</div>
			<div id="extra">
				<div class="warning">
					<strong>WARNING:</strong><br />total size of all attachments
					cannot exceed 600 KB.
				</div>
			</div>
			<div id="extra">
				<table border="0" width="100%">
					<tbody>
						<tr>
							<td valign="bottom" class="style1">Attachment 1:</td>
							<td class="cell"><input type="file" name="FileUpload1"
								id="FileUpload1" /></td>
						</tr>
						<tr>
							<td valign="bottom" class="style1">Attachment 2:</td>
							<td class="cell"><input type="file" name="FileUpload2"
								id="FileUpload2" /></td>
						</tr>
						<tr>
							<td valign="bottom" class="style1">Attachment 3:</td>
							<td class="cell"><input type="file" name="FileUpload3"
								id="FileUpload3" /></td>
						</tr>
						<tr>
							<td valign="bottom" class="style1">Attachment 4:</td>
							<td class="cell"><input type="file" name="FileUpload4"
								id="FileUpload4" /></td>
						</tr>
						<tr>
							<td valign="bottom" class="style1">Attachment 5:</td>
							<td class="cell"><input type="file" name="FileUpload5"
								id="FileUpload5" /></td>
						</tr>
					</tbody>
				</table>
				<table>
					<tbody>
						<tr>
							<td><input type="submit" name="sendMessageButton"
								value="Send Message" id="sendMessageButton" /></td>
						</tr>
					</tbody>
				</table>
			</div>
			<br clear="all" />
		</form>
		<%	
		    if (sendMsgBtnClicked)
		    {
					if (statusCode ==200 || statusCode == 201) 
					{
						JSONObject rpcObject = new JSONObject(responze);
						ID = rpcObject.getString("Id");
						groupCheckBox = null;
					%>
						<div class="successWide">
						<strong>SUCCESS</strong><br /> <strong>Message ID: <%=ID%></strong>
						</div>
						<br />
					<%
					}
					else if (groupBoxError)
					{
					%>
						<div class="errorWide">
						<strong>ERROR:</strong><br /> <strong>Cant select group and short</strong>
						</div>
						<br />
					<%
					}
					else
					{
					%>
						<div class="errorWide">
						<strong>ERROR:</strong><br /> <strong><%=responze%></strong>
						</div>
						<br />
					<%
					}
			}
		%>
		<div align="center">
			<div id="sendMessagePanel"
				style="font-family: Calibri; font-size: XX-Small;"></div>
		</div>
		<br clear="all" />
		<div id="footer">
			<div
				style="float: right; width: 20%; font-size: 9px; text-align: right">Powered
				by AT&amp;T Cloud Architecture</div>
			<p>
				© 2012 AT&amp;T Intellectual Property. All rights reserved. <a
					href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
				<br> The Application hosted on this site are working examples
				intended to be used for reference in creating products to consume
				AT&amp;T Services and not meant to be used as part of your product.
				The data in these pages is for test purposes only and intended only
				for use as a reference in how the services perform. <br> For
				download of tools and documentation, please go to <a
					href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
				<br> For more information contact <a
					href="mailto:developer.support@att.com">developer.support@att.com</a>
		</div>		
</body>
</html>