<%-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
--%>

  AT&T API Platform Samples - MMS app 2
 ------------------------------

This file describes how to set-up, configure and run the Java Applications using AT&T API Platform services. 
It covers all steps required to register the application, based on the generated API keys and secrets, 
create and run one's own full-fledged sample applications.

  1. Configuration
  2. Installation
  3. Parameters
  4. Running the application


1. Configuration

  Configuration consists of a few steps necessary to get an application registered on DevConnect with the proper services and 
  endpoints, depending on the type of client-side application (autonomous/non-autonomous). 

  To register an application, go to https://devconnect-api.att.com/ and login with your valid username and password.
  Next, choose "My Apps" from the bar at the top of the page and click the "Setup a New Application" button. 

  Fill in the form, in particular all fields marked as "required".

NOTE: You MUST select MMS in the list of services under field 'Services' in order to use this sample application code. 

  Having your application registered, you will get back an important pair of data: an API key and Secret key. They are 
  necessary to get your applications working with the AT&T APIs. See 'Adjusting parameters' below to learn how to use 
  these keys.

  Initially your newly registered application is restricted to the "Sandbox" environment only. To move it to production,
  you may promote it by clicking the "Promote to production" button. Notice that you will get a different API key and secret,
  so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on either the Autonomous Client or the Web-Server 
  Client OAuth flow (see https://devconnect-api.att.com/docs/oauth20/autonomous-client-application-oauth-flow or
  https://devconnect-api.att.com/docs/oauth20/web-server-client-application-oauth-flow respectively).


2. Installation

** Requirements

   To run the examples you need a Java environment and at least Apache Tomcat 6, or another Java web server such as Jetty. 

** Setting up multiple sample applications simultaneously

   In case multiple applications need to be run at the same time, make sure to put each app in a separate folders.

3. Parameters
   
Each sample application contains a config.jsp file. It holds configurable parameters described in an easy to read format. 
Please populate the following parameters in config.jsp as specified below:

1) clientIdAut                        : {set the value as per your registered appliaction 'API key' field value} 

2) clientSecretAut                    : {set the value as per your registered appliaction 'Secret key' field value} 

3) FQDN    		  	          : https://api.att.com

4) shortCode1                         : short-code 1

Note: If your application is promoted from Sandbox environment to Production environment and you decide to use production 
application settings, you must update parameters 1-2 as per production application details.


4. Running the application

  To run the application, put the entire contents of the application folder into a separate folder named SampleApp inside the webapps 
  folder in your Apache Tomcat home directory. If you have specified a different home directory in Tomcat for your web applications, 
  put it there instead. 

  Depending on your security settings in Apache Tomcat, you might need to enable write access to the OauthStorage.jsp file.

  Once you start tomcat, typically using the command "<your-tomcat-root-folder>/bin/startup.sh", your application becomes available 
  in a web browser, so you may visit: http://localhost:8080/SampleApp/MMS2.jsp to see it working.

