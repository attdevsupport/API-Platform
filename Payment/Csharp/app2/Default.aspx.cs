// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' September 2011
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2011 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
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
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

#endregion

/// <summary>
/// Payment App2 class
/// </summary>
public partial class Payment_App2 : System.Web.UI.Page
{
    /// <summary>
    /// Local Variables
    /// </summary>
    private string accessTokenFilePath, endPoint, subsDetailsFile, subsRefundFile, notificationDetailsFile;

    /// <summary>
    /// Local Variables
    /// </summary>
    private string apiKey, secretKey, authCode, accessToken, authorizeRedirectUri, scope, expirySeconds, refreshToken, accessTokenExpiryTime, refreshTokenExpiryTime;

    /// <summary>
    /// Local Variables
    /// </summary>
    private Table successTable, failureTable, successTableGetTransaction, failureTableGetTransaction, successTableGetSubscriptionDetails, successTableSubscriptionRefund;

    /// <summary>
    /// Local Variables
    /// </summary>
    private string amount, channel, description, merchantTransactionId, merchantProductId, merchantApplicationId;

    /// <summary>
    /// Local Variables
    /// </summary>
    private int category, noOfNotificationsToDisplay;

    /// <summary>
    /// Local Variables
    /// </summary>
    private Uri merchantRedirectURI;

    /// <summary>
    /// Local Variables
    /// </summary>
    private string merchantSubscriptionIdList, subscriptionRecurringPeriod, subscriptionRecurringNumber, subscriptionRecurringPeriodAmount, isPurchaseOnNoActiveSubscription;

    /// <summary>
    /// Local Variables
    /// </summary>
    private DateTime transactionTime;

    /// <summary>
    /// Local Variables
    /// </summary>
    private string transactionTimeString, payLoadStringFromRequest, signedPayload, signedSignature, notaryURL;

    /// <summary>
    /// Local Variables
    /// </summary>
    private int subsDetailsCountToDisplay = 0;

    /// <summary>
    /// Local Variables
    /// </summary>
    private List<KeyValuePair<string, string>> subsDetailsList = new List<KeyValuePair<string, string>>();

    /// <summary>
    /// Local Variables
    /// </summary>
    private List<KeyValuePair<string, string>> subsRefundList = new List<KeyValuePair<string, string>>();

    /// <summary>
    /// Local Variables
    /// </summary>
    private bool latestFive = true;

    /// <summary>
    /// Local Variables
    /// </summary>
    private Table notificationDetailsTable;

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
            delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    /// <summary>
    /// Default method, that gets called upon loading the page and performs the following actions
    /// Reads from config file
    /// Process Notary Response
    /// Process New Transaction Response
    /// </summary>
    /// <param name="sender">object that invoked this method</param>
    /// <param name="e">Event arguments</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        BypassCertificateError();

        subsRefundSuccessTable.Visible = false;
        subsDetailsSuccessTable.Visible = false;
        subscriptionSuccessTable.Visible = false;
        subsGetStatusTable.Visible = false;
        
        DateTime currentServerTime = DateTime.UtcNow;
        lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
        
        bool ableToRead = this.ReadConfigFile();
        if (ableToRead == false)
        {
            return;
        }

        if ((Request["ret_signed_payload"] != null) && (Request["ret_signature"] != null))
        {
            this.signedPayload = Request["ret_signed_payload"];
            this.signedSignature = Request["ret_signature"];
            Session["sub_signedPayLoad"] = this.signedPayload;
            Session["sub_signedSignature"] = this.signedSignature;
            this.ProcessNotaryResponse();
        }
        else if ((Request["SubscriptionAuthCode"] != null) && (Session["sub_merTranId"] != null))
        {
            this.ProcessCreateTransactionResponse();
        }
        else if ((Request["shown_notary"] != null) && (Session["sub_processNotary"] != null))
        {
            Session["sub_processNotary"] = null;
            GetSubscriptionMerchantSubsID.Text = "Merchant Transaction ID: " + Session["sub_tempMerTranId"].ToString();
            GetSubscriptionAuthCode.Text = "Auth Code: " + Session["sub_TranAuthCode"].ToString();
        }

