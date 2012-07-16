// <copyright file="Listener.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
#endregion

/// <summary>
/// Listener class for saving message counts.
/// </summary>
public partial class Listener : System.Web.UI.Page
{
    #region Events
    /// <summary>
    /// This method called when the page is loaded into the browser. This method requests input stream and parses it to get message counts.
    /// </summary>
    /// <param name="sender">object, which invoked this method</param>
    /// <param name="e">EventArgs, which specifies arguments specific to this method</param>
    protected void Page_Load(object sender, EventArgs e)
    {

        System.IO.Stream stream = Request.InputStream;

        if (null != stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);
            string responseData = Encoding.ASCII.GetString(bytes);

            JavaScriptSerializer serializeObject = new JavaScriptSerializer();
            InboundSMSMessage message = (InboundSMSMessage)serializeObject.Deserialize(responseData, typeof(InboundSMSMessage));

            if (null != message)
            {
                this.SaveMessageCount(message);
                this.SaveMessage(message);
            }
        }
/*
        string inputStreamContents;
        int stringLength;
        int strRead;
        System.IO.Stream str = Request.InputStream;
        stringLength = Convert.ToInt32(str.Length);
        byte[] stringArray = new byte[stringLength];
        strRead = str.Read(stringArray, 0, stringLength);
        inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray);
        System.IO.File.WriteAllText(Request.MapPath("~\\UAT\\Csharp-RESTful\\sms\\app2\\ListenerLog.txt"), inputStreamContents);
 */

    }
    #endregion
#region Method to store the received message to file
    /// <summary>
    /// This method reads the incoming message and stores the received message details.
    /// </summary>
    /// <param name="message">InboundSMSMessage, message received from Request</param>
    private void SaveMessage(InboundSMSMessage message)
    {
        string filePath = ConfigurationManager.AppSettings["MessagesFilePath"];

        string messageLineToStore = message.DateTime.ToString() + "_-_-" +
                                    message.MessageId.ToString() + "_-_-" +
                                    message.Message.ToString() + "_-_-" +
                                    message.SenderAddress.ToString() + "_-_-" +
                                    message.DestinationAddress.ToString();

        using (StreamWriter streamWriter = File.AppendText(Request.MapPath(filePath)))
        {
            streamWriter.WriteLine(messageLineToStore);
            streamWriter.Close();
        }
    }
#endregion

    #region Methods to parse and save the message counts
    /// <summary>
    /// This method reads the incoming message and decides on to which message count needs to be updated.
    /// This method invokes another method to write the count to file
    /// </summary>
    /// <param name="message">InboundSMSMessage, message received from Request</param>
    private void SaveMessageCount(InboundSMSMessage message)
    {
        if (!string.IsNullOrEmpty(message.Message))
        {
            string messageText = message.Message.Trim().ToLower();

            string filePathConfigKey = string.Empty;
            switch (messageText)
            {
                case "basketball":
                    filePathConfigKey = "BasketBallFilePath";
                    break;
                case "football":
                    filePathConfigKey = "FootBallFilePath";
                    break;
                case "baseball":
                    filePathConfigKey = "BaseBallFilePath";
                    break;
            }

            if (!string.IsNullOrEmpty(filePathConfigKey))
            {
                this.WriteToFile(filePathConfigKey);
            }
        }
    }

    /// <summary>
    /// This method gets the file name, reads from the file, increments the count(if any) and writes back to the file.
    /// </summary>
    /// <param name="filePathConfigKey">string, parameter which specifies the name of the file</param>
    private void WriteToFile(string filePathConfigKey)
    {
        string filePath = ConfigurationManager.AppSettings[filePathConfigKey];

        int count = 0;
        using (StreamReader streamReader = File.OpenText(Request.MapPath(filePath)))
        {
            count = Convert.ToInt32(streamReader.ReadToEnd());
            streamReader.Close();
        }

        count = count + 1;

        using (StreamWriter streamWriter = File.CreateText(Request.MapPath(filePath)))
        {
            streamWriter.Write(count);
            streamWriter.Close();
        }
    }
    #endregion
}

#region Message Structure
/// <summary>
/// Message structure received
/// </summary>
public class InboundSMSMessage
{
    /// <summary>
    /// Gets or sets the value of DateTime
    /// </summary>
    public string DateTime
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of MessageId
    /// </summary>
    public string MessageId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of Message
    /// </summary>
    public string Message
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of SenderAddress
    /// </summary>
    public string SenderAddress
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of DestinationAddress
    /// </summary>
    public string DestinationAddress
    {
        get;
        set;
    }
}
#endregion
