// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

#endregion

/// <summary>
/// Default class
/// </summary>
public partial class SMS_App1 : System.Web.UI.Page
{
    #region Variable Declaration
    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private string shortCode, endPoint, accessTokenFilePath, apiKey, secretKey, accessToken, accessTokenExpiryTime,
        scope, refreshToken, refreshTokenExpiryTime;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private string[] shortCodes;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    /// <summary>
    /// Access Token Types
    /// </summary>
    private enum AccessType
    {
        /// <summary>
        /// Access Token Type is based on Client Credential Mode
        /// </summary>
        ClientCredential,

        /// <summary>
        /// Access Token Type is based on Refresh Token
        /// </summary>
        RefreshToken
    }
    #endregion

    #region SMS Application Events
    /// <summary>
    /// This function is called when the applicaiton page is loaded into the browser.
    /// This fucntion reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = string.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            
            bool ableToRead = this.ReadConfigFile();
            if (ableToRead == false)
            {
                return;
            }
            
            this.shortCodes = this.shortCode.Split(';');
            this.shortCode = this.shortCodes[0];
            Table table = new Table();
            table.Font.Size = 8;
            foreach (string srtCode in this.shortCodes)
            {
                Button button = new Button();
                button.Click += new EventHandler(this.GetMessagesButton_Click);
                button.Text = "Get Messages for " + srtCode;
                TableRow rowOne = new TableRow();
                TableCell rowOneCellOne = new TableCell();
                rowOne.Controls.Add(rowOneCellOne);
                rowOneCellOne.Controls.Add(button);
                table.Controls.Add(rowOne);
            }

