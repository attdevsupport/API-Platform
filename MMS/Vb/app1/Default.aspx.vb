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
Imports System.Drawing
Imports System
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates

Partial Public Class [Default]
    Inherits System.Web.UI.Page
    Private shortCode As String, FQDN As String, accessTokenFilePath As String, oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String

    ' This function reads the Access Token File and stores the values of access token, expiry seconds
    '     * refresh token, last access token time and refresh token expiry time
    '     * This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    '     

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
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/token")
                accessTokenRequest.Method = "POST"
                Dim oauthParameters As String = "client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&grant_type=client_credentials&scope=MMS"
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
    '     * This function is called when the applicaiton page is loaded into the browser.
    '     * This fucntion reads the web.config and gets the values of the attributes
    '     * 
    '     

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'BypassCertificateError()
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
                accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
            Else
                accessTokenFilePath = "~\MMSApp1AccessToken.txt"
            End If
            If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
                drawPanelForFailure(sendMessagePanel, "FQDN is not defined in configuration file")
                Return
            End If
            FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
            If ConfigurationManager.AppSettings("short_code") Is Nothing Then
                drawPanelForFailure(sendMessagePanel, "short_code is not defined in configuration file")
                Return
            End If
            shortCode = ConfigurationManager.AppSettings("short_code").ToString()
            If ConfigurationManager.AppSettings("api_key") Is Nothing Then
                drawPanelForFailure(sendMessagePanel, "api_key is not defined in configuration file")
                Return
            End If
            api_key = ConfigurationManager.AppSettings("api_key").ToString()
            If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
                drawPanelForFailure(sendMessagePanel, "secret_key is not defined in configuration file")
                Return
            End If
            secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
            If ConfigurationManager.AppSettings("scope") Is Nothing Then
                scope = "MMS"
            Else
                scope = ConfigurationManager.AppSettings("scope").ToString()
            End If
        Catch ex As Exception
            drawPanelForFailure(sendMessagePanel, ex.ToString())
            Response.Write(ex.ToString())
        End Try
    End Sub
    '
    ' * This funciton calls send mms message to send the selected files
    ' 

    Private Sub sendMms()
        Try
            Dim smsAddressInput As String = phoneTextBox.Text.ToString()
            Dim smsAddressFormatted As String
            Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"
            If System.Text.RegularExpressions.Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
                smsAddressFormatted = smsAddressInput.Replace("-", "")
            Else
                smsAddressFormatted = smsAddressInput
            End If
            Dim smsAddressForRequest As String = smsAddressFormatted.ToString()
            Dim tryParseResult As Long = 0
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
                drawPanelForFailure(sendMessagePanel, "Invalid phone number: " & smsAddressInput)
            Else
                Dim mmsAddress As String = smsAddressForRequest.ToString()
                Dim mmsMessage As String = messageTextBox.Text.ToString()

                If mmsMessage Is Nothing OrElse mmsMessage.Length <= 0 Then
                    drawPanelForFailure(sendMessagePanel, "Message is null or empty")
                    Return
                End If

                If (Session("mmsFilePath1") Is Nothing) AndAlso (Session("mmsFilePath2") Is Nothing) AndAlso (Session("mmsFilePath3") Is Nothing) Then
                    Dim boundaryToSend As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")
                    Dim mmsRequestObject1 As HttpWebRequest = DirectCast(WebRequest.Create("" & FQDN & "/rest/mms/2/messaging/outbox?access_token=" & access_token.ToString()), HttpWebRequest)
                    mmsRequestObject1.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" & boundaryToSend & """" & vbCr & vbLf
                    mmsRequestObject1.Method = "POST"
                    mmsRequestObject1.KeepAlive = True
                    Dim encoding1 As New UTF8Encoding()
                    Dim bytesToSend As Byte() = encoding1.GetBytes("")
                    Dim mmsParameters As String = "Address=" & Server.UrlEncode("tel:" & mmsAddress) & "&Subject=" & Server.UrlEncode(mmsMessage)
                    Dim dataToSend As String = ""
                    dataToSend += "--" & boundaryToSend & vbCr & vbLf
                    dataToSend += "Content-Type: application/x-www-form-urlencoded; charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding: 8bit" & vbCr & vbLf & "Content-Disposition: form-data; name=""root-fields""" & vbCr & vbLf & "Content-ID: <startpart>" & vbCr & vbLf & vbCr & vbLf & mmsParameters & vbCr & vbLf
                    dataToSend += "--" & boundaryToSend & "--" & vbCr & vbLf
                    bytesToSend = encoding1.GetBytes(dataToSend)
                    Dim sizeToSend As Integer = bytesToSend.Length
                    Dim memBufToSend = New MemoryStream(New Byte(sizeToSend - 1) {}, 0, sizeToSend, True, True)
                    memBufToSend.Write(bytesToSend, 0, bytesToSend.Length)
                    'ms.Write(image, 0, image.Length);
                    Dim finalData As Byte() = memBufToSend.GetBuffer()
                    mmsRequestObject1.ContentLength = finalData.Length
                    Dim postStream1 As Stream = mmsRequestObject1.GetRequestStream()
                    postStream1.Write(finalData, 0, finalData.Length)
                    postStream1.Close()

                    Dim mmsResponseObject1 As WebResponse = mmsRequestObject1.GetResponse()
                    Using sr As New StreamReader(mmsResponseObject1.GetResponseStream())
                        Dim mmsResponseData As String = sr.ReadToEnd()
                        Dim deserializeJsonObject As New JavaScriptSerializer()
                        Dim deserializedJsonObj As mmsResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(mmsResponseId)), mmsResponseId)
                        messageIDTextBox.Text = deserializedJsonObj.id.ToString()
                        drawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString())
                        sr.Close()
                    End Using

                    Return
                End If

                Dim boundary As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

                Dim mmsRequestObject As HttpWebRequest = DirectCast(WebRequest.Create("" & FQDN & "/rest/mms/2/messaging/outbox?access_token=" & access_token.ToString()), HttpWebRequest)
                mmsRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" & boundary & """" & vbCr & vbLf
                mmsRequestObject.Method = "POST"
                mmsRequestObject.KeepAlive = True
                Dim encoding As New UTF8Encoding()
                Dim postBytes As Byte() = encoding.GetBytes("")
                Dim postBytes1 As Byte() = encoding.GetBytes("")
                Dim postBytes2 As Byte() = encoding.GetBytes("")
                Dim totalpostBytes As Byte() = encoding.GetBytes("")
                Dim sendMMSData As String = "Address=" & Server.UrlEncode("tel:" & mmsAddress) & "&Subject=" & Server.UrlEncode(mmsMessage)
                Dim data As String = ""
                data += "--" & boundary & vbCr & vbLf
                data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding:8bit" & vbCr & vbLf & "Content-ID:<startpart>" & vbCr & vbLf & vbCr & vbLf & sendMMSData & vbCr & vbLf

                If Session("mmsFilePath1") IsNot Nothing Then
                    Dim mmsFileName As String = Path.GetFileName(Session("mmsFilePath1").ToString())
                    Dim fs As New FileStream(Session("mmsFilePath1").ToString(), FileMode.Open, FileAccess.Read)
                    Dim br As New BinaryReader(fs)
                    Dim image As Byte() = br.ReadBytes(CInt(fs.Length))
                    br.Close()
                    fs.Close()
                    data += "--" & boundary & vbCr & vbLf
                    data += "Content-Disposition:attachment;name=""" & mmsFileName & """" & vbCr & vbLf
                    data += "Content-Type:image/gif" & vbCr & vbLf
                    data += "Content-ID:<" & mmsFileName & ">" & vbCr & vbLf
                    data += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf
                    Dim firstPart As Byte() = encoding.GetBytes(data)
                    Dim newSize As Integer = firstPart.Length + image.Length
                    Dim ms = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
                    ms.Write(firstPart, 0, firstPart.Length)
                    ms.Write(image, 0, image.Length)
                    postBytes = ms.GetBuffer()
                    'Session["mmsFilePath1"] = null;
                    totalpostBytes = postBytes
                End If

                If Session("mmsFilePath2") IsNot Nothing Then
                    If Session("mmsFilePath1") IsNot Nothing Then
                        data = "--" & boundary & vbCr & vbLf
                    Else
                        data += "--" & boundary & vbCr & vbLf
                    End If
                    Dim mmsFileName As String = Path.GetFileName(Session("mmsFilePath2").ToString())
                    Dim fs As New FileStream(Session("mmsFilePath2").ToString(), FileMode.Open, FileAccess.Read)
                    Dim br As New BinaryReader(fs)
                    Dim image As Byte() = br.ReadBytes(CInt(fs.Length))
                    br.Close()
                    fs.Close()
                    data += "Content-Disposition:attachment;name=""" & mmsFileName & """" & vbCr & vbLf
                    data += "Content-Type:image/gif" & vbCr & vbLf
                    data += "Content-ID:<" & mmsFileName & ">" & vbCr & vbLf
                    data += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf
                    Dim firstPart As Byte() = encoding.GetBytes(data)
                    Dim newSize As Integer = firstPart.Length + image.Length
                    Dim ms = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
                    ms.Write(firstPart, 0, firstPart.Length)
                    ms.Write(image, 0, image.Length)
                    postBytes1 = ms.GetBuffer()
                    'byte[] secondpart = ms.GetBuffer();
                    'byte[] thirdpart = encoding.GetBytes("\r\n--" + boundary + "--\r\n");
                    If Session("mmsFilePath1") IsNot Nothing Then
                        Dim ms2 = JoinTwoByteArrays(postBytes, postBytes1)
                        totalpostBytes = ms2.GetBuffer()
                    Else
                        totalpostBytes = postBytes1
                        'Session["mmsFilePath2"] = null;
                    End If
                End If

                If Session("mmsFilePath3") IsNot Nothing Then
                    If Session("mmsFilePath1") IsNot Nothing OrElse Session("mmsFilePath2") IsNot Nothing Then
                        data = "--" & boundary & vbCr & vbLf
                    Else
                        data += "--" & boundary & vbCr & vbLf
                    End If
                    Dim mmsFileName As String = Path.GetFileName(Session("mmsFilePath3").ToString())
                    Dim fs As New FileStream(Session("mmsFilePath3").ToString(), FileMode.Open, FileAccess.Read)
                    Dim br As New BinaryReader(fs)
                    Dim image As Byte() = br.ReadBytes(CInt(fs.Length))
                    br.Close()
                    fs.Close()
                    data += "Content-Disposition:attachment;name=""" & mmsFileName & """" & vbCr & vbLf
                    data += "Content-Type:image/gif" & vbCr & vbLf
                    data += "Content-ID:<" & mmsFileName & ">" & vbCr & vbLf
                    data += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf
                    Dim firstPart As Byte() = encoding.GetBytes(data)
                    Dim newSize As Integer = firstPart.Length + image.Length
                    Dim ms = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
                    ms.Write(firstPart, 0, firstPart.Length)
                    ms.Write(image, 0, image.Length)
                    postBytes2 = ms.GetBuffer()
                    If Session("mmsFilePath1") IsNot Nothing OrElse Session("mmsFilePath2") IsNot Nothing Then
                        Dim temp As Byte() = totalpostBytes
                        Dim ms2 = JoinTwoByteArrays(temp, postBytes2)
                        totalpostBytes = ms2.GetBuffer()
                    Else
                        totalpostBytes = postBytes2
                    End If
                End If

                Dim byteLastBoundary As Byte() = encoding.GetBytes(vbCr & vbLf & "--" & boundary & "--" & vbCr & vbLf)
                'int totalSize = postBytes.Length + postBytes1.Length + postBytes2.Length + byteLastBoundary.Length;
                Dim totalSize As Integer = totalpostBytes.Length + byteLastBoundary.Length
                Dim totalMS = New MemoryStream(New Byte(totalSize - 1) {}, 0, totalSize, True, True)
                totalMS.Write(totalpostBytes, 0, totalpostBytes.Length)
                totalMS.Write(byteLastBoundary, 0, byteLastBoundary.Length)
                Dim finalpostBytes As Byte() = totalMS.GetBuffer()
                mmsRequestObject.ContentLength = finalpostBytes.Length
                Dim postStream As Stream = mmsRequestObject.GetRequestStream()
                postStream.Write(finalpostBytes, 0, finalpostBytes.Length)
                postStream.Close()

                Dim mmsResponseObject As WebResponse = mmsRequestObject.GetResponse()
                Using sr As New StreamReader(mmsResponseObject.GetResponseStream())
                    Dim mmsResponseData As String = sr.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As mmsResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(mmsResponseId)), mmsResponseId)
                    messageIDTextBox.Text = deserializedJsonObj.id.ToString()
                    drawPanelForSuccess(sendMessagePanel, deserializedJsonObj.id.ToString())
                    sr.Close()
                End Using
                mmsRequestObject = Nothing
                Dim indexj As Integer = 1
                While indexj <= 3
                    If Convert.ToString(Session("mmsFilePath" & indexj)) <> "" Then
                        If File.Exists(Session("mmsFilePath" & indexj).ToString()) Then
                            Dim fileInfo As New System.IO.FileInfo(Session("mmsFilePath" & indexj).ToString())
                            fileInfo.Delete()
                            Session("mmsFilePath" & indexj) = Nothing
                        End If
                    End If
                    indexj += 1
                End While
            End If
        Catch ex As Exception
            drawPanelForFailure(sendMessagePanel, ex.ToString())
            Dim index As Integer = 1

            While index <= 3
                If Convert.ToString(Session("mmsFilePath" & index)) <> "" Then
                    If File.Exists(Session("mmsFilePath" & index).ToString()) Then
                        Dim fileInfo As New System.IO.FileInfo(Session("mmsFilePath" & index).ToString())
                        fileInfo.Delete()
                        Session("mmsFilePath" & index) = Nothing
                    End If
                End If
                index += 1
            End While
        End Try
    End Sub
    ' this function add two byte arrays and returns the address of buffer 

    Private Shared Function JoinTwoByteArrays(ByVal firstByteArray As Byte(), ByVal secondByteArray As Byte()) As MemoryStream
        Dim newSize As Integer = firstByteArray.Length + secondByteArray.Length
        Dim ms = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
        ms.Write(firstByteArray, 0, firstByteArray.Length)
        ms.Write(secondByteArray, 0, secondByteArray.Length)
        Return ms
    End Function

    ' this function draws success table 

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
        ' rowOneCellOne.BorderWidth = 1;
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
    ' this function draws error table 

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

    ' this funciton is called when user clicks on send mms button 


    Protected Sub sendMMSMessageButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If readAndGetAccessToken(sendMessagePanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    Return
                End If
                Dim fileSize As Long = 0
                If FileUpload1.FileName.ToString() <> "" Then
                    FileUpload1.SaveAs(Request.MapPath(FileUpload1.FileName.ToString()))
                    Session("mmsFilePath1") = Request.MapPath(FileUpload1.FileName)
                    Dim fileInfoObj As New FileInfo(Session("mmsFilePath1").ToString())
                    fileSize = fileSize + fileInfoObj.Length \ 1024
                    If fileSize > 600 Then
                        drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                        Return
                    End If
                End If
                If FileUpload2.FileName.ToString() <> "" Then
                    FileUpload2.SaveAs(Request.MapPath(FileUpload2.FileName))
                    Session("mmsFilePath2") = Request.MapPath(FileUpload2.FileName)
                    Dim fileInfoObj As New FileInfo(Session("mmsFilePath2").ToString())
                    fileSize = fileSize + fileInfoObj.Length \ 1024
                    If fileSize > 600 Then
                        drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                        Return
                    End If
                End If
                If FileUpload3.FileName.ToString() <> "" Then
                    FileUpload3.SaveAs(Request.MapPath(FileUpload3.FileName))
                    Session("mmsFilePath3") = Request.MapPath(FileUpload3.FileName)
                    Dim fileInfoObj As New FileInfo(Session("mmsFilePath3").ToString())
                    fileSize = fileSize + fileInfoObj.Length \ 1024
                    If fileSize > 600 Then
                        drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                        Return
                    End If
                End If
                If fileSize <= 600 Then
                    sendMms()
                Else
                    drawPanelForFailure(sendMessagePanel, "Attachment file size exceeded 600kb")
                    Return
                End If
            End If
        Catch ex As Exception
            drawPanelForFailure(sendMessagePanel, ex.ToString())
            Return
        End Try
    End Sub

    ' this function calls get message delivery status api to fetch the delivery status 

    Private Sub getMmsDeliveryStatus()
        Try
            Dim mmsId As String = messageIDTextBox.Text.ToString()
            If mmsId Is Nothing OrElse mmsId.Length <= 0 Then
                drawPanelForFailure(getStatusPanel, "Message Id is null or empty")
                Return
            End If
            If readAndGetAccessToken(sendMessagePanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    Return
                End If
                Dim mmsDeliveryStatus As [String]
                Dim mmsStatusRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/rest/mms/2/messaging/outbox/" & mmsId & "?access_token=" & access_token.ToString()), HttpWebRequest)
                mmsStatusRequestObject.Method = "GET"
                Dim mmsStatusResponseObject As HttpWebResponse = DirectCast(mmsStatusRequestObject.GetResponse(), HttpWebResponse)
                Using mmsStatusResponseStream As New StreamReader(mmsStatusResponseObject.GetResponseStream())
                    mmsDeliveryStatus = mmsStatusResponseStream.ReadToEnd()
                    mmsDeliveryStatus = mmsDeliveryStatus.Replace("-", "")
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim status As GetDeliveryStatus = DirectCast(deserializeJsonObject.Deserialize(mmsDeliveryStatus, GetType(GetDeliveryStatus)), GetDeliveryStatus)
                    drawGetStatusSuccess(status.DeliveryInfoList.deliveryInfo(0).deliverystatus, status.DeliveryInfoList.resourceURL)
                    mmsStatusResponseStream.Close()
                End Using
            End If
        Catch ex As Exception
            drawPanelForFailure(getStatusPanel, ex.ToString())
        End Try
    End Sub
    ' this function draws table for get status result 

    Private Sub drawGetStatusSuccess(ByVal status As String, ByVal url As String)
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
        Dim rowTwoCellTwo As New TableCell()
        'rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = "Status: "
        rowTwoCellOne.Font.Bold = True
        rowTwo.Controls.Add(rowTwoCellOne)
        rowTwoCellTwo.Text = status.ToString()
        'rowTwoCellTwo.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellTwo)
        table.Controls.Add(rowTwo)
        Dim rowThree As New TableRow()
        Dim rowThreeCellOne As New TableCell()
        Dim rowThreeCellTwo As New TableCell()
        rowThreeCellOne.Text = "ResourceURL: "
        rowThreeCellOne.Font.Bold = True
        'rowThreeCellOne.BorderWidth = 1;
        rowThree.Controls.Add(rowThreeCellOne)
        rowThreeCellTwo.Text = url.ToString()
        'rowThreeCellTwo.BorderWidth = 1;
        rowThree.Controls.Add(rowThreeCellTwo)
        table.Controls.Add(rowThree)
        table.BorderWidth = 2
        table.BorderColor = Color.DarkGreen
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        getStatusPanel.Controls.Add(table)
    End Sub

    ' this function is called when user click on get status button 

    Protected Sub getStatusButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        getMmsDeliveryStatus()
    End Sub
End Class

' The following are data structures used for the application 

Public Class mmsResponseId
    Public id As String
End Class

Public Class GetDeliveryStatus
    Public DeliveryInfoList As New DeliveryInfoList()
End Class
Public Class DeliveryInfoList
    Public resourceURL As String
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

Public Class deliveryInfo
    Public Property id() As String
        Get
            Return m_id
        End Get
        Set(ByVal value As String)
            m_id = Value
        End Set
    End Property
    Private m_id As String
    Public Property address() As String
        Get
            Return m_address
        End Get
        Set(ByVal value As String)
            m_address = Value
        End Set
    End Property
    Private m_address As String
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

Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String
End Class