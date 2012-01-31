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
Imports System.IO
Imports System.Web.Services
Imports System.Text
Imports System.Configuration
Imports System.Web.Script.Serialization
Imports System.Net.Sockets
Imports System.Security.Cryptography.X509Certificates
Imports System.Net.Security
Imports System.Collections.Specialized
Imports System.Drawing
Imports System
Imports System.Net.Mail


Partial Public Class [Default]
    Inherits System.Web.UI.Page
    Private shortCode As String, FQDN As String, accessTokenFilePath As String, oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private shortCodes As String()
    Private wapFilePath As String
    Public Function readAccessTokenFile() As Boolean
        Try
            Dim file As New FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read)
            Dim sr As New StreamReader(file)
            access_token = sr.ReadLine()
            expiryMilliSeconds = sr.ReadLine()
            refresh_token = sr.ReadLine()
            lastTokenTakenTime = sr.ReadLine()
            refreshTokenExpiryTime = sr.ReadLine()
            sr.Close()
            file.Close()
        Catch ex As Exception
            Return False
        End Try
        If (access_token Is Nothing) OrElse (expiryMilliSeconds Is Nothing) OrElse (refresh_token Is Nothing) OrElse (lastTokenTakenTime Is Nothing) OrElse (refreshTokenExpiryTime Is Nothing) Then
            Return False
        End If
        Return True
    End Function

    ' This function validates the expiry of the access token and refresh token,
    '     * function compares the current time with the refresh token taken time, if current time is greater then 
    '     * returns INVALID_REFRESH_TOKEN
    '     * function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    '     * funciton returns INVALID_ACCESS_TOKEN
    '     * otherwise returns VALID_ACCESS_TOKEN
    '    

    Public Function isTokenValid() As String
        Try

            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim lastRefreshTokenTime As DateTime = DateTime.Parse(refreshTokenExpiryTime)
            Dim refreshSpan As TimeSpan = currentServerTime.Subtract(lastRefreshTokenTime)
            If currentServerTime >= lastRefreshTokenTime Then
                Return "INVALID_ACCESS_TOKEN"
            End If
            Dim lastTokenTime As DateTime = DateTime.Parse(lastTokenTakenTime)
            Dim tokenSpan As TimeSpan = currentServerTime.Subtract(lastTokenTime)
            If ((tokenSpan.TotalSeconds)) > Convert.ToInt32(expiryMilliSeconds) Then
                Return "REFRESH_ACCESS_TOKEN"
            Else
                Return "VALID_ACCESS_TOKEN"
            End If
        Catch ex As Exception
            Return "INVALID_ACCESS_TOKEN"
        End Try
    End Function


    ' This function get the access token based on the type parameter type values.
    '     * If type value is 1, access token is fetch for client credential flow
    '     * If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    '     

    Public Function getAccessToken(ByVal type As Integer, ByVal panelParam As Panel) As Boolean
        '  This is client credential flow: 

        If type = 1 Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/access_token?client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&grant_type=client_credentials&scope=WAP")
                accessTokenRequest.Method = "GET"

                Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
                Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                    Dim jsonAccessToken As String = accessTokenResponseStream.ReadToEnd().ToString()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(jsonAccessToken, GetType(AccessTokenResponse)), AccessTokenResponse)
                    access_token = deserializedJsonObj.access_token.ToString()
                    expiryMilliSeconds = deserializedJsonObj.expires_in.ToString()
                    refresh_token = deserializedJsonObj.refresh_token.ToString()
                    Dim file As New FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write)
                    Dim sw As New StreamWriter(file)
                    sw.WriteLine(access_token)
                    sw.WriteLine(expiryMilliSeconds)
                    sw.WriteLine(refresh_token)
                    sw.WriteLine(currentServerTime.ToLongDateString() & " " & currentServerTime.ToLongTimeString())
                    lastTokenTakenTime = currentServerTime.ToLongDateString() & " " & currentServerTime.ToLongTimeString()
                    'Refresh token valids for 24 hours
                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(24)
                    refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()
                    sw.WriteLine(refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString())
                    sw.Close()
                    file.Close()
                    ' Close and clean up the StreamReader
                    accessTokenResponseStream.Close()
                    Return True
                End Using
            Catch ex As Exception
                drawPanelForFailure(panelParam, ex.ToString())
                Return False
            End Try
        ElseIf type = 2 Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/access_token?grant_type=refresh_token&client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&refresh_token=" & refresh_token.ToString())
                accessTokenRequest.Method = "GET"
                Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
                Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                    Dim access_token_json As String = accessTokenResponseStream.ReadToEnd().ToString()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(access_token_json, GetType(AccessTokenResponse)), AccessTokenResponse)
                    access_token = deserializedJsonObj.access_token.ToString()
                    expiryMilliSeconds = deserializedJsonObj.expires_in.ToString()
                    refresh_token = deserializedJsonObj.refresh_token.ToString()
                    Dim file As New FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write)
                    Dim sw As New StreamWriter(file)
                    sw.WriteLine(access_token)
                    sw.WriteLine(expiryMilliSeconds)
                    sw.WriteLine(refresh_token)
                    sw.WriteLine(currentServerTime.ToLongDateString() & " " & currentServerTime.ToLongTimeString())
                    lastTokenTakenTime = currentServerTime.ToLongDateString() & " " & currentServerTime.ToLongTimeString()
                    'Refresh token valids for 24 hours
                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(24)
                    refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()
                    sw.WriteLine(refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString())
                    sw.Close()
                    file.Close()
                    accessTokenResponseStream.Close()
                    Return True
                End Using
            Catch ex As Exception
                drawPanelForFailure(panelParam, ex.ToString())
                Return False
            End Try
        End If
        Return False
    End Function

    ' This function is used to neglect the ssl handshake error with authentication server 


    Public Shared Sub BypassCertificateError()
        ServicePointManager.ServerCertificateValidationCallback = DirectCast([Delegate].Combine(ServicePointManager.ServerCertificateValidationCallback, Function(sender1 As [Object], certificate As X509Certificate, chain As X509Chain, sslPolicyErrors As SslPolicyErrors) True), RemoteCertificateValidationCallback)
    End Sub
    ' This function is used to read access token file and validate the access token
    '     * this function returns true if access token is valid, or else false is returned
    '     

    Public Function readAndGetAccessToken(ByVal panelParam As Panel) As Boolean
        Dim result As Boolean = True
        If readAccessTokenFile() = False Then
            result = getAccessToken(1, panelParam)
        Else
            Dim tokenValidity As String = isTokenValid()
            If tokenValidity.CompareTo("REFRESH_ACCESS_TOKEN") = 0 Then
                result = getAccessToken(2, panelParam)
            ElseIf String.Compare(isTokenValid(), "INVALID_ACCESS_TOKEN") = 0 Then
                result = getAccessToken(1, panelParam)
            End If
        End If
        Return result
    End Function
    '
    '     * On page load if query string 'code' is present, invoke get_access_token
    '     

    Public Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'BypassCertificateError()
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            If ConfigurationManager.AppSettings("WAPFilePath") IsNot Nothing Then
                wapFilePath = ConfigurationManager.AppSettings("WAPFilePath")
            Else
                wapFilePath = "~\R2-csharp-dotnet\wap\app1\WAPText.txt"
            End If
            wapFilePath = Request.MapPath(wapFilePath)
            If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
                accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
            Else
                accessTokenFilePath = "~\WAPApp1AccessToken.txt"
            End If
            If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
                drawPanelForFailure(wapPanel, "FQDN is not defined in configuration file")
                Return
            End If
            FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
            If ConfigurationManager.AppSettings("api_key") Is Nothing Then
                drawPanelForFailure(wapPanel, "api_key is not defined in configuration file")
                Return
            End If
            api_key = ConfigurationManager.AppSettings("api_key").ToString()
            If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
                drawPanelForFailure(wapPanel, "secret_key is not defined in configuration file")
                Return
            End If
            secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
            If ConfigurationManager.AppSettings("scope") IsNot Nothing Then

                scope = ConfigurationManager.AppSettings("scope").ToString()
            Else
                scope = "WAP"
            End If
        Catch ex As Exception
            drawPanelForFailure(wapPanel, ex.ToString())
            Response.Write(ex.ToString())
        End Try

    End Sub

    ' This function is called to draw the table in the panelParam panel for success response 

    Private Sub drawPanelForSuccess(ByVal panelParam As Panel, ByVal message As String)
        Dim table As New Table()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        'rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Font.Bold = True
        rowTwoCellOne.Text = "Message ID:"
        rowTwoCellOne.Width = Unit.Pixel(70)
        'rowOneCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellOne)
        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellTwo.Text = message.ToString()
        'rowTwoCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellTwo)
        table.Controls.Add(rowTwo)
        table.BorderWidth = 2
        table.BorderColor = Color.DarkGreen
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        panelParam.Controls.Add(table)
    End Sub

    ' This function draws table for failed response in the panalParam panel 

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
    ' * This function is called when user clicks on send wap message button.
    ' * this funciton calls send wap message API to send the wap message.
    ' 

    Protected Sub btnSendWAP_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If readAndGetAccessToken(wapPanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    Return
                End If
                sendWapPush()
            End If
        Catch ex As Exception
            drawPanelForFailure(wapPanel, ex.ToString())
        End Try
    End Sub

    'this function validates string against the valid msisdn 

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

    ' This function calls send wap message api to send wap messsage 

    Private Sub sendWapPush()
        Try
            If isValidMISDN(txtAddressWAPPush.Text.ToString()) = False Then
                drawPanelForFailure(wapPanel, "Invalid Number: " & txtAddressWAPPush.Text.ToString())
                Return
            End If
            Dim wapAddress As String = txtAddressWAPPush.Text.ToString().Replace("tel:+1", "")
            wapAddress = wapAddress.ToString().Replace("tel:+", "")
            wapAddress = wapAddress.ToString().Replace("tel:1", "")
            wapAddress = wapAddress.ToString().Replace("tel:", "")
            wapAddress = wapAddress.ToString().Replace("tel:", "")
            wapAddress = wapAddress.ToString().Replace("-", "")

            Dim wapMessage As String = txtAlert.Text.ToString()
            Dim wapUrl As String = txtUrl.Text.ToString()

            Dim boundary As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

            Dim wapData As String = ""
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

            Dim wapFileWriter As StreamWriter = File.CreateText(wapFilePath)
            wapFileWriter.Write(wapData)
            wapFileWriter.Close()

            'string filename = Path.GetFileName(wapFilePath);
            'FileStream fs = new FileStream(wapFilePath, FileMode.Open, FileAccess.Read);
            'BinaryReader br = new BinaryReader(fs);
            'byte[] pushFile = br.ReadBytes((int)fs.Length);
            'br.Close();
            'fs.Close();
            Dim sr As New StreamReader(wapFilePath)
            Dim pushFile As String = sr.ReadToEnd()
            sr.Close()

            'string headerTemplate = "Content-Disposition: form-data; name=\"_attachments\"; filename=\"WAPPush.txt\"\r\n Content-Type: application/octet-stream\r\n\r\n";
            Dim wapRequestObject As HttpWebRequest = DirectCast(WebRequest.Create("" & FQDN & "/1/messages/outbox/wapPush?access_token=" & access_token.ToString()), HttpWebRequest)
            wapRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""""; boundary=""" & boundary & """" & vbCr & vbLf
            wapRequestObject.Method = "POST"
            wapRequestObject.KeepAlive = True

            Dim sendWapData As String = "address=" & Server.UrlEncode("tel:" & wapAddress.ToString()) & "&subject=" & Server.UrlEncode("Wap Message") & "&priority=High&content-type=" & Server.UrlEncode("application/xml")
            'Wap Push Data 
            Dim data As String = ""
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
                drawPanelForSuccess(wapPanel, deserializedJsonObj.id.ToString())
                wapResponseStream.Close()
            End Using
            wapRequestObject = Nothing
            If File.Exists(wapFilePath) Then
                Dim fileInfo As New System.IO.FileInfo(wapFilePath)
                fileInfo.Delete()
            End If
        Catch ex As Exception
            If File.Exists(wapFilePath) Then
                Dim fileInfo As New System.IO.FileInfo(wapFilePath)
                fileInfo.Delete()
            End If
            drawPanelForFailure(wapPanel, ex.ToString())
        End Try
    End Sub

End Class

' Following are the data structures used for the applicaiton 

Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String

End Class
Public Class SendWapResponse
    Public id As String
End Class