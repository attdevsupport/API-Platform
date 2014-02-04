//  SimpleSpeechAppDelegate.m
//  SimpleSpeech
//
// Licensed by AT&T under 'Software Development Kit Tools Agreement' 2012.
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&T Intellectual Property. All rights reserved.
// For more information contact developer.support@att.com http://developer.att.com

#import "SimpleSpeechAppDelegate.h"
#import "SimpleSpeechViewController.h"

@implementation SimpleSpeechAppDelegate

@synthesize window;
@synthesize viewController;


#pragma mark -
#pragma mark Application lifecycle

- (BOOL) application: (UIApplication*) application didFinishLaunchingWithOptions: (NSDictionary*) launchOptions 
{    
    // Override point for customization after application launch.

    // Hook up the UI from Interface Builder.
    self.window.rootViewController = self.viewController;
    [self.window makeKeyAndVisible];

    return YES;
}

- (void) applicationDidBecomeActive: (UIApplication*) application
{
    // Since the app has come to the foreground, (re-)initialize SpeechKit.
    [viewController prepareSpeech];
}

- (void) dealloc 
{
    [viewController release];
    [window release];
    [super dealloc];
}


@end
