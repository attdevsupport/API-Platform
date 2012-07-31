<!-- 
Licensed by AT&T under 'Software Development Kit Tools Agreement.'2012
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
For more information contact developer.support@att.com
-->

<?php
header("Content-Type: text/html; charset=ISO-8859-1");
include ("config.php");
include ($oauth_file);

session_start();
$getReceivedSms = $_REQUEST["getReceivedSms"];   
?>
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
<title>AT&T Sample SMS Application - SMS app 2 - Voting</title>
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

  <h1>AT&T Sample SMS Application - SMS app 2 - Voting</h1>
<h2>Feature 1: Calculate Votes sent via SMS to <?php echo $short_code ?> with text "Football", "Basketball", or "Baseball"</h2>

</div>
</div>
<div id="navigation">
  <br /><br />

	<form name="getReceivedSms" method="post">

<?php
      
	$path_is = __FILE__;
        $folder = dirname ($path_is);
        $folder = $folder . "/" . "tally";
        $db4_filename = $folder . "/". "smslistner.db";
        $db3_filename = $folder . "/". "smslistner2.db";     
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


	       //        	print "Receive SMS Messages : <br/>";
                $invalidMsg=false;
		
		   $responses = unserialize(file_get_contents($db3_filename));
			foreach($responses as $response) {
 
                      if(strtolower($response["Message"])=="football")
                        {
                            $footBallTotalCount += 1;
                            $validmsg = true;
                            $totalVotes+= 1;
                        }
                        elseif(strtolower($response["Message"])=="baseball")
                        {
                            $baseBallTotalCount += 1;
                            $validmsg = true;
				$totalVotes+= 1;
                        }
                        elseif(strtolower($response["Message"])=="basketball")
                        {
                            $basketBallTotalCount += 1;
                            $validmsg = true;
				$totalVotes+= 1;
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
$totalVotes = $footBallTotalCount +  $baseBallTotalCount + $basketBallTotalCount;
 
	
      }  
?>

		     <div class="success">
		     <strong>SUCCESS:</strong><br />
			<strong>Total votes:</strong> <?php echo $totalVotes ; ?>
		     </div>
		     

		     

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
	</table> </form>
<?php
	if($getReceivedSms != null) {

 ?>
</div>
<br clear="all" />
<div align="center">
</div>

<?php
 if($invalidMsg)
{

        foreach($responses as $response) {
            if((strtolower($response["Message"])!="football") && (strtolower($response["Message"])!="baseball") && (strtolower($response["Message"])!="basketball")){
                ?>
                <table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
<th class="cell" align="left"><strong>DateTime</strong></th>
        
<th class="cell" align="left"><strong>SenderAddress</strong></th>
<th class="cell" align="left"><strong>Message</strong></th>
<th class="cell" align="left"><strong>DestinationAddress</strong></th>      
<th class="cell" align="left"><strong>MessageId</strong></th>



</td>
	</tr>
</thead>
<tbody>
<tr>
                        <td class="cell" align="left" style="background: #fcc">
	                  <?php echo $response["DateTime"];?>

			     </td>
                         <td class="cell" align="left" style="background: #fcc">
                         <?php echo $response["SenderAddress"];?>
</td>

                        </td>
                        <td class="cell" align="left" style="background: #fcc"><?php echo $response["Message"];?></td>
                        <td class="cell" align="left" style="background: #fcc"><?php echo $response["DestinationAddress"];?></td>
                        <td class="cell" align="left" style="background: #fcc"><?php echo $response["MessageId"];?></td>
                      </tr>  

    </table>
    <?php
} 
}
}

if($validmsg) {
foreach($responses as $response) {
            if((strtolower($response["Message"])=="football") || (strtolower($response["Message"])=="baseball") || (strtolower($response["Message"])=="basketball")){
                ?>
               
<table style="width: 750px" cellpadding="1" cellspacing="1" border="0">
<thead>
    <tr>
<th class="cell" align="left"><strong>DateTime</strong></th>
        
<th class="cell" align="left"><strong>SenderAddress</strong></th>
<th class="cell" align="left"><strong>Message</strong></th>
<th class="cell" align="left"><strong>DestinationAddress</strong></th>      
<th class="cell" align="left"><strong>MessageId</strong></th>



</td>
	</tr>
</thead>
<tbody><?php
foreach($responses as $response) {


?>
<tr>
                        <td class="cell" align="left">
	                  <?php echo $response["DateTime"];?>

			     </td>
                         <td class="cell" align="left">
                         <?php echo $response["SenderAddress"];?>
</td>

                        </td>
                        <td class="cell" align="left"><?php echo $response["Message"];?></td>
                        <td class="cell" align="left"><?php echo $response["DestinationAddress"];?></td>
                        <td class="cell" align="left"><?php echo $response["MessageId"];?></td>
                      </tr>
</table>  
<?php 

 }
 } }
} }
foreach($responses as $response) {
 $fp = fopen($db3_filename, 'w+') or die("I could not open $db3_filename.");
            unset($response["DateTime"], $response["SenderAddress"], $response["Message"], $response["DestinationAddress"], $response["MessageId"]);}
?></div>
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p> 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
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
