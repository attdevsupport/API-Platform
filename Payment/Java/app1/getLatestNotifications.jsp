<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<%@ page contentType="application/json" language="java"%><%@ page
	import="java.io.*"%><%@ page import="java.util.Arrays"%><%@ page
	import="java.util.Collections"%><%@ page import="java.util.Comparator"%><%@ include
	file="config.jsp"%>
<%

String notificationId = "";

File directory = new File(application.getRealPath("/Notifications/")); 
File[] files = directory.listFiles();
Arrays.sort(files, new Comparator<File>(){
    public int compare(File f1, File f2)
    {
        return Long.valueOf(f1.lastModified()).compareTo(f2.lastModified());
    } });
Collections.reverse(Arrays.asList(files));

%>{"totalNumberOfNotifications":"<%=directory.listFiles().length%>","notificationList":[<%



if(directory.listFiles().length>0) {
    int i = 0;
    for(File notificationFile : files){  
          String notificationFileName = notificationFile.getName(); 
            RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("Notifications/" + notificationFileName),"r");
            if (i > 9)
			{
				File f = new File(application.getRealPath("Notifications/" + notificationFileName));
				f.delete();	
			}
			notificationId = (inFile1.readLine()).trim(); 
            inFile1.close();
      
            if((i==directory.listFiles().length-1)) {    
                  %>{"path":"Notifications/<%=notificationFileName%>","notificationId":"<%=notificationId%>"}]}<%
            } else {
                  %>{"path":"Notifications/<%=notificationFileName%>","notificationId":"<%=notificationId%>"},<%
            }
        i += 1;
    }
}
%>