            receiveMessagePanel.Controls.Add(table);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendSMSPanel, ex.ToString());
            Response.Write(ex.ToString());
        }
    }

    /// <summary>
    /// Reads from config file
    /// </summary>
    /// <returns>true/false; true if able to read else false</returns>
    private bool ReadConfigFile()
    {
        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "~\\SMSApp1AccessToken.txt";
        }

        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(sendSMSPanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.shortCode = ConfigurationManager.AppSettings["short_code"];
        if (string.IsNullOrEmpty(this.shortCode))
        {
            this.DrawPanelForFailure(sendSMSPanel, "short_code is not defined in configuration file");
            return false;
        }

        this.apiKey = ConfigurationManager.AppSettings["api_key"];
        if (string.IsNullOrEmpty(this.apiKey))
        {
            this.DrawPanelForFailure(sendSMSPanel, "api_key is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(sendSMSPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "SMS";
        }

        string refreshTokenExpires = ConfigurationManager.AppSettings["refreshTokenExpiresIn"];
        if (!string.IsNullOrEmpty(refreshTokenExpires))
        {
            this.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires);
        }
        else
        {
            this.refreshTokenExpiresIn = 24; // Default value
        }

        return true;
    }

    /// <summary>
    /// This function is called with user clicks on send SMS
    /// This validates the access token and then calls sendSMS method to invoke send SMS API.
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnSendSMS_Click(object sender, EventArgs e)
    {
        try
        {
            if (this.ReadAndGetAccessToken(sendSMSPanel) == true)
            {
                this.SendSms();
            }
            else
            {
                this.DrawPanelForFailure(sendSMSPanel, "Unable to get access token.");
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendSMSPanel, ex.ToString());
        }
    }

    /// <summary>
    /// This method is called when user clicks on get delivery status button
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void GetDeliveryStatusButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (this.ReadAndGetAccessToken(getStatusPanel) == true)
            {
                this.GetSmsDeliveryStatus();
            }
            else
            {
                this.DrawPanelForFailure(getStatusPanel, "Unable to get access token.");
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(getStatusPanel, ex.ToString());
        }
    }

    /// <summary>
    /// This method is called when user clicks on get message button
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void GetMessagesButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (this.ReadAndGetAccessToken(getMessagePanel) == true)
            {
                Button button = sender as Button;
                string buttonCaption = button.Text.ToString();
                this.shortCode = buttonCaption.Replace("Get Messages for ", string.Empty);
                this.RecieveSms();
            }
            else
            {
                this.DrawPanelForFailure(getMessagePanel, "Unable to get access token.");
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(getMessagePanel, ex.ToString());
        }
    }
    #endregion

    #region SMS Application related functions
    /// <summary>
    /// This function is used to neglect the ssl handshake error with authentication server
    /// </summary>
    private static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    /// <summary>
    /// This function reads the Access Token File and stores the values of access token, expiry seconds
    /// refresh token, last access token time and refresh token expiry time
    /// This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns boolean</returns>    
    private bool ReadAccessTokenFile(Panel panelParam)
    {
        FileStream fileStream = null;
        StreamReader streamReader = null;
        try
        {
            fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            streamReader = new StreamReader(fileStream);
            this.accessToken = streamReader.ReadLine();
            this.accessTokenExpiryTime = streamReader.ReadLine();
            this.refreshToken = streamReader.ReadLine();
            this.refreshTokenExpiryTime = streamReader.ReadLine();
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(panelParam, ex.Message);
            return false;
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

        if ((this.accessToken == null) || (this.accessTokenExpiryTime == null) || (this.refreshToken == null) || (this.refreshTokenExpiryTime == null))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This function validates the expiry of the access token and refresh token,
    /// function compares the current time with the refresh token taken time, if current time is greater then 
    /// returns INVALID_REFRESH_TOKEN
    /// function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    /// funciton returns INVALID_ACCESS_TOKEN
    /// otherwise returns VALID_ACCESS_TOKEN
    /// </summary>
    /// <returns>Return String</returns>
    private string IsTokenValid()
    {
        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            if (currentServerTime >= DateTime.Parse(this.accessTokenExpiryTime))
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
    /// This function is used to read access token file and validate the access token
    /// this function returns true if access token is valid, or else false is returned
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns Boolean</returns>
    private bool ReadAndGetAccessToken(Panel panelParam)
    {
        bool result = true;
        if (this.ReadAccessTokenFile(panelParam) == false)
        {
            result = this.GetAccessToken(AccessType.ClientCredential, panelParam);
        }
        else
        {
            string tokenValidity = this.IsTokenValid();
            if (tokenValidity == "REFRESH_TOKEN")
            {
                result = this.GetAccessToken(AccessType.RefreshToken, panelParam);
            }
            else if (string.Compare(tokenValidity, "INVALID_ACCESS_TOKEN") == 0)
            {
                result = this.GetAccessToken(AccessType.ClientCredential, panelParam);
            }
        }

        if (this.accessToken == null || this.accessToken.Length <= 0)
        {
            return false;
        }
        else
        {
            return result;
        }
    }
    
    /// <summary>
    /// This function get the access token based on the type parameter type values.
    /// If type value is 1, access token is fetch for client credential flow
    /// If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    /// </summary>
    /// <param name="type">Type as integer</param>
    /// <param name="panelParam">Panel details</param>
    /// <returns>Return boolean</returns>
    private bool GetAccessToken(AccessType type, Panel panelParam)
    {
        FileStream fileStream = null;
        Stream postStream = null;
        StreamWriter streamWriter = null;

        // This is client credential flow
        if (type == AccessType.ClientCredential)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token");
                accessTokenRequest.Method = "POST";
                string oauthParameters = string.Empty;
                if (type == AccessType.ClientCredential)
                {
                    oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=" + this.scope;
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
                    this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString();
                    this.refreshToken = deserializedJsonObj.refresh_token;

                    DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                    if (deserializedJsonObj.expires_in.Equals("0"))
                    {
                        int defaultAccessTokenExpiresIn = 100; // In Yearsint yearsToAdd = 100;
                        this.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongDateString() + " " + currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongTimeString();
                    }

                    this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();

                    fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                    streamWriter = new StreamWriter(fileStream);
                    streamWriter.WriteLine(this.accessToken);
                    streamWriter.WriteLine(this.accessTokenExpiryTime);
                    streamWriter.WriteLine(this.refreshToken);
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

                this.DrawPanelForFailure(panelParam, errorResponse + Environment.NewLine + we.ToString());
            }
            catch (Exception ex)
            {
                this.DrawPanelForFailure(panelParam, ex.Message);
                return false;
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
        }
        else if (type == AccessType.RefreshToken)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token");
                accessTokenRequest.Method = "POST";

                string oauthParameters = "grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken;
                accessTokenRequest.ContentType = "application/x-www-form-urlencoded";

                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(oauthParameters);
                accessTokenRequest.ContentLength = postBytes.Length;

                postStream = accessTokenRequest.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);

                WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                {
                    string accessTokenJSon = accessTokenResponseStream.ReadToEnd().ToString();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();

                    AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(accessTokenJSon, typeof(AccessTokenResponse));
                    this.accessToken = deserializedJsonObj.access_token.ToString();
                    DateTime accessTokenExpiryTime = currentServerTime.AddMilliseconds(Convert.ToDouble(deserializedJsonObj.expires_in.ToString()));
                    this.refreshToken = deserializedJsonObj.refresh_token.ToString();

                    fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                    streamWriter = new StreamWriter(fileStream);
                    streamWriter.WriteLine(this.accessToken);
                    streamWriter.WriteLine(this.accessTokenExpiryTime);
                    streamWriter.WriteLine(this.refreshToken);

                    // Refresh token valids for 24 hours
                    DateTime refreshExpiry = currentServerTime.AddHours(24);
                    this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    streamWriter.WriteLine(refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString());

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

                this.DrawPanelForFailure(panelParam, errorResponse + Environment.NewLine + we.ToString());
            }
            catch (Exception ex)
            {
                this.DrawPanelForFailure(panelParam, ex.Message);
                return false;
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
        }

        return false;
    }

    /// <summary>
    /// This function validates the input fields and if they are valid send sms api is invoked
    /// </summary>
    private void SendSms()
    {
        try
        {
            string smsAddressInput = txtmsisdn.Text.ToString();
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

            string smsAddressForRequest = smsAddressFormatted.ToString();
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
                this.DrawPanelForFailure(sendSMSPanel, "Invalid phone number: " + smsAddressInput);
            }
            else
            {
                string smsMessage = txtmsg.Text.ToString();
                if (smsMessage == null || smsMessage.Length <= 0)
                {
                    this.DrawPanelForFailure(sendSMSPanel, "Message is null or empty");
                    return;
                }

                string sendSmsResponseData;
                //// HttpWebRequest sendSmsRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/sms/2/messaging/outbox?access_token=" + this.access_token.ToString());
                HttpWebRequest sendSmsRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/sms/2/messaging/outbox");
                string strReq = "{'Address':'tel:" + smsAddressFormatted + "','Message':'" + smsMessage + "'}";
                sendSmsRequestObject.Method = "POST";
                sendSmsRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);
                sendSmsRequestObject.ContentType = "application/json";
                sendSmsRequestObject.Accept = "application/json";

                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(strReq);
                sendSmsRequestObject.ContentLength = postBytes.Length;

                Stream postStream = sendSmsRequestObject.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();

                HttpWebResponse sendSmsResponseObject = (HttpWebResponse)sendSmsRequestObject.GetResponse();
                using (StreamReader sendSmsResponseStream = new StreamReader(sendSmsResponseObject.GetResponseStream()))
                {
                    sendSmsResponseData = sendSmsResponseStream.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    SendSmsResponse deserializedJsonObj = (SendSmsResponse)deserializeJsonObject.Deserialize(sendSmsResponseData, typeof(SendSmsResponse));
                    txtSmsId.Text = deserializedJsonObj.Id.ToString();
                    this.DrawPanelForSuccess(sendSMSPanel, deserializedJsonObj.Id.ToString());
                    sendSmsResponseStream.Close();
                }
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

            this.DrawPanelForFailure(sendSMSPanel, errorResponse + Environment.NewLine + we.ToString());
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendSMSPanel, ex.ToString());
        }
    }
    
    /// <summary>
    /// This function is called when user clicks on get delivery status button.
    /// this funciton calls get sms delivery status API to fetch the status.
    /// </summary>
    private void GetSmsDeliveryStatus()
    {
        try
        {
            string smsId = txtSmsId.Text.ToString();
            if (smsId == null || smsId.Length <= 0)
            {
                this.DrawPanelForFailure(getStatusPanel, "Message is null or empty");
                return;
            }

            if (this.ReadAndGetAccessToken(getStatusPanel) == true)
            {
                string getSmsDeliveryStatusResponseData;
                // HttpWebRequest getSmsDeliveryStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.FQDN + "/rest/sms/2/messaging/outbox/" + smsId.ToString() + "?access_token=" + this.access_token.ToString());
                HttpWebRequest getSmsDeliveryStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/sms/2/messaging/outbox/" + smsId.ToString());
                getSmsDeliveryStatusRequestObject.Method = "GET";
                getSmsDeliveryStatusRequestObject.Headers.Add("Authorization", "BEARER " + this.accessToken);
                getSmsDeliveryStatusRequestObject.ContentType = "application/JSON";
                getSmsDeliveryStatusRequestObject.Accept = "application/json";

                HttpWebResponse getSmsDeliveryStatusResponse = (HttpWebResponse)getSmsDeliveryStatusRequestObject.GetResponse();
                using (StreamReader getSmsDeliveryStatusResponseStream = new StreamReader(getSmsDeliveryStatusResponse.GetResponseStream()))
                {
                    getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseStream.ReadToEnd();
                    getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseData.Replace("-", string.Empty);
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    GetDeliveryStatus status = (GetDeliveryStatus)deserializeJsonObject.Deserialize(getSmsDeliveryStatusResponseData, typeof(GetDeliveryStatus));
                    this.DrawGetStatusSuccess(status.DeliveryInfoList.DeliveryInfo[0].Deliverystatus, status.DeliveryInfoList.ResourceURL);
                    getSmsDeliveryStatusResponseStream.Close();
                }
            }
            else
            {
                this.DrawPanelForFailure(getStatusPanel, "Unable to get access token.");
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

            this.DrawPanelForFailure(getStatusPanel, errorResponse + Environment.NewLine + we.ToString());
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(getStatusPanel, ex.ToString());
        }
    }
    
    /// <summary>
    /// This function is used to draw the table for get status success response
    /// </summary>
    /// <param name="status">Status as string</param>
    /// <param name="url">url as string</param>
    private void DrawGetStatusSuccess(string status, string url)
    {
        Table table = new Table();
        TableRow rowOne = new TableRow();
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        table.BorderStyle = BorderStyle.Outset;
        table.Width = Unit.Pixel(650);
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        TableCell rowTwoCellTwo = new TableCell();
        rowTwoCellOne.Text = "Status: ";
        rowTwoCellOne.Font.Bold = true;
        rowTwo.Controls.Add(rowTwoCellOne);
        rowTwoCellTwo.Text = status.ToString();
        rowTwo.Controls.Add(rowTwoCellTwo);
        table.Controls.Add(rowTwo);
        TableRow rowThree = new TableRow();
        TableCell rowThreeCellOne = new TableCell();
        TableCell rowThreeCellTwo = new TableCell();
        rowThreeCellOne.Text = "ResourceURL: ";
        rowThreeCellOne.Font.Bold = true;
        rowThree.Controls.Add(rowThreeCellOne);
        rowThreeCellTwo.Text = url.ToString();
        rowThree.Controls.Add(rowThreeCellTwo);
        table.Controls.Add(rowThree);
        table.BorderWidth = 2;
        table.BorderColor = Color.DarkGreen;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        getStatusPanel.Controls.Add(table);
    }

    /// <summary>
    /// This function is called to draw the table in the panelParam panel for success response
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="message">Message as string</param>
    private void DrawPanelForSuccess(Panel panelParam, string message)
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
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Font.Bold = true;
        rowTwoCellOne.Text = "Message ID:";
        rowTwoCellOne.Width = Unit.Pixel(70);
        rowTwo.Controls.Add(rowTwoCellOne);
        TableCell rowTwoCellTwo = new TableCell();
        rowTwoCellTwo.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellTwo);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.DarkGreen;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(table);
    }
    
    /// <summary>
    /// This function draws table for failed response in the panalParam panel
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="message">Message as string</param>
    private void DrawPanelForFailure(Panel panelParam, string message)
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
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.Red;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(table);
    }
    
    /// <summary>
    /// This function calls receive sms api to fetch the sms's
    /// </summary>
    private void RecieveSms()
    {
        try
        {
            string receiveSmsResponseData;
            if (this.shortCode == null || this.shortCode.Length <= 0)
            {
                this.DrawPanelForFailure(getMessagePanel, "Short code is null or empty");
                return;
            }

            if (this.accessToken == null || this.accessToken.Length <= 0)
            {
                this.DrawPanelForFailure(getMessagePanel, "Invalid access token");
                return;
            }
            
            // HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.FQDN + "/rest/sms/2/messaging/inbox?access_token=" + this.access_token.ToString() + "&RegistrationID=" + this.shortCode.ToString());
            HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/sms/2/messaging/inbox?RegistrationID=" + this.shortCode.ToString());
            objRequest.Method = "GET";
            objRequest.Headers.Add("Authorization", "BEARER " + this.accessToken);
            HttpWebResponse receiveSmsResponseObject = (HttpWebResponse)objRequest.GetResponse();
            using (StreamReader receiveSmsResponseStream = new StreamReader(receiveSmsResponseObject.GetResponseStream()))
            {
                receiveSmsResponseData = receiveSmsResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                RecieveSmsResponse deserializedJsonObj = (RecieveSmsResponse)deserializeJsonObject.Deserialize(receiveSmsResponseData, typeof(RecieveSmsResponse));
                int numberOfMessagesInThisBatch = deserializedJsonObj.InboundSMSMessageList.NumberOfMessagesInThisBatch;
                string resourceURL = deserializedJsonObj.InboundSMSMessageList.ResourceURL.ToString();
                string totalNumberOfPendingMessages = deserializedJsonObj.InboundSMSMessageList.TotalNumberOfPendingMessages.ToString();

                string parsedJson = "MessagesInThisBatch : " + numberOfMessagesInThisBatch.ToString() + "<br/>" + "MessagesPending : " + totalNumberOfPendingMessages.ToString() + "<br/>";
                Table table = new Table();
                table.Font.Name = "Sans-serif";
                table.Font.Size = 9;
                table.BorderStyle = BorderStyle.Outset;
                table.Width = Unit.Pixel(650);
                TableRow tableRow = new TableRow();
                TableCell tableCell = new TableCell();
                tableCell.Width = Unit.Pixel(110);
                tableCell.Text = "SUCCESS:";
                tableCell.Font.Bold = true;
                tableRow.Cells.Add(tableCell);
                table.Rows.Add(tableRow);
                tableRow = new TableRow();
                tableCell = new TableCell();
                tableCell.Width = Unit.Pixel(150);
                tableCell.Text = "Messages in this batch:";
                tableCell.Font.Bold = true;
                tableRow.Cells.Add(tableCell);
                tableCell = new TableCell();
                tableCell.HorizontalAlign = HorizontalAlign.Left;
                tableCell.Text = numberOfMessagesInThisBatch.ToString();
                tableRow.Cells.Add(tableCell);
                table.Rows.Add(tableRow);
                tableRow = new TableRow();
                tableCell = new TableCell();
                tableCell.Width = Unit.Pixel(110);
                tableCell.Text = "Messages pending:";
                tableCell.Font.Bold = true;
                tableRow.Cells.Add(tableCell);
                tableCell = new TableCell();
                tableCell.Text = totalNumberOfPendingMessages.ToString();
                tableRow.Cells.Add(tableCell);
                table.Rows.Add(tableRow);
                tableRow = new TableRow();
                table.Rows.Add(tableRow);
                tableRow = new TableRow();
                table.Rows.Add(tableRow);
                Table secondTable = new Table();
                if (numberOfMessagesInThisBatch > 0)
                {
                    tableRow = new TableRow();
                    secondTable.Font.Name = "Sans-serif";
                    secondTable.Font.Size = 9;
                    tableCell = new TableCell();
                    tableCell.Width = Unit.Pixel(100);
                    tableCell.Text = "Message Index";
                    tableCell.HorizontalAlign = HorizontalAlign.Center;
                    tableCell.Font.Bold = true;
                    tableRow.Cells.Add(tableCell);
                    tableCell = new TableCell();
                    tableCell.Font.Bold = true;
                    tableCell.Width = Unit.Pixel(350);
                    tableCell.Wrap = true;
                    tableCell.Text = "Message Text";
                    tableCell.HorizontalAlign = HorizontalAlign.Center;
                    tableRow.Cells.Add(tableCell);
                    tableCell = new TableCell();
                    tableCell.Text = "Sender Address";
                    tableCell.HorizontalAlign = HorizontalAlign.Center;
                    tableCell.Font.Bold = true;
                    tableCell.Width = Unit.Pixel(175);
                    tableRow.Cells.Add(tableCell);
                    secondTable.Rows.Add(tableRow);

                    foreach (InboundSMSMessage prime in deserializedJsonObj.InboundSMSMessageList.InboundSMSMessage)
                    {
                        tableRow = new TableRow();
                        TableCell tableCellmessageId = new TableCell();
                        tableCellmessageId.Width = Unit.Pixel(75);
                        tableCellmessageId.Text = prime.MessageId.ToString();
                        tableCellmessageId.HorizontalAlign = HorizontalAlign.Center;
                        TableCell tableCellmessage = new TableCell();
                        tableCellmessage.Width = Unit.Pixel(350);
                        tableCellmessage.Wrap = true;
                        tableCellmessage.Text = prime.Message.ToString();
                        tableCellmessage.HorizontalAlign = HorizontalAlign.Center;
                        TableCell tableCellsenderAddress = new TableCell();
                        tableCellsenderAddress.Width = Unit.Pixel(175);
                        tableCellsenderAddress.Text = prime.SenderAddress.ToString();
                        tableCellsenderAddress.HorizontalAlign = HorizontalAlign.Center;
                        tableRow.Cells.Add(tableCellmessageId);
                        tableRow.Cells.Add(tableCellmessage);
                        tableRow.Cells.Add(tableCellsenderAddress);
                        secondTable.Rows.Add(tableRow);
                    }
                }

                table.BorderColor = Color.DarkGreen;
                table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
                table.BorderWidth = 2;
                
                getMessagePanel.Controls.Add(table);
                getMessagePanel.Controls.Add(secondTable);
                receiveSmsResponseStream.Close();
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

            this.DrawPanelForFailure(getMessagePanel, errorResponse + Environment.NewLine + we.ToString());
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(getMessagePanel, ex.ToString());
        }
    }
    #endregion

    #region SMS Application related Data Structures
    /// <summary>
    /// Class to hold access token response
    /// </summary>
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets access token
        /// </summary>
        public string access_token { get; set; }

        /// <summary>
        /// Gets or sets refresh token
        /// </summary>
        public string refresh_token { get; set; }

        /// <summary>
        /// Gets or sets expires in
        /// </summary>
        public string expires_in { get; set; }
    }

    /// <summary>
    /// Class to hold send sms response
    /// </summary>
    public class SendSmsResponse
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class to hold sms delivery status
    /// </summary>
    public class GetSetSmsDeliveryStatus
    {
        /// <summary>
        /// Gets or sets status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets resource url
        /// </summary>
        public string ResourceUrl { get; set; }
    }

    /// <summary>
    /// Class to hold sms status
    /// </summary>
    public class SmsStatus
    {
        /// <summary>
        /// Gets or sets status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets resource url
        /// </summary>
        public string ResourceURL { get; set; }
    }

    /// <summary>
    /// Class to hold rececive sms response
    /// </summary>
    public class RecieveSmsResponse
    {
        /// <summary>
        /// Gets or sets inbound sms message list
        /// </summary>
        public InboundSMSMessageList InboundSMSMessageList { get; set; }
    }

    /// <summary>
    /// Class to hold inbound sms message list
    /// </summary>
    public class InboundSMSMessageList
    {
        /// <summary>
        /// Gets or sets inbound sms message
        /// </summary>
        public List<InboundSMSMessage> InboundSMSMessage { get; set; }

        /// <summary>
        /// Gets or sets number of messages in a batch
        /// </summary>
        public int NumberOfMessagesInThisBatch { get; set; }

        /// <summary>
        /// Gets or sets resource url
        /// </summary>
        public string ResourceURL { get; set; }

        /// <summary>
        /// Gets or sets total number of pending messages
        /// </summary>
        public int TotalNumberOfPendingMessages { get; set; }
    }

    /// <summary>
    /// Class to hold inbound sms message
    /// </summary>
    public class InboundSMSMessage
    {
        /// <summary>
        /// Gets or sets datetime
        /// </summary>
        public string DateTime { get; set; }

        /// <summary>
        /// Gets or sets destination address
        /// </summary>
        public string DestinationAddress { get; set; }

        /// <summary>
        /// Gets or sets message id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Gets or sets message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets sender address
        /// </summary>
        public string SenderAddress { get; set; }
    }

    /// <summary>
    /// Class to hold delivery status
    /// </summary>
    public class GetDeliveryStatus
    {
        /// <summary>
        /// Gets or sets delivery info list
        /// </summary>
        public DeliveryInfoList DeliveryInfoList { get; set; }
    }

    /// <summary>
    /// Class to hold delivery info list
    /// </summary>
    public class DeliveryInfoList
    {
        /// <summary>
        /// Gets or sets resource url
        /// </summary>
        public string ResourceURL { get; set; }

        /// <summary>
        /// Gets or sets delivery info
        /// </summary>
        public List<DeliveryInfo> DeliveryInfo { get; set; }
    }

    /// <summary>
    /// Class to hold delivery info
    /// </summary>
    public class DeliveryInfo
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets address
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets delivery status
        /// </summary>
        public string Deliverystatus { get; set; }
    }
    #endregion
}