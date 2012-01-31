Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Text

Public Partial Class Listener
	Inherits System.Web.UI.Page
	Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
		
		Dim random As New Random()
		Dim currentServerTime As DateTime = DateTime.UtcNow
		Dim receivedTime As String = currentServerTime.ToString("HH-MM-SS")
		Dim receivedDate As String = currentServerTime.ToString("MM-dd-yyyy")
		Dim inputStreamContents As String
		Dim stringLength As Integer
		Dim strRead As Integer
		Dim str As System.IO.Stream = Request.InputStream
		stringLength = Convert.ToInt32(str.Length)
		Dim stringArray As Byte() = New Byte(stringLength - 1) {}
		strRead = str.Read(stringArray, 0, stringLength)
		inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray)
            'System.IO.File.WriteAllText("D:\Webs\wincod\APIPlatform\2\0\1\PROD\Vb-RESTful\mms\app3\MoImages\WriteText.txt", inputStreamContents )
		Dim splitData As String() = Regex.Split(inputStreamContents, "</SenderAddress>")
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
		'Give Images directory as path example: "D:\folder"
		If Directory.Exists("D:\Webs\wincod\APIPlatform\2\0\1\PROD\Vb-RESTful\mms\app3\MoImages\") Then

		Else
			'Give Images directory as path example: "D:\folder", same as above value
			System.IO.Directory.CreateDirectory("D:\Webs\wincod\APIPlatform\2\0\1\PROD\Vb-RESTful\mms\app3\MoImages\")
		End If
			Dim fileNameToSave As String = "From_" & senderAddress & "_At_" + receivedTime & "_UTC_On_" & receivedDate & random.[Next]() 
			'Give Images directory as first argument example: "D:\folder", same as above value
            	'Dim fileNameToSave As String = "Testing"
			Dim fs As New FileStream("D:\Webs\wincod\APIPlatform\2\0\1\PROD\Vb-RESTful\mms\app3\MoImages\" + fileNameToSave + "." + type, FileMode.CreateNew, FileAccess.Write)
			fs.Write(todecode_byte, 0, todecode_byte.Length)
			fs.Close()
		'random.Next() 
	End Sub
End Class