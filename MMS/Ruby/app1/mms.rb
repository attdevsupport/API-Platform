#!/usr/bin/ruby
require 'rubygems'
require 'json'
require 'rest_client'
require 'sinatra'
require 'sinatra/config_file'
require 'base64'
require File.join(File.dirname(__FILE__), 'common.rb')

enable :sessions

config_file 'config.yml'

set :port, settings.port

SCOPE = 'SMS,MMS'

# setup filter fired before reaching our urls
# this is to ensure we are o-authenticated before actual action (like sendMms)

# autonomous version

['/sendMms', '/getMmsDeliveryStatus'].each do |path|
  before path do
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  erb :mms
end

post '/sendMms' do
  session[:mms1_address] = params[:address]
  session[:mms1_subject] = params[:subject]
  send_mms
end
  
get '/getMmsDeliveryStatus' do
  session[:mms_id] = params[:mmsId]
  get_mms_delivery_status
end

# use this URL to clear token file
get '/clear' do
  File.delete settings.tokens_file if File.exists? settings.tokens_file
  redirect '/'
end

def send_mms
  if @address_valid = parse_address(session[:mms1_address])
    address = 'tel:' + session[:mms1_address].gsub("-","")
    att_idx = 0

    @split = "----=_Part_0_#{((rand*10000000) + 10000000).to_i}.#{((Time.new.to_f) * 1000).to_i}"
    @contents = []

    result = "Content-Type: application/json"
    result += "\nContent-ID: <startpart>"
    result += "\nContent-Disposition: form-data; name=\"root-fields\""
    result += "\n\n"
    result += '{ "Address" : "' + "#{address}" + '", "Subject" : "' + "#{params[:subject]}" + '", "Priority": "High" }'
    result += "\n"

    @contents << result

    [ params[:f1], params[:f2], params[:f3] ].each do |param|
      if param
        temp = param[:tempfile]
        file = File.open(temp.path, "rb")

        result = "Content-Type: #{param[:type]}"
        content_id = "<attachment#{att_idx}>"

        result += "\nContent-ID: #{content_id}"
        result += "\nContent-Transfer-Encoding: base64 "
        result += "\nContent-Disposition: attachment; name=\"\"; filename=\"\""
        attachment = Base64.encode64(file.read)

        result += "\n\n#{attachment}"
        result += "\n"

        @contents << result

        file.close
        att_idx += 1
      end
    end

    mimeContent = "--#{@split}\n" + @contents.join("--#{@split}\n") + "--#{@split}--\n"
    response = RestClient.post "#{settings.FQDN}/rest/mms/2/messaging/outbox?access_token=#{@access_token}", "#{mimeContent}", :Accept => 'application/json', :Content_Type => 'multipart/form-data; type="application/json"; start=""; boundary="' + @split + '"'

    @mms_id = session[:mms_id] = JSON.parse(response)['Id']
  end
rescue => e
  @send_error = e.response
ensure
  return erb :mms
end

def get_mms_delivery_status
  response = RestClient.get "#{settings.FQDN}/rest/mms/2/messaging/outbox/#{session[:mms_id]}?access_token=#{@access_token}"

  delivery_info_list = JSON.parse(response).fetch 'DeliveryInfoList'
  delivery_info = delivery_info_list['DeliveryInfo'].first

  @delivery_status = delivery_info['DeliveryStatus']
  @delivery_URL    = delivery_info_list['ResourceURL']
rescue => e
  @delivery_error = e.response
ensure
  return erb :mms
end

