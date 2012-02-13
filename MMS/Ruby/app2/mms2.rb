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
  result += '{ "Address" : ['+"#{addresses}"+'], "Subject" : "' + session[:mms2_subject] + '", "Priority": "High" }'
  result += "\n"
  contents << result

  # part 2
  result = "Content-Type: image/jpeg"
  result += "\nContent-ID: '<attachment>'"
  result += "\nContent-Transfer-Encoding: base64 "
  result += "\nContent-Disposition: attachment; name=\"\"; filename=\"\""
  result += "\n\n" + attachment
  result += "\n"
  contents << result
          
  mimeContent = "--#{split}\n" + contents.join("--#{split}\n") + "--#{split}--\n"

  # send
  RestClient.post "#{settings.FQDN}/rest/mms/2/messaging/outbox?access_token=#{@access_token}",
    mimeContent, :Accept => 'application/json',
    :Content_Type => 'multipart/form-data; type="application/json"; start=""; boundary="' + split + '"' do |response, request, result, &block|
    @r = response
  end

  if @r.code == 201
    @result = JSON.parse @r
    session[:mms2_id] = @result['Id']
  else
    @error = @r
  end
rescue => e
  @error = e.message
ensure
  return erb :mms2
end

def check_status
  if session[:mms2_id].nil?
    @error2 = 'You need to send an MMS first'
    return
  end
  
  url = "#{settings.FQDN}/rest/mms/2/messaging/outbox/" + session[:mms2_id]

  # access_token
  url += "?access_token=#{@access_token}"
  # mms id
  url += "&Id=" + session[:mms2_id]

  RestClient.get url do |response, request, result, &block|
    @r = response
  end

  if @r.code == 200
    @result2 = JSON.parse @r
  else
    @error2 = @r
  end
  
rescue => e
  @error2 = e.message
ensure
  return erb :mms2
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
  
  @error = ''

  addresses.each do |address|
    a = parse_address(address)
    if a
      session[:mms2_address] << a
    else
      @error += 'Phone number format not recognized: ' + address + '<br>'
    end
  end

  if session[:mms2_address].length > 0
    send_messages
  else
    return erb :mms2
  end
end

post '/checkStatus' do
  check_status
end
