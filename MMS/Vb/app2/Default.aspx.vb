' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System.Collections.Generic
Imports System.Configuration
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Web.Script.Serialization
Imports System.Web.UI
Imports System.Web.UI.WebControls

#End Region

''' <summary>
''' Access Token Types
''' </summary>
Public Enum AccessTokenType
    ''' <summary>
    ''' Access Token Type is based on Client Credential Mode
    ''' </summary>
    Client_Credential

    ''' <summary>
    ''' Access Token Type is based on Authorization Code
    ''' </summary>
    Authorization_Code

    ''' <summary>
    ''' Access Token Type is based on Refresh Token
    ''' </summary>
    Refresh_Token
End Enum

''' <summary>
''' MMS_App2 class
''' </summary>
''' <remarks>
''' This is a server side application which also has a web interface. 
''' The application looks for a file called numbers.txt containing MSISDNs of desired recipients, and an image called coupon.jpg, 
''' and message text from a file called subject.txt, and then sends an MMS message with the attachment to every recipient in the list. 
''' This can be triggered via a command line on the server, or through the web interface, which then displays all the returned mmsIds or respective errors
''' </remarks>
Partial Public Class MMS_App2
    Inherits System.Web.UI.Page
#Region "Instance Variables"
    ''' <summary>
    ''' Instance variables for get status table
    ''' </summary>
    Private getStatusTable As Table
    Private secondTable As Table

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private endPoint As String, accessTokenFilePath As String, messageFilePath As String, phoneListFilePath As String, couponPath As String, couponFileName As String

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private apiKey As String, secretKey As String, authCode As String, accessToken As String, scope As String, expirySeconds As String, _
     refreshToken As String, refreshTokenExpiryTime As String

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private phoneNumbersList As New List(Of String)()

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private invalidPhoneNumbers As New List(Of String)()

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private phoneNumbersParameter As String = Nothing

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private accessTokenExpiryTime As DateTime

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

#End Region

