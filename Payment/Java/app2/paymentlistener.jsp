<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="java.util.*" %>
<%@ page import="org.apache.commons.fileupload.servlet.*" %>
<%@ page import="org.apache.commons.fileupload.*" %>
<%@ page import="org.apache.commons.fileupload.disk.*" %>
<%@ page import="java.io.*" %>
<%@ page import="java.net.*" %>
<%@ page import="java.util.*" %>
<%@ page import="java.text.*" %>
<%@ page import="org.apache.commons.codec.binary.Base64" %>
<%



///paymentlistener.jsp
////////////////////////
//notificationId///////
///////////////////////

LinkedHashMap<String, String> paramLinkedMap = new LinkedHashMap<String, String>();

try{
String data=null;

InputStream is = request.getInputStream();
ByteArrayOutputStream baos = new ByteArrayOutputStream();

byte buf[] = new byte[1024];
int letti;

while ((letti = is.read(buf)) > 0)
baos.write(buf, 0, letti);

data = new String(baos.toByteArray());
String notificationId = "";

int random = (int)(Math.random()*10000000);
FileOutputStream fous = new FileOutputStream(application.getRealPath("/notifications/"+random+".txt"));
fous.write(outFile);
fous.close();
String decodedText = "";

StringTokenizer st = new StringTokenizer(data, "&");
String token;
while(st.hasMoreTokens()){
token = st.nextToken();
paramLinkedMap.put(URLDecoder.decode(token.substring(0, token.indexOf("="))),
URLDecoder.decode(token.substring(token.indexOf("=")+1, token.length())));
}

PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/notifications/" + random + "." + type + ".txt"))), false);
String toSave = notificationId;
outWrite.write(toSave);
outWrite.close();
}catch(Exception e){

}
%>
