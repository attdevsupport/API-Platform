//Licensed by AT&T under 'Software Development Kit Tools Agreement.' September 2011
//TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
//Copyright 2011 AT&T Intellectual Property. All rights reserved. http://developer.att.com
//For more information contact developer.support@att.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.IO;
using System.Web.Services;
using System.Text;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections.Specialized;
using System.Drawing;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail;
using System.IO;

public partial class Default : System.Web.UI.Page
{
    string shortCode, FQDN, accessTokenFilePath, oauthFlow;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    string[] shortCodes;
    string wapFilePath;
    public bool readAccessTokenFile()
    {
        try
        {
            FileStream file = new FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader sr = new StreamReader(file);
            access_token = sr.ReadLine();
            expiryMilliSeconds = sr.ReadLine();
            refresh_token = sr.ReadLine();
            lastTokenTakenTime = sr.ReadLine();
            refreshTokenExpiryTime = sr.ReadLine();
            sr.Close();
            file.Close();
        }
        catch (Exception ex)
        {
            return false;
        }
        if ((access_token == null) || (expiryMilliSeconds == null) || (refresh_token == null) || (lastTokenTakenTime == null) || (refreshTokenExpiryTime == null))
        {
            return false;
        }
        return true;
    }

    /* This function validates the expiry of the access token and refresh token,
     * function compares the current time with the refresh token taken time, if current time is greater then 
     * returns INVALID_REFRESH_TOKEN
     * function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
     * funciton returns INVALID_ACCESS_TOKEN
     * otherwise returns VALID_ACCESS_TOKEN
    */
    public string isTokenValid()
    {
        try
        {

            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            DateTime lastRefreshTokenTime = DateTime.Parse(refreshTokenExpiryTime);
            TimeSpan refreshSpan = currentServerTime.Subtract(lastRefreshTokenTime);
            if (currentServerTime >= lastRefreshTokenTime)
            {
                return "INVALID_ACCESS_TOKEN";
            }
            DateTime lastTokenTime = DateTime.Parse(lastTokenTakenTime);
            TimeSpan tokenSpan = currentServerTime.Subtract(lastTokenTime);
            if (((tokenSpan.TotalSeconds)) > Convert.ToInt32(expiryMilliSeconds))
            {
                return "REFRESH_ACCESS_TOKEN";
            }
            else
            {
                return "VALID_ACCESS_TOKEN";
            }
        }
        catch (Exception ex)
        {
            return "INVALID_ACCESS_TOKEN";
        }
    }