#Region "Bypass SSL Certificate Error"

    ''' <summary>
    ''' This method neglects the ssl handshake error with authentication server
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
    ''' Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Dim streamReader As StreamReader = Nothing
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)

            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

            Me.ReadConfigFile()

            If Not Page.IsPostBack Then
                streamReader = New StreamReader(Request.MapPath(Me.phoneListFilePath))
                phoneListTextBox.Text = streamReader.ReadToEnd()
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMMSPanel, ex.ToString())
        Finally
            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If
        End Try
    End Sub

    ''' <summary>
    ''' This method will be called when user clicks on send mms button
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub SendButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim ableToGetNumbers As Boolean = Me.GetPhoneNumbers()
        If ableToGetNumbers = False Then
            'this.DrawPanelForFailure(sendMMSPanel, "Specify phone numbers to send");
            Return
        End If

        If Me.ReadAndGetAccessToken() = False Then
            Return
        End If

        Me.SendMMS()
    End Sub

    ''' <summary>
    ''' This method will be called when user clicks on  get status button
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub StatusButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If String.IsNullOrEmpty(msgIdLabel.Text) Then
                Return
            End If

            If Me.ReadAndGetAccessToken() = False Then
                Return
            End If

            Dim mmsId As String = msgIdLabel.Text
            Dim mmsDeliveryStatus As String

            Dim mmsStatusRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/mms/2/messaging/outbox/" & mmsId), HttpWebRequest)
            mmsStatusRequestObject.Headers.Add("Authorization", "Bearer " & Me.accessToken)
            mmsStatusRequestObject.Method = "GET"

            Dim mmsStatusResponseObject As HttpWebResponse = DirectCast(mmsStatusRequestObject.GetResponse(), HttpWebResponse)
            Using mmsStatusResponseStream As New StreamReader(mmsStatusResponseObject.GetResponseStream())
                mmsDeliveryStatus = mmsStatusResponseStream.ReadToEnd()
                mmsDeliveryStatus = mmsDeliveryStatus.Replace("-", String.Empty)
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim status As GetDeliveryStatus = DirectCast(deserializeJsonObject.Deserialize(mmsDeliveryStatus, GetType(GetDeliveryStatus)), GetDeliveryStatus)
                Dim dinfoList As DeliveryInfoList = status.DeliveryInfoList

                Me.DrawPanelForGetStatusResult(Nothing, Nothing, Nothing, True)

                For Each deliveryInfo As DeliveryInfoRaw In dinfoList.DeliveryInfo
                    Me.DrawPanelForGetStatusResult(deliveryInfo.Id, deliveryInfo.Address, deliveryInfo.DeliveryStatus, False)
                Next

                msgIdLabel.Text = String.Empty
                mmsStatusResponseStream.Close()
            End Using
        Catch we As WebException
            Dim errorResponse As String = String.Empty

            Try
                Using sr2 As New StreamReader(we.Response.GetResponseStream())
                    errorResponse = sr2.ReadToEnd()
                    sr2.Close()
                End Using
            Catch
                errorResponse = "Unable to get response"
            End Try
            Me.DrawPanelForFailure(statusPanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        End Try
    End Sub

#End Region

#Region "Access Token Methods"

    ''' <summary>
    ''' This function reads access token file, validates the access token and gets a new access token
    ''' </summary>
    ''' <returns>true if access token is valid, or else false is returned</returns>
    Private Function ReadAndGetAccessToken() As Boolean
        Dim ableToGetToken As Boolean = True

        If Me.ReadAccessTokenFile() = False Then
            ableToGetToken = Me.GetAccessToken(AccessTokenType.Client_Credential)
        Else
            Dim tokenValidity As String = Me.IsTokenValid()

            If tokenValidity.Equals("REFRESH_TOKEN") Then
                ableToGetToken = Me.GetAccessToken(AccessTokenType.Refresh_Token)
            ElseIf tokenValidity.Equals("INVALID_ACCESS_TOKEN") Then
                ableToGetToken = Me.GetAccessToken(AccessTokenType.Client_Credential)
            End If
        End If

        Return ableToGetToken
    End Function

    ''' <summary>
    ''' This function reads the Access Token File and stores the values of access token, expiry seconds, refresh token, 
    ''' last access token time and refresh token expiry time. 
    ''' </summary>
    ''' <returns>true, if access token file and all others attributes read successfully otherwise returns false</returns>
    Private Function ReadAccessTokenFile() As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamReader As StreamReader = Nothing
        Dim ableToRead As Boolean = True
        Try
            fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read)
            streamReader = New StreamReader(fileStream)
            Me.accessToken = streamReader.ReadLine()
            Me.expirySeconds = streamReader.ReadLine()
            Me.refreshToken = streamReader.ReadLine()
            Me.accessTokenExpiryTime = Convert.ToDateTime(streamReader.ReadLine())
            Me.refreshTokenExpiryTime = streamReader.ReadLine()
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMMSPanel, ex.ToString())
            ableToRead = False
        Finally
            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

        If Me.accessToken Is Nothing OrElse Me.expirySeconds Is Nothing OrElse Me.refreshToken Is Nothing OrElse Me.refreshTokenExpiryTime Is Nothing Then
            ableToRead = False
        End If

        Return ableToRead
    End Function

    ''' <summary>
    ''' Validates he expiry of the access token and refresh token
    ''' </summary>
    ''' <returns>string, returns VALID_ACCESS_TOKEN if its valid
    ''' otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    ''' return REFRESH_TOKEN, if access token in expired and refresh token is valid</returns>
    Private Function IsTokenValid() As String
        If Me.accessToken Is Nothing Then
            Return "INVALID_ACCESS_TOKEN"
        End If

        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            If currentServerTime >= Me.accessTokenExpiryTime Then
                If currentServerTime >= DateTime.Parse(Me.refreshTokenExpiryTime) Then
                    Return "INVALID_ACCESS_TOKEN"
                Else
                    Return "REFRESH_TOKEN"
                End If
            Else
                Return "VALID_ACCESS_TOKEN"
            End If
        Catch
            Return "INVALID_ACCESS_TOKEN"
        End Try
    End Function

    ''' <summary>
    ''' This method gets access token based on either client credentials mode or refresh token.
    ''' </summary>
    ''' <param name="type">AccessTokenType; either Client_Credential or Refresh_Token</param>
    ''' <returns>true/false; true if able to get access token, else false</returns>
    Private Function GetAccessToken(ByVal type As AccessTokenType) As Boolean
        Dim postStream As Stream = Nothing
        Dim streamWriter As StreamWriter = Nothing
        Dim fileStream As FileStream = Nothing
        Try

            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()

            Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/token")
            accessTokenRequest.Method = "POST"
            Dim oauthParameters As String = String.Empty
            If type = AccessTokenType.Client_Credential Then
                oauthParameters = "client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=" & Me.scope
            Else
                oauthParameters = "grant_type=refresh_token&client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&refresh_token=" & Me.refreshToken
            End If

            accessTokenRequest.ContentType = "application/x-www-form-urlencoded"
            Dim encoding As New UTF8Encoding()
            Dim postBytes As Byte() = encoding.GetBytes(oauthParameters)
            accessTokenRequest.ContentLength = postBytes.Length

            postStream = accessTokenRequest.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)
            postStream.Close()

            Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
            Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                Dim jsonAccessToken As String = accessTokenResponseStream.ReadToEnd().ToString()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(jsonAccessToken, GetType(AccessTokenResponse)), AccessTokenResponse)

                Me.accessToken = deserializedJsonObj.access_token.ToString()
                Me.expirySeconds = deserializedJsonObj.expires_in.ToString()
                Me.refreshToken = deserializedJsonObj.refresh_token.ToString()
                Me.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in.ToString()))

                Dim refreshExpiry As DateTime = currentServerTime.AddHours(Me.refreshTokenExpiresIn)

                If deserializedJsonObj.expires_in.Equals("0") Then
                    Dim defaultAccessTokenExpiresIn As Integer = 100
                    ' In Years
                    Me.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn)
                End If

                Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()

                fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write)
                streamWriter = New StreamWriter(fileStream)

                streamWriter.WriteLine(Me.accessToken)
                streamWriter.WriteLine(Me.expirySeconds)
                streamWriter.WriteLine(Me.refreshToken)
                streamWriter.WriteLine(Me.accessTokenExpiryTime.ToString())
                streamWriter.WriteLine(Me.refreshTokenExpiryTime)

                ' Close and clean up the StreamReader
                accessTokenResponseStream.Close()
                Return True
            End Using
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMMSPanel, ex.ToString())
            Return False
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If

            If streamWriter IsNot Nothing Then
                streamWriter.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

        Return False
    End Function

#End Region

#Region "Display Status message methods"
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
        table.CssClass = "successWide"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOneCellOne.Width = Unit.Pixel(75)
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)

        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Font.Bold = True
        rowTwoCellOne.Text = "Message ID:"
        rowTwoCellOne.Width = Unit.Pixel(75)
        rowTwo.Controls.Add(rowTwoCellOne)

        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellTwo.Text = message
        rowTwoCellTwo.HorizontalAlign = HorizontalAlign.Left
        rowTwo.Controls.Add(rowTwoCellTwo)
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

    ''' <summary>
    ''' This function draws table for failed numbers
    ''' </summary>
    ''' <param name="panelParam">Panel to draw error</param>
    Private Sub DrawPanelForFailedNumbers(ByVal panelParam As Panel)
        'if (panelParam.HasControls())
        '{
        '   panelParam.Controls.Clear();
        '}

        Dim table As New Table()
        table.CssClass = "errorWide"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR: Invalid numbers"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)

        For Each number As String In Me.invalidPhoneNumbers
            Dim rowTwo As New TableRow()
            Dim rowTwoCellOne As New TableCell()
            rowTwoCellOne.Text = number.ToString()
            rowTwo.Controls.Add(rowTwoCellOne)
            table.Controls.Add(rowTwo)
        Next

        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' This method draws table for get status response
    ''' </summary>
    ''' <param name="msgid">string, Message Id</param>
    ''' <param name="phone">string, phone number</param>
    ''' <param name="status">string, status</param>
    ''' <param name="headerFlag">bool, headerFlag</param>
    Private Sub DrawPanelForGetStatusResult(ByVal msgid As String, ByVal phone As String, ByVal status As String, ByVal headerFlag As Boolean)
        If headerFlag = True Then
            getStatusTable = New Table()
            getStatusTable.CssClass = "successWide"
            getStatusTable.Font.Name = "Sans-serif"
            getStatusTable.Font.Size = 9

            Dim rowOne As New TableRow()
            Dim rowOneCellOne As New TableCell()
            rowOneCellOne.Width = Unit.Pixel(110)
            rowOneCellOne.Font.Bold = True
            rowOneCellOne.Text = "SUCCESS:"
            rowOne.Controls.Add(rowOneCellOne)
            getStatusTable.Controls.Add(rowOne)
            Dim rowTwo As New TableRow()
            Dim rowTwoCellOne As New TableCell()
            rowTwoCellOne.Width = Unit.Pixel(250)
            rowTwoCellOne.Text = "Messages Delivered"

            rowTwo.Controls.Add(rowTwoCellOne)
            getStatusTable.Controls.Add(rowTwo)
            getStatusTable.Controls.Add(rowOne)
            getStatusTable.Controls.Add(rowTwo)
            statusPanel.Controls.Add(getStatusTable)

            secondTable = New Table()
            secondTable.Font.Name = "Sans-serif"
            secondTable.Font.Size = 9
            secondTable.Width = Unit.Pixel(650)
            Dim tableRow As New TableRow()
            Dim tableCell As New TableCell()
            tableCell.Width = Unit.Pixel(300)
            tableCell.Text = "Recipient"
            tableCell.HorizontalAlign = HorizontalAlign.Center
            tableCell.Font.Bold = True
            tableRow.Cells.Add(tableCell)
            tableCell = New TableCell()
            tableCell.Font.Bold = True
            tableCell.Width = Unit.Pixel(300)
            tableCell.Wrap = True
            tableCell.Text = "Status"
            tableCell.HorizontalAlign = HorizontalAlign.Center
            tableRow.Cells.Add(tableCell)
            secondTable.Rows.Add(tableRow)
            statusPanel.Controls.Add(secondTable)
        Else
            Dim row As New TableRow()
            Dim cell1 As New TableCell()
            Dim cell2 As New TableCell()
            cell1.Text = phone.ToString()
            cell1.Width = Unit.Pixel(300)
            cell1.HorizontalAlign = HorizontalAlign.Center
            row.Controls.Add(cell1)
            cell2.Text = status.ToString()
            cell2.Width = Unit.Pixel(300)
            cell2.HorizontalAlign = HorizontalAlign.Center
            row.Controls.Add(cell2)
            secondTable.Controls.Add(row)
            statusPanel.Controls.Add(secondTable)
        End If
    End Sub

