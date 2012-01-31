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
    Private shortCode As String, accessTokenFilePath As String, FQDN As String, oauthFlow As String, subsDetailsFile As String, subsRefundFile As String
    Private api_key As String, secret_key As String, auth_code As String, access_token As String, authorize_redirect_uri As String, scope As String, _
     expiryMilliSeconds As String, refresh_token As String, lastTokenTakenTime As String, refreshTokenExpiryTime As String
    Private successTable As Table, failureTable As Table
    Private successTableGetTransaction As Table, failureTableGetTransaction As Table, successTableGetSubscriptionDetails As Table
    Private amount As String
    Private category As Int32
    Private channel As String, description As String, merchantTransactionId As String, merchantProductId As String, merchantApplicationId As String
    Private merchantRedirectURI As Uri
    Private MerchantSubscriptionIdList As String, SubscriptionRecurringPeriod As String
    Private SubscriptionRecurringNumber As String, SubscriptionRecurringPeriodAmount As String
    Private IsPurchaseOnNoActiveSubscription As String
    Private transactionTime As DateTime
    Private transactionTimeString As String
    Private payLoadStringFromRequest As String
    Private signedPayload As String, signedSignature As String
    Private notaryURL As String
    'Private consumerId As String
    Private subsDetailsCountToDisplay As Integer = 0
    Private subsDetailsList As New List(Of KeyValuePair(Of String, String))()
    Private subsRefundList As New List(Of KeyValuePair(Of String, String))()
    Private LatestFive As Boolean = True
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        subsRefundSuccessTable.Visible = False
        subsDetailsSuccessTable.Visible = False
        subscriptionSuccessTable.Visible = False
        subsGetStatusTable.Visible = False
        Dim currentServerTime As DateTime = DateTime.UtcNow
        lblServerTime.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
        If ConfigurationManager.AppSettings("FQDN") Is Nothing Then
            drawPanelForFailure(newSubscriptionPanel, "FQDN is not defined in configuration file")
            Return
        End If
        FQDN = ConfigurationManager.AppSettings("FQDN").ToString()
        If ConfigurationManager.AppSettings("api_key") Is Nothing Then
            drawPanelForFailure(newSubscriptionPanel, "api_key is not defined in configuration file")
            Return
        End If
        api_key = ConfigurationManager.AppSettings("api_key").ToString()
        If ConfigurationManager.AppSettings("secret_key") Is Nothing Then
            drawPanelForFailure(newSubscriptionPanel, "secret_key is not defined in configuration file")
            Return
        End If
        secret_key = ConfigurationManager.AppSettings("secret_key").ToString()
        If ConfigurationManager.AppSettings("AccessTokenFilePath") IsNot Nothing Then
            accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        Else
            accessTokenFilePath = "~\PayApp2AccessToken.txt"
        End If
        If ConfigurationManager.AppSettings("subsDetailsFile") IsNot Nothing Then
            subsDetailsFile = ConfigurationManager.AppSettings("subsDetailsFile")
        Else
            subsDetailsFile = "~\subsDetailsFile.txt"
        End If
        If ConfigurationManager.AppSettings("subsRefundFile") IsNot Nothing Then
            subsRefundFile = ConfigurationManager.AppSettings("subsRefundFile")
        Else
            subsRefundFile = "~\subsRefundFile.txt"
        End If
        If ConfigurationManager.AppSettings("subsDetailsCountToDisplay") IsNot Nothing Then
            subsDetailsCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("subsDetailsCountToDisplay"))
        Else
            subsDetailsCountToDisplay = 5
        End If
        If ConfigurationManager.AppSettings("scope") Is Nothing Then
            scope = "PAYMENT"
        Else
            scope = ConfigurationManager.AppSettings("scope").ToString()
        End If
        'If ConfigurationManager.AppSettings("consumerId") Is Nothing Then
        'drawPanelForFailure(newSubscriptionPanel, "consumerId is not defined in configuration file")
        'Return
        'End If
        'consumerId = ConfigurationManager.AppSettings("consumerId").ToString()
        If ConfigurationManager.AppSettings("notaryURL") Is Nothing Then
            drawPanelForFailure(newSubscriptionPanel, "notaryURL is not defined in configuration file")
            Return
        End If
        If ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl") Is Nothing Then
            drawPanelForFailure(newSubscriptionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file")
            Return
        End If
        If ConfigurationManager.AppSettings("DisableLatestFive") IsNot Nothing Then
            LatestFive = False
        End If
        merchantRedirectURI = New Uri(ConfigurationManager.AppSettings("MerchantPaymentRedirectUrl"))
        notaryURL = ConfigurationManager.AppSettings("notaryURL")
        If (Request("ret_signed_payload") IsNot Nothing) AndAlso (Request("ret_signature") IsNot Nothing) Then
            signedPayload = Request("ret_signed_payload").ToString()
            signedSignature = Request("ret_signature").ToString()
            Session("sub_signedPayLoad") = signedPayload.ToString()
            Session("sub_signedSignature") = signedSignature.ToString()
            processNotaryResponse()
        ElseIf (Request("SubscriptionAuthCode") IsNot Nothing) AndAlso (Session("sub_merTranId") IsNot Nothing) Then
            processCreateTransactionResponse()
        ElseIf (Request("shown_notary") IsNot Nothing) AndAlso (Session("sub_processNotary") IsNot Nothing) Then
            Session("sub_processNotary") = Nothing
            GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: " & Session("sub_tempMerTranId").ToString()
            GetSubscriptionAuthCode.Text = "Auth Code: " & Session("sub_TranAuthCode").ToString()
        End If
        subsDetailsTable.Controls.Clear()
        drawSubsDetailsSection(False)
        subsRefundTable.Controls.Clear()
        drawSubsRefundSection(False)
        Return
    End Sub
    Public Sub addRowToSubsDetailsSection(ByVal subscription As String, ByVal merchantsubscription As String)
        Dim rowOne As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.CssClass = "cell"
        cellOne.Width = Unit.Pixel(150)
        'cellOne.Text = transaction.ToString();
        Dim rbutton As New RadioButton()
        rbutton.Text = subscription.ToString()
        rbutton.GroupName = "SubsDetailsSection"
        rbutton.ID = subscription.ToString()
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
        CellThree.Text = merchantsubscription.ToString()
        rowOne.Controls.Add(CellThree)

        Dim CellFour As New TableCell()
        CellFour.CssClass = "cell"
        rowOne.Controls.Add(CellFour)

        subsDetailsTable.Controls.Add(rowOne)
    End Sub
    Public Sub addRowToSubsRefundSection(ByVal subscription As String, ByVal merchantsubscription As String)
        Dim rowOne As New TableRow()
        Dim cellOne As New TableCell()
        cellOne.HorizontalAlign = HorizontalAlign.Right
        cellOne.CssClass = "cell"
        cellOne.Width = Unit.Pixel(150)
        'cellOne.Text = transaction.ToString();
        Dim rbutton As New RadioButton()
        rbutton.Text = subscription.ToString()
        rbutton.GroupName = "SubsRefundSection"
        rbutton.ID = subscription.ToString()
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
        CellThree.Text = merchantsubscription.ToString()
        rowOne.Controls.Add(CellThree)

        Dim CellFour As New TableCell()
        CellFour.CssClass = "cell"
        rowOne.Controls.Add(CellFour)

        subsRefundTable.Controls.Add(rowOne)
    End Sub
    Public Sub drawSubsDetailsSection(ByVal onlyRow As Boolean)
        Try
            If onlyRow = False Then
                Dim headingRow As New TableRow()
                Dim headingCellOne As New TableCell()
                headingCellOne.HorizontalAlign = HorizontalAlign.Right
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
            resetSubsDetailsList()
            getSubsDetailsFromFile()

            Dim tempCountToDisplay As Integer = 1
            While (tempCountToDisplay <= subsDetailsCountToDisplay) AndAlso (tempCountToDisplay <= subsDetailsList.Count) AndAlso (subsDetailsList.Count > 0)
                addRowToSubsDetailsSection(subsDetailsList(tempCountToDisplay - 1).Key, subsDetailsList(tempCountToDisplay - 1).Value)
                tempCountToDisplay += 1
                'addButtonToRefundSection("Refund Transaction");
            End While
        Catch ex As Exception
            drawPanelForFailure(subsDetailsPanel, ex.ToString())
        End Try
    End Sub
    Public Sub drawSubsRefundSection(ByVal onlyRow As Boolean)
        Try
            If onlyRow = False Then
                Dim headingRow As New TableRow()
                Dim headingCellOne As New TableCell()
                headingCellOne.HorizontalAlign = HorizontalAlign.Right
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
            resetSubsRefundList()
            getSubsRefundFromFile()

            Dim tempCountToDisplay As Integer = 1
            While (tempCountToDisplay <= subsDetailsCountToDisplay) AndAlso (tempCountToDisplay <= subsRefundList.Count) AndAlso (subsRefundList.Count > 0)
                addRowToSubsRefundSection(subsRefundList(tempCountToDisplay - 1).Key, subsRefundList(tempCountToDisplay - 1).Value)
                tempCountToDisplay += 1
            End While
        Catch ex As Exception
            drawPanelForFailure(subsRefundPanel, ex.ToString())
        End Try
    End Sub
    Public Function getValueOfKeyFromRefund(ByVal key As String) As String
        Dim tempCount As Integer = 0
        While tempCount < subsDetailsList.Count
            If subsRefundList(tempCount).Key.CompareTo(key) = 0 Then
                Return subsRefundList(tempCount).Value
            End If
            tempCount += 1
        End While
        Return "null"
    End Function
    Public Function getValueOfKey(ByVal key As String) As String
        Dim tempCount As Integer = 0
        While tempCount < subsDetailsList.Count
            If subsDetailsList(tempCount).Key.CompareTo(key) = 0 Then
                Return subsDetailsList(tempCount).Value
            End If
            tempCount += 1
        End While
        Return "null"
    End Function
    Public Sub resetSubsRefundList()
        subsRefundList.RemoveRange(0, subsRefundList.Count)
    End Sub
    Public Sub resetSubsDetailsList()
        subsDetailsList.RemoveRange(0, subsDetailsList.Count)
    End Sub
    Public Sub getSubsDetailsFromFile()
        ' Read the refund file for the list of transactions and store locally 

        Dim file As New FileStream(Request.MapPath(subsDetailsFile), FileMode.Open, FileAccess.Read)
        Dim sr As New StreamReader(file)
        Dim line As String

        While ((InlineAssignHelper(line, sr.ReadLine())) IsNot Nothing)
            Dim subsDetailsKeys As String() = Regex.Split(line, ":-:")
            If subsDetailsKeys(0) IsNot Nothing AndAlso subsDetailsKeys(1) IsNot Nothing Then
                subsDetailsList.Add(New KeyValuePair(Of String, String)(subsDetailsKeys(0), subsDetailsKeys(1)))
            End If
        End While
        sr.Close()
        file.Close()
        subsDetailsList.Reverse(0, subsDetailsList.Count)
    End Sub
    Public Sub getSubsRefundFromFile()
        ' Read the refund file for the list of transactions and store locally 

        Dim file As New FileStream(Request.MapPath(subsRefundFile), FileMode.Open, FileAccess.Read)
        Dim sr As New StreamReader(file)
        Dim line As String

        While ((InlineAssignHelper(line, sr.ReadLine())) IsNot Nothing)
            Dim subsRefundKeys As String() = Regex.Split(line, ":-:")
            If subsRefundKeys(0) IsNot Nothing AndAlso subsRefundKeys(1) IsNot Nothing Then
                subsRefundList.Add(New KeyValuePair(Of String, String)(subsRefundKeys(0), subsRefundKeys(1)))
            End If
        End While
        sr.Close()
        file.Close()
        subsRefundList.Reverse(0, subsRefundList.Count)
    End Sub
    Public Sub updatesubsRefundListToFile()
        If subsRefundList.Count <> 0 Then
            subsRefundList.Reverse(0, subsRefundList.Count)
        End If
        Using sr As StreamWriter = File.CreateText(Request.MapPath(subsRefundFile))
            Dim tempCount As Integer = 0
            While tempCount < subsRefundList.Count
                Dim lineToWrite As String = subsRefundList(tempCount).Key & ":-:" & subsRefundList(tempCount).Value
                sr.WriteLine(lineToWrite)
                tempCount += 1
            End While
            sr.Close()
        End Using
    End Sub
    Public Sub updatesubsDetailsListToFile()
        If subsDetailsList.Count <> 0 Then
            subsDetailsList.Reverse(0, subsDetailsList.Count)
        End If
        Using sr As StreamWriter = File.CreateText(Request.MapPath(subsDetailsFile))
            Dim tempCount As Integer = 0
            While tempCount < subsDetailsList.Count
                Dim lineToWrite As String = subsDetailsList(tempCount).Key & ":-:" & subsDetailsList(tempCount).Value
                sr.WriteLine(lineToWrite)
                tempCount += 1
            End While
            sr.Close()
        End Using
    End Sub
    Public Function checkItemInSubsRefundFile(ByVal transactionid As String, ByVal merchantTransactionId As String) As Boolean
        Dim line As String
        Dim lineToFind As String = transactionid & ":-:" & merchantTransactionId
        Dim file As New System.IO.StreamReader(Request.MapPath(subsRefundFile))
        While (InlineAssignHelper(line, file.ReadLine())) IsNot Nothing
            If line.CompareTo(lineToFind) = 0 Then
                file.Close()
                Return True
            End If
        End While
        file.Close()
        Return False
    End Function
    Public Function checkItemInSubsDetailsFile(ByVal transactionid As String, ByVal merchantTransactionId As String) As Boolean
        Dim line As String
        Dim lineToFind As String = transactionid & ":-:" & merchantTransactionId
        Dim file As New System.IO.StreamReader(Request.MapPath(subsDetailsFile))
        While (InlineAssignHelper(line, file.ReadLine())) IsNot Nothing
            If line.CompareTo(lineToFind) = 0 Then
                file.Close()
                Return True
            End If
        End While
        file.Close()
        Return False
    End Function
    Public Sub writeSubsRefundToFile(ByVal transactionid As String, ByVal merchantTransactionId As String)
        ' Read the refund file for the list of transactions and store locally 

        'FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        'StreamWriter sr = new StreamWriter(file);
        'DateTime junkTime = DateTime.UtcNow;
        'string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        Using appendContent As StreamWriter = File.AppendText(Request.MapPath(subsRefundFile))
            Dim line As String = transactionid & ":-:" & merchantTransactionId
            appendContent.WriteLine(line)
            appendContent.Flush()
            'file.Close();
            appendContent.Close()
        End Using
    End Sub

    Public Sub writeSubsDetailsToFile(ByVal transactionid As String, ByVal merchantTransactionId As String)
        ' Read the refund file for the list of transactions and store locally 

        'FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        'StreamWriter sr = new StreamWriter(file);
        'DateTime junkTime = DateTime.UtcNow;
        'string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        Using appendContent As StreamWriter = File.AppendText(Request.MapPath(subsDetailsFile))
            Dim line As String = transactionid & ":-:" & merchantTransactionId
            appendContent.WriteLine(line)
            appendContent.Flush()
            'file.Close();
            appendContent.Close()
        End Using
    End Sub

    Private Sub readTransactionParametersFromConfigurationFile()
        transactionTime = DateTime.UtcNow
        transactionTimeString = [String].Format("{0:dddMMMddyyyyHHmmss}", transactionTime)
        If Radio_SubscriptionProductType.SelectedIndex = 0 Then
            amount = "1.99"
        ElseIf Radio_SubscriptionProductType.SelectedIndex = 1 Then
            amount = "3.99"
        End If
        Session("sub_tranType") = Radio_SubscriptionProductType.SelectedIndex.ToString()
        If ConfigurationManager.AppSettings("Category") Is Nothing Then
            drawPanelForFailure(newSubscriptionPanel, "Category is not defined in configuration file")
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
        Session("sub_merTranId") = merchantTransactionId.ToString()
        merchantProductId = "ProdId" & transactionTimeString
        merchantApplicationId = "MerAppId" & transactionTimeString
        MerchantSubscriptionIdList = "MSIList" & transactionTimeString
        Session("MerchantSubscriptionIdList") = MerchantSubscriptionIdList.ToString()
        IsPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings("IsPurchaseOnNoActiveSubscription")
        If ConfigurationManager.AppSettings("IsPurchaseOnNoActiveSubscription") Is Nothing Then
            IsPurchaseOnNoActiveSubscription = "false"
        Else
            IsPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings("IsPurchaseOnNoActiveSubscription")
        End If
        If ConfigurationManager.AppSettings("SubscriptionRecurringNumber") Is Nothing Then
            SubscriptionRecurringNumber = "99999"
        Else
            SubscriptionRecurringNumber = ConfigurationManager.AppSettings("SubscriptionRecurringNumber")
        End If
        If ConfigurationManager.AppSettings("SubscriptionRecurringPeriod") Is Nothing Then
            SubscriptionRecurringPeriod = "MONTHLY"
        Else
            SubscriptionRecurringPeriod = ConfigurationManager.AppSettings("SubscriptionRecurringPeriod")
        End If
        If ConfigurationManager.AppSettings("SubscriptionRecurringPeriodAmount") Is Nothing Then
            SubscriptionRecurringPeriodAmount = "1"
        Else
            SubscriptionRecurringPeriodAmount = ConfigurationManager.AppSettings("SubscriptionRecurringPeriodAmount")
        End If
    End Sub


    Private Sub processNotaryResponse()
        If Session("sub_tranType") IsNot Nothing Then
            Radio_SubscriptionProductType.SelectedIndex = Convert.ToInt32(Session("sub_tranType").ToString())
            Session("sub_tranType") = Nothing
        End If
        Response.Redirect(FQDN & "/Commerce/Payment/Rest/2/Subscriptions?clientid=" & api_key.ToString() & "&SignedPaymentDetail=" & signedPayload.ToString() & "&Signature=" & signedSignature.ToString())

    End Sub

    Public Sub processCreateTransactionResponse()
        lblsubscode.Text = Request("SubscriptionAuthCode").ToString()
        lblsubsid.Text = Session("sub_merTranId").ToString()
        subscriptionSuccessTable.Visible = True
        GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: " & Session("sub_merTranId").ToString()
        GetSubscriptionAuthCode.Text = "Auth Code: " & Request("SubscriptionAuthCode").ToString()
        GetSubscriptionID.Text = "Subscription ID: "
        Session("sub_tempMerTranId") = Session("sub_merTranId").ToString()
        Session("sub_merTranId") = Nothing
        Session("sub_TranAuthCode") = Request("SubscriptionAuthCode").ToString()
        Return
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

    Private Sub drawPanelForGetTransactionSuccess(ByVal panelParam As Panel)
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

    Private Sub addRowToGetTransactionSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
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

    Private Sub drawPanelForGetSubscriptionDetailsSuccess(ByVal panelParam As Panel)
        successTableGetSubscriptionDetails = New Table()
        successTableGetSubscriptionDetails.Font.Name = "Sans-serif"
        successTableGetSubscriptionDetails.Font.Size = 8
        successTableGetSubscriptionDetails.Width = Unit.Pixel(650)
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
        successTableGetSubscriptionDetails.Controls.Add(rowOne)
        panelParam.Controls.Add(successTableGetSubscriptionDetails)
    End Sub
    'This function adds row to the success table 

    Private Sub addRowToGetSubscriptionDetailsSuccessPanel(ByVal panelParam As Panel, ByVal attribute As String, ByVal value As String)
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
        successTableGetSubscriptionDetails.Controls.Add(row)
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
            Set(ByVal value As String)
                m_TransactionId = Value
            End Set
        End Property
        Private m_TransactionId As String
        Public Property TransactionStatus() As String
            Get
                Return m_TransactionStatus
            End Get
            Set(ByVal value As String)
                m_TransactionStatus = Value
            End Set
        End Property
        Private m_TransactionStatus As String
        Public Property IsSuccess() As String
            Get
                Return m_IsSuccess
            End Get
            Set(ByVal value As String)
                m_IsSuccess = Value
            End Set
        End Property
        Private m_IsSuccess As String
        Public Property Version() As String
            Get
                Return m_Version
            End Get
            Set(ByVal value As String)
                m_Version = Value
            End Set
        End Property
        Private m_Version As String
    End Class
    Public Class subscriptionStatusResponse
        Public Property Currency() As String
            Get
                Return m_Currency
            End Get
            Set(ByVal value As String)
                m_Currency = Value
            End Set
        End Property
        Private m_Currency As String
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
        Public Property MerchantTransactionId() As String
            Get
                Return m_MerchantTransactionId
            End Get
            Set(ByVal value As String)
                m_MerchantTransactionId = Value
            End Set
        End Property
        Private m_MerchantTransactionId As String
        Public Property ConsumerId() As String
            Get
                Return m_ConsumerId
            End Get
            Set(ByVal value As String)
                m_ConsumerId = Value
            End Set
        End Property
        Private m_ConsumerId As String
        Public Property Description() As String
            Get
                Return m_Description
            End Get
            Set(ByVal value As String)
                m_Description = Value
            End Set
        End Property
        Private m_Description As String
        Public Property Amount() As String
            Get
                Return m_Amount
            End Get
            Set(ByVal value As String)
                m_Amount = Value
            End Set
        End Property
        Private m_Amount As String
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
        Public Property MerchantApplicationId() As String
            Get
                Return m_MerchantApplicationId
            End Get
            Set(ByVal value As String)
                m_MerchantApplicationId = Value
            End Set
        End Property
        Private m_MerchantApplicationId As String
        Public Property Channel() As String
            Get
                Return m_Channel
            End Get
            Set(ByVal value As String)
                m_Channel = Value
            End Set
        End Property
        Private m_Channel As String
        Public Property SubscriptionPeriod() As String
            Get
                Return m_SubscriptionPeriod
            End Get
            Set(ByVal value As String)
                m_SubscriptionPeriod = Value
            End Set
        End Property
        Private m_SubscriptionPeriod As String
        Public Property PeriodAmount() As String
            Get
                Return m_PeriodAmount
            End Get
            Set(ByVal value As String)
                m_PeriodAmount = Value
            End Set
        End Property
        Private m_PeriodAmount As String
        Public Property Recurrences() As String
            Get
                Return m_Recurrences
            End Get
            Set(ByVal value As String)
                m_Recurrences = Value
            End Set
        End Property
        Private m_Recurrences As String
        Public Property MerchantSubscriptionId() As String
            Get
                Return m_MerchantSubscriptionId
            End Get
            Set(ByVal value As String)
                m_MerchantSubscriptionId = Value
            End Set
        End Property
        Private m_MerchantSubscriptionId As String
        Public Property MerchantIdentifier() As String
            Get
                Return m_MerchantIdentifier
            End Get
            Set(ByVal value As String)
                m_MerchantIdentifier = Value
            End Set
        End Property
        Private m_MerchantIdentifier As String
        Public Property IsAutoCommitted() As String
            Get
                Return m_IsAutoCommitted
            End Get
            Set(ByVal value As String)
                m_IsAutoCommitted = Value
            End Set
        End Property
        Private m_IsAutoCommitted As String
        Public Property SubscriptionId() As String
            Get
                Return m_SubscriptionId
            End Get
            Set(ByVal value As String)
                m_SubscriptionId = Value
            End Set
        End Property
        Private m_SubscriptionId As String
        Public Property SubscriptionStatus() As String
            Get
                Return m_SubscriptionStatus
            End Get
            Set(ByVal value As String)
                m_SubscriptionStatus = Value
            End Set
        End Property
        Private m_SubscriptionStatus As String
        Public Property SubscriptionType() As String
            Get
                Return m_SubscriptionType
            End Get
            Set(ByVal value As String)
                m_SubscriptionType = Value
            End Set
        End Property
        Private m_SubscriptionType As String
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

    Public Class subscriptionDetailsResponse
        Public Property IsActiveSubscription() As String
            Get
                Return m_IsActiveSubscription
            End Get
            Set(ByVal value As String)
                m_IsActiveSubscription = Value
            End Set
        End Property
        Private m_IsActiveSubscription As String
        Public Property Currency() As String
            Get
                Return m_Currency
            End Get
            Set(ByVal value As String)
                m_Currency = Value
            End Set
        End Property
        Private m_Currency As String
        Public Property CreationDate() As String
            Get
                Return m_CreationDate
            End Get
            Set(ByVal value As String)
                m_CreationDate = Value
            End Set
        End Property
        Private m_CreationDate As String
        Public Property CurrentStartDate() As String
            Get
                Return m_CurrentStartDate
            End Get
            Set(ByVal value As String)
                m_CurrentStartDate = Value
            End Set
        End Property
        Private m_CurrentStartDate As String
        Public Property CurrentEndDate() As String
            Get
                Return m_CurrentEndDate
            End Get
            Set(ByVal value As String)
                m_CurrentEndDate = Value
            End Set
        End Property
        Private m_CurrentEndDate As String
        Public Property GrossAmount() As String
            Get
                Return m_GrossAmount
            End Get
            Set(ByVal value As String)
                m_GrossAmount = Value
            End Set
        End Property
        Private m_GrossAmount As String
        Public Property Recurrences() As String
            Get
                Return m_Recurrences
            End Get
            Set(ByVal value As String)
                m_Recurrences = Value
            End Set
        End Property
        Private m_Recurrences As String
        Public Property RecurrencesLeft() As String
            Get
                Return m_RecurrencesLeft
            End Get
            Set(ByVal value As String)
                m_RecurrencesLeft = Value
            End Set
        End Property
        Private m_RecurrencesLeft As String
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
    '"\",\"MerchantApplicationId\":\"" + merchantApplicationId.ToString() +
    'for amount do we need to add quotes, it worked for transaction with quotes
    Protected Sub newSubscriptionButton_Click1(ByVal sender As Object, ByVal e As EventArgs)
        readTransactionParametersFromConfigurationFile()
        Dim payLoadString As String = "{""Amount"":" & amount.ToString() & ",""Category"":" & category.ToString() & ",""Channel"":""" & channel.ToString() & """,""Description"":""" & description.ToString() & """,""MerchantTransactionId"":""" & merchantTransactionId.ToString() & """,""MerchantProductId"":""" & merchantProductId.ToString() & """,""MerchantPaymentRedirectUrl"":""" & merchantRedirectURI.ToString() & """,""MerchantSubscriptionIdList"":""" & MerchantSubscriptionIdList.ToString() & """,""IsPurchaseOnNoActiveSubscription"":""" & IsPurchaseOnNoActiveSubscription.ToString() & """,""SubscriptionRecurringNumber"":" & SubscriptionRecurringNumber.ToString() & ",""SubscriptionRecurringPeriod"":""" & SubscriptionRecurringPeriod.ToString() & """,""SubscriptionRecurringPeriodAmount"":" & SubscriptionRecurringPeriodAmount.ToString() & "}"
        Session("sub_payloadData") = payLoadString.ToString()
        Response.Redirect(notaryURL.ToString() & "?request_to_sign=" & payLoadString.ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&api_key=" & api_key.ToString() & "&secret_key=" & secret_key.ToString())
    End Sub
    Protected Sub getSubscriptionButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim resourcePathString As String = ""
        Try
            Dim keyValue As String = ""
            If Radio_SubscriptionStatus.SelectedIndex = 0 Then
                keyValue = GetSubscriptionMerchantSubsID.Text.ToString().Replace("Merchant Sub. ID: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
                resourcePathString = "" & FQDN & "/Commerce/Payment/Rest/2/Subscriptions/MerchantTransactionId/" & keyValue.ToString()
            End If
            If Radio_SubscriptionStatus.SelectedIndex = 1 Then
                keyValue = GetSubscriptionAuthCode.Text.ToString().Replace("Auth Code: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
                resourcePathString = "" & FQDN & "/Commerce/Payment/Rest/2/Subscriptions/TransactionAuthCode/" & keyValue.ToString()
            End If
            If Radio_SubscriptionStatus.SelectedIndex = 2 Then
                keyValue = GetSubscriptionID.Text.ToString().Replace("Subscription ID: ", "")
                If keyValue.Length = 0 Then
                    Return
                End If
                resourcePathString = "" & FQDN & "/Commerce/Payment/Rest/2/Subscriptions/SubscriptionId/" & keyValue.ToString()
            End If
            If readAndGetAccessToken(newSubscriptionPanel) = True Then
                If access_token Is Nothing OrElse access_token.Length <= 0 Then
                    Return
                End If
                'String getTransactionStatusResponseData;
                resourcePathString = resourcePathString & "?access_token=" & access_token.ToString()
                'HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/" + Session["MerchantSubscriptionIdList"].ToString() + "/Detail/" + consumerId.ToString() + "?access_token=" + access_token.ToString());
                Dim objRequest As HttpWebRequest = DirectCast(System.Net.WebRequest.Create(resourcePathString), HttpWebRequest)
                objRequest.Method = "GET"
                Dim getTransactionStatusResponseObject As HttpWebResponse = DirectCast(objRequest.GetResponse(), HttpWebResponse)
                Using getTransactionStatusResponseStream As New StreamReader(getTransactionStatusResponseObject.GetResponseStream())
                    Dim getTransactionStatusResponseData As [String] = getTransactionStatusResponseStream.ReadToEnd()
                    Dim deserializeJsonObject As New JavaScriptSerializer()
                    Dim deserializedJsonObj As subscriptionStatusResponse = DirectCast(deserializeJsonObject.Deserialize(getTransactionStatusResponseData, GetType(subscriptionStatusResponse)), subscriptionStatusResponse)
                    lblstatusMerSubsId.Text = deserializedJsonObj.MerchantSubscriptionId.ToString()
                    lblstatusSubsId.Text = deserializedJsonObj.SubscriptionId.ToString()
                    GetSubscriptionID.Text = "Subscription ID: " & deserializedJsonObj.SubscriptionId.ToString()
                    If checkItemInSubsDetailsFile(deserializedJsonObj.MerchantSubscriptionId.ToString(), deserializedJsonObj.ConsumerId.ToString()) = False Then
                        writeSubsDetailsToFile(deserializedJsonObj.MerchantSubscriptionId.ToString(), deserializedJsonObj.ConsumerId.ToString())
                    End If
                    If checkItemInSubsRefundFile(deserializedJsonObj.SubscriptionId.ToString(), deserializedJsonObj.MerchantSubscriptionId.ToString()) = False Then
                        writeSubsRefundToFile(deserializedJsonObj.SubscriptionId.ToString(), deserializedJsonObj.MerchantSubscriptionId.ToString())
                    End If
                    subsDetailsTable.Controls.Clear()
                    drawSubsDetailsSection(False)
                    subsRefundTable.Controls.Clear()
                    drawSubsRefundSection(False)
                    subsGetStatusTable.Visible = True
                    drawPanelForGetTransactionSuccess(getSubscriptionStatusPanel)
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionId", deserializedJsonObj.SubscriptionId.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionStatus", deserializedJsonObj.SubscriptionStatus.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionType", deserializedJsonObj.SubscriptionType.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantTransactionId", deserializedJsonObj.MerchantTransactionId.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Description", deserializedJsonObj.Description.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Amount", deserializedJsonObj.Amount.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Channel ", deserializedJsonObj.Channel.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionPeriod", deserializedJsonObj.SubscriptionPeriod.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "PeriodAmount", deserializedJsonObj.PeriodAmount.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Recurrences", deserializedJsonObj.Recurrences.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantSubscriptionId", deserializedJsonObj.MerchantSubscriptionId.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantIdentifier", deserializedJsonObj.MerchantIdentifier.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsAutoCommitted", deserializedJsonObj.IsAutoCommitted.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString())
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "OriginalTransactionId", deserializedJsonObj.OriginalTransactionId.ToString())
                    getTransactionStatusResponseStream.Close()
                End Using
            End If
        Catch ex As Exception
            drawPanelForFailure(getSubscriptionStatusPanel, ex.ToString())
        End Try
        '
        '        DateTime dummy = DateTime.UtcNow;
        '        string dummyString = String.Format("{0:dddMMMddyyyyHHmmss}", dummy);
        '        if (checkItemInSubsDetailsFile("Tran" + dummyString, "MerTran" + dummyString) == false)
        '        {
        '            writeSubsDetailsToFile("Tran" + dummyString, "MerTran" + dummyString);
        '        }
        '        subsDetailsTable.Controls.Clear();
        '        drawSubsDetailsSection(false);
        '

    End Sub
    Protected Sub viewNotaryButton_Click(ByVal sender As Object, ByVal e As EventArgs)
        If (Session("sub_payloadData") IsNot Nothing) AndAlso (Session("sub_signedPayLoad") IsNot Nothing) AndAlso (Session("sub_signedSignature") IsNot Nothing) Then
            Session("sub_processNotary") = "notary"
            Response.Redirect(notaryURL.ToString() & "?signed_payload=" & Session("sub_signedPayLoad").ToString() & "&goBackURL=" & merchantRedirectURI.ToString() & "&signed_signature=" & Session("sub_signedSignature").ToString() & "&signed_request=" & Session("sub_payloadData").ToString())
        End If
    End Sub

    Protected Sub btnGetSubscriptionDetails_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim merSubsID As String = ""
        Dim recordFound As Boolean = False
        Try
            If subsDetailsList.Count > 0 Then
                For Each subDetailsTableRow As Control In subsDetailsTable.Controls
                    If TypeOf subDetailsTableRow Is TableRow Then
                        For Each subDetailsTableRowCell As Control In subDetailsTableRow.Controls
                            If TypeOf subDetailsTableRowCell Is TableCell Then
                                For Each subDetailsTableCellControl As Control In subDetailsTableRowCell.Controls
                                    If (TypeOf subDetailsTableCellControl Is RadioButton) Then

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
                    If readAndGetAccessToken(subsDetailsPanel) = True Then
                        If access_token Is Nothing OrElse access_token.Length <= 0 Then
                            Return
                        End If
                        Dim consID As [String] = getValueOfKey(merSubsID)
                        If consID.CompareTo("null") = 0 Then
                            Return
                        End If
                        'drawPanelForFailure(getSubscriptionStatusPanel, merchantSubId.ToString());
                        'String getTransactionStatusResponseData;
                        Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/Commerce/Payment/Rest/2/Subscriptions/" & merSubsID.ToString() & "/Detail/" & consID.ToString() & "?access_token=" & access_token.ToString()), WebRequest)
                        'WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/" + merSubsID.ToString() + "/Detail/" +  consID.ToString() );
                        objRequest.Method = "GET"
                        objRequest.ContentType = "application/json"
                        Dim subsDetailsResponeObject As WebResponse = DirectCast(objRequest.GetResponse(), WebResponse)
                        Using subsDetailsResponseStream As New StreamReader(subsDetailsResponeObject.GetResponseStream())
                            Dim subsDetailsResponseData As [String] = subsDetailsResponseStream.ReadToEnd()
                            Dim deserializeJsonObject As New JavaScriptSerializer()
                            Dim deserializedJsonObj As subscriptionDetailsResponse = DirectCast(deserializeJsonObject.Deserialize(subsDetailsResponseData, GetType(subscriptionDetailsResponse)), subscriptionDetailsResponse)
                            subsDetailsSuccessTable.Visible = True
                            lblMerSubId.Text = merSubsID.ToString()
                            lblConsId.Text = consID.ToString()
                            drawPanelForGetSubscriptionDetailsSuccess(subsDetailsPanel)
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Recurrences", deserializedJsonObj.Recurrences.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentStartDate", deserializedJsonObj.CurrentStartDate.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsActiveSubscription", deserializedJsonObj.IsActiveSubscription.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "RecurrencesLeft", deserializedJsonObj.RecurrencesLeft.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "GrossAmount", deserializedJsonObj.GrossAmount.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CreationDate", deserializedJsonObj.CreationDate.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString())
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentEndDate", deserializedJsonObj.CurrentEndDate.ToString())
                            'subsDetailsList.RemoveAll(x => x.Key.Equals(merSubsID));
                            'updatesubsDetailsListToFile();
                            'resetSubsDetailsList();
                            'subsDetailsTable.Controls.Clear();
                            'drawSubsDetailsSection(false);
                            'GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: ";
                            'GetSubscriptionAuthCode.Text = "Auth Code: ";
                            'GetSubscriptionID.Text = "Subscription ID: ";
                            If LatestFive = False Then
                            End If
                            subsDetailsResponseStream.Close()
                        End Using
                        '
                        '                    subsDetailsSuccessTable.Visible = true;
                        '                    lblMerSubsId.Text = merSubsID.ToString();
                        '                    lblSubsIdDetails.Text = "SUCCESSFUL";
                        '                    lblMerTranId.Text = "true";
                        '                    if (LatestFive == false)
                        '                    {
                        '                        //subsDetailsList.RemoveAll(x => x.Key.Equals(merSubsID));
                        '                        //updatesubsDetailsListToFile();
                        '                        //resetSubsDetailsList();
                        '                        //subsDetailsTable.Controls.Clear();
                        '                        //drawSubsDetailsSection(false);
                        '                        GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: ";
                        '                        GetSubscriptionAuthCode.Text = "Auth Code: ";
                        '                        GetSubscriptionID.Text = "Subscription ID: ";
                        '                    }

                    End If
                End If
            End If
        Catch ex As Exception
            drawPanelForFailure(subsDetailsPanel, ex.ToString())
        End Try
    End Sub
    Protected Sub btnGetSubscriptionRefund_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim SubsID As String = ""
        Dim recordFound As Boolean = False
        Dim strReq As String = "{""RefundReasonCode"":1,""RefundReasonText"":""Customer was not happy""}"
        Dim dataLength As String = ""
        Try
            If subsRefundList.Count > 0 Then
                For Each subRefundTableRow As Control In subsRefundTable.Controls
                    If TypeOf subRefundTableRow Is TableRow Then
                        For Each subRefundTableRowCell As Control In subRefundTableRow.Controls
                            If TypeOf subRefundTableRowCell Is TableCell Then
                                For Each subRefundTableCellControl As Control In subRefundTableRowCell.Controls
                                    If (TypeOf subRefundTableCellControl Is RadioButton) Then

                                        If DirectCast(subRefundTableCellControl, RadioButton).Checked Then
                                            SubsID = DirectCast(subRefundTableCellControl, RadioButton).Text.ToString()
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
                    If readAndGetAccessToken(subsRefundPanel) = True Then
                        If access_token Is Nothing OrElse access_token.Length <= 0 Then
                            Return
                        End If
                        Dim merSubsID As [String] = getValueOfKeyFromRefund(SubsID)
                        If merSubsID.CompareTo("null") = 0 Then
                            Return
                        End If
                        'drawPanelForFailure(getSubscriptionStatusPanel, merchantSubId.ToString());
                        'String getTransactionStatusResponseData;
                        Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create("" & FQDN & "/Commerce/Payment/Rest/2/Transactions/" & SubsID.ToString() & "?access_token=" & access_token.ToString() & "&Action=refund"), WebRequest)
                        objRequest.Method = "PUT"
                        objRequest.ContentType = "application/json"
                        Dim encoding As New UTF8Encoding()
                        Dim postBytes As Byte() = encoding.GetBytes(strReq)
                        objRequest.ContentLength = postBytes.Length
                        Dim postStream As Stream = objRequest.GetRequestStream()
                        postStream.Write(postBytes, 0, postBytes.Length)
                        dataLength = postBytes.Length.ToString()
                        postStream.Close()
                        Dim subsRefundResponeObject As WebResponse = DirectCast(objRequest.GetResponse(), WebResponse)
                        Using subsRefundResponseStream As New StreamReader(subsRefundResponeObject.GetResponseStream())
                            Dim subsRefundResponseData As [String] = subsRefundResponseStream.ReadToEnd()
                            Dim deserializeJsonObject As New JavaScriptSerializer()
                            Dim deserializedJsonObj As RefundResponse = DirectCast(deserializeJsonObject.Deserialize(subsRefundResponseData, GetType(RefundResponse)), RefundResponse)
                            subsRefundSuccessTable.Visible = True
                            'drawPanelForGetSubscriptionDetailsSuccess(subsRefundPanel);
                            lbRefundTranID.Text = deserializedJsonObj.TransactionId.ToString()
                            lbRefundTranStatus.Text = deserializedJsonObj.TransactionStatus.ToString()
                            lbRefundIsSuccess.Text = deserializedJsonObj.IsSuccess.ToString()
                            lbRefundVersion.Text = deserializedJsonObj.Version.ToString()
                            If LatestFive = False Then
                                subsRefundList.RemoveAll(Function(x) x.Key.Equals(SubsID))
                                updatesubsRefundListToFile()
                                resetSubsRefundList()
                                subsRefundTable.Controls.Clear()
                                drawSubsRefundSection(False)
                                GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: "
                                GetSubscriptionAuthCode.Text = "Auth Code: "
                                GetSubscriptionID.Text = "Subscription ID: "
                            End If
                            subsRefundResponseStream.Close()
                        End Using
                    End If
                End If
            End If
        Catch ex As Exception
            drawPanelForFailure(subsRefundPanel, ex.ToString())
        End Try
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
        target = value
        Return value
    End Function
End Class
