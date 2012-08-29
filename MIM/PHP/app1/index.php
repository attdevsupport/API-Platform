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
      $_SESSION["dc1_mim_access_token"]=$access_token;//store the access token in to session.

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
$headerCntTextBox = $_SESSION["headerCntTextBox"];
$indexCrsrTextBox = $_SESSION["indexCrsrTextBox"];



/* Extract the session variables */  
$access_token = $_SESSION["dc1_mim_access_token"];

if ($_REQUEST["getMsgHeadersButton"]) {
  $_SESSION["dc1_mim"] = true;

  $headerCntTextBox=$_POST['headerCntTextBox'];
  $_SESSION["headerCntTextBox"]=$headerCntTextBox;
  $indexCrsrTextBox=$_POST['indexCrsrTextBox'];
  $_SESSION["indexCrsrTextBox"]=$indexCrsrTextBox;

  
 
 }  

  
if ($_SESSION["dc1_mim"]) {
  
  if($access_token == null || $access_token == '') {
    $authCode = $_GET["code"];
    if ($authCode == null || $authCode == "") {   
      getAuthCode( $FQDN,$api_key,$secret_key,$scope,$authorize_redirect_uri);
    }else{
      $access_token = GetAccessToken($FQDN,$api_key,$secret_key,$scope,$authCode);
      $_SESSION["dc1_mim_access_token"] =  $access_token;
    }
  }
 }

