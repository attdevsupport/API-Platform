
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

['/SpeechToText'].each do |path|
  before path do
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  erb :speech
end

post '/SpeechToText' do
  if params[:f1] != nil
    @type = (params[:f1][:filename]).to_s.split(".")[1]
    #Basic file extension check to ensure proper file types are uploaded
    #Some times browser's may recognize mime types are application/octet-stream if the system does not know about the files mime type
    if @type.to_s.eql?"wav"
      @type = "audio/wav"
      speech_to_text
    elsif @type.to_s.eql?"amr"
      @type = "audio/amr"
      speech_to_text
    else
      @error = "Invalid file type, use audio/wav,audio/x-wav or audio/amr formats..."
      return erb :speech
    end
  else
    speech_default_file
  end
end


def speech_to_text

  temp_file = params[:f1][:tempfile]

  @file_contents = File.read(temp_file.path)

  url = "#{settings.FQDN}/rest/1/SpeechToText"

  response = RestClient.post "#{settings.FQDN}/rest/1/SpeechToText", "#{@file_contents}", :Authorization => "Bearer #{@access_token}", :Content_Transfer_Encoding => 'chunked', :X_SpeechContext => 'Generic', :Content_Type => "#{@type}" , :Accept => 'application/json'

  @result = JSON.parse response

rescue => e
  if e.response.nil?
    @error = e.message
  else
    @error = e.response
  end
ensure
  return erb :speech
end



def speech_default_file
  @filename = 'bostonSeltics.wav'
  @type = 'audio/wav'


  fullname = File.expand_path(File.dirname(File.dirname(__FILE__)))
  final = fullname + '/' + @filename
  @file_contents = File.read(final)

  url = "#{settings.FQDN}/rest/1/SpeechToText"

  response = RestClient.post "#{settings.FQDN}/rest/1/SpeechToText", "#{@file_contents}", :Authorization => "Bearer #{@access_token}", :Content_Transfer_Encoding => 'chunked', :X_SpeechContext => 'Generic', :Content_Type => "#{@type}" , :Accept => 'application/json'

  @result = JSON.parse response

rescue => e
  if e.response.nil?
    @error = e.message
  else
    @error = e.response
  end
ensure
  return erb:speech
end
