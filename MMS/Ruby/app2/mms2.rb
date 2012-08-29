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
require File.join(File.dirname(__FILE__), 'common.rb')

enable :sessions

config_file 'config.yml'

set :port, settings.port

Scope = 'MMS'

# reads subject and phone numbers from text files (one phone number per line)
def read_files
  begin
    f = File.open(settings.subject_file, "r")
    session[:mms2_subject] = f.read.strip
  rescue
    session[:mms2_subject] = ''
  end
  
  begin
    f = File.open(settings.phones_file, "r")
    contents = f.read.strip
    session[:mms2_phones] = contents.split "\n"
  rescue
    session[:mms2_phones] = Array.new
  end
  
end


# composes and sends MMS messages to given phone numbers
def send_messages
  coupon_file = File.open("public/" + settings.coupon_file, 'rb')
  attachment = Base64.encode64(coupon_file.read)
  
  split = "----=_Part_0_#{((rand*10000000) + 10000000).to_i}.#{((Time.new.to_f) * 1000).to_i}"
  
  addresses = ''
  session[:mms2_address].each do |p|
    addresses += '"tel:' + p + '",'
  end

  contents = Array.new
  
  # part 1
  result = "Content-Type: application/json"
  result += "\nContent-ID: <startpart>"
  result += "\nContent-Disposition: form-data; name=\"root-fields\""

  result += "\n\n"
  if session[:mms2_address].length > 1
    result += '{ "Address" : ['+"#{addresses}"+'], "Subject" : "' + session[:mms2_subject] + '", "Priority": "High" }'
  else
    result += '{ "Address" : "' + 'tel:' + session[:mms2_entered_address] + '", "Subject" : "' + session[:mms2_subject] + '", "Priority": "High" }'
  end
  result += "\n"
  contents << result

  # part 2
  result = "Content-Type: image/jpeg; name=coupon.jpg"
  result += "\nContent-ID: '<attachment>'"
  result += "\nContent-Transfer-Encoding: base64 "
  result += "\nContent-Disposition: attachment; filename=coupon.jpg"  
  result += "\n\n" + attachment
  result += "\n"
  contents << result
          
  mimeContent = "--#{split}\n" + contents.join("--#{split}\n") + "--#{split}--\n"

  # send
  RestClient.post "#{settings.FQDN}/rest/mms/2/messaging/outbox?", "#{mimeContent}", :Authorization => "Bearer #{@access_token}",
  :Accept => 'application/json',
    :Content_Type => 'multipart/related; boundary="' + split + '"' do |response, request, result, &block|
  @r = response
  end

  if @r.code == 201
    @result = JSON.parse @r
    session[:mms2_id] = @result['Id']
  else
    @send_error = @r
  end
 erb :mms2
end

def check_status
  if session[:mms2_id].nil?
    redirect '/'
  end
  
  url = "#{settings.FQDN}/rest/mms/2/messaging/outbox/" + session[:mms2_id]

  RestClient.get url, :Authorization => "Bearer #{@access_token}" do |response, request, result, &block|
    @r = response
  end

  if @r.code == 200
    @result2 = JSON.parse @r
  else
    @error2 = @r
  end
  
  erb :mms2
end

# -- methods --
# '/' -> '/submit' -> '/'
# '/' -> '/check-status' -> '/'

['/submit','/checkStatus'].each do |path|
  before path do
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, Scope, settings.tokens_file)
  end
end

get '/' do
  read_files
  erb :mms2
end

post '/submit' do
 
  addresses = params[:address].strip.split ","
  session[:mms2_entered_address] = params[:address]
  
  session[:mms2_address] = Array.new
  addresses.each do |address|
    a = parse_address(address)
    if a
      session[:mms2_address] << a
    end
  end

  if session[:mms2_address].length > 0
    send_messages
  else
    @error = 'Please enter in a valid phone number'
    return erb :mms2
  end
end

post '/checkStatus' do
  check_status
end

