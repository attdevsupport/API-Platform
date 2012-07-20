<!--Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
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

if (!empty($_REQUEST["sendSms"])) {
  $_SESSION["sms1_smsMsg"] = $_POST['message'];
  $_SESSION["sms1_address"] = $_POST['address'];
 }

$smsID=$_SESSION["sms1_smsID"];
$smsMsg=$_SESSION["sms1_smsMsg"];
$address=$_SESSION["sms1_address"];

if($address==null){
  $address = $default_address;
 }
if($smsMsg==null){
  $smsMsg = $default_smsMsg;
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
      $updateTime=$currentTime + ( 24*60*60); // Time to get a new token update, current time + 24h

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
<html xmlns="http://www.w3.org/1999/xhtml" lang="en" xml:lang="en">
<head>
<title>AT&amp;T Sample SMS Application - Basic SMS Service Application</title>
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

  <h1>AT&amp;T Sample SMS Application - Basic SMS Service Application</h1>
<h2>Feature 1: Send SMS</h2>

</div>
</div>

<form name="sendSms" method="post">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<?php echo $address; ?>" style="width: 90%" ></input>
    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Message:</td>
    <td class="cell"><textarea rows="4" name="message" style="width: 90%" ><?php echo $smsMsg; ?></textarea>
    </td></tr>
  </tbody></table>

</div>
<div id="extra">

  <table>
  <tbody>
  <tr>
  	<td><br /><br /><br /><br /><br /><button type="submit" name="sendSms" value="Send SMS Message">Send SMS Message</button></td>
  </tr>
  </tbody>
  </table>


</div>
<br clear="all" />
<div align="center"></div>

</form>

<?php
	/* Extract POST parmeters from send SMS form
	   and invoke he URL to send SMS along with access token
	*/
    if (!empty($_REQUEST["sendSms"])) {

      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      
      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];

      $smsMsg = $_POST['message'];
      $address =  str_replace("-","",$_POST['address']);
      $address =  str_replace("tel:","",$address);
      $address =  str_replace("+1","",$address);
      $address = "tel:" . $address;
	
	// Form the URL to send SMS 
      $sendSMS_RequestBody = '{"Address":"'.$address.'","Message":"'.$smsMsg.'"}';//post data
      $sendSMS_Url = "$FQDN/rest/sms/2/messaging/outbox?";
	  $authorization = 'Authorization: Bearer '.$accessToken;
	 $content = "Content-Type: application/json";
      

	//Invoke the URL
	$sendSMS = curl_init();
	
	curl_setopt($sendSMS, CURLOPT_URL, $sendSMS_Url);
	curl_setopt($sendSMS, CURLOPT_POST, 1);
	curl_setopt($sendSMS, CURLOPT_HEADER, 0);
	curl_setopt($sendSMS, CURLINFO_HEADER_OUT, 0);
	curl_setopt($sendSMS, CURLOPT_HTTPHEADER, array($authorization, $content));
	curl_setopt($sendSMS, CURLOPT_POSTFIELDS, $sendSMS_RequestBody);
	curl_setopt($sendSMS, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($sendSMS, CURLOPT_SSL_VERIFYPEER, false);
	
	
	$sendSMS_response = curl_exec($sendSMS);
	
	$responseCode=curl_getinfo($sendSMS,CURLINFO_HTTP_CODE);

        /*
	  If URL invocation is successful print success msg along with sms ID,
	  else print the error msg
	*/
	if($responseCode==200 || $responseCode ==201 || $responseCode==300)
	{
		$jsonObj = json_decode($sendSMS_response);
		$smsID = $jsonObj->{'Id'};//if the SMS send successfully ,then will get a SMS id.
		$_SESSION["sms1_smsID"] = $smsID;
        	$msghead="Message Id";
		$msgdata=$smsID; ?>

		<div class="success">
                <strong>SUCCESS:</strong><br />
               <strong> Message ID</strong> <?php echo $msgdata; ?>
                </div>

	<?php } 
	else{
		$msghead="Error";
		$msgdata=curl_error($sendSMS);
		$errormsg= $msgdata.$sendSMS_response;
		?>

                <div class="errorWide">
                <strong>ERROR:</strong><br />
			
                <?php echo $errormsg  ?>
                </div>

	<?php }
	curl_close ($sendSMS);
}
?>

<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Delivery Status</h2>

</div>
</div>
<form name="getSmsDeliveryStatus" method="post">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Message ID:</td>
    <td class="cell"><input maxlength="20" size="12" name="id" value="<?php echo $smsID; ?>" style="width: 90%">
    </td>
  </tr>
  </tbody></table>
  
</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="getSmsDeliveryStatus" value="Get Status" >Get Status</button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />

</form>

<?php
        /* Extract sms ID  from getSmsDeliveryStatus form
           and invoke the URL to get Sms Delivery Status along with access token
        */
   if (!empty($_REQUEST["getSmsDeliveryStatus"] )) {

     $fullToken["accessToken"]=$accessToken;
     $fullToken["refreshToken"]=$refreshToken;
     $fullToken["refreshTime"]=$refreshTime;
     $fullToken["updateTime"]=$updateTime;
     
     $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
     $accessToken=$fullToken["accessToken"];

      $smsID = $_POST['id'];
      $_SESSION["sms1_smsID"] = $_POST['id'];
	// Form the URL to get Sms Delivery Status
      $getSMSDelStatus_Url = "$FQDN/rest/sms/2/messaging/outbox/";
      $getSMSDelStatus_Url .= $smsID;
    
	 $authorization = 'Authorization: Bearer '.$accessToken;
	 $content = "Content-Type: application/json";
	
	//Invoke the URL
	$getSMSDelStatus = curl_init();	
	curl_setopt($getSMSDelStatus, CURLOPT_URL, $getSMSDelStatus_Url);
	curl_setopt($getSMSDelStatus, CURLOPT_HTTPGET, 1);
	curl_setopt($getSMSDelStatus, CURLOPT_HEADER, 0);
	curl_setopt($getSMSDelStatus, CURLINFO_HEADER_OUT, 0);
	curl_setopt($getSMSDelStatus, CURLOPT_HTTPHEADER, array($authorization, $content));
	curl_setopt($getSMSDelStatus, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($getSMSDelStatus, CURLOPT_SSL_VERIFYPEER, false);
	

	$getSMSDelStatus_response = curl_exec($getSMSDelStatus);
	$responseCode=curl_getinfo($getSMSDelStatus,CURLINFO_HTTP_CODE);

        /*
	  If URL invocation is successful print success msg along with sms delivery status,
	  else print the error msg
	*/
	if($responseCode==200)
	{
		//decode the response and display it.
	  $jsonObj = json_decode($getSMSDelStatus_response,true);
	  $deliveryStatus=$jsonObj['DeliveryInfoList']['DeliveryInfo']['0']['DeliveryStatus'];
	  $resourceURL=$jsonObj['DeliveryInfoList']['ResourceUrl'];

	  ?>
	    <div class="successWide">
	       <strong>SUCCESS:</strong><br />
	       <strong>Status:</strong><?php echo $deliveryStatus; ?><br />
               <strong>Resource URL:</strong><?php echo $resourceURL; ?>
            </div>
          <?php
	}
	else{
	  $msghead="Error";
	  $msgdata=curl_error($getSMSDelStatus);
	  $errormsg=$msgdata.$getSMSDelStatus_response;
	  ?>
		<div class="errorWide">
                <strong>ERROR:</strong><br />
                <?php  echo $errormsg ;  ?>
                </div>

		
	<?php }
	curl_close ($getSMSDelStatus);
	
}
?>
<div id="wrapper">
<div id="content">

<h2><br />Feature 3: Get Received Messages</h2>

</div>
</div>

<form name="receiveSms" method="post">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell">
     <button type="submit" name="receiveSms" value="<?php echo $short_code; ?>">Get Messages for Short Code <?php echo $short_code ?></button>
     <button type="submit" name="receiveSms" value="<?php echo $short_code2; ?>">Get Messages for Short Code <?php echo $short_code2 ?></button>
    </td>
  </tr>
  </tbody>
</table>

</div>
<br clear="all" />
</form>

<?php
        /*
	  If Receive SMS request is submitted, then invoke the URL to get the inbox messages
	  by using the registrationID i.e. short code, along with the access token.
	*/

    if (!empty($_REQUEST["receiveSms"] )) {

      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;

      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];

	// Form the URL for getting the inbox messages

      $shortCode = $_POST['receiveSms'];
      $receiveSMS_Url = "$FQDN/rest/sms/2/messaging/inbox?&RegistrationID=".$shortCode;
      
	//$receiveSMS_headers = 
	/*array(
	//	'Content-Type: application/x-www-form-urlencoded'
		'Accept: application/json'
	);*/
 $authorization = 'Authorization: Bearer '.$accessToken;
	 $content = "Content-Type: application/json";
	 
	//Invoke the URL
	$receiveSMS = curl_init();
	curl_setopt($receiveSMS, CURLOPT_URL, $receiveSMS_Url);
	curl_setopt($receiveSMS, CURLOPT_HTTPGET, 1);
	curl_setopt($receiveSMS, CURLOPT_HEADER, 0);
	curl_setopt($receiveSMS, CURLINFO_HEADER_OUT, 0);
	curl_setopt($receiveSMS, CURLOPT_HTTPHEADER, array($authorization, $content));
	curl_setopt($receiveSMS, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($receiveSMS, CURLOPT_SSL_VERIFYPEER, false);


	$receiveSMS_response = curl_exec($receiveSMS);
	$responseCode=curl_getinfo($receiveSMS,CURLINFO_HTTP_CODE);
       /*
	  If URL invocation is successful fetch all the received sms,else display the error.
	*/
        if($responseCode==200 || $responseCode==300)
        {
		//decode the response and display the messages.
	$jsonObj = json_decode($receiveSMS_response,true);
	$smsMsgList = $jsonObj['InboundSmsMessageList'];
	$noOfReceivedSMSMsg = $smsMsgList['NumberOfMessagesInThisBatch'];
	$noOfPendingMsg = $smsMsgList['TotalNumberOfPendingMessages'];

		  ?>
		    <div class="successWide">
                    <strong>SUCCESS:</strong><br />
		       <strong>Messages in this batch: </strong><?php echo $noOfReceivedSMSMsg; ?><br />
		       <strong>Messages pending: </strong><?php echo $noOfPendingMsg; ?>
                    </div>
                    <div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
                    <thead>
                        <tr>
                        	<th style="width: 100px" class="cell"><strong>Message Index</strong></th>
                            <th style="width: 275px" class="cell"><strong>Message Text</strong></th>
                            <th style="width: 125px" class="cell"><strong>Sender Address</strong></th>
                    	</tr>
                    </thead>
                    <tbody>
		    <?php
		    foreach($smsMsgList["InboundSmsMessage"] as $smsTag=>$val) {
		    ?>
                    <tr>
		    <td class="cell"><?php echo $val["MessageId"] ?></td>
		    <td align="center" class="cell"><?php echo $val["Message"] ?></td>
                    <td align="center" class="cell"><?php echo $val["SenderAddress"] ?></td>
                    </tr>
		    <?php
		  }
                   ?>
		    </tbody>
                    </table>
                    </div><br/>
        <?php
			
        } else{
		$msghead="Error";
		$msgdata=curl_error($receiveSMS);
		$errormsg=$msgdata.$receiveSMS_response;
               ?>
		<div class="errorWide">
                <strong>ERROR:</strong><br />
                <?php  echo $errormsg ;  ?>
                </div>

        <?php }
	curl_close ($receiveSMS);
    }
?>
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p> &#169; 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
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

