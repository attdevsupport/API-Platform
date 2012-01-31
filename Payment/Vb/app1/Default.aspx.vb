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

Partial Public Class _Default
    Inherits System.Web.UI.Page
    Private shortCode As String, accessTokenFilePath As String, FQDN As String, oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private successTable As Table, failureTable As Table
    Private successTableGetTransaction As Table, failureTableGetTransaction As Table
    Private amount As String
    Private category As Int32
    Private channel As String, description As String, merchantTransactionId As String, merchantProductId As String, merchantApplicationId As String
    Private merchantRedirectURI As Uri
    Private paymentType As String
    Private MerchantSubscriptionIdList As String, SubscriptionRecurringPeriod As String
    Private SubscriptionRecurringNumber As Int32, SubscriptionRecurringPeriodAmount As Int32
    Private IsPurchaseOnNoActiveSubscription As String
    Private transactionTime As DateTime
    Private transactionTimeString As String
    Private payLoadStringFromRequest As String
    Private vb_signedPayLoad As String, vb_signedSignature As String
    Private notaryURL As String
    '  This function is called when application is getting loaded 
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        transactionSuccessTable.Visible = False
        Dim currentServerTime As DateTime = DateTime.UtcNow
        serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
        FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
        If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
            drawPanelForFailure(newTransactionPanel, "FQDN is not defined in configuration file")
            Return
        End If
        FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
        If ConfigurationManager.AppSettings("api_key") Is Nothing Then
            drawPanelForFailure(newTransactionPanel, "api_key is not defined in configuration file")
            Return
        End If
        api_key = ConfigurationManager.AppSettings("api_key").ToString()
        If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
            drawPanelForFailure(newTransactionPanel, "secret_key is not defined in configuration file")
            Return
        End If
        secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
        If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
            accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        Else
            accessTokenFilePath = "~\PayApp1AccessToken.txt"
        End If
        If ConfigurationManager.AppSettings("scope") Is Nothing Then
            scope = "PAYMENT"
        Else
            scope = ConfigurationManager.AppSettings("scope").ToString()
        End If
        If ConfigurationManager.AppSettings("notaryURL") Is Nothing Then
            drawPanelForFailure(newTransactionPanel, "notaryURL is not defined in configuration file")
            Return
        End If
        If ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl") Is Nothing Then
            drawPanelForFailure(newTransactionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file")
            Return
        End If
        merchantRedirectURI = New Uri(ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl"))
        notaryURL = ConfigurationManager.AppSettings("notaryURL")
        If (Request("ret_signed_payload") IsNot Nothing) AndAlso (Request("ret_signature") IsNot Nothing) Then
            vb_signedPayLoad = Request("ret_signed_payload").ToString()
            vb_signedSignature = Request("ret_signature").ToString()
            Session("vb_signedPayLoad") = vb_signedPayLoad.ToString()
            Session("vb_signedSignature") = vb_signedSignature.ToString()
            processNotaryResponse()
        ElseIf (Request("TransactionAuthCode") IsNot Nothing) AndAlso (Session("vb_merTranId") IsNot Nothing) Then
            processCreateTransactionResponse()
        ElseIf (Request("shown_notary") IsNot Nothing) AndAlso (Session("vb_processNotary") IsNot Nothing) Then
            Session("vb_processNotary") = Nothing
            GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " & Session("vb_tempMerTranId").ToString()
            GetTransactionAuthCode.Text = "Auth Code: " & Session("vb_TranAuthCode").ToString()
        End If
        Return
    End Sub
    'This function reads transaction parameter values from configuration file
    Private Sub readTransactionParametersFromConfigurationFile()
        transactionTime = DateTime.UtcNow
        transactionTimeString = [String].Format("{0:dddMMMddyyyyHHmmss}", transactionTime)
        '
        '        if (ConfigurationManager.AppSettings["Amount"] == null)
        '        {
        '            drawPanelForFailure(newTransactionPanel, "Amount is not defined in configuration file");
        '            return;
        '        }
        '        amount = ConfigurationManager.AppSettings["Amount"];
        '        

        If Radio_TransactionProductType.SelectedIndex = 0 Then
            amount = "0.99"
        ElseIf Radio_TransactionProductType.SelectedIndex = 1 Then
            amount = "2.99"
        End If
        Session("vb_tranType") = Radio_TransactionProductType.SelectedIndex.ToString()
        If ConfigurationManager.AppSettings("Category") Is Nothing Then
            drawPanelForFailure(newTransactionPanel, "Category is not defined in configuration file")
            Return
        End If
        category = Convert.ToInt32(ConfigurationManager.AppSettings("Category"))
        If ConfigurationManager.AppSettings("Channel") Is Nothing Then
            channel = "MOBILE_WEB"
        Else
            channel = ConfigurationManager.AppSettings("Channel")
        End If
        description = "TrDesc" & transactionTimeString
        merchantTransactionId = "TrId" & transactionTimeString
        Session("vb_merTranId") = merchantTransactionId.ToString()
        merchantProductId = "ProdId" & transactionTimeString
        merchantApplicationId = "MerAppId" & transactionTimeString
    End Sub
    'This function is called after receiving response from notary application

    Private Sub processNotaryResponse()
        If Session("vb_tranType") IsNot Nothing Then
            Radio_TransactionProductType.SelectedIndex = Convert.ToInt32(Session("vb_tranType").ToString())
            Session("vb_tranType") = Nothing
        End If
        Response.Redirect(FQDN & "/Commerce/Payment/Rest/2/Transactions?clientid=" & api_key.ToString() & "&SignedPaymentDetail=" & vb_signedPayLoad.ToString() & "&Signature=" & vb_signedSignature.ToString())

    End Sub
    'This function is called if user clicks on view notary button
    Protected Sub viewNotary_Click(ByVal sender As Object, ByVal e As EventArgs)
        If (Session("vb_payloadData") IsNot Nothing) AndAlso (Session("vb_signedPayLoad") IsNot Nothing) AndAlso (Session("vb_signedSignature") IsNot Nothing) Then
            'Response.Redirect("www.google.com");
            Response.Redirect(notaryURL.ToString() & "?signed_payload=" & Session("vb_payloadData").ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&signed_signature=" & Session("vb_signedSignature").ToString() & "&signed_request=" & Session("vb_signedSignature").ToString())
        End If
        '
        '        if (Session["vb_payloadData"] != null)
        '            addRowToGetTransactionSuccessPanel(newTransactionPanel, "Merchant Transaction ID", "user573transaction1377");
        '        if (Session["vb_signedPayLoad"] != null)
        '            addRowToGetTransactionSuccessPanel(newTransactionPanel, "Transaction Auth Cod", "66574834711");
        '        if (Session["vb_signedSignature"] != null)
        '            addRowToGetTransactionSuccessPanel(newTransactionPanel, "Transaction ID", "trx83587123897598612897");
        '
        '         
    End Sub
    'this function is called to add button to success table
    Private Sub addButtonToSuccessPanel(ByVal panelParam As Panel)
        Dim button As New Button()
        AddHandler button.Click, New EventHandler(AddressOf getTransactionButton_Click)
        button.Text = "View Notary"
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.Text = ""
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Controls.Add(button)
        row.Controls.Add(cellTwo)
        successTable.Controls.Add(row)
        Return
    End Sub
    'this function is called after getting transaction response
    Public Sub processCreateTransactionResponse()
        'drawPanelForSuccess(newTransactionPanel);
        'addRowToSuccessPanel(newTransactionPanel, "Merchant Transaction ID", Session["vb_merTranId"].ToString());
        'addRowToSuccessPanel(newTransactionPanel, "Transaction Auth Cod", Request["TransactionAuthCode"].ToString());
        'addRowToSuccessPanel(newTransactionPanel, "", "");
        'addRowToSuccessPanel(newTransactionPanel, "", "");
        'addButtonToSuccessPanel(newTransactionPanel);
        lbltrancode.Text = Request("TransactionAuthCode").ToString()
        lbltranid.Text = Session("vb_merTranId").ToString()
        transactionSuccessTable.Visible = True
        GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " & Session("vb_merTranId").ToString()
        GetTransactionAuthCode.Text = "Auth Code: " & Request("TransactionAuthCode").ToString()
        GetTransactionTransID.Text = "Transaction ID: "
        Session("vb_tempMerTranId") = Session("vb_merTranId").ToString()
        Session("vb_merTranId") = Nothing
        Session("vb_TranAuthCode") = Request("TransactionAuthCode").ToString()
        Return
    End Sub
    '"\",\"MerchantApplicationId\":\"" + merchantApplicationId.ToString() +
    'this function is called if user clicks on new transaction button
    Protected Sub newTransactionButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        readTransactionParametersFromConfigurationFile()
        Dim payLoadString As String = "{""Amount"":" & amount.ToString() & ",""Category"":" & category.ToString() & ",""Channel"":""" & channel.ToString() & """,""Description"":""" & description.ToString() & """,""MerchantTransactionId"":""" & merchantTransactionId.ToString() & """,""MerchantProductId"":""" & merchantProductId.ToString() & """,""MerchantPaymentRedirectUrl"":""" & merchantRedirectURI.ToString() & """}"
        Session("vb_payloadData") = payLoadString.ToString()
        'string returnURL = "https://wincode-api-att.com/BF_R2_Production_Csharp_Apps/payment/app1/Default.aspx";
        Response.Redirect(notaryURL.ToString() & "?request_to_sign=" & payLoadString.ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&api_key=" & api_key.ToString() & "&secret_key=" & secret_key.ToString())
    End Sub
    ' This function draws the success table 

    Private Sub drawPanelForSuccess(ByVal panelParam As Panel)
        successTable = New Table()
        successTable.Font.Name = "Sans-serif"
        successTable.Font.Size = 8
        successTable.BorderStyle = BorderStyle.Outset
        successTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        successTable.Controls.Add(rowOne)
        successTable.BorderWidth = 2
        successTable.BorderColor = Color.DarkGreen
        successTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        panelParam.Controls.Add(successTable)
    End Sub
    'This function adds row to the success table 

    Private Sub addRowToSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.Text = attribute.ToString()
        cellOne.Font.Bold = True
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Text = value.ToString()
        row.Controls.Add(cellTwo)
        successTable.Controls.Add(row)
    End Sub
    ' This function draws error table 

    Private Sub drawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        failureTable = New Table()
        failureTable.Font.Name = "Sans-serif"
        failureTable.Font.Size = 8
        failureTable.BorderStyle = BorderStyle.Outset
        failureTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        failureTable.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        failureTable.Controls.Add(rowTwo)
        failureTable.BorderWidth = 2
        failureTable.BorderColor = Color.Red
        failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(failureTable)
    End Sub
    'this function is called when user clicks on get transaction status
    Protected Sub getTransactionButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try

            Dim keyValue As String = ""
            If Radio_TransactionStatus.SelectedIndex = 0 Then
                keyValue = GetTransactionMerchantTransID.Text.ToString().Replace("Merchant Transaction ID: ", "")
                If keyValue.Length = 0 Then
                    Return

                End If
            End If
            If Radio_TransactionStatus.SelectedIndex = 1 Then
                keyValue = GetTransactionAuthCode.Text.ToString().Replace("Auth Code: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
            End If
            If Radio_TransactionStatus.SelectedIndex = 2 Then
                keyValue = GetTransactionTransID.Text.ToString().Replace("Transaction ID: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
            End If
            If readAndGetAccessToken(newTransactionStatusPanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    Return
                End If
                Dim getTransactionStatusResponseData As [String]
                Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" & Session("vb_TranAuthCode").ToString() & "?access_token=" & access_token.ToString()), HttpWebRequest)
                objRequest.Method = "GET"
                Dim getTransactionStatusResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)
                Using getTransactionStatusResponseStream As New StreamReader(getTransactionStatusResponseObject.GetResponseStream())
                    getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As transactionResponse = DirectCast(deserializeJsonObject.Deserialize(getTransactionStatusResponseData, GetType(transactionResponse)), transactionResponse)

                    'getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                    drawPanelForGetTransactionSuccess(newTransactionStatusPanel)
                    addRowToSuccessPanel(newTransactionStatusPanel, "Merchant Transaction ID", deserializedJsonObj.MerchantTransactionId)
                    'addRowToSuccessPanel(newTransactionPanel, "Transaction Auth Cod", deserializedJsonObj.);
                    addRowToSuccessPanel(newTransactionStatusPanel, "Transaction ID", deserializedJsonObj.TransactionId)
                    addRowToSuccessPanel(newTransactionStatusPanel, "Amount", deserializedJsonObj.Amount)
                    addRowToSuccessPanel(newTransactionStatusPanel, "Channel ", deserializedJsonObj.Channel)
                    addRowToSuccessPanel(newTransactionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId)
                    addRowToSuccessPanel(newTransactionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory)
                    addRowToSuccessPanel(newTransactionStatusPanel, "Currency", deserializedJsonObj.Currency)
                    addRowToSuccessPanel(newTransactionStatusPanel, "Description", deserializedJsonObj.Description)
                    addRowToSuccessPanel(newTransactionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId)
                    addRowToSuccessPanel(newTransactionStatusPanel, "MerchantIdentifier", deserializedJsonObj.MerchantIdentifier)
                    addRowToSuccessPanel(newTransactionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId)
                    addRowToSuccessPanel(newTransactionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess)
                    addRowToSuccessPanel(newTransactionStatusPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus)
                    getTransactionStatusResponseStream.Close()
                End Using
            End If
        Catch ex As Exception
            drawPanelForFailure(newTransactionStatusPanel, ex.ToString())
        End Try

    End Sub
    'this function draws the success table for get transaction status result
    Private Sub drawPanelForGetTransactionSuccess(ByVal panelParam As Panel)
        successTableGetTransaction = New Table()
        successTableGetTransaction.Font.Name = "Sans-serif"
        successTableGetTransaction.Font.Size = 8
        successTableGetTransaction.BorderStyle = BorderStyle.Outset
        successTableGetTransaction.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        successTableGetTransaction.Controls.Add(rowOne)
        successTableGetTransaction.BorderWidth = 2
        successTableGetTransaction.BorderColor = Color.DarkGreen
        successTableGetTransaction.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc")
        panelParam.Controls.Add(successTableGetTransaction)
    End Sub
    'This function adds row to the success table 

    Private Sub addRowToGetTransactionSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.Text = attribute.ToString()
        cellOne.Font.Bold = True
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Text = value.ToString()
        row.Controls.Add(cellTwo)
        successTableGetTransaction.Controls.Add(row)
    End Sub

    Protected Sub Unnamed1_Click(ByVal sender As Object, ByVal e As EventArgs)
        If (Session("vb_payloadData") IsNot Nothing) AndAlso (Session("vb_signedPayLoad") IsNot Nothing) AndAlso (Session("vb_signedSignature") IsNot Nothing) Then
            Session("vb_processNotary") = "notary"
            Response.Redirect(notaryURL.ToString() & "?signed_payload=" & Session("vb_signedPayLoad").ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&signed_signature=" & Session("vb_signedSignature").ToString() & "&signed_request=" & Session("vb_payloadData").ToString())
        End If
    End Sub

    ' This function reads the Access Token File and stores the values of access token, expiry seconds
    ' * refresh token, last access token time and refresh token expiry time
    ' * This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
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
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create("" & FQDN & "/oauth/access_token?client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&grant_type=client_credentials&scope=PAYMENT")
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
    ' following are the data structures used for the applicaiton
    Public Class AccessTokenResponse
        Public access_token As String
        Public refresh_token As String
        Public expires_in As String
    End Class
    Public Class transactionResponse
        Public Property Channel() As String
            Get
                Return m_Channel
            End Get
            Set(ByVal value As String)
                m_Channel = Value
            End Set
        End Property
        Private m_Channel As String
        Public Property Description() As String
            Get
                Return m_Description
            End Get
            Set(ByVal value As String)
                m_Description = Value
            End Set
        End Property
        Private m_Description As String
        Public Property Currency() As String
            Get
                Return m_Currency
            End Get
            Set(ByVal value As String)
                m_Currency = Value
            End Set
        End Property
        Private m_Currency As String
        Public Property TransactionType() As String
            Get
                Return m_TransactionType
            End Get
            Set(ByVal value As String)
                m_TransactionType = Value
            End Set
        End Property
        Private m_TransactionType As String
        Public Property TransactionStatus() As String
            Get
                Return m_TransactionStatus
            End Get
            Set(ByVal value As String)
                m_TransactionStatus = Value
            End Set
        End Property
        Private m_TransactionStatus As String
        Public Property ConsumerId() As String
            Get
                Return m_ConsumerId
            End Get
            Set(ByVal value As String)
                m_ConsumerId = Value
            End Set
        End Property
        Private m_ConsumerId As String
        Public Property MerchantTransactionId() As String
            Get
                Return m_MerchantTransactionId
            End Get
            Set(ByVal value As String)
                m_MerchantTransactionId = Value
            End Set
        End Property
        Private m_MerchantTransactionId As String
        Public Property MerchantApplicationId() As String
            Get
                Return m_MerchantApplicationId
            End Get
            Set(ByVal value As String)
                m_MerchantApplicationId = Value
            End Set
        End Property
        Private m_MerchantApplicationId As String
        Public Property TransactionId() As String
            Get
                Return m_TransactionId
            End Get
            Set(ByVal value As String)
                m_TransactionId = Value
            End Set
        End Property
        Private m_TransactionId As String
        Public Property ContentCategory() As String
            Get
                Return m_ContentCategory
            End Get
            Set(ByVal value As String)
                m_ContentCategory = Value
            End Set
        End Property
        Private m_ContentCategory As String
        Public Property MerchantProductId() As String
            Get
                Return m_MerchantProductId
            End Get
            Set(ByVal value As String)
                m_MerchantProductId = Value
            End Set
        End Property
        Private m_MerchantProductId As String
        Public Property MerchantIdentifier() As String
            Get
                Return m_MerchantIdentifier
            End Get
            Set(ByVal value As String)
                m_MerchantIdentifier = Value
            End Set
        End Property
        Private m_MerchantIdentifier As String
        Public Property Amount() As String
            Get
                Return m_Amount
            End Get
            Set(ByVal value As String)
                m_Amount = Value
            End Set
        End Property
        Private m_Amount As String
        Public Property Version() As String
            Get
                Return m_Version
            End Get
            Set(ByVal value As String)
                m_Version = Value
            End Set
        End Property
        Private m_Version As String
        Public Property IsSuccess() As String
            Get
                Return m_IsSuccess
            End Get
            Set(ByVal value As String)
                m_IsSuccess = Value
            End Set
        End Property
        Private m_IsSuccess As String
    End Class
    '{ "Channel":"MOBILE_WEB", "":"TrDescSatJan072012131709", "":"USD", "TransactionType":"SINGLEPAY", "TransactionStatus":"SUCCESSFUL", "ConsumerId":"d2ca3d8b-df6b-49b0-a00d-e8b6336e62cb", "MerchantTransactionId":"TrIdSatJan072012131709", "MerchantApplicationId":"e1745ae95cfa72e5150446773be67ed1", "TransactionId":"8499458613902133", "ContentCategory":"1", "MerchantProductId":"ProdIdSatJan072012131709", "MerchantIdentifier":"a75ad884-cf58-4398-ae28-104b619da686", "Amount":"0.99", "Version":"1", "IsSuccess":"true" }
End Class