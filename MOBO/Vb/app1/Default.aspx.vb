' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>
#Region "References"

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Configuration
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
''' Mobo Sample Applicaton class
''' </summary>
Partial Public Class Mobo_App1
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' API Address
    ''' </summary>
    Private endPoint As String

    ''' <summary>
    ''' Access token variables - temporary
    ''' </summary>
    Private apiKey As String, authCode As String, authorizeRedirectUri As String, secretKey As String, accessToken As String, scope As String, _
     refreshToken As String, refreshTokenExpiryTime As String, accessTokenExpiryTime As String

    ''' <summary>
    ''' Maximum number of addresses user can specify
    ''' </summary>
    Private maxAddresses As Integer

    ''' <summary>
    ''' List of addresses to send
    ''' </summary>
    Private addressList As New List(Of String)()

    ''' <summary>
    ''' Variable to hold phone number(s)/email address(es)/short code(s) parameter.
    ''' </summary>
    Private phoneNumbersParameter As String = Nothing

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

    ''' <summary>
    ''' This function is called when the applicaiton page is loaded into the browser.
    ''' This function reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">Button that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        Try
            ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
            Dim currentServerTime As DateTime = DateTime.UtcNow
            lblServerTime.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC"

            Me.ReadConfigFile()

            If (Session("Vb_mobo_session_appState") = "GetToken") AndAlso (Request("Code") IsNot Nothing) Then
                Me.authCode = Request("code").ToString()
                If Me.GetAccessToken(AccessTokenType.Authorization_Code) = True Then
                    If Session("Vb_Address") IsNot Nothing Then
                        txtPhone.Text = Session("Vb_Address").ToString()
                    End If

                    If Session("Vb_Message") IsNot Nothing Then
                        txtMessage.Text = Session("Vb_Message").ToString()
                    End If

                    If Session("Vb_Subject") IsNot Nothing Then
                        txtSubject.Text = Session("Vb_Subject").ToString()
                    End If

                    If Session("Vb_Group") IsNot Nothing Then
                        chkGroup.Checked = Convert.ToBoolean(Session("Vb_Group").ToString())
                    End If

                    Me.IsValidAddress()
                    Dim attachmentsList As ArrayList = DirectCast(Session("Vb_Attachments"), ArrayList)
                    Me.SendMessage(attachmentsList)
                Else
                    Me.DrawPanelForFailure(statusPanel, "Failed to get Access token")
                    Me.ResetTokenSessionVariables()
                    Me.ResetTokenVariables()
                    Return
                End If
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Event, that gets called when user clicks on send message button, performs validations and initiates api call to send message
    ''' </summary>
    ''' <param name="sender">object that initiated this method</param>
    ''' <param name="e">Event Agruments</param>
    Protected Sub BtnSendMessage_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Perform validations

        ' Read from config file and initialize variables
        If Me.ReadConfigFile() = False Then
            Return
        End If

        ' Is valid address
        Dim isValid As Boolean = False
        isValid = Me.IsValidAddress()
        If isValid = False Then
            Return
        End If

        ' User provided any attachments?
        If Not String.IsNullOrEmpty(fileUpload1.FileName) OrElse Not String.IsNullOrEmpty(fileUpload2.FileName) OrElse Not String.IsNullOrEmpty(fileUpload3.FileName) OrElse Not String.IsNullOrEmpty(fileUpload4.FileName) OrElse Not String.IsNullOrEmpty(fileUpload5.FileName) Then
            ' Is valid file size
            isValid = Me.IsValidFileSize()
            If isValid = False Then
                Return
            End If
        Else
            ' Message is mandatory, if no attachments
            If String.IsNullOrEmpty(txtMessage.Text) Then
                Me.DrawPanelForFailure(statusPanel, "Specify message to be sent")
                Return
            End If
        End If

        Session("Vb_Address") = txtPhone.Text
        Session("Vb_Message") = txtMessage.Text
        Session("Vb_Subject") = txtSubject.Text
        Session("Vb_Group") = chkGroup.Checked

        Me.ReadTokenSessionVariables()

        Dim tokentResult As String = Me.IsTokenValid()

        If tokentResult.CompareTo("INVALID_ACCESS_TOKEN") = 0 Then
            Session("Vb_mobo_session_appState") = "GetToken"
            Me.GetAuthCode()
        ElseIf tokentResult.CompareTo("REFRESH_TOKEN") = 0 Then
            If Me.GetAccessToken(AccessTokenType.Refresh_Token) = False Then
                Me.DrawPanelForFailure(statusPanel, "Failed to get Access token")
                Me.ResetTokenSessionVariables()
                Me.ResetTokenVariables()
                Return
            End If
        End If

        If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
            Return
        End If

        ' Initiate api call to send message
        Dim attachmentsList As ArrayList = DirectCast(Session("Vb_Attachments"), ArrayList)
        Me.SendMessage(attachmentsList)
    End Sub

#Region "Validation Functions"

    ''' <summary>
    ''' Validates the given addresses based on following conditions
    ''' 1. Group messages should not allow short codes
    ''' 2. Short codes should be 3-8 digits in length
    ''' 3. Valid Email Address
    ''' 4. Group message must contain more than one address
    ''' 5. Valid Phone number
    ''' </summary>
    ''' <returns>true/false; true - if address specified met the validation criteria, else false</returns>
    Private Function IsValidAddress() As Boolean
        Dim phonenumbers As String = String.Empty

        Dim isValid As Boolean = True
        If String.IsNullOrEmpty(txtPhone.Text) Then
            Me.DrawPanelForFailure(statusPanel, "Address field cannot be blank.")
            Return False
        End If

        Dim addresses As String() = txtPhone.Text.Trim().Split(","c)

        If addresses.Length > Me.maxAddresses Then
            Me.DrawPanelForFailure(statusPanel, "Message cannot be delivered to more than 10 receipients.")
            Return False
        End If

        If chkGroup.Checked AndAlso addresses.Length < 2 Then
            Me.DrawPanelForFailure(statusPanel, "Specify more than one address for Group message.")
            Return False
        End If

        For Each address As String In addresses
            If String.IsNullOrEmpty(address) Then
                Exit For
            End If

            If address.Length < 3 Then
                Me.DrawPanelForFailure(statusPanel, "Invalid address specified.")
                Return False
            End If

            ' Verify if short codes are present in address
            If Not address.StartsWith("short") AndAlso (address.Length > 2 AndAlso address.Length < 9) Then
                If chkGroup.Checked Then
                    Me.DrawPanelForFailure(statusPanel, "Group Message with short codes is not allowed.")
                    Return False
                End If

                Me.addressList.Add(address)
                Me.phoneNumbersParameter = Me.phoneNumbersParameter + "Addresses=short:" + Server.UrlEncode(address.ToString()) + "&"
            End If

            If address.StartsWith("short") Then
                If chkGroup.Checked Then
                    Me.DrawPanelForFailure(statusPanel, "Group Message with short codes is not allowed.")
                    Return False
                End If

                Dim regex As System.Text.RegularExpressions.Regex = New Regex("^[0-9]*$")
                If Not regex.IsMatch(address.Substring(6)) Then
                    Me.DrawPanelForFailure(statusPanel, "Invalid short code specified.")
                    Return False
                End If

                Me.addressList.Add(address)
                Me.phoneNumbersParameter = Me.phoneNumbersParameter + "Addresses=" + Server.UrlEncode(address.ToString()) + "&"
            ElseIf address.Contains("@") Then
                isValid = Me.IsValidEmail(address)
                If isValid = False Then
                    Me.DrawPanelForFailure(statusPanel, "Specified Email Address is invalid.")
                    Return False
                Else
                    Me.addressList.Add(address)
                    Me.phoneNumbersParameter = Me.phoneNumbersParameter + "Addresses=" + Server.UrlEncode(address.ToString()) + "&"
                End If
            Else
                If Me.IsValidMISDN(address) = True Then
                    If address.StartsWith("tel:") Then
                        phonenumbers = address.Replace("-", String.Empty)
                        Me.phoneNumbersParameter = Me.phoneNumbersParameter + "Addresses=" + Server.UrlEncode(phonenumbers.ToString()) + "&"
                    Else
                        phonenumbers = address.Replace("-", String.Empty)
                        Me.phoneNumbersParameter = Me.phoneNumbersParameter + "Addresses=" + Server.UrlEncode("tel:" + phonenumbers.ToString()) + "&"
                    End If

                    Me.addressList.Add(address)
                End If
            End If
        Next

        Return True
    End Function

    ''' <summary>
    ''' Validate given string for MSISDN
    ''' </summary>
    ''' <param name="number">Phone number to be validated</param>
    ''' <returns>true/false; true - if valid MSISDN, else false</returns>
    Private Function IsValidMISDN(ByVal number As String) As Boolean
        Dim smsAddressInput As String = number
        Dim tryParseResult As Long = 0
        Dim smsAddressFormatted As String
        Dim phoneStringPattern As String = "^\d{3}-\d{3}-\d{4}$"
        If Regex.IsMatch(smsAddressInput, phoneStringPattern) Then
            smsAddressFormatted = smsAddressInput.Replace("-", String.Empty)
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

    ''' <summary>
    ''' Validates given mail ID for standard mail format
    ''' </summary>
    ''' <param name="emailID">Mail Id to be validated</param>
    ''' <returns> true/false; true - if valid email id, else false</returns>
    Private Function IsValidEmail(ByVal emailID As String) As Boolean
        Dim strRegex As String = "^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" + "\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" + ".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"
        Dim re As New Regex(strRegex)
        If re.IsMatch(emailID) Then
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Validates a given string for digits
    ''' </summary>
    ''' <param name="address">string to be validated</param>
    ''' <returns>true/false; true - if passed string has all digits, else false</returns>
    Private Function IsNumber(ByVal address As String) As Boolean
        Dim isValid As Boolean = False
        Dim regex As New Regex("^[0-9]*$")
        If regex.IsMatch(address) Then
            isValid = True
        End If

        Return isValid
    End Function

    ''' <summary>
    ''' Validates for file size
    ''' Per specification, the maximum file size should be less than 600 KB
    ''' </summary>
    ''' <returns>true/false; Returns false, if file size exceeds 600KB. else true</returns>
    Private Function IsValidFileSize() As Boolean
        Dim fileList As New ArrayList()

        Dim fileSize As Long = 0
        If Not [String].IsNullOrEmpty(fileUpload1.FileName) Then
            fileUpload1.SaveAs(Request.MapPath(fileUpload1.FileName.ToString()))
            fileList.Add(Request.MapPath(fileUpload1.FileName))
            Dim fileInfoObj As New FileInfo(Request.MapPath(fileUpload1.FileName))
            fileSize = fileSize + (fileInfoObj.Length / 1024)
            If fileSize > 600 Then
                'delete saved file.
                Me.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB")
                Return False
            End If
        End If

        If Not [String].IsNullOrEmpty(fileUpload2.FileName) Then
            fileUpload2.SaveAs(Request.MapPath(fileUpload2.FileName))
            fileList.Add(Request.MapPath(fileUpload2.FileName))
            Dim fileInfoObj As New FileInfo(Request.MapPath(fileUpload2.FileName))
            fileSize = fileSize + (fileInfoObj.Length / 1024)
            If fileSize > 600 Then
                Me.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB")
                Return False
            End If
        End If

        If Not [String].IsNullOrEmpty(fileUpload3.FileName) Then
            fileUpload3.SaveAs(Request.MapPath(fileUpload3.FileName))
            fileList.Add(Request.MapPath(fileUpload3.FileName))
            Dim fileInfoObj As New FileInfo(Request.MapPath(fileUpload3.FileName))
            fileSize = fileSize + (fileInfoObj.Length / 1024)
            If fileSize > 600 Then
                'delete saved file.
                Me.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB")
                Return False
            End If
        End If

        If Not [String].IsNullOrEmpty(fileUpload4.FileName) Then
            fileUpload4.SaveAs(Request.MapPath(fileUpload4.FileName))
            fileList.Add(Request.MapPath(fileUpload4.FileName))
            Dim fileInfoObj As New FileInfo(Request.MapPath(fileUpload4.FileName))
            fileSize = fileSize + (fileInfoObj.Length / 1024)
            If fileSize > 600 Then
                'delete saved file.
                Me.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600 KB")
                Return False
            End If
        End If

        If Not [String].IsNullOrEmpty(fileUpload5.FileName) Then
            fileUpload5.SaveAs(Request.MapPath(fileUpload5.FileName))
            fileList.Add(Request.MapPath(fileUpload5.FileName))
            Dim fileInfoObj As New FileInfo(Request.MapPath(fileUpload5.FileName))
            fileSize = fileSize + (fileInfoObj.Length / 1024)
            If fileSize > 600 Then
                'delete saved file.
                Me.DrawPanelForFailure(statusPanel, "Attachment file size exceeded 600kb")
                Return False
            End If
        End If

        If fileList IsNot Nothing AndAlso fileList.Count <> 0 Then
            Session("Vb_Attachments") = fileList
        End If

        Return True
    End Function

