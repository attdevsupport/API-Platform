<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' September 2011
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2011 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");

function GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode){

  // **********************************************************************
  // ** code to get access token by passing auth code, client ID and
  // ** client secret
  // **********************************************************************

  //Form URL to get the access token
  $accessTok_Url = "$FQDN/oauth/token";
  $accessTok_Url .= "?client_id=".$api_key;
  $accessTok_Url .= "&client_secret=".$secret_key;
  $accessTok_Url .= "&code=" . $authCode . "&grant_type=authorization_code";
  //http header values
  $accessTok_headers = array(
			     'Content-Type: application/x-www-form-urlencoded'
			     );
            

  //Invoke the URL
  $accessTok = curl_init();

  $post_data="client_id=".$api_key."&client_secret=".$secret_key."&code=".$authCode."&grant_type=authorization_code";

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
  /*
   If URL invocation is successful fetch the access token and store it in session,
   else display the error.
  */
  if($responseCode==200)
    {
      $jsonObj = json_decode($accessTok_response);
      $accessToken = $jsonObj->{'access_token'};//fetch the access token from the response.
      $_SESSION["tl1_deviceLocation_access_token"]=$accessToken;//store the access token in to session.
    }
  else{
    //    echo curl_error($accessTok);
  }
  curl_close ($accessTok);
  header("location:index.php");//redirect to the index page.
  exit;

}

function getAuthCode($FQDN,$api_key,$secret_key,$scope,$authorize_redirect_uri){
  //Form URL to get the authorization code
  $authorizeUrl = "$FQDN/oauth/authorize";
  $authorizeUrl .= "?scope=".$scope;
  $authorizeUrl .= "&client_id=".$api_key;
  $authorizeUrl .= "&redirect_uri=".$authorize_redirect_uri;

  header("Location: $authorizeUrl");
}

session_start();
include ("config.php");
/* Extract the session variables */

$address=$_SESSION["tl1_address"];
$requestedAccuracy = $_SESSION["tl1_requestedAccuracy"];
$acceptableAccuracy = $_SESSION["tl1_acceptableAccuracy"];
$tolerance=$_SESSION["tl1_tolerance"];

if($address==null){
  $address = $default_address;
  $_SESSION["tl1_address"] = $address;

 }

/* Extract the session variables */
$accessToken = $_SESSION["tl1_deviceLocation_access_token"];

if (!empty($_REQUEST["deviceLocation"])) {
  $_SESSION["tl1_deviceLocation"] = true;
  $address = $_POST['address'];
  if( $address != $_SESSION["tl1_address"]){ // new address entered, need to request new auth code and token 
    $accessToken = null;
    $authCode =null;
  }
  $_SESSION["tl1_address"]=$_POST['address'];
  $requestedAccuracy=$_POST['requestedAccuracy'];
  $_SESSION["tl1_requestedAccuracy"]=$requestedAccuracy;
  $acceptableAccuracy=$_POST['acceptableAccuracy'];
  $_SESSION["tl1_acceptableAccuracy"]=$acceptableAccuracy;
  $tolerance=$_POST['tolerance'];
  $_SESSION["tl1_tolerance"]=$tolerance;
 }
 
if ($_SESSION["tl1_deviceLocation"]) {
  
  if($accessToken == null || $accessToken == '') {
    $authCode = $_GET["code"];
    if ($authCode == null || $authCode == "") {   
      getAuthCode( $FQDN,$api_key,$secret_key,$scope,$authorize_redirect_uri);
    }else{
      $accessToken = GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode);
      $_SESSION["tl1_deviceLocation_access_token"] =  $accessToken;
    }
  }
 } 
  
    

?>
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&amp;T Sample Application - TL Service Application</title>
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

<h1>AT&T Sample Application - TL</h1>
<h2>Feature 1: Map of Device Location</h2>

</div>
</div>


<form name="deviceLocation" method="post">

