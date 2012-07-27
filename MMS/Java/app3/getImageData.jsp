<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<%@ page contentType="application/json" language="java" %><%@ page import="java.io.*" %><%@ page import="java.util.Arrays" %><%@ page import="java.util.Collections" %><%@ page import="java.util.Comparator" %><%@ include file="config.jsp" %><%
String senderAddress = "";
String date = "";
String text = "";

File directory = new File(application.getRealPath("/MoMmsImages/")); 
File[] files = directory.listFiles();
Arrays.sort(files, new Comparator<File>(){
    public int compare(File f1, File f2)
    {
        return Long.valueOf(f1.lastModified()).compareTo(f2.lastModified());
    } });
Collections.reverse(Arrays.asList(files));

%>{"totalNumberOfImagesSent":"<%=directory.listFiles().length%>","imageList":[<%

if(directory.listFiles().length>0) {
    int i = 0;
    for(File imageFile : files){  
          String imageFileName = imageFile.getName(); 
            RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("MoMmsData/" + imageFileName + ".txt"),"r");
            senderAddress = inFile1.readLine();
            date = inFile1.readLine();
            text = inFile1.readLine();
            if(text==null || text.equalsIgnoreCase("null"))
                text = "";
            if(text.indexOf(194)!=-1)
                text = text.substring(0, text.length()-2);
            inFile1.close();
            if((i==directory.listFiles().length-1) || (i==9)) {
                  %>{"path":"MoMmsImages/<%=imageFileName%>","senderAddress":"<%=senderAddress%>","date":"<%=date%>","text":"<%=text%>"}]}<%
            } else {
                  %>{"path":"MoMmsImages/<%=imageFileName%>","senderAddress":"<%=senderAddress%>","date":"<%=date%>","text":"<%=text%>"},<%
            }
        i += 1;
        if(i==10)
            break;
    }
}
%>
