<%-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
--%>

<%@ page contentType="application/json" language="java" %><%@ page import="java.io.*" %><%@ page import="java.util.Arrays" %><%@ page import="java.util.Collections" %><%@ page import="java.util.Comparator" %><%@ include file="config.jsp" %><%
String notificationId = "";
String notificationType = "";
String transactionId = "";
String merchantTransactionId = "";

File directory = new File(application.getRealPath("notifications/")); 
File[] files = directory.listFiles();
if(files.length>0) {
    Arrays.sort(files, new Comparator<File>(){
        public int compare(File f1, File f2)
        {
            return Long.valueOf(f1.lastModified()).compareTo(f2.lastModified());
        } });
    Collections.reverse(Arrays.asList(files));
}

%>{"totalNumberOfNotifications":"<%=directory.listFiles().length%>","notificationList":[<%

if(directory.listFiles().length>0) {
    int i = 0;
    for(File imageFile : files){  
          String imageFileName = imageFile.getName(); 
            RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("notifications/" + imageFileName),"r");
            notificationId = inFile1.readLine();
            notificationType = inFile1.readLine();
            transactionId = inFile1.readLine();
            merchantTransactionId = inFile1.readLine();
            if(notificationId==null || notificationId.equalsIgnoreCase("null"))
                notificationId = "";
            if(notificationId.indexOf(194)!=-1)
                notificationId = notificationId.substring(0, notificationId.length()-2);
            if(notificationType==null || notificationType.equalsIgnoreCase("null"))
                notificationType = "";
            if(notificationType.indexOf(194)!=-1)
                notificationType = notificationType.substring(0, notificationType.length()-2);
            if(transactionId==null || transactionId.equalsIgnoreCase("null"))
                transactionId = "";
            if(transactionId.indexOf(194)!=-1)
                transactionId = transactionId.substring(0, transactionId.length()-2);
            if(merchantTransactionId==null || merchantTransactionId.equalsIgnoreCase("null"))
                merchantTransactionId = "";
            if(merchantTransactionId.indexOf(194)!=-1)
                merchantTransactionId = merchantTransactionId.substring(0, merchantTransactionId.length()-2);
            inFile1.close();
            if((i==directory.listFiles().length-1) || (i==4)) {
                  %>{"notificationId":"<%=notificationId%>","notificationType":"<%=notificationType%>","transactionId":"<%=transactionId%>", "merchantTransactionId":"<%=merchantTransactionId%>"}]}<%
            } else {
                  %>{"notificationId":"<%=notificationId%>","notificationType":"<%=notificationType%>","transactionId":"<%=transactionId%>", "merchantTransactionId":"<%=merchantTransactionId%>"},<%
            }
        i += 1;
        if(i==5)
            break;
    } 
} else {
        %>]}<%
}
%>