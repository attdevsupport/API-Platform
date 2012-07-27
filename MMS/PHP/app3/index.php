<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");
include("config.php");
?>
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
	<title>AT&T Sample MMS Application 3 - MMS Gallery Application</title>
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

<h1>AT&T Sample MMS Application 3 - MMS Gallery Application</h1>
<h2>Feature 1: Web gallery of MMS photos sent to short code</h2>

</div>
</div>
<?php
$path_is = __FILE__;
$folder = dirname($path_is);
$folder = $folder. "/MoMessages";
if(!is_dir($folder))
  {
    echo "MoMessages folder is missing ( $folder )";
    exit();
  }
$db_filename = $folder . "/". "mmslistner.db";
$messages = unserialize(file_get_contents($db_filename)); 
$count = 0;
foreach ( $messages as $message ){
$count = $count + 1;
}
?>
<br />
<br />
<p>Photos sent to short code <?php echo $short_code; ?>:<?php echo $count; ?></p>

<div id="gallerywrapper">

<?php
$db_filename = $folder . "/". "mmslistner.db";
$messages = unserialize(file_get_contents($db_filename)); 
$count = 0;
foreach ( $messages as $message ){
  $message_txt =  file_get_contents( $folder.'/'.$message["text"]);
  $message_image =  "MoMessages/".$message['image'];
  $address = $message['address'];
$count = $count + 1;
  ?>
    <div id="gallery"><img src="<?php echo $message_image ; ?>" width="150" border="0"  /><br /><strong>Sent from:</strong> <?php echo $address; ?> <br /><strong>On:</strong> <?php echo $message['date']; ?><div><?php echo $message_txt; ?></div></div>

<?php  
																													}

        ?>
</div>
<br clear="all" />

<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p> © 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
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
