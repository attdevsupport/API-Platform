<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.'2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");
include ("config.php");
include ($oauth_file);
error_reporting(0);

    session_start();

if (!empty($_REQUEST["sendWAP"] )) {
  $_SESSION["wap1_address"] = $_POST['address'];
  $_SESSION["wap1_subject"] = $_POST['subject'];
  $_SESSION["wap1_url"] = $_POST['url'];
 }

//get the session variables.
$subject=$_SESSION["wap1_subject"];
$address=$_SESSION["wap1_address"];
$url=$_SESSION["wap1_url"];

if($url==null){
  $url =  $default_url;
 }
if($subject==null){
  $subject = $default_subject;
 }


function RefreshToken($FQDN,$api_key,$secret_key,$scope,$fullTocken){

  $refreshToken=$fullTocken["refreshToken"];
  $accessTok_Url = $FQDN."/oauth/token";

  //http header values
  $accessTok_headers = array(
			     'Content-Type: application/x-www-form-urlencoded'
			     );

  //Invoke the URL
  $post_data="client_id=".$api_key."&client_secret=".$secret_key."&refresh_token=".$refreshToken."&grant_type=refresh_token";
$proxy = "http://proxy.entp.attws.com";
  $accessTok = curl_init();
  curl_setopt($accessTok, CURLOPT_URL, $accessTok_Url);
  curl_setopt($accessTok, CURLOPT_HTTPGET, 1);
  curl_setopt($accessTok, CURLOPT_HEADER, 0);
  curl_setopt($accessTok, CURLINFO_HEADER_OUT, 0);
  //curl_setopt($accessTok, CURLOPT_HTTPHEADER, $accessTok_headers);
  curl_setopt($accessTok, CURLOPT_RETURNTRANSFER, 1);
  curl_setopt($accessTok, CURLOPT_SSL_VERIFYPEER, false);
  curl_setopt($accessTok, CURLOPT_POST, 1);
  curl_setopt($accessTok, CURLOPT_POSTFIELDS,$post_data);

  $accessTok_response = curl_exec($accessTok);
  $currentTime=time();

  $responseCode=curl_getinfo($accessTok,CURLINFO_HTTP_CODE);
  if($responseCode==200){
    $jsonObj = json_decode($accessTok_response);
    $accessToken = $jsonObj->{'access_token'};//fetch the access token from the response.
    $refreshToken = $jsonObj->{'refresh_token'};
    $expiresIn = $jsonObj->{'expires_in'};

     if($expiresIn == 0) {
	  $expiresIn = 24*60*60;
	
	  }
	      
    $refreshTime=$currentTime+(int)($expiresIn); // Time for tocken refresh
    $updateTime=$currentTime + ( 24*60*60); // Time to get for a new tocken update, current time + 24h 
	      
    $fullTocken["accessToken"]=$accessToken;
    $fullTocken["refreshToken"]=$refreshToken;
    $fullTocken["refreshTime"]=$refreshTime;
    $fullTocken["updateTime"]=$updateTime;
                        
  }
  else{
    $fullTocken["accessToken"]=null;
    $fullTocken["errorMessage"]=curl_error($accessTok).$accessTok_response;

			
  }
  curl_close ($accessTok);
  return $fullTocken;

}
function GetAccessToken($FQDN,$api_key,$secret_key,$scope){

  $accessTok_Url = $FQDN."/oauth/token";
	    
  //http header values
  $accessTok_headers = array(
			     'Content-Type: application/x-www-form-urlencoded'
			     );

  //Invoke the URL
  $post_data = "client_id=".$api_key."&client_secret=".$secret_key."&scope=".$scope."&grant_type=client_credentials";

  $accessTok = curl_init();
  curl_setopt($accessTok, CURLOPT_URL, $accessTok_Url);
  curl_setopt($accessTok, CURLOPT_HTTPGET, 1);
  curl_setopt($accessTok, CURLOPT_HEADER, 0);
  curl_setopt($accessTok, CURLINFO_HEADER_OUT, 0);
  //  curl_setopt($accessTok, CURLOPT_HTTPHEADER, $accessTok_headers);
  curl_setopt($accessTok, CURLOPT_RETURNTRANSFER, 1);
  curl_setopt($accessTok, CURLOPT_SSL_VERIFYPEER, false);
  curl_setopt($accessTok, CURLOPT_POST, 1);
  curl_setopt($accessTok, CURLOPT_POSTFIELDS,$post_data);
  $accessTok_response = curl_exec($accessTok);
  
  $responseCode=curl_getinfo($accessTok,CURLINFO_HTTP_CODE);
  $currentTime=time();
  /*
   If URL invocation is successful fetch the access token and store it in session,
   else display the error.
  */
  if($responseCode==200)
    {
      $jsonObj = json_decode($accessTok_response);
      $accessToken = $jsonObj->{'access_token'};//fetch the access token from the response.
      $refreshToken = $jsonObj->{'refresh_token'};
      $expiresIn = $jsonObj->{'expires_in'};

       if($expiresIn == 0) {
	  $expiresIn = 24*60*60*365*100;
	 
	  }

      $refreshTime=$currentTime+(int)($expiresIn); // Time for tocken refresh
      $updateTime=$currentTime + ( 24*60*60); // Time to get for a new tocken update, current time + 24h

      $fullTocken["accessToken"]=$accessToken;
      $fullTocken["refreshToken"]=$refreshToken;
      $fullTocken["refreshTime"]=$refreshTime;
      $fullTocken["updateTime"]=$updateTime;
      
    }else{
 
    $fullTocken["accessToken"]=null;
    $fullTocken["errorMessage"]=curl_error($accessTok).$accessTok_response;

  }
  curl_close ($accessTok);
  return $fullTocken;
}
function SaveToken( $fullTocken,$oauth_file ){

  $accessToken=$fullTocken["accessToken"];
  $refreshToken=$fullTocken["refreshToken"];
  $refreshTime=$fullTocken["refreshTime"];
  $updateTime=$fullTocken["updateTime"];
      

  $tockenfile = $oauth_file;
  $fh = fopen($tockenfile, 'w');
  $tockenfile="<?php \$accessToken=\"".$accessToken."\"; \$refreshToken=\"".$refreshToken."\"; \$refreshTime=".$refreshTime."; \$updateTime=".$updateTime."; ?>";
  fwrite($fh,$tockenfile);
  fclose($fh);
}

