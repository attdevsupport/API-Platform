
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012 
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com 
# For more information contact developer.support@att.com

#!/usr/bin/ruby
require 'rubygems'
require 'sinatra'
require 'sinatra/config_file'
require 'json'
require 'rest_client'
require 'base64'

enable :sessions

config_file 'config.yml'

set :port, settings.port

get '/' do
  display_images
end

post '/mmslistener' do
  mms_listener
end

get '/getImageData' do
  get_image_data
end

def display_images
  response = RestClient.get  settings.images_url + '/getImageData'
  json = JSON.parse response

  @images_total = json['totalNumberOfImagesSent']
  @images_list = json['imageList']

  erb :mms3
end

def mms_listener
  input   = request.env["rack.input"].read
  address = /\<SenderAddress\>tel:([0-9\+]+)<\/SenderAddress>/.match(input)[1]
  parts   = input.split "--Nokia-mm-messageHandler-BoUnDaRy"
  body    = parts[2].split "BASE64"
  type    = /Content\-Type: image\/([^;]+)/.match(body[0])[1];
  date    = Time.now.utc

  random  = rand(10000000).to_s

  File.open("#{settings.momms_image_dir}/#{random}.#{type}", 'w') { |f| f.puts Base64.decode64 body[1] }

  # TODO: tokenizer stuff

  text = parts.length > 4 ? Base64.decode64(parts[3].split("BASE64")[1]).strip : ""
  File.open("#{settings.momms_data_dir}/#{random}.#{type}.txt", 'w') { |f| f.puts address, date, text } 
end

def get_image_data
  content_type :json
  images = []

  Dir.glob(settings.momms_image_dir+"/*").each do |entry|
    if File.file? entry
      data = entry.sub(settings.momms_image_dir, settings.momms_data_dir)+".txt";
      if File.exists? data
        File.open(data, "r") { |f| images.push( {:path => entry.sub('public/',''), :senderAddress => f.gets.strip, :date => f.gets.strip, :text => f.gets.strip} ) }
      end
    end
  end
  { :totalNumberOfImagesSent => images.length, :imageList => images }.to_json
end

