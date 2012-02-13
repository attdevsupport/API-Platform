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

def authorize
  # obtain an access token if necessary
  if(session[:tl_access_token] == nil or session[:tl_address_for_token] != session[:tl_address]) then
    session[:tl_address_for_token] = session[:tl_address]
    redirect "#{settings.FQDN}/oauth/authorize?client_id=#{settings.api_key}&scope=TL&redirect_uri=#{settings.redirect_url}"
  else
    redirect '/getDeviceLocation'
  end
end

# perform the API call
def get_device_location
  url = "#{settings.FQDN}/1/devices/tel:#{session[:tl_address]}/location?"

  # access_token
  url += "access_token=#{session[:tl_access_token]}&"
  # accuracy
  url += "RequestedAccuracy=#{session[:tl_requested_accuracy]}&AcceptableAccuracy=#{session[:tl_acceptable_accuracy]}&"
  # tolerance
  url += "Tolerance=#{session[:tl_tolerance]}"

  t1 = Time.now
  RestClient.get url do |response, request, code, &block|
    @r = response
  end
  session[:tl_elapsed] = (Time.now-t1).truncate
  
  if @r.code == 200
    @result = JSON.parse @r
  else
    @error = @r
  end

rescue => e
  @error = e.message
ensure
  return erb :tl
end

# -- methods --
# '/' -> '/submit' -> oAuth -> '/getDeviceLocation' -> '/'


get '/' do
  erb :tl
end

get '/auth/callback' do
  response = RestClient.get "#{settings.FQDN}/oauth/token?grant_type=authorization_code&client_id=#{settings.api_key}&client_secret=#{settings.secret_key}&code=#{params[:code]}"
  from_json = JSON.parse(response.to_str)
  session[:tl_access_token] = from_json['access_token']
  redirect '/getDeviceLocation'
end

get '/getDeviceLocation' do
  get_device_location
end

post '/submit' do
    
  a = parse_address(params[:address])

  unless a
    @error = 'Phone number format not recognized, try xxx-xxx-xxxx or xxxxxxxxxx'
    return erb :tl
  else
    session[:tl_address] = a
    session[:tl_entered_address] = params[:address]
    session[:tl_requested_accuracy] = params[:requestedAccuracy]
    session[:tl_acceptable_accuracy] = params[:acceptableAccuracy]
    session[:tl_tolerance] = params[:tolerance]
    authorize
  end
end
