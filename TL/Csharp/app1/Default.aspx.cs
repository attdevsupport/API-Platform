// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References

using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;

#endregion

/// <summary>
/// Access Token Types
/// </summary>
public enum AccessTokenType
{
    /// <summary>
    /// Access Token Type is based on Authorization Code
    /// </summary>
    Authorization_Code,

    /// <summary>
    /// Access Token Type is based on Refresh Token
    /// </summary>
    Refresh_Token
}

/// <summary>
/// TL_App1 class
/// </summary>
public partial class TL_App1 : System.Web.UI.Page
{
    #region Local variables

    /// <summary>
    /// Gets or sets the value of endPoint
    /// </summary>
    private string endPoint;

    /// <summary>
    /// Access Token Variables
    /// </summary>
    private string apiKey, secretKey, accessToken, authorizeRedirectUri, scope, refreshToken, accessTokenExpiryTime, refreshTokenExpiryTime;

    /// <summary>
    /// Gets or sets the value of authCode
    /// </summary>
    private string authCode;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    /// <summary>
    /// Gets or sets the Status Table
    /// </summary>
    private Table getStatusTable;

#endregion

    #region SSL Handshake Error
    
    /// <summary>
    /// Neglect the ssl handshake error with authentication server
    /// </summary>
    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    #endregion

    #region Events

    /// <summary>
    /// This function is called when the applicaiton page is loaded into the browser.
    /// This function reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">object that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();
            map_canvas.Visible = false;

            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";

            bool ableToRead = this.ReadConfigFile();
            if (!ableToRead)
            {
                return;
            }

            if (null != Session["tl_session_acceptableAccuracy"])
            {
                Radio_AcceptedAccuracy.SelectedIndex = Convert.ToInt32(Session["tl_session_acceptableAccuracy"].ToString());
                Radio_RequestedAccuracy.SelectedIndex = Convert.ToInt32(Session["tl_session_requestedAccuracy"].ToString());
                Radio_DelayTolerance.SelectedIndex = Convert.ToInt32(Session["tl_session_tolerance"].ToString());
            }

            if ((Session["tl_session_appState"] == "GetToken") && (Request["Code"] != null))
            {
                this.authCode = Request["code"];
                bool ableToGetToken = this.GetAccessToken(AccessTokenType.Authorization_Code);
                if (ableToGetToken)
                {
                    this.GetDeviceLocation();
                }
                else
                {
                    this.DrawPanelForFailure(tlPanel, "Failed to get Access token");
                    this.ResetTokenSessionVariables();
                    this.ResetTokenVariables();
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(tlPanel, ex.ToString());
        }
    }

