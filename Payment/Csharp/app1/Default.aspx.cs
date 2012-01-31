
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Text;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

public partial class _Default : System.Web.UI.Page
{
    string shortCode, accessTokenFilePath, FQDN, oauthFlow, refundFile;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    Table successTable, failureTable;
    Table successTableGetTransaction, failureTableGetTransaction;
    string amount;
    Int32 category;
    string channel, description, merchantTransactionId, merchantProductId, merchantApplicationId;
    Uri merchantRedirectURI;
    string MerchantSubscriptionIdList, SubscriptionRecurringPeriod;
    Int32 SubscriptionRecurringNumber, SubscriptionRecurringPeriodAmount;
    string IsPurchaseOnNoActiveSubscription;
    DateTime transactionTime;
    string transactionTimeString;
    string payLoadStringFromRequest;
    string signedPayload, signedSignature;
    string notaryURL;
   // Button refundButton;
    int refundCountToDisplay = 0;
    List<KeyValuePair<string, string>> refundList = new List<KeyValuePair<string, string>>();
    bool LatestFive = true;
    protected void Page_Load(object sender, EventArgs e)
    {
        transactionSuccessTable.Visible = false;
        tranGetStatusTable.Visible = false;
        refundSuccessTable.Visible = false;
        DateTime currentServerTime = DateTime.UtcNow;
        serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
        //refundButton = new Button();
        //refundButton.Click += new EventHandler(refundButtonClick);
        FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
        if (ConfigurationManager.AppSettings["FQDN"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "FQDN is not defined in configuration file");
            return;
        }
        FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
        if (ConfigurationManager.AppSettings["api_key"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "api_key is not defined in configuration file");
            return;
        }
        api_key = ConfigurationManager.AppSettings["api_key"].ToString();
        if (ConfigurationManager.AppSettings["secret_key"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "secret_key is not defined in configuration file");
            return;
        }
        secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
        if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
        {
            accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        }
        else
        {
            accessTokenFilePath = "~\\PayApp1AccessToken.txt";
        }
        if (ConfigurationManager.AppSettings["refundFile"] != null)
        {
            refundFile = ConfigurationManager.AppSettings["refundFile"];
        }
        else
        {
            refundFile = "~\\refundFile.txt";
        }

        if (ConfigurationManager.AppSettings["refundCountToDisplay"] != null)
        {
            refundCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["refundCountToDisplay"]);
        }
        else
        {
            refundCountToDisplay = 5;
        }

        if (ConfigurationManager.AppSettings["scope"] == null)
        {
            scope = "PAYMENT";
        }
        else
        {
            scope = ConfigurationManager.AppSettings["scope"].ToString();
        }
        if (ConfigurationManager.AppSettings["DisableLatestFive"] != null)
        {
            LatestFive = false;
        }
        if (ConfigurationManager.AppSettings["notaryURL"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "notaryURL is not defined in configuration file");
            return;
        }
        if (ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file");
            return;
        }
        merchantRedirectURI = new Uri(ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"]);
        notaryURL = ConfigurationManager.AppSettings["notaryURL"];
        if ((Request["ret_signed_payload"] != null) && (Request["ret_signature"] != null))
        {
            signedPayload = Request["ret_signed_payload"].ToString();
            signedSignature = Request["ret_signature"].ToString();
            Session["signedPayLoad"] = signedPayload.ToString();
            Session["signedSignature"] = signedSignature.ToString();
            processNotaryResponse();
        }
        else if ((Request["TransactionAuthCode"] != null) && (Session["merTranId"] != null))
        {
            processCreateTransactionResponse();
        }
        else if ( (Request["shown_notary"] != null) && (Session["processNotary"] != null) )
        {
            Session["processNotary"] = null;
            GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " + Session["tempMerTranId"].ToString();
            GetTransactionAuthCode.Text = "Auth Code: " + Session["TranAuthCode"].ToString();
        }
        refundTable.Controls.Clear();
        drawRefundSection(false);
        return;
    }
    private void readTransactionParametersFromConfigurationFile()
    {
        transactionTime = DateTime.UtcNow;
        transactionTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", transactionTime);
        if (Radio_TransactionProductType.SelectedIndex == 0)
            amount = "0.99";
        else if (Radio_TransactionProductType.SelectedIndex == 1)
            amount = "2.99";
        Session["tranType"] = Radio_TransactionProductType.SelectedIndex.ToString();
        if (ConfigurationManager.AppSettings["Category"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "Category is not defined in configuration file");
            return;
        }
        category = Convert.ToInt32(ConfigurationManager.AppSettings["Category"]);
        if (ConfigurationManager.AppSettings["Channel"] == null)
        {
            channel = "MOBILE_WEB";
        }
        else
        {
            channel = ConfigurationManager.AppSettings["Channel"];
        }
        description = "TrDesc" + transactionTimeString;
        merchantTransactionId = "TrId" + transactionTimeString;
        Session["merTranId"] = merchantTransactionId.ToString();
        merchantProductId = "ProdId" + transactionTimeString;
        merchantApplicationId = "MerAppId" + transactionTimeString;
    }


