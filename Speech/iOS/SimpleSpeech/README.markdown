Licensed by AT&T under 'Software Development Kit Tools Agreement' 2012.
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. 
For more information contact developer.support@att.com http://developer.att.com

# SimpleSpeech example app for AT&T SpeechKit

This sample app includes source code and an Xcode project to show how to call the AT&T SpeechKit from an application.  The app displays a bare-bones screen with button that initiates a speech interaction and a text area to show the recognition result.

## Setting up the project

The SimpleSpeech Xcode project is already configured to link with SpeechKit, but it needs a copy of the files from the SpeechKit distribution.  Follow these steps to add the latest SpeechKit to the project.

1. Unzip the AT&T Speech SDK into its own folder.
2. Copy the files `ATTSpeechKit.a` and `ATTSpeechKit.h` into the `ATTSpeechKit` subfolder of this sample app.
3. Open the SimpleSpeech Xcode project.
4. Expand the `ATTSpeechKit` group within the project window.  Both files should appear in black text, not red (which would indicate that Xcode can't find them).

## Running the sample

Before building the sample app, you will need to add the configuration for your Speech Enabler account: the URL of the service, your application's API key and API secret.  Add those strings to the top of `SimpleSpeechViewController.m`.  (Make sure you obfuscate your API key and secret before releasing your app to the public.)

## Understanding the speech code

The main code of the sample app is in the SimpleSpeechViewController class.  Look in the `-[prepareSpeech]` and `-[listen:]` methods for examples of setting up and starting a speech interaction.  Look in the `-[speechServiceSucceeded:]` and `[speechService:failedWithError:]` methods for examples of handling recognition responses.  Look in `-[prepareSpeech]` for examples of obtaining an OAuth access token. 

## OAuth sample code

The SpeechAuth class provides example code for authenticating your application with the OAuth client credentials protocol.  It performs an asynchronous network request for an OAuth access token that can be used in the Speech API.  
