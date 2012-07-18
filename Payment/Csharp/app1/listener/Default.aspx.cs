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
using System.Web.Script.Serialization;
using System.Xml.Serialization;

#endregion

/// <summary>
/// Payment App2 Listener class
/// </summary>
public partial class PaymentApp1_Listener : System.Web.UI.Page
{
    /// <summary>
    /// Local variables for processing of request stream
    /// </summary>
    private string apiKey, endPoint, secretKey, accessTokenFilePath, accessToken, expirySeconds,
        refreshToken, accessTokenExpiryTime, refreshTokenExpiryTime, notificationDetailsFile;

    /// <summary>
    /// Local variables for processing of request stream
    /// </summary>    
    private string notificationId;

    /// <summary>
    /// Local variables for processing of request stream
    /// </summary>
    private StreamWriter notificationDetailsStreamWriter;

    /// <summary>
    /// Gets or sets the value of refreshTokenExpiresIn
    /// </summary>
    private int refreshTokenExpiresIn;

    /// <summary>
    /// This function is used to neglect the ssl handshake error with authentication server.
    /// </summary>
    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }

    /// <summary>
    /// Default method, that gets called upon loading the page.
    /// </summary>
    /// <param name="sender">object that invoked this method</param>
    /// <param name="e">Event arguments</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        BypassCertificateError();

        try
        {
            Stream inputStream = Request.InputStream;

            //int streamLength = Convert.ToInt32(inputStream.Length);
            //byte[] stringArray = new byte[streamLength];
            //inputStream.Read(stringArray, 0, streamLength);

            //string xmlString = System.Text.Encoding.UTF8.GetString(stringArray);

            //string xmlString = "<?xml version='1.0' encoding='UTF-8'?><hub:notifications xmlns:hub=\"http://hub.amdocs.com\"><hub:notificationId>37231c36-fdf5-464d-9734-aa0389ffc9a4</hub:notificationId></hub:notifications>";
            //XmlSerializer serializer = new XmlSerializer(typeof(Notification));
            //UTF8Encoding encoding = new UTF8Encoding();
            //MemoryStream ms = new MemoryStream(encoding.GetBytes(xmlString));
            //Notifications notificationIds = (Notifications)serializer.Deserialize(ms);
            
            ArrayList listOfNotificationIds = new ArrayList();
            listOfNotificationIds = this.GetNotificationIds(inputStream);
            
            if (!this.ReadConfigFile())
            {
                //this.LogError("Unable to read config file");
                return;
            }

            if (!this.ReadAndGetAccessToken())
            {
                //this.LogError("Unable to get access token");
                return;
            }

            
            int noOfItems = listOfNotificationIds.Count;

            int notificationIdIndex = 0;
            while (noOfItems > 0)
            {
                noOfItems--;
                this.notificationId = listOfNotificationIds[notificationIdIndex].ToString();

                // Get notification details
                WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(this.endPoint + "/rest/3/Commerce/Payment/Notifications/" + this.notificationId);
                objRequest.Headers.Add("Authorization", "Bearer " + this.accessToken);
                objRequest.Method = "GET";
                objRequest.ContentType = "application/json";
                IDictionary<string, object> notificationResponse;
                WebResponse notificationRespone = (WebResponse)objRequest.GetResponse();
                using (StreamReader notificationResponseStream = new StreamReader(notificationRespone.GetResponseStream()))
                {
                    string notificationResponseData = notificationResponseStream.ReadToEnd();
                    //this.LogError(notificationResponseData);
                    //using (StreamWriter streamWriter = File.AppendText(Request.MapPath(Mess.txt)))
                    //{
                       // streamWriter.Write(notificationResponseData);
                       // streamWriter.WriteLine("***************************************");
                       // streamWriter.Close();
                   // }
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    notificationResponse = (IDictionary<string, object>)deserializeJsonObject.Deserialize(notificationResponseData, typeof(IDictionary<string, object>));


                }
                notificationRespone.Close();

                WebRequest acknowledgeNotificationRequestObj = (WebRequest)System.Net.WebRequest.Create(this.endPoint + "/rest/3/Commerce/Payment/Notifications/" + this.notificationId);
                acknowledgeNotificationRequestObj.Method = "PUT";
                acknowledgeNotificationRequestObj.Headers.Add("Authorization", "Bearer " + this.accessToken);
                acknowledgeNotificationRequestObj.ContentType = "application/json";
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes("");
                acknowledgeNotificationRequestObj.ContentLength = postBytes.Length;
                Stream postStream = acknowledgeNotificationRequestObj.GetRequestStream();
                postStream.Write(postBytes, 0, postBytes.Length);
                postStream.Close();
                WebResponse acknowledgeNotificationRespone = (WebResponse)acknowledgeNotificationRequestObj.GetResponse();
                using (StreamReader acknowledgeNotificationResponseStream = new StreamReader(acknowledgeNotificationRespone.GetResponseStream()))
                {
                    string notificationType = string.Empty;
                    string originalTransactionId = string.Empty;

                    foreach (KeyValuePair<string, object> keyValue in notificationResponse)
                    {
                        if (keyValue.Key == "GetNotificationResponse")
                        {
                            IDictionary<string, object> getNotificationResponse = (IDictionary<string, object>)keyValue.Value;
                            foreach (KeyValuePair<string, object> notificationResponseKeyValue in getNotificationResponse)
                            {
                                if (notificationResponseKeyValue.Key == "OriginalTransactionId")
                                {
                                    originalTransactionId = notificationResponseKeyValue.Value.ToString();
                                }

                                if (notificationResponseKeyValue.Key == "NotificationType")
                                {
                                    notificationType = notificationResponseKeyValue.Value.ToString();
                                }
                            }
                            using (notificationDetailsStreamWriter = File.AppendText(Request.MapPath(this.notificationDetailsFile)))
                            {
                                notificationDetailsStreamWriter.WriteLine(notificationId + ":" + notificationType + ":" + originalTransactionId + "$");
                                notificationDetailsStreamWriter.Close();
                            }
                        }
                    }
                    string acknowledgeNotificationResponseData = acknowledgeNotificationResponseStream.ReadToEnd();
                    //this.LogError("Successfully acknowledge to " + this.notificationId + "***" + "\n");
                    acknowledgeNotificationResponseStream.Close();
                }
                notificationIdIndex++;
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    //this.LogError(new StreamReader(stream).ReadToEnd());                    
                }
            }
        }
        catch (Exception ex)
        {
            //this.LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Logs error message onto file
    /// </summary>
    /// <param name="text">Text to be logged</param>
    private void LogError(string text)
    {
        File.AppendAllText(Request.MapPath("errorInNotification.txt"),Environment.NewLine + DateTime.Now.ToString() + ": " + text);        
    }

    /// <summary>
    /// This function reads the Access Token File and stores the values of access token, expiry seconds
    /// refresh token, last access token time and refresh token expiry time
    /// This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    /// </summary>
    /// <returns>Returns Boolean</returns>
    private bool ReadAccessTokenFile()
    {
        FileStream fileStream = null;
        StreamReader streamReader = null;

        try
        {
            fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            streamReader = new StreamReader(fileStream);
            this.accessToken = streamReader.ReadLine();
            this.expirySeconds = streamReader.ReadLine();
            this.refreshToken = streamReader.ReadLine();
            this.accessTokenExpiryTime = streamReader.ReadLine();
            this.refreshTokenExpiryTime = streamReader.ReadLine();
        }
        catch
        {
            //this.LogError("Unable to read access token file");
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

        if ((this.accessToken == null) || (this.expirySeconds == null) || (this.refreshToken == null) || (this.accessTokenExpiryTime == null) || (this.refreshTokenExpiryTime == null))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This function validates the expiry of the access token and refresh token, function compares the current time with the refresh token taken time, if current time is greater then returns INVALID_REFRESH_TOKEN
    /// function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    /// funciton returns INVALID_ACCESS_TOKEN
    /// otherwise returns VALID_ACCESS_TOKEN
    /// </summary>
    /// <returns>Returns String</returns>
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
    /// This function get the access token based on the type parameter type values.
    /// If type value is 1, access token is fetch for client credential flow
    /// If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    /// </summary>
    /// <param name="type">Type as Integer</param>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns boolean</returns>
    private bool GetAccessToken(int type)
    {
        FileStream fileStream = null;
        StreamWriter streamWriter = null;

        try
        {
            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            WebRequest accessTokenRequest = null;
            if (type == 1)
            {
                accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token?client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=PAYMENT");
            }
            else if (type == 2)
            {
                accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.endPoint + "/oauth/token?grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken);
            }

            accessTokenRequest.Method = "GET";

            WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
            using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
            {
                string jsonAccessToken = accessTokenResponseStream.ReadToEnd();

                JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));
                this.accessToken = deserializedJsonObj.access_token;
                this.expirySeconds = deserializedJsonObj.expires_in;
                this.refreshToken = deserializedJsonObj.refresh_token;

                this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(this.expirySeconds)).ToLongDateString() + " " + currentServerTime.AddSeconds(Convert.ToDouble(this.expirySeconds)).ToLongTimeString();

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
                streamWriter.WriteLine(this.expirySeconds);
                streamWriter.WriteLine(this.refreshToken);
                streamWriter.WriteLine(this.accessTokenExpiryTime);
                streamWriter.WriteLine(this.refreshTokenExpiryTime);

                accessTokenResponseStream.Close();
                return true;
            }
        }
        catch (WebException we)
        {
            if (null != we.Response)
            {
                using (Stream stream = we.Response.GetResponseStream())
                {
                    //this.LogError(new StreamReader(stream).ReadToEnd());
                    throw we;
                }
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
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
    /// This function is used to read access token file and validate the access token.
    /// This function returns true if access token is valid, or else false is returned.
    /// </summary>
    /// <param name="panelParam">Panel Details</param>
    /// <returns>Returns Boolean</returns>
    private bool ReadAndGetAccessToken()
    {
        bool result = true;
        if (this.ReadAccessTokenFile() == false)
        {
            result = this.GetAccessToken(1);
        }
        else
        {
            string tokenValidity = this.IsTokenValid();
            if (tokenValidity.CompareTo("REFRESH_TOKEN") == 0)
            {
                result = this.GetAccessToken(2);
            }
            else if (tokenValidity.Equals("INVALID_ACCESS_TOKEN"))
            {
                result = this.GetAccessToken(1);
            }
        }

        return result;
    }

    #region Data Structures

    /// <summary>
    /// This class defines Access Token Response
    /// </summary>
    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets Access Token
        /// </summary>
        public string access_token { get; set; }

        /// <summary>
        /// Gets or sets Refresh Token
        /// </summary>
        public string refresh_token { get; set; }

        /// <summary>
        /// Gets or sets Expires In
        /// </summary>
        public string expires_in { get; set; }
    }

    #endregion

    /// <summary>
    /// Reads from config file and assigns to local variables
    /// </summary>
    /// <returns>true/false; true if able to read all values, false otherwise</returns>
    private bool ReadConfigFile()
    {
        this.apiKey = ConfigurationManager.AppSettings["api_key"].ToString();
        if (string.IsNullOrEmpty(this.apiKey))
        {
            return false;
        }

        this.endPoint = ConfigurationManager.AppSettings["endPoint"].ToString();
        if (string.IsNullOrEmpty(this.endPoint))
        {
            return false;
        }

        this.secretKey = ConfigurationManager.AppSettings["secret_key"].ToString();
        if (string.IsNullOrEmpty(this.secretKey))
        {
            return false;
        }

        this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        if (string.IsNullOrEmpty(this.accessTokenFilePath))
        {
            this.accessTokenFilePath = "~\\PayApp1AccessToken.txt";
        }

        this.notificationDetailsFile = ConfigurationManager.AppSettings["notificationDetailsFile"];
        if (string.IsNullOrEmpty(this.notificationDetailsFile))
        {
            this.notificationDetailsFile = "~\\notificationDetailsFile.txt";
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

    ///// <summary>
    ///// Represents the List of Notification Ids
    ///// </summary>
    //[System.Xml.Serialization.XmlRoot("Notifications")]
    //public class Notifications
    //{
    //    /// <summary>
    //    /// Gets or sets the list of Notificationids.
    //    /// </summary>
    //    [System.Xml.Serialization.XmlElement("NotificationId")]
    //    public string[] notificationid { get; set; }
    //}

    /// <summary>
    /// Method fetches notification ids from the stream.
    /// </summary>
    /// <param name="stream">Input stream received from listener</param>
    /// <returns>List of Notification Ids</returns>
    public ArrayList GetNotificationIds(System.IO.Stream stream)
    {
        ArrayList listfofNotificationIds = null;
        try
        {
            string inputStreamContent = string.Empty;
            int inputStreamContentLength = Convert.ToInt32(stream.Length);
            byte[] inputStreamByte = new byte[inputStreamContentLength];
            int strRead = stream.Read(inputStreamByte, 0, inputStreamContentLength);
            inputStreamContent = System.Text.Encoding.UTF8.GetString(inputStreamByte);

            //StreamWriter writer = File.AppendText(Request.MapPath("PaymentApp1ListenerInvoked.txt"));
            //writer.Write(Environment.NewLine + DateTime.Now.ToString() + ":Payment App1 has been invoked" + inputStreamContent);
            //writer.Close();

            string[] findText = { "notificationId>" };

            string[] Ids = inputStreamContent.Split(findText, StringSplitOptions.RemoveEmptyEntries);
            if (null != Ids)
            {
                listfofNotificationIds = new ArrayList();
            }

            foreach (string Id in Ids)
            {
                string temp = Id.Substring(0, Id.IndexOf('<'));
                if (!string.IsNullOrEmpty(temp))
                {
                    listfofNotificationIds.Add(temp);
                }
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }

        return listfofNotificationIds;
    }
}
