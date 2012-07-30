// <copyright file="Default.aspx.cs" company="AT&amp;T">
// Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
// TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
// Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
// For more information contact developer.support@att.com
// </copyright>

#region References
using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Configuration;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
#endregion

/// <summary>
/// Class file for SMS_Apps application
/// </summary>
public partial class SMS_App2 : System.Web.UI.Page
{
    #region Local Variables
    
    /// <summary>
    /// Gets or sets the value of shortCode
    /// </summary>
    private string shortCode;

    /// <summary>
    /// Gets or sets the value of football filepath
    /// </summary>
    private string footballFilePath;

    /// <summary>
    /// Gets or sets the value of baseball filepath
    /// </summary>
    private string baseballFilePath;
    
    /// <summary>
    /// Gets or sets the value of basketball filepath
    /// </summary>
    private string basketballFilePath;

    #endregion

    #region Bypass SSL Handshake Error Method
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
    /// This method called when the page is loaded into the browser. Reads the config values and sets the local variables
    /// </summary>
    /// <param name="sender">object, which invoked this method</param>
    /// <param name="e">EventArgs, which specifies arguments specific to this method</param>
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            BypassCertificateError();

            DateTime currentServerTime = DateTime.UtcNow;
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
            this.ReadConfigFile();
            if (!Page.IsPostBack)
            {
                shortCodeLabel.Text = this.shortCode;
                this.UpdateVoteCount();
                this.drawMessages();
            }
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.ToString());
        }
    }


    /// <summary>
    /// Method will be called when the user clicks on Update Votes Total button
    /// </summary>
    /// <param name="sender">object, that invoked this method</param>
    /// <param name="e">EventArgs, specific to this method</param>
    protected void UpdateButton_Click(object sender, EventArgs e)
    {
        try
        {
            this.UpdateVoteCount();
            this.drawMessages();
        }
        catch (Exception ex)
        {
            this.DrawPanelForFailure(statusPanel, ex.ToString());
        }
    }

    #endregion

    #region Display Status Functions

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
        table.CssClass = "success";
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
        rowTwoCellOne.Text = "<b>Total Votes:</b>" + message;
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

    #endregion

    #region SMS Specific Functions

    /// <summary>
    /// This method reads from config file and assign the values to local variables.
    /// Displays error message in case of ay mandatory value not specified
    /// </summary>
    /// <returns>true/false; true - if able to read all the mandatory values from config file; else false</returns>
    private bool ReadConfigFile()
    {
        this.footballFilePath = ConfigurationManager.AppSettings["FootBallFilePath"];

        if (string.IsNullOrEmpty(this.footballFilePath))
        {
            this.DrawPanelForFailure(statusPanel, "FootBallFilePath is not defined in configuration file");
            return false;
        }
        
        this.baseballFilePath = ConfigurationManager.AppSettings["BaseBallFilePath"];
        if (string.IsNullOrEmpty(this.baseballFilePath))
        {
            this.DrawPanelForFailure(statusPanel, "BaseBallFilePath is not defined in configuration file");
            return false;
        }

        this.basketballFilePath = ConfigurationManager.AppSettings["BasketBallFilePath"];
        if (string.IsNullOrEmpty(this.basketballFilePath))
        {
            this.DrawPanelForFailure(statusPanel, "BasketBallFilePath is not defined in configuration file");
            return false;
        }

        this.shortCode = ConfigurationManager.AppSettings["ShortCode"];
        if (string.IsNullOrEmpty(this.shortCode))
        {
            this.DrawPanelForFailure(statusPanel, "ShortCode is not defined in configuration file");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// This method reads the messages file and draw the table.
    /// </summary>
    private void drawMessages()
    {
        string srcFile = Request.MapPath(ConfigurationManager.AppSettings["MessagesFilePath"]);
        string destFile = Request.MapPath(ConfigurationManager.AppSettings["MessagesTempFilePath"]);
        string messagesLine = String.Empty;
        receiveMessagePanel.Controls.Clear();
        if (File.Exists(srcFile))
        {
            File.Move(srcFile, destFile);
            Table secondTable = new Table();
            secondTable.Font.Name = "Sans-serif";
            secondTable.Font.Size = 9;
            TableRow TableRow = new TableRow();
            secondTable.Font.Size = 8;
            secondTable.Width = Unit.Pixel(1000);
            TableCell TableCell = new TableCell();
            TableCell.Width = Unit.Pixel(200);
            TableCell.Text = "DateTime";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableCell.Font.Bold = true;
            TableRow.Cells.Add(TableCell);
            TableCell = new TableCell();
            TableCell.Font.Bold = true;
            TableCell.Width = Unit.Pixel(100);
            TableCell.Wrap = true;
            TableCell.Text = "MessageId";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableRow.Cells.Add(TableCell);
            TableCell = new TableCell();
            TableCell.Text = "Message";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableCell.Font.Bold = true;
            TableCell.Width = Unit.Pixel(300);
            TableRow.Cells.Add(TableCell);
            TableCell = new TableCell();
            TableCell.Text = "SenderAddress";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableCell.Font.Bold = true;
            TableCell.Width = Unit.Pixel(150);
            TableRow.Cells.Add(TableCell);
            TableCell = new TableCell();
            TableCell.Text = "DestinationAddress";
            TableCell.HorizontalAlign = HorizontalAlign.Center;
            TableCell.Font.Bold = true;
            TableCell.Width = Unit.Pixel(150);
            TableRow.Cells.Add(TableCell);
            secondTable.Rows.Add(TableRow);
            receiveMessagePanel.Controls.Add(secondTable);
            using (StreamReader sr = new StreamReader(destFile))
            {
                while (sr.Peek() >= 0)
                {
                    messagesLine = sr.ReadLine();
                    string[] messageValues = Regex.Split(messagesLine, "_-_-");
                    TableRow = new TableRow();
                    TableCell TableCellDateTime = new TableCell();
                    TableCellDateTime.Width = Unit.Pixel(200);
                    TableCellDateTime.Text = messageValues[0];
                    TableCellDateTime.HorizontalAlign = HorizontalAlign.Center;
                    TableCell TableCellMessageId = new TableCell();
                    TableCellMessageId.Width = Unit.Pixel(100);
                    TableCellMessageId.Wrap = true;
                    TableCellMessageId.Text = messageValues[1];
                    TableCellMessageId.HorizontalAlign = HorizontalAlign.Center;
                    TableCell TableCellMessage = new TableCell();
                    TableCellMessage.Width = Unit.Pixel(300);
                    TableCellMessage.Text = messageValues[2];
                    TableCellMessage.HorizontalAlign = HorizontalAlign.Center;
                    TableCell TableCellSenderAddress = new TableCell();
                    TableCellSenderAddress.Width = Unit.Pixel(150);
                    TableCellSenderAddress.Text = messageValues[3];
                    TableCellSenderAddress.HorizontalAlign = HorizontalAlign.Center;
                    TableCell TableCellDestinationAddress = new TableCell();
                    TableCellDestinationAddress.Width = Unit.Pixel(150);
                    TableCellDestinationAddress.Text = messageValues[4];
                    TableCellDestinationAddress.HorizontalAlign = HorizontalAlign.Center;
                    TableRow.Cells.Add(TableCellDateTime);
                    TableRow.Cells.Add(TableCellMessageId);
                    TableRow.Cells.Add(TableCellMessage);
                    TableRow.Cells.Add(TableCellSenderAddress);
                    TableRow.Cells.Add(TableCellDestinationAddress);
                    secondTable.Rows.Add(TableRow);
                    string msgtxt = TableCellMessage.Text.ToString();
                    if (msgtxt.Equals("football", StringComparison.CurrentCultureIgnoreCase) ||
                        msgtxt.Equals("baseball", StringComparison.CurrentCultureIgnoreCase) ||
                        msgtxt.Equals("basketball", StringComparison.CurrentCultureIgnoreCase))
                    {
                    }
                    else
                    {
                        TableCellDateTime.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                        TableCellMessageId.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                        TableCellMessage.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                        TableCellSenderAddress.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                        TableCellDestinationAddress.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
                    }
                }
                sr.Close();
                File.Delete(destFile);
            }

        }
    }

    /// <summary>
    /// This method updates the vote counts by reading from the files
    /// </summary>
    private void UpdateVoteCount()
    {
        try
        {   
            footballLabel.Text = this.GetCountFromFile(this.footballFilePath).ToString();
            baseballLabel.Text = this.GetCountFromFile(this.baseballFilePath).ToString();
            basketballLabel.Text = this.GetCountFromFile(this.basketballFilePath).ToString();

            int totalCount = Convert.ToInt32(footballLabel.Text) + Convert.ToInt32(baseballLabel.Text) + Convert.ToInt32(basketballLabel.Text);
            this.DrawPanelForSuccess(statusPanel, totalCount.ToString());
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    /// This method reads from files and returns the number of messages.
    /// </summary>
    /// <param name="filePath">string, Name of the file to read from</param>
    /// <returns>int, count of messages</returns>
    private int GetCountFromFile(string filePath)
    {
        int count = 0;
        using (StreamReader streamReader = File.OpenText(Request.MapPath(filePath)))
        {
            count = Convert.ToInt32(streamReader.ReadToEnd());
            streamReader.Close();
        }

        return count;
    }
    #endregion
}