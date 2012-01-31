<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.JSONObject"%>
<%@ page import="org.json.JSONArray"%>
<%@ page import="java.io.*" %>
<%@ include file="OauthStorage.jsp" %>
<%@ include file="config.jsp" %>

<%
//Initialize some variables here, check if relevant variables were passed in, if not then check session, otherwise set default.
String scope = "WAP";
String accessToken = "";
String refreshToken = "";
String expires_in = "";
Long date = System.currentTimeMillis();

            //This application uses the Autonomous Client OAuth consumption model
            //Check if there is a valid access token that has not expired
            if(date < savedAccessTokenExpiry) {
                accessToken = savedAccessToken;
           } else if(date < savedRefreshTokenExpiry) {      //Otherwise if there is a refresh token that has not expired, use that to renew and save to file
                String url = FQDN + "/oauth/access_token";   
                HttpClient client = new HttpClient();
                GetMethod method = new GetMethod(url);   
                method.setQueryString("grant_type=refresh_token&client_id=" + clientIdAut + "&client_secret=" + clientSecretAut + "&refresh_token=" + savedRefreshToken);
                int statusCode = client.executeMethod(method);
                JSONObject rpcObject = new JSONObject(method.getResponseBodyAsString());
                accessToken = rpcObject.getString("access_token");
                refreshToken = rpcObject.getString("refresh_token");
                expires_in = rpcObject.getString("expires_in");
                savedAccessTokenExpiry = date + Long.parseLong(expires_in);
                savedRefreshTokenExpiry = date + Long.parseLong("86400000");
                method.releaseConnection();
    	        PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/OauthStorage.jsp"))), false);
                String toSave = "\u003C\u0025\nString savedAccessToken = \"" + accessToken + "\";\nLong savedAccessTokenExpiry = Long.parseLong(\"" + savedAccessTokenExpiry + "\");\nString savedRefreshToken = \"" + refreshToken + "\";\nLong savedRefreshTokenExpiry = Long.parseLong(\"" + savedRefreshTokenExpiry + "\");\n\u0025\u003E";
                outWrite.write(toSave);
   		        outWrite.close();
           } else if(date > savedRefreshTokenExpiry) {       //Otherwise get a new access token and refresh token, and save them to file
                String url = FQDN + "/oauth/access_token";   
                HttpClient client = new HttpClient();
                GetMethod method = new GetMethod(url);   
                method.setQueryString("grant_type=client_credentials&client_id=" + clientIdAut + "&client_secret=" + clientSecretAut + "&scope=" + scope);
                int statusCode = client.executeMethod(method);
                JSONObject rpcObject = new JSONObject(method.getResponseBodyAsString());
                accessToken = rpcObject.getString("access_token");
            	refreshToken = rpcObject.getString("refresh_token");
                expires_in = rpcObject.getString("expires_in");
                savedAccessTokenExpiry = date + Long.parseLong(expires_in);
                savedRefreshTokenExpiry = date + Long.parseLong("86400000");
                method.releaseConnection();
                PrintWriter outWrite = new PrintWriter(new BufferedWriter(new FileWriter(application.getRealPath("/OauthStorage.jsp"))), false);
       	        String toSave = "\u003C\u0025\nString savedAccessToken = \"" + accessToken + "\";\nLong savedAccessTokenExpiry = Long.parseLong(\"" + savedAccessTokenExpiry + "\");\nString savedRefreshToken = \"" + refreshToken + "\";\nLong savedRefreshTokenExpiry = Long.parseLong(\"" + savedRefreshTokenExpiry + "\");\n\u0025\u003E";
                outWrite.write(toSave);
   		        outWrite.close();
           }
           
%>