#End Region

#Region "Display status Functions"

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
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Font.Bold = True
        rowTwoCellOne.Text = "Message ID:"
        rowTwoCellOne.Width = Unit.Pixel(70)
        rowTwo.Controls.Add(rowTwoCellOne)
        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellTwo.Text = message.ToString()
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

#Region "Access Token functions"

    ''' <summary>
    ''' Read parameters from configuraton file
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
            Me.scope = "MOBO"
        End If

        If String.IsNullOrEmpty(ConfigurationManager.AppSettings("max_addresses")) Then
            Me.maxAddresses = 10
        Else
            Me.maxAddresses = Convert.ToInt32(ConfigurationManager.AppSettings("max_addresses"))
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
    ''' This function resets access token related session variable to null 
    ''' </summary>
    Private Sub ResetTokenSessionVariables()
        Session("Vb_mobo_session_access_token") = Nothing
        Session("Vb_mobo_session_accessTokenExpiryTime") = Nothing
        Session("Vb_mobo_session_refresh_token") = Nothing
        Session("Vb_mobo_session_refreshTokenExpiryTime") = Nothing
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
    ''' Redirect to OAuth and get Authorization Code
    ''' </summary>
    Private Sub GetAuthCode()
        Try
            ' Response.Redirect(string.Empty + "https://auth-uat.san2.attcompute.com/user/login?scope=" + this.scope + "&client_id=" + this.apiKey + "&redirect_url=" + this.authorizeRedirectUri);
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
        If Session("Vb_mobo_session_access_token") IsNot Nothing Then
            Me.accessToken = Session("Vb_mobo_session_access_token").ToString()
        Else
            Me.accessToken = Nothing
        End If

        If Session("Vb_mobo_session_accessTokenExpiryTime") IsNot Nothing Then
            Me.accessTokenExpiryTime = Session("Vb_mobo_session_accessTokenExpiryTime").ToString()
        Else
            Me.accessTokenExpiryTime = Nothing
        End If

        If Session("Vb_mobo_session_refresh_token") IsNot Nothing Then
            Me.refreshToken = Session("Vb_mobo_session_refresh_token").ToString()
        Else
            Me.refreshToken = Nothing
        End If

        If Session("Vb_mobo_session_refreshTokenExpiryTime") IsNot Nothing Then
            Me.refreshTokenExpiryTime = Session("Vb_mobo_session_refreshTokenExpiryTime").ToString()
        Else
            Me.refreshTokenExpiryTime = Nothing
        End If

        If (Me.accessToken Is Nothing) OrElse (Me.accessTokenExpiryTime Is Nothing) OrElse (Me.refreshToken Is Nothing) OrElse (Me.refreshTokenExpiryTime Is Nothing) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Validates access token related variables
    ''' </summary>
    ''' <returns>string, returns VALID_ACCESS_TOKEN if its valid
    ''' otherwise, returns INVALID_ACCESS_TOKEN if refresh token expired or not able to read session variables
    ''' return REFRESH_TOKEN, if access token in expired and refresh token is valid</returns>
    Private Function IsTokenValid() As String
        If Session("Vb_mobo_session_access_token") Is Nothing Then
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

    ''' <summary>
    ''' Get access token based on the type parameter type values.
    ''' </summary>
    ''' <param name="type">If type value is 0, access token is fetch for authorization code flow
    ''' If type value is 2, access token is fetch for authorization code floww based on the exisiting refresh token</param>
    ''' <returns>true/false; true if success, else false</returns>
    Private Function GetAccessToken(ByVal type As AccessTokenType) As Boolean
        Dim postStream As Stream = Nothing
        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty + Me.endPoint + "/oauth/token")
            accessTokenRequest.Method = "POST"
            Dim oauthParameters As String = String.Empty

            If type = AccessTokenType.Authorization_Code Then
                oauthParameters = "client_id=" + Me.apiKey + "&client_secret=" + Me.secretKey + "&code=" + Me.authCode + "&grant_type=authorization_code&scope=MOBO"
            Else
                oauthParameters = "grant_type=refresh_token&client_id=" + Me.apiKey + "&client_secret=" + Me.secretKey + "&refresh_token=" + Me.refreshToken
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

                    Dim refreshExpiry As DateTime = currentServerTime.AddHours(Me.refreshTokenExpiresIn)

                    Session("Vb_mobo_session_accessTokenExpiryTime") = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in))

                    If deserializedJsonObj.expires_in.Equals("0") Then
                        Dim defaultAccessTokenExpiresIn As Integer = 100
                        ' In Years
                        Session("Vb_mobo_session_accessTokenExpiryTime") = currentServerTime.AddYears(defaultAccessTokenExpiresIn)
                    End If

                    Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString()

                    Session("Vb_mobo_session_access_token") = Me.accessToken

                    Me.accessTokenExpiryTime = Session("Vb_mobo_session_accessTokenExpiryTime").ToString()
                    Session("Vb_mobo_session_refresh_token") = Me.refreshToken
                    Session("Vb_mobo_session_refreshTokenExpiryTime") = Me.refreshTokenExpiryTime.ToString()
                    Session("Vb_mobo_session_appState") = "TokenReceived"
                    accessTokenResponseStream.Close()
                    Return True
                Else
                    Me.DrawPanelForFailure(statusPanel, "Auth server returned null access token")
                    Return False
                End If
            End Using
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try

        Return False
    End Function

