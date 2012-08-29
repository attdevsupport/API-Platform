' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
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
''' MIM_App1 class
''' </summary>
Partial Public Class MIM_App1
    Inherits System.Web.UI.Page

#Region "Instance variables"

    ''' <summary>
    ''' API Address
    ''' </summary>
    Private endPoint As String

    ''' <summary>
    ''' Access token variables - temporary
    ''' </summary>
    Private apiKey As String, authCode As String, authorizeRedirectUri As String, secretKey As String, accessToken As String, scope As String, refreshToken As String,
        refreshTokenExpiryTime As String, accessTokenExpiryTime As String

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

#End Region

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

#Region "Application events"

    ''' <summary>
    ''' This function is called when the applicaiton page is loaded into the browser.
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
            pnlHeader.Visible = False
            imagePanel.Visible = False
            smilpanel.Visible = False
            If Not Page.IsPostBack Then
                Me.ReadConfigFile()
            End If

            Dim currentServerTime As DateTime = DateTime.UtcNow
            lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC"

            If Not Page.IsPostBack Then
                If (Session("Vb_mim_session_appState") = "GetToken") AndAlso (Request("Code") IsNot Nothing) Then
                    Me.authCode = Request("Code").ToString()
                    If Me.GetAccessToken(AccessTokenType.Authorization_Code) = True Then
                        Dim isUserSpecifiedValues As Boolean = Me.GetSessionValues()
                        If isUserSpecifiedValues = True Then
                            If Session("Vb_Request") = "GetMessageHeaders" Then
                                Me.GetMessageHeaders()
                            ElseIf Session("Vb_Request") = "GetMessageContent" Then
                                Me.GetMessageContentByIDnPartNumber()
                            End If
                        End If
                    Else
                        Me.DrawPanelForFailure(statusPanel, "Failed to get Access token")
                        Me.ResetTokenSessionVariables()
                        Me.ResetTokenVariables()
                    End If
                End If
                End If
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Event, that gets called when user clicks on Get message headers button, 
    ''' performs validations and initiates api call to get header messages.
    ''' </summary>
    ''' <param name="sender">object that initiated this method</param>
    ''' <param name="e">Event Agruments</param>
    Protected Sub GetHeaderButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        If String.IsNullOrEmpty(txtHeaderCount.Text.Trim()) Then
            Me.DrawPanelForFailure(statusPanel, "Specify number of messages to be retrieved")
            Return
        End If

        Dim regex As Regex = New Regex("\d+")
        If Not regex.IsMatch(txtHeaderCount.Text.Trim()) Then
            Me.DrawPanelForFailure(statusPanel, "Specify valid header count")
            Return
        End If

        txtHeaderCount.Text = txtHeaderCount.Text.Trim()
        Session("Vb_HeaderCount") = txtHeaderCount.Text
        Session("Vb_IndexCursor") = txtIndexCursor.Text

        Dim headerCount As Integer = Convert.ToInt32(txtHeaderCount.Text.Trim())
        If headerCount < 1 Or headerCount > 500 Then
            Me.DrawPanelForFailure(statusPanel, "Header Count must be a number between 1-500")
            Return
        End If

        ' Read from config file and initialize variables
        If Me.ReadConfigFile() = False Then
            Return
        End If

        Session("Vb_Request") = "GetMessageHeaders"

        ' Is valid address
        Dim isValid As Boolean = False

        isValid = Me.ReadAndGetAccessToken()

        If isValid = True Then
            pnlHeader.Visible = False
            Me.GetMessageHeaders()
        End If
    End Sub

    ''' <summary>
    ''' Event, that gets called when user clicks on Get message content button, 
    ''' performs validations and calls DisplayImage.aspx page.
    ''' </summary>
    ''' <param name="sender">object that initiated this method</param>
    ''' <param name="e">Event Agruments</param>
    Protected Sub GetMessageContent_Click(ByVal sender As Object, ByVal e As EventArgs)
        If String.IsNullOrEmpty(txtMessageId.Text) Then
            Me.DrawPanelForFailure(ContentPanelStatus, "Specify Message ID")
            Return
        End If

        If String.IsNullOrEmpty(txtPartNumber.Text) Then
            Me.DrawPanelForFailure(ContentPanelStatus, "Specify Part Number of the message")
            Return
        End If

        Session("Vb_MessageID") = txtMessageId.Text
        Session("Vb_PartNumber") = txtPartNumber.Text

        If (Me.ReadConfigFile = False) Then
            Return
        End If

        Session("Vb_Request") = "GetMessageContent"
        Dim isValid As Boolean = False
        isValid = Me.ReadAndGetAccessToken
        If (isValid = True) Then
            Me.GetMessageContentByIDnPartNumber()
        End If

        Return
    End Sub

