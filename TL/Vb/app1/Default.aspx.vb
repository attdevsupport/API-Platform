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
Imports System.Text.RegularExpressions
Imports System.Web.Script.Serialization
Imports System.Web.UI.WebControls

#End Region

''' <summary>
''' Access Token Types
''' </summary>
Public Enum AccessTokenType
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
''' TL_App1 class
''' </summary>
Partial Public Class TL_App1
    Inherits System.Web.UI.Page
#Region "Local variables"

    ''' <summary>
    ''' Gets or sets the value of endPoint
    ''' </summary>
    Private endPoint As String

    ''' <summary>
    ''' Access Token Variables
    ''' </summary>
    Private apiKey As String, secretKey As String, accessToken As String, authorizeRedirectUri As String, scope As String, refreshToken As String, _
     accessTokenExpiryTime As String, refreshTokenExpiryTime As String

    ''' <summary>
    ''' Gets or sets the value of authCode
    ''' </summary>
    Private authCode As String

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

    ''' <summary>
    ''' Gets or sets the Status Table
    ''' </summary>
    Private getStatusTable As Table

#End Region

#Region "SSL Handshake Error"

    ''' <summary>
    ''' Neglect the ssl handshake error with authentication server
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
    ''' This function is called when the applicaiton page is loaded into the browser.
    ''' This function reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">object that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
            map_canvas.Visible = False

            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

            Dim ableToRead As Boolean = Me.ReadConfigFile()
            If Not ableToRead Then
                Return
            End If

            If Session("tl_session_acceptableAccuracy") IsNot Nothing Then
                Radio_AcceptedAccuracy.SelectedIndex = Convert.ToInt32(Session("tl_session_acceptableAccuracy").ToString())
                Radio_RequestedAccuracy.SelectedIndex = Convert.ToInt32(Session("tl_session_requestedAccuracy").ToString())
                Radio_DelayTolerance.SelectedIndex = Convert.ToInt32(Session("tl_session_tolerance").ToString())
            End If

            If (Session("tl_session_appState") Is "GetToken") AndAlso (Request("Code") IsNot Nothing) Then
                Me.authCode = Request("code")
                Dim ableToGetToken As Boolean = Me.GetAccessToken(AccessTokenType.Authorization_Code)
                If ableToGetToken Then
                    Me.GetDeviceLocation()
                Else
                    Me.DrawPanelForFailure(tlPanel, "Failed to get Access token")
                    Me.ResetTokenSessionVariables()
                    Me.ResetTokenVariables()
                End If
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(tlPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Event that will be triggered when the user clicks on GetPhoneLocation button
    ''' This method calls GetDeviceLocation Api
    ''' </summary>
    ''' <param name="sender">object that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub GetDeviceLocation_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Session("tl_session_acceptableAccuracy") = Radio_AcceptedAccuracy.SelectedIndex
            Session("tl_session_requestedAccuracy") = Radio_RequestedAccuracy.SelectedIndex
            Session("tl_session_tolerance") = Radio_DelayTolerance.SelectedIndex

            Dim ableToGetAccessToken As Boolean = Me.ReadAndGetAccessToken()
            If ableToGetAccessToken Then
                Me.GetDeviceLocation()
            Else
                Me.DrawPanelForFailure(tlPanel, "Unable to get access token")
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(tlPanel, ex.Message)
        End Try
    End Sub

#End Region

