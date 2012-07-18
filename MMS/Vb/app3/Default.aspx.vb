' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System.Collections.Generic
Imports System.Configuration
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Web.UI.WebControls

#End Region

''' <summary>
''' MMS_App3 class
''' </summary>
Partial Public Class MMS_App3
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Instance Variables for local processing
    ''' </summary>
    Private shortCode As String, directoryPath As String

    ''' <summary>
    ''' Instance Variables for local processing
    ''' </summary>
    Private numOfFilesToDisplay As Integer

    ''' <summary>
    ''' Event, that triggers when the applicaiton page is loaded into the browser, reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Dim currentServerTime As DateTime = DateTime.UtcNow
        lblServerTime.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"
        Me.ReadConfigFile()
        Me.GetMmsFiles()
    End Sub

    ''' <summary>
    ''' Gets the list of files from directory and displays them in the page
    ''' </summary>
    Private Sub GetMmsFiles()
        Dim columnCount As Integer = 0

        Dim tableRow As TableRow = Nothing
        Dim secondRow As TableRow = Nothing

        Dim totalFiles As Integer = 0
        Dim pictureTable As New Table()
        Dim tableControl As New Table()

        Dim directory As New DirectoryInfo(Request.MapPath(Me.directoryPath))
        Dim imageList As List(Of FileInfo) = Nothing
        Try
            imageList = directory.GetFiles().OrderBy(Function(f) f.CreationTime).ToList()
        Catch
        End Try

        If imageList Is Nothing Then
            lbl_TotalCount.Text = "0"
            Return
        End If

        totalFiles = imageList.Count

        Dim fileShownMessage As String = imageList.Count.ToString()
        lbl_TotalCount.Text = fileShownMessage
        Dim fileCountIndex As Integer = 0
        For Each file As FileInfo In imageList
            If fileCountIndex = Me.numOfFilesToDisplay Then
                Exit For
            End If

            If columnCount = 0 Then
                tableRow = New TableRow()
                secondRow = New TableRow()
                Dim tableCellImage As New TableCell()
                Dim image1 As New System.Web.UI.WebControls.Image()
                image1.ImageUrl = String.Format("{0}{1}", Me.directoryPath, file.Name)
                image1.Width = 150
                image1.Height = 150
                tableCellImage.Controls.Add(image1)
                tableRow.Controls.Add(tableCellImage)

                Dim tableCellSubject As New TableCell()
                tableCellSubject.Text = file.Name
                tableCellSubject.Width = 150
                secondRow.Controls.Add(tableCellSubject)
                columnCount += 1
            Else
                Dim tableCellImage As New TableCell()
                Dim image1 As New System.Web.UI.WebControls.Image()
                image1.ImageUrl = String.Format("{0}{1}", Me.directoryPath, file.Name)
                image1.Width = 150
                image1.Height = 150
                tableCellImage.Controls.Add(image1)
                tableRow.Controls.Add(tableCellImage)
                Dim tableCellSubject As New TableCell()
                tableCellSubject.Text = file.Name
                tableCellSubject.Width = 150
                secondRow.Controls.Add(tableCellSubject)
                columnCount += 1
                If columnCount = 5 Then
                    columnCount = 0
                End If

                fileCountIndex += 1
            End If

            pictureTable.Controls.Add(tableRow)
            pictureTable.Controls.Add(secondRow)
        Next

        messagePanel.Controls.Add(pictureTable)
    End Sub

    ''' <summary>
    ''' This method reads config file and assigns values to local variables
    ''' </summary>
    ''' <returns>true/false, true- if able to read from config file</returns>
    Private Function ReadConfigFile() As Boolean
        Me.shortCode = ConfigurationManager.AppSettings("short_code")
        If String.IsNullOrEmpty(Me.shortCode) Then
            Me.DrawPanelForFailure("short_code is not defined in configuration file")
            Return False
        End If

        shortCodeLabel.Text = Me.shortCode

        Me.directoryPath = ConfigurationManager.AppSettings("ImageDirectory")
        If String.IsNullOrEmpty(Me.directoryPath) Then
            Me.DrawPanelForFailure("ImageDirectory is not defined in configuration file")
            Return False
        End If

        If ConfigurationManager.AppSettings("NumOfFilesToDisplay") Is Nothing Then
            Me.numOfFilesToDisplay = 5
        Else
            Me.numOfFilesToDisplay = Convert.ToInt32(ConfigurationManager.AppSettings("NumOfFilesToDisplay"))
        End If

        Return True
    End Function

    ''' <summary>
    ''' Displays error message
    ''' </summary>
    ''' <param name="message">string, message to be displayed</param>
    Private Sub DrawPanelForFailure(ByVal message As String)
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

        messagePanel.Controls.Add(table)
    End Sub
End Class
