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

SCOPE = 'SMS,MMS'

# setup filter fired before reaching our urls
# this is to ensure we are o-authenticated before actual action (like sendSms)

# autonomous version

['/sendSms', '/getDeliveryStatus', '/getReceivedSms'].each do |path|
  before path do
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  erb :sms
end

post '/sendSms' do
  session[:sms1_address] = params[:address]
  session[:sms1_message] = params[:message]
  send_sms
end

get '/getDeliveryStatus' do
  session[:sms_id] = params[:smsId]
  get_delivery_status
end

get '/getReceivedSms' do
  get_received_sms
end

# use this URL to clear token file
get '/clear' do
  File.delete settings.tokens_file if File.exists? settings.tokens_file
  redirect '/'
end

def send_sms
  if @address_valid = parse_address(session[:sms1_address])
    address = 'tel:' + params[:address].gsub("-","")
    response = RestClient.post "#{settings.FQDN}/rest/sms/2/messaging/outbox?access_token=#{@access_token}", :Address => address, :Message => params[:message]

    @sms_id = session[:sms_id] = JSON.parse(response)['Id']
  end
rescue => e
  @send_error = e.response
ensure
  return erb :sms
end


def get_delivery_status
  response = RestClient.get "#{settings.FQDN}/rest/sms/2/messaging/outbox/#{session[:sms_id]}?access_token=#{@access_token}"

  delivery_info_list = JSON.parse(response).fetch 'DeliveryInfoList';
  delivery_info = delivery_info_list['DeliveryInfo'].first

  @delivery_status = delivery_info['DeliveryStatus']
  @resource_URL = delivery_info_list['ResourceURL']

rescue => e
  @delivery_error = e.response
ensure
  return erb :sms
end


def get_received_sms
  response = RestClient.get "#{settings.FQDN}/rest/sms/2/messaging/inbox?access_token=#{@access_token}&RegistrationID=#{params[:getReceivedSms]}"

  messageList = JSON.parse(response).fetch 'InboundSmsMessageList'

  @messages_in_batch = messageList['NumberOfMessagesInThisBatch']
  @messages_pending  = messageList['TotalNumberOfPendingMessages']
  @messages_inbound  = messageList['InboundSmsMessage']

rescue => e
  @received_error = e.response
ensure
  return erb :sms
end

