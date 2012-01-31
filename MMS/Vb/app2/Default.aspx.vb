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
Imports System.Configuration
Imports System.Web.Script.Serialization
Imports System.Drawing
Imports System.Text
Imports System
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates


Partial Public Class [Default]
    Inherits System.Web.UI.Page
    'string shortCode, FQDN, accessTokenString, accessToken, refreshToken, responseData, apiKey, secretKey, scope, authCode, accessTokenFilePath;
    'string[] accessTokenJson, accessTokenDetails, refreshTokenDetails, accessTokenExpireDetails;
    'DateTime expireTime;
    'double expiresIn;
    Private shortCode As String, FQDN As String, accessTokenFilePath As String, messageFilePath As String, phoneListFilePath As String, couponPath As String, _
     couponFileName As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private phoneNumbersList As New List(Of String)()
    Private invalidPhoneNumbers As New List(Of String)()
    Private phoneNumber As String, phoneNumbersParameter As String = Nothing
    Private phoneNumberAndValidity As Dictionary(Of String, String)
    Private getStatusTable As Table, secondTable As Table
    Private textChanged As Boolean = False
    Private phoneListContent As String = ""
    Private errorInput As Boolean = False

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
        rowTwoCellOne.Width = Unit.Pixel(70)
        rowTwoCellOne.Text = "Message ID:"
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

    ' this function validates the given string as valid msisdn 

    Private Function isValidMISDN(ByVal number As String) As [Boolean]
        Dim smsAddressInput As String = number
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

    '
    '     * On page load if query string 'code' is present, invoke get_access_token
    '     


    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'BypassCertificateError()
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            If ConfigurationManager.AppSettings("messageFilePath") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "Message file path is missing in configuration file")
                Return
            End If
            messageFilePath = ConfigurationManager.AppSettings("messageFilePath")
            If ConfigurationManager.AppSettings("phoneListFilePath") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "Phone list file path is missing in configuration file")
                Return
            End If
            phoneListFilePath = ConfigurationManager.AppSettings("phoneListFilePath")
            If ConfigurationManager.AppSettings("couponPath") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "Coupon file path is missing in configuration file")
                Return
            End If
            couponPath = ConfigurationManager.AppSettings("couponPath")

            If ConfigurationManager.AppSettings("couponFileName") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "Coupon file name is missing in configuration file")
                Return
            End If
            couponFileName = ConfigurationManager.AppSettings("couponFileName")

            Dim _dir As New DirectoryInfo(Request.MapPath(couponPath))
            Dim _imgs As FileInfo() = _dir.GetFiles()
            Dim fileindex As Integer = 0
            Dim foundFlag As Boolean = False
            For Each tempFileInfo As FileInfo In _imgs
                If tempFileInfo.Name.ToLower().CompareTo(couponFileName.ToLower()) = 0 Then
                    foundFlag = True
                    Exit For
                Else
                    fileindex += 1
                End If
            Next
            If foundFlag = False Then
                drawPanelForFailure(sendMMSPanel, "Coupon doesnt exists")
                Return
            End If
            Image1.ImageUrl = String.Format("{0}{1}", couponPath, _imgs(fileindex).Name)

            'Image1.ImageUrl = Request.MapPath(couponPath + couponFileName);
            If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
                accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
            Else
                accessTokenFilePath = "~\MMSApp2AccessToken.txt"
            End If
            If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "FQDN is not defined in configuration file")
                Return
            End If
            FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
            If ConfigurationManager.AppSettings("short_code") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "short_code is not defined in configuration file")
                Return
            End If
            shortCode = ConfigurationManager.AppSettings("short_code").ToString()
            If ConfigurationManager.AppSettings("api_key") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "api_key is not defined in configuration file")
                Return
            End If
            api_key = ConfigurationManager.AppSettings("api_key").ToString()
            If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
                drawPanelForFailure(sendMMSPanel, "secret_key is not defined in configuration file")
                Return
            End If
            secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
            If ConfigurationManager.AppSettings("scope") Is Nothing Then
                scope = "MMS"
            Else
                scope = ConfigurationManager.AppSettings("scope").ToString()
            End If
            'StreamReader str2 = File.OpenText(Request.MapPath(messageFilePath));
            Dim str2 As New StreamReader(Request.MapPath(messageFilePath))
            subjectLabel.Text = str2.ReadToEnd()
            str2.Close()

            If Not Page.IsPostBack Then
                Dim str3 As New StreamReader(Request.MapPath(phoneListFilePath))
                phoneListTextBox.Text = str3.ReadToEnd()
                str3.Close()

            End If
        Catch ex As Exception
            drawPanelForFailure(sendMMSPanel, ex.ToString())
            Response.Write(ex.ToString())
        End Try
    End Sub

    ' this function draws table for failed numbers 

    Private Sub drawPanelForFailedNumbers(ByVal panelParam As Panel)
        Dim table As New Table()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        'rowOneCellOne.BorderWidth = 1;
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR: Invalid numbers"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        For Each number As String In invalidPhoneNumbers
            Dim rowTwo As New TableRow()
            Dim rowTwoCellOne As New TableCell()
            'rowTwoCellOne.BorderWidth = 1;
            rowTwoCellOne.Text = number.ToString()
            rowTwo.Controls.Add(rowTwoCellOne)
            table.Controls.Add(rowTwo)
        Next
        table.BorderWidth = 2
        table.BorderColor = Color.Red
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(table)
    End Sub

    ' this function draws table for get status response 

    Private Sub drawPanelForGetStatusResult(ByVal msgid As String, ByVal phone As String, ByVal status As String, ByVal headerFlag As Boolean)
        If headerFlag = True Then
            getStatusTable = New Table()
            getStatusTable.Font.Name = "Sans-serif"
            getStatusTable.Font.Size = 9
            getStatusTable.BorderStyle = BorderStyle.Outset
            getStatusTable.Width = Unit.Pixel(650)
            Dim rowOne As New TableRow()
            Dim rowOneCellOne As New TableCell()
            rowOneCellOne.Width = Unit.Pixel(110)
            rowOneCellOne.Font.Bold = True
            rowOneCellOne.Text = "SUCCESS:"
            'rowOneCellOne.BorderWidth = 1;
            rowOne.Controls.Add(rowOneCellOne)
            getStatusTable.Controls.Add(rowOne)
            Dim rowTwo As New TableRow()
            Dim rowTwoCellOne As New TableCell()
            rowTwoCellOne.Width = Unit.Pixel(110)
            rowTwoCellOne.Text = "Messages Delivered"
            'rowTwoCellOne.BorderWidth = 1;
            rowTwo.Controls.Add(rowTwoCellOne)
            getStatusTable.Controls.Add(rowTwo)


            getStatusTable.BorderWidth = 2
            getStatusTable.BorderColor = Color.DarkGreen
            getStatusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
            getStatusTable.Controls.Add(rowOne)
            getStatusTable.Controls.Add(rowTwo)
            'getStatusTable.Controls.Add(rowThree);
            statusPanel.Controls.Add(getStatusTable)

            secondTable = New Table()
            secondTable.Font.Name = "Sans-serif"
            secondTable.Font.Size = 9
            secondTable.Width = Unit.Pixel(650)
            Dim TableRow As New TableRow()
            'secondTable.Width = Unit.Percentage(80);
            Dim TableCell As New TableCell()
            TableCell.Width = Unit.Pixel(300)
            'TableCell.BorderWidth = 1;
            TableCell.Text = "Recipient"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableCell.Font.Bold = True
            TableRow.Cells.Add(TableCell)
            TableCell = New TableCell()
            'TableCell.BorderWidth = 1;
            TableCell.Font.Bold = True
            TableCell.Width = Unit.Pixel(300)
            TableCell.Wrap = True
            TableCell.Text = "Status"
            TableCell.HorizontalAlign = HorizontalAlign.Center
            TableRow.Cells.Add(TableCell)
            secondTable.Rows.Add(TableRow)
            statusPanel.Controls.Add(secondTable)
        Else
            Dim row As New TableRow()
            Dim cell1 As New TableCell()
            Dim cell2 As New TableCell()
            'TableCell cell3 = new TableCell();
            'cell1.BorderWidth = 1;
            'cell2.BorderWidth = 1;
            'cell3.BorderWidth = 1;
            'cell1.Text = msgid.ToString();
            'row.Controls.Add(cell1);
            cell1.Text = phone.ToString()
            cell1.Width = Unit.Pixel(300)
            cell1.HorizontalAlign = HorizontalAlign.Center
            row.Controls.Add(cell1)
            cell2.Text = status.ToString()
            cell2.Width = Unit.Pixel(300)
            cell2.HorizontalAlign = HorizontalAlign.Center
            row.Controls.Add(cell2)
            secondTable.Controls.Add(row)
        End If
    End Sub

    ' this function is called with user clicks on send mms button 

    Protected Sub sendButton_Click(ByVal sender As Object, ByVal e As EventArgs)

        If phoneListTextBox.Text.Length = 0 Then
            Return
        End If
        Dim phoneNumbers As String() = phoneListTextBox.Text.ToString().Split(","c)
        For Each phoneNum As String In phoneNumbers
            If phoneNum IsNot Nothing AndAlso (String.Compare(phoneNum, "") <> 0) Then
                phoneNumbersList.Add(phoneNum.ToString())
            End If
        Next
        phoneNumberAndValidity = New Dictionary(Of String, String)()
        For Each phNumber As String In phoneNumbersList
            If isValidMISDN(phNumber) = True Then
                If phNumber.StartsWith("tel:") Then

                    Dim phNumberWithoutHyphens As String = phNumber.Replace("-", "")
                    phoneNumbersParameter = phoneNumbersParameter & "Address=" & Server.UrlEncode(phNumberWithoutHyphens.ToString()) & "&"
                Else
                    Dim phNumberWithoutHyphens As String = phNumber.Replace("-", "")
                    phoneNumbersParameter = phoneNumbersParameter & "Address=" & Server.UrlEncode("tel:" & phNumberWithoutHyphens.ToString()) & "&"

                End If
            Else
                invalidPhoneNumbers.Add(phNumber)
            End If
        Next
        If phoneNumbersParameter Is Nothing Then
            If invalidPhoneNumbers.Count > 0 Then
                drawPanelForFailedNumbers(sendMMSPanel)
            End If
            Return
        End If
        If readAndGetAccessToken(sendMMSPanel) = False Then
            Return
        End If

        Dim mmsFilePath As String = Request.MapPath(couponPath)
        'Table table = new Table();
        'table.Font.Size = 8;
        Try
            Dim boundary As String = "----------------------------" & DateTime.Now.Ticks.ToString("x")

            Dim mmsRequestObject As HttpWebRequest = DirectCast(WebRequest.Create("" & FQDN & "/rest/mms/2/messaging/outbox?access_token=" & access_token.ToString()), HttpWebRequest)
            mmsRequestObject.ContentType = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" & boundary & """" & vbCr & vbLf
            mmsRequestObject.Method = "POST"
            mmsRequestObject.KeepAlive = True
            Dim encoding As New UTF8Encoding()
            Dim postBytes As Byte() = encoding.GetBytes("")
            Dim sendMMSData As String = phoneNumbersParameter & "&Subject=" & Server.UrlEncode(subjectLabel.Text.ToString())
            Dim data As String = ""
            'string mmsFileName = Path.GetFileName(mmsFilePath.ToString());
            Dim fs As New FileStream(mmsFilePath & couponFileName, FileMode.Open, FileAccess.Read)
            Dim br As New BinaryReader(fs)
            Dim image As Byte() = br.ReadBytes(CInt(fs.Length))
            br.Close()
            fs.Close()

            data += "--" & boundary & vbCr & vbLf
            data += "Content-Type:application/x-www-form-urlencoded;charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding:8bit" & vbCr & vbLf & "Content-ID:<startpart>" & vbCr & vbLf & vbCr & vbLf & sendMMSData & vbCr & vbLf
            data += "--" & boundary & vbCr & vbLf
            data += "Content-Disposition:attachment;name=""" & "coupon.jpg" & """" & vbCr & vbLf
            data += "Content-Type:image/gif" & vbCr & vbLf
            data += "Content-ID:<" & "coupon.jpg" & ">" & vbCr & vbLf
            data += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf
            Dim firstPart As Byte() = encoding.GetBytes(data)
            Dim newSize As Integer = firstPart.Length + image.Length
            Dim ms = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
            ms.Write(firstPart, 0, firstPart.Length)
            ms.Write(image, 0, image.Length)
            Dim secondpart As Byte() = ms.GetBuffer()
            Dim thirdpart As Byte() = encoding.GetBytes(vbCr & vbLf & "--" & boundary & "--" & vbCr & vbLf)
            newSize = secondpart.Length + thirdpart.Length
            Dim ms2 = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
            ms2.Write(secondpart, 0, secondpart.Length)
            ms2.Write(thirdpart, 0, thirdpart.Length)
            postBytes = ms2.GetBuffer()

            mmsRequestObject.ContentLength = postBytes.Length

            Dim postStream As Stream = mmsRequestObject.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)
            postStream.Close()
            Dim mmsResponseObject As WebResponse = mmsRequestObject.GetResponse()
            Using sr As New StreamReader(mmsResponseObject.GetResponseStream())
                Dim mmsResponseData As String = sr.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As mmsResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(mmsResponseId)), mmsResponseId)
                msgIdLabel.Text = deserializedJsonObj.id.ToString()
                drawPanelForSuccess(sendMMSPanel, deserializedJsonObj.id.ToString())
                sr.Close()
            End Using
            mmsRequestObject = Nothing
            'sendMMSPanel.Controls.Add(table);
            If invalidPhoneNumbers.Count > 0 Then
                drawPanelForFailedNumbers(sendMMSPanel)
            End If
        Catch ex As Exception
            drawPanelForFailure(sendMMSPanel, ex.ToString())
            If invalidPhoneNumbers.Count > 0 Then
                drawPanelForFailedNumbers(sendMMSPanel)
            End If
        End Try
    End Sub

    'this function is called when user clicks on get status button 

    Protected Sub statusButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'Session["Inprocess"] = null;

            If msgIdLabel.Text Is Nothing OrElse msgIdLabel.Text.ToString() Is Nothing OrElse msgIdLabel.Text.ToString().Length <= 0 Then
                Return
            End If
            If readAndGetAccessToken(statusPanel) = False Then
                Return
            End If
            Dim mmsId As String = msgIdLabel.Text.ToString()
            Dim mmsDeliveryStatus As [String]
            Dim mmsStatusRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/rest/mms/2/messaging/outbox/" & mmsId & "?access_token=" & access_token.ToString()), HttpWebRequest)
            mmsStatusRequestObject.Method = "GET"
            Dim mmsStatusResponseObject As HttpWebResponse = DirectCast(mmsStatusRequestObject.GetResponse(), HttpWebResponse)
            Using mmsStatusResponseStream As New StreamReader(mmsStatusResponseObject.GetResponseStream())
                mmsDeliveryStatus = mmsStatusResponseStream.ReadToEnd()
                mmsDeliveryStatus = mmsDeliveryStatus.Replace("-", "")
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim status As GetDeliveryStatus = DirectCast(deserializeJsonObject.Deserialize(mmsDeliveryStatus, GetType(GetDeliveryStatus)), GetDeliveryStatus)
                Dim dinfoList As DeliveryInfoList = status.DeliveryInfoList
                drawPanelForGetStatusResult(Nothing, Nothing, Nothing, True)
                For Each dInfo As deliveryInfo In dinfoList.deliveryInfo
                    drawPanelForGetStatusResult(dInfo.id, dInfo.address, dInfo.deliverystatus, False)
                Next
                msgIdLabel.Text = ""
                mmsStatusResponseStream.Close()
            End Using
        Catch ex As Exception
            drawPanelForFailure(statusPanel, ex.ToString())
        End Try
    End Sub


End Class

'following are data structures used for the application 


Public Class mmsResponseId
    Public id As String
End Class

Public Class mmsStatus
    Public status As String
    Public resourceURL As String
End Class

Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String
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