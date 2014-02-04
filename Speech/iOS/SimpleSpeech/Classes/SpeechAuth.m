//  SpeechAuth.m
//
// Licensed by AT&T under 'Software Development Kit Tools Agreement' 2012.
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&T Intellectual Property. All rights reserved.
// For more information contact developer.support@att.com http://developer.att.com

#import "SpeechAuth.h"
#import "ATTSpeechKit.h"

typedef enum
{
    LoaderStateUnknown = 0,
    LoaderStateInitialized,
    LoaderStateConnecting,
    LoaderStateReceivedResponse,
    LoaderStateReceivedData,
    LoaderStateFinished,
    LoaderStateFailed,
    LoaderStateCanceling,
    LoaderStateCanceled
} LoaderState;

// Memory Management
// 
// This object will retain its initialiation parameters (the NSURLRequest)
// during the lifetime of this object.  
// It will retain the connection, response, data, and delegate only between
// the call to start and the callbacks to the delegate.
// It will also add a retain of itself during that interval.  That way, the 
// client can autorelease this object after starting, and this object 
// will remain in memory while active.

@interface SpeechAuth () {
    @private
    LoaderState state;
}
@property (copy) SpeechAuthBlock authenticatedBlock;
@property (copy) NSURLRequest* request;// Make a copy in case it's mutable
@property (retain) NSURLConnection* connection;
@property (retain) NSURLResponse* response;
@property (retain) NSMutableData* data;

- (NSInteger) statusCode;
- (void) clear;
@end

@implementation SpeechAuth

@synthesize authenticatedBlock = _authenticatedBlock;
@synthesize request = _request;
@synthesize connection = _connection;
@synthesize response = _response;
@synthesize data = _data;


- (id) initWithRequest: (NSURLRequest*) request
{
    self = [super init];
    if (self != nil)
    {
        // First see if the request can be handled.
        if (![NSURLConnection canHandleRequest: request]) {
            [self release];
            return nil;
        }
        self.request = request; 
        self.data = [NSMutableData data];
        _connection = nil; // Create connection when client wants to start loading.
        _response = nil;
        state = LoaderStateInitialized;
    }
    return self;
}

+ (SpeechAuth*) authenticatorForService: (NSURL*) oauth_url
                                 withId: (NSString*) client_id
                                 secret: (NSString*) client_secret
{
    NSMutableURLRequest* request = [NSMutableURLRequest requestWithURL: oauth_url];
    NSString* postString = [NSString stringWithFormat:
        @"grant_type=client_credentials&scope=SPEECH&client_id=%@&client_secret=%@", 
        client_id, client_secret];
    request.HTTPMethod = @"POST";
    request.HTTPBody = [postString dataUsingEncoding:NSUTF8StringEncoding];
                  
    // The OAuth server is quite quick.  
    // The only timeouts will be on network failures.
    request.timeoutInterval = 5;   
    return [[[self alloc] initWithRequest: request] autorelease];
    
}

- (void) dealloc
{
    // We should have already been cleared, but just in case...
    [_connection cancel];
    
    self.request = nil;
    self.response = nil;
    self.data = nil;
    self.authenticatedBlock = nil;
    self.connection = nil;

    [super dealloc];
}

- (NSInteger) statusCode
{
    int code;
    if (_response == nil)
        code= 100; // HTTP "Continue"
    else if ([_response respondsToSelector: @selector(statusCode)])
        code = [(NSHTTPURLResponse*)_response statusCode];
    // The other kind of response we support is file: URLs.
    // The behavior of NSURLConnection in that case is to only call connection:didReceiveResponse:
    // when the file is found.  
    // When it's not found, it calls connection:didFailWithError: directly.
    // So if our response is non-nil, assume it's OK.
    else 
        code = 200 ;
    return code;
}

