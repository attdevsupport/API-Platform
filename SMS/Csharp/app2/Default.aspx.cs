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
using System.Configuration;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using System.Threading;

public partial class Default : System.Web.UI.Page
{
    string shortCode, FQDN, accessTokenFilePath, footballFilePath, baseballFilePath, basketballFilePath, oauthFlow;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    Table statusTable;
    private static System.Timers.Timer TheTimer;
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
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=SMS");
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

    /* This function is called to draw the table in the panelParam panel for success response */
    private void drawPanelForSuccess(Panel panelParam, string message)
    {
        if (panelParam.HasControls())
        {
            panelParam.Controls.Remove(statusTable);
        }
        statusTable = new Table();
        statusTable.Font.Name = "Sans-serif";
        statusTable.Font.Size = 9;
        statusTable.BorderStyle = BorderStyle.Outset;
        statusTable.Width = Unit.Pixel(200);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        //rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne);
        statusTable.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        //rowTwoCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellOne);
        statusTable.Controls.Add(rowTwo);
        statusTable.BorderWidth = 2;
        statusTable.BorderColor = Color.DarkGreen;
        statusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(statusTable);
    }
    /* This function draws table for failed response in the panalParam panel */
    private void drawPanelForFailure(Panel panelParam, string message)
    {
        if (panelParam.HasControls())
        {
            panelParam.Controls.Remove(statusTable);
        }
        statusTable = new Table();
        statusTable.Font.Name = "Sans-serif";
        statusTable.Font.Size = 9;
        statusTable.BorderStyle = BorderStyle.Outset;
        statusTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        //rowOneCellOne.BorderWidth = 1;
        statusTable.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        //rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        statusTable.Controls.Add(rowTwo);
        statusTable.BorderWidth = 2;
        statusTable.BorderColor = Color.Red;
        statusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(statusTable);
    }

    /*
     * This function is called when the applicaiton page is loaded into the browser.
     * This fucntion reads the web.config and gets the values of the attributes
     * 
     */

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            if (ConfigurationManager.AppSettings["FootBallFilePath"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "FootBallFilePath is not defined in configuration file");
                return;
            }
            footballFilePath = ConfigurationManager.AppSettings["FootBallFilePath"].ToString();
            if (ConfigurationManager.AppSettings["BaseBallFilePath"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "BaseBallFilePath is not defined in configuration file");
                return;
            }
            baseballFilePath = ConfigurationManager.AppSettings["BaseBallFilePath"].ToString();
            if (ConfigurationManager.AppSettings["BasketBallFilePath"] == null)
            {
                drawPanelForFailure(sendSMSPanel, "BasketBallFilePath is not defined in configuration file");
                return;
            }
            basketballFilePath = ConfigurationManager.AppSettings["BasketBallFilePath"].ToString();
            if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
            {
                accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
            }
            else
            {
                accessTokenFilePath = "~\\SMSApp2AccessToken.txt";
            }
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
            shortCodeLabel.Text = shortCode.ToString();
            updateButton.Text = "Update votes for " + shortCode.ToString();
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
          

            if (!Page.IsPostBack)
            {
                voteCount();
            }

        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendSMSPanel, ex.ToString());
            Response.Write(ex.ToString());
        }
    }


    /*  This function calls receive sms API and updates the GUI with the updated votes */
    public void voteCount()
    {
        try
        {
            if (readAndGetAccessToken(sendSMSPanel) == false)
                return;
            if (access_token == null || access_token.Length <= 0)
            {
                //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                return;
            }
            int football_count_val=0, baseball_count_val=0, basketball_count_val=0;

            using (StreamReader str1 = File.OpenText(Request.MapPath(footballFilePath)))
            {
                football_count_val = Convert.ToInt32(str1.ReadToEnd());
                str1.Close();
            }
            using (StreamReader str2 = File.OpenText(Request.MapPath(baseballFilePath)))
            {
                baseball_count_val = Convert.ToInt32(str2.ReadToEnd());
                str2.Close();
            }
            using (StreamReader str3 = File.OpenText(Request.MapPath(basketballFilePath)))
            {
                basketball_count_val = Convert.ToInt32(str3.ReadToEnd());
                str3.Close();
            }
            String smsVoteCountOutput;
            int iTotalVotes = 0;
            string totalVotes;
            HttpWebRequest smsVoteCountRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/sms/2/messaging/inbox?access_token=" + access_token.ToString() + "&RegistrationID=" + shortCode.ToString());
            smsVoteCountRequestObject.Method = "GET";
            HttpWebResponse smsVoteCountResponseObject = (HttpWebResponse)smsVoteCountRequestObject.GetResponse();
            using (StreamReader smsVoteCountResponseStream = new StreamReader(smsVoteCountResponseObject.GetResponseStream()))
            {
                smsVoteCountOutput = smsVoteCountResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                RecieveSmsResponse deserializedJsonObj = (RecieveSmsResponse)deserializeJsonObject.Deserialize(smsVoteCountOutput, typeof(RecieveSmsResponse));
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
                secondTable.Font.Name = "Sans-serif";
                secondTable.Font.Size = 9;
                    TableRow = new TableRow();
                    secondTable.Font.Size = 8;
                    secondTable.Width = Unit.Pixel(600);
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
                        string msgtxt = TableCellmessage.Text.ToString();
                        if (msgtxt.Equals("football", StringComparison.CurrentCultureIgnoreCase))
                        {
                            football_count_val = football_count_val + 1;
                        }
                        else if (msgtxt.Equals("baseball", StringComparison.CurrentCultureIgnoreCase))
                        {
                            baseball_count_val = baseball_count_val + 1;
                        }
                        else if (msgtxt.Equals("basketball", StringComparison.CurrentCultureIgnoreCase))
                        {
                            basketball_count_val = basketball_count_val + 1;
                        }
                        else
                        {
                            TableCellmessageId.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                            TableCellmessage.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                            TableCellsenderAddress.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                        }

                    }
                    iTotalVotes = football_count_val + baseball_count_val + basketball_count_val;
                    totalVotes = "Total Votes: " + iTotalVotes.ToString();
                    table.BorderColor = Color.DarkGreen;
                    table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
                    table.BorderWidth = 2;
                    receiveMessagePanel.Controls.Add(table);
                    if (numberOfMessagesInThisBatch > 0)
                    {
                        receiveMessagePanel.Controls.Add(secondTable);
                    }
                    smsVoteCountResponseStream.Close();
            }
            footballLabel.Text = football_count_val.ToString();
            baseballLabel.Text = baseball_count_val.ToString();
            basketballLabel.Text = basketball_count_val.ToString();
            using (StreamWriter str1 = File.CreateText(Request.MapPath(footballFilePath)))
            {
                str1.Write(footballLabel.Text.ToString());
                str1.Close();
            }
            using (StreamWriter str2 = File.CreateText(Request.MapPath(baseballFilePath)))
            {
                str2.Write(baseballLabel.Text.ToString());
                str2.Close();
            }
            using (StreamWriter str3 = File.CreateText(Request.MapPath(basketballFilePath)))
            {
                str3.Write(basketballLabel.Text.ToString());
                str3.Close();
            }
            drawPanelForSuccess(sendSMSPanel, totalVotes);
        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendSMSPanel, ex.ToString());
        }
    }

    /* This method is called when user clicks update votes button */
    protected void updateButton_Click(object sender, EventArgs e)
    {
        voteCount();
    }
}

/* following are the data structures used for this application */
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

public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;
}