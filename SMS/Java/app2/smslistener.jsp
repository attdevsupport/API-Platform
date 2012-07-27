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

	  String[] temp;

	  String delimiter = ",";
	  temp = data.split(delimiter);
	  /* print substrings */
  
	
    String dateTime = temp[0].substring(19,temp[0].length() -1);
	String messageId =temp[1].substring(18,temp[1].length() -2);
	String message =temp[2].substring(17,temp[2].length() -1);
	String senderAdd =temp[3].substring(27,temp[3].length() -1);
	String destinationAdd = temp[4].substring(28,temp[4].length() -3);

	int random = (int)(Math.random()*10000000);

	PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/Votes/" + random + ".txt"))), false);

	outWrite.println(dateTime);
	outWrite.println(messageId);
	outWrite.println(message);
	outWrite.println(senderAdd);
	outWrite.println(destinationAdd);

	outWrite.close();
    
}catch(Exception e){

} 
%>