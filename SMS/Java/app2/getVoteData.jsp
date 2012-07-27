<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<%@ page contentType="application/json" language="java" %><%@ page import="java.io.*" %><%@ page import="java.util.Arrays" %><%@ page import="java.util.Collections" %><%@ page import="java.util.Comparator" %><%@ include file="config.jsp" %><%
String dateTime = "";
String messageId = "";
String message = "";
String senderAdd = "";
String destinationAdd = "";


File directory = new File(application.getRealPath("/Votes/")); 
File[] files = directory.listFiles();
Arrays.sort(files, new Comparator<File>(){
    public int compare(File f1, File f2)
    {
        return Long.valueOf(f1.lastModified()).compareTo(f2.lastModified());
    } });
Collections.reverse(Arrays.asList(files));

int totalNumberOfVotesSent = directory.listFiles().length;
int footballVotes =0;
int baseballVotes =0;
int basketballVotes =0;

%>{"voteList":[<%

if(directory.listFiles().length>0) {
    int i = 0;
	   for(File voteFile : files){  
          String voteFileName = voteFile.getName(); 
            RandomAccessFile inFile2 = new RandomAccessFile(application.getRealPath("Votes/" + voteFileName),"r");
            dateTime = inFile2.readLine();
            messageId = inFile2.readLine();
            message = inFile2.readLine();
			
			if(message.equalsIgnoreCase("football")) {
						footballVotes += 1;
			} 
			else
			if(message.equalsIgnoreCase("baseball")) {
						baseballVotes += 1;
			} 
			else
			if(message.equalsIgnoreCase("basketball")) {
						basketballVotes += 1;
					}
	
            inFile2.close(); 
	
    }
	
    for(File voteFile : files){  
          String voteFileName = voteFile.getName(); 
            RandomAccessFile inFile1 = new RandomAccessFile(application.getRealPath("Votes/" + voteFileName),"r");
            dateTime = inFile1.readLine();
            messageId = inFile1.readLine();
            message = inFile1.readLine();
            senderAdd = inFile1.readLine();
			destinationAdd = inFile1.readLine();
	
            inFile1.close(); 
			if((i==directory.listFiles().length-1) || (i==9)) {
                  %>{"Date&Time":"<%=dateTime%>","MessageID":"<%=messageId%>","Message":"<%=message%>","SenderAddress":"<%=senderAdd%>","DestinationAddress":"<%=destinationAdd%>"}]}<%
            } else {
                  %>{"Date&Time":"<%=dateTime%>","MessageID":"<%=messageId%>","Message":"<%=message%>","SenderAddress":"<%=senderAdd%>","DestinationAddress":"<%=destinationAdd%>"},<%
            }
        i += 1;
        if(i==10)
            break;
    }
	

	PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/VoteTotals/voteTotals.txt"))), false);
	outWrite.println(footballVotes);
	outWrite.println(baseballVotes);
	outWrite.println(basketballVotes);
	outWrite.println(footballVotes + baseballVotes + basketballVotes);
	outWrite.close();
}
%>