// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
#endregion

/// <summary>
/// Default Class
/// </summary>
public partial class Payment_App1 : System.Web.UI.Page
{
    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private string accessTokenFilePath, refundFile, apiKey, secretKey, accessToken, endPoint,
        scope, expirySeconds, refreshToken, accessTokenExpiryTime, refreshTokenExpiryTime,
        amount, channel, description, merchantTransactionId, merchantProductId, merchantApplicationId,
        transactionTimeString, signedPayload, signedSignature, notaryURL, notificationDetailsFile;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private Table successTable, failureTable, successTableGetTransaction, notificationDetailsTable, successTableRefund;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private int category, noOfNotificationsToDisplay;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private Uri merchantRedirectURI;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private DateTime transactionTime;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private int refundCountToDisplay = 0;

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private List<KeyValuePair<string, string>> refundList = new List<KeyValuePair<string, string>>();

    /// <summary>
    /// Global Variable Declaration
    /// </summary>
    private bool latestFive = true;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    /// <summary>
    /// This function is used to neglect the ssl handshake error with authentication server.
    /// </summary>
    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    /// <summary>
    /// Method to process create transaction response
    /// </summary>
    public void ProcessCreateTransactionResponse()
    {
        lbltrancode.Text = Request["TransactionAuthCode"].ToString();
        lbltranid.Text = Session["merTranId"].ToString();
        transactionSuccessTable.Visible = true;
        GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " + Session["merTranId"].ToString();
        GetTransactionAuthCode.Text = "Auth Code: " + Request["TransactionAuthCode"].ToString();
        GetTransactionTransID.Text = "Transaction ID: ";
        Session["tempMerTranId"] = Session["merTranId"].ToString();
        Session["merTranId"] = null;
        Session["TranAuthCode"] = Request["TransactionAuthCode"].ToString();
        return;
    }

    /// <summary>
    /// This function reads the Access Token File and stores the values of access token, expiry seconds
    /// refresh token, last access token time and refresh token expiry time
    /// This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    /// </summary>
    /// <returns>Returns Boolean</returns>
    public bool ReadAccessTokenFile()
    {
        try
        {
            FileStream file = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader sr = new StreamReader(file);
            this.accessToken = sr.ReadLine();
            this.expirySeconds = sr.ReadLine();
            this.refreshToken = sr.ReadLine();
            this.accessTokenExpiryTime = sr.ReadLine();
            this.refreshTokenExpiryTime = sr.ReadLine();
            sr.Close();
            file.Close();
        }
        catch (Exception)
        {
            return false;
        }

        if ((this.accessToken == null) || (this.expirySeconds == null) || (this.refreshToken == null) || (this.accessTokenExpiryTime == null) || (this.refreshTokenExpiryTime == null))
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
    public string IsTokenValid()
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
    /// This function get the access token based on the type parameter type values.
    /// If type value is 1, access token is fetch for client credential flow
    /// If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    /// </summary>
    /// <param name="type">Type as Interger</param>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns Boolean</returns>
    public bool GetAccessToken(int type, Panel panelParam)
    {
        FileStream fileStream = null;
        StreamWriter streamWriter = null;

        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            string oauthURL;
            oauthURL = string.Empty + this.endPoint + "/oauth/token";
            WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(oauthURL);
            accessTokenRequest.Method = "POST";

            string oauthParameters = string.Empty;
            if (type == 1) // Client Credential flow
            {
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=PAYMENT";
            }
            else // Refresh Token flow
            {
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=refresh_token" + "&refresh_token=" + this.refreshToken;
            }

            accessTokenRequest.ContentType = "application/x-www-form-urlencoded";

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = encoding.GetBytes(oauthParameters);
            accessTokenRequest.ContentLength = postBytes.Length;

            Stream postStream = accessTokenRequest.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd().ToString();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));
                this.accessToken = deserializedJsonObj.access_token;
                this.expirySeconds = deserializedJsonObj.expires_in;
                this.refreshToken = deserializedJsonObj.refresh_token;