#End Region

#Region "Appllication Methods"

#Region "Display Status Methods"
    ''' <summary>
    ''' Displays success message.
    ''' </summary>
    ''' <param name="panelParam">Panel to draw success message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForSuccess(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Clear()
        End If

        Dim table As Table = New Table()
        table.CssClass = "successWide"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9

        Dim rowOne As TableRow = New TableRow()
        Dim rowOneCellOne As TableCell = New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)

        Dim rowTwo As TableRow = New TableRow()
        Dim rowTwoCellOne As TableCell = New TableCell()
        rowTwoCellOne.Text = message
        rowTwo.Controls.Add(rowTwoCellOne)
        table.Controls.Add(rowTwo)

        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' Displays error message.
    ''' </summary>
    ''' <param name="panelParam">Panel to draw success message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Clear()
        End If

        Dim table As Table = New Table()
        table.CssClass = "errorWide"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9

        Dim rowOne As TableRow = New TableRow()
        Dim rowOneCellOne As TableCell = New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)

        Dim rowTwo As TableRow = New TableRow()
        Dim rowTwoCellOne As TableCell = New TableCell()
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        table.Controls.Add(rowTwo)

        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' Displays the deserialized output to a grid
    ''' </summary>
    ''' <param name="messageHeaders">Deserialized message header list</param>
    Private Sub DisplayGrid(ByVal messageHeaders As MessageHeaderList)
        Try
            Dim headerTable As DataTable = Me.GetHeaderDataTable()

            If messageHeaders IsNot Nothing And messageHeaders.Headers IsNot Nothing Then
                pnlHeader.Visible = True
                lblHeaderCount.Text = messageHeaders.HeaderCount.ToString()
                lblIndexCursor.Text = messageHeaders.IndexCursor

                Dim row As DataRow
                Dim header As Header
                For Each header In messageHeaders.Headers
                    row = headerTable.NewRow()

                    row("MessageId") = header.MessageId
                    row("From") = header.From
                    Dim SendTo As String
                    If header.To Is Nothing Then
                        SendTo = String.Empty
                    Else
                        SendTo = String.Join("," + Environment.NewLine, header.To.ToArray())
                    End If

                    row("To") = SendTo
                    row("Received") = header.Received
                    row("Text") = header.Text
                    row("Favourite") = header.Favorite
                    row("Read") = header.Read
                    row("Direction") = header.Direction
                    row("Type") = header.Type

                    headerTable.Rows.Add(row)
                    If ((Not (header.Type) Is Nothing) AndAlso (header.Type.ToLower = "mms")) Then
                        For Each mmsCont As MMSContent In header.MmsContent
                            Dim mmsDetailsRow As DataRow = headerTable.NewRow
                            mmsDetailsRow("PartNumber") = mmsCont.PartNumber
                            mmsDetailsRow("ContentType") = mmsCont.ContentType
                            mmsDetailsRow("ContentName") = mmsCont.ContentName
                            headerTable.Rows.Add(mmsDetailsRow)
                        Next
                    End If
                Next

                gvMessageHeaders.DataSource = headerTable
                gvMessageHeaders.DataBind()
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Creates a datatable with message header columns
    ''' </summary>
    ''' <returns>data table with the structure of the grid</returns>
    Private Function GetHeaderDataTable() As DataTable
        Dim messageTable As DataTable = New DataTable()
        Dim column As DataColumn = New DataColumn("MessageId")
        messageTable.Columns.Add(column)

        column = New DataColumn("PartNumber")
        messageTable.Columns.Add(column)

        column = New DataColumn("ContentType")
        messageTable.Columns.Add(column)

        column = New DataColumn("ContentName")
        messageTable.Columns.Add(column)

        column = New DataColumn("From")
        messageTable.Columns.Add(column)

        column = New DataColumn("To")
        messageTable.Columns.Add(column)

        column = New DataColumn("Received")
        messageTable.Columns.Add(column)

        column = New DataColumn("Text")
        messageTable.Columns.Add(column)

        column = New DataColumn("Favourite")
        messageTable.Columns.Add(column)

        column = New DataColumn("Read")
        messageTable.Columns.Add(column)

        column = New DataColumn("Type")
        messageTable.Columns.Add(column)

        column = New DataColumn("Direction")
        messageTable.Columns.Add(column)

        Return messageTable
    End Function