    /* This function get the access token based on the type parameter type values.
     * If type value is 1, access token is fetch for client credential flow
     * If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
     */
    public bool getAccessToken(int type, Panel panelParam)
    {
        /*  This is client credential flow: */
        if (type == 1)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=WAP");
                accessTokenRequest.Method = "GET";

                WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                {
                    string jsonAccessToken = accessTokenResponseStream.ReadToEnd().ToString();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));
                    access_token = deserializedJsonObj.access_token.ToString();
                    expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                    refresh_token = deserializedJsonObj.refresh_token.ToString();
                    FileStream file = new FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(file);
                    sw.WriteLine(access_token);
                    sw.WriteLine(expiryMilliSeconds);
                    sw.WriteLine(refresh_token);
                    sw.WriteLine(currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString());
                    lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                    //Refresh token valids for 24 hours
                    DateTime refreshExpiry = currentServerTime.AddHours(24);
                    refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    sw.WriteLine(refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString());
                    sw.Close();
                    file.Close();
                    // Close and clean up the StreamReader
                    accessTokenResponseStream.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(panelParam, ex.ToString());
                return false;
            }
        }
        else if (type == 2)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?grant_type=refresh_token&client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&refresh_token=" + refresh_token.ToString());
                accessTokenRequest.Method = "GET";
                WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                {
                    string access_token_json = accessTokenResponseStream.ReadToEnd().ToString();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(access_token_json, typeof(AccessTokenResponse));
                    access_token = deserializedJsonObj.access_token.ToString();
                    expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                    refresh_token = deserializedJsonObj.refresh_token.ToString();
                    FileStream file = new FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(file);
                    sw.WriteLine(access_token);
                    sw.WriteLine(expiryMilliSeconds);
                    sw.WriteLine(refresh_token);
                    sw.WriteLine(currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString());
                    lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                    //Refresh token valids for 24 hours
                    DateTime refreshExpiry = currentServerTime.AddHours(24);
                    refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    sw.WriteLine(refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString());
                    sw.Close();
                    file.Close();
                    accessTokenResponseStream.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(panelParam, ex.ToString());
                return false;
            }
        }
        return false;
    }

    /* This function is used to neglect the ssl handshake error with authentication server */

    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }
    /* This function is used to read access token file and validate the access token
     * this function returns true if access token is valid, or else false is returned
     */
    public bool readAndGetAccessToken(Panel panelParam)
    {
        bool result = true;
        if (readAccessTokenFile() == false)
        {
            result = getAccessToken(1, panelParam);
        }
        else
        {
            string tokenValidity = isTokenValid();
            if (tokenValidity.CompareTo("REFRESH_ACCESS_TOKEN") == 0)
            {
                result = getAccessToken(2, panelParam);
            }
            else if (string.Compare(isTokenValid(), "INVALID_ACCESS_TOKEN") == 0)
            {
                result = getAccessToken(1, panelParam);
            }
        }
        return result;
    }
    /*
     * On page load if query string 'code' is present, invoke get_access_token
     */
    public void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            if (ConfigurationManager.AppSettings["WAPFilePath"] != null)
            {
                wapFilePath = ConfigurationManager.AppSettings["WAPFilePath"];
            }
            else
            {
                wapFilePath = "~\\R2-csharp-dotnet\\wap\\app1\\WAPText.txt";
            }
            wapFilePath = Request.MapPath(wapFilePath);
            if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
            {
                accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
            }
            else
            {
                accessTokenFilePath = "~\\WAPApp1AccessToken.txt";
            }
            if (ConfigurationManager.AppSettings["FQDN"] == null)
            {
                drawPanelForFailure(wapPanel, "FQDN is not defined in configuration file");
                return;
            }
            FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
            if (ConfigurationManager.AppSettings["api_key"] == null)
            {
                drawPanelForFailure(wapPanel, "api_key is not defined in configuration file");
                return;
            }
            api_key = ConfigurationManager.AppSettings["api_key"].ToString();
            if (ConfigurationManager.AppSettings["secret_key"] == null)
            {
                drawPanelForFailure(wapPanel, "secret_key is not defined in configuration file");
                return;
            }
            secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
            if (ConfigurationManager.AppSettings["scope"] != null)
            {

                scope = ConfigurationManager.AppSettings["scope"].ToString();
            }
            else
            {
                scope = "WAP";
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(wapPanel, ex.ToString());
            Response.Write(ex.ToString());
        }

    }

    /* This function is called to draw the table in the panelParam panel for success response */
    private void drawPanelForSuccess(Panel panelParam, string message)
    {
        Table table = new Table();
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        table.BorderStyle = BorderStyle.Outset;
        table.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        //rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Font.Bold = true;
        rowTwoCellOne.Text = "Message ID:";
        rowTwoCellOne.Width = Unit.Pixel(70);
        //rowOneCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellOne);
        TableCell rowTwoCellTwo = new TableCell();
        rowTwoCellTwo.Text = message.ToString();
        //rowTwoCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellTwo);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.DarkGreen;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(table);
    }

    /* This function draws table for failed response in the panalParam panel */
    private void drawPanelForFailure(Panel panelParam, string message)
    {
        Table table = new Table();
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        table.BorderStyle = BorderStyle.Outset;
        table.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        //rowOneCellOne.BorderWidth = 1;
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        //rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.Red;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(table);
    }

    /* 
 * This function is called when user clicks on send wap message button.
 * this funciton calls send wap message API to send the wap message.
 */
    protected void btnSendWAP_Click(object sender, EventArgs e)
    {
        try
        {
            if (readAndGetAccessToken(wapPanel) == true)
            {
                if (access_token == null || access_token.Length <= 0)
                {
                    return;
                }
                sendWapPush();
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(wapPanel, ex.ToString());
        }
    }

    /*this function validates string against the valid msisdn */
    private Boolean isValidMISDN(string number)
    {
        string smsAddressInput = number;
        string smsAddressFormatted;
        string phoneStringPattern = "^\\d{3}-\\d{3}-\\d{4}$";
        if (System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern))
        {
            smsAddressFormatted = smsAddressInput.Replace("-", "");
        }
        else
        {
            smsAddressFormatted = smsAddressInput;
        }
        long tryParseResult = 0;
        if (smsAddressFormatted.Length == 16 && smsAddressFormatted.StartsWith("tel:+1"))
        {
            smsAddressFormatted = smsAddressFormatted.Substring(6, 10);
        }
        else if (smsAddressFormatted.Length == 15 && smsAddressFormatted.StartsWith("tel:1"))
        {
            smsAddressFormatted = smsAddressFormatted.Substring(5, 10);
        }
        else if (smsAddressFormatted.Length == 14 && smsAddressFormatted.StartsWith("tel:"))
        {
            smsAddressFormatted = smsAddressFormatted.Substring(4, 10);
        }
        else if (smsAddressFormatted.Length == 12 && smsAddressFormatted.StartsWith("+1"))
        {
            smsAddressFormatted = smsAddressFormatted.Substring(2, 10);
        }
        else if (smsAddressFormatted.Length == 11 && smsAddressFormatted.StartsWith("1"))
        {
            smsAddressFormatted = smsAddressFormatted.Substring(1, 10);
        }
        if ((smsAddressFormatted.Length != 10) || (!long.TryParse(smsAddressFormatted, out tryParseResult)))
        {
            return false;
        }
        return true;
    }

    /* This function calls send wap message api to send wap messsage */
    private void sendWapPush()
    {
        try
        {
            if (isValidMISDN(txtAddressWAPPush.Text.ToString()) == false)
            {
                drawPanelForFailure(wapPanel, "Invalid Number: " + txtAddressWAPPush.Text.ToString());
                return;
            }
            string wapAddress = txtAddressWAPPush.Text.ToString().Replace("tel:+1", "");
            wapAddress = wapAddress.ToString().Replace("tel:+", "");
            wapAddress = wapAddress.ToString().Replace("tel:1", "");
            wapAddress = wapAddress.ToString().Replace("tel:", "");
            wapAddress = wapAddress.ToString().Replace("tel:", "");
            wapAddress = wapAddress.ToString().Replace("-", "");

            string wapMessage = txtAlert.Text.ToString();
            string wapUrl = txtUrl.Text.ToString();

            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

            string wapData = "";
            wapData += "Content-Disposition: form-data; name=\"PushContent\"\n";
            wapData += "Content-Type: text/vnd.wap.si\n";
            wapData += "Content-Length: 20\n";
            wapData += "X-Wap-Application-Id: x-wap-application:wml.ua\n\n";
            wapData += "<?xml version='1.0'?>\n";
            wapData += "<!DOCTYPE si PUBLIC \"-//WAPFORUM//DTD SI 1.0//EN\" "+"\"http://www.wapforum.org/DTD/si.dtd\">\n";
            wapData += "<si>\n";
            wapData += "<indication href=\"" + wapUrl.ToString() +"\" " + "action=\"signal-medium\" si-id=\"6532\">\n";
            wapData += wapMessage.ToString();
            wapData += "\n</indication>";
            wapData += "\n</si>";

            StreamWriter wapFileWriter = File.CreateText(wapFilePath);
            wapFileWriter.Write(wapData);
            wapFileWriter.Close();

            //string filename = Path.GetFileName(wapFilePath);
            //FileStream fs = new FileStream(wapFilePath, FileMode.Open, FileAccess.Read);
            //BinaryReader br = new BinaryReader(fs);
            //byte[] pushFile = br.ReadBytes((int)fs.Length);
            //br.Close();
            //fs.Close();
            StreamReader sr = new StreamReader(wapFilePath);
            string pushFile = sr.ReadToEnd();
            sr.Close();

            //string headerTemplate = "Content-Disposition: form-data; name=\"_attachments\"; filename=\"WAPPush.txt\"\r\n Content-Type: application/octet-stream\r\n\r\n";
            HttpWebRequest wapRequestObject = (HttpWebRequest)WebRequest.Create("" + FQDN + "/1/messages/outbox/wapPush?access_token=" + access_token.ToString());
            wapRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"\"; boundary=\"" + boundary + "\"\r\n";
            wapRequestObject.Method = "POST";
            wapRequestObject.KeepAlive = true;

            string sendWapData = "address=" + Server.UrlEncode("tel:" + wapAddress.ToString()) + "&subject=" + Server.UrlEncode("Wap Message") + "&priority=High&content-type=" + Server.UrlEncode("application/xml");
            //Wap Push Data 
            string data = "";
            data += "--" + boundary + "\r\n";
            data += "Content-type: application/x-www-form-urlencoded; charset=UTF-8\r\n";
            data += "Content-Transfer-Encoding: 8bit\r\n";
            data += "Content-ID: <startpart>\r\n";
            data += "Content-Disposition: form-data; name=\"root-fields\"\r\n\r\n" + sendWapData.ToString() + "\r\n";
            data += "--" + boundary + "\r\n";
            data += "Content-Disposition: attachment; name=Push.txt\r\n\r\n";
            data += "Content-Type: text/plain\r\n";
            data += "Content-ID: <Push.txt>\r\n";
            data += "Content-Transfer-Encoding: binary\r\n";
            data += pushFile + "\r\n";
            data += "--" + boundary + "--\r\n";

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = encoding.GetBytes(data);
            wapRequestObject.ContentLength = postBytes.Length;
            using (Stream writeStream = wapRequestObject.GetRequestStream())
            {
                writeStream.Write(postBytes, 0, postBytes.Length);
                writeStream.Close();
            }

            HttpWebResponse wapResponseObject = (HttpWebResponse)wapRequestObject.GetResponse();
            using (StreamReader wapResponseStream = new StreamReader(wapResponseObject.GetResponseStream()))
            {
                string strResult = wapResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                SendWapResponse deserializedJsonObj = (SendWapResponse)deserializeJsonObject.Deserialize(strResult, typeof(SendWapResponse));
                drawPanelForSuccess(wapPanel, deserializedJsonObj.id.ToString());
                wapResponseStream.Close();
            }
            wapRequestObject = null;
            if (File.Exists(wapFilePath))
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(wapFilePath);
                fileInfo.Delete();
            }
        }
        catch (Exception ex)
        {
            if (File.Exists(wapFilePath))
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(wapFilePath);
                fileInfo.Delete();
            }
            drawPanelForFailure(wapPanel, ex.ToString());
        }
    }

}

/* Following are the data structures used for the applicaiton */
public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;

}
public class SendWapResponse
{
    public string id;
}