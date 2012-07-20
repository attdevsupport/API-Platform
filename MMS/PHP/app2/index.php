<!--- Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012 TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");
include ("config.php");
include ($oauth_file);
session_start();

$_SESSION["mms2_mmsID"] = null;    

if (!empty($_REQUEST["sendMMS"])) {
  $_SESSION["mms2_addresses"] = $_POST['addresses'];
 }
    /* Extract the session variables */
$subject = $default_subject;
$mmsID=$_SESSION["mms2_mmsID"];

$addresses=$_SESSION["mms2_addresses"];
if($addresses==null){
  $addresses = $default_address;
 }


function RefreshToken($FQDN,$api_key,$secret_key,$scope,$fullToken){

  $refreshToken=$fullToken["refreshToken"];
  $accessTok_Url = $FQDN."/oauth/token";

  //http header values
  $accessTok_headers = array(
			     'Content-Type: application/x-www-form-urlencoded'			     );

  //Invoke the URL
  $post_data="client_id=".$api_key."&client_secret=".$secret_key."&refresh_token=".$refreshToken."&grant_type=refresh_token";

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
<title>AT&amp;T Sample MMS Application 2 - MMS Coupon Application</title>
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

  <h1>AT&amp;T Sample MMS Application 2 - MMS Coupon Application</h1>
<h2>Feature 1: Send coupon image to list of subscribers</h2>

</div>
</div>
<form method="post" name="sendMMS" enctype="multipart/form-data">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input  size="12" name="addresses" value="<?php echo $addresses; ?>" style="width: 90%">
    </td>
  </tr>
  <tr>
    <td valign="top" class="label">Subject:</td>
    <td class="cell"><?php echo $subject; ?></td>
  </tr>
  </tbody></table>

<div class="warning">
<strong>WARNING:</strong><br />
total size of all attachments cannot exceed 600 KB.
</div>

</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td valign="top" class="label">Attachment:</td>
    <td class="cell"><div style="width: 250px; background: #fc9; border: 3px double #006; text-align: center; padding: 25px"><em><img width="250px" src="coupon.jpg" /></em></div>
    </td></em></div>
    </td>
  </tr>
  </tbody></table>
  <table>
  <tbody>
  <tr>
  	<td><br /><button type="submit" name="sendMMS" value="Send Coupon">Send Coupon</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />
<div align="center"></div>
</form>
<?php

    if (!empty($_REQUEST["sendMMS"])) {

      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      
      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];

      $addresses = $_POST['addresses'];      
      $addresses_url="";
      $addresses_array=explode(",",$addresses);
      $invalid_addresses = array();
      foreach($addresses_array as $address){
	$clean_addres =  str_replace("-","",$address);
	$clean_addres =  str_replace("tel:","",$clean_addres);
	$clean_addres =  str_replace("+1","",$clean_addres);
	if(preg_match("/\d{10}/",$clean_addres)){
	     $addresses_url.="Address=tel:".urlencode($clean_addres)."&";
	     
	   }else{
	  array_push($invalid_addresses,$address);
	}
      }
      if ( $addresses_url != "" ){

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
      $sendMMSData = $addresses_url.'Subject='.urlencode($subject);

	//Form the MIME part with MIME message headers and MIME attachment
      $data = "";
      $data .= "--$boundary\r\n";
      $data .= "Content-Type: application/x-www-form-urlencoded; charset=UTF-8\r\nContent-Transfer-Encoding: 8bit\r\nContent-Disposition: form-data; name=\"root-fields\"\r\nContent-ID: <startpart>\r\n\r\n".$sendMMSData."\r\n";
      $data .= "--$boundary\r\n";
      $data .= "Content-Disposition: attachment; filename=\"coupon.jpg\"\r\n";
      $data .= "Content-Type:image/png\r\n";
      $data .= "Content-ID: <coupon.jpg>\r\n";
      $data .= "Content-Transfer-Encoding: binary\r\n\r\n";
      $data .= join("", file("coupon.jpg"))."\r\n";
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
	    $_SESSION["mms2_mmsID"] = $mmsID;

	    ?>

	      <div class="successWide">
		 <strong>SUCCESS:</strong><br />
		<strong> Message ID</strong> <?php echo $mmsID; ?>
		 </div>

	<?php } else {
	  $_SESSION["mms2_mmsID"] = null;
	    //print "The Request was Not Successful";
	    ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo $sendMMS_response;  ?>
	    </div>

	<?php }
      }
      if(!empty($invalid_addresses )){
	$_SESSION["mms2_mmsID"] = null;
	?>
	<div class="errorWide">
	<strong>ERROR: Invalid numbers</strong><br />
	<?php 
	foreach ( $invalid_addresses as $invalid_address ){
	  echo $invalid_address."<br/>";
	}  
	?>
	</div>
	<?php 
      }
}
?>

<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Check Delivery Status for each Recipient</h2>

</div>
</div>
<div id="navigation">
<form  name="getMmsDeliveryStatus" method="post">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell">
   <button type="submit" name="getMmsDeliveryStatus" value="Get Status">Check Status</button>
  <input type="hidden" name="mmsID" value="<?php echo $mmsID; ?>"><?php echo $_SESSION["mms2_mmsID"];?>
    </td>
  </tr>
  </tbody></table>
  
</div>
<div id="extra">


</div>
<br clear="all" />
</form>
<?php
    if (!empty($_REQUEST["getMmsDeliveryStatus"])) {
      
      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      
      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];


    	$mmsID=$_POST["mmsID"];
        $_SESSION["mms2_mmsID"] = $_POST['mmsID'];

	//Form URL to get the delivery status
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
	  $deliveryInfo=$jsonObj['DeliveryInfoList']['DeliveryInfo'];

	  ?>
	    <div class="successWide">
	       <strong>SUCCESS:</strong><br />
	       Messages Delivered
            </div>
	       <br />

	       <div align="center">
	       <table width="500" cellpadding="1" cellspacing="1" border="0">
	       <thead>
	       <tr>
	       <th width="50%" class="label">Recipient</th>
	       <th width="50%" class="label">Status</th>
	       </tr>
	       </thead>
	       <tbody>
	       <?php
	       foreach ( $deliveryInfo as $status ){
	    ?>
	    <tr>
		 <td class="cell" align="center"><?php echo $status["Address"]; ?></td>
		 <td class="cell" align="center"><?php echo $status["DeliveryStatus"]; ?></td>
	    </tr>
           <?php 
	  }
	       ?>
	       </tbody>
		   </table>
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

