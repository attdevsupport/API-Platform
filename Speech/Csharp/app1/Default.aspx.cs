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
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;

#endregion

/// <summary>
/// Speech application
/// </summary>
public partial class Speech_App1 : System.Web.UI.Page
{
    #region Class variables and Data structures
    /// <summary>
    /// Temporary variables for processing
    /// </summary>
    private string fqdn, accessTokenFilePath;

    /// <summary>
    /// Temporary variables for processing
    /// </summary>
    private string apiKey, secretKey, accessToken, scope,  refreshToken, refreshTokenExpiryTime, accessTokenExpiryTime;

    /// <summary>
    /// variable for having the posted file.
    /// </summary>
    private string fileToConvert;

    /// <summary>
    /// Flag for deletion of the temporary file
    /// </summary>
    private bool deleteFile;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    /// <summary>
    /// Access Token Types
    /// </summary>
    public enum AccessType
    {
        /// <summary>
        /// Access Token Type is based on Client Credential Mode
        /// </summary>
        ClientCredential,

        /// <summary>
        /// Access Token Type is based on Refresh Token
        /// </summary>
        RefreshToken
    }
    #endregion

    #region Events

    /// <summary>
    /// This function is called when the applicaiton page is loaded into the browser.
    /// This function reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">Button that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        this.BypassCertificateError();
        if (!Page.IsPostBack)
        {
            resultsPanel.Visible = false;
        }

        DateTime currentServerTime = DateTime.UtcNow;
        lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";

        this.ReadConfigFile();

