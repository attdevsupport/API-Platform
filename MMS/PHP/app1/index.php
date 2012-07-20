<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");
include ("config.php");
include ($oauth_file);

session_start();

if (!empty($_REQUEST["sendMMS"])) {
  $_SESSION["mms1_address"] = $_POST['address'];
  $_SESSION["mms1_subject"] = $_POST['subject'];
 }


$mmsID=$_SESSION["mms1_mmsID"];
$subject=$_SESSION["mms1_subject"];
$address=$_SESSION["mms1_address"];



if($subject==null){
  $subject = $default_subject;
 }


function RefreshToken($FQDN,$api_key,$secret_key,$scope,$fullToken){

  $refreshToken=$fullToken["refreshToken"];
  $accessTok_Url = $FQDN."/oauth/token";

  //http header values
  $accessTok_headers = array(
			     'Content-Type: application/x-www-form-urlencoded'
			     );

  //Invoke the URL
  $post_data="client_id=".$api_key."&client_secret=".$secret_key."&refresh_token=".$refreshToken."&grant_type=refresh_token";

  $accessTok = curl_init();
  curl_setopt($accessTok, CURLOPT_URL, $accessTok_Url);
  curl_setopt($accessTok, CURLOPT_HTTPGET, 1);
  curl_setopt($accessTok, CURLOPT_HEADER, 0);
  curl_setopt($accessTok, CURLINFO_HEADER_OUT, 0);
  curl_setopt($accessTok, CURLOPT_HTTPHEADER, $accessTok_headers);
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
	      
    $refreshTime=$currentTime+(int)($expiresIn); // Time for token refresh
    $updateTime=$currentTime + ( 24*60*60); // Time to get for a new token update, current time + 24h 
	      
    $fullToken["accessToken"]=$accessToken;
    $fullToken["refreshToken"]=$refreshToken;
    $fullToken["refreshTime"]=$refreshTime;
    $fullToken["updateTime"]=$updateTime;
                        
  }
  else{
    $fullToken["accessToken"]=null;
    $fullToken["errorMessage"]=curl_error($accessTok).$accessTok_response;

			
  }
  curl_close ($accessTok);
  return $fullToken;
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
  curl_setopt($accessTok, CURLOPT_HTTPHEADER, $accessTok_headers);
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

      $refreshTime=$currentTime+(int)($expiresIn); // Time for token refresh
      $updateTime=$currentTime + ( 24*60*60); // Time to get for a new token update, current time + 24h

      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      
    }else{
 
    $fullToken["accessToken"]=null;
    $fullToken["errorMessage"]=curl_error($accessTok).$accessTok_response;

  }
  curl_close ($accessTok);
  return $fullToken;
}
function SaveToken( $fullToken,$oauth_file ){

  $accessToken=$fullToken["accessToken"];
  $refreshToken=$fullToken["refreshToken"];
  $refreshTime=$fullToken["refreshTime"];
  $updateTime=$fullToken["updateTime"];
      

  $tokenfile = $oauth_file;
  $fh = fopen($tokenfile, 'w');
  $tokenfile="<?php \$accessToken=\"".$accessToken."\"; \$refreshToken=\"".$refreshToken."\"; \$refreshTime=".$refreshTime."; \$updateTime=".$updateTime."; ?>";
  fwrite($fh,$tokenfile);
  fclose($fh);
}

function check_token( $FQDN,$api_key,$secret_key,$scope, $fullToken,$oauth_file){

  $currentTime=time();

  if ( ($fullToken["updateTime"] == null) || ($fullToken["updateTime"] <= $currentTime)){
    $fullToken=GetAccessToken($FQDN,$api_key,$secret_key,$scope);
    if(  $fullToken["accessToken"] == null ){
      //      echo $fullToken["errorMessage"];
    }else{
      //      echo $fullToken["accessToken"];
      SaveToken( $fullToken,$oauth_file );
    }
  }
  elseif ($fullToken["refreshTime"]<= $currentTime){
    $fullToken=RefreshToken($FQDN,$api_key,$secret_key,$scope, $fullToken);
    if(  $fullToken["accessToken"] == null ){
      //      echo $fullToken["errorMessage"];
    }else{
      //      echo $fullToken["accessToken"];
      SaveToken( $fullToken,$oauth_file );
    }
  }
  
  return $fullToken;
  
}

