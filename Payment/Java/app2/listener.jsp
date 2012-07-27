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
<%@ page import="java.util.regex.*"%>

<%LinkedHashMap<String, String> paramLinkedMap = new LinkedHashMap<String, String>();

try{
	String data=null;

	InputStream is = request.getInputStream();
	ByteArrayOutputStream baos = new ByteArrayOutputStream();

	byte buf[] = new byte[1024];
	int letti;

	while ((letti = is.read(buf)) > 0)
	{
	baos.write(buf, 0, letti);
	}

	data = new String(baos.toByteArray());
	String getSplit =data.split("<hub:notificationId>")[1];
	String notificationId = getSplit.substring(0,36);

	int random = (int)(Math.random()*10000000);
	PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/Notifications/" + random + ".txt"))), false);
	String toSave = notificationId;
	outWrite.write(toSave);

	outWrite.close();
    
}catch(Exception e){

} 
%>