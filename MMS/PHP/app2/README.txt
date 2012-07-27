******************************************************************************************
* Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
* TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
* Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
* For more information contact developer.support@att.com<mailto:developer.support@att.com>
******************************************************************************************
  AT&T API Platform Samples - MMS app2
 ----------------------------------------

This file describes how to set up, configure and run the php versions of the AT&T API Platform Program sample applications. 
It covers all steps required to register the application on DevConnect and, based on the generated API keys and secrets, 
create and run one's own full-fledged sample applications.

  1. Configuration
  2. Installation
  3. Parameters


1. Configuration

  Configuration consists of a few steps necessary to get an application registered on DevConnect with the proper services and 
  endpoints, depending on the type of client-side application (autonomous/non-autonomous). 

  To register an application, go to https://devconnect-api.att.com/ and login with your valid username and password.
  Next, choose "My Apps" from the bar at the top of the page and click the "Setup a New Application" button. 

  Fill in the form, in particular all fields marked as "required". 

  Be careful while filling in the "OAuth Redirect URL" field. It should contain the URL that the oAuth provider will redirect
  users to when he/she successfully authenticates and authorizes your application.

  NOTE: You MUST select MMS in the list of services under field 'Services' in order to use this sample application code.

  Having your application registered, you will get back an important pair of data: an API key and Secret key. They are 
  necessary to get your applications working with the AT&T Platform APIs. See 'Adjusting parameters' below to learn how to use 
  these keys.

  Initially your newly registered application is restricted to the "Sandbox" environment only. To move it to production,
  you may promote it by clicking the "Promote to production" button. Notice that you will get a different API key and secret,
  so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on either the Autonomous Client or the Web-Server 
  Client OAuth flow (see https://devconnect-api.att.com/docs/oauth20/autonomous-client-application-oauth-flow or
  https://devconnect-api.att.com/docs/oauth20/web-server-client-application-oauth-flow respectively).


2. Installation

Requirements:
Apache web server
PHP 5.2+
Apache and php configured , on most Linux systems if installed using packages this will be done automatically 

Installation:
Copy the sample application  folder to apache web root folder, for example /var/www/html 
 

3. Parameters

   Each application contains a config.php file. It holds the following configurable parameters and defaults 
   
   1) $api_key                                : Client API key
   2) $secret_key                             : Client secret key
   3) $short_code                             : Short code
   4) $FQDN = "https://api.att.com"           : Endpoint 
   5) $oauth_file = "/tmp/mmsoauthtoken.php"  : Oauth Token persistance location, should be writable by Apache server  
   6) $scope = "MMS"                          : Oauth scope ( MMS ) 
   7) $default_subject                        : Default text message

   Note: If your application is promoted from Sandbox environment to Production environment and you decide to use
   production application settings, you must update parameters 1-2 as per production application details.

