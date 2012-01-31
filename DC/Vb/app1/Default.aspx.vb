'Licensed by AT&T under 'Software Development Kit Tools Agreement.' September 2011
'TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
'Copyright 2011 AT&T Intellectual Property. All rights reserved. http://developer.att.com
'For more information contact developer.support@att.com

Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Net
Imports System.Configuration
Imports System.IO
Imports System.Xml
Imports System.Text
Imports System.Web.Script.Serialization
Imports System.Drawing
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System

Partial Public Class [Default]
    Inherits System.Web.UI.Page
    Private shortCode As String, FQDN As String, accessTokenFilePath As String, oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private getStatusTable As Table

    ' This function reads access token related session variables to local variables 

    Public Sub readTokenSessionVariables()
        If Session("vb_dc_session_access_token") IsNot Nothing Then
            access_token = Session("vb_dc_session_access_token").ToString()
        Else
            access_token = Nothing
        End If
        If Session("vb_dc_session_expiryMilliSeconds") IsNot Nothing Then
            expiryMilliSeconds = Session("vb_dc_session_expiryMilliSeconds").ToString()
        Else
            expiryMilliSeconds = Nothing
        End If
        If Session("vb_dc_session_refresh_token") IsNot Nothing Then
            refresh_token = Session("vb_dc_session_refresh_token").ToString()
        Else
            refresh_token = Nothing
        End If
        If Session("vb_dc_session_lastTokenTakenTime") IsNot Nothing Then
            lastTokenTakenTime = Session("vb_dc_session_lastTokenTakenTime").ToString()
        Else
            lastTokenTakenTime = Nothing
        End If
        If Session("vb_dc_session_refreshTokenExpiryTime") IsNot Nothing Then
            refreshTokenExpiryTime = Session("vb_dc_session_refreshTokenExpiryTime").ToString()
        Else
            refreshTokenExpiryTime = Nothing
        End If
    End Sub

    ' This function resets access token related session variable to null 

    Public Sub resetTokenSessionVariables()
        Session("vb_dc_session_access_token") = Nothing
        Session("vb_dc_session_expiryMilliSeconds") = Nothing
        Session("vb_dc_session_refresh_token") = Nothing
        Session("vb_dc_session_lastTokenTakenTime") = Nothing
        Session("vb_dc_session_refreshTokenExpiryTime") = Nothing
    End Sub
    ' This function resets access token related  variable to null 

    Public Sub resetTokenVariables()
        access_token = Nothing
        expiryMilliSeconds = Nothing
        refresh_token = Nothing
        lastTokenTakenTime = Nothing
        refreshTokenExpiryTime = Nothing
    End Sub

    ' This function validates access token related variables and returns VALID_ACCESS_TOKEN if its valid
    '     * otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    '     * return REFRESH_TOKEN, if access token in expired and refresh token is valid
    '     

    Public Function isTokenValid() As String
        Try
            If Session("vb_dc_session_access_token") Is Nothing Then
                Return "INVALID_ACCESS_TOKEN"
            End If
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim lastRefreshTokenTime As DateTime = DateTime.Parse(refreshTokenExpiryTime)
            Dim refreshSpan As TimeSpan = currentServerTime.Subtract(lastRefreshTokenTime)
            If currentServerTime >= lastRefreshTokenTime Then
                Return "INVALID_ACCESS_TOKEN"
            End If
            Dim lastTokenTime As DateTime = DateTime.Parse(lastTokenTakenTime)
            Dim tokenSpan As TimeSpan = currentServerTime.Subtract(lastTokenTime)
            If ((tokenSpan.TotalSeconds)) > Convert.ToInt32(expiryMilliSeconds) Then
                Return "REFRESH_TOKEN"
            Else
                Return "VALID_ACCESS_TOKEN"
            End If
        Catch ex As Exception
            Return "INVALID_ACCESS_TOKEN"
        End Try
    End Function

    ' This function is used to neglect the ssl handshake error with authentication server 

    Public Shared Sub BypassCertificateError()
        ServicePointManager.ServerCertificateValidationCallback = DirectCast([Delegate].Combine(ServicePointManager.ServerCertificateValidationCallback, Function(sender1 As [Object], certificate As X509Certificate, chain As X509Chain, sslPolicyErrors As SslPolicyErrors) True), RemoteCertificateValidationCallback)
    End Sub

    ' This function redirects to authentication server to get the access code 

    Public Sub getAuthCode()
        Try
            Response.Redirect("" & FQDN & "/oauth/authorize?scope=" & scope & "&client_id=" & api_key & "&redirect_url=" & authorize_redirect_uri)
        Catch ex As Exception
            drawPanelForFailure(dcPanel, ex.ToString())
        End Try
    End Sub

    ' This function get the access token based on the type parameter type values.
    '    * If type value is 0, access token is fetch for authorization code flow
    '    * If type value is 2, access token is fetch for authorization code floww based on the exisiting refresh token
    '    

    Public Function getAccessToken(ByVal type As Integer) As Boolean
        If type = 0 Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/token")
                accessTokenRequest.Method = "POST"
                Dim oauthParameters As String = "client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&code=" & auth_code.ToString() & "&grant_type=authorization_code"
                accessTokenRequest.ContentType = "application/x-www-form-urlencoded"
                'sendSmsRequestObject.Accept = "application/json";
                Dim encoding As New UTF8Encoding()
                Dim postBytes As Byte() = encoding.GetBytes(oauthParameters)
                accessTokenRequest.ContentLength = postBytes.Length
                Dim postStream As Stream = accessTokenRequest.GetRequestStream()
                postStream.Write(postBytes, 0, postBytes.Length)
                postStream.Close()
                Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
                Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                    Dim access_token_json As String = accessTokenResponseStream.ReadToEnd().ToString()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(access_token_json, GetType(AccessTokenResponse)), AccessTokenResponse)
                    If deserializedJsonObj.access_token IsNot Nothing Then
                        access_token = deserializedJsonObj.access_token.ToString()
                        expiryMilliSeconds = deserializedJsonObj.expires_in.ToString()
                        refresh_token = deserializedJsonObj.refresh_token.ToString()
                        lastTokenTakenTime = currentServerTime.ToLongDateString() & " " & currentServerTime.ToLongTimeString()
                        Dim refreshExpiry As DateTime = currentServerTime.AddHours(24)
                        refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()
                        Session("vb_dc_session_access_token") = access_token.ToString()
                        Session("vb_dc_session_expiryMilliSeconds") = expiryMilliSeconds.ToString()
                        Session("vb_dc_session_refresh_token") = refresh_token.ToString()
                        Session("vb_dc_session_lastTokenTakenTime") = lastTokenTakenTime.ToString()
                        Session("vb_dc_session_refreshTokenExpiryTime") = refreshTokenExpiryTime.ToString()
                        accessTokenResponseStream.Close()
                        Return True
                    Else
                        drawPanelForFailure(dcPanel, "Auth server returned null access token")

                        Return False
                    End If
                End Using
            Catch ex As Exception
                drawPanelForFailure(dcPanel, ex.ToString())
                Return False
            End Try
        ElseIf type = 2 Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/token")
                accessTokenRequest.Method = "POST"
                Dim oauthParameters As String = "grant_type=refresh_token&client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&refresh_token=" & refresh_token.ToString()
                accessTokenRequest.ContentType = "application/x-www-form-urlencoded"
                'sendSmsRequestObject.Accept = "application/json";
                Dim encoding As New UTF8Encoding()
                Dim postBytes As Byte() = encoding.GetBytes(oauthParameters)
                accessTokenRequest.ContentLength = postBytes.Length
                Dim postStream As Stream = accessTokenRequest.GetRequestStream()
                postStream.Write(postBytes, 0, postBytes.Length)
                postStream.Close()
                Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
                Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                    Dim access_token_json As String = accessTokenResponseStream.ReadToEnd().ToString()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(access_token_json, GetType(AccessTokenResponse)), AccessTokenResponse)
                    If deserializedJsonObj.access_token IsNot Nothing Then
                        access_token = deserializedJsonObj.access_token.ToString()
                        expiryMilliSeconds = deserializedJsonObj.expires_in.ToString()
                        refresh_token = deserializedJsonObj.refresh_token.ToString()
                        lastTokenTakenTime = currentServerTime.ToLongDateString() & " " & currentServerTime.ToLongTimeString()
                        Dim refreshExpiry As DateTime = currentServerTime.AddHours(24)
                        refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()
                        Session("vb_dc_session_access_token") = access_token.ToString()
                        Session("vb_dc_session_expiryMilliSeconds") = expiryMilliSeconds.ToString()
                        Session("vb_dc_session_refresh_token") = refresh_token.ToString()
                        Session("vb_dc_session_lastTokenTakenTime") = lastTokenTakenTime.ToString()
                        Session("vb_dc_session_refreshTokenExpiryTime") = refreshTokenExpiryTime.ToString()
                        accessTokenResponseStream.Close()
                        Return True
                    Else
                        drawPanelForFailure(dcPanel, "Auth server returned null access token")
                        Return False
                    End If
                End Using
            Catch ex As Exception
                drawPanelForFailure(dcPanel, ex.ToString())
                Return False
            End Try
        End If
        Return False
    End Function

    ' This funciton draws table for error response 

    Private Sub drawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
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
        'rowOneCellOne.BorderWidth = 1;
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        'rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        table.Controls.Add(rowTwo)
        table.BorderWidth = 2
        table.BorderColor = Color.Red
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(table)
    End Sub
    '
    '     * This function is called when the applicaiton page is loaded into the browser.
    '     * This fucntion reads the web.config and gets the values of the attributes
    '     * 
    '     

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'BypassCertificateError()
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
                drawPanelForFailure(dcPanel, "FQDN is not defined in configuration file")
                Return
            End If
            FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
            If ConfigurationManager.AppSettings("api_key") Is Nothing Then
                drawPanelForFailure(dcPanel, "api_key is not defined in configuration file")
                Return
            End If
            api_key = ConfigurationManager.AppSettings("api_key").ToString()
            If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
                drawPanelForFailure(dcPanel, "secret_key is not defined in configuration file")
                Return
            End If
            secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
            If ConfigurationManager.AppSettings("scope") Is Nothing Then
                scope = "DC"
            Else
                scope = ConfigurationManager.AppSettings("scope").ToString()
            End If
            If ConfigurationManager.AppSettings("authorize_redirect_uri") Is Nothing Then
                drawPanelForFailure(dcPanel, "authorize_redirect_uri is not defined in configuration file")
                Return
            End If
            authorize_redirect_uri = ConfigurationManager.AppSettings("authorize_redirect_uri").ToString()
            If (Session("vb_dc_session_appState") IsNot Nothing) AndAlso (Request("Code") IsNot Nothing) Then
                auth_code = Request("code").ToString()
                If getAccessToken(0) = True Then
                    readTokenSessionVariables()
                Else
                    drawPanelForFailure(dcPanel, "Failed to get Access token")
                    resetTokenSessionVariables()
                    resetTokenVariables()
                    Session("vb_dc_session_DeviceIdForWhichTokenAcquired") = Nothing
                    Return
                End If
            End If
            If Session("vb_dc_session_appState") IsNot Nothing Then
                Session("vb_dc_session_appState") = Nothing
                If Session("vb_dc_session_GdeviceID") IsNot Nothing Then
                    Session("vb_dc_session_DeviceIdForWhichTokenAcquired") = Session("vb_dc_session_GdeviceID").ToString()
                    Session("vb_dc_session_GdeviceID") = Nothing
                End If
                dcPhoneNumberTextBox.Text = Session("vb_dc_session_deviceID").ToString()
                Session("vb_dc_session_deviceID") = Nothing
                getDCCapabilities_Click(Nothing, Nothing)
            End If
        Catch ex As Exception
            drawPanelForFailure(dcPanel, ex.ToString())
        End Try

    End Sub
    'This funciton checks the validity of string as msisdn 


    Private Function isValidMISDN(ByVal number As String) As [Boolean]
        Dim smsAddressInput As String = number
        Dim tryParseResult As Long = 0
        Dim smsAddressFormatted As String
        Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"
        If System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
            smsAddressFormatted = smsAddressInput.Replace("-", "")
        Else
            smsAddressFormatted = smsAddressInput
        End If
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

    'This method is used to draw table for successful response of get device capabilities 


    Private Sub drawPanelForGetStatusResult(ByVal attribute As String, ByVal value As String, ByVal headerFlag As Boolean)
        If headerFlag = True Then
            Dim getStatusTableHeading As New Table()
            getStatusTableHeading.Font.Name = "Sans-serif"
            getStatusTableHeading.Font.Size = 9
            getStatusTableHeading.BorderStyle = BorderStyle.Outset
            getStatusTableHeading.Width = Unit.Pixel(650)
            Dim one As New TableRow()
            Dim cell As New TableCell()
            cell.Text = "SUCCESS:"
            cell.Font.Bold = True
            one.Controls.Add(cell)
            Dim two As New TableRow()
            cell = New TableCell()
            cell.Text = "Device parameters listed below"
            two.Controls.Add(cell)
            getStatusTableHeading.Controls.Add(one)
            getStatusTableHeading.Controls.Add(two)
            getStatusTableHeading.BorderWidth = 2
            getStatusTableHeading.BorderColor = Color.DarkGreen
            getStatusTableHeading.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
            getStatusTableHeading.Font.Size = 9
            dcPanel.Controls.Add(getStatusTableHeading)

            getStatusTable = New Table()
            getStatusTable.Font.Size = 9
            getStatusTable.Font.Name = "Sans-serif"
            getStatusTable.Font.Italic = True
            'getStatusTable.HorizontalAlign = HorizontalAlign.Center;
            Dim rowOne As New TableRow()
            Dim rowOneCellOne As New TableCell()
            rowOneCellOne.Font.Bold = True
            'rowOneCellOne.BorderWidth = 1;
            rowOneCellOne.Text = "Parameter"
            rowOneCellOne.HorizontalAlign = HorizontalAlign.Center
            rowOne.Controls.Add(rowOneCellOne)
            Dim rowOneCellTwo As New TableCell()
            rowOneCellTwo.Font.Bold = True
            'rowOneCellTwo.BorderWidth = 1;
            rowOneCellTwo.Text = "Value"
            rowOneCellTwo.HorizontalAlign = HorizontalAlign.Center
            rowOne.Controls.Add(rowOneCellTwo)
            'getStatusTable.BorderWidth = 2;
            'getStatusTable.BorderColor = Color.DarkGreen;
            'getStatusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
            getStatusTable.Controls.Add(rowOne)
            dcPanel.Controls.Add(getStatusTable)
        Else
            Dim row As New TableRow()
            Dim cell1 As New TableCell()
            Dim cell2 As New TableCell()
            cell1.Text = attribute.ToString()
            'cell1.BorderWidth = 1;
            cell1.HorizontalAlign = HorizontalAlign.Center
            row.Controls.Add(cell1)
            cell2.Text = value.ToString()
            'cell2.BorderWidth = 1;
            cell2.HorizontalAlign = HorizontalAlign.Center
            row.Controls.Add(cell2)
            getStatusTable.Controls.Add(row)
        End If
    End Sub
    '
    '    * User invoked Event to get device Information
    ' 

    Protected Sub getDCCapabilities_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Try
                If isValidMISDN(dcPhoneNumberTextBox.Text.ToString()) = False Then
                    drawPanelForFailure(dcPanel, "Invalid Number")
                    Return
                End If
                Dim deviceId As String = dcPhoneNumberTextBox.Text.ToString().Replace("tel:+1", "")
                deviceId = deviceId.ToString().Replace("tel:+", "")
                deviceId = deviceId.ToString().Replace("tel:1", "")
                deviceId = deviceId.ToString().Replace("tel:", "")
                deviceId = deviceId.ToString().Replace("tel:", "")
                deviceId = deviceId.ToString().Replace("+1", "")
                deviceId = deviceId.ToString().Replace("-", "")
                If deviceId.Length = 11 Then
                    deviceId = deviceId.Remove(0, 1)
                End If
                Dim dcResponseData As [String]
                readTokenSessionVariables()
                Dim tokentResult As String = isTokenValid()
                If tokentResult.CompareTo("INVALID_ACCESS_TOKEN") = 0 Then
                    Session("vb_dc_session_appState") = "GetToken"
                    Session("vb_dc_session_deviceID") = dcPhoneNumberTextBox.Text.ToString()
                    Session("vb_dc_session_GdeviceID") = deviceId.ToString()
                    getAuthCode()
                ElseIf tokentResult.CompareTo("REFRESH_TOKEN") = 0 Then
                    If getAccessToken(2) = True Then
                        readTokenSessionVariables()
                    Else
                        drawPanelForFailure(dcPanel, "Failed to get Access token")
                        resetTokenSessionVariables()
                        resetTokenVariables()
                        Session("vb_dc_session_DeviceIdForWhichTokenAcquired") = Nothing
                        Return
                    End If
                End If
                If (Session("vb_dc_session_DeviceIdForWhichTokenAcquired") IsNot Nothing) AndAlso (Session("vb_dc_session_DeviceIdForWhichTokenAcquired").ToString().CompareTo(deviceId.ToString()) <> 0) Then
                    resetTokenSessionVariables()
                    resetTokenVariables()
                    Session("vb_dc_session_appState") = "GetToken"
                    Session("vb_dc_session_deviceID") = dcPhoneNumberTextBox.Text.ToString()
                    Session("vb_dc_session_GdeviceID") = deviceId.ToString()
                    getAuthCode()
                End If
                'readCheckVerifyAccessToken();
                ' Form Http Web Request
                Dim deviceInfoRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/1/devices/tel:" & deviceId.ToString() & "/info?access_token=" & access_token.ToString()), HttpWebRequest)
                deviceInfoRequestObject.Method = "GET"

                Dim deviceInfoResponse As HttpWebResponse = DirectCast(deviceInfoRequestObject.GetResponse(), HttpWebResponse)
                Using deviceInfoResponseStream As New StreamReader(deviceInfoResponse.GetResponseStream())
                    dcResponseData = deviceInfoResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As DeviceCapabilities = DirectCast(deserializeJsonObject.Deserialize(dcResponseData, GetType(DeviceCapabilities)), DeviceCapabilities)
                    drawPanelForGetStatusResult("", "", True)
                    drawPanelForGetStatusResult("acwmodel", deserializedJsonObj.deviceId.acwmodel.ToString(), False)
                    drawPanelForGetStatusResult("acwdevcert", deserializedJsonObj.deviceId.acwdevcert.ToString(), False)
                    drawPanelForGetStatusResult("acwrel", deserializedJsonObj.deviceId.acwrel.ToString(), False)
                    drawPanelForGetStatusResult("acwvendor", deserializedJsonObj.deviceId.acwvendor.ToString(), False)
                    drawPanelForGetStatusResult("acwaocr", deserializedJsonObj.capabilities.acwaocr.ToString(), False)
                    drawPanelForGetStatusResult("acwav", deserializedJsonObj.capabilities.acwav.ToString(), False)
                    drawPanelForGetStatusResult("acwcf", deserializedJsonObj.capabilities.acwcf.ToString(), False)
                    drawPanelForGetStatusResult("acwtermtype", deserializedJsonObj.capabilities.acwtermtype.ToString(), False)
                    'Session["DeviceIdForWhichTokenAcquired"] = deviceId.ToString();
                    deviceInfoResponseStream.Close()
                End Using
            Catch ex As Exception
                drawPanelForFailure(dcPanel, ex.ToString())
            End Try
        Catch ex As Exception
            drawPanelForFailure(dcPanel, ex.ToString())
        End Try
    End Sub

