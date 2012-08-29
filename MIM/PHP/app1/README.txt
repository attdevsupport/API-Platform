******************************************************************************************
* Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
* Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
* For more information contact developer.support@att.com<mailto:developer.support@att.com>
******************************************************************************************

  AT&T API Platform Samples - MIM app 1
 --------------------------------

This application allows the AT&T subscriber access to message related data 
stored in the AT&T Messages environment.

This file describes how to set up, configure and run the PHP Applications of the 
AT&T API Platform Restful sample applications. 
It covers all steps required to register the application on DevConnect and, based
on the generated API keys and secrets, create and run one's own full-fledged 
sample applications.

  1. Configuration
  2. Installation
  3. Parameters


1. Configuration

  Configuration consists of a few steps necessary to get an application registered
  on DevConnect with the proper services and endpoints, depending on the type of 
  client-side application (autonomous/non-autonomous). 

  To register an application, go to https://devconnect-api.att.com/ and login with
  your valid username and password. Next, choose "My Apps" from the bar at the top
  of the page and click the "Setup a New Application" button. 

  Fill in the form, in particular all fields marked as "required".

NOTE: You MUST select MOBO in the list of services under field 'Services' in 
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

   Apache web server
     PHP 5.2+
     Apache and PHP configured, on most Linux systems if installed using packages this will be done automatically.

   Installation:
     Copy the sample application folder to Apache web root folder, for example /var/www/html.



3. Parameters

Each sample application contains a config.php file. It holds configurable parameters 
described in an easy to read format. Please populate the following parameters in 
config.php as specified below:

1) api_key           : This is mandatory parameter, set the value as per your 
                          registered application 'API key' field value.

2) secret_key		: This is mandatory parameter, set the value as per your 
                          registered application 'Secret key' field value.

3) FQDN		: This is mandatory parameter, set it to the end point URI 
                          of AT&T Service.

4) scope		: MIM (Scope of the ATT service that will be invoked by 
                          the Application)

5) authorize_redirect_uri: This is mandatory key and value should be equal
         		   to MOBO Service registered application 'OAuth Redirect URL'

6) refreshTokenExpiresIn: This is optional key, which specifies the expiry time of 
			  refresh token in Hrs. Default value is 24Hrs.

Note: You must update parameters 1-2 after you promote your application from 'Sandbox' 
environment to 'Production' environment.