#End Region

#Region "Send Message Functions"

    ''' <summary>
    ''' Gets the mapping of extension with predefined content types
    ''' </summary>
    ''' <param name="extension">file extension</param>
    ''' <returns>string, content type</returns>
    Private Function GetContentTypeFromExtension(ByVal extension As String) As String
        Dim extensionToContentType As New Dictionary(Of String, String)()
        extensionToContentType.Add(".jpg", "image/jpeg")
        extensionToContentType.Add(".bmp", "image/bmp")
        extensionToContentType.Add(".mp3", "audio/mp3")
        extensionToContentType.Add(".m4a", "audio/m4a")
        extensionToContentType.Add(".gif", "image/gif")
        extensionToContentType.Add(".3gp", "video/3gpp")
        extensionToContentType.Add(".3g2", "video/3gpp2")
        extensionToContentType.Add(".wmv", "video/x-ms-wmv")
        extensionToContentType.Add(".m4v", "video/x-m4v")
        extensionToContentType.Add(".mp4", "video/mp4")
        extensionToContentType.Add(".avi", "video/x-msvideo")
        extensionToContentType.Add(".mov", "video/quicktime")
        extensionToContentType.Add(".mpeg", "video/mpeg")
        extensionToContentType.Add(".wav", "audio/x-wav")
        extensionToContentType.Add(".aiff", "audio/x-aiff")
        extensionToContentType.Add(".aifc", "audio/x-aifc")
        extensionToContentType.Add(".midi", ".midi")
        extensionToContentType.Add(".au", "audio/basic")
        extensionToContentType.Add(".xwd", "image/x-xwindowdump")
        extensionToContentType.Add(".png", "image/png")
        extensionToContentType.Add(".tiff", "image/tiff")
        extensionToContentType.Add(".ief", "image/ief")
        extensionToContentType.Add(".txt", "text/plain")
        extensionToContentType.Add(".html", "text/html")
        extensionToContentType.Add(".vcf", "text/x-vcard")
        extensionToContentType.Add(".vcs", "text/x-vcalendar")
        extensionToContentType.Add(".mid", "application/x-midi")
        extensionToContentType.Add(".imy", "audio/iMelody")

        If extensionToContentType.ContainsKey(extension) Then
            Return extensionToContentType(extension)
        Else
            Return "Not Found"
        End If
    End Function

    ''' <summary>
    ''' Sends message to the list of addresses provided.
    ''' </summary>
    ''' <param name="attachments">List of attachments</param>
    Private Sub SendMessage(ByVal attachments As ArrayList)
        Dim postStream As Stream = Nothing

        Try
            Dim subject As String = txtSubject.Text
            Dim boundaryToSend As String = "----------------------------" + DateTime.Now.Ticks.ToString("x")

            Dim msgRequestObject As HttpWebRequest = DirectCast(WebRequest.Create(String.Empty + Me.endPoint + "/rest/1/MyMessages"), HttpWebRequest)
            msgRequestObject.Headers.Add("Authorization", "Bearer " + Me.accessToken)
            msgRequestObject.Method = "POST"
            Dim contentType As String = "multipart/form-data; type=""application/x-www-form-urlencoded""; start=""<startpart>""; boundary=""" + boundaryToSend + """" & vbCr & vbLf
            msgRequestObject.ContentType = contentType
            Dim mmsParameters As String = Me.phoneNumbersParameter + "Subject=" + Server.UrlEncode(subject) + "&Text=" + Server.UrlEncode(txtMessage.Text) + "&Group=" + chkGroup.Checked.ToString().ToLower()

            Dim dataToSend As String = String.Empty
            dataToSend += "--" + boundaryToSend + vbCr & vbLf
            dataToSend += "Content-Type: application/x-www-form-urlencoded; charset=UTF-8" & vbCr & vbLf & "Content-Transfer-Encoding: 8bit" & vbCr & vbLf & "Content-Disposition: form-data; name=""root-fields""" & vbCr & vbLf & "Content-ID: <startpart>" & vbCr & vbLf & vbCr & vbLf + mmsParameters + vbCr & vbLf

            Dim encoding As New UTF8Encoding()
            If (attachments Is Nothing) OrElse (attachments.Count = 0) Then
                If Not chkGroup.Checked Then
                    msgRequestObject.ContentType = "application/x-www-form-urlencoded"
                    Dim postBytes As Byte() = encoding.GetBytes(mmsParameters)
                    msgRequestObject.ContentLength = postBytes.Length

                    postStream = msgRequestObject.GetRequestStream()
                    postStream.Write(postBytes, 0, postBytes.Length)
                    postStream.Close()

                    Dim mmsResponseObject1 As WebResponse = msgRequestObject.GetResponse()
                    Using sr As New StreamReader(mmsResponseObject1.GetResponseStream())
                        Dim mmsResponseData As String = sr.ReadToEnd()
                        Dim deserializeJsonObject As New JavaScriptSerializer()
                        Dim deserializedJsonObj As MsgResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(MsgResponseId)), MsgResponseId)
                        Me.DrawPanelForSuccess(statusPanel, deserializedJsonObj.Id.ToString())
                        sr.Close()
                    End Using
                Else
                    dataToSend += "--" + boundaryToSend + "--" & vbCr & vbLf
                    Dim bytesToSend As Byte() = encoding.GetBytes(dataToSend)

                    Dim sizeToSend As Integer = bytesToSend.Length

                    Dim memBufToSend = New MemoryStream(New Byte(sizeToSend - 1) {}, 0, sizeToSend, True, True)
                    memBufToSend.Write(bytesToSend, 0, bytesToSend.Length)

                    Dim finalData As Byte() = memBufToSend.GetBuffer()
                    msgRequestObject.ContentLength = finalData.Length

                    postStream = msgRequestObject.GetRequestStream()
                    postStream.Write(finalData, 0, finalData.Length)

                    Dim mmsResponseObject1 As WebResponse = msgRequestObject.GetResponse()
                    Using sr As New StreamReader(mmsResponseObject1.GetResponseStream())
                        Dim mmsResponseData As String = sr.ReadToEnd()
                        Dim deserializeJsonObject As New JavaScriptSerializer()
                        Dim deserializedJsonObj As MsgResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(MsgResponseId)), MsgResponseId)
                        Me.DrawPanelForSuccess(statusPanel, deserializedJsonObj.Id.ToString())
                        sr.Close()
                    End Using
                End If
            Else
                Dim dataBytes As Byte() = encoding.GetBytes(String.Empty)
                Dim totalDataBytes As Byte() = encoding.GetBytes(String.Empty)
                Dim count As Integer = 0
                For Each attachment As String In attachments
                    Dim mmsFileName As String = Path.GetFileName(attachment.ToString())
                    Dim mmsFileExtension As String = Path.GetExtension(attachment.ToString())
                    Dim attachmentContentType As String = Me.GetContentTypeFromExtension(mmsFileExtension)
                    Dim imageFileStream As New FileStream(attachment.ToString(), FileMode.Open, FileAccess.Read)
                    Dim imageBinaryReader As New BinaryReader(imageFileStream)
                    Dim image As Byte() = imageBinaryReader.ReadBytes(CInt(imageFileStream.Length))
                    imageBinaryReader.Close()
                    imageFileStream.Close()
                    If count = 0 Then
                        dataToSend += vbCr & vbLf & "--" + boundaryToSend + vbCr & vbLf
                    Else
                        dataToSend = vbCr & vbLf & "--" + boundaryToSend + vbCr & vbLf
                    End If

                    dataToSend += "Content-Disposition: form-data; name=""file" + count.ToString() + """; filename=""" + mmsFileName + """" & vbCr & vbLf
                    dataToSend += "Content-Type:" + attachmentContentType + vbCr & vbLf
                    dataToSend += "Content-ID:<" + mmsFileName + ">" & vbCr & vbLf
                    dataToSend += "Content-Transfer-Encoding:binary" & vbCr & vbLf & vbCr & vbLf
                    Dim dataToSendByte As Byte() = encoding.GetBytes(dataToSend)
                    Dim dataToSendSize As Integer = dataToSendByte.Length + image.Length
                    Dim tempMemoryStream = New MemoryStream(New Byte(dataToSendSize - 1) {}, 0, dataToSendSize, True, True)
                    tempMemoryStream.Write(dataToSendByte, 0, dataToSendByte.Length)
                    tempMemoryStream.Write(image, 0, image.Length)
                    dataBytes = tempMemoryStream.GetBuffer()
                    If count = 0 Then
                        totalDataBytes = dataBytes
                    Else
                        Dim tempForTotalBytes As Byte() = totalDataBytes
                        Dim tempMemoryStreamAttach = JoinTwoByteArrays(tempForTotalBytes, dataBytes)
                        totalDataBytes = tempMemoryStreamAttach.GetBuffer()
                    End If

                    count += 1
                Next

                Dim byteLastBoundary As Byte() = encoding.GetBytes(vbCr & vbLf & "--" + boundaryToSend + "--" & vbCr & vbLf)
                Dim totalDataSize As Integer = totalDataBytes.Length + byteLastBoundary.Length
                Dim totalMemoryStream = New MemoryStream(New Byte(totalDataSize - 1) {}, 0, totalDataSize, True, True)
                totalMemoryStream.Write(totalDataBytes, 0, totalDataBytes.Length)
                totalMemoryStream.Write(byteLastBoundary, 0, byteLastBoundary.Length)
                Dim finalpostBytes As Byte() = totalMemoryStream.GetBuffer()

                msgRequestObject.ContentLength = finalpostBytes.Length

                postStream = msgRequestObject.GetRequestStream()
                postStream.Write(finalpostBytes, 0, finalpostBytes.Length)

                Dim mmsResponseObject1 As WebResponse = msgRequestObject.GetResponse()
                Using sr As New StreamReader(mmsResponseObject1.GetResponseStream())
                    Dim mmsResponseData As String = sr.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As MsgResponseId = DirectCast(deserializeJsonObject.Deserialize(mmsResponseData, GetType(MsgResponseId)), MsgResponseId)
                    Me.DrawPanelForSuccess(statusPanel, deserializedJsonObj.Id.ToString())
                    sr.Close()
                End Using
            End If
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Dim reader As New StreamReader(stream)
                    Me.DrawPanelForFailure(statusPanel, reader.ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If

            If attachments IsNot Nothing AndAlso attachments.Count <> 0 Then
                For Each file__1 As String In attachments
                    Try
                        File.Delete(file__1)
                        Session("Vb_Attachments") = Nothing
                    Catch
                    End Try
                Next
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Sums up two byte arrays.
    ''' </summary>
    ''' <param name="firstByteArray">First byte array</param>
    ''' <param name="secondByteArray">second byte array</param>
    ''' <returns>The memorystream"/> summed memory stream</returns>
    Private Function JoinTwoByteArrays(ByVal firstByteArray As Byte(), ByVal secondByteArray As Byte()) As MemoryStream
        Dim newSize As Integer = firstByteArray.Length + secondByteArray.Length
        Dim totalMemoryStream = New MemoryStream(New Byte(newSize - 1) {}, 0, newSize, True, True)
        totalMemoryStream.Write(firstByteArray, 0, firstByteArray.Length)
        totalMemoryStream.Write(secondByteArray, 0, secondByteArray.Length)
        Return totalMemoryStream
    End Function

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
''' Response from Mobo api
''' </summary>
Public Class MsgResponseId
    ''' <summary>
    ''' Gets or sets Message ID
    ''' </summary>
    Public Property Id() As String
        Get
            Return m_Id
        End Get
        Set(ByVal value As String)
            m_Id = Value
        End Set
    End Property
    Private m_Id As String
End Class

#End Region
