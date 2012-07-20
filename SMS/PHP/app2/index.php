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
$accessToken = '82dbe2315d77da8e6ddd22e7fd4352e'; 

function RefreshToken($FQDN,$api_key,$secret_key,$scope,$fullToken){

  $refreshToken=$fullToken["refreshToken"];
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
  curl_setopt($accessTok, CURLOPT_PROXY, $proxy);
  curl_setopt($accessTok, CURLOPT_PROXYPORT, "8080");
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
$proxy = "http://proxy.entp.attws.com";
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
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
<title>AT&amp;T Sample SMS Application . SMS app 2 . Voting</title>
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

  <h1>AT&amp;T Sample SMS Application - SMS app 2 - Voting</h1>
<h2>Feature 1: Calculate Votes sent via SMS to <?php echo $short_code ?> with text "Football", "Basketball", or "Baseball"</h2>

</div>
</div>
<div id="navigation">
  <br /><br />

	<form name="getReceivedSms" method="post">

<?php
        /*
	  If Receive SMS request is submitted, then invoke the URL to get the inbox messages
	  by using the registrationID i.e. short code, along with the access token.
	*/
	$path_is = __FILE__;
        $folder = dirname ($path_is);
        $folder = $folder . "/" . "tally";
        if (!is_dir($folder))
        {
           echo "$folder tally folder is missing";
        }
        else
        {
           $footBallFile = $folder . "/tally1.txt";
           $baseBallFile = $folder . "/tally2.txt";
           $basketBallFile = $folder . "/tally3.txt";
           if (!is_file($footBallFile) || !is_file($baseBallFile) || !is_file($basketBallFile))
           {
              echo "Missing tally files, make sure tally1.txt, tally2.txt and tally3.txt are present in $folder";
           }
           else
           {
             if (!$footBallFileHandle = fopen($footBallFile,'r'))
             {
                echo "Unable to open football tally file";
             }
             if (!$baseBallFileHandle = fopen($baseBallFile,'r'))
             {
                echo "Unable to open baseball tally file";
             }
             if (!$basketBallFileHandle = fopen($basketBallFile,'r'))
             {
                echo "Unable to open basketball tally file";
             }
             $footBallTotalCount = fgets($footBallFileHandle);
             $baseBallTotalCount = fgets($baseBallFileHandle);
             $basketBallTotalCount = fgets($basketBallFileHandle);
             fclose($footBallFileHandle);
             fclose($baseBallFileHandle);
             fclose($basketBallFileHandle);
	     /*if (!empty($_REQUEST["getReceivedSms"] )) {

	       $fullToken["accessToken"]=$accessToken;
	       $fullToken["refreshToken"]=$refreshToken;
	       $fullToken["refreshTime"]=$refreshTime;
	       $fullToken["updateTime"]=$updateTime;

	       $fullToken=check_token($FQDN,$api_key,$secret_key,$scope,$fullToken,$oauth_file);
	       $accessToken=$fullToken["accessToken"];

	       $receiveSMS_Url = "$FQDN/rest/sms/2/messaging/inbox?access_token=".$accessToken."&RegistrationID=".$short_code;
	       $receiveSMS_headers = array(
					   'Content-Type: application/x-www-form-urlencoded'
					   );
		  $proxy = "http://proxy.entp.attws.com";
	      $receiveSMS = curl_init();
	       curl_setopt($receiveSMS, CURLOPT_URL, $receiveSMS_Url);
	       curl_setopt($receiveSMS, CURLOPT_HTTPGET, 1);
	       curl_setopt($receiveSMS, CURLOPT_HEADER, 0);
	       curl_setopt($receiveSMS, CURLINFO_HEADER_OUT, 0);
	       curl_setopt($receiveSMS, CURLOPT_HTTPHEADER, $receiveSMS_headers);
	       curl_setopt($receiveSMS, CURLOPT_RETURNTRANSFER, 1);
	       curl_setopt($receiveSMS, CURLOPT_SSL_VERIFYPEER, false);

	       $receiveSMS_response = curl_exec($receiveSMS);
	      $responseCode=curl_getinfo($receiveSMS,CURLINFO_HTTP_CODE);
       /*
	  If URL invocation is successful fetch all the received sms,else display the error.
	*/
             /*if($responseCode==200 || $responseCode==300)
             {
	               	print "Receive SMS Messages : <br/>";
		//decode the response and display the messages.
		$jsonObj = json_decode($receiveSMS_response,true);
		$smsMsgList = $jsonObj['InboundSmsMessageList'];
		$noOfReceivedSMSMsg = $smsMsgList['NumberOfMessagesInThisBatch'];
		$totalVotes = $noOfReceivedSMSMsg + $footBallTotalCount +  $baseBallTotalCount + $basketBallTotalCount;
		?>
		     <div class="success">
		     <strong>SUCCESS:</strong><br />
			<strong>Total votes:</strong> <?php echo $totalVotes ; ?>
		     </div>
		     

		     <?php
                $invalidMsg=false;
		if ($noOfReceivedSMSMsg == 0) {
		} else {
		    foreach($smsMsgList["InboundSmsMessage"] as $smsTag=>$val) {
                      if(strtolower($val["Message"])=="football")
                        {
                            $footBallTotalCount += 1;
                        }
                        elseif(strtolower($val["Message"])=="baseball")
                        {
                            $baseBallTotalCount += 1;
                        }
                        elseif(strtolower($val["Message"])=="basketball")
                        {
                            $basketBallTotalCount += 1;
                        }
                        else{
                            $invalidMsg=true;
                        }
		    }
                    $footBallFileHandle = fopen($footBallFile,'w');
                    $baseBallFileHandle = fopen($baseBallFile,'w');
                    $basketBallFileHandle = fopen($basketBallFile,'w');
                    fputs($footBallFileHandle,$footBallTotalCount);
                    fputs($baseBallFileHandle, $baseBallTotalCount);
                    fputs($basketBallFileHandle, $basketBallTotalCount);
                    fclose($footBallFileHandle);
                    fclose($baseBallBallFileHandle);
                    fclose($basketBallFileHandle);
                } 
             } else{
		$msghead="Error";
		$msgdata=curl_error($receiveSMS);
		$errormsg= $msgdata.$receiveSMS_response;
		?>
                <div class="errorWide">
                <strong>ERROR:</strong><br />
                <?php echo $errormsg  ?>
                </div>
        <?php }
	//curl_close ($receiveSMS);
     }
      }
      }*/
	  
	  $path_is = __FILE__;
$folder = dirname($path_is);
$folder = $folder. "/tally";
if(!is_dir($folder))
  {
    echo "MoMessages folder is missing ( $folder )";
    exit();
  }
$db_filename = $folder . "/". "smslistner.db";
$messages = file_get_contents($db_filename); 

foreach ( $messages as $message ){
  $message_txt =  file_get_contents( $folder.'/'.$message["text"]);
  $address = $message['address'];
  ?>
    <div id width="150" border="0"  /><br /><strong>Sent from:</strong> <?php echo $address; ?> <br /><strong>On:</strong> <?php echo $message['date']; ?><div><?php echo $message_txt; ?></div></div>

<?php  
																													}
}}
 ?>
<table style="width: 300px" cellpadding="1" cellspacing="1" border="0">
<thead>
	<tr>
    	<th style="width: 125px" class="cell"><strong>Favorite Sport</strong></th>
        <th style="width: 125px" class="cell"><strong>Number of Votes</strong></th>
	</tr>
</thead>
<tbody>
	<tr>
        <td align="center" class="cell">Football</td>
        <td align="center" class="cell"><?php echo $footBallTotalCount;?></td>
    </tr>
	<tr>
        <td align="center" class="cell">Baseball</td>
        <td align="center" class="cell"><?php echo $baseBallTotalCount;?></td>
    </tr>
	<tr>
        <td align="center" class="cell">Basketball</td>
        <td align="center" class="cell"><?php echo $basketBallTotalCount;?></td>
    </tr>
</tbody>
</table>
</div>
<div id="extra">


	  <table>
	<tbody>
	<tr>
  	<td><br /><br /><br /><br /><br /><br /><br /><br /><button type="submit" name="getReceivedSms" value="Update" >Update Vote Totals</button></td>
	</tr>
	</tbody>
	</table>
</div>
<br clear="all" />
<div align="center"></div>
</div>
 </form>
<?php
if($invalidMsg)
{
    ?>
    <table border="1" bgcolor="#ff3300">
        <tr><td>Invalid Vote Text</td><td>Sender Address</td></tr>
        <?php
        foreach($smsMsgList["InboundSmsMessage"] as $smsTag=>$val) {
            if((strtolower($val["Message"])!="football") && (strtolower($val["Message"])!="baseball") && (strtolower($val["Message"])!="basketball")){
                ?>
                <tr><td><?php echo $val["Message"]; ?></td><td><?php echo $val["SenderAddress"]; ?></td></tr>
                <?php
            }
        }
        ?>
    </table>
    <?php
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


