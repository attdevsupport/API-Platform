<%-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
--%>

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
System.out.print(data);

String senderAddress = data.split("<SenderAddress>tel:")[1].split("</SenderAddress>")[0].substring(2);
Date d = new Date();
DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.US);
df.setTimeZone(TimeZone.getTimeZone("PST"));
DateFormat tf = DateFormat.getTimeInstance(DateFormat.LONG, Locale.US);
tf.setTimeZone(TimeZone.getTimeZone("PST"));
String date = df.format(d) + ", " +tf.format(d);

String[] parts = data.split("--Nokia-mm-messageHandler-BoUnDaRy");
String[] lowerParts = parts[2].split("BASE64");
String type = lowerParts[0].split("image/")[1].split(";")[0];
byte[] outFile = Base64.decodeBase64(lowerParts[1]);
int random = (int)(Math.random()*10000000);


FileOutputStream fous = new FileOutputStream(application.getRealPath("/MoMmsImages/"+random+"."+type));
fous.write(outFile);
fous.close();
String decodedText = "";
if(parts.length>4) {
    String textPart = parts[3].split("BASE64")[1];
    decodedText = new String(Base64.decodeBase64(textPart));
    decodedText = decodedText.trim();
} 
StringTokenizer st = new StringTokenizer(data, "&");
String token;
while(st.hasMoreTokens()){
token = st.nextToken();
paramLinkedMap.put(URLDecoder.decode(token.substring(0, token.indexOf("="))),
URLDecoder.decode(token.substring(token.indexOf("=")+1, token.length())));
}

PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/MoMmsData/" + random + "." + type + ".txt"))), false);
String toSave = senderAddress + "\n" + date + "\n" + decodedText;
outWrite.write(toSave);
outWrite.close();
}
catch(Exception e){

}
%>