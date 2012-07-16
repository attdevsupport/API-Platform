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
/// MMS_App1 class
/// </summary>
/// <remarks> This application allows an end user to send an MMS message with up to three attachments of any common format, 
/// and check the delivery status of that MMS message.
/// </remarks>
public partial class MMS_App1 : System.Web.UI.Page
{
    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string endPoint, accessTokenFilePath, apiKey, secretKey, accessToken, scope, refreshToken;

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string expirySeconds, refreshTokenExpiryTime;
    
    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private DateTime accessTokenExpiryTime;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

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

    #region Page and Button Events

    /// <summary>
    /// Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void Page_Load(object sender, EventArgs e)
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
            this.DrawPanelForFailure(sendMessagePanel, ex.ToString());
        }
    }

    /// <summary>
    /// This method will be called when user clicks on send mms button
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void SendMMSMessageButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (this.ReadAndGetAccessToken() == true)
            {   
                long fileSize = 0;
                if (!string.IsNullOrEmpty(FileUpload1.FileName))
                {
                    FileUpload1.SaveAs(Request.MapPath(FileUpload1.FileName.ToString()));
                    Session["mmsFilePath1"] = Request.MapPath(FileUpload1.FileName);
                    FileInfo fileInfoObj = new FileInfo(Session["mmsFilePath1"].ToString());
                    fileSize = fileSize + (fileInfoObj.Length / 1024);
                    if (fileSize > 600)
                    {
                        this.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(FileUpload2.FileName))
                {
                    FileUpload2.SaveAs(Request.MapPath(FileUpload2.FileName));
                    Session["mmsFilePath2"] = Request.MapPath(FileUpload2.FileName);
                    FileInfo fileInfoObj = new FileInfo(Session["mmsFilePath2"].ToString());
                    fileSize = fileSize + (fileInfoObj.Length / 1024);
                    if (fileSize > 600)
                    {
                        this.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(FileUpload3.FileName))
                {
                    FileUpload3.SaveAs(Request.MapPath(FileUpload3.FileName));
                    Session["mmsFilePath3"] = Request.MapPath(FileUpload3.FileName);
                    FileInfo fileInfoObj = new FileInfo(Session["mmsFilePath3"].ToString());
                    fileSize = fileSize + (fileInfoObj.Length / 1024);
                    if (fileSize > 600)
                    {
                        this.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                        return;
                    }
                }

                if (fileSize <= 600)
                {
                    this.SendMMS();
                }
                else
                {
                    this.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendMessagePanel, ex.ToString());
            return;
        }
    }

    /// <summary>
    /// This method will be called when user click on get status button
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void GetStatusButton_Click(object sender, EventArgs e)
    {
        this.GetMmsDeliveryStatus();
    }

    #endregion

    #region Access Token Methods

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
            this.DrawPanelForFailure(sendMessagePanel, ex.ToString());
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
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=MMS";
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
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd();
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
            this.DrawPanelForFailure(sendMessagePanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendMessagePanel, ex.ToString());
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

    #endregion

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

    /// <summary>
    /// Displays Resource url upon success of GetMmsDelivery
    /// </summary>
    /// <param name="status">string, status of the request</param>
    /// <param name="url">string, url of the resource</param>
    private void DrawGetStatusSuccess(string status, string url)
    {
        if (getStatusPanel.HasControls())
        {
            getStatusPanel.Controls.Clear();
        }

        Table table = new Table();
        table.CssClass = "successWide";
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        
        TableRow rowOne = new TableRow();
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
        
        getStatusPanel.Controls.Add(table);
    }

#endregion

    #region Application Specific Methods

    /// <summary>
    /// This function calls get message delivery status api to fetch the delivery status
    /// </summary>
    private void GetMmsDeliveryStatus()
    {
        try
        {
            string mmsId = messageIDTextBox.Text;
            if (mmsId == null || mmsId.Length <= 0)
            {
                this.DrawPanelForFailure(getStatusPanel, "Message Id is null or empty");
                return;
            }

            if (this.ReadAndGetAccessToken() == true)
            {
                string mmsDeliveryStatus;
                HttpWebRequest mmsStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/mms/2/messaging/outbox/" + mmsId);
                mmsStatusRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);
                mmsStatusRequestObject.Method = "GET";

                HttpWebResponse mmsStatusResponseObject = (HttpWebResponse)mmsStatusRequestObject.GetResponse();
                using (StreamReader mmsStatusResponseStream = new StreamReader(mmsStatusResponseObject.GetResponseStream()))
                {
                    mmsDeliveryStatus = mmsStatusResponseStream.ReadToEnd();
                    mmsDeliveryStatus = mmsDeliveryStatus.Replace("-", string.Empty);
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    GetDeliveryStatus status = (GetDeliveryStatus)deserializeJsonObject.Deserialize(mmsDeliveryStatus, typeof(GetDeliveryStatus));
                    this.DrawGetStatusSuccess(status.DeliveryInfoList.deliveryInfo[0].deliverystatus, status.DeliveryInfoList.resourceURL);
                    mmsStatusResponseStream.Close();
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
            this.DrawPanelForFailure(getStatusPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(getStatusPanel, ex.ToString());
        }
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
            this.DrawPanelForFailure(sendMessagePanel, "api_key is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(sendMessagePanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(sendMessagePanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "MMS";
        }

        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "~\\MMSApp1AccessToken.txt";
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
    /// Gets formatted phone number
    /// </summary>
    /// <returns>string, phone number</returns>
    private string GetPhoneNumber()
    {
        long tryParseResult = 0;

        string smsAddressInput = phoneTextBox.Text.ToString();

        string smsAddressFormatted = string.Empty;

        string phoneStringPattern = "^\\d{3}-\\d{3}-\\d{4}$";
        if (System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern))
        {
            smsAddressFormatted = smsAddressInput.Replace("-", string.Empty);
        }
        else
        {
            smsAddressFormatted = smsAddressInput;
        }

        if (smsAddressFormatted.Length == 16 && smsAddressFormatted.StartsWith("tel:+1"))
        {
            smsAddressFormatted = smsAddressFormatted.Substring(6, 10);
        }
        else if (smsAddressFormatted.Length == 15 && smsAddressFormatted.StartsWith("tel:+"))
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
            this.DrawPanelForFailure(sendMessagePanel, "Invalid phone number: " + smsAddressInput);
            smsAddressFormatted = string.Empty;
        }

        return smsAddressFormatted;
    }

    /// <summary>
    /// This funciton initiates send mms api call to send selected files as an mms
    /// </summary>
    private void SendMMS()
    {
        try
        {
            string mmsAddress = this.GetPhoneNumber();
            string mmsMessage = messageTextBox.Text.ToString();

            if (((Session["mmsFilePath1"] == null) && (Session["mmsFilePath2"] == null) && (Session["mmsFilePath3"] == null)) && string.IsNullOrEmpty(mmsMessage))
            {
                this.DrawPanelForFailure(sendMessagePanel, "Message is null or empty");
                return;
            }

            if ((Session["mmsFilePath1"] == null) && (Session["mmsFilePath2"] == null) && (Session["mmsFilePath3"] == null))
            {
                this.SendMessageNoAttachments(mmsAddress, mmsMessage);
            }
            else
            {
                this.SendMultimediaMessage(mmsAddress, mmsMessage);
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
            this.DrawPanelForFailure(sendMessagePanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendMessagePanel, ex.ToString());
        }
        finally
        {
            int index = 1;

            object tmpVar = null;
            while (index <= 3)
            {
                tmpVar = Session["mmsFilePath" + index];
                if (tmpVar != null)
                {
                    if (File.Exists(tmpVar.ToString()))
                    {
                        File.Delete(tmpVar.ToString());
                        Session["mmsFilePath" + index] = null;
                    }
                }

                index++;
            }
        }
    }

    /// <summary>
    /// Sends MMS by calling messaging api
    /// </summary>
    /// <param name="mmsAddress">string, phone number</param>
    /// <param name="mmsMessage">string, mms message</param>
    private void SendMultimediaMessage(string mmsAddress, string mmsMessage)
    {
        string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

        HttpWebRequest mmsRequestObject = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/mms/2/messaging/outbox");
        mmsRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);        
        mmsRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundary + "\"\r\n";
        mmsRequestObject.Method = "POST";
        mmsRequestObject.KeepAlive = true;

        UTF8Encoding encoding = new UTF8Encoding();

        byte[] totalpostBytes = encoding.GetBytes(string.Empty);
        string sendMMSData = "Address=" + Server.UrlEncode("tel:" + mmsAddress) + "&Subject=" + Server.UrlEncode(mmsMessage);

        string data = string.Empty;
        data += "--" + boundary + "\r\n";
        data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8\r\nContent-Transfer-Encoding:8bit\r\nContent-ID:<startpart>\r\n\r\n" + sendMMSData + "\r\n";

        totalpostBytes = this.FormMIMEParts(boundary, ref data);

        byte[] byteLastBoundary = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
        int totalSize = totalpostBytes.Length + byteLastBoundary.Length;

        var totalMS = new MemoryStream(new byte[totalSize], 0, totalSize, true, true);
        totalMS.Write(totalpostBytes, 0, totalpostBytes.Length);
        totalMS.Write(byteLastBoundary, 0, byteLastBoundary.Length);

        byte[] finalpostBytes = totalMS.GetBuffer();
        mmsRequestObject.ContentLength = finalpostBytes.Length;

        Stream postStream = null;
        try
        {
            postStream = mmsRequestObject.GetRequestStream();
            postStream.Write(finalpostBytes, 0, finalpostBytes.Length);
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (null != postStream)
            {
                postStream.Close();
            }
        }

        WebResponse mmsResponseObject = mmsRequestObject.GetResponse();
        using (StreamReader streamReader = new StreamReader(mmsResponseObject.GetResponseStream()))
        {
            string mmsResponseData = streamReader.ReadToEnd();
            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
            MmsResponseId deserializedJsonObj = (MmsResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(MmsResponseId));
            messageIDTextBox.Text = deserializedJsonObj.id.ToString();
            this.DrawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString());
            streamReader.Close();
        }
    }

    /// <summary>
    /// Form mime parts for the user input files
    /// </summary>
    /// <param name="boundary">string, boundary data</param>
    /// <param name="data">string, mms message</param>
    /// <returns>returns byte array of files</returns>
    private byte[] FormMIMEParts(string boundary, ref string data)
    {
        UTF8Encoding encoding = new UTF8Encoding();

        byte[] postBytes = encoding.GetBytes(string.Empty);
        byte[] totalpostBytes = encoding.GetBytes(string.Empty);

        if (Session["mmsFilePath1"] != null)
        {
            postBytes = this.GetBytesOfFile(boundary, ref data, Session["mmsFilePath1"].ToString());
            totalpostBytes = postBytes;
        }

        if (Session["mmsFilePath2"] != null)
        {
            if (Session["mmsFilePath1"] != null)
            {
                data = "--" + boundary + "\r\n";
            }
            else
            {
                data += "--" + boundary + "\r\n";
            }

            postBytes = this.GetBytesOfFile(boundary, ref data, Session["mmsFilePath2"].ToString());

            if (Session["mmsFilePath1"] != null)
            {
                var ms2 = JoinTwoByteArrays(totalpostBytes, postBytes);
                totalpostBytes = ms2.GetBuffer();
            }
            else
            {
                totalpostBytes = postBytes;
            }
        }

        if (Session["mmsFilePath3"] != null)
        {
            if (Session["mmsFilePath1"] != null || Session["mmsFilePath2"] != null)
            {
                data = "--" + boundary + "\r\n";
            }
            else
            {
                data += "--" + boundary + "\r\n";
            }

            postBytes = this.GetBytesOfFile(boundary, ref data, Session["mmsFilePath3"].ToString());

            if (Session["mmsFilePath1"] != null || Session["mmsFilePath2"] != null)
            {
                var ms2 = JoinTwoByteArrays(totalpostBytes, postBytes);
                totalpostBytes = ms2.GetBuffer();
            }
            else
            {
                totalpostBytes = postBytes;
            }
        }

        return totalpostBytes;
    }

    /// <summary>
    /// Gets the bytes representation of file along with mime part
    /// </summary>
    /// <param name="boundary">string, boundary message</param>
    /// <param name="data">string, mms message</param>
    /// <param name="filePath">string, filepath</param>
    /// <returns>byte[], representation of file in bytes</returns>
    private byte[] GetBytesOfFile(string boundary, ref string data, string filePath)
    {
        UTF8Encoding encoding = new UTF8Encoding();
        byte[] postBytes = encoding.GetBytes(string.Empty);
        FileStream fileStream = null;
        BinaryReader binaryReader = null;

        try
        {
            string mmsFileName = Path.GetFileName(filePath);

            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            binaryReader = new BinaryReader(fileStream);

            byte[] image = binaryReader.ReadBytes((int)fileStream.Length);

            data += "--" + boundary + "\r\n";
            data += "Content-Disposition:attachment;name=\"" + mmsFileName + "\"\r\n";
            data += "Content-Type:image/gif\r\n";
            data += "Content-ID:<" + mmsFileName + ">\r\n";
            data += "Content-Transfer-Encoding:binary\r\n\r\n";

            byte[] firstPart = encoding.GetBytes(data);
            int newSize = firstPart.Length + image.Length;

            var memoryStream = new MemoryStream(new byte[newSize], 0, newSize, true, true);
            memoryStream.Write(firstPart, 0, firstPart.Length);
            memoryStream.Write(image, 0, image.Length);

            postBytes = memoryStream.GetBuffer();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (null != binaryReader)
            {
                binaryReader.Close();
            }

            if (null != fileStream)
            {
                fileStream.Close();
            }
        }

        return postBytes;
    }

    /// <summary>
    /// Invokes messaging api to send message without any attachments
    /// </summary>
    /// <param name="mmsAddress">string, phone number</param>
    /// <param name="mmsMessage">string, mms message</param>
    private void SendMessageNoAttachments(string mmsAddress, string mmsMessage)
    {
        string boundaryToSend = "----------------------------" + DateTime.Now.Ticks.ToString("x");

        HttpWebRequest mmsRequestObject = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/mms/2/messaging/outbox");
        mmsRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);
        mmsRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundaryToSend + "\"\r\n";
        mmsRequestObject.Method = "POST";
        mmsRequestObject.KeepAlive = true;

        UTF8Encoding encoding = new UTF8Encoding();
        byte[] bytesToSend = encoding.GetBytes(string.Empty);
        string mmsParameters = "Address=" + Server.UrlEncode("tel:" + mmsAddress) + "&Subject=" + Server.UrlEncode(mmsMessage);

        string dataToSend = string.Empty;
        dataToSend += "--" + boundaryToSend + "\r\n";
        dataToSend += "Content-Type: application/x-www-form-urlencoded; charset=UTF-8\r\nContent-Transfer-Encoding: 8bit\r\nContent-Disposition: form-data; name=\"root-fields\"\r\nContent-ID: <startpart>\r\n\r\n" + mmsParameters + "\r\n";
        dataToSend += "--" + boundaryToSend + "--\r\n";
        bytesToSend = encoding.GetBytes(dataToSend);

        int sizeToSend = bytesToSend.Length;
        var memBufToSend = new MemoryStream(new byte[sizeToSend], 0, sizeToSend, true, true);
        memBufToSend.Write(bytesToSend, 0, bytesToSend.Length);
        byte[] finalData = memBufToSend.GetBuffer();
        mmsRequestObject.ContentLength = finalData.Length;

        Stream postStream = null;
        try
        {
            postStream = mmsRequestObject.GetRequestStream();
            postStream.Write(finalData, 0, finalData.Length);
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (null != postStream)
            {
                postStream.Close();
            }
        }

        WebResponse mmsResponseObject = mmsRequestObject.GetResponse();
        using (StreamReader streamReader = new StreamReader(mmsResponseObject.GetResponseStream()))
        {
            string mmsResponseData = streamReader.ReadToEnd();
            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
            MmsResponseId deserializedJsonObj = (MmsResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(MmsResponseId));
            messageIDTextBox.Text = deserializedJsonObj.id.ToString();
            this.DrawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString());
            streamReader.Close();
        }
    }

    /// <summary>
    /// This function adds two byte arrays
    /// </summary>
    /// <param name="firstByteArray">first array of bytes</param>
    /// <param name="secondByteArray">second array of bytes</param>
    /// <returns>returns MemoryStream after joining two byte arrays</returns>
    private static MemoryStream JoinTwoByteArrays(byte[] firstByteArray, byte[] secondByteArray)
    {
        int newSize = firstByteArray.Length + secondByteArray.Length;
        var ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
        ms.Write(firstByteArray, 0, firstByteArray.Length);
        ms.Write(secondByteArray, 0, secondByteArray.Length);
        return ms;
    }

    #endregion
}

#region Data Structures

/// <summary>
/// MmsResponseId object
/// </summary>
public class MmsResponseId
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

/// <summary>
/// Response of GetMmsDelivery
/// </summary>
public class GetDeliveryStatus
{
    /// <summary>
    /// Gets or sets the value of DeliveryInfoList
    /// </summary>
    public DeliveryInfoList DeliveryInfoList
    {
        get;
        set;
    }
}

/// <summary>
/// DeliveryInfoList object
/// </summary>
public class DeliveryInfoList
{
    /// <summary>
    /// Gets or sets the value of resourceURL
    /// </summary>
    public string resourceURL
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of deliveryInfo
    /// </summary>
    public List<deliveryInfo> deliveryInfo 
    { 
        get; 
        set;
    }
}

/// <summary>
/// deliveryInfo object
/// </summary>
public class deliveryInfo
{
    /// <summary>
    /// Gets or sets the value of id
    /// </summary>
    public string id
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of address
    /// </summary>
    public string address
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets the value of deliveryStatus
    /// </summary>
    public string deliverystatus
    {
        get;
        set;
    }
}

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
#endregion