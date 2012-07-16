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
using System.Web.UI;
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
/// MMS_App2 class
/// </summary>
/// <remarks>
/// This is a server side application which also has a web interface. 
/// The application looks for a file called numbers.txt containing MSISDNs of desired recipients, and an image called coupon.jpg, 
/// and message text from a file called subject.txt, and then sends an MMS message with the attachment to every recipient in the list. 
/// This can be triggered via a command line on the server, or through the web interface, which then displays all the returned mmsIds or respective errors
/// </remarks>
public partial class MMS_App2 : System.Web.UI.Page
{
    #region Instance Variables
    /// <summary>
    /// Instance variables for get status table
    /// </summary>
    private Table getStatusTable;
    private Table secondTable;

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string endPoint, accessTokenFilePath, messageFilePath, phoneListFilePath, couponPath, couponFileName;

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string apiKey, secretKey, authCode, accessToken, scope, expirySeconds, refreshToken, refreshTokenExpiryTime;

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private List<string> phoneNumbersList = new List<string>();

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private List<string> invalidPhoneNumbers = new List<string>();

    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private string phoneNumbersParameter = null;
    
    /// <summary>
    /// Instance variables for local processing
    /// </summary>
    private DateTime accessTokenExpiryTime;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    #endregion

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

    #region Events

