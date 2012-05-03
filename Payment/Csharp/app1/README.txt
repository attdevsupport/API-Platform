
  AT&T API Samples - Payment app 1
 ------------------------------

This file describes how to set up, configure and run the C# Applications of the AT&T RESTFul sample applications. 
It covers all steps required to register the application on DevConnect and, based on the generated API keys and secrets, 
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

  Be careful while filling in the "OAuth Redirect URL" field. It should contain the URL that the oAuth provider will redirect
  users to when he/she successfully authenticates and authorizes your application.

NOTE: You MUST select PAYMENT in the list of services under field 'Services' in order to use this sample application code. 

  Having your application registered, you will get back an important pair of data: an API key and Secret key. They are 
  necessary to get your applications working with the AT&T HTML5 APIs. See 'Adjusting parameters' below to learn how to use 
  these keys.

  Initially your newly registered application is restricted to the "Sandbox" environment only. To move it to production,
  you may promote it by clicking the "Promote to production" button. Notice that you will get a different API key and secret,
  so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on either the Autonomous Client or the Web-Server 
  Client OAuth flow (see https://devconnect-api.att.com/docs/oauth-v1/client-credentials-grant-type or
  https://devconnect-api.att.com/docs/oauth-v1/authorization-code-grant-type respectively).


2. Installation

** Requirements

   1. To run the this sample application you need an IIS Server. 
   2. Change the value of "href" at the following line in Default.aspx to point to the location of the "common.css" of "style" folder:
	<link rel="stylesheet" type="text/css" href="../../style/common.css"/>
   3. Change the value of the "url" at the following line in common.css to point to the location of the "att.gif" of "images" folder.
	div#header { background:url(../images/att.gif) left center no-repeat; margin: 10px 5px}


3. Parameters

   
Each sample application contains a config.web file. It holds configurable parameters described in an easy to read format. 
Please populate the following parameters in config.web as specified below:

1) api_key                		: {set the value as per your registered application 'API key' field value} 


2) secret_key     	  		 : {set the value as per your registered application 'Secret key' field value} 

3) FQDN			  	 : https://api.att.com

4) scope				 : PAYMENT   

5) refundFile			: {set the value to file path which has read/write permissions}

6) Amount			: {set the value to decimal value}

7) Category			: {set the value to 1,3,4 or 5}

8) Channel			: {set the value to MOBILE_WEB}

9) MerchantPaymentRedirectUrl	 : {set the value to the payment app1 application url}

10) IsPurchaseOnNoActiveSubscription	 : {set the value to false}

11) SubscriptionRecurringNumber"	: {set the value to 9999}

12) SubscriptionRecurringPeriod	: {set the value to MONTHLY}

13) SubscriptionRecurringPeriodAmount	: {set the value to 1}

14) AccessTokenFilePath		: {set the value to the path of the file, which application creates and stores access token }

15) notaryURL				: {set the value to the notary app url}


4. Running the application

Notary application 1 is needed for this application.
Suppose you copied the sample app files in your IIS server webroot/payment/app1/ folder, In order to run the sample application, type in'http://IIS_HOSTNAME/payment/app1/Default.aspx'