    /// <summary>
    /// Event that will be triggered when the user clicks on GetPhoneLocation button
    /// This method calls GetDeviceLocation Api
    /// </summary>
    /// <param name="sender">object that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void GetDeviceLocation_Click(object sender, EventArgs e)
    {
        try
        {
            Session["tl_session_acceptableAccuracy"] = Radio_AcceptedAccuracy.SelectedIndex;
            Session["tl_session_requestedAccuracy"] = Radio_RequestedAccuracy.SelectedIndex;
            Session["tl_session_tolerance"] = Radio_DelayTolerance.SelectedIndex;

            bool ableToGetAccessToken = this.ReadAndGetAccessToken();
            if (ableToGetAccessToken)
            {
                this.GetDeviceLocation();
            }
            else
            {
                this.DrawPanelForFailure(tlPanel, "Unable to get access token");
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(tlPanel, ex.Message);
        }
    }

    #endregion

    #region API Invokation

    /// <summary>
    /// This method invokes Device Location API and displays the location
    /// </summary>
    private void GetDeviceLocation()
    {
        try
        {
            int[] definedReqAccuracy = new int[3] { 100, 1000, 10000 };
            string[] definedTolerance = new string[3] { "NoDelay", "LowDelay", "DelayTolerant" };

            int requestedAccuracy, acceptableAccuracy;
            string tolerance;

            acceptableAccuracy = definedReqAccuracy[Radio_AcceptedAccuracy.SelectedIndex];
            requestedAccuracy = definedReqAccuracy[Radio_RequestedAccuracy.SelectedIndex];
            tolerance = definedTolerance[Radio_DelayTolerance.SelectedIndex];
            
            string strResult;

            HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/2/devices/location?requestedAccuracy=" + requestedAccuracy + "&acceptableAccuracy=" + acceptableAccuracy + "&tolerance=" + tolerance);
            webRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
            webRequest.Method = "GET";
            
            DateTime msgSentTime = DateTime.UtcNow.ToLocalTime();
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            DateTime msgReceivedTime = DateTime.UtcNow.ToLocalTime();
            TimeSpan tokenSpan = msgReceivedTime.Subtract(msgSentTime);
            
            using (StreamReader responseStream = new StreamReader(webResponse.GetResponseStream()))
            {
                strResult = responseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                TLResponse deserializedJsonObj = (TLResponse)deserializeJsonObject.Deserialize(strResult, typeof(TLResponse));

                this.DrawPanelForGetLocationResult(string.Empty, string.Empty, true);
                this.DrawPanelForGetLocationResult("Accuracy:", deserializedJsonObj.accuracy, false);
                this.DrawPanelForGetLocationResult("Latitude:", deserializedJsonObj.latitude, false);
                this.DrawPanelForGetLocationResult("Longitude:", deserializedJsonObj.longitude, false);
                this.DrawPanelForGetLocationResult("TimeStamp:", deserializedJsonObj.timestamp, false);
                this.DrawPanelForGetLocationResult("Response Time:", tokenSpan.Seconds.ToString() + "seconds", false);

                MapTerminalLocation.Visible = true;
                map_canvas.Visible = true;
                StringBuilder googleString = new StringBuilder();
                googleString.Append("http://maps.google.com/?q=" + deserializedJsonObj.latitude + "+" + deserializedJsonObj.longitude + "&output=embed");
                MapTerminalLocation.Attributes["src"] = googleString.ToString();

                responseStream.Close();
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    StreamReader streamReader = new StreamReader(stream);
                    this.DrawPanelForFailure(tlPanel, streamReader.ReadToEnd());
                    streamReader.Close();
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(tlPanel, ex.Message);
        }
    }

    #endregion

    #region Access Token Methods

    /// <summary>
    /// Reads from session variables and gets access token
    /// </summary>
    /// <returns>true/false; true on successfully getting access token, else false</returns>
    private bool ReadAndGetAccessToken()
    {
        this.ReadTokenSessionVariables();

        string tokentResult = this.IsTokenValid();
        if (tokentResult.Equals("INVALID_ACCESS_TOKEN"))
        {
            Session["tl_session_appState"] = "GetToken";
            this.GetAuthCode();
        }
        else if (tokentResult.Equals("REFRESH_TOKEN"))
        {
            bool ableToGetToken = this.GetAccessToken(AccessTokenType.Refresh_Token);
            if (ableToGetToken == false)
            {
                this.DrawPanelForFailure(tlPanel, "Failed to get Access token");
                this.ResetTokenSessionVariables();
                this.ResetTokenVariables();
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This function reads access token related session variables to local variables 
    /// </summary>
    private void ReadTokenSessionVariables()
    {
        this.accessToken = string.Empty;
        if (Session["tl_session_access_token"] != null)
        {
            this.accessToken = Session["tl_session_access_token"].ToString();
        }

        this.refreshToken = null;
        if (Session["tl_session_refresh_token"] != null)
        {
            this.refreshToken = Session["tl_session_refresh_token"].ToString();
        }

        this.accessTokenExpiryTime = null;
        if (Session["tl_session_accessTokenExpiryTime"] != null)
        {
            this.accessTokenExpiryTime = Session["tl_session_accessTokenExpiryTime"].ToString();
        }

        this.refreshTokenExpiryTime = null;
        if (Session["tl_session_refreshTokenExpiryTime"] != null)
        {
            this.refreshTokenExpiryTime = Session["tl_session_refreshTokenExpiryTime"].ToString();
        }        
    }

    /// <summary>
    /// This function resets access token related session variable to null 
    /// </summary>
    private void ResetTokenSessionVariables()
    {
        Session["tl_session_access_token"] = null;
        Session["tl_session_refresh_token"] = null;
        Session["tl_session_accessTokenExpiryTime"] = null;
        Session["tl_session_refreshTokenExpiryTime"] = null;
    }

    /// <summary>
    /// This function resets access token related  variable to null 
    /// </summary>
    private void ResetTokenVariables()
    {
        this.accessToken = null;
        this.refreshToken = null;
        this.accessTokenExpiryTime = null;
        this.refreshTokenExpiryTime = null;
    }

    /// <summary>
    /// This function validates access token related variables and returns VALID_ACCESS_TOKEN if its valid
    /// otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    /// return REFRESH_TOKEN, if access token in expired and refresh token is valid 
    /// </summary>
    /// <returns>string variable containing valid/invalid access/refresh token</returns>
    private string IsTokenValid()
    {
        if (Session["tl_session_access_token"] == null)
        {
            return "INVALID_ACCESS_TOKEN";
        }

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
    /// Redirects to authentication server to get the access code
    /// </summary>
    private void GetAuthCode()
    {
        Response.Redirect(string.Empty + this.endPoint + "/oauth/authorize?scope=" + this.scope + "&client_id=" + this.apiKey + "&redirect_url=" + this.authorizeRedirectUri);
    }

    /// <summary>
    /// Get access token based on the type parameter type values.
    /// </summary>
    /// <param name="type">If type value is Authorization_code, access token is fetch for authorization code flow
    /// If type value is Refresh_Token, access token is fetch for authorization code floww based on the exisiting refresh token</param>
    /// <returns>true/false; true if success, else false</returns>
    private bool GetAccessToken(AccessTokenType type)
    {
        Stream postStream = null;
        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token");
            accessTokenRequest.Method = "POST";

            string oauthParameters = string.Empty;

            if (type == AccessTokenType.Authorization_Code)
            {
                oauthParameters = "client_id=" + this.apiKey.ToString() + "&client_secret=" + this.secretKey + "&code=" + this.authCode + "&grant_type=authorization_code&scope=" + this.scope;
            }
            else if (type == AccessTokenType.Refresh_Token)
            {
                oauthParameters = "grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken;
            }

            accessTokenRequest.ContentType = "application/x-www-form-urlencoded";
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = encoding.GetBytes(oauthParameters);
            accessTokenRequest.ContentLength = postBytes.Length;
            postStream = accessTokenRequest.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string access_token_json = accessTokenResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(access_token_json, typeof(AccessTokenResponse));
                if (deserializedJsonObj.access_token != null)
                {
                    this.accessToken = deserializedJsonObj.access_token;
                    this.refreshToken = deserializedJsonObj.refresh_token;
                    this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString();

                    DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                    if (deserializedJsonObj.expires_in.Equals("0"))
                    {
                        int defaultAccessTokenExpiresIn = 100; // In Years
                        this.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToString();
                    }

                    this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();                    
                    
                    Session["tl_session_access_token"] = this.accessToken;
                    Session["tl_session_refresh_token"] = this.refreshToken;
                    Session["tl_session_accessTokenExpiryTime"] = this.accessTokenExpiryTime;
                    Session["tl_session_refreshTokenExpiryTime"] = this.refreshTokenExpiryTime;
                    Session["tl_session_appState"] = "TokenReceived";

                    accessTokenResponseStream.Close();
                    return true;
                }
                else
                {
                    this.DrawPanelForFailure(tlPanel, "Auth server returned null access token");
                }
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    StreamReader streamReader = new StreamReader(stream);
                    this.DrawPanelForFailure(tlPanel, streamReader.ReadToEnd());
                    streamReader.Close();
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(tlPanel, ex.Message);
        }
        finally
        {
            if (null != postStream)
            {
                postStream.Close();
            }
        }

        return false;
    }

    /// <summary>
    /// Read parameters from configuraton file
    /// </summary>
    /// <returns>true/false; true if all required parameters are specified, else false</returns>
    private bool ReadConfigFile()
    {
        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(tlPanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.apiKey = ConfigurationManager.AppSettings["api_key"];
        if (string.IsNullOrEmpty(this.apiKey))
        {
            this.DrawPanelForFailure(tlPanel, "api_key is not defined in configuration file");
            return false; 
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(tlPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.authorizeRedirectUri = ConfigurationManager.AppSettings["authorize_redirect_uri"];
        if (string.IsNullOrEmpty(this.authorizeRedirectUri))
        {
            this.DrawPanelForFailure(tlPanel, "authorize_redirect_uri is not defined in configuration file");
            return false;
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "TL";
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

        return true;
    }

    #endregion

    #region Display Methods

    /// <summary>
    /// Displays error message
    /// </summary>
    /// <param name="panelParam">Panel to draw error message</param>
    /// <param name="message">Message to display</param>
    private void DrawPanelForFailure(Panel panelParam, string message)
    {
        Table table = new Table();
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        table.BorderStyle = BorderStyle.Outset;
        table.CssClass = "errorWide";
        table.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message;
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.Red;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(table);
    }

    /// <summary>
    /// This method is used to draw table for successful response of get device locations
    /// </summary>
    /// <param name="attribute">string, attribute to be displayed</param>
    /// <param name="value">string, value to be displayed</param>
    /// <param name="headerFlag">boolean, flag indicating to draw header panel</param>
    private void DrawPanelForGetLocationResult(string attribute, string value, bool headerFlag)
    {
        if (headerFlag == true)
        {
            this.getStatusTable = new Table();
            this.getStatusTable.CssClass = "successWide";
            TableRow rowOne = new TableRow();
            TableCell rowOneCellOne = new TableCell();
            rowOneCellOne.Font.Bold = true;
            rowOneCellOne.Text = "SUCCESS:";
            rowOne.Controls.Add(rowOneCellOne);
            this.getStatusTable.Controls.Add(rowOne);
            tlPanel.Controls.Add(this.getStatusTable);
        }
        else
        {
            TableRow row = new TableRow();
            TableCell cell1 = new TableCell();
            TableCell cell2 = new TableCell();
            cell1.Text = attribute.ToString();
            cell1.Font.Bold = true;
            cell1.Width = Unit.Pixel(100);
            row.Controls.Add(cell1);
            cell2.Text = value.ToString();
            row.Controls.Add(cell2);
            this.getStatusTable.Controls.Add(row);
        }
    }

    #endregion
}

#region Data Structures

/// <summary>
/// Access Token Data Structure
/// </summary>
public class AccessTokenResponse
{
    /// <summary>
    /// Gets or sets Access Token ID
    /// </summary>
    public string access_token
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets Refresh Token ID
    /// </summary>
    public string refresh_token
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets Expires in milli seconds
    /// </summary>
    public string expires_in
    {
        get;
        set;
    }
}

/// <summary>
/// Terminal Location Response object
/// </summary>
public class TLResponse
{
    /// <summary>
    /// Gets or sets the value of accuracy - This is the target MSISDN that was used in the Device Location request
    /// </summary>
    public string accuracy { get; set; }

    /// <summary>
    /// Gets or sets the value of latitude - The current latitude of the device's geo-position.
    /// </summary>
    public string latitude { get; set; }

    /// <summary>
    /// Gets or sets the value of longitude - The current longitude of the device geo-position.
    /// </summary>
    public string longitude { get; set; }

    /// <summary>
    /// Gets or sets the value of timestamp - Timestamp of the location data.
    /// </summary>
    public string timestamp { get; set; }
}
#endregion