#End Region

#Region "Get Message Header_Content Methods"
    ''' <summary>
    ''' Retreives the message headers based on headerCount and inderCursor.
    ''' </summary>
    Private Sub GetMessageHeaders()
        Try
            Dim mimRequestObject1 As HttpWebRequest

            mimRequestObject1 = CType(WebRequest.Create(String.Empty + Me.endPoint + "/rest/1/MyMessages?HeaderCount=" + txtHeaderCount.Text), HttpWebRequest)
            If Not String.IsNullOrEmpty(txtIndexCursor.Text) Then
                mimRequestObject1 = CType(WebRequest.Create(String.Empty + Me.endPoint + "/rest/1/MyMessages?HeaderCount=" + txtHeaderCount.Text + "&IndexCursor=" + txtIndexCursor.Text), HttpWebRequest)
            End If

            mimRequestObject1.Headers.Add("Authorization", "Bearer " + Me.accessToken)
            mimRequestObject1.Method = "GET"
            mimRequestObject1.KeepAlive = True

            Dim mimResponseObject1 As WebResponse = mimRequestObject1.GetResponse()
            Using sr As New StreamReader(mimResponseObject1.GetResponseStream())
                Dim mimResponseData As String = sr.ReadToEnd()

                Dim deserializeJsonObject As JavaScriptSerializer = New JavaScriptSerializer()
                Dim deserializedJsonObj As MIMResponse = DirectCast(deserializeJsonObject.Deserialize(mimResponseData, GetType(MIMResponse)), MIMResponse)
                If deserializedJsonObj IsNot Nothing Then
                    Me.DrawPanelForSuccess(statusPanel, String.Empty)
                    Me.DisplayGrid(deserializedJsonObj.MessageHeadersList)
                Else
                    Me.DrawPanelForFailure(statusPanel, "No response from server")
                End If

                sr.Close()
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

            Me.DrawPanelForFailure(statusPanel, errorResponse + Environment.NewLine + we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
            Return
        End Try
    End Sub

    ''' <summary>
    ''' Gets the message content for MMS messages based on Message ID and Part Number
    ''' </summary>
    Private Sub GetMessageContentByIDnPartNumber()
        Try
            Dim mimRequestObject1 As HttpWebRequest = CType(WebRequest.Create((String.Empty _
                            + (Me.endPoint + ("/rest/1/MyMessages/" _
                            + (txtMessageId.Text + ("/" + txtPartNumber.Text)))))), HttpWebRequest)
            mimRequestObject1.Headers.Add("Authorization", ("Bearer " + Me.accessToken))
            mimRequestObject1.Method = "GET"
            mimRequestObject1.KeepAlive = True
            Dim offset As Integer = 0
            Dim mimResponseObject1 As WebResponse = mimRequestObject1.GetResponse
            Dim remaining As Integer = Convert.ToInt32(mimResponseObject1.ContentLength)
            Dim stream As Stream = mimResponseObject1.GetResponseStream
            Dim bytes As Byte() = New Byte((mimResponseObject1.ContentLength) - 1) {}

            While (remaining > 0)
                Dim read As Integer = stream.Read(bytes, offset, remaining)
                If (read <= 0) Then
                    Me.DrawPanelForFailure(ContentPanelStatus, String.Format("End of stream reached with {0} bytes left to read", remaining))
                    Return
                End If
                remaining = (remaining - read)
                offset = (offset + read)

            End While
            Dim splitData() As String = Regex.Split(mimResponseObject1.ContentType.ToLower, ";")
            Dim ext() As String = Regex.Split(splitData(0), "/")
            If mimResponseObject1.ContentType.ToLower.Contains("application/smil") Then
                smilpanel.Visible = True
                TextBox1.Text = System.Text.Encoding.Default.GetString(bytes)
                Me.DrawPanelForSuccess(ContentPanelStatus, String.Empty)
            ElseIf mimResponseObject1.ContentType.ToLower.Contains("text/plain") Then
                Me.DrawPanelForSuccess(ContentPanelStatus, System.Text.Encoding.Default.GetString(bytes))
            Else
                imagePanel.Visible = True
                Me.DrawPanelForSuccess(ContentPanelStatus, String.Empty)
                imagetoshow.Src = ("data:" + (splitData(0) + (";base64," + Convert.ToBase64String(bytes, Base64FormattingOptions.None))))
            End If

            If stream IsNot Nothing Then
                stream.Close()
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(ContentPanelStatus, ex.Message)
        End Try
    End Sub