    /// <summary>
    /// Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        StreamReader streamReader = null;
        try
        {
            BypassCertificateError();

            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";

            this.ReadConfigFile();

            if (!Page.IsPostBack)
            {
                streamReader = new StreamReader(Request.MapPath(this.phoneListFilePath));
                phoneListTextBox.Text = streamReader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendMMSPanel, ex.ToString());
        }
        finally
        {
            if (null != streamReader)
            {
                streamReader.Close();
            }
        }
    }

    /// <summary>
    /// This method will be called when user clicks on send mms button
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void SendButton_Click(object sender, EventArgs e)
    {
        bool ableToGetNumbers = this.GetPhoneNumbers();
        if (ableToGetNumbers == false)
        {
            //this.DrawPanelForFailure(sendMMSPanel, "Specify phone numbers to send");
            return;
        }

        if (this.ReadAndGetAccessToken() == false)
        {
            return;
        }

        this.SendMMS();
    }

    /// <summary>
    /// This method will be called when user clicks on  get status button
    /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void StatusButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(msgIdLabel.Text))
            {
                return;
            }

            if (this.ReadAndGetAccessToken() == false)
            {
                return;
            }

            string mmsId = msgIdLabel.Text;
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
                DeliveryInfoList dinfoList = status.DeliveryInfoList;
            
                this.DrawPanelForGetStatusResult(null, null, null, true);

                foreach (DeliveryInfoRaw deliveryInfo in dinfoList.DeliveryInfo)
                {
                   this.DrawPanelForGetStatusResult(deliveryInfo.Id, deliveryInfo.Address, deliveryInfo.DeliveryStatus, false);
                }

                msgIdLabel.Text = string.Empty;
                mmsStatusResponseStream.Close();
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
            this.DrawPanelForFailure(statusPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.ToString());
        }
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
            this.DrawPanelForFailure(sendMMSPanel, ex.ToString());
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
            postStream.Close();

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd().ToString();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));

                this.accessToken = deserializedJsonObj.access_token.ToString();
                this.expirySeconds = deserializedJsonObj.expires_in.ToString();
                this.refreshToken = deserializedJsonObj.refresh_token.ToString();
                this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in.ToString()));

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
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendMMSPanel, ex.ToString());
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
    /// This function draws table for failed numbers
    /// </summary>
    /// <param name="panelParam">Panel to draw error</param>
    private void DrawPanelForFailedNumbers(Panel panelParam)
    {
        //if (panelParam.HasControls())
        //{
         //   panelParam.Controls.Clear();
        //}

        Table table = new Table();
        table.CssClass = "errorWide";
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();        
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR: Invalid numbers";
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);

        foreach (string number in this.invalidPhoneNumbers)
        {
            TableRow rowTwo = new TableRow();
            TableCell rowTwoCellOne = new TableCell();
            rowTwoCellOne.Text = number.ToString();
            rowTwo.Controls.Add(rowTwoCellOne);
            table.Controls.Add(rowTwo);
        }

        panelParam.Controls.Add(table);
    }

    /// <summary>
    /// This method draws table for get status response
    /// </summary>
    /// <param name="msgid">string, Message Id</param>
    /// <param name="phone">string, phone number</param>
    /// <param name="status">string, status</param>
    /// <param name="headerFlag">bool, headerFlag</param>
    private void DrawPanelForGetStatusResult(string msgid, string phone, string status, bool headerFlag)
    {
        if (headerFlag == true)
        {
            getStatusTable = new Table();
            getStatusTable.CssClass = "successWide";
            getStatusTable.Font.Name = "Sans-serif";
            getStatusTable.Font.Size = 9;

            TableRow rowOne = new TableRow();
            TableCell rowOneCellOne = new TableCell();
            rowOneCellOne.Width = Unit.Pixel(110);
            rowOneCellOne.Font.Bold = true;
            rowOneCellOne.Text = "SUCCESS:";
            rowOne.Controls.Add(rowOneCellOne);
            getStatusTable.Controls.Add(rowOne);
            TableRow rowTwo = new TableRow();
            TableCell rowTwoCellOne = new TableCell();
            rowTwoCellOne.Width = Unit.Pixel(250);
            rowTwoCellOne.Text = "Messages Delivered";

            rowTwo.Controls.Add(rowTwoCellOne);
            getStatusTable.Controls.Add(rowTwo);
            getStatusTable.Controls.Add(rowOne);
            getStatusTable.Controls.Add(rowTwo);
            statusPanel.Controls.Add(getStatusTable);

            secondTable = new Table();
            secondTable.Font.Name = "Sans-serif";
            secondTable.Font.Size = 9;
            secondTable.Width = Unit.Pixel(650);
            TableRow tableRow = new TableRow();
            TableCell tableCell = new TableCell();
            tableCell.Width = Unit.Pixel(300);
            tableCell.Text = "Recipient";
            tableCell.HorizontalAlign = HorizontalAlign.Center;
            tableCell.Font.Bold = true;
            tableRow.Cells.Add(tableCell);
            tableCell = new TableCell();
            tableCell.Font.Bold = true;
            tableCell.Width = Unit.Pixel(300);
            tableCell.Wrap = true;
            tableCell.Text = "Status";
            tableCell.HorizontalAlign = HorizontalAlign.Center;
            tableRow.Cells.Add(tableCell);
            secondTable.Rows.Add(tableRow);
            statusPanel.Controls.Add(secondTable);
        }
        else
        {
            TableRow row = new TableRow();
            TableCell cell1 = new TableCell();
            TableCell cell2 = new TableCell();
            cell1.Text = phone.ToString();
            cell1.Width = Unit.Pixel(300);
            cell1.HorizontalAlign = HorizontalAlign.Center;
            row.Controls.Add(cell1);
            cell2.Text = status.ToString();
            cell2.Width = Unit.Pixel(300);
            cell2.HorizontalAlign = HorizontalAlign.Center;
            row.Controls.Add(cell2);
            secondTable.Controls.Add(row);
            statusPanel.Controls.Add(secondTable);
        }
    }