#End Region

#Region "MMS App2 specific methods"

    ''' <summary>
    ''' This method validates the given string as valid msisdn
    ''' </summary>
    ''' <param name="number">string, phone number</param>
    ''' <returns>true/false; true if valid phone number, else false</returns>
    Private Function IsValidMISDN(ByVal number As String) As Boolean
        Dim smsAddressInput As String = number
        Dim smsAddressFormatted As String
        Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"

        If System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
            smsAddressFormatted = smsAddressInput.Replace("-", String.Empty)
        Else
            smsAddressFormatted = smsAddressInput
        End If

        Dim smsAddressForRequest As String = smsAddressFormatted.ToString()
        Dim tryParseResult As Long = 0
        If smsAddressFormatted.Length = 16 AndAlso smsAddressFormatted.StartsWith("tel:+1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(6, 10)
        ElseIf smsAddressFormatted.Length = 15 AndAlso smsAddressFormatted.StartsWith("tel:1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(5, 10)
        ElseIf smsAddressFormatted.Length = 14 AndAlso smsAddressFormatted.StartsWith("tel:") Then
            smsAddressFormatted = smsAddressFormatted.Substring(4, 10)
        ElseIf smsAddressFormatted.Length = 12 AndAlso smsAddressFormatted.StartsWith("+1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(2, 10)
        ElseIf smsAddressFormatted.Length = 11 AndAlso smsAddressFormatted.StartsWith("1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(1, 10)
        End If

        If (smsAddressFormatted.Length <> 10) OrElse (Not Long.TryParse(smsAddressFormatted, tryParseResult)) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' This method reads config file and assigns values to local variables
    ''' </summary>
    ''' <returns>true/false, true- if able to read from config file</returns>
    Private Function ReadConfigFile() As Boolean
        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(sendMMSPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(sendMMSPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(sendMMSPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "MMS"
        End If

        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "MMSApp1AccessToken.txt"
        End If

        Me.messageFilePath = ConfigurationManager.AppSettings("messageFilePath")
        If String.IsNullOrEmpty(Me.messageFilePath) Then
            Me.DrawPanelForFailure(sendMMSPanel, "Message file path is missing in configuration file")
            Return False
        End If

        Me.phoneListFilePath = ConfigurationManager.AppSettings("phoneListFilePath")
        If String.IsNullOrEmpty(Me.phoneListFilePath) Then
            Me.DrawPanelForFailure(sendMMSPanel, "Phone list file path is missing in configuration file")
            Return False
        End If

        Me.couponPath = ConfigurationManager.AppSettings("couponPath")
        If String.IsNullOrEmpty(Me.couponPath) Then
            Me.DrawPanelForFailure(sendMMSPanel, "Coupon path is missing in configuration file")
            Return False
        End If

        Me.couponFileName = ConfigurationManager.AppSettings("couponFileName")
        If String.IsNullOrEmpty(Me.couponFileName) Then
            Me.DrawPanelForFailure(sendMMSPanel, "Coupon file name is missing in configuration file")
            Return False
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        Dim dirInfo As New DirectoryInfo(Request.MapPath(Me.couponPath))
        Dim imgFileList As FileInfo() = dirInfo.GetFiles()
        Dim fileindex As Integer = 0
        Dim foundFlag As Boolean = False
        For Each tempFileInfo As FileInfo In imgFileList
            If tempFileInfo.Name.ToLower().Equals(Me.couponFileName.ToLower()) Then
                foundFlag = True
                Exit For
            Else
                fileindex += 1
            End If
        Next

        If foundFlag = False Then
            Me.DrawPanelForFailure(sendMMSPanel, "Coupon doesnt exists")
            Return False
        End If

        Image1.ImageUrl = String.Format("{0}{1}", Me.couponPath, imgFileList(fileindex).Name)

        Dim streamReader As StreamReader = Nothing
        Try
            streamReader = New StreamReader(Request.MapPath(Me.messageFilePath))
            subjectLabel.Text = streamReader.ReadToEnd()
        Catch ex As Exception
            Throw ex
        Finally
            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If
        End Try

        Return True
    End Function

    ''' <summary>
    ''' Sends MMS message by invoking Send MMS api
    ''' </summary>
    Private Sub SendMMS()
        Dim postStream As Stream = Nothing
        Dim fileStream As FileStream = Nothing
        Dim binaryReader As BinaryReader = Nothing
        Try
            Dim mmsFilePath As String = Request.MapPath(Me.couponPath)
            Dim boundary As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

            Dim mmsRequestObject As HttpWebRequest = DirectCast(WebRequest.Create(String.Empty & Me.endPoint & "/rest/mms/2/messaging/outbox"), HttpWebRequest)
            mmsRequestObject.Headers.Add("Authorization", "Bearer " & Me.accessToken)
            mmsRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" & boundary & """" & vbCr & vbLf
            mmsRequestObject.Method = "POST"
            mmsRequestObject.KeepAlive = True

            Dim encoding As New UTF8Encoding()
            Dim postBytes As Byte() = encoding.GetBytes(String.Empty)
            Dim sendMMSData As String = Me.phoneNumbersParameter & "&Subject=" & Server.UrlEncode(subjectLabel.Text.ToString())
            Dim data As String = String.Empty

            fileStream = New FileStream(mmsFilePath & Me.couponFileName, FileMode.Open, FileAccess.Read)
            binaryReader = New BinaryReader(fileStream)
            Dim image As Byte() = binaryReader.ReadBytes(CInt(fileStream.Length))

            data += "--" & boundary & vbCr & vbLf
            data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding:8bit" & vbCr & vbLf & "Content-ID:<startpart>" & vbCr & vbLf & vbCr & vbLf & sendMMSData & vbCr & vbLf
            data += "--" & boundary & vbCr & vbLf
            data += "Content-Disposition:attachment;name=""" & "coupon.jpg" & """" & vbCr & vbLf
            data += "Content-Type:image/gif" & vbCr & vbLf
            data += "Content-ID:<" & "coupon.jpg" & ">" & vbCr & vbLf
            data += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf
            Dim firstPart As Byte() = encoding.GetBytes(data)
            Dim newSize As Integer = firstPart.Length + image.Length

            Dim memoryStream = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
            memoryStream.Write(firstPart, 0, firstPart.Length)
            memoryStream.Write(image, 0, image.Length)

            Dim secondpart As Byte() = memoryStream.GetBuffer()
            Dim thirdpart As Byte() = encoding.GetBytes(vbCr & vbLf & "--" & boundary & "--" & vbCr & vbLf)
            newSize = secondpart.Length + thirdpart.Length

            Dim memoryStream2 = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
            memoryStream2.Write(secondpart, 0, secondpart.Length)
            memoryStream2.Write(thirdpart, 0, thirdpart.Length)

            postBytes = memoryStream2.GetBuffer()
            mmsRequestObject.ContentLength = postBytes.Length

            postStream = mmsRequestObject.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)
            postStream.Close()

            Dim mmsResponseObject As WebResponse = mmsRequestObject.GetResponse()
            Using streamReader As New StreamReader(mmsResponseObject.GetResponseStream())
                Dim mmsResponseData As String = streamReader.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As MmsResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(MmsResponseId)), MmsResponseId)

                msgIdLabel.Text = deserializedJsonObj.Id
                Me.DrawPanelForSuccess(sendMMSPanel, deserializedJsonObj.Id)
                streamReader.Close()
            End Using
            'if (this.invalidPhoneNumbers.Count > 0)
            '            {
            '                this.DrawPanelForFailedNumbers(sendMMSPanel);
            '            }

            mmsRequestObject = Nothing
        Catch we As WebException
            Dim errorResponse As String = String.Empty

            Try
                Using sr2 As New StreamReader(we.Response.GetResponseStream())
                    errorResponse = sr2.ReadToEnd()
                    sr2.Close()
                End Using
            Catch
                errorResponse = "Unable to get response"
            End Try
            Me.DrawPanelForFailure(sendMMSPanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMMSPanel, ex.ToString())
        Finally
            If binaryReader IsNot Nothing Then
                binaryReader.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If

            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try
    End Sub

    ''' <summary>
    ''' This method gets the phone numbers present in phonenumber text box and validates each phone number and prepares valid and invalid phone number lists
    ''' and returns a bool value indicating if able to get the phone numbers.
    ''' </summary>
    ''' <returns>true/false; true if able to get valis phone numbers, else false</returns>
    Private Function GetPhoneNumbers() As Boolean
        If String.IsNullOrEmpty(phoneListTextBox.Text) Then
            Return False
        End If

        Dim phoneNumbers As String() = phoneListTextBox.Text.Split(","c)
        For Each phoneNum As String In phoneNumbers
            If Not String.IsNullOrEmpty(phoneNum) Then
                Me.phoneNumbersList.Add(phoneNum)
            End If
        Next

        For Each phoneNo As String In Me.phoneNumbersList
            If Me.IsValidMISDN(phoneNo) = True Then
                If phoneNo.StartsWith("tel:") Then
                    Dim phoneNumberWithoutHyphens As String = phoneNo.Replace("-", String.Empty)
                    Me.phoneNumbersParameter = Me.phoneNumbersParameter & "Address=" & Server.UrlEncode(phoneNumberWithoutHyphens.ToString()) & "&"
                Else
                    Dim phoneNumberWithoutHyphens As String = phoneNo.Replace("-", String.Empty)
                    Me.phoneNumbersParameter = Me.phoneNumbersParameter & "Address=" & Server.UrlEncode("tel:" & phoneNumberWithoutHyphens.ToString()) & "&"
                End If
            Else
                Me.invalidPhoneNumbers.Add(phoneNo)
            End If
        Next

        'if (string.IsNullOrEmpty(this.phoneNumbersParameter))
        '{
        If Me.invalidPhoneNumbers.Count > 0 Then
            Me.DrawPanelForFailedNumbers(sendMMSPanel)
            Return False
        End If
        'return false;
        ' }

        Return True
    End Function

#End Region
End Class

#Region "Data Structures"

''' <summary>
''' MmsResponseId object
''' </summary>
Public Class MmsResponseId
    ''' <summary>
    ''' Gets or sets the value of Id
    ''' </summary>
    Public Property Id() As String
        Get
            Return m_Id
        End Get
        Set(ByVal value As String)
            m_Id = Value
        End Set
    End Property
    Private m_Id As String

    ''' <summary>
    ''' Gets or sets the value of Resource Reference
    ''' </summary>
    Public Property ResourceReference() As ResourceReferenceRaw
        Get
            Return m_ResourceReference
        End Get
        Set(ByVal value As ResourceReferenceRaw)
            m_ResourceReference = Value
        End Set
    End Property
    Private m_ResourceReference As ResourceReferenceRaw
End Class

''' <summary>
''' ResourceReferenceRaw object
''' </summary>
Public Class ResourceReferenceRaw
    ''' <summary>
    ''' Gets or sets the value of ResourceUrl
    ''' </summary>
    Public Property ResourceUrl() As String
        Get
            Return m_ResourceUrl
        End Get
        Set(ByVal value As String)
            m_ResourceUrl = Value
        End Set
    End Property
    Private m_ResourceUrl As String
End Class

''' <summary>
''' AccessTokenResponse Object
''' </summary>
Public Class AccessTokenResponse
    ''' <summary>
    ''' Gets or sets the value of access_token
    ''' </summary>
    Public Property access_token() As String
        Get
            Return m_access_token
        End Get
        Set(ByVal value As String)
            m_access_token = Value
        End Set
    End Property
    Private m_access_token As String

    ''' <summary>
    ''' Gets or sets the value of refresh_token
    ''' </summary>
    Public Property refresh_token() As String
        Get
            Return m_refresh_token
        End Get
        Set(ByVal value As String)
            m_refresh_token = Value
        End Set
    End Property
    Private m_refresh_token As String

    ''' <summary>
    ''' Gets or sets the value of expires_in
    ''' </summary>
    Public Property expires_in() As String
        Get
            Return m_expires_in
        End Get
        Set(ByVal value As String)
            m_expires_in = Value
        End Set
    End Property
    Private m_expires_in As String
End Class

''' <summary>
''' GetDeliveryStatus object
''' </summary>
Public Class GetDeliveryStatus
    ''' <summary>
    ''' Gets or sets the value of DeliveryInfoList object
    ''' </summary>
    Public Property DeliveryInfoList() As DeliveryInfoList
        Get
            Return m_DeliveryInfoList
        End Get
        Set(ByVal value As DeliveryInfoList)
            m_DeliveryInfoList = Value
        End Set
    End Property
    Private m_DeliveryInfoList As DeliveryInfoList
End Class

''' <summary>
''' DeliveryInfoList object
''' </summary>
Public Class DeliveryInfoList
    ''' <summary>
    ''' Gets or sets the value of Resource Url
    ''' </summary>
    Public Property ResourceUrl() As String
        Get
            Return m_ResourceUrl
        End Get
        Set(ByVal value As String)
            m_ResourceUrl = Value
        End Set
    End Property
    Private m_ResourceUrl As String

    ''' <summary>
    ''' Gets or sets the value of DeliveryInfo List
    ''' </summary>
    Public Property DeliveryInfo() As List(Of DeliveryInfoRaw)
        Get
            Return m_DeliveryInfo
        End Get
        Set(ByVal value As List(Of DeliveryInfoRaw))
            m_DeliveryInfo = Value
        End Set
    End Property
    Private m_DeliveryInfo As List(Of DeliveryInfoRaw)
End Class

''' <summary>
''' DeliveryInfoRaw Object
''' </summary>
Public Class DeliveryInfoRaw
    ''' <summary>
    ''' Gets or sets the value of Id
    ''' </summary>
    Public Property Id() As String
        Get
            Return m_Id
        End Get
        Set(ByVal value As String)
            m_Id = Value
        End Set
    End Property
    Private m_Id As String

    ''' <summary>
    ''' Gets or sets the value of Address
    ''' </summary>
    Public Property Address() As String
        Get
            Return m_Address
        End Get
        Set(ByVal value As String)
            m_Address = Value
        End Set
    End Property
    Private m_Address As String

    ''' <summary>
    ''' Gets or sets the value of DeliveryStatus
    ''' </summary>
    Public Property DeliveryStatus() As String
        Get
            Return m_DeliveryStatus
        End Get
        Set(ByVal value As String)
            m_DeliveryStatus = Value
        End Set
    End Property
    Private m_DeliveryStatus As String
End Class

#End Region
