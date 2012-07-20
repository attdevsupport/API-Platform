
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012 
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com 
# For more information contact developer.support@att.com

# Tries to parse supplied address using one of known formats. Returns false on failure.
def parse_address(address)
  address.strip!
  if (address.match('^\d{10}$'))
    
  elsif (m = address.match('^1(\d{10})$'))
    address = m[1].to_s
  elsif (m = address.match('^\+1(\d{10})$'))
    address = m[1].to_s
  elsif (m = address.match('^tel:(\d{10})$'))
    address = m[1].to_s
  elsif (address.match('^\d{3}-\d{3}-\d{4}$'))
    address.gsub! '-', ''
  else
    return false
  end

  address
end


# Makes sure that valid access_token is stored in the session. Retrieves new tokens if needed.
def obtain_tokens(fqdn, client_id, client_secret, scope, tokens_file)
  read_tokens(tokens_file)
  if !@access_token.nil? and @access_token_expires > Time.now
    return
  elsif !@refresh_token.nil? and @refresh_token_expires > Time.now
    url = "#{fqdn}/oauth/token?grant_type=refresh_token&client_id=" + client_id + "&client_secret=" + client_secret + "&refresh_token=" + @refresh_token
  else
    url = "#{fqdn}/oauth/token?grant_type=client_credentials&client_id=" + client_id + "&client_secret=" + client_secret + "&scope=" + scope
  end

  response = RestClient.get url
  from_json = JSON.parse(response.to_str)
  @access_token = from_json['access_token']
  @refresh_token = from_json['refresh_token']
  @access_token_expires = Time.now + (from_json['expires_in'].to_i)/1000
  @refresh_token_expires = Time.now + (60*60*24)
  write_tokens(tokens_file)
end

def write_tokens(tokens_file)
  File.open(tokens_file, 'w+') { |f| f.puts @access_token, @access_token_expires, @refresh_token, @refresh_token_expires }
end

def read_tokens(tokens_file)
  @access_token, access_expiration, @refresh_token, refresh_expiration = File.foreach(tokens_file).first(4).map! &:strip!
  @access_token_expires  = Time.parse access_expiration
  @refresh_token_expires = Time.parse refresh_expiration
rescue
  return
end

# general helper functions to read and write recently generated items

def read_recent_items file_name
  items = []
  File.open file_name, 'r' do |f| 
    while line = f.gets
      items.push line
    end 
  end
ensure
  return items
end

def write_recent_items file_name, number, items
  items = items.first(number-1)
  File.open file_name, 'w+' do |f|
    items.each do |t|
      f.puts t
    end
  end
end
