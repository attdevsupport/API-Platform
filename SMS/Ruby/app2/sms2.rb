
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

#!/usr/bin/ruby
require 'rubygems'
require 'json'
require 'rest_client'
require 'sinatra'
require 'sinatra/config_file'
require File.join(File.dirname(__FILE__), 'common.rb')

enable :sessions

config_file 'config.yml'

set :port, settings.port

SCOPE = 'SMS'


# setup filter fired before reaching our urls
# this is to ensure we are o-authenticated before actual action (like getReceivedSms)

# autonomous version

['/getReceivedSms', '/getVotes'].each do |path|
  before path do
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  session[:batch1] ||= 0
  session[:batch2] ||= 0
  session[:batch3] ||= 0

  File.open 'tally1.txt', 'r+' do |f|
    session[:batch1] = f.read.to_i
  end
  File.open 'tally2.txt', 'r+' do |f|
    session[:batch2] = f.read.to_i
  end
  File.open 'tally3.txt', 'r+' do |f|
    session[:batch3]= f.read.to_i
  end
  @received_total = session[:batch1]+session[:batch2]+session[:batch3]
  erb :sms2
end

get '/getReceivedSms' do
  get_received_sms
end

get '/getVotes' do
  get_votes
end
  
# use this URL to clear token file
get '/clear' do
  File.delete settings.tokens_file if File.exists? settings.tokens_file
  redirect '/'
end


def get_received_sms
  response = RestClient.get "#{settings.FQDN}/rest/sms/2/messaging/inbox?RegistrationID=#{settings.registration_id}", :Authorization => "Bearer #{@access_token}", :Accept => "application/json"

  sms_list = JSON.parse(response)['InboundSmsMessageList']

  @messages_in_this_batch = sms_list['NumberOfMessagesInThisBatch'].to_i
  @total_pending_messages = sms_list['TotalNumberOfPendingMessages'].to_i
  @messages = sms_list['InboundSmsMessage']

  @invalid_messages = []

  @messages.each do |message|
    text = message['message']
    if text.downcase.eql? 'football'
      session[:batch1] = session[:batch1]+1
    elsif text.downcase.eql? 'baseball' 
      session[:batch2] = session[:batch2]+1
    elsif text.downcase.eql? 'basketball' 
      session[:batch3] = session[:batch3]+1
    else 
      @invalid_messages.push message
    end
  end

  @received_total = session[:batch1]+session[:batch2]+session[:batch3]

  File.open 'tally1.txt', 'w' do |f|
    f.write(session[:batch1])
  end
  File.open 'tally2.txt', 'w' do |f|
    f.write(session[:batch2])
  end
  File.open 'tally3.txt', 'w' do |f|
    f.write(session[:batch3])
  end
rescue => e
  @received_error = e.response
ensure
  return erb :sms2
end

def get_votes
  get_received_sms

  { :totalNumberOfVotes => @received_total, 
    :footballVotes => session[:batch1], 
    :baseballVotes => session[:batch2], 
    :basketballVotes => session[:batch3] }.to_json
end
