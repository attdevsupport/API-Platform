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
using System.Web.Services;
using System.Text;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public partial class Default : System.Web.UI.Page
{
    string shortCode, FQDN, accessTokenFilePath, oauthFlow;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;

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
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=MMS");
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
            if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
            {
                accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
            }
            else
            {
                accessTokenFilePath = "~\\MMSApp1AccessToken.txt";
            }
            if (ConfigurationManager.AppSettings["FQDN"] == null)
            {
                drawPanelForFailure(sendMessagePanel, "FQDN is not defined in configuration file");
                return;
            }
            FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
            if (ConfigurationManager.AppSettings["short_code"] == null)
            {
                drawPanelForFailure(sendMessagePanel, "short_code is not defined in configuration file");
                return;
            }
            shortCode = ConfigurationManager.AppSettings["short_code"].ToString();
            if (ConfigurationManager.AppSettings["api_key"] == null)
            {
                drawPanelForFailure(sendMessagePanel, "api_key is not defined in configuration file");
                return;
            }
            api_key = ConfigurationManager.AppSettings["api_key"].ToString();
            if (ConfigurationManager.AppSettings["secret_key"] == null)
            {
                drawPanelForFailure(sendMessagePanel, "secret_key is not defined in configuration file");
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
        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendMessagePanel, ex.ToString());
            Response.Write(ex.ToString());
        }
    }
    /*
 * This funciton calls send mms message to send the selected files
 */
    private void sendMms()
    {
        try
        {
            string smsAddressInput = phoneTextBox.Text.ToString();
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
                drawPanelForFailure(sendMessagePanel, "Invalid phone number: " + smsAddressInput);
            }
            else
            {
                string mmsAddress = smsAddressForRequest.ToString();
                string mmsMessage = messageTextBox.Text.ToString();

                if (mmsMessage == null || mmsMessage.Length <=0)
                {
                    drawPanelForFailure(sendMessagePanel, "Message is null or empty");
                    return;
                }

                if ((Session["mmsFilePath1"] == null) && (Session["mmsFilePath2"] == null) && (Session["mmsFilePath3"] == null))
                {
                    string boundaryToSend = "----------------------------" + DateTime.Now.Ticks.ToString("x");
                    HttpWebRequest mmsRequestObject1 = (HttpWebRequest)WebRequest.Create("" + FQDN + "/rest/mms/2/messaging/outbox?access_token=" + access_token.ToString());
                    mmsRequestObject1.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundaryToSend + "\"\r\n";
                    mmsRequestObject1.Method = "POST";
                    mmsRequestObject1.KeepAlive = true;
                    UTF8Encoding encoding1 = new UTF8Encoding();
                    byte[] bytesToSend = encoding1.GetBytes("");
                    string mmsParameters = "Address=" + Server.UrlEncode("tel:" + mmsAddress) + "&Subject=" + Server.UrlEncode(mmsMessage);
                    string dataToSend = "";
                    dataToSend += "--" + boundaryToSend + "\r\n";
                    dataToSend += "Content-Type: application/x-www-form-urlencoded; charset=UTF-8\r\nContent-Transfer-Encoding: 8bit\r\nContent-Disposition: form-data; name=\"root-fields\"\r\nContent-ID: <startpart>\r\n\r\n"+ mmsParameters +"\r\n";
                    dataToSend += "--" + boundaryToSend + "--\r\n";
                    bytesToSend = encoding1.GetBytes(dataToSend);
                    int sizeToSend = bytesToSend.Length;
                    var memBufToSend = new MemoryStream(new byte[sizeToSend], 0, sizeToSend, true, true);
                    memBufToSend.Write(bytesToSend, 0, bytesToSend.Length);
                    //ms.Write(image, 0, image.Length);
                    byte[] finalData = memBufToSend.GetBuffer();
                    mmsRequestObject1.ContentLength = finalData.Length;
                    Stream postStream1 = mmsRequestObject1.GetRequestStream();
                    postStream1.Write(finalData, 0, finalData.Length);
                    postStream1.Close();

                    WebResponse mmsResponseObject1 = mmsRequestObject1.GetResponse();
                    using (StreamReader sr = new StreamReader(mmsResponseObject1.GetResponseStream()))
                    {
                        string mmsResponseData = sr.ReadToEnd();
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                        mmsResponseId deserializedJsonObj = (mmsResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(mmsResponseId));
                        messageIDTextBox.Text = deserializedJsonObj.id.ToString();
                        drawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString());
                        sr.Close();
                    }
                    return;

                }
               
                string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

                HttpWebRequest mmsRequestObject = (HttpWebRequest)WebRequest.Create("" + FQDN + "/rest/mms/2/messaging/outbox?access_token=" + access_token.ToString());
                mmsRequestObject.ContentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundary + "\"\r\n";
                mmsRequestObject.Method = "POST";
                mmsRequestObject.KeepAlive = true;
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes("");
                byte[] postBytes1 = encoding.GetBytes("");
                byte[] postBytes2 = encoding.GetBytes("");
                byte[] totalpostBytes = encoding.GetBytes("");
                string sendMMSData = "Address=" + Server.UrlEncode("tel:" + mmsAddress) + "&Subject=" + Server.UrlEncode(mmsMessage);
                string data = "";
                data += "--" + boundary + "\r\n";
                data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8\r\nContent-Transfer-Encoding:8bit\r\nContent-ID:<startpart>\r\n\r\n" + sendMMSData + "\r\n";

                if (Session["mmsFilePath1"] != null)
                {
                    string mmsFileName = Path.GetFileName(Session["mmsFilePath1"].ToString());
                    FileStream fs = new FileStream(Session["mmsFilePath1"].ToString(), FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    byte[] image = br.ReadBytes((int)fs.Length);
                    br.Close();
                    fs.Close();
                    data += "--" + boundary + "\r\n";
                    data += "Content-Disposition:attachment;name=\"" + mmsFileName + "\"\r\n";
                    data += "Content-Type:image/gif\r\n";
                    data += "Content-ID:<" + mmsFileName + ">\r\n";
                    data += "Content-Transfer-Encoding:binary\r\n\r\n";
                    byte[] firstPart = encoding.GetBytes(data);
                    int newSize = firstPart.Length + image.Length;
                    var ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
                    ms.Write(firstPart, 0, firstPart.Length);
                    ms.Write(image, 0, image.Length);
                    postBytes = ms.GetBuffer();
                    totalpostBytes = postBytes;
                    //Session["mmsFilePath1"] = null;
                }

                if (Session["mmsFilePath2"] != null)
                {
                    if (Session["mmsFilePath1"] != null)
                        data = "--" + boundary + "\r\n";
                    else
                        data += "--" + boundary + "\r\n";
                    string mmsFileName = Path.GetFileName(Session["mmsFilePath2"].ToString());
                    FileStream fs = new FileStream(Session["mmsFilePath2"].ToString(), FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    byte[] image = br.ReadBytes((int)fs.Length);
                    br.Close();
                    fs.Close();
                    data += "Content-Disposition:attachment;name=\"" + mmsFileName + "\"\r\n";
                    data += "Content-Type:image/gif\r\n";
                    data += "Content-ID:<" + mmsFileName + ">\r\n";
                    data += "Content-Transfer-Encoding:binary\r\n\r\n";
                    byte[] firstPart = encoding.GetBytes(data);
                    int newSize = firstPart.Length + image.Length;
                    var ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
                    ms.Write(firstPart, 0, firstPart.Length);
                    ms.Write(image, 0, image.Length);
                    postBytes1 = ms.GetBuffer();
                    //byte[] secondpart = ms.GetBuffer();
                    //byte[] thirdpart = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
                    if (Session["mmsFilePath1"] != null)
                    {
                        var ms2 = JoinTwoByteArrays(postBytes, postBytes1);
                        totalpostBytes = ms2.GetBuffer();
                    }
                    else
                    {
                        totalpostBytes = postBytes1;
                    }
                    //Session["mmsFilePath2"] = null;
                }

                if (Session["mmsFilePath3"] != null)
                {
                    if (Session["mmsFilePath1"] != null || Session["mmsFilePath2"] != null)
                        data = "--" + boundary + "\r\n";
                    else
                        data += "--" + boundary + "\r\n";
                    string mmsFileName = Path.GetFileName(Session["mmsFilePath3"].ToString());
                    FileStream fs = new FileStream(Session["mmsFilePath3"].ToString(), FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    byte[] image = br.ReadBytes((int)fs.Length);
                    br.Close();
                    fs.Close();
                    data += "Content-Disposition:attachment;name=\"" + mmsFileName + "\"\r\n";
                    data += "Content-Type:image/gif\r\n";
                    data += "Content-ID:<" + mmsFileName + ">\r\n";
                    data += "Content-Transfer-Encoding:binary\r\n\r\n";
                    byte[] firstPart = encoding.GetBytes(data);
                    int newSize = firstPart.Length + image.Length;
                    var ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
                    ms.Write(firstPart, 0, firstPart.Length);
                    ms.Write(image, 0, image.Length);
                    postBytes2 = ms.GetBuffer();
                    if (Session["mmsFilePath1"] != null || Session["mmsFilePath2"] != null)
                    {
                        byte[] temp = totalpostBytes;
                        var ms2 = JoinTwoByteArrays(temp, postBytes2);
                        totalpostBytes = ms2.GetBuffer();
                    }
                    else
                    {
                        totalpostBytes = postBytes2;
                    }
                }

                byte[] byteLastBoundary = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
                //int totalSize = postBytes.Length + postBytes1.Length + postBytes2.Length + byteLastBoundary.Length;
                int totalSize = totalpostBytes.Length + byteLastBoundary.Length;
                var totalMS = new MemoryStream(new byte[totalSize], 0, totalSize, true, true);
                totalMS.Write(totalpostBytes, 0, totalpostBytes.Length);
                totalMS.Write(byteLastBoundary, 0, byteLastBoundary.Length);
                byte[] finalpostBytes = totalMS.GetBuffer();
                mmsRequestObject.ContentLength = finalpostBytes.Length;
                Stream postStream = mmsRequestObject.GetRequestStream();
                postStream.Write(finalpostBytes, 0, finalpostBytes.Length);
                postStream.Close();

                WebResponse mmsResponseObject = mmsRequestObject.GetResponse();
                using (StreamReader sr = new StreamReader(mmsResponseObject.GetResponseStream()))
                {
                    string mmsResponseData = sr.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    mmsResponseId deserializedJsonObj = (mmsResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(mmsResponseId));
                    messageIDTextBox.Text = deserializedJsonObj.id.ToString();
                    drawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString());
                    sr.Close();
                }
                mmsRequestObject = null;
                int indexj = 1;
                while (indexj <= 3)
                {
                    if (Convert.ToString(Session["mmsFilePath" + indexj]) != "")
                    {
                        if (File.Exists(Session["mmsFilePath" + indexj].ToString()))
                        {
                            System.IO.FileInfo fileInfo = new System.IO.FileInfo(Session["mmsFilePath" + indexj].ToString());
                            fileInfo.Delete();
                            Session["mmsFilePath" + indexj] = null;
                        }
                    }
                    indexj++;
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendMessagePanel, ex.ToString());
            int index = 1;

            while (index <= 3)
            {
                if (Convert.ToString(Session["mmsFilePath" + index]) != "")
                {
                    if (File.Exists(Session["mmsFilePath" + index].ToString()))
                    {
                        System.IO.FileInfo fileInfo = new System.IO.FileInfo(Session["mmsFilePath" + index].ToString());
                        fileInfo.Delete();
                        Session["mmsFilePath" + index] = null;
                    }
                }
                index++;
            }
        }
    }
    /* this function add two byte arrays and returns the address of buffer */
    private static MemoryStream JoinTwoByteArrays(byte[] firstByteArray, byte[] secondByteArray)
    {
        int newSize = firstByteArray.Length + secondByteArray.Length;
        var ms = new MemoryStream(new byte[newSize], 0, newSize, true, true);
        ms.Write(firstByteArray, 0, firstByteArray.Length);
        ms.Write(secondByteArray, 0, secondByteArray.Length);
        return ms;
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

    /* this funciton is called when user clicks on send mms button */

    protected void sendMMSMessageButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (readAndGetAccessToken(sendMessagePanel) == true)
            {
                if (access_token == null || access_token.Length <= 0)
                {
                    //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    return;
                }
                long fileSize = 0;
                if (FileUpload1.FileName.ToString() != "")
                {
                    FileUpload1.SaveAs(Request.MapPath(FileUpload1.FileName.ToString()));
                    Session["mmsFilePath1"] = Request.MapPath(FileUpload1.FileName);
                    FileInfo fileInfoObj = new FileInfo(Session["mmsFilePath1"].ToString());
                    fileSize = fileSize + fileInfoObj.Length / 1024;
                    if (fileSize > 600)
                    {
                        drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                        return;
                    }
                }
                if (FileUpload2.FileName.ToString() != "")
                {
                    FileUpload2.SaveAs(Request.MapPath(FileUpload2.FileName));
                    Session["mmsFilePath2"] = Request.MapPath(FileUpload2.FileName);
                    FileInfo fileInfoObj = new FileInfo(Session["mmsFilePath2"].ToString());
                    fileSize = fileSize + fileInfoObj.Length / 1024;
                    if (fileSize > 600)
                    {
                        drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                        return;
                    }
                }
                if (FileUpload3.FileName.ToString() != "")
                {
                    FileUpload3.SaveAs(Request.MapPath(FileUpload3.FileName));
                    Session["mmsFilePath3"] = Request.MapPath(FileUpload3.FileName);
                    FileInfo fileInfoObj = new FileInfo(Session["mmsFilePath3"].ToString());
                    fileSize = fileSize + fileInfoObj.Length / 1024;
                    if (fileSize > 600)
                    {
                        drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                        return;
                    }
                }
                if (fileSize <= 600)
                {
                    sendMms();
                }
                else
                {
                    drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(sendMessagePanel, ex.ToString());
            return;
        }
    }

    /* this function calls get message delivery status api to fetch the delivery status */
    private void getMmsDeliveryStatus()
    {
        try
        {
            string mmsId = messageIDTextBox.Text.ToString();
            if (mmsId == null || mmsId.Length <= 0)
            {
                drawPanelForFailure(getStatusPanel, "Message Id is null or empty");
                return;
            }
            if (readAndGetAccessToken(sendMessagePanel) == true)
            {
                if (access_token == null || access_token.Length <= 0)
                {
                    //drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    return;
                }
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
                    drawGetStatusSuccess(status.DeliveryInfoList.deliveryInfo[0].deliverystatus, status.DeliveryInfoList.resourceURL);
                    mmsStatusResponseStream.Close();
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(getStatusPanel, ex.ToString());
        }
    }
    /* this function draws table for get status result */
    private void drawGetStatusSuccess(string status, string url)
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
        //rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        TableCell rowTwoCellTwo = new TableCell();
        //rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = "Status: ";
        rowTwoCellOne.Font.Bold = true;
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

    /* this function is called when user click on get status button */
    protected void getStatusButton_Click(object sender, EventArgs e)
    {
        getMmsDeliveryStatus();
    }
}

/* The following are data structures used for the application */
public class mmsResponseId
{
    public string id;
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

public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;
}