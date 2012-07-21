<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="com.sun.jersey.multipart.file.*" %>
<%@ page import="com.sun.jersey.multipart.BodyPart" %>
<%@ page import="com.sun.jersey.multipart.MultiPart" %>
<%@ page import="com.att.rest.*" %>
<%@ page import="java.net.*" %>
<%@ page import="javax.ws.rs.core.*" %>
<%@ page import="org.json.*"%>
<%@ page import="org.json.JSONObject"%>
<%@ page import="org.json.JSONArray"%>
<%@ page import="org.w3c.dom.*" %>
<%@ page import="javax.xml.parsers.*" %>
<%@ page import="javax.xml.transform.*" %>
<%@ page import="javax.xml.transform.stream.*" %>
<%@ page import="org.apache.commons.fileupload.*"%>
<%@ page import="javax.xml.transform.dom.*" %>
<%@ page import="java.io.*" %>
<%@ page import="java.util.List"%>  
<%@ page import="java.util.Iterator"%>  
<%@ page import="java.io.File"%>  
<%@ page import="org.apache.commons.fileupload.*"%>  
<%@ page import="org.apache.commons.fileupload.disk.*"%>  
<%@ page import="org.apache.commons.fileupload.servlet.*"%>
<%@ include file="getToken.jsp"%>
<%@ page import="java.util.*"%>
<%@ page import="java.net.URL"%>
<%@ page import="java.net.URLConnection"%>
<%@ page import="java.net.URLEncoder,java.io.*"%>
<%@ page import="org.apache.commons.httpclient.HttpClient"%>
<%@ page import="org.apache.commons.httpclient.methods.PostMethod"%>
<%@ page import="org.apache.http.entity.mime.MultipartEntity"%>
<%@ page import="org.apache.http.params.CoreProtocolPNames"%>
<%@ page import="org.apache.http.util.EntityUtils"%>
<%@ page import="org.apache.http.impl.client.DefaultHttpClient"%>
<%@ page import="org.apache.http.entity.mime.content.ContentBody"%>
<%@ page import="org.apache.http.entity.mime.content.FileBody"%>
<%@ page import="org.apache.http.HttpEntity"%>
<%@ page import="org.apache.http.HttpResponse"%>
<%@ page import="org.apache.http.client.methods.HttpPost"%>

<%@ page import="org.apache.http.client.ResponseHandler"%>

<%@ page import="org.apache.http.client.methods.HttpGet"%>
<%@ page import="org.apache.http.impl.client.BasicResponseHandler"%>

<%@ page import="org.apache.commons.httpclient.HttpClient"%>
<%@ page import="org.apache.commons.httpclient.HostConfiguration"%>
<%@ page import="org.apache.commons.httpclient.methods.GetMethod"%>
<%@ page import="org.apache.http.entity.mime.content.StringBody"%>
<%@ page import="org.apache.http.entity.FileEntity"%>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head>
    <title>AT&amp;T Sample Speech Application - Speech to Text (Generic) Application</title>
    <meta content="text/html; charset=UTF-8" http-equiv="Content-Type" />
    <link rel="stylesheet" type="text/css" href="style/common.css"/>
</head>
    <body>
        <div id="container">
            <!-- open HEADER -->
            <div id="header">
                <div>
                    <div class="hcRight">
                        <%=new java.util.Date()%>
                    </div>
                    <div class="hcLeft">
                        Server Time:</div>
                </div>
                <div>
                    <div class="hcRight">
                        <script language="JavaScript" type="text/javascript">
                            var myDate = new Date();
                            document.write(myDate);
                        </script>
                    </div>
                    <div class="hcLeft">
                        Client Time:</div>
                </div>
                <div>
                    <div class="hcRight">
                        <script language="JavaScript" type="text/javascript">
                            document.write("" + navigator.userAgent);
                        </script>
                    </div>
                    <div class="hcLeft">
                        User Agent:</div>
                </div>
                <br clear="all" />
            </div>
            <!-- close HEADER -->
            <div>
                <div class="content">
                    <h1>
                        AT&amp;T Sample Speech Application - Speech to Text (Generic) Application</h1>
                    <h2>
                        Feature 1: Speech to Text (Generic)</h2>
                </div>
            </div>
            <br />
            <br />
            
            <form name="SpeechToText" enctype="multipart/form-data" action="Speech.jsp?SpeechToText=true" method="post">
                <div class="navigation">
                    <table border="0" width="100%">
                        <tbody>
                            <tr>
                                <td width="20%" valign="top" class="label">Audio File:</td>
                    
                                <td class="cell"><input name="f1" type="file"></td>
                                 
                            </tr>
                                  <tr>
                                  <td />
                                     <td>
                                           <div id = "extraleft"> 
                                            <div class="warning">
                            <strong>Note:</strong><br />
                            If no file chosen, a <a href="./bostonSeltics.wav">default.wav</a> will be loaded on submit.<br />
                            <strong>Speech file format constraints:</strong> <br />
                                *   16 bit PCM WAV, single channel, 8 kHz sampling<br />
                                *	AMR (narrowband), 12.2 kbit/s, 8 kHz sampling<br />
                        </div>
                                        </div>
                                        </div>
                                        </tr>
                        </tbody>
                    </table>
                </div>
                <div id="extra">
                    <table>
                        <tbody>
                            <tr>
                                <td><button type="submit" name="SpeechToText">Submit</button></td>
                                   
                            </tr>
                        </tbody>
                    </table>
                </div>
            </form>
            <br clear="all" />
        



