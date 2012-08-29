
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

#!/usr/bin/ruby
require 'rubygems'
require 'json'
require 'base64'
require 'rest_client'
require 'sinatra'
require 'sinatra/config_file'
require File.join(File.dirname(__FILE__), 'common.rb')

enable :sessions

SCOPE = 'MIM'

config_file 'config.yml'

set :port, settings.port

def authorize
  # obtain an access token if necessary
  if session[:mim_access_token].nil? then
    redirect "#{settings.FQDN}/oauth/authorize?client_id=#{settings.api_key}&scope=MIM&redirect_uri=#{settings.redirect_url}"
  else
    redirect '/getMessageHeader'
  end
end

# perform the API call
def get_message_header

  if params[:headerCrsrTextBox].nil? then
    url = "#{settings.FQDN}/rest/1/MyMessages?HeaderCount=" + session[:headerCntTextBox]
  else
    url = "#{settings.FQDN}/rest/1/MyMessages?HeaderCount=" + session[:headerCntTextBox] + "&IndexCursor" + session[:headerCrsrTextBox]
  end
  
  RestClient.get url, :Authorization => "Bearer #{session[:mim_access_token]}", :Accept => 'application/json', :Content_Type => 'application/json' do |response, request, code, &block|
    @r = response
  end
  
  if @r.code == 200
    @result = JSON.parse @r
  else
    @error = @r
  end

rescue => e
  @error = e.message
ensure
  return erb :mim
end

def get_message_content

  url = "#{settings.FQDN}/rest/1/MyMessages/" + session[:MessageId] + "/" + session[:PartNumber]
  
  RestClient.get url, :Authorization => "Bearer #{session[:mim_access_token]}", :Accept => 'application/json', :Content_Type => 'application/json' do |response, request, code, &block|
    @r = response
  end
  
  if @r.code == 200
    @content_result = @r
    @headers = @content_result.headers[:content_type]
    content_string = @headers.split("; ")
    @image_string = @headers.split("/")
    @image = @image_string[0]
    @image_content = content_string[0]
     
  else
    @content_error = @r
end

rescue => e
  @content_error = e.message
ensure
  return erb :mim
end

get '/' do
  erb :mim
end

get '/auth/callback' do
  response = RestClient.post "#{settings.FQDN}/oauth/access_token?", :grant_type => "authorization_code", :client_id => settings.api_key, :client_secret => settings.secret_key, :code => params[:code]
  from_json = JSON.parse(response.to_str)
  session[:mim_access_token] = from_json['access_token']
  redirect '/getMessageHeader'
end

get '/getMessageHeader' do
  get_message_header
end

get '/getMessageContent' do
  get_message_content
end

post '/submit' do
  session[:headerCntTextBox] = params[:headerCntTextBox]
  session[:headerCrsrTextBox] = params[:headerCrsrTextBox]
  authorize
end

post '/submit1' do
  session[:MessageId] = params[:MessageId]
  session[:PartNumber] = params[:PartNumber]
  get_message_content
end
