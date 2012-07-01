AT&T API Samples - Speech app 1
 ------------------------------

This file describes how to set up, configure and run the ruby versions of the AT&T HTML5 Program sample applications. 
It covers all steps required to register the application on DevConnect and, based on the generated API keys and secrets, 
create and run one's own full-fledged sample applications.

  1. Configuration
  2. Installation
  3. Parameters
  4. Running the application


1. Configuration

  Configuration consists of a few steps necessary to get an application registered on DevConnect with the proper services and endpoints, depending on the type of client-side application (autonomous/non-autonomous). 

  To register an application, go to https://devconnect-api.att.com/ and login with your valid username and password. Next, choose "My Apps" from the bar at the top of the page and click the "Setup a New Application" button. 

  Fill in the form, in particular all fields marked as "required". 

  Be careful while filling in the "OAuth Redirect URL" field. It should contain the URL that the oAuth provider will redirect users to when he/she successfully authenticates and authorizes your application.

  NOTE: You MUST select Speech in the list of services under field 'Services' in order to use this sample application code. 

  Having your application registered, you will get back an important pair of data: an API key and Secret key. They are necessary to get your applications working with the AT&T HTML5 APIs. See 'Adjusting parameters' below to learn how to use these keys.

  Initially your newly registered application is restricted to the "Sandbox" environment only. To move it to production, you may promote it by clicking the "Promote to production" button. Notice that you will get a different API key and secret, so these values in your application should be adjusted accordingly.

  Depending on the kind of authentication used, an application may be based on either the Autonomous Client or the Web-Server 
  Client OAuth flow (see https://devconnect-api.att.com/docs/oauth20/autonomous-client-application-oauth-flow or
  https://devconnect-api.att.com/docs/oauth20/web-server-client-application-oauth-flow respectively).


2. Installation

** Requirements

   To run the examples you need ruby 1.8+ and a few gems that the applications are heavily based on:

     - sinatra (http://www.sinatrarb.com/) 
       used to construct a simple web application and manage URLs within.

  	 - eventmachine
  
	 To ensure installation of the sinatra-contrib gem you must first install the gem eventmachine. Sinatra-contrib has a dependency on eventmachine. EventMachine enables programs to easily interface with other programs using TCP/IP, especially if custom protocols are required.

     - sinatra-contrib 
       Additional set of useful helpers, including ones used to read    settings from external files.

     - Also make sure you have rest-client and json gems installed. 

   	To install these gems open up a terminal window and invoke:

     - gem install sinatra eventmachine sinatra-contrib rest-client json

	If you are experiencing any additional difficulties installing gems, particularly sinatra-contrib, it may prove useful to install the ruby gem thin. This can be installed in the terminal by invoking:

	 - gem install thin

   Having them installed, you are almost ready to run the sample applications.


** Setting up on a different port number

   In case multiple applications need to be run at the same time, you need to consider using different port numbers.

   By default sinatra uses port number 4567 and only one running application may use this port. In case you want to run one more sinatra-based application, you need to change its port number, eg. to 4568. This way you may have each application running on a unique port.



3. Parameters

   Each application contains a config.yml file. It holds configurable parameters 
   described in an easy to read YAML format which you are free to adjust to your needs. Following are short descriptions
   of parameters used by this application:

   1) FQDN              : https://api.att.com

   2) port              : port number that application binds to (default: 4567)

   3) api_key           : set the value as per your registered appliaction 'API key' field value

   4) secret_key        : set the value as per your registered appliaction 'Secret key' field value

   5) tokens_file       : file containing authorization and refresh tokens

   Note: If your application is promoted from Sandbox environment to Production environment and you decide to use
   production application settings, you must update parameters 1-2 as per production application details.


4. Running the application

  To run the application, open up a terminal window and type:

    ruby ./speech.rb

  or

    ./speech.rb

  Your application becomes available in a web browser, so you may visit: http://localhost:4567/ to see it working.

  You may interrupt the application at any time by pressing ctrl-C.
  
  

