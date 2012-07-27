//  SimpleSpeechViewController.m
//  SimpleSpeech
//
// Licensed by AT&T under 'Software Development Kit Tools Agreement' 2012.
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&T Intellectual Property. All rights reserved.
// For more information contact developer.support@att.com http://developer.att.com

#import "SimpleSpeechViewController.h"
#import "SpeechAuth.h"

// Replace the URLs below with the appropriate ones for your Speech API account.
#define SPEECH_URL @"https://api.att.com/rest/1/SpeechToText"
#define OAUTH_URL @"http://api.att.com/oauth/token"
#error Add code to unobfuscate your Speech API credentials in the macros below, then delete this line.
#define API_KEY MY_UNOBFUSCATOR(my_obfuscated_api_key)
#define API_SECRET MY_UNOBFUSCATOR(my_obfuscated_api_key)

@interface SimpleSpeechViewController ()
- (void) speechAuthFailed: (NSError*) error;
@end

@implementation SimpleSpeechViewController

@synthesize textView;
@synthesize talkButton;

#pragma mark -
#pragma mark Lifecyle

- (void) dealloc
{
    [textView release];
    [talkButton release];
    [super dealloc];
}


// Initialize SpeechKit for this app.
- (void) prepareSpeech
{
    // Access the SpeechKit singleton.
    ATTSpeechService* speechService = [ATTSpeechService sharedSpeechService];
    
    // Point to the SpeechToText API.
    speechService.recognitionURL = [NSURL URLWithString: SPEECH_URL];
    
    // Hook ourselves up as a delegate so we can get called back with the response.
    speechService.delegate = self;
    
    // Use default speech UI.
    speechService.showUI = YES;
    
    // Choose the speech recognition package.
    speechService.speechContext = @"BusinessSearch";
    
    // Start the OAuth background operation, disabling the Talk button until 
    // it's done.
    talkButton.enabled = NO;
    [[SpeechAuth authenticatorForService: [NSURL URLWithString: OAUTH_URL]
                                  withId: API_KEY secret: API_SECRET]
        fetchTo: ^(NSString* token, NSError* error) {
            if (token) {
                speechService.bearerAuthToken = token;
                talkButton.enabled = YES;
            }
            else
                [self speechAuthFailed: error];
    }];

    // Wake the audio components so there is minimal delay on the first request.
    [speechService prepare];
}


#pragma mark -
#pragma mark Actions

// Perform the action of the "Push to talk" button
- (IBAction) listen: (id) sender
{
    NSLog(@"Starting speech request");
    
    // Start listening via the microphone.
    ATTSpeechService* speechService = [ATTSpeechService sharedSpeechService];
    [speechService startWithMicrophone];
}

#pragma mark -
#pragma mark Speech Service Delegate Methods

- (void) speechServiceSucceeded: (ATTSpeechService*) speechService
{
    NSLog(@"Speech service succeeded");
    
    // Extract the needed data from the SpeechService object:
    // For raw bytes, read speechService.responseData.
    // For a JSON tree, read speechService.responseDictionary.
    // For the n-best ASR strings, use speechService.responseStrings.
    
    // In this example, use the ASR strings.
    // There can be 0 strings, 1 empty string, or 1 non-empty string.
    // Display the recognized text in the interface is it's non-empty,
    // otherwise have the user try again.
    NSArray* nbest = speechService.responseStrings;
    NSString* recognizedText = @"";
    if (nbest != nil && nbest.count > 0)
        recognizedText = [nbest objectAtIndex: 0];
    if (recognizedText.length) // non-empty?
        [self.textView setText: recognizedText];
    else {
        UIAlertView* alert = [[UIAlertView alloc] initWithTitle: @"Didn't recognize speech"
                                                        message: @"Please try again."
                                                       delegate: self 
                                              cancelButtonTitle: @"OK"
                                              otherButtonTitles: nil];
        [alert show];
        [alert release];
    }
}

- (void) speechService: (ATTSpeechService*) speechService 
         failedWithError: (NSError*) error
{
    if ([error.domain isEqualToString: ATTSpeechServiceErrorDomain]
        && (error.code == ATTSpeechServiceErrorCodeCanceledByUser)) {
        NSLog(@"Speech service canceled");
        // Nothing to do in this case
        return;
    }
    NSLog(@"Speech service had an error: %@", error);
    
    UIAlertView* alert = [[UIAlertView alloc] initWithTitle: @"An error occurred"
                                                    message: @"Please try again later."
                                                   delegate: self 
                                          cancelButtonTitle: @"OK"
                                          otherButtonTitles: nil];
    [alert show];
    [alert release];
}

#pragma mark -
#pragma mark OAuth

/* The SpeechAuth authentication failed. */
- (void) speechAuthFailed: (NSError*) error
{
    NSLog(@"OAuth error: %@", error);
    UIAlertView* alert = 
        [[UIAlertView alloc] initWithTitle: @"Speech Unavailable"
                                   message: @"This app was rejected by the speech service.  Contact the developer for an update."
                                  delegate: self 
                         cancelButtonTitle: @"OK"
                         otherButtonTitles: nil];
    [alert show];
    [alert release];
}

@end
