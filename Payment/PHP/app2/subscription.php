
<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' June 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");
include ("config.php");
include ($oauth_file);
error_reporting(0);

$path_is = __FILE__;
$folder = dirname($path_is);
$folder = $folder. "/Notifications";
if(!is_dir($folder))
  {
    echo "Notifications folder is missing ( $folder )";
    exit();
  }

$db2_filename = $folder . "/". "singlepaylistener.db";
$db9_filename = $folder . "/". "subscriptionlistener.txt";
$db3_filename = $folder . "/". "notificationdetails.txt";
$db10_filename = $folder . "/". "checker.db";
$db4_filename = $folder . "/". "notificationack.txt";
$db5_filename = $folder . "/". "notifications.txt";


session_start();

$TransactionTime = time();
$db_filename = "transactionDatasubscription.db";
$scope = "PAYMENT";
$newSubscription = $_REQUEST["newSubscription"];
$getSubscriptionStatus = $_REQUEST["getSubscriptionStatus"];
$getSubscriptionDetails = $_REQUEST["getSubscriptionDetails"];
$refundSubscription = $_REQUEST["refundSubscription"];
$TransactionOperationStatus = "Refunded";
$merchantSunscriptionIdList = $_REQUEST["merSubscriptionIdList"];
if($mercantSubscriptionIdList == null || $merchantSubscriptionIdList == "")
$merchantSubscriptionIdList = $_SESSION["merSubscriptionIdList"];
if($mercantSubscriptionIdList == null || $merchantSubscriptionIdList == "")
$merchantSubscriptionIdList = "";
$refundReasonText = "User did not like product";
$trxId =  $_SESSION["pay2_trxId"];
if( $trxId==null || $trxId == "")
    $trxId = "";
$trxIdRefund = $_REQUEST["trxIdRefund"];
if($trxIdRefund==null || $trxIdRefund == "")
    $trxIdRefund = "";
$merchantTrxId = $_REQUEST["merchantTrxId"];
if($merchantTrxId==null || $merchantTrxId == "")
    $merchantTrxId =  $_SESSION["pay2_merchantTrxId"];
if($merchantTrxId==null || $merchantTrxId == "")
    $merchantTrxId = "";
$authCode = $_REQUEST["SubscriptionAuthCode"];
if($authCode==null || $authCode == "")
    $authCode =$_SESSION["pay2_authCode"];
$consumerId = $_REQUEST["consumerId"];
if($consumerId==null || $consumerId == "")
    $consumerId = $_SESSION["pay2_consumerId"];
$product = 0;
if($_REQUEST["product"]!=null)
    $product = $_REQUEST["product"];
