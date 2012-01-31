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
Imports System
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates


Partial Public Class [Default]
    Inherits System.Web.UI.Page
    Private shortCode As String, FQDN As String, accessTokenFilePath As String, oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private shortCodes As String()

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
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/access_token?client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&grant_type=client_credentials&scope=SMS")
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
    '     * This function is called when the applicaiton page is loaded into the browser.
    '     * This fucntion reads the web.config and gets the values of the attributes
    '     * 
    '     

    Public Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Try
            'BypassCertificateError()
            Dim currentServerTime As DateTime = DateTime.UtcNow
            serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
            If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
                accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
            Else
                accessTokenFilePath = "~\SMSApp1AccessToken.txt"
            End If
            'accessTokenFilePath = "~\\SMSApp1AccessToken.txt";
            If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "FQDN is not defined in configuration file")
                Return
            End If
            FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
            If ConfigurationManager.AppSettings("short_code") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "short_code is not defined in configuration file")
                Return
            End If
            shortCode = ConfigurationManager.AppSettings("short_code").ToString()
            If ConfigurationManager.AppSettings("api_key") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "api_key is not defined in configuration file")
                Return
            End If
            api_key = ConfigurationManager.AppSettings("api_key").ToString()
            If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "secret_key is not defined in configuration file")
                Return
            End If
            secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
            If ConfigurationManager.AppSettings("scope") Is Nothing Then
                scope = "SMS"
            Else
                scope = ConfigurationManager.AppSettings("scope").ToString()
            End If
            shortCodes = shortCode.Split(";"c)
            shortCode = shortCodes(0)
            Dim table As New Table()
            table.Font.Size = 8
            For Each sCode As String In shortCodes
                Dim button As New Button()
                AddHandler button.Click, New EventHandler(AddressOf getMessagesButton_Click)
                button.Text = "Get Messages for " & sCode
                Dim rowOne As New TableRow()
                Dim rowOneCellOne As New TableCell()
                rowOne.Controls.Add(rowOneCellOne)
                rowOneCellOne.Controls.Add(button)
                table.Controls.Add(rowOne)
            Next
            receiveMessagePanel.Controls.Add(table)
        Catch ex As Exception
            drawPanelForFailure(sendSMSPanel, ex.ToString())
            Response.Write(ex.ToString())
        End Try

    End Sub

    '
    '     * This funciton is called with user clicks on send SMS
    '     * This validates the access token and then calls sendSMS method to invoke send SMS API.
    '     


    Protected Sub Button1_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If readAndGetAccessToken(sendSMSPanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    Return
                End If
                sendSms()

            End If
        Catch ex As Exception
            drawPanelForFailure(sendSMSPanel, ex.ToString())
        End Try
    End Sub
    ' This function validates the input fields and if they are valid send sms api is invoked 

    Private Sub sendSms()
        Try
            Dim smsAddressInput As String = txtmsisdn.Text.ToString()
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
                drawPanelForFailure(sendSMSPanel, "Invalid phone number: " & smsAddressInput)
            Else
                'string smsMessage = Session["smsMessage"].ToString();
                Dim smsMessage As String = txtmsg.Text.ToString()
                If smsMessage Is Nothing OrElse smsMessage.Length <= 0 Then
                    drawPanelForFailure(sendSMSPanel, "Message is null or empty")
                    Return
                End If
                Dim sendSmsResponseData As [String]
                'HttpWebRequest sendSmsRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/rest/sms/2/messaging/outbox?access_token=" + access_token.ToString());
                Dim sendSmsRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/rest/sms/2/messaging/outbox?access_token=" & access_token.ToString()), HttpWebRequest)
                Dim strReq As String = "{'Address':'tel:" & smsAddressFormatted & "','Message':'" & smsMessage & "'}"
                sendSmsRequestObject.Method = "POST"
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
                    txtSmsId.Text = deserializedJsonObj.id.ToString()
                    drawPanelForSuccess(sendSMSPanel, deserializedJsonObj.id.ToString())
                    sendSmsResponseStream.Close()
                End Using
            End If
        Catch ex As Exception
            drawPanelForFailure(sendSMSPanel, ex.ToString())
        End Try
    End Sub

    ' 
    '     * This function is called when user clicks on get delivery status button.
    '     * this funciton calls get sms delivery status API to fetch the status.
    '     

    Private Sub getSmsDeliveryStatus()
        Try

            'string smsId = Session["smsId"].ToString();
            Dim smsId As String = txtSmsId.Text.ToString()
            If smsId Is Nothing OrElse smsId.Length <= 0 Then
                drawPanelForFailure(getStatusPanel, "Message is null or empty")
                Return
            End If
            If readAndGetAccessToken(getStatusPanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                    Return
                End If
            End If
            Dim getSmsDeliveryStatusResponseData As [String]
            'HttpWebRequest getSmsDeliveryStatusRequestObject = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/1/messages/outbox/sms/" + smsId.ToString() + "?access_token=" + Session["csharp_sms_app1_access_token"].ToString());
            Dim getSmsDeliveryStatusRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/rest/sms/2/messaging/outbox/" & smsId.ToString() & "?access_token=" & access_token.ToString()), HttpWebRequest)
            getSmsDeliveryStatusRequestObject.Method = "GET"
            getSmsDeliveryStatusRequestObject.ContentType = "application/JSON"
            getSmsDeliveryStatusRequestObject.Accept = "application/json"

            Dim getSmsDeliveryStatusResponse As HttpWebResponse = DirectCast(getSmsDeliveryStatusRequestObject.GetResponse(), HttpWebResponse)
            Using getSmsDeliveryStatusResponseStream As New StreamReader(getSmsDeliveryStatusResponse.GetResponseStream())
                getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseStream.ReadToEnd()
                getSmsDeliveryStatusResponseData = getSmsDeliveryStatusResponseData.Replace("-", "")
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim status As GetDeliveryStatus = DirectCast(deserializeJsonObject.Deserialize(getSmsDeliveryStatusResponseData, GetType(GetDeliveryStatus)), GetDeliveryStatus)
                drawGetStatusSuccess(status.DeliveryInfoList.deliveryInfo(0).deliverystatus, status.DeliveryInfoList.resourceURL)
                'getSMSStatusResponseLabel.Text = "Status :" + status.DeliveryInfoList.deliveryInfo[0].deliverystatus + "\r\n" + "ResourceURL :" + status.DeliveryInfoList.resourceURL;
                getSmsDeliveryStatusResponseStream.Close()

            End Using
        Catch ex As Exception
            drawPanelForFailure(getStatusPanel, ex.ToString())
        End Try
    End Sub
    ' this function is used to draw the table for get status success response 

    Private Sub drawGetStatusSuccess(ByVal status As String, ByVal url As String)
        Dim table As New Table()
        Dim rowOne As New TableRow()
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        table.BorderStyle = BorderStyle.Outset
        table.Width = Unit.Pixel(650)
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        'rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellOne.Text = "Status: "
        rowTwoCellOne.Font.Bold = True
        'rowTwoCellOne.BorderWidth = 1;
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
    ' This function calls receive sms api to fetch the sms's 

    Private Sub recieveSms()
        Try
            Dim receiveSmsResponseData As [String]
            If shortCode Is Nothing OrElse shortCode.Length <= 0 Then
                drawPanelForFailure(getMessagePanel, "Short code is null or empty")
                Return
            End If
            If access_token Is Nothing OrElse access_token.Length <= 0 Then
                drawPanelForFailure(getMessagePanel, "Invalid access token")
                Return
            End If
            Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/rest/sms/2/messaging/inbox?access_token=" & access_token.ToString() & "&RegistrationID=" & shortCode.ToString()), HttpWebRequest)
            'HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/1/messages/inbox/sms?registrationID=" + shortCode.ToString() + "&access_token=" + access_token.ToString());
            objRequest.Method = "GET"
            Dim receiveSmsResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)
            Using receiveSmsResponseStream As New StreamReader(receiveSmsResponseObject.GetResponseStream())
                receiveSmsResponseData = receiveSmsResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As RecieveSmsResponse = DirectCast(deserializeJsonObject.Deserialize(receiveSmsResponseData, GetType(RecieveSmsResponse)), RecieveSmsResponse)
                Dim numberOfMessagesInThisBatch As Integer = deserializedJsonObj.inboundSMSMessageList.numberOfMessagesInThisBatch
                Dim resourceURL As String = deserializedJsonObj.inboundSMSMessageList.resourceURL.ToString()
                Dim totalNumberOfPendingMessages As String = deserializedJsonObj.inboundSMSMessageList.totalNumberOfPendingMessages.ToString()

                Dim parsedJson As String = "MessagesInThisBatch : " & numberOfMessagesInThisBatch.ToString() & "<br/>" & "MessagesPending : " & totalNumberOfPendingMessages.ToString() & "<br/>"
                Dim table As New Table()
                table.Font.Name = "Sans-serif"
                table.Font.Size = 9
                table.BorderStyle = BorderStyle.Outset
                table.Width = Unit.Pixel(650)
                Dim TableRow As New TableRow()
                Dim TableCell As New TableCell()
                TableCell.Width = Unit.Pixel(110)
                TableCell.Text = "SUCCESS:"
                TableCell.Font.Bold = True
                TableRow.Cells.Add(TableCell)
                table.Rows.Add(TableRow)
                TableRow = New TableRow()
                TableCell = New TableCell()
                TableCell.Width = Unit.Pixel(150)
                TableCell.Text = "Messages in this batch:"
                TableCell.Font.Bold = True
                'TableCell.BorderWidth = 1;
                TableRow.Cells.Add(TableCell)
                TableCell = New TableCell()
                TableCell.HorizontalAlign = HorizontalAlign.Left
                TableCell.Text = numberOfMessagesInThisBatch.ToString()
                'TableCell.BorderWidth = 1;
                TableRow.Cells.Add(TableCell)
                table.Rows.Add(TableRow)
                TableRow = New TableRow()
                TableCell = New TableCell()
                'TableCell.BorderWidth = 1;
                TableCell.Width = Unit.Pixel(110)
                TableCell.Text = "Messages pending:"
                TableCell.Font.Bold = True
                TableRow.Cells.Add(TableCell)
                TableCell = New TableCell()
                'TableCell.BorderWidth = 1;
                TableCell.Text = totalNumberOfPendingMessages.ToString()
                TableRow.Cells.Add(TableCell)
                table.Rows.Add(TableRow)
                TableRow = New TableRow()
                table.Rows.Add(TableRow)
                TableRow = New TableRow()
                table.Rows.Add(TableRow)
                Dim secondTable As New Table()
                If numberOfMessagesInThisBatch > 0 Then
                    TableRow = New TableRow()
                    secondTable.Font.Name = "Sans-serif"
                    secondTable.Font.Size = 9
                    'secondTable.Width = Unit.Percentage(80);
                    TableCell = New TableCell()
                    TableCell.Width = Unit.Pixel(100)
                    'TableCell.BorderWidth = 1;
                    TableCell.Text = "Message Index"
                    TableCell.HorizontalAlign = HorizontalAlign.Center
                    TableCell.Font.Bold = True
                    TableRow.Cells.Add(TableCell)
                    TableCell = New TableCell()
                    'TableCell.BorderWidth = 1;
                    TableCell.Font.Bold = True
                    TableCell.Width = Unit.Pixel(350)
                    TableCell.Wrap = True
                    TableCell.Text = "Message Text"
                    TableCell.HorizontalAlign = HorizontalAlign.Center
                    TableRow.Cells.Add(TableCell)
                    TableCell = New TableCell()
                    'TableCell.BorderWidth = 1;
                    TableCell.Text = "Sender Address"
                    TableCell.HorizontalAlign = HorizontalAlign.Center
                    TableCell.Font.Bold = True
                    TableCell.Width = Unit.Pixel(175)
                    TableRow.Cells.Add(TableCell)
                    secondTable.Rows.Add(TableRow)
                    'table.Rows.Add(TableRow);

                    For Each prime As inboundSMSMessage In deserializedJsonObj.inboundSMSMessageList.inboundSMSMessage
                        TableRow = New TableRow()
                        Dim TableCellmessageId As New TableCell()
                        TableCellmessageId.Width = Unit.Pixel(75)
                        TableCellmessageId.Text = prime.messageId.ToString()
                        TableCellmessageId.HorizontalAlign = HorizontalAlign.Center
                        Dim TableCellmessage As New TableCell()
                        TableCellmessage.Width = Unit.Pixel(350)
                        TableCellmessage.Wrap = True
                        TableCellmessage.Text = prime.message.ToString()
                        TableCellmessage.HorizontalAlign = HorizontalAlign.Center
                        Dim TableCellsenderAddress As New TableCell()
                        TableCellsenderAddress.Width = Unit.Pixel(175)
                        TableCellsenderAddress.Text = prime.senderAddress.ToString()
                        TableCellsenderAddress.HorizontalAlign = HorizontalAlign.Center
                        TableRow.Cells.Add(TableCellmessageId)
                        TableRow.Cells.Add(TableCellmessage)
                        TableRow.Cells.Add(TableCellsenderAddress)
                        'table.Rows.Add(TableRow);
                        secondTable.Rows.Add(TableRow)
                    Next
                End If
                table.BorderColor = Color.DarkGreen
                table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
                table.BorderWidth = 2

                getMessagePanel.Controls.Add(table)
                getMessagePanel.Controls.Add(secondTable)

                'getMessagePanel.BorderColor = Color.DarkGreen;
                'getMessagePanel.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
                'getMessagePanel.BorderWidth = 2;

                receiveSmsResponseStream.Close()
            End Using
        Catch ex As Exception
            drawPanelForFailure(getMessagePanel, ex.ToString())
        End Try
    End Sub

    ' this method is called when user clicks on get delivery status button 


    Protected Sub getDeliveryStatusButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If readAndGetAccessToken(getStatusPanel) = False Then
                Return
            End If
            If access_token Is Nothing OrElse access_token.Length <= 0 Then
                'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                Return
            End If
            getSmsDeliveryStatus()
        Catch ex As Exception
            drawPanelForFailure(getStatusPanel, ex.ToString())
        End Try
    End Sub

    '
    'this method is called when user clicks on get message button
    '

    Protected Sub getMessagesButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            readAndGetAccessToken(getMessagePanel)
            If access_token Is Nothing OrElse access_token.Length <= 0 Then
                'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                Return
            End If
            Dim button As Button = TryCast(sender, Button)
            Dim buttonCaption As String = button.Text.ToString()
            shortCode = buttonCaption.Replace("Get Messages for ", "")
            recieveSms()
        Catch ex As Exception
            drawPanelForFailure(getMessagePanel, ex.ToString())
        End Try
    End Sub
End Class
' Below are the data structures used for this applicaiton 


Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String
End Class

Public Class SendSmsResponse
    Public id As String
End Class

Public Class GetSmsDeliveryStatus
    Public Status As String
    Public ResourceUrl As String
End Class

Public Class SmsStatus
    Public status As String
    Public resourceURL As String
End Class

Public Class RecieveSmsResponse
    Public inboundSMSMessageList As New inboundSMSMessageList()
End Class

Public Class inboundSMSMessageList

    Public Property inboundSMSMessage() As List(Of inboundSMSMessage)
        Get
            Return m_inboundSMSMessage
        End Get
        Set(ByVal value As List(Of inboundSMSMessage))
            m_inboundSMSMessage = Value
        End Set
    End Property
    Private m_inboundSMSMessage As List(Of inboundSMSMessage)
    Public Property numberOfMessagesInThisBatch() As Integer
        Get
            Return m_numberOfMessagesInThisBatch
        End Get
        Set(ByVal value As Integer)
            m_numberOfMessagesInThisBatch = Value
        End Set
    End Property
    Private m_numberOfMessagesInThisBatch As Integer
    Public Property resourceURL() As String
        Get
            Return m_resourceURL
        End Get
        Set(ByVal value As String)
            m_resourceURL = Value
        End Set
    End Property
    Private m_resourceURL As String

    Public Property totalNumberOfPendingMessages() As Integer
        Get
            Return m_totalNumberOfPendingMessages
        End Get
        Set(ByVal value As Integer)
            m_totalNumberOfPendingMessages = Value
        End Set
    End Property
    Private m_totalNumberOfPendingMessages As Integer

End Class

Public Class inboundSMSMessage
    Public Property dateTime() As String
        Get
            Return m_dateTime
        End Get
        Set(ByVal value As String)
            m_dateTime = Value
        End Set
    End Property
    Private m_dateTime As String
    Public Property destinationAddress() As String
        Get
            Return m_destinationAddress
        End Get
        Set(ByVal value As String)
            m_destinationAddress = Value
        End Set
    End Property
    Private m_destinationAddress As String
    Public Property messageId() As String
        Get
            Return m_messageId
        End Get
        Set(ByVal value As String)
            m_messageId = Value
        End Set
    End Property
    Private m_messageId As String
    Public Property message() As String
        Get
            Return m_message
        End Get
        Set(ByVal value As String)
            m_message = Value
        End Set
    End Property
    Private m_message As String

    Public Property senderAddress() As String
        Get
            Return m_senderAddress
        End Get
        Set(ByVal value As String)
            m_senderAddress = Value
        End Set
    End Property
    Private m_senderAddress As String
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