- (void) start
{
    state = LoaderStateConnecting;
    // Add a retention to this object do it doesn't dispose during the connection.
    [self retain];

    // Allocate the NSURLConnection and start it in one step.
    self.connection = [NSURLConnection connectionWithRequest: _request 
                                                    delegate: self];
        
    if (_connection == nil) {
        state = LoaderStateFailed;
        // Report the error the delegate on the next time through the runloop.
        [[NSOperationQueue mainQueue] addOperationWithBlock: ^{
            // TO DO: the arguments to NSError are completely arbitrary!
            NSError* error = [NSError errorWithDomain: ATTSpeechServiceErrorDomain 
                                                 code: ATTSpeechServiceErrorCodeConnectionFailure 
                                             userInfo: nil];
            _authenticatedBlock(nil, error);
            [self clear];
        }];
        return;
    }
    // Don't call [_connection start], since it's already started.
}

- (void) fetchTo: (SpeechAuthBlock) block
{
    self.authenticatedBlock = block;
    [self start];
}

- (void) clear
{
    // Completely dispose the connection, delegate, and response data 
    // when we are done.
    self.authenticatedBlock = nil;
    [_connection cancel];
    self.connection = nil;
    self.response = nil;
    _data.length = 0;
    // And release the retain count we added during start.
    [self release];
}

- (void) cancel
{
    // Completely dispose the connection when we cancel it.
    if (state != LoaderStateFinished)
    {
        state = LoaderStateCanceling;
        [self clear];
    }
}

// NSURLConnection delegate methods


- (void) connection: (NSURLConnection*) connection 
 didReceiveResponse: (NSURLResponse*) response
{
    // The connection just got a new response.  Clear out anything we've already loaded.
    self.response = response;
    _data.length = 0;
    state = LoaderStateReceivedResponse;
}

- (void) connection: (NSURLConnection*) connection 
     didReceiveData: (NSData*) data
{
    // The connection is sending us some data incrementally.
    [_data appendData: data];
    state = LoaderStateReceivedData;
}

- (void) connectionDidFinishLoading: (NSURLConnection*) connection
{
    // Loading is complete. 
    state = LoaderStateFinished;

    NSError* error = nil;
    BOOL succeeded = NO;
    if (self.statusCode == 200 && _data.length) {
        // Use iOS 5 JSON library to decode OAuth response.
        // Be very circumspect about the data types so that we don't crash on bad data.
        NSDictionary* json = [NSJSONSerialization JSONObjectWithData: _data options: 0 error: &error];
        if ([json isKindOfClass: [NSDictionary class]]) {
            id token = [json objectForKey: @"access_token"];
            if (token != nil) {
                // We have a token, so give it to the delegate.
                _authenticatedBlock([token description], nil);
                succeeded = YES;
            }
        }
    }
    if (!succeeded) {
        // TO DO: What data should we put in the userInfo?
        if (error == nil)
            [NSError errorWithDomain: ATTSpeechServiceHTTPErrorDomain 
                                code: self.statusCode userInfo: nil];
        _authenticatedBlock(nil, error);
    }
    
    // The callback is complete, so clean up.
    [self clear];
}

- (void) connection: (NSURLConnection*) connection didFailWithError: (NSError*) error
{
    // Loading failed. 
    state = LoaderStateFailed;

    _authenticatedBlock(nil, error);
    
    // The callback is complete, so clean up.
    [self clear];
}

// HACK: For bad SSL servers
-(BOOL) connection: (NSURLConnection*) connection
    canAuthenticateAgainstProtectionSpace: (NSURLProtectionSpace*) protectionSpace
{  
    return [protectionSpace.authenticationMethod isEqualToString: NSURLAuthenticationMethodServerTrust];
}   

-(void) connection: (NSURLConnection*) connection
    didReceiveAuthenticationChallenge: (NSURLAuthenticationChallenge*) challenge
{  
    if ([challenge.protectionSpace.authenticationMethod isEqualToString: NSURLAuthenticationMethodServerTrust])  
        //if ([trustedHosts containsObject: challenge.protectionSpace.host])  
            [challenge.sender useCredential: [NSURLCredential  credentialForTrust:challenge.protectionSpace.serverTrust]
                 forAuthenticationChallenge: challenge];  
    [challenge.sender continueWithoutCredentialForAuthenticationChallenge: challenge];
}

@end