?>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample Mim Application 1 &#8211; Basic Mim Service Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type"/>
    <link rel="stylesheet" type="text/css" href="style/common.css"/>
    <style type="text/css">
        .style2
        {
            width: 491px;
        }
        #Submit1
        {
            width: 213px;
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

<h1>AT&T Sample Mim Application 1 – Basic Mim Service Application</h1>
<h2>Feature 1: Get Message Header</h2>

</div>
</div>

<br clear="all" />
<form method="post" action="" id="msgHeader">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Header Count:</td>
    <td class="cell">
        <input name="headerCntTextBox" type="text" maxlength="3" id="headerCntTextBox" style="width:70px;" />     
            </td>
  </tr> 
  <tr>
    <td width="20%" valign="top" class="label">Index Cursor:</td>
    <td class="cell">
        <input name="indexCrsrTextBox" type="text" maxlength="30" id="indexCrsrTextBox" style="width:291px;" />     
            </td>
  </tr> 
  </tbody>
</table>
<br clear="all" />
</div>
<div id="extraleft">
    <div class="warning">
        <strong>Information:</strong>
        Header Count is mandatory(1-500) and Index cursor is optional. To Use MIM, mobile number should be registered at messages.att.net
    </div>
</div>

<div id="extra">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell">
      <input type="submit" name="getMsgHeadersButton" value="Get Message Headers" id="Submit1" /></td>
    </td>
  </tr>
  </tbody>
  </table>
</div>
</form>

<br clear="all" />
<div align="center">
    <div id="sendMessagePanel" style="font-family:Calibri;font-size:XX-Small;">
	
    
</div></div>
<br clear="all" />

<?php
if ($_SESSION["dc1_mim"] && $access_token != null && $access_token != '') {

if($_SESSION["headerCntTextBox"] > 500) {
     ?>
	    <div class="errorWide">
	    <strong>ERROR:</strong><br />
	    <?php echo "HeaderCount must be between 1-500";  ?>
	    </div>
<?php
}else{

	  $_SESSION["dc1_mim"]=false;


$mim_Url = $FQDN."/rest/1/MyMessages?HeaderCount=".$_SESSION["headerCntTextBox"];
	  $authorization = 'Authorization: Bearer '.$_SESSION["dc1_mim_access_token"];
	  $content_type = 'Content-Type: application/json';
	  $accept = 'Accept: application/json';
	
	


	$mim = curl_init();

	curl_setopt($mim, CURLOPT_URL,$mim_Url);
	curl_setopt($mim, CURLOPT_RETURNTRANSFER,true);
	curl_setopt($mim, CURLOPT_HTTPGET, true);
	curl_setopt($mim, CURLINFO_HEADER_OUT, 1);
	curl_setopt($mim, CURLOPT_SSL_VERIFYPEER,false);
	curl_setopt($mim, CURLOPT_SSL_VERIFYHOST,false);
	curl_setopt($mim, CURLOPT_HTTPHEADER, array($authorization, $content_type, $accept));
	
        	$mim_response = curl_exec($mim);
	
$toarray = array();
	$responseCode=curl_getinfo($mim,CURLINFO_HTTP_CODE);
if($responseCode == '200') {
$jsonresponse = json_decode($mim_response);
$indexCrsrTextBoxresponse = $jsonresponse->MessageHeadersList->IndexCursor;

?>
<div class="successWide">
<strong>SUCCESS</strong><br />
</div>
<div class="content" align="left">
            <div id="pnlHeader">
                <table>
                    <tr>
                        <td width="10%" valign="left" class="label">
                            Header Count:
                        </td>
                        <td class="cell" align="left">
                            <span id="lblHeaderCount" class="label"><?php echo $headerCntTextBox ; ?></span>
                        </td>
                    </tr>
                    <tr>
                        <td width="10%" valign="left" class="label">
                            Index Cursor:
                        </td>
                        <td class="cell" align="left">
                            <span id="lblIndexCursor" class="label"><?php echo $indexCrsrTextBoxresponse ; ?></span>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <div>
                                <table class="style1" cellspacing="0" cellpadding="3" rules="all" id="gvMessageHeaders"
                                    style="background-color: White; border-color: #CCCCCC; border-width: 1px; border-style: None;
                                    width: 989px; border-collapse: collapse;">
                                    <tr style="color: White; background-color: #006699; font-weight: bold;">
                                        <th scope="col" class="style3">
                                            MessageId
                                        </th>
                                        <th scope="col" class="style3">
                                            PartNumber
                                        </th>
				           <th scope="col" class="style3">
                                            ContentType
                                        </th>
					    <th scope="col" class="style3">
                                            ContentName
                                        </th>
                                        <th scope="col" class="style3">
                                            From
                                        </th>
                                        <th scope="col" class="style3">
                                            To
                                        </th>
                                        <th scope="col" class="style3">
                                            Received
                                        </th>
                                        <th scope="col" class="style3">
                                            Text
                                        </th>
                                        <th scope="col" class="style3">
                                            Favourite
                                        </th>
                                        <th scope="col" class="style3">
                                            Read
                                        </th>
                                        <th scope="col" class="style3">
                                            Type
                                        </th>
                                        <th scope="col" class="style3">
                                            Direction
                                        </th>
<?php
                                    for($i = 0; $i <= $headerCntTextBox; $i++) { ?>
                                    </tr>
                                    <tr style="color: #000066;">
                                        <td class="style3">
                                         <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->MessageId;?>   
                                        </td>
                                         <td class="style3"></td><td class="style3"></td><td class="style3"></td>
                                        <td class="style3">
                                        <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->From;?>  
                                        </td>
                                         <td class="style3">
                                          <?php for($a = 0; $a <= $headerCntTextBox; $a++) {
                                         echo $jsonresponse->MessageHeadersList->Headers[$i]->To[$a]." "; }?>
                                        </td>
                                        <td class="style3">
                                         <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->Received;?>   
                                        </td>
                                        <td class="style3">
                                        <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->Text;?>   
                                        </td>
                                        <td class="style3">
                                        <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->Favorite;?>
                                        </td>
                                        <td class="style3">
                                        <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->Read;?>
                                        </td>
                                        <td class="style3">
                                        <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->Type;?>
                                        </td>
                                        <td class="style3">
                                          <?php echo $jsonresponse->MessageHeadersList->Headers[$i]->Direction;?>  
                                        </td>
                                    </tr><?php if($jsonresponse->MessageHeadersList->Headers[$i]->MmsContent != null) {
                                         $counter = count($jsonresponse->MessageHeadersList->Headers[$i]->MmsContent); ?>
                                         <?php for($j = 0; $j <= $counter; $j++) {?>
                                         <tr style="color: #000066;">
                                    	<td class="style3"></td>
                                    	<td class="style3"><?php
                                         echo $jsonresponse->MessageHeadersList->Headers[$i]->MmsContent[$j]->PartNumber." "; ?>
                                        </td>
                                         <td class="style3"><?php
                                        echo $jsonresponse->MessageHeadersList->Headers[$i]->MmsContent[$j]->ContentType." "; ?>
                                        </td>
                                        <td class="style3"><?php
                                         echo $jsonresponse->MessageHeadersList->Headers[$i]->MmsContent[$j]->ContentName." "; ?>

                                        </td><td class="style3"></td><td class="style3"></td><td class="style3"></td><td class="style3"></td>
                                        <td class="style3"></td><td class="style3"></td><td class="style3"></td><td class="style3"></td>

<?php } }?>                       </tr><?php } ?>
                                </table>
                            </div>
                        </td>
                    </tr>
                </table>
            </div>
        </div>
</form>

<?php
}else{ ?><div class="errorWide">
            <strong>ERROR:</strong><br />
             <?php echo $mim_response ?><br />
            </div><?php
}


}
}

?>
<br clear="all" />
    
    <div id="wrapper">
        <div id="content">
        <br clear="all" />
        
            <h2>Feature 2: Get Message Content</h2>
        
        </div>
    </div>
<form method="post" action="" id="msgContent">
<div id="navigation">

<table border="0" width="100%">
  <tbody>
  <tr>
    <td width="20%" valign="top" class="label">Message ID:</td>
    <td class="cell">
        <input name="headerCntTextBox2" type="text" maxlength="30" id="Text1" style="width:291px;" />     
            </td>
  </tr> 
  <tr>
    <td width="20%" valign="top" class="label">Part Number:</td>
    <td class="cell">
        <input name="indexCrsrTextBox2" type="text" maxlength="30" id="Text2" style="width:291px;" />     
            </td>
  </tr> 
  </tbody>
</table>
</div>

<div id="extra">
<table border="0" width="100%">
  <tbody>
  <tr>
    <td class="cell">
      <input type="submit" name="getMsgContentButton" value="Get Message Content" id="Submit1" /></td>
    </td>
  </tr>
  </tbody>
  </table>
</div>

</form>
</div></div></div>
<br clear="all" />
<?php


if($_REQUEST["getMsgContentButton"] != null) {
$headerCntTextBox2=$_POST['headerCntTextBox2'];
  $_SESSION["headerCntTextBox2"]=$headerCntTextBox2;
  $indexCrsrTextBox2=$_POST['indexCrsrTextBox2'];
  $_SESSION["indexCrsrTextBox2"]=$indexCrsrTextBox2;



$mim2_Url = $FQDN."/rest/1/MyMessages/". $_SESSION["headerCntTextBox2"]."/".$_SESSION["indexCrsrTextBox2"];
          
	 $authorization = 'Authorization: Bearer '.$_SESSION["dc1_mim_access_token"];
	  $content_type = 'Content-Type: application/json';
	  $accept = 'Accept: application/json';
	
	


	$mim2 = curl_init();

	curl_setopt($mim2, CURLOPT_URL,$mim2_Url);
	curl_setopt($mim2, CURLOPT_RETURNTRANSFER,true);
	curl_setopt($mim2, CURLOPT_HTTPGET, true);
	curl_setopt($mim2, CURLINFO_HEADER_OUT, 1);
	curl_setopt($mim2, CURLOPT_SSL_VERIFYPEER,false);
	curl_setopt($mim2, CURLOPT_SSL_VERIFYHOST,false);
	curl_setopt($mim2, CURLOPT_HTTPHEADER, array($authorization, $content_type, $accept));
	
    	$mim2_response = curl_exec($mim2);
	$headers = curl_getinfo($mim2);
$string = $headers["content_type"];
$token = strtok($string, ";");
$content = strtok($string, "/");
$responseCode=curl_getinfo($mim2,CURLINFO_HTTP_CODE);
if($responseCode == '200') {
?>
</div></div>
        </div>
<div style="text-align: left">
  <br style="clear: both;" />
<div class="successWide">
<strong>SUCCESS:</strong><br />
<?php if($content == "TEXT") {
echo $mim2_response; } ?>
</div>
        <br />
        <br />
<br style="clear: both;" />

        <div style="text-align: center">
<?php if($token == "APPLICATION/SMIL") {?>

            <div id="smilpanel">
	
                <textarea name="TextBox1" rows="2" cols="20" id="TextBox1" disabled="disabled" class="aspNetDisabled" style="height:100px;width:500px;"><?php echo $mim2_response;?></textarea>
<?php  } if($content == "IMAGE") {
 
?>

     <div id="imagePanel" style="text-align:center;">
	<img src="data:<?php echo $token; ?>;base64,<?php echo base64_encode($mim2_response); ?>" />
      </div>
<?php
}
}else {?><div class="errorWide">
            <strong>ERROR:</strong><br />
             <?php echo $mim2_response ?><br />
            </div><?php

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