End Class

' Below are data structures used for application 


Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String
End Class

Public Class DeviceCapabilities
    Public Property deviceId() As DeviceId
        Get
            Return m_deviceId
        End Get
        Set(ByVal value As DeviceId)
            m_deviceId = Value
        End Set
    End Property
    Private m_deviceId As DeviceId
    Public Property capabilities() As Capabilities
        Get
            Return m_capabilities
        End Get
        Set(ByVal value As Capabilities)
            m_capabilities = Value
        End Set
    End Property
    Private m_capabilities As Capabilities
End Class

Public Class DeviceId
    Public Property acwdevcert() As String
        Get
            Return m_acwdevcert
        End Get
        Set(ByVal value As String)
            m_acwdevcert = Value
        End Set
    End Property
    Private m_acwdevcert As String
    Public Property acwrel() As String
        Get
            Return m_acwrel
        End Get
        Set(ByVal value As String)
            m_acwrel = Value
        End Set
    End Property
    Private m_acwrel As String
    Public Property acwmodel() As String
        Get
            Return m_acwmodel
        End Get
        Set(ByVal value As String)
            m_acwmodel = Value
        End Set
    End Property
    Private m_acwmodel As String
    Public Property acwvendor() As String
        Get
            Return m_acwvendor
        End Get
        Set(ByVal value As String)
            m_acwvendor = Value
        End Set
    End Property
    Private m_acwvendor As String
End Class

Public Class Capabilities
    Public Property acwav() As String
        Get
            Return m_acwav
        End Get
        Set(ByVal value As String)
            m_acwav = Value
        End Set
    End Property
    Private m_acwav As String
    Public Property acwaocr() As String
        Get
            Return m_acwaocr
        End Get
        Set(ByVal value As String)
            m_acwaocr = Value
        End Set
    End Property
    Private m_acwaocr As String
    Public Property acwcf() As String
        Get
            Return m_acwcf
        End Get
        Set(ByVal value As String)
            m_acwcf = Value
        End Set
    End Property
    Private m_acwcf As String
    Public Property acwtermtype() As String
        Get
            Return m_acwtermtype
        End Get
        Set(ByVal value As String)
            m_acwtermtype = Value
        End Set
    End Property
    Private m_acwtermtype As String
End Class