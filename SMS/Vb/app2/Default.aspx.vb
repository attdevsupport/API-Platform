' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Net
Imports System.Configuration
Imports System.IO
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text.RegularExpressions
#End Region

''' <summary>
''' Class file for SMS_Apps application
''' </summary>
Partial Public Class SMS_App2
    Inherits System.Web.UI.Page
#Region "Local Variables"

    ''' <summary>
    ''' Gets or sets the value of shortCode
    ''' </summary>
    Private shortCode As String

    ''' <summary>
    ''' Gets or sets the value of football filepath
    ''' </summary>
    Private footballFilePath As String

    ''' <summary>
    ''' Gets or sets the value of baseball filepath
    ''' </summary>
    Private baseballFilePath As String

    ''' <summary>
    ''' Gets or sets the value of basketball filepath
    ''' </summary>
    Private basketballFilePath As String

#End Region

#Region "Bypass SSL Handshake Error Method"
    ''' <summary>
    ''' This function is used to neglect the ssl handshake error with authentication server
    ''' </summary>
    Function CertificateValidationCallBack( _
    ByVal sender As Object, _
    ByVal certificate As X509Certificate, _
    ByVal chain As X509Chain, _
    ByVal sslPolicyErrors As SslPolicyErrors _
) As Boolean

        Return True
    End Function
#End Region

#Region "Events"

    ''' <summary>
    ''' This method called when the page is loaded into the browser. Reads the config values and sets the local variables
    ''' </summary>
    ''' <param name="sender">object, which invoked this method</param>
    ''' <param name="e">EventArgs, which specifies arguments specific to this method</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)

            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            Me.ReadConfigFile()
            If Not Page.IsPostBack Then
                shortCodeLabel.Text = Me.shortCode
                Me.UpdateVoteCount()
                Me.drawMessages()
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        End Try
    End Sub


    ''' <summary>
    ''' Method will be called when the user clicks on Update Votes Total button
    ''' </summary>
    ''' <param name="sender">object, that invoked this method</param>
    ''' <param name="e">EventArgs, specific to this method</param>
    Protected Sub UpdateButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Me.UpdateVoteCount()
            Me.drawMessages()
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        End Try
    End Sub

#End Region

#Region "Display Status Functions"

    ''' <summary>
    ''' Display success message
    ''' </summary>
    ''' <param name="panelParam">Panel to draw success message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForSuccess(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Clear()
        End If

        Dim table As New Table()
        table.CssClass = "success"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)

        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Text = "<b>Total Votes:</b>" & message
        rowTwo.Controls.Add(rowTwoCellOne)
        table.Controls.Add(rowTwo)

        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' Displays error message
    ''' </summary>
    ''' <param name="panelParam">Panel to draw success message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Clear()
        End If

        Dim table As New Table()
        table.CssClass = "errorWide"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        table.Controls.Add(rowTwo)

        panelParam.Controls.Add(table)
    End Sub

#End Region

