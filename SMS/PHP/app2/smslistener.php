<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' September 2011
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2011 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
   -->

   <?php
$path_is = __FILE__;
$folder = dirname($path_is);
$folder = $folder . "/" . "tally";
if(!is_dir($folder))
  {
    echo "tally  folder is missing";
    exit();
  }
$db_filename = $folder . "/". "smslistner.db";
$db1_filename = $folder . "/". "smslistner2.db";
$db2_filename = $folder . "/". "counter.txt";
$post_body = file_get_contents('php://input');
$jsonresponse = json_decode($post_body, true);
$senderaddress = $jsonresponse["SenderAddress"];
$datetime = $jsonresponse["DateTime"];
$destinationaddress = $jsonresponse["DestinationAddress"];
$messageId = $jsonresponse["MessageId"];
$message = $jsonresponse["Message"];
$votes = array();

if ( file_exists( $db1_filename) ){
            $voters = unserialize(file_get_contents($db1_filename));
            array_push($votes,$jsonresponse);
            $fp = fopen($db1_filename, 'w') or die("I could not open $db1_filename.");
            fwrite($fp, serialize($votes));
           
            
            
           
   }

$fp = fopen($db_filename, 'w+') or die("I could not open $filename.");
fwrite($fp, $post_body);
fclose($fp);
//$fp = fopen($db1_filename, 'a+') or die("I could not open $filename.");
//fwrite($fp, serialize($votes));
//fclose($fp);
//print_r($messages);


?>