        subsDetailsTable.Controls.Clear();
        this.DrawSubsDetailsSection(false);
        subsRefundTable.Controls.Clear();
        this.DrawSubsRefundSection(false);
        this.DrawNotificationTableHeaders();
        this.GetNotificationDetails();
        return;
    }

    /// <summary>
    /// Subscription button click event
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">Event Arguments</param>
    protected void NewSubscriptionButton_Click1(object sender, EventArgs e)
    {
        this.ReadTransactionParametersFromConfigurationFile();
        string payLoadString = "{\"Amount\":" + this.amount + ",\"Category\":" + this.category + ",\"Channel\":\"" +
                        this.channel + "\",\"Description\":\"" + this.description + "\",\"MerchantTransactionId\":\""
                        + this.merchantTransactionId + "\",\"MerchantProductId\":\"" + this.merchantProductId
                        + "\",\"MerchantPaymentRedirectUrl\":\"" + this.merchantRedirectURI + "\",\"MerchantSubscriptionIdList\":\""
                        + this.merchantSubscriptionIdList + "\",\"IsPurchaseOnNoActiveSubscription\":\""
                        + this.isPurchaseOnNoActiveSubscription + "\",\"SubscriptionRecurrences\":" + this.subscriptionRecurringNumber
                        + ",\"SubscriptionPeriod\":\"" + this.subscriptionRecurringPeriod
                        + "\",\"SubscriptionPeriodAmount\":" + this.subscriptionRecurringPeriodAmount +
                        "}";
        Session["sub_payloadData"] = payLoadString;
        Response.Redirect(this.notaryURL + "?request_to_sign=" + payLoadString + "&goBackURL=" + this.merchantRedirectURI + "&api_key=" + this.apiKey + "&secret_key=" + this.secretKey);
    }

    /// <summary>
    /// Get Subscription button click event
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void GetSubscriptionButton_Click(object sender, EventArgs e)
    {
        string resourcePathString = string.Empty;
        try
        {
            string keyValue = string.Empty;
            if (Radio_SubscriptionStatus.SelectedIndex == 0)
            {
                keyValue = GetSubscriptionMerchantSubsID.Text.ToString().Replace("Merchant Transaction ID: ", string.Empty);
                if (keyValue.Length == 0)
                {
                    return;
                }

                resourcePathString = string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Subscriptions/MerchantTransactionId/" + keyValue;
            }

            if (Radio_SubscriptionStatus.SelectedIndex == 1)
            {
                keyValue = GetSubscriptionAuthCode.Text.ToString().Replace("Auth Code: ", string.Empty);
                if (keyValue.Length == 0)
                {
                    return;
                }

                resourcePathString = string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Subscriptions/TransactionAuthCode/" + keyValue;
            }

            if (Radio_SubscriptionStatus.SelectedIndex == 2)
            {
                keyValue = GetSubscriptionID.Text.ToString().Replace("Subscription ID: ", string.Empty);
                if (keyValue.Length == 0)
                {
                    return;
                }

                resourcePathString = string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Subscriptions/SubscriptionId/" + keyValue;
            }

            if (this.ReadAndGetAccessToken(newSubscriptionPanel) == true)
            {
                if (this.accessToken == null || this.accessToken.Length <= 0)
                {
                    return;
                }

                // resourcePathString = resourcePathString + "?access_token=" + this.access_token;
                HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(resourcePathString);
                objRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                objRequest.Method = "GET";

                HttpWebResponse getTransactionStatusResponseObject = (HttpWebResponse)objRequest.GetResponse();

                using (StreamReader getTransactionStatusResponseStream = new StreamReader(getTransactionStatusResponseObject.GetResponseStream()))
                {
                    string getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    SubscriptionStatusResponse deserializedJsonObj = (SubscriptionStatusResponse)deserializeJsonObject.Deserialize(getTransactionStatusResponseData, typeof(SubscriptionStatusResponse));
                    //DrawPanelForFailure(getSubscriptionStatusPanel, getTransactionStatusResponseData);
                    //lblstatusMerSubsId.Text = deserializedJsonObj.MerchantSubscriptionId;
                    //lblstatusSubsId.Text = deserializedJsonObj.SubscriptionId;
                    GetSubscriptionID.Text = "Subscription ID: " + deserializedJsonObj.SubscriptionId;

                    if (this.CheckItemInSubsDetailsFile(deserializedJsonObj.MerchantSubscriptionId, deserializedJsonObj.ConsumerId) == false)
                    {
                        this.WriteSubsDetailsToFile(deserializedJsonObj.MerchantSubscriptionId, deserializedJsonObj.ConsumerId);
                    }

                    if (this.CheckItemInSubsRefundFile(deserializedJsonObj.SubscriptionId, deserializedJsonObj.MerchantSubscriptionId) == false)
                    {
                        this.WriteSubsRefundToFile(deserializedJsonObj.SubscriptionId, deserializedJsonObj.MerchantSubscriptionId);
                    }

                    subsDetailsTable.Controls.Clear();
                    this.DrawSubsDetailsSection(false);
                    subsRefundTable.Controls.Clear();
                    this.DrawSubsRefundSection(false);
                    subsGetStatusTable.Visible = true;

                    this.DrawPanelForGetTransactionSuccess(getSubscriptionStatusPanel);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Amount", deserializedJsonObj.Amount);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Channel ", deserializedJsonObj.Channel);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Description", deserializedJsonObj.Description);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsAutoCommitted", deserializedJsonObj.IsAutoCommitted);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantId", deserializedJsonObj.MerchantId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantTransactionId", deserializedJsonObj.MerchantTransactionId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantSubscriptionId", deserializedJsonObj.MerchantSubscriptionId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "OriginalTransactionId", deserializedJsonObj.OriginalTransactionId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionId", deserializedJsonObj.SubscriptionId);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionPeriod", deserializedJsonObj.SubscriptionPeriod);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionPeriodAmount", deserializedJsonObj.SubscriptionPeriodAmount);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionRecurrences", deserializedJsonObj.SubscriptionRecurrences);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionStatus", deserializedJsonObj.SubscriptionStatus);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionType", deserializedJsonObj.SubscriptionType);
                    this.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version);
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
                    StreamReader reader = new StreamReader(stream);
                    this.DrawPanelForFailure(getSubscriptionStatusPanel, reader.ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(getSubscriptionStatusPanel, ex.ToString());
        }
    }

    /// <summary>
    /// View Notary button click event
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void ViewNotaryButton_Click(object sender, EventArgs e)
    {
        if ((Session["sub_payloadData"] != null) && (Session["sub_signedPayLoad"] != null) && (Session["sub_signedSignature"] != null))
        {
            Session["sub_processNotary"] = "notary";
            Response.Redirect(this.notaryURL + "?signed_payload=" + Session["sub_signedPayLoad"].ToString() + "&goBackURL=" + this.merchantRedirectURI + "&signed_signature=" + Session["sub_signedSignature"].ToString() + "&signed_request=" + Session["sub_payloadData"].ToString());
        }
    }

    /// <summary>
    /// Get Subscription Details button click
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnGetSubscriptionDetails_Click(object sender, EventArgs e)
    {
        string merSubsID = string.Empty;
        bool recordFound = false;
        try
        {
            if (this.subsDetailsList.Count > 0)
            {
                foreach (Control subDetailsTableRow in subsDetailsTable.Controls)
                {
                    if (subDetailsTableRow is TableRow)
                    {
                        foreach (Control subDetailsTableRowCell in subDetailsTableRow.Controls)
                        {
                            if (subDetailsTableRowCell is TableCell)
                            {
                                foreach (Control subDetailsTableCellControl in subDetailsTableRowCell.Controls)
                                {
                                    if (subDetailsTableCellControl is RadioButton)
                                    {
                                        if (((RadioButton)subDetailsTableCellControl).Checked)
                                        {
                                            merSubsID = ((RadioButton)subDetailsTableCellControl).Text.ToString();
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
                    if (this.ReadAndGetAccessToken(subsDetailsPanel) == true)
                    {
                        if (this.accessToken == null || this.accessToken.Length <= 0)
                        {
                            return;
                        }

                        string consID = this.GetValueOfKey(merSubsID);

                        if (consID.CompareTo("null") == 0)
                        {
                            return;
                        }

                        WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Subscriptions/" + merSubsID + "/Detail/" + consID);

                        objRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                        objRequest.Method = "GET";
                        objRequest.ContentType = "application/json";

                        WebResponse subsDetailsResponeObject = (WebResponse)objRequest.GetResponse();

                        using (StreamReader subsDetailsResponseStream = new StreamReader(subsDetailsResponeObject.GetResponseStream()))
                        {
                            string subsDetailsResponseData = subsDetailsResponseStream.ReadToEnd();
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            SubscriptionDetailsResponse deserializedJsonObj = (SubscriptionDetailsResponse)deserializeJsonObject.Deserialize(subsDetailsResponseData, typeof(SubscriptionDetailsResponse));
                            subsDetailsSuccessTable.Visible = true;
                            //lblMerSubId.Text = merSubsID.ToString();
                            //lblConsId.Text = consID.ToString();
                            //DrawPanelForFailure(getSubscriptionStatusPanel, subsDetailsResponseData);
                            this.DrawPanelForGetSubscriptionDetailsSuccess(subsDetailsPanel);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CreationDate", deserializedJsonObj.CreationDate);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentEndDate", deserializedJsonObj.CurrentEndDate);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentStartDate", deserializedJsonObj.CurrentStartDate);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "GrossAmount", deserializedJsonObj.GrossAmount);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsActiveSubscription", deserializedJsonObj.IsActiveSubscription);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Recurrences", deserializedJsonObj.Recurrences);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "RecurrencesLeft", deserializedJsonObj.RecurrencesLeft);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Status", deserializedJsonObj.Status);
                            this.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version);

                            subsDetailsResponseStream.Close();
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
                    StreamReader reader = new StreamReader(stream);
                    this.DrawPanelForFailure(subsDetailsPanel, reader.ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(subsDetailsPanel, ex.ToString());
        }
    }

    /// <summary>
    /// Get Subscription Refund button click
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnGetSubscriptionRefund_Click(object sender, EventArgs e)
    {
        string subsID = string.Empty;
        bool recordFound = false;
        string strReq = "{\"TransactionOperationStatus\":\"Refunded\",\"RefundReasonCode\":1,\"RefundReasonText\":\"Customer was not happy\"}";
        string dataLength = string.Empty;
        try
        {
            if (this.subsRefundList.Count > 0)
            {
                foreach (Control subRefundTableRow in subsRefundTable.Controls)
                {
                    if (subRefundTableRow is TableRow)
                    {
                        foreach (Control subRefundTableRowCell in subRefundTableRow.Controls)
                        {
                            if (subRefundTableRowCell is TableCell)
                            {
                                foreach (Control subRefundTableCellControl in subRefundTableRowCell.Controls)
                                {
                                    if (subRefundTableCellControl is RadioButton)
                                    {
                                        if (((RadioButton)subRefundTableCellControl).Checked)
                                        {
                                            subsID = ((RadioButton)subRefundTableCellControl).Text.ToString();
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
                    if (this.ReadAndGetAccessToken(subsRefundPanel) == true)
                    {
                        if (this.accessToken == null || this.accessToken.Length <= 0)
                        {
                            return;
                        }

                        string merSubsID = this.GetValueOfKeyFromRefund(subsID);

                        if (merSubsID.CompareTo("null") == 0)
                        {
                            return;
                        }

                        WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/" + subsID);
                        objRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                        objRequest.Method = "PUT";
                        objRequest.ContentType = "application/json";

                        UTF8Encoding encoding = new UTF8Encoding();
                        byte[] postBytes = encoding.GetBytes(strReq);
                        objRequest.ContentLength = postBytes.Length;

                        dataLength = postBytes.Length.ToString();

                        Stream postStream = objRequest.GetRequestStream();
                        postStream.Write(postBytes, 0, postBytes.Length);
                        postStream.Close();

                        WebResponse subsRefundResponeObject = (WebResponse)objRequest.GetResponse();
                        using (StreamReader subsRefundResponseStream = new StreamReader(subsRefundResponeObject.GetResponseStream()))
                        {
                            string subsRefundResponseData = subsRefundResponseStream.ReadToEnd();
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            RefundResponse deserializedJsonObj = (RefundResponse)deserializeJsonObject.Deserialize(subsRefundResponseData, typeof(RefundResponse));
                            //DrawPanelForFailure(subsRefundPanel, subsRefundResponseData);
                            subsRefundSuccessTable.Visible = true;
                            //lbRefundTranID.Text = deserializedJsonObj.TransactionId;
                            DrawPanelForSubscriptionRefundSuccess(subsRefundPanel);

                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "CommitConfirmationId", deserializedJsonObj.CommitConfirmationId);
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "IsSuccess", deserializedJsonObj.IsSuccess);
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "OriginalPurchaseAmount", deserializedJsonObj.OriginalPurchaseAmount);
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "TransactionId", deserializedJsonObj.TransactionId);
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus);
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "Version", deserializedJsonObj.Version);
 
                            if (this.latestFive == false)
                            {
                                this.subsRefundList.RemoveAll(x => x.Key.Equals(subsID));
                                this.UpdatesSubsRefundListToFile();
                                this.ResetSubsRefundList();
                                subsRefundTable.Controls.Clear();
                                this.DrawSubsRefundSection(false);
                                GetSubscriptionMerchantSubsID.Text = "Merchant Transaction ID: ";
                                GetSubscriptionAuthCode.Text = "Auth Code: ";
                                GetSubscriptionID.Text = "Subscription ID: ";
                            }

                            subsRefundResponseStream.Close();
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
                    StreamReader reader = new StreamReader(stream);
                    this.DrawPanelForFailure(subsRefundPanel, reader.ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(subsRefundPanel, ex.ToString());
        }
    }

    /// <summary>
    /// Refresh notification messages
    /// </summary>
    /// <param name="sender">Sender Details</param>
    /// <param name="e">List of Arguments</param>
    protected void BtnRefreshNotifications_Click(object sender, EventArgs e)
    {
        this.notificationDetailsTable.Controls.Clear();
        this.DrawNotificationTableHeaders();
        this.GetNotificationDetails();      
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
                noOfNotifications = notificationDetailArray.Length - 1;
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
        cellOne.HorizontalAlign = HorizontalAlign.Right;
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
    /// Reads from config file and assigns to local variables
    /// </summary>
    /// <returns>true/false; true if able to read all values, false otherwise</returns>
    private bool ReadConfigFile()
    {
        this.endPoint = ConfigurationManager.AppSettings["endPoint"];

        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(newSubscriptionPanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.apiKey = ConfigurationManager.AppSettings["api_key"];
        if (string.IsNullOrEmpty(this.apiKey))
        {
            this.DrawPanelForFailure(newSubscriptionPanel, "api_key is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(newSubscriptionPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "~\\PayApp2AccessToken.txt";
        }

        this.subsDetailsFile = ConfigurationManager.AppSettings["subsDetailsFile"];
        if (string.IsNullOrEmpty(this.subsDetailsFile))
        {
            this.subsDetailsFile = "~\\subsDetailsFile.txt";
        }

        this.subsRefundFile = ConfigurationManager.AppSettings["subsRefundFile"];
        if (string.IsNullOrEmpty(this.subsRefundFile))
        {
            this.subsRefundFile = "~\\subsRefundFile.txt";
        }

        this.subsDetailsCountToDisplay = 5;
        if (ConfigurationManager.AppSettings["subsDetailsCountToDisplay"] != null)
        {
            this.subsDetailsCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["subsDetailsCountToDisplay"]);
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "PAYMENT";
        }

        this.notaryURL = ConfigurationManager.AppSettings["notaryURL"];
        if (ConfigurationManager.AppSettings["notaryURL"] == null)
        {
            this.DrawPanelForFailure(newSubscriptionPanel, "notaryURL is not defined in configuration file");
            return false;
        }

        if (ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"] == null)
        {
            this.DrawPanelForFailure(newSubscriptionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file");
            return false;
        }

        this.merchantRedirectURI = new Uri(ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"]);

        if (ConfigurationManager.AppSettings["DisableLatestFive"] != null)
        {
            this.latestFive = false;
        }

        this.notificationDetailsFile = ConfigurationManager.AppSettings["notificationDetailsFile"];
        if (string.IsNullOrEmpty(this.notificationDetailsFile))
        {
            this.notificationDetailsFile = "~\\listener\\notificationDetailsFile.txt";
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

        if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["noOfNotificationsToDisplay"]))
        {
            this.noOfNotificationsToDisplay = 5;
        }
        else
        {
            noOfNotificationsToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["noOfNotificationsToDisplay"]);
        }

        return true;
    }

    /// <summary>
    /// This medthod is used for adding row in Subscription Details Section
    /// </summary>
    /// <param name="subscription">Subscription Details</param>
    /// <param name="merchantsubscription">Merchant Details</param>
    private void AddRowToSubsDetailsSection(string subscription, string merchantsubscription)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Left;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);        
        RadioButton rbutton = new RadioButton();
        rbutton.Text = subscription.ToString();
        rbutton.GroupName = "SubsDetailsSection";
        rbutton.ID = subscription.ToString();
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
        cellThree.Text = merchantsubscription.ToString();
        rowOne.Controls.Add(cellThree);
        TableCell cellFour = new TableCell();
        cellFour.CssClass = "cell";
        rowOne.Controls.Add(cellFour);

        subsDetailsTable.Controls.Add(rowOne);
    }

    /// <summary>
    /// This medthod is used for adding row in Subscription Refund Section
    /// </summary>
    /// <param name="subscription">Subscription Details</param>
    /// <param name="merchantsubscription">Merchant Details</param>
    private void AddRowToSubsRefundSection(string subscription, string merchantsubscription)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Left;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);
        RadioButton rbutton = new RadioButton();
        rbutton.Text = subscription.ToString();
        rbutton.GroupName = "SubsRefundSection";
        rbutton.ID = subscription.ToString();
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
        cellThree.Text = merchantsubscription.ToString();
        rowOne.Controls.Add(cellThree);
        TableCell cellFour = new TableCell();
        cellFour.CssClass = "cell";
        rowOne.Controls.Add(cellFour);

        subsRefundTable.Controls.Add(rowOne);
    }

    /// <summary>
    /// This medthod is used for Drawing Subscription Details Section
    /// </summary>
    /// <param name="onlyRow">Row Details</param>
    private void DrawSubsDetailsSection(bool onlyRow)
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
                headingCellOne.Text = "Merchant Subscription ID";
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
                headingCellThree.Text = "Consumer ID";
                headingRow.Controls.Add(headingCellThree);
                TableCell headingCellFour = new TableCell();
                headingCellFour.CssClass = "warning";
                LiteralControl warningMessage = new LiteralControl("<b>WARNING:</b><br/>You must use Get Subscription Status before you can view details of it.");
                headingCellFour.Controls.Add(warningMessage);
                headingRow.Controls.Add(headingCellFour);
                subsDetailsTable.Controls.Add(headingRow);
            }

            this.ResetSubsDetailsList();
            this.GetSubsDetailsFromFile();

            int tempCountToDisplay = 1;
            while ((tempCountToDisplay <= this.subsDetailsCountToDisplay) && (tempCountToDisplay <= this.subsDetailsList.Count) && (this.subsDetailsList.Count > 0))
            {
                this.AddRowToSubsDetailsSection(this.subsDetailsList[tempCountToDisplay - 1].Key, this.subsDetailsList[tempCountToDisplay - 1].Value);
                tempCountToDisplay++;
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(subsDetailsPanel, ex.ToString());
        }
    }

    /// <summary>
    /// This medthod is used for drawing Subscription Refund Section
    /// </summary>
    /// <param name="onlyRow">Row Details</param>
    private void DrawSubsRefundSection(bool onlyRow)
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
                headingCellOne.Text = "Subscription ID";
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
                headingCellThree.Text = "Merchant Subscription ID";
                headingRow.Controls.Add(headingCellThree);
                TableCell headingCellFour = new TableCell();
                headingCellFour.CssClass = "warning";
                LiteralControl warningMessage = new LiteralControl("<b>WARNING:</b><br/>You must use Get Subscription Status before you can refund.");
                headingCellFour.Controls.Add(warningMessage);
                headingRow.Controls.Add(headingCellFour);
                subsRefundTable.Controls.Add(headingRow);
            }

            this.ResetSubsRefundList();
            this.GetSubsRefundFromFile();

            int tempCountToDisplay = 1;
            while ((tempCountToDisplay <= this.subsDetailsCountToDisplay) && (tempCountToDisplay <= this.subsRefundList.Count) && (this.subsRefundList.Count > 0))
            {
                this.AddRowToSubsRefundSection(this.subsRefundList[tempCountToDisplay - 1].Key, this.subsRefundList[tempCountToDisplay - 1].Value);
                tempCountToDisplay++;
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(subsRefundPanel, ex.ToString());
        }
    }

    /// <summary>
    /// Method to get the value of key from the selected row in Refund Section
    /// </summary>
    /// <param name="key">Key Value to be found</param>
    /// <returns>Returns the value in String</returns>
    private string GetValueOfKeyFromRefund(string key)
    {
        int tempCount = 0;
        while (tempCount < this.subsRefundList.Count)
        {
            if (this.subsRefundList[tempCount].Key.CompareTo(key) == 0)
            {
                return this.subsRefundList[tempCount].Value;
            }

            tempCount++;
        }

        return "null";
    }

    /// <summary>
    /// Method to get the value from Key value
    /// </summary>
    /// <param name="key">Key Value to be found</param>
    /// <returns>Returns the value in String</returns>
    private string GetValueOfKey(string key)
    {
        int tempCount = 0;
        while (tempCount < this.subsDetailsList.Count)
        {
            if (this.subsDetailsList[tempCount].Key.CompareTo(key) == 0)
            {
                return this.subsDetailsList[tempCount].Value;
            }

            tempCount++;
        }

        return "null";
    }

    /// <summary>
    /// Method to reset Subscription Refund List
    /// </summary>
    private void ResetSubsRefundList()
    {
        this.subsRefundList.RemoveRange(0, this.subsRefundList.Count);
    }

    /// <summary>
    /// Method to reset Subscription Details List
    /// </summary>
    private void ResetSubsDetailsList()
    {
        this.subsDetailsList.RemoveRange(0, this.subsDetailsList.Count);
    }

    /// <summary>
    /// Method to get Subscription Details from the file.
    /// </summary>
    private void GetSubsDetailsFromFile()
    {
        FileStream file = new FileStream(Request.MapPath(this.subsDetailsFile), FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string line;

        while ((line = sr.ReadLine()) != null)
        {
            string[] subsDetailsKeys = Regex.Split(line, ":-:");
            if (subsDetailsKeys[0] != null && subsDetailsKeys[1] != null)
            {
                this.subsDetailsList.Add(new KeyValuePair<string, string>(subsDetailsKeys[0], subsDetailsKeys[1]));
            }
        }

        sr.Close();
        file.Close();
        this.subsDetailsList.Reverse(0, this.subsDetailsList.Count);
    }

    /// <summary>
    /// Method to get Subscription Refund from the file.
    /// </summary>
    private void GetSubsRefundFromFile()
    {
        FileStream file = new FileStream(Request.MapPath(this.subsRefundFile), FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string line;

        while ((line = sr.ReadLine()) != null)
        {
            string[] subsRefundKeys = Regex.Split(line, ":-:");
            if (subsRefundKeys[0] != null && subsRefundKeys[1] != null)
            {
                this.subsRefundList.Add(new KeyValuePair<string, string>(subsRefundKeys[0], subsRefundKeys[1]));
            }
        }

        sr.Close();
        file.Close();
        this.subsRefundList.Reverse(0, this.subsRefundList.Count);
    }

    /// <summary>
    /// Method to update Subscription Refund list to the file.
    /// </summary>
    private void UpdatesSubsRefundListToFile()
    {
        if (this.subsRefundList.Count != 0)
        {
            this.subsRefundList.Reverse(0, this.subsRefundList.Count);
        }

        using (StreamWriter sr = File.CreateText(Request.MapPath(this.subsRefundFile)))
        {
            int tempCount = 0;
            while (tempCount < this.subsRefundList.Count)
            {
                string lineToWrite = this.subsRefundList[tempCount].Key + ":-:" + this.subsRefundList[tempCount].Value;
                sr.WriteLine(lineToWrite);
                tempCount++;
            }

            sr.Close();
        }
    }

    /// <summary>
    /// Method to update Subscription Details list to the file.
    /// </summary>
    private void UpdateSubsDetailsListToFile()
    {
        if (this.subsDetailsList.Count != 0)
        {
            this.subsDetailsList.Reverse(0, this.subsDetailsList.Count);
        }

        using (StreamWriter sr = File.CreateText(Request.MapPath(this.subsDetailsFile)))
        {
            int tempCount = 0;
            while (tempCount < this.subsDetailsList.Count)
            {
                string lineToWrite = this.subsDetailsList[tempCount].Key + ":-:" + this.subsDetailsList[tempCount].Value;
                sr.WriteLine(lineToWrite);
                tempCount++;
            }

            sr.Close();
        }
    }

    /// <summary>
    /// Method to check item in Subscription Refund file.
    /// </summary>
    /// <param name="transactionid">Transaction Id details</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id details</param>
    /// <returns>Returns True or False</returns>
    private bool CheckItemInSubsRefundFile(string transactionid, string merchantTransactionId)
    {
        string line;
        string lineToFind = transactionid + ":-:" + merchantTransactionId;
        System.IO.StreamReader file = new System.IO.StreamReader(Request.MapPath(this.subsRefundFile));
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
    /// Method to check item in Subscription Details file.
    /// </summary>
    /// <param name="transactionid">Transaction Id details</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id details</param>
    /// <returns>Returns True or False</returns>
    private bool CheckItemInSubsDetailsFile(string transactionid, string merchantTransactionId)
    {
        string line;
        string lineToFind = transactionid + ":-:" + merchantTransactionId;
        System.IO.StreamReader file = new System.IO.StreamReader(Request.MapPath(this.subsDetailsFile));
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
    /// Method to write Subscription Refund to file.
    /// </summary>
    /// <param name="transactionid">Transaction Id</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id</param>
    private void WriteSubsRefundToFile(string transactionid, string merchantTransactionId)
    {   
        using (StreamWriter appendContent = File.AppendText(Request.MapPath(this.subsRefundFile)))
        {
            string line = transactionid + ":-:" + merchantTransactionId;
            appendContent.WriteLine(line);
            appendContent.Flush();
            appendContent.Close();            
        }
    }

    /// <summary>
    /// Method to write Subscription Details to file.
    /// </summary>
    /// <param name="transactionid">Transaction Id</param>
    /// <param name="merchantTransactionId">Merchant Transaction Id</param>
    private void WriteSubsDetailsToFile(string transactionid, string merchantTransactionId)
    {
        using (StreamWriter appendContent = File.AppendText(Request.MapPath(this.subsDetailsFile)))
        {
            string line = transactionid + ":-:" + merchantTransactionId;
            appendContent.WriteLine(line);
            appendContent.Flush();
            appendContent.Close();
        }
    }

    /// <summary>
    /// Method to read Transaction Parameters from Configuration file.
    /// </summary>
    private void ReadTransactionParametersFromConfigurationFile()
    {
        this.transactionTime = DateTime.UtcNow;
        this.transactionTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", this.transactionTime);
        if (Radio_SubscriptionProductType.SelectedIndex == 0)
        {
            this.amount = "1.99";
        }
        else if (Radio_SubscriptionProductType.SelectedIndex == 1)
        {
            this.amount = "3.99";
        }

        Session["sub_tranType"] = Radio_SubscriptionProductType.SelectedIndex.ToString();

        if (ConfigurationManager.AppSettings["Category"] == null)
        {
            this.DrawPanelForFailure(newSubscriptionPanel, "Category is not defined in configuration file");
            return;
        }

        this.category = Convert.ToInt32(ConfigurationManager.AppSettings["Category"]);
        this.channel = ConfigurationManager.AppSettings["Channel"];
        if (string.IsNullOrEmpty(this.channel))
        {
            this.channel = "MOBILE_WEB";
        }

        this.description = "TrDesc" + this.transactionTimeString;
        this.merchantTransactionId = "TrId" + this.transactionTimeString;
        Session["sub_merTranId"] = this.merchantTransactionId;
        this.merchantProductId = "ProdId" + this.transactionTimeString;
        this.merchantApplicationId = "MerAppId" + this.transactionTimeString;
        this.merchantSubscriptionIdList = "ML" + new Random().Next();
        Session["MerchantSubscriptionIdList"] = this.merchantSubscriptionIdList;

        this.isPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings["IsPurchaseOnNoActiveSubscription"];

        if (string.IsNullOrEmpty(this.isPurchaseOnNoActiveSubscription))
        {
            this.isPurchaseOnNoActiveSubscription = "false";
        }

        this.subscriptionRecurringNumber = ConfigurationManager.AppSettings["SubscriptionRecurringNumber"];
        if (string.IsNullOrEmpty(this.subscriptionRecurringNumber))
        {
            this.subscriptionRecurringNumber = "99999";
        }

        this.subscriptionRecurringPeriod = ConfigurationManager.AppSettings["SubscriptionRecurringPeriod"];
        if (string.IsNullOrEmpty(this.subscriptionRecurringPeriod))
        {
            this.subscriptionRecurringPeriod = "MONTHLY";
        }

        this.subscriptionRecurringPeriodAmount = ConfigurationManager.AppSettings["SubscriptionRecurringPeriodAmount"];
        if (string.IsNullOrEmpty(this.subscriptionRecurringPeriodAmount))
        {
            this.subscriptionRecurringPeriodAmount = "1";
        }
    }

    /// <summary>
    /// Method to process Notary Response
    /// </summary>
    private void ProcessNotaryResponse()
    {
        if (Session["sub_tranType"] != null)
        {
            Radio_SubscriptionProductType.SelectedIndex = Convert.ToInt32(Session["sub_tranType"].ToString());
            Session["sub_tranType"] = null;
        }

        Response.Redirect(this.endPoint + "/rest/3/Commerce/Payment/Subscriptions?clientid=" + this.apiKey + "&SignedPaymentDetail=" + this.signedPayload + "&Signature=" + this.signedSignature);
    }

    /// <summary>
    /// Method to process create transaction response
    /// </summary>
    private void ProcessCreateTransactionResponse()
    {
        lblsubscode.Text = Request["SubscriptionAuthCode"].ToString();
        lblsubsid.Text = Session["sub_merTranId"].ToString();
        subscriptionSuccessTable.Visible = true;
        GetSubscriptionMerchantSubsID.Text = "Merchant Transaction ID: " + Session["sub_merTranId"].ToString();
        GetSubscriptionAuthCode.Text = "Auth Code: " + Request["SubscriptionAuthCode"].ToString();
        GetSubscriptionID.Text = "Subscription ID: ";
        Session["sub_tempMerTranId"] = Session["sub_merTranId"].ToString();
        Session["sub_merTranId"] = null;
        Session["sub_TranAuthCode"] = Request["SubscriptionAuthCode"].ToString();        
    }

    /// <summary>
    /// Method to draw the success table.
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
    /// Method to add rows to success table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attributes as String</param>
    /// <param name="value">Value as String</param>
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
    /// Method to draw error table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="message">Message as String</param>
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
        rowTwoCellOne.Text = message;
        rowTwo.Controls.Add(rowTwoCellOne);
        this.failureTable.Controls.Add(rowTwo);
        this.failureTable.BorderWidth = 2;
        this.failureTable.BorderColor = Color.Red;
        this.failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(this.failureTable);
    }

    /// <summary>
    /// Method to draw panel for successful transaction.
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
    /// Method to add row to success table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attribute as String</param>
    /// <param name="value">Value as String</param>
    private void AddRowToGetTransactionSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute;
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value;
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        this.successTableGetTransaction.Controls.Add(row);
    }

    /// <summary>
    /// Method to draw panel for successful refund.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    private void DrawPanelForSubscriptionRefundSuccess(Panel panelParam)
    {
        this.successTableSubscriptionRefund = new Table();
        this.successTableSubscriptionRefund.Font.Name = "Sans-serif";
        this.successTableSubscriptionRefund.Font.Size = 8;
        this.successTableSubscriptionRefund.Width = Unit.Pixel(650);
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
        this.successTableSubscriptionRefund.Controls.Add(rowOne);
        panelParam.Controls.Add(this.successTableSubscriptionRefund);
    }

    /// <summary>
    /// Method to draw panel for successful transaction.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    private void DrawPanelForGetSubscriptionDetailsSuccess(Panel panelParam)
    {
        this.successTableGetSubscriptionDetails = new Table();
        this.successTableGetSubscriptionDetails.Font.Name = "Sans-serif";
        this.successTableGetSubscriptionDetails.Font.Size = 8;
        this.successTableGetSubscriptionDetails.Width = Unit.Pixel(650);
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
        this.successTableGetSubscriptionDetails.Controls.Add(rowOne);
        panelParam.Controls.Add(this.successTableGetSubscriptionDetails);
    }

    /// <summary>
    /// Method to add row to success table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attribute as String</param>
    /// <param name="value">Value as String</param>
    private void AddRowToSubscriptionRefundSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute;
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value;
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        this.successTableSubscriptionRefund.Controls.Add(row);
    }

    /// <summary>
    /// Method to add row to success table.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <param name="attribute">Attribute as String</param>
    /// <param name="value">Value as String</param>
    private void AddRowToGetSubscriptionDetailsSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute;
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value;
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        this.successTableGetSubscriptionDetails.Controls.Add(row);
    }

    /// <summary>
    /// This function reads the Access Token File and stores the values of access token, expiry seconds, 
    /// refresh token, last access token time and refresh token expiry time.
    /// This funciton returns true, if access token file and all others attributes read successfully otherwise returns false.
    /// </summary>
    /// <returns>Return Boolean</returns>
    private bool ReadAccessTokenFile()
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
        catch
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
    /// This function validates the expiry of the access token and refresh token, function compares the current time with the refresh token taken time, if current time is greater then returns INVALID_REFRESH_TOKEN
    /// function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    /// funciton returns INVALID_ACCESS_TOKEN
    /// otherwise returns VALID_ACCESS_TOKEN
    /// </summary>
    /// <returns>Returns String</returns>
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
    /// This function get the access token based on the type parameter type values.
    /// If type value is 1, access token is fetch for client credential flow
    /// If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    /// </summary>
    /// <param name="type">Type as Integer</param>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns boolean</returns>
    private bool GetAccessToken(int type, Panel panelParam)
    {
        FileStream fileStream = null;
        StreamWriter streamWriter = null;

        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            WebRequest accessTokenRequest = null;
            if (type == 1)
            {
                accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/access_token?client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=PAYMENT");
            }
            else if (type == 2)
            {
                accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/access_token?grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken);
            }

            accessTokenRequest.Method = "GET";

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd();

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

                accessTokenResponseStream.Close();
                return true;
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
    /// This function is used to read access token file and validate the access token.
    /// This function returns true if access token is valid, or else false is returned.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns Boolean</returns>
    private bool ReadAndGetAccessToken(Panel panelParam)
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
            else if (tokenValidity.Equals("INVALID_ACCESS_TOKEN"))
            {
                result = this.GetAccessToken(1, panelParam);
            }
        }

        return result;
    }

    #region Data Structures

    /// <summary>
    /// This class defines Access Token Response
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
    /// This class defines Refund Response.
    /// </summary>
    public class RefundResponse
    {
        /// <summary>
        /// Gets or sets Transaction Id.
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets Transaction Status.
        /// </summary>
        public string TransactionStatus { get; set; }

        /// <summary>
        /// Gets or sets Is Success.
        /// </summary>
        public string IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets Version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets OriginalPurchaseAmount.
        /// </summary>
        public string OriginalPurchaseAmount { get; set; }
        
        /// <summary>
        /// Gets or sets CommitConfirmationId.
        /// </summary>
        public string CommitConfirmationId { get; set; }
    }

    /// <summary>
    /// This class defines Subscription Status Response
    /// </summary>
    public class SubscriptionStatusResponse
    {
        /// <summary>
        /// Gets or sets Currency
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Is Success
        /// </summary>
        public string IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets Merchant Transaction Id
        /// </summary>
        public string MerchantTransactionId { get; set; }

        /// <summary>
        /// Gets or sets Consumer Id
        /// </summary>
        public string ConsumerId { get; set; }

        /// <summary>
        /// Gets or sets Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets Amount
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Gets or sets Content Category
        /// </summary>
        public string ContentCategory { get; set; }

        /// <summary>
        /// Gets or sets Merchant Product Id
        /// </summary>
        public string MerchantProductId { get; set; }

        /// <summary>
        /// Gets or sets Merchant Application Id
        /// </summary>
        public string MerchantApplicationId { get; set; }

        /// <summary>
        /// Gets or sets Channel
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets Subscription Period
        /// </summary>
        public string SubscriptionPeriod { get; set; }

        /// <summary>
        /// Gets or sets Period Amount
        /// </summary>
        public string SubscriptionPeriodAmount { get; set; }

        /// <summary>
        /// Gets or sets Recurrences
        /// </summary>
        public string SubscriptionRecurrences { get; set; }

        /// <summary>
        /// Gets or sets Merchante Subscription Id
        /// </summary>
        public string MerchantSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets Merchant Identifier
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets Is Auto Committed
        /// </summary>
        public string IsAutoCommitted { get; set; }

        /// <summary>
        /// Gets or sets Subscription Id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets Subscription Status
        /// </summary>
        public string SubscriptionStatus { get; set; }

        /// <summary>
        /// Gets or sets Subscription Type
        /// </summary>
        public string SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets Original Transaction Id
        /// </summary>
        public string OriginalTransactionId { get; set; }
    }

    /// <summary>
    /// This class defines Subscription Details Response
    /// </summary>
    public class SubscriptionDetailsResponse
    {
        /// <summary>
        /// Gets or sets Is Active Subscription
        /// </summary>
        public string IsActiveSubscription { get; set; }

        /// <summary>
        /// Gets or sets Currency
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets Creation Date
        /// </summary>
        public string CreationDate { get; set; }

        /// <summary>
        /// Gets or sets Current Start Date
        /// </summary>
        public string CurrentStartDate { get; set; }

        /// <summary>
        /// Gets or sets Current End Date
        /// </summary>
        public string CurrentEndDate { get; set; }

        /// <summary>
        /// Gets or sets Gross Amount
        /// </summary>
        public string GrossAmount { get; set; }

        /// <summary>
        /// Gets or sets SubscriptionRecurrences
        /// </summary>
        public string Recurrences { get; set; }

        /// <summary>
        /// Gets or sets SubscriptionRemaining
        /// </summary>
       // public string SubscriptionRemaining { get; set; }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Is Success
        /// </summary>
        public string IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets RecurrencesLeft
        /// </summary>
        public string RecurrencesLeft { get; set; }
        
    }

    #endregion
}