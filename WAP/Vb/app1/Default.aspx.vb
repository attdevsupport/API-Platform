' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

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
''' WapPush_App1 application
''' </summary>
''' <remarks>
''' This application allows a user to send a WAP Push message to a mobile device, by entering the address, alert text, and URL to be sent.
''' This application uses Autonomous Client Credentials consumption model to send messages. The user enters the alert text and URL, 
''' but the application in the background must build the push.txt file to attach with the requested values.
''' </remarks>
Partial Public Class WapPush_App1
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private endPoint As String, accessTokenFilePath As String, wapFilePath As String

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private apiKey As String, secretKey As String, accessToken As String, scope As String, expirySeconds As String, refreshToken As String, _
     refreshTokenExpiryTime As String

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

    ''' <summary>
    ''' Instance variables for local processing
    ''' </summary>
    Private accessTokenExpiryTime As DateTime

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

    ''' <summary>
    ''' Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Public Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)

            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

            Me.ReadConfigFile()
        Catch ex As Exception
            Me.DrawPanelForFailure(wapPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This function is called when user clicks on send wap message button. This funciton calls send wap message API to send the wap message
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub SendWAPButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If String.IsNullOrEmpty(txtAddressWAPPush.Text) Then
                Me.DrawPanelForFailure(wapPanel, "Specify phone number")
            End If

            If String.IsNullOrEmpty(txtAlert.Text) Then
                Me.DrawPanelForFailure(wapPanel, "Specify alert text")
            End If

            If String.IsNullOrEmpty(txtUrl.Text) Then
                Me.DrawPanelForFailure(wapPanel, "Specify Url")
            End If

            If Me.ReadAndGetAccessToken() = True Then
                Me.SendWapPush()
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(wapPanel, ex.ToString())
        End Try
    End Sub

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

#End Region

#Region "WAP Push Related methods"

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
            Me.DrawPanelForFailure(wapPanel, ex.ToString())
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
                oauthParameters = "client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=WAP"
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
            Me.DrawPanelForFailure(wapPanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(wapPanel, ex.ToString())
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
    ''' This method reads config file and assigns values to local variables
    ''' </summary>
    ''' <returns>true/false, true- if able to read from config file</returns>
    Private Function ReadConfigFile() As Boolean
        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(wapPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(wapPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(wapPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "WAP"
        End If

        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "WAPApp1AccessToken.txt"
        End If

        Me.wapFilePath = ConfigurationManager.AppSettings("WAPFilePath")
        If String.IsNullOrEmpty(Me.wapFilePath) Then
            Me.accessTokenFilePath = "WAPText.txt"
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
    ''' This function validates string against the valid msisdn
    ''' </summary>
    ''' <param name="number">string, destination number</param>
    ''' <returns>true/false; true if valid MSISDN, else false</returns>
    Private Function IsValidMISDN(ByVal number As String) As Boolean
        Dim smsAddressInput As String = number
        Dim smsAddressFormatted As String
        Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"
        If System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
            smsAddressFormatted = smsAddressInput.Replace("-", String.Empty)
        Else
            smsAddressFormatted = smsAddressInput
        End If

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
    ''' This function calls send wap message api to send wap messsage
    ''' </summary>
    Private Sub SendWapPush()
        Dim wapFileWriter As StreamWriter = Nothing
        Dim streamReader As StreamReader = Nothing
        Try
            If Me.IsValidMISDN(txtAddressWAPPush.Text.ToString()) = False Then
                Me.DrawPanelForFailure(wapPanel, "Invalid Number: " & txtAddressWAPPush.Text.ToString())
                Return
            End If

            Dim wapAddress As String = txtAddressWAPPush.Text.ToString().Replace("tel:+1", String.Empty)
            wapAddress = wapAddress.ToString().Replace("tel:+", String.Empty)
            wapAddress = wapAddress.ToString().Replace("tel:1", String.Empty)
            wapAddress = wapAddress.ToString().Replace("tel:", String.Empty)
            wapAddress = wapAddress.ToString().Replace("tel:", String.Empty)
            wapAddress = wapAddress.ToString().Replace("-", String.Empty)

            Dim wapMessage As String = txtAlert.Text
            Dim wapUrl As String = txtUrl.Text

            Dim boundary As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

            Dim wapData As String = String.Empty
            wapData += "Content-Disposition: form-data; name=""PushContent""" & vbLf
            wapData += "Content-Type: text/vnd.wap.si" & vbLf
            wapData += "Content-Length: 20" & vbLf
            wapData += "X-Wap-Application-Id: x-wap-application:wml.ua" & vbLf & vbLf
            wapData += "<?xml version='1.0'?>" & vbLf
            wapData += "<!DOCTYPE si PUBLIC ""-//WAPFORUM//DTD SI 1.0//EN"" " & """http://www.wapforum.org/DTD/si.dtd"">" & vbLf
            wapData += "<si>" & vbLf
            wapData += "<indication href=""" & wapUrl.ToString() & """ " & "action=""signal-medium"" si-id=""6532"">" & vbLf
            wapData += wapMessage.ToString()
            wapData += vbLf & "</indication>"
            wapData += vbLf & "</si>"

            wapFileWriter = File.CreateText(Request.MapPath(Me.wapFilePath))
            wapFileWriter.Write(wapData)
            wapFileWriter.Close()

            streamReader = New StreamReader(Request.MapPath(Me.wapFilePath))
            Dim pushFile As String = streamReader.ReadToEnd()

            Dim wapRequestObject As HttpWebRequest = DirectCast(WebRequest.Create(String.Empty & Me.endPoint & "/1/messages/outbox/wapPush"), HttpWebRequest)
            wapRequestObject.Headers.Add("Authorization", "Bearer " & Me.accessToken)
            wapRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""""; boundary=""" & boundary & """" & vbCr & vbLf
            wapRequestObject.Method = "POST"
            wapRequestObject.KeepAlive = True

            Dim sendWapData As String = "address=" & Server.UrlEncode("tel:" & wapAddress.ToString()) & "&subject=" & Server.UrlEncode("Wap Message") & "&priority=High&content-type=" & Server.UrlEncode("application/xml")

            Dim data As String = String.Empty
            data += "--" & boundary & vbCr & vbLf
            data += "Content-type: application/x-www-form-urlencoded; charset=UTF-8" & vbCr & vbLf
            data += "Content-Transfer-Encoding: 8bit" & vbCr & vbLf
            data += "Content-ID: <startpart>" & vbCr & vbLf
            data += "Content-Disposition: form-data; name=""root-fields""" & vbCr & vbLf & vbCr & vbLf & sendWapData.ToString() & vbCr & vbLf
            data += "--" & boundary & vbCr & vbLf
            data += "Content-Disposition: attachment; name=Push.txt" & vbCr & vbLf & vbCr & vbLf
            data += "Content-Type: text/plain" & vbCr & vbLf
            data += "Content-ID: <Push.txt>" & vbCr & vbLf
            data += "Content-Transfer-Encoding: binary" & vbCr & vbLf
            data += pushFile & vbCr & vbLf
            data += "--" & boundary & "--" & vbCr & vbLf

            Dim encoding As New UTF8Encoding()
            Dim postBytes As Byte() = encoding.GetBytes(data)
            wapRequestObject.ContentLength = postBytes.Length

            Using writeStream As Stream = wapRequestObject.GetRequestStream()
                writeStream.Write(postBytes, 0, postBytes.Length)
                writeStream.Close()
            End Using

            Dim wapResponseObject As HttpWebResponse = DirectCast(wapRequestObject.GetResponse(), HttpWebResponse)
            Using wapResponseStream As New StreamReader(wapResponseObject.GetResponseStream())
                Dim strResult As String = wapResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As SendWapResponse = DirectCast(deserializeJsonObject.Deserialize(strResult, GetType(SendWapResponse)), SendWapResponse)
                Me.DrawPanelForSuccess(wapPanel, deserializedJsonObj.id.ToString())
                wapResponseStream.Close()
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
            Me.DrawPanelForFailure(wapPanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(wapPanel, ex.ToString())
        Finally
            If wapFileWriter IsNot Nothing Then
                wapFileWriter.Close()
            End If

            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If

            If File.Exists(Request.MapPath(Me.wapFilePath)) Then
                File.Delete(Request.MapPath(Me.wapFilePath))
            End If
        End Try
    End Sub

#End Region
End Class

#Region "Data Structures"

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
''' WAP Response Object
''' </summary>
Public Class SendWapResponse
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

#End Region