        this.deleteFile = false;
    }

    /// <summary>
    /// Method that calls SpeechToText api when user clicked on submit button
    /// </summary>
    /// <param name="sender">sender that invoked this event</param>
    /// <param name="e">eventargs of the button</param>
    protected void BtnSubmit_Click(object sender, EventArgs e)
    {
        try
        {
            resultsPanel.Visible = false;

            if (string.IsNullOrEmpty(fileUpload1.FileName))
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["DefaultFile"]))
                    this.fileToConvert = Request.MapPath(ConfigurationManager.AppSettings["DefaultFile"]);
                else
                {
                    this.DrawPanelForFailure(statusPanel, "No file selected, and default file is not defined in web.config");
                    return;
                }
            }
            else
            {
                string fileName = fileUpload1.FileName;
                if (fileName.CompareTo("default.wav") == 0)
                {
                    fileName = "1" + fileUpload1.FileName;
                }
                fileUpload1.PostedFile.SaveAs(Request.MapPath("") + "/" + fileName);
                this.fileToConvert = Request.MapPath("").ToString() + "/" + fileName;
                this.deleteFile = true;
            }

            bool IsValid = this.IsValidFile(this.fileToConvert);

            if (IsValid == false)
            {
                return;
            }

            IsValid = this.ReadAndGetAccessToken();
            if (IsValid == false)
            {                
                this.DrawPanelForFailure(statusPanel, "Unable to get access token");
                return;
            }

            this.ConvertToSpeech();
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
            return;
        }
    }
    
    #endregion

    #region Access Token Related Functions

    /// <summary>
    /// Read parameters from configuraton file
    /// </summary>
    /// <returns>true/false; true if all required parameters are specified, else false</returns>
    private bool ReadConfigFile()
    {
        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "~\\SpeechApp1AccessToken.txt";
        }

        this.fqdn = ConfigurationManager.AppSettings["FQDN"];
        if (string.IsNullOrEmpty(this.fqdn))
        {
            this.DrawPanelForFailure(statusPanel, "FQDN is not defined in configuration file");
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

        this.scope = ConfigurationManager.AppSettings["scope"];
        if (string.IsNullOrEmpty(this.scope))
        {
            this.scope = "SPEECH";
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
    /// This function reads the Access Token File and stores the values of access token, expiry seconds
    /// refresh token, last access token time and refresh token expiry time
    /// </summary>
    /// <returns>
    /// This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    /// </returns>
    private bool ReadAccessTokenFile()
    {
        FileStream fileStream = null;
        StreamReader streamReader = null;
        try
        {
            fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            streamReader = new StreamReader(fileStream);
            this.accessToken = streamReader.ReadLine();
            this.accessTokenExpiryTime = streamReader.ReadLine();
            this.refreshToken = streamReader.ReadLine();
            this.refreshTokenExpiryTime = streamReader.ReadLine();
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
            return false;
        }
        finally
        {
            if (null != streamReader)
            {
                streamReader.Close();
            }

            if (null != fileStream)
            {
                fileStream.Close();
            }
        }

        if ((this.accessToken == null) || (this.accessTokenExpiryTime == null) || (this.refreshToken == null) || (this.refreshTokenExpiryTime == null))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This function validates the expiry of the access token and refresh token.
    /// function compares the current time with the refresh token taken time, if current time is greater then returns INVALID_REFRESH_TOKEN
    /// function compares the difference of last access token taken time and the current time with the expiry seconds, if its more, returns INVALID_ACCESS_TOKEN    
    /// otherwise returns VALID_ACCESS_TOKEN
    /// </summary>
    /// <returns>string, which specifies the token validity</returns>
    private string IsTokenValid()
    {
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
    /// Get the access token based on Access Type
    /// </summary>
    /// <param name="type">Access Type - either client Credential or Refresh Token</param>
    /// <returns>true/false; true - if success on getting access token, else false</returns>
    private bool GetAccessToken(AccessType type)
    {
        FileStream fileStream = null;
        Stream postStream = null;
        StreamWriter streamWriter = null;


        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

            WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.fqdn + "/oauth/token");
            accessTokenRequest.Method = "POST";

            string oauthParameters = string.Empty;
            if (type == AccessType.ClientCredential)
            {
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=" + this.scope;
            }
            else
            {
                oauthParameters = "grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken;
            }
            accessTokenRequest.ContentType = "application/x-www-form-urlencoded";

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postBytes = encoding.GetBytes(oauthParameters);
            accessTokenRequest.ContentLength = postBytes.Length;

            postStream = accessTokenRequest.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd();
                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();

                AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));
                this.accessToken = deserializedJsonObj.access_token;
                this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString();
                this.refreshToken = deserializedJsonObj.refresh_token;

                DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                if (deserializedJsonObj.expires_in.Equals("0"))
                {
                    int defaultAccessTokenExpiresIn = 100; // In Years
                    this.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongDateString() + " " + currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongTimeString();                    
                }

                this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();

                fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(this.accessToken);
                streamWriter.WriteLine(this.accessTokenExpiryTime);
                streamWriter.WriteLine(this.refreshToken);                
                streamWriter.WriteLine(this.refreshTokenExpiryTime);

                // Close and clean up the StreamReader
                accessTokenResponseStream.Close();
                return true;
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

            this.DrawPanelForFailure(statusPanel, errorResponse + Environment.NewLine + we.ToString());
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

            if (null != streamWriter)
            {
                streamWriter.Close();
            }

            if (null != fileStream)
            {
                fileStream.Close();
            }
        }

        return false;
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
    /// Read access token file and validate the access token
    /// </summary>
    /// <returns>true/false; true if access token is valid, else false</returns>
    private bool ReadAndGetAccessToken()
    {
        bool result = true;

        if (this.ReadAccessTokenFile() == false)
        {
            result = this.GetAccessToken(AccessType.ClientCredential);
        }
        else
        {
            string tokenValidity = this.IsTokenValid();
            if (tokenValidity == "REFRESH_TOKEN")
            {
                result = this.GetAccessToken(AccessType.RefreshToken);
            }
            else if (string.Compare(tokenValidity, "INVALID_ACCESS_TOKEN") == 0)
            {
                result = this.GetAccessToken(AccessType.ClientCredential);
            }
        }

        if (string.IsNullOrEmpty(this.accessToken))
        {
            result = false;
        }

        return result;
    }

    #endregion

    #region Display status Functions

    /// <summary>
    /// Display success message
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
        TableCell rowTwoCellTwo = new TableCell();
        rowTwoCellTwo.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellTwo);
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

    #endregion

    #region Speech Service Functions

    /// <summary>
    /// Verifies whether the given file satisfies the criteria for speech api
    /// </summary>
    /// <param name="file">Name of the sound file</param>
    /// <returns>true/false; true if valid file, else false</returns>
    private bool IsValidFile(string file)
    {
        bool isValid = false;

        // Verify File Extension
        string extension = System.IO.Path.GetExtension(file);

        if (!string.IsNullOrEmpty(extension) && (extension.Equals(".wav") || extension.Equals(".amr")))
        {
            isValid = true;
        }
        else
        {
            this.DrawPanelForFailure(statusPanel, "Invalid file specified. Valid file formats are .wav and .amr");
        }

        return isValid;
    }

    /// <summary>
    /// Content type based on the file extension.
    /// </summary>
    /// <param name="extension">file extension</param>
    /// <returns>the Content type mapped to the extension"/> summed memory stream</returns>
    private string MapContentTypeFromExtension(string extension)
    {
        Dictionary<string, string> extensionToContentTypeMapping = new Dictionary<string, string>()
            {
                { ".jpg", "image/jpeg" }, { ".bmp", "image/bmp" }, { ".mp3", "audio/mp3" },
                { ".m4a", "audio/m4a" }, { ".gif", "image/gif" }, { ".3gp", "video/3gpp" },
                { ".3g2", "video/3gpp2" }, { ".wmv", "video/x-ms-wmv" }, { ".m4v", "video/x-m4v" },
                { ".amr", "audio/amr" }, { ".mp4", "video/mp4" }, { ".avi", "video/x-msvideo" },
                { ".mov", "video/quicktime" }, { ".mpeg", "video/mpeg" }, { ".wav", "audio/wav" },
                { ".aiff", "audio/x-aiff" }, { ".aifc", "audio/x-aifc" }, { ".midi", ".midi" },
                { ".au", "audio/basic" }, { ".xwd", "image/x-xwindowdump" }, { ".png", "image/png" },
                { ".tiff", "image/tiff" }, { ".ief", "image/ief" }, { ".txt", "text/plain" },
                { ".html", "text/html" }, { ".vcf", "text/x-vcard" }, { ".vcs", "text/x-vcalendar" },
                { ".mid", "application/x-midi" }, { ".imy", "audio/iMelody" }
            };
        if (extensionToContentTypeMapping.ContainsKey(extension))
        {
            return extensionToContentTypeMapping[extension];
        }
        else
        {
            throw new ArgumentException("invalid attachment extension");
        }
    }

    /// <summary>
    /// This function invokes api SpeechToText to convert the given wav amr file and displays the result.
    /// </summary>
    private void ConvertToSpeech()
    {
        Stream postStream = null;
        FileStream audioFileStream = null;
        try
        {
            string mmsFilePath = this.fileToConvert;
            audioFileStream = new FileStream(mmsFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(audioFileStream);
            byte[] binaryData = reader.ReadBytes((int)audioFileStream.Length);
            reader.Close();
            audioFileStream.Close();
            if (null != binaryData)
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(string.Empty + this.fqdn + "/rest/1/SpeechToText");
                httpRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                httpRequest.Headers.Add("X-SpeechContext", "Generic");

                string contentType = this.MapContentTypeFromExtension(Path.GetExtension(mmsFilePath));
                httpRequest.ContentLength = binaryData.Length;
                httpRequest.ContentType = contentType;
                httpRequest.Accept = "application/json";
                httpRequest.Method = "POST";
                httpRequest.KeepAlive = true;

                postStream = httpRequest.GetRequestStream();
                postStream.Write(binaryData, 0, binaryData.Length);
                postStream.Close();

                HttpWebResponse speechResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (StreamReader streamReader = new StreamReader(speechResponse.GetResponseStream()))
                {
                    string speechResponseData = streamReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(speechResponseData))
                    {
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                        SpeechResponse deserializedJsonObj = (SpeechResponse)deserializeJsonObject.Deserialize(speechResponseData, typeof(SpeechResponse));
                        if (null != deserializedJsonObj)
                        {
                            resultsPanel.Visible = true;
                            this.DrawPanelForSuccess(statusPanel, "Response Parameters listed below");
                            this.DisplayResult(deserializedJsonObj);
                        }
                        else
                        {
                            this.DrawPanelForFailure(statusPanel, "Empty speech to text response");
                        }
                    }
                    else
                    {
                        this.DrawPanelForFailure(statusPanel, "Empty speech to text response");
                    }

                    streamReader.Close();
                }
            }
            else
            {
                this.DrawPanelForFailure(statusPanel, "Empty speech to text response");
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

            this.DrawPanelForFailure(statusPanel, errorResponse + Environment.NewLine + we.ToString());
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.ToString());
        }
        finally
        {
            if ((this.deleteFile == true) && (File.Exists(this.fileToConvert)))
            {
                File.Delete(this.fileToConvert);
                this.deleteFile = false;
            }
            if (null != postStream)
            {
                postStream.Close();
            }
        }
    }

    /// <summary>
    /// Displays the result onto the page
    /// </summary>
    /// <param name="speechResponse">SpeechResponse received from api</param>
    private void DisplayResult(SpeechResponse speechResponse)
    {
        lblResponseId.Text = speechResponse.Recognition.ResponseId;
        foreach (NBest nbest in speechResponse.Recognition.NBest)
        {
            lblHypothesis.Text = nbest.Hypothesis;
            lblLanguageId.Text = nbest.LanguageId;
            lblResultText.Text = nbest.ResultText;
            lblGrade.Text = nbest.Grade;
            lblConfidence.Text = nbest.Confidence.ToString();

            string strText = "[";
            foreach (string word in nbest.Words)
            {
                strText += "\"" + word + "\", ";
            }
            strText = strText.Substring(0, strText.LastIndexOf(","));
            strText = strText + "]";

            lblWords.Text = nbest.Words != null ? strText : string.Empty;

            lblWordScores.Text = "[" + string.Join(", ", nbest.WordScores.ToArray()) + "]";
        }
    }

    #endregion
}

