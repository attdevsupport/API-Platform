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
        If Session("vb_tl_session_access_token") IsNot Nothing Then
            access_token = Session("vb_tl_session_access_token").ToString()
        Else
            access_token = Nothing
        End If
        If Session("vb_tl_session_expiryMilliSeconds") IsNot Nothing Then
            expiryMilliSeconds = Session("vb_tl_session_expiryMilliSeconds").ToString()
        Else
            expiryMilliSeconds = Nothing
        End If
        If Session("vb_tl_session_refresh_token") IsNot Nothing Then
            refresh_token = Session("vb_tl_session_refresh_token").ToString()
        Else
            refresh_token = Nothing
        End If
        If Session("vb_tl_session_lastTokenTakenTime") IsNot Nothing Then
            lastTokenTakenTime = Session("vb_tl_session_lastTokenTakenTime").ToString()
        Else
            lastTokenTakenTime = Nothing
        End If
        If Session("vb_tl_session_refreshTokenExpiryTime") IsNot Nothing Then
            refreshTokenExpiryTime = Session("vb_tl_session_refreshTokenExpiryTime").ToString()
        Else
            refreshTokenExpiryTime = Nothing
        End If
    End Sub
    ' This function resets access token related session variable to null 

    Public Sub resetTokenSessionVariables()
        Session("vb_tl_session_access_token") = Nothing
        Session("vb_tl_session_expiryMilliSeconds") = Nothing
        Session("vb_tl_session_refresh_token") = Nothing
        Session("vb_tl_session_lastTokenTakenTime") = Nothing
        Session("vb_tl_session_refreshTokenExpiryTime") = Nothing
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
    ' * otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    ' * return REFRESH_TOKEN, if access token in expired and refresh token is valid
    ' 

    Public Function isTokenValid() As String
        Try
            If Session("vb_tl_session_access_token") Is Nothing Then
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
            'drawPanelForFailure(dcPanel, ex.ToString());
            Return "INVALID_ACCESS_TOKEN"
        End Try
    End Function
    ' This function redirects to authentication server to get the access code 

    Public Sub getAuthCode()
        Try
            Response.Redirect("" & FQDN & "/oauth/authorize?scope=" & scope & "&client_id=" & api_key & "&redirect_url=" & authorize_redirect_uri)
        Catch ex As Exception
            drawPanelForFailure(tlPanel, ex.ToString())
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
                        Session("vb_tl_session_access_token") = access_token.ToString()
                        Session("vb_tl_session_expiryMilliSeconds") = expiryMilliSeconds.ToString()
                        Session("vb_tl_session_refresh_token") = refresh_token.ToString()
                        Session("vb_tl_session_lastTokenTakenTime") = lastTokenTakenTime.ToString()
                        Session("vb_tl_session_refreshTokenExpiryTime") = refreshTokenExpiryTime.ToString()
                        accessTokenResponseStream.Close()
                        Return True
                    Else
                        drawPanelForFailure(tlPanel, "Auth server returned null access token")

                        Return False
                    End If
                End Using
            Catch ex As Exception
                drawPanelForFailure(tlPanel, ex.ToString())
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
                        Session("vb_tl_session_access_token") = access_token.ToString()
                        Session("vb_tl_session_expiryMilliSeconds") = expiryMilliSeconds.ToString()
                        Session("vb_tl_session_refresh_token") = refresh_token.ToString()
                        Session("vb_tl_session_lastTokenTakenTime") = lastTokenTakenTime.ToString()
                        Session("vb_tl_session_refreshTokenExpiryTime") = refreshTokenExpiryTime.ToString()
                        accessTokenResponseStream.Close()
                        Return True
                    Else
                        drawPanelForFailure(tlPanel, "Auth server returned null access token")
                        Return False
                    End If
                End Using
            Catch ex As Exception
                drawPanelForFailure(tlPanel, ex.ToString())
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


    Public Shared Sub BypassCertificateError()
        ServicePointManager.ServerCertificateValidationCallback = DirectCast([Delegate].Combine(ServicePointManager.ServerCertificateValidationCallback, Function(sender1 As [Object], certificate As X509Certificate, chain As X509Chain, sslPolicyErrors As SslPolicyErrors) True), RemoteCertificateValidationCallback)
    End Sub

    '
    '    * This function is called when the applicaiton page is loaded into the browser.
    '    * This fucntion reads the web.config and gets the values of the attributes
    '    * 
    '    

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'BypassCertificateError()
            map_canvas.Visible = False
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
                drawPanelForFailure(tlPanel, "FQDN is not defined in configuration file")
                Return
            End If
            FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
            If ConfigurationManager.AppSettings("api_key") Is Nothing Then
                drawPanelForFailure(tlPanel, "api_key is not defined in configuration file")
                Return
            End If
            api_key = ConfigurationManager.AppSettings("api_key").ToString()
            If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
                drawPanelForFailure(tlPanel, "secret_key is not defined in configuration file")
                Return
            End If
            secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
            If ConfigurationManager.AppSettings("scope") IsNot Nothing Then

                scope = ConfigurationManager.AppSettings("scope").ToString()
            Else
                scope = "TL"
            End If

            If ConfigurationManager.AppSettings("authorize_redirect_uri") Is Nothing Then
                drawPanelForFailure(tlPanel, "authorize_redirect_uri is not defined in configuration file")
                Return
            End If
            authorize_redirect_uri = ConfigurationManager.AppSettings("authorize_redirect_uri").ToString()
            If (Session("vb_tl_session_appState") IsNot Nothing) AndAlso (Request("Code") IsNot Nothing) Then
                auth_code = Request("code").ToString()
                If getAccessToken(0) = True Then
                    readTokenSessionVariables()
                Else
                    drawPanelForFailure(tlPanel, "Failed to get Access token")
                    resetTokenSessionVariables()
                    resetTokenVariables()
                    Session("vb_tl_session_DeviceIdForWhichTokenAcquired") = Nothing
                    Return
                End If
            End If
            If Session("vb_tl_session_appState") IsNot Nothing Then
                Session("vb_tl_session_appState") = Nothing
                If Session("vb_tl_session_GdeviceID") IsNot Nothing Then
                    Session("vb_tl_session_DeviceIdForWhichTokenAcquired") = Session("vb_tl_session_GdeviceID").ToString()
                    Session("vb_tl_session_GdeviceID") = Nothing
                End If
                tlTextBox.Text = Session("vb_tl_session_deviceID").ToString()
                Radio_AcceptedAccuracy.SelectedIndex = Convert.ToInt32(Session("vb_tl_session_acceptableAccuracy").ToString())
                Radio_RequestedAccuracy.SelectedIndex = Convert.ToInt32(Session("vb_tl_session_requestedAccuracy").ToString())
                Radio_DelayTolerance.SelectedIndex = Convert.ToInt32(Session("vb_tl_session_tolerance").ToString())
                Session("vb_tl_session_deviceID") = Nothing
                Session("vb_tl_session_acceptableAccuracy") = Nothing
                Session("vb_tl_session_requestedAccuracy") = Nothing
                Session("vb_tl_session_tolerance") = Nothing
                tlButton_Click1(Nothing, Nothing)
            End If
        Catch ex As Exception
            drawPanelForFailure(tlPanel, ex.ToString())
        End Try

    End Sub

    'This funciton checks the validity of string as msisdn 

    Private Function isValidMISDN(ByVal number As String) As [Boolean]
        Dim smsAddressInput As String = number
        Dim smsAddressFormatted As String
        Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"
        If System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
            smsAddressFormatted = smsAddressInput.Replace("-", "")
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

    'This method is used to draw table for successful response of get device locations 


    Private Sub drawPanelForGetLocationResult(ByVal attribute As String, ByVal value As String, ByVal headerFlag As Boolean)
        If headerFlag = True Then
            getStatusTable = New Table()
            getStatusTable.Font.Name = "Sans-serif"
            getStatusTable.Font.Size = 9
            getStatusTable.BorderStyle = BorderStyle.Outset
            getStatusTable.Width = Unit.Pixel(650)
            Dim rowOne As New TableRow()
            Dim rowOneCellOne As New TableCell()
            rowOneCellOne.Font.Bold = True
            rowOneCellOne.Text = "SUCCESS:"
            rowOne.Controls.Add(rowOneCellOne)
            getStatusTable.BorderWidth = 2
            getStatusTable.BorderColor = Color.DarkGreen
            getStatusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
            getStatusTable.Controls.Add(rowOne)
            tlPanel.Controls.Add(getStatusTable)
        Else
            Dim row As New TableRow()
            Dim cell1 As New TableCell()
            Dim cell2 As New TableCell()
            cell1.Text = attribute.ToString()
            cell1.Font.Bold = True
            cell1.Width = Unit.Pixel(100)
            'cell1.BorderWidth = 1;
            row.Controls.Add(cell1)
            cell2.Text = value.ToString()
            'cell2.BorderWidth = 1;
            row.Controls.Add(cell2)
            getStatusTable.Controls.Add(row)
        End If
    End Sub

    ' this function is called when user clicks on get location button 


    Protected Sub tlButton_Click1(ByVal sender As Object, ByVal e As EventArgs)
        Try

            If isValidMISDN(tlTextBox.Text.ToString()) = False Then
                drawPanelForFailure(tlPanel, "Invalid Number")
                Return
            End If
            Dim deviceId As String = tlTextBox.Text.ToString().Replace("tel:+1", "")
            deviceId = deviceId.ToString().Replace("tel:+", "")
            deviceId = deviceId.ToString().Replace("tel:1", "")
            deviceId = deviceId.ToString().Replace("tel:", "")
            deviceId = deviceId.ToString().Replace("tel:", "")
            deviceId = deviceId.ToString().Replace("+1", "")
            deviceId = deviceId.ToString().Replace("-", "")
            If deviceId.Length = 11 Then
                deviceId = deviceId.Remove(0, 1)
            End If
            Dim strResult As [String]
            Dim definedReqAccuracy As Integer() = New Integer(2) {100, 1000, 10000}
            Dim definedTolerance As String() = New String(2) {"NoDelay", "LowDelay", "DelayTolerant"}
            Dim requestedAccuracy As Integer, acceptableAccuracy As Integer
            Dim tolerance As String
            acceptableAccuracy = definedReqAccuracy(Radio_AcceptedAccuracy.SelectedIndex)
            requestedAccuracy = definedReqAccuracy(Radio_RequestedAccuracy.SelectedIndex)
            tolerance = definedTolerance(Radio_DelayTolerance.SelectedIndex)
            readTokenSessionVariables()
            Dim tokentResult As String = isTokenValid()
            If tokentResult.CompareTo("INVALID_ACCESS_TOKEN") = 0 Then
                Session("vb_tl_session_appState") = "GetToken"
                Session("vb_tl_session_deviceID") = tlTextBox.Text.ToString()
                Session("vb_tl_session_acceptableAccuracy") = Radio_AcceptedAccuracy.SelectedIndex
                Session("vb_tl_session_requestedAccuracy") = Radio_RequestedAccuracy.SelectedIndex
                Session("vb_tl_session_tolerance") = Radio_DelayTolerance.SelectedIndex
                Session("vb_tl_session_GdeviceID") = deviceId.ToString()
                getAuthCode()
            ElseIf tokentResult.CompareTo("REFRESH_TOKEN") = 0 Then
                If getAccessToken(2) = True Then
                    readTokenSessionVariables()
                Else
                    drawPanelForFailure(tlPanel, "Failed to get Access token")
                    resetTokenSessionVariables()
                    resetTokenVariables()
                    Session("vb_tl_session_DeviceIdForWhichTokenAcquired") = Nothing
                    Return
                End If
            End If
            If (Session("vb_tl_session_DeviceIdForWhichTokenAcquired") IsNot Nothing) AndAlso (Session("vb_tl_session_DeviceIdForWhichTokenAcquired").ToString().CompareTo(deviceId.ToString()) <> 0) Then
                'if ((Session["vb_tl_session_DeviceIdForWhichTokenAcquired"] != null) && (Session["vb_tl_session_DeviceIdForWhichTokenAcquired"].ToString().CompareTo(tlTextBox.Text.ToString()) != 0))
                resetTokenSessionVariables()
                resetTokenVariables()
                Session("vb_tl_session_appState") = "GetToken"
                Session("vb_tl_session_GdeviceID") = deviceId.ToString()
                Session("vb_tl_session_deviceID") = tlTextBox.Text.ToString()
                Session("vb_tl_session_acceptableAccuracy") = Radio_AcceptedAccuracy.SelectedIndex
                Session("vb_tl_session_requestedAccuracy") = Radio_RequestedAccuracy.SelectedIndex
                Session("vb_tl_session_tolerance") = Radio_DelayTolerance.SelectedIndex
                getAuthCode()
            End If
            Dim tlRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/1/devices/tel:" & deviceId.ToString() & "/location?access_token=" & access_token.ToString() & "&requestedAccuracy=" & requestedAccuracy.ToString() & "&acceptableAccuracy=" & acceptableAccuracy.ToString() & "&tolerance=" & tolerance.ToString()), HttpWebRequest)
            tlRequestObject.Method = "GET"
            Dim msgSentTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim tlResponseObject As HttpWebResponse = DirectCast(tlRequestObject.GetResponse(), HttpWebResponse)
            Dim msgReceivedTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim tokenSpan As TimeSpan = msgReceivedTime.Subtract(msgSentTime)
            ' the using keyword will automatically dispose the object 
            ' once complete
            Using tlResponseStream As New StreamReader(tlResponseObject.GetResponseStream())
                strResult = tlResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As tlResponse = DirectCast(deserializeJsonObject.Deserialize(strResult, GetType(tlResponse)), tlResponse)
                drawPanelForGetLocationResult("", "", True)
                drawPanelForGetLocationResult("Accuracy:", deserializedJsonObj.accuracy.ToString(), False)
                drawPanelForGetLocationResult("Altitude:", deserializedJsonObj.altitude.ToString(), False)
                drawPanelForGetLocationResult("Latitude:", deserializedJsonObj.latitude.ToString(), False)
                drawPanelForGetLocationResult("Longitude:", deserializedJsonObj.longitude.ToString(), False)
                drawPanelForGetLocationResult("TimeStamp:", deserializedJsonObj.timestamp.ToString(), False)
                drawPanelForGetLocationResult("Response Time:", tokenSpan.Seconds.ToString() & "seconds", False)
                MapTerminalLocation.Visible = True
                map_canvas.Visible = True
                Dim googleString As New StringBuilder()
                googleString.Append("http://maps.google.com/?q=" & deserializedJsonObj.latitude.ToString() & "+" & deserializedJsonObj.longitude.ToString() & "&output=embed")
                MapTerminalLocation.Attributes("src") = googleString.ToString()
                ' Close and clean up the StreamReader
                tlResponseStream.Close()
            End Using
        Catch ex As Exception
            drawPanelForFailure(tlPanel, ex.ToString())
        End Try
    End Sub
End Class

' Following are the data structures used for this applicaiton 


Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String

End Class
Public Class tlResponse
    Public Property accuracy() As String
        Get
            Return m_accuracy
        End Get
        Set(ByVal value As String)
            m_accuracy = Value
        End Set
    End Property
    Private m_accuracy As String
    Public Property altitude() As Double
        Get
            Return m_altitude
        End Get
        Set(ByVal value As Double)
            m_altitude = Value
        End Set
    End Property
    Private m_altitude As Double
    Public Property latitude() As Double
        Get
            Return m_latitude
        End Get
        Set(ByVal value As Double)
            m_latitude = Value
        End Set
    End Property
    Private m_latitude As Double
    Public Property longitude() As String
        Get
            Return m_longitude
        End Get
        Set(ByVal value As String)
            m_longitude = Value
        End Set
    End Property
    Private m_longitude As String
    Public Property timestamp() As DateTime
        Get
            Return m_timestamp
        End Get
        Set(ByVal value As DateTime)
            m_timestamp = Value
        End Set
    End Property
    Private m_timestamp As DateTime
End Class