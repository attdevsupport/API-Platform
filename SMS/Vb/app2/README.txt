
******************************************************************************************
* Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
* TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
* Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
* For more information contact developer.support@att.com<mailto:developer.support@att.com>
******************************************************************************************

  AT&T API Platform Samples - SMS app 2
 ------------------------------

SMS voting application with a server component and a web interface. Application
allows users to send SMS messages to a specified short code (given in the
application interface) with the one of the words 'baseball', 'basketball' or
'football'. The application should count up the votes for each sport and display
the results to the user. This application contains a server element (listner)
running constantly and checking for new messages, as well as a web interface to
display the results and allow the user to manually trigger an update.

This file describes how to set up, configure and run the VB Applications of the
AT&T API Platform sample applications. It covers all steps required to register
the application on DevConnect and, based on the generated API keys and secrets, 
create and run one's own full-fledged sample applications.

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

  Be careful while filling in the "OAuth Redirect URL" field. It should contain the
  URL that the oAuth provider will redirect users to when he/she successfully
  authenticates and authorizes your application.

NOTE: You MUST select SMS in the list of services under field 'Services' in order
to use this sample application code. 

  Having your application registered, you will get back an important pair of data:
  an API key and Secret key. They are necessary to get your applications working with
  the AT&T Platform APIs.

  Initially your newly registered application is restricted to the "Sandbox"
  environment only. To move it to production, you may promote it by clicking the
  "Promote to production" button. Notice that you will get a different API key and
  secret, so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on either
  the Autonomous Client or the Web-Server Client OAuth flow (see 
  https://devconnect-api.att.com/docs/oauth20/autonomous-client-application-oauth-flow or
  https://devconnect-api.att.com/docs/oauth20/web-server-client-application-oauth-flow
  respectively).



2. Installation

** Requirements

   To run the this sample application you need an IIS Server. 

   Download the application files from the download link published in AT&T portal
   into webdomain of your IIS server.



3. Parameters

Each sample application contains a web.config file. It holds configurable parameters
described in an easy to read format. Please populate the following parameters in
web.config as specified below:

1) short_code		: This is mandatory parameter, set the value as per your
			  registered application 'short code' field value.

2) FootBallFilePath	: ~\\tally1.txt (This is mandatory parameter, which points to
			  the file path, where application stores football vote count.
			  If the parameter is not configured, it will take the default
			  value as ~\\tally1.txt. Give read/write access to this file.)

3) BaseBallFilePath	: ~\\tally2.txt (This is mandatory parameter, which points to
			  the file path, where application stores baseball vote count.
			  If the parameter is not configured, it will take the default
			  value as ~\\tally2.txt. Give read/write access to this file.)

4) BasketBallFilePath	: ~\\tally3.txt (This is mandatory parameter, which points to
			  the file path, where application stores basketball vote count.
			  If the parameter is not configured, it will take the default
			  value as ~\\tally3.txt. Give read/write access to this file.)


Note: If your application is promoted from Sandbox environment to Production
environment and you decide to use production application settings, you must update
parameters 1-2 as per production application details.



4. Running the application

Suppose you copied the sample app files in your IIS server webroot/sms/app2/ folder.
In order to run the sample application, type in'http://IIS_HOSTNAME/sms/app2/Default.aspx'.
