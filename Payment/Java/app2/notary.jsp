<% 
//Licensed by AT&T under 'Software Development Kit Tools Agreement.' 2012
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2012 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com
%>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&amp;T Sample Notary Application - Sign Payload Application</title>
    <meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="style/common.css"/ >
    <script type="text/javascript" src="js/helper.js">
</script>
<style type="text/css">
pre {
    white-space: pre;           /* CSS 2.0 */
    white-space: pre-wrap;      /* CSS 2.1 */
	white-space: pre-line;      /* CSS 3.0 */
	white-space: -pre-wrap;     /* Opera 4-6 */
	white-space: -o-pre-wrap;   /* Opera 7 */
	white-space: -moz-pre-wrap; /* Mozilla */
	white-space: -hp-pre-wrap;  /* HP Printers */
	word-wrap: break-word;      /* IE 5+ */
	}
</style>
<body>

<%@ page contentType="text/html; charset=iso-8859-1" language="java" %>
<%@ page import="org.apache.commons.httpclient.*"%>
<%@ page import="org.apache.commons.httpclient.methods.*"%>
<%@ page import="org.json.*"%>
<%@ page import="java.io.*" %>
<%@ include file="config.jsp" %>
<%
    String scope = "PAYMENT";
    String accessToken = "";
    String refreshToken = "";
    String expires_in = "";
    Long date = System.currentTimeMillis();
    String signPayload = request.getParameter("signPayload");
    String payload = request.getParameter("payload");
    if(payload==null || payload.equalsIgnoreCase("null"))
        payload = (String) session.getAttribute("payload");
    if(payload==null || payload.equalsIgnoreCase("null"))
        payload = "{\"Amount\":0.99,\n \"Category\":2,\n \"Channel\":"+
"\"MOBILE_WEB\",\n\"Description\":\"5 puzzles per month plan\",\n"+
"\"MerchantTransactionId\":\"user573transaction1377\",\n \"MerchantProductId\":\"SudokuMthlyPlan5\",\n"+
"\"MerchantApplicationId\":\"Sudoku\",\n"+
"\"MerchantPaymentRedirectUrl\":"+
"\"http://somewhere.com/OauthResponse.php\",\n"+
"\"MerchantSubscriptionIdList\":"+
"[\"p1\","+
"\"p2\",\"p3\",\"p4\",\"p5\"],\n"+
"\"IsPurchaseOnNoActiveSubscription\":false,\n"+
"\"SubscriptionRecurringNumber\": 5,\n \"SubscriptionRecurringPeriod\" : \"MONTHLY\",\n \"SubscriptionRecurringPeriodAmount\" : 1, }";
    String signedPayload = request.getParameter("signedPayload");
    if(signedPayload==null || signedPayload.equalsIgnoreCase("null"))
        signedPayload = (String) session.getAttribute("signedPayload");
    if(signedPayload==null || signedPayload.equalsIgnoreCase("null"))
        signedPayload = "Sbe gur Abgnel ncc, fvzcyr gbby. Gurer fubhyq whfg or n Erdhrfg fvqr ba gur yrsg, pbagnvavat bar YNETR grkg obk jvgu ab qrsnhyg inyhr. Guvf vf jurer gur hfre pna chg va n obql bs grkg jvgu nyy gur cnenzrgref sbe n cnlzrag genafnpgvba, ohg gurl jvyy perngr guvf grkg gurzfryirf onfrq ba gur genafnpgvba qrgnvyf. Gura gurl pyvpx gur ohggba, juvpu qvfcynlf n grkg obk ba gur evtug jvgu gur Fvtarq Cnlybnq, naq gur Fvtangher, obgu bs juvpu gur hfre fubhyq or noyr gb pbcl rnfvyl naq cnfgr vagb gur cnlzrag nccyvpngvba yngre ba. Va erny yvsr, guvf jvyy or qbar nhgbzngvpnyyl ol pbqr, ohg guvf ncc whfg arrqf gb fubj gur onfvp vagrenpgvba jvgu guvf arj Abgnel NCV, juvpu yvgrenyyl whfg gnxrf gur grkg lbh fraq, naq ergheaf gur fvtarq cnlybnq (grkg) naq gur fvtangher. V ubcr gung znxrf frafr";
    String signature = request.getParameter("signature");
    if(signature==null || signature.equalsIgnoreCase("null"))
        signature = (String) session.getAttribute("signature");
    if(signature==null || signature.equalsIgnoreCase("null"))
        signature = "hfd7adsf76asffs987sdf98fs6a7a98ff6a";
%>

<div id="container">
<!-- open HEADER --><div id="header">

<div>
    <div id="hcRight">
        <%=new java.util.Date()%>
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

<h1>AT&amp;T Sample Notary Application - Sign Payload Application</h1>

</div>
</div>

<%
    //If Sign Payload button was clicked, do this.
       if(signPayload!=null) {
session.setAttribute("payload", payload);
String url = FQDN + "/Security/Notary/Rest/1/SignedPayload";
HttpClient client = new HttpClient();
PostMethod method = new PostMethod(url);  
method.addRequestHeader("Accept","application/json");
method.addRequestHeader("client_id", clientIdAut);
method.addRequestHeader("client_secret", clientSecretAut);
method.setRequestBody(payload);
int statusCode = client.executeMethod(method); 
if(statusCode==200) {
    JSONObject jsonResponse = new JSONObject(method.getResponseBodyAsString());
    signedPayload = jsonResponse.getString("SignedDocument");
    session.setAttribute("signedPayload", signedPayload);
    signature = jsonResponse.getString("Signature");
    session.setAttribute("signature", signature);
    if(request.getParameter("return")!=null)
        response.sendRedirect(request.getParameter("return") + "?signedPayload=" + signedPayload + "&signature=" + signature);
} else {
    %><%=method.getResponseBodyAsString()%><%
}
method.releaseConnection();
}
%>

<div id="wrapper">
  <div id="content">

<h2><br />
Feature 1: Sign Payload</h2>
<br/>
</div>
</div>
<form method="post" name="signPayload">
<div id="navigation">

<table border="0" width="950px">
  <tbody>
  <tr>
    <td valign="top" class="label">Request:</td>
    <td class="cell" ><textarea rows="20" cols="60" name="payload" ><%=payload.replaceAll(",", ",\n")%></textarea>
    </td>
    <td width="50px"></td>
    <td  valign="top" class="label">Signed Payload:</td>
    <td class="cell" width="400px"><%=signedPayload.replaceAll("(.{5})", "$1 ")%></td>
  </tr>
<tr>
    <td></td>
    <td></td>
    <td width="50px"></td>
    <td valign="top" class="label">Signature:</td>
    <td class="cell"><%=signature.replaceAll("(.{5})", "$1 ")%></td>
</tr>
  <tr>
    <td></td>
    <td class="cell" align="right"><input type="button" value="Back" onclick="history.go(-1)"></td>
  </tr>
  </tbody></table>
</div>

<br clear="all" />
</form>

<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Cloud Architecture</div>
    <p>&#169; 2012 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>

</div>
</div>

</body></html>

