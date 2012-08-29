<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
    header("Content-Type: text/html; charset=ISO-8859-1");
    session_start();
    include ("config.php");
	error_reporting(0);

function GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode){

  // **********************************************************************
  // ** code to get access token by passing auth code, client ID and
  // ** client secret
  // **********************************************************************

  //Form URL to get the access token
  $accessTok_Url = "$FQDN/oauth/access_token";
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
  //$currentTime=time();
   /*If URL invocation is successful fetch the access token and store it in session,
   else display the error.
  */
  if($responseCode==200)
    {
      $jsonObj = json_decode($accessTok_response);
      $access_token = $jsonObj->{'access_token'};//fetch the access token from the response.
      $_SESSION["dc1_mobo_access_token"]=$access_token;//store the access token in to session.

    }
  else{

        echo curl_error($accessTok);
 
  }
  curl_close ($accessTok);
  header("location:index.php");//redirect to the index page.
  exit;

}

function getAuthCode($FQDN,$api_key,$secret_key,$scope,$authorize_redirect_uri){
  //Form URL to get the authorization code
  $authorizeUrl = $FQDN."/oauth/authorize";
  $authorizeUrl .= "?scope=".$scope;
  $authorizeUrl .= "&client_id=".$api_key;
  $authorizeUrl .= "&redirect_uri=".$authorize_redirect_uri;

  header("Location: $authorizeUrl");
}
$messageTextBox = $_SESSION["messageTextBox"];
$subjectTextBox = $_SESSION["subjectTextBox"];
$addresses = $_SESSION["phoneTextBox"];



/* Extract the session variables */  
$access_token = $_SESSION["dc1_mobo_access_token"];

if ($_REQUEST["sendMessageButton"]) {


  $_SESSION["dc1_mobo"] = true;


  $messageTextBox=$_POST['messageTextBox'];
  $_SESSION["messageTextBox"]=$messageTextBox;
  $subjectTextBox=$_POST['subjectTextBox'];
  $_SESSION["subjectTextBox"]=$subjectTextBox;
  $addresses = $_POST['phoneTextBox'];
  $_SESSION["phoneTextBox"]=$addresses;
$FileUpload1 = $_POST['FileUpload1'];
$_SESSION["FileUpload1"] = $_POST['FileUpload1'];
$FileUpload2 = $_POST['FileUpload2'];
$_SESSION["FileUpload2"] = $FileUpload2;
$FileUpload3 = $_REQUEST["FileUpload3"];
$_SESSION["FileUpload3"] = $FileUpload3;
$FileUpload4 = $_REQUEST["FileUpload4"];
$_SESSION["FileUpload4"] = $FileUpload4;
$FileUpload5 = $_REQUEST["FileUpload5"];
$_SESSION["FileUpload5"] = $FileUpload5;

  
 
 }  

  
if ($_SESSION["dc1_mobo"]) {
  
  if($access_token == null || $access_token == '') {
    $authCode = $_GET["code"];
    if ($authCode == null || $authCode == "") {   
      getAuthCode( $FQDN,$api_key,$secret_key,$scope,$authorize_redirect_uri);
    }else{
      $access_token = GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode);
      $_SESSION["dc1_mobo_access_token"] =  $access_token;
    }
  }
 }
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample Mobo Application 1 &#8211; Basic Mobo Service Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type"/>
    <link rel="stylesheet" type="text/css" href="style/common.css"/>
    <style type="text/css">
        .style1
        {
            font-style: normal;
            font-variant: normal;
            font-weight: bold;
            font-size: 12px;
            line-height: normal;
            font-family: Arial, Sans-serif;
            width: 92px;
        }
    </style>
    </head>
<body>
<div id="container">
<!-- open HEADER --><div id="header">
<div>
    <div id="hcLeft">Server Time:</div>
       	<div id="hcRight">
            <span id="serverTimeLabel">Wed, Apr 11, 2012 02:22:37 UTC</span>
        </div>
    </div>
<div>
    <div id="hcLeft">Client Time:</div>
	<div id="hcRight">
        <script language="JavaScript" type="text/javascript">
            var myDate = new Date();
            document.write(myDate);
        </script>
    </div>
