<?php
    header("Content-Type: text/html; charset=ISO-8859-1");
    session_start();
    include ("config.php");

function GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode){

  // **********************************************************************
  // ** code to get access token by passing auth code, client ID and
  // ** client secret
  // **********************************************************************

  //Form URL to get the access token
  $accessTok_Url = "$FQDN/oauth/token";
  //http header values
  $accessTok_headers = array(
			     'Content-Type: application/x-www-form-urlencoded'
			     );
            

  //Invoke the URL
  $post_data="client_id=".$api_key."&client_secret=".$secret_key."&code=".$authCode."&grant_type=authorization_code";

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
  /*
   If URL invocation is successful fetch the access token and store it in session,
   else display the error.
  */
  if($responseCode==200)
    {
      $jsonObj = json_decode($accessTok_response);
      $accessToken = $jsonObj->{'access_token'};//fetch the access token from the response.
      $_SESSION["dc1_deviceInfo_access_token"]=$accessToken;//store the access token in to session.
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

$address=$_SESSION["dc1_address"];
if($address==null){
  $address = $default_address;
  $_SESSION["dc1_address"] = $address;
 }


/* Extract the session variables */  
$accessToken = $_SESSION["dc1_deviceInfo_access_token"];

if (!empty($_REQUEST["deviceInfo"])) {
  $_SESSION["dc1_deviceInfo"] = true;
  $address = $_POST['address'];
  if( $address != $_SESSION["dc1_address"]){ // new address entered, need to request new auth code and token 
    $accessToken = null;
    $authCode =null;
  }
  $_SESSION["dc1_address"] =  $_POST['address'];
 }  
  
if ($_SESSION["dc1_deviceInfo"]) {
  
  if($accessToken == null || $accessToken == '') {
    $authCode = $_GET["code"];
    if ($authCode == null || $authCode == "") {   
      getAuthCode( $FQDN,$api_key,$secret_key,$scope,$authorize_redirect_uri);
    }else{
      $accessToken = GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode);
      $_SESSION["dc1_deviceInfo_access_token"] =  $accessToken;
    }
  }
 } 
  
?>
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample DC Application � Get Device Capabilities Application</title>
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


<h1>AT&amp;T Sample DC Application - Get Device Capabilities Application</h1>
<h2>Feature 1: Get Device Capabilities</h2>

</div>
</div>
<form name="deviceInfo" method="post">

<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Phone:</td>
    <td class="cell"><input maxlength="16" size="12" name="address" value="<?php echo $address; ?>" style="width: 90%">
    </td>
  </tr>
  </tbody></table>
  
</div>
<div id="extra">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell"><button type="submit" name="deviceInfo" value="deviceInfo" >Get Device Capabilities</button>
    </td>
  </tr>
  </tbody></table>

</div>
<br clear="all" />
</form>

<?php
	/* Extract parmeter device Id from deviceInfo form
	   and invoke the URL to get deviceInfo along with access token
	*/
	
	if ($_SESSION["dc1_deviceInfo"] && $accessToken != null && $accessToken != '') {
	
	  $_SESSION["dc1_deviceInfo"]=false;
	  
	  $address =  str_replace("-","",$_SESSION["dc1_address"]);
	  $address =  str_replace("tel:","",$address);
	  $address =  str_replace("+1","",$address);
	  $address = "tel:" . $address;

	  // Form the URL to get deviceInfo
	  $device_info_Url = "$FQDN/1/devices/".$address."/info";
	  $device_info_Url .= "?access_token=".$accessToken;
	  
	  $device_info_headers = array(
				       'Content-Type: application/x-www-form-urlencoded'
				       );
	//Invoke the URL
	$device_info = curl_init();
	curl_setopt($device_info, CURLOPT_URL, $device_info_Url);
	curl_setopt($device_info, CURLOPT_HTTPGET, 1);
	curl_setopt($device_info, CURLOPT_HEADER, 0);
	curl_setopt($device_info, CURLINFO_HEADER_OUT, 0);
	curl_setopt($device_info, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($device_info, CURLOPT_SSL_VERIFYPEER, false);
	$device_info_response = curl_exec($device_info);
	$responseCode=curl_getinfo($device_info,CURLINFO_HTTP_CODE);
	
       /*
	  If URL invocation is successful fetch the device Information and display,
	  else display the error.
	*/
        if($responseCode>=200 && $responseCode<=300)
        {
	  $jsonObj = json_decode($device_info_response,true); //decode the response and display it.
	   ?>
	    <div class="successWide">
	       <strong>SUCCESS:</strong><br />
	       Device parameters listed below.
	       </div>
	       <br />

	       <div align="center">
	       <table width="500" cellpadding="1" cellspacing="1" border="0">
	       <thead>
	       <tr>
	       <th width="50%" class="label">Parameter</th>
	       <th width="50%" class="label">Value</th>
	       </tr>
	       </thead>
	       <tbody>
   <?php
	       foreach ( $jsonObj["deviceId"] as $key => $value){
?>
    	<tr>
         <td class="cell" align="center"><em><?php echo $key; ?></em></td>
          <td class="cell" align="center"><em><?php echo $value; ?></em></td>
       </tr>
<?php }
	  foreach ( $jsonObj["capabilities"] as $key => $value){
	    ?>
	    <tr>
	      <td class="cell" align="center"><em><?php echo $key; ?></em></td>
              <td class="cell" align="center"><em><?php echo $value; ?></em></td>
	    </tr>
<?php } ?>
                    </tbody>
                </table>
                </div>
                
                <br />

       <?php }
        else{
    		$msghead="Error";
		$msgdata=curl_error($device_info); 
		$errormsg=$msgdata.$device_info_response;
               ?>
		<div class="errorWide">
                <strong>ERROR:</strong><br />
                <?php  echo $errormsg ;  ?>
                </div>
       <?php }
	curl_close ($device_info);
    
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
