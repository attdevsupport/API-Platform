' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"
Imports System.Collections
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Script.Serialization
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Xml
#End Region

''' <summary>
''' Default Class
''' </summary>
Partial Public Class Payment_App1
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private accessTokenFilePath As String, refundFile As String, apiKey As String, secretKey As String, accessToken As String, endPoint As String, _
     scope As String, expirySeconds As String, refreshToken As String, accessTokenExpiryTime As String, refreshTokenExpiryTime As String, amount As String, _
     channel As String, description As String, merchantTransactionId As String, merchantProductId As String, merchantApplicationId As String, transactionTimeString As String, _
     signedPayload As String, signedSignature As String, notaryURL As String, notificationDetailsFile As String

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private successTable As Table, failureTable As Table, successTableGetTransaction As Table, notificationDetailsTable As Table, successTableRefund As Table

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private category As Integer, noOfNotificationsToDisplay As Integer

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private merchantRedirectURI As Uri

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private transactionTime As DateTime

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private refundCountToDisplay As Integer = 0

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private refundList As New List(Of KeyValuePair(Of String, String))()

    ''' <summary>
    ''' Global Variable Declaration
    ''' </summary>
    Private latestFive As Boolean = True

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

    ''' <summary>
    ''' This function is used to neglect the ssl handshake error with authentication server.
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
    ''' Method to process create transaction response
    ''' </summary>
    Public Sub ProcessCreateTransactionResponse()
        lbltrancode.Text = Request("TransactionAuthCode").ToString()
        lbltranid.Text = Session("merTranId").ToString()
        transactionSuccessTable.Visible = True
        GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " & Session("merTranId").ToString()
        GetTransactionAuthCode.Text = "Auth Code: " & Request("TransactionAuthCode").ToString()
        GetTransactionTransID.Text = "Transaction ID: "
        Session("tempMerTranId") = Session("merTranId").ToString()
        Session("merTranId") = Nothing
        Session("TranAuthCode") = Request("TransactionAuthCode").ToString()
        Return
    End Sub

    ''' <summary>
    ''' This function reads the Access Token File and stores the values of access token, expiry seconds
    ''' refresh token, last access token time and refresh token expiry time
    ''' This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    ''' </summary>
    ''' <returns>Returns Boolean</returns>
    Public Function ReadAccessTokenFile() As Boolean
        Try
            Dim file As New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read)
            Dim sr As New StreamReader(file)
            Me.accessToken = sr.ReadLine()
            Me.expirySeconds = sr.ReadLine()
            Me.refreshToken = sr.ReadLine()
            Me.accessTokenExpiryTime = sr.ReadLine()
            Me.refreshTokenExpiryTime = sr.ReadLine()
            sr.Close()
            file.Close()
        Catch generatedExceptionName As Exception
            Return False
        End Try

        If (Me.accessToken Is Nothing) OrElse (Me.expirySeconds Is Nothing) OrElse (Me.refreshToken Is Nothing) OrElse (Me.accessTokenExpiryTime Is Nothing) OrElse (Me.refreshTokenExpiryTime Is Nothing) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function validates the expiry of the access token and refresh token,
    ''' function compares the current time with the refresh token taken time, if current time is greater then 
    ''' returns INVALID_REFRESH_TOKEN
    ''' function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    ''' funciton returns INVALID_ACCESS_TOKEN
    ''' otherwise returns VALID_ACCESS_TOKEN
    ''' </summary>
    ''' <returns>Return String</returns>
    Public Function IsTokenValid() As String
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
    ''' This function get the access token based on the type parameter type values.
    ''' If type value is 1, access token is fetch for client credential flow
    ''' If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
    ''' </summary>
    ''' <param name="type">Type as Interger</param>
    ''' <param name="panelParam">Panel Details</param>
    ''' <returns>Returns Boolean</returns>
    Public Function GetAccessToken(ByVal type As Integer, ByVal panelParam As Panel) As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamWriter As StreamWriter = Nothing

        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim oauthURL As String
            oauthURL = String.Empty & Me.endPoint & "/oauth/token"
            Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(oauthURL)
            accessTokenRequest.Method = "POST"

            Dim oauthParameters As String = String.Empty
            If type = 1 Then
                ' Client Credential flow
                oauthParameters = "client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=PAYMENT"
            Else
                ' Refresh Token flow
                oauthParameters = "client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=refresh_token" & "&refresh_token=" & Me.refreshToken
            End If

            accessTokenRequest.ContentType = "application/x-www-form-urlencoded"

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
                Me.accessToken = deserializedJsonObj.access_token
                Me.expirySeconds = deserializedJsonObj.expires_in
                Me.refreshToken = deserializedJsonObj.refresh_token

                Me.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(Me.expirySeconds)).ToLongDateString() & " " & currentServerTime.AddSeconds(Convert.ToDouble(Me.expirySeconds)).ToLongTimeString()

                Dim refreshExpiry As DateTime = currentServerTime.AddHours(Me.refreshTokenExpiresIn)

                If deserializedJsonObj.expires_in.Equals("0") Then
                    Dim defaultAccessTokenExpiresIn As Integer = 100
                    ' In Years
                    Me.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongDateString() & " " & currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongTimeString()
                End If

                Me.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() & " " & refreshExpiry.ToLongTimeString()

                fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write)
                streamWriter = New StreamWriter(fileStream)
                streamWriter.WriteLine(Me.accessToken)
                streamWriter.WriteLine(Me.expirySeconds)
                streamWriter.WriteLine(Me.refreshToken)
                streamWriter.WriteLine(Me.accessTokenExpiryTime)
                streamWriter.WriteLine(Me.refreshTokenExpiryTime)

                streamWriter.Close()
                fileStream.Close()

                accessTokenResponseStream.Close()
                Return True
            End Using
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Me.DrawPanelForFailure(panelParam, New StreamReader(stream).ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(panelParam, ex.ToString())
        Finally
            If streamWriter IsNot Nothing Then
                streamWriter.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

        Return False
    End Function

    ''' <summary>
    ''' Method to add row to refund section.
    ''' </summary>
    ''' <param name="transaction">Transaction as String</param>
    ''' <param name="merchant">Merchant as string</param>
    Public Sub AddRowToRefundSection(ByVal transaction As String, ByVal merchant As String)
        Dim rowOne As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Left
        cellOne.CssClass = "cell"
        cellOne.Width = Unit.Pixel(150)
        '''/ cellOne.Text = transaction.ToString();
        Dim rbutton As New RadioButton()
        rbutton.Text = transaction.ToString()
        rbutton.GroupName = "RefundSection"
        rbutton.ID = transaction.ToString()
        cellOne.Controls.Add(rbutton)
        rowOne.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.CssClass = "cell"
        cellTwo.Width = Unit.Pixel(100)
        rowOne.Controls.Add(cellTwo)

        Dim cellThree As New TableCell()
        cellThree.CssClass = "cell"
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Width = Unit.Pixel(240)
        cellThree.Text = merchant.ToString()
        rowOne.Controls.Add(cellThree)

        Dim cellFour As New TableCell()
        cellFour.CssClass = "cell"
        rowOne.Controls.Add(cellFour)

        refundTable.Controls.Add(rowOne)
    End Sub

    ''' <summary>
    ''' Method to draw refund section
    ''' </summary>
    ''' <param name="onlyRow">Row details</param>
    Public Sub DrawRefundSection(ByVal onlyRow As Boolean)
        Try
            If onlyRow = False Then
                Dim headingRow As New TableRow()
                Dim headingCellOne As New TableCell()
                headingCellOne.HorizontalAlign = HorizontalAlign.Left
                headingCellOne.CssClass = "cell"
                headingCellOne.Width = Unit.Pixel(200)
                headingCellOne.Font.Bold = True
                headingCellOne.Text = "Transaction ID"
                headingRow.Controls.Add(headingCellOne)
                Dim headingCellTwo As New TableCell()
                headingCellTwo.CssClass = "cell"
                headingCellTwo.Width = Unit.Pixel(100)
                headingRow.Controls.Add(headingCellTwo)
                Dim headingCellThree As New TableCell()
                headingCellThree.CssClass = "cell"
                headingCellThree.HorizontalAlign = HorizontalAlign.Left
                headingCellThree.Width = Unit.Pixel(240)
                headingCellThree.Font.Bold = True
                headingCellThree.Text = "Merchant Transaction ID"
                headingRow.Controls.Add(headingCellThree)
                Dim headingCellFour As New TableCell()
                headingCellFour.CssClass = "warning"
                Dim warningMessage As New LiteralControl("<b>WARNING:</b><br/>You must use Get Transaction Status to get the Transaction ID before you can refund it.")
                headingCellFour.Controls.Add(warningMessage)
                headingRow.Controls.Add(headingCellFour)
                refundTable.Controls.Add(headingRow)
            End If

            Me.ResetRefundList()
            Me.GetRefundListFromFile()

            Dim tempCountToDisplay As Integer = 1
            While (tempCountToDisplay <= Me.refundCountToDisplay) AndAlso (tempCountToDisplay <= Me.refundList.Count) AndAlso (Me.refundList.Count > 0)
                Me.AddRowToRefundSection(Me.refundList(tempCountToDisplay - 1).Key, Me.refundList(tempCountToDisplay - 1).Value)
                tempCountToDisplay += 1

                '''/ addButtonToRefundSection("Refund Transaction");
            End While
        Catch ex As Exception
            Me.DrawPanelForFailure(newTransactionPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Method to update refund list to file.
    ''' </summary>
    Public Sub UpdateRefundListToFile()
        If Me.refundList.Count <> 0 Then
            Me.refundList.Reverse(0, Me.refundList.Count)
        End If

        Using sr As StreamWriter = File.CreateText(Request.MapPath(Me.refundFile))
            Dim tempCount As Integer = 0
            While tempCount < Me.refundList.Count
                Dim lineToWrite As String = Me.refundList(tempCount).Key & ":-:" & Me.refundList(tempCount).Value
                sr.WriteLine(lineToWrite)
                tempCount += 1
            End While

            sr.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Method to reset refund list
    ''' </summary>
    Public Sub ResetRefundList()
        Me.refundList.RemoveRange(0, Me.refundList.Count)
    End Sub

    ''' <summary>
    ''' Method to check item in refund file.
    ''' </summary>
    ''' <param name="transactionid">Transaction Id</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id</param>
    ''' <returns>Return Boolean</returns>
    Public Function CheckItemInRefundFile(ByVal transactionid As String, ByVal merchantTransactionId As String) As Boolean
        Dim line As String
        Dim lineToFind As String = transactionid & ":-:" & merchantTransactionId
        Dim file As New System.IO.StreamReader(Request.MapPath(Me.refundFile))
        While (InlineAssignHelper(line, file.ReadLine())) IsNot Nothing
            If line.CompareTo(lineToFind) = 0 Then
                file.Close()
                Return True
            End If
        End While

        file.Close()
        Return False
    End Function

    ''' <summary>
    ''' Method to write refund to file.
    ''' </summary>
    ''' <param name="transactionid">Transaction Id</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id</param>
    Public Sub WriteRefundToFile(ByVal transactionid As String, ByVal merchantTransactionId As String)
        '''/ Read the refund file for the list of transactions and store locally
        '''/ FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        '''/ StreamWriter sr = new StreamWriter(file);
        '''/ DateTime junkTime = DateTime.UtcNow;
        '''/ string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        Using appendContent As StreamWriter = File.AppendText(Request.MapPath(Me.refundFile))
            Dim line As String = transactionid & ":-:" & merchantTransactionId
            appendContent.WriteLine(line)
            appendContent.Flush()
            '''/ file.Close();
            appendContent.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Method to get refung list from file.
    ''' </summary>
    Public Sub GetRefundListFromFile()
        '''/ Read the refund file for the list of transactions and store locally
        Dim file As New FileStream(Request.MapPath(Me.refundFile), FileMode.Open, FileAccess.Read)
        Dim sr As New StreamReader(file)
        Dim line As String

        While (InlineAssignHelper(line, sr.ReadLine())) IsNot Nothing
            Dim refundKeys As String() = Regex.Split(line, ":-:")
            If refundKeys(0) IsNot Nothing AndAlso refundKeys(1) IsNot Nothing Then
                Me.refundList.Add(New KeyValuePair(Of String, String)(refundKeys(0), refundKeys(1)))
            End If
        End While

        sr.Close()
        file.Close()
        Me.refundList.Reverse(0, Me.refundList.Count)
    End Sub

    ''' <summary>
    ''' This function is used to read access token file and validate the access token
    ''' this function returns true if access token is valid, or else false is returned
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <returns>Retunr Boolean</returns>
    Public Function ReadAndGetAccessToken(ByVal panelParam As Panel) As Boolean
        Dim result As Boolean = True
        If Me.ReadAccessTokenFile() = False Then
            result = Me.GetAccessToken(1, panelParam)
        Else
            Dim tokenValidity As String = Me.IsTokenValid()
            If tokenValidity.CompareTo("REFRESH_TOKEN") = 0 Then
                result = Me.GetAccessToken(2, panelParam)
            ElseIf String.Compare(Me.IsTokenValid(), "INVALID_ACCESS_TOKEN") = 0 Then
                result = Me.GetAccessToken(1, panelParam)
            End If
        End If

        Return result
    End Function

    ''' <summary>
    ''' Page Load method
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
        transactionSuccessTable.Visible = False
        tranGetStatusTable.Visible = False
        refundSuccessTable.Visible = False
        Dim currentServerTime As DateTime = DateTime.UtcNow
        serverTimeLabel.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
        '''/ Convert.ToString(Session["merTranId"]);
        Dim ableToReadFromConfig As Boolean = Me.ReadConfigFile()

        If ableToReadFromConfig = False Then
            Return
        End If

        If (Request("ret_signed_payload") IsNot Nothing) AndAlso (Request("ret_signature") IsNot Nothing) Then
            Me.signedPayload = Request("ret_signed_payload").ToString()
            Me.signedSignature = Request("ret_signature").ToString()
            Session("signedPayLoad") = Me.signedPayload.ToString()
            Session("signedSignature") = Me.signedSignature.ToString()
            Me.ProcessNotaryResponse()
        ElseIf (Request("TransactionAuthCode") IsNot Nothing) AndAlso (Session("merTranId") IsNot Nothing) Then
            Me.ProcessCreateTransactionResponse()
        ElseIf (Request("shown_notary") IsNot Nothing) AndAlso (Session("processNotary") IsNot Nothing) Then
            Session("processNotary") = Nothing
            GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " & Session("tempMerTranId").ToString()
            GetTransactionAuthCode.Text = "Auth Code: " & Session("TranAuthCode").ToString()
        End If

        refundTable.Controls.Clear()
        Me.DrawRefundSection(False)
        Me.DrawNotificationTableHeaders()
        Me.GetNotificationDetails()
        Return
    End Sub

    ''' <summary>
    ''' Reads from config file
    ''' </summary>
    ''' <returns>true/false; true if able to read else false</returns>
    Private Function ReadConfigFile() As Boolean
        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(newTransactionPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.endPoint = ConfigurationManager.AppSettings("endPoint")
        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(newTransactionPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(newTransactionPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "~\PayApp1AccessToken.txt"
        End If

        Me.refundFile = ConfigurationManager.AppSettings("refundFile")
        If String.IsNullOrEmpty(Me.refundFile) Then
            Me.refundFile = "~\refund.txt"
        End If

        Me.refundCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("refundCountToDisplay"))
        If String.IsNullOrEmpty(Convert.ToString(Me.refundCountToDisplay)) Then
            Me.refundCountToDisplay = 5
        End If

        ' this.noOfNotificationsToDisplay = ConfigurationManager.AppSettings["noOfNotificationsToDisplay"];
        If String.IsNullOrEmpty(ConfigurationManager.AppSettings("noOfNotificationsToDisplay")) Then
            Me.noOfNotificationsToDisplay = 5
        Else
            noOfNotificationsToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("noOfNotificationsToDisplay"))
        End If

        Me.notificationDetailsFile = ConfigurationManager.AppSettings("notificationDetailsFile")
        If String.IsNullOrEmpty(Me.notificationDetailsFile) Then
            Me.notificationDetailsFile = "~\notificationDetailsFile.txt"
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "PAYMENT"
        End If

        If ConfigurationManager.AppSettings("DisableLatestFive") IsNot Nothing Then
            Me.latestFive = False
        End If

        Me.notaryURL = ConfigurationManager.AppSettings("notaryURL")
        If String.IsNullOrEmpty(Me.notaryURL) Then
            Me.DrawPanelForFailure(newTransactionPanel, "notaryURL is not defined in configuration file")
            Return False
        End If


        If ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl") Is Nothing Then
            Me.DrawPanelForFailure(newTransactionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file")
            Return False


        End If

        Me.merchantRedirectURI = New Uri(ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl"))

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        Return True
    End Function

    ''' <summary>
    ''' New Transaction event
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub NewTransactionButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Me.ReadTransactionParametersFromConfigurationFile()
        Dim payLoadString As String = "{""Amount"":" & Me.amount.ToString() & ",""Category"":" & Me.category.ToString() & ",""Channel"":""" & Me.channel.ToString() & """,""Description"":""" & Me.description.ToString() & """,""MerchantTransactionId"":""" & Me.merchantTransactionId.ToString() & """,""MerchantProductId"":""" & Me.merchantProductId.ToString() & """,""MerchantPaymentRedirectUrl"":""" & Me.merchantRedirectURI.ToString() & """}"
        Session("payloadData") = payLoadString.ToString()
        Response.Redirect(Me.notaryURL.ToString() & "?request_to_sign=" & payLoadString.ToString() & "&goBackURL=" & Me.merchantRedirectURI.ToString() & "&api_key=" & Me.apiKey.ToString() & "&secret_key=" & Me.secretKey.ToString())
    End Sub

    ''' <summary>
    ''' Event to get transaction.
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub GetTransactionButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Dim keyValue As String = String.Empty
            Dim resourcePathString As String = String.Empty
            If Radio_TransactionStatus.SelectedIndex = 0 Then
                keyValue = GetTransactionMerchantTransID.Text.ToString().Replace("Merchant Transaction ID: ", String.Empty)
                If keyValue.Length = 0 Then
                    Return
                End If

                resourcePathString = String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Transactions/MerchantTransactionId/" & keyValue.ToString()
            End If

            If Radio_TransactionStatus.SelectedIndex = 1 Then
                keyValue = GetTransactionAuthCode.Text.ToString().Replace("Auth Code: ", String.Empty)
                If keyValue.Length = 0 Then
                    Return
                End If

                resourcePathString = String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Transactions/TransactionAuthCode/" & keyValue.ToString()
            End If

            If Radio_TransactionStatus.SelectedIndex = 2 Then
                keyValue = GetTransactionTransID.Text.ToString().Replace("Transaction ID: ", String.Empty)
                If keyValue.Length = 0 Then
                    Return
                End If

                resourcePathString = String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Transactions/TransactionId/" & keyValue.ToString()
            End If

            If Me.ReadAndGetAccessToken(newTransactionStatusPanel) = True Then
                If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
                    Return
                End If

                '''/ resourcePathString = resourcePathString + "?access_token=" + this.access_token.ToString();
                '''/ HttpWebRequest objRequest = (HttpWebRequest) System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + Session["TranAuthCode"].ToString() + "?access_token=" + access_token.ToString());
                Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(resourcePathString), HttpWebRequest)
                objRequest.Method = "GET"
                objRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                Dim getTransactionStatusResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)
                Using getTransactionStatusResponseStream As New StreamReader(getTransactionStatusResponseObject.GetResponseStream())
                    Dim getTransactionStatusResponseData As String = getTransactionStatusResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As TransactionResponse = DirectCast(deserializeJsonObject.Deserialize(getTransactionStatusResponseData, GetType(TransactionResponse)), TransactionResponse)
                    GetTransactionTransID.Text = "Transaction ID: " & deserializedJsonObj.TransactionId.ToString()
                    'lblstatusTranId.Text = deserializedJsonObj.TransactionId.ToString()
                    'lblstatusMerTranId.Text = deserializedJsonObj.MerchantTransactionId.ToString()
                    'DrawPanelForFailure(newTransactionStatusPanel, getTransactionStatusResponseData);
                    If Me.CheckItemInRefundFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString()) = False Then
                        Me.WriteRefundToFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString())
                    End If

                    refundTable.Controls.Clear()
                    Me.DrawRefundSection(False)
                    tranGetStatusTable.Visible = True
                    Me.DrawPanelForGetTransactionSuccess(newTransactionStatusPanel)
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Amount", deserializedJsonObj.Amount.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Channel ", deserializedJsonObj.Channel.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Description", deserializedJsonObj.Description.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantId", deserializedJsonObj.MerchantId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantTransactionId", deserializedJsonObj.MerchantTransactionId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "OriginalTransactionId", deserializedJsonObj.OriginalTransactionId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionId", deserializedJsonObj.TransactionId.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionType", deserializedJsonObj.TransactionType.ToString())
                    Me.AddRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Version", deserializedJsonObj.Version.ToString())
                    getTransactionStatusResponseStream.Close()
                End Using
            End If
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Me.DrawPanelForFailure(newTransactionStatusPanel, New StreamReader(stream).ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(newTransactionStatusPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Method to be triggered on Get Notification button click
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnGetNotification_Click(ByVal sender As Object, ByVal e As EventArgs)
        Me.notificationDetailsTable.Controls.Clear()
        Me.DrawNotificationTableHeaders()
        Me.GetNotificationDetails()
    End Sub

    ''' <summary>
    ''' Event to view notary
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnViewNotary_Click(ByVal sender As Object, ByVal e As EventArgs)
        If (Session("payloadData") IsNot Nothing) AndAlso (Session("signedPayLoad") IsNot Nothing) AndAlso (Session("signedSignature") IsNot Nothing) Then
            Session("processNotary") = "notary"
            Response.Redirect(Me.notaryURL.ToString() & "?signed_payload=" & Session("signedPayLoad").ToString() & "&goBackURL=" & Me.merchantRedirectURI.ToString() & "&signed_signature=" & Session("signedSignature").ToString() & "&signed_request=" & Session("payloadData").ToString())
        End If
    End Sub

    ''' <summary>
    ''' Event for refund transaction
    ''' </summary>
    ''' <param name="sender">Sender Information</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnRefundTransaction_Click1(ByVal sender As Object, ByVal e As EventArgs)
        Dim transactionToRefund As String = String.Empty
        Dim recordFound As Boolean = False
        Dim strReq As String = "{""TransactionOperationStatus"":""Refunded"",""RefundReasonCode"":1,""RefundReasonText"":""Customer was not happy""}"
        ' string strReq = "{\"RefundReasonCode\":1,\"RefundReasonText\":\"Customer was not happy\"}";
        Dim dataLength As String = String.Empty
        Try
            If Me.refundList.Count > 0 Then
                For Each refundTableRow As Control In refundTable.Controls
                    If TypeOf refundTableRow Is TableRow Then
                        For Each refundTableRowCell As Control In refundTableRow.Controls
                            If TypeOf refundTableRowCell Is TableCell Then
                                For Each refundTableCellControl As Control In refundTableRowCell.Controls
                                    If TypeOf refundTableCellControl Is RadioButton Then
                                        If DirectCast(refundTableCellControl, RadioButton).Checked Then
                                            transactionToRefund = DirectCast(refundTableCellControl, RadioButton).Text.ToString()
                                            '''/ refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
                                            recordFound = True
                                            Exit For
                                        End If
                                    End If
                                Next
                            End If
                        Next
                    End If
                Next

                If recordFound = True Then
                    If Me.ReadAndGetAccessToken(refundPanel) = True Then
                        If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
                            Return
                        End If
                        '''/ String getTransactionStatusResponseData;
                        '''/ WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.FQDN + "/rest/3/Commerce/Payment/Transactions/" + transactionToRefund.ToString() + "?access_token=" + this.access_token.ToString() + "&Action=refund");
                        'WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create(string.Empty + this.endPoint + "/rest/3/Commerce/Payment/Transactions/" + transactionToRefund.ToString() + "?Action=refund");
                        Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Transactions/" & transactionToRefund.ToString()), WebRequest)
                        objRequest.Method = "PUT"
                        objRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                        objRequest.ContentType = "application/json"
                        Dim encoding As New UTF8Encoding()
                        Dim postBytes As Byte() = encoding.GetBytes(strReq)
                        objRequest.ContentLength = postBytes.Length
                        Dim postStream As Stream = objRequest.GetRequestStream()
                        postStream.Write(postBytes, 0, postBytes.Length)
                        dataLength = postBytes.Length.ToString()
                        postStream.Close()
                        Dim refundTransactionResponeObject As WebResponse = DirectCast(objRequest.GetResponse(), WebResponse)
                        Using refundResponseStream As New StreamReader(refundTransactionResponeObject.GetResponseStream())
                            Dim refundTransactionResponseData As String = refundResponseStream.ReadToEnd()
                            Dim deserializeJsonObject As New JavaScriptSerializer()
                            Dim deserializedJsonObj As RefundResponse = DirectCast(deserializeJsonObject.Deserialize(refundTransactionResponseData, GetType(RefundResponse)), RefundResponse)
                            'lbRefundTranID.Text = deserializedJsonObj.TransactionId.ToString()
                            refundSuccessTable.Visible = True
                            DrawPanelForRefundSuccess(refundPanel)
                            AddRowToRefundSuccessPanel(refundPanel, "CommitConfirmationId", deserializedJsonObj.CommitConfirmationId)
                            AddRowToRefundSuccessPanel(refundPanel, "IsSuccess", deserializedJsonObj.IsSuccess)
                            AddRowToRefundSuccessPanel(refundPanel, "OriginalPurchaseAmount", deserializedJsonObj.OriginalPurchaseAmount)
                            AddRowToRefundSuccessPanel(refundPanel, "TransactionId", deserializedJsonObj.TransactionId)
                            AddRowToRefundSuccessPanel(refundPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus)
                            AddRowToRefundSuccessPanel(refundPanel, "Version", deserializedJsonObj.Version)
                            refundResponseStream.Close()
                            If Me.latestFive = False Then
                                Me.refundList.RemoveAll(Function(x) x.Key.Equals(transactionToRefund))
                                Me.UpdateRefundListToFile()
                                Me.ResetRefundList()
                                refundTable.Controls.Clear()
                                Me.DrawRefundSection(False)
                                GetTransactionMerchantTransID.Text = "Merchant Transaction ID: "
                                GetTransactionAuthCode.Text = "Auth Code: "
                                GetTransactionTransID.Text = "Transaction ID: "
                            End If
                        End Using
                    End If
                End If
            End If
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Me.DrawPanelForFailure(refundPanel, New StreamReader(stream).ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            '''/ + strReq + transactionToRefund.ToString() + dataLength
            Me.DrawPanelForFailure(refundPanel, ex.ToString() & strReq & transactionToRefund.ToString() & dataLength)
        End Try
    End Sub

    ''' <summary>
    ''' Method to read transaction parameters from configuration file.
    ''' </summary>
    Private Sub ReadTransactionParametersFromConfigurationFile()
        Me.transactionTime = DateTime.UtcNow
        Me.transactionTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", Me.transactionTime)
        If Radio_TransactionProductType.SelectedIndex = 0 Then
            Me.amount = "0.99"
        ElseIf Radio_TransactionProductType.SelectedIndex = 1 Then
            Me.amount = "2.99"
        End If

        Session("tranType") = Radio_TransactionProductType.SelectedIndex.ToString()
        If ConfigurationManager.AppSettings("Category") Is Nothing Then
            Me.DrawPanelForFailure(newTransactionPanel, "Category is not defined in configuration file")
            Return
        End If

        Me.category = Convert.ToInt32(ConfigurationManager.AppSettings("Category"))
        If ConfigurationManager.AppSettings("Channel") Is Nothing Then
            Me.channel = "MOBILE_WEB"
        Else
            Me.channel = ConfigurationManager.AppSettings("Channel")
        End If

        Me.description = "TrDesc" & Me.transactionTimeString
        Me.merchantTransactionId = "TrId" & Me.transactionTimeString
        Session("merTranId") = Me.merchantTransactionId.ToString()
        Me.merchantProductId = "ProdId" & Me.transactionTimeString
        Me.merchantApplicationId = "MerAppId" & Me.transactionTimeString
    End Sub

    ''' <summary>
    ''' Method to process notary response
    ''' </summary>
    Private Sub ProcessNotaryResponse()
        If Session("tranType") IsNot Nothing Then
            Radio_TransactionProductType.SelectedIndex = Convert.ToInt32(Session("tranType").ToString())
            Session("tranType") = Nothing
        End If

        Response.Redirect(Me.endPoint & "/rest/3/Commerce/Payment/Transactions?clientid=" & Me.apiKey.ToString() & "&SignedPaymentDetail=" & Me.signedPayload.ToString() & "&Signature=" & Me.signedSignature.ToString())
    End Sub

    ''' <summary>
    ''' Method to get notification details
    ''' </summary>
    Private Sub GetNotificationDetails()
        Dim notificationDetailsStream As StreamReader = Nothing
        Dim notificationDetail As String = String.Empty
        If Not File.Exists(Request.MapPath(Me.notificationDetailsFile)) Then
            Return
        End If
        Try
            notificationDetailsStream = File.OpenText(Request.MapPath(Me.notificationDetailsFile))
            notificationDetail = notificationDetailsStream.ReadToEnd()
            notificationDetailsStream.Close()
            Dim notificationDetailArray As String() = notificationDetail.Split("$"c)
            Dim noOfNotifications As Integer = 0
            If notificationDetailArray IsNot Nothing Then
                noOfNotifications = notificationDetailArray.Length - 1
            End If
            Dim count As Integer = 0

            While noOfNotifications >= 0
                Dim notificationDetails As String() = notificationDetailArray(noOfNotifications).Split(":"c)
                If count <= noOfNotificationsToDisplay Then
                    If notificationDetails.Length = 3 Then
                        Me.AddRowToNotificationTable(notificationDetails(0), notificationDetails(1), notificationDetails(2))
                    End If
                Else
                    Exit While
                End If
                count += 1
                noOfNotifications -= 1
            End While
        Catch ex As Exception
            Me.DrawPanelForFailure(notificationPanel, ex.ToString())
        Finally
            If notificationDetailsStream IsNot Nothing Then
                notificationDetailsStream.Close()
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Method to add rows to notification response table with notification details
    ''' </summary>
    ''' <param name="notificationId">Notification Id</param>
    ''' <param name="notificationType">Notification Type</param>
    ''' <param name="transactionId">Transaction Id</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id</param>
    Private Sub AddRowToNotificationTable(ByVal notificationId As String, ByVal notificationType As String, ByVal transactionId As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Left
        cellOne.Text = notificationId
        cellOne.Width = Unit.Pixel(300)
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Width = Unit.Pixel(50)
        row.Controls.Add(cellTwo)

        Dim cellThree As New TableCell()
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Text = notificationType
        cellThree.Width = Unit.Pixel(300)
        row.Controls.Add(cellThree)
        Dim cellFour As New TableCell()
        cellFour.Width = Unit.Pixel(50)
        row.Controls.Add(cellFour)

        Dim cellFive As New TableCell()
        cellFive.HorizontalAlign = HorizontalAlign.Left
        cellFive.Text = transactionId
        cellFive.Width = Unit.Pixel(300)
        row.Controls.Add(cellFive)
        Dim cellSix As New TableCell()
        cellSix.Width = Unit.Pixel(50)
        row.Controls.Add(cellSix)

        Me.notificationDetailsTable.Controls.Add(row)
        notificationPanel.Controls.Add(Me.notificationDetailsTable)
    End Sub

    ''' <summary>
    ''' Method to display notification response table with headers
    ''' </summary>
    Private Sub DrawNotificationTableHeaders()
        Me.notificationDetailsTable = New Table()
        Me.notificationDetailsTable.Font.Name = "Sans-serif"
        Me.notificationDetailsTable.Font.Size = 8
        Me.notificationDetailsTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Left
        rowOneCellOne.Text = "Notification ID"
        rowOneCellOne.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellOne)
        Dim rowOneCellTwo As New TableCell()
        rowOneCellTwo.Width = Unit.Pixel(50)
        rowOne.Controls.Add(rowOneCellTwo)

        Dim rowOneCellThree As New TableCell()
        rowOneCellThree.Font.Bold = True
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left
        rowOneCellThree.Text = "Notification Type"
        rowOneCellThree.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellThree)
        Me.notificationDetailsTable.Controls.Add(rowOne)
        Dim rowOneCellFour As New TableCell()
        rowOneCellFour.Width = Unit.Pixel(50)
        rowOne.Controls.Add(rowOneCellFour)

        Dim rowOneCellFive As New TableCell()
        rowOneCellFive.Font.Bold = True
        rowOneCellFive.HorizontalAlign = HorizontalAlign.Left
        rowOneCellFive.Text = "Transaction ID"
        rowOneCellFive.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellFive)
        Me.notificationDetailsTable.Controls.Add(rowOne)
        Dim rowOneCellSix As New TableCell()
        rowOneCellSix.Width = Unit.Pixel(50)
        rowOne.Controls.Add(rowOneCellSix)
        Me.notificationDetailsTable.Controls.Add(rowOne)

        notificationPanel.Controls.Add(Me.notificationDetailsTable)
    End Sub

    ''' <summary>
    ''' Method to draw the success table
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    Private Sub DrawPanelForSuccess(ByVal panelParam As Panel)
        Me.successTable = New Table()
        Me.successTable.Font.Name = "Sans-serif"
        Me.successTable.Font.Size = 8
        Me.successTable.BorderStyle = BorderStyle.Outset
        Me.successTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        Me.successTable.Controls.Add(rowOne)
        Me.successTable.BorderWidth = 2
        Me.successTable.BorderColor = Color.DarkGreen
        Me.successTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        panelParam.Controls.Add(Me.successTable)
    End Sub

    ''' <summary>
    ''' Method to add row to the success table
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attribute as string</param>
    ''' <param name="value">value as string</param>
    Private Sub AddRowToSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.Text = attribute.ToString()
        cellOne.Font.Bold = True
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Text = value.ToString()
        row.Controls.Add(cellTwo)
        Me.successTable.Controls.Add(row)
    End Sub

    ''' <summary>
    ''' Method to draws error table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="message">Message as string</param>
    Private Sub DrawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        Me.failureTable = New Table()
        Me.failureTable.Font.Name = "Sans-serif"
        Me.failureTable.Font.Size = 8
        Me.failureTable.BorderStyle = BorderStyle.Outset
        Me.failureTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        Me.failureTable.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        Me.failureTable.Controls.Add(rowTwo)
        Me.failureTable.BorderWidth = 2
        Me.failureTable.BorderColor = Color.Red
        Me.failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(Me.failureTable)
    End Sub

    ''' <summary>
    ''' Method to draw panel for refund success
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    Private Sub DrawPanelForRefundSuccess(ByVal panelParam As Panel)
        Me.successTableRefund = New Table()
        Me.successTableRefund.Font.Name = "Sans-serif"
        Me.successTableRefund.Font.Size = 8
        Me.successTableRefund.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Right
        rowOneCellOne.Text = "Parameter"
        rowOneCellOne.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellOne)
        Dim rowOneCellTwo As New TableCell()
        rowOneCellTwo.Width = Unit.Pixel(50)
        rowOne.Controls.Add(rowOneCellTwo)

        Dim rowOneCellThree As New TableCell()
        rowOneCellThree.Font.Bold = True
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left
        rowOneCellThree.Text = "Value"
        rowOneCellThree.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellThree)
        Me.successTableRefund.Controls.Add(rowOne)
        panelParam.Controls.Add(Me.successTableRefund)
    End Sub

    ''' <summary>
    ''' This function adds row to the refund success table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attribute as string</param>
    ''' <param name="value">Value as string</param>
    Private Sub AddRowToRefundSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.Text = attribute.ToString()
        cellOne.Width = Unit.Pixel(300)
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Width = Unit.Pixel(50)
        row.Controls.Add(cellTwo)
        Dim cellThree As New TableCell()
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Text = value.ToString()
        cellThree.Width = Unit.Pixel(300)
        row.Controls.Add(cellThree)
        Me.successTableRefund.Controls.Add(row)
    End Sub

    ''' <summary>
    ''' Method to draw panel for successful transaction
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    Private Sub DrawPanelForGetTransactionSuccess(ByVal panelParam As Panel)
        Me.successTableGetTransaction = New Table()
        Me.successTableGetTransaction.Font.Name = "Sans-serif"
        Me.successTableGetTransaction.Font.Size = 8
        Me.successTableGetTransaction.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Right
        rowOneCellOne.Text = "Parameter"
        rowOneCellOne.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellOne)
        Dim rowOneCellTwo As New TableCell()
        rowOneCellTwo.Width = Unit.Pixel(50)
        rowOne.Controls.Add(rowOneCellTwo)

        Dim rowOneCellThree As New TableCell()
        rowOneCellThree.Font.Bold = True
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left
        rowOneCellThree.Text = "Value"
        rowOneCellThree.Width = Unit.Pixel(300)
        rowOne.Controls.Add(rowOneCellThree)
        Me.successTableGetTransaction.Controls.Add(rowOne)
        panelParam.Controls.Add(Me.successTableGetTransaction)
    End Sub

    ''' <summary>
    ''' This function adds row to the success table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attribute as string</param>
    ''' <param name="value">Value as string</param>
    Private Sub AddRowToGetTransactionSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.Text = attribute.ToString()
        cellOne.Width = Unit.Pixel(300)
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Width = Unit.Pixel(50)
        row.Controls.Add(cellTwo)
        Dim cellThree As New TableCell()
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Text = value.ToString()
        cellThree.Width = Unit.Pixel(300)
        row.Controls.Add(cellThree)
        Me.successTableGetTransaction.Controls.Add(row)
    End Sub

    ''' <summary>
    ''' Method to clear refund table.
    ''' </summary>
    Private Sub ClearRefundTable()
        For Each refundTableRow As Control In refundTable.Controls
            refundTable.Controls.Remove(refundTableRow)
        Next
    End Sub

    ''' <summary>
    ''' This class defines access token response
    ''' </summary>
    Public Class AccessTokenResponse
        ''' <summary>
        ''' Gets or sets Access Token
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
        ''' Gets or sets Refresh Token
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
        ''' Gets or sets Expires In
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
    ''' This class defines refund response
    ''' </summary>
    Public Class RefundResponse
        ''' <summary>
        ''' Gets or sets Transaction Id
        ''' </summary>
        Public Property TransactionId() As String
            Get
                Return m_TransactionId
            End Get
            Set(ByVal value As String)
                m_TransactionId = Value
            End Set
        End Property
        Private m_TransactionId As String

        ''' <summary>
        ''' Gets or sets Transaction Status
        ''' </summary>
        Public Property TransactionStatus() As String
            Get
                Return m_TransactionStatus
            End Get
            Set(ByVal value As String)
                m_TransactionStatus = Value
            End Set
        End Property
        Private m_TransactionStatus As String

        ''' <summary>
        ''' Gets or sets Is Success
        ''' </summary>
        Public Property IsSuccess() As String
            Get
                Return m_IsSuccess
            End Get
            Set(ByVal value As String)
                m_IsSuccess = Value
            End Set
        End Property
        Private m_IsSuccess As String

        ''' <summary>
        ''' Gets or sets Version
        ''' </summary>
        Public Property Version() As String
            Get
                Return m_Version
            End Get
            Set(ByVal value As String)
                m_Version = Value
            End Set
        End Property
        Private m_Version As String

        ''' <summary>
        ''' Gets or sets Version
        ''' </summary>
        Public Property OriginalPurchaseAmount() As String
            Get
                Return m_OriginalPurchaseAmount
            End Get
            Set(ByVal value As String)
                m_OriginalPurchaseAmount = Value
            End Set
        End Property
        Private m_OriginalPurchaseAmount As String

        ''' <summary>
        ''' Gets or sets Version
        ''' </summary>
        Public Property CommitConfirmationId() As String
            Get
                Return m_CommitConfirmationId
            End Get
            Set(ByVal value As String)
                m_CommitConfirmationId = Value
            End Set
        End Property
        Private m_CommitConfirmationId As String
    End Class

    ''' <summary>
    ''' This class defines transaction response
    ''' </summary>
    Public Class TransactionResponse
        ''' <summary>
        ''' Gets or sets Channel
        ''' </summary>
        Public Property Channel() As String
            Get
                Return m_Channel
            End Get
            Set(ByVal value As String)
                m_Channel = Value
            End Set
        End Property
        Private m_Channel As String

        ''' <summary>
        ''' Gets or sets Description
        ''' </summary>
        Public Property Description() As String
            Get
                Return m_Description
            End Get
            Set(ByVal value As String)
                m_Description = Value
            End Set
        End Property
        Private m_Description As String

        ''' <summary>
        ''' Gets or sets Currency
        ''' </summary>
        Public Property Currency() As String
            Get
                Return m_Currency
            End Get
            Set(ByVal value As String)
                m_Currency = Value
            End Set
        End Property
        Private m_Currency As String

        ''' <summary>
        ''' Gets or sets Transaction Type
        ''' </summary>
        Public Property TransactionType() As String
            Get
                Return m_TransactionType
            End Get
            Set(ByVal value As String)
                m_TransactionType = Value
            End Set
        End Property
        Private m_TransactionType As String

        ''' <summary>
        ''' Gets or sets Transaction Status
        ''' </summary>
        Public Property TransactionStatus() As String
            Get
                Return m_TransactionStatus
            End Get
            Set(ByVal value As String)
                m_TransactionStatus = Value
            End Set
        End Property
        Private m_TransactionStatus As String

        ''' <summary>
        ''' Gets or sets Transaction Consumer Id
        ''' </summary>
        Public Property ConsumerId() As String
            Get
                Return m_ConsumerId
            End Get
            Set(ByVal value As String)
                m_ConsumerId = Value
            End Set
        End Property
        Private m_ConsumerId As String

        ''' <summary>
        ''' Gets or sets Merchant Transaction Id
        ''' </summary>
        Public Property MerchantTransactionId() As String
            Get
                Return m_MerchantTransactionId
            End Get
            Set(ByVal value As String)
                m_MerchantTransactionId = Value
            End Set
        End Property
        Private m_MerchantTransactionId As String

        ''' <summary>
        ''' Gets or sets Merchant Application Id
        ''' </summary>
        Public Property MerchantApplicationId() As String
            Get
                Return m_MerchantApplicationId
            End Get
            Set(ByVal value As String)
                m_MerchantApplicationId = Value
            End Set
        End Property
        Private m_MerchantApplicationId As String

        ''' <summary>
        ''' Gets or sets Transaction Id
        ''' </summary>
        Public Property TransactionId() As String
            Get
                Return m_TransactionId
            End Get
            Set(ByVal value As String)
                m_TransactionId = Value
            End Set
        End Property
        Private m_TransactionId As String

        ''' <summary>
        ''' Gets or sets Content Category
        ''' </summary>
        Public Property ContentCategory() As String
            Get
                Return m_ContentCategory
            End Get
            Set(ByVal value As String)
                m_ContentCategory = Value
            End Set
        End Property
        Private m_ContentCategory As String

        ''' <summary>
        ''' Gets or sets Merchant Product Id
        ''' </summary>
        Public Property MerchantProductId() As String
            Get
                Return m_MerchantProductId
            End Get
            Set(ByVal value As String)
                m_MerchantProductId = Value
            End Set
        End Property
        Private m_MerchantProductId As String

        ''' <summary>
        ''' Gets or sets Merchant Identifier
        ''' </summary>
        Public Property MerchantId() As String
            Get
                Return m_MerchantId
            End Get
            Set(ByVal value As String)
                m_MerchantId = Value
            End Set
        End Property
        Private m_MerchantId As String

        ''' <summary>
        ''' Gets or sets Amount
        ''' </summary>
        Public Property Amount() As String
            Get
                Return m_Amount
            End Get
            Set(ByVal value As String)
                m_Amount = Value
            End Set
        End Property
        Private m_Amount As String

        ''' <summary>
        ''' Gets or sets Version
        ''' </summary>
        Public Property Version() As String
            Get
                Return m_Version
            End Get
            Set(ByVal value As String)
                m_Version = Value
            End Set
        End Property
        Private m_Version As String

        ''' <summary>
        ''' Gets or sets Is Success
        ''' </summary>
        Public Property IsSuccess() As String
            Get
                Return m_IsSuccess
            End Get
            Set(ByVal value As String)
                m_IsSuccess = Value
            End Set
        End Property
        Private m_IsSuccess As String

        ''' <summary>
        ''' Gets or sets Is Success
        ''' </summary>
        Public Property OriginalTransactionId() As String
            Get
                Return m_OriginalTransactionId
            End Get
            Set(ByVal value As String)
                m_OriginalTransactionId = Value
            End Set
        End Property
        Private m_OriginalTransactionId As String
    End Class
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
        target = value
        Return value
    End Function
End Class
