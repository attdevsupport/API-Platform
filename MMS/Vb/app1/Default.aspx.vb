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
''' MMS_App1 class
''' </summary>
''' <remarks> This application allows an end user to send an MMS message with up to three attachments of any common format, 
''' and check the delivery status of that MMS message.
''' </remarks>
Partial Public Class MMS_App1
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private endPoint As String, accessTokenFilePath As String, apiKey As String, secretKey As String, accessToken As String, scope As String, _
     refreshToken As String

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private expirySeconds As String, refreshTokenExpiryTime As String

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private accessTokenExpiryTime As DateTime

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

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

#Region "Page and Button Events"

    ''' <summary>
    ''' Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)

            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

            Me.ReadConfigFile()
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMessagePanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This method will be called when user clicks on send mms button
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub SendMMSMessageButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If Me.ReadAndGetAccessToken() = True Then
                Dim fileSize As Long = 0
                If Not String.IsNullOrEmpty(FileUpload1.FileName) Then
                    FileUpload1.SaveAs(Request.MapPath(FileUpload1.FileName.ToString()))
                    Session("mmsFilePath1") = Request.MapPath(FileUpload1.FileName)
                    Dim fileInfoObj As New FileInfo(Session("mmsFilePath1").ToString())
                    fileSize = fileSize + (fileInfoObj.Length \ 1024)
                    If fileSize > 600 Then
                        Me.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                        Return
                    End If
                End If

                If Not String.IsNullOrEmpty(FileUpload2.FileName) Then
                    FileUpload2.SaveAs(Request.MapPath(FileUpload2.FileName))
                    Session("mmsFilePath2") = Request.MapPath(FileUpload2.FileName)
                    Dim fileInfoObj As New FileInfo(Session("mmsFilePath2").ToString())
                    fileSize = fileSize + (fileInfoObj.Length \ 1024)
                    If fileSize > 600 Then
                        Me.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                        Return
                    End If
                End If

                If Not String.IsNullOrEmpty(FileUpload3.FileName) Then
                    FileUpload3.SaveAs(Request.MapPath(FileUpload3.FileName))
                    Session("mmsFilePath3") = Request.MapPath(FileUpload3.FileName)
                    Dim fileInfoObj As New FileInfo(Session("mmsFilePath3").ToString())
                    fileSize = fileSize + (fileInfoObj.Length \ 1024)
                    If fileSize > 600 Then
                        Me.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                        Return
                    End If
                End If

                If fileSize <= 600 Then
                    Me.SendMMS()
                Else
                    Me.DrawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                    Return
                End If
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMessagePanel, ex.ToString())
            Return
        End Try
    End Sub

    ''' <summary>
    ''' This method will be called when user click on get status button
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub GetStatusButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Me.GetMmsDeliveryStatus()
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
            Me.DrawPanelForFailure(sendMessagePanel, ex.ToString())
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
                oauthParameters = "client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=MMS"
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
                Dim jsonAccessToken As String = accessTokenResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(jsonAccessToken, GetType(AccessTokenResponse)), AccessTokenResponse)

                Me.accessToken = deserializedJsonObj.access_token
                Me.expirySeconds = deserializedJsonObj.expires_in
                Me.refreshToken = deserializedJsonObj.refresh_token
                Me.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in))

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
            Me.DrawPanelForFailure(sendMessagePanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMessagePanel, ex.ToString())
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
    ''' Displays Resource url upon success of GetMmsDelivery
    ''' </summary>
    ''' <param name="status">string, status of the request</param>
    ''' <param name="url">string, url of the resource</param>
    Private Sub DrawGetStatusSuccess(ByVal status As String, ByVal url As String)
        If getStatusPanel.HasControls() Then
            getStatusPanel.Controls.Clear()
        End If

        Dim table As New Table()
        table.CssClass = "successWide"
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

        getStatusPanel.Controls.Add(table)
    End Sub

#End Region

#Region "Application Specific Methods"

    ''' <summary>
    ''' This function calls get message delivery status api to fetch the delivery status
    ''' </summary>
    Private Sub GetMmsDeliveryStatus()
        Try
            Dim mmsId As String = messageIDTextBox.Text
            If mmsId Is Nothing OrElse mmsId.Length <= 0 Then
                Me.DrawPanelForFailure(getStatusPanel, "Message Id is null or empty")
                Return
            End If

            If Me.ReadAndGetAccessToken() = True Then
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
                    Me.DrawGetStatusSuccess(status.DeliveryInfoList.deliveryInfo(0).deliverystatus, status.DeliveryInfoList.resourceURL)
                    mmsStatusResponseStream.Close()
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
            Me.DrawPanelForFailure(getStatusPanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(getStatusPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This method reads config file and assigns values to local variables
    ''' </summary>
    ''' <returns>true/false, true- if able to read from config file</returns>
    Private Function ReadConfigFile() As Boolean
        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(sendMessagePanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(sendMessagePanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(sendMessagePanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "MMS"
        End If

        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "~\MMSApp1AccessToken.txt"
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        Return True
    End Function

    ''' <summary>
    ''' Gets formatted phone number
    ''' </summary>
    ''' <returns>string, phone number</returns>
    Private Function GetPhoneNumber() As String
        Dim tryParseResult As Long = 0

        Dim smsAddressInput As String = phoneTextBox.Text.ToString()

        Dim smsAddressFormatted As String = String.Empty

        Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"
        If System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
            smsAddressFormatted = smsAddressInput.Replace("-", String.Empty)
        Else
            smsAddressFormatted = smsAddressInput
        End If

        If smsAddressFormatted.Length = 16 AndAlso smsAddressFormatted.StartsWith("tel:+1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(6, 10)
        ElseIf smsAddressFormatted.Length = 15 AndAlso smsAddressFormatted.StartsWith("tel:+") Then
            smsAddressFormatted = smsAddressFormatted.Substring(5, 10)
        ElseIf smsAddressFormatted.Length = 14 AndAlso smsAddressFormatted.StartsWith("tel:") Then
            smsAddressFormatted = smsAddressFormatted.Substring(4, 10)
        ElseIf smsAddressFormatted.Length = 12 AndAlso smsAddressFormatted.StartsWith("+1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(2, 10)
        ElseIf smsAddressFormatted.Length = 11 AndAlso smsAddressFormatted.StartsWith("1") Then
            smsAddressFormatted = smsAddressFormatted.Substring(1, 10)
        End If

        If (smsAddressFormatted.Length <> 10) OrElse (Not Long.TryParse(smsAddressFormatted, tryParseResult)) Then
            Me.DrawPanelForFailure(sendMessagePanel, "Invalid phone number: " & smsAddressInput)
            smsAddressFormatted = String.Empty
        End If

        Return smsAddressFormatted
    End Function

    ''' <summary>
    ''' This funciton initiates send mms api call to send selected files as an mms
    ''' </summary>
    Private Sub SendMMS()
        Try
            Dim mmsAddress As String = Me.GetPhoneNumber()
            Dim mmsMessage As String = messageTextBox.Text.ToString()

            If ((Session("mmsFilePath1") Is Nothing) AndAlso (Session("mmsFilePath2") Is Nothing) AndAlso (Session("mmsFilePath3") Is Nothing)) AndAlso String.IsNullOrEmpty(mmsMessage) Then
                Me.DrawPanelForFailure(sendMessagePanel, "Message is null or empty")
                Return
            End If

            If (Session("mmsFilePath1") Is Nothing) AndAlso (Session("mmsFilePath2") Is Nothing) AndAlso (Session("mmsFilePath3") Is Nothing) Then
                Me.SendMessageNoAttachments(mmsAddress, mmsMessage)
            Else
                Me.SendMultimediaMessage(mmsAddress, mmsMessage)
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
            Me.DrawPanelForFailure(sendMessagePanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(sendMessagePanel, ex.ToString())
        Finally
            Dim index As Integer = 1

            Dim tmpVar As Object = Nothing
            While index <= 3
                tmpVar = Session("mmsFilePath" & index)
                If tmpVar IsNot Nothing Then
                    If File.Exists(tmpVar.ToString()) Then
                        File.Delete(tmpVar.ToString())
                        Session("mmsFilePath" & index) = Nothing
                    End If
                End If

                index += 1
            End While
        End Try
    End Sub

    ''' <summary>
    ''' Sends MMS by calling messaging api
    ''' </summary>
    ''' <param name="mmsAddress">string, phone number</param>
    ''' <param name="mmsMessage">string, mms message</param>
    Private Sub SendMultimediaMessage(ByVal mmsAddress As String, ByVal mmsMessage As String)
        Dim boundary As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

        Dim mmsRequestObject As HttpWebRequest = DirectCast(WebRequest.Create(String.Empty & Me.endPoint & "/rest/mms/2/messaging/outbox"), HttpWebRequest)
        mmsRequestObject.Headers.Add("Authorization", "Bearer " & Me.accessToken)
        mmsRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" & boundary & """" & vbCr & vbLf
        mmsRequestObject.Method = "POST"
        mmsRequestObject.KeepAlive = True

        Dim encoding As New UTF8Encoding()

        Dim totalpostBytes As Byte() = encoding.GetBytes(String.Empty)
        Dim sendMMSData As String = "Address=" & Server.UrlEncode("tel:" & mmsAddress) & "&Subject=" & Server.UrlEncode(mmsMessage)

        Dim data As String = String.Empty
        data += "--" & boundary & vbCr & vbLf
        data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding:8bit" & vbCr & vbLf & "Content-ID:<startpart>" & vbCr & vbLf & vbCr & vbLf & sendMMSData & vbCr & vbLf

        totalpostBytes = Me.FormMIMEParts(boundary, data)

        Dim byteLastBoundary As Byte() = encoding.GetBytes(vbCr & vbLf & "--" & boundary & "--" & vbCr & vbLf)
        Dim totalSize As Integer = totalpostBytes.Length + byteLastBoundary.Length

        Dim totalMS = New MemoryStream(New Byte(totalSize - 1) {}, 0, totalSize, True, True)
        totalMS.Write(totalpostBytes, 0, totalpostBytes.Length)
        totalMS.Write(byteLastBoundary, 0, byteLastBoundary.Length)

        Dim finalpostBytes As Byte() = totalMS.GetBuffer()
        mmsRequestObject.ContentLength = finalpostBytes.Length

        Dim postStream As Stream = Nothing
        Try
            postStream = mmsRequestObject.GetRequestStream()
            postStream.Write(finalpostBytes, 0, finalpostBytes.Length)
        Catch ex As Exception
            Throw ex
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try

        Dim mmsResponseObject As WebResponse = mmsRequestObject.GetResponse()
        Using streamReader As New StreamReader(mmsResponseObject.GetResponseStream())
            Dim mmsResponseData As String = streamReader.ReadToEnd()
            Dim deserializeJsonObject As New JavaScriptSerializer()
            Dim deserializedJsonObj As MmsResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(MmsResponseId)), MmsResponseId)
            messageIDTextBox.Text = deserializedJsonObj.id.ToString()
            Me.DrawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString())
            streamReader.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Form mime parts for the user input files
    ''' </summary>
    ''' <param name="boundary">string, boundary data</param>
    ''' <param name="data">string, mms message</param>
    ''' <returns>returns byte array of files</returns>
    Private Function FormMIMEParts(ByVal boundary As String, ByRef data As String) As Byte()
        Dim encoding As New UTF8Encoding()

        Dim postBytes As Byte() = encoding.GetBytes(String.Empty)
        Dim totalpostBytes As Byte() = encoding.GetBytes(String.Empty)

        If Session("mmsFilePath1") IsNot Nothing Then
            postBytes = Me.GetBytesOfFile(boundary, data, Session("mmsFilePath1").ToString())
            totalpostBytes = postBytes
        End If

        If Session("mmsFilePath2") IsNot Nothing Then
            If Session("mmsFilePath1") IsNot Nothing Then
                data = "--" & boundary & vbCr & vbLf
            Else
                data += "--" & boundary & vbCr & vbLf
            End If

            postBytes = Me.GetBytesOfFile(boundary, data, Session("mmsFilePath2").ToString())

            If Session("mmsFilePath1") IsNot Nothing Then
                Dim ms2 = JoinTwoByteArrays(totalpostBytes, postBytes)
                totalpostBytes = ms2.GetBuffer()
            Else
                totalpostBytes = postBytes
            End If
        End If

        If Session("mmsFilePath3") IsNot Nothing Then
            If Session("mmsFilePath1") IsNot Nothing OrElse Session("mmsFilePath2") IsNot Nothing Then
                data = "--" & boundary & vbCr & vbLf
            Else
                data += "--" & boundary & vbCr & vbLf
            End If

            postBytes = Me.GetBytesOfFile(boundary, data, Session("mmsFilePath3").ToString())

            If Session("mmsFilePath1") IsNot Nothing OrElse Session("mmsFilePath2") IsNot Nothing Then
                Dim ms2 = JoinTwoByteArrays(totalpostBytes, postBytes)
                totalpostBytes = ms2.GetBuffer()
            Else
                totalpostBytes = postBytes
            End If
        End If

        Return totalpostBytes
    End Function

    ''' <summary>
    ''' Gets the bytes representation of file along with mime part
    ''' </summary>
    ''' <param name="boundary">string, boundary message</param>
    ''' <param name="data">string, mms message</param>
    ''' <param name="filePath">string, filepath</param>
    ''' <returns>byte[], representation of file in bytes</returns>
    Private Function GetBytesOfFile(ByVal boundary As String, ByRef data As String, ByVal filePath As String) As Byte()
        Dim encoding As New UTF8Encoding()
        Dim postBytes As Byte() = encoding.GetBytes(String.Empty)
        Dim fileStream As FileStream = Nothing
        Dim binaryReader As BinaryReader = Nothing

        Try
            Dim mmsFileName As String = Path.GetFileName(filePath)

            fileStream = New FileStream(filePath, FileMode.Open, FileAccess.Read)
            binaryReader = New BinaryReader(fileStream)

            Dim image As Byte() = binaryReader.ReadBytes(CInt(fileStream.Length))

            data += "--" & boundary & vbCr & vbLf
            data += "Content-Disposition:attachment;name=""" & mmsFileName & """" & vbCr & vbLf
            data += "Content-Type:image/gif" & vbCr & vbLf
            data += "Content-ID:<" & mmsFileName & ">" & vbCr & vbLf
            data += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf

            Dim firstPart As Byte() = encoding.GetBytes(data)
            Dim newSize As Integer = firstPart.Length + image.Length

            Dim memoryStream = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
            memoryStream.Write(firstPart, 0, firstPart.Length)
            memoryStream.Write(image, 0, image.Length)

            postBytes = memoryStream.GetBuffer()
        Catch ex As Exception
            Throw ex
        Finally
            If binaryReader IsNot Nothing Then
                binaryReader.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

        Return postBytes
    End Function

    ''' <summary>
    ''' Invokes messaging api to send message without any attachments
    ''' </summary>
    ''' <param name="mmsAddress">string, phone number</param>
    ''' <param name="mmsMessage">string, mms message</param>
    Private Sub SendMessageNoAttachments(ByVal mmsAddress As String, ByVal mmsMessage As String)
        Dim boundaryToSend As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

        Dim mmsRequestObject As HttpWebRequest = DirectCast(WebRequest.Create(String.Empty & Me.endPoint & "/rest/mms/2/messaging/outbox"), HttpWebRequest)
        mmsRequestObject.Headers.Add("Authorization", "Bearer " & Me.accessToken)
        mmsRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" & boundaryToSend & """" & vbCr & vbLf
        mmsRequestObject.Method = "POST"
        mmsRequestObject.KeepAlive = True

        Dim encoding As New UTF8Encoding()
        Dim bytesToSend As Byte() = encoding.GetBytes(String.Empty)
        Dim mmsParameters As String = "Address=" & Server.UrlEncode("tel:" & mmsAddress) & "&Subject=" & Server.UrlEncode(mmsMessage)

        Dim dataToSend As String = String.Empty
        dataToSend += "--" & boundaryToSend & vbCr & vbLf
        dataToSend += "Content-Type: application/x-www-form-urlencoded; charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding: 8bit" & vbCr & vbLf & "Content-Disposition: form-data; name=""root-fields""" & vbCr & vbLf & "Content-ID: <startpart>" & vbCr & vbLf & vbCr & vbLf & mmsParameters & vbCr & vbLf
        dataToSend += "--" & boundaryToSend & "--" & vbCr & vbLf
        bytesToSend = encoding.GetBytes(dataToSend)

        Dim sizeToSend As Integer = bytesToSend.Length
        Dim memBufToSend = New MemoryStream(New Byte(sizeToSend - 1) {}, 0, sizeToSend, True, True)
        memBufToSend.Write(bytesToSend, 0, bytesToSend.Length)
        Dim finalData As Byte() = memBufToSend.GetBuffer()
        mmsRequestObject.ContentLength = finalData.Length

        Dim postStream As Stream = Nothing
        Try
            postStream = mmsRequestObject.GetRequestStream()
            postStream.Write(finalData, 0, finalData.Length)
        Catch ex As Exception
            Throw ex
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try

        Dim mmsResponseObject As WebResponse = mmsRequestObject.GetResponse()
        Using streamReader As New StreamReader(mmsResponseObject.GetResponseStream())
            Dim mmsResponseData As String = streamReader.ReadToEnd()
            Dim deserializeJsonObject As New JavaScriptSerializer()
            Dim deserializedJsonObj As MmsResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(MmsResponseId)), MmsResponseId)
            messageIDTextBox.Text = deserializedJsonObj.id.ToString()
            Me.DrawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString())
            streamReader.Close()
        End Using
    End Sub

    ''' <summary>
    ''' This function adds two byte arrays
    ''' </summary>
    ''' <param name="firstByteArray">first array of bytes</param>
    ''' <param name="secondByteArray">second array of bytes</param>
    ''' <returns>returns MemoryStream after joining two byte arrays</returns>
    Private Shared Function JoinTwoByteArrays(ByVal firstByteArray As Byte(), ByVal secondByteArray As Byte()) As MemoryStream
        Dim newSize As Integer = firstByteArray.Length + secondByteArray.Length
        Dim ms = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
        ms.Write(firstByteArray, 0, firstByteArray.Length)
        ms.Write(secondByteArray, 0, secondByteArray.Length)
        Return ms
    End Function

#End Region
End Class

#Region "Data Structures"

''' <summary>
''' MmsResponseId object
''' </summary>
Public Class MmsResponseId
    ''' <summary>
    ''' Gets or sets the value of id
    ''' </summary>
    Public Property id() As String
        Get
            Return m_id
        End Get
        Set(ByVal value As String)
            m_id = Value
        End Set
    End Property
    Private m_id As String
End Class

''' <summary>
''' Response of GetMmsDelivery
''' </summary>
Public Class GetDeliveryStatus
    ''' <summary>
    ''' Gets or sets the value of DeliveryInfoList
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
    ''' Gets or sets the value of resourceURL
    ''' </summary>
    Public Property resourceURL() As String
        Get
            Return m_resourceURL
        End Get
        Set(ByVal value As String)
            m_resourceURL = Value
        End Set
    End Property
    Private m_resourceURL As String

    ''' <summary>
    ''' Gets or sets the value of deliveryInfo
    ''' </summary>
    Public Property deliveryInfo() As List(Of deliveryInfo)
        Get
            Return m_deliveryInfo
        End Get
        Set(ByVal value As List(Of deliveryInfo))
            m_deliveryInfo = Value
        End Set
    End Property
    Private m_deliveryInfo As List(Of deliveryInfo)
End Class

''' <summary>
''' deliveryInfo object
''' </summary>
Public Class deliveryInfo
    ''' <summary>
    ''' Gets or sets the value of id
    ''' </summary>
    Public Property id() As String
        Get
            Return m_id
        End Get
        Set(ByVal value As String)
            m_id = Value
        End Set
    End Property
    Private m_id As String

    ''' <summary>
    ''' Gets or sets the value of address
    ''' </summary>
    Public Property address() As String
        Get
            Return m_address
        End Get
        Set(ByVal value As String)
            m_address = Value
        End Set
    End Property
    Private m_address As String

    ''' <summary>
    ''' Gets or sets the value of deliveryStatus
    ''' </summary>
    Public Property deliverystatus() As String
        Get
            Return m_deliverystatus
        End Get
        Set(ByVal value As String)
            m_deliverystatus = Value
        End Set
    End Property
    Private m_deliverystatus As String
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
#End Region
