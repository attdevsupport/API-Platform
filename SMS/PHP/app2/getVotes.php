<?php

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
	 $totalVotes = $footBallTotalCount + $baseBallTotalCount + $basketBallTotalCount;
	 fclose($footBallFileHandle);
	 fclose($baseBallFileHandle);
	 fclose($basketBallFileHandle);
	 echo "{\"totalNumberOfVotes\":".$totalVotes.", \"footballVotes\":".$footBallTotalCount.", \"baseballVotes\":".$baseBallTotalCount." , \"basketballVotes\":".$baseBallTotalCount."}";
       }
   }

?>