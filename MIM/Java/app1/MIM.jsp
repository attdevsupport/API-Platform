<%
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">

<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
<meta name="generator" content="HTML Tidy, see www.w3.org" />

<title>AT&amp;T Sample Mim Application 1 - Basic Mim Service
    Application</title>
<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type" />
<link rel="stylesheet" type="text/css" href="style/common.css" />
<style type="text/css">
.style2 {
    width: 491px;
}

#Submit1 {
    width: 213px;
}

.style3 {
    font-size: x-small;
}
</style>
</head>

<body>
    <%@ page contentType="text/html; charset=iso-8859-1" language="java"%>
    <%@ page import="org.apache.commons.httpclient.*"%>
    <%@ page import="org.apache.commons.httpclient.methods.*"%>
    <%@ page import="org.json.JSONObject"%>
    <%@ page import="org.apache.commons.codec.binary.Base64"%>
    <%@ page import="org.apache.http.HttpEntity"%>
    <%@ page import="org.json.JSONArray"%>
    <%@ page import="java.util.*"%>
    <%@ page import="java.net.URL"%>
    <%@ page import="java.net.URLConnection"%>
    <%@ page import="java.net.URLEncoder,java.io.*"%>
    <%@ page import="java.net.URLDecoder,java.io.*"%>
    <%@ page import="org.apache.commons.httpclient.HttpClient"%>
    <%@ page import="org.apache.commons.httpclient.methods.PostMethod"%>
    <%@ page import="org.apache.http.HttpResponse"%>
    <%@ page import="org.apache.http.util.EntityUtils"%>
    <%@ page import="java.util.List,java.util.Iterator"%>
    <%@ page import="sun.misc.BASE64Encoder"%>
    <%@ page import="sun.misc.BASE64Decoder"%>
    <%@ page import="org.apache.commons.codec.binary.Base64"%>
    <%@ include file="config.jsp"%>
    <%
String scope = "MIM";

//Buttons
String getMsgHeadersButton = request.getParameter("getMsgHeadersButton");
String msgContent = request.getParameter("msgContent");

String postOauth = "MIM.jsp?getMsgHeadersButton=true";

//Fields
String HeaderCount = request.getParameter("HeaderCount");
if(HeaderCount==null || HeaderCount.equalsIgnoreCase("null"))
        HeaderCount = (String) session.getAttribute("headercount");
if(HeaderCount==null || HeaderCount.equalsIgnoreCase("null"))
        HeaderCount = "";
session.setAttribute("headercount",HeaderCount);
    
String IndexCursor = request.getParameter("IndexCursor");
if(IndexCursor==null || IndexCursor.equalsIgnoreCase("null"))
    IndexCursor = (String) session.getAttribute("IndexCursor");
if(IndexCursor==null || IndexCursor.equalsIgnoreCase("null"))
    IndexCursor = "";
session.setAttribute("IndexCursor",IndexCursor);

String MessageId = request.getParameter("MessageId");
if(MessageId==null || MessageId.equalsIgnoreCase("null"))
        MessageId = (String) session.getAttribute("MessageId");
if(MessageId==null || MessageId.equalsIgnoreCase("null"))
        MessageId = "";
session.setAttribute("MessageId",MessageId);

String PartNumber = request.getParameter("PartNumber");
if(PartNumber==null || PartNumber.equalsIgnoreCase("null"))
        PartNumber = (String) session.getAttribute("PartNumber");
if(PartNumber==null || PartNumber.equalsIgnoreCase("null"))
        PartNumber = "";