#Region "SMS Specific Functions"

    ''' <summary>
    ''' This method reads from config file and assign the values to local variables.
    ''' Displays error message in case of ay mandatory value not specified
    ''' </summary>
    ''' <returns>true/false; true - if able to read all the mandatory values from config file; else false</returns>
    Private Function ReadConfigFile() As Boolean
        Me.footballFilePath = ConfigurationManager.AppSettings("FootBallFilePath")

        If String.IsNullOrEmpty(Me.footballFilePath) Then
            Me.DrawPanelForFailure(statusPanel, "FootBallFilePath is not defined in configuration file")
            Return False
        End If

        Me.baseballFilePath = ConfigurationManager.AppSettings("BaseBallFilePath")
        If String.IsNullOrEmpty(Me.baseballFilePath) Then
            Me.DrawPanelForFailure(statusPanel, "BaseBallFilePath is not defined in configuration file")
            Return False
        End If

        Me.basketballFilePath = ConfigurationManager.AppSettings("BasketBallFilePath")
        If String.IsNullOrEmpty(Me.basketballFilePath) Then
            Me.DrawPanelForFailure(statusPanel, "BasketBallFilePath is not defined in configuration file")
            Return False
        End If

        Me.shortCode = ConfigurationManager.AppSettings("ShortCode")
        If String.IsNullOrEmpty(Me.shortCode) Then
            Me.DrawPanelForFailure(statusPanel, "ShortCode is not defined in configuration file")
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' This method reads the messages file and draw the table.
    ''' </summary>
    Private Sub drawMessages()
        Dim srcFile As String = Request.MapPath(ConfigurationManager.AppSettings("MessagesFilePath"))
        Dim destFile As String = Request.MapPath(ConfigurationManager.AppSettings("MessagesTempFilePath"))
        Dim messagesLine As String = [String].Empty
        receiveMessagePanel.Controls.Clear()
        If File.Exists(srcFile) Then
            File.Move(srcFile, destFile)
            Dim secondTable As New Table()
            secondTable.Font.Name = "Sans-serif"
            secondTable.Font.Size = 9
            Dim TableRow As New TableRow()
            secondTable.Font.Size = 8
            secondTable.Width = Unit.Pixel(1000)
            Dim TableCell As New TableCell()
            TableCell.Width = Unit.Pixel(200)
            TableCell.Text = "DateTime"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableCell.Font.Bold = True
            TableRow.Cells.Add(TableCell)
            TableCell = New TableCell()
            TableCell.Font.Bold = True
            TableCell.Width = Unit.Pixel(100)
            TableCell.Wrap = True
            TableCell.Text = "MessageId"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableRow.Cells.Add(TableCell)
            TableCell = New TableCell()
            TableCell.Text = "Message"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableCell.Font.Bold = True
            TableCell.Width = Unit.Pixel(300)
            TableRow.Cells.Add(TableCell)
            TableCell = New TableCell()
            TableCell.Text = "SenderAddress"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableCell.Font.Bold = True
            TableCell.Width = Unit.Pixel(150)
            TableRow.Cells.Add(TableCell)
            TableCell = New TableCell()
            TableCell.Text = "DestinationAddress"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableCell.Font.Bold = True
            TableCell.Width = Unit.Pixel(150)
            TableRow.Cells.Add(TableCell)
            secondTable.Rows.Add(TableRow)
            receiveMessagePanel.Controls.Add(secondTable)
            Using sr As New StreamReader(destFile)
                While sr.Peek() >= 0
                    messagesLine = sr.ReadLine()
                    Dim messageValues As String() = Regex.Split(messagesLine, "_-_-")
                    TableRow = New TableRow()
                    Dim TableCellDateTime As New TableCell()
                    TableCellDateTime.Width = Unit.Pixel(200)
                    TableCellDateTime.Text = messageValues(0)
                    TableCellDateTime.HorizontalAlign = HorizontalAlign.Center
                    Dim TableCellMessageId As New TableCell()
                    TableCellMessageId.Width = Unit.Pixel(100)
                    TableCellMessageId.Wrap = True
                    TableCellMessageId.Text = messageValues(1)
                    TableCellMessageId.HorizontalAlign = HorizontalAlign.Center
                    Dim TableCellMessage As New TableCell()
                    TableCellMessage.Width = Unit.Pixel(300)
                    TableCellMessage.Text = messageValues(2)
                    TableCellMessage.HorizontalAlign = HorizontalAlign.Center
                    Dim TableCellSenderAddress As New TableCell()
                    TableCellSenderAddress.Width = Unit.Pixel(150)
                    TableCellSenderAddress.Text = messageValues(3)
                    TableCellSenderAddress.HorizontalAlign = HorizontalAlign.Center
                    Dim TableCellDestinationAddress As New TableCell()
                    TableCellDestinationAddress.Width = Unit.Pixel(150)
                    TableCellDestinationAddress.Text = messageValues(4)
                    TableCellDestinationAddress.HorizontalAlign = HorizontalAlign.Center
                    TableRow.Cells.Add(TableCellDateTime)
                    TableRow.Cells.Add(TableCellMessageId)
                    TableRow.Cells.Add(TableCellMessage)
                    TableRow.Cells.Add(TableCellSenderAddress)
                    TableRow.Cells.Add(TableCellDestinationAddress)
                    secondTable.Rows.Add(TableRow)
                    Dim msgtxt As String = TableCellMessage.Text.ToString()
                    If msgtxt.Equals("football", StringComparison.CurrentCultureIgnoreCase) OrElse msgtxt.Equals("baseball", StringComparison.CurrentCultureIgnoreCase) OrElse msgtxt.Equals("basketball", StringComparison.CurrentCultureIgnoreCase) Then
                    Else
                        TableCellDateTime.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                        TableCellMessageId.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                        TableCellMessage.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                        TableCellSenderAddress.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                        TableCellDestinationAddress.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                    End If
                End While
                sr.Close()
                File.Delete(destFile)

            End Using
        End If
    End Sub

    ''' <summary>
    ''' This method updates the vote counts by reading from the files
    ''' </summary>
    Private Sub UpdateVoteCount()
        Try
            footballLabel.Text = Me.GetCountFromFile(Me.footballFilePath).ToString()
            baseballLabel.Text = Me.GetCountFromFile(Me.baseballFilePath).ToString()
            basketballLabel.Text = Me.GetCountFromFile(Me.basketballFilePath).ToString()

            Dim totalCount As Integer = Convert.ToInt32(footballLabel.Text) + Convert.ToInt32(baseballLabel.Text) + Convert.ToInt32(basketballLabel.Text)
            Me.DrawPanelForSuccess(statusPanel, totalCount.ToString())
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    ''' <summary>
    ''' This method reads from files and returns the number of messages.
    ''' </summary>
    ''' <param name="filePath">string, Name of the file to read from</param>
    ''' <returns>int, count of messages</returns>
    Private Function GetCountFromFile(ByVal filePath As String) As Integer
        Dim count As Integer = 0
        Using streamReader As StreamReader = File.OpenText(Request.MapPath(filePath))
            count = Convert.ToInt32(streamReader.ReadToEnd())
            streamReader.Close()
        End Using

        Return count
    End Function
#End Region
End Class
