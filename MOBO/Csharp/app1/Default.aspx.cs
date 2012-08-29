// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
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
/// This application allows the user to send SMS and MMS on behalf of subscriber, 
/// with subscriber’s consent, using the MOBO API.
/// </summary>
public partial class Mobo_App1 : System.Web.UI.Page
{
    #region Instance variables

    /// <summary>
    /// API Address
    /// </summary>
    private string endPoint;

    /// <summary>
    /// Access token variables - temporary
    /// </summary>
    private string apiKey, authCode, authorizeRedirectUri, secretKey, accessToken, 
        scope, refreshToken, refreshTokenExpiryTime, accessTokenExpiryTime;

    /// <summary>
    /// Maximum number of addresses user can specify
    /// </summary>
    private int maxAddresses;
    
    /// <summary>
    /// List of addresses to send
    /// </summary>
    private List<string> addressList = new List<string>();

    /// <summary>
    /// Variable to hold phone number(s)/email address(es)/short code(s) parameter.
    /// </summary>
    private string phoneNumbersParameter = null;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    #endregion

    #region Application events

    /// <summary>
    /// This function is called when the applicaiton page is loaded into the browser.
    /// This function reads the web.config and gets the values of the attributes
    /// </summary>
    /// <param name="sender">Button that caused this event</param>
    /// <param name="e">Event that invoked this function</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            this.BypassCertificateError();
            DateTime currentServerTime = DateTime.UtcNow;
            lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";

            this.ReadConfigFile();

