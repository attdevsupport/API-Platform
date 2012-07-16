
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

#!/usr/bin/ruby
require 'rubygems'
require 'json'
require 'rest_client'
require 'base64'
require 'sinatra'
require 'sinatra/config_file'

enable :sessions

config_file 'configNotary.yml'

set :port, settings.port

set :notary_result, Hash.new
set :notary_payload, ''

def split_string string
  a = string.split(/(.....)/)
  a.delete_if do |item|
    item == ''
  end
  result = ''

  i = 0
  a.each do |e|
    result += e + ' '
    i += 1
    result += '<br>' if i%7 == 0
  end

  result
end

def sign_payload payload
  RestClient.post "#{settings.FQDN}/Security/Notary/Rest/1/SignedPayload", 
  payload, :Accept => 'application/json', :Content_Type => 'application/json', 'client_id' => settings.api_key, 'client_secret' => settings.secret_key do |response, request, code, &block|
    @r = response
  end

  from_json = JSON.parse @r
  
  result = Hash.new
  result[:signed_payload] = from_json['SignedDocument']
  result[:signature] = from_json['Signature']
  
  set :notary_payload, params[:payload]
  set :notary_result, result
  
  return result

end

def api_call
  result = sign_payload params[:payload]
  result[:signature] = split_string result[:signature]
  result[:signed_payload] = split_string result[:signed_payload]

  set :notary_payload, params[:payload]
  set :notary_result, result
rescue => e
  @error = e.message
ensure
  return erb :notary
end

# -- methods --
# '/' -> '/submit' -> '/'

get '/' do
  erb :notary
end

post '/submit' do
  api_call
end

post '/signPayload' do
  content_type :json
  r = sign_payload params[:payload]
  result = Hash.new 
  result[:signature] = split_string r[:signature]
  result[:signed_payload] = split_string r[:signed_payload]

  set :notary_payload, params[:payload]
  set :notary_result, result

  return r.to_json
end