?>
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&amp;T Sample MMS Application 1 - Basic MMS Service Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="common.css"/ >
</head>

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

<h1>AT&T Sample MMS Application 1 - Basic MMS Service Application</h1>
<h2>Feature 1: Send MMS Message</h2>

</div>
</div>

<form name="sendMMS" method="post" enctype="multipart/form-data">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<?php echo $address; ?>" style="width: 90%">
    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Message:</td>
    <td class="cell"><textarea rows="4" name="subject" style="width: 90%"><?php echo $subject; ?></textarea>
	</td>
  </tr>
  </tbody>
</table>

</div>
<div id="extra">

<div class="warning">
<strong>WARNING:</strong><br />
total size of all attachments cannot exceed 600 KB.
</div>

<table border="0" width="100%">
  <tbody>
  <tr>
    <td valign="top" class="label">Attachment 1:</td>
    <td class="cell"><input name="file" type="file">
    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Attachment 2:</td>
    <td class="cell"><input name="f2" type="file">
    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Attachment 3:</td>
    <td class="cell"><input name="f3" type="file">
    </td>
  </tr>
  </tbody></table>
  <table>
  <tbody>
  <tr>
  	<td><button type="submit" name="sendMMS" value="Send MMS" >Send MMS Message</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />
<div align="center"></div>
</form>

<?php

    if (!empty($_REQUEST["sendMMS"])) {
if($address==null){?>
  	<div class="errorWide">
	<strong>ERROR: No number entered</strong><br />
	
	 
	
	</div><?php
 }else{


      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      
      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];

      $address =  str_replace("-","",$_POST['address']);
      $address =  str_replace("tel:","",$address);
      $address =  str_replace("+1","",$address);
      $address = "tel:" . $address;

      $subject = $_POST['subject'];


      $server=substr($FQDN,8);
      
      $fileContents = join("", file($_FILES['file']['tmp_name']));
       
      $host="ssl://$server";
      $port="443";
      $fp = fsockopen($host, $port, $errno, $errstr);

      if (!$fp) {
	echo "errno: $errno \n";
	echo "errstr: $errstr\n";
	return $result;
      }
      //Boundary for MIME part
      $boundary = "----------------------------".substr(md5(date("c")),0,10);
      
      //Form the first part of MIME body containing address, subject in urlencided format
      $sendMMSData = 'Address='.urlencode($address).'&Subject='.urlencode($subject);

      //Form the MIME part with MIME message headers and MIME attachment
      $data = "";
      $data .= "--$boundary\r\n";
      $data .= "Content-Type: application/x-www-form-urlencoded; charset=UTF-8\r\nContent-Transfer-Encoding: 8bit\r\nContent-Disposition: form-data; name=\"root-fields\"\r\nContent-ID: <startpart>\r\n\r\n".$sendMMSData."\r\n";
      foreach ( $_FILES as $file ){
	if ($file['name'] != ""){
	  $data .= "--$boundary\r\n";
	  $data .= "Content-Disposition: attachment; filename=\"".$file['name']."\"\r\n";
	  $data .= "Content-Type:".$file['type']."\r\n";
	  $data .= "Content-ID: <".$file['name'].">\r\n";
	  $data .= "Content-Transfer-Encoding: binary\r\n\r\n";
	  $data .= join("", file($file['tmp_name']))."\r\n";
	}
      }
      $data .= "--$boundary--\r\n";

      // Form the HTTP headers
      $header = "POST $FQDN/rest/mms/2/messaging/outbox? HTTP/1.0\r\n";
      $header .= "Authorization: BEARER ".$accessToken."\r\n"; 
      $header .= "Content-type: multipart/related; boundary=\"$boundary\"\r\n";
      $header .= "MIME-Version: 1.0\r\n";
      $header .= "Host: $server\r\n";
      $dc = strlen($data); //content length
      $header .= "Content-length: $dc\r\n\r\n";
      
      $httpRequest = $header.$data;
      fputs($fp, $httpRequest);
      
      $sendMMS_response="";
      while(!feof($fp)) {
	$sendMMS_response .= fread($fp,1024);
      }
      fclose($fp);
      $responseCode=trim(substr($sendMMS_response,9,4));//get the response code.

      /*
       If URL invocation is successful print the mms ID,
       else print the error msg
      */
      if($responseCode>=200 && $responseCode<=300)
	{
	  $splitString=explode("{",$sendMMS_response);
	  $joinString="{".implode("{",array($splitString[1],$splitString[2]));
	  $jsonObj = json_decode($joinString,true);
	  $msghead="Message Id";
	  $mmsID=$jsonObj['Id']; 
	  $_SESSION["mms1_mmsID"] = $mmsID;
	    ?>

	      <div class="success">
		 <strong>SUCCESS:</strong><br />
		 <strong>Message ID</strong> <?php echo $mmsID; ?>
		 </div>
	<?php } else {
	    
	    //print "The Request was Not Successful";
	    ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo $sendMMS_response  ?>
	    </div>

	<?php }
}}
?>
<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Delivery Status</h2>

