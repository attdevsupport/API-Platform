******************************************************************************************
* Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
* Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
* For more information contact developer.support@att.com<mailto:developer.support@att.com>
******************************************************************************************

  AT&T API Samples - MIM app 1
 --------------------------------

This application allows the AT&T subscriber access to message related data 
stored in the AT&T Messages environment.

This file describes how to set up, configure and run the VB.NET Applications of the 
AT&T API Platform Restful sample applications. 
It covers all steps required to register the application on DevConnect and, based
on the generated API keys and secrets, create and run one's own full-fledged 
sample applications.

  1. Configuration
  2. Installation
  3. Parameters
  4. Running the application


1. Configuration

  Configuration consists of a few steps necessary to get an application registered
  on DevConnect with the proper services and endpoints, depending on the type of 
  client-side application (autonomous/non-autonomous). 

  To register an application, go to https://devconnect-api.att.com/ and login with
  your valid username and password. Next, choose "My Apps" from the bar at the top
  of the page and click the "Setup a New Application" button. 

  Fill in the form, in particular all fields marked as "required".

NOTE: You MUST select "Messaging On Behalf Of" in the list of services under field 'Services' in 
order to use this sample application code. 

  Having your application registered, you will get back an important pair of data: 
  an API key and Secret key. They are necessary to get your applications working 
  with the AT&T Platform APIs.

  Initially your newly registered application is restricted to the "Sandbox" 
  environment only. To move it to production, you may promote it by clicking the 
  "Promote to production" button. Notice that you will get a different API key and 
   secret, so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on 
  either the Autonomous Client or the Web-Server Client OAuth flow 
  (see https://devconnect-api.att.com/docs/oauth20/autonomous-client-application-oauth-flow or
  https://devconnect-api.att.com/docs/oauth20/web-server-client-application-oauth-flow 
  respectively).



2. Installation

** Requirements

   To run the examples you need an IIS Server. 

   Download the application files from the download link published in AT&T portal 
   into webdomain of your IIS server.



3. Parameters

Each sample application contains a web.config file. It holds configurable parameters 
described in an easy to read format. Please populate the following parameters in 
web.config as specified below:

1) api_key              : This is mandatory parameter, set the value as per your 
                          registered application 'API key' field value.

2) secret_key		: This is mandatory parameter, set the value as per your 
                          registered application 'Secret key' field value.

3) endPoint		: This is mandatory parameter, set it to the end point URI 
                          of AT&T Service.

4) scope		: MIM (Scope of the ATT service that will be invoked by 
                          the Application)

5) authorize_redirect_uri: This is mandatory key and value should be equal
         		   to MOBO Service registered application 'OAuth Redirect URL'

6) refreshTokenExpiresIn: This is optional key, which specifies the expiry time of 
			  refresh token in Hrs. Default value is 24Hrs.

Note: You must update parameters 1-2 after you promote your application from 'Sandbox' 
environment to 'Production' environment.



4. Running the application

Suppose you copied the sample app files in your IIS server webroot/mim/app1/ folder.
In order to run the sample application, type in'http://IIS_HOSTNAME:8080/mim/app1/Default.aspx'
(assuming you're using a HOSTNAME machine with IIS Server and have not changed the 
default port number, otherwise adjust accordingly) on your browser.