    private void processNotaryResponse()
    {
        if (Session["tranType"] != null)
        {
            Radio_TransactionProductType.SelectedIndex = Convert.ToInt32(Session["tranType"].ToString());
            Session["tranType"] = null;
        }
        Response.Redirect(FQDN + "/Commerce/Payment/Rest/2/Transactions?clientid=" + api_key.ToString() + "&SignedPaymentDetail=" + signedPayload.ToString() + "&Signature=" + signedSignature.ToString());

    }

    public void processCreateTransactionResponse()
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
    //"\",\"MerchantApplicationId\":\"" + merchantApplicationId.ToString() +
    protected void newTransactionButton_Click(object sender, EventArgs e)
    {
        readTransactionParametersFromConfigurationFile();
        string payLoadString = "{\"Amount\":" + amount.ToString() + ",\"Category\":" + category.ToString() + ",\"Channel\":\"" +
                        channel.ToString() + "\",\"Description\":\"" + description.ToString() + "\",\"MerchantTransactionId\":\""
                        + merchantTransactionId.ToString() + "\",\"MerchantProductId\":\"" + merchantProductId.ToString()
                        + "\",\"MerchantPaymentRedirectUrl\":\"" + merchantRedirectURI.ToString() + "\"}";
        Session["payloadData"] = payLoadString.ToString();
        //string returnURL = "https://wincode-api-att.com/BF_R2_Production_Csharp_Apps/payment/app1/Default.aspx";
        Response.Redirect(notaryURL.ToString() + "?request_to_sign=" + payLoadString.ToString() + "&goBackURL=" + merchantRedirectURI.ToString() + "&api_key=" + api_key.ToString() + "&secret_key=" + secret_key.ToString());
    }
    /* This function draws the success table */
    private void drawPanelForSuccess(Panel panelParam)
    {
        successTable = new Table();
        successTable.Font.Name = "Sans-serif";
        successTable.Font.Size = 8;
        successTable.BorderStyle = BorderStyle.Outset;
        successTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOne.Controls.Add(rowOneCellOne);
        successTable.Controls.Add(rowOne);
        successTable.BorderWidth = 2;
        successTable.BorderColor = Color.DarkGreen;
        successTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(successTable);
    }
    /*This function adds row to the success table */
    private void addRowToSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.Text = attribute.ToString();
        cellOne.Font.Bold = true;
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Text = value.ToString();
        row.Controls.Add(cellTwo);
        successTable.Controls.Add(row);
    }
    /* This function draws error table */
    private void drawPanelForFailure(Panel panelParam, string message)
    {
        failureTable = new Table();
        failureTable.Font.Name = "Sans-serif";
        failureTable.Font.Size = 8;
        failureTable.BorderStyle = BorderStyle.Outset;
        failureTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        failureTable.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        failureTable.Controls.Add(rowTwo);
        failureTable.BorderWidth = 2;
        failureTable.BorderColor = Color.Red;
        failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(failureTable);
    }
    protected void getTransactionButton_Click(object sender, EventArgs e)
    {
        try {
        string keyValue = "";
        string resourcePathString = "";
        if (Radio_TransactionStatus.SelectedIndex == 0)
        {
            keyValue = GetTransactionMerchantTransID.Text.ToString().Replace("Merchant Transaction ID: ","");
            if (keyValue.Length == 0)
            {
                //writeRefundToFile("a", "kdsfaksdf;akfjf");
                //Response.Redirect(Request.Url.ToString());
                return;
            }
            resourcePathString = "" + FQDN + "/Commerce/Payment/Rest/2/Transactions/MerchantTransactionId/" + keyValue.ToString();
        }
        if (Radio_TransactionStatus.SelectedIndex == 1)
        {
            keyValue = GetTransactionAuthCode.Text.ToString().Replace("Auth Code: ", "");
            if (keyValue.Length == 0)
                return;
            resourcePathString = "" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + keyValue.ToString();
        }
        if (Radio_TransactionStatus.SelectedIndex == 2)
        {
            keyValue = GetTransactionTransID.Text.ToString().Replace("Transaction ID: ", "");
            if (keyValue.Length == 0)
                return;
            resourcePathString = "" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionId/" + keyValue.ToString();
        }
        if (readAndGetAccessToken(newTransactionStatusPanel) == true)
        {
            if (access_token == null || access_token.Length <= 0)
            {
                return;
            }
            //String getTransactionStatusResponseData;
            resourcePathString = resourcePathString + "?access_token=" + access_token.ToString();
            //HttpWebRequest objRequest = (HttpWebRequest) System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + Session["TranAuthCode"].ToString() + "?access_token=" + access_token.ToString());
            HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(resourcePathString);
            objRequest.Method = "GET";
            HttpWebResponse getTransactionStatusResponseObject = (HttpWebResponse) objRequest.GetResponse();
            using (StreamReader getTransactionStatusResponseStream = new StreamReader(getTransactionStatusResponseObject.GetResponseStream()))
            {
                String getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                transactionResponse deserializedJsonObj = (transactionResponse)deserializeJsonObject.Deserialize(getTransactionStatusResponseData, typeof(transactionResponse));
                GetTransactionTransID.Text = "Transaction ID: " + deserializedJsonObj.TransactionId.ToString();
                lblstatusTranId.Text = deserializedJsonObj.TransactionId.ToString();
                lblstatusMerTranId.Text = deserializedJsonObj.MerchantTransactionId.ToString();
                //drawPanelForFailure(newTransactionStatusPanel, getTransactionStatusResponseData);
                if (checkItemInRefundFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString()) == false)
                {
                    writeRefundToFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString());
                    //clearRefundTable();
                }
                refundTable.Controls.Clear();
                drawRefundSection(false);
                tranGetStatusTable.Visible = true;
                drawPanelForGetTransactionSuccess(newTransactionStatusPanel);
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Amount", deserializedJsonObj.Amount.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Channel ", deserializedJsonObj.Channel.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Description", deserializedJsonObj.Description.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantIdentifier", deserializedJsonObj.MerchantIdentifier.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionType", deserializedJsonObj.TransactionType.ToString());
                addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Version", deserializedJsonObj.Version.ToString());
                getTransactionStatusResponseStream.Close();
            }
        }
        }
        catch(Exception ex)
        {
            drawPanelForFailure(newTransactionStatusPanel, ex.ToString());
        }

    }
    private void drawPanelForGetTransactionSuccess(Panel panelParam)
    {
        successTableGetTransaction = new Table();
        successTableGetTransaction.Font.Name = "Sans-serif";
        successTableGetTransaction.Font.Size = 8;
        successTableGetTransaction.Width = Unit.Pixel(650);
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
        successTableGetTransaction.Controls.Add(rowOne);
        panelParam.Controls.Add(successTableGetTransaction);
    }
    /*This function adds row to the success table */
    private void addRowToGetTransactionSuccessPanel(Panel panelParam, string attribute, string value)
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
        successTableGetTransaction.Controls.Add(row);
    }

    protected void Unnamed1_Click(object sender, EventArgs e)
    {
        if ((Session["payloadData"] != null) && (Session["signedPayLoad"] != null) && (Session["signedSignature"] != null))
        {
            Session["processNotary"] = "notary";
            Response.Redirect(notaryURL.ToString() + "?signed_payload=" + Session["signedPayLoad"].ToString() + "&goBackURL=" + merchantRedirectURI.ToString() + "&signed_signature=" + Session["signedSignature"].ToString() + "&signed_request=" + Session["payloadData"].ToString());
        }
    }

    /* This function reads the Access Token File and stores the values of access token, expiry seconds
 * refresh token, last access token time and refresh token expiry time
 * This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
 */
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
                string OauthURL;
                OauthURL = "" + FQDN + "/oauth/token";
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(OauthURL);
                accessTokenRequest.Method = "POST";
                string oauthParameters = "client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=PAYMENT";
                accessTokenRequest.ContentType = "application/x-www-form-urlencoded";
                //sendSmsRequestObject.Accept = "application/json";
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
                string OauthURL;
                OauthURL = "" + FQDN + "/oauth/token";
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(OauthURL);
                accessTokenRequest.Method = "POST";
                string oauthParameters = "client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=refresh_token" + "&refresh_token=" + refresh_token.ToString();
                accessTokenRequest.ContentType = "application/x-www-form-urlencoded";
                //sendSmsRequestObject.Accept = "application/json";
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(oauthParameters);
                accessTokenRequest.ContentLength = postBytes.Length;
                Stream postStream = accessTokenRequest.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();
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
    /*
    public void addButtonToRefundSection(string caption)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);
        rowOne.Controls.Add(cellOne);

        TableCell CellTwo = new TableCell();
        CellTwo.CssClass = "cell";
        CellTwo.Width = Unit.Pixel(100);
        rowOne.Controls.Add(CellTwo);

        TableCell CellThree = new TableCell();
        CellThree.CssClass = "cell";
        CellThree.HorizontalAlign = HorizontalAlign.Left;
        CellThree.Width = Unit.Pixel(240);
        rowOne.Controls.Add(CellThree);

        TableCell CellFour = new TableCell();
        CellFour.CssClass = "cell";
        refundButton.Text = caption.ToString();
        CellFour.Controls.Add(refundButton);
        rowOne.Controls.Add(CellFour);

        refundTable.Controls.Add(rowOne);
    }
     */
    public void addRowToRefundSection(string transaction, string merchant)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);
        //cellOne.Text = transaction.ToString();
        RadioButton rbutton = new RadioButton();
        rbutton.Text = transaction.ToString();
        rbutton.GroupName = "RefundSection";
        rbutton.ID = transaction.ToString();
        cellOne.Controls.Add(rbutton);
        rowOne.Controls.Add(cellOne);
        TableCell CellTwo = new TableCell();
        CellTwo.CssClass = "cell";
        CellTwo.Width = Unit.Pixel(100);
        rowOne.Controls.Add(CellTwo);

        TableCell CellThree = new TableCell();
        CellThree.CssClass = "cell";
        CellThree.HorizontalAlign = HorizontalAlign.Left;
        CellThree.Width = Unit.Pixel(240);
        CellThree.Text = merchant.ToString();
        rowOne.Controls.Add(CellThree);

        TableCell CellFour = new TableCell();
        CellFour.CssClass = "cell";
        rowOne.Controls.Add(CellFour);

        refundTable.Controls.Add(rowOne);
    }
    public void drawRefundSection(bool onlyRow)
    {
        try
        {
            if (onlyRow == false)
            {
                TableRow headingRow = new TableRow();
                TableCell headingCellOne = new TableCell();
                headingCellOne.HorizontalAlign = HorizontalAlign.Right;
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
            resetRefundList();
            getRefundListFromFile();

            int tempCountToDisplay = 1;
            while ((tempCountToDisplay <= refundCountToDisplay) && (tempCountToDisplay <= refundList.Count) && (refundList.Count > 0))
            {
                addRowToRefundSection(refundList[tempCountToDisplay - 1].Key, refundList[tempCountToDisplay - 1].Value);
                tempCountToDisplay++;
            }
            //addButtonToRefundSection("Refund Transaction");
        }
        catch (Exception ex)
        {
            drawPanelForFailure(newTransactionPanel, ex.ToString());
        }
    }


    public void updateRefundListToFile()
    {
        if (refundList.Count != 0)
            refundList.Reverse(0, refundList.Count);
        using (StreamWriter sr = File.CreateText(Request.MapPath(refundFile)))
        {
            int tempCount = 0;
            while (tempCount < refundList.Count)
            {
                string lineToWrite = refundList[tempCount].Key + ":-:" + refundList[tempCount].Value;
                sr.WriteLine(lineToWrite);
                tempCount++;
            }
            sr.Close();
        }
    }

    public void resetRefundList()
    {
        refundList.RemoveRange(0, refundList.Count);
    }

    public bool checkItemInRefundFile(string transactionid, string merchantTransactionId)
    {
        string line;
        string lineToFind = transactionid + ":-:" + merchantTransactionId;
        System.IO.StreamReader file = new System.IO.StreamReader(Request.MapPath(refundFile));
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

    public void writeRefundToFile(string transactionid, string merchantTransactionId)
    {
        /* Read the refund file for the list of transactions and store locally */
        //FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        //StreamWriter sr = new StreamWriter(file);
        //DateTime junkTime = DateTime.UtcNow;
        //string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        using (StreamWriter appendContent = File.AppendText(Request.MapPath(refundFile)))
        {
            string line = transactionid + ":-:" + merchantTransactionId;
            appendContent.WriteLine(line);
            appendContent.Flush();
            appendContent.Close();
            //file.Close();
        }
    }

    public void getRefundListFromFile()
    {
        /* Read the refund file for the list of transactions and store locally */
        FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string line;

        while (((line = sr.ReadLine()) != null))
        {
            string[] refundKeys = Regex.Split(line, ":-:");
            if (refundKeys[0] != null && refundKeys[1] != null)
            {
                refundList.Add(new KeyValuePair<string, string>(refundKeys[0], refundKeys[1]));
            }
        }
        sr.Close();
        file.Close();
        refundList.Reverse(0, refundList.Count);
    }

    void clearRefundTable()
    {
        foreach (Control refundTableRow in refundTable.Controls)
        {
            refundTable.Controls.Remove(refundTableRow);
        }
    }
    /*
    void processRefundButtonClick()
    {
        if (refundList.Count > 0)
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
                                if ((refundTableCellControl is RadioButton))
                                {

                                    if (((RadioButton)refundTableCellControl).Checked)
                                    {
                                        string transactionToRefund = ((RadioButton)refundTableCellControl).Text.ToString();
                                        refundList.RemoveAll(x => x.Key.Equals(Session[transactionToRefund].ToString()));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            updateRefundListToFile();
            resetRefundList();
            refundTable.Controls.Clear();
            drawRefundSection(false);
        }
    }

    protected void refundButtonClick(object sender, EventArgs e)
    {
        if (refundList.Count > 0)
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
                                if ((refundTableCellControl is RadioButton))
                                {

                                    if (((RadioButton)refundTableCellControl).Checked)
                                    {
                                        string transactionToRefund = ((RadioButton)refundTableCellControl).Text.ToString();
                                        refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            updateRefundListToFile();
            resetRefundList();
            refundTable.Controls.Clear();
            drawRefundSection(false);
        }
    }
     */
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
    public class AccessTokenResponse
    {
        public string access_token;
        public string refresh_token;
        public string expires_in;
    }
    public class RefundResponse
    {
        public string TransactionId { get; set; }
        public string TransactionStatus { get; set; }
        public string IsSuccess { get; set; }
        public string Version { get; set; }
    }
    public class transactionResponse
    {
        public string Channel { get; set; }
        public string Description { get; set; }
        public string Currency { get; set; }
        public string TransactionType { get; set; }
        public string TransactionStatus { get; set; }
        public string ConsumerId { get; set; }
        public string MerchantTransactionId { get; set; }
        public string MerchantApplicationId { get; set; }
        public string TransactionId { get; set; }
        public string ContentCategory { get; set; }
        public string MerchantProductId { get; set; }
        public string MerchantIdentifier { get; set; }
        public string Amount { get; set; }
        public string Version { get; set; }
        public string IsSuccess { get; set; }
    }
    protected void Unnamed1_Click1(object sender, EventArgs e)
    {
        string transactionToRefund = "";
        bool recordFound = false;
        string strReq = "{\"RefundReasonCode\":1,\"RefundReasonText\":\"Customer was not happy\"}";
        string dataLength = "";
        try
        {
            if (refundList.Count > 0)
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
                                    if ((refundTableCellControl is RadioButton))
                                    {

                                        if (((RadioButton)refundTableCellControl).Checked)
                                        {
                                            transactionToRefund = ((RadioButton)refundTableCellControl).Text.ToString();
                                            //refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
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
                    if (readAndGetAccessToken(refundPanel) == true)
                    {
                        if (access_token == null || access_token.Length <= 0)
                        {
                            return;
                        }
                        //String getTransactionStatusResponseData;
                        WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/" + transactionToRefund.ToString() + "?access_token=" + access_token.ToString() + "&Action=refund");
                        objRequest.Method = "PUT";
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
                            String refundTransactionResponseData = refundResponseStream.ReadToEnd();
                            //drawPanelForFailure(refundPanel, refundTransactionResponseData);
                            //{ "TransactionId":"6216898841002136", "TransactionStatus":"SUCCESSFUL", "IsSuccess":true, "Version":"1" }
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            RefundResponse deserializedJsonObj = (RefundResponse)deserializeJsonObject.Deserialize(refundTransactionResponseData, typeof(RefundResponse));
                            lbRefundTranID.Text = deserializedJsonObj.TransactionId.ToString();
                            lbRefundTranStatus.Text = deserializedJsonObj.TransactionStatus.ToString();
                            lbRefundIsSuccess.Text = deserializedJsonObj.IsSuccess.ToString();
                            lbRefundVersion.Text = deserializedJsonObj.Version.ToString();
                            //lbRefundTranID.Text = transactionToRefund.ToString();
                           // lbRefundTranStatus.Text = "SUCCESSFUL";
                            //lbRefundIsSuccess.Text = "true";
                           // lbRefundVersion.Text = "1";
                            refundSuccessTable.Visible = true;
                            refundResponseStream.Close();
                            if (LatestFive == false)
                            {
                                refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
                                updateRefundListToFile();
                                resetRefundList();
                                refundTable.Controls.Clear();
                                drawRefundSection(false);
                                GetTransactionMerchantTransID.Text = "Merchant Transaction ID: ";
                                GetTransactionAuthCode.Text = "Auth Code: ";
                                GetTransactionTransID.Text = "Transaction ID: ";
                            }
                        }
                    }

                }
            }
        }
        catch (Exception ex)
        {
            // + strReq + transactionToRefund.ToString() + dataLength
            drawPanelForFailure(refundPanel, ex.ToString());
        }
    }
}

/*
use Request.InputStream:
byte[] buffer = new byte[1024];
int c;
while ((c = Request.InputStream.Read(buffer, 0, buffer.Length)) 0)
{
    drawPanelForFailure(refundPanel,buffer.tostring());
}

*/