</div>
</div>
<form name="getMmsDeliveryStatus" method="post">
 
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Message ID:</td>
    <td class="cell"><input maxlength="20" size="12" name="mmsID" value="<?php echo $mmsID; ?>" style="width: 90%">
    </td>
  </tr>
  </tbody></table>
  
</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="getMmsDeliveryStatus" value="Get Status">Get Status</button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
</form>

<?php

	/* Extract mms ID  from getMmsDeliveryStatus form
           and invoke the URL to get Mms Delivery Status along with access token
        */
    if (!empty($_REQUEST["getMmsDeliveryStatus"])) {
      
      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      
      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];


    	$mmsID=$_POST["mmsID"];
        $_SESSION["mms1_mmsID"] = $_POST['mmsID'];

	//For

        	$getMMSDelStatus_Url = "$FQDN/rest/mms/2/messaging/outbox/";
	$getMMSDelStatus_Url .= $mmsID;
	
        		$authorization = "Authorization: BEARER ".$accessToken; 
		$content = "Content-Type: application/xml";
		
		
	
		
	
 $getMMSDelStatus = curl_init();
  curl_setopt($getMMSDelStatus, CURLOPT_URL, $getMMSDelStatus_Url);
  curl_setopt($getMMSDelStatus, CURLOPT_HTTPGET, 1);
  curl_setopt($getMMSDelStatus, CURLOPT_HEADER, 0);
  curl_setopt($getMMSDelStatus, CURLINFO_HEADER_OUT, 1);
  curl_setopt($getMMSDelStatus, CURLOPT_HTTPHEADER, array($authorization, $content));
	curl_setopt($getMMSDelStatus, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($getMMSDelStatus, CURLOPT_SSL_VERIFYPEER, false);

	$getMMSDelStatus_response = curl_exec($getMMSDelStatus);
	$responseCode=curl_getinfo($getMMSDelStatus,CURLINFO_HTTP_CODE);

        /*
	  If URL invocation is successful print success msg along with mms delivery status,
	  else print the error msg
	*/

	if($responseCode==200)
	{
	    //decode the response and display the delivery status.
	  $jsonObj = json_decode($getMMSDelStatus_response,true);
	  $deliveryStatus=$jsonObj['DeliveryInfoList']['DeliveryInfo']['0']['DeliveryStatus'];
	  $resourceURL=$jsonObj['DeliveryInfoList']['ResourceUrl'];

	  ?>
	    <div class="successWide">
	       <strong>SUCCESS:</strong><br />
	       <strong>Status:</strong> <?php echo $deliveryStatus; ?><br />
               <strong>Resource URL:</strong><?php echo $resourceURL; ?>
            </div>
	<?php }
	else{
		$msghead="Error";
		$msgdata=curl_error($getMMSDelStatus);
		$errormsg = $msgdata.$getMMSDelStatus_response;
		?>
                <div class="errorWide">
                <strong>ERROR:</strong><br />
                <?php echo $errormsg  ?>
                </div>
	<?php }
	curl_close ($getMMSDelStatus);
    }
?>
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p>&#169;  2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>

</body>
</html>