$amount = "";
$description = "";
$merchantProductId = "";
$trxIdGetDetails = "";
if($product==1) {
    $amount = "1.99";
    $description = "Word Game Subscription 1";
    $merchantProductId = "WordGameSubscription1";
} else if($product==2) {
    $amount = "3.99";
    $description = "Number Game Subscription 1";
    $merchantProductId = "NumberGameSubscription1";
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

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
   <title>AT&amp;T Sample Payment Application - Subscription Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >

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

  <h1>AT&amp;T Sample Payment Application - Subscription Application</h1>
<h2>Feature 1: Create New Subscription</h2><br/>

</div>
</div>
<form method="post" name="newSubscription" >
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><input type="radio" name="product" value="1" checked>
    <td valign="top" class="label">Subscribe for $1.99 per month</td>
    </td>
  </tr>
  <tr>
    <td class="cell"><input type="radio" name="product" value="2">
    <td valign="top" class="label">Subscribe for $3.99 per month</td>
    </td></tr>
  </tbody></table>

</div>
<div id="extra">

  <table>
  <tbody>
  <tr>
      <td><br /><br /><button type="submit" name="newSubscription" value="newSubscription">Subscribe</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />
<div align="center"></div>
</form>

<?php if($newSubscription!=null) { 
  $merchantTrxId = "user".rand(1,10000000)."subscription".rand(1,10000000);
  $merchantSubscriptionIdList = "SL".rand(10, 10000);


  $_SESSION["pay2_merchantTrxId"] =  $merchantTrxId;
  $_SESSION["pay2_mersubscriptionIdList"] = $merchantSubscriptionIdList;
  $_SESSION["pay2_trxId"] = null;
  $_SESSION["pay2_authCode"] = null;
  $_SESSION["pay2_consumerId"] =  null;



$forNotary = "notary.php?signPayload=true&return=subscription.jsp&payload=".
"{\"Amount\":".$amount.",".
"\"Category\":1, \"Channel\":\"MOBILE_WEB\",".
"\"Description\":\"".$description."\",".
"\"MerchantTransactionId\":\"".$merchantTrxId."\",".
"\"MerchantProductId\":\"".$merchantProductId."\",".
"\"MerchantPaymentRedirectUrl\":\"".$subscriptionRedirect."\",".
"\"MerchantSubscriptionIdList\":\"".$merchantSubscriptionIdList."\",".
"\"IsPurchaseOnNoActiveSubscription\":\"false\",".
"\"SubscriptionRecurrences\":99999,".
"\"SubscriptionPeriod\":\"MONTHLY\",".
"\"SubscriptionPeriodAmount\":1}";
header("location:".$forNotary);
} ?>

<?php if($_REQUEST["SubscriptionAuthCode"] !=null) { 
$_SESSION["pay2_authCode"] = $authCode;
?>
<div class="successWide">
<strong>SUCCESS:</strong><br />
   <strong>Merchant Subscription ID</strong> <?php echo $_SESSION["pay2_merchantTrxId"]; ?><br/>
<strong>Subscription Auth Code</strong> <?php echo $_SESSION["pay2_authCode"]; ?><br /><br/>
<form name="getNotaryDetails" action="notary.php">
    <input type="submit" name="getNotaryDetails" value="View Notary Details" />
</form>
</div><br/>
<?php } ?>

<?php
if($_REQUEST["signedPayload"]!=null && $_REQUEST["signature"]!=null){
    header("location:".$FQDN."/rest/3/Commerce/Payment/Subscriptions?clientid=".$api_key."&SignedPaymentDetail=".$_REQUEST["signedPayload"]."&Signature=".$_REQUEST["signature"]);
}
?>

<?php if($getSubscriptionStatus!=null) { 
  //This application uses the Autonomous Client OAuth consumption model
  //Check if there is a valid access token that has not expired
  $fullToken["accessToken"]=$accessToken;
  $fullToken["refreshToken"]=$refreshToken;
  $fullToken["refreshTime"]=$refreshTime;
  $fullToken["updateTime"]=$updateTime;

  $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
  $accessToken=$fullToken["accessToken"];
  $getTransactionType = $_REQUEST["getTransactionType"];
  $url = "";
if($getTransactionType==1)
    $url = $FQDN."/rest/3/Commerce/Payment/Subscriptions/MerchantTransactionId/".$_SESSION["pay2_merchantTrxId"];   
if($getTransactionType==2)
    $url = $FQDN."/rest/3/Commerce/Payment/Subscriptions/SubscriptionAuthCode/".$_SESSION["pay2_authCode"];
if($getTransactionType==3)
    $url = $FQDN."/rest/3/Commerce/Payment/Subscriptions/SubscriptionId/".$_SESSION["pay2_trxId"] ;


  $accept = "Accept: application/json";
  $authorization = "Authorization: Bearer ".$accessToken;
  $content = "Content-Type: application/json";
  
  
  $request = curl_init();
  curl_setopt($request, CURLOPT_URL, $url);
  curl_setopt($request, CURLOPT_HTTPGET, 1);
  curl_setopt($request, CURLOPT_HEADER, 0);
  curl_setopt($request, CURLINFO_HEADER_OUT, 0);
  curl_setopt($request, CURLOPT_HTTPHEADER, array($authorization, $content, $accept));
  curl_setopt($request, CURLOPT_RETURNTRANSFER, 1);
  curl_setopt($request, CURLOPT_SSL_VERIFYPEER, false);

  $response = curl_exec($request);
  
  $responseCode=curl_getinfo($request,CURLINFO_HTTP_CODE);

  if($responseCode==200) {
    $jsonResponse = json_decode($response,true);
    $trxId = $jsonResponse["SubscriptionId"];
    $_SESSION["pay2_trxId"] = $trxId;
    $consumerId = $jsonResponse["ConsumerId"];
    $_SESSION["pay2_consumerId"] = $consumerId;
    $merchantTrxId = $jsonResponse["MerchantTransactionId"];
    $_SESSION["pay2_merchantTrxId"]=$merchantTrxId;
$merchantSubscriptionId = $jsonResponse["MerchantSubscriptionId"];
   $_SESSION["pay2_merchantSubscriptionId"] = $merchantSubscriptionId;

    If ( $trxId != null && $trxId != ""){

      $transaction["trxId"] = $trxId;
      $transaction["merchantTrxId"] = $merchantTrxId;
      $transaction["authCode"] = $authCode;
      $transaction["consumerId"] = $consumerId;
      $transaction["merchantSubscriptionId"] = $merchantSubscriptionId;

      if ( file_exists( $db_filename) ){
	$transactions = unserialize(file_get_contents($db_filename));
	$transaction_exist = false;
	foreach( $transactions as $tr){
	  if($tr["MerchantSubscriptionId"] == $merchantSubscriptionId){
	    $transaction_exist = true;
	  }
	}
	if(!$transaction_exist){
	  $stored_tnumber = array_unshift($transactions,$transaction);
	  if ( $stored_tnumber > 5 ){
	    array_pop($transactions);
	  }
	}
      }else{
	$transactions = array($transaction);
      }
      $fp = fopen($db_filename, 'w+') or die("I could not open $filename.");
      fwrite($fp,serialize($transactions));
      fclose($fp);
    }
  }

}
?>

<div id="wrapper">
<div id="content">

<h2><br />
Feature 2: Get Subscription Status</h2>

</div>
</div>
<form method="post" name="getSubscriptionStatus" action="subscription.php">
<div id="navigation" align="center">

<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th style="width: 160px" class="cell" align="right"></th>
        <th style="width: 100px" class="cell"></th>
        <th style="width: 240px" class="cell" align="left"></th>
    </tr>
</thead>
  <tbody>
  <tr>
    <td class="cell" align="left">
    <input type="radio" name="getTransactionType" value="1" checked /> Merchant Transaction ID:
    </td>
    <td></td>
    <td class="cell" align="left"><?php echo $_SESSION["pay2_merchantTrxId"]?></td>
  </tr>
  <tr>
    <td class="cell" align="left">
        <input type="radio" name="getTransactionType" value="2" /> Auth Code:
    <td></td>
    <td class="cell" align="left"><?php echo $_SESSION["pay2_authCode"]?></td>
    </td>
  </tr>
  <tr>
    <td class="cell" align="left">
        <input type="radio" name="getTransactionType" value="3" /> Subscription ID:
    <td></td>
    <td class="cell" align="left"><?php echo $_SESSION["pay2_trxId"] ?></td>
    </td>
  </tr>
  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button  type="submit" name="getSubscriptionStatus" value="getSubscriptionStatus">Get Subscription Status</button>
    </td>
  </tr>
  </tbody></table>


</form>
</div>
<br clear="all" />

<?php if($getSubscriptionStatus!=null) { 
    if($responseCode==200) {

    ?>
        <div class="successWide">
        <strong>SUCCESS</strong><br />
	  </div><br/>
        <div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
        <thead>
            <tr>
                <th style="width: 100px" class="cell" align="right"><strong>Parameter</strong></th>
                <th style="width: 100px" class="cell"><strong></strong></th>
                <th style="width: 275px" class="cell" align="left"><strong>Value</strong></th>
            </tr>
        </thead>
        <tbody>
            <?php   foreach ( $jsonResponse as $parameter => $value ){ ?>
            	<tr>
		    <td align="right" class="cell"><?php echo $parameter; ?></td>
                    <td align="center" class="cell"></td>
		    <td align="left" class="cell"><?php echo $value; ?></td>
                </tr>
        	<?php } ?>
        </tbody>
        </table>
        </div><br/>
    <?php
} else {
?>
    <div class="errorWide">
    <strong>ERROR:</strong><br />
<?php echo $response; ?>
    </div><br/>
<?php } }?>
<div id="wrapper">
<div id="content">

<h2><br />Feature 3: Get Subscription Details</h2>

<?php if($getSubscriptionDetails!=null) { 
      //This application uses the Autonomous Client OAuth consumption model
      //Check if there is a valid access token that has not expired
      $fullToken["accessToken"]=$accessToken;
      $fullToken["refreshToken"]=$refreshToken;
      $fullToken["refreshTime"]=$refreshTime;
      $fullToken["updateTime"]=$updateTime;
      $trxIdGetDetails = $_REQUEST["trxIdGetDetails"];
      
      $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
      $accessToken=$fullToken["accessToken"];
      
      $url = $FQDN."/rest/3/Commerce/Payment/Subscriptions/".$_SESSION["pay2_mersubscriptionIdList"]."/Detail/".$trxIdGetDetails;
      
      
    $accept = "Accept: application/json";
  $authorization = "Authorization: Bearer ".$accessToken;
  $content = "Content-Type: application/json";
  
  
      $request = curl_init();
      curl_setopt($request, CURLOPT_URL, $url);
      curl_setopt($request, CURLOPT_HTTPGET, 1);
      curl_setopt($request, CURLOPT_HEADER, 0);
      curl_setopt($request, CURLINFO_HEADER_OUT, 1);
      curl_setopt($request, CURLOPT_HTTPHEADER, array($authorization, $content, $accept));
      curl_setopt($request, CURLOPT_RETURNTRANSFER, 1);
      curl_setopt($request, CURLOPT_SSL_VERIFYPEER, false);

      $response = curl_exec($request);
      $info = curl_getinfo($request);
      echo $info['request headers'];
  
      $responseCode=curl_getinfo($request,CURLINFO_HTTP_CODE);

      if($responseCode==200) {
	$jsonResponse = json_decode($response,true);
	$trxId = $jsonResponse["TransactionId"];
	$merchantTrxId = $jsonResponse["MerchantTransactionId"];
      }
  }
?>

</div>
</div>
<br/>
<form method="post" name="getSubscriptionDetails" >
<div id="navigation" align="center">

<table style="width: 850px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th class="cell" align="left"><strong>Consumer ID</strong></th>
        <th style="width: 100px" class="cell"></th>
        <th class="cell" align="left"><strong>Merchant Subscription ID</strong></th>
    <td><div class="warning">
<strong>WARNING:</strong><br />
You must use Get Subscription Status to get the Consumer ID before you can get details.
</div></td>
    </tr>
</thead>
  <tbody>
<?php
if(true) {
	    $transactions = unserialize(file_get_contents($db_filename)); 
	    foreach ( $transactions as $transaction ){
?>
                      <tr>
                        <td class="cell" align="right">
	                <?php 	if ( $checked ){
	                          $checked = false;
                        ?>
				  <input type="radio" name="trxIdGetDetails" value="<?php echo $transaction["consumerId"]; ?>"/><?php echo $transaction["consumerId"];?>
				  <?php } else { ?>
                            <input type="radio" name="trxIdGetDetails" value="<?php echo $transaction["consumerId"]; ?>" /><?php echo $transaction["consumerId"]; ?>
	    <?php } ?>

                        </td>
                        <td></td>
                        <td class="cell" align="left"><?php echo $transaction["merchantSubscriptionId"] ?></td>
                      </tr>  
			    <?php }

            }
?>

  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button  type="submit" name="getSubscriptionDetails" value="getSubscriptionDetails">Get Subscription Details</button>
    </td>
  </tr>
  </tbody></table>


</form>
</div>
<br clear="all" />


<?php if($getSubscriptionDetails!=null) { 
      if($responseCode==200) {
      ?>
        <div class="successWide">
        <strong>SUCCESS</strong><br />
        </div><br/>
        <div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
        <thead>
            <tr>
                <th style="width: 100px" class="cell" align="right"><strong>Parameter</strong></th>
                <th style="width: 100px" class="cell"><strong></strong></th>
                <th style="width: 275px" class="cell" align="left"><strong>Value</strong></th>
            </tr>
        </thead>
        <tbody>
	    <?php 
	       foreach ( $jsonResponse as $parameter => $value ){ ?>
            	<tr>
		     <td align="right" class="cell"><?php echo $parameter; ?></td>
                    <td align="center" class="cell"></td>
                    <td align="left" class="cell"><?php echo $value; ?></td>
                </tr>
<?php } 
 ?>
        </tbody>
        </table>
        </div><br/>
    <?php
  } else {
  	?>
            <div class="errorWide">
            <strong>ERROR:</strong><br />
	<?php echo $response; ?>
            </div><br/>
	<?php
  }
}
?>

<div id="wrapper">
<div id="content">

<h2><br />Feature 4: Refund Subscription</h2>
<?php if($refundSubscription!=null) {
  //This application uses the Autonomous Client OAuth consumption model
  //Check if there is a valid access token that has not expired
        $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
        $accessToken=$fullToken["accessToken"];
        $trxIdGetRefund =$_REQUEST["trxIdGetRefund"];

        $url = $FQDN."/rest/3/Commerce/Payment/Transactions/".$trxIdGetRefund;
       


        $payload = "{\"TransactionOperationStatus\":Refunded,\n \"RefundReasonCode\":1,\n \"RefundReasonText\":\"".$refundReasonText."\"}";
        $putData = tmpfile();
        fwrite($putData, $payload);
        fseek($putData, 0);

$accept = "Accept: application/json";
  $authorization = "Authorization: Bearer ".$accessToken;
  $content = "Content-Type: application/json";

        $request = curl_init();
        curl_setopt($request, CURLOPT_URL, $url);
        curl_setopt($request, CURLOPT_HTTPGET, 1);
        curl_setopt($request, CURLOPT_HEADER, 0);
        curl_setopt($request, CURLINFO_HEADER_OUT, 0);
        curl_setopt($request, CURLOPT_HTTPHEADER, array($authorization, $content, $accept));
        curl_setopt($request, CURLOPT_RETURNTRANSFER, 1);
        curl_setopt($request, CURLOPT_SSL_VERIFYPEER, false);
        curl_setopt($request, CURLOPT_PUT, 1);
        curl_setopt($request, CURLOPT_INFILE, $putData);
        curl_setopt($request, CURLOPT_INFILESIZE, strlen($payload));
        $response = curl_exec($request);
        fclose($putData);


        $responseCode=curl_getinfo($request,CURLINFO_HTTP_CODE);

        if($responseCode==200) {

          $jsonResponse = json_decode($response,true);

          if ( file_exists( $db_filename) ){
            $transactions = unserialize(file_get_contents($db_filename));
	    foreach($transactions as $key=>&$transaction){
	      if($transaction["trxId"] == $trxIdRefund){
		   unset($transactions[$key]);
	      }
	    }
	    $fp = fopen($db_filename, 'w+') or die("I could not open $filename.");
	    fwrite($fp, serialize($transactions));
	    fclose($fp);
	  }
	}
  }
  ?>

</div>
</div>
<form method="post" name="refundSubscription" >
<div id="navigation" align="center">

<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
        <th class="cell" align="left"><strong>Subscription ID</strong></th>
        <th style="width: 100px" class="cell"></th>
        <th class="cell" align="left"><strong>Merchant Subscription ID</strong></th>
    <td><div class="warning">
<strong>WARNING:</strong><br />
You must use Get Subscription Status to get the Consumer ID before you can get details.
</div></td>
    </tr>
</thead>
  <tbody>
<?php
if(true) {
	    $transactions = unserialize(file_get_contents($db_filename)); 
	    foreach ( $transactions as $transaction ){
?>
                      <tr>
                        <td class="cell" align="right">
	                <?php 	if ( $checked ){
	                          $checked = false;
                        ?>
				  <input type="radio" name="trxIdGetRefund" value="<?php echo $transaction["trxId"]; ?>"/><?php echo $transaction["trxId"];?>
				  <?php } else { ?>
                            <input type="radio" name="trxIdGetRefund" value="<?php echo $transaction["trxId"]; ?>" /><?php echo $transaction["trxId"]; ?>
	    <?php } ?>

                        </td>
                        <td></td>
                        <td class="cell" align="left"><?php echo $transaction["merchantSubscriptionId"] ?></td>
                      </tr>  
			    <?php }

            }
?>

  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button  type="submit" name="refundSubscription" value="refundSubscription">Refund Subscription</button>
    </td>
  </tr>
  </tbody></table>


</form>
</div>
<br clear="all" />


<?php if($refundSubscription!=null) { 
      if($responseCode==200) {
      ?>
        <div class="successWide">
        <strong>SUCCESS</strong><br />
        </div><br/>
        <div align="center"><table style="width: 650px" cellpadding="1" cellspacing="1" border="0">
        <thead>
            <tr>
                <th style="width: 100px" class="cell" align="right"><strong>Parameter</strong></th>
                <th style="width: 100px" class="cell"><strong></strong></th>
                <th style="width: 275px" class="cell" align="left"><strong>Value</strong></th>
            </tr>
        </thead>
        <tbody>
	    <?php 
	       foreach ( $jsonResponse as $parameter => $value ){ ?>
            	<tr>
		     <td align="right" class="cell"><?php echo $parameter; ?></td>
                    <td align="center" class="cell"></td>
                    <td align="left" class="cell"><?php echo $value; ?></td>
                </tr>
<?php } 
 ?>
        </tbody>
        </table>
        </div><br/>
    <?php
  } else {
  	?>
            <div class="errorWide">
            <strong>ERROR:</strong><br />
	<?php echo $response; ?>
            </div><br/>
	<?php
  }
}
?>
<div id="wrapper">
<div id="content">
<h2><br />Feature 4: Notifications</h2>

</div>
</div>
<form method="post" name="refreshNotifications" action="subscription.php">
<div id="navigation" align="center">

<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
<th class="cell" align="left"><strong>Notification ID</strong></th>
        
<th class="cell" align="left"><strong>Notification Type</strong></th>
<th style="width: 100px" class="cell"></th>
<th class="cell" align="left"><strong>Transaction ID</strong></th>


</td>
	</tr>
</thead>
<tbody>
<?php
if(true) {

              $responses = unserialize(file_get_contents($db10_filename));
      foreach($responses as $response){
        

?>
                      <tr>
                        <td class="cell" align="left">
	                  <?php echo $response["NotificationID"];?>

			     </td>
                         <td class="cell" align="left">
                         <?php echo $response["NotificationType"]?>
</td>

                        </td>
                        <td></td>
                        <td class="cell" align="left"><?php echo $response["OriginalTransactionId"]?></td>
                      </tr>  
<?php 
      }

}?>
  <tr>
    <td></td>
    <td></td>
    <td></td>
    <td class="cell"><button type="submit" name="refreshNotifications" value="refreshNotifications">Refresh</button>
    </td
  </tr>
  </tbody></table>


</form>
</div>
<?php
$refreshNotifications = $_REQUEST["refreshNotifications"];

if($refreshNotifications != null) {

$notificationlist = array();
$notifications = file_get_contents($db9_filename);

    preg_match_all("'<hub:notificationId>(.*?)</hub:notificationId>'si", $notifications, $match);

    foreach($match[1] as $val) {
   $notificationId = $val;
   array_push($notificationlist, $notificationId);

    
$fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
  $accessToken=$fullToken["accessToken"];


  

  
    $url = $FQDN."/rest/3/Commerce/Payment/Notifications/".$notificationId;

  
  $accept = "Accept: application/json";
  $authorization = "Authorization: Bearer ".$accessToken;
  $content = "Content-Type: application/json";
  $request = curl_init();
  curl_setopt($request, CURLOPT_URL, $url);
  curl_setopt($request, CURLOPT_HTTPGET, 1);
  curl_setopt($request, CURLOPT_HEADER, 0);
  curl_setopt($request, CURLINFO_HEADER_OUT, 0);
  curl_setopt($request, CURLOPT_HTTPHEADER, array($authorization, $content, $accept));
  curl_setopt($request, CURLOPT_SSL_VERIFYPEER, false);
  curl_setopt($request, CURLOPT_RETURNTRANSFER, 1);
 

  $response = curl_exec($request);

  $responseCode=curl_getinfo($request,CURLINFO_HTTP_CODE);

if($responseCode==200) {
	$jsonResponse = json_decode($response,true);
    $originaltrxId = $jsonResponse["GetNotificationResponse"]["OriginalTransactionId"];
    $notificationtype = $jsonResponse["GetNotificationResponse"]["NotificationType"];
    $responses["NotificationType"] = $notificationtype;
    $responses["NotificationID"] = $notificationId;
   $responses["OriginalTransactionId"] = $originaltrxId;






$details = array();
  if ( file_exists( $db3_filename) ){
            $notificationdetails = unserialize(file_get_contents($db3_filename));
            array_push($details, $response); 
            $fp = fopen($db3_filename, 'w+') or die("I could not open $db3_filename.");
            fwrite($fp, serialize($response));
            
   }

if ( file_exists( $db10_filename) ){
            $responsetest = unserialize(file_get_contents($db10_filename));
            $responsetest = array($responses);
            //array_push($responsetest,$responses);
            $fp = fopen($db10_filename, 'w+') or die("I could not open $db10_filename.");
            fwrite($fp, serialize($responsetest));
           
            
            
           
   }


if($refreshNotifications != null) {
        if($responseCode==200) {

   $url = $FQDN."/rest/3/Commerce/Payment/Notifications/".$notificationId;

  

       $payload = "";
	$putData = tmpfile();
	fwrite($putData, $payload);
	fseek($putData, 0);

$accept = "Accept: application/json";
  $authorization = "Authorization: Bearer ".$accessToken;
  $content = "Content-Type: application/json";
	
	$request = curl_init();
	curl_setopt($request, CURLOPT_URL, $url);
	curl_setopt($request, CURLOPT_HTTPGET, 1);
	curl_setopt($request, CURLOPT_HEADER, 0);
	curl_setopt($request, CURLINFO_HEADER_OUT, 0);
	curl_setopt($request, CURLOPT_HTTPHEADER, array($authorization, $content, $accept));
	curl_setopt($request, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($request, CURLOPT_SSL_VERIFYPEER, false);
	curl_setopt($request, CURLOPT_PUT, 1);
	curl_setopt($request, CURLOPT_INFILE, $putData);
	curl_setopt($request, CURLOPT_INFILESIZE, strlen($payload));
	$response = curl_exec($request);
	fclose($putData);

  

  $responseCode=curl_getinfo($request,CURLINFO_HTTP_CODE);

  if($responseCode==200) {
    $jsonResponse = json_decode($response,true);
    $acknowledgements = array();
    if ( file_exists( $db4_filename) ){
            $acknowledgements = unserialize(file_get_contents($db4_filename));
            
            $fp = fopen($db4_filename, 'a+') or die("I could not open $db4_filename.");
            array_push($acknowledgements, $response); 
            fwrite($fp, $response);
           
   }
}

    

} 

} } 

 if ( file_exists( $db5_filename) ){
            $nootificationlist = unserialize(file_get_contents($db5_filename));
            
            $fp = fopen($db5_filename, 'w+') or die("I could not open $db5_filename.");
            fwrite($fp, serialize($notificationlist));
fclose($fp);}}
} ?>
</tbody>
</table>
</div>

</div>
<br clear="all" />
</form></div>
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p>&#169; 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>

</body></html>








