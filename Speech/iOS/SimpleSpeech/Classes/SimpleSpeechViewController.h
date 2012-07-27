//  SimpleSpeechViewController.h
//  SimpleSpeech
//
// Licensed by AT&T under 'Software Development Kit Tools Agreement' 2012.
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&T Intellectual Property. All rights reserved.
// For more information contact developer.support@att.com http://developer.att.com

#import <UIKit/UIViewController.h>
#import "ATTSpeechKit.h"


@interface SimpleSpeechViewController : UIViewController <ATTSpeechServiceDelegate>

@property (retain, nonatomic) IBOutlet UITextView* textView;
@property (retain, nonatomic) IBOutlet UIButton* talkButton;

// Initialize SpeechKit for this app.
- (void) prepareSpeech;

// Message sent by "Press to Talk" button in UI
- (IBAction) listen: (id) sender;

@end