#region Access Token and Speech Response Data Structures

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
/// Speech Response to an audio file
/// </summary>
public class SpeechResponse
{
    /// <summary>
    /// Gets or sets the Recognition value returned by api
    /// </summary>
    public Recognition Recognition { get; set; }
}

/// <summary>
/// Recognition returned by the server for Speech to text request.
/// </summary>
public class Recognition
{
    /// <summary>
    /// Gets or sets a unique string that identifies this particular transaction.
    /// </summary>
    public string ResponseId { get; set; }

    /// <summary>
    /// Gets or sets NBest Complex structure that holds the results of the transcription. Supports multiple transcriptions.
    /// </summary>
    public List<NBest> NBest { get; set; }
}

/// <summary>
/// Complex structure that holds the results of the transcription. Supports multiple transcriptions.
/// </summary>
public class NBest
{
    /// <summary>
    /// Gets or sets the transcription of the audio. 
    /// </summary>
    public string Hypothesis { get; set; }

    /// <summary>
    /// Gets or sets the language used to decode the Hypothesis. 
    /// Represented using the two-letter ISO 639 language code, hyphen, two-letter ISO 3166 country code in lower case, e.g. “en-us”.
    /// </summary>
    public string LanguageId { get; set; }

    /// <summary>
    /// Gets or sets the confidence value of the Hypothesis, a value between 0.0 and 1.0 inclusive.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets a machine-readable string indicating an assessment of utterance/result quality and the recommended treatment of the Hypothesis. 
    /// The assessment reflects a confidence region based on prior experience with similar results. 
    /// accept - the hypothesis value has acceptable confidence
    /// confirm - the hypothesis should be independently confirmed due to lower confidence
    /// reject - the hypothesis should be rejected due to low confidence
    /// </summary>
    public string Grade { get; set; }

    /// <summary>
    /// Gets or sets a text string prepared according to the output domain of the application package. 
    /// The string will generally be a formatted version of the hypothesis, but the words may have been altered through 
    /// insertions/deletions/substitutions to make the result more readable or usable for the client.  
    /// </summary>
    public string ResultText { get; set; }

    /// <summary>
    /// Gets or sets the words of the Hypothesis split into separate strings.  
    /// May omit some of the words of the Hypothesis string, and can be empty.  Never contains words not in hypothesis string.  
    /// </summary>
    public List<string> Words { get; set; }

    /// <summary>
    /// Gets or sets the confidence scores for each of the strings in the words array.  Each value ranges from 0.0 to 1.0 inclusive.
    /// </summary>
    public List<double> WordScores { get; set; }
}
#endregion