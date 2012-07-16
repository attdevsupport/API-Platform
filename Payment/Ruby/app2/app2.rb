
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

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

SCOPE = 'PAYMENT'

['/','/newSubscription','/getSubscriptionStatus','/getSubscriptionDetails','/refundSubscription','/refreshNotifications', '/callbackSubscription'].each do |path|
  before path do
    read_recent_items
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  erb :app2
end

post '/newSubscription' do
  new_subscription
end

get '/callbackSubscription' do
  callback_subscription
end

post '/getSubscriptionStatus' do
  get_subscription_status
end

post '/getSubscriptionDetails' do
  get_subscription_details
end

post '/refundSubscription' do
  refund_subscription
end

get '/refreshNotifications' do
  refresh_notifications
end

# URL handlers go here

def read_recent_items
  @subscriptions = Array.new
  File.open settings.subscriptions_file, 'r' do |f| 
    while (line = f.gets)
      a = line.split
      t = Hash.new
      t[:merchant_subscription_id] = a[0]
      t[:subscription_id] = a[1]
      #t[:subscription_auth_code] = a[1] 
      t[:consumer_id] = a[2]
      @subscriptions.push t
    end 
  end
  @subscriptions = @subscriptions.drop(@subscriptions.length - settings.recent_subscriptions_stored) if @subscriptions.length > settings.recent_subscriptions_stored
rescue
  return
end

def write_recent_items
  File.open settings.subscriptions_file, 'w+' do |f|
    @subscriptions.each do |t|
      f.puts t[:merchant_subscription_id] + ' ' + (t[:subscription_id] ? ' ' + t[:subscription_id] : '') + (t[:consumer_id] ? ' ' + t[:consumer_id] : '')
    end
  end
end

def new_subscription
  
  session[:merchant_transaction_id] = 'User' + sprintf('%03d', rand(1000)) + 'Subscription' + sprintf('%04d', rand(10000))
  session[:merchant_subscription_id_list] = 'MSList' + sprintf('%04d', rand(10000))
  
  # prepare payload
  data = {
    :Amount => params[:product] == "1" ? 1.99 : 3.99,
    :Category => 1,
    :Channel => 'MOBILE_WEB',
    :Description => 'Word game 1',
    :MerchantTransactionId => session[:merchant_transaction_id],
    :MerchantProductId => 'wordGame1',
    :MerchantPaymentRedirectUrl => settings.subscription_redirect_url,
    :MerchantSubscriptionIdList => session[:merchant_subscription_id_list],
    :IsPurchaseOnNoActiveSubscription => 'false',
    :SubscriptionRecurrences => '99999',
    :SubscriptionPeriod => 'MONTHLY',
    :SubscriptionPeriodAmount => '1'
  }

  response = RestClient.post settings.notary_app_sign_url, :payload => data.to_json
  from_json = JSON.parse response

  u = settings.FQDN + "/rest/3/Commerce/Payment/Subscriptions?Signature=#{from_json['signature']}&SignedPaymentDetail=#{from_json['signed_payload']}&clientid=#{settings.api_key}"
  
  redirect u
end

def callback_subscription
  @new_subscription = Hash.new

  @new_subscription[:merchant_transaction_id] = session[:merchant_transaction_id]
  @new_subscription[:subscription_auth_code] = params['SubscriptionAuthCode']
  params['SubscriptionAuthCode'] = session[:subscription_auth_code]
  @subscriptions.push @new_subscription

  @subscriptions.delete_at 0 if @subscriptions.length > settings.recent_subscriptions_stored
  write_recent_items

ensure
  return erb :app2
end


def get_subscription_status
  if params['getSubscriptionType'] == '1'
    u = settings.FQDN + "/rest/3/Commerce/Payment/Subscriptions/MerchantTransactionId/" + session[:merchant_transaction_id]
  elsif params['getSubscriptionType'] == '2'
    u = settings.FQDN + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionAuthCode/" + @subscriptions.last[:subscription_auth_code]
  elsif params['getSubscriptionType'] == '3'
    u = settings.FQDN + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionId/" + @subscriptions.last[:subscription_id]
  end
  
  RestClient.get u, :Authorization => "Bearer #{@access_token}", :Content_Type => 'application/json', :Accept => 'application/json' do |response, request, code, &block|
    @r = response
  end
  
  if @r.code == 200 

  @subscription_status = @subscriptions.last
  @subscription_status[:status] = JSON.parse @r
  
  @subscriptions.last[:subscription_id] = @subscription_status[:status]['SubscriptionId']
  session[:subscription_id] = @subscriptions.last[:subscription_id]
  @subscriptions.last[:merchant_subscription_id] = @subscription_status[:status]['MerchantSubscriptionId']
  @subscriptions.last[:consumer_id] = @subscription_status[:status]['ConsumerId']
  
  write_recent_items
  else
    @subscription_status_error = @r
  end
  
  erb :app2
end


def get_subscription_details
  
  if params['consumer_id'].nil? || params['consumer_id'].empty?
    redirect '/'
  end

  url = settings.FQDN + "/rest/3/Commerce/Payment/Subscriptions/" + session[:merchant_subscription_id_list] + "/Detail/" + @subscriptions.last[:consumer_id]
  
  RestClient.get url, :Authorization => "Bearer #{@access_token}", :Content_Type => 'application/json', :Accept => 'application/json' do |response, request, code, &block|
    @r = response
  end
  
  if @r.code == 200
  
   @subscription_details = @subscriptions.last
   @subscription_details[:status] = JSON.parse @r
   session[:consumer_id] = params[:consumer_id]
   
else
    @details_error = @r
  end
  erb :app2
end


def refund_subscription
  if params['trxId'].nil? || params['trxId'].empty?
    redirect '/'
  end

  url = settings.FQDN + "/rest/3/Commerce/Payment/Transactions/" + params['trxId']
  
  payload = Hash.new
  payload['TransactionOperationStatus'] = 'Refunded'
  payload['RefundReasonCode'] = 1
  payload['RefundReasonText'] = 'User did not like product'
 
  RestClient.put url, payload.to_json, :Authorization => "Bearer #{@access_token}", :Content_Type => 'application/json', :Accept => 'application/json' do |response, request, code, &block|
    @r = response
  end
  
  if @r.code == 200
    @subscription_refund = @subscriptions.last
    @subscription_refund[:status] = JSON.parse @r
    @refund = Hash.new
    @refund[:subscription_id] = params['trxId']
  else
    @refund_error = @r
  end
  erb :app2
end


def refresh_notifications


ensure
  return erb :app2
end



