
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

['/receiveSms'].each do |path|
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

post '/smslistener' do
  sms_listener
end

get '/receiveSms' do
  receive_sms
end
  
# use this URL to clear token file
get '/clear' do
  File.delete settings.tokens_file if File.exists? settings.tokens_file
  redirect '/'
end

def receive_sms
  @votes = Array.new
  File.open settings.votes_file, 'r' do |f| 
    while (line = f.gets)
      a = line.split
      n = Hash.new
      n[:date_time] = a[0]
      n[:message_id] = a[1] 
      n[:message] = a[2]
      n[:sender] = a[3]
      n[:destination] = a[4]
      
      @invalid_messages = []
      
      text = n[:message]
      if text.downcase.eql? 'football'
         session[:batch1] = session[:batch1]+1
         @votes.push n
      elsif text.downcase.eql? 'baseball' 
         session[:batch2] = session[:batch2]+1
         @votes.push n
      elsif text.downcase.eql? 'basketball' 
         session[:batch3] = session[:batch3]+1
         @votes.push n
      else 
         @invalid_messages.push n
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
    end
  end
  rescue => e
  @received_error = e.response
ensure
  return erb :sms2
end


def sms_listener
  input   = request.env["rack.input"].read

  File.open("#{settings.mosms_file_dir}/notifications", 'a+') { |f| f.puts input }
  
  sms_list = JSON.parse input
  
  @date_time = sms_list['DateTime']
  @message_id = sms_list['MessageId']
  @message = sms_list['Message']
  @sender = sms_list['SenderAddress']
  @destination = sms_list['DestinationAddress']
  
  File.open("#{settings.mosms_file_dir}/vote_data", 'w+') { |f| f.puts @date_time + ' ' + @message_id + ' ' + @message + ' ' + @sender + ' ' + @destination }
  
  ensure
  return erb :sms2
end

