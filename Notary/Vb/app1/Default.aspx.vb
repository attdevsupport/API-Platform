' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

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
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates

Partial Public Class _Default
    Inherits System.Web.UI.Page
    Private shortCode As String, FQDN As String, oauthFlow As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private successTable As Table, failureTable As Table
    Private amount As String
    Private category As Int32
    Private channel As String, description As String, merchantTransactionId As String, merchantProductId As String, merchantApplicationId As String
    Private merchantRedirectURI As Uri
    Private paymentType As String
    Private signedPayLoad As String, signature As String, goBackURL As String
    Private MerchantSubscriptionIdList As String, SubscriptionRecurringPeriod As String
    Private SubscriptionRecurringNumber As Int32, SubscriptionRecurringPeriodAmount As Int32
    Private IsPurchaseOnNoActiveSubscription As String
    Private transactionTime As DateTime
    Private transactionTimeString As String
    Private payLoadStringFromRequest As String

    Function CertificateValidationCallBack( _
    ByVal sender As Object, _
    ByVal certificate As X509Certificate, _
    ByVal chain As X509Chain, _
    ByVal sslPolicyErrors As SslPolicyErrors _
) As Boolean

        Return True
    End Function

    Private Sub readTransactionParametersFromConfigurationFile()
        transactionTime = DateTime.UtcNow
        transactionTimeString = [String].Format("{0:ddd-MMM-dd-yyyy-HH-mm-ss}", transactionTime)
        If ConfigurationManager.AppSettings("Amount") Is Nothing Then
            drawPanelForFailure(notaryPanel, "Amount is not defined in configuration file")
            Return
        End If
        amount = ConfigurationManager.AppSettings("Amount")
        'requestText.Text = "Amount: " + amount + "\r\n";
        If ConfigurationManager.AppSettings("Category") Is Nothing Then
            drawPanelForFailure(notaryPanel, "Category is not defined in configuration file")
            Return
        End If
        category = Convert.ToInt32(ConfigurationManager.AppSettings("Category"))
        'requestText.Text = requestText.Text + "Category: " + category + "\r\n";
        If ConfigurationManager.AppSettings("Channel") Is Nothing Then
            channel = "MOBILE_WEB"
        Else
            channel = ConfigurationManager.AppSettings("Channel")
        End If
        'requestText.Text = requestText.Text + "Channel: " + channel + "\r\n";
        description = "TrDesc" & transactionTimeString
        'requestText.Text = requestText.Text + "Description: " + description + "\r\n";
        merchantTransactionId = "TrId" & transactionTimeString
        'requestText.Text = requestText.Text + "MerchantTransactionId: " + merchantTransactionId + "\r\n";
        merchantProductId = "ProdId" & transactionTimeString
        'requestText.Text = requestText.Text + "MerchantProductId: " + merchantProductId + "\r\n";
        merchantApplicationId = "MerAppId" & transactionTimeString
        'requestText.Text = requestText.Text + "MerchantApplicationId: " + merchantApplicationId + "\r\n";
        If ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl") Is Nothing Then
            drawPanelForFailure(notaryPanel, "MerchantPaymentRedirectUrl is not defined in configuration file")
            Return
        End If
        merchantRedirectURI = New Uri(ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl"))
        'requestText.Text = requestText.Text + "MerchantPaymentRedirectUrl: " + merchantRedirectURI;
    End Sub
    Private Sub readSubscriptionParametersFromConfigurationFile()
        If ConfigurationManager.AppSettings("MerchantSubscriptionIdList") Is Nothing Then
            MerchantSubscriptionIdList = "merSubIdList" & transactionTimeString
        Else
            MerchantSubscriptionIdList = ConfigurationManager.AppSettings("MerchantSubscriptionIdList")
        End If
        'requestText.Text = requestText.Text + "\r\n" + "MerchantSubscriptionIdList: " + MerchantSubscriptionIdList + "\r\n";
        If ConfigurationManager.AppSettings("SubscriptionRecurringPeriod") Is Nothing Then
            SubscriptionRecurringPeriod = "MONTHLY"
        Else
            SubscriptionRecurringPeriod = ConfigurationManager.AppSettings("SubscriptionRecurringPeriod")
        End If
        'requestText.Text = requestText.Text + "SubscriptionRecurringPeriod: " + SubscriptionRecurringPeriod + "\r\n";
        If ConfigurationManager.AppSettings("SubscriptionRecurringNumber") Is Nothing Then
            SubscriptionRecurringNumber = Convert.ToInt32("9999")
        Else
            SubscriptionRecurringNumber = Convert.ToInt32(ConfigurationManager.AppSettings("SubscriptionRecurringNumber"))
        End If
        'requestText.Text = requestText.Text + "SubscriptionRecurringNumber: " + SubscriptionRecurringNumber + "\r\n";
        If ConfigurationManager.AppSettings("SubscriptionRecurringPeriodAmount") Is Nothing Then
            SubscriptionRecurringPeriodAmount = Convert.ToInt32("1")
        Else
            SubscriptionRecurringPeriodAmount = Convert.ToInt32(ConfigurationManager.AppSettings("SubscriptionRecurringPeriodAmount"))
        End If
        ' requestText.Text = requestText.Text + "SubscriptionRecurringPeriodAmount: " + SubscriptionRecurringPeriodAmount + "\r\n";
        If ConfigurationManager.AppSettings("IsPurchaseOnNoActiveSubscription") Is Nothing Then
            IsPurchaseOnNoActiveSubscription = "false"
        Else
            IsPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings("IsPurchaseOnNoActiveSubscription")
        End If
        'requestText.Text = requestText.Text + "IsPurchaseOnNoActiveSubscription: " + IsPurchaseOnNoActiveSubscription;
    End Sub
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
        Dim currentServerTime As DateTime = DateTime.UtcNow
        serverTimeLabel.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
        FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
        If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
            drawPanelForFailure(notaryPanel, "FQDN is not defined in configuration file")
            Return
        End If
        FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
        If ConfigurationManager.AppSettings("api_key") Is Nothing Then
            drawPanelForFailure(notaryPanel, "api_key is not defined in configuration file")
            Return
        End If
        api_key = ConfigurationManager.AppSettings("api_key").ToString()
        If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
            drawPanelForFailure(notaryPanel, "secret_key is not defined in configuration file")
            Return
        End If
        secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
        If ConfigurationManager.AppSettings("scope") Is Nothing Then
            scope = "PAYMENT"
        Else
            scope = ConfigurationManager.AppSettings("scope").ToString()
        End If
        If (Request("signed_payload") IsNot Nothing) AndAlso (Request("signed_signature") IsNot Nothing) AndAlso (Request("goBackURL") IsNot Nothing) AndAlso (Request("signed_request") IsNot Nothing) Then
            signPayLoadButton.Text = "Back"
            requestText.Text = Request("signed_request").ToString()
            SignedPayLoadTextBox.Text = Request("signed_payload").ToString()
            SignatureTextBox.Text = Request("signed_signature").ToString()
            goBackURL = Request("goBackURL").ToString()
        Else
            If (Request("request_to_sign") IsNot Nothing) AndAlso (Request("goBackURL") IsNot Nothing) AndAlso (Request("api_key") IsNot Nothing) AndAlso (Request("secret_key") IsNot Nothing) Then
                payLoadStringFromRequest = Request("request_to_sign").ToString()
                goBackURL = Request("goBackURL").ToString()
                SignedPayLoadTextBox.Text = payLoadStringFromRequest.ToString()
                api_key = Request("api_key").ToString()
                secret_key = Request("secret_key").ToString()
                executeSignedPayloadFromRequest()
            Else
                If Not Page.IsPostBack Then
                    If ConfigurationManager.AppSettings("paymentType") Is Nothing Then
                        drawPanelForFailure(notaryPanel, "paymentType is not defined in configuration file")
                        Return
                    End If
                    paymentType = ConfigurationManager.AppSettings("paymentType")
                    If paymentType.Equals("Transaction", StringComparison.OrdinalIgnoreCase) Then
                        readTransactionParametersFromConfigurationFile()
                        Dim payLoadString As String = "{'Amount':'" & amount.ToString() & "','Category':'" & category.ToString() & "','Channel':'" & channel.ToString() & "','Description':'" & description.ToString() & "','MerchantTransactionId':'" & merchantTransactionId.ToString() & "','MerchantProductId':'" & merchantProductId.ToString() & "','MerchantApplicaitonId':'" & merchantApplicationId.ToString() & "','MerchantPaymentRedirectUrl':'" & merchantRedirectURI.ToString() & "'}"
                        requestText.Text = payLoadString.ToString()
                    ElseIf paymentType.Equals("Subscription", StringComparison.OrdinalIgnoreCase) Then
                        readTransactionParametersFromConfigurationFile()
                        readSubscriptionParametersFromConfigurationFile()
                        'string payLoadString = "{'Amount':'" + amount.ToString() + "','Category':'" + category.ToString() + "','Channel':'" + channel.ToString() + "','Description':'" + description.ToString() + "','MerchantTransactionId':'" + merchantTransactionId.ToString() + "','MerchantProductId':'" + merchantProductId.ToString() + "','MerchantApplicaitonId':'" + merchantApplicationId.ToString() + "','MerchantPaymentRedirectUrl':'" + merchantRedirectURI.ToString() + "','MerchantSubscriptionIdList':'" + MerchantSubscriptionIdList.ToString() + "','IsPurchaseOnNoActiveSubscription':'" + IsPurchaseOnNoActiveSubscription.ToString() + "','SubscriptionRecurringNumber':'" + SubscriptionRecurringNumber.ToString() + "','SubscriptionRecurringPeriod':'" + SubscriptionRecurringPeriod.ToString() + "','SubscriptionRecurringPeriodAmount':'" + SubscriptionRecurringPeriodAmount.ToString() + "'}";
                        Dim payLoadString As String = "{'Amount':'" & amount.ToString() & "','Category':'" & category.ToString() & "','Channel':'" & channel.ToString() & "','Description':'" & description.ToString() & "','MerchantTransactionId':'" & merchantTransactionId.ToString() & "','MerchantProductId':'" & merchantProductId.ToString() & "','MerchantPaymentRedirectUrl':'" & merchantRedirectURI.ToString() & "','MerchantSubscriptionIdList':'" & MerchantSubscriptionIdList.ToString() & "','IsPurchaseOnNoActiveSubscription':'" & IsPurchaseOnNoActiveSubscription.ToString() & "','SubscriptionRecurrences':'" & SubscriptionRecurringNumber.ToString() & "','SubscriptionPeriod':'" & SubscriptionRecurringPeriod.ToString() & "','SubscriptionPeriodAmount':'" & SubscriptionRecurringPeriodAmount.ToString() & "'}"
                        'Response.Write(payLoadString);
                        requestText.Text = payLoadString.ToString()
                    Else
                        drawPanelForFailure(notaryPanel, "paymentType is  defined with invalid value in configuration file.  Valid values are Transaction or Subscription.")
                        Return
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub executeSignedPayloadFromRequest()
        Try
            Dim sendingData As String = payLoadStringFromRequest.ToString()
            Dim newTransactionResponseData As [String]
            Dim notaryAddress As String
            notaryAddress = "" & FQDN & "/Security/Notary/Rest/1/SignedPayload"
            'WebRequest newTransactionRequestObject = (WebRequest)System.Net.WebRequest.Create("" + FQDN + "/Security/Notary/Rest/1/SignedPayload?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString());
            Dim newTransactionRequestObject As WebRequest = DirectCast(System.Net.WebRequest.Create(notaryAddress), WebRequest)
            newTransactionRequestObject.Headers.Add("client_id", api_key.ToString())
            newTransactionRequestObject.Headers.Add("client_secret", secret_key.ToString())
            newTransactionRequestObject.Method = "POST"
            newTransactionRequestObject.ContentType = "application/json"
            Dim encoding As New UTF8Encoding()
            Dim postBytes As Byte() = encoding.GetBytes(sendingData)
            newTransactionRequestObject.ContentLength = postBytes.Length

            Dim postStream As Stream = newTransactionRequestObject.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)
            postStream.Close()

            Dim newTransactionResponseObject As WebResponse = DirectCast(newTransactionRequestObject.GetResponse(), HttpWebResponse)
            Using newTransactionResponseStream As New StreamReader(newTransactionResponseObject.GetResponseStream())
                newTransactionResponseData = newTransactionResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As TransactionResponse = DirectCast(deserializeJsonObject.Deserialize(newTransactionResponseData, GetType(TransactionResponse)), TransactionResponse)
                newTransactionResponseStream.Close()
                'SignedPayLoadTextBox.Text = deserializedJsonObj.SignedDocument.ToString();
                'SignatureTextBox.Text = deserializedJsonObj.Signature.ToString();
                Response.Redirect(goBackURL.ToString() & "?ret_signed_payload=" & deserializedJsonObj.SignedDocument.ToString() & "&ret_signature=" & deserializedJsonObj.Signature.ToString())
            End Using
            'SignatureTextBox.Text = ex.ToString();
            'Response.Redirect(goBackURL.ToString() + "?ret_signed_payload_failed=true");
        Catch ex As Exception
        End Try
    End Sub

    Public Function executeSignedPayload() As Boolean
        Try
            Dim newTransactionResponseData As [String]
            Dim notaryAddress As String
            notaryAddress = "" & FQDN & "/Security/Notary/Rest/1/SignedPayload"
            Dim newTransactionRequestObject As WebRequest = DirectCast(System.Net.WebRequest.Create(notaryAddress), WebRequest)
            newTransactionRequestObject.Headers.Add("client_id", api_key.ToString())
            newTransactionRequestObject.Headers.Add("client_secret", secret_key.ToString())
            newTransactionRequestObject.Method = "POST"
            newTransactionRequestObject.ContentType = "application/json"
            Dim encoding As New UTF8Encoding()
            Dim payLoadString As String = requestText.Text.ToString()
            Dim postBytes As Byte() = encoding.GetBytes(payLoadString)
            newTransactionRequestObject.ContentLength = postBytes.Length
            Dim postStream As Stream = newTransactionRequestObject.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)
            postStream.Close()

            Dim newTransactionResponseObject As WebResponse = DirectCast(newTransactionRequestObject.GetResponse(), HttpWebResponse)
            Using newTransactionResponseStream As New StreamReader(newTransactionResponseObject.GetResponseStream())
                newTransactionResponseData = newTransactionResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()
                Dim deserializedJsonObj As TransactionResponse = DirectCast(deserializeJsonObject.Deserialize(newTransactionResponseData, GetType(TransactionResponse)), TransactionResponse)
                SignedPayLoadTextBox.Text = deserializedJsonObj.SignedDocument.ToString()
                SignatureTextBox.Text = deserializedJsonObj.Signature.ToString()
                'Response.Redirect(redirectUrl.ToString());
                newTransactionResponseStream.Close()
                Return True
            End Using
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Me.drawPanelForFailure(notaryPanel, New StreamReader(stream).ReadToEnd())
                End Using
            End If
            Return False
        Catch ex As Exception
            Return False
        End Try
    End Function
    Protected Sub signPayLoadButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        If signPayLoadButton.Text.Equals("Back", StringComparison.CurrentCultureIgnoreCase) Then
            Try
                Response.Redirect(goBackURL.ToString() & "?shown_notary=true")
            Catch ex As Exception
                drawPanelForFailure(notaryPanel, ex.ToString())
            End Try
        Else
            Dim result As Boolean = executeSignedPayload()
        End If
    End Sub


    Private Sub drawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        failureTable = New Table()
        failureTable.Font.Name = "Sans-serif"
        failureTable.Font.Size = 9
        failureTable.BorderStyle = BorderStyle.Outset
        failureTable.Width = Unit.Pixel(650)
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "ERROR:"
        rowOne.Controls.Add(rowOneCellOne)
        'rowOneCellOne.BorderWidth = 1;
        failureTable.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellOne As New TableCell()
        'rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellOne)
        failureTable.Controls.Add(rowTwo)
        failureTable.BorderWidth = 2
        failureTable.BorderColor = Color.Red
        failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(failureTable)
    End Sub
End Class

Public Class AccessTokenResponse
    Public access_token As String
    Public refresh_token As String
    Public expires_in As String
End Class

Public Class TransactionResponse
    Public Property SignedDocument() As String
        Get
            Return m_SignedDocument
        End Get
        Set(ByVal value As String)
            m_SignedDocument = Value
        End Set
    End Property
    Private m_SignedDocument As String
    Public Property Signature() As String
        Get
            Return m_Signature
        End Get
        Set(ByVal value As String)
            m_Signature = Value
        End Set
    End Property
    Private m_Signature As String
End Class
