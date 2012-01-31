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
using System.Configuration;
using System.IO;
using System.Xml;
using System.Text;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates; 


public partial class Default : System.Web.UI.Page
{
    string shortCode, FQDN, accessTokenFilePath, oauthFlow;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    string[] shortCodes;

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
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/token");
                accessTokenRequest.Method = "POST";
                string oauthParameters = "client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=SMS";
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
                    lastTokenTakenTime =  currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
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
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/token");
                accessTokenRequest.Method = "POST";
                string oauthParameters = "grant_type=refresh_token&client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&refresh_token=" + refresh_token.ToString();
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
     * This function is called when the applicaiton page is loaded into the browser.
     * This fucntion reads the web.config and gets the values of the attributes
     * 
     */
    public void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
            {
                accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
            }
            else
            {
                accessTokenFilePath = "~\\SMSApp1AccessToken.txt";
            }
            //accessTokenFilePath = "~\\SMSApp1AccessToken.txt";
            if (ConfigurationManager.AppSettings["FQDN"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "FQDN is not defined in configuration file");
                return;
            }
            FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
            if (ConfigurationManager.AppSettings["short_code"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "short_code is not defined in configuration file");
                return;
            }
            shortCode = ConfigurationManager.AppSettings["short_code"].ToString();
            if (ConfigurationManager.AppSettings["api_key"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "api_key is not defined in configuration file");
                return;
            }
            api_key = ConfigurationManager.AppSettings["api_key"].ToString();
            if (ConfigurationManager.AppSettings["secret_key"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "secret_key is not defined in configuration file");
                return;
            }
            secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
            if (ConfigurationManager.AppSettings["scope"] == null)
            {
                scope = "SMS";
            }
            else
            {
                scope = ConfigurationManager.AppSettings["scope"].ToString();
            }
            shortCodes = shortCode.Split(';');
            shortCode = shortCodes[0];
            Table table = new Table();
            table.Font.Size = 8;
            foreach (string sCode in shortCodes)
            {
                Button button = new Button();
                button.Click += new EventHandler(getMessagesButton_Click);
                button.Text = "Get Messages for " + sCode;
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
            drawPanelForFailure(sendSMSPanel, ex.ToString());
            Response.Write(ex.ToString());
        }

    }

    /*
     * This funciton is called with user clicks on send SMS
     * This validates the access token and then calls sendSMS method to invoke send SMS API.
     */

    protected void Button1_Click(object sender, EventArgs e)
    {
        try
        {
            if (readAndGetAccessToken(sendSMSPanel) == true)
            {
                if (access_token == null || access_token.Length <= 0)
                {
                    //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    return;
                }
                sendSms();
            }

        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendSMSPanel, ex.ToString());
        }
    }
    /* This function validates the input fields and if they are valid send sms api is invoked */
    private void sendSms()
    {
        try
        {
            string smsAddressInput = txtmsisdn.Text.ToString();
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
                drawPanelForFailure(sendSMSPanel, "Invalid phone number: " + smsAddressInput);
            }
            else
            {
                //string smsMessage = Session["smsMessage"].ToString();
                string smsMessage = txtmsg.Text.ToString();
                if (smsMessage == null || smsMessage.Length <= 0)
                {
                    drawPanelForFailure(sendSMSPanel, "Message is null or empty");
                    return;
                }
                String sendSmsResponseData;
                //HttpWebRequest sendSmsRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/sms/2/messaging/outbox?access_token=" + access_token.ToString());
                HttpWebRequest sendSmsRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/sms/2/messaging/outbox?access_token=" + access_token.ToString());
                string strReq = "{'Address':'tel:" + smsAddressFormatted + "','Message':'" + smsMessage + "'}";
                sendSmsRequestObject.Method = "POST";
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
                    txtSmsId.Text = deserializedJsonObj.id.ToString();
                    drawPanelForSuccess(sendSMSPanel,deserializedJsonObj.id.ToString());
                    sendSmsResponseStream.Close();
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendSMSPanel, ex.ToString());
        }
    }

    /* 
     * This function is called when user clicks on get delivery status button.
     * this funciton calls get sms delivery status API to fetch the status.
     */
    private void getSmsDeliveryStatus()
    {
        try
        {
            
            //string smsId = Session["smsId"].ToString();
            string smsId = txtSmsId.Text.ToString();
            if (smsId == null || smsId.Length <= 0)
            {
                drawPanelForFailure(getStatusPanel, "Message is null or empty");
                return;
            }
            if (readAndGetAccessToken(getStatusPanel) == true)
            {
                if (access_token == null || access_token.Length <= 0)
                {
                    //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    return;
                }
            }
            String getSmsDeliveryStatusResponseData;
            //HttpWebRequest getSmsDeliveryStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/1/messages/outbox/sms/" + smsId.ToString() + "?access_token=" + Session["csharp_sms_app1_access_token"].ToString());
            HttpWebRequest getSmsDeliveryStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/sms/2/messaging/outbox/" + smsId.ToString() + "?access_token=" + access_token.ToString());
            getSmsDeliveryStatusRequestObject.Method = "GET";
            getSmsDeliveryStatusRequestObject.ContentType = "application/JSON";
            getSmsDeliveryStatusRequestObject.Accept = "application/json";

            HttpWebResponse getSmsDeliveryStatusResponse = (HttpWebResponse)getSmsDeliveryStatusRequestObject.GetResponse();
            using (StreamReader getSmsDeliveryStatusResponseStream = new StreamReader(getSmsDeliveryStatusResponse.GetResponseStream()))
            {
                getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseStream.ReadToEnd();
                getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseData.Replace("-", "");
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                GetDeliveryStatus status = (GetDeliveryStatus)deserializeJsonObject.Deserialize(getSmsDeliveryStatusResponseData, typeof(GetDeliveryStatus));
                drawGetStatusSuccess(status.DeliveryInfoList.deliveryInfo[0].deliverystatus, status.DeliveryInfoList.resourceURL);
                //getSMSStatusResponseLabel.Text = "Status :" + status.DeliveryInfoList.deliveryInfo[0].deliverystatus + "\r\n" + "ResourceURL :" + status.DeliveryInfoList.resourceURL;
                getSmsDeliveryStatusResponseStream.Close();
            }

        }
        catch (Exception ex)
        {
            drawPanelForFailure(getStatusPanel, ex.ToString());
        }
    }
    /* this function is used to draw the table for get status success response */
    private void drawGetStatusSuccess(string status, string url)
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
        //rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        TableCell rowTwoCellTwo = new TableCell();
        rowTwoCellOne.Text = "Status: ";
        rowTwoCellOne.Font.Bold = true;
        //rowTwoCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellOne);
        rowTwoCellTwo.Text = status.ToString();
        //rowTwoCellTwo.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellTwo);
        table.Controls.Add(rowTwo);
        TableRow rowThree = new TableRow();
        TableCell rowThreeCellOne = new TableCell();
        TableCell rowThreeCellTwo = new TableCell();
        rowThreeCellOne.Text = "ResourceURL: ";
        rowThreeCellOne.Font.Bold = true;
        //rowThreeCellOne.BorderWidth = 1;
        rowThree.Controls.Add(rowThreeCellOne);
        rowThreeCellTwo.Text = url.ToString();
        //rowThreeCellTwo.BorderWidth = 1;
        rowThree.Controls.Add(rowThreeCellTwo);
        table.Controls.Add(rowThree);
        table.BorderWidth = 2;
        table.BorderColor = Color.DarkGreen;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        getStatusPanel.Controls.Add(table);
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
       // rowOneCellOne.BorderWidth = 1;
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
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc"); ;
        panelParam.Controls.Add(table);
    }
    /* This function calls receive sms api to fetch the sms's */
    private void recieveSms()
    {
        try
        {
            String receiveSmsResponseData;
            if (shortCode == null || shortCode.Length <= 0)
            {
                drawPanelForFailure(getMessagePanel, "Short code is null or empty");
                return;
            }
            if (access_token == null || access_token.Length <= 0)
            {
                drawPanelForFailure(getMessagePanel, "Invalid access token");
                return;
            }
            HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/sms/2/messaging/inbox?access_token=" + access_token.ToString() + "&RegistrationID=" + shortCode.ToString());
            //HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/1/messages/inbox/sms?registrationID=" + shortCode.ToString() + "&access_token=" + access_token.ToString());
            objRequest.Method = "GET";
            HttpWebResponse receiveSmsResponseObject = (HttpWebResponse)objRequest.GetResponse();
            using (StreamReader receiveSmsResponseStream = new StreamReader(receiveSmsResponseObject.GetResponseStream()))
            {
                receiveSmsResponseData = receiveSmsResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                RecieveSmsResponse deserializedJsonObj = (RecieveSmsResponse)deserializeJsonObject.Deserialize(receiveSmsResponseData, typeof(RecieveSmsResponse));
                int numberOfMessagesInThisBatch = deserializedJsonObj.inboundSMSMessageList.numberOfMessagesInThisBatch;
                string resourceURL = deserializedJsonObj.inboundSMSMessageList.resourceURL.ToString();
                string totalNumberOfPendingMessages = deserializedJsonObj.inboundSMSMessageList.totalNumberOfPendingMessages.ToString();

                string parsedJson = "MessagesInThisBatch : " + numberOfMessagesInThisBatch.ToString() + "<br/>" + "MessagesPending : " + totalNumberOfPendingMessages.ToString() + "<br/>";
                Table table = new Table();
                table.Font.Name = "Sans-serif";
                table.Font.Size = 9;
                table.BorderStyle = BorderStyle.Outset;
                table.Width = Unit.Pixel(650);
                TableRow TableRow = new TableRow();
                TableCell TableCell = new TableCell();
                TableCell.Width = Unit.Pixel(110);
                TableCell.Text = "SUCCESS:";
                TableCell.Font.Bold = true;
                TableRow.Cells.Add(TableCell);
                table.Rows.Add(TableRow);
                TableRow = new TableRow();
                TableCell = new TableCell();
                TableCell.Width = Unit.Pixel(150);
                TableCell.Text = "Messages in this batch:";
                TableCell.Font.Bold = true;
                //TableCell.BorderWidth = 1;
                TableRow.Cells.Add(TableCell);
                TableCell = new TableCell();
                TableCell.HorizontalAlign = HorizontalAlign.Left;
                TableCell.Text = numberOfMessagesInThisBatch.ToString();
                //TableCell.BorderWidth = 1;
                TableRow.Cells.Add(TableCell);
                table.Rows.Add(TableRow);
                TableRow = new TableRow();
                TableCell = new TableCell();
                //TableCell.BorderWidth = 1;
                TableCell.Width = Unit.Pixel(110);
                TableCell.Text = "Messages pending:";
                TableCell.Font.Bold = true;
                TableRow.Cells.Add(TableCell);
                TableCell = new TableCell();
                //TableCell.BorderWidth = 1;
                TableCell.Text = totalNumberOfPendingMessages.ToString();
                TableRow.Cells.Add(TableCell);
                table.Rows.Add(TableRow);
                TableRow = new TableRow();
                table.Rows.Add(TableRow);
                TableRow = new TableRow();
                table.Rows.Add(TableRow);
                Table secondTable = new Table();
                if (numberOfMessagesInThisBatch > 0)
                {
                    TableRow = new TableRow();
                    secondTable.Font.Name = "Sans-serif";
                    secondTable.Font.Size = 9;
                    //secondTable.Width = Unit.Percentage(80);
                    TableCell = new TableCell();
                    TableCell.Width = Unit.Pixel(100);
                    //TableCell.BorderWidth = 1;
                    TableCell.Text = "Message Index";
                    TableCell.HorizontalAlign = HorizontalAlign.Center;
                    TableCell.Font.Bold = true;
                    TableRow.Cells.Add(TableCell);
                    TableCell = new TableCell();
                    //TableCell.BorderWidth = 1;
                    TableCell.Font.Bold = true;
                    TableCell.Width = Unit.Pixel(350);
                    TableCell.Wrap = true;
                    TableCell.Text = "Message Text";
                    TableCell.HorizontalAlign = HorizontalAlign.Center;
                    TableRow.Cells.Add(TableCell);
                    TableCell = new TableCell();
                    //TableCell.BorderWidth = 1;
                    TableCell.Text = "Sender Address";
                    TableCell.HorizontalAlign = HorizontalAlign.Center;
                    TableCell.Font.Bold = true;
                    TableCell.Width = Unit.Pixel(175);
                    TableRow.Cells.Add(TableCell);
                    secondTable.Rows.Add(TableRow);
                    //table.Rows.Add(TableRow);

                    foreach (inboundSMSMessage prime in deserializedJsonObj.inboundSMSMessageList.inboundSMSMessage)
                    {
                        TableRow = new TableRow();
                        TableCell TableCellmessageId = new TableCell();
                        TableCellmessageId.Width = Unit.Pixel(75);
                        TableCellmessageId.Text = prime.messageId.ToString();
                        TableCellmessageId.HorizontalAlign = HorizontalAlign.Center;
                        TableCell TableCellmessage = new TableCell();
                        TableCellmessage.Width = Unit.Pixel(350);
                        TableCellmessage.Wrap = true;
                        TableCellmessage.Text = prime.message.ToString();
                        TableCellmessage.HorizontalAlign = HorizontalAlign.Center;
                        TableCell TableCellsenderAddress = new TableCell();
                        TableCellsenderAddress.Width = Unit.Pixel(175);
                        TableCellsenderAddress.Text = prime.senderAddress.ToString();
                        TableCellsenderAddress.HorizontalAlign = HorizontalAlign.Center;
                        TableRow.Cells.Add(TableCellmessageId);
                        TableRow.Cells.Add(TableCellmessage);
                        TableRow.Cells.Add(TableCellsenderAddress);
                        secondTable.Rows.Add(TableRow);
                        //table.Rows.Add(TableRow);
                    }
                }
                table.BorderColor = Color.DarkGreen;
                table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
                table.BorderWidth = 2;
                
                getMessagePanel.Controls.Add(table);
                getMessagePanel.Controls.Add(secondTable);

                //getMessagePanel.BorderColor = Color.DarkGreen;
                //getMessagePanel.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
                //getMessagePanel.BorderWidth = 2;
                
                receiveSmsResponseStream.Close();
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(getMessagePanel, ex.ToString());
        }
    }

    /* this method is called when user clicks on get delivery status button */

    protected void getDeliveryStatusButton_Click(object sender, EventArgs e)
    {
        try
        {
             if (readAndGetAccessToken(getStatusPanel) == false)
                 return;
            if (access_token == null || access_token.Length <= 0)
            {
                //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                return;
            }
            getSmsDeliveryStatus();
        }
        catch (Exception ex)
        {
            drawPanelForFailure(getStatusPanel, ex.ToString());
        }
    }

/*
this method is called when user clicks on get message button
*/
    protected void getMessagesButton_Click(object sender, EventArgs e)
    {
        try
        {
            readAndGetAccessToken(getMessagePanel);
            if (access_token == null || access_token.Length <= 0)
            {
                //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                return;
            }
            Button button = sender as Button;
            string buttonCaption = button.Text.ToString();
            shortCode = buttonCaption.Replace("Get Messages for ", "");
            recieveSms();
        }
        catch (Exception ex)
        {
            drawPanelForFailure(getMessagePanel, ex.ToString());
        }
    }
}
/* Below are the data structures used for this applicaiton */