<% 
String fileName = "";
String url = FQDN + "/rest/1/SpeechToText";
String SpeechToText = request.getParameter("SpeechToText");
String f1 = request.getParameter("f1");
String fileTest = "";
 
if(SpeechToText!=null){
            
			boolean isMultipart = ServletFileUpload.isMultipartContent(request);
 
			if (isMultipart)
			{			
					DiskFileUpload fu = new DiskFileUpload();
					List fileItems = fu.parseRequest(request);
					Iterator itr = fileItems.iterator();
					
					while(itr.hasNext()) {
					  FileItem fi = (FileItem)itr.next();
					  if(!fi.isFormField()) {
							File fNew = new File(application.getRealPath("/"), fi.getName());
							fileName = fileName + fi.getName();
							if(!(fi.getName().equalsIgnoreCase(""))){
								 fi.write((fNew));
							  }		
					  }
					
					}
			}
				
				
						File file = new File(application.getRealPath("/") + fileName);
						if(!file.isFile())
						{
						file = new File(application.getRealPath("/") + "bostonSeltics.wav");
							fileName = fileName + "bostonSeltics.wav";
						}
				
						String newPath = application.getRealPath("/") + fileName;
						String fileFormat = newPath.substring(newPath.length() - 3, newPath.length());
						
						
						DefaultHttpClient httpclient = new DefaultHttpClient();
						
						HttpPost httppost = new HttpPost(url);

						FileEntity reqEntity = new FileEntity(file, "binary/octet-stream");

						httppost.setEntity(reqEntity);
						reqEntity.setContentType("binary/octet-stream");

					
						httppost.addHeader("Authorization","Bearer " + accessToken);

						if (fileFormat.equals("amr")) {
						httppost.addHeader("Content-Type","audio/amr");
						}
						else
						if (fileFormat.equals("wav")) {
						httppost.addHeader("Content-Type","audio/wav");
						}
						
						httppost.addHeader("Accept","application/json");
						
						HttpResponse responze = httpclient.execute(httppost);
						HttpEntity resEntity = responze.getEntity();
						
						System.out.println(responze.getStatusLine());
						String result = EntityUtils.toString(resEntity);
					
					
						int statusCode = responze.getStatusLine().getStatusCode();
						
						if(statusCode == 200 || statusCode == 201) {
						
						JSONObject jsonResponse = new JSONObject(result);
						JSONArray parameters = jsonResponse.names();

						%>
					<div class="successWide">
					<strong>SUCCESS:</strong><br />
					Response parameters listed below.
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
								String [] paramsArray = {"WordScores" , "Confidence", "Grade", "ResultText", "Words", "LanguageId", "Hypothesis"};

									 JSONObject jsonObject = new JSONObject(result);
									 JSONObject recognition = jsonObject.getJSONObject("Recognition");
									 JSONArray nBest = recognition.getJSONArray("NBest");	
							
						
						     for (int i=0; i<nBest.length(); ++i) { 
							  JSONObject nBestjsonObject = (JSONObject)nBest.get(i);%>
                    <tr>
                    <td class="cell" align="center"><em>ResponseId</em></td>
                    <td class="cell" align="center"><em><%=recognition.getString("ResponseId")%></em></td>
                   </tr>
                	<tr>
                    <td class="cell" align="center"><em>Hypothesis</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("Hypothesis")%></em></td>
                   </tr>        
                	<tr>
                    <td class="cell" align="center"><em>LanguageId</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("LanguageId")%></em></td>
                   </tr>        
                	<tr>
                    <td class="cell" align="center"><em>Confidence</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("Confidence")%></em></td>
                   </tr>        
                   <tr>
                    <td class="cell" align="center"><em>Grade</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("Grade")%></em></td>
                   </tr>  
                	<tr>
                    <td class="cell" align="center"><em>ResultText</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("ResultText")%></em></td>
                   </tr>  
                	<tr>
                    <td class="cell" align="center"><em>Words</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("Words")%></em></td>
                   </tr>
                	<tr>
                    <td class="cell" align="center"><em>WordScores</em></td>
                	<td class="cell" align="center"><em><%=nBestjsonObject.getString("WordScores")%></em></td>
                   </tr> 
						<% } %>
					</tbody>
					</table>
					</div><br/>
						<%
						}
					else {
							JSONObject jsonResponse = new JSONObject();
						%>
						<div class="errorWide">
						<strong>ERROR: Invalid file specified. Valid file formats are .wav and .amr.</strong><br />
						</div><br/>
						<%
					}
					httpclient.getConnectionManager().shutdown();
}
%>
            <br clear="all" />
            <div id="footer">
                <div style="float: right; width: 20%; font-size: 9px; text-align: right">
                    Powered by AT&amp;T Cloud Architecture</div>
                <p>
                    &#169; 2012 AT&amp;T Intellectual Property. All rights reserved. <a href="http://developer.att.com/"
                        target="_blank">http://developer.att.com</a>
                    <br />
                    The Application hosted on this site are working examples intended to be used for
                    reference in creating products to consume AT&amp;T Services and not meant to be
                    used as part of your product. The data in these pages is for test purposes only
                    and intended only for use as a reference in how the services perform.
                    <br />
                    For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/"
                        target="_blank">https://devconnect-api.att.com</a>
                    <br />
                    For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a></p>
            </div>
        </div>
        <p>
            &nbsp;</p>
    </body>
</html>

