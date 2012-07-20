<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&amp;T Sample Notary Application - Sign Payload Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
</script>
<style type="text/css">
pre {
    white-space: pre;           /* CSS 2.0 */
	white-space: pre-wrap;      /* CSS 2.1 */
	white-space: pre-line;      /* CSS 3.0 */
	white-space: -pre-wrap;     /* Opera 4-6 */
	white-space: -o-pre-wrap;   /* Opera 7 */
	white-space: -moz-pre-wrap; /* Mozilla */
	white-space: -hp-pre-wrap;  /* HP Printers */
	word-wrap: break-word;      /* IE 5+ */
	}
</style>
<?php header("Content-Type: text/html; charset=ISO-8859-1"); 
session_start();
include ("config.php");
?>
<body>
<?php
$scope = "PAYMENT";
$accessToken = "";
$refreshToken = "";
$expires_in = "";
$signPayload = "";
$payload ="";
$signPayload = $_REQUEST["signPayload"];
$payload = $_REQUEST["payload"];
if($payload==null || $payload ==''){
      $payload = $_SESSION["not1_payload"];
 }
if($payload==null || $payload ==''){
  $payload = "{\"Amount\":0.99,\n \"Category\":2,\n \"Channel\":".
"\"MOBILE_WEB\",\n\"Description\":\"5 puzzles per month plan\",\n".
"\"MerchantTransactionId\":\"user573transaction1377\",\n \"MerchantProductId\":\"SudokuMthlyPlan5\",\n".
"\"MerchantApplicationId\":\"Sudoku\",\n".
"\"MerchantPaymentRedirectUrl\":".
"\"http://somewhere.com/OauthResponse.php\",\n".
"\"MerchantSubscriptionIdList\":".
"[\"p1\",".
"\"p2\",\"p3\",\"p4\",\"p5\"],\n".
"\"IsPurchaseOnNoActiveSubscription\":false,\n".
"\"SubscriptionRecurringNumber\": 5,\n \"SubscriptionRecurringPeriod\" : \"MONTHLY\",\n \"SubscriptionRecurringPeriodAmount\" : 1, }";
 }
 ?>
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

<h1>AT&amp;T Sample Notary Application - Sign Payload Application</h1>

</div>
</div>

<?php
    //If Sign Payload button was clicked, do this.
       if($signPayload!=null) {
	 $_SESSION["not1_payload"]=$payload;
	 $url = "$FQDN/Security/Notary/Rest/1/SignedPayload";
	 $headers = array(
			  'Accept: application/json',
			  'client_id: '.$api_key,
			  'client_secret: '.$secret_key
		);
	 $request = curl_init();
	 curl_setopt($request, CURLOPT_URL, $url);
	 curl_setopt($request, CURLOPT_HTTPGET, 1);
	 curl_setopt($request, CURLOPT_HEADER, 0);
	 curl_setopt($request, CURLINFO_HEADER_OUT, 0);
	 curl_setopt($request, CURLOPT_HTTPHEADER, $headers);
	 curl_setopt($request, CURLOPT_RETURNTRANSFER, 1);
	 curl_setopt($request, CURLOPT_SSL_VERIFYPEER, false);
	 curl_setopt($request, CURLOPT_POST, 1);
	 curl_setopt($request, CURLOPT_POSTFIELDS, $payload);

	 $response = curl_exec($request);
	 
	 $responseCode=curl_getinfo($request,CURLINFO_HTTP_CODE);
	 
	 if($responseCode==200) {
	   $jsonResponse = json_decode($response);
	   $signedPayload = $jsonResponse->{"SignedDocument"};
	   $_SESSION["not1_signedPayload"]=$signedPayload;
	   $signature = $jsonResponse->{"Signature"};
	   $_SESSION["not1_signature"] =  $signature;
	   if($_REQUEST["return"]!=null){
	     header("location:singlepay.php?signedPayload=".$signedPayload."&signature=".$signature);
	   }
	 } else {
	   echo curl_error($request).$response;
       }
    }
?>

<div id="wrapper">
  <div id="content">

<h2><br />
Feature 1: Sign Payload</h2>
<br/>
</div>
</div>
<form method="post" name="signPayload">
<div id="navigation">

<table border="0" width="950px">
  <tbody>
  <tr>
<?php 
$split = str_split($signedPayload,5);
$formattedSignedPayload ="";
foreach ( $split as $line ){
  $formattedSignedPayload .= $line." ";
}
$split = str_split($signature,5);
$formattedSignature ="";
foreach ( $split as $line ){
  $formattedSignature .= $line." ";
}

?>
    <td valign="top" class="label">Request:</td>
    <td class="cell" ><textarea rows="20" cols="60" name="payload" ><?php echo str_replace(",\n",",",$payload) ?></textarea>
    </td>
    <td width="50px"></td>
    <td  valign="top" class="label">Signed Payload:</td><?php  if($signPayload!=null) {?>
    <td class="cell" width="400px" ><?php  echo $formattedSignedPayload; } ?></td>
  </tr>
<tr>
    <td></td>
    <td></td>
    <td width="50px"></td>
    <td valign="top" class="label">Signature:</td><?php  if($signPayload!=null) {?>
    <td class="cell"><?php  echo $formattedSignature;?></td><?php }?>
</tr>
  <tr>
    <td></td>
    <td class="cell" align="right"><button type="submit" name="signPayload" value="signPayload">Sign Payload</button></td>
  </tr>
  </tbody></table>
</div>

<br clear="all" />
</form>

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


