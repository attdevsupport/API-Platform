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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public partial class Default : System.Web.UI.Page
{
    string FQDN;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    Table getStatusTable;

    /* This function reads access token related session variables to local variables */
    public void readTokenSessionVariables()
    {
        if (Session["dc_session_access_token"] != null)
            access_token = Session["dc_session_access_token"].ToString();
        else
            access_token = null;
        if (Session["dc_session_expiryMilliSeconds"] != null)
            expiryMilliSeconds = Session["dc_session_expiryMilliSeconds"].ToString();
        else
            expiryMilliSeconds = null;
        if (Session["dc_session_refresh_token"] != null)
            refresh_token = Session["dc_session_refresh_token"].ToString();
        else
            refresh_token = null;
        if (Session["dc_session_lastTokenTakenTime"] != null)
            lastTokenTakenTime = Session["dc_session_lastTokenTakenTime"].ToString();
        else
            lastTokenTakenTime = null;
        if (Session["dc_session_refreshTokenExpiryTime"] != null)
            refreshTokenExpiryTime = Session["dc_session_refreshTokenExpiryTime"].ToString();
        else
            refreshTokenExpiryTime = null; 
    }

    /* This function resets access token related session variable to null */
    public void resetTokenSessionVariables()
    {
        Session["dc_session_access_token"] = null;
        Session["dc_session_expiryMilliSeconds"] = null;
        Session["dc_session_refresh_token"] = null;
        Session["dc_session_lastTokenTakenTime"] = null;
        Session["dc_session_refreshTokenExpiryTime"] = null;
    }
    /* This function resets access token related  variable to null */
    public void resetTokenVariables()
    {
        access_token = null;
        expiryMilliSeconds = null;
        refresh_token = null;
        lastTokenTakenTime = null;
        refreshTokenExpiryTime = null;
    }

    /* This function validates access token related variables and returns VALID_ACCESS_TOKEN if its valid
     * otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
     * return REFRESH_TOKEN, if access token in expired and refresh token is valid
     */
    public string isTokenValid()
    {
        try
        {
            if (Session["dc_session_access_token"] == null)
                return "INVALID_ACCESS_TOKEN";
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
                return "REFRESH_TOKEN";
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

    /* This function is used to neglect the ssl handshake error with authentication server */
    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    } 

    /* This function redirects to authentication server to get the access code */
    public void getAuthCode()
    {
        try
        {
            Response.Redirect("" + FQDN + "/oauth/authorize?scope=" + scope + "&client_id=" + api_key + "&redirect_url=" + authorize_redirect_uri);
        }
        catch (Exception ex)
        {
            drawPanelForFailure(dcPanel, ex.ToString());
        }
    }

    /* This function get the access token based on the type parameter type values.
    * If type value is 0, access token is fetch for authorization code flow
    * If type value is 2, access token is fetch for authorization code floww based on the exisiting refresh token
    */
    public bool getAccessToken(int type)
    {
        if (type == 0)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/token");
                accessTokenRequest.Method = "POST";
                string oauthParameters = "client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&code=" + auth_code.ToString() + "&grant_type=authorization_code";
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
                    if (deserializedJsonObj.access_token != null)
                    {
                        access_token = deserializedJsonObj.access_token.ToString();
                        expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                        refresh_token = deserializedJsonObj.refresh_token.ToString();
                        lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                        DateTime refreshExpiry = currentServerTime.AddHours(24);
                        refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                        Session["dc_session_access_token"] = access_token.ToString();
                        Session["dc_session_expiryMilliSeconds"] = expiryMilliSeconds.ToString();
                        Session["dc_session_refresh_token"] = refresh_token.ToString();
                        Session["dc_session_lastTokenTakenTime"] = lastTokenTakenTime.ToString();
                        Session["dc_session_refreshTokenExpiryTime"] = refreshTokenExpiryTime.ToString();
                        accessTokenResponseStream.Close();
                        return true;
                    }
                    else
                    {
                        drawPanelForFailure(dcPanel, "Auth server returned null access token");
                        return false;

                    }
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(dcPanel, ex.ToString());
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
                    if (deserializedJsonObj.access_token != null)
                    {
                        access_token = deserializedJsonObj.access_token.ToString();
                        expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                        refresh_token = deserializedJsonObj.refresh_token.ToString();
                        lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                        DateTime refreshExpiry = currentServerTime.AddHours(24);
                        refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                        Session["dc_session_access_token"] = access_token.ToString();
                        Session["dc_session_expiryMilliSeconds"] = expiryMilliSeconds.ToString();
                        Session["dc_session_refresh_token"] = refresh_token.ToString();
                        Session["dc_session_lastTokenTakenTime"] = lastTokenTakenTime.ToString();
                        Session["dc_session_refreshTokenExpiryTime"] = refreshTokenExpiryTime.ToString();
                        accessTokenResponseStream.Close();
                        return true;
                    }
                    else
                    {
                        drawPanelForFailure(dcPanel, "Auth server returned null access token");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(dcPanel, ex.ToString());
                return false;
            }
        }
        return false;
    }

    /* This funciton draws table for error response */
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
            if (ConfigurationManager.AppSettings["FQDN"] == null)
            {
                drawPanelForFailure(dcPanel, "FQDN is not defined in configuration file");
                return;
            }
            FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
            if (ConfigurationManager.AppSettings["api_key"] == null)
            {
                drawPanelForFailure(dcPanel, "api_key is not defined in configuration file");
                return;
            }
            api_key = ConfigurationManager.AppSettings["api_key"].ToString();
            if (ConfigurationManager.AppSettings["secret_key"] == null)
            {
                drawPanelForFailure(dcPanel, "secret_key is not defined in configuration file");
                return;
            }
            secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
            if (ConfigurationManager.AppSettings["scope"] == null)
            {
                scope = "DC";
            }
            else
            {
                scope = ConfigurationManager.AppSettings["scope"].ToString();
            }
            if (ConfigurationManager.AppSettings["authorize_redirect_uri"] == null)
            {
                drawPanelForFailure(dcPanel, "authorize_redirect_uri is not defined in configuration file");
                return;
            }
            authorize_redirect_uri = ConfigurationManager.AppSettings["authorize_redirect_uri"].ToString();
            if ((Session["dc_session_appState"] != null) && (Request["Code"] != null))
            {
                auth_code = Request["code"].ToString();
                if (getAccessToken(0) == true)
                {
                    readTokenSessionVariables();
                }
                else
                {
                    drawPanelForFailure(dcPanel, "Failed to get Access token");
                    resetTokenSessionVariables();
                    resetTokenVariables();
                    Session["dc_session_DeviceIdForWhichTokenAcquired"] = null;
                    return;
                }
            }
            if (Session["dc_session_appState"] != null)
            {
                Session["dc_session_appState"] = null;
                if (Session["dc_session_GdeviceID"] != null)
                {
                    Session["dc_session_DeviceIdForWhichTokenAcquired"] = Session["dc_session_GdeviceID"].ToString();
                    Session["dc_session_GdeviceID"] = null;
                }
                dcPhoneNumberTextBox.Text = Session["dc_session_deviceID"].ToString();
                Session["dc_session_deviceID"] = null;
                getDCCapabilities_Click(null,null);
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(dcPanel, ex.ToString());
        }

    }
    /*This funciton checks the validity of string as msisdn */

    private Boolean isValidMISDN(string number)
    {
        string smsAddressInput = number;
        long tryParseResult = 0;
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

    /*This method is used to draw table for successful response of get device capabilities */

    private void drawPanelForGetStatusResult(string attribute, string value, bool headerFlag)
    {
        if (headerFlag == true)
        {
            Table getStatusTableHeading = new Table();
            getStatusTableHeading.Font.Name = "Sans-serif";
            getStatusTableHeading.Font.Size = 9;
            getStatusTableHeading.BorderStyle = BorderStyle.Outset;
            getStatusTableHeading.Width = Unit.Pixel(650);
            TableRow one = new TableRow();
            TableCell cell = new TableCell();
            cell.Text = "SUCCESS:";
            cell.Font.Bold = true;
            one.Controls.Add(cell);
            TableRow two = new TableRow();
            cell = new TableCell();
            cell.Text = "Device parameters listed below";
            two.Controls.Add(cell);
            getStatusTableHeading.Controls.Add(one);
            getStatusTableHeading.Controls.Add(two);
            getStatusTableHeading.BorderWidth = 2;
            getStatusTableHeading.BorderColor = Color.DarkGreen;
            getStatusTableHeading.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
            getStatusTableHeading.Font.Size = 9;
            dcPanel.Controls.Add(getStatusTableHeading);

            getStatusTable = new Table();
            getStatusTable.Font.Size = 9;
            getStatusTable.Font.Name = "Sans-serif";
            getStatusTable.Font.Italic = true;
            //getStatusTable.HorizontalAlign = HorizontalAlign.Center;
            TableRow rowOne = new TableRow();
            TableCell rowOneCellOne = new TableCell();
            rowOneCellOne.Font.Bold = true;
            //rowOneCellOne.BorderWidth = 1;
            rowOneCellOne.Text = "Parameter";
            rowOneCellOne.HorizontalAlign = HorizontalAlign.Center;
            rowOne.Controls.Add(rowOneCellOne);
            TableCell rowOneCellTwo = new TableCell();
            rowOneCellTwo.Font.Bold = true;
            //rowOneCellTwo.BorderWidth = 1;
            rowOneCellTwo.Text = "Value";
            rowOneCellTwo.HorizontalAlign = HorizontalAlign.Center;
            rowOne.Controls.Add(rowOneCellTwo);
            //getStatusTable.BorderWidth = 2;
            //getStatusTable.BorderColor = Color.DarkGreen;
            //getStatusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
            getStatusTable.Controls.Add(rowOne);
            dcPanel.Controls.Add(getStatusTable);
        }
        else
        {
            TableRow row = new TableRow();
            TableCell cell1 = new TableCell();
            TableCell cell2 = new TableCell();
            cell1.Text = attribute.ToString();
            //cell1.BorderWidth = 1;
            cell1.HorizontalAlign = HorizontalAlign.Center;
            row.Controls.Add(cell1);
            cell2.Text = value.ToString();
            //cell2.BorderWidth = 1;
            cell2.HorizontalAlign = HorizontalAlign.Center;
            row.Controls.Add(cell2);
            getStatusTable.Controls.Add(row);
        }
    }
  /*
    * User invoked Event to get device Information
 */
    protected void getDCCapabilities_Click(object sender, EventArgs e)
    {
        try
        {
            try
            {
                if (isValidMISDN(dcPhoneNumberTextBox.Text.ToString()) == false)
                {
                    drawPanelForFailure(dcPanel,"Invalid Number");
                    return;
                }
                string deviceId =dcPhoneNumberTextBox.Text.ToString().Replace("tel:+1", "");
                deviceId = deviceId.ToString().Replace("tel:+", "");
                deviceId = deviceId.ToString().Replace("tel:1", "");
                deviceId = deviceId.ToString().Replace("tel:", "");
                deviceId = deviceId.ToString().Replace("tel:", "");
                deviceId = deviceId.ToString().Replace("+1", "");
                deviceId = deviceId.ToString().Replace("-", "");
                if (deviceId.Length == 11)
                {
                    deviceId = deviceId.Remove(0, 1);
                }
                String dcResponseData;
                readTokenSessionVariables();
                string tokentResult = isTokenValid();
                if (tokentResult.CompareTo("INVALID_ACCESS_TOKEN") == 0)
                {
                    Session["dc_session_appState"] = "GetToken";
                    Session["dc_session_deviceID"] = dcPhoneNumberTextBox.Text.ToString();
                    Session["dc_session_GdeviceID"] = deviceId.ToString();
                    getAuthCode();
                }
                else if (tokentResult.CompareTo("REFRESH_TOKEN") == 0)
                {
                    if (getAccessToken(2) == true)
                    {
                        readTokenSessionVariables();
                    }
                    else
                    {
                        drawPanelForFailure(dcPanel, "Failed to get Access token");
                        resetTokenSessionVariables();
                        resetTokenVariables();
                        Session["dc_session_DeviceIdForWhichTokenAcquired"] = null;
                        return;
                    }
                }
                if ((Session["dc_session_DeviceIdForWhichTokenAcquired"] != null) && (Session["dc_session_DeviceIdForWhichTokenAcquired"].ToString().CompareTo(deviceId.ToString()) != 0))
                {
                    resetTokenSessionVariables();
                    resetTokenVariables();
                    Session["dc_session_appState"] = "GetToken";
                    Session["dc_session_deviceID"] = dcPhoneNumberTextBox.Text.ToString();
                    Session["dc_session_GdeviceID"] = deviceId.ToString();
                    getAuthCode();
                }
                //readCheckVerifyAccessToken();
                // Form Http Web Request
                HttpWebRequest deviceInfoRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/1/devices/tel:" + deviceId.ToString() + "/info?access_token=" + access_token.ToString());
                deviceInfoRequestObject.Method = "GET";
                
                HttpWebResponse deviceInfoResponse = (HttpWebResponse)deviceInfoRequestObject.GetResponse();
                using (StreamReader deviceInfoResponseStream = new StreamReader(deviceInfoResponse.GetResponseStream()))
                {
                    dcResponseData = deviceInfoResponseStream.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    DeviceCapabilities deserializedJsonObj = (DeviceCapabilities)deserializeJsonObject.Deserialize(dcResponseData, typeof(DeviceCapabilities));
                    drawPanelForGetStatusResult("", "", true);
                    drawPanelForGetStatusResult("acwmodel", deserializedJsonObj.deviceId.acwmodel.ToString(), false);
                    drawPanelForGetStatusResult("acwdevcert", deserializedJsonObj.deviceId.acwdevcert.ToString(), false);
                    drawPanelForGetStatusResult("acwrel", deserializedJsonObj.deviceId.acwrel.ToString(), false);
                    drawPanelForGetStatusResult("acwvendor", deserializedJsonObj.deviceId.acwvendor.ToString(), false);
                    drawPanelForGetStatusResult("acwaocr", deserializedJsonObj.capabilities.acwaocr.ToString(), false);
                    drawPanelForGetStatusResult("acwav", deserializedJsonObj.capabilities.acwav.ToString(), false);
                    drawPanelForGetStatusResult("acwcf", deserializedJsonObj.capabilities.acwcf.ToString(), false);
                    drawPanelForGetStatusResult("acwtermtype", deserializedJsonObj.capabilities.acwtermtype.ToString(), false);
                    //Session["DeviceIdForWhichTokenAcquired"] = deviceId.ToString();
                    deviceInfoResponseStream.Close();
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(dcPanel, ex.ToString());
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(dcPanel, ex.ToString());
        }
    }

}

/* Below are data structures used for application */

public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;
}

public class DeviceCapabilities
{
    public DeviceId deviceId { get; set; }
    public Capabilities capabilities { get; set; }
}

public class DeviceId
{
    public string acwdevcert { get; set; }
    public string acwrel { get; set; }
    public string acwmodel { get; set; }
    public string acwvendor { get; set; }
}

public class Capabilities
{
    public string acwav { get; set; }
    public string acwaocr { get; set; }
    public string acwcf { get; set; }
    public string acwtermtype { get; set; }
}