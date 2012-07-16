<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' June 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
   -->

   <?php
$path_is = __FILE__;
$folder = dirname($path_is);
$folder = $folder. "/Notifications";
if(!is_dir($folder))
  {
    echo "Notifications folder is missing";
    exit();
  }
$db_filename = $folder . "/". "singlepaylistener.db";
$post_body = file_get_contents('php://input');
//$post_body = file_get_contents( "full_message3.mm");

if ( file_exists( $db_filename) ){
  $notifications = unserialize(file_get_contents($db_filename)); 
 }else{
  $notifications = null;
 }

$local_post_body = $post_body;
$ini = strpos($local_post_body,"NotificationId");
if ($ini == 0 )
  {
    exit();
  }else{
  preg_match("@NotificationId(.*)<@NotificationId>@i",$local_post_body,$matches) }

if( $notifications !=null ){
  $notifications_stored=array_push($notifications,$notification);
  if ( $notifications_stored > 1000 ){
    $old_notification = array_shift($notifications);
    // remove old message folder 
  }
 }else{
    $notifications = array($notification);
 }

$fp = fopen($db_filename, 'w+') or die("I could not open $filename.");
fwrite($fp, serialize($notifications));
fclose($fp);
//print_r($messages);


?>



