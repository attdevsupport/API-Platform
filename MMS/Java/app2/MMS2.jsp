<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
<title>AT&amp;T Sample MMS Application 2 - MMS Coupon
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
	<%@ page import="org.apache.commons.httpclient.*"%>
	<%@ page import="org.apache.commons.httpclient.methods.*"%>
	<%@ page import="org.json.*"%>
	<%@ page import="org.w3c.dom.*"%>
	<%@ page import="javax.xml.parsers.*"%>
	<%@ page import="javax.xml.transform.*"%>
	<%@ page import="javax.xml.transform.stream.*"%>
	<%@ page import="javax.xml.transform.dom.*"%>
	<%@ include file="getToken.jsp"%>

	<%

String sendMms = request.getParameter("sendMms");	
String getMmsDeliveryStatus = request.getParameter("getMmsDeliveryStatus");
String contentBodyFormat = "FORM-ENCODED";		
String priority = "High";
String responseFormat = "json";
String requestFormat = "json";
String endpoint = FQDN + "/rest/mms/2/messaging/outbox";
RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("message.txt"),"rw");
String subject = inFile1.readLine();
inFile1.close();
String readAddresses = "";
if (request.getParameter("address")!=null)
        readAddresses= request.getParameter("address");
String[] address = new String[10];


if (!readAddresses.isEmpty()) {
    address = readAddresses.split(",");
}

String mmsId = (String) session.getAttribute("mmsId");
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

				<h1>AT&amp;T Sample MMS Application 2 - MMS Coupon Application</h1>
				<h2>Feature 1: Send coupon image to list of subscribers</h2>

			</div>
		</div>
		<form method="post" name="sendMms">
			<div id="navigation">

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td width="20%" valign="top" class="label">Phone:</td>
							<td class="cell"><input size="20" name="address"
								value="<%=readAddresses%>" style="width: 90%">
							</td>
						</tr>
						<tr>
							<td valign="top" class="label">Subject:</td>
							<td class="cell"><%=subject%></td>
						</tr>
					</tbody>
				</table>

				<div class="warning">
					<strong>WARNING:</strong><br /> total size of all attachments
					cannot exceed 600 KB.
				</div>

			</div>
			<div id="extra">

				<table border="0" width="100%">
					<tbody>
						<tr>
							<td valign="top" class="label">Attachment:</td>
							<td class="cell"><div
									style="width: 250px; background: #fc9; border: 3px double #006; text-align: center; padding: 25px">
									<em><img width="250px" src="coupon.jpg" /> </em>
								</div>
							</td>
						</tr>
					</tbody>
				</table>
				<table>
					<tbody>
						<tr>
							<td><br />
								<button type="submit" name="sendMms">Send Coupon</button></td>
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

		String attachment = "coupon.jpg";

		MediaType contentBodyType = null;
		String requestBody = "";
		contentBodyType = new MediaType("multipart","related");

   		FileDataBodyPart fIlE = new FileDataBodyPart();
           
        java.util.Map<String, String> conttypeattr = new java.util.HashMap<String, String>();
        conttypeattr.put("name",attachment); 
        MediaType media = fIlE.getPredictor().getMediaTypeFromFileName("/" + attachment);
        MediaType medTyp = new MediaType(media.getType(),media.getSubtype(),conttypeattr);           
           
 
   		 
   		ServletContext context = getServletContext();


		// This currently uses a proprietary rest client to assemble the request body that does not follow SMIL standards. It is recommended to follow SMIL standards to ensure attachment delivery.
		RestClient client = new RestClient(endpoint, contentBodyType, MediaType.APPLICATION_JSON_TYPE);
    	JSONObject requestObject = new JSONObject();

		   String responze ="";
            List addresses = new java.util.ArrayList();
            for(String a : address) {
                addresses.add("\"tel:" + a + "\"");
            }
            if(addresses.size()!=1) {
                requestObject.put("Address", addresses);
            } else {
                requestObject.put("Address", addresses.get(0).toString().split("\"")[1]);
            }
		 	requestObject.put("Priority", priority);
			requestObject.put("Subject", subject);
		 	requestBody += requestObject.toString();
			MultiPart mPart = new MultiPart().bodyPart(new BodyPart(requestBody,MediaType.APPLICATION_JSON_TYPE));
			mPart.bodyPart(new BodyPart(context.getResourceAsStream(attachment), medTyp));
		 	
		 	mPart.getBodyParts().get(0).getHeaders().add("Content-Transfer-Encoding", "8bit");
	 		mPart.getBodyParts().get(0).getHeaders().add("Content-Disposition","form-data; name=\"root-fields\"");
	 		mPart.getBodyParts().get(0).getHeaders().add("Content-ID", "<startpart>");
            mPart.getBodyParts().get(1).getHeaders().add("Content-Disposition","attachment; filename="+attachment);
            mPart.getBodyParts().get(1).getHeaders().add("Content-Transfer-Encoding","Binary");
            mPart.getBodyParts().get(1).getHeaders().add("Content-ID",attachment);	
            
			client.addRequestBody(mPart);
			responze = client.invoke(com.att.rest.HttpMethod.POST, accessToken);
		
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

			%>
		</table>
		<%
}
%>

		<div id="wrapper">
			<div id="content">

				<h2>
					<br /> Feature 2: Check Delivery Status for each Recipient
				</h2>

			</div>
		</div>
		<div id="navigation">
			<form method="post">
				<table border="0" width="100%">
					<tbody>
						<tr>
							<td class="cell"><button type="submit"
									name="getMmsDeliveryStatus">Check Status</button>
							</td>
						</tr>
					</tbody>
				</table>
		</div>
		<div id="extra"></div>
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
           int statusCode = client.executeMethod(method); 
           if(statusCode==200) {
              	JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
              	JSONObject deliveryInfoList = new JSONObject(jsonResponse.getString("DeliveryInfoList"));
              	JSONArray deliveryInfoArray = new JSONArray(deliveryInfoList.getString("DeliveryInfo"));
              	JSONObject deliveryInfo = new JSONObject(deliveryInfoArray.getString(0));
              	%>
		<div class="successWide">
			<strong>SUCCESS:</strong><br /> Messages Delivered
		</div>
		<br />

		<div align="center">
			<table width="500" cellpadding="1" cellspacing="1" border="0">
				<thead>
					<tr>
						<th width="50%" class="label">Recipient</th>
						<th width="50%" class="label">Status</th>
					</tr>
				</thead>
				<tbody>
					<%
                for(int j=0; j<deliveryInfoArray.length(); j++) {
                    deliveryInfo = new JSONObject(deliveryInfoArray.getString(j));
                    %>
					<tr>
						<td class="cell" align="center"><%=deliveryInfo.getString("Address")%></td>
						<td class="cell" align="center"><%=deliveryInfo.getString("DeliveryStatus")%></td>
					</tr>
					<%
                }
                %>
				</tbody>
			</table>
		</div>
		</form>
		<%
           } else {
           	%>
		<div class="errorWide">
			<strong>ERROR:</strong><br />
			<%=method.getResponseBodyAsString()%>
		</div>
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
