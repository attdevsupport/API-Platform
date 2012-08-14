
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

#!/usr/bin/ruby
require 'rubygems'
require 'json'
require 'rest_client'
require 'sinatra'
require 'open-uri'
require 'sinatra/config_file'
require File.join(File.dirname(__FILE__), 'common.rb')

enable :sessions

config_file 'config.yml'

set :port, settings.port

SCOPE = 'SPEECH'

error do
  @error = e.message
end

['/SpeechToText'].each do |path|
  if settings.api_key.nil? || settings.secret_key.nil?
    raise RuntimeError, "The API and Secret keys must be set the config.yml file"
  end
  
  before path do
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  erb :speech
end

post '/SpeechToText' do
  if params[:f1] != nil
    speech_to_text
  else
    speech_default_file
  end
end 


def speech_to_text
  @type = params[:f1][:type]
  temp_file = params[:f1][:tempfile]

  @file_contents = File.read(temp_file.path)

  if @type == "application/octet-stream"
    @type = "audio/amr"
  end

  url = "#{settings.FQDN}/rest/1/SpeechToText"

  response = RestClient.post "#{settings.FQDN}/rest/1/SpeechToText", "#{@file_contents}", :Authorization => "Bearer #{@access_token}", :Content_Transfer_Encoding => 'chunked', :X_SpeechContext => 'Generic', :Content_Type => "#{@type}" , :Accept => 'application/json'

  @result = JSON.parse response
rescue => e
  @error = e.message
ensure
  return erb :speech
end



def speech_default_file
  @filename = 'bostonSeltics.wav'
  @type = 'audio/wav'


  fullname = File.expand_path(File.dirname(File.dirname(__FILE__)))
  final = fullname + '/app1/' + @filename
  @file_contents = File.read(final)

  url = "#{settings.FQDN}/rest/1/SpeechToText"

  response = RestClient.post "#{settings.FQDN}/rest/1/SpeechToText", "#{@file_contents}", :Authorization => "Bearer #{@access_token}", :Content_Transfer_Encoding => 'chunked', :X_SpeechContext => 'Generic', :Content_Type => "#{@type}" , :Accept => 'application/json'

  @result = JSON.parse response
rescue => e
  @error = e.message
ensure
  return erb:speech
end