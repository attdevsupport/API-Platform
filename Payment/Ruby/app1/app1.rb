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

  u = settings.FQDN + "/Commerce/Payment/Rest/2/Transactions?clientid=" + settings.api_key + "&SignedPaymentDetail=" + from_json['signed_payload'] + "&Signature=" + from_json['signature']

  redirect u
end

def return_transaction
  @new_transaction = Hash.new

  @new_transaction[:merchant_transaction_id] = session[:merchant_transaction_id]
  @new_transaction[:transaction_auth_code] = params['TransactionAuthCode']
  @transactions.push @new_transaction
  
  @transactions.delete_at 0 if @transactions.length > settings.recent_transactions_stored
  write_recent_transactions

ensure
  return erb :app1
end

def get_transaction_status
  if params['getTransactionType'] == '1'
    u = settings.FQDN + "/Commerce/Payment/Rest/2/Transactions/MerchantTransactionId/" + @transactions.last[:merchant_transaction_id] + "?access_token=" + @access_token
  elsif params['getTransactionType'] == '2'
    u = settings.FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + @transactions.last[:transaction_auth_code] + "?access_token=" + @access_token
  elsif params['getTransactionType'] == '3'
    u = settings.FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionId/" + @transactions.last[:transaction_id] + "?access_token=" + @access_token
  end

  RestClient.get u do |response, request, code, &block|
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

  u = settings.FQDN + "/Commerce/Payment/Rest/2/Transactions/" + params['trxId'] + "?access_token=" + @access_token + "&Action=refund"

  payload = Hash.new
  payload['RefundReasonCode'] = 1
  payload['RefundReasonText'] = 'User did not like product'

  RestClient.put u, payload.to_json, :content_type => 'application/json', :accept => 'application/json' do |response, request, code, &block|
    @r = response
  end

  if @r.code == 200
    @refund = Hash.new
    @refund[:transaction_id] = params['trxId']
  else
    @refund_error = @r
  end
  erb :app1
end

#def refresh_notifications
#  # make the API call
#
#ensure
#  return erb :app1
#end

['/','/newTransaction','/getTransactionStatus','/refundTransaction', '/refreshNotifications', '/returnTransaction'].each do |path|
  before path do
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
  # validate parameters
  get_transaction_status
end

post '/refundTransaction'  do
  # validate parameters
  refund_transaction
end

post '/refreshNotifications'  do
  # validate parameters
  refresh_notifications
end
