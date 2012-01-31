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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates; 

/* This funciton is called when applicaiton is getting loaded, this reads the web.config and stores parameters,
   The function search the directory MoImages and based on the configuration parameter, application displays images.
 */
public partial class _Default : System.Web.UI.Page
{
    string shortCode, directoryPath;
    int numOfFilesToDisplay;
    protected void Page_Load(object sender, EventArgs e)
    {
        DateTime currentServerTime = DateTime.UtcNow;
        lblServerTime.Text = String.Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC";
        if (ConfigurationManager.AppSettings["short_code"] == null)
        {
            drawPanelForFailure("short_code is not defined in configuration file");
            return;
        }
        shortCode = ConfigurationManager.AppSettings["short_code"].ToString();
        shortCodeLabel.Text = shortCode.ToString();
        if (ConfigurationManager.AppSettings["ImageDirectory"] == null)
        {
            drawPanelForFailure("ImageDirectory is not defined in configuration file");
            return;
        }
        directoryPath = ConfigurationManager.AppSettings["ImageDirectory"];
        if (ConfigurationManager.AppSettings["NumOfFilesToDisplay"] == null)
        {
            numOfFilesToDisplay = 5;
        }
        else
        {
            numOfFilesToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings["NumOfFilesToDisplay"]);
        }
        int columnCount = 0;
		TableRow TableRow = null;
		TableRow tr = null;
		int totalFiles = 0;
        Table pictureTable = new Table();
        Table tbControl = new Table();
        DirectoryInfo _dir = new DirectoryInfo(Request.MapPath(directoryPath));
        List<FileInfo> _imgs = _dir.GetFiles().OrderBy(f => f.CreationTime).ToList();
        totalFiles = _imgs.Count;
        //string fileShownMessage = "Displaying" + numOfFilesToDisplay.ToString() + "out of " + _imgs.Count.ToString();
        string fileShownMessage =  _imgs.Count.ToString();
        lbl_TotalCount.Text = fileShownMessage;
        int fileCountIndex = 0;
        foreach (FileInfo file in _imgs)
        {
            
            if (fileCountIndex == numOfFilesToDisplay)
            {
                break;
            }
            if (columnCount == 0)
            {
                TableRow = new TableRow();
                tr = new TableRow();
                TableCell TableCellImage = new TableCell();
                System.Web.UI.WebControls.Image Image1 = new System.Web.UI.WebControls.Image();
                Image1.ImageUrl = string.Format("{0}{1}", directoryPath, file.Name);
                Image1.Width = 150;
                Image1.Height = 150;

                TableCellImage.Controls.Add(Image1);
                TableRow.Controls.Add(TableCellImage);
                TableCell TableCellSubject = new TableCell();
                TableCellSubject.Text = file.Name;
                tr.Controls.Add(TableCellSubject);
                columnCount += 1;
            }
            else
            {
                TableCell TableCellImage = new TableCell();
                System.Web.UI.WebControls.Image Image1 = new System.Web.UI.WebControls.Image();
                Image1.ImageUrl = string.Format("{0}{1}", directoryPath, file.Name);
                Image1.Width = 150;
                Image1.Height = 150;

                TableCellImage.Controls.Add(Image1);
                TableRow.Controls.Add(TableCellImage);
                TableCell TableCellSubject = new TableCell();
                TableCellSubject.Text = file.Name;
                tr.Controls.Add(TableCellSubject);
                columnCount += 1;
                if (columnCount == 5)
                {
                    columnCount = 0;
                }
                fileCountIndex++;
            }
            pictureTable.Controls.Add(TableRow);
            pictureTable.Controls.Add(tr);
        }
        messagePanel.Controls.Add(pictureTable);
    }

    /* This function draws failure table, in case there is an error */
    public void drawPanelForFailure(string message)
    {
        Table table = new Table();
        table.Font.Name = "Sans-serif";
        table.Font.Size = 9;
        table.BorderStyle = BorderStyle.Outset;
        table.Width = Unit.Pixel(650);
        TableRow rowOne = new TableRow();
        TableCell rowOneCellOne = new TableCell();
        rowOneCellOne.Font.Bold = true;
        rowOneCellOne.Text = "ERROR:";
        rowOne.Controls.Add(rowOneCellOne);
        //rowOneCellOne.BorderWidth = 1;
        table.Controls.Add(rowOne);
        TableRow rowTwo = new TableRow();
        TableCell rowTwoCellOne = new TableCell();
        //rowTwoCellOne.BorderWidth = 1;
        rowTwoCellOne.Text = message.ToString();
        rowTwo.Controls.Add(rowTwoCellOne);
        table.Controls.Add(rowTwo);
        table.BorderWidth = 2;
        table.BorderColor = Color.Red;
        table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc"); ;
        messagePanel.Controls.Add(table);
    }
}
