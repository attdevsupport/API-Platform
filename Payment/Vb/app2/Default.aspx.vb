' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' September 2011
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2011 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System.Collections.Generic
Imports System.Configuration
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web.Script.Serialization
Imports System.Web.UI
Imports System.Web.UI.WebControls

#End Region

''' <summary>
''' Payment App2 class
''' </summary>
Partial Public Class Payment_App2
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private accessTokenFilePath As String, endPoint As String, subsDetailsFile As String, subsRefundFile As String, notificationDetailsFile As String

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private apiKey As String, secretKey As String, authCode As String, accessToken As String, authorizeRedirectUri As String, scope As String, _
     expirySeconds As String, refreshToken As String, accessTokenExpiryTime As String, refreshTokenExpiryTime As String

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private successTable As Table, failureTable As Table, successTableGetTransaction As Table, failureTableGetTransaction As Table, successTableGetSubscriptionDetails As Table, successTableSubscriptionRefund As Table

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private amount As String, channel As String, description As String, merchantTransactionId As String, merchantProductId As String, merchantApplicationId As String

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private category As Integer, noOfNotificationsToDisplay As Integer

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private merchantRedirectURI As Uri

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private merchantSubscriptionIdList As String, subscriptionRecurringPeriod As String, subscriptionRecurringNumber As String, subscriptionRecurringPeriodAmount As String, isPurchaseOnNoActiveSubscription As String

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private transactionTime As DateTime

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private transactionTimeString As String, payLoadStringFromRequest As String, signedPayload As String, signedSignature As String, notaryURL As String

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private subsDetailsCountToDisplay As Integer = 0

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private subsDetailsList As New List(Of KeyValuePair(Of String, String))()

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private subsRefundList As New List(Of KeyValuePair(Of String, String))()

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private latestFive As Boolean = True

    ''' <summary>
    ''' Local Variables
    ''' </summary>
    Private notificationDetailsTable As Table

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
    ''' Default method, that gets called upon loading the page and performs the following actions
    ''' Reads from config file
    ''' Process Notary Response
    ''' Process New Transaction Response
    ''' </summary>
    ''' <param name="sender">object that invoked this method</param>
    ''' <param name="e">Event arguments</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)

        subsRefundSuccessTable.Visible = False
        subsDetailsSuccessTable.Visible = False
        subscriptionSuccessTable.Visible = False
        subsGetStatusTable.Visible = False

        Dim currentServerTime As DateTime = DateTime.UtcNow
        lblServerTime.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

        Dim ableToRead As Boolean = Me.ReadConfigFile()
        If ableToRead = False Then
            Return
        End If

        If (Request("ret_signed_payload") IsNot Nothing) AndAlso (Request("ret_signature") IsNot Nothing) Then
            Me.signedPayload = Request("ret_signed_payload")
            Me.signedSignature = Request("ret_signature")
            Session("sub_signedPayLoad") = Me.signedPayload
            Session("sub_signedSignature") = Me.signedSignature
            Me.ProcessNotaryResponse()
        ElseIf (Request("SubscriptionAuthCode") IsNot Nothing) AndAlso (Session("sub_merTranId") IsNot Nothing) Then
            Me.ProcessCreateTransactionResponse()
        ElseIf (Request("shown_notary") IsNot Nothing) AndAlso (Session("sub_processNotary") IsNot Nothing) Then
            Session("sub_processNotary") = Nothing
            GetSubscriptionMerchantSubsID.Text = "Merchant Transaction ID: " & Session("sub_tempMerTranId").ToString()
            GetSubscriptionAuthCode.Text = "Auth Code: " & Session("sub_TranAuthCode").ToString()
        End If

        subsDetailsTable.Controls.Clear()
        Me.DrawSubsDetailsSection(False)
        subsRefundTable.Controls.Clear()
        Me.DrawSubsRefundSection(False)
        Me.DrawNotificationTableHeaders()
        Me.GetNotificationDetails()
        Return
    End Sub

    ''' <summary>
    ''' Subscription button click event
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">Event Arguments</param>
    Protected Sub NewSubscriptionButton_Click1(ByVal sender As Object, ByVal e As EventArgs)
        Me.ReadTransactionParametersFromConfigurationFile()
        Dim payLoadString As String = "{""Amount"":" & Me.amount & ",""Category"":" & Me.category & ",""Channel"":""" & Me.channel & """,""Description"":""" & Me.description & """,""MerchantTransactionId"":""" & Me.merchantTransactionId & """,""MerchantProductId"":""" & Me.merchantProductId & """,""MerchantPaymentRedirectUrl"":""" & Convert.ToString(Me.merchantRedirectURI) & """,""MerchantSubscriptionIdList"":""" & Me.merchantSubscriptionIdList & """,""IsPurchaseOnNoActiveSubscription"":""" & Me.isPurchaseOnNoActiveSubscription & """,""SubscriptionRecurrences"":" & Me.subscriptionRecurringNumber & ",""SubscriptionPeriod"":""" & Me.subscriptionRecurringPeriod & """,""SubscriptionPeriodAmount"":" & Me.subscriptionRecurringPeriodAmount & "}"
        Session("sub_payloadData") = payLoadString
        Response.Redirect(Me.notaryURL & "?request_to_sign=" & payLoadString & "&goBackURL=" & Convert.ToString(Me.merchantRedirectURI) & "&api_key=" & Me.apiKey & "&secret_key=" & Me.secretKey)
    End Sub

    ''' <summary>
    ''' Get Subscription button click event
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub GetSubscriptionButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim resourcePathString As String = String.Empty
        Try
            Dim keyValue As String = String.Empty
            If Radio_SubscriptionStatus.SelectedIndex = 0 Then
                keyValue = GetSubscriptionMerchantSubsID.Text.ToString().Replace("Merchant Transaction ID: ", String.Empty)
                If keyValue.Length = 0 Then
                    Return
                End If

                resourcePathString = String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Subscriptions/MerchantTransactionId/" & keyValue
            End If

            If Radio_SubscriptionStatus.SelectedIndex = 1 Then
                keyValue = GetSubscriptionAuthCode.Text.ToString().Replace("Auth Code: ", String.Empty)
                If keyValue.Length = 0 Then
                    Return
                End If

                resourcePathString = String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Subscriptions/TransactionAuthCode/" & keyValue
            End If

            If Radio_SubscriptionStatus.SelectedIndex = 2 Then
                keyValue = GetSubscriptionID.Text.ToString().Replace("Subscription ID: ", String.Empty)
                If keyValue.Length = 0 Then
                    Return
                End If

                resourcePathString = String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Subscriptions/SubscriptionId/" & keyValue
            End If

            If Me.ReadAndGetAccessToken(newSubscriptionPanel) = True Then
                If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
                    Return
                End If

                ' resourcePathString = resourcePathString + "?access_token=" + this.access_token;
                Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(resourcePathString), HttpWebRequest)
                objRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                objRequest.Method = "GET"

                Dim getTransactionStatusResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)

                Using getTransactionStatusResponseStream As New StreamReader(getTransactionStatusResponseObject.GetResponseStream())
                    Dim getTransactionStatusResponseData As String = getTransactionStatusResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As SubscriptionStatusResponse = DirectCast(deserializeJsonObject.Deserialize(getTransactionStatusResponseData, GetType(SubscriptionStatusResponse)), SubscriptionStatusResponse)
                    'DrawPanelForFailure(getSubscriptionStatusPanel, getTransactionStatusResponseData);
                    'lblstatusMerSubsId.Text = deserializedJsonObj.MerchantSubscriptionId
                    'lblstatusSubsId.Text = deserializedJsonObj.SubscriptionId
                    GetSubscriptionID.Text = "Subscription ID: " & deserializedJsonObj.SubscriptionId

                    If Me.CheckItemInSubsDetailsFile(deserializedJsonObj.MerchantSubscriptionId, deserializedJsonObj.ConsumerId) = False Then
                        Me.WriteSubsDetailsToFile(deserializedJsonObj.MerchantSubscriptionId, deserializedJsonObj.ConsumerId)
                    End If

                    If Me.CheckItemInSubsRefundFile(deserializedJsonObj.SubscriptionId, deserializedJsonObj.MerchantSubscriptionId) = False Then
                        Me.WriteSubsRefundToFile(deserializedJsonObj.SubscriptionId, deserializedJsonObj.MerchantSubscriptionId)
                    End If

                    subsDetailsTable.Controls.Clear()
                    Me.DrawSubsDetailsSection(False)
                    subsRefundTable.Controls.Clear()
                    Me.DrawSubsRefundSection(False)
                    subsGetStatusTable.Visible = True

                    Me.DrawPanelForGetTransactionSuccess(getSubscriptionStatusPanel)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Amount", deserializedJsonObj.Amount)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Channel ", deserializedJsonObj.Channel)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Description", deserializedJsonObj.Description)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsAutoCommitted", deserializedJsonObj.IsAutoCommitted)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantId", deserializedJsonObj.MerchantId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantTransactionId", deserializedJsonObj.MerchantTransactionId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantSubscriptionId", deserializedJsonObj.MerchantSubscriptionId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "OriginalTransactionId", deserializedJsonObj.OriginalTransactionId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionId", deserializedJsonObj.SubscriptionId)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionPeriod", deserializedJsonObj.SubscriptionPeriod)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionPeriodAmount", deserializedJsonObj.SubscriptionPeriodAmount)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionRecurrences", deserializedJsonObj.SubscriptionRecurrences)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionStatus", deserializedJsonObj.SubscriptionStatus)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionType", deserializedJsonObj.SubscriptionType)
                    Me.AddRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version)

                    getTransactionStatusResponseStream.Close()
                End Using
            End If
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Dim reader As New StreamReader(stream)
                    Me.DrawPanelForFailure(getSubscriptionStatusPanel, reader.ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(getSubscriptionStatusPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' View Notary button click event
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub ViewNotaryButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        If (Session("sub_payloadData") IsNot Nothing) AndAlso (Session("sub_signedPayLoad") IsNot Nothing) AndAlso (Session("sub_signedSignature") IsNot Nothing) Then
            Session("sub_processNotary") = "notary"
            Response.Redirect(Me.notaryURL & "?signed_payload=" & Session("sub_signedPayLoad").ToString() & "&goBackURL=" & Convert.ToString(Me.merchantRedirectURI) & "&signed_signature=" & Session("sub_signedSignature").ToString() & "&signed_request=" & Session("sub_payloadData").ToString())
        End If
    End Sub

    ''' <summary>
    ''' Get Subscription Details button click
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnGetSubscriptionDetails_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim merSubsID As String = String.Empty
        Dim recordFound As Boolean = False
        Try
            If Me.subsDetailsList.Count > 0 Then
                For Each subDetailsTableRow As Control In subsDetailsTable.Controls
                    If TypeOf subDetailsTableRow Is TableRow Then
                        For Each subDetailsTableRowCell As Control In subDetailsTableRow.Controls
                            If TypeOf subDetailsTableRowCell Is TableCell Then
                                For Each subDetailsTableCellControl As Control In subDetailsTableRowCell.Controls
                                    If TypeOf subDetailsTableCellControl Is RadioButton Then
                                        If DirectCast(subDetailsTableCellControl, RadioButton).Checked Then
                                            merSubsID = DirectCast(subDetailsTableCellControl, RadioButton).Text.ToString()
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
                    If Me.ReadAndGetAccessToken(subsDetailsPanel) = True Then
                        If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
                            Return
                        End If

                        Dim consID As String = Me.GetValueOfKey(merSubsID)

                        If consID.CompareTo("null") = 0 Then
                            Return
                        End If

                        Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Subscriptions/" & merSubsID & "/Detail/" & consID), WebRequest)

                        objRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                        objRequest.Method = "GET"
                        objRequest.ContentType = "application/json"

                        Dim subsDetailsResponeObject As WebResponse = DirectCast(objRequest.GetResponse(), WebResponse)

                        Using subsDetailsResponseStream As New StreamReader(subsDetailsResponeObject.GetResponseStream())
                            Dim subsDetailsResponseData As String = subsDetailsResponseStream.ReadToEnd()
                            Dim deserializeJsonObject As New JavaScriptSerializer()
                            Dim deserializedJsonObj As SubscriptionDetailsResponse = DirectCast(deserializeJsonObject.Deserialize(subsDetailsResponseData, GetType(SubscriptionDetailsResponse)), SubscriptionDetailsResponse)
                            subsDetailsSuccessTable.Visible = True
                            'lblMerSubId.Text = merSubsID.ToString()
                            'lblConsId.Text = consID.ToString()
                            Me.DrawPanelForGetSubscriptionDetailsSuccess(subsDetailsPanel)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CreationDate", deserializedJsonObj.CreationDate)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentEndDate", deserializedJsonObj.CurrentEndDate)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentStartDate", deserializedJsonObj.CurrentStartDate)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "GrossAmount", deserializedJsonObj.GrossAmount)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsActiveSubscription", deserializedJsonObj.IsActiveSubscription)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Recurrences", deserializedJsonObj.Recurrences)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "RecurrencesLeft", deserializedJsonObj.RecurrencesLeft)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Status", deserializedJsonObj.Status)
                            Me.AddRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version)

                            ' subsDetailsList.RemoveAll(x => x.Key.Equals(merSubsID));
                            ' updatesubsDetailsListToFile();
                            ' resetSubsDetailsList();
                            ' subsDetailsTable.Controls.Clear();
                            ' drawSubsDetailsSection(false);
                            ' GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: ";
                            ' GetSubscriptionAuthCode.Text = "Auth Code: ";
                            ' GetSubscriptionID.Text = "Subscription ID: ";
                            If Me.latestFive = False Then
                            End If

                            subsDetailsResponseStream.Close()
                        End Using
                    End If
                End If
            End If
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Dim reader As New StreamReader(stream)
                    Me.DrawPanelForFailure(subsDetailsPanel, reader.ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(subsDetailsPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Get Subscription Refund button click
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnGetSubscriptionRefund_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim subsID As String = String.Empty
        Dim recordFound As Boolean = False
        Dim strReq As String = "{""TransactionOperationStatus"":""Refunded"",""RefundReasonCode"":1,""RefundReasonText"":""Customer was not happy""}"
        Dim dataLength As String = String.Empty
        Try
            If Me.subsRefundList.Count > 0 Then
                For Each subRefundTableRow As Control In subsRefundTable.Controls
                    If TypeOf subRefundTableRow Is TableRow Then
                        For Each subRefundTableRowCell As Control In subRefundTableRow.Controls
                            If TypeOf subRefundTableRowCell Is TableCell Then
                                For Each subRefundTableCellControl As Control In subRefundTableRowCell.Controls
                                    If TypeOf subRefundTableCellControl Is RadioButton Then
                                        If DirectCast(subRefundTableCellControl, RadioButton).Checked Then
                                            subsID = DirectCast(subRefundTableCellControl, RadioButton).Text.ToString()
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
                    If Me.ReadAndGetAccessToken(subsRefundPanel) = True Then
                        If Me.accessToken Is Nothing OrElse Me.accessToken.Length <= 0 Then
                            Return
                        End If

                        Dim merSubsID As String = Me.GetValueOfKeyFromRefund(subsID)

                        If merSubsID.CompareTo("null") = 0 Then
                            Return
                        End If

                        Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create(String.Empty & Me.endPoint & "/rest/3/Commerce/Payment/Transactions/" & subsID), WebRequest)
                        objRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                        objRequest.Method = "PUT"
                        objRequest.ContentType = "application/json"

                        Dim encoding As New UTF8Encoding()
                        Dim postBytes As Byte() = encoding.GetBytes(strReq)
                        objRequest.ContentLength = postBytes.Length

                        dataLength = postBytes.Length.ToString()

                        Dim postStream As Stream = objRequest.GetRequestStream()
                        postStream.Write(postBytes, 0, postBytes.Length)
                        postStream.Close()

                        Dim subsRefundResponeObject As WebResponse = DirectCast(objRequest.GetResponse(), WebResponse)
                        Using subsRefundResponseStream As New StreamReader(subsRefundResponeObject.GetResponseStream())
                            Dim subsRefundResponseData As String = subsRefundResponseStream.ReadToEnd()
                            Dim deserializeJsonObject As New JavaScriptSerializer()
                            Dim deserializedJsonObj As RefundResponse = DirectCast(deserializeJsonObject.Deserialize(subsRefundResponseData, GetType(RefundResponse)), RefundResponse)
                            'DrawPanelForFailure(subsRefundPanel, subsRefundResponseData);
                            subsRefundSuccessTable.Visible = True
                            'lbRefundTranID.Text = deserializedJsonObj.TransactionId
                            DrawPanelForSubscriptionRefundSuccess(subsRefundPanel)
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "CommitConfirmationId", deserializedJsonObj.CommitConfirmationId)
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "IsSuccess", deserializedJsonObj.IsSuccess)
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "OriginalPurchaseAmount", deserializedJsonObj.OriginalPurchaseAmount)
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "TransactionId", deserializedJsonObj.TransactionId)
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "TransactionStatus", deserializedJsonObj.TransactionStatus)
                            AddRowToSubscriptionRefundSuccessPanel(subsRefundPanel, "Version", deserializedJsonObj.Version)


                            If Me.latestFive = False Then
                                Me.subsRefundList.RemoveAll(Function(x) x.Key.Equals(subsID))
                                Me.UpdatesSubsRefundListToFile()
                                Me.ResetSubsRefundList()
                                subsRefundTable.Controls.Clear()
                                Me.DrawSubsRefundSection(False)
                                GetSubscriptionMerchantSubsID.Text = "Merchant Transaction ID: "
                                GetSubscriptionAuthCode.Text = "Auth Code: "
                                GetSubscriptionID.Text = "Subscription ID: "
                            End If

                            subsRefundResponseStream.Close()
                        End Using
                    End If
                End If
            End If
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    Dim reader As New StreamReader(stream)
                    Me.DrawPanelForFailure(subsRefundPanel, reader.ReadToEnd())
                End Using
            End If
        Catch ex As Exception
            Me.DrawPanelForFailure(subsRefundPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Refresh notification messages
    ''' </summary>
    ''' <param name="sender">Sender Details</param>
    ''' <param name="e">List of Arguments</param>
    Protected Sub BtnRefreshNotifications_Click(ByVal sender As Object, ByVal e As EventArgs)
        Me.notificationDetailsTable.Controls.Clear()
        Me.DrawNotificationTableHeaders()
        Me.GetNotificationDetails()
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
        cellOne.HorizontalAlign = HorizontalAlign.Right
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
    ''' Reads from config file and assigns to local variables
    ''' </summary>
    ''' <returns>true/false; true if able to read all values, false otherwise</returns>
    Private Function ReadConfigFile() As Boolean
        Me.endPoint = ConfigurationManager.AppSettings("endPoint")

        If String.IsNullOrEmpty(Me.endPoint) Then
            Me.DrawPanelForFailure(newSubscriptionPanel, "endPoint is not defined in configuration file")
            Return False
        End If

        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(newSubscriptionPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(newSubscriptionPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "~\PayApp2AccessToken.txt"
        End If

        Me.subsDetailsFile = ConfigurationManager.AppSettings("subsDetailsFile")
        If String.IsNullOrEmpty(Me.subsDetailsFile) Then
            Me.subsDetailsFile = "~\subsDetailsFile.txt"
        End If

        Me.subsRefundFile = ConfigurationManager.AppSettings("subsRefundFile")
        If String.IsNullOrEmpty(Me.subsRefundFile) Then
            Me.subsRefundFile = "~\subsRefundFile.txt"
        End If

        Me.subsDetailsCountToDisplay = 5
        If ConfigurationManager.AppSettings("subsDetailsCountToDisplay") IsNot Nothing Then
            Me.subsDetailsCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("subsDetailsCountToDisplay"))
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "PAYMENT"
        End If

        Me.notaryURL = ConfigurationManager.AppSettings("notaryURL")
        If ConfigurationManager.AppSettings("notaryURL") Is Nothing Then
            Me.DrawPanelForFailure(newSubscriptionPanel, "notaryURL is not defined in configuration file")
            Return False
        End If

        If ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl") Is Nothing Then
            Me.DrawPanelForFailure(newSubscriptionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file")
            Return False
        End If

        Me.merchantRedirectURI = New Uri(ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl"))

        If ConfigurationManager.AppSettings("DisableLatestFive") IsNot Nothing Then
            Me.latestFive = False
        End If

        Me.notificationDetailsFile = ConfigurationManager.AppSettings("notificationDetailsFile")
        If String.IsNullOrEmpty(Me.notificationDetailsFile) Then
            Me.notificationDetailsFile = "~\listener\notificationDetailsFile.txt"
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        If String.IsNullOrEmpty(ConfigurationManager.AppSettings("noOfNotificationsToDisplay")) Then
            Me.noOfNotificationsToDisplay = 5
        Else
            noOfNotificationsToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("noOfNotificationsToDisplay"))
        End If

        Return True
    End Function

    ''' <summary>
    ''' This medthod is used for adding row in Subscription Details Section
    ''' </summary>
    ''' <param name="subscription">Subscription Details</param>
    ''' <param name="merchantsubscription">Merchant Details</param>
    Private Sub AddRowToSubsDetailsSection(ByVal subscription As String, ByVal merchantsubscription As String)
        Dim rowOne As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Left
        cellOne.CssClass = "cell"
        cellOne.Width = Unit.Pixel(150)
        Dim rbutton As New RadioButton()
        rbutton.Text = subscription.ToString()
        rbutton.GroupName = "SubsDetailsSection"
        rbutton.ID = subscription.ToString()
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
        cellThree.Text = merchantsubscription.ToString()
        rowOne.Controls.Add(cellThree)
        Dim cellFour As New TableCell()
        cellFour.CssClass = "cell"
        rowOne.Controls.Add(cellFour)

        subsDetailsTable.Controls.Add(rowOne)
    End Sub

    ''' <summary>
    ''' This medthod is used for adding row in Subscription Refund Section
    ''' </summary>
    ''' <param name="subscription">Subscription Details</param>
    ''' <param name="merchantsubscription">Merchant Details</param>
    Private Sub AddRowToSubsRefundSection(ByVal subscription As String, ByVal merchantsubscription As String)
        Dim rowOne As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Left
        cellOne.CssClass = "cell"
        cellOne.Width = Unit.Pixel(150)
        Dim rbutton As New RadioButton()
        rbutton.Text = subscription.ToString()
        rbutton.GroupName = "SubsRefundSection"
        rbutton.ID = subscription.ToString()
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
        cellThree.Text = merchantsubscription.ToString()
        rowOne.Controls.Add(cellThree)
        Dim cellFour As New TableCell()
        cellFour.CssClass = "cell"
        rowOne.Controls.Add(cellFour)

        subsRefundTable.Controls.Add(rowOne)
    End Sub

    ''' <summary>
    ''' This medthod is used for Drawing Subscription Details Section
    ''' </summary>
    ''' <param name="onlyRow">Row Details</param>
    Private Sub DrawSubsDetailsSection(ByVal onlyRow As Boolean)
        Try
            If onlyRow = False Then
                Dim headingRow As New TableRow()
                Dim headingCellOne As New TableCell()
                headingCellOne.HorizontalAlign = HorizontalAlign.Left
                headingCellOne.CssClass = "cell"
                headingCellOne.Width = Unit.Pixel(200)
                headingCellOne.Font.Bold = True
                headingCellOne.Text = "Merchant Subscription ID"
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
                headingCellThree.Text = "Consumer ID"
                headingRow.Controls.Add(headingCellThree)
                Dim headingCellFour As New TableCell()
                headingCellFour.CssClass = "warning"
                Dim warningMessage As New LiteralControl("<b>WARNING:</b><br/>You must use Get Subscription Status before you can view details of it.")
                headingCellFour.Controls.Add(warningMessage)
                headingRow.Controls.Add(headingCellFour)
                subsDetailsTable.Controls.Add(headingRow)
            End If

            Me.ResetSubsDetailsList()
            Me.GetSubsDetailsFromFile()

            Dim tempCountToDisplay As Integer = 1
            While (tempCountToDisplay <= Me.subsDetailsCountToDisplay) AndAlso (tempCountToDisplay <= Me.subsDetailsList.Count) AndAlso (Me.subsDetailsList.Count > 0)
                Me.AddRowToSubsDetailsSection(Me.subsDetailsList(tempCountToDisplay - 1).Key, Me.subsDetailsList(tempCountToDisplay - 1).Value)
                tempCountToDisplay += 1
            End While
        Catch ex As Exception
            Me.DrawPanelForFailure(subsDetailsPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' This medthod is used for drawing Subscription Refund Section
    ''' </summary>
    ''' <param name="onlyRow">Row Details</param>
    Private Sub DrawSubsRefundSection(ByVal onlyRow As Boolean)
        Try
            If onlyRow = False Then
                Dim headingRow As New TableRow()
                Dim headingCellOne As New TableCell()
                headingCellOne.HorizontalAlign = HorizontalAlign.Left
                headingCellOne.CssClass = "cell"
                headingCellOne.Width = Unit.Pixel(200)
                headingCellOne.Font.Bold = True
                headingCellOne.Text = "Subscription ID"
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
                headingCellThree.Text = "Merchant Subscription ID"
                headingRow.Controls.Add(headingCellThree)
                Dim headingCellFour As New TableCell()
                headingCellFour.CssClass = "warning"
                Dim warningMessage As New LiteralControl("<b>WARNING:</b><br/>You must use Get Subscription Status before you can refund.")
                headingCellFour.Controls.Add(warningMessage)
                headingRow.Controls.Add(headingCellFour)
                subsRefundTable.Controls.Add(headingRow)
            End If

            Me.ResetSubsRefundList()
            Me.GetSubsRefundFromFile()

            Dim tempCountToDisplay As Integer = 1
            While (tempCountToDisplay <= Me.subsDetailsCountToDisplay) AndAlso (tempCountToDisplay <= Me.subsRefundList.Count) AndAlso (Me.subsRefundList.Count > 0)
                Me.AddRowToSubsRefundSection(Me.subsRefundList(tempCountToDisplay - 1).Key, Me.subsRefundList(tempCountToDisplay - 1).Value)
                tempCountToDisplay += 1
            End While
        Catch ex As Exception
            Me.DrawPanelForFailure(subsRefundPanel, ex.ToString())
        End Try
    End Sub

    ''' <summary>
    ''' Method to get the value of key from the selected row in Refund Section
    ''' </summary>
    ''' <param name="key">Key Value to be found</param>
    ''' <returns>Returns the value in String</returns>
    Private Function GetValueOfKeyFromRefund(ByVal key As String) As String
        Dim tempCount As Integer = 0
        While tempCount < Me.subsRefundList.Count
            If Me.subsRefundList(tempCount).Key.CompareTo(key) = 0 Then
                Return Me.subsRefundList(tempCount).Value
            End If

            tempCount += 1
        End While

        Return "null"
    End Function

    ''' <summary>
    ''' Method to get the value from Key value
    ''' </summary>
    ''' <param name="key">Key Value to be found</param>
    ''' <returns>Returns the value in String</returns>
    Private Function GetValueOfKey(ByVal key As String) As String
        Dim tempCount As Integer = 0
        While tempCount < Me.subsDetailsList.Count
            If Me.subsDetailsList(tempCount).Key.CompareTo(key) = 0 Then
                Return Me.subsDetailsList(tempCount).Value
            End If

            tempCount += 1
        End While

        Return "null"
    End Function

    ''' <summary>
    ''' Method to reset Subscription Refund List
    ''' </summary>
    Private Sub ResetSubsRefundList()
        Me.subsRefundList.RemoveRange(0, Me.subsRefundList.Count)
    End Sub

    ''' <summary>
    ''' Method to reset Subscription Details List
    ''' </summary>
    Private Sub ResetSubsDetailsList()
        Me.subsDetailsList.RemoveRange(0, Me.subsDetailsList.Count)
    End Sub

    ''' <summary>
    ''' Method to get Subscription Details from the file.
    ''' </summary>
    Private Sub GetSubsDetailsFromFile()
        Dim file As New FileStream(Request.MapPath(Me.subsDetailsFile), FileMode.Open, FileAccess.Read)
        Dim sr As New StreamReader(file)
        Dim line As String

        While (InlineAssignHelper(line, sr.ReadLine())) IsNot Nothing
            Dim subsDetailsKeys As String() = Regex.Split(line, ":-:")
            If subsDetailsKeys(0) IsNot Nothing AndAlso subsDetailsKeys(1) IsNot Nothing Then
                Me.subsDetailsList.Add(New KeyValuePair(Of String, String)(subsDetailsKeys(0), subsDetailsKeys(1)))
            End If
        End While

        sr.Close()
        file.Close()
        Me.subsDetailsList.Reverse(0, Me.subsDetailsList.Count)
    End Sub

    ''' <summary>
    ''' Method to get Subscription Refund from the file.
    ''' </summary>
    Private Sub GetSubsRefundFromFile()
        Dim file As New FileStream(Request.MapPath(Me.subsRefundFile), FileMode.Open, FileAccess.Read)
        Dim sr As New StreamReader(file)
        Dim line As String

        While (InlineAssignHelper(line, sr.ReadLine())) IsNot Nothing
            Dim subsRefundKeys As String() = Regex.Split(line, ":-:")
            If subsRefundKeys(0) IsNot Nothing AndAlso subsRefundKeys(1) IsNot Nothing Then
                Me.subsRefundList.Add(New KeyValuePair(Of String, String)(subsRefundKeys(0), subsRefundKeys(1)))
            End If
        End While

        sr.Close()
        file.Close()
        Me.subsRefundList.Reverse(0, Me.subsRefundList.Count)
    End Sub

    ''' <summary>
    ''' Method to update Subscription Refund list to the file.
    ''' </summary>
    Private Sub UpdatesSubsRefundListToFile()
        If Me.subsRefundList.Count <> 0 Then
            Me.subsRefundList.Reverse(0, Me.subsRefundList.Count)
        End If

        Using sr As StreamWriter = File.CreateText(Request.MapPath(Me.subsRefundFile))
            Dim tempCount As Integer = 0
            While tempCount < Me.subsRefundList.Count
                Dim lineToWrite As String = Me.subsRefundList(tempCount).Key & ":-:" & Me.subsRefundList(tempCount).Value
                sr.WriteLine(lineToWrite)
                tempCount += 1
            End While

            sr.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Method to update Subscription Details list to the file.
    ''' </summary>
    Private Sub UpdateSubsDetailsListToFile()
        If Me.subsDetailsList.Count <> 0 Then
            Me.subsDetailsList.Reverse(0, Me.subsDetailsList.Count)
        End If

        Using sr As StreamWriter = File.CreateText(Request.MapPath(Me.subsDetailsFile))
            Dim tempCount As Integer = 0
            While tempCount < Me.subsDetailsList.Count
                Dim lineToWrite As String = Me.subsDetailsList(tempCount).Key & ":-:" & Me.subsDetailsList(tempCount).Value
                sr.WriteLine(lineToWrite)
                tempCount += 1
            End While

            sr.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Method to check item in Subscription Refund file.
    ''' </summary>
    ''' <param name="transactionid">Transaction Id details</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id details</param>
    ''' <returns>Returns True or False</returns>
    Private Function CheckItemInSubsRefundFile(ByVal transactionid As String, ByVal merchantTransactionId As String) As Boolean
        Dim line As String
        Dim lineToFind As String = transactionid & ":-:" & merchantTransactionId
        Dim file As New System.IO.StreamReader(Request.MapPath(Me.subsRefundFile))
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
    ''' Method to check item in Subscription Details file.
    ''' </summary>
    ''' <param name="transactionid">Transaction Id details</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id details</param>
    ''' <returns>Returns True or False</returns>
    Private Function CheckItemInSubsDetailsFile(ByVal transactionid As String, ByVal merchantTransactionId As String) As Boolean
        Dim line As String
        Dim lineToFind As String = transactionid & ":-:" & merchantTransactionId
        Dim file As New System.IO.StreamReader(Request.MapPath(Me.subsDetailsFile))
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
    ''' Method to write Subscription Refund to file.
    ''' </summary>
    ''' <param name="transactionid">Transaction Id</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id</param>
    Private Sub WriteSubsRefundToFile(ByVal transactionid As String, ByVal merchantTransactionId As String)
        Using appendContent As StreamWriter = File.AppendText(Request.MapPath(Me.subsRefundFile))
            Dim line As String = transactionid & ":-:" & merchantTransactionId
            appendContent.WriteLine(line)
            appendContent.Flush()
            appendContent.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Method to write Subscription Details to file.
    ''' </summary>
    ''' <param name="transactionid">Transaction Id</param>
    ''' <param name="merchantTransactionId">Merchant Transaction Id</param>
    Private Sub WriteSubsDetailsToFile(ByVal transactionid As String, ByVal merchantTransactionId As String)
        Using appendContent As StreamWriter = File.AppendText(Request.MapPath(Me.subsDetailsFile))
            Dim line As String = transactionid & ":-:" & merchantTransactionId
            appendContent.WriteLine(line)
            appendContent.Flush()
            appendContent.Close()
        End Using
    End Sub

    ''' <summary>
    ''' Method to read Transaction Parameters from Configuration file.
    ''' </summary>
    Private Sub ReadTransactionParametersFromConfigurationFile()
        Me.transactionTime = DateTime.UtcNow
        Me.transactionTimeString = [String].Format("{0:dddMMMddyyyyHHmmss}", Me.transactionTime)
        If Radio_SubscriptionProductType.SelectedIndex = 0 Then
            Me.amount = "1.99"
        ElseIf Radio_SubscriptionProductType.SelectedIndex = 1 Then
            Me.amount = "3.99"
        End If

        Session("sub_tranType") = Radio_SubscriptionProductType.SelectedIndex.ToString()

        If ConfigurationManager.AppSettings("Category") Is Nothing Then
            Me.DrawPanelForFailure(newSubscriptionPanel, "Category is not defined in configuration file")
            Return
        End If

        Me.category = Convert.ToInt32(ConfigurationManager.AppSettings("Category"))
        Me.channel = ConfigurationManager.AppSettings("Channel")
        If String.IsNullOrEmpty(Me.channel) Then
            Me.channel = "MOBILE_WEB"
        End If

        Me.description = "TrDesc" & Me.transactionTimeString
        Me.merchantTransactionId = "TrId" & Me.transactionTimeString
        Session("sub_merTranId") = Me.merchantTransactionId
        Me.merchantProductId = "ProdId" & Me.transactionTimeString
        Me.merchantApplicationId = "MerAppId" & Me.transactionTimeString
        Me.merchantSubscriptionIdList = "ML" & New Random().[Next]()
        Session("MerchantSubscriptionIdList") = Me.merchantSubscriptionIdList

        Me.isPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings("IsPurchaseOnNoActiveSubscription")

        If String.IsNullOrEmpty(Me.isPurchaseOnNoActiveSubscription) Then
            Me.isPurchaseOnNoActiveSubscription = "false"
        End If

        Me.subscriptionRecurringNumber = ConfigurationManager.AppSettings("SubscriptionRecurringNumber")
        If String.IsNullOrEmpty(Me.subscriptionRecurringNumber) Then
            Me.subscriptionRecurringNumber = "99999"
        End If

        Me.subscriptionRecurringPeriod = ConfigurationManager.AppSettings("SubscriptionRecurringPeriod")
        If String.IsNullOrEmpty(Me.subscriptionRecurringPeriod) Then
            Me.subscriptionRecurringPeriod = "MONTHLY"
        End If

        Me.subscriptionRecurringPeriodAmount = ConfigurationManager.AppSettings("SubscriptionRecurringPeriodAmount")
        If String.IsNullOrEmpty(Me.subscriptionRecurringPeriodAmount) Then
            Me.subscriptionRecurringPeriodAmount = "1"
        End If
    End Sub

    ''' <summary>
    ''' Method to process Notary Response
    ''' </summary>
    Private Sub ProcessNotaryResponse()
        If Session("sub_tranType") IsNot Nothing Then
            Radio_SubscriptionProductType.SelectedIndex = Convert.ToInt32(Session("sub_tranType").ToString())
            Session("sub_tranType") = Nothing
        End If

        Response.Redirect(Me.endPoint & "/rest/3/Commerce/Payment/Subscriptions?clientid=" & Me.apiKey & "&SignedPaymentDetail=" & Me.signedPayload & "&Signature=" & Me.signedSignature)
    End Sub

    ''' <summary>
    ''' Method to process create transaction response
    ''' </summary>
    Private Sub ProcessCreateTransactionResponse()
        lblsubscode.Text = Request("SubscriptionAuthCode").ToString()
        lblsubsid.Text = Session("sub_merTranId").ToString()
        subscriptionSuccessTable.Visible = True
        GetSubscriptionMerchantSubsID.Text = "Merchant Transaction ID: " & Session("sub_merTranId").ToString()
        GetSubscriptionAuthCode.Text = "Auth Code: " & Request("SubscriptionAuthCode").ToString()
        GetSubscriptionID.Text = "Subscription ID: "
        Session("sub_tempMerTranId") = Session("sub_merTranId").ToString()
        Session("sub_merTranId") = Nothing
        Session("sub_TranAuthCode") = Request("SubscriptionAuthCode").ToString()
    End Sub

    ''' <summary>
    ''' Method to draw the success table.
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
    ''' Method to add rows to success table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attributes as String</param>
    ''' <param name="value">Value as String</param>
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
    ''' Method to draw error table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="message">Message as String</param>
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
        rowTwoCellOne.Text = message
        rowTwo.Controls.Add(rowTwoCellOne)
        Me.failureTable.Controls.Add(rowTwo)
        Me.failureTable.BorderWidth = 2
        Me.failureTable.BorderColor = Color.Red
        Me.failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
        panelParam.Controls.Add(Me.failureTable)
    End Sub

    ''' <summary>
    ''' Method to draw panel for successful transaction.
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
    ''' Method to add row to success table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attribute as String</param>
    ''' <param name="value">Value as String</param>
    Private Sub AddRowToGetTransactionSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.Text = attribute
        cellOne.Width = Unit.Pixel(300)
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Width = Unit.Pixel(50)
        row.Controls.Add(cellTwo)
        Dim cellThree As New TableCell()
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Text = value
        cellThree.Width = Unit.Pixel(300)
        row.Controls.Add(cellThree)
        Me.successTableGetTransaction.Controls.Add(row)
    End Sub

    ''' <summary>
    ''' Method to draw panel for successful refund.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    Private Sub DrawPanelForSubscriptionRefundSuccess(ByVal panelParam As Panel)
        Me.successTableSubscriptionRefund = New Table()
        Me.successTableSubscriptionRefund.Font.Name = "Sans-serif"
        Me.successTableSubscriptionRefund.Font.Size = 8
        Me.successTableSubscriptionRefund.Width = Unit.Pixel(650)
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
        Me.successTableSubscriptionRefund.Controls.Add(rowOne)
        panelParam.Controls.Add(Me.successTableSubscriptionRefund)
    End Sub

    ''' <summary>
    ''' Method to draw panel for successful transaction.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    Private Sub DrawPanelForGetSubscriptionDetailsSuccess(ByVal panelParam As Panel)
        Me.successTableGetSubscriptionDetails = New Table()
        Me.successTableGetSubscriptionDetails.Font.Name = "Sans-serif"
        Me.successTableGetSubscriptionDetails.Font.Size = 8
        Me.successTableGetSubscriptionDetails.Width = Unit.Pixel(650)
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
        Me.successTableGetSubscriptionDetails.Controls.Add(rowOne)
        panelParam.Controls.Add(Me.successTableGetSubscriptionDetails)
    End Sub

    ''' <summary>
    ''' Method to add row to success table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attribute as String</param>
    ''' <param name="value">Value as String</param>
    Private Sub AddRowToSubscriptionRefundSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.Text = attribute
        cellOne.Width = Unit.Pixel(300)
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Width = Unit.Pixel(50)
        row.Controls.Add(cellTwo)
        Dim cellThree As New TableCell()
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Text = value
        cellThree.Width = Unit.Pixel(300)
        row.Controls.Add(cellThree)
        Me.successTableSubscriptionRefund.Controls.Add(row)
    End Sub

    ''' <summary>
    ''' Method to add row to success table.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <param name="attribute">Attribute as String</param>
    ''' <param name="value">Value as String</param>
    Private Sub AddRowToGetSubscriptionDetailsSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
        Dim row As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.Text = attribute
        cellOne.Width = Unit.Pixel(300)
        row.Controls.Add(cellOne)
        Dim cellTwo As New TableCell()
        cellTwo.Width = Unit.Pixel(50)
        row.Controls.Add(cellTwo)
        Dim cellThree As New TableCell()
        cellThree.HorizontalAlign = HorizontalAlign.Left
        cellThree.Text = value
        cellThree.Width = Unit.Pixel(300)
        row.Controls.Add(cellThree)
        Me.successTableGetSubscriptionDetails.Controls.Add(row)
    End Sub

    ''' <summary>
    ''' This function reads the Access Token File and stores the values of access token, expiry seconds, 
    ''' refresh token, last access token time and refresh token expiry time.
    ''' This funciton returns true, if access token file and all others attributes read successfully otherwise returns false.
    ''' </summary>
    ''' <returns>Return Boolean</returns>
    Private Function ReadAccessTokenFile() As Boolean
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
        Catch
            Return False
        End Try

        If (Me.accessToken Is Nothing) OrElse (Me.expirySeconds Is Nothing) OrElse (Me.refreshToken Is Nothing) OrElse (Me.accessTokenExpiryTime Is Nothing) OrElse (Me.refreshTokenExpiryTime Is Nothing) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function validates the expiry of the access token and refresh token, function compares the current time with the refresh token taken time, if current time is greater then returns INVALID_REFRESH_TOKEN
    ''' function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
    ''' funciton returns INVALID_ACCESS_TOKEN
    ''' otherwise returns VALID_ACCESS_TOKEN
    ''' </summary>
    ''' <returns>Returns String</returns>
    Private Function IsTokenValid() As String
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
    ''' <param name="type">Type as Integer</param>
    ''' <param name="panelParam">Panel Details</param>
    ''' <returns>Returns boolean</returns>
    Private Function GetAccessToken(ByVal type As Integer, ByVal panelParam As Panel) As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamWriter As StreamWriter = Nothing

        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim accessTokenRequest As WebRequest = Nothing
            If type = 1 Then
                accessTokenRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/access_token?client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=PAYMENT")
            ElseIf type = 2 Then
                accessTokenRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/access_token?grant_type=refresh_token&client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&refresh_token=" & Me.refreshToken)
            End If

            accessTokenRequest.Method = "GET"

            Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
            Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                Dim jsonAccessToken As String = accessTokenResponseStream.ReadToEnd()

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

                accessTokenResponseStream.Close()
                Return True
            End Using
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
    ''' This function is used to read access token file and validate the access token.
    ''' This function returns true if access token is valid, or else false is returned.
    ''' </summary>
    ''' <param name="panelParam">Panel Details</param>
    ''' <returns>Returns Boolean</returns>
    Private Function ReadAndGetAccessToken(ByVal panelParam As Panel) As Boolean
        Dim result As Boolean = True
        If Me.ReadAccessTokenFile() = False Then
            result = Me.GetAccessToken(1, panelParam)
        Else
            Dim tokenValidity As String = Me.IsTokenValid()
            If tokenValidity.CompareTo("REFRESH_TOKEN") = 0 Then
                result = Me.GetAccessToken(2, panelParam)
            ElseIf tokenValidity.Equals("INVALID_ACCESS_TOKEN") Then
                result = Me.GetAccessToken(1, panelParam)
            End If
        End If

        Return result
    End Function

#Region "Data Structures"

    ''' <summary>
    ''' This class defines Access Token Response
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
    ''' This class defines Refund Response.
    ''' </summary>
    Public Class RefundResponse
        ''' <summary>
        ''' Gets or sets Transaction Id.
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
        ''' Gets or sets Transaction Status.
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
        ''' Gets or sets Is Success.
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
        ''' Gets or sets Version.
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
        ''' Gets or sets OriginalPurchaseAmount.
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
        ''' Gets or sets CommitConfirmationId.
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
    ''' This class defines Subscription Status Response
    ''' </summary>
    Public Class SubscriptionStatusResponse
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
        ''' Gets or sets Consumer Id
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
        ''' Gets or sets Subscription Period
        ''' </summary>
        Public Property SubscriptionPeriod() As String
            Get
                Return m_SubscriptionPeriod
            End Get
            Set(ByVal value As String)
                m_SubscriptionPeriod = Value
            End Set
        End Property
        Private m_SubscriptionPeriod As String

        ''' <summary>
        ''' Gets or sets Period Amount
        ''' </summary>
        Public Property SubscriptionPeriodAmount() As String
            Get
                Return m_SubscriptionPeriodAmount
            End Get
            Set(ByVal value As String)
                m_SubscriptionPeriodAmount = Value
            End Set
        End Property
        Private m_SubscriptionPeriodAmount As String

        ''' <summary>
        ''' Gets or sets Recurrences
        ''' </summary>
        Public Property SubscriptionRecurrences() As String
            Get
                Return m_SubscriptionRecurrences
            End Get
            Set(ByVal value As String)
                m_SubscriptionRecurrences = Value
            End Set
        End Property
        Private m_SubscriptionRecurrences As String

        ''' <summary>
        ''' Gets or sets Merchante Subscription Id
        ''' </summary>
        Public Property MerchantSubscriptionId() As String
            Get
                Return m_MerchantSubscriptionId
            End Get
            Set(ByVal value As String)
                m_MerchantSubscriptionId = Value
            End Set
        End Property
        Private m_MerchantSubscriptionId As String

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
        ''' Gets or sets Is Auto Committed
        ''' </summary>
        Public Property IsAutoCommitted() As String
            Get
                Return m_IsAutoCommitted
            End Get
            Set(ByVal value As String)
                m_IsAutoCommitted = Value
            End Set
        End Property
        Private m_IsAutoCommitted As String

        ''' <summary>
        ''' Gets or sets Subscription Id
        ''' </summary>
        Public Property SubscriptionId() As String
            Get
                Return m_SubscriptionId
            End Get
            Set(ByVal value As String)
                m_SubscriptionId = Value
            End Set
        End Property
        Private m_SubscriptionId As String

        ''' <summary>
        ''' Gets or sets Subscription Status
        ''' </summary>
        Public Property SubscriptionStatus() As String
            Get
                Return m_SubscriptionStatus
            End Get
            Set(ByVal value As String)
                m_SubscriptionStatus = Value
            End Set
        End Property
        Private m_SubscriptionStatus As String

        ''' <summary>
        ''' Gets or sets Subscription Type
        ''' </summary>
        Public Property SubscriptionType() As String
            Get
                Return m_SubscriptionType
            End Get
            Set(ByVal value As String)
                m_SubscriptionType = Value
            End Set
        End Property
        Private m_SubscriptionType As String

        ''' <summary>
        ''' Gets or sets Original Transaction Id
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

    ''' <summary>
    ''' This class defines Subscription Details Response
    ''' </summary>
    Public Class SubscriptionDetailsResponse
        ''' <summary>
        ''' Gets or sets Is Active Subscription
        ''' </summary>
        Public Property IsActiveSubscription() As String
            Get
                Return m_IsActiveSubscription
            End Get
            Set(ByVal value As String)
                m_IsActiveSubscription = Value
            End Set
        End Property
        Private m_IsActiveSubscription As String

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
        ''' Gets or sets Creation Date
        ''' </summary>
        Public Property CreationDate() As String
            Get
                Return m_CreationDate
            End Get
            Set(ByVal value As String)
                m_CreationDate = Value
            End Set
        End Property
        Private m_CreationDate As String

        ''' <summary>
        ''' Gets or sets Current Start Date
        ''' </summary>
        Public Property CurrentStartDate() As String
            Get
                Return m_CurrentStartDate
            End Get
            Set(ByVal value As String)
                m_CurrentStartDate = Value
            End Set
        End Property
        Private m_CurrentStartDate As String

        ''' <summary>
        ''' Gets or sets Current End Date
        ''' </summary>
        Public Property CurrentEndDate() As String
            Get
                Return m_CurrentEndDate
            End Get
            Set(ByVal value As String)
                m_CurrentEndDate = Value
            End Set
        End Property
        Private m_CurrentEndDate As String

        ''' <summary>
        ''' Gets or sets Gross Amount
        ''' </summary>
        Public Property GrossAmount() As String
            Get
                Return m_GrossAmount
            End Get
            Set(ByVal value As String)
                m_GrossAmount = Value
            End Set
        End Property
        Private m_GrossAmount As String

        ''' <summary>
        ''' Gets or sets SubscriptionRecurrences
        ''' </summary>
        Public Property Recurrences() As String
            Get
                Return m_Recurrences
            End Get
            Set(ByVal value As String)
                m_Recurrences = Value
            End Set
        End Property
        Private m_Recurrences As String

        ''' <summary>
        ''' Gets or sets SubscriptionRemaining
        ''' </summary>
        ' public string SubscriptionRemaining { get; set; }

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
        ''' Gets or sets Status
        ''' </summary>
        Public Property Status() As String
            Get
                Return m_Status
            End Get
            Set(ByVal value As String)
                m_Status = Value
            End Set
        End Property
        Private m_Status As String

        ''' <summary>
        ''' Gets or sets RecurrencesLeft
        ''' </summary>
        Public Property RecurrencesLeft() As String
            Get
                Return m_RecurrencesLeft
            End Get
            Set(ByVal value As String)
                m_RecurrencesLeft = Value
            End Set
        End Property
        Private m_RecurrencesLeft As String

    End Class
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
        target = value
        Return value
    End Function

#End Region
End Class