#End Region

    ''' <summary>
    ''' Read parameters from configuraton file and assigns to instance variables.
    ''' </summary>
    ''' <returns>true/false; true if all required parameters are specified, else false</returns>
    Private Function ReadConfigFile() As Boolean
        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(statusPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(statusPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(statusPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.authorizeRedirectUri = ConfigurationManager.AppSettings("authorize_redirect_uri")
        If String.IsNullOrEmpty(Me.authorizeRedirectUri) Then
            Me.DrawPanelForFailure(statusPanel, "authorize_redirect_uri is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "MIM"
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
    ''' Get session values, user supplied and assign to controls.
    ''' </summary>
    ''' <returns>true/false; true if values supplied, else false</returns>
    Private Function GetSessionValues() As Boolean
        Dim isValuesPresent As Boolean = False

        If (Not (Session("Vb_HeaderCount")) Is Nothing) Then
            txtHeaderCount.Text = Session("Vb_HeaderCount").ToString
            isValuesPresent = True
        End If
        If (Not (Session("Vb_IndexCursor")) Is Nothing) Then
            txtIndexCursor.Text = Session("Vb_IndexCursor").ToString
            isValuesPresent = True
        End If
        If (Not (Session("Vb_MessageID")) Is Nothing) Then
            txtMessageId.Text = Session("Vb_MessageID").ToString
            isValuesPresent = True
        End If
        If (Not (Session("Vb_PartNumber")) Is Nothing) Then
            txtPartNumber.Text = Session("Vb_PartNumber").ToString
            isValuesPresent = True
        End If

        Return isValuesPresent
    End Function

    ''' <summary>
    ''' Reads access token information from session and validates the token and 
    ''' based on validity, the method will redirect to auth server/return the access token
    ''' </summary>
    ''' <returns>true/false; true if success in getting access token, else false
    ''' </returns>
    Private Function ReadAndGetAccessToken() As Boolean
        Dim ableToReadAndGetToken As Boolean = True

        Me.ReadTokenSessionVariables()

        Dim tokentResult As String = Me.IsTokenValid()

        If tokentResult.CompareTo("INVALID_ACCESS_TOKEN") = 0 Then
            Session("Vb_mim_session_appState") = "GetToken"
            Me.GetAuthCode()
        ElseIf tokentResult.CompareTo("REFRESH_TOKEN") = 0 Then
            If Me.GetAccessToken(AccessTokenType.Refresh_Token) = False Then
                Me.DrawPanelForFailure(statusPanel, "Failed to get Access token")
                Me.ResetTokenSessionVariables()
                Me.ResetTokenVariables()
                ableToReadAndGetToken = False
            End If
        End If

        If String.IsNullOrEmpty(Me.accessToken) Then
            Me.DrawPanelForFailure(statusPanel, "Failed to get Access token")
            Me.ResetTokenSessionVariables()
            Me.ResetTokenVariables()
            ableToReadAndGetToken = False
        End If

        Return ableToReadAndGetToken
    End Function

    ''' <summary>
    ''' This function resets access token related session variable to null 
    ''' </summary>
    Private Sub ResetTokenSessionVariables()
        Session("Vb_mim_session_access_token") = Nothing
        Session("Vb_mim_session_accessTokenExpiryTime") = Nothing
        Session("Vb_mim_session_refresh_token") = Nothing
        Session("Vb_mim_session_refreshTokenExpiryTime") = Nothing
    End Sub

    ''' <summary>
    ''' This function resets access token related  variable to null 
    ''' </summary>
    Private Sub ResetTokenVariables()
        Me.accessToken = Nothing
        Me.refreshToken = Nothing
        Me.refreshTokenExpiryTime = Nothing
        Me.accessTokenExpiryTime = Nothing
    End Sub

    ''' <summary>
    ''' Redirect to OAuth Consent page and get Authorization Code.
    ''' </summary>
    Private Sub GetAuthCode()
        Try
            Response.Redirect(String.Empty + Me.endPoint + "/oauth/authorize?scope=" + Me.scope + "&client_id=" + Me.apiKey + "&redirect_url=" + Me.authorizeRedirectUri)
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Reads access token related session variables to local variables
    ''' </summary>
    ''' <returns>true/false depending on the session variables</returns>
    Private Function ReadTokenSessionVariables() As Boolean
        If Not Session("Vb_mim_session_access_token") Is Nothing Then
            Me.accessToken = Session("Vb_mim_session_access_token").ToString()
        Else
            Me.accessToken = Nothing
        End If

        If Not Session("Vb_mim_session_accessTokenExpiryTime") Is Nothing Then
            Me.accessTokenExpiryTime = Session("Vb_mim_session_accessTokenExpiryTime").ToString()
        Else
            Me.accessTokenExpiryTime = Nothing
        End If

        If Not Session("Vb_mim_session_refresh_token") Is Nothing Then
            Me.refreshToken = Session("Vb_mim_session_refresh_token").ToString()
        Else
            Me.refreshToken = Nothing
        End If

        If Not Session("Vb_mim_session_refreshTokenExpiryTime") Is Nothing Then
            Me.refreshTokenExpiryTime = Session("Vb_mim_session_refreshTokenExpiryTime").ToString()
        Else
            Me.refreshTokenExpiryTime = Nothing
        End If

        If (Me.accessToken = Nothing) Or (Me.accessTokenExpiryTime = Nothing) Or (Me.refreshToken = Nothing) Or (Me.refreshTokenExpiryTime = Nothing) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Validates access token expiry times.
    ''' </summary>
    ''' <returns>string, returns VALID_ACCESS_TOKEN if its valid
    ''' otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    ''' return REFRESH_TOKEN, if access token in expired and refresh token is valid</returns>
    Private Function IsTokenValid() As String
        If Session("Vb_mim_session_access_token") Is Nothing Then
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
    ''' Get access token based on the type parameter type values.
    ''' </summary>
    ''' <param name="type">If type value is Authorization_Code, access token is fetch for authorization code flow.
    ''' If type value is  Refresh_Token, access token is fetched on the exisiting refresh token.</param>
    ''' <returns>true/false; true if success, else false</returns>
    Private Function GetAccessToken(ByVal type As AccessTokenType) As Boolean
        Dim result As Boolean = False

        Dim postStream As Stream = Nothing
        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty + Me.endPoint + "/oauth/token")
            accessTokenRequest.Method = "POST"

            Dim oauthParameters As String = String.Empty
            If type = AccessTokenType.Authorization_Code Then
                oauthParameters = "client_id=" + Me.apiKey + "&client_secret=" + Me.secretKey + "&code=" + Me.authCode + "&grant_type=authorization_code&scope=" + Me.scope
            ElseIf type = AccessTokenType.Refresh_Token Then
                oauthParameters = "grant_type=refresh_token&client_id=" + Me.apiKey + "&client_secret=" + Me.secretKey + "&refresh_token=" + Me.refreshToken
            End If

            accessTokenRequest.ContentType = "application/x-www-form-urlencoded"
            Dim encoding As UTF8Encoding = New UTF8Encoding()
            Dim postBytes() As Byte = encoding.GetBytes(oauthParameters)
            accessTokenRequest.ContentLength = postBytes.Length
            postStream = accessTokenRequest.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)
            postStream.Close()

            Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
            Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                Dim access_token_json As String = accessTokenResponseStream.ReadToEnd()
                Dim deserializeJsonObject As JavaScriptSerializer = New JavaScriptSerializer()
                Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(access_token_json, GetType(AccessTokenResponse)), AccessTokenResponse)
                If Not deserializedJsonObj.access_token Is Nothing Then
                    Me.accessToken = deserializedJsonObj.access_token
                    Me.refreshToken = deserializedJsonObj.refresh_token

                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(Me.refreshTokenExpiresIn)

                    Session("Vb_mim_session_accessTokenExpiryTime") = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in))

                    If deserializedJsonObj.expires_in.Equals("0") Then
                        Dim defaultAccessTokenExpiresIn As Integer = 100  ' In Years
                        Session("Vb_mim_session_accessTokenExpiryTime") = currentServerTime.AddYears(defaultAccessTokenExpiresIn)
                    End If

                    Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString()
                    Session("Vb_mim_session_access_token") = Me.accessToken
                    Me.accessTokenExpiryTime = Session("Vb_mim_session_accessTokenExpiryTime").ToString()
                    Session("Vb_mim_session_refresh_token") = Me.refreshToken
                    Session("Vb_mim_session_refreshTokenExpiryTime") = Me.refreshTokenExpiryTime
                    Session("Vb_mim_session_appState") = "TokenReceived"
                    accessTokenResponseStream.Close()
                    result = True
                Else
                    Me.DrawPanelForFailure(statusPanel, "Auth server returned null access token")
                End If
            End Using
        Catch we As WebException
            Dim errorResponse As String = String.Empty

            Try
                Using sr2 As New StreamReader(we.Response.GetResponseStream())
                    errorResponse = sr2.ReadToEnd()
                    sr2.Close()
                End Using
            Catch
                errorResponse = "Unable to get access token"
            End Try
            Me.DrawPanelForFailure(statusPanel, errorResponse & Environment.NewLine & we.Message)
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try

        Return result
    End Function

#End Region

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


''' <summary>
''' Response returned from MyMessages api
''' </summary>
Public Class MIMResponse

    ''' <summary>
    ''' Gets or sets the value of message header list.
    ''' </summary>
    Public Property MessageHeadersList() As MessageHeaderList
        Get
            Return m_MessageHeadersList
        End Get
        Set(ByVal value As MessageHeaderList)
            m_MessageHeadersList = value
        End Set
    End Property
    Private m_MessageHeadersList As MessageHeaderList

End Class

''' <summary>
''' Message Header List
''' </summary>
Public Class MessageHeaderList

    ''' <summary>
    ''' Gets or sets the value of object containing a List of Messages Headers
    ''' </summary>
    Public Property Headers() As List(Of Header)
        Get
            Return m_Headers
        End Get
        Set(ByVal value As List(Of Header))
            m_Headers = value
        End Set
    End Property
    Private m_Headers As List(Of Header)

    ''' <summary>
    ''' Gets or sets the value of a number representing the number of headers returned for this request.
    ''' </summary>
    Public Property HeaderCount As Integer
        Get
            Return m_HeaderCount
        End Get
        Set(ByVal value As Integer)
            m_HeaderCount = value
        End Set
    End Property
    Private m_HeaderCount As Integer

    ''' <summary>
    ''' Gets or sets the value of a string which defines the start of the next block of messages for the current request.
    ''' A value of zero (0) indicates the end of the block.
    ''' </summary>
    Public Property IndexCursor As String
        Get
            Return m_IndexCursor
        End Get
        Set(ByVal value As String)
            m_IndexCursor = value
        End Set
    End Property
    Private m_IndexCursor As String

End Class

''' <summary>
''' Object containing a List of Messages Headers
''' </summary>
Public Class Header

    ''' <summary>
    ''' Gets or sets the value of Unique message identifier
    ''' </summary>
    Public Property MessageId As String
        Get
            Return m_MessageId
        End Get
        Set(ByVal value As String)
            m_MessageId = value
        End Set
    End Property
    Private m_MessageId As String

    ''' <summary>
    ''' Gets or sets the value of message sender
    ''' </summary>
    Public Property From As String
        Get
            Return m_From
        End Get
        Set(value As String)
            m_From = value
        End Set
    End Property
    Private m_From As String

    Public Property [To] As List(Of String)
        Get
            Return m_To
        End Get
        Set(value As List(Of String))
            m_To = value
        End Set
    End Property
    Private m_To As List(Of String)

    ''' <summary>
    ''' Gets or sets a value of message text
    ''' </summary>
    Public Property Text As String
        Get
            Return m_Text
        End Get
        Set(value As String)
            m_Text = value
        End Set
    End Property
    Private m_Text As String

    ''' <summary>
    ''' Gets or sets a value of message part descriptions
    ''' </summary>
    Public Property MmsContent As List(Of MMSContent)
        Get
            Return m_MmsContent
        End Get
        Set(value As List(Of MMSContent))
            m_MmsContent = value
        End Set
    End Property
    Private m_MmsContent As List(Of MMSContent)

    ''' <summary>
    ''' Gets or sets the value of date/time message received
    ''' </summary>
    Public Property Received As DateTime
        Get
            Return m_Received
        End Get
        Set(value As DateTime)
            m_Received = value
        End Set
    End Property
    Private m_Received As DateTime

    ''' <summary>
    ''' Gets or sets a value indicating whether its a favourite or not
    ''' </summary>
    Public Property Favorite As Boolean
        Get
            Return m_Favorite
        End Get
        Set(value As Boolean)
            m_Favorite = value
        End Set
    End Property
    Private m_Favorite As Boolean

    ''' <summary>
    ''' Gets or sets a value indicating whether message is read or not
    ''' </summary>
    Public Property Read As Boolean
        Get
            Return m_Read
        End Get
        Set(value As Boolean)
            m_Read = value
        End Set
    End Property
    Private m_Read As Boolean

    ''' <summary>
    ''' Gets or sets the value of type of message, TEXT or MMS
    ''' </summary>
    Public Property Type As String
        Get
            Return m_Type
        End Get
        Set(value As String)
            m_Type = value
        End Set
    End Property
    Private m_Type As String

    ''' <summary>
    ''' Gets or sets the message direction IN or OUT
    ''' </summary>
    Public Property Direction As String
        Get
            Return m_Direction
        End Get
        Set(value As String)
            m_Direction = value
        End Set
    End Property
    Private m_Direction As String

End Class

''' <summary>
''' Message part descriptions
''' </summary>
Public Class MMSContent

    ''' <summary>
    ''' Gets or sets the value of content name
    ''' </summary>
    Public Property ContentName As String
        Get
            Return m_ContentName
        End Get
        Set(value As String)
            m_ContentName = value
        End Set
    End Property
    Private m_ContentName As String

    ''' <summary>
    ''' Gets or sets the value of content type
    ''' </summary>
    Public Property ContentType As String
        Get
            Return m_ContentType
        End Get
        Set(value As String)
            m_ContentType = value
        End Set
    End Property
    Private m_ContentType As String

    ''' <summary>
    ''' Gets or sets the value of part number
    ''' </summary>
    Public Property PartNumber As String
        Get
            Return m_PartNumber
        End Get
        Set(value As String)
            m_PartNumber = value
        End Set
    End Property
    Private m_PartNumber As String
End Class