<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.'2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
 
$file_cont = file_get_contents( "/tmp/sample MMS delivery.txt");
//echo $file_cont;

$boundaries_parts = explode("--Nokia-mm-messageHandler-BoUnDaRy",$file_cont);

echo "-------------------------------------------------\n\n\n";

print_r( $boundaries_parts );

echo "##################################################\n\n\n";

foreach ( $boundaries_parts as $part ){
echo "##################################################\n\n\n";
  $mm_part = explode("\n\n", $part );
  //  print_r($mm_part);
  preg_match("@Filename=([^;^\n]+)@i",$mm_part[0],$matches);
  $filename = $matches[1];
  preg_match("@Content-Type:([^;^\n]+)@i",$mm_part[0],$matches);
  $content_type = $matches[1];

  if ( $filename == null ){
    preg_match("@Content-ID: ([^;^\n]+)@i",$mm_part[0],$matches);
    $filename = $matches[1];    
  }
  echo " #########".$filename." ##########".$content_type." \n\n";

}
?>