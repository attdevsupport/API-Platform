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
String notificationId = data.split("<hub:notificationId>")[1].split("</hub:notificationId>")[0].substring(2);


System.out.print("the data is : " + data);
//String print = data;

int random = (int)(Math.random()*10000000);

PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/Notifications/" + random + ".txt"))), false);
String toSave = notificationId;
outWrite.write(toSave);

outWrite.close();
    
}catch(Exception e){

} 
%>