function check_tocken( $FQDN,$api_key,$secret_key,$scope, $fullTocken,$oauth_file){

  $currentTime=time();

  if ( ($fullTocken["updateTime"] == null) || ($fullTocken["updateTime"] <= $currentTime)){
    $fullTocken=GetAccessToken($FQDN,$api_key,$secret_key,$scope);
    if(  $fullTocken["accessToken"] == null ){
      //      echo $fullTocken["errorMessage"];
    }else{
      //      echo $fullTocken["accessToken"];
      SaveToken( $fullTocken,$oauth_file );
    }
  }
  elseif ($fullTocken["refreshTime"]<= $currentTime){
    $fullTocken=RefreshToken($FQDN,$api_key,$secret_key,$scope, $fullTocken);
    if(  $fullTocken["accessToken"] == null ){
      //      echo $fullTocken["errorMessage"];
    }else{
      //      echo $fullTocken["accessToken"];
      SaveToken( $fullTocken,$oauth_file );
    }
  }
  
  return $fullTocken;
  
}

?>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample Application - WAPPush</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="common.css"/ >

<body>
<div id="container">
<!-- open HEADER --><div id="header">
<div>
    <div id="hcRight">
      <?php echo  date("D M j G:i:s T Y"); ?>
    </div>
    <div id="hcLeft">Server Time:</div>
</div>
<div>
    <div id="hcRight"><script language="JavaScript" type="text/javascript">
var myDate = new Date();
document.write(myDate);
</script></div>
    <div id="hcLeft">Client Time:</div>
</div>
<div>
    <div id="hcRight"><script language="JavaScript" type="text/javascript">
document.write("" + navigator.userAgent);
</script></div>
    <div id="hcLeft">User Agent:</div>
</div>
<br clear="all" />
</div><!-- close HEADER -->

<div id="wrapper">
<div id="content">

<h1>AT&T Sample Application - WAPPush</h1>
<h2>Feature 1: Send basic WAP message</h2>

</div>
</div>

<form method="post" name="sendWAP" enctype="multipart/form-data" >
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<?php echo $address; ?>" style="width: 90%">
    </td>
  </tr>
  <tr>
    <td width="20%" valign="top" class="label">URL:</td>
    <td class="cell"><input  size="12" name="url" value="<?php echo $url; ?>" style="width: 90%">
    </td>
  </tr>
  <tr>
  	<td valign="top" class="label">Service Type</td>
    <td valign="top" class="cell">Service Indication <input type="radio" name="" value="" checked /> Service Loading<input type="radio" name="" value="" disabled />  </td>
  </tr>
  </tbody></table>

<div class="warning">
<strong>WARNING:</strong><br />
At this time, AT&T only supports Service Type: Service Indication due to security concerns.
</div>

</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Alert Text:</td>
    <td class="cell"><textarea rows="4" name="subject" style="width: 90%"><?php echo $subject; ?></textarea></td>
  </tr>
  </tbody></table>
  <table>
  <tbody>
  <tr>
  	<td><button type="submit" name="sendWAP" value="sendWAP" >Send WAP Message</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />
<div align="center"></div>
</form>

