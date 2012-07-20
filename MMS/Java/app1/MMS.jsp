<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
<title>AT&amp;T Sample MMS Application 1 - Basic SMS Service
	Application</title>
<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
<link rel="stylesheet" type="text/css" href="style/common.css"/ >
<script type="text/javascript" src="js/helper.js">
	
</script>
<body>

	<%@ page contentType="text/html; charset=iso-8859-1" language="java"%>
	<%@ page import="com.sun.jersey.multipart.file.*"%>
	<%@ page import="com.sun.jersey.multipart.BodyPart"%>
	<%@ page import="com.sun.jersey.multipart.MultiPart"%>
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
	<%@ include file="getToken.jsp"%>
	<%

String redirectUri = "";
String getMmsDeliveryStatus = request.getParameter("getMmsDeliveryStatus");
String mmsId = request.getParameter("mmsId");
if (mmsId==null) mmsId = (String) session.getAttribute("mmsId");
if (mmsId==null) mmsId = "";
String sendMms = request.getParameter("sendMms");        
String contentBodyFormat = "FORM-ENCODED";        
String address = request.getParameter("address");
if(address==null || address.equalsIgnoreCase("null"))
    address = (String) session.getAttribute("addressSms2");
if(address==null || address.equalsIgnoreCase("null"))
    address = "";	
String fileName = "";
String subject = (String) session.getAttribute("subject");
if(subject==null || subject.equalsIgnoreCase("null"))
subject = "simple message to myself";		
String priority = "High";
String responseFormat = "json";
String requestFormat = "json";
String endpoint = FQDN + "/rest/mms/2/messaging/outbox";
String senderAddress = shortCode1;

