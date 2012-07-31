<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
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
$db9_filename = $folder . "/". "subscriptionlistener.txt";
$post_body = file_get_contents('php://input');

$fp = fopen($db9_filename, 'w+') or die("I could not open $filename.");
fwrite($fp, $post_body);
fclose($fp);
//print_r($messages);

?>