session.setAttribute("PartNumber",PartNumber);
%>
    <div id="container">
        <!-- open HEADER -->

        <div id="header">
            <div>
                <div id="hcLeft">Server Time:</div>

                <div id="hcRight">
                    <span id="serverTimeLabel"><%=new java.util.Date()%></span>
                </div>
            </div>

            <div>
                <div id="hcLeft">Client Time:</div>

                <div id="hcRight">
                    <script language="JavaScript" type="text/javascript">
                        var myDate = new Date();
                        document.write(myDate);
                    </script>
                </div>
            </div>

            <div>
                <div id="hcLeft">User Agent:</div>

                <div id="hcRight">
                    <script language="JavaScript" type="text/javascript">
                        document.write("" + navigator.userAgent);
                    </script>
                </div>
            </div>
            <br clear="all" />
        </div>
        <!-- close HEADER -->

        <div id="wrapper">
            <div id="content">
                <h1>AT&amp;T Sample Mim Application 1 - Basic Mim Service
                    Application</h1>

                <h2>Feature 1: Get Message Header</h2>
            </div>
        </div>
        <br clear="all" />

        <form method="post" action="" id="msgHeader">
            <div id="navigation">
                <table border="0" width="100%">
                    <tbody>
                        <tr>
                            <td width="20%" valign="top" class="label">Header Count:</td>

                            <td class="cell"><input name="HeaderCount" type="text"
                                maxlength="6" id="HeaderCount" style="width: 70px;" /></td>
                        </tr>

                        <tr>
                            <td width="20%" valign="top" class="label">Index Cursor:</td>

                            <td class="cell"><input name="IndexCursor" type="text"
                                maxlength="30" id="IndexCursor" style="width: 291px;" /></td>
                        </tr>
                    </tbody>
                </table>
                <br clear="all" />
            </div>

            <div id="extraleft">
                <div class="warning">
                    <strong>INFORMATION:</strong> Header Count is mandatory(1-500) and
                    Index cursor is optional. To Use MIM, mobile number should be
                    registered at messages.att.net
                </div>
            </div>

            <div id="extra">
                <table border="0" width="100%">
                    <tbody>
                        <tr>
                            <td class="cell"><input type="submit"
                                name="getMsgHeadersButton" value="Get Message Headers"
                                id="Submit1" />
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </form>
        <br clear="all" /> <br clear="all" />
        <div id="pnlHeader">
            <%  
    if(getMsgHeadersButton!=null) {
        String accessToken = request.getParameter("access_token");
        if(accessToken==null || accessToken == "null"){
            accessToken = (String)session.getAttribute("accessToken");
            session.setAttribute("accessToken", accessToken);
        }
        if((accessToken==null) || (!scope.equalsIgnoreCase("MIM")) && (!scope.equalsIgnoreCase("SMS,MMS,WAP,DC,TL,PAYMENT,MOBO,MIM"))) {
            session.setAttribute("scope", "MIM");
            session.setAttribute("clientId", clientIdWeb);
            session.setAttribute("clientSecret", clientSecretWeb);
            session.setAttribute("postOauth", postOauth);
            session.setAttribute("redirectUri", redirectUri);
            response.sendRedirect("oauth.jsp?getExtCode=yes");
        }
        
        String url = FQDN + "/rest/1/MyMessages";
        HttpClient client = new HttpClient();
        GetMethod method = new GetMethod(url);  
        method.setQueryString("HeaderCount=" + HeaderCount);
        method.addRequestHeader("Accept","application/json");
        method.addRequestHeader("Content-Type","application/json");
        method.addRequestHeader("Authorization","Bearer " + accessToken);
        int statusCode1 = client.executeMethod(method);  
        System.out.println("AccessToken: " + accessToken);
        System.out.println("method: " + method);            
        //System.out.println("StatusCode: " + statusCode);
       
		System.out.println("StatusCode before if: "+ statusCode1 + " msgContent:" + msgContent );
        if((statusCode1==200 || statusCode1==201) && msgContent==null) {
            JSONObject rpcObject = new JSONObject(method.getResponseBodyAsString());
            JSONObject mgeHdrList = rpcObject.getJSONObject("MessageHeadersList");
            JSONArray headersArray = mgeHdrList.getJSONArray("Headers");
            
            String [] paramsArray = {"MessageId" , "PartNumber", "ContentType", "ContentName", "From", "To", "Received", "Text", "Favourite", "Read", "Type", "Direction"};    %>
            <div class="successWide">
                <strong>SUCCESS</strong><br />
            </div>
            <div class="content" align="left">

                <table>
                    <tr>
                        <td width="10%" valign="middle" class="label">Header Count:</td>
                        <td class="cell" align="left"><span id="lblHeaderCount"
                            class="label"><%=mgeHdrList.getString("HeaderCount")%></span></td>
                    </tr>
                    <tr>
                        <td width="10%" valign="middle" class="label">Index Cursor:</td>
                        <td class="cell" align="left"><span id="lblIndexCursor"
                            class="label"><%=mgeHdrList.getString("IndexCursor")%></span></td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <div>
                                <table id="gvMessageHeaders" cellspacing="0" cellpadding="3"
                                    style="background-color: White; border-color: #CCCCCC; border-width: 1px; border-style: None; width: 100%; border-collapse: collapse;"
                                    rules="all">
                                    <tr style="color: White; background-color: #006699;">
                                        <th scope="col" class="style3">MessageId</th>
                                        <th scope="col" class="style3">PartNumber</th>
                                        <th scope="col" class="style3">ContentType</th>
                                        <th scope="col" class="style3">ContentName</th>
                                        <th scope="col" class="style3">From</th>
                                        <th scope="col" class="style3">To</th>
                                        <th scope="col" class="style3">Received</th>
                                        <th scope="col" class="style3">Text</th>
                                        <th scope="col" class="style3">Favourite</th>
                                        <th scope="col" class="style3">Read</th>
                                        <th scope="col" class="style3">Type</th>
                                        <th scope="col" class="style3">Direction</th>
                                    </tr>
                                    <%for (int i = 0; i < headersArray.length(); i++) {
                                    JSONObject inHeaders = headersArray.getJSONObject(i);
                                    %>
                                    <tr class="style3">
                                        <td class="style3"><%=inHeaders.getString("MessageId")%></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"><%=inHeaders.getString("From")%></td>
                                        <td class="style3"><%=inHeaders.getString("To")%></td>
                                        <td class="style3"><%=inHeaders.getString("Received")%></td>
                                        <td class="style3"><%=inHeaders.getString("Favorite")%></td>
                                        <%if(inHeaders.has("Text")){%>
                                        <td class="style3"><%=inHeaders.getString("Text")%></td>
                                        <%}%>
                                        <td class="style3"><%=inHeaders.getString("Read")%></td>
                                        <td class="style3"><%=inHeaders.getString("Type")%></td>
                                        <td class="style3"><%=inHeaders.getString("Direction")%></td>
                                        <%
                                    if(inHeaders.has("MmsContent")){
                                        JSONArray MmsContentArray = inHeaders.getJSONArray("MmsContent");
                                        for (int x = 0; x < MmsContentArray.length(); x++) {
                                        JSONObject inMmsContent = MmsContentArray.getJSONObject(x);%>
                                    
                                    <tr>
                                        <td class="style3"></td>
                                        <td class="style3"><%=inMmsContent.getString("PartNumber")%></td>
                                        <td class="style3"><%=inMmsContent.getString("ContentType")%></td>
                                        <td class="style3"><%=inMmsContent.getString("ContentName")%></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                        <td class="style3"></td>
                                    </tr>
                                    <%}
                                        }    %>
                                    <tr></tr>
                                    </tr>
                                    <%}%>
                                </table>
                            </div></td>
                    </tr>
                </table>
            </div>
        </div>
        <%}
				else if( getMsgHeadersButton!=null && request.getParameter("HeaderCount")==""){
			%><div class="errorWide">
			<strong>ERROR:</strong> Header Count field is empty.<br />
		    </div><br /><%
		method.releaseConnection();}
		else if( getMsgHeadersButton!=null && request.getParameter("HeaderCount")=="" && request.getParameter("IndexCursor")!=""){
			%><div class="errorWide">
			<strong>ERROR:</strong> Header Count field is empty.<br />
		    </div><br /><%
		method.releaseConnection();}
		else if((statusCode1 != 200 || statusCode1 != 201) && msgContent ==null){ %>
        <div class="errorWide">
            <strong>ERROR:</strong><br />
            <%=method.getResponseBodyAsString()%>
        </div>
        <br />
        <%}    
        method.releaseConnection();
    }%>



        <div id="wrapper">
            <div id="content">
                <br clear="all" />


                <h2>Feature 2: Get Message Content</h2>
            </div>
        </div>
        <br clear="all" />


        <form method="post" action="" id="msgContent">
            <div id="navigation">
                <table border="0" width="100%">
                    <tbody>
                        <tr>
                            <td width="20%" valign="top" class="label">Message ID:</td>

                            <td class="cell"><input name="MessageId" type="text"
                                maxlength="6" id="Text1" style="width: 291px;" /></td>
                        </tr>

                        <tr>
                            <td width="20%" valign="top" class="label">Part Number:</td>

                            <td class="cell"><input name="PartNumber" type="text"
                                maxlength="30" id="Text2" style="width: 291px;" /></td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <div id="extra">
                <table border="0" width="100%">
                    <tbody>
                        <tr>
                            <td class="cell"><input type="submit" name="msgContent"
                                value="Get Message Content" id="Submit1" />
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </form>
        <br clear="all" />


        <%  
		
    //If Check Delivery Status button was clicked, do this.
    if(msgContent!=null) {
		
               
        String accessToken = request.getParameter("access_token");
        if(accessToken==null || accessToken == "null"){
            accessToken = (String)session.getAttribute("accessToken");
            session.setAttribute("accessToken", accessToken);
        }
        if((accessToken==null) || (!scope.equalsIgnoreCase("MIM")) && (!scope.equalsIgnoreCase("SMS,MMS,WAP,DC,TL,PAYMENT,MOBO,MIM"))) {
            session.setAttribute("scope", "MIM");
            session.setAttribute("clientId", clientIdWeb);
            session.setAttribute("clientSecret", clientSecretWeb);
            session.setAttribute("postOauth", postOauth);
            session.setAttribute("redirectUri", redirectUri);
            response.sendRedirect("oauth.jsp?getExtCode=yes");
        }
            
           //Initialize the client
           String url = FQDN + "/rest/1/MyMessages/" + MessageId + "/" + PartNumber;   
           URL url1 = new URL(FQDN + "/rest/1/MyMessages/" + MessageId + "/" + PartNumber);
        
           HttpClient client = new HttpClient();
           GetMethod method = new GetMethod(url);  
           method.setQueryString("MessageId=" + MessageId + "&PartNumber=" + PartNumber);
           method.addRequestHeader("Content-Type","application/json");
           method.addRequestHeader("Accept","application/json");
           method.addRequestHeader("Authorization","Bearer " + accessToken);
           URLConnection conn = url1.openConnection();
           conn.setRequestProperty("Authorization","Bearer " + accessToken);
		   BASE64Encoder encoder = new BASE64Encoder();


          List <String> headerN = new java.util.ArrayList();
          List <String> headerV = new java.util.ArrayList();
          try{
                for (int i=0; ; i++) {
                    
                    String headerName = conn.getHeaderFieldKey(i);
                    String headerValue = conn.getHeaderField(i);
                    System.out.print("headerName: "+headerName+ " ");
                    System.out.println("header :" + headerValue);
                    headerN.add(headerName);
                    headerV.add(headerValue);
                    if (headerName == null && headerValue == null) {
                        // No more headers
                        break;
                    }
                    if (headerName == null) {
                        // The header value contains the server's HTTP version
                    }
                }
            }     catch (Exception e) {
                }
                String cont = "";
        
                String[] resHeaders = (String[]) headerN.toArray(new String[0]);
                String[] resValues = (String[]) headerV.toArray(new String[0]);
                String []tokens = new String [2];
                String []tokens1 = new String [2];
            
			
                if(resHeaders[4].equals("Content-Type"))    //works
                {
                    tokens = resValues[4].split(";");
                    System.out.println(tokens[0]);        //tokens[0] contains IMAGE/JPEG
                    
                    tokens1 = tokens[0].split("/"); //tokens1[0] contains IMAGE    
                    System.out.println(tokens1[0]);
                }
        //Send the request, parse based on HTTP status code
        int statusCode = client.executeMethod(method);
        System.out.println("response bod is: "+method.getResponseBodyAsString());
        if(statusCode==200) {
             if((tokens1[0]).equalsIgnoreCase("TEXT")){%>
        <div class="successWide" align="left">
            <strong>SUCCESS:</strong><br /><%=method.getResponseBodyAsString()%><br />
        </div>
        <br />
        <br />
        <%
          }
           
            else if((tokens1[0]).equalsIgnoreCase("APPLICATION")){%>
        <div class="successWide" align="left">
            <strong>SUCCESS:</strong><br />
        </div>
        <br />
        <br />
        <div style="text-align: center">
            <div id="smilpanel">
                <textarea id="TextBox1" class="aspNetDisabled"
                    style="height: 100px; width: 500px;" disabled="disabled" cols="20"
                    rows="2" name="TextBox1"><%=method.getResponseBodyAsString()%></textarea>
            </div>
        </div>
        <br />
        <%}
            else 
			 if((tokens1[0]).equals("IMAGE")){
				byte[] encoded = method.getResponseBodyAsString().getBytes();
				Base64 dat = new Base64();
				
		%>
		<div id="imagePanel" style="text-align: center;">
		<img src = "data:<%=tokens[0]%>;base64,<%=dat.encodeBase64String(method.getResponseBody())%>"/>
        <br />
            <%}
           method.releaseConnection();
      }
	  	   else if( msgContent!=null && request.getParameter("MessageId")==""){
			%><div class="errorWide">
			<strong>ERROR:</strong> Message Id field is empty.<br />
		    </div><br /><%
		method.releaseConnection();}
		   else if( msgContent!=null && request.getParameter("MessageId")=="" && request.getParameter("PartNumber")!=""){
			%><div class="errorWide">
			<strong>ERROR:</strong> Message Id field is empty.<br />
		    </div><br /><%
		method.releaseConnection();}
			else if( msgContent!=null && request.getParameter("PartNumber")==""){
			%><div class="errorWide">
			<strong>ERROR:</strong> Part Number field is empty.<br />
		    </div><br /><%
		method.releaseConnection();}
           else {%>
        <div class="errorWide">
            <strong>ERROR:</strong><br />
            <%=method.getResponseBodyAsString()%>
        </div>
        <br />
        <%}
}      %>

        <div id="footer">
            <div
                style="float: right; width: 20%; font-size: 9px; text-align: right">
                Powered by AT&amp;T Cloud Architecture</div>

            <p>
                &copy; 2012 AT&amp;T Intellectual Property. All rights reserved. <a
                    href="http://developer.att.com/" target="_blank">http://developer.att.com</a><br />
                The Application hosted on this site are working examples intended to
                be used for reference in creating products to consume AT&amp;T
                Services and not meant to be used as part of your product. The data
                in these pages is for test purposes only and intended only for use
                as a reference in how the services perform.<br /> For download of
                tools and documentation, please go to <a
                    href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a><br />
                For more information contact <a
                    href="mailto:developer.support@att.com">developer.support@att.com</a>
            </p>
        </div>
    </div>
</body>
</html>