#Region "API Invokation"

    ''' <summary>
    ''' This method invokes Device Location API and displays the location
    ''' </summary>
    Private Sub GetDeviceLocation()
        Try
            Dim definedReqAccuracy As Integer() = New Integer(2) {100, 1000, 10000}
            Dim definedTolerance As String() = New String(2) {"NoDelay", "LowDelay", "DelayTolerant"}

            Dim requestedAccuracy As Integer, acceptableAccuracy As Integer
            Dim tolerance As String

            acceptableAccuracy = definedReqAccuracy(Radio_AcceptedAccuracy.SelectedIndex)
            requestedAccuracy = definedReqAccuracy(Radio_RequestedAccuracy.SelectedIndex)
            tolerance = definedTolerance(Radio_DelayTolerance.SelectedIndex)

            Dim strResult As String

            Dim webRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/2/devices/location?requestedAccuracy=" & requestedAccuracy & "&acceptableAccuracy=" & acceptableAccuracy & "&tolerance=" & tolerance), HttpWebRequest)
            webRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
            webRequest.Method = "GET"

            Dim msgSentTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim webResponse As HttpWebResponse = DirectCast(webRequest.GetResponse(), HttpWebResponse)
            Dim msgReceivedTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim tokenSpan As TimeSpan = msgReceivedTime.Subtract(msgSentTime)

            Using responseStream As New StreamReader(webResponse.GetResponseStream())
                strResult = responseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As TLResponse = DirectCast(deserializeJsonObject.Deserialize(strResult, GetType(TLResponse)), TLResponse)

                Me.DrawPanelForGetLocationResult(String.Empty, String.Empty, True)
                Me.DrawPanelForGetLocationResult("Accuracy:", deserializedJsonObj.accuracy, False)
                Me.DrawPanelForGetLocationResult("Latitude:", deserializedJsonObj.latitude, False)
                Me.DrawPanelForGetLocationResult("Longitude:", deserializedJsonObj.longitude, False)
                Me.DrawPanelForGetLocationResult("TimeStamp:", deserializedJsonObj.timestamp, False)
                Me.DrawPanelForGetLocationResult("Response Time:", tokenSpan.Seconds.ToString() & "seconds", False)

                MapTerminalLocation.Visible = True
                map_canvas.Visible = True
                Dim googleString As New StringBuilder()
                googleString.Append("http://maps.google.com/?q=" & deserializedJsonObj.latitude & "+" & deserializedJsonObj.longitude & "&output=embed")
                MapTerminalLocation.Attributes("src") = googleString.ToString()

                responseStream.Close()
            End Using
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Dim streamReader As New StreamReader(stream)
                    Me.DrawPanelForFailure(tlPanel, streamReader.ReadToEnd())
                    streamReader.Close()
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(tlPanel, ex.Message)
        End Try
    End Sub

#End Region

#Region "Access Token Methods"

    ''' <summary>
    ''' Reads from session variables and gets access token
    ''' </summary>
    ''' <returns>true/false; true on successfully getting access token, else false</returns>
    Private Function ReadAndGetAccessToken() As Boolean
        Me.ReadTokenSessionVariables()

        Dim tokentResult As String = Me.IsTokenValid()
        If tokentResult.Equals("INVALID_ACCESS_TOKEN") Then
            Session("tl_session_appState") = "GetToken"
            Me.GetAuthCode()
        ElseIf tokentResult.Equals("REFRESH_TOKEN") Then
            Dim ableToGetToken As Boolean = Me.GetAccessToken(AccessTokenType.Refresh_Token)
            If ableToGetToken = False Then
                Me.DrawPanelForFailure(tlPanel, "Failed to get Access token")
                Me.ResetTokenSessionVariables()
                Me.ResetTokenVariables()
                Return False
            End If
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function reads access token related session variables to local variables 
    ''' </summary>
    Private Sub ReadTokenSessionVariables()
        Me.accessToken = String.Empty
        If Session("tl_session_access_token") IsNot Nothing Then
            Me.accessToken = Session("tl_session_access_token").ToString()
        End If

        Me.refreshToken = Nothing
        If Session("tl_session_refresh_token") IsNot Nothing Then
            Me.refreshToken = Session("tl_session_refresh_token").ToString()
        End If

        Me.accessTokenExpiryTime = Nothing
        If Session("tl_session_accessTokenExpiryTime") IsNot Nothing Then
            Me.accessTokenExpiryTime = Session("tl_session_accessTokenExpiryTime").ToString()
        End If

        Me.refreshTokenExpiryTime = Nothing
        If Session("tl_session_refreshTokenExpiryTime") IsNot Nothing Then
            Me.refreshTokenExpiryTime = Session("tl_session_refreshTokenExpiryTime").ToString()
        End If
    End Sub

    ''' <summary>
    ''' This function resets access token related session variable to null 
    ''' </summary>
    Private Sub ResetTokenSessionVariables()
        Session("tl_session_access_token") = Nothing
        Session("tl_session_refresh_token") = Nothing
        Session("tl_session_accessTokenExpiryTime") = Nothing
        Session("tl_session_refreshTokenExpiryTime") = Nothing
    End Sub

    ''' <summary>
    ''' This function resets access token related  variable to null 
    ''' </summary>
    Private Sub ResetTokenVariables()
        Me.accessToken = Nothing
        Me.refreshToken = Nothing
        Me.accessTokenExpiryTime = Nothing
        Me.refreshTokenExpiryTime = Nothing
    End Sub

    ''' <summary>
    ''' This function validates access token related variables and returns VALID_ACCESS_TOKEN if its valid
    ''' otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    ''' return REFRESH_TOKEN, if access token in expired and refresh token is valid 
    ''' </summary>
    ''' <returns>string variable containing valid/invalid access/refresh token</returns>
    Private Function IsTokenValid() As String
        If Session("tl_session_access_token") Is Nothing Then
            Return "INVALID_ACCESS_TOKEN"
        End If

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
    ''' Redirects to authentication server to get the access code
    ''' </summary>
    Private Sub GetAuthCode()
        Response.Redirect(String.Empty & Me.endPoint & "/oauth/authorize?scope=" & Me.scope & "&client_id=" & Me.apiKey & "&redirect_url=" & Me.authorizeRedirectUri)
    End Sub

    ''' <summary>
    ''' Get access token based on the type parameter type values.
    ''' </summary>
    ''' <param name="type">If type value is Authorization_code, access token is fetch for authorization code flow
    ''' If type value is Refresh_Token, access token is fetch for authorization code floww based on the exisiting refresh token</param>
    ''' <returns>true/false; true if success, else false</returns>
    Private Function GetAccessToken(ByVal type As AccessTokenType) As Boolean
        Dim postStream As Stream = Nothing
        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/token")
            accessTokenRequest.Method = "POST"

            Dim oauthParameters As String = String.Empty

            If type = AccessTokenType.Authorization_Code Then
                oauthParameters = "client_id=" & Me.apiKey.ToString() & "&client_secret=" & Me.secretKey & "&code=" & Me.authCode & "&grant_type=authorization_code&scope=" & Me.scope
            ElseIf type = AccessTokenType.Refresh_Token Then
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
                Dim access_token_json As String = accessTokenResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(access_token_json, GetType(AccessTokenResponse)), AccessTokenResponse)
                If deserializedJsonObj.access_token IsNot Nothing Then
                    Me.accessToken = deserializedJsonObj.access_token
                    Me.refreshToken = deserializedJsonObj.refresh_token
                    Me.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString()

                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(Me.refreshTokenExpiresIn)

                    If deserializedJsonObj.expires_in.Equals("0") Then
                        Dim defaultAccessTokenExpiresIn As Integer = 100
                        ' In Years
                        Me.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToString()
                    End If

                    Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()

                    Session("tl_session_access_token") = Me.accessToken
                    Session("tl_session_refresh_token") = Me.refreshToken
                    Session("tl_session_accessTokenExpiryTime") = Me.accessTokenExpiryTime
                    Session("tl_session_refreshTokenExpiryTime") = Me.refreshTokenExpiryTime
                    Session("tl_session_appState") = "TokenReceived"

                    accessTokenResponseStream.Close()
                    Return True
                Else
                    Me.DrawPanelForFailure(tlPanel, "Auth server returned null access token")
                End If
            End Using
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Dim streamReader As New StreamReader(stream)
                    Me.DrawPanelForFailure(tlPanel, streamReader.ReadToEnd())
                    streamReader.Close()
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(tlPanel, ex.Message)
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try

        Return False
    End Function

    ''' <summary>
    ''' Read parameters from configuraton file
    ''' </summary>
    ''' <returns>true/false; true if all required parameters are specified, else false</returns>
    Private Function ReadConfigFile() As Boolean
        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(tlPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(tlPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(tlPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.authorizeRedirectUri = ConfigurationManager.AppSettings("authorize_redirect_uri")
        If String.IsNullOrEmpty(Me.authorizeRedirectUri) Then
            Me.DrawPanelForFailure(tlPanel, "authorize_redirect_uri is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "TL"
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        Return True
    End Function

#End Region

#Region "Display Methods"

    ''' <summary>
    ''' Displays error message
    ''' </summary>
    ''' <param name="panelParam">Panel to draw error message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        Dim table As New Table()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.CssClass = "errorWide"
        table.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Text = message
        rowTwo.Controls.Add(rowTwoCellOne)
        table.Controls.Add(rowTwo)
        table.BorderWidth = 2
        table.BorderColor = Color.Red
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' This method is used to draw table for successful response of get device locations
    ''' </summary>
    ''' <param name="attribute">string, attribute to be displayed</param>
    ''' <param name="value">string, value to be displayed</param>
    ''' <param name="headerFlag">boolean, flag indicating to draw header panel</param>
    Private Sub DrawPanelForGetLocationResult(ByVal attribute As String, ByVal value As String, ByVal headerFlag As Boolean)
        If headerFlag = True Then
            Me.getStatusTable = New Table()
            Me.getStatusTable.CssClass = "successWide"
            Dim rowOne As New TableRow()
            Dim rowOneCellOne As New TableCell()
            rowOneCellOne.Font.Bold = True
            rowOneCellOne.Text = "SUCCESS:"
            rowOne.Controls.Add(rowOneCellOne)
            Me.getStatusTable.Controls.Add(rowOne)
            tlPanel.Controls.Add(Me.getStatusTable)
        Else
            Dim row As New TableRow()
            Dim cell1 As New TableCell()
            Dim cell2 As New TableCell()
            cell1.Text = attribute.ToString()
            cell1.Font.Bold = True
            cell1.Width = Unit.Pixel(100)
            row.Controls.Add(cell1)
            cell2.Text = value.ToString()
            row.Controls.Add(cell2)
            Me.getStatusTable.Controls.Add(row)
        End If
    End Sub

#End Region
End Class

#Region "Data Structures"

''' <summary>
''' Access Token Data Structure
''' </summary>
Public Class AccessTokenResponse
    ''' <summary>
    ''' Gets or sets Access Token ID
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
    ''' Gets or sets Refresh Token ID
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
    ''' Gets or sets Expires in milli seconds
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
''' Terminal Location Response object
''' </summary>
Public Class TLResponse
    ''' <summary>
    ''' Gets or sets the value of accuracy - This is the target MSISDN that was used in the Device Location request
    ''' </summary>
    Public Property accuracy() As String
        Get
            Return m_accuracy
        End Get
        Set(ByVal value As String)
            m_accuracy = Value
        End Set
    End Property
    Private m_accuracy As String

    ''' <summary>
    ''' Gets or sets the value of latitude - The current latitude of the device's geo-position.
    ''' </summary>
    Public Property latitude() As String
        Get
            Return m_latitude
        End Get
        Set(ByVal value As String)
            m_latitude = Value
        End Set
    End Property
    Private m_latitude As String

    ''' <summary>
    ''' Gets or sets the value of longitude - The current longitude of the device geo-position.
    ''' </summary>
    Public Property longitude() As String
        Get
            Return m_longitude
        End Get
        Set(ByVal value As String)
            m_longitude = Value
        End Set
    End Property
    Private m_longitude As String

    ''' <summary>
    ''' Gets or sets the value of timestamp - Timestamp of the location data.
    ''' </summary>
    Public Property timestamp() As String
        Get
            Return m_timestamp
        End Get
        Set(ByVal value As String)
            m_timestamp = Value
        End Set
    End Property
    Private m_timestamp As String
End Class
#End Region
