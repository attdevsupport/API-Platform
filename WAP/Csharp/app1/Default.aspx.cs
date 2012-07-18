// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References

using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;

#endregion

/// <summary>
/// Access Token Types
/// </summary>
public enum AccessTokenType
{
    /// <summary>
    /// Access Token Type is based on Client Credential Mode
    /// </summary>
    Client_Credential,

    /// <summary>
    /// Access Token Type is based on Authorization Code
    /// </summary>
    Authorization_Code,

    /// <summary>
    /// Access Token Type is based on Refresh Token
    /// </summary>
    Refresh_Token
}

/// <summary>
/// WapPush_App1 application
/// </summary>
/// <remarks>
/// This application allows a user to send a WAP Push message to a mobile device, by entering the address, alert text, and URL to be sent.
/// This application uses Autonomous Client Credentials consumption model to send messages. The user enters the alert text and URL, 
/// but the application in the background must build the push.txt file to attach with the requested values.
/// </remarks>
public partial class WapPush_App1 : System.Web.UI.Page
{
    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string endPoint, accessTokenFilePath, wapFilePath;

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string apiKey, secretKey, accessToken, scope, expirySeconds, refreshToken, refreshTokenExpiryTime;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private DateTime accessTokenExpiryTime;

    #region Bypass SSL Certificate Error

    /// <summary>
    /// This method neglects the ssl handshake error with authentication server
    /// </summary>
    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    #endregion

    /// <summary>
    /// Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    public void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();

            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";