            if ((Session["mobo_session_appState"] == "GetToken") && (Request["Code"] != null))
            {
                this.authCode = Request["code"].ToString();                
                if (this.GetAccessToken(AccessTokenType.Authorization_Code) == true)
                {
                    if (null != Session["Address"])
                    {
                        txtPhone.Text = Session["Address"].ToString();
                    }

                    if (null != Session["Message"])
                    {
                        txtMessage.Text = Session["Message"].ToString();
                    }

                    if (null != Session["Subject"])
                    {
                        txtSubject.Text = Session["Subject"].ToString();
                    }

                    if (null != Session["Group"])
                    {
                        chkGroup.Checked = Convert.ToBoolean(Session["Group"].ToString());
                    }

                    this.IsValidAddress();
                    ArrayList attachmentsList = (ArrayList)Session["Attachments"];
                    this.SendMessage(attachmentsList);
                }
                else
                {
                    this.DrawPanelForFailure(statusPanel, "Failed to get Access token");
                    this.ResetTokenSessionVariables();
                    this.ResetTokenVariables();                    
                    return;
                }
            }            
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.Message);
        }
    }

    /// <summary>
    /// Event, that gets called when user clicks on send message button, performs validations and initiates api call to send message
    /// </summary>
    /// <param name="sender">object that initiated this method</param>
    /// <param name="e">Event Agruments</param>
    protected void BtnSendMessage_Click(object sender, EventArgs e)
    {
        // Perform validations

        // Read from config file and initialize variables
        if (this.ReadConfigFile() == false)
        {
            return;
        }

        // Is valid address
        bool isValid = false;
        isValid = this.IsValidAddress();
        if (isValid == false)
        {
            return;
        }

        // User provided any attachments?
        if (!string.IsNullOrEmpty(fileUpload1.FileName) || !string.IsNullOrEmpty(fileUpload2.FileName) || !string.IsNullOrEmpty(fileUpload3.FileName) || !string.IsNullOrEmpty(fileUpload4.FileName) || !string.IsNullOrEmpty(fileUpload5.FileName))
        {
            // Is valid file size
            isValid = this.IsValidFileSize();
            if (isValid == false)
            {
                return;
            }
        }
        else
        {
            // Message is mandatory, if no attachments
            if (string.IsNullOrEmpty(txtMessage.Text))
            {
                this.DrawPanelForFailure(statusPanel, "Specify message to be sent");
                return;
            }
        }

        Session["Address"] = txtPhone.Text;
        Session["Message"] = txtMessage.Text;
        Session["Subject"] = txtSubject.Text;
        Session["Group"] = chkGroup.Checked;

        this.ReadTokenSessionVariables();

        string tokentResult = this.IsTokenValid();

        if (tokentResult.CompareTo("INVALID_ACCESS_TOKEN") == 0)
        {
            Session["mobo_session_appState"] = "GetToken";
            this.GetAuthCode();
        }
        else if (tokentResult.CompareTo("REFRESH_TOKEN") == 0)
        {
            if (this.GetAccessToken(AccessTokenType.Refresh_Token) == false)
            {
                this.DrawPanelForFailure(statusPanel, "Failed to get Access token");
                this.ResetTokenSessionVariables();
                this.ResetTokenVariables();
                return;
            }
        }

        if (this.accessToken == null || this.accessToken.Length <= 0)
        {
            return;
        }

        // Initiate api call to send message
        ArrayList attachmentsList = (ArrayList)Session["Attachments"];
        this.SendMessage(attachmentsList);
    }

    #endregion

    #region Application methods

    #region Send Message Functions

    /// <summary>
    /// Gets the mapping of extension with predefined content types
    /// </summary>
    /// <param name="extension">file extension</param>
    /// <returns>string, content type</returns>
    private string GetContentTypeFromExtension(string extension)
    {
        Dictionary<string, string> extensionToContentType = new Dictionary<string, string>()
            {
                { ".jpg", "image/jpeg" }, { ".bmp", "image/bmp" }, { ".mp3", "audio/mp3" },
                { ".m4a", "audio/m4a" }, { ".gif", "image/gif" }, { ".3gp", "video/3gpp" },
                { ".3g2", "video/3gpp2" }, { ".wmv", "video/x-ms-wmv" }, { ".m4v", "video/x-m4v" },
                { ".amr", "audio/amr" }, { ".mp4", "video/mp4" }, { ".avi", "video/x-msvideo" },
                { ".mov", "video/quicktime" }, { ".mpeg", "video/mpeg" }, { ".wav", "audio/x-wav" },
                { ".aiff", "audio/x-aiff" }, { ".aifc", "audio/x-aifc" }, { ".midi", ".midi" },
                { ".au", "audio/basic" }, { ".xwd", "image/x-xwindowdump" }, { ".png", "image/png" },
                { ".tiff", "image/tiff" }, { ".ief", "image/ief" }, { ".txt", "text/plain" },
                { ".html", "text/html" }, { ".vcf", "text/x-vcard" }, { ".vcs", "text/x-vcalendar" },
                { ".mid", "application/x-midi" }, { ".imy", "audio/iMelody" }
            };
        if (extensionToContentType.ContainsKey(extension))
        {
            return extensionToContentType[extension];
        }
        else
        {
            return "Not Found";
        }
    }

    /// <summary>
    /// Sends message to the list of addresses provided.
    /// </summary>
    /// <param name="attachments">List of attachments</param>
    private void SendMessage(ArrayList attachments)
    {
        Stream postStream = null;

        try
        {
            string subject = txtSubject.Text;
            string boundaryToSend = "----------------------------" + DateTime.Now.Ticks.ToString("x");

            HttpWebRequest msgRequestObject = (HttpWebRequest)WebRequest.Create(string.Empty + this.endPoint + "/rest/1/MyMessages");
            msgRequestObject.Headers.Add("Authorization", "Bearer " + this.accessToken);
            msgRequestObject.Method = "POST";
            string contentType = "multipart/form-data; type=\"application/x-www-form-urlencoded\"; start=\"<startpart>\"; boundary=\"" + boundaryToSend + "\"\r\n";
            msgRequestObject.ContentType = contentType;
            string mmsParameters = this.phoneNumbersParameter + "Subject=" + Server.UrlEncode(subject) + "&Text=" + Server.UrlEncode(txtMessage.Text) + "&Group=" + chkGroup.Checked.ToString().ToLower();

            string dataToSend = string.Empty;
            dataToSend += "--" + boundaryToSend + "\r\n";
            dataToSend += "Content-Type: application/x-www-form-urlencoded; charset=UTF-8\r\nContent-Transfer-Encoding: 8bit\r\nContent-Disposition: form-data; name=\"root-fields\"\r\nContent-ID: <startpart>\r\n\r\n" + mmsParameters + "\r\n";

            UTF8Encoding encoding = new UTF8Encoding();
            if ((attachments == null) || (attachments.Count == 0))
            {
                if (!chkGroup.Checked)
                {
                    msgRequestObject.ContentType = "application/x-www-form-urlencoded";
                    byte[] postBytes = encoding.GetBytes(mmsParameters);
                    msgRequestObject.ContentLength = postBytes.Length;

                    postStream = msgRequestObject.GetRequestStream();
                    postStream.Write(postBytes, 0, postBytes.Length);
                    postStream.Close();

                    WebResponse mmsResponseObject1 = msgRequestObject.GetResponse();
                    using (StreamReader sr = new StreamReader(mmsResponseObject1.GetResponseStream()))
                    {
                        string mmsResponseData = sr.ReadToEnd();
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                        MsgResponseId deserializedJsonObj = (MsgResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(MsgResponseId));
                        this.DrawPanelForSuccess(statusPanel, deserializedJsonObj.Id.ToString());
                        sr.Close();
                    }
                }
                else
                {
                    dataToSend += "--" + boundaryToSend + "--\r\n";
                    byte[] bytesToSend = encoding.GetBytes(dataToSend);

                    int sizeToSend = bytesToSend.Length;

                    var memBufToSend = new MemoryStream(new byte[sizeToSend], 0, sizeToSend, true, true);
                    memBufToSend.Write(bytesToSend, 0, bytesToSend.Length);

                    byte[] finalData = memBufToSend.GetBuffer();
                    msgRequestObject.ContentLength = finalData.Length;

                    postStream = msgRequestObject.GetRequestStream();
                    postStream.Write(finalData, 0, finalData.Length);

                    WebResponse mmsResponseObject1 = msgRequestObject.GetResponse();
                    using (StreamReader sr = new StreamReader(mmsResponseObject1.GetResponseStream()))
                    {
                        string mmsResponseData = sr.ReadToEnd();
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                        MsgResponseId deserializedJsonObj = (MsgResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(MsgResponseId));
                        this.DrawPanelForSuccess(statusPanel, deserializedJsonObj.Id.ToString());
                        sr.Close();
                    }
                }
            }
            else
            {
                byte[] dataBytes = encoding.GetBytes(string.Empty);
                byte[] totalDataBytes = encoding.GetBytes(string.Empty);
                int count = 0;
                foreach (string attachment in attachments)
                {
                    string mmsFileName = Path.GetFileName(attachment.ToString());
                    string mmsFileExtension = Path.GetExtension(attachment.ToString());
                    string attachmentContentType = this.GetContentTypeFromExtension(mmsFileExtension);
                    FileStream imageFileStream = new FileStream(attachment.ToString(), FileMode.Open, FileAccess.Read);
                    BinaryReader imageBinaryReader = new BinaryReader(imageFileStream);
                    byte[] image = imageBinaryReader.ReadBytes((int)imageFileStream.Length);
                    imageBinaryReader.Close();
                    imageFileStream.Close();
                    if (count == 0)
                    {
                        dataToSend += "\r\n--" + boundaryToSend + "\r\n";
                    }
                    else
                    {
                        dataToSend = "\r\n--" + boundaryToSend + "\r\n";
                    }
                    
                    dataToSend += "Content-Disposition: form-data; name=\"file" + count + "\"; filename=\"" + mmsFileName + "\"\r\n";
                    dataToSend += "Content-Type:" + attachmentContentType + "\r\n";
                    dataToSend += "Content-ID:<" + mmsFileName + ">\r\n";
                    dataToSend += "Content-Transfer-Encoding:binary\r\n\r\n";
                    byte[] dataToSendByte = encoding.GetBytes(dataToSend);
                    int dataToSendSize = dataToSendByte.Length + image.Length;
                    var tempMemoryStream = new MemoryStream(new byte[dataToSendSize], 0, dataToSendSize, true, true);
                    tempMemoryStream.Write(dataToSendByte, 0, dataToSendByte.Length);
                    tempMemoryStream.Write(image, 0, image.Length);
                    dataBytes = tempMemoryStream.GetBuffer();
                    if (count == 0)
                    {
                        totalDataBytes = dataBytes;
                    }
                    else
                    {
                        byte[] tempForTotalBytes = totalDataBytes;
                        var tempMemoryStreamAttach = this.JoinTwoByteArrays(tempForTotalBytes, dataBytes);
                        totalDataBytes = tempMemoryStreamAttach.GetBuffer();
                    }

                    count++;
                }

                byte[] byteLastBoundary = encoding.GetBytes("\r\n--" + boundaryToSend + "--\r\n");
                int totalDataSize = totalDataBytes.Length + byteLastBoundary.Length;
                var totalMemoryStream = new MemoryStream(new byte[totalDataSize], 0, totalDataSize, true, true);
                totalMemoryStream.Write(totalDataBytes, 0, totalDataBytes.Length);
                totalMemoryStream.Write(byteLastBoundary, 0, byteLastBoundary.Length);
                byte[] finalpostBytes = totalMemoryStream.GetBuffer();

                msgRequestObject.ContentLength = finalpostBytes.Length;

                postStream = msgRequestObject.GetRequestStream();
                postStream.Write(finalpostBytes, 0, finalpostBytes.Length);

                WebResponse mmsResponseObject1 = msgRequestObject.GetResponse();
                using (StreamReader sr = new StreamReader(mmsResponseObject1.GetResponseStream()))
                {
                    string mmsResponseData = sr.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    MsgResponseId deserializedJsonObj = (MsgResponseId)deserializeJsonObject.Deserialize(mmsResponseData, typeof(MsgResponseId));
                    this.DrawPanelForSuccess(statusPanel, deserializedJsonObj.Id.ToString());
                    sr.Close();
                }
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    this.DrawPanelForFailure(statusPanel, reader.ReadToEnd());
                }
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.ToString());
        }
        finally
        {
            if (null != postStream)
            {
                postStream.Close();
            }

            if (attachments != null && attachments.Count != 0)
            {
                foreach (string file in attachments)
                {
                    try
                    {
                        File.Delete(file);
                        Session["Attachments"] = null;
                    }
                    catch 
                    { }
                }
            }
        }
    }

    /// <summary>
    /// Sums up two byte arrays.
    /// </summary>
    /// <param name="firstByteArray">First byte array</param>
    /// <param name="secondByteArray">second byte array</param>
    /// <returns>The memorystream"/> summed memory stream</returns>
    private MemoryStream JoinTwoByteArrays(byte[] firstByteArray, byte[] secondByteArray)
    {
        int newSize = firstByteArray.Length + secondByteArray.Length;
        var totalMemoryStream = new MemoryStream(new byte[newSize], 0, newSize, true, true);
        totalMemoryStream.Write(firstByteArray, 0, firstByteArray.Length);
        totalMemoryStream.Write(secondByteArray, 0, secondByteArray.Length);
        return totalMemoryStream;
    }

    #endregion

    #region Validation Functions

    /// <summary>
    /// Validates the given addresses based on following conditions
    /// 1. Group messages should not allow short codes
    /// 2. Short codes should be 3-8 digits in length
    /// 3. Valid Email Address
    /// 4. Group message must contain more than one address
    /// 5. Valid Phone number
    /// </summary>
    /// <returns>true/false; true - if address specified met the validation criteria, else false</returns>
    private bool IsValidAddress()
    {
        string phonenumbers = string.Empty;

        bool isValid = true;
        if (string.IsNullOrEmpty(txtPhone.Text))
        {
            this.DrawPanelForFailure(statusPanel, "Address field cannot be blank.");
            return false;
        }

        string[] addresses = txtPhone.Text.Trim().Split(',');

        if (addresses.Length > this.maxAddresses)
        {
            this.DrawPanelForFailure(statusPanel, "Message cannot be delivered to more than 10 receipients.");
            return false;
        }

        if (chkGroup.Checked && addresses.Length < 2)
        {
            this.DrawPanelForFailure(statusPanel, "Specify more than one address for Group message.");
            return false;
        }

        foreach (string address in addresses)
        {
            if (string.IsNullOrEmpty(address))
            {
                break;
            }

            if (address.Length < 3)
            {
                this.DrawPanelForFailure(statusPanel, "Invalid address specified.");
                return false;
            }

            // Verify if short codes are present in address
            if (!address.StartsWith("short") && (address.Length > 2 && address.Length < 9))
            {
                if (chkGroup.Checked)
                {
                    this.DrawPanelForFailure(statusPanel, "Group Message with short codes is not allowed.");
                    return false;
                }
                
                this.addressList.Add(address);
                this.phoneNumbersParameter = this.phoneNumbersParameter + "Addresses=short:" + Server.UrlEncode(address.ToString()) + "&";
            }

            if (address.StartsWith("short"))
            {
                if (chkGroup.Checked)
                {
                    this.DrawPanelForFailure(statusPanel, "Group Message with short codes is not allowed.");
                    return false;
                }

                System.Text.RegularExpressions.Regex regex = new Regex("^[0-9]*$");
                if (!regex.IsMatch(address.Substring(6)))
                {
                    this.DrawPanelForFailure(statusPanel, "Invalid short code specified.");
                    return false;               
                }

                this.addressList.Add(address);
                this.phoneNumbersParameter = this.phoneNumbersParameter + "Addresses=" + Server.UrlEncode(address.ToString()) + "&";
            }            
            else if (address.Contains("@"))
            {
                isValid = this.IsValidEmail(address);
                if (isValid == false)
                {
                    this.DrawPanelForFailure(statusPanel, "Specified Email Address is invalid.");
                    return false;
                }
                else
                {
                    this.addressList.Add(address);
                    this.phoneNumbersParameter = this.phoneNumbersParameter + "Addresses=" + Server.UrlEncode(address.ToString()) + "&";
                }
            }
            else
            {
                if (this.IsValidMISDN(address) == true)
                {
                    if (address.StartsWith("tel:"))
                    {
                        phonenumbers = address.Replace("-", string.Empty);
                        this.phoneNumbersParameter = this.phoneNumbersParameter + "Addresses=" + Server.UrlEncode(phonenumbers.ToString()) + "&";
                    }
                    else
                    {
                        phonenumbers = address.Replace("-", string.Empty);
                        this.phoneNumbersParameter = this.phoneNumbersParameter + "Addresses=" + Server.UrlEncode("tel:" + phonenumbers.ToString()) + "&";
                    }

                    this.addressList.Add(address);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Validate given string for MSISDN
    /// </summary>
    /// <param name="number">Phone number to be validated</param>
    /// <returns>true/false; true - if valid MSISDN, else false</returns>
    private bool IsValidMISDN(string number)
    {
        string smsAddressInput = number;
        long tryParseResult = 0;
        string smsAddressFormatted;
        string phoneStringPattern = "^\\d{3}-\\d{3}-\\d{4}$";
        if (Regex.IsMatch(smsAddressInput, phoneStringPattern))
        {
            smsAddressFormatted = smsAddressInput.Replace("-", string.Empty);
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

    /// <summary>
    /// Validates given mail ID for standard mail format
    /// </summary>
    /// <param name="emailID">Mail Id to be validated</param>
    /// <returns> true/false; true - if valid email id, else false</returns>
    private bool IsValidEmail(string emailID)
    {
        string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
              @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
              @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
        Regex re = new Regex(strRegex);
        if (re.IsMatch(emailID))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a given string for digits
    /// </summary>
    /// <param name="address">string to be validated</param>
    /// <returns>true/false; true - if passed string has all digits, else false</returns>
    private bool IsNumber(string address)
    {
        bool isValid = false;
        Regex regex = new Regex("^[0-9]*$");
        if (regex.IsMatch(address))
        {
            isValid = true;
        }

        return isValid;
    }

    /// <summary>
    /// Validates for file size
    /// Per specification, the maximum file size should be less than 600 KB
    /// </summary>
    /// <returns>true/false; Returns false, if file size exceeds 600KB. else true</returns>
    private bool IsValidFileSize()
    {
        ArrayList fileList = new ArrayList();

        long fileSize = 0;
        if (!String.IsNullOrEmpty(fileUpload1.FileName))
        {
            fileUpload1.SaveAs(Request.MapPath(fileUpload1.FileName.ToString()));
            fileList.Add(Request.MapPath(fileUpload1.FileName));
            FileInfo fileInfoObj = new FileInfo(Request.MapPath(fileUpload1.FileName));
            fileSize = fileSize + (fileInfoObj.Length / 1024);
            if (fileSize > 600)
            {
                // delete saved file.
                this.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB");
                return false;
            }
        }

        if (!String.IsNullOrEmpty(fileUpload2.FileName))
        {
            fileUpload2.SaveAs(Request.MapPath(fileUpload2.FileName));
            fileList.Add(Request.MapPath(fileUpload2.FileName));
            FileInfo fileInfoObj = new FileInfo(Request.MapPath(fileUpload2.FileName));
            fileSize = fileSize + (fileInfoObj.Length / 1024);
            if (fileSize > 600)
            {
                this.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB");
                return false;
            }
        }

        if (!String.IsNullOrEmpty(fileUpload3.FileName))
        {
            fileUpload3.SaveAs(Request.MapPath(fileUpload3.FileName));
            fileList.Add(Request.MapPath(fileUpload3.FileName));
            FileInfo fileInfoObj = new FileInfo(Request.MapPath(fileUpload3.FileName));
            fileSize = fileSize + (fileInfoObj.Length / 1024);
            if (fileSize > 600)
            {
                // delete saved file.
                this.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB");
                return false;
            }
        }

        if (!String.IsNullOrEmpty(fileUpload4.FileName))
        {
            fileUpload4.SaveAs(Request.MapPath(fileUpload4.FileName));
            fileList.Add(Request.MapPath(fileUpload4.FileName));
            FileInfo fileInfoObj = new FileInfo(Request.MapPath(fileUpload4.FileName));
            fileSize = fileSize + (fileInfoObj.Length / 1024);
            if (fileSize > 600)
            {
                // delete saved file.
                this.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB");
                return false;
            }
        }

        if (!String.IsNullOrEmpty(fileUpload5.FileName))
        {
            fileUpload5.SaveAs(Request.MapPath(fileUpload5.FileName));
            fileList.Add(Request.MapPath(fileUpload5.FileName));
            FileInfo fileInfoObj = new FileInfo(Request.MapPath(fileUpload5.FileName));
            fileSize = fileSize + (fileInfoObj.Length / 1024);
            if (fileSize > 600)
            {
                // delete saved file.
                this.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600kb");
                return false;
            }
        }

        if (null != fileList && fileList.Count != 0)
        {
            Session["Attachments"] = fileList;
        }

        return true;
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
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Font.Bold = true;
        rowTwoCellOne.Text = "Message ID:";
        rowTwoCellOne.Width = Unit.Pixel(70);
        rowTwo.Controls.Add(rowTwoCellOne);
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
            this.scope = "MOBO";
        }

        if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["max_addresses"]))
        {
            this.maxAddresses = 10;
        }
        else
        {
            this.maxAddresses = Convert.ToInt32(ConfigurationManager.AppSettings["max_addresses"]);
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
    /// This function resets access token related session variable to null 
    /// </summary>
    private void ResetTokenSessionVariables()
    {
        Session["mobo_session_access_token"] = null;
        Session["mobo_session_accessTokenExpiryTime"] = null;
        Session["mobo_session_refresh_token"] = null;
        Session["mobo_session_refreshTokenExpiryTime"] = null;
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
        if (Session["mobo_session_access_token"] != null)
        {
            this.accessToken = Session["mobo_session_access_token"].ToString();
        }
        else
        {
            this.accessToken = null;
        }

        if (Session["mobo_session_accessTokenExpiryTime"] != null)
        {
            this.accessTokenExpiryTime = Session["mobo_session_accessTokenExpiryTime"].ToString();
        }
        else
        {
            this.accessTokenExpiryTime = null;
        }

        if (Session["mobo_session_refresh_token"] != null)
        {
            this.refreshToken = Session["mobo_session_refresh_token"].ToString();
        }
        else
        {
            this.refreshToken = null;
        }

        if (Session["mobo_session_refreshTokenExpiryTime"] != null)
        {
            this.refreshTokenExpiryTime = Session["mobo_session_refreshTokenExpiryTime"].ToString();
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
        if (Session["mobo_session_access_token"] == null)
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
    /// <param name="type">If type value is 0, access token is fetch for authorization code flow
    /// If type value is 2, access token is fetch for authorization code floww based on the exisiting refresh token</param>
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
                oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&code=" + this.authCode + "&grant_type=authorization_code&scope=MOBO";
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

                    Session["mobo_session_accessTokenExpiryTime"] = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in));

                    if (deserializedJsonObj.expires_in.Equals("0"))
                    {
                        int defaultAccessTokenExpiresIn = 100; // In Years
                        Session["mobo_session_accessTokenExpiryTime"] = currentServerTime.AddYears(defaultAccessTokenExpiresIn);
                    }

                    this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    
                    Session["mobo_session_access_token"] = this.accessToken;
                    
                    this.accessTokenExpiryTime = Session["mobo_session_accessTokenExpiryTime"].ToString();
                    Session["mobo_session_refresh_token"] = this.refreshToken;
                    Session["mobo_session_refreshTokenExpiryTime"] = this.refreshTokenExpiryTime.ToString();
                    Session["mobo_session_appState"] = "TokenReceived";
                    accessTokenResponseStream.Close();
                    return true;
                }
                else
                {
                    this.DrawPanelForFailure(statusPanel, "Auth server returned null access token");
                    return false;
                }
            }
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

        return false;
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
/// Response from Mobo api
/// </summary>
public class MsgResponseId
{
    /// <summary>
    /// Gets or sets Message ID
    /// </summary>
    public string Id { get; set; }
}

#endregion