</div>
<div>
    <div id="hcLeft">User Agent:</div>    
	<div id="hcRight">
        <script language="JavaScript" type="text/javascript">
            document.write("" + navigator.userAgent);
        </script>
    </div>
</div>

<br clear="all" />
</div><!-- close HEADER -->

<div id="wrapper">
<div id="content">

<h1>AT&T Sample Mobo Application 1 – Basic Mobo Service Application</h1>
<h2>Feature 1: Send Message</h2>

</div>
</div>
<br clear="all" />
<form method="post" action="" id="form1" enctype="multipart/form-data">
<div class="aspNetHidden">
<input type="hidden" name="__VIEWSTATE" id="__VIEWSTATE" value="/wEPDwULLTE0Njg1MzU3NjYPZBYEAgEPDxYCHgRUZXh0BR5XZWQsIEFwciAxMSwgMjAxMiAwMjoyMjozNyBVVENkZAIDDxYCHgdlbmN0eXBlBRNtdWx0aXBhcnQvZm9ybS1kYXRhZGRKqwJESS23JTte7DyE68Cg2Npvy+OhbfvIuIvYPupl3w==" />
</div>

<div class="aspNetHidden">

	<input type="hidden" name="__EVENTVALIDATION" id="__EVENTVALIDATION" value="/wEWBgLarpiPBgKE8IO1CQKqp5K5DgLBpcqdCwLS2tfyDgKd/YaEBF51lEQEv77ck+EeFQ2Pv31ApGDVJCMt+60O1qIFfQEJ" />
</div>           
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Address:</td>
    <td class="cell">
        <input name="phoneTextBox" type="text" id="phoneTextBox" style="width:291px;" />     
            </td>
  </tr> 
  <tr>
    <td valign="top" class="label">Message:</td>
    <td class="cell">
        <textarea name="messageTextBox" rows="2" cols="20" id="messageTextBox" style="height:99px;width:291px;">
</textarea></td>
  </tr>
  <tr>
    <td valign="top" class="label">Subject:</td>
    <td class="cell">
        <textarea name="subjectTextBox" rows="2" cols="20" id="subjectTextBox" style="height:99px;width:291px;">
</textarea></td>
  </tr>
  <tr>
    <td valign="top" class="label">Group:</td>
    <td class="cell">
        <input name="groupCheckBox" type="checkbox" maxlength="30" id="phoneTextBox"/></td>
  </tr>
  </tbody>
</table>

</div>

<div id="extraleft">
<div class="warning" >
<strong>WARNING:</strong><br />
total size of all attachments cannot exceed 600 KB.
</div>
</div>
<div id="extra">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td valign="bottom" class="style1">Attachment 1:</td>
    <td class="cell"><input type="file" name="FileUpload1" id="FileUpload1" />
    </td>
  </tr>
  <tr>
    <td valign="bottom" class="style1">Attachment 2:</td>
    <td class="cell"><input type="file" name="FileUpload2" id="FileUpload2" />
    </td>
  </tr>
  <tr>
    <td valign="bottom" class="style1">Attachment 3:</td>
    <td class="cell"><input type="file" name="FileUpload3" id="FileUpload3" />
    </td>
  </tr>
 <tr>
    <td valign="bottom" class="style1">Attachment 4:</td>
    <td class="cell"><input type="file" name="FileUpload4" id="FileUpload4" />
    </td>
  </tr>
  <tr>
    <td valign="bottom" class="style1">Attachment 5:</td>
    <td class="cell"><input type="file" name="FileUpload5" id="FileUpload5" />
    </td>
  </tr>
 
  </tbody></table>
  <table>
  <tbody>
  <tr>
  	<td>
          <input type="submit" name="sendMessageButton" value="Send Message" id="sendMessageButton" /></td>
  </tr>
  </tbody></table></form>
</div>
<br clear="all" />
<div align="center">
    <div id="sendMessagePanel" style="font-family:Calibri;font-size:XX-Small;">
	
    
