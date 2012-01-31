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


# setup filter fired before reaching our urls
# this is to ensure we are o-authenticated before actual action

# autonomous version

['/','/createSubscription','/getSubscriptionStatus','/getSubscriptionDetails','/refundSubscription','/refreshNotifications', '/callbackSubscription'].each do |path|
  before path do
    @subscriptions = []

    @merchant_subscription_id ||= session[:merchant_subscription_id]
    @subscription_auth_code   ||= session[:subscription_auth_code]
    @consumer_id              ||= session[:consumer_id]

    read_recent_items(settings.subscriptions_file).map {|item| a,b,c=item.split; @subscriptions.push({:merchant_subscription_id => a, :subscription_id => b, :consumer_id => c}) }
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, SCOPE, settings.tokens_file)
  end
end

get '/' do
  erb :app2
end

post '/createSubscription' do
  create_subscription
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

post '/refreshNotifications' do
  refresh_notifications
end

# URL handlers go here

def create_subscription
  session[:merchant_subscription_id] = 'User' + sprintf('%03d', rand(1000)) + 'Subscription' + sprintf('%04d', rand(10000))

  # prepare payload
  data = {
    :Amount => params[:product] == '1' ? 1.99 : 3.99,
    :Category => 1,
    :Channel => 'MOBILE_WEB',
    :Description => 'Word game 1',
    :MerchantTransactionId => session[:merchant_subscription_id],
    :MerchantProductId => 'wordGame1',
    :MerchantPaymentRedirectUrl => settings.subscription_redirect_url,
    :MerchantSubscriptionIdList => 'sampleSubscription1',
    :IsPurchaseOnNoActiveSubscription => 'false',
    :SubscriptionRecurringNumber => '99999',
    :SubscriptionRecurringPeriod => 'MONTHLY',
    :SubscriptionRecurringPeriodAmount => 1
  }

  response = RestClient.post settings.notary_app_sign_url, :payload => data.to_json
  from_json = JSON.parse response

  redirect settings.FQDN + "/Commerce/Payment/Rest/2/Subscriptions?clientid=#{settings.api_key}&SignedPaymentDetail=#{from_json['signed_payload']}&Signature=#{from_json['signature']}"
end

def callback_subscription
  @subscription_auth_code = session[:subscription_auth_code] = params['SubscriptionAuthCode']

  @new_subscription = {
    :merchant_subscription_id => session[:merchant_subscription_id],
    :subscription_auth_code => @subscription_auth_code
  }  
  items = @subscriptions.unshift(@new_subscription).map {|s| s[:merchant_subscription_id] + " " + (s[:subscription_id] || "") + " " + (s[:consumer_id] || "")}
  write_recent_items(settings.subscriptions_file, settings.recent_subscriptions_stored, items);

rescue => e
  @create_error = true
ensure
  return erb :app2
end


def get_subscription_status
  u = settings.FQDN + "/Commerce/Payment/Rest/2/Subscriptions/SubscriptionAuthCode/#{@subscription_auth_code}?access_token=#{@access_token}"
  response = RestClient.get u

  @subscription_status = JSON.parse response
  @subscriptions.first[:subscription_id] = @subscription_status["SubscriptionId"]
  @subscriptions.first[:consumer_id] = @subscription_status["ConsumerId"]

  items = @subscriptions.map {|s| s[:merchant_subscription_id] + " " + (s[:subscription_id] || "") + " " + (s[:consumer_id] || "")}
  write_recent_items(settings.subscriptions_file, settings.recent_subscriptions_stored, items);

rescue => e
  @status_error = true
ensure
  return erb :app2
end


def get_subscription_details
  redirect '/' if params[:consumer_id].nil? || params[:consumer_id].empty?

  u = settings.FQDN + "/Commerce/Payment/Rest/2/Subscriptions/sampleSubscription1/Detail/#{params[:consumer_id]}?access_token=#{@access_token}"
  response = RestClient.get u

  @consumer_id = session[:consumer_id] = params[:consumer_id]
  @subscription_details = JSON.parse response

rescue => e
  @details_error = true
ensure
  return erb :app2
end


def refund_subscription
  redirect '/' if params[:subscription_id].nil? || params[:subscription_id].empty?

  u = settings.FQDN + "/Commerce/Payment/Rest/2/Transactions/#{params[:subscription_id]}?Action=refund&access_token=#{@access_token}"
  payload = { :RefundReasonCode => 1, :RefundReasonText => 'User did not like product'}
 
  RestClient.put u, payload.to_json, :content_type => 'application/json', :accept => 'application/json' do |response, request, code, &block|
    @refund_details = JSON.parse response
  end

rescue => e
  @refund_error = true
ensure
  return erb :app2
end


def refresh_notifications

rescue => e
  @refresh_error = true
ensure
  return erb :app2
end