<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<?php echo $address; ?>" style="width: 90%">
    </td>
  </tr>
  <tr>
  	<td valign="top" class="label">Requested Accuracy</td>
    <td valign="top" class="cell">150 m <input type="radio" name="requestedAccuracy" value="150" /> 1,000 m <input type="radio" name="requestedAccuracy"checked="checked"  value="1000" /> 10,000 m <input type="radio" name="requestedAccuracy" value="10000" /></td>
  </tr>
  <tr>
  	<td valign="top" class="label">Acceptable Accuracy</td>
    <td valign="top" class="cell">150 m <input type="radio" name="acceptableAccuracy" value="150" /> 1,000 m <input type="radio" name="acceptableAccuracy"   value="1000" /> 10,000 m <input type="radio" name="acceptableAccuracy"  checked="checked" value="10000" /></td>
  </tr>
  <tr>
    <td valign="top" class="label">Delay Tolerance:</td>
    <td valign="top" class="cell">No Delay <input type="radio" name="tolerance" value="NoDelay" /> Low Delay <input type="radio" name="tolerance" checked="checked" value="LowDelay" /> Delay Tolerant <input type="radio" name="tolerance" value="DelayTolerant" /></td>
    </td>
  </tr>
  </tbody></table>

</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><br /><br /><br /><br /><br /><br /><button type="submit" name="deviceLocation" value="deviceLocation">Get Phone Location</button></td>
  </tr>
  </tbody></table>


</div>
<br clear="all" />

<div align="center"></div>

</form>

<?php
	/*
	   Extract parameter device Id from deviceLocation form
	   and invoke the URL to get device location along with access token
	*/
	
    if ($_SESSION["tl1_deviceLocation"] && $accessToken != null && $accessToken != '') {
	
	  $_SESSION["tl1_deviceLocation"]=false;
	  $address =  str_replace("-","",$_SESSION["tl1_address"]);
	  $address =  str_replace("tel:","",$address);
	  $address =  str_replace("+1","",$address);
	  $address = "tel:" . $address;


 	// Form the URL to get device location
	$device_location_Url = "$FQDN/1/devices/".$address."/location";
	$device_location_Url .= "?access_token=".$accessToken."&requestedAccuracy=".$requestedAccuracy."&acceptableAccuracy=".$acceptableAccuracy."&tolerance=".$tolerance;
	$device_location_headers = array(
		'Content-Type: application/x-www-form-urlencoded'
	);
	//Invoke the URL
	$device_location = curl_init();
	curl_setopt($device_location, CURLOPT_URL, $device_location_Url);
	curl_setopt($device_location, CURLOPT_HTTPGET, 1);
	curl_setopt($device_location, CURLOPT_HEADER, 0);
	curl_setopt($device_location, CURLINFO_HEADER_OUT, 0);
	//curl_setopt($device_location, CURLOPT_HTTPHEADER, $device_location_headers);
	curl_setopt($device_location, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($device_location, CURLOPT_SSL_VERIFYPEER, false);
	$startTime=time();
    	$device_location_response = curl_exec($device_location);
	$endTime=time();
	$elapsedTime=$endTime-$startTime;

	$responseCode=curl_getinfo($device_location,CURLINFO_HTTP_CODE);


       /*
	  If URL invocation is successful fetch the device location information and display,
	  else display the error.
       */
	if($responseCode>=200 && $responseCode<=300)
        {
		$jsonObj = json_decode($device_location_response);//decode the response and display it.
?>
            <div class="successWide">
            <strong>SUCCESS:</strong><br />
	    <strong>Latitude:</strong> <?php echo $jsonObj->{'latitude'}; ?><br />
	    <strong>Longitude:</strong> <?php echo $jsonObj->{'longitude'}; ?><br />
	    <strong>Accuracy:</strong> <?php echo $jsonObj->{'accuracy'}; ?><br />
	    <strong>Response Time:</strong> <?php echo $elapsedTime; ?> seconds
            </div>
            <br /><br />
            
            <div align="center">
            <iframe width="600" height="400" frameborder="0" scrolling="no" marginheight="0" marginwidth="0" 
    		src="http://maps.google.com/?q=<?php echo $jsonObj->{'latitude'}; ?>+<?php echo $jsonObj->{'longitude'}; ?>&output=embed"></iframe><br /></div>
		<?php
	  }
	else{
	    
		$msghead="Error";
		$msgdata=curl_error($device_location);
		$errormsg=$msgdata.$device_location_response; ?>

		<div class="errorWide">
                <strong>ERROR:</strong><br />
                <?php  echo $errormsg ;  ?>
                </div>

	<?php }
		curl_close ($device_location);
	}
?>
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Virtual Mobile</div>
    <p>� 2011 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
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
