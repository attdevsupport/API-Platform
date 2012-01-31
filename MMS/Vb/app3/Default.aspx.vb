Imports System
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

Public Partial Class _Default
	Inherits System.Web.UI.Page
	Private shortCode As String, directoryPath As String
	Private numOfFilesToDisplay As Integer
	Protected Sub Page_Load(sender As Object, e As EventArgs)
		Dim currentServerTime As DateTime = DateTime.UtcNow
		lblServerTime.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) + " UTC"
		If ConfigurationManager.AppSettings("short_code") Is Nothing Then
			drawPanelForFailure("short_code is not defined in configuration file")
			Return
		End If
		shortCode = ConfigurationManager.AppSettings("short_code").ToString()
		shortCodeLabel.Text = shortCode.ToString()
		If ConfigurationManager.AppSettings("ImageDirectory") Is Nothing Then
			drawPanelForFailure("ImageDirectory is not defined in configuration file")
			Return
		End If
		directoryPath = ConfigurationManager.AppSettings("ImageDirectory")
		If ConfigurationManager.AppSettings("NumOfFilesToDisplay") Is Nothing Then
			numOfFilesToDisplay = 5
		Else
			numOfFilesToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("NumOfFilesToDisplay"))
		End If
		Dim columnCount As Integer = 0
		Dim TableRow As TableRow = Nothing
		Dim tr As TableRow = Nothing
		Dim totalFiles As Integer = 0
		Dim pictureTable As New Table()
		Dim tbControl As New Table()
		Dim _dir As New DirectoryInfo(Request.MapPath(directoryPath))
		Dim _imgs As List(Of FileInfo) = _dir.GetFiles().OrderBy(Function(f) f.CreationTime).ToList()
		totalFiles = _imgs.Count
		'string fileShownMessage = "Displaying" + numOfFilesToDisplay.ToString() + "out of " + _imgs.Count.ToString();
		Dim fileShownMessage As String = _imgs.Count.ToString()
		lbl_TotalCount.Text = fileShownMessage
		Dim fileCountIndex As Integer = 0
		For Each file As FileInfo In _imgs

			If fileCountIndex = numOfFilesToDisplay Then
				Exit For
			End If
			If columnCount = 0 Then
				TableRow = New TableRow()
				tr = New TableRow()
				Dim TableCellImage As New TableCell()
				Dim Image1 As New System.Web.UI.WebControls.Image()
				Image1.ImageUrl = String.Format("{0}{1}", directoryPath, file.Name)
				Image1.Width = 150
				Image1.Height = 150

				TableCellImage.Controls.Add(Image1)
				TableRow.Controls.Add(TableCellImage)
				Dim TableCellSubject As New TableCell()
				TableCellSubject.Text = file.Name
				TableCellSubject.Width = 150
				tr.Controls.Add(TableCellSubject)
				columnCount += 1
			Else
				Dim TableCellImage As New TableCell()
				Dim Image1 As New System.Web.UI.WebControls.Image()
				Image1.ImageUrl = String.Format("{0}{1}", directoryPath, file.Name)
				Image1.Width = 150
				Image1.Height = 150

				TableCellImage.Controls.Add(Image1)
				TableRow.Controls.Add(TableCellImage)
				Dim TableCellSubject As New TableCell()
				TableCellSubject.Text = file.Name
				TableCellSubject.Width = 150
				tr.Controls.Add(TableCellSubject)
				columnCount += 1
				If columnCount = 5 Then
					columnCount = 0
				End If
				fileCountIndex += 1
			End If
			pictureTable.Controls.Add(TableRow)
			pictureTable.Controls.Add(tr)
		Next
		messagePanel.Controls.Add(pictureTable)
	End Sub
	Public Sub drawPanelForFailure(message As String)
		Dim table As New Table()
		table.Font.Name = "Sans-serif"
		table.Font.Size = 9
		table.BorderStyle = BorderStyle.Outset
		table.Width = Unit.Pixel(650)
		Dim rowOne As New TableRow()
		Dim rowOneCellOne As New TableCell()
		rowOneCellOne.Font.Bold = True
		rowOneCellOne.Text = "ERROR:"
		rowOne.Controls.Add(rowOneCellOne)
		'rowOneCellOne.BorderWidth = 1;
		table.Controls.Add(rowOne)
		Dim rowTwo As New TableRow()
		Dim rowTwoCellOne As New TableCell()
		'rowTwoCellOne.BorderWidth = 1;
		rowTwoCellOne.Text = message.ToString()
		rowTwo.Controls.Add(rowTwoCellOne)
		table.Controls.Add(rowTwo)
		table.BorderWidth = 2
		table.BorderColor = Color.Red
		table.BackColor = System.Drawing.ColorTranslator.FromHtml("#fcc")
		

		messagePanel.Controls.Add(table)
	End Sub
End Class