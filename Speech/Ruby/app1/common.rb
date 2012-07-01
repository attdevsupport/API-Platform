
# Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
# TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
# Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
# For more information contact developer.support@att.com

def obtain_tokens(fqdn, client_id, client_secret, scope, tokens_file)
  read_tokens(tokens_file)
  
  response = RestClient.post "#{fqdn}/oauth/access_token", :grant_type => 'client_credentials', :client_id => client_id, :client_secret => client_secret, :scope => scope
	
  from_json = JSON.parse(response.to_str)
  @access_token = from_json['access_token']
  @refresh_token = from_json['refresh_token']
  write_tokens(tokens_file)
end

def write_tokens(tokens_file)
  File.open(tokens_file, 'w+') { |f| f.puts @access_token, @refresh_token }
end

def read_tokens(tokens_file)
  @access_token, @refresh_token, refresh_expiration = File.foreach(tokens_file).first(2).map! &:strip!
rescue
  return
end

