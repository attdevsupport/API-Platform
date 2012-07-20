<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<%@ page contentType="application/json" language="java" %>
<%@ page import="java.io.*" %>
<%@ page import="java.util.Arrays" %>
<%@ page import="java.util.Collections" %>
<%@ page import="java.util.Comparator" %>
<%@ include file="config.jsp" %>
<%
String invalidMessagePresent = null;
    
	   String lineData1 = "";
	   RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("/WEB-INF/tally1.txt"),"rw");
	   lineData1 = inFile1.readLine();
	   inFile1.close();
	   Integer totalTally1 = Integer.parseInt(lineData1);
	   
   	   String lineData2 = "";
	   RandomAccessFile inFile2 = new RandomAccessFile(application.getRealPath("/WEB-INF/tally2.txt"),"rw");
	   lineData2 = inFile2.readLine();
	   inFile2.close();
	   Integer totalTally2 = Integer.parseInt(lineData2);
	   
   	   String lineData3 = "";
	   RandomAccessFile inFile3 = new RandomAccessFile(application.getRealPath("/WEB-INF/tally3.txt"),"rw");
	   lineData3 = inFile3.readLine();
	   inFile3.close();
	   Integer totalTally3 = Integer.parseInt(lineData3);
%>{"totalNumberOfVotes":<%=totalTally1+totalTally2+totalTally3%>, "footballVotes":<%=totalTally1%>, "baseballVotes":<%=totalTally2%>, "basketballVotes":<%=totalTally3%>}
