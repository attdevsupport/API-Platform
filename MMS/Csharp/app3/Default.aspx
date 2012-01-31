<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xml:lang="en" xmlns="http://www.w3.org/1999/xhtml" lang="en"><head>
    <title>AT&T Sample MMS Application 3 – MMS Gallery Application</title>
	<meta content="text/html; charset=ISO-8859-1" http-equiv="Content-Type">
    <link rel="stylesheet" type="text/css" href="../../style/common.css"/ >
    

<body>
    <form id="form1" runat="server">
<div id="container">
<!-- open HEADER --><div id="header">

<div>
    <div id="hcRight">
            <asp:Label ID="lblServerTime" runat="server"></asp:Label>
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

<h1>AT&T Sample MMS Application 3 – MMS Gallery Application</h1>
<h2>Feature 1: Web gallery of MMS photos sent to short code</h2>

</div>
</div>


<br />
<p>Photos sent Photos sent to short code <asp:Label runat="server" ID="shortCodeLabel"></asp:Label>:
    <asp:Label ID="lbl_TotalCount" runat="server"></asp:Label>
        </p>
<div id="gallerywrapper" runat="server">
</div>
<br clear="all" />
<div align="center">
    <asp:Panel runat="server" ID="messagePanel">
    </asp:Panel>
</div>
<br clear="all" />
<div id="footer">

	<div style="float: right; width: 20%; font-size: 9px; text-align: right">Powered by AT&amp;T Virtual Mobile</div>
    <p>© 2011 AT&amp;T Intellectual Property. All rights reserved.  <a href="http://developer.att.com/" target="_blank">http://developer.att.com</a>
<br>
The Application hosted on this site are working examples intended to be used for reference in creating products to consume AT&amp;T Services and  not meant to be used as part of your product.  The data in these pages is for test purposes only and intended only for use as a reference in how the services perform.
<br>
For download of tools and documentation, please go to <a href="https://devconnect-api.att.com/" target="_blank">https://devconnect-api.att.com</a>
<br>
For more information contact <a href="mailto:developer.support@att.com">developer.support@att.com</a>
    </form>

</body></html>
