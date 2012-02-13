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

# obtain an OAuth access token if necessary
def authorize
  if session[:dc_access_token].nil? or session[:dc_address_for_token] != session[:dc_address] then
    session[:dc_address_for_token] = session[:dc_address]
    redirect "#{settings.FQDN}/oauth/authorize?client_id=#{settings.api_key}&scope=DC&redirect_uri=#{settings.redirect_url}"
  else
    redirect '/getDeviceCapabilities'
  end
end

# perform the API call
def get_device_capabilities
  url = "#{settings.FQDN}/1/devices/tel:#{session[:dc_address]}/info?"

  # access_token
  url += "access_token=#{session[:dc_access_token]}"

  RestClient.get url do |response, request, code, &block|
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
  return erb :dc
end

# -- methods --
# '/' -> '/submit' -> oAuth -> '/getDeviceCapabilities' -> '/'

get '/' do
  erb :dc
end

get '/auth/callback' do
  response = RestClient.get "#{settings.FQDN}/oauth/token?grant_type=authorization_code&client_id=#{settings.api_key}&client_secret=#{settings.secret_key}&code=#{params[:code]}"
  from_json = JSON.parse(response.to_str)
  session[:dc_access_token] = from_json['access_token']
  redirect '/getDeviceCapabilities'
end


get '/getDeviceCapabilities' do
  get_device_capabilities
end

post '/submit' do
  # validate phone number
  a = parse_address(params[:address])
  unless a
    @error = 'Phone number format not recognized, try xxx-xxx-xxxx or xxxxxxxxxx'
    erb :dc
  else
    session[:dc_address] = a
    session[:dc_entered_address] = params[:address]
    authorize # after a successful authorization browser will be redirected to /auth/callback and then /get-device-capabilities
  end

end