<?php
//if the user submitted the sendWAP form, then get the values and try to send the WAP PUSH message.  
	
    if (!empty($_REQUEST["sendWAP"] )) {


if( $_SESSION["wap1_address"] == null){?>
  	<div class="errorWide">
	<strong>ERROR:</strong><br> No number entered. <br />
	
	 
	
	</div><?php
 }else{

      $fullTocken["accessToken"]=$accessToken;
      $fullTocken["refreshToken"]=$refreshToken;
      $fullTocken["refreshTime"]=$refreshTime;
      $fullTocken["updateTime"]=$updateTime;
      
      $fullTocken=check_tocken($FQDN,$api_key,$secret_key,$scope,$fullTocken,$oauth_file);
      $accessToken=$fullTocken["accessToken"];

      

     
	$address =  str_replace("-","",$_POST['address']);
      $address =  str_replace("tel:","",$address);
      $address = "tel:" . $address;
      
      $subject = $_POST['subject'];
      $url = $_POST['url'];
      $server=substr($FQDN,8);
      
      $host="ssl://$server";//target host name
      $port="443";//port number
      //try to connect the socket  
      $fp = fsockopen($host, $port, $errno, $errstr, 1000); 
      //checking the connection ,if it fails display the error 
      if (!$fp) { 
	echo "errno: $errno \n"; 
	echo "errstr: $errstr\n"; 
	//return $result; 
      }
      
      
      //Prepare wap message attachment 
      $wap_message="Content-Disposition: form-data; name=\"PushContent\"\n";
      $wap_message .= "Content-Type: text/vnd.wap.si\n";
      $wap_message .= "Content-Length: 20\n";
      $wap_message .= "X-Wap-Application-Id: x-wap-application:wml.ua\n\n";
      $wap_message .= "<?xml version=\"1.0\"?>\n";
      $wap_message .= "<!DOCTYPE si PUBLIC \"-//WAPFORUM//DTD SI 1.0//EN\" \"http://www.wapforum.org/DTD/si.dtd\">\n";
      $wap_message .= "<si>\n";
      $wap_message .= "<indication href=\"".$url."\" action=\"signal-medium\" si-id=\"6532\" >\n";
      $wap_message .= $subject."\n";
      $wap_message .= "</indication>\n";
      $wap_message .= "</si>\n";
		
   
      //WAPPUSH data
      $boundary = "--0246824681357ACXZabcxyz";
      $sendWAP_JSonData = 'address='.urlencode($address).'&subject='.urlencode($subject).'&content-type='.urlencode("application/xml");
      
      $data = "";
      $data .= "--$boundary\r\n";
      $data .= "Content-type: application/x-www-form-urlencoded; charset=UTF-8\r\n";
      $data .= "Content-Transfer-Encoding: 8bit\r\n";
      $data .= "Content-ID: <startpart>\r\n";
      $data .= "Content-Disposition: form-data; name=\"root-fields\"\r\n\r\n".$sendWAP_JSonData."\r\n"; 
      $data .= "--$boundary\r\n";
      $data .= "Content-Disposition: attachment; name=\"\"\r\n\r\n";
      $data .= "Content-Type: text/plain\r\n";
      $data .= "Content-ID: <PushContent.txt>\r\n";
      $data .= "Content-Transfer-Encoding: binary\r\n";
      $data .= $wap_message."\r\n"; 
      $data .= "--$boundary--\r\n"; 
      //http header values
      $header = "POST $FQDN/1/messages/outbox/wapPush? HTTP/1.0\r\n";
      $header .= "Host: $server\r\n";
	  $header .= "Authorization:Bearer ".$accessToken."\r\n";
      $header .= "MIME-Version: 1.0\r\n";
      $dc = strlen($data); //content length
      $header .= "Content-Type: multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"\"; boundary=\"$boundary\"\r\n";
      $header .= "Content-length: $dc\r\n\r\n";

      $httpRequest = $header.$data;
      fputs($fp, $httpRequest);
      //read the response from file and store it.
      $res="";
      while(!feof($fp)) { 
	$res .= fread($fp,1024); 
      } 
      fclose($fp); //close the socket

      $responseCode=trim(substr($res,9,4));//get the response code.
      //if the request is successful, get the id.else display the error.
      if($responseCode>=200 && $responseCode<=300)
	{
	  $splitString=explode("\r\n\r\n",$res);
	  $jsonObj = json_decode($splitString[1],true);
	  $msghead="Message Id";
	  $msgdata=$jsonObj['id']; ?>
	    <div class="successWide">
	       <strong>SUCCESS:</strong><br />
	       <strong>Message ID:</strong> <?php echo $msgdata; ?>
	       </div>
			
		   <?php } else {

	$splitString=explode("\r\n\r\n",$res);
	$errormsg= $splitString[1];

	?>
	<div class="errorWide">
	<strong>ERROR:</strong><br />
	<?php echo $errormsg ?>
	</div>
	<?php 
      }
    }
}?>

<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p> &#169;  2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>

</body></html>


