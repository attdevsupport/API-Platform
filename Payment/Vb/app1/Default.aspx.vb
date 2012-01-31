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
Imports System.Text.RegularExpressions

Partial Public Class _Default
    Inherits System.Web.UI.Page
    Private shortCode As String, accessTokenFilePath As String, FQDN As String, oauthFlow As String, refundFile As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private successTable As Table, failureTable As Table
    Private successTableGetTransaction As Table, failureTableGetTransaction As Table
    Private amount As String
    Private category As Int32
    Private channel As String, description As String, merchantTransactionId As String, merchantProductId As String, merchantApplicationId As String
    Private merchantRedirectURI As Uri
    Private MerchantSubscriptionIdList As String, SubscriptionRecurringPeriod As String
    Private SubscriptionRecurringNumber As Int32, SubscriptionRecurringPeriodAmount As Int32
    Private IsPurchaseOnNoActiveSubscription As String
    Private transactionTime As DateTime
    Private transactionTimeString As String
    Private payLoadStringFromRequest As String
    Private signedPayload As String, signedSignature As String
    Private notaryURL As String
    ' Button refundButton;
    Private refundCountToDisplay As Integer = 0
    Private refundList As New List(Of KeyValuePair(Of String, String))()
    Private LatestFive As Boolean = True
    Protected Sub Page_Load(sender As Object, e As EventArgs)
        transactionSuccessTable.Visible = False
        tranGetStatusTable.Visible = False
        refundSuccessTable.Visible = False
        Dim currentServerTime As DateTime = DateTime.UtcNow
        serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
        'refundButton = new Button();
        'refundButton.Click += new EventHandler(refundButtonClick);
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
        If ConfigurationManager.AppSettings("refundFile") IsNot Nothing Then
            refundFile = ConfigurationManager.AppSettings("refundFile")
        Else
            refundFile = "~\refundFile.txt"
        End If

        If ConfigurationManager.AppSettings("refundCountToDisplay") IsNot Nothing Then
            refundCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("refundCountToDisplay"))
        Else
            refundCountToDisplay = 5
        End If

        If ConfigurationManager.AppSettings("scope") Is Nothing Then
            scope = "PAYMENT"
        Else
            scope = ConfigurationManager.AppSettings("scope").ToString()
        End If
        If ConfigurationManager.AppSettings("DisableLatestFive") IsNot Nothing Then
            LatestFive = False
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
            signedPayload = Request("ret_signed_payload").ToString()
            signedSignature = Request("ret_signature").ToString()
            Session("signedPayLoad") = signedPayload.ToString()
            Session("signedSignature") = signedSignature.ToString()
            processNotaryResponse()
        ElseIf (Request("TransactionAuthCode") IsNot Nothing) AndAlso (Session("merTranId") IsNot Nothing) Then
            processCreateTransactionResponse()
        ElseIf (Request("shown_notary") IsNot Nothing) AndAlso (Session("processNotary") IsNot Nothing) Then
            Session("processNotary") = Nothing
            GetTransactionMerchantTransID.Text = "Merchant Transaction ID: " & Session("tempMerTranId").ToString()
            GetTransactionAuthCode.Text = "Auth Code: " & Session("TranAuthCode").ToString()
        End If
        refundTable.Controls.Clear()
        drawRefundSection(False)
        Return
    End Sub
    Private Sub readTransactionParametersFromConfigurationFile()
        transactionTime = DateTime.UtcNow
        transactionTimeString = [String].Format("{0:dddMMMddyyyyHHmmss}", transactionTime)
        If Radio_TransactionProductType.SelectedIndex = 0 Then
            amount = "0.99"
        ElseIf Radio_TransactionProductType.SelectedIndex = 1 Then
            amount = "2.99"
        End If
        Session("tranType") = Radio_TransactionProductType.SelectedIndex.ToString()
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
        Session("merTranId") = merchantTransactionId.ToString()
        merchantProductId = "ProdId" & transactionTimeString
        merchantApplicationId = "MerAppId" & transactionTimeString
    End Sub


    Private Sub processNotaryResponse()
        If Session("tranType") IsNot Nothing Then
            Radio_TransactionProductType.SelectedIndex = Convert.ToInt32(Session("tranType").ToString())
            Session("tranType") = Nothing
        End If
        Response.Redirect(FQDN & "/Commerce/Payment/Rest/2/Transactions?clientid=" & api_key.ToString() & "&SignedPaymentDetail=" & signedPayload.ToString() & "&Signature=" & signedSignature.ToString())

    End Sub

    Public Sub processCreateTransactionResponse()
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
    '"\",\"MerchantApplicationId\":\"" + merchantApplicationId.ToString() +
    Protected Sub newTransactionButton_Click(sender As Object, e As EventArgs)
        readTransactionParametersFromConfigurationFile()
        Dim payLoadString As String = "{""Amount"":" & amount.ToString() & ",""Category"":" & category.ToString() & ",""Channel"":""" & channel.ToString() & """,""Description"":""" & description.ToString() & """,""MerchantTransactionId"":""" & merchantTransactionId.ToString() & """,""MerchantProductId"":""" & merchantProductId.ToString() & """,""MerchantPaymentRedirectUrl"":""" & merchantRedirectURI.ToString() & """}"
        Session("payloadData") = payLoadString.ToString()
        'string returnURL = "https://wincode-api-att.com/BF_R2_Production_Csharp_Apps/payment/app1/Default.aspx";
        Response.Redirect(notaryURL.ToString() & "?request_to_sign=" & payLoadString.ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&api_key=" & api_key.ToString() & "&secret_key=" & secret_key.ToString())
    End Sub
    ' This function draws the success table 

    Private Sub drawPanelForSuccess(panelParam As Panel)
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

    Private Sub addRowToSuccessPanel(panelParam As Panel, attribute As String, value As String)
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

    Private Sub drawPanelForFailure(panelParam As Panel, message As String)
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
    Protected Sub getTransactionButton_Click(sender As Object, e As EventArgs)
        Try
            Dim keyValue As String = ""
            Dim resourcePathString As String = ""
            If Radio_TransactionStatus.SelectedIndex = 0 Then
                keyValue = GetTransactionMerchantTransID.Text.ToString().Replace("Merchant Transaction ID: ", "")
                If keyValue.Length = 0 Then
                    'writeRefundToFile("a", "kdsfaksdf;akfjf");
                    'Response.Redirect(Request.Url.ToString());
                    Return
                End If
                resourcePathString = "" & FQDN & "/Commerce/Payment/Rest/2/Transactions/MerchantTransactionId/" & keyValue.ToString()
            End If
            If Radio_TransactionStatus.SelectedIndex = 1 Then
                keyValue = GetTransactionAuthCode.Text.ToString().Replace("Auth Code: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
                resourcePathString = "" & FQDN & "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" & keyValue.ToString()
            End If
            If Radio_TransactionStatus.SelectedIndex = 2 Then
                keyValue = GetTransactionTransID.Text.ToString().Replace("Transaction ID: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
                resourcePathString = "" & FQDN & "/Commerce/Payment/Rest/2/Transactions/TransactionId/" & keyValue.ToString()
            End If
            If readAndGetAccessToken(newTransactionStatusPanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    Return
                End If
                'String getTransactionStatusResponseData;
                resourcePathString = resourcePathString & "?access_token=" & access_token.ToString()
                'HttpWebRequest objRequest = (HttpWebRequest) System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/TransactionAuthCode/" + Session["TranAuthCode"].ToString() + "?access_token=" + access_token.ToString());
                Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(resourcePathString), HttpWebRequest)
                objRequest.Method = "GET"
                Dim getTransactionStatusResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)
                Using getTransactionStatusResponseStream As New StreamReader(getTransactionStatusResponseObject.GetResponseStream())
                    Dim getTransactionStatusResponseData As [String] = getTransactionStatusResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As transactionResponse = DirectCast(deserializeJsonObject.Deserialize(getTransactionStatusResponseData, GetType(transactionResponse)), transactionResponse)
                    GetTransactionTransID.Text = "Transaction ID: " & deserializedJsonObj.TransactionId.ToString()
                    lblstatusTranId.Text = deserializedJsonObj.TransactionId.ToString()
                    lblstatusMerTranId.Text = deserializedJsonObj.MerchantTransactionId.ToString()
                    'drawPanelForFailure(newTransactionStatusPanel, getTransactionStatusResponseData);
                    If checkItemInRefundFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString()) = False Then
                        'clearRefundTable();
                        writeRefundToFile(deserializedJsonObj.TransactionId.ToString(), deserializedJsonObj.MerchantTransactionId.ToString())
                    End If
                    refundTable.Controls.Clear()
                    drawRefundSection(False)
                    tranGetStatusTable.Visible = True
                    drawPanelForGetTransactionSuccess(newTransactionStatusPanel)
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Amount", deserializedJsonObj.Amount.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Channel ", deserializedJsonObj.Channel.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Description", deserializedJsonObj.Description.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantIdentifier", deserializedJsonObj.MerchantIdentifier.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "TransactionType", deserializedJsonObj.TransactionType.ToString())
                    addRowToGetTransactionSuccessPanel(newTransactionStatusPanel, "Version", deserializedJsonObj.Version.ToString())
                    getTransactionStatusResponseStream.Close()
                End Using
            End If
        Catch ex As Exception
            drawPanelForFailure(newTransactionStatusPanel, ex.ToString())
        End Try

    End Sub
    Private Sub drawPanelForGetTransactionSuccess(panelParam As Panel)
        successTableGetTransaction = New Table()
        successTableGetTransaction.Font.Name = "Sans-serif"
        successTableGetTransaction.Font.Size = 8
        successTableGetTransaction.Width = Unit.Pixel(650)
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
        successTableGetTransaction.Controls.Add(rowOne)
        panelParam.Controls.Add(successTableGetTransaction)
    End Sub
    'This function adds row to the success table 

    Private Sub addRowToGetTransactionSuccessPanel(panelParam As Panel, attribute As String, value As String)
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
        successTableGetTransaction.Controls.Add(row)
    End Sub

    Protected Sub Unnamed1_Click(sender As Object, e As EventArgs)
        If (Session("payloadData") IsNot Nothing) AndAlso (Session("signedPayLoad") IsNot Nothing) AndAlso (Session("signedSignature") IsNot Nothing) Then
            Session("processNotary") = "notary"
            Response.Redirect(notaryURL.ToString() & "?signed_payload=" & Session("signedPayLoad").ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&signed_signature=" & Session("signedSignature").ToString() & "&signed_request=" & Session("payloadData").ToString())
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

    Public Function getAccessToken(type As Integer, panelParam As Panel) As Boolean
        '  This is client credential flow: 

        If type = 1 Then
            Try
                Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
                Dim OauthURL As String
                OauthURL = "" & FQDN & "/oauth/token"
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(OauthURL)
                accessTokenRequest.Method = "POST"
                Dim oauthParameters As String = "client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&grant_type=client_credentials&scope=PAYMENT"
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
                Dim OauthURL As String
                OauthURL = "" & FQDN & "/oauth/token"
                Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(OauthURL)
                accessTokenRequest.Method = "POST"
                Dim oauthParameters As String = "client_id=" & api_key.ToString() & "&client_secret=" & secret_key.ToString() & "&grant_type=refresh_token" & "&refresh_token=" + refresh_token.ToString()
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
    '
    '    public void addButtonToRefundSection(string caption)
    '    {
    '        TableRow rowOne = new TableRow();
    '        TableCell cellOne = new TableCell();
    '        cellOne.HorizontalAlign = HorizontalAlign.Right;
    '        cellOne.CssClass = "cell";
    '        cellOne.Width = Unit.Pixel(150);
    '        rowOne.Controls.Add(cellOne);
    '
    '        TableCell CellTwo = new TableCell();
    '        CellTwo.CssClass = "cell";
    '        CellTwo.Width = Unit.Pixel(100);
    '        rowOne.Controls.Add(CellTwo);
    '
    '        TableCell CellThree = new TableCell();
    '        CellThree.CssClass = "cell";
    '        CellThree.HorizontalAlign = HorizontalAlign.Left;
    '        CellThree.Width = Unit.Pixel(240);
    '        rowOne.Controls.Add(CellThree);
    '
    '        TableCell CellFour = new TableCell();
    '        CellFour.CssClass = "cell";
    '        refundButton.Text = caption.ToString();
    '        CellFour.Controls.Add(refundButton);
    '        rowOne.Controls.Add(CellFour);
    '
    '        refundTable.Controls.Add(rowOne);
    '    }
    '     

    Public Sub addRowToRefundSection(transaction As String, merchant As String)
        Dim rowOne As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.CssClass = "cell"
        cellOne.Width = Unit.Pixel(150)
        'cellOne.Text = transaction.ToString();
        Dim rbutton As New RadioButton()
        rbutton.Text = transaction.ToString()
        rbutton.GroupName = "RefundSection"
        rbutton.ID = transaction.ToString()
        cellOne.Controls.Add(rbutton)
        rowOne.Controls.Add(cellOne)
        Dim CellTwo As New TableCell()
        CellTwo.CssClass = "cell"
        CellTwo.Width = Unit.Pixel(100)
        rowOne.Controls.Add(CellTwo)

        Dim CellThree As New TableCell()
        CellThree.CssClass = "cell"
        CellThree.HorizontalAlign = HorizontalAlign.Left
        CellThree.Width = Unit.Pixel(240)
        CellThree.Text = merchant.ToString()
        rowOne.Controls.Add(CellThree)

        Dim CellFour As New TableCell()
        CellFour.CssClass = "cell"
        rowOne.Controls.Add(CellFour)

        refundTable.Controls.Add(rowOne)
    End Sub
    Public Sub drawRefundSection(onlyRow As Boolean)
        Try
            If onlyRow = False Then
                Dim headingRow As New TableRow()
                Dim headingCellOne As New TableCell()
                headingCellOne.HorizontalAlign = HorizontalAlign.Right
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
            resetRefundList()
            getRefundListFromFile()

            Dim tempCountToDisplay As Integer = 1
            While (tempCountToDisplay <= refundCountToDisplay) AndAlso (tempCountToDisplay <= refundList.Count) AndAlso (refundList.Count > 0)
                addRowToRefundSection(refundList(tempCountToDisplay - 1).Key, refundList(tempCountToDisplay - 1).Value)
                tempCountToDisplay += 1
                'addButtonToRefundSection("Refund Transaction");
            End While
        Catch ex As Exception
            drawPanelForFailure(newTransactionPanel, ex.ToString())
        End Try
    End Sub


    Public Sub updateRefundListToFile()
        If refundList.Count <> 0 Then
            refundList.Reverse(0, refundList.Count)
        End If
        Using sr As StreamWriter = File.CreateText(Request.MapPath(refundFile))
            Dim tempCount As Integer = 0
            While tempCount < refundList.Count
                Dim lineToWrite As String = refundList(tempCount).Key & ":-:" & refundList(tempCount).Value
                sr.WriteLine(lineToWrite)
                tempCount += 1
            End While
            sr.Close()
        End Using
    End Sub

    Public Sub resetRefundList()
        refundList.RemoveRange(0, refundList.Count)
    End Sub

    Public Function checkItemInRefundFile(transactionid As String, merchantTransactionId As String) As Boolean
        Dim line As String
        Dim lineToFind As String = transactionid & ":-:" & merchantTransactionId
        Dim file As New System.IO.StreamReader(Request.MapPath(refundFile))
        While (InlineAssignHelper(line, file.ReadLine())) IsNot Nothing
            If line.CompareTo(lineToFind) = 0 Then
                file.Close()
                Return True
            End If
        End While
        file.Close()
        Return False
    End Function

    Public Sub writeRefundToFile(transactionid As String, merchantTransactionId As String)
        ' Read the refund file for the list of transactions and store locally 

        'FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        'StreamWriter sr = new StreamWriter(file);
        'DateTime junkTime = DateTime.UtcNow;
        'string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        Using appendContent As StreamWriter = File.AppendText(Request.MapPath(refundFile))
            Dim line As String = transactionid & ":-:" & merchantTransactionId
            appendContent.WriteLine(line)
            appendContent.Flush()
            'file.Close();
            appendContent.Close()
        End Using
    End Sub

    Public Sub getRefundListFromFile()
        ' Read the refund file for the list of transactions and store locally 

        Dim file As New FileStream(Request.MapPath(refundFile), FileMode.Open, FileAccess.Read)
        Dim sr As New StreamReader(file)
        Dim line As String

        While ((InlineAssignHelper(line, sr.ReadLine())) IsNot Nothing)
            Dim refundKeys As String() = Regex.Split(line, ":-:")
            If refundKeys(0) IsNot Nothing AndAlso refundKeys(1) IsNot Nothing Then
                refundList.Add(New KeyValuePair(Of String, String)(refundKeys(0), refundKeys(1)))
            End If
        End While
        sr.Close()
        file.Close()
        refundList.Reverse(0, refundList.Count)
    End Sub

    Private Sub clearRefundTable()
        For Each refundTableRow As Control In refundTable.Controls
            refundTable.Controls.Remove(refundTableRow)
        Next
    End Sub
    '
    '    void processRefundButtonClick()
    '    {
    '        if (refundList.Count > 0)
    '        {
    '            foreach (Control refundTableRow in refundTable.Controls)
    '            {
    '                if (refundTableRow is TableRow)
    '                {
    '                    foreach (Control refundTableRowCell in refundTableRow.Controls)
    '                    {
    '                        if (refundTableRowCell is TableCell)
    '                        {
    '                            foreach (Control refundTableCellControl in refundTableRowCell.Controls)
    '                            {
    '                                if ((refundTableCellControl is RadioButton))
    '                                {
    '
    '                                    if (((RadioButton)refundTableCellControl).Checked)
    '                                    {
    '                                        string transactionToRefund = ((RadioButton)refundTableCellControl).Text.ToString();
    '                                        refundList.RemoveAll(x => x.Key.Equals(Session[transactionToRefund].ToString()));
    '                                        break;
    '                                    }
    '                                }
    '                            }
    '                        }
    '                    }
    '                }
    '            }
    '            updateRefundListToFile();
    '            resetRefundList();
    '            refundTable.Controls.Clear();
    '            drawRefundSection(false);
    '        }
    '    }
    '
    '    protected void refundButtonClick(object sender, EventArgs e)
    '    {
    '        if (refundList.Count > 0)
    '        {
    '            foreach (Control refundTableRow in refundTable.Controls)
    '            {
    '                if (refundTableRow is TableRow)
    '                {
    '                    foreach (Control refundTableRowCell in refundTableRow.Controls)
    '                    {
    '                        if (refundTableRowCell is TableCell)
    '                        {
    '                            foreach (Control refundTableCellControl in refundTableRowCell.Controls)
    '                            {
    '                                if ((refundTableCellControl is RadioButton))
    '                                {
    '
    '                                    if (((RadioButton)refundTableCellControl).Checked)
    '                                    {
    '                                        string transactionToRefund = ((RadioButton)refundTableCellControl).Text.ToString();
    '                                        refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
    '                                        break;
    '                                    }
    '                                }
    '                            }
    '                        }
    '                    }
    '                }
    '            }
    '            updateRefundListToFile();
    '            resetRefundList();
    '            refundTable.Controls.Clear();
    '            drawRefundSection(false);
    '        }
    '    }
    '     

    ' This function is used to read access token file and validate the access token
    '     * this function returns true if access token is valid, or else false is returned
    '     

    Public Function readAndGetAccessToken(panelParam As Panel) As Boolean
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
    Public Class AccessTokenResponse
        Public access_token As String
        Public refresh_token As String
        Public expires_in As String
    End Class
    Public Class RefundResponse
        Public Property TransactionId() As String
            Get
                Return m_TransactionId
            End Get
            Set(value As String)
                m_TransactionId = Value
            End Set
        End Property
        Private m_TransactionId As String
        Public Property TransactionStatus() As String
            Get
                Return m_TransactionStatus
            End Get
            Set(value As String)
                m_TransactionStatus = Value
            End Set
        End Property
        Private m_TransactionStatus As String
        Public Property IsSuccess() As String
            Get
                Return m_IsSuccess
            End Get
            Set(value As String)
                m_IsSuccess = Value
            End Set
        End Property
        Private m_IsSuccess As String
        Public Property Version() As String
            Get
                Return m_Version
            End Get
            Set(value As String)
                m_Version = Value
            End Set
        End Property
        Private m_Version As String
    End Class
    Public Class transactionResponse
        Public Property Channel() As String
            Get
                Return m_Channel
            End Get
            Set(value As String)
                m_Channel = Value
            End Set
        End Property
        Private m_Channel As String
        Public Property Description() As String
            Get
                Return m_Description
            End Get
            Set(value As String)
                m_Description = Value
            End Set
        End Property
        Private m_Description As String
        Public Property Currency() As String
            Get
                Return m_Currency
            End Get
            Set(value As String)
                m_Currency = Value
            End Set
        End Property
        Private m_Currency As String
        Public Property TransactionType() As String
            Get
                Return m_TransactionType
            End Get
            Set(value As String)
                m_TransactionType = Value
            End Set
        End Property
        Private m_TransactionType As String
        Public Property TransactionStatus() As String
            Get
                Return m_TransactionStatus
            End Get
            Set(value As String)
                m_TransactionStatus = Value
            End Set
        End Property
        Private m_TransactionStatus As String
        Public Property ConsumerId() As String
            Get
                Return m_ConsumerId
            End Get
            Set(value As String)
                m_ConsumerId = Value
            End Set
        End Property
        Private m_ConsumerId As String
        Public Property MerchantTransactionId() As String
            Get
                Return m_MerchantTransactionId
            End Get
            Set(value As String)
                m_MerchantTransactionId = Value
            End Set
        End Property
        Private m_MerchantTransactionId As String
        Public Property MerchantApplicationId() As String
            Get
                Return m_MerchantApplicationId
            End Get
            Set(value As String)
                m_MerchantApplicationId = Value
            End Set
        End Property
        Private m_MerchantApplicationId As String
        Public Property TransactionId() As String
            Get
                Return m_TransactionId
            End Get
            Set(value As String)
                m_TransactionId = Value
            End Set
        End Property
        Private m_TransactionId As String
        Public Property ContentCategory() As String
            Get
                Return m_ContentCategory
            End Get
            Set(value As String)
                m_ContentCategory = Value
            End Set
        End Property
        Private m_ContentCategory As String
        Public Property MerchantProductId() As String
            Get
                Return m_MerchantProductId
            End Get
            Set(value As String)
                m_MerchantProductId = Value
            End Set
        End Property
        Private m_MerchantProductId As String
        Public Property MerchantIdentifier() As String
            Get
                Return m_MerchantIdentifier
            End Get
            Set(value As String)
                m_MerchantIdentifier = Value
            End Set
        End Property
        Private m_MerchantIdentifier As String
        Public Property Amount() As String
            Get
                Return m_Amount
            End Get
            Set(value As String)
                m_Amount = Value
            End Set
        End Property
        Private m_Amount As String
        Public Property Version() As String
            Get
                Return m_Version
            End Get
            Set(value As String)
                m_Version = Value
            End Set
        End Property
        Private m_Version As String
        Public Property IsSuccess() As String
            Get
                Return m_IsSuccess
            End Get
            Set(value As String)
                m_IsSuccess = Value
            End Set
        End Property
        Private m_IsSuccess As String
    End Class
    Protected Sub Unnamed1_Click1(sender As Object, e As EventArgs)
        Dim transactionToRefund As String = ""
        Dim recordFound As Boolean = False
        Dim strReq As String = "{""RefundReasonCode"":1,""RefundReasonText"":""Customer was not happy""}"
        Dim dataLength As String = ""
        Try
            If refundList.Count > 0 Then
                For Each refundTableRow As Control In refundTable.Controls
                    If TypeOf refundTableRow Is TableRow Then
                        For Each refundTableRowCell As Control In refundTableRow.Controls
                            If TypeOf refundTableRowCell Is TableCell Then
                                For Each refundTableCellControl As Control In refundTableRowCell.Controls
                                    If (TypeOf refundTableCellControl Is RadioButton) Then

                                        If DirectCast(refundTableCellControl, RadioButton).Checked Then
                                            transactionToRefund = DirectCast(refundTableCellControl, RadioButton).Text.ToString()
                                            'refundList.RemoveAll(x => x.Key.Equals(transactionToRefund));
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
                    If readAndGetAccessToken(refundPanel) = True Then
                        If access_token Is Nothing OrElse access_token.Length <= 0 Then
                            Return
                        End If
                        'String getTransactionStatusResponseData;
                        Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/Commerce/Payment/Rest/2/Transactions/" & transactionToRefund.ToString() & "?access_token=" & access_token.ToString() & "&Action=refund"), WebRequest)
                        objRequest.Method = "PUT"
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
                            Dim refundTransactionResponseData As [String] = refundResponseStream.ReadToEnd()
                            'drawPanelForFailure(refundPanel, refundTransactionResponseData);
                            '{ "TransactionId":"6216898841002136", "TransactionStatus":"SUCCESSFUL", "IsSuccess":true, "Version":"1" }
                            Dim deserializeJsonObject As New JavaScriptSerializer()
                            Dim deserializedJsonObj As RefundResponse = DirectCast(deserializeJsonObject.Deserialize(refundTransactionResponseData, GetType(RefundResponse)), RefundResponse)
                            lbRefundTranID.Text = deserializedJsonObj.TransactionId.ToString()
                            lbRefundTranStatus.Text = deserializedJsonObj.TransactionStatus.ToString()
                            lbRefundIsSuccess.Text = deserializedJsonObj.IsSuccess.ToString()
                            lbRefundVersion.Text = deserializedJsonObj.Version.ToString()
                            'lbRefundTranID.Text = transactionToRefund.ToString();
                            ' lbRefundTranStatus.Text = "SUCCESSFUL";
                            'lbRefundIsSuccess.Text = "true";
                            ' lbRefundVersion.Text = "1";
                            refundSuccessTable.Visible = True
                            refundResponseStream.Close()
                            If LatestFive = False Then
                                refundList.RemoveAll(Function(x) x.Key.Equals(transactionToRefund))
                                updateRefundListToFile()
                                resetRefundList()
                                refundTable.Controls.Clear()
                                drawRefundSection(False)
                                GetTransactionMerchantTransID.Text = "Merchant Transaction ID: "
                                GetTransactionAuthCode.Text = "Auth Code: "
                                GetTransactionTransID.Text = "Transaction ID: "
                            End If
                        End Using

                    End If
                End If
            End If
        Catch ex As Exception
            ' + strReq + transactionToRefund.ToString() + dataLength
            drawPanelForFailure(refundPanel, ex.ToString())
        End Try
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class

'
'use Request.InputStream:
'byte[] buffer = new byte[1024];
'int c;
'while ((c = Request.InputStream.Read(buffer, 0, buffer.Length)) 0)
'{
'    drawPanelForFailure(refundPanel,buffer.tostring());
'}
'
'
