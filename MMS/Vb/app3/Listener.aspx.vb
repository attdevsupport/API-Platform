Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Text

Partial Public Class Listener
    Inherits System.Web.UI.Page
    'This function is called when application is getting loaded, which reads the MO data and parse the data and saves image to the MoImages directory
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Dim random As New Random()

        Dim mmsFilePath As String = Request.MapPath("MoImages\")

        Dim inputStreamContents As String

        Dim stringLength As Integer
        Dim strRead As Integer

        Dim str As System.IO.Stream = Request.InputStream


        stringLength = Convert.ToInt32(str.Length)

        Dim stringArray As Byte() = New Byte(stringLength - 1) {}

        strRead = str.Read(stringArray, 0, stringLength)

        inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray)

        Dim splitData As String() = Regex.Split(inputStreamContents, "</sender-address>")
        Dim data As String = splitData(0).ToString()

        Dim senderAddress As [String] = inputStreamContents.Substring(data.IndexOf("tel:") + 4, data.Length - (data.IndexOf("tel:") + 4))

        Dim parts As [String]() = Regex.Split(inputStreamContents, "--Nokia-mm-messageHandler-BoUnDaRy")
        Dim lowerParts As [String]() = Regex.Split(parts(2), "BASE64")
        Dim imageType As [String]() = Regex.Split(lowerParts(0), "image/")
        Dim indexOfSemicolon As Integer = imageType(1).IndexOf(";")

        Dim type As String = imageType(1).Substring(0, indexOfSemicolon)

        Dim encoder As New System.Text.UTF8Encoding()
        Dim utf8Decode As System.Text.Decoder = encoder.GetDecoder()

        Dim todecode_byte As Byte() = Convert.FromBase64String(lowerParts(1))
        Dim fs As New FileStream(mmsFilePath & random.[Next]() & "." & type, FileMode.CreateNew, FileAccess.Write)
        fs.Write(todecode_byte, 0, todecode_byte.Length)
        fs.Close()
    End Sub
End Class