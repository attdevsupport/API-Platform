
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

SCOPE = 'MOBO'

config_file 'config.yml'

set :port, settings.port

# perform the API call
def send_messages
    
if params[:f1] != nil || params[:f2] != nil || params[:f3] != nil || params[:f4] != nil || params[:f5] != nil || params[:groupCheckBox] != "false" then
 
    addresses = ''
    invalidaddresses = ''
    params[:address].each do |p|
    if (p.match('^\d{10}$'))
     addresses += 'Addresses=tel:' + p + '&'
    elsif (p.scan('/^[^@]*@[^@]*\.[^@]*$/'))
      addresses += 'Addresses=' + p + '&'
    else
      invalidaddresses += p
    end
    end
       
    att_idx = 0

    @split = "----=_Part_0_#{((rand*10000000) + 10000000).to_i}.#{((Time.new.to_f) * 1000).to_i}"
    @contents = []

    result = "Content-Type: application/x-www-form-urlencoded; charset=UTF-8"
    result += "\nContent-Transfer-Encoding: 8bit"
    result += "\nContent-Disposition: form-data; name=\"root-fields\""
    result += "\nContent-ID: <startpart>"
    result += "\n\n"
    
    if params[:address].length > 0
      result += "#{addresses}" + 'Subject='+ "#{params[:subject]}" + '&Text='+ "#{params[:message]}" + '&Group='"#{params[:groupCheckBox]}"
    end

    result += "\n\n"
    @contents << result
    
    [ params[:f1], params[:f2], params[:f3], params[:f4], params[:f5] ].each do |param|
      if param
        temp = param[:tempfile]
        file = File.open(temp.path, "rb")
        
        result = "Content-Disposition: form-data; name=\"#{param[:name]}\"; filename=\"#{param[:filename]}\""
        result += "\nContent-Type: #{param[:type]}"
        result += "\nContent-ID: #{param[:type]}"
        result += "\nContent-Transfer-Encoding: binary "
        @file_contents = File.read(file.path)
        attachment = @file_contents

        result += "\n\n#{attachment}"
        result += "\n"

        @contents << result

        file.close
        att_idx += 1
      end
    end
    
    mimeContent = "--#{@split}\n" + @contents.join("--#{@split}\n") + "--#{@split}--\n"
    
    RestClient.post "#{settings.FQDN}/rest/1/MyMessages", "#{mimeContent}", :Authorization => "Bearer #{session[:mobo_access_token]}", :Accept => 'application/json', :Content_Type => 'multipart/related; type="application/x-www-form-urlencoded"; start="<startpart>"; boundary="' + @split + '"' do |response, request, code, &block|
      @mmsresp = response
    end
    
    if @mmsresp.code == 200
      session[:mms_id] = JSON.parse(@mmsresp)["Id"]
      @mms_id = session[:mms_id]
    else
      @mms_send_error = @mmsresp
      session[:mms_error_id] = @mms_send_error
    end
    
else

    addresses = ''
    invalidaddresses = ''
    params[:address].each do |p|
    if (p.match('^\d{8}$'))
      addresses += 'Addresses=short:' + p + '&'
    elsif (p.match('^\d{10}$'))
      addresses += 'Addresses=tel:' + p + '&'
    elsif (p.scan('/^[^@]*@[^@]*\.[^@]*$/'))
      addresses += 'Addresses=' + p + '&'
    else
      invalidaddresses += p
    end
    end
    
    if params[:address].length > 0
      result = "#{addresses}" + 'Subject='+ "#{params[:subject]}" + '&Text='+ "#{params[:message]}" + '&Group=false'
    end
    
    RestClient.post "#{settings.FQDN}/rest/1/MyMessages", "#{result}", :Authorization => "Bearer #{session[:mobo_access_token]}", :Content_Type => 'application/x-www-form-urlencoded' do |response, request, code, &block|
      @smsresp = response
    end
    
    if @smsresp.code == 200
      session[:sms_id] = JSON.parse(@smsresp)["Id"]
      @sms_id = session[:sms_id]
    else
      @sms_send_error = @smsresp
      session[:sms_error_id] = @sms_send_error   
    end
    end
    erb :mobo
end


get '/' do
  erb :mobo
end

get '/auth/callback' do
  response = RestClient.post "#{settings.FQDN}/oauth/access_token?", :grant_type => "authorization_code", :client_id => settings.api_key, :client_secret => settings.secret_key, :code => params[:code]
  from_json = JSON.parse(response.to_str)
  session[:mobo_access_token] = from_json['access_token']
  redirect '/sendMessages'
end

get '/sendMessages' do
  session[:address] = params[:address]
  session[:message] = params[:message]
  session[:subject] = params[:subject]
  if session[:sms_id] != nil then
     @sms_id = session[:sms_id]
  elsif session[:mms_id] != nil then
     @mms_id = session[:mms_id]
  elsif session[:sms_error_id] != nil then
     @sms_send_error = session[:sms_error_id]
  elsif session[:mms_error_id] != nil then
     @mms_send_error = session[:mms_error_id]
  end
  erb :mobo
end

post '/submit' do
  if session[:mobo_access_token].nil? then
    redirect "#{settings.FQDN}/oauth/authorize?client_id=#{settings.api_key}&scope=MOBO&redirect_uri=#{settings.redirect_url}"
  else

  if params[:groupCheckBox].nil?
    params[:groupCheckBox] = "false"
  end

  session[:sms_id] = nil
  session[:mms_id] = nil
  session[:sms_error_id] = nil
  session[:mms_error_id] = nil
  
  addresses = params[:address].strip.split ","
  params[:entered_address] = params[:address]
  
  params[:address] = Array.new
  addresses.each do |address|
    a = parse_address(address)
    if a
      params[:address] << a
    end
  end
  
  if params[:address].length > 0
    send_messages
    
  else
    @error = 'Please enter in a valid phone number'
    return erb :mobo   
  end
  end
end