            this.ReadConfigFile();
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(wapPanel, ex.ToString());
        }
    }

    /// <summary>
    /// This function is called when user clicks on send wap message button. This funciton calls send wap message API to send the wap message
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void SendWAPButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtAddressWAPPush.Text))
            {
                this.DrawPanelForFailure(wapPanel, "Specify phone number");
            }

            if (string.IsNullOrEmpty(txtAlert.Text))
            {
                this.DrawPanelForFailure(wapPanel, "Specify alert text");
            }

            if (string.IsNullOrEmpty(txtUrl.Text))
            {
                this.DrawPanelForFailure(wapPanel, "Specify Url");
            }

            if (this.ReadAndGetAccessToken() == true)
            {
                this.SendWapPush();
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(wapPanel, ex.ToString());
        }
    }

    #region Display Status message methods
    /// <summary>
    /// Display success message
    /// </summary>
    /// <param name="panelParam">Panel to draw success message</param>
    /// <param name="message">Message to display</param>
    private void DrawPanelForSuccess(Panel panelParam, string message)
    {
        if (panelParam.HasControls())
        {
            panelParam.Controls.Clear();
        }

        Table table = new Table();
        table.CssClass = "successWide";
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOneCellOne.Width = Unit.Pixel(75);
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);

        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Font.Bold = true;
        rowTwoCellOne.Text = "Message ID:";
        rowTwoCellOne.Width = Unit.Pixel(75);
        rowTwo.Controls.Add(rowTwoCellOne);

        TableCell rowTwoCellTwo = new TableCell();
        rowTwoCellTwo.Text = message;
        rowTwoCellTwo.HorizontalAlign = HorizontalAlign.Left;
        rowTwo.Controls.Add(rowTwoCellTwo);
        table.Controls.Add(rowTwo);
        panelParam.Controls.Add(table);
    }

    /// <summary>
    /// Displays error message
    /// </summary>
    /// <param name="panelParam">Panel to draw success message</param>
    /// <param name="message">Message to display</param>
    private void DrawPanelForFailure(Panel panelParam, string message)
    {
        if (panelParam.HasControls())
        {
            panelParam.Controls.Clear();
        }

        Table table = new Table();
        table.CssClass = "errorWide";
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        panelParam.Controls.Add(table);
    }

    #endregion

    #region WAP Push Related methods

    /// <summary>
    /// This function reads the Access Token File and stores the values of access token, expiry seconds, refresh token, 
    /// last access token time and refresh token expiry time. 
    /// </summary>
    /// <returns>true, if access token file and all others attributes read successfully otherwise returns false</returns>
    private bool ReadAccessTokenFile()
    {
        FileStream fileStream = null;
        StreamReader streamReader = null;
        bool ableToRead = true;
        try
        {
            fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            streamReader = new StreamReader(fileStream);
            this.accessToken = streamReader.ReadLine();
            this.expirySeconds = streamReader.ReadLine();
            this.refreshToken = streamReader.ReadLine();
            this.accessTokenExpiryTime = Convert.ToDateTime(streamReader.ReadLine());
            this.refreshTokenExpiryTime = streamReader.ReadLine();
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(wapPanel, ex.ToString());
            ableToRead = false;
        }
        finally
        {
            if (null != streamReader)
            {
                streamReader.Close();
            }

            if (null != fileStream)
            {
                fileStream.Close();
            }
        }

        if (this.accessToken == null || this.expirySeconds == null || this.refreshToken == null || this.accessTokenExpiryTime == null || this.refreshTokenExpiryTime == null)
        {
            ableToRead = false;
        }

        return ableToRead;
    }

    /// <summary>
    /// Validates he expiry of the access token and refresh token
    /// </summary>
    /// <returns>string, returns VALID_ACCESS_TOKEN if its valid
    /// otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    /// return REFRESH_TOKEN, if access token in expired and refresh token is valid</returns>
    private string IsTokenValid()
    {

        if (this.accessToken == null)
        {
            return "INVALID_ACCESS_TOKEN";
        }

        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

            if (currentServerTime >= this.accessTokenExpiryTime)
            {
                if (currentServerTime >= DateTime.Parse(this.refreshTokenExpiryTime))
                {
                    return "INVALID_ACCESS_TOKEN";
                }
                else
                {
                    return "REFRESH_TOKEN";
                }
            }
            else
            {
                return "VALID_ACCESS_TOKEN";
            }
        }
        catch
        {
            return "INVALID_ACCESS_TOKEN";
        }
    }

    /// <summary>
    /// This method gets access token based on either client credentials mode or refresh token.
    /// </summary>
    /// <param name="type">AccessTokenType; either Client_Credential or Refresh_Token</param>
    /// <returns>true/false; true if able to get access token, else false</returns>
    private bool GetAccessToken(AccessTokenType type)
    {
        Stream postStream = null;
        StreamWriter streamWriter = null;
        FileStream fileStream = null;
        try
        {

            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

            WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token");
            accessTokenRequest.Method = "POST";

            string oauthParameters = string.Empty;
            if (type == AccessTokenType.Client_Credential)
            {
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=WAP";
            }
            else
            {
                oauthParameters = "grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken;
            }

            accessTokenRequest.ContentType = "application/x-www-form-urlencoded";

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = encoding.GetBytes(oauthParameters);
            accessTokenRequest.ContentLength = postBytes.Length;
            postStream = accessTokenRequest.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd().ToString();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));

                this.accessToken = deserializedJsonObj.access_token;
                this.expirySeconds = deserializedJsonObj.expires_in;
                this.refreshToken = deserializedJsonObj.refresh_token;
                this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in));
                
                DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                if (deserializedJsonObj.expires_in.Equals("0"))
                {
                    int defaultAccessTokenExpiresIn = 100; // In Years
                    this.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn);
                }

                this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();

                fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                streamWriter = new StreamWriter(fileStream);

                streamWriter.WriteLine(this.accessToken);
                streamWriter.WriteLine(this.expirySeconds);
                streamWriter.WriteLine(this.refreshToken);
                streamWriter.WriteLine(this.accessTokenExpiryTime.ToString());
                streamWriter.WriteLine(this.refreshTokenExpiryTime);

                // Close and clean up the StreamReader
                accessTokenResponseStream.Close();
                return true;
            }
        }
        catch (WebException we)
        {
            string errorResponse = string.Empty;

            try
            {
                using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                {
                    errorResponse = sr2.ReadToEnd();
                    sr2.Close();
                }
            }
            catch
            {
                errorResponse = "Unable to get response";
            }
            this.DrawPanelForFailure(wapPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(wapPanel, ex.ToString());
        }
        finally
        {
            if (null != postStream)
            {
                postStream.Close();
            }

            if (null != streamWriter)
            {
                streamWriter.Close();
            }

            if (null != fileStream)
            {
                fileStream.Close();
            }
        }

        return false;
    }

    /// <summary>
    /// This function reads access token file, validates the access token and gets a new access token
    /// </summary>
    /// <returns>true if access token is valid, or else false is returned</returns>
    private bool ReadAndGetAccessToken()
    {
        bool ableToGetToken = true;

        if (this.ReadAccessTokenFile() == false)
        {
            ableToGetToken = this.GetAccessToken(AccessTokenType.Client_Credential);
        }
        else
        {
            string tokenValidity = this.IsTokenValid();

            if (tokenValidity.Equals("REFRESH_TOKEN"))
            {
                ableToGetToken = this.GetAccessToken(AccessTokenType.Refresh_Token);
            }
            else if (tokenValidity.Equals("INVALID_ACCESS_TOKEN"))
            {
                ableToGetToken = this.GetAccessToken(AccessTokenType.Client_Credential);
            }
        }

        return ableToGetToken;
    }

    /// <summary>
    /// This method reads config file and assigns values to local variables
    /// </summary>
    /// <returns>true/false, true- if able to read from config file</returns>
    private bool ReadConfigFile()
    {
        this.apiKey = ConfigurationManager.AppSettings["api_key"];
        if (string.IsNullOrEmpty(this.apiKey))
        {
            this.DrawPanelForFailure(wapPanel, "api_key is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(wapPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(wapPanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "WAP";
        }

        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "WAPApp1AccessToken.txt";
        }

        this.wapFilePath = ConfigurationManager.AppSettings["WAPFilePath"];
        if (string.IsNullOrEmpty(this.wapFilePath))
        {
            this.accessTokenFilePath = "WAPText.txt";
        }

        string refreshTokenExpires = ConfigurationManager.AppSettings["refreshTokenExpiresIn"];
        if (!string.IsNullOrEmpty(refreshTokenExpires))
        {
            this.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires);
        }
        else
        {
            this.refreshTokenExpiresIn = 24;
        }

        return true;
    }

    /// <summary>
    /// This function validates string against the valid msisdn
    /// </summary>
    /// <param name="number">string, destination number</param>
    /// <returns>true/false; true if valid MSISDN, else false</returns>
    private bool IsValidMISDN(string number)
    {
        string smsAddressInput = number;
        string smsAddressFormatted;
        string phoneStringPattern = "^\\d{3}-\\d{3}-\\d{4}$";
        if (System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern))
        {
            smsAddressFormatted = smsAddressInput.Replace("-", string.Empty);
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

    /// <summary>
    /// This function calls send wap message api to send wap messsage
    /// </summary>
    private void SendWapPush()
    {
        StreamWriter wapFileWriter = null;
        StreamReader streamReader = null;
        try
        {
            if (this.IsValidMISDN(txtAddressWAPPush.Text.ToString()) == false)
            {
                this.DrawPanelForFailure(wapPanel, "Invalid Number: " + txtAddressWAPPush.Text.ToString());
                return;
            }

            string wapAddress = txtAddressWAPPush.Text.ToString().Replace("tel:+1", string.Empty);
            wapAddress = wapAddress.ToString().Replace("tel:+", string.Empty);
            wapAddress = wapAddress.ToString().Replace("tel:1", string.Empty);
            wapAddress = wapAddress.ToString().Replace("tel:", string.Empty);
            wapAddress = wapAddress.ToString().Replace("tel:", string.Empty);
            wapAddress = wapAddress.ToString().Replace("-", string.Empty);

            string wapMessage = txtAlert.Text;
            string wapUrl = txtUrl.Text;

            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

            string wapData = string.Empty;
            wapData += "Content-Disposition: form-data; name=\"PushContent\"\n";
            wapData += "Content-Type: text/vnd.wap.si\n";
            wapData += "Content-Length: 20\n";
            wapData += "X-Wap-Application-Id: x-wap-application:wml.ua\n\n";
            wapData += "<?xml version='1.0'?>\n";
            wapData += "<!DOCTYPE si PUBLIC \"-//WAPFORUM//DTD SI 1.0//EN\" " + "\"http://www.wapforum.org/DTD/si.dtd\">\n";
            wapData += "<si>\n";
            wapData += "<indication href=\"" + wapUrl.ToString() + "\" " + "action=\"signal-medium\" si-id=\"6532\">\n";
            wapData += wapMessage.ToString();
            wapData += "\n</indication>";
            wapData += "\n</si>";

            wapFileWriter = File.CreateText(Request.MapPath(this.wapFilePath));
            wapFileWriter.Write(wapData);
            wapFileWriter.Close();

            streamReader = new StreamReader(Request.MapPath(this.wapFilePath));
            string pushFile = streamReader.ReadToEnd();

            HttpWebRequest wapRequestObject = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/1/messages/outbox/wapPush");
            wapRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);
            wapRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"\"; boundary=\"" + boundary + "\"\r\n";
            wapRequestObject.Method = "POST";
            wapRequestObject.KeepAlive = true;

            string sendWapData = "address=" + Server.UrlEncode("tel:" + wapAddress.ToString()) + "&subject=" + Server.UrlEncode("Wap Message") + "&priority=High&content-type=" + Server.UrlEncode("application/xml");

            string data = string.Empty;
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
                this.DrawPanelForSuccess(wapPanel, deserializedJsonObj.id.ToString());
                wapResponseStream.Close();
            }
        }
        catch (WebException we)
        {
            string errorResponse = string.Empty;

            try
            {
                using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                {
                    errorResponse = sr2.ReadToEnd();
                    sr2.Close();
                }
            }
            catch
            {
                errorResponse = "Unable to get response";
            }
            this.DrawPanelForFailure(wapPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(wapPanel, ex.ToString());
        }
        finally
        {
            if (null != wapFileWriter)
            {
                wapFileWriter.Close();
            }

            if (null != streamReader)
            {
                streamReader.Close();
            }

            if (File.Exists(Request.MapPath(this.wapFilePath)))
            {
                File.Delete(Request.MapPath(this.wapFilePath));
            }
        }
    }

    #endregion
}

#region Data Structures

/// <summary>
/// AccessTokenResponse Object
/// </summary>
public class AccessTokenResponse
{
    /// <summary>
    /// Gets or sets the value of access_token
    /// </summary>
    public string access_token
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of refresh_token
    /// </summary>
    public string refresh_token
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of expires_in
    /// </summary>
    public string expires_in
    {
        get;
        set;
    }
}

/// <summary>
/// WAP Response Object
/// </summary>
public class SendWapResponse
{
    /// <summary>
    /// Gets or sets the value of id
    /// </summary>
    public string id
    {
        get;
        set;
    }
}

#endregion