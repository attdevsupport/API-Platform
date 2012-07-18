' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Text

#End Region

''' <summary>
''' MMSApp3_Listener class
''' </summary>
Partial Public Class MMSApp3_Listener
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Event, that triggers when the applicaiton page is loaded into the browser
    ''' Listens to server and stores the mms messages in server
    ''' </summary>
    ''' <param name="sender">object, that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Dim fileStream As FileStream = Nothing
        Try
            Dim random As New Random()
            Dim currentServerTime As DateTime = DateTime.UtcNow

            Dim receivedTime As String = currentServerTime.ToString("HH-MM-SS")
            Dim receivedDate As String = currentServerTime.ToString("MM-dd-yyyy")

            Dim inputStreamContents As String
            Dim stringLength As Integer
            Dim strRead As Integer

            Dim stream As Stream = Request.InputStream
            stringLength = Convert.ToInt32(stream.Length)

            Dim stringArray As Byte() = New Byte(stringLength - 1) {}
            strRead = stream.Read(stringArray, 0, stringLength)
            inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray)

            Dim splitData As String() = Regex.Split(inputStreamContents, "</SenderAddress>")
            Dim data As String = splitData(0).ToString()
            Dim senderAddress As String = inputStreamContents.Substring(data.IndexOf("tel:") + 4, data.Length - (data.IndexOf("tel:") + 4))
            Dim parts As String() = Regex.Split(inputStreamContents, "--Nokia-mm-messageHandler-BoUnDaRy")
            Dim lowerParts As String() = Regex.Split(parts(2), "BASE64")
            Dim imageType As String() = Regex.Split(lowerParts(0), "image/")
            Dim indexOfSemicolon As Integer = imageType(1).IndexOf(";")
            Dim type As String = imageType(1).Substring(0, indexOfSemicolon)
            Dim encoder As UTF8Encoding = New System.Text.UTF8Encoding()
            Dim utf8Decode As Decoder = encoder.GetDecoder()

            Dim todecode_byte As Byte() = Convert.FromBase64String(lowerParts(1))

            If Not Directory.Exists(Request.MapPath(ConfigurationManager.AppSettings("ImageDirectory"))) Then
                Directory.CreateDirectory(Request.MapPath(ConfigurationManager.AppSettings("ImageDirectory")))
            End If

            Dim fileNameToSave As String = "From_" & senderAddress.Replace("+", "") & "_At_" & receivedTime & "_UTC_On_" & receivedDate & random.[Next]()
            fileStream = New FileStream(Request.MapPath(ConfigurationManager.AppSettings("ImageDirectory")) & fileNameToSave & "." & type, FileMode.CreateNew, FileAccess.Write)
            fileStream.Write(todecode_byte, 0, todecode_byte.Length)
        Catch
        Finally
            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

    End Sub
End Class