</div></div>
<br clear="all" />
<?php


if ($_SESSION["dc1_mobo"] && $access_token != null && $access_token != '') {

	  $_SESSION["dc1_mobo"]=false;

if(isset($_POST['groupCheckBox'])) {
$group = "true";
} else{
   $group = "false";
}


if($group == "true" || !empty($_FILES['FileUpload1']['name']) || !empty($_FILES['FileUpload2']['name']) || !empty($_FILES['FileUpload3']['name']) || !empty($_FILES['FileUpload4']['name']) || !empty($_FILES['FileUpload5']['name'])) {

if($messageTextBox == null) {
     ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo "Specify message to be sent";  ?>
	    </div>
<?php
} else {

	//$addresses = $_POST['phoneTextBox'];      
      $addresses_url="";
      $addresses_array=explode(",",$_SESSION["phoneTextBox"]);
      $invalid_addresses = array();
      $addresses_second = array();
      foreach($addresses_array as $address){
	$clean_addres =  str_replace("-","",$address);
	$clean_addres =  str_replace("tel:","",$clean_addres);
	$clean_addres =  str_replace("+1","",$clean_addres);
	if(preg_match("/\d{10}/",$clean_addres)){
           $addresses_url.="Addresses=".urlencode("tel:".$clean_addres)."&";
            array_push($addresses_second,$clean_addres);                       
	   }else if (preg_match("/^[^@]*@[^@]*\.[^@]*$/", $clean_addres)) {
           $addresses_url.="Addresses=".urlencode($clean_addres)."&";
           array_push($addresses_second,$clean_addres); 
	   }else{
	  array_push($invalid_addresses,$address);
	}
      }

$addresses_url = substr($addresses_url, 0, -1);
$addresslength = count($addresses_second);

if($group == "true" && $addresslength == 1) {
     ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo "Specify more than one address for Group message.";  ?>
	    </div>
<?php
}else{
      if ( $addresses_url != "" ){
     
    
      //$subjectTextBox = $_POST['subjectTextBox'];
	  

      $server=substr($FQDN,8);
      
      $fileContents = join("", file($_FILES['file']['tmp_name']));
       //$fileContents = join("", file($_SESSION["FileUpload1"]));

       
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
  
      $startpart = "<startpart>";
     
      //Form the first part of MIME body containing address, subject in urlencided format
      $sendMMSData = $addresses_url.'&Subject='.urlencode($_SESSION["subjectTextBox"]).'&Text='.urlencode($_SESSION["messageTextBox"]).'&Group='.$group;

      //Form the MIME part with MIME message headers and MIME attachment
      $data = "";
      $data .= "--$boundary\r\n";
      $data .= "Content-Type: application/x-www-form-urlencoded; charset=UTF-8\r\nContent-Transfer-Encoding: 8bit\r\nContent-Disposition: form-data; name=\"root-fields\"\r\nContent-ID: ".$startpart."\r\n\r\n".$sendMMSData."\r\n\r\n";


      foreach ( $_FILES as $file ){
	if ($file['name'] != ""){
	  $data .= "--$boundary\r\n";
	  $data .= "Content-Disposition: form-data; name=\"file0\"; filename=\"".$file['name']."\"\r\n";//Attachment; Filename=\"".$file['name']."\"\r\n";
	  $data .= "Content-Type:".$file['type']."\r\n";
	  $data .= "Content-ID:<".$file['name'].">\r\n";
	  $data .= "Content-Transfer-Encoding:binary\r\n\r\n";
	  $data .= join("", file($file['tmp_name']))."\r\n";
	}
      }
      $data .= "--$boundary--\r\n";

      // Form the HTTP headers
      $header = "POST $FQDN/rest/1/MyMessages? HTTP/1.0\r\n";
      $header .= "Authorization: BEARER ".$_SESSION["dc1_mobo_access_token"]."\r\n"; 
      $header .= "Content-Type: multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"$startpart\"; boundary=\"$boundary\"\r\n";
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
           //echo $joinString;
           $id = substr($joinString, 11, -6);
           

	    ?>

	      <div class="success">
		 <strong>SUCCESS:</strong><br />
		 <strong>Message ID</strong> <?php echo $id; ?>
		 </div>
<?php }else {
	    
	    //print "The Request was Not Successful";
	    ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo $sendMMS_response  ?>
	    </div>

	<?php }
}
}
}
}else {



//$addresses = $_POST['phoneTextBox'];
//$_SESSION["phoneTextBox"] = $addresses;      
      $addresses_url="";
      $addresses_array=explode(",",$_SESSION["phoneTextBox"]);
      $invalid_addresses = array();
      $addresses_second = array();
      foreach($addresses_array as $address){
	$clean_addres =  str_replace("-","",$address);
	$clean_addres =  str_replace("tel:","",$clean_addres);
	$clean_addres =  str_replace("+1","",$clean_addres);
	if(preg_match("/\d{10}/",$clean_addres)){
           $addresses_url.="Addresses=".urlencode("tel:".$clean_addres)."&";
                                   
	     
	   }
          else if (preg_match("/^[^@]*@[^@]*\.[^@]*$/", $clean_addres)) {
           $addresses_url.="Addresses=".urlencode($clean_addres)."&";
                                   
	     
	   }
           else if(preg_match("/\d[3-8]/",$clean_addres)){
           $addresses_url.="Addresses=".urlencode("short:".$clean_addres)."&";
                                   
	   }else{
	  array_push($invalid_addresses,$address);
	}
      }

      if ( $addresses_url != "" ){
     
    
      //$subjectTextBox = $_POST['subjectTextBox'];



// Form the URL to send SMS
        $moboSMS_RequestBody = $addresses_url.'Subject='.urlencode($_SESSION["subjectTextBox"]).'&Text='.urlencode($_SESSION["messageTextBox"]).'&Group=false'; 

      $moboSMS_Url = "$FQDN/rest/1/MyMessages?";
	  $authorization = 'Authorization: Bearer '.$_SESSION["dc1_mobo_access_token"];
	 $content = "Content-Type: application/x-www-form-urlencoded";

      

	//Invoke the URL
	$moboSMS = curl_init();
	
	curl_setopt($moboSMS, CURLOPT_URL, $moboSMS_Url);
	curl_setopt($moboSMS, CURLOPT_POST, 1);
	curl_setopt($moboSMS, CURLOPT_HEADER, 0);
	curl_setopt($moboSMS, CURLINFO_HEADER_OUT, 0);
	curl_setopt($moboSMS, CURLOPT_HTTPHEADER, array($authorization, $content));
	curl_setopt($moboSMS, CURLOPT_POSTFIELDS, $moboSMS_RequestBody);
	curl_setopt($moboSMS, CURLOPT_RETURNTRANSFER, 1);
	curl_setopt($moboSMS, CURLOPT_SSL_VERIFYPEER, false);
	
	
	$moboSMS_response = curl_exec($moboSMS);
	$responseCode=curl_getinfo($moboSMS,CURLINFO_HTTP_CODE);

        /*
	  If URL invocation is successful print success msg along with sms ID,
	  else print the error msg
	*/
	if($responseCode==200 || $responseCode ==201 || $responseCode==300)
	{
		$jsonObj = json_decode($moboSMS_response);
		$smsID = $jsonObj->{'Id'};//if the SMS send successfully ,then will get a SMS id.
		$_SESSION["sms1_smsID"] = $smsID;
?>
         <div class="success">
		 <strong>SUCCESS:</strong><br />
		 <strong>Message ID</strong> <?php echo $jsonObj->{'Id'}; ?>

		 </div>
<?php

		}else {
	    
	    //print "The Request was Not Successful";
	    ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo $moboSMS_response  ?>
	    </div>

	<?php }

}

if(!empty($invalid_addresses )){
	
	?>
	<div class="errorWide">
	<strong>ERROR: Invalid numbers</strong><br />
	<?php 
	foreach ( $invalid_addresses as $invalid_address ){
	  echo $invalid_address."<br/>";
	}  


?> </div> <?php 
	} 
}
}





	  
	  



?>

<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p>© 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>

</body></html>