public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;
}

public class SendSmsResponse
{
    public string id;
}

public class GetSmsDeliveryStatus
{
    public string Status;
    public string ResourceUrl;
}

public class SmsStatus
{
    public string status;
    public string resourceURL;
}

public class RecieveSmsResponse
{
    public inboundSMSMessageList inboundSMSMessageList = new inboundSMSMessageList();
}

public class inboundSMSMessageList
{

    public List<inboundSMSMessage> inboundSMSMessage { get; set; }
    public int numberOfMessagesInThisBatch
    {
        get;
        set;
    }
    public string resourceURL
    {
        get;
        set;
    }

    public int totalNumberOfPendingMessages
    {
        get;
        set;
    }

}

public class inboundSMSMessage
{
    public string dateTime
    {
        get;
        set;
    }
    public string destinationAddress
    {
        get;
        set;
    }
    public string messageId
    {
        get;
        set;
    }
    public string message
    {
        get;
        set;
    }

    public string senderAddress
    {
        get;
        set;
    }
}

public class GetDeliveryStatus
{
    public DeliveryInfoList DeliveryInfoList = new DeliveryInfoList();
}
public class DeliveryInfoList
{
    public string resourceURL;
    public List<deliveryInfo> deliveryInfo { get; set; }
}

public class deliveryInfo
{
    public string id { get; set; }
    public string address { get; set; }
    public string deliverystatus { get; set; }
}