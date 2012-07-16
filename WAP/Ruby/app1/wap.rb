
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

Scope = 'WAP'

# perform the API call
def wapPush
  @split = "----=_Part_0_#{((rand*10000000) + 10000000).to_i}.#{((Time.new.to_f) * 1000).to_i}"
  @contents = []

  # part 1
  part1 = "Content-Type: application/json"
  part1 += "\nContent-ID: <startpart>"
  part1 += "\nContent-Disposition: form-data; name=\"root-fields\""
  part1 += "\n\n"
  part1 += '{ "address" : "tel:' + session[:wap_address] + '"}'
  part1 += "\n"
  @contents << part1

  attachmentBody = "Content-Disposition: form-data; name=\"PushContent\"\n";
  attachmentBody += "Content-Type: text/vnd.wap.si\n";
  attachmentBody += "Content-Length: 20\n";
  attachmentBody += "X-Wap-Application-Id: x-wap-application:wml.ua\n\n";
  attachmentBody += "<?xml version=\"1.0\"?>\n";
  attachmentBody += "<!DOCTYPE si PUBLIC \"-//WAPFORUM//DTD SI 1.0//EN\" \"http://www.wapforum.org/DTD/si.dtd\">\n";
  attachmentBody += "<si>";
  attachmentBody += "<indication href=\"" + session[:wap_url] + "\" action=\"signal-medium\" si-id=\"6532\" >\n";
  attachmentBody += session[:wap_alert] + "\n";
  attachmentBody += "</indication>\n";
  attachmentBody += "</si>\n";
  
  # part 2
  part2 = "Content-Transfer-Encoding: base64\n"
  part2 += "Content-ID: <attachment>\n"
  part2 += "Content-Disposition: attachment; name=\"\"; filename=\"\"\n\n"
  part2 += Base64.encode64(attachmentBody);

  @contents << part2

  mimeContent = "--#{@split}\n" + @contents.join("--#{@split}\n") + "--#{@split}--\n"
  
  url = "#{settings.FQDN}/1/messages/outbox/wapPush?"
  
  RestClient.post url, mimeContent, :Authorization => "Bearer #{@access_token}", :Accept => 'application/json', :Content_Type => 'multipart/form-data; type="application/json"; start=""; boundary="' + @split + '"' do |response, request, code, &block|
    @r = response
  end

  if @r.code == 200
    @result = JSON.parse @r
    session[:wap_id] = @result['id']
  else
    @error = @r
  end
  
rescue => e
  @error = e.message
ensure
  return erb :wap
end

# -- methods --
# '/' -> '/submit' -> '/'

get '/' do
  erb :wap
end

post '/submit' do
  a = parse_address(params[:address])

  unless a
    @error = 'Phone number format not recognized, try xxx-xxx-xxxx or xxxxxxxxxx'
    return erb :wap
  else
    session[:wap_entered_address] = params[:address]
    session[:wap_entered_alert] = params[:alert]
    session[:wap_entered_url] = params[:url]
    session[:wap_address] = a

    session[:wap_alert] = params[:alert].strip
    session[:wap_url] = params[:url].strip

    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, Scope, settings.tokens_file)
    wapPush
  end
end


