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
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Timers
Imports System.Threading
Imports System


Partial Public Class [Default]
    Inherits System.Web.UI.Page
    Private shortCode As String, FQDN As String, accessTokenFilePath As String, footballFilePath As String, baseballFilePath As String, basketballFilePath As String, _
     oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private statusTable As Table
    Private Shared TheTimer As System.Timers.Timer
    ' This function reads the Access Token File and stores the values of access token, expiry seconds
    '    * refresh token, last access token time and refresh token expiry time
    '    * This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
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

    ' This function is called to draw the table in the panelParam panel for success response 

    Private Sub drawPanelForSuccess(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Remove(statusTable)
        End If
        statusTable = New Table()
        statusTable.Font.Name = "Sans-serif"
        statusTable.Font.Size = 9
        statusTable.BorderStyle = BorderStyle.Outset
        statusTable.Width = Unit.Pixel(200)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        'rowOneCellOne.BorderWidth = 1;
        rowOne.Controls.Add(rowOneCellOne)
        statusTable.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Text = message.ToString()
        'rowTwoCellOne.BorderWidth = 1;
        rowTwo.Controls.Add(rowTwoCellOne)
        statusTable.Controls.Add(rowTwo)
        statusTable.BorderWidth = 2
        statusTable.BorderColor = Color.DarkGreen
        statusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        panelParam.Controls.Add(statusTable)
    End Sub
    ' This function draws table for failed response in the panalParam panel 

    Private Sub drawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Remove(statusTable)
        End If
        statusTable = New Table()
        statusTable.Font.Name = "Sans-serif"
        statusTable.Font.Size = 9
        statusTable.BorderStyle = BorderStyle.Outset
        statusTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        'rowOneCellOne.BorderWidth = 1;
        statusTable.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        'rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        statusTable.Controls.Add(rowTwo)
        statusTable.BorderWidth = 2
        statusTable.BorderColor = Color.Red
        statusTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(statusTable)
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
            If ConfigurationManager.AppSettings("FootBallFilePath") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "FootBallFilePath is not defined in configuration file")
                Return
            End If
            footballFilePath = ConfigurationManager.AppSettings("FootBallFilePath").ToString()
            If ConfigurationManager.AppSettings("BaseBallFilePath") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "BaseBallFilePath is not defined in configuration file")
                Return
            End If
            baseballFilePath = ConfigurationManager.AppSettings("BaseBallFilePath").ToString()
            If ConfigurationManager.AppSettings("BasketBallFilePath") Is Nothing Then
                drawPanelForFailure(sendSMSPanel, "BasketBallFilePath is not defined in configuration file")
                Return
            End If
            basketballFilePath = ConfigurationManager.AppSettings("BasketBallFilePath").ToString()
            If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
                accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
            Else
                accessTokenFilePath = "~\SMSApp2AccessToken.txt"
            End If
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
            shortCodeLabel.Text = shortCode.ToString()
            updateButton.Text = "Update votes for " & shortCode.ToString()
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


            If Not Page.IsPostBack Then
                voteCount()

            End If
        Catch ex As Exception
            drawPanelForFailure(sendSMSPanel, ex.ToString())
            Response.Write(ex.ToString())
        End Try
    End Sub


    '  This function calls receive sms API and updates the GUI with the updated votes 

    Public Sub voteCount()
        Try
            If readAndGetAccessToken(sendSMSPanel) = False Then
                Return
            End If
            If access_token Is Nothing OrElse access_token.Length <= 0 Then
                'drawPanelForFailure(sendSMSPanel, "Invalid access token");
                Return
            End If
            Dim football_count_val As Integer = 0, baseball_count_val As Integer = 0, basketball_count_val As Integer = 0

            Using str1 As StreamReader = File.OpenText(Request.MapPath(footballFilePath))
                football_count_val = Convert.ToInt32(str1.ReadToEnd())
                str1.Close()
            End Using
            Using str2 As StreamReader = File.OpenText(Request.MapPath(baseballFilePath))
                baseball_count_val = Convert.ToInt32(str2.ReadToEnd())
                str2.Close()
            End Using
            Using str3 As StreamReader = File.OpenText(Request.MapPath(basketballFilePath))
                basketball_count_val = Convert.ToInt32(str3.ReadToEnd())
                str3.Close()
            End Using
            Dim smsVoteCountOutput As [String]
            Dim iTotalVotes As Integer = 0
            Dim totalVotes As String
            Dim smsVoteCountRequestObject As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/rest/sms/2/messaging/inbox?access_token=" & access_token.ToString() & "&RegistrationID=" & shortCode.ToString()), HttpWebRequest)
            smsVoteCountRequestObject.Method = "GET"
            Dim smsVoteCountResponseObject As HttpWebResponse = DirectCast(smsVoteCountRequestObject.GetResponse(), HttpWebResponse)
            Using smsVoteCountResponseStream As New StreamReader(smsVoteCountResponseObject.GetResponseStream())
                smsVoteCountOutput = smsVoteCountResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As RecieveSmsResponse = DirectCast(deserializeJsonObject.Deserialize(smsVoteCountOutput, GetType(RecieveSmsResponse)), RecieveSmsResponse)
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
                secondTable.Font.Name = "Sans-serif"
                secondTable.Font.Size = 9
                TableRow = New TableRow()
                secondTable.Font.Size = 8
                secondTable.Width = Unit.Pixel(600)
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
                    secondTable.Rows.Add(TableRow)
                    Dim msgtxt As String = TableCellmessage.Text.ToString()
                    If msgtxt.Equals("football", StringComparison.CurrentCultureIgnoreCase) Then
                        football_count_val = football_count_val + 1
                    ElseIf msgtxt.Equals("baseball", StringComparison.CurrentCultureIgnoreCase) Then
                        baseball_count_val = baseball_count_val + 1
                    ElseIf msgtxt.Equals("basketball", StringComparison.CurrentCultureIgnoreCase) Then
                        basketball_count_val = basketball_count_val + 1
                    Else
                        TableCellmessageId.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                        TableCellmessage.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
                        TableCellsenderAddress.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")

                    End If
                Next
                iTotalVotes = football_count_val + baseball_count_val + basketball_count_val
                totalVotes = "Total Votes: " & iTotalVotes.ToString()
                table.BorderColor = Color.DarkGreen
                table.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
                table.BorderWidth = 2
                receiveMessagePanel.Controls.Add(table)
                If numberOfMessagesInThisBatch > 0 Then
                    receiveMessagePanel.Controls.Add(secondTable)
                End If
                smsVoteCountResponseStream.Close()
            End Using
            footballLabel.Text = football_count_val.ToString()
            baseballLabel.Text = baseball_count_val.ToString()
            basketballLabel.Text = basketball_count_val.ToString()
            Using str1 As StreamWriter = File.CreateText(Request.MapPath(footballFilePath))
                str1.Write(footballLabel.Text.ToString())
                str1.Close()
            End Using
            Using str2 As StreamWriter = File.CreateText(Request.MapPath(baseballFilePath))
                str2.Write(baseballLabel.Text.ToString())
                str2.Close()
            End Using
            Using str3 As StreamWriter = File.CreateText(Request.MapPath(basketballFilePath))
                str3.Write(basketballLabel.Text.ToString())
                str3.Close()
            End Using
            drawPanelForSuccess(sendSMSPanel, totalVotes)
        Catch ex As Exception
            drawPanelForFailure(sendSMSPanel, ex.ToString())
        End Try
    End Sub

    ' This method is called when user clicks update votes button 

    Protected Sub updateButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        voteCount()
    End Sub
End Class

' following are the data structures used for this application 

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

Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String
End Class