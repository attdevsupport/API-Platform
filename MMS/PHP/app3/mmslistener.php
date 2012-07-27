<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.'2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2011 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
   -->

   <?php
$path_is = __FILE__;
$folder = dirname($path_is);
$folder = $folder. "/MoMessages";
if(!is_dir($folder))
  {
    echo "MoMessages folder is missing";
    exit();
  }
$db_filename = $folder . "/". "mmslistner.db";
$post_body = file_get_contents('php://input');
//$post_body = file_get_contents( "full_message3.mm");

if ( file_exists( $db_filename) ){
  $messages = unserialize(file_get_contents($db_filename)); 
 }else{
  $messages = null;
 }

$local_post_body = $post_body;
$ini = strpos($local_post_body,"<SenderAddress>tel:+");
if ($ini == 0 )
  {
    exit();
  }else{
  preg_match("@<SenderAddress>tel:(.*)</SenderAddress>@i",$local_post_body,$matches);
  $message["address"] = $matches[1];
  preg_match("@<subject>(.*)</subject>@i",$local_post_body,$matches);
  $message["subject"] = $matches[1];
  $message["date"]= date("D M j G:i:s T Y");
 }

if( $messages !=null ){
  $last=end($messages);
  $message['id']=$last['id']+1;
 }else{
    $message['id'] = 0;
 }

mkdir($folder.'/'.$message['id']);

$boundaries_parts = explode("--Nokia-mm-messageHandler-BoUnDaRy",$local_post_body);

foreach ( $boundaries_parts as $mime_part ){
  if ( preg_match( "@BASE64@",$mime_part )){
    $mm_part = explode("BASE64", $mime_part );
    $filename = null;
    $content_type =null;
    if ( preg_match("@Filename=([^;^\n]+)@i",$mm_part[0],$matches)){
      $filename = trim($matches[1]);
    }
    if ( preg_match("@Content-Type:([^;^\n]+)@i",$mm_part[0],$matches)){
      $content_type = trim($matches[1]);
    }
    if ( $content_type != null ){
      if ( $filename == null ){
	preg_match("@Content-ID: ([^;^\n]+)@i",$mm_part[0],$matches);
	$filename = trim($matches[1]);    
      }
      if ( $filename != null ){
	//Save file 
	$base64_data = base64_decode($mm_part[1]);
	$full_filename = $folder.'/'.$message['id'].'/'.$filename;
	if (!$file_handle = fopen($full_filename, 'w')) {
	  echo "Cannot open file ($full_filename)";
	  exit;
	}
	fwrite($file_handle, $base64_data);
	fclose($file_handle);
	
	if ( preg_match( "@image@",$content_type ) && ( !isset($message["image"]))){
	  $message["image"]=$message['id'].'/'.$filename;
	}
	if ( preg_match( "@text@",$content_type ) && ( !isset($message["text"]))){
	  $message["text"]=$message['id'].'/'.$filename;
	}
      }
    }
  }
}

if( $messages !=null ){
  $messages_stored=array_push($messages,$message);
  if ( $messages_stored > 10 ){
    $old_message = array_shift($messages);
    // remove old message folder 
  }
 }else{
    $messages = array($message);
 }

$fp = fopen($db_filename, 'w+') or die("I could not open $filename.");
fwrite($fp, serialize($messages));
fclose($fp);
//print_r($messages);


?>
