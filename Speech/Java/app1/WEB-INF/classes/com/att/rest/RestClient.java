package com.att.rest;

import java.util.Set;

import javax.ws.rs.core.MediaType;
import javax.ws.rs.core.MultivaluedMap;

import com.sun.jersey.api.client.Client;
import com.sun.jersey.api.client.ClientResponse;
import com.sun.jersey.api.client.WebResource;
import com.sun.jersey.api.client.config.ClientConfig;
import com.sun.jersey.api.client.config.DefaultClientConfig;
import com.sun.jersey.core.util.StringKeyIgnoreCaseMultivaluedMap;


public class RestClient 
{
	private String																	endpoint;
	private MultivaluedMap<String, String>					parameters;
	
	private MediaType															httpContentType;
	private MediaType															httpAcceptType;
	
	private Client																	client;
	private WebResource													service;
	
	private Object																	requestBody;
	private ClientResponse													clientResponse;
	private String																	responseBody;
	
	public RestClient(
			String endpoint,
			MediaType httpContentType,
			MediaType httpAcceptType) 
	{
		this.endpoint = endpoint;
		this.httpContentType = httpContentType;
		this.httpAcceptType = httpAcceptType;
		
		this.parameters = new StringKeyIgnoreCaseMultivaluedMap<String>();
	}
	
	public void addRequestBody(Object requestBody)
	{
		this.requestBody = requestBody;
	}

	public void addParameter(String key, String value)
	{
		if (key != null && key != "" && value != null && value != "")
			parameters.add(key, value);
	}
	
	public void clearParameters()
	{
		parameters.clear();
		
	}
	
	public String printResponse()
	{
		StringBuffer tmp = new StringBuffer();
		tmp.append("HTTP " + clientResponse.getStatus() + " " + clientResponse.getClientResponseStatus().toString() + "\n");
		
		MultivaluedMap<String, String> headers = clientResponse.getHeaders();
		Set<String> keys = headers.keySet();
		
		for (String key : keys)
			tmp.append(key + ": " + headers.get(key).get(0) + "\n");
		
		tmp.append(responseBody);
		
		return tmp.toString();
	}
	
	public int getHttpResponseCode()
	{
		return clientResponse.getStatus();
	}
	
	public String getResponseContent()
	{
		return responseBody;
	}
	
	public ClientResponse getClientResponseObject()
	{
		return clientResponse;
	}
	
	public void updateEndpoint(String endpoint)
	{
		this.endpoint = endpoint;
	}
	
	public void updateContentType(MediaType contentType)
	{
		httpContentType = contentType;
	}
	
	//Implements the POST method
	public String	invoke(HttpMethod method, boolean includeParameters)
	{	
		if (includeParameters && parameters.isEmpty())
		{
			try 
			{
				throw new Exception();
			} catch (Exception e) 
			{
				return "Exception: Parameters map is empty, please execute addParameter() method before calling post()";
			}
		}
		
		//Create a service object
		ClientConfig configuration = new DefaultClientConfig();
		configuration.getProperties().put(ClientConfig.PROPERTY_FOLLOW_REDIRECTS, true);
		//configuration.getProperties().put(ClientConfig.PROPERTY_CHUNKED_ENCODING_SIZE,0);
		client = Client.create(configuration);
		client.setFollowRedirects(true);
		service = client.resource(endpoint);
		
		switch (method)
		{
		case POST:
			if (!httpContentType.getType().equalsIgnoreCase("multipart")) {
				clientResponse = service.queryParams(parameters).type(httpContentType).accept(httpAcceptType).post(ClientResponse.class, requestBody);
			 }
			else
			{
				if (!httpContentType.getSubtype().equalsIgnoreCase("form-data")) {
					clientResponse = service.queryParams(parameters).header("Content-Type", "multipart/mixed; type=\"application/json\"; start=\"<startpart>\"; boundary=\"foo\"").accept(httpAcceptType).post(ClientResponse.class, requestBody);
				}
				else {
					clientResponse = service.queryParams(parameters).header("Content-Type", "multipart/form-encoded; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"foo\"").accept(httpAcceptType).post(ClientResponse.class, requestBody);
				}


			}
			break;
		case GET:
			clientResponse = service.queryParams(parameters).accept(httpAcceptType).get(ClientResponse.class);
			break;
		case PUT:
			break;
		case DELETE:
			break;
		case OPTIONS:
			break;
		}
		
		//System.out.println("request is:   " + client.getHeadHandler().toString());
		
		responseBody = clientResponse.getEntity(String.class);
		return responseBody;
	}
}
