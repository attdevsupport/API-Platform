
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Text;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

public partial class _Default : System.Web.UI.Page
{
    string shortCode, accessTokenFilePath, FQDN, oauthFlow, subsDetailsFile, subsRefundFile;
    string api_key, secret_key, auth_code, access_token, authorize_redirect_uri, scope, expiryMilliSeconds, refresh_token, lastTokenTakenTime, refreshTokenExpiryTime;
    Table successTable, failureTable;
    Table successTableGetTransaction, failureTableGetTransaction, successTableGetSubscriptionDetails;
    string amount;
    Int32 category;
    string channel, description, merchantTransactionId, merchantProductId, merchantApplicationId;
    Uri merchantRedirectURI;
    string MerchantSubscriptionIdList, SubscriptionRecurringPeriod;
    string SubscriptionRecurringNumber, SubscriptionRecurringPeriodAmount;
    string IsPurchaseOnNoActiveSubscription;
    DateTime transactionTime;
    string transactionTimeString;
    string payLoadStringFromRequest;
    string signedPayload, signedSignature;
    string notaryURL;
    //string consumerId;
    int subsDetailsCountToDisplay = 0;
    List<KeyValuePair<string, string>> subsDetailsList = new List<KeyValuePair<string, string>>();
    List<KeyValuePair<string, string>> subsRefundList = new List<KeyValuePair<string, string>>();
    bool LatestFive = true;
    protected void Page_Load(object sender, EventArgs e)
    {
        subsRefundSuccessTable.Visible = false;
	    subsDetailsSuccessTable.Visible = false;
        subscriptionSuccessTable.Visible = false;
        subsGetStatusTable.Visible = false;
        DateTime currentServerTime = DateTime.UtcNow;
        lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
        if (ConfigurationManager.AppSettings["FQDN"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "FQDN is not defined in configuration file");
            return;
        }
        FQDN = ConfigurationManager.AppSettings["FQDN"].ToString();
        if (ConfigurationManager.AppSettings["api_key"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "api_key is not defined in configuration file");
            return;
        }
        api_key = ConfigurationManager.AppSettings["api_key"].ToString();
        if (ConfigurationManager.AppSettings["secret_key"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "secret_key is not defined in configuration file");
            return;
        }
        secret_key = ConfigurationManager.AppSettings["secret_key"].ToString();
        if (ConfigurationManager.AppSettings["AccessTokenFilePath"] != null)
        {
            accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
        }
        else
        {
            accessTokenFilePath = "~\\PayApp2AccessToken.txt";
        }
        if (ConfigurationManager.AppSettings["subsDetailsFile"] != null)
        {
            subsDetailsFile = ConfigurationManager.AppSettings["subsDetailsFile"];
        }
        else
        {
            subsDetailsFile = "~\\subsDetailsFile.txt";
        }
        if (ConfigurationManager.AppSettings["subsRefundFile"] != null)
        {
            subsRefundFile = ConfigurationManager.AppSettings["subsRefundFile"];
        }
        else
        {
            subsRefundFile = "~\\subsRefundFile.txt";
        }
        if (ConfigurationManager.AppSettings["subsDetailsCountToDisplay"] != null)
        {
            subsDetailsCountToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["subsDetailsCountToDisplay"]);
        }
        else
        {
            subsDetailsCountToDisplay = 5;
        }
        if (ConfigurationManager.AppSettings["scope"] == null)
        {
            scope = "PAYMENT";
        }
        else
        {
            scope = ConfigurationManager.AppSettings["scope"].ToString();
        }
       /* if (ConfigurationManager.AppSettings["consumerId"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "consumerId is not defined in configuration file");
            return;
        }
        consumerId = ConfigurationManager.AppSettings["consumerId"].ToString(); */
        if (ConfigurationManager.AppSettings["notaryURL"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "notaryURL is not defined in configuration file");
            return;
        }
        if (ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "MerchantPaymentRedirectUrl is not defined in configuration file");
            return;
        }
        if (ConfigurationManager.AppSettings["DisableLatestFive"] != null)
        {
            LatestFive = false;
        }
        merchantRedirectURI = new Uri(ConfigurationManager.AppSettings["MerchantPaymentRedirectUrl"]);
        notaryURL = ConfigurationManager.AppSettings["notaryURL"];
        if ((Request["ret_signed_payload"] != null) && (Request["ret_signature"] != null))
        {
            signedPayload = Request["ret_signed_payload"].ToString();
            signedSignature = Request["ret_signature"].ToString();
            Session["sub_signedPayLoad"] = signedPayload.ToString();
            Session["sub_signedSignature"] = signedSignature.ToString();
            processNotaryResponse();
        }
        else if ((Request["SubscriptionAuthCode"] != null) && (Session["sub_merTranId"] != null))
        {
            processCreateTransactionResponse();
        }
        else if ((Request["shown_notary"] != null) && (Session["sub_processNotary"] != null))
        {
            Session["sub_processNotary"] = null;
            GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: " + Session["sub_tempMerTranId"].ToString();
            GetSubscriptionAuthCode.Text = "Auth Code: " + Session["sub_TranAuthCode"].ToString();
        }
        subsDetailsTable.Controls.Clear();
        drawSubsDetailsSection(false);
        subsRefundTable.Controls.Clear();
        drawSubsRefundSection(false);
        return;
    }
    public void addRowToSubsDetailsSection(string subscription, string merchantsubscription)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);
        //cellOne.Text = transaction.ToString();
        RadioButton rbutton = new RadioButton();
        rbutton.Text = subscription.ToString();
        rbutton.GroupName = "SubsDetailsSection";
        rbutton.ID = subscription.ToString();
        cellOne.Controls.Add(rbutton);
        rowOne.Controls.Add(cellOne);
        TableCell CellTwo = new TableCell();
        CellTwo.CssClass = "cell";
        CellTwo.Width = Unit.Pixel(100);
        rowOne.Controls.Add(CellTwo);

        TableCell CellThree = new TableCell();
        CellThree.CssClass = "cell";
        CellThree.HorizontalAlign = HorizontalAlign.Left;
        CellThree.Width = Unit.Pixel(240);
        CellThree.Text = merchantsubscription.ToString();
        rowOne.Controls.Add(CellThree);

        TableCell CellFour = new TableCell();
        CellFour.CssClass = "cell";
        rowOne.Controls.Add(CellFour);

        subsDetailsTable.Controls.Add(rowOne);
    }
    public void addRowToSubsRefundSection(string subscription, string merchantsubscription)
    {
        TableRow rowOne = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.CssClass = "cell";
        cellOne.Width = Unit.Pixel(150);
        //cellOne.Text = transaction.ToString();
        RadioButton rbutton = new RadioButton();
        rbutton.Text = subscription.ToString();
        rbutton.GroupName = "SubsRefundSection";
        rbutton.ID = subscription.ToString();
        cellOne.Controls.Add(rbutton);
        rowOne.Controls.Add(cellOne);
        TableCell CellTwo = new TableCell();
        CellTwo.CssClass = "cell";
        CellTwo.Width = Unit.Pixel(100);
        rowOne.Controls.Add(CellTwo);

        TableCell CellThree = new TableCell();
        CellThree.CssClass = "cell";
        CellThree.HorizontalAlign = HorizontalAlign.Left;
        CellThree.Width = Unit.Pixel(240);
        CellThree.Text = merchantsubscription.ToString();
        rowOne.Controls.Add(CellThree);

        TableCell CellFour = new TableCell();
        CellFour.CssClass = "cell";
        rowOne.Controls.Add(CellFour);

        subsRefundTable.Controls.Add(rowOne);
    }
    public void drawSubsDetailsSection(bool onlyRow)
    {
        try
        {
            if (onlyRow == false)
            {
                TableRow headingRow = new TableRow();
                TableCell headingCellOne = new TableCell();
                headingCellOne.HorizontalAlign = HorizontalAlign.Right;
                headingCellOne.CssClass = "cell";
                headingCellOne.Width = Unit.Pixel(200);
                headingCellOne.Font.Bold = true;
                headingCellOne.Text = "Merchant Subscription ID";
                headingRow.Controls.Add(headingCellOne);
                TableCell headingCellTwo = new TableCell();
                headingCellTwo.CssClass = "cell";
                headingCellTwo.Width = Unit.Pixel(100);
                headingRow.Controls.Add(headingCellTwo);
                TableCell headingCellThree = new TableCell();
                headingCellThree.CssClass = "cell";
                headingCellThree.HorizontalAlign = HorizontalAlign.Left;
                headingCellThree.Width = Unit.Pixel(240);
                headingCellThree.Font.Bold = true;
                headingCellThree.Text = "Consumer ID";
                headingRow.Controls.Add(headingCellThree);
                TableCell headingCellFour = new TableCell();
                headingCellFour.CssClass = "warning";
                LiteralControl warningMessage = new LiteralControl("<b>WARNING:</b><br/>You must use Get Subscription Status before you can view details of it.");
                headingCellFour.Controls.Add(warningMessage);
                headingRow.Controls.Add(headingCellFour);
                subsDetailsTable.Controls.Add(headingRow);
            }
            resetSubsDetailsList();
            getSubsDetailsFromFile();

            int tempCountToDisplay = 1;
            while ((tempCountToDisplay <= subsDetailsCountToDisplay) && (tempCountToDisplay <= subsDetailsList.Count) && (subsDetailsList.Count > 0))
            {
                addRowToSubsDetailsSection(subsDetailsList[tempCountToDisplay - 1].Key, subsDetailsList[tempCountToDisplay - 1].Value);
                tempCountToDisplay++;
            }
            //addButtonToRefundSection("Refund Transaction");
        }
        catch (Exception ex)
        {
            drawPanelForFailure(subsDetailsPanel, ex.ToString());
        }
    }
    public void drawSubsRefundSection(bool onlyRow)
    {
        try
        {
            if (onlyRow == false)
            {
                TableRow headingRow = new TableRow();
                TableCell headingCellOne = new TableCell();
                headingCellOne.HorizontalAlign = HorizontalAlign.Right;
                headingCellOne.CssClass = "cell";
                headingCellOne.Width = Unit.Pixel(200);
                headingCellOne.Font.Bold = true;
                headingCellOne.Text = "Subscription ID";
                headingRow.Controls.Add(headingCellOne);
                TableCell headingCellTwo = new TableCell();
                headingCellTwo.CssClass = "cell";
                headingCellTwo.Width = Unit.Pixel(100);
                headingRow.Controls.Add(headingCellTwo);
                TableCell headingCellThree = new TableCell();
                headingCellThree.CssClass = "cell";
                headingCellThree.HorizontalAlign = HorizontalAlign.Left;
                headingCellThree.Width = Unit.Pixel(240);
                headingCellThree.Font.Bold = true;
                headingCellThree.Text = "Merchant Subscription ID";
                headingRow.Controls.Add(headingCellThree);
                TableCell headingCellFour = new TableCell();
                headingCellFour.CssClass = "warning";
                LiteralControl warningMessage = new LiteralControl("<b>WARNING:</b><br/>You must use Get Subscription Status before you can refund.");
                headingCellFour.Controls.Add(warningMessage);
                headingRow.Controls.Add(headingCellFour);
                subsRefundTable.Controls.Add(headingRow);
            }
            resetSubsRefundList();
            getSubsRefundFromFile();

            int tempCountToDisplay = 1;
            while ((tempCountToDisplay <= subsDetailsCountToDisplay) && (tempCountToDisplay <= subsRefundList.Count) && (subsRefundList.Count > 0))
            {
                addRowToSubsRefundSection(subsRefundList[tempCountToDisplay - 1].Key, subsRefundList[tempCountToDisplay - 1].Value);
                tempCountToDisplay++;
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(subsRefundPanel, ex.ToString());
        }
    }
    public string getValueOfKeyFromRefund(string key)
    {
        int tempCount = 0;
        while (tempCount < subsDetailsList.Count)
        {
            if (subsRefundList[tempCount].Key.CompareTo(key) == 0)
                return subsRefundList[tempCount].Value;
            tempCount++;
        }
        return "null";
    }
    public string getValueOfKey( string key)
    {
        int tempCount = 0;
        while (tempCount < subsDetailsList.Count)
        {
            if ( subsDetailsList[tempCount].Key.CompareTo(key) == 0)
                return subsDetailsList[tempCount].Value;
            tempCount++;
        }
        return "null";
    }
    public void resetSubsRefundList()
    {
        subsRefundList.RemoveRange(0, subsRefundList.Count);
    }
    public void resetSubsDetailsList()
    {
        subsDetailsList.RemoveRange(0, subsDetailsList.Count);
    }
    public void getSubsDetailsFromFile()
    {
        /* Read the refund file for the list of transactions and store locally */
        FileStream file = new FileStream(Request.MapPath(subsDetailsFile), FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string line;

        while (((line = sr.ReadLine()) != null))
        {
            string[] subsDetailsKeys = Regex.Split(line, ":-:");
            if (subsDetailsKeys[0] != null && subsDetailsKeys[1] != null)
            {
                subsDetailsList.Add(new KeyValuePair<string, string>(subsDetailsKeys[0], subsDetailsKeys[1]));
            }
        }
        sr.Close();
        file.Close();
        subsDetailsList.Reverse(0, subsDetailsList.Count);
    }
    public void getSubsRefundFromFile()
    {
        /* Read the refund file for the list of transactions and store locally */
        FileStream file = new FileStream(Request.MapPath(subsRefundFile), FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(file);
        string line;

        while (((line = sr.ReadLine()) != null))
        {
            string[] subsRefundKeys = Regex.Split(line, ":-:");
            if (subsRefundKeys[0] != null && subsRefundKeys[1] != null)
            {
                subsRefundList.Add(new KeyValuePair<string, string>(subsRefundKeys[0], subsRefundKeys[1]));
            }
        }
        sr.Close();
        file.Close();
        subsRefundList.Reverse(0, subsRefundList.Count);
    }
    public void updatesubsRefundListToFile()
    {
        if (subsRefundList.Count != 0)
            subsRefundList.Reverse(0, subsRefundList.Count);
        using (StreamWriter sr = File.CreateText(Request.MapPath(subsRefundFile)))
        {
            int tempCount = 0;
            while (tempCount < subsRefundList.Count)
            {
                string lineToWrite = subsRefundList[tempCount].Key + ":-:" + subsRefundList[tempCount].Value;
                sr.WriteLine(lineToWrite);
                tempCount++;
            }
            sr.Close();
        }
    }
    public void updatesubsDetailsListToFile()
    {
        if (subsDetailsList.Count != 0)
            subsDetailsList.Reverse(0, subsDetailsList.Count);
        using (StreamWriter sr = File.CreateText(Request.MapPath(subsDetailsFile)))
        {
            int tempCount = 0;
            while (tempCount < subsDetailsList.Count)
            {
                string lineToWrite = subsDetailsList[tempCount].Key + ":-:" + subsDetailsList[tempCount].Value;
                sr.WriteLine(lineToWrite);
                tempCount++;
            }
            sr.Close();
        }
    }
    public bool checkItemInSubsRefundFile(string transactionid, string merchantTransactionId)
    {
        string line;
        string lineToFind = transactionid + ":-:" + merchantTransactionId;
        System.IO.StreamReader file = new System.IO.StreamReader(Request.MapPath(subsRefundFile));
        while ((line = file.ReadLine()) != null)
        {
            if (line.CompareTo(lineToFind) == 0)
            {
                file.Close();
                return true;
            }
        }
        file.Close();
        return false;
    }
    public bool checkItemInSubsDetailsFile(string transactionid, string merchantTransactionId)
    {
        string line;
        string lineToFind = transactionid + ":-:" + merchantTransactionId;
        System.IO.StreamReader file = new System.IO.StreamReader(Request.MapPath(subsDetailsFile));
        while ((line = file.ReadLine()) != null)
        {
            if (line.CompareTo(lineToFind) == 0)
            {
                file.Close();
                return true;
            }
        }
        file.Close();
        return false;
    }
    public void writeSubsRefundToFile(string transactionid, string merchantTransactionId)
    {
        /* Read the refund file for the list of transactions and store locally */
        //FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        //StreamWriter sr = new StreamWriter(file);
        //DateTime junkTime = DateTime.UtcNow;
        //string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        using (StreamWriter appendContent = File.AppendText(Request.MapPath(subsRefundFile)))
        {
            string line = transactionid + ":-:" + merchantTransactionId;
            appendContent.WriteLine(line);
            appendContent.Flush();
            appendContent.Close();
            //file.Close();
        }
    }

    public void writeSubsDetailsToFile(string transactionid, string merchantTransactionId)
    {
        /* Read the refund file for the list of transactions and store locally */
        //FileStream file = new FileStream(Request.MapPath(refundFile), FileMode.Append, FileAccess.Write);
        //StreamWriter sr = new StreamWriter(file);
        //DateTime junkTime = DateTime.UtcNow;
        //string junkTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", junkTime);
        using (StreamWriter appendContent = File.AppendText(Request.MapPath(subsDetailsFile)))
        {
            string line = transactionid + ":-:" + merchantTransactionId;
            appendContent.WriteLine(line);
            appendContent.Flush();
            appendContent.Close();
            //file.Close();
        }
    }

    private void readTransactionParametersFromConfigurationFile()
    {
        transactionTime = DateTime.UtcNow;
        transactionTimeString = String.Format("{0:dddMMMddyyyyHHmmss}", transactionTime);
        if (Radio_SubscriptionProductType.SelectedIndex == 0)
            amount = "1.99";
        else if (Radio_SubscriptionProductType.SelectedIndex == 1)
            amount = "3.99";
        Session["sub_tranType"] = Radio_SubscriptionProductType.SelectedIndex.ToString();
        if (ConfigurationManager.AppSettings["Category"] == null)
        {
            drawPanelForFailure(newSubscriptionPanel, "Category is not defined in configuration file");
            return;
        }
        category = Convert.ToInt32(ConfigurationManager.AppSettings["Category"]);
        if (ConfigurationManager.AppSettings["Channel"] == null)
        {
            channel = "MOBILE_WEB";
        }
        else
        {
            channel = ConfigurationManager.AppSettings["Channel"];
        }
        description = "TrDesc" + transactionTimeString;
        merchantTransactionId = "TrId" + transactionTimeString;
        Session["sub_merTranId"] = merchantTransactionId.ToString();
        merchantProductId = "ProdId" + transactionTimeString;
        merchantApplicationId = "MerAppId" + transactionTimeString;
        MerchantSubscriptionIdList = "MSIList" + transactionTimeString;
        Session["MerchantSubscriptionIdList"] = MerchantSubscriptionIdList.ToString();
        IsPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings["IsPurchaseOnNoActiveSubscription"];
        if (ConfigurationManager.AppSettings["IsPurchaseOnNoActiveSubscription"] == null)
        {
            IsPurchaseOnNoActiveSubscription = "false";
        }
        else
        {
            IsPurchaseOnNoActiveSubscription = ConfigurationManager.AppSettings["IsPurchaseOnNoActiveSubscription"];
        }
        if (ConfigurationManager.AppSettings["SubscriptionRecurringNumber"] == null)
        {
            SubscriptionRecurringNumber = "99999";
        }
        else
        {
            SubscriptionRecurringNumber = ConfigurationManager.AppSettings["SubscriptionRecurringNumber"];
        }
        if (ConfigurationManager.AppSettings["SubscriptionRecurringPeriod"] == null)
        {
            SubscriptionRecurringPeriod = "MONTHLY";
        }
        else
        {
            SubscriptionRecurringPeriod = ConfigurationManager.AppSettings["SubscriptionRecurringPeriod"];
        }
        if (ConfigurationManager.AppSettings["SubscriptionRecurringPeriodAmount"] == null)
        {
            SubscriptionRecurringPeriodAmount = "1";
        }
        else
        {
            SubscriptionRecurringPeriodAmount = ConfigurationManager.AppSettings["SubscriptionRecurringPeriodAmount"];
        }
    }


    private void processNotaryResponse()
    {
        if (Session["sub_tranType"] != null)
        {
            Radio_SubscriptionProductType.SelectedIndex = Convert.ToInt32(Session["sub_tranType"].ToString());
            Session["sub_tranType"] = null;
        }
        Response.Redirect(FQDN + "/Commerce/Payment/Rest/2/Subscriptions?clientid=" + api_key.ToString() + "&SignedPaymentDetail=" + signedPayload.ToString() + "&Signature=" + signedSignature.ToString());

    }

    public void processCreateTransactionResponse()
    {
        lblsubscode.Text = Request["SubscriptionAuthCode"].ToString();
        lblsubsid.Text = Session["sub_merTranId"].ToString();
        subscriptionSuccessTable.Visible = true;
        GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: " + Session["sub_merTranId"].ToString();
        GetSubscriptionAuthCode.Text = "Auth Code: " + Request["SubscriptionAuthCode"].ToString();
        GetSubscriptionID.Text = "Subscription ID: ";
        Session["sub_tempMerTranId"] = Session["sub_merTranId"].ToString();
        Session["sub_merTranId"] = null;
        Session["sub_TranAuthCode"] = Request["SubscriptionAuthCode"].ToString();
        return;
    }

    /* This function draws the success table */
    private void drawPanelForSuccess(Panel panelParam)
    {
        successTable = new Table();
        successTable.Font.Name = "Sans-serif";
        successTable.Font.Size = 8;
        successTable.BorderStyle = BorderStyle.Outset;
        successTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "SUCCESS:";
        rowOne.Controls.Add(rowOneCellOne);
        successTable.Controls.Add(rowOne);
        successTable.BorderWidth = 2;
        successTable.BorderColor = Color.DarkGreen;
        successTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#cfc");
        panelParam.Controls.Add(successTable);
    }
    /*This function adds row to the success table */
    private void addRowToSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.Text = attribute.ToString();
        cellOne.Font.Bold = true;
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Text = value.ToString();
        row.Controls.Add(cellTwo);
        successTable.Controls.Add(row);
    }
    /* This function draws error table */
    private void drawPanelForFailure(Panel panelParam, string message)
    {
        failureTable = new Table();
        failureTable.Font.Name = "Sans-serif";
        failureTable.Font.Size = 8;
        failureTable.BorderStyle = BorderStyle.Outset;
        failureTable.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        failureTable.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        failureTable.Controls.Add(rowTwo);
        failureTable.BorderWidth = 2;
        failureTable.BorderColor = Color.Red;
        failureTable.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc");
        panelParam.Controls.Add(failureTable);
    }

    private void drawPanelForGetTransactionSuccess(Panel panelParam)
    {
        successTableGetTransaction = new Table();
        successTableGetTransaction.Font.Name = "Sans-serif";
        successTableGetTransaction.Font.Size = 8;
        successTableGetTransaction.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Right;
        rowOneCellOne.Text = "Parameter";
        rowOneCellOne.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellOne);
        TableCell rowOneCellTwo = new TableCell();
        rowOneCellTwo.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellTwo);

        TableCell rowOneCellThree = new TableCell();
        rowOneCellThree.Font.Bold = true;
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellThree.Text = "Value";
        rowOneCellThree.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellThree);
        successTableGetTransaction.Controls.Add(rowOne);
        panelParam.Controls.Add(successTableGetTransaction);
    }
    /*This function adds row to the success table */
    private void addRowToGetTransactionSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute.ToString();
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value.ToString();
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        successTableGetTransaction.Controls.Add(row);
    }

    private void drawPanelForGetSubscriptionDetailsSuccess(Panel panelParam)
    {
        successTableGetSubscriptionDetails = new Table();
        successTableGetSubscriptionDetails.Font.Name = "Sans-serif";
        successTableGetSubscriptionDetails.Font.Size = 8;
        successTableGetSubscriptionDetails.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.HorizontalAlign = HorizontalAlign.Right;
        rowOneCellOne.Text = "Parameter";
        rowOneCellOne.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellOne);
        TableCell rowOneCellTwo = new TableCell();
        rowOneCellTwo.Width = Unit.Pixel(50);
        rowOne.Controls.Add(rowOneCellTwo);

        TableCell rowOneCellThree = new TableCell();
        rowOneCellThree.Font.Bold = true;
        rowOneCellThree.HorizontalAlign = HorizontalAlign.Left;
        rowOneCellThree.Text = "Value";
        rowOneCellThree.Width = Unit.Pixel(300);
        rowOne.Controls.Add(rowOneCellThree);
        successTableGetSubscriptionDetails.Controls.Add(rowOne);
        panelParam.Controls.Add(successTableGetSubscriptionDetails);
    }
    /*This function adds row to the success table */
    private void addRowToGetSubscriptionDetailsSuccessPanel(Panel panelParam, string attribute, string value)
    {
        TableRow row = new TableRow();
        TableCell cellOne = new TableCell();
        cellOne.HorizontalAlign = HorizontalAlign.Right;
        cellOne.Text = attribute.ToString();
        cellOne.Width = Unit.Pixel(300);
        row.Controls.Add(cellOne);
        TableCell cellTwo = new TableCell();
        cellTwo.Width = Unit.Pixel(50);
        row.Controls.Add(cellTwo);
        TableCell cellThree = new TableCell();
        cellThree.HorizontalAlign = HorizontalAlign.Left;
        cellThree.Text = value.ToString();
        cellThree.Width = Unit.Pixel(300);
        row.Controls.Add(cellThree);
        successTableGetSubscriptionDetails.Controls.Add(row);
    }
    /* This function reads the Access Token File and stores the values of access token, expiry seconds
 * refresh token, last access token time and refresh token expiry time
 * This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
 */
    public bool readAccessTokenFile()
    {
        try
        {
            FileStream file = new FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader sr = new StreamReader(file);
            access_token = sr.ReadLine();
            expiryMilliSeconds = sr.ReadLine();
            refresh_token = sr.ReadLine();
            lastTokenTakenTime = sr.ReadLine();
            refreshTokenExpiryTime = sr.ReadLine();
            sr.Close();
            file.Close();
        }
        catch (Exception ex)
        {
            return false;
        }
        if ((access_token == null) || (expiryMilliSeconds == null) || (refresh_token == null) || (lastTokenTakenTime == null) || (refreshTokenExpiryTime == null))
        {
            return false;
        }
        return true;
    }

    /* This function validates the expiry of the access token and refresh token,
     * function compares the current time with the refresh token taken time, if current time is greater then 
     * returns INVALID_REFRESH_TOKEN
     * function compares the difference of last access token taken time and the current time with the expiry seconds, if its more,
     * funciton returns INVALID_ACCESS_TOKEN
     * otherwise returns VALID_ACCESS_TOKEN
    */
    public string isTokenValid()
    {
        try
        {

            DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
            DateTime lastRefreshTokenTime = DateTime.Parse(refreshTokenExpiryTime);
            TimeSpan refreshSpan = currentServerTime.Subtract(lastRefreshTokenTime);
            if (currentServerTime >= lastRefreshTokenTime)
            {
                return "INVALID_ACCESS_TOKEN";
            }
            DateTime lastTokenTime = DateTime.Parse(lastTokenTakenTime);
            TimeSpan tokenSpan = currentServerTime.Subtract(lastTokenTime);
            if (((tokenSpan.TotalSeconds)) > Convert.ToInt32(expiryMilliSeconds))
            {
                return "REFRESH_ACCESS_TOKEN";
            }
            else
            {
                return "VALID_ACCESS_TOKEN";
            }
        }
        catch (Exception ex)
        {
            return "INVALID_ACCESS_TOKEN";
        }
    }


    /* This function get the access token based on the type parameter type values.
     * If type value is 1, access token is fetch for client credential flow
     * If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
     */
    public bool getAccessToken(int type, Panel panelParam)
    {
        /*  This is client credential flow: */
        if (type == 1)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&grant_type=client_credentials&scope=PAYMENT");
                accessTokenRequest.Method = "GET";

                WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                {
                    string jsonAccessToken = accessTokenResponseStream.ReadToEnd().ToString();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));
                    access_token = deserializedJsonObj.access_token.ToString();
                    expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                    refresh_token = deserializedJsonObj.refresh_token.ToString();
                    FileStream file = new FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(file);
                    sw.WriteLine(access_token);
                    sw.WriteLine(expiryMilliSeconds);
                    sw.WriteLine(refresh_token);
                    sw.WriteLine(currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString());
                    lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                    //Refresh token valids for 24 hours
                    DateTime refreshExpiry = currentServerTime.AddHours(24);
                    refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    sw.WriteLine(refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString());
                    sw.Close();
                    file.Close();
                    // Close and clean up the StreamReader
                    accessTokenResponseStream.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(panelParam, ex.ToString());
                return false;
            }
        }
        else if (type == 2)
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
                WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create("" + FQDN + "/oauth/access_token?grant_type=refresh_token&client_id=" + api_key.ToString() + "&client_secret=" + secret_key.ToString() + "&refresh_token=" + refresh_token.ToString());
                accessTokenRequest.Method = "GET";
                WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                {
                    string access_token_json = accessTokenResponseStream.ReadToEnd().ToString();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(access_token_json, typeof(AccessTokenResponse));
                    access_token = deserializedJsonObj.access_token.ToString();
                    expiryMilliSeconds = deserializedJsonObj.expires_in.ToString();
                    refresh_token = deserializedJsonObj.refresh_token.ToString();
                    FileStream file = new FileStream(Request.MapPath(accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(file);
                    sw.WriteLine(access_token);
                    sw.WriteLine(expiryMilliSeconds);
                    sw.WriteLine(refresh_token);
                    sw.WriteLine(currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString());
                    lastTokenTakenTime = currentServerTime.ToLongDateString() + " " + currentServerTime.ToLongTimeString();
                    //Refresh token valids for 24 hours
                    DateTime refreshExpiry = currentServerTime.AddHours(24);
                    refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                    sw.WriteLine(refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString());
                    sw.Close();
                    file.Close();
                    accessTokenResponseStream.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                drawPanelForFailure(panelParam, ex.ToString());
                return false;
            }
        }
        return false;
    }

    /* This function is used to neglect the ssl handshake error with authentication server */

    public static void BypassCertificateError()
    {
        ServicePointManager.ServerCertificateValidationCallback +=
            delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
    }
    /* This function is used to read access token file and validate the access token
     * this function returns true if access token is valid, or else false is returned
     */
    public bool readAndGetAccessToken(Panel panelParam)
    {
        bool result = true;
        if (readAccessTokenFile() == false)
        {
            result = getAccessToken(1, panelParam);
        }
        else
        {
            string tokenValidity = isTokenValid();
            if (tokenValidity.CompareTo("REFRESH_ACCESS_TOKEN") == 0)
            {
                result = getAccessToken(2, panelParam);
            }
            else if (string.Compare(isTokenValid(), "INVALID_ACCESS_TOKEN") == 0)
            {
                result = getAccessToken(1, panelParam);
            }
        }
        return result;
    }
    public class AccessTokenResponse
    {
        public string access_token;
        public string refresh_token;
        public string expires_in;
    }
    public class RefundResponse
    {
        public string TransactionId { get; set; }
        public string TransactionStatus { get; set; }
        public string IsSuccess { get; set; }
        public string Version { get; set; }
    }
    public class subscriptionStatusResponse
    {
        public string Currency { get; set; }
        public string Version { get; set; }
        public string IsSuccess { get; set; }
        public string MerchantTransactionId { get; set; }
        public string ConsumerId { get; set; }
        public string Description { get; set; }
        public string Amount { get; set; }
        public string ContentCategory { get; set; }
        public string MerchantProductId { get; set; }
        public string MerchantApplicationId { get; set; }
        public string Channel { get; set; }
        public string SubscriptionPeriod{ get; set; }
        public string PeriodAmount { get; set; }
        public string Recurrences { get; set; }
        public string MerchantSubscriptionId { get; set; }
        public string MerchantIdentifier { get; set; }
        public string IsAutoCommitted { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionStatus { get; set; }
        public string SubscriptionType { get; set; }
        public string OriginalTransactionId { get; set;}
    }

    public class subscriptionDetailsResponse
    {
        public string IsActiveSubscription { get; set; }
        public string Currency { get; set; }
        public string CreationDate { get; set; }
        public string CurrentStartDate { get; set; }
        public string CurrentEndDate { get; set; }
        public string GrossAmount { get; set; }
        public string Recurrences { get; set; }
        public string RecurrencesLeft { get; set; }
        public string Version { get; set; }
        public string IsSuccess { get; set; }
    }
    //"\",\"MerchantApplicationId\":\"" + merchantApplicationId.ToString() +
    //for amount do we need to add quotes, it worked for transaction with quotes
    protected void newSubscriptionButton_Click1(object sender, EventArgs e)
    {
        readTransactionParametersFromConfigurationFile();
        string payLoadString = "{\"Amount\":" + amount.ToString() + ",\"Category\":" + category.ToString() + ",\"Channel\":\"" +
                        channel.ToString() + "\",\"Description\":\"" + description.ToString() + "\",\"MerchantTransactionId\":\""
                        + merchantTransactionId.ToString() + "\",\"MerchantProductId\":\"" + merchantProductId.ToString()
                        + "\",\"MerchantPaymentRedirectUrl\":\"" + merchantRedirectURI.ToString() + "\",\"MerchantSubscriptionIdList\":\""
                        + MerchantSubscriptionIdList.ToString() + "\",\"IsPurchaseOnNoActiveSubscription\":\""
                        + IsPurchaseOnNoActiveSubscription.ToString() + "\",\"SubscriptionRecurringNumber\":" + SubscriptionRecurringNumber.ToString()
                        + ",\"SubscriptionRecurringPeriod\":\"" + SubscriptionRecurringPeriod.ToString()
                        + "\",\"SubscriptionRecurringPeriodAmount\":" + SubscriptionRecurringPeriodAmount.ToString() +
                        "}";
        Session["sub_payloadData"] = payLoadString.ToString();
        Response.Redirect(notaryURL.ToString() + "?request_to_sign=" + payLoadString.ToString() + "&goBackURL=" + merchantRedirectURI.ToString() + "&api_key=" + api_key.ToString() + "&secret_key=" + secret_key.ToString());
    }
    protected void getSubscriptionButton_Click(object sender, EventArgs e)
    {
        string resourcePathString = "";
        try
        {
            string keyValue = "";
            if (Radio_SubscriptionStatus.SelectedIndex == 0)
            {
                keyValue = GetSubscriptionMerchantSubsID.Text.ToString().Replace("Merchant Sub. ID: ", "");
                if (keyValue.Length == 0)
                    return;
                resourcePathString = "" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/MerchantTransactionId/" + keyValue.ToString();
            }
            if (Radio_SubscriptionStatus.SelectedIndex == 1)
            {
                keyValue = GetSubscriptionAuthCode.Text.ToString().Replace("Auth Code: ", "");
                if (keyValue.Length == 0)
                    return;
                resourcePathString = "" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/TransactionAuthCode/" + keyValue.ToString();
            }
            if (Radio_SubscriptionStatus.SelectedIndex == 2)
            {
                keyValue = GetSubscriptionID.Text.ToString().Replace("Subscription ID: ", "");
                if (keyValue.Length == 0)
                    return;
                resourcePathString = "" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/SubscriptionId/" + keyValue.ToString();
            }
            if (readAndGetAccessToken(newSubscriptionPanel) == true)
            {
                if (access_token == null || access_token.Length <= 0)
                {
                    return;
                }
                //String getTransactionStatusResponseData;
                resourcePathString = resourcePathString + "?access_token=" + access_token.ToString();
                //HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/" + Session["MerchantSubscriptionIdList"].ToString() + "/Detail/" + consumerId.ToString() + "?access_token=" + access_token.ToString());
                HttpWebRequest objRequest = (HttpWebRequest)System.Net.WebRequest.Create(resourcePathString); 
                objRequest.Method = "GET";
                HttpWebResponse getTransactionStatusResponseObject = (HttpWebResponse)objRequest.GetResponse();
                using (StreamReader getTransactionStatusResponseStream = new StreamReader(getTransactionStatusResponseObject.GetResponseStream()))
                {
                    String getTransactionStatusResponseData = getTransactionStatusResponseStream.ReadToEnd();
                    JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                    subscriptionStatusResponse deserializedJsonObj = (subscriptionStatusResponse)deserializeJsonObject.Deserialize(getTransactionStatusResponseData, typeof(subscriptionStatusResponse));
                    lblstatusMerSubsId.Text = deserializedJsonObj.MerchantSubscriptionId.ToString();
                    lblstatusSubsId.Text = deserializedJsonObj.SubscriptionId.ToString();
                    GetSubscriptionID.Text = "Subscription ID: " + deserializedJsonObj.SubscriptionId.ToString();
                    if (checkItemInSubsDetailsFile(deserializedJsonObj.MerchantSubscriptionId.ToString(),deserializedJsonObj.ConsumerId.ToString()) == false)
                    {
                        writeSubsDetailsToFile(deserializedJsonObj.MerchantSubscriptionId.ToString(),deserializedJsonObj.ConsumerId.ToString());
                    }
                    if (checkItemInSubsRefundFile(deserializedJsonObj.SubscriptionId.ToString(), deserializedJsonObj.MerchantSubscriptionId.ToString()) == false)
                    {
                        writeSubsRefundToFile(deserializedJsonObj.SubscriptionId.ToString(), deserializedJsonObj.MerchantSubscriptionId.ToString());
                    }
                    subsDetailsTable.Controls.Clear();
                    drawSubsDetailsSection(false);
                    subsRefundTable.Controls.Clear();
                    drawSubsRefundSection(false);
                    subsGetStatusTable.Visible = true;
                    drawPanelForGetTransactionSuccess(getSubscriptionStatusPanel);
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionId", deserializedJsonObj.SubscriptionId.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionStatus", deserializedJsonObj.SubscriptionStatus.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionType", deserializedJsonObj.SubscriptionType.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ConsumerId", deserializedJsonObj.ConsumerId.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantTransactionId", deserializedJsonObj.MerchantTransactionId.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Description", deserializedJsonObj.Description.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Amount", deserializedJsonObj.Amount.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "ContentCategory", deserializedJsonObj.ContentCategory.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantProductId", deserializedJsonObj.MerchantProductId.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantApplicationId", deserializedJsonObj.MerchantApplicationId.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Channel ", deserializedJsonObj.Channel.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "SubscriptionPeriod", deserializedJsonObj.SubscriptionPeriod.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "PeriodAmount", deserializedJsonObj.PeriodAmount.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Recurrences", deserializedJsonObj.Recurrences.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantSubscriptionId", deserializedJsonObj.MerchantSubscriptionId.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "MerchantIdentifier", deserializedJsonObj.MerchantIdentifier.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsAutoCommitted", deserializedJsonObj.IsAutoCommitted.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString());
                    addRowToGetTransactionSuccessPanel(getSubscriptionStatusPanel, "OriginalTransactionId", deserializedJsonObj.OriginalTransactionId.ToString());
                    getTransactionStatusResponseStream.Close();
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(getSubscriptionStatusPanel, ex.ToString());
        }
/*
        DateTime dummy = DateTime.UtcNow;
        string dummyString = String.Format("{0:dddMMMddyyyyHHmmss}", dummy);
        if (checkItemInSubsDetailsFile("Tran" + dummyString, "MerTran" + dummyString) == false)
        {
            writeSubsDetailsToFile("Tran" + dummyString, "MerTran" + dummyString);
        }
        subsDetailsTable.Controls.Clear();
        drawSubsDetailsSection(false);
*/
    }
    protected void viewNotaryButton_Click(object sender, EventArgs e)
    {
        if ((Session["sub_payloadData"] != null) && (Session["sub_signedPayLoad"] != null) && (Session["sub_signedSignature"] != null))
        {
            Session["sub_processNotary"] = "notary";
            Response.Redirect(notaryURL.ToString() + "?signed_payload=" + Session["sub_signedPayLoad"].ToString() + "&goBackURL=" + merchantRedirectURI.ToString() + "&signed_signature=" + Session["sub_signedSignature"].ToString() + "&signed_request=" + Session["sub_payloadData"].ToString());
        }
    }

    protected void btnGetSubscriptionDetails_Click(object sender, EventArgs e)
    {
        string merSubsID = "";
        bool recordFound = false;
        try
        {
            if (subsDetailsList.Count > 0)
            {
                foreach (Control subDetailsTableRow in subsDetailsTable.Controls)
                {
                    if (subDetailsTableRow is TableRow)
                    {
                        foreach (Control subDetailsTableRowCell in subDetailsTableRow.Controls)
                        {
                            if (subDetailsTableRowCell is TableCell)
                            {
                                foreach (Control subDetailsTableCellControl in subDetailsTableRowCell.Controls)
                                {
                                    if ((subDetailsTableCellControl is RadioButton))
                                    {

                                        if (((RadioButton)subDetailsTableCellControl).Checked)
                                        {
                                            merSubsID = ((RadioButton)subDetailsTableCellControl).Text.ToString();
                                            recordFound = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (recordFound == true)
                {
                    if (readAndGetAccessToken(subsDetailsPanel) == true)
                    {
                        if (access_token == null || access_token.Length <= 0)
                        {
                            return;
                        }
                        String consID = getValueOfKey(merSubsID);
                        if (consID.CompareTo("null") == 0)
                             return;
                        //drawPanelForFailure(getSubscriptionStatusPanel, merchantSubId.ToString());
                        //String getTransactionStatusResponseData;
                        WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/" + merSubsID.ToString() + "/Detail/" +  consID.ToString() + "?access_token=" + access_token.ToString());
                        //WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Subscriptions/" + merSubsID.ToString() + "/Detail/" +  consID.ToString() );
                        objRequest.Method = "GET";
                        objRequest.ContentType = "application/json";
                        WebResponse subsDetailsResponeObject = (WebResponse)objRequest.GetResponse();
                        using (StreamReader subsDetailsResponseStream = new StreamReader(subsDetailsResponeObject.GetResponseStream()))
                        {
                            String subsDetailsResponseData = subsDetailsResponseStream.ReadToEnd();
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            subscriptionDetailsResponse deserializedJsonObj = (subscriptionDetailsResponse)deserializeJsonObject.Deserialize(subsDetailsResponseData, typeof(subscriptionDetailsResponse));
                            subsDetailsSuccessTable.Visible = true;
                            lblMerSubId.Text = merSubsID.ToString();
                            lblConsId.Text = consID.ToString();
                            drawPanelForGetSubscriptionDetailsSuccess(subsDetailsPanel);
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Recurrences", deserializedJsonObj.Recurrences.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentStartDate", deserializedJsonObj.CurrentStartDate.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsSuccess", deserializedJsonObj.IsSuccess.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "IsActiveSubscription", deserializedJsonObj.IsActiveSubscription.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "RecurrencesLeft", deserializedJsonObj.RecurrencesLeft.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "GrossAmount", deserializedJsonObj.GrossAmount.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CreationDate", deserializedJsonObj.CreationDate.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Version", deserializedJsonObj.Version.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "Currency", deserializedJsonObj.Currency.ToString());
                            addRowToGetSubscriptionDetailsSuccessPanel(getSubscriptionStatusPanel, "CurrentEndDate", deserializedJsonObj.CurrentEndDate.ToString());
                            if (LatestFive == false)
                            {
                                //subsDetailsList.RemoveAll(x => x.Key.Equals(merSubsID));
                                //updatesubsDetailsListToFile();
                                //resetSubsDetailsList();
                                //subsDetailsTable.Controls.Clear();
                                //drawSubsDetailsSection(false);
                                //GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: ";
                                //GetSubscriptionAuthCode.Text = "Auth Code: ";
                                //GetSubscriptionID.Text = "Subscription ID: ";
                            }
                            subsDetailsResponseStream.Close();
                        }
                    }
/*
                    subsDetailsSuccessTable.Visible = true;
                    lblMerSubsId.Text = merSubsID.ToString();
                    lblSubsIdDetails.Text = "SUCCESSFUL";
                    lblMerTranId.Text = "true";
                    if (LatestFive == false)
                    {
                        //subsDetailsList.RemoveAll(x => x.Key.Equals(merSubsID));
                        //updatesubsDetailsListToFile();
                        //resetSubsDetailsList();
                        //subsDetailsTable.Controls.Clear();
                        //drawSubsDetailsSection(false);
                        GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: ";
                        GetSubscriptionAuthCode.Text = "Auth Code: ";
                        GetSubscriptionID.Text = "Subscription ID: ";
                    }*/
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(subsDetailsPanel, ex.ToString());
        }
    }
    protected void btnGetSubscriptionRefund_Click(object sender, EventArgs e)
    {
        string SubsID = "";
        bool recordFound = false;
        string strReq = "{\"RefundReasonCode\":1,\"RefundReasonText\":\"Customer was not happy\"}";
        string dataLength = "";
        try
        {
            if (subsRefundList.Count > 0)
            {
                foreach (Control subRefundTableRow in subsRefundTable.Controls)
                {
                    if (subRefundTableRow is TableRow)
                    {
                        foreach (Control subRefundTableRowCell in subRefundTableRow.Controls)
                        {
                            if (subRefundTableRowCell is TableCell)
                            {
                                foreach (Control subRefundTableCellControl in subRefundTableRowCell.Controls)
                                {
                                    if ((subRefundTableCellControl is RadioButton))
                                    {

                                        if (((RadioButton)subRefundTableCellControl).Checked)
                                        {
                                            SubsID = ((RadioButton)subRefundTableCellControl).Text.ToString();
                                            recordFound = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (recordFound == true)
                {
                    if (readAndGetAccessToken(subsRefundPanel) == true)
                    {
                        if (access_token == null || access_token.Length <= 0)
                        {
                            return;
                        }
                        String merSubsID = getValueOfKeyFromRefund(SubsID);
                        if (merSubsID.CompareTo("null") == 0)
                            return;
                        //drawPanelForFailure(getSubscriptionStatusPanel, merchantSubId.ToString());
                        //String getTransactionStatusResponseData;
                        WebRequest objRequest = (WebRequest)System.Net.WebRequest.Create("" + FQDN + "/Commerce/Payment/Rest/2/Transactions/" + SubsID.ToString() + "?access_token=" + access_token.ToString() + "&Action=refund");
                        objRequest.Method = "PUT";
                        objRequest.ContentType = "application/json";
                        UTF8Encoding encoding = new UTF8Encoding();
                        byte[] postBytes = encoding.GetBytes(strReq);
                        objRequest.ContentLength = postBytes.Length;
                        Stream postStream = objRequest.GetRequestStream();
                        postStream.Write(postBytes, 0, postBytes.Length);
                        dataLength = postBytes.Length.ToString();
                        postStream.Close();
                        WebResponse subsRefundResponeObject = (WebResponse)objRequest.GetResponse();
                        using (StreamReader subsRefundResponseStream = new StreamReader(subsRefundResponeObject.GetResponseStream()))
                        {
                            String subsRefundResponseData = subsRefundResponseStream.ReadToEnd();
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            RefundResponse deserializedJsonObj = (RefundResponse)deserializeJsonObject.Deserialize(subsRefundResponseData, typeof(RefundResponse));
                            subsRefundSuccessTable.Visible = true;
                            //drawPanelForGetSubscriptionDetailsSuccess(subsRefundPanel);
                            lbRefundTranID.Text = deserializedJsonObj.TransactionId.ToString();
                            lbRefundTranStatus.Text = deserializedJsonObj.TransactionStatus.ToString();
                            lbRefundIsSuccess.Text = deserializedJsonObj.IsSuccess.ToString();
                            lbRefundVersion.Text = deserializedJsonObj.Version.ToString(); 
                            if (LatestFive == false)
                            {
                                subsRefundList.RemoveAll(x => x.Key.Equals(SubsID));
                                updatesubsRefundListToFile();
                                resetSubsRefundList();
                                subsRefundTable.Controls.Clear();
                                drawSubsRefundSection(false);
                                GetSubscriptionMerchantSubsID.Text = "Merchant Sub. ID: ";
                                GetSubscriptionAuthCode.Text = "Auth Code: ";
                                GetSubscriptionID.Text = "Subscription ID: ";
                            }
                            subsRefundResponseStream.Close();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            drawPanelForFailure(subsRefundPanel, ex.ToString());
        }
    }
}