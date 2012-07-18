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
Imports System.Linq
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Xml

#End Region

''' <summary>
''' Default class
''' </summary>
Partial Public Class SMS_App1
    Inherits System.Web.UI.Page
#Region "Variable Declaration"
    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private shortCode As String, endPoint As String, accessTokenFilePath As String, apiKey As String, secretKey As String, accessToken As String, _
     accessTokenExpiryTime As String, scope As String, refreshToken As String, refreshTokenExpiryTime As String

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private shortCodes As String()

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

    ''' <summary>
    ''' Access Token Types
    ''' </summary>
    Private Enum AccessType
        ''' <summary>
        ''' Access Token Type is based on Client Credential Mode
        ''' </summary>
        ClientCredential

        ''' <summary>
        ''' Access Token Type is based on Refresh Token
        ''' </summary>
        RefreshToken
    End Enum
#End Region

#Region "SMS Application Events"
    ''' <summary>
    ''' This function is called when the applicaiton page is loaded into the browser.
    ''' This fucntion reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

            Dim ableToRead As Boolean = Me.ReadConfigFile()
            If ableToRead = False Then
                Return
            End If

            Me.shortCodes = Me.shortCode.Split(";"c)
            Me.shortCode = Me.shortCodes(0)
            Dim table As New Table()
            table.Font.Size = 8
            For Each srtCode As String In Me.shortCodes
                Dim button As New Button()
                AddHandler button.Click, New EventHandler(AddressOf Me.GetMessagesButton_Click)
                button.Text = "Get Messages for " & srtCode
                Dim rowOne As New TableRow()
                Dim rowOneCellOne As New TableCell()
                rowOne.Controls.Add(rowOneCellOne)
                rowOneCellOne.Controls.Add(button)
                table.Controls.Add(rowOne)
            Next

            receiveMessagePanel.Controls.Add(table)
        Catch ex As Exception
            Me.DrawPanelForFailure(sendSMSPanel, ex.ToString())
            Response.Write(ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Reads from config file
    ''' </summary>
    ''' <returns>true/false; true if able to read else false</returns>
    Private Function ReadConfigFile() As Boolean
        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "~\SMSApp1AccessToken.txt"
        End If

        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(sendSMSPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.shortCode = ConfigurationManager.AppSettings("short_code")
        If String.IsNullOrEmpty(Me.shortCode) Then
            Me.DrawPanelForFailure(sendSMSPanel, "short_code is not defined in configuration file")
            Return False
        End If

        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(sendSMSPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(sendSMSPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "SMS"
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            ' Default value
            Me.refreshTokenExpiresIn = 24
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function is called with user clicks on send SMS
    ''' This validates the access token and then calls sendSMS method to invoke send SMS API.
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnSendSMS_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If Me.ReadAndGetAccessToken(sendSMSPanel) = True Then
                Me.SendSms()
            Else
                Me.DrawPanelForFailure(sendSMSPanel, "Unable to get access token.")
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(sendSMSPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This method is called when user clicks on get delivery status button
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub GetDeliveryStatusButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If Me.ReadAndGetAccessToken(getStatusPanel) = True Then
                Me.GetSmsDeliveryStatus()
            Else
                Me.DrawPanelForFailure(getStatusPanel, "Unable to get access token.")
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(getStatusPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This method is called when user clicks on get message button
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub GetMessagesButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If Me.ReadAndGetAccessToken(getMessagePanel) = True Then
                Dim button As Button = TryCast(sender, Button)
                Dim buttonCaption As String = button.Text.ToString()
                Me.shortCode = buttonCaption.Replace("Get Messages for ", String.Empty)
                Me.RecieveSms()
            Else
                Me.DrawPanelForFailure(getMessagePanel, "Unable to get access token.")
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(getMessagePanel, ex.ToString())
        End Try
    End Sub
#End Region

#Region "SMS Application related functions"
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

    ''' <summary>
    ''' This function reads the Access Token File and stores the values of access token, expiry seconds
    ''' refresh token, last access token time and refresh token expiry time
    ''' This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <returns>Returns boolean</returns>    
    Private Function ReadAccessTokenFile(ByVal panelParam As Panel) As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamReader As StreamReader = Nothing
        Try
            fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read)
            streamReader = New StreamReader(fileStream)
            Me.accessToken = streamReader.ReadLine()
            Me.accessTokenExpiryTime = streamReader.ReadLine()
            Me.refreshToken = streamReader.ReadLine()
            Me.refreshTokenExpiryTime = streamReader.ReadLine()
        Catch ex As Exception
            Me.DrawPanelForFailure(panelParam, ex.Message)
            Return False
        Finally
            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

        If (Me.accessToken Is Nothing) OrElse (Me.accessTokenExpiryTime Is Nothing) OrElse (Me.refreshToken Is Nothing) OrElse (Me.refreshTokenExpiryTime Is Nothing) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function validates the expiry of the access token and refresh token,
    ''' function compares the current time with the refresh token taken time, if current time is greater then 
    ''' returns INVALID_REFRESH_TOKEN
    ''' function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    ''' funciton returns INVALID_ACCESS_TOKEN
    ''' otherwise returns VALID_ACCESS_TOKEN
    ''' </summary>
    ''' <returns>Return String</returns>
    Private Function IsTokenValid() As String
        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            If currentServerTime >= DateTime.Parse(Me.accessTokenExpiryTime) Then
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
    ''' This function is used to read access token file and validate the access token
    ''' this function returns true if access token is valid, or else false is returned
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <returns>Returns Boolean</returns>
    Private Function ReadAndGetAccessToken(ByVal panelParam As Panel) As Boolean
        Dim result As Boolean = True
        If Me.ReadAccessTokenFile(panelParam) = False Then
            result = Me.GetAccessToken(AccessType.ClientCredential, panelParam)
        Else
            Dim tokenValidity As String = Me.IsTokenValid()
            If tokenValidity = "REFRESH_TOKEN" Then
                result = Me.GetAccessToken(AccessType.RefreshToken, panelParam)
            ElseIf String.Compare(tokenValidity, "INVALID_ACCESS_TOKEN") = 0 Then
                result = Me.GetAccessToken(AccessType.ClientCredential, panelParam)
            End If
        End If

        If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
            Return False
        Else
            Return result
        End If
    End Function

    ''' <summary>
    ''' This function get the access token based on the type parameter type values.
    ''' If type value is 1, access token is fetch for client credential flow
    ''' If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    ''' </summary>
    ''' <param name="type">Type as integer</param>
    ''' <param name="panelParam">Panel details</param>
    ''' <returns>Return boolean</returns>
    Private Function GetAccessToken(ByVal type As AccessType, ByVal panelParam As Panel) As Boolean
        Dim fileStream As FileStream = Nothing
        Dim postStream As Stream = Nothing
        Dim streamWriter As StreamWriter = Nothing

        ' This is client credential flow
        If type = AccessType.ClientCredential Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()

                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/token")
                accessTokenRequest.Method = "POST"
                Dim oauthParameters As String = String.Empty
                If type = AccessType.ClientCredential Then
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

                Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
                Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                    Dim jsonAccessToken As String = accessTokenResponseStream.ReadToEnd().ToString()
                    Dim deserializeJsonObject As New JavaScriptSerializer()

                    Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(jsonAccessToken, GetType(AccessTokenResponse)), AccessTokenResponse)

                    Me.accessToken = deserializedJsonObj.access_token
                    Me.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString()
                    Me.refreshToken = deserializedJsonObj.refresh_token

                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(Me.refreshTokenExpiresIn)

                    If deserializedJsonObj.expires_in.Equals("0") Then
                        Dim defaultAccessTokenExpiresIn As Integer = 100
                        ' In Yearsint yearsToAdd = 100;
                        Me.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongDateString() & " " & currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongTimeString()
                    End If

                    Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()

                    fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write)
                    streamWriter = New StreamWriter(fileStream)
                    streamWriter.WriteLine(Me.accessToken)
                    streamWriter.WriteLine(Me.accessTokenExpiryTime)
                    streamWriter.WriteLine(Me.refreshToken)
                    streamWriter.WriteLine(Me.refreshTokenExpiryTime)

                    ' Close and clean up the StreamReader
                    accessTokenResponseStream.Close()
                    Return True
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

                Me.DrawPanelForFailure(panelParam, errorResponse & Environment.NewLine & we.ToString())
            Catch ex As Exception
                Me.DrawPanelForFailure(panelParam, ex.Message)
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
        ElseIf type = AccessType.RefreshToken Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()

                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/token")
                accessTokenRequest.Method = "POST"

                Dim oauthParameters As String = "grant_type=refresh_token&client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&refresh_token=" & Me.refreshToken
                accessTokenRequest.ContentType = "application/x-www-form-urlencoded"

                Dim encoding As New UTF8Encoding()
                Dim postBytes As Byte() = encoding.GetBytes(oauthParameters)
                accessTokenRequest.ContentLength = postBytes.Length

                postStream = accessTokenRequest.GetRequestStream()
                postStream.Write(postBytes, 0, postBytes.Length)

                Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
                Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                    Dim accessTokenJSon As String = accessTokenResponseStream.ReadToEnd().ToString()
                    Dim deserializeJsonObject As New JavaScriptSerializer()

                    Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(accessTokenJSon, GetType(AccessTokenResponse)), AccessTokenResponse)
                    Me.accessToken = deserializedJsonObj.access_token.ToString()
                    Dim accessTokenExpiryTime As DateTime = currentServerTime.AddMilliseconds(Convert.ToDouble(deserializedJsonObj.expires_in.ToString()))
                    Me.refreshToken = deserializedJsonObj.refresh_token.ToString()

                    fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write)
                    streamWriter = New StreamWriter(fileStream)
                    streamWriter.WriteLine(Me.accessToken)
                    streamWriter.WriteLine(Me.accessTokenExpiryTime)
                    streamWriter.WriteLine(Me.refreshToken)

                    ' Refresh token valids for 24 hours
                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(24)
                    Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()
                    streamWriter.WriteLine(refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString())

                    accessTokenResponseStream.Close()
                    Return True
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

                Me.DrawPanelForFailure(panelParam, errorResponse & Environment.NewLine & we.ToString())
            Catch ex As Exception
                Me.DrawPanelForFailure(panelParam, ex.Message)
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
        End If

        Return False
    End Function

    ''' <summary>
    ''' This function validates the input fields and if they are valid send sms api is invoked
    ''' </summary>
    Private Sub SendSms()
        Try
            Dim smsAddressInput As String = txtmsisdn.Text.ToString()
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
                Me.DrawPanelForFailure(sendSMSPanel, "Invalid phone number: " & smsAddressInput)
            Else
                Dim smsMessage As String = txtmsg.Text.ToString()
                If smsMessage Is Nothing OrElse smsMessage.Length <= 0 Then
                    Me.DrawPanelForFailure(sendSMSPanel, "Message is null or empty")
                    Return
                End If

                Dim sendSmsResponseData As String
                '''/ HttpWebRequest sendSmsRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/sms/2/messaging/outbox?access_token=" + this.access_token.ToString());
                Dim sendSmsRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/sms/2/messaging/outbox"), HttpWebRequest)
                Dim strReq As String = "{'Address':'tel:" & smsAddressFormatted & "','Message':'" & smsMessage & "'}"
                sendSmsRequestObject.Method = "POST"
                sendSmsRequestObject.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                sendSmsRequestObject.ContentType = "application/json"
                sendSmsRequestObject.Accept = "application/json"

                Dim encoding As New UTF8Encoding()
                Dim postBytes As Byte() = encoding.GetBytes(strReq)
                sendSmsRequestObject.ContentLength = postBytes.Length

                Dim postStream As Stream = sendSmsRequestObject.GetRequestStream()
                postStream.Write(postBytes, 0, postBytes.Length)
                postStream.Close()

                Dim sendSmsResponseObject As HttpWebResponse = DirectCast(sendSmsRequestObject.GetResponse(), HttpWebResponse)
                Using sendSmsResponseStream As New StreamReader(sendSmsResponseObject.GetResponseStream())
                    sendSmsResponseData = sendSmsResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As SendSmsResponse = DirectCast(deserializeJsonObject.Deserialize(sendSmsResponseData, GetType(SendSmsResponse)), SendSmsResponse)
                    txtSmsId.Text = deserializedJsonObj.Id.ToString()
                    Me.DrawPanelForSuccess(sendSMSPanel, deserializedJsonObj.Id.ToString())
                    sendSmsResponseStream.Close()
                End Using
            End If
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

            Me.DrawPanelForFailure(sendSMSPanel, errorResponse & Environment.NewLine & we.ToString())
        Catch ex As Exception
            Me.DrawPanelForFailure(sendSMSPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This function is called when user clicks on get delivery status button.
    ''' this funciton calls get sms delivery status API to fetch the status.
    ''' </summary>
    Private Sub GetSmsDeliveryStatus()
        Try
            Dim smsId As String = txtSmsId.Text.ToString()
            If smsId Is Nothing OrElse smsId.Length <= 0 Then
                Me.DrawPanelForFailure(getStatusPanel, "Message is null or empty")
                Return
            End If

            If Me.ReadAndGetAccessToken(getStatusPanel) = True Then
                Dim getSmsDeliveryStatusResponseData As String
                ' HttpWebRequest getSmsDeliveryStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.FQDN + "/rest/sms/2/messaging/outbox/" + smsId.ToString() + "?access_token=" + this.access_token.ToString());
                Dim getSmsDeliveryStatusRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/sms/2/messaging/outbox/" & smsId.ToString()), HttpWebRequest)
                getSmsDeliveryStatusRequestObject.Method = "GET"
                getSmsDeliveryStatusRequestObject.Headers.Add("Authorization", "BEARER " & Me.accessToken)
                getSmsDeliveryStatusRequestObject.ContentType = "application/JSON"
                getSmsDeliveryStatusRequestObject.Accept = "application/json"

                Dim getSmsDeliveryStatusResponse As HttpWebResponse = DirectCast(getSmsDeliveryStatusRequestObject.GetResponse(), HttpWebResponse)
                Using getSmsDeliveryStatusResponseStream As New StreamReader(getSmsDeliveryStatusResponse.GetResponseStream())
                    getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseStream.ReadToEnd()
                    getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseData.Replace("-", String.Empty)
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim status As GetDeliveryStatus = DirectCast(deserializeJsonObject.Deserialize(getSmsDeliveryStatusResponseData, GetType(GetDeliveryStatus)), GetDeliveryStatus)
                    Me.DrawGetStatusSuccess(status.DeliveryInfoList.DeliveryInfo(0).Deliverystatus, status.DeliveryInfoList.ResourceURL)
                    getSmsDeliveryStatusResponseStream.Close()
                End Using
            Else
                Me.DrawPanelForFailure(getStatusPanel, "Unable to get access token.")
            End If
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

            Me.DrawPanelForFailure(getStatusPanel, errorResponse & Environment.NewLine & we.ToString())
        Catch ex As Exception
            Me.DrawPanelForFailure(getStatusPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This function is used to draw the table for get status success response
    ''' </summary>
    ''' <param name="status">Status as string</param>
    ''' <param name="url">url as string</param>
    Private Sub DrawGetStatusSuccess(ByVal status As String, ByVal url As String)
        Dim table As New Table()
        Dim rowOne As New TableRow()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.Width = Unit.Pixel(650)
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellOne.Text = "Status: "
        rowTwoCellOne.Font.Bold = True
        rowTwo.Controls.Add(rowTwoCellOne)
        rowTwoCellTwo.Text = status.ToString()
        rowTwo.Controls.Add(rowTwoCellTwo)
        table.Controls.Add(rowTwo)
        Dim rowThree As New TableRow()
        Dim rowThreeCellOne As New TableCell()
        Dim rowThreeCellTwo As New TableCell()
        rowThreeCellOne.Text = "ResourceURL: "
        rowThreeCellOne.Font.Bold = True
        rowThree.Controls.Add(rowThreeCellOne)
        rowThreeCellTwo.Text = url.ToString()
        rowThree.Controls.Add(rowThreeCellTwo)
        table.Controls.Add(rowThree)
        table.BorderWidth = 2
        table.BorderColor = Color.DarkGreen
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        getStatusPanel.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' This function is called to draw the table in the panelParam panel for success response
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="message">Message as string</param>
    Private Sub DrawPanelForSuccess(ByVal panelParam As Panel, ByVal message As String)
        Dim table As New Table()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Font.Bold = True
        rowTwoCellOne.Text = "Message ID:"
        rowTwoCellOne.Width = Unit.Pixel(70)
        rowTwo.Controls.Add(rowTwoCellOne)
        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellTwo.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellTwo)
        table.Controls.Add(rowTwo)
        table.BorderWidth = 2
        table.BorderColor = Color.DarkGreen
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' This function draws table for failed response in the panalParam panel
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="message">Message as string</param>
    Private Sub DrawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        Dim table As New Table()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.Width = Unit.Pixel(650)
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
        table.BorderWidth = 2
        table.BorderColor = Color.Red
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' This function calls receive sms api to fetch the sms's
    ''' </summary>
    Private Sub RecieveSms()
        Try
            Dim receiveSmsResponseData As String
            If Me.shortCode Is Nothing OrElse Me.shortCode.Length <= 0 Then
                Me.DrawPanelForFailure(getMessagePanel, "Short code is null or empty")
                Return
            End If

            If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
                Me.DrawPanelForFailure(getMessagePanel, "Invalid access token")
                Return
            End If

            ' HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(string.Empty + this.FQDN + "/rest/sms/2/messaging/inbox?access_token=" + this.access_token.ToString() + "&RegistrationID=" + this.shortCode.ToString());
            Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/sms/2/messaging/inbox?RegistrationID=" & Me.shortCode.ToString()), HttpWebRequest)
            objRequest.Method = "GET"
            objRequest.Headers.Add("Authorization", "BEARER " & Me.accessToken)
            Dim receiveSmsResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)
            Using receiveSmsResponseStream As New StreamReader(receiveSmsResponseObject.GetResponseStream())
                receiveSmsResponseData = receiveSmsResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As RecieveSmsResponse = DirectCast(deserializeJsonObject.Deserialize(receiveSmsResponseData, GetType(RecieveSmsResponse)), RecieveSmsResponse)
                Dim numberOfMessagesInThisBatch As Integer = deserializedJsonObj.InboundSMSMessageList.NumberOfMessagesInThisBatch
                Dim resourceURL As String = deserializedJsonObj.InboundSMSMessageList.ResourceURL.ToString()
                Dim totalNumberOfPendingMessages As String = deserializedJsonObj.InboundSMSMessageList.TotalNumberOfPendingMessages.ToString()

                Dim parsedJson As String = "MessagesInThisBatch : " & numberOfMessagesInThisBatch.ToString() & "<br/>" & "MessagesPending : " & totalNumberOfPendingMessages.ToString() & "<br/>"
                Dim table As New Table()
                table.Font.Name = "Sans-serif"
                table.Font.Size = 9
                table.BorderStyle = BorderStyle.Outset
                table.Width = Unit.Pixel(650)
                Dim tableRow As New TableRow()
                Dim tableCell As New TableCell()
                tableCell.Width = Unit.Pixel(110)
                tableCell.Text = "SUCCESS:"
                tableCell.Font.Bold = True
                tableRow.Cells.Add(tableCell)
                table.Rows.Add(tableRow)
                tableRow = New TableRow()
                tableCell = New TableCell()
                tableCell.Width = Unit.Pixel(150)
                tableCell.Text = "Messages in this batch:"
                tableCell.Font.Bold = True
                tableRow.Cells.Add(tableCell)
                tableCell = New TableCell()
                tableCell.HorizontalAlign = HorizontalAlign.Left
                tableCell.Text = numberOfMessagesInThisBatch.ToString()
                tableRow.Cells.Add(tableCell)
                table.Rows.Add(tableRow)
                tableRow = New TableRow()
                tableCell = New TableCell()
                tableCell.Width = Unit.Pixel(110)
                tableCell.Text = "Messages pending:"
                tableCell.Font.Bold = True
                tableRow.Cells.Add(tableCell)
                tableCell = New TableCell()
                tableCell.Text = totalNumberOfPendingMessages.ToString()
                tableRow.Cells.Add(tableCell)
                table.Rows.Add(tableRow)
                tableRow = New TableRow()
                table.Rows.Add(tableRow)
                tableRow = New TableRow()
                table.Rows.Add(tableRow)
                Dim secondTable As New Table()
                If numberOfMessagesInThisBatch > 0 Then
                    tableRow = New TableRow()
                    secondTable.Font.Name = "Sans-serif"
                    secondTable.Font.Size = 9
                    tableCell = New TableCell()
                    tableCell.Width = Unit.Pixel(100)
                    tableCell.Text = "Message Index"
                    tableCell.HorizontalAlign = HorizontalAlign.Center
                    tableCell.Font.Bold = True
                    tableRow.Cells.Add(tableCell)
                    tableCell = New TableCell()
                    tableCell.Font.Bold = True
                    tableCell.Width = Unit.Pixel(350)
                    tableCell.Wrap = True
                    tableCell.Text = "Message Text"
                    tableCell.HorizontalAlign = HorizontalAlign.Center
                    tableRow.Cells.Add(tableCell)
                    tableCell = New TableCell()
                    tableCell.Text = "Sender Address"
                    tableCell.HorizontalAlign = HorizontalAlign.Center
                    tableCell.Font.Bold = True
                    tableCell.Width = Unit.Pixel(175)
                    tableRow.Cells.Add(tableCell)
                    secondTable.Rows.Add(tableRow)

                    For Each prime As InboundSMSMessage In deserializedJsonObj.InboundSMSMessageList.InboundSMSMessage
                        tableRow = New TableRow()
                        Dim tableCellmessageId As New TableCell()
                        tableCellmessageId.Width = Unit.Pixel(75)
                        tableCellmessageId.Text = prime.MessageId.ToString()
                        tableCellmessageId.HorizontalAlign = HorizontalAlign.Center
                        Dim tableCellmessage As New TableCell()
                        tableCellmessage.Width = Unit.Pixel(350)
                        tableCellmessage.Wrap = True
                        tableCellmessage.Text = prime.Message.ToString()
                        tableCellmessage.HorizontalAlign = HorizontalAlign.Center
                        Dim tableCellsenderAddress As New TableCell()
                        tableCellsenderAddress.Width = Unit.Pixel(175)
                        tableCellsenderAddress.Text = prime.SenderAddress.ToString()
                        tableCellsenderAddress.HorizontalAlign = HorizontalAlign.Center
                        tableRow.Cells.Add(tableCellmessageId)
                        tableRow.Cells.Add(tableCellmessage)
                        tableRow.Cells.Add(tableCellsenderAddress)
                        secondTable.Rows.Add(tableRow)
                    Next
                End If

                table.BorderColor = Color.DarkGreen
                table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
                table.BorderWidth = 2

                getMessagePanel.Controls.Add(table)
                getMessagePanel.Controls.Add(secondTable)
                receiveSmsResponseStream.Close()
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

            Me.DrawPanelForFailure(getMessagePanel, errorResponse & Environment.NewLine & we.ToString())
        Catch ex As Exception
            Me.DrawPanelForFailure(getMessagePanel, ex.ToString())
        End Try
    End Sub
#End Region

#Region "SMS Application related Data Structures"
    ''' <summary>
    ''' Class to hold access token response
    ''' </summary>
    Public Class AccessTokenResponse
        ''' <summary>
        ''' Gets or sets access token
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
        ''' Gets or sets refresh token
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
        ''' Gets or sets expires in
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
    ''' Class to hold send sms response
    ''' </summary>
    Public Class SendSmsResponse
        ''' <summary>
        ''' Gets or sets id
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
    End Class

    ''' <summary>
    ''' Class to hold sms delivery status
    ''' </summary>
    Public Class GetSetSmsDeliveryStatus
        ''' <summary>
        ''' Gets or sets status
        ''' </summary>
        Public Property Status() As String
            Get
                Return m_Status
            End Get
            Set(ByVal value As String)
                m_Status = Value
            End Set
        End Property
        Private m_Status As String

        ''' <summary>
        ''' Gets or sets resource url
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
    ''' Class to hold sms status
    ''' </summary>
    Public Class SmsStatus
        ''' <summary>
        ''' Gets or sets status
        ''' </summary>
        Public Property Status() As String
            Get
                Return m_Status
            End Get
            Set(ByVal value As String)
                m_Status = Value
            End Set
        End Property
        Private m_Status As String

        ''' <summary>
        ''' Gets or sets resource url
        ''' </summary>
        Public Property ResourceURL() As String
            Get
                Return m_ResourceURL
            End Get
            Set(ByVal value As String)
                m_ResourceURL = Value
            End Set
        End Property
        Private m_ResourceURL As String
    End Class

    ''' <summary>
    ''' Class to hold rececive sms response
    ''' </summary>
    Public Class RecieveSmsResponse
        ''' <summary>
        ''' Gets or sets inbound sms message list
        ''' </summary>
        Public Property InboundSMSMessageList() As InboundSMSMessageList
            Get
                Return m_InboundSMSMessageList
            End Get
            Set(ByVal value As InboundSMSMessageList)
                m_InboundSMSMessageList = Value
            End Set
        End Property
        Private m_InboundSMSMessageList As InboundSMSMessageList
    End Class

    ''' <summary>
    ''' Class to hold inbound sms message list
    ''' </summary>
    Public Class InboundSMSMessageList
        ''' <summary>
        ''' Gets or sets inbound sms message
        ''' </summary>
        Public Property InboundSMSMessage() As List(Of InboundSMSMessage)
            Get
                Return m_InboundSMSMessage
            End Get
            Set(ByVal value As List(Of InboundSMSMessage))
                m_InboundSMSMessage = Value
            End Set
        End Property
        Private m_InboundSMSMessage As List(Of InboundSMSMessage)

        ''' <summary>
        ''' Gets or sets number of messages in a batch
        ''' </summary>
        Public Property NumberOfMessagesInThisBatch() As Integer
            Get
                Return m_NumberOfMessagesInThisBatch
            End Get
            Set(ByVal value As Integer)
                m_NumberOfMessagesInThisBatch = Value
            End Set
        End Property
        Private m_NumberOfMessagesInThisBatch As Integer

        ''' <summary>
        ''' Gets or sets resource url
        ''' </summary>
        Public Property ResourceURL() As String
            Get
                Return m_ResourceURL
            End Get
            Set(ByVal value As String)
                m_ResourceURL = Value
            End Set
        End Property
        Private m_ResourceURL As String

        ''' <summary>
        ''' Gets or sets total number of pending messages
        ''' </summary>
        Public Property TotalNumberOfPendingMessages() As Integer
            Get
                Return m_TotalNumberOfPendingMessages
            End Get
            Set(ByVal value As Integer)
                m_TotalNumberOfPendingMessages = Value
            End Set
        End Property
        Private m_TotalNumberOfPendingMessages As Integer
    End Class

    ''' <summary>
    ''' Class to hold inbound sms message
    ''' </summary>
    Public Class InboundSMSMessage
        ''' <summary>
        ''' Gets or sets datetime
        ''' </summary>
        Public Property DateTime() As String
            Get
                Return m_DateTime
            End Get
            Set(ByVal value As String)
                m_DateTime = Value
            End Set
        End Property
        Private m_DateTime As String

        ''' <summary>
        ''' Gets or sets destination address
        ''' </summary>
        Public Property DestinationAddress() As String
            Get
                Return m_DestinationAddress
            End Get
            Set(ByVal value As String)
                m_DestinationAddress = Value
            End Set
        End Property
        Private m_DestinationAddress As String

        ''' <summary>
        ''' Gets or sets message id
        ''' </summary>
        Public Property MessageId() As String
            Get
                Return m_MessageId
            End Get
            Set(ByVal value As String)
                m_MessageId = Value
            End Set
        End Property
        Private m_MessageId As String

        ''' <summary>
        ''' Gets or sets message
        ''' </summary>
        Public Property Message() As String
            Get
                Return m_Message
            End Get
            Set(ByVal value As String)
                m_Message = Value
            End Set
        End Property
        Private m_Message As String

        ''' <summary>
        ''' Gets or sets sender address
        ''' </summary>
        Public Property SenderAddress() As String
            Get
                Return m_SenderAddress
            End Get
            Set(ByVal value As String)
                m_SenderAddress = Value
            End Set
        End Property
        Private m_SenderAddress As String
    End Class

    ''' <summary>
    ''' Class to hold delivery status
    ''' </summary>
    Public Class GetDeliveryStatus
        ''' <summary>
        ''' Gets or sets delivery info list
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
    ''' Class to hold delivery info list
    ''' </summary>
    Public Class DeliveryInfoList
        ''' <summary>
        ''' Gets or sets resource url
        ''' </summary>
        Public Property ResourceURL() As String
            Get
                Return m_ResourceURL
            End Get
            Set(ByVal value As String)
                m_ResourceURL = Value
            End Set
        End Property
        Private m_ResourceURL As String

        ''' <summary>
        ''' Gets or sets delivery info
        ''' </summary>
        Public Property DeliveryInfo() As List(Of DeliveryInfo)
            Get
                Return m_DeliveryInfo
            End Get
            Set(ByVal value As List(Of DeliveryInfo))
                m_DeliveryInfo = Value
            End Set
        End Property
        Private m_DeliveryInfo As List(Of DeliveryInfo)
    End Class

    ''' <summary>
    ''' Class to hold delivery info
    ''' </summary>
    Public Class DeliveryInfo
        ''' <summary>
        ''' Gets or sets id
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
        ''' Gets or sets address
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
        ''' Gets or sets delivery status
        ''' </summary>
        Public Property Deliverystatus() As String
            Get
                Return m_Deliverystatus
            End Get
            Set(ByVal value As String)
                m_Deliverystatus = Value
            End Set
        End Property
        Private m_Deliverystatus As String
    End Class
#End Region
End Class
