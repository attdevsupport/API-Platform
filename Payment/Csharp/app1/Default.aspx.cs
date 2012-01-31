/*using System;
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
using System.Drawing;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
*/
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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates; 

public partial class _Default : System.Web.UI.Page
{
    string shortCode, accessTokenFilePath, FQDN, oauthFlow;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    Table successTable, failureTable;
    Table successTableGetTransaction, failureTableGetTransaction;
    string amount;
    Int32 category;
    string channel, description, merchantTransactionId, merchantProductId, merchantApplicationId;
    Uri merchantRedirectURI;
    string paymentType;
    string MerchantSubscriptionIdList, SubscriptionRecurringPeriod;
    Int32 SubscriptionRecurringNumber, SubscriptionRecurringPeriodAmount;
    string IsPurchaseOnNoActiveSubscription;
    DateTime transactionTime;
    string transactionTimeString;
    string payLoadStringFromRequest;
    string signedPayload, signedSignature;
    string notaryURL;

    /* This function is called when application is getting loaded */

    protected void Page_Load(object sender, EventArgs e)
    {
        transactionSuccessTable.Visible = false;
        DateTime currentServerTime = DateTime.UtcNow;
        serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
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
        if (ConfigurationManager.AppSettings["scope"] == null)
        {
            scope = "PAYMENT";
        }
        else
        {
            scope = ConfigurationManager.AppSettings["scope"].ToString();
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
        return;
    }

    /* This function reads transaction parameter values from configuration file */
    private void readTransactionParametersFromConfigurationFile()
    {
        transactionTime = DateTime.UtcNow;
        transactionTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", transactionTime);
        /*
        if (ConfigurationManager.AppSettings["Amount"] == null)
        {
            drawPanelForFailure(newTransactionPanel, "Amount is not defined in configuration file");
            return;
        }
        amount = ConfigurationManager.AppSettings["Amount"];
        */
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

    /* This function is called after receiving response from notary application */
    private void processNotaryResponse()
    {
        if (Session["tranType"] != null)
        {
            Radio_TransactionProductType.SelectedIndex = Convert.ToInt32(Session["tranType"].ToString());
            Session["tranType"] = null;
        }
        Response.Redirect(FQDN + "/Commerce/Payment/Rest/2/Transactions?clientid=" + api_key.ToString() + "&SignedPaymentDetail=" + signedPayload.ToString() + "&Signature=" + signedSignature.ToString());

    }

    /* This function is called if user clicks on view notary button */

    protected void viewNotary_Click(object sender, EventArgs e)
    {
        if ((Session["payloadData"] != null) && (Session["signedPayLoad"] != null) && (Session["signedSignature"] != null))
        {
            //Response.Redirect("www.google.com");
            Response.Redirect(notaryURL.ToString() + "?signed_payload=" + Session["payloadData"].ToString() + "&goBackURL=" + merchantRedirectURI.ToString() + "&signed_signature=" + Session["signedSignature"].ToString() + "&signed_request=" + Session["signedSignature"].ToString());
        }/*
        if (Session["payloadData"] != null)
            addRowToGetTransactionSuccessPanel(newTransactionPanel, "Merchant Transaction ID", "user573transaction1377");
        if (Session["signedPayLoad"] != null)
            addRowToGetTransactionSuccessPanel(newTransactionPanel, "Transaction Auth Cod", "66574834711");
        if (Session["signedSignature"] != null)
            addRowToGetTransactionSuccessPanel(newTransactionPanel, "Transaction ID", "trx83587123897598612897");

         */
    }

    /* this function is called to add button to success table */
    private void addButtonToSuccessPanel(Panel panelParam)
    {
        Button button = new Button();
        button.Click += new EventHandler(getTransactionButton_Click);
        button.Text = "View Notary";
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.Text = "";
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Controls.Add(button);
        row.Controls.Add(cellTwo);
        successTable.Controls.Add(row);
        return;
    }

    /* this function is called after getting transaction response */
    public void processCreateTransactionResponse()
    {
        //drawPanelForSuccess(newTransactionPanel);
        //addRowToSuccessPanel(newTransactionPanel, "Merchant Transaction ID", Session["merTranId"].ToString());
        //addRowToSuccessPanel(newTransactionPanel, "Transaction Auth Cod", Request["TransactionAuthCode"].ToString());
        //addRowToSuccessPanel(newTransactionPanel, "", "");
        //addRowToSuccessPanel(newTransactionPanel, "", "");
        //addButtonToSuccessPanel(newTransactionPanel);
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

    /* this function is called if user clicks on new transaction button */
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

    /* this function is called when user clicks on get transaction status */
    protected void getTransactionButton_Click(object sender, EventArgs e)
    {
        try {

        string keyValue = "";
        if (Radio_TransactionStatus.SelectedIndex == 0)
        {
            keyValue = GetTransactionMerchantTransID.Text.ToString().Replace("Merchant Transaction ID: ","");
            if (keyValue.Length == 0)
                return;
            
        }
        if (Radio_TransactionStatus.SelectedIndex == 1)
        {
            keyValue = GetTransactionAuthCode.Text.ToString().Replace("Auth Code: ", "");
            if (keyValue.Length == 0)
                return;
        }
        if (Radio_TransactionStatus.SelectedIndex == 2)
        {
            keyValue = GetTransactionTransID.Text.ToString().Replace("Transaction ID: ", "");
            if (keyValue.Length == 0)
                return;
        }
        if (readAndGetAccessToken(newTransactionStatusPanel) == true)
        {
            if (access_token == null || access_token.Length <= 0)
            {
                return;
            }
            String getTransactionStatusResponseData;
            HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + Session["TranAuthCode"].ToString() + "?access_token=" + access_token.ToString());
            objRequest.Method = "GET";
            HttpWebResponse getTransactionStatusResponseObject = (HttpWebResponse)objRequest.GetResponse();
            using (StreamReader getTransactionStatusResponseStream = new StreamReader(getTransactionStatusResponseObject.GetResponseStream()))
            {
                getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                transactionResponse deserializedJsonObj = (transactionResponse)deserializeJsonObject.Deserialize(getTransactionStatusResponseData, typeof(transactionResponse));
                    
                //getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                drawPanelForGetTransactionSuccess(newTransactionStatusPanel);
                addRowToSuccessPanel(newTransactionStatusPanel, "Merchant Transaction ID", deserializedJsonObj.MerchantTransactionId);
                //addRowToSuccessPanel(newTransactionPanel, "Transaction Auth Cod", deserializedJsonObj.);
                addRowToSuccessPanel(newTransactionStatusPanel, "Transaction ID", deserializedJsonObj.TransactionId);
                addRowToSuccessPanel(newTransactionStatusPanel, "Amount", deserializedJsonObj.Amount);
                addRowToSuccessPanel(newTransactionStatusPanel, "Channel ", deserializedJsonObj.Channel);
                addRowToSuccessPanel(newTransactionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId);
                addRowToSuccessPanel(newTransactionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory);
                addRowToSuccessPanel(newTransactionStatusPanel, "Currency", deserializedJsonObj.Currency);
                addRowToSuccessPanel(newTransactionStatusPanel, "Description", deserializedJsonObj.Description);
                addRowToSuccessPanel(newTransactionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId);
                addRowToSuccessPanel(newTransactionStatusPanel, "MerchantIdentifier", deserializedJsonObj.MerchantIdentifier);
                addRowToSuccessPanel(newTransactionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId);
                addRowToSuccessPanel(newTransactionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess);
                addRowToSuccessPanel(newTransactionStatusPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus);
                getTransactionStatusResponseStream.Close();
            }
        }
        }
        catch(Exception ex)
        {
            drawPanelForFailure(newTransactionStatusPanel, ex.ToString());
        }

    }

    /* this function draws the success table for get transaction status result */
    private void drawPanelForGetTransactionSuccess(Panel panelParam)
    {
        successTableGetTransaction = new Table();
        successTableGetTransaction.Font.Name = "Sans-serif";
        successTableGetTransaction.Font.Size = 8;
        successTableGetTransaction.BorderStyle = BorderStyle.Outset;
        successTableGetTransaction.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOne.Controls.Add(rowOneCellOne);
        successTableGetTransaction.Controls.Add(rowOne);
        successTableGetTransaction.BorderWidth = 2;
        successTableGetTransaction.BorderColor = Color.DarkGreen;
        successTableGetTransaction.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(successTableGetTransaction);
    }
    /*This function adds row to the success table */
    private void addRowToGetTransactionSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.Text = attribute.ToString();
        cellOne.Font.Bold = true;
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Text = value.ToString();
        row.Controls.Add(cellTwo);
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
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=PAYMENT");
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

    /* following are the data structures used for the applicaiton */
    public class AccessTokenResponse
    {
        public string access_token;
        public string refresh_token;
        public string expires_in;
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
    	//{ "Channel":"MOBILE_WEB", "":"TrDescSatJan072012131709", "":"USD", "TransactionType":"SINGLEPAY", "TransactionStatus":"SUCCESSFUL", "ConsumerId":"d2ca3d8b-df6b-49b0-a00d-e8b6336e62cb", "MerchantTransactionId":"TrIdSatJan072012131709", "MerchantApplicationId":"e1745ae95cfa72e5150446773be67ed1", "TransactionId":"8499458613902133", "ContentCategory":"1", "MerchantProductId":"ProdIdSatJan072012131709", "MerchantIdentifier":"a75ad884-cf58-4398-ae28-104b619da686", "Amount":"0.99", "Version":"1", "IsSuccess":"true" }
}