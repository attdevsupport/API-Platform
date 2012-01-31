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
    Table getStatusTable;

    /* This function reads access token related session variables to local variables */
    public void readTokenSessionVariables()
    {
        if (Session["tl_session_access_token"] != null)
            access_token = Session["tl_session_access_token"].ToString();
        else
            access_token = null;
        if (Session["tl_session_expiryMilliSeconds"] != null)
            expiryMilliSeconds = Session["tl_session_expiryMilliSeconds"].ToString();
        else
            expiryMilliSeconds = null;
        if (Session["tl_session_refresh_token"] != null)
            refresh_token = Session["tl_session_refresh_token"].ToString();
        else
            refresh_token = null;
        if (Session["tl_session_lastTokenTakenTime"] != null)
            lastTokenTakenTime = Session["tl_session_lastTokenTakenTime"].ToString();
        else
            lastTokenTakenTime = null;
        if (Session["tl_session_refreshTokenExpiryTime"] != null)
            refreshTokenExpiryTime = Session["tl_session_refreshTokenExpiryTime"].ToString();
        else
            refreshTokenExpiryTime = null;
    }
    /* This function resets access token related session variable to null */
    public void resetTokenSessionVariables()
    {
        Session["tl_session_access_token"] = null;
        Session["tl_session_expiryMilliSeconds"] = null;
        Session["tl_session_refresh_token"] = null;
        Session["tl_session_lastTokenTakenTime"] = null;
        Session["tl_session_refreshTokenExpiryTime"] = null;
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
            if (Session["tl_session_access_token"] == null)
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
            //drawPanelForFailure(dcPanel, ex.ToString());
            return "INVALID_ACCESS_TOKEN";
        }
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
            drawPanelForFailure(tlPanel, ex.ToString());
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
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&code=" + auth_code.ToString() + "&grant_type=authorization_code");
                accessTokenRequest.Method = "GET";
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
                        Session["tl_session_access_token"] = access_token.ToString();
                        Session["tl_session_expiryMilliSeconds"] = expiryMilliSeconds.ToString();
                        Session["tl_session_refresh_token"] = refresh_token.ToString();
                        Session["tl_session_lastTokenTakenTime"] = lastTokenTakenTime.ToString();
                        Session["tl_session_refreshTokenExpiryTime"] = refreshTokenExpiryTime.ToString();
                        accessTokenResponseStream.Close();
                        return true;
                    }
                    else
                    {
                        drawPanelForFailure(tlPanel, "Auth server returned null access token");
                        return false;

                    }
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(tlPanel, ex.ToString());
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
                    if (deserializedJsonObj.access_token != null)
                    {
                        access_token = deserializedJsonObj.access_token.ToString();
                        expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                        refresh_token = deserializedJsonObj.refresh_token.ToString();
                        lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                        DateTime refreshExpiry = currentServerTime.AddHours(24);
                        refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                        Session["tl_session_access_token"] = access_token.ToString();
                        Session["tl_session_expiryMilliSeconds"] = expiryMilliSeconds.ToString();
                        Session["tl_session_refresh_token"] = refresh_token.ToString();
                        Session["tl_session_lastTokenTakenTime"] = lastTokenTakenTime.ToString();
                        Session["tl_session_refreshTokenExpiryTime"] = refreshTokenExpiryTime.ToString();
                        accessTokenResponseStream.Close();
                        return true;
                    }
                    else
                    {
                        drawPanelForFailure(tlPanel, "Auth server returned null access token");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(tlPanel, ex.ToString());
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
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.Red;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(table);
    }


    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
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
            map_canvas.Visible = false;
            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            if (ConfigurationManager.AppSettings["FQDN"] == null)
            {
                drawPanelForFailure(tlPanel, "FQDN is not defined in configuration file");
                return;
            }
            FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
            if (ConfigurationManager.AppSettings["api_key"] == null)
            {
                drawPanelForFailure(tlPanel, "api_key is not defined in configuration file");
                return;
            }
            api_key = ConfigurationManager.AppSettings["api_key"].ToString();
            if (ConfigurationManager.AppSettings["secret_key"] == null)
            {
                drawPanelForFailure(tlPanel, "secret_key is not defined in configuration file");
                return;
            }
            secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
            if (ConfigurationManager.AppSettings["scope"] != null)
            {

                scope = ConfigurationManager.AppSettings["scope"].ToString();
            }
            else
            {
                scope = "TL";
            }

            if (ConfigurationManager.AppSettings["authorize_redirect_uri"] == null)
            {
                drawPanelForFailure(tlPanel, "authorize_redirect_uri is not defined in configuration file");
                return;
            }
            authorize_redirect_uri = ConfigurationManager.AppSettings["authorize_redirect_uri"].ToString();
            if ((Session["tl_session_appState"] != null) && (Request["Code"] != null))
            {
                auth_code = Request["code"].ToString();
                if (getAccessToken(0) == true)
                {
                    readTokenSessionVariables();
                }
                else
                {
                    drawPanelForFailure(tlPanel, "Failed to get Access token");
                    resetTokenSessionVariables();
                    resetTokenVariables();
                    Session["tl_session_DeviceIdForWhichTokenAcquired"] = null;
                    return;
                }
            }
            if (Session["tl_session_appState"] != null)
            {
                Session["tl_session_appState"] = null;
                if (Session["tl_session_GdeviceID"] != null)
                {
                    Session["tl_session_DeviceIdForWhichTokenAcquired"] = Session["tl_session_GdeviceID"].ToString();
                    Session["tl_session_GdeviceID"] = null;
                }
                tlTextBox.Text = Session["tl_session_deviceID"].ToString();
                Radio_AcceptedAccuracy.SelectedIndex = Convert.ToInt32(Session["tl_session_acceptableAccuracy"].ToString());
                Radio_RequestedAccuracy.SelectedIndex = Convert.ToInt32(Session["tl_session_requestedAccuracy"].ToString());
                Radio_DelayTolerance.SelectedIndex = Convert.ToInt32(Session["tl_session_tolerance"].ToString());
                Session["tl_session_deviceID"] = null;
                Session["tl_session_acceptableAccuracy"] = null;
                Session["tl_session_requestedAccuracy"] = null;
                Session["tl_session_tolerance"] = null;
                tlButton_Click1(null, null);
            }
         }
        catch (Exception ex)
        {
            drawPanelForFailure(tlPanel, ex.ToString());
        }

    }

    /*This funciton checks the validity of string as msisdn */
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

    /*This method is used to draw table for successful response of get device locations */

    private void drawPanelForGetLocationResult(string attribute, string value, bool headerFlag)
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
            rowOneCellOne.Font.Bold = true;
            rowOneCellOne.Text = "SUCCESS:";
            rowOne.Controls.Add(rowOneCellOne);
            getStatusTable.BorderWidth = 2;
            getStatusTable.BorderColor = Color.DarkGreen;
            getStatusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
            getStatusTable.Controls.Add(rowOne);
            tlPanel.Controls.Add(getStatusTable);
        }
        else
        {
            TableRow row = new TableRow();
            TableCell cell1 = new TableCell();
            TableCell cell2 = new TableCell();
            cell1.Text = attribute.ToString();
            cell1.Font.Bold = true;
            cell1.Width = Unit.Pixel(100);
            //cell1.BorderWidth = 1;
            row.Controls.Add(cell1);
            cell2.Text = value.ToString();
            //cell2.BorderWidth = 1;
            row.Controls.Add(cell2);
            getStatusTable.Controls.Add(row);
        }
    }

    /* this function is called when user clicks on get location button */

    protected void tlButton_Click1(object sender, EventArgs e)
    {
        try
        {

            if (isValidMISDN(tlTextBox.Text.ToString()) == false)
            {
                drawPanelForFailure(tlPanel, "Invalid Number");
                return;
            }
            string deviceId = tlTextBox.Text.ToString().Replace("tel:+1", "");
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
            String strResult;
            int[] definedReqAccuracy = new int[3] { 100, 1000, 10000 };
            string[] definedTolerance = new string[3] { "NoDelay", "LowDelay", "DelayTolerant" };
            int requestedAccuracy, acceptableAccuracy;
            string tolerance;
            acceptableAccuracy = definedReqAccuracy[Radio_AcceptedAccuracy.SelectedIndex];
            requestedAccuracy = definedReqAccuracy[Radio_RequestedAccuracy.SelectedIndex];
            tolerance = definedTolerance[Radio_DelayTolerance.SelectedIndex];
            readTokenSessionVariables();
            string tokentResult = isTokenValid();
            if (tokentResult.CompareTo("INVALID_ACCESS_TOKEN") == 0)
            {
                Session["tl_session_appState"] = "GetToken";
                Session["tl_session_deviceID"] = tlTextBox.Text.ToString();
                Session["tl_session_acceptableAccuracy"] = Radio_AcceptedAccuracy.SelectedIndex;
                Session["tl_session_requestedAccuracy"] = Radio_RequestedAccuracy.SelectedIndex;
                Session["tl_session_tolerance"] = Radio_DelayTolerance.SelectedIndex;
                Session["tl_session_GdeviceID"] = deviceId.ToString();
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
                    drawPanelForFailure(tlPanel, "Failed to get Access token");
                    resetTokenSessionVariables();
                    resetTokenVariables();
                    Session["tl_session_DeviceIdForWhichTokenAcquired"] = null;
                    return;
                }
            }
            if ((Session["tl_session_DeviceIdForWhichTokenAcquired"] != null) && (Session["tl_session_DeviceIdForWhichTokenAcquired"].ToString().CompareTo(deviceId.ToString()) != 0))
            //if ((Session["tl_session_DeviceIdForWhichTokenAcquired"] != null) && (Session["tl_session_DeviceIdForWhichTokenAcquired"].ToString().CompareTo(tlTextBox.Text.ToString()) != 0))
            {
                resetTokenSessionVariables();
                resetTokenVariables();
                Session["tl_session_appState"] = "GetToken";
                Session["tl_session_GdeviceID"] = deviceId.ToString();
                Session["tl_session_deviceID"] = tlTextBox.Text.ToString();
                Session["tl_session_acceptableAccuracy"] = Radio_AcceptedAccuracy.SelectedIndex;
                Session["tl_session_requestedAccuracy"] = Radio_RequestedAccuracy.SelectedIndex;
                Session["tl_session_tolerance"] = Radio_DelayTolerance.SelectedIndex;
                getAuthCode();
            }
            HttpWebRequest tlRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/1/devices/tel:" + deviceId.ToString() + "/location?access_token=" + access_token.ToString() + "&requestedAccuracy=" + requestedAccuracy.ToString() + "&acceptableAccuracy=" + acceptableAccuracy.ToString() + "&tolerance=" + tolerance.ToString());
            tlRequestObject.Method = "GET";
            DateTime msgSentTime = DateTime.UtcNow.ToLocalTime();
            HttpWebResponse tlResponseObject = (HttpWebResponse)tlRequestObject.GetResponse();
            DateTime msgReceivedTime = DateTime.UtcNow.ToLocalTime();
            TimeSpan tokenSpan = msgReceivedTime.Subtract(msgSentTime);
            // the using keyword will automatically dispose the object 
            // once complete
            using (StreamReader tlResponseStream = new StreamReader(tlResponseObject.GetResponseStream()))
            {
                strResult = tlResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                tlResponse deserializedJsonObj = (tlResponse)deserializeJsonObject.Deserialize(strResult, typeof(tlResponse));
                drawPanelForGetLocationResult("", "", true);
                drawPanelForGetLocationResult("Accuracy:", deserializedJsonObj.accuracy.ToString(), false);
                drawPanelForGetLocationResult("Altitude:", deserializedJsonObj.altitude.ToString(), false);
                drawPanelForGetLocationResult("Latitude:", deserializedJsonObj.latitude.ToString(), false);
                drawPanelForGetLocationResult("Longitude:", deserializedJsonObj.longitude.ToString(), false);
                drawPanelForGetLocationResult("TimeStamp:", deserializedJsonObj.timestamp.ToString(), false);
                drawPanelForGetLocationResult("Response Time:",tokenSpan.Seconds.ToString()+"seconds", false);
                MapTerminalLocation.Visible = true;
                map_canvas.Visible = true;
                StringBuilder googleString = new StringBuilder();
                googleString.Append("http://maps.google.com/?q=" + deserializedJsonObj.latitude.ToString() + "+" + deserializedJsonObj.longitude.ToString() + "&output=embed");
                MapTerminalLocation.Attributes["src"]= googleString.ToString();
                // Close and clean up the StreamReader
                tlResponseStream.Close();
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(tlPanel,ex.ToString());
        }
    }
}

/* Following are the data structures used for this applicaiton */

public class AccessTokenResponse
{
    public string access_token;
    public string refresh_token;
    public string expires_in;

}
public class tlResponse
{
    public string accuracy { get; set; }
    public double altitude { get; set; }
    public double latitude { get; set; }
    public string longitude { get; set; }
    public DateTime timestamp { get; set; }
}