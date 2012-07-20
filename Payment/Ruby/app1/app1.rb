
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

#!/usr/bin/ruby
require 'rubygems'
require 'json'
require 'uri'
require 'rest_client'
require 'base64'
require 'sinatra'
require 'sinatra/config_file'
require File.join(File.dirname(__FILE__), 'common.rb')

enable :sessions

config_file 'config.yml'

set :port, settings.port

Scope = 'PAYMENT'

def read_recent_notifications
  @notifications = Array.new
  File.open settings.notificationdetails_file, 'r' do |f| 
    while (line = f.gets)
      a = line.split
      n = Hash.new
      n[:original_trxId] = a[0]
      n[:notificationtype] = a[1] 
      n[:notificationId] = a[2]
      @notifications.push n
    end 
  end
  @notifications = @notifications.drop(@notifications.length - settings.recent_notifications_stored) if @notifications.length > settings.recent_notifications_stored
rescue
  return
end


def read_recent_transactions
  @transactions = Array.new
  File.open settings.transactions_file, 'r' do |f| 
    while (line = f.gets)
      a = line.split
      t = Hash.new
      t[:merchant_transaction_id] = a[0]
      t[:transaction_auth_code] = a[1] 
      t[:transaction_id] = a[2]
      @transactions.push t
    end 
  end
  @transactions = @transactions.drop(@transactions.length - settings.recent_transactions_stored) if @transactions.length > settings.recent_transactions_stored
rescue
  return
end

def write_recent_transactions
  File.open settings.transactions_file, 'w+' do |f|
    @transactions.each do |t|
      f.puts t[:merchant_transaction_id] + ' ' + t[:transaction_auth_code] + (t[:transaction_id] ? ' ' + t[:transaction_id] : '')
    end
  end
end


def new_transaction
  session[:merchant_transaction_id] = 'User' + sprintf('%03d', rand(1000)) + 'Transaction' + sprintf('%04d', rand(10000))
  
  # prepare payload
  data = {
    :Amount => params[:product] == "1" ? 0.99 : 2.99,
    :Category => 1,
    :Channel => 'MOBILE_WEB',
    :Description => 'Word game 1',
    :MerchantTransactionId => session[:merchant_transaction_id],
    :MerchantProductId => 'wordGame1',
    :MerchantApplicationId => 'wordGames',
    :MerchantPaymentRedirectUrl => settings.payment_redirect_url
  }
  
  response = RestClient.post settings.notary_app_sign_url, :payload => data.to_json
  from_json = JSON.parse response
 
  u = settings.FQDN + "/rest/3/Commerce/Payment/Transactions?Signature=#{from_json['signature']}&SignedPaymentDetail=#{from_json['signed_payload']}&clientid=#{settings.api_key}"

  redirect u
end

def return_transaction
  @new_transaction = Hash.new

  @new_transaction[:merchant_transaction_id] = session[:merchant_transaction_id]
  @new_transaction[:transaction_auth_code] = params['TransactionAuthCode']
  params['TransactionAuthCode'] = session[:transaction_auth_code]
  @transactions.push @new_transaction
  
  @transactions.delete_at 0 if @transactions.length > settings.recent_transactions_stored
  write_recent_transactions

ensure
  return erb :app1
end

def get_transaction_status
  if params['getTransactionType'] == '1'
    url = settings.FQDN + "/rest/3/Commerce/Payment/Transactions/MerchantTransactionId/" + @transactions.last[:merchant_transaction_id]
  elsif params['getTransactionType'] == '2'
    url = settings.FQDN + "/rest/3/Commerce/Payment/Transactions/TransactionAuthCode/" + @transactions.last[:transaction_auth_code]
  elsif params['getTransactionType'] == '3'
    url = settings.FQDN + "/rest/3/Commerce/Payment/Transactions/TransactionId/" + @transactions.last[:transaction_id]
  end

  RestClient.get url, :Authorization => "Bearer #{@access_token}", :Content_Type => 'application/json', :Accept => 'application/json' do |response, request, code, &block|
    @r = response
  end

  if @r.code == 200
    @transaction_status = @transactions.last
    @transaction_status[:status] = JSON.parse @r

    @transactions.last[:transaction_id] = @transaction_status[:status]['TransactionId']

    write_recent_transactions
  else
    @transaction_status_error = @r
  end

  erb :app1
end

def refund_transaction
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
    @transaction_refund = @transactions.last
    @transaction_refund[:status] = JSON.parse @r
    @refund = Hash.new
    @refund[:transaction_id] = params['trxId']
  else
    @refund_error = @r
  end
  erb :app1
end

def refresh_notifications
  File.open settings.notifications_file, 'r' do |f| 
    while (line = f.gets)
 
      url = settings.FQDN + "/rest/3/Commerce/Payment/Notifications/" + line 
      
      RestClient.get url, :Authorization => "Bearer #{@access_token}", :Content_Type => 'application/json', :Accept => 'application/json' do |response, request, code, &block|
        @r = response
      end
    
 
  if @r.code == 200
  from_json = JSON.parse @r
    originaltrxId = from_json['GetNotificationResponse']['OriginalTransactionId']
    notificationtype = from_json['GetNotificationResponse']['NotificationType']
    input = line + ' ' + originaltrxId +' ' + notificationtype

  
    
  File.open("#{settings.notifications_file_dir}/notificationdetails", 'a+') { |f| f.puts input}
  
  acknowledge_notifications 
  end
  end
 end
 read_recent_notifications
 erb :app1
end
 
def acknowledge_notifications
 read_recent_notifications
 File.open settings.notifications_file, 'r' do |f| 
    while (line = f.gets)
  
    url = settings.FQDN + "/rest/3/Commerce/Payment/Notifications/" + line 
  
    
    payload = ''
    RestClient.put url, payload, :Authorization => "Bearer #{@access_token}", :Content_Type => 'application/json', :Accept => 'application/json' do |response, request, code, &block|
    
    @r2 = response
  
  end
  if @r2.code == 200
   
  end
  end
  end
ensure
  return erb :app1
  read_recent_notifications
end

def transaction_listener
  # make the API call
  input   = request.env["rack.input"].read
  notificationId = /\<hub:notificationId\>(.*?)<\/hub:notificationId>/.match(input)[1]
  random  = rand(10000000).to_s

  File.open("#{settings.notifications_file_dir}/notifications", 'a+') { |f| f.puts notificationId }
 
ensure
  return erb :app1
end

['/', '/newTransaction', '/getTransactionStatus', '/refundTransaction', '/refreshNotifications', '/acknowledgeNotifications', '/returnTransaction'].each do |path|
  before path do
  read_recent_notifications
    read_recent_transactions
    obtain_tokens(settings.FQDN, settings.api_key, settings.secret_key, Scope, settings.tokens_file)
  end
end

get '/' do
  erb :app1
end

get '/returnTransaction' do
  return_transaction
end

post '/newTransaction'  do
  new_transaction
end

post '/getTransactionStatus'  do
  get_transaction_status
end

post '/refundTransaction'  do
  refund_transaction
end

post '/transactionListener'  do
  transaction_listener
end

post '/refreshNotifications' do
  refresh_notifications
end

get '/acknowledgeNotifications' do
  acknowledge_notifications
end

