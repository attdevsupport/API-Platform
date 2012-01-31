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
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public partial class Default : System.Web.UI.Page
{
    //string shortCode, FQDN, accessTokenString, accessToken, refreshToken, responseData, apiKey, secretKey, scope, authCode, accessTokenFilePath;
    //string[] accessTokenJson, accessTokenDetails, refreshTokenDetails, accessTokenExpireDetails;
    //DateTime expireTime;
    //double expiresIn;
    string shortCode, FQDN, accessTokenFilePath, messageFilePath, phoneListFilePath, couponPath, couponFileName;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    List<string> phoneNumbersList = new List<string>();
    List<string> invalidPhoneNumbers = new List<string>();
    string phoneNumber,phoneNumbersParameter = null;
    Dictionary<string, string> phoneNumberAndValidity;
    Table getStatusTable, secondTable;
    bool textChanged = false;
    string phoneListContent = "";
    bool errorInput = false;

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
                string oauthParameters = "client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=MMS";
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
    /* this function draws success table */
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
        rowTwoCellOne.Width = Unit.Pixel(70);
        rowTwoCellOne.Text = "Message ID:";
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
    /* this function draws error table */
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
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(table);
    }

    /* this function validates the given string as valid msisdn */
    private Boolean isValidMISDN(string number)
    {
        string smsAddressInput = number;
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
            return false;
        }
        return true;
    }

    /*
     * On page load if query string 'code' is present, invoke get_access_token
     */

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            if (ConfigurationManager.AppSettings["messageFilePath"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "Message file path is missing in configuration file");
                return;
            }
            messageFilePath = ConfigurationManager.AppSettings["messageFilePath"];
            if (ConfigurationManager.AppSettings["phoneListFilePath"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "Phone list file path is missing in configuration file");
                return;
            }
            phoneListFilePath = ConfigurationManager.AppSettings["phoneListFilePath"];
            if (ConfigurationManager.AppSettings["couponPath"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "Coupon file path is missing in configuration file");
                return;
            }
            couponPath = ConfigurationManager.AppSettings["couponPath"];

            if (ConfigurationManager.AppSettings["couponFileName"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "Coupon file name is missing in configuration file");
                return;
            }
            couponFileName = ConfigurationManager.AppSettings["couponFileName"];

            DirectoryInfo _dir = new DirectoryInfo(Request.MapPath(couponPath));
            FileInfo[] _imgs = _dir.GetFiles();
            int fileindex = 0;
            bool foundFlag = false;
            foreach (FileInfo tempFileInfo in _imgs)
            {
                if (tempFileInfo.Name.ToLower().CompareTo(couponFileName.ToLower()) == 0)
                {
                    foundFlag = true;
                    break;
                }
                else
                    fileindex++;
            }
            if (foundFlag == false)
            {
                drawPanelForFailure(sendMMSPanel, "Coupon doesnt exists");
                return;
            }
            Image1.ImageUrl = string.Format("{0}{1}", couponPath, _imgs[fileindex].Name);

            //Image1.ImageUrl = Request.MapPath(couponPath + couponFileName);
            if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
            {
                accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
            }
            else
            {
                accessTokenFilePath = "~\\MMSApp2AccessToken.txt";
            }
            if (ConfigurationManager.AppSettings["FQDN"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "FQDN is not defined in configuration file");
                return;
            }
            FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
            if (ConfigurationManager.AppSettings["short_code"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "short_code is not defined in configuration file");
                return;
            }
            shortCode = ConfigurationManager.AppSettings["short_code"].ToString();
            if (ConfigurationManager.AppSettings["api_key"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "api_key is not defined in configuration file");
                return;
            }
            api_key = ConfigurationManager.AppSettings["api_key"].ToString();
            if (ConfigurationManager.AppSettings["secret_key"] == null)
            {
                drawPanelForFailure(sendMMSPanel, "secret_key is not defined in configuration file");
                return;
            }
            secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
            if (ConfigurationManager.AppSettings["scope"] == null)
            {
                scope = "MMS";
            }
            else
            {
                scope = ConfigurationManager.AppSettings["scope"].ToString();
            }
            //StreamReader str2 = File.OpenText(Request.MapPath(messageFilePath));
            StreamReader str2 = new StreamReader(Request.MapPath(messageFilePath));
            subjectLabel.Text = str2.ReadToEnd();
            str2.Close();

            if (!Page.IsPostBack)
            {
                StreamReader str3 = new StreamReader(Request.MapPath(phoneListFilePath));
                phoneListTextBox.Text = str3.ReadToEnd();
                str3.Close();
            }

        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendMMSPanel, ex.ToString());
            Response.Write(ex.ToString());
        }
    }

    /* this function draws table for failed numbers */
    private void drawPanelForFailedNumbers(Panel panelParam)
    {
        Table table = new Table();
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        table.BorderStyle = BorderStyle.Outset;
        table.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        //rowOneCellOne.BorderWidth = 1;
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR: Invalid numbers";
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        foreach (string number in invalidPhoneNumbers)
        {
            TableRow rowTwo = new TableRow();
            TableCell rowTwoCellOne = new TableCell();
            //rowTwoCellOne.BorderWidth = 1;
            rowTwoCellOne.Text = number.ToString();
            rowTwo.Controls.Add(rowTwoCellOne);
            table.Controls.Add(rowTwo);
        }
        table.BorderWidth = 2;
        table.BorderColor = Color.Red;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(table);
    }

    /* this function draws table for get status response */
    private void drawPanelForGetStatusResult(string msgid, string phone, string status, bool headerFlag)
    {
        if (headerFlag == true)
        {
            getStatusTable = new Table();
            getStatusTable.Font.Name = "Sans-serif";
            getStatusTable.Font.Size = 9;
            getStatusTable.BorderStyle = BorderStyle.Outset;
            getStatusTable.Width = Unit.Pixel(650);
            TableRow rowOne = new TableRow();
            TableCell rowOneCellOne = new TableCell();
            rowOneCellOne.Width = Unit.Pixel(110);
            rowOneCellOne.Font.Bold = true;
            rowOneCellOne.Text = "SUCCESS:";
            //rowOneCellOne.BorderWidth = 1;
            rowOne.Controls.Add(rowOneCellOne);
            getStatusTable.Controls.Add(rowOne);
            TableRow rowTwo = new TableRow();
            TableCell rowTwoCellOne = new TableCell();
            rowTwoCellOne.Width = Unit.Pixel(110);
            rowTwoCellOne.Text = "Messages Delivered";
            //rowTwoCellOne.BorderWidth = 1;
            rowTwo.Controls.Add(rowTwoCellOne);
            getStatusTable.Controls.Add(rowTwo);


            getStatusTable.BorderWidth = 2;
            getStatusTable.BorderColor = Color.DarkGreen;
            getStatusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
            getStatusTable.Controls.Add(rowOne);
            getStatusTable.Controls.Add(rowTwo);
            //getStatusTable.Controls.Add(rowThree);
            statusPanel.Controls.Add(getStatusTable);

            secondTable = new Table();
            secondTable.Font.Name = "Sans-serif";
            secondTable.Font.Size = 9;
            secondTable.Width = Unit.Pixel(650);
            TableRow TableRow = new TableRow();
            //secondTable.Width = Unit.Percentage(80);
            TableCell TableCell = new TableCell();
            TableCell.Width = Unit.Pixel(300);
            //TableCell.BorderWidth = 1;
            TableCell.Text = "Recipient";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableCell.Font.Bold = true;
            TableRow.Cells.Add(TableCell);
            TableCell = new TableCell();
            //TableCell.BorderWidth = 1;
            TableCell.Font.Bold = true;
            TableCell.Width = Unit.Pixel(300);
            TableCell.Wrap = true;
            TableCell.Text = "Status";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableRow.Cells.Add(TableCell);
            secondTable.Rows.Add(TableRow);
            statusPanel.Controls.Add(secondTable);
        }
        else
        {
            TableRow row = new TableRow();
            TableCell cell1 = new TableCell();
            TableCell cell2 = new TableCell();
            //TableCell cell3 = new TableCell();
            //cell1.BorderWidth = 1;
            //cell2.BorderWidth = 1;
            //cell3.BorderWidth = 1;
            //cell1.Text = msgid.ToString();
            //row.Controls.Add(cell1);
            cell1.Text = phone.ToString();
            cell1.Width = Unit.Pixel(300);
            cell1.HorizontalAlign = HorizontalAlign.Center;
            row.Controls.Add(cell1);
            cell2.Text = status.ToString();
            cell2.Width = Unit.Pixel(300);
            cell2.HorizontalAlign = HorizontalAlign.Center;
            row.Controls.Add(cell2);
            secondTable.Controls.Add(row);
        }
    }

    /* this function is called with user clicks on send mms button */
    protected void sendButton_Click(object sender, EventArgs e)
    {

        if (phoneListTextBox.Text.Length == 0)
        {
            return;
        }
        string[] phoneNumbers = phoneListTextBox.Text.ToString().Split(',');
        foreach (string phoneNum in phoneNumbers)
        {
            if (phoneNum != null && (string.Compare(phoneNum, "") != 0))
                phoneNumbersList.Add(phoneNum.ToString());
        }
        phoneNumberAndValidity = new Dictionary<string, string>();
        foreach (string phNumber in phoneNumbersList)
        {
            if (isValidMISDN(phNumber) == true)
            {
                if (phNumber.StartsWith("tel:"))
                {

                    string phNumberWithoutHyphens = phNumber.Replace("-", "");
                    phoneNumbersParameter = phoneNumbersParameter + "Address=" + Server.UrlEncode(phNumberWithoutHyphens.ToString()) + "&";
                }
                else
                {
                    string phNumberWithoutHyphens = phNumber.Replace("-", "");
                    phoneNumbersParameter = phoneNumbersParameter + "Address=" + Server.UrlEncode("tel:" + phNumberWithoutHyphens.ToString()) + "&";
                }

            }
            else
            {
                invalidPhoneNumbers.Add(phNumber);
            }
        }
        if (phoneNumbersParameter  == null)
        {
            if (invalidPhoneNumbers.Count > 0)
                drawPanelForFailedNumbers(sendMMSPanel);
            return;
        }
        if (readAndGetAccessToken(sendMMSPanel) == false)
        {
            return;
        }
            
        string mmsFilePath = Request.MapPath(couponPath);
        //Table table = new Table();
        //table.Font.Size = 8;
                try
                {
                    string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

                    HttpWebRequest mmsRequestObject = (HttpWebRequest)WebRequest.Create("" + FQDN + "/rest/mms/2/messaging/outbox?access_token=" + access_token.ToString());
                    mmsRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundary + "\"\r\n";
                    mmsRequestObject.Method = "POST";
                    mmsRequestObject.KeepAlive = true;
                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] postBytes = encoding.GetBytes("");
                    string sendMMSData = phoneNumbersParameter + "&Subject=" + Server.UrlEncode(subjectLabel.Text.ToString());
                    string data = "";
                    //string mmsFileName = Path.GetFileName(mmsFilePath.ToString());
                    FileStream fs = new FileStream(mmsFilePath + couponFileName, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    byte[] image = br.ReadBytes((int)fs.Length);
                    br.Close();
                    fs.Close();

                    data += "--" + boundary + "\r\n";
                    data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8\r\nContent-Transfer-Encoding:8bit\r\nContent-ID:<startpart>\r\n\r\n" + sendMMSData + "\r\n";
                    data += "--" + boundary + "\r\n";
                    data += "Content-Disposition:attachment;name=\"" + "coupon.jpg" + "\"\r\n";
                    data += "Content-Type:image/gif\r\n";
                    data += "Content-ID:<" + "coupon.jpg" + ">\r\n";
                    data += "Content-Transfer-Encoding:binary\r\n\r\n";
                    byte[] firstPart = encoding.GetBytes(data);
                    int newSize = firstPart.Length + image.Length;
                    var ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
                    ms.Write(firstPart, 0, firstPart.Length);
                    ms.Write(image, 0, image.Length);
                    byte[] secondpart = ms.GetBuffer();
                    byte[] thirdpart = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
                    newSize = secondpart.Length + thirdpart.Length;
                    var ms2 = new MemoryStream(new byte[newSize], 0, newSize, true, true);
                    ms2.Write(secondpart, 0, secondpart.Length);
                    ms2.Write(thirdpart, 0, thirdpart.Length);
                    postBytes = ms2.GetBuffer();
                    
                    mmsRequestObject.ContentLength = postBytes.Length;

                    Stream postStream = mmsRequestObject.GetRequestStream();
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Close();
                    WebResponse mmsResponseObject = mmsRequestObject.GetResponse();
                    using (StreamReader sr = new StreamReader(mmsResponseObject.GetResponseStream()))
                    {
                        string mmsResponseData = sr.ReadToEnd();
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                        mmsResponseId deserializedJsonObj = (mmsResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(mmsResponseId));
                        msgIdLabel.Text = deserializedJsonObj.id.ToString();
                        drawPanelForSuccess(sendMMSPanel, deserializedJsonObj.id.ToString());
                        sr.Close();
                    }
                    mmsRequestObject = null;
                    //sendMMSPanel.Controls.Add(table);
                    if (invalidPhoneNumbers.Count > 0 )
                        drawPanelForFailedNumbers(sendMMSPanel);
                }
                catch (Exception ex)
                {
                    drawPanelForFailure(sendMMSPanel, ex.ToString());
                    if (invalidPhoneNumbers.Count > 0)
                        drawPanelForFailedNumbers(sendMMSPanel);
                }
    }

    /*this function is called when user clicks on get status button */
    protected void statusButton_Click(object sender, EventArgs e)
    {
        try
        {
            //Session["Inprocess"] = null;

            if (msgIdLabel.Text == null || msgIdLabel.Text.ToString() == null || msgIdLabel.Text.ToString().Length <= 0)
            {
                return;
            }
            if (readAndGetAccessToken(statusPanel) == false)
            {
                return;
            }
            string mmsId = msgIdLabel.Text.ToString();
            String mmsDeliveryStatus;
            HttpWebRequest mmsStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/mms/2/messaging/outbox/" + mmsId + "?access_token=" + access_token.ToString());
            mmsStatusRequestObject.Method = "GET";
            HttpWebResponse mmsStatusResponseObject = (HttpWebResponse)mmsStatusRequestObject.GetResponse();
            using (StreamReader mmsStatusResponseStream = new StreamReader(mmsStatusResponseObject.GetResponseStream()))
            {
                mmsDeliveryStatus = mmsStatusResponseStream.ReadToEnd();
                mmsDeliveryStatus = mmsDeliveryStatus.Replace("-", "");
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                GetDeliveryStatus status = (GetDeliveryStatus)deserializeJsonObject.Deserialize(mmsDeliveryStatus, typeof(GetDeliveryStatus));
                DeliveryInfoList dinfoList = status.DeliveryInfoList;
                drawPanelForGetStatusResult(null, null, null, true);
                foreach (deliveryInfo dInfo in dinfoList.deliveryInfo)
                {
                    drawPanelForGetStatusResult(dInfo.id, dInfo.address, dInfo.deliverystatus, false);
                }
                msgIdLabel.Text = "";
                mmsStatusResponseStream.Close();
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(statusPanel, ex.ToString());
        }
    }


}

/*following are data structures used for the application */

public class mmsResponseId
{
    public string id;
}

public class mmsStatus
{
    public string status;
    public string resourceURL;
}

public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;
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