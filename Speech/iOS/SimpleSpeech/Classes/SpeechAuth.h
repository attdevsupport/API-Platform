//  SpeechAuth.h
//
// Licensed by AT&T under 'Software Development Kit Tools Agreement' 2012.
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&T Intellectual Property. All rights reserved.
// For more information contact developer.support@att.com http://developer.att.com

#import <Foundation/NSObject.h>

@class NSString, NSError;

/**
 * Type of block called when SpeechAuth gets credential or fails.
 * token will be the OAuth bearer token.
 * If there's an problem authenticating, token will be nil and 
 * error will contain the error.  
 * TO DO: document the keys in error.userInfo
**/
typedef void (^SpeechAuthBlock)(NSString* token, NSError* error);

/**
 * Fetches OAuth client credentials, calling a block when done.
**/
@interface SpeechAuth : NSObject {
}

/** Creates a SpeechAuth object with the given credentials. **/
+ (SpeechAuth*) authenticatorForService: (NSURL*) oauth_url
                                 withId: (NSString*) client_id
                                 secret: (NSString*) client_secret;

/*! Beging fetching the credentials.  Will call block when done. !*/
- (void) fetchTo: (SpeechAuthBlock) block;

/*! Stop fetching. Once stopped, loading may not resume. !*/
- (void) cancel;

@end