                this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(this.expirySeconds)).ToLongDateString() + " " + currentServerTime.AddSeconds(Convert.ToDouble(this.expirySeconds)).ToLongTimeString();

                DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                if (deserializedJsonObj.expires_in.Equals("0"))
                {
                    int defaultAccessTokenExpiresIn = 100; // In Years
                    this.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongDateString() + " " + currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongTimeString();                    
                }

                this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();

                fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(this.accessToken);
                streamWriter.WriteLine(this.expirySeconds);
                streamWriter.WriteLine(this.refreshToken);
                streamWriter.WriteLine(this.accessTokenExpiryTime);
                streamWriter.WriteLine(this.refreshTokenExpiryTime);
                
                streamWriter.Close();
                fileStream.Close();
                
                accessTokenResponseStream.Close();
                return true;
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    this.DrawPanelForFailure(panelParam, new StreamReader(stream).ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(panelParam, ex.ToString());
        }
        finally
        {
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
    /// Method to add row to refund section.
    /// </summary>
    /// <param name="transaction">Transaction as String</param>
    /// <param name="merchant">Merchant as string</param>
    public void AddRowToRefundSection(string transaction, string merchant)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Left;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);
        //// cellOne.Text = transaction.ToString();
        RadioButton rbutton = new RadioButton();
        rbutton.Text = transaction.ToString();
        rbutton.GroupName = "RefundSection";
        rbutton.ID = transaction.ToString();
        cellOne.Controls.Add(rbutton);
        rowOne.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.CssClass = "cell";
        cellTwo.Width = Unit.Pixel(100);
        rowOne.Controls.Add(cellTwo);

        TableCell cellThree = new TableCell();
        cellThree.CssClass = "cell";
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Width = Unit.Pixel(240);
        cellThree.Text = merchant.ToString();
        rowOne.Controls.Add(cellThree);

        TableCell cellFour = new TableCell();
        cellFour.CssClass = "cell";
        rowOne.Controls.Add(cellFour);

        refundTable.Controls.Add(rowOne);
    }

    /// <summary>
    /// Method to draw refund section
    /// </summary>
    /// <param name="onlyRow">Row details</param>
    public void DrawRefundSection(bool onlyRow)
    {
        try
        {
            if (onlyRow == false)
            {
                TableRow headingRow = new TableRow();
                TableCell headingCellOne = new TableCell();
                headingCellOne.HorizontalAlign = HorizontalAlign.Left;
                headingCellOne.CssClass = "cell";
                headingCellOne.Width = Unit.Pixel(200);
                headingCellOne.Font.Bold = true;
                headingCellOne.Text = "Transaction ID";
                headingRow.Controls.Add(headingCellOne);
                TableCell headingCellTwo = new TableCell();
                headingCellTwo.CssClass = "cell";
                headingCellTwo.Width = Unit.Pixel(100);
                headingRow.Controls.Add(headingCellTwo);
                TableCell headingCellThree = new TableCell();
                headingCellThree.CssClass = "cell";
                headingCellThree.HorizontalAlign = HorizontalAlign.Left;
                headingCellThree.Width = Unit.Pixel(240);
                headingCellThree.Font.Bold = true;
                headingCellThree.Text = "Merchant Transaction ID";
                headingRow.Controls.Add(headingCellThree);
                TableCell headingCellFour = new TableCell();
                headingCellFour.CssClass = "warning";
                LiteralControl warningMessage = new LiteralControl("<b>WARNING:</b><br/>You must use Get Transaction Status to get the Transaction ID before you can refund it.");
                headingCellFour.Controls.Add(warningMessage);
                headingRow.Controls.Add(headingCellFour);
                refundTable.Controls.Add(headingRow);
            }

            this.ResetRefundList();
            this.GetRefundListFromFile();

            int tempCountToDisplay = 1;
            while ((tempCountToDisplay <= this.refundCountToDisplay) && (tempCountToDisplay <= this.refundList.Count) && (this.refundList.Count > 0))
            {
                this.AddRowToRefundSection(this.refundList[tempCountToDisplay - 1].Key, this.refundList[tempCountToDisplay - 1].Value);
                tempCountToDisplay++;
            }

            //// addButtonToRefundSection("Refund Transaction");
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(newTransactionPanel, ex.ToString());
        }
    }

    /// <summary>
    /// Method to update refund list to file.
    /// </summary>
    public void UpdateRefundListToFile()
    {
        if (this.refundList.Count != 0)
        {
            this.refundList.Reverse(0, this.refundList.Count);
        }

        using (StreamWriter sr = File.CreateText(Request.MapPath(this.refundFile)))
        {
            int tempCount = 0;
            while (tempCount < this.refundList.Count)
            {
                string lineToWrite = this.refundList[tempCount].Key + ":-:" + this.refundList[tempCount].Value;
                sr.WriteLine(lineToWrite);
                tempCount++;
            }

            sr.Close();
        }
    }

    /// <summary>
    /// Method to reset refund list
    /// </summary>
    public void ResetRefundList()
    {
        this.refundList.RemoveRange(0, this.refundList.Count);
    }

    /// <summary>
    /// Method to check item in refund file.
    /// </summary>
    /// <param name="transactionid">Transaction Id</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id</param>
    /// <returns>Return Boolean</returns>
    public bool CheckItemInRefundFile(string transactionid, string merchantTransactionId)
    {
        string line;
        string lineToFind = transactionid + ":-:" + merchantTransactionId;
        System.IO.StreamReader file = new System.IO.StreamReader(Request.MapPath(this.refundFile));
        while ((line = file.ReadLine()) != null)
        {
            if (line.CompareTo(lineToFind) == 0)
            {
                file.Close();
                return true;
            }
        }

        file.Close();
        return false;
    }

    /// <summary>
    /// Method to write refund to file.
    /// </summary>
    /// <param name="transactionid">Transaction Id</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id</param>
    public void WriteRefundToFile(string transactionid, string merchantTransactionId)
    {
        //// Read the refund file for the list of transactions and store locally
        //// FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        //// StreamWriter sr = new StreamWriter(file);
        //// DateTime junkTime = DateTime.UtcNow;
        //// string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        using (StreamWriter appendContent = File.AppendText(Request.MapPath(this.refundFile)))
        {
            string line = transactionid + ":-:" + merchantTransactionId;
            appendContent.WriteLine(line);
            appendContent.Flush();
            appendContent.Close();
            //// file.Close();
        }
    }

    /// <summary>
    /// Method to get refung list from file.
    /// </summary>
    public void GetRefundListFromFile()
    {
        //// Read the refund file for the list of transactions and store locally
        FileStream file = new FileStream(Request.MapPath(this.refundFile), FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string line;

        while ((line = sr.ReadLine()) != null)
        {
            string[] refundKeys = Regex.Split(line, ":-:");
            if (refundKeys[0] != null && refundKeys[1] != null)
            {
                this.refundList.Add(new KeyValuePair<string, string>(refundKeys[0], refundKeys[1]));
            }
        }

        sr.Close();
        file.Close();
        this.refundList.Reverse(0, this.refundList.Count);
    }

    /// <summary>
    /// This function is used to read access token file and validate the access token
    /// this function returns true if access token is valid, or else false is returned
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Retunr Boolean</returns>
    public bool ReadAndGetAccessToken(Panel panelParam)
    {
        bool result = true;
        if (this.ReadAccessTokenFile() == false)
        {
            result = this.GetAccessToken(1, panelParam);
        }
        else
        {
            string tokenValidity = this.IsTokenValid();
            if (tokenValidity.CompareTo("REFRESH_TOKEN") == 0)
            {
                result = this.GetAccessToken(2, panelParam);
            }
            else if (string.Compare(this.IsTokenValid(), "INVALID_ACCESS_TOKEN") == 0)
            {
                result = this.GetAccessToken(1, panelParam);
            }
        }

        return result;
    }

    /// <summary>
    /// Page Load method
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        transactionSuccessTable.Visible = false;
        tranGetStatusTable.Visible = false;
        refundSuccessTable.Visible = false;
        DateTime currentServerTime = DateTime.UtcNow;
        serverTimeLabel.Text = string.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC"; //// Convert.ToString(Session["merTranId"]);

        bool ableToReadFromConfig = this.ReadConfigFile();

        if (ableToReadFromConfig == false)
        {
            return;
        }

        if ((Request["ret_signed_payload"] != null) && (Request["ret_signature"] != null))
        {
            this.signedPayload = Request["ret_signed_payload"].ToString();
            this.signedSignature = Request["ret_signature"].ToString();
            Session["signedPayLoad"] = this.signedPayload.ToString();
            Session["signedSignature"] = this.signedSignature.ToString();
            this.ProcessNotaryResponse();
        }
        else if ((Request["TransactionAuthCode"] != null) && (Session["merTranId"] != null))
        {
            this.ProcessCreateTransactionResponse();
        }
        else if ((Request["shown_notary"] != null) && (Session["processNotary"] != null))
        {
            Session["processNotary"] = null;
            GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " + Session["tempMerTranId"].ToString();
            GetTransactionAuthCode.Text = "Auth Code: " + Session["TranAuthCode"].ToString();
        }
        
        refundTable.Controls.Clear();
        this.DrawRefundSection(false);
        this.DrawNotificationTableHeaders();
        this.GetNotificationDetails();
        return;
    }

    /// <summary>
    /// Reads from config file
    /// </summary>
    /// <returns>true/false; true if able to read else false</returns>
    private bool ReadConfigFile()
    {
        this.apiKey = ConfigurationManager.AppSettings["api_key"];
        if (string.IsNullOrEmpty(this.apiKey))
        {
            this.DrawPanelForFailure(newTransactionPanel, "api_key is not defined in configuration file");
            return false;
        }

        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(newTransactionPanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(newTransactionPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "~\\PayApp1AccessToken.txt";
        }

        this.refundFile = ConfigurationManager.AppSettings["refundFile"];
        if (string.IsNullOrEmpty(this.refundFile))
        {
            this.refundFile = "~\\refund.txt";
        }

        this.refundCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["refundCountToDisplay"]);
        if (string.IsNullOrEmpty(Convert.ToString(this.refundCountToDisplay)))
        {
            this.refundCountToDisplay = 5;
        }

       // this.noOfNotificationsToDisplay = ConfigurationManager.AppSettings["noOfNotificationsToDisplay"];
        if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["noOfNotificationsToDisplay"]))
        {
            this.noOfNotificationsToDisplay = 5;
        }
        else
        {
            noOfNotificationsToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["noOfNotificationsToDisplay"]);
        }

        this.notificationDetailsFile = ConfigurationManager.AppSettings["notificationDetailsFile"];
        if (string.IsNullOrEmpty(this.notificationDetailsFile))
        {
            this.notificationDetailsFile = "~\\notificationDetailsFile.txt";
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "PAYMENT";
        }

        if (ConfigurationManager.AppSettings["DisableLatestFive"] != null)
        {
            this.latestFive = false;
        }

        this.notaryURL = ConfigurationManager.AppSettings["notaryURL"];
        if (string.IsNullOrEmpty(this.notaryURL))
        {
            this.DrawPanelForFailure(newTransactionPanel, "notaryURL is not defined in configuration file");
            return false;
        }

        
        if (ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"] == null)
        {
            this.DrawPanelForFailure(newTransactionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file");
            return false;;
        }

        this.merchantRedirectURI = new Uri(ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"]);
        
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
    /// New Transaction event
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void NewTransactionButton_Click(object sender, EventArgs e)
    {
        this.ReadTransactionParametersFromConfigurationFile();
        string payLoadString = "{\"Amount\":" + this.amount.ToString() + ",\"Category\":" + this.category.ToString() + ",\"Channel\":\"" +
                        this.channel.ToString() + "\",\"Description\":\"" + this.description.ToString() + "\",\"MerchantTransactionId\":\""
                        + this.merchantTransactionId.ToString() + "\",\"MerchantProductId\":\"" + this.merchantProductId.ToString()
                        + "\",\"MerchantPaymentRedirectUrl\":\"" + this.merchantRedirectURI.ToString() + "\"}";
        Session["payloadData"] = payLoadString.ToString();
        Response.Redirect(this.notaryURL.ToString() + "?request_to_sign=" + payLoadString.ToString() + "&goBackURL=" + this.merchantRedirectURI.ToString() + "&api_key=" + this.apiKey.ToString() + "&secret_key=" + this.secretKey.ToString());
    }

    /// <summary>
    /// Event to get transaction.
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void GetTransactionButton_Click(object sender, EventArgs e)
    {
        try
        {
            string keyValue = string.Empty;
            string resourcePathString = string.Empty;
            if (Radio_TransactionStatus.SelectedIndex == 0)
            {
                keyValue = GetTransactionMerchantTransID.Text.ToString().Replace("Merchant Transaction ID: ", string.Empty);
                if (keyValue.Length == 0)
                {
                    return;
                }

                resourcePathString = string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/MerchantTransactionId/" + keyValue.ToString();
            }

            if (Radio_TransactionStatus.SelectedIndex == 1)
            {
                keyValue = GetTransactionAuthCode.Text.ToString().Replace("Auth Code: ", string.Empty);
                if (keyValue.Length == 0)
                {
                    return;
                }

                resourcePathString = string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/TransactionAuthCode/" + keyValue.ToString();
            }

            if (Radio_TransactionStatus.SelectedIndex == 2)
            {
                keyValue = GetTransactionTransID.Text.ToString().Replace("Transaction ID: ", string.Empty);
                if (keyValue.Length == 0)
                {
                    return;
                }

                resourcePathString = string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/TransactionId/" + keyValue.ToString();
            }

            if (this.ReadAndGetAccessToken(newTransactionStatusPanel) == true)
            {
                if (this.accessToken == null || this.accessToken.Length <= 0)
                {
                    return;
                }

                //// resourcePathString = resourcePathString + "?access_token=" + this.access_token.ToString();
                //// HttpWebRequest objRequest = (HttpWebRequest) System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + Session["TranAuthCode"].ToString() + "?access_token=" + access_token.ToString());
                HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(resourcePathString);
                objRequest.Method = "GET";
                objRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                HttpWebResponse getTransactionStatusResponseObject = (HttpWebResponse)objRequest.GetResponse();
                using (StreamReader getTransactionStatusResponseStream = new StreamReader(getTransactionStatusResponseObject.GetResponseStream()))
                {
                    string getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    TransactionResponse deserializedJsonObj = (TransactionResponse)deserializeJsonObject.Deserialize(getTransactionStatusResponseData, typeof(TransactionResponse));
                    GetTransactionTransID.Text = "Transaction ID: " + deserializedJsonObj.TransactionId.ToString();
                    //lblstatusTranId.Text = deserializedJsonObj.TransactionId.ToString();
                    //lblstatusMerTranId.Text = deserializedJsonObj.MerchantTransactionId.ToString();
                    //DrawPanelForFailure(newTransactionStatusPanel, getTransactionStatusResponseData);
                    if (this.CheckItemInRefundFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString()) == false)
                    {
                        this.WriteRefundToFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString());
                    }

                    refundTable.Controls.Clear();
                    this.DrawRefundSection(false);
                    tranGetStatusTable.Visible = true;
                    this.DrawPanelForGetTransactionSuccess(newTransactionStatusPanel);
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Amount", deserializedJsonObj.Amount.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Channel ", deserializedJsonObj.Channel.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Description", deserializedJsonObj.Description.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantId", deserializedJsonObj.MerchantId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantTransactionId", deserializedJsonObj.MerchantTransactionId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "OriginalTransactionId", deserializedJsonObj.OriginalTransactionId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionId", deserializedJsonObj.TransactionId.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionType", deserializedJsonObj.TransactionType.ToString());
                    this.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Version", deserializedJsonObj.Version.ToString());
                    getTransactionStatusResponseStream.Close();
                }
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    this.DrawPanelForFailure(newTransactionStatusPanel, new StreamReader(stream).ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(newTransactionStatusPanel, ex.ToString());
        }
    }

    /// <summary>
    /// Method to be triggered on Get Notification button click
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnGetNotification_Click(object sender, EventArgs e)
    {
        this.notificationDetailsTable.Controls.Clear();
        this.DrawNotificationTableHeaders();
        this.GetNotificationDetails();
    }

    /// <summary>
    /// Event to view notary
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnViewNotary_Click(object sender, EventArgs e)
    {
        if ((Session["payloadData"] != null) && (Session["signedPayLoad"] != null) && (Session["signedSignature"] != null))
        {
            Session["processNotary"] = "notary";
            Response.Redirect(this.notaryURL.ToString() + "?signed_payload=" + Session["signedPayLoad"].ToString() + "&goBackURL=" + this.merchantRedirectURI.ToString() + "&signed_signature=" + Session["signedSignature"].ToString() + "&signed_request=" + Session["payloadData"].ToString());
        }
    }

    /// <summary>
    /// Event for refund transaction
    /// </summary>
    /// <param name="sender">Sender Information</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnRefundTransaction_Click1(object sender, EventArgs e)
    {
        string transactionToRefund = string.Empty;
        bool recordFound = false;
        string strReq = "{\"TransactionOperationStatus\":\"Refunded\",\"RefundReasonCode\":1,\"RefundReasonText\":\"Customer was not happy\"}";
        // string strReq = "{\"RefundReasonCode\":1,\"RefundReasonText\":\"Customer was not happy\"}";
        string dataLength = string.Empty;
        try
        {
            if (this.refundList.Count > 0)
            {
                foreach (Control refundTableRow in refundTable.Controls)
                {
                    if (refundTableRow is TableRow)
                    {
                        foreach (Control refundTableRowCell in refundTableRow.Controls)
                        {
                            if (refundTableRowCell is TableCell)
                            {
                                foreach (Control refundTableCellControl in refundTableRowCell.Controls)
                                {
                                    if (refundTableCellControl is RadioButton)
                                    {
                                        if (((RadioButton)refundTableCellControl).Checked)
                                        {
                                            transactionToRefund = ((RadioButton)refundTableCellControl).Text.ToString();
                                            //// refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
                                            recordFound = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (recordFound == true)
                {
                    if (this.ReadAndGetAccessToken(refundPanel) == true)
                    {
                        if (this.accessToken == null || this.accessToken.Length <= 0)
                        {
                            return;
                        }
                        //// String getTransactionStatusResponseData;
                        //// WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.FQDN + "/rest/3/Commerce/Payment/Transactions/" + transactionToRefund.ToString() + "?access_token=" + this.access_token.ToString() + "&Action=refund");
                        //WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/" + transactionToRefund.ToString() + "?Action=refund");
                        WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/" + transactionToRefund.ToString());
                        objRequest.Method = "PUT";
                        objRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                        objRequest.ContentType = "application/json";
                        UTF8Encoding encoding = new UTF8Encoding();
                        byte[] postBytes = encoding.GetBytes(strReq);
                        objRequest.ContentLength = postBytes.Length;
                        Stream postStream = objRequest.GetRequestStream();
                        postStream.Write(postBytes, 0, postBytes.Length);
                        dataLength = postBytes.Length.ToString();
                        postStream.Close();
                        WebResponse refundTransactionResponeObject = (WebResponse)objRequest.GetResponse();
                        using (StreamReader refundResponseStream = new StreamReader(refundTransactionResponeObject.GetResponseStream()))
                        {
                            string refundTransactionResponseData = refundResponseStream.ReadToEnd();
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            RefundResponse deserializedJsonObj = (RefundResponse)deserializeJsonObject.Deserialize(refundTransactionResponseData, typeof(RefundResponse));
                            //lbRefundTranID.Text = deserializedJsonObj.TransactionId.ToString();
                            refundSuccessTable.Visible = true;
                            DrawPanelForRefundSuccess(refundPanel);
                            AddRowToRefundSuccessPanel(refundPanel, "CommitConfirmationId", deserializedJsonObj.CommitConfirmationId);
                            AddRowToRefundSuccessPanel(refundPanel, "IsSuccess", deserializedJsonObj.IsSuccess);
                            AddRowToRefundSuccessPanel(refundPanel, "OriginalPurchaseAmount", deserializedJsonObj.OriginalPurchaseAmount);
                            AddRowToRefundSuccessPanel(refundPanel, "TransactionId", deserializedJsonObj.TransactionId);
                            AddRowToRefundSuccessPanel(refundPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus);
                            AddRowToRefundSuccessPanel(refundPanel, "Version", deserializedJsonObj.Version);
                            refundResponseStream.Close();
                            if (this.latestFive == false)
                            {
                                this.refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
                                this.UpdateRefundListToFile();
                                this.ResetRefundList();
                                refundTable.Controls.Clear();
                                this.DrawRefundSection(false);
                                GetTransactionMerchantTransID.Text = "Merchant Transaction ID: ";
                                GetTransactionAuthCode.Text = "Auth Code: ";
                                GetTransactionTransID.Text = "Transaction ID: ";
                            }
                        }
                    }
                }
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    this.DrawPanelForFailure(refundPanel, new StreamReader(stream).ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            //// + strReq + transactionToRefund.ToString() + dataLength
            this.DrawPanelForFailure(refundPanel, ex.ToString() + strReq + transactionToRefund.ToString() + dataLength);
        }
    }

    /// <summary>
    /// Method to read transaction parameters from configuration file.
    /// </summary>
    private void ReadTransactionParametersFromConfigurationFile()
    {
        this.transactionTime = DateTime.UtcNow;
        this.transactionTimeString = string.Format("{0:dddMMMddyyyyHHmmss}", this.transactionTime);
        if (Radio_TransactionProductType.SelectedIndex == 0)
        {
            this.amount = "0.99";
        }
        else if (Radio_TransactionProductType.SelectedIndex == 1)
        {
            this.amount = "2.99";
        }
        
        Session["tranType"] = Radio_TransactionProductType.SelectedIndex.ToString();
        if (ConfigurationManager.AppSettings["Category"] == null)
        {
            this.DrawPanelForFailure(newTransactionPanel, "Category is not defined in configuration file");
            return;
        }

        this.category = Convert.ToInt32(ConfigurationManager.AppSettings["Category"]);
        if (ConfigurationManager.AppSettings["Channel"] == null)
        {
            this.channel = "MOBILE_WEB";
        }
        else
        {
            this.channel = ConfigurationManager.AppSettings["Channel"];
        }

        this.description = "TrDesc" + this.transactionTimeString;
        this.merchantTransactionId = "TrId" + this.transactionTimeString;
        Session["merTranId"] = this.merchantTransactionId.ToString();
        this.merchantProductId = "ProdId" + this.transactionTimeString;
        this.merchantApplicationId = "MerAppId" + this.transactionTimeString;
    }

    /// <summary>
    /// Method to process notary response
    /// </summary>
    private void ProcessNotaryResponse()
    {
        if (Session["tranType"] != null)
        {
            Radio_TransactionProductType.SelectedIndex = Convert.ToInt32(Session["tranType"].ToString());
            Session["tranType"] = null;
        }

        Response.Redirect(this.endPoint + "/rest/3/Commerce/Payment/Transactions?clientid=" + this.apiKey.ToString() + "&SignedPaymentDetail=" + this.signedPayload.ToString() + "&Signature=" + this.signedSignature.ToString());
    }

    /// <summary>
    /// Method to get notification details
    /// </summary>
    private void GetNotificationDetails()
    {
        StreamReader notificationDetailsStream = null;
        string notificationDetail = string.Empty;
        if (!File.Exists(Request.MapPath(this.notificationDetailsFile)))
        {
            return;
        }
        try
        {
            using (notificationDetailsStream = File.OpenText(Request.MapPath(this.notificationDetailsFile)))
            {
                notificationDetail = notificationDetailsStream.ReadToEnd();
                notificationDetailsStream.Close();
            }
            string[] notificationDetailArray = notificationDetail.Split('$');
            int noOfNotifications = 0;
            if (null != notificationDetailArray)
            {
                noOfNotifications = notificationDetailArray.Length-1;
            }
            int count = 0;

            while (noOfNotifications >= 0)
            {
                string[] notificationDetails = notificationDetailArray[noOfNotifications].Split(':');
                if (count <= noOfNotificationsToDisplay)
                {
                    if (notificationDetails.Length == 3)
                    {
                        this.AddRowToNotificationTable(notificationDetails[0], notificationDetails[1], notificationDetails[2]);
                    }
                }
                else
                {
                    break;
                }
                count++;
                noOfNotifications--;
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(notificationPanel, ex.ToString());
        }
        finally
        {
            if (null != notificationDetailsStream)
            {
                notificationDetailsStream.Close();
            }
        }
    }

    /// <summary>
    /// Method to add rows to notification response table with notification details
    /// </summary>
    /// <param name="notificationId">Notification Id</param>
    /// <param name="notificationType">Notification Type</param>
    /// <param name="transactionId">Transaction Id</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id</param>
    private void AddRowToNotificationTable(string notificationId, string notificationType, string transactionId)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Left;
        cellOne.Text = notificationId;
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);

        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = notificationType;
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        TableCell cellFour = new TableCell();
        cellFour.Width = Unit.Pixel(50);
        row.Controls.Add(cellFour);

        TableCell cellFive = new TableCell();
        cellFive.HorizontalAlign = HorizontalAlign.Left;
        cellFive.Text = transactionId;
        cellFive.Width = Unit.Pixel(300);
        row.Controls.Add(cellFive);
        TableCell cellSix = new TableCell();
        cellSix.Width = Unit.Pixel(50);
        row.Controls.Add(cellSix);

        this.notificationDetailsTable.Controls.Add(row);
        notificationPanel.Controls.Add(this.notificationDetailsTable);
    }

    /// <summary>
    /// Method to display notification response table with headers
    /// </summary>
    private void DrawNotificationTableHeaders()
    {
        this.notificationDetailsTable = new Table();
        this.notificationDetailsTable.Font.Name = "Sans-serif";
        this.notificationDetailsTable.Font.Size = 8;
        this.notificationDetailsTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellOne.Text = "Notification ID";
        rowOneCellOne.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellOne);
        TableCell rowOneCellTwo = new TableCell();
        rowOneCellTwo.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellTwo);

        TableCell rowOneCellThree = new TableCell();
        rowOneCellThree.Font.Bold = true;
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellThree.Text = "Notification Type";
        rowOneCellThree.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellThree);
        this.notificationDetailsTable.Controls.Add(rowOne);
        TableCell rowOneCellFour = new TableCell();
        rowOneCellFour.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellFour);

        TableCell rowOneCellFive = new TableCell();
        rowOneCellFive.Font.Bold = true;
        rowOneCellFive.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellFive.Text = "Transaction ID";
        rowOneCellFive.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellFive);
        this.notificationDetailsTable.Controls.Add(rowOne);
        TableCell rowOneCellSix = new TableCell();
        rowOneCellSix.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellSix);
        this.notificationDetailsTable.Controls.Add(rowOne);

        notificationPanel.Controls.Add(this.notificationDetailsTable);
    }

    /// <summary>
    /// Method to draw the success table
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    private void DrawPanelForSuccess(Panel panelParam)
    {
        this.successTable = new Table();
        this.successTable.Font.Name = "Sans-serif";
        this.successTable.Font.Size = 8;
        this.successTable.BorderStyle = BorderStyle.Outset;
        this.successTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOne.Controls.Add(rowOneCellOne);
        this.successTable.Controls.Add(rowOne);
        this.successTable.BorderWidth = 2;
        this.successTable.BorderColor = Color.DarkGreen;
        this.successTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(this.successTable);
    }

    /// <summary>
    /// Method to add row to the success table
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attribute as string</param>
    /// <param name="value">value as string</param>
    private void AddRowToSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.Text = attribute.ToString();
        cellOne.Font.Bold = true;
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Text = value.ToString();
        row.Controls.Add(cellTwo);
        this.successTable.Controls.Add(row);
    }

    /// <summary>
    /// Method to draws error table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="message">Message as string</param>
    private void DrawPanelForFailure(Panel panelParam, string message)
    {
        this.failureTable = new Table();
        this.failureTable.Font.Name = "Sans-serif";
        this.failureTable.Font.Size = 8;
        this.failureTable.BorderStyle = BorderStyle.Outset;
        this.failureTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        this.failureTable.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        this.failureTable.Controls.Add(rowTwo);
        this.failureTable.BorderWidth = 2;
        this.failureTable.BorderColor = Color.Red;
        this.failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(this.failureTable);
    }

    /// <summary>
    /// Method to draw panel for refund success
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    private void DrawPanelForRefundSuccess(Panel panelParam)
    {
        this.successTableRefund = new Table();
        this.successTableRefund.Font.Name = "Sans-serif";
        this.successTableRefund.Font.Size = 8;
        this.successTableRefund.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Right;
        rowOneCellOne.Text = "Parameter";
        rowOneCellOne.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellOne);
        TableCell rowOneCellTwo = new TableCell();
        rowOneCellTwo.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellTwo);

        TableCell rowOneCellThree = new TableCell();
        rowOneCellThree.Font.Bold = true;
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellThree.Text = "Value";
        rowOneCellThree.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellThree);
        this.successTableRefund.Controls.Add(rowOne);
        panelParam.Controls.Add(this.successTableRefund);
    }

    /// <summary>
    /// This function adds row to the refund success table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attribute as string</param>
    /// <param name="value">Value as string</param>
    private void AddRowToRefundSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute.ToString();
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value.ToString();
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        this.successTableRefund.Controls.Add(row);
    }

    /// <summary>
    /// Method to draw panel for successful transaction
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    private void DrawPanelForGetTransactionSuccess(Panel panelParam)
    {
        this.successTableGetTransaction = new Table();
        this.successTableGetTransaction.Font.Name = "Sans-serif";
        this.successTableGetTransaction.Font.Size = 8;
        this.successTableGetTransaction.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Right;
        rowOneCellOne.Text = "Parameter";
        rowOneCellOne.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellOne);
        TableCell rowOneCellTwo = new TableCell();
        rowOneCellTwo.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellTwo);

        TableCell rowOneCellThree = new TableCell();
        rowOneCellThree.Font.Bold = true;
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellThree.Text = "Value";
        rowOneCellThree.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellThree);
        this.successTableGetTransaction.Controls.Add(rowOne);
        panelParam.Controls.Add(this.successTableGetTransaction);
    }

    /// <summary>
    /// This function adds row to the success table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attribute as string</param>
    /// <param name="value">Value as string</param>
    private void AddRowToGetTransactionSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute.ToString();
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value.ToString();
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        this.successTableGetTransaction.Controls.Add(row);
    }

    /// <summary>
    /// Method to clear refund table.
    /// </summary>
    private void ClearRefundTable()
    {
        foreach (Control refundTableRow in refundTable.Controls)
        {
            refundTable.Controls.Remove(refundTableRow);
        }
    }

    /// <summary>
    /// This class defines access token response
    /// </summary>
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets Access Token
        /// </summary>
        public string access_token { get; set; }

        /// <summary>
        /// Gets or sets Refresh Token
        /// </summary>
        public string refresh_token { get; set; }

        /// <summary>
        /// Gets or sets Expires In
        /// </summary>
        public string expires_in { get; set; }
    }

    /// <summary>
    /// This class defines refund response
    /// </summary>
    public class RefundResponse
    {
        /// <summary>
        /// Gets or sets Transaction Id
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets Transaction Status
        /// </summary>
        public string TransactionStatus { get; set; }

        /// <summary>
        /// Gets or sets Is Success
        /// </summary>
        public string IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string OriginalPurchaseAmount { get; set; }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string CommitConfirmationId { get; set; }
    }

    /// <summary>
    /// This class defines transaction response
    /// </summary>
    public class TransactionResponse
    {
        /// <summary>
        /// Gets or sets Channel
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets Currency
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets Transaction Type
        /// </summary>
        public string TransactionType { get; set; }

        /// <summary>
        /// Gets or sets Transaction Status
        /// </summary>
        public string TransactionStatus { get; set; }

        /// <summary>
        /// Gets or sets Transaction Consumer Id
        /// </summary>
        public string ConsumerId { get; set; }

        /// <summary>
        /// Gets or sets Merchant Transaction Id
        /// </summary>
        public string MerchantTransactionId { get; set; }

        /// <summary>
        /// Gets or sets Merchant Application Id
        /// </summary>
        public string MerchantApplicationId { get; set; }

        /// <summary>
        /// Gets or sets Transaction Id
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets Content Category
        /// </summary>
        public string ContentCategory { get; set; }

        /// <summary>
        /// Gets or sets Merchant Product Id
        /// </summary>
        public string MerchantProductId { get; set; }

        /// <summary>
        /// Gets or sets Merchant Identifier
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets Amount
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Is Success
        /// </summary>
        public string IsSuccess { get; set; }
        
        /// <summary>
        /// Gets or sets Is Success
        /// </summary>
        public string OriginalTransactionId { get; set; }
    }
}