#endregion

    #region MMS App2 specific methods

    /// <summary>
    /// This method validates the given string as valid msisdn
    /// </summary>
    /// <param name="number">string, phone number</param>
    /// <returns>true/false; true if valid phone number, else false</returns>
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
            return false;
        }

        return true;
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
            this.DrawPanelForFailure(sendMMSPanel, "api_key is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(sendMMSPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(sendMMSPanel, "endPoint is not defined in configuration file");
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
            this.accessTokenFilePath = "MMSApp1AccessToken.txt";
        }

        this.messageFilePath = ConfigurationManager.AppSettings["messageFilePath"];
        if (string.IsNullOrEmpty(this.messageFilePath))
        {
            this.DrawPanelForFailure(sendMMSPanel, "Message file path is missing in configuration file");
            return false;
        }

        this.phoneListFilePath = ConfigurationManager.AppSettings["phoneListFilePath"];
        if (string.IsNullOrEmpty(this.phoneListFilePath))
        {
            this.DrawPanelForFailure(sendMMSPanel, "Phone list file path is missing in configuration file");
            return false;
        }

        this.couponPath = ConfigurationManager.AppSettings["couponPath"];
        if (string.IsNullOrEmpty(this.couponPath))
        {
            this.DrawPanelForFailure(sendMMSPanel, "Coupon path is missing in configuration file");
            return false;
        }

        this.couponFileName = ConfigurationManager.AppSettings["couponFileName"];
        if (string.IsNullOrEmpty(this.couponFileName))
        {
            this.DrawPanelForFailure(sendMMSPanel, "Coupon file name is missing in configuration file");
            return false;
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

        DirectoryInfo dirInfo = new DirectoryInfo(Request.MapPath(this.couponPath));
        FileInfo[] imgFileList = dirInfo.GetFiles();
        int fileindex = 0;
        bool foundFlag = false;
        foreach (FileInfo tempFileInfo in imgFileList)
        {
            if (tempFileInfo.Name.ToLower().Equals(this.couponFileName.ToLower()))
            {
                foundFlag = true;
                break;
            }
            else
            {
                fileindex++;
            }
        }

        if (foundFlag == false)
        {
            this.DrawPanelForFailure(sendMMSPanel, "Coupon doesnt exists");
            return false;
        }

        Image1.ImageUrl = string.Format("{0}{1}", this.couponPath, imgFileList[fileindex].Name);

        StreamReader streamReader = null;
        try
        {
            streamReader = new StreamReader(Request.MapPath(this.messageFilePath));
            subjectLabel.Text = streamReader.ReadToEnd();            
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (null != streamReader)
            {
                streamReader.Close();
            }
        }

        return true;
    }

    /// <summary>
    /// Sends MMS message by invoking Send MMS api
    /// </summary>
    private void SendMMS()
    {
        Stream postStream = null;
        FileStream fileStream = null;
        BinaryReader binaryReader = null;
        try
        {
            string mmsFilePath = Request.MapPath(this.couponPath);
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

            HttpWebRequest mmsRequestObject = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/mms/2/messaging/outbox");
            mmsRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);
            mmsRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundary + "\"\r\n";
            mmsRequestObject.Method = "POST";
            mmsRequestObject.KeepAlive = true;

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = encoding.GetBytes(string.Empty);
            string sendMMSData = this.phoneNumbersParameter + "&Subject=" + Server.UrlEncode(subjectLabel.Text.ToString());
            string data = string.Empty;

            fileStream = new FileStream(mmsFilePath + this.couponFileName, FileMode.Open, FileAccess.Read);
            binaryReader = new BinaryReader(fileStream);
            byte[] image = binaryReader.ReadBytes((int)fileStream.Length);

            data += "--" + boundary + "\r\n";
            data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8\r\nContent-Transfer-Encoding:8bit\r\nContent-ID:<startpart>\r\n\r\n" + sendMMSData + "\r\n";
            data += "--" + boundary + "\r\n";
            data += "Content-Disposition:attachment;name=\"" + "coupon.jpg" + "\"\r\n";
            data += "Content-Type:image/gif\r\n";
            data += "Content-ID:<" + "coupon.jpg" + ">\r\n";
            data += "Content-Transfer-Encoding:binary\r\n\r\n";
            byte[] firstPart = encoding.GetBytes(data);
            int newSize = firstPart.Length + image.Length;

            var memoryStream = new MemoryStream(new byte[newSize], 0, newSize, true, true);
            memoryStream.Write(firstPart, 0, firstPart.Length);
            memoryStream.Write(image, 0, image.Length);

            byte[] secondpart = memoryStream.GetBuffer();
            byte[] thirdpart = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
            newSize = secondpart.Length + thirdpart.Length;

            var memoryStream2 = new MemoryStream(new byte[newSize], 0, newSize, true, true);
            memoryStream2.Write(secondpart, 0, secondpart.Length);
            memoryStream2.Write(thirdpart, 0, thirdpart.Length);

            postBytes = memoryStream2.GetBuffer();
            mmsRequestObject.ContentLength = postBytes.Length;

            postStream = mmsRequestObject.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            WebResponse mmsResponseObject = mmsRequestObject.GetResponse();
            using (StreamReader streamReader = new StreamReader(mmsResponseObject.GetResponseStream()))
            {
                string mmsResponseData = streamReader.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                MmsResponseId deserializedJsonObj = (MmsResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(MmsResponseId));

                msgIdLabel.Text = deserializedJsonObj.Id;
                this.DrawPanelForSuccess(sendMMSPanel, deserializedJsonObj.Id);
                streamReader.Close();
            }
            /*if (this.invalidPhoneNumbers.Count > 0)
            {
                this.DrawPanelForFailedNumbers(sendMMSPanel);
            }*/
            mmsRequestObject = null;
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
            this.DrawPanelForFailure(sendMMSPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(sendMMSPanel, ex.ToString());
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

            if (null != postStream)
            {
                postStream.Close();
            }
        }
    }

    /// <summary>
    /// This method gets the phone numbers present in phonenumber text box and validates each phone number and prepares valid and invalid phone number lists
    /// and returns a bool value indicating if able to get the phone numbers.
    /// </summary>
    /// <returns>true/false; true if able to get valis phone numbers, else false</returns>
    private bool GetPhoneNumbers()
    {
        if (string.IsNullOrEmpty(phoneListTextBox.Text))
        {
            return false;
        }

        string[] phoneNumbers = phoneListTextBox.Text.Split(',');
        foreach (string phoneNum in phoneNumbers)
        {
            if (!string.IsNullOrEmpty(phoneNum))
            {
                this.phoneNumbersList.Add(phoneNum);
            }
        }

        foreach (string phoneNo in this.phoneNumbersList)
        {
            if (this.IsValidMISDN(phoneNo) == true)
            {
                if (phoneNo.StartsWith("tel:"))
                {
                    string phoneNumberWithoutHyphens = phoneNo.Replace("-", string.Empty);
                    this.phoneNumbersParameter = this.phoneNumbersParameter + "Address=" + Server.UrlEncode(phoneNumberWithoutHyphens.ToString()) + "&";
                }
                else
                {
                    string phoneNumberWithoutHyphens = phoneNo.Replace("-", string.Empty);
                    this.phoneNumbersParameter = this.phoneNumbersParameter + "Address=" + Server.UrlEncode("tel:" + phoneNumberWithoutHyphens.ToString()) + "&";
                }
            }
            else
            {
                this.invalidPhoneNumbers.Add(phoneNo);
            }
        }

        //if (string.IsNullOrEmpty(this.phoneNumbersParameter))
        //{
            if (this.invalidPhoneNumbers.Count > 0)
            {
                this.DrawPanelForFailedNumbers(sendMMSPanel);
                return false;
            }
            //return false;
       // }

        return true;
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
    /// Gets or sets the value of Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the value of Resource Reference
    /// </summary>
    public ResourceReferenceRaw ResourceReference
    { 
        get; 
        set; 
    }
}

/// <summary>
/// ResourceReferenceRaw object
/// </summary>
public class ResourceReferenceRaw
{
    /// <summary>
    /// Gets or sets the value of ResourceUrl
    /// </summary>
    public string ResourceUrl
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

/// <summary>
/// GetDeliveryStatus object
/// </summary>
public class GetDeliveryStatus
{
    /// <summary>
    /// Gets or sets the value of DeliveryInfoList object
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
    /// Gets or sets the value of Resource Url
    /// </summary>
    public string ResourceUrl
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of DeliveryInfo List
    /// </summary>
    public List<DeliveryInfoRaw> DeliveryInfo 
    { 
        get; 
        set; 
    }
}

/// <summary>
/// DeliveryInfoRaw Object
/// </summary>
public class DeliveryInfoRaw
{
    /// <summary>
    /// Gets or sets the value of Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the value of Address
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets or sets the value of DeliveryStatus
    /// </summary>
    public string DeliveryStatus { get; set; }
}

#endregion