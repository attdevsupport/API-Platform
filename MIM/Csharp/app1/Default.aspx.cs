// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
/// This application allows the AT&T subscriber access to message related data 
/// stored in the AT&amp;T Messages environment.
/// </summary>
public partial class MIM_App1 : System.Web.UI.Page
{
    #region Application Instance Variables

    /// <summary>
    /// API Address
    /// </summary>
    private string endPoint;
    
    /// <summary>
    /// Access token variables - temporary
    /// </summary>
    private string apiKey, authCode, authorizeRedirectUri, secretKey, accessToken, scope, refreshToken, 
        refreshTokenExpiryTime, accessTokenExpiryTime;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    #endregion

    #region Application Events

    /// <summary>
    /// This function is called when the application page is loaded into the browser.
        /// </summary>
    /// <param name="sender">object, that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            pnlHeader.Visible = false;
            imagePanel.Visible = false;
            smilpanel.Visible = false;
            if (!Page.IsPostBack)
            {
                this.ReadConfigFile();
            }

            this.BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            if (!Page.IsPostBack)
            {
                if (Session["mim_session_appState"] != null && Session["mim_session_appState"].ToString().Equals("GetToken") 
                    && Request["Code"] != null)
                {
                    this.authCode = Request["Code"].ToString();
                    if (this.GetAccessToken(AccessTokenType.Authorization_Code) == true)
                    {
                        bool isUserSpecifiedValues = this.GetSessionValues();
                        if (isUserSpecifiedValues == true)
                        {
                            if (Session["Request"] != null && Session["Request"].ToString().Equals("GetMessageHeaders"))
                            {
                                this.GetMessageHeaders();
                            }
                            else if (Session["Request"] != null && Session["Request"].ToString().Equals("GetMessageContent"))
                            {
                                this.GetMessageContentByIDnPartNumber();
                            }
                        }
                    }
                    else
                    {
                        this.DrawPanelForFailure(statusPanel, "Failed to get Access token");
                        this.ResetTokenSessionVariables();
                        this.ResetTokenVariables();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
        }
    }

    /// <summary>
    /// Event, that gets called when user clicks on Get message headers button, 
    /// performs validations and initiates api call to get header messages.
    /// </summary>
    /// <param name="sender">object that initiated this method</param>
    /// <param name="e">Event Agruments</param>
    protected void GetHeaderButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtHeaderCount.Text.Trim()))
        {
            this.DrawPanelForFailure(statusPanel, "Specify number of messages to be retrieved");
            return;
        }

        Regex regex = new Regex(@"\d+");
        if (!regex.IsMatch(txtHeaderCount.Text.Trim()))
        {
            this.DrawPanelForFailure(statusPanel, "Specify valid header count");
            return;
        }

        txtHeaderCount.Text = txtHeaderCount.Text.Trim();
        Session["HeaderCount"] = txtHeaderCount.Text;
        Session["IndexCursor"] = txtIndexCursor.Text;

        int headerCount = Convert.ToInt32(txtHeaderCount.Text.Trim());
        if (headerCount < 1 || headerCount > 500)
        {
            this.DrawPanelForFailure(statusPanel, "Header Count must be a number between 1-500");
            return;
        }

        // Read from config file and initialize variables
        if (this.ReadConfigFile() == false)
        {
            return;
        }

        Session["Request"] = "GetMessageHeaders";

        // Is valid address
        bool isValid = false;

        isValid = this.ReadAndGetAccessToken();

        if (isValid == true)
        {
            pnlHeader.Visible = false;
            this.GetMessageHeaders();
        }
    }

    /// <summary>
    /// Event, that gets called when user clicks on Get message content button, 
    /// performs validations and initiates api call to get message content
    /// </summary>
    /// <param name="sender">object that initiated this method</param>
    /// <param name="e">Event Agruments</param>    
    protected void GetMessageContent_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtMessageId.Text))
        {
            this.DrawPanelForFailure(ContentPanelStatus, "Specify Message ID");
            return;
        }

        if (string.IsNullOrEmpty(txtPartNumber.Text))
        {
            this.DrawPanelForFailure(ContentPanelStatus, "Specify Part Number of the message");
            return;
        }

        Session["MessageID"] = txtMessageId.Text;
        Session["PartNumber"] = txtPartNumber.Text;

        // Read from config file and initialize variables
        if (this.ReadConfigFile() == false)
        {
            return;
        }

        Session["Request"] = "GetMessageContent";

        // Is valid address
        bool isValid = false;

        isValid = this.ReadAndGetAccessToken();

        if (isValid == true)
        {
            this.GetMessageContentByIDnPartNumber();
        }
    }

    #endregion

    #region Application Specific Methods
    
    #region Display status Functions

    /// <summary>
    /// Displays success message
    /// </summary>
    /// <param name="panelParam">Panel to draw success message</param>
    /// <param name="message">Message to display</param>
    private void DrawPanelForSuccess(Panel panelParam, string message)
    {
        if (panelParam.HasControls())
        {
            panelParam.Controls.Clear();
        }

        Table table = new Table();
        table.CssClass = "successWide";
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOne.Controls.Add(rowOneCellOne);
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message;
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        panelParam.Controls.Add(table);
    }

    /// <summary>
    /// Displays error message
    /// </summary>
    /// <param name="panelParam">Panel to draw success message</param>
    /// <param name="message">Message to display</param>
    private void DrawPanelForFailure(Panel panelParam, string message)
    {
        if (panelParam.HasControls())
        {
            panelParam.Controls.Clear();
        }

        Table table = new Table();
        table.CssClass = "errorWide";
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
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
        panelParam.Controls.Add(table);
    }

    /// <summary>
    /// Displays the deserialized output to a grid
    /// </summary>
    /// <param name="messageHeaders">Deserialized message header list</param>
    private void DisplayGrid(MessageHeaderList messageHeaders)
    {
        try
        {
            DataTable headerTable = this.GetHeaderDataTable();

           if (null != messageHeaders && null != messageHeaders.Headers)
           {
               pnlHeader.Visible = true;
               lblHeaderCount.Text = messageHeaders.HeaderCount.ToString();
               lblIndexCursor.Text = messageHeaders.IndexCursor;

               DataRow row;
               foreach (Header header in messageHeaders.Headers)
               {
                   row = headerTable.NewRow();

                   row["MessageId"] = header.MessageId;
                   row["From"] = header.From;
                   row["To"] = header.To != null ? string.Join(",", header.To.ToArray()) : string.Empty;
                   row["Received"] = header.Received;
                   row["Text"] = header.Text;
                   row["Favourite"] = header.Favorite;
                   row["Read"] = header.Read;
                   row["Direction"] = header.Direction;
                   row["Type"] = header.Type;
                   headerTable.Rows.Add(row);
                   if (null != header.Type && header.Type.ToLower() == "mms")
                   {
                       foreach (MMSContent mmsCont in header.MmsContent)
                       {
                           DataRow mmsDetailsRow = headerTable.NewRow();
                           mmsDetailsRow["PartNumber"] = mmsCont.PartNumber;
                           mmsDetailsRow["ContentType"] = mmsCont.ContentType;
                           mmsDetailsRow["ContentName"] = mmsCont.ContentName;
                           headerTable.Rows.Add(mmsDetailsRow);
                       }
                   }
               }

               gvMessageHeaders.DataSource = headerTable;
               gvMessageHeaders.DataBind();
           }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
        }
    }

    /// <summary>
    /// Creates a datatable with message header columns
    /// </summary>
    /// <returns>data table with the structure of the grid</returns>
    private DataTable GetHeaderDataTable()
    {
        DataTable messageTable = new DataTable();
        DataColumn column = new DataColumn("MessageId");
        messageTable.Columns.Add(column);

        column = new DataColumn("PartNumber");
        messageTable.Columns.Add(column);

        column = new DataColumn("ContentType");
        messageTable.Columns.Add(column);

        column = new DataColumn("ContentName");
        messageTable.Columns.Add(column);

        column = new DataColumn("From");
        messageTable.Columns.Add(column);

        column = new DataColumn("To");
        messageTable.Columns.Add(column);

        column = new DataColumn("Received");
        messageTable.Columns.Add(column);

        column = new DataColumn("Text");
        messageTable.Columns.Add(column);

        column = new DataColumn("Favourite");
        messageTable.Columns.Add(column);

        column = new DataColumn("Read");
        messageTable.Columns.Add(column);

        column = new DataColumn("Type");
        messageTable.Columns.Add(column);

        column = new DataColumn("Direction");
        messageTable.Columns.Add(column);

        return messageTable;
    }

    #endregion

    #region Access Token functions

    /// <summary>
    /// Read parameters from configuraton file
    /// </summary>
    /// <returns>true/false; true if all required parameters are specified, else false</returns>
    private bool ReadConfigFile()
    {
        this.endPoint = ConfigurationManager.AppSettings["endPoint"];
        if (string.IsNullOrEmpty(this.endPoint))
        {
            this.DrawPanelForFailure(statusPanel, "endPoint is not defined in configuration file");
            return false;
        }

        this.apiKey = ConfigurationManager.AppSettings["api_key"];
        if (string.IsNullOrEmpty(this.apiKey))
        {
            this.DrawPanelForFailure(statusPanel, "api_key is not defined in configuration file");
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"];
        if (string.IsNullOrEmpty(this.secretKey))
        {
            this.DrawPanelForFailure(statusPanel, "secret_key is not defined in configuration file");
            return false;
        }

        this.authorizeRedirectUri = ConfigurationManager.AppSettings["authorize_redirect_uri"];
        if (string.IsNullOrEmpty(this.authorizeRedirectUri))
        {
            this.DrawPanelForFailure(statusPanel, "authorize_redirect_uri is not defined in configuration file");
            return false;
        }

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "MIM";
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

    /// <summary>
    /// Get session values, user supplied and assign to controls.
    /// </summary>
    /// <returns>true/false; true if values supplied, else false</returns>
    private bool GetSessionValues()
    {
        bool isValuesPresent = false;

        if (null != Session["HeaderCount"])
        {
            txtHeaderCount.Text = Session["HeaderCount"].ToString();
            isValuesPresent = true;
        }

        if (null != Session["IndexCursor"])
        {
            txtIndexCursor.Text = Session["IndexCursor"].ToString();
            isValuesPresent = true;
        }

        if (null != Session["MessageID"])
        {
            txtMessageId.Text = Session["MessageID"].ToString();
            isValuesPresent = true;
        }

        if (null != Session["PartNumber"])
        {
            txtPartNumber.Text = Session["PartNumber"].ToString();
            isValuesPresent = true;
        }

        return isValuesPresent;
    }

    /// <summary>
    /// This function resets access token related session variable to null 
    /// </summary>
    private void ResetTokenSessionVariables()
    {
        Session["mim_session_access_token"] = null;
        Session["mim_session_accessTokenExpiryTime"] = null;
        Session["mim_session_refresh_token"] = null;
        Session["mim_session_refreshTokenExpiryTime"] = null;
    }

    /// <summary>
    /// This function resets access token related  variable to null 
    /// </summary>
    private void ResetTokenVariables()
    {
        this.accessToken = null;
        this.refreshToken = null;
        this.refreshTokenExpiryTime = null;
        this.accessTokenExpiryTime = null;
    }

    /// <summary>
    /// Reads access token information from session and validates the token and 
    /// based on validity, the method will redirect to auth server/return the access token
    /// </summary>
    /// <returns>true/false; true if success in getting access token, else false
    /// </returns>
    private bool ReadAndGetAccessToken()
    {
        bool ableToReadAndGetToken = true;

        this.ReadTokenSessionVariables();

        string tokentResult = this.IsTokenValid();

        if (tokentResult.CompareTo("INVALID_ACCESS_TOKEN") == 0)
        {
            Session["mim_session_appState"] = "GetToken";
            this.GetAuthCode();
        }
        else if (tokentResult.CompareTo("REFRESH_TOKEN") == 0)
        {
            if (this.GetAccessToken(AccessTokenType.Refresh_Token) == false)
            {
                this.DrawPanelForFailure(statusPanel, "Failed to get Access token");
                this.ResetTokenSessionVariables();
                this.ResetTokenVariables();
                ableToReadAndGetToken = false;
            }
        }

        if (string.IsNullOrEmpty(this.accessToken))
        {
            this.DrawPanelForFailure(statusPanel, "Failed to get Access token");
            this.ResetTokenSessionVariables();
            this.ResetTokenVariables();
            ableToReadAndGetToken = false;
        }

        return ableToReadAndGetToken;
    }

    /// <summary>
    /// Redirect to OAuth and get Authorization Code
    /// </summary>
    private void GetAuthCode()
    {
        try
        {
            Response.Redirect(string.Empty + this.endPoint + "/oauth/authorize?scope=" + this.scope + "&client_id=" + this.apiKey + "&redirect_url=" + this.authorizeRedirectUri);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
        }
    }

    /// <summary>
    /// Reads access token related session variables to local variables
    /// </summary>
    /// <returns>true/false depending on the session variables</returns>
    private bool ReadTokenSessionVariables()
    {
        if (Session["mim_session_access_token"] != null)
        {
            this.accessToken = Session["mim_session_access_token"].ToString();
        }
        else
        {
            this.accessToken = null;
        }

        if (Session["mim_session_accessTokenExpiryTime"] != null)
        {
            this.accessTokenExpiryTime = Session["mim_session_accessTokenExpiryTime"].ToString();
        }
        else
        {
            this.accessTokenExpiryTime = null;
        }

        if (Session["mim_session_refresh_token"] != null)
        {
            this.refreshToken = Session["mim_session_refresh_token"].ToString();
        }
        else
        {
            this.refreshToken = null;
        }

        if (Session["mim_session_refreshTokenExpiryTime"] != null)
        {
            this.refreshTokenExpiryTime = Session["mim_session_refreshTokenExpiryTime"].ToString();
        }
        else
        {
            this.refreshTokenExpiryTime = null;
        }

        if ((this.accessToken == null) || (this.accessTokenExpiryTime == null) || (this.refreshToken == null) || (this.refreshTokenExpiryTime == null))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates access token related variables
    /// </summary>
    /// <returns>string, returns VALID_ACCESS_TOKEN if its valid
    /// otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    /// return REFRESH_TOKEN, if access token in expired and refresh token is valid</returns>
    private string IsTokenValid()
    {
        if (Session["mim_session_access_token"] == null)
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
    /// Neglect the ssl handshake error with authentication server
    /// </summary>
    private void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    /// <summary>
    /// Get access token based on the type parameter type values.
    /// </summary>
    /// <param name="type">If type value is Authorization_code, access token is fetch for authorization code flow
    /// If type value isRefresh_Token, access token is fetch for authorization code floww based on the exisiting refresh token</param>
    /// <returns>true/false; true if success, else false</returns>
    private bool GetAccessToken(AccessTokenType type)
    {
        bool result = false;

        Stream postStream = null;
        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token");
            accessTokenRequest.Method = "POST";

            string oauthParameters = string.Empty;
            if (type == AccessTokenType.Authorization_Code)
            {
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&code=" + this.authCode + "&grant_type=authorization_code&scope=" + this.scope;
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

                    DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                    Session["mim_session_accessTokenExpiryTime"] = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in));

                    if (deserializedJsonObj.expires_in.Equals("0"))
                    {
                        int defaultAccessTokenExpiresIn = 100; // In Years
                        Session["mim_session_accessTokenExpiryTime"] = currentServerTime.AddYears(defaultAccessTokenExpiresIn);
                    }

                    this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    Session["mim_session_access_token"] = this.accessToken;
                    this.accessTokenExpiryTime = Session["mim_session_accessTokenExpiryTime"].ToString();
                    Session["mim_session_refresh_token"] = this.refreshToken;
                    Session["mim_session_refreshTokenExpiryTime"] = this.refreshTokenExpiryTime;
                    Session["mim_session_appState"] = "TokenReceived";
                    accessTokenResponseStream.Close();
                    result = true;
                }
                else
                {
                    this.DrawPanelForFailure(statusPanel, "Auth server returned null access token");
                }
            }
        }
        catch (WebException we)
        {
            string errorResponse = string.Empty;
            try 
            {
                using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                {
                    errorResponse = sr2.ReadToEnd();
                    sr2.Close();
                }
            }
            catch 
            {
                errorResponse = "Unable to get access token";
            }

            this.DrawPanelForFailure(statusPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
        }
        finally
        {
            if (null != postStream)
            {
                postStream.Close();
            }
        }

        return result;
    }

    #endregion

    #region Get Message Headers Functions

    /// <summary>
    /// Retreives the message headers based on headerCount and inderCursor
    /// </summary>
    private void GetMessageHeaders()
    {
        try
        {
            HttpWebRequest mimRequestObject1;
            
            mimRequestObject1 = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/1/MyMessages?HeaderCount=" + txtHeaderCount.Text);
            if (!string.IsNullOrEmpty(txtIndexCursor.Text))
            {
                mimRequestObject1 = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/1/MyMessages?HeaderCount=" + txtHeaderCount.Text + "&IndexCursor=" + txtIndexCursor.Text);
            }

            mimRequestObject1.Headers.Add("Authorization", "Bearer " + this.accessToken);
            mimRequestObject1.Method = "GET";
            mimRequestObject1.KeepAlive = true;

            WebResponse mimResponseObject1 = mimRequestObject1.GetResponse();
            using (StreamReader sr = new StreamReader(mimResponseObject1.GetResponseStream()))
            {
                string mimResponseData = sr.ReadToEnd();

                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                MIMResponse deserializedJsonObj = (MIMResponse)deserializeJsonObject.Deserialize(mimResponseData, typeof(MIMResponse));

                if (null != deserializedJsonObj)
                {
                    this.DrawPanelForSuccess(statusPanel, string.Empty);
                    this.DisplayGrid(deserializedJsonObj.MessageHeadersList);
                }
                else
                {
                    this.DrawPanelForFailure(statusPanel, "No response from server");
                }

                sr.Close();
            }
        }
        catch (WebException we)
        {
            string errorResponse = string.Empty;

            try
            {
                using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                {
                    errorResponse = sr2.ReadToEnd();
                    sr2.Close();
                }
            }
            catch
            {
                errorResponse = "Unable to get response";
            }

            this.DrawPanelForFailure(statusPanel, errorResponse + Environment.NewLine + we.Message);
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
            return;
        }        
    }

    /// <summary>
    /// Gets the message content for MMS messages based on Message ID and Part Number
    /// </summary>
    private void GetMessageContentByIDnPartNumber()
    {
        try
        {
            HttpWebRequest mimRequestObject1 = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/1/MyMessages/" + txtMessageId.Text + "/" + txtPartNumber.Text);
            mimRequestObject1.Headers.Add("Authorization", "Bearer " + this.accessToken);
            mimRequestObject1.Method = "GET";
            mimRequestObject1.KeepAlive = true;
            int offset = 0;
            WebResponse mimResponseObject1 = mimRequestObject1.GetResponse();
            int remaining = Convert.ToInt32(mimResponseObject1.ContentLength);
            using (var stream = mimResponseObject1.GetResponseStream())
            {
                var bytes = new byte[mimResponseObject1.ContentLength];
                while (remaining > 0)
                {
                    int read = stream.Read(bytes, offset, remaining);
                    if (read <= 0)
                    {
                        this.DrawPanelForFailure(ContentPanelStatus, String.Format("End of stream reached with {0} bytes left to read", remaining));
                        return;
                    }

                    remaining -= read;
                    offset += read;
                }

                string[] splitData = Regex.Split(mimResponseObject1.ContentType.ToLower(), ";");
                string[] ext = Regex.Split(splitData[0], "/");

                if (mimResponseObject1.ContentType.ToLower().Contains("application/smil"))
                {
                    smilpanel.Visible = true;
                    TextBox1.Text = System.Text.Encoding.Default.GetString(bytes);
                    this.DrawPanelForSuccess(ContentPanelStatus, string.Empty);
                }
                else if (mimResponseObject1.ContentType.ToLower().Contains("text/plain"))
                {
                    this.DrawPanelForSuccess(ContentPanelStatus, System.Text.Encoding.Default.GetString(bytes));
                }
                else
                {
                    imagePanel.Visible = true;
                    this.DrawPanelForSuccess(ContentPanelStatus, string.Empty);
                    imagetoshow.Src = "data:" + splitData[0] + ";base64," + Convert.ToBase64String(bytes, Base64FormattingOptions.None);
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(ContentPanelStatus, ex.Message);
        }
    }

    #endregion

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
/// Response returned from MyMessages api
/// </summary>
public class MIMResponse
{
    /// <summary>
    /// Gets or sets the value of message header list.
    /// </summary>
    public MessageHeaderList MessageHeadersList 
    { 
        get; 
        set; 
    }
}

/// <summary>
/// Message Header List
/// </summary>
public class MessageHeaderList
{
    /// <summary>
    /// Gets or sets the value of object containing a List of Messages Headers
    /// </summary>
    public List<Header> Headers 
    { 
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of a number representing the number of headers returned for this request.
    /// </summary>
    public int HeaderCount 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets the value of a string which defines the start of the next block of messages for the current request.
    /// A value of zero (0) indicates the end of the block.
    /// </summary>
    public string IndexCursor 
    { 
        get; 
        set; 
    }
}   

/// <summary>
/// Object containing a List of Messages Headers
/// </summary>
public class Header
{
    /// <summary>
    /// Gets or sets the value of Unique message identifier
    /// </summary>
    public string MessageId 
    { 
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of message sender
    /// </summary>
    public string From 
    { 
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of the addresses, whom the message need to be delivered. 
    /// If Group Message, this will contain multiple Addresses.
    /// </summary>
    public List<string> To 
    { 
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value of message text
    /// </summary>
    public string Text 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets a value of message part descriptions
    /// </summary>
    public List<MMSContent> MmsContent 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets the value of date/time message received
    /// </summary>
    public DateTime Received 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether its a favourite or not
    /// </summary>
    public bool Favorite 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether message is read or not
    /// </summary>
    public bool Read 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets the value of type of message, TEXT or MMS
    /// </summary>
    public string Type 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets the value of indicator, which indicates if message is Incoming or Outgoing “IN” or “OUT”
    /// </summary>
    public string Direction 
    { 
        get; 
        set;
    }
}

/// <summary>
/// Message part descriptions
/// </summary>
public class MMSContent
{
    /// <summary>
    /// Gets or sets the value of content name
    /// </summary>
    public string ContentName 
    { 
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of content type
    /// </summary>
    public string ContentType 
    { 
        get; 
        set;
    }

    /// <summary>
    /// Gets or sets the value of part number
    /// </summary>
    public string PartNumber 
    { 
        get; 
        set;
    }
}
    
#endregion