//If Send MMS button was clicked, do this to get some parameters from the form.
if(request.getParameter("sendMms")!=null) {    
try{
        DiskFileUpload fu = new DiskFileUpload();
        List fileItems = fu.parseRequest(request);
        Iterator itr = fileItems.iterator();
        while(itr.hasNext()) {
          FileItem fi = (FileItem)itr.next();
          if(!fi.isFormField()) {
                File fNew= new File(application.getRealPath("/"), fi.getName());
            	fileName = fileName + "," + fi.getName();
            	if(!(fi.getName().equalsIgnoreCase(""))){
            		fi.write(fNew);
            	}
          } else if(fi.getFieldName().equalsIgnoreCase("address")) {
            	address = fi.getString();
                session.setAttribute("addressSms2",address);
          } else if(fi.getFieldName().equalsIgnoreCase("subject")) {
          	subject = fi.getString();
          	session.setAttribute("subject",subject);
          } 
        }		
} catch(Exception e){}; 
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

				<h1>AT&amp;T Sample MMS Application 1 - Basic MMS Service
					Application</h1>
				<h2>Feature 1: Send MMS Message</h2>

			</div>
		</div>
		<form method="post" name="sendMms" enctype="multipart/form-data"
			action="MMS.jsp?sendMms=true">
			<div id="navigation">

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td width="20%" valign="top" class="label">Phone:</td>
							<td class="cell"><input maxlength="16" size="12"
								name="address" value="<%=address%>" style="width: 90%">
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Message:</td>
							<td class="cell"><textarea rows="4" name="subject"
									style="width: 90%"><%=subject%></textarea>
							</td>
						</tr>
					</tbody>
				</table>

			</div>
			<div id="extra">

				<div class="warning">
					<strong>WARNING:</strong><br /> total size of all attachments
					cannot exceed 600 KB.
				</div>

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td valign="top" class="label">Attachment 1:</td>
							<td class="cell"><input name="f1" type="file">
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Attachment 2:</td>
							<td class="cell"><input name="f2" type="file">
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Attachment 3:</td>
							<td class="cell"><input name="f3" type="file">
							</td>
						</tr>
					</tbody>
				</table>
				<table>
					<tbody>
						<tr>
							<td><button type="submit" name="sendMms">Send MMS
									Message</button></td>
						</tr>
					</tbody>
				</table>


			</div>
			<br clear="all" />
			<div align="center"></div>
		</form>

		<% 
//If Send MMS button was clicked, do this.
if(request.getParameter("sendMms")!=null) {    
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
        if(fileName.equalsIgnoreCase(""))
        	fileName = (String) session.getAttribute("fileName");

		String attachmentsStr = fileName;
		String[] attachments = attachmentsStr.split(",");

		MediaType contentBodyType = null;
		String requestBody = "";
		MultiPart mPart;
           contentBodyType = new MediaType ("multipart","related");
   
			JSONObject requestObject = new JSONObject();
   		 	requestObject.put("Priority", priority);
   		    requestObject.put("Address", address);
   			requestObject.put("Subject", subject);
   		 	requestBody += requestObject.toString();
   		 mPart = new MultiPart().bodyPart(new BodyPart(requestBody,MediaType.APPLICATION_JSON_TYPE));
   		 mPart.getBodyParts().get(0).getHeaders().add("Content-Transfer-Encoding", "8bit");
		 mPart.getBodyParts().get(0).getHeaders().add("Content-Disposition","form-data; name=\"root-fields\"");
		 mPart.getBodyParts().get(0).getHeaders().add("Content-ID", "<startpart>");
		 MediaType[] medTyp = new MediaType[4];
         MediaType media = new MediaType();
   		 for(int i=1;i<attachments.length; i++) {
            java.util.Map<String, String> conttypeattr = new java.util.HashMap<String, String>();
            conttypeattr.put("name",attachments[i]);    
   			FileDataBodyPart fIlE = new FileDataBodyPart();
   			media = fIlE.getPredictor().getMediaTypeFromFileName("/" + attachments[i]);
            medTyp[i] = new MediaType(media.getType(),media.getSubtype(),conttypeattr);
   		}
   		 
   		ServletContext context = getServletContext();
   		if(attachments.length == 2){
   	   		mPart.bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[1]), medTyp[1]));
            mPart.getBodyParts().get(1).getHeaders().add("Content-Disposition","attachment; filename="+attachments[1]);
            mPart.getBodyParts().get(1).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(1).getHeaders().add("Content-ID",attachments[1]);

   		} else if(attachments.length == 3) {
   	   		mPart.bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[1]), medTyp[1])).bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[2]), medTyp[2]));
            mPart.getBodyParts().get(1).getHeaders().add("Content-Disposition","attachment; filename="+attachments[1]);
            mPart.getBodyParts().get(1).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(1).getHeaders().add("Content-ID",attachments[1]);
            mPart.getBodyParts().get(2).getHeaders().add("Content-Disposition","attachment; filename="+attachments[2]);
            mPart.getBodyParts().get(2).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(2).getHeaders().add("Content-ID",attachments[2]);

      
   		} else if(attachments.length == 4) {
   	   		mPart.bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[1]), medTyp[1])).bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[2]), medTyp[2])).bodyPart(new BodyPart(context.getResourceAsStream("/" + attachments[3]), medTyp[3]));
            mPart.getBodyParts().get(1).getHeaders().add("Content-Disposition","attachment; filename="+attachments[1]);
            mPart.getBodyParts().get(1).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(1).getHeaders().add("Content-ID",attachments[1]);
            mPart.getBodyParts().get(2).getHeaders().add("Content-Disposition","attachment; filename="+attachments[2]);
            mPart.getBodyParts().get(2).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(2).getHeaders().add("Content-ID",attachments[2]);
            mPart.getBodyParts().get(3).getHeaders().add("Content-Disposition","attachment; filename="+attachments[3]);
            mPart.getBodyParts().get(3).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(3).getHeaders().add("Content-ID",attachments[3]);

   		}

		// This currently uses a proprietary rest client to assemble the request body that does not follow SMIL standards. It is recommended to follow SMIL standards to ensure attachment delivery.
		RestClient client;
		client = new RestClient(endpoint, contentBodyType, MediaType.APPLICATION_JSON_TYPE);
		client.addRequestBody(mPart);
		String responze = client.invoke(com.att.rest.HttpMethod.POST, accessToken);
		
		if (client.getHttpResponseCode() == 201) {
			JSONObject rpcObject = new JSONObject(responze);
			mmsId = rpcObject.getString("Id");
			session.setAttribute("mmsId", mmsId);
	       	%>
		<div class="successWide">
			<strong>SUCCESS:</strong><br /> <strong>Message ID:</strong>
			<%=mmsId%>
		</div>
		<br />
		<%
		} else {
	    	%>
		<div class="errorWide">
			<strong>ERROR:</strong><br />
			<%=responze%>
		</div>
		<br />
		<%
		}
    } else { %>
		<div class="errorWide">
			<strong>ERROR:</strong><br /> Invalid Address Entered
		</div>
		<br />
		<%    }
}
%> 

		<div id="wrapper">
			<div id="content">

				<h2>
					<br /> Feature 2: Get Delivery Status
				</h2>

			</div>
		</div>

		<form method="post" name="getMmsDeliveryStatus" action="MMS.jsp">
			<div id="navigation">

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td width="20%" valign="top" class="label">Message ID:</td>
							<td class="cell"><input size="12" name="mmsId"
								value="<%=mmsId%>" style="width: 90%">
							</td>
						</tr>
					</tbody>
				</table>

			</div>
			<div id="extra">

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td class="cell"><button type="submit"
									name="getMmsDeliveryStatus">Get Status</button>
							</td>
						</tr>
					</tbody>
				</table>

			</div>
			<br clear="all" />
		</form>

		<%  
    //If Check Delivery Status button was clicked, do this.
       if(getMmsDeliveryStatus!=null) {

    	   String url = FQDN + "/rest/mms/2/messaging/outbox/" + mmsId;
           HttpClient client = new HttpClient();
           GetMethod method = new GetMethod(url);  
           method.setQueryString("access_token=" + accessToken + "&id=" + mmsId);
           method.addRequestHeader("Accept","application/" + responseFormat);
    method.addRequestHeader("Authorization","Bearer " + accessToken);        

           int statusCode = client.executeMethod(method); 
           if(statusCode==200) {
              	JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
              	JSONObject deliveryInfoList = new JSONObject(jsonResponse.getString("DeliveryInfoList"));
              	JSONArray deliveryInfoArray = new JSONArray(deliveryInfoList.getString("DeliveryInfo"));
              	JSONObject deliveryInfo = new JSONObject(deliveryInfoArray.getString(0));
              	%>
		<div class="successWide">
			<strong>SUCCESS:</strong><br /> <strong>Status:</strong>
			<%=deliveryInfo.getString("DeliveryStatus")%><br /> <strong>Resource
				URL:</strong>
			<%=deliveryInfoList.getString("ResourceUrl")%><br />
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
				<br> For more information contact <a
					href="mailto:developer.support@att.com">developer.support@att.com</a>
		</div>
	</div>

</body>
</html>
