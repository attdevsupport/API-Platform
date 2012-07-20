******************************************************************************************
* Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
* TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
* Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
* For more information contact developer.support@att.com<mailto:developer.support@att.com>
******************************************************************************************

  AT&T API Platform Samples - MMS app 2
 ------------------------------

This is a server side application which also has a web interface. The application 
looks for a file called numbers.txt containing MSISDNs of desired recipients, and 
an image called coupon.jpg, and message text from a file called subject.txt, and 
then sends an MMS message with the attachment to every recipient in the list. This 
can be triggered via a command line on the server, or through the web interface,
which then displays all the returned mmsIds or respective errors.

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

NOTE: You MUST select MMS in the list of services under field 'Services' in order
to use this sample application code. 

  Having your application registered, you will get back an important pair of data:
  an API key and Secret key. They are necessary to get your applications working
  with the AT&T Platform APIs. 

  Initially your newly registered application is restricted to the "Sandbox"
  environment only. To move it to production, you may promote it by clicking the 
  "Promote to production" button. Notice that you will get a different API key and 
  secret, so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on 
  either the Autonomous Client or the Web-Server Client OAuth flow (see 
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

1) api_key              : This is mandatory parameter, Set the value as per your
			  registered appliaction 'API key' field value.

2) secret_key     	: This is mandatory parameter,Set the value as per your
			  registered appliaction 'Secret key' field value.

3) endPoint		: This is mandatory parameter, Set it to the end point
			  URI of AT&T Service.

5) scope		: MMS (Scope of the ATT service that will be invoked by the
			  Application)

7) AccessTokenFilePath	: ~\\MMSApp1AccessToken.txt (This is optional parameter,
			  which points to the file path, where application stores
			  access token information. If the parameter is not configured,
			  it will take the default value as ~\\MMSApp1AccessToken.txt.
			  Give read/write access to this file.)

8) messageFilePath	: coupon_info\\message.txt (This is manadatory parameter,
			  which points to the file path, from where application reads
			  the message content to be sent along with the coupon.
			  Give read/write access to this file and folder.)

9) phoneListFilePath	: coupon_info\\phone.txt (This is manadatory parameter,
			  which points to the file path, from where application reads
			  the destination address for sending coupon.
			  Give read/write access to this file and folder.)

10) couponPath		: coupon_info\\ (This is mandatory parameter, which will
			  point to the directory, which contains the image to be sent
			  as the coupon. Give read/write access to this folder.)

11) couponFileName	: Coupon.jpg (This is mandatory parameter, which is the name
			  of the image/coupon to be sent, make sure that the image
			  exists in the "couponPath" key directory. Give read/write
			  access to this folder.)


Note: If your application is promoted from Sandbox environment to Production environment
and you decide to use production application settings, you must update parameters 1-2 as
per production application details.



4. Running the application

Suppose you copied the sample app files in your IIS server webroot/mms/app2/ folder.
In order to run the sample application, type in'http://IIS_HOSTNAME/mms/app2/Default.aspx'.
