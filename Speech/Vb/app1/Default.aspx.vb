' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System.Collections.Generic
Imports System.Configuration
Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Web.Script.Serialization
Imports System.Web.UI.WebControls

#End Region

''' <summary>
''' Speech application
''' </summary>
Partial Public Class Speech_App1
    Inherits System.Web.UI.Page
#Region "Class variables and Data structures"
    ''' <summary>
    ''' Temporary variables for processing
    ''' </summary>
    Private fqdn As String, accessTokenFilePath As String

    ''' <summary>
    ''' Temporary variables for processing
    ''' </summary>
    Private apiKey As String, secretKey As String, accessToken As String, scope As String, refreshToken As String, refreshTokenExpiryTime As String, _
     accessTokenExpiryTime As String

    ''' <summary>
    ''' variable for having the posted file.
    ''' </summary>
    Private fileToConvert As String

    ''' <summary>
    ''' Flag for deletion of the temporary file
    ''' </summary>
    Private deleteFile As Boolean

    ''' <summary>
    ''' Gets or sets the value of refreshTokenExpiresIn
    ''' </summary>
    Private refreshTokenExpiresIn As Integer

    ''' <summary>
    ''' Access Token Types
    ''' </summary>
    Public Enum AccessType
        ''' <summary>
        ''' Access Token Type is based on Client Credential Mode
        ''' </summary>
        ClientCredential

        ''' <summary>
        ''' Access Token Type is based on Refresh Token
        ''' </summary>
        RefreshToken
    End Enum
#End Region

#Region "Events"

    ''' <summary>
    ''' This function is called when the applicaiton page is loaded into the browser.
    ''' This function reads the web.config and gets the values of the attributes
    ''' </summary>
    ''' <param name="sender">Button that caused this event</param>
    ''' <param name="e">Event that invoked this function</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)
        If Not Page.IsPostBack Then
            resultsPanel.Visible = False
        End If

        Dim currentServerTime As DateTime = DateTime.UtcNow
        lblServerTime.Text = [String].Format("{0:ddd, MMM dd, yyyy HH:mm:ss}", currentServerTime) & " UTC"

        Me.ReadConfigFile()

        Me.deleteFile = False
    End Sub

    ''' <summary>
    ''' Method that calls SpeechToText api when user clicked on submit button
    ''' </summary>
    ''' <param name="sender">sender that invoked this event</param>
    ''' <param name="e">eventargs of the button</param>
    Protected Sub BtnSubmit_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            resultsPanel.Visible = False

            If String.IsNullOrEmpty(fileUpload1.FileName) Then
                If Not String.IsNullOrEmpty(ConfigurationManager.AppSettings("DefaultFile")) Then
                    Me.fileToConvert = Request.MapPath(ConfigurationManager.AppSettings("DefaultFile"))
                Else
                    Me.DrawPanelForFailure(statusPanel, "No file selected, and default file is not defined in web.config")
                    Return
                End If
            Else
                Dim fileName As String = fileUpload1.FileName
                If fileName.CompareTo("default.wav") = 0 Then
                    fileName = "1" + fileUpload1.FileName
                End If
                fileUpload1.PostedFile.SaveAs(Request.MapPath("") & "/" & fileName)
                Me.fileToConvert = Request.MapPath("").ToString() & "/" & fileName
                Me.deleteFile = True
            End If

            Dim IsValid As Boolean = Me.IsValidFile(Me.fileToConvert)

            If IsValid = False Then
                Return
            End If

            IsValid = Me.ReadAndGetAccessToken()
            If IsValid = False Then
                Me.DrawPanelForFailure(statusPanel, "Unable to get access token")
                Return
            End If

            Me.ConvertToSpeech()
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
            Return
        End Try
    End Sub

#End Region

#Region "Access Token Related Functions"

    ''' <summary>
    ''' Read parameters from configuraton file
    ''' </summary>
    ''' <returns>true/false; true if all required parameters are specified, else false</returns>
    Private Function ReadConfigFile() As Boolean
        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "~\SpeechApp1AccessToken.txt"
        End If

        Me.fqdn = ConfigurationManager.AppSettings("FQDN")
        If String.IsNullOrEmpty(Me.fqdn) Then
            Me.DrawPanelForFailure(statusPanel, "FQDN is not defined in configuration file")
            Return False
        End If

        Me.apiKey = ConfigurationManager.AppSettings("api_key")
        If String.IsNullOrEmpty(Me.apiKey) Then
            Me.DrawPanelForFailure(statusPanel, "api_key is not defined in configuration file")
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key")
        If String.IsNullOrEmpty(Me.secretKey) Then
            Me.DrawPanelForFailure(statusPanel, "secret_key is not defined in configuration file")
            Return False
        End If

        Me.scope = ConfigurationManager.AppSettings("scope")
        If String.IsNullOrEmpty(Me.scope) Then
            Me.scope = "SPEECH"
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function reads the Access Token File and stores the values of access token, expiry seconds
    ''' refresh token, last access token time and refresh token expiry time
    ''' </summary>
    ''' <returns>
    ''' This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    ''' </returns>
    Private Function ReadAccessTokenFile() As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamReader As StreamReader = Nothing
        Try
            fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read)
            streamReader = New StreamReader(fileStream)
            Me.accessToken = streamReader.ReadLine()
            Me.accessTokenExpiryTime = streamReader.ReadLine()
            Me.refreshToken = streamReader.ReadLine()
            Me.refreshTokenExpiryTime = streamReader.ReadLine()
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
            Return False
        Finally
            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
        End Try

        If (Me.accessToken Is Nothing) OrElse (Me.accessTokenExpiryTime Is Nothing) OrElse (Me.refreshToken Is Nothing) OrElse (Me.refreshTokenExpiryTime Is Nothing) Then
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' This function validates the expiry of the access token and refresh token.
    ''' function compares the current time with the refresh token taken time, if current time is greater then returns INVALID_REFRESH_TOKEN
    ''' function compares the difference of last access token taken time and the current time with the expiry seconds, if its more, returns INVALID_ACCESS_TOKEN    
    ''' otherwise returns VALID_ACCESS_TOKEN
    ''' </summary>
    ''' <returns>string, which specifies the token validity</returns>
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
    ''' Get the access token based on Access Type
    ''' </summary>
    ''' <param name="type">Access Type - either client Credential or Refresh Token</param>
    ''' <returns>true/false; true - if success on getting access token, else false</returns>
    Private Function GetAccessToken(ByVal type As AccessType) As Boolean
        Dim fileStream As FileStream = Nothing
        Dim postStream As Stream = Nothing
        Dim streamWriter As StreamWriter = Nothing


        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()

            Dim accessTokenRequest As WebRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.fqdn & "/oauth/token")
            accessTokenRequest.Method = "POST"

            Dim oauthParameters As String = String.Empty
            If type = AccessType.ClientCredential Then
                oauthParameters = "client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=" & Me.scope
            Else
                oauthParameters = "grant_type=refresh_token&client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&refresh_token=" & Me.refreshToken
            End If
            accessTokenRequest.ContentType = "application/x-www-form-urlencoded"

            Dim encoding As New UTF8Encoding()
            Dim postBytes As Byte() = encoding.GetBytes(oauthParameters)
            accessTokenRequest.ContentLength = postBytes.Length

            postStream = accessTokenRequest.GetRequestStream()
            postStream.Write(postBytes, 0, postBytes.Length)

            Dim accessTokenResponse As WebResponse = accessTokenRequest.GetResponse()
            Using accessTokenResponseStream As New StreamReader(accessTokenResponse.GetResponseStream())
                Dim jsonAccessToken As String = accessTokenResponseStream.ReadToEnd()
                Dim deserializeJsonObject As New JavaScriptSerializer()

                Dim deserializedJsonObj As AccessTokenResponse = DirectCast(deserializeJsonObject.Deserialize(jsonAccessToken, GetType(AccessTokenResponse)), AccessTokenResponse)
                Me.accessToken = deserializedJsonObj.access_token
                Me.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString()
                Me.refreshToken = deserializedJsonObj.refresh_token

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
                streamWriter.WriteLine(Me.accessTokenExpiryTime)
                streamWriter.WriteLine(Me.refreshToken)
                streamWriter.WriteLine(Me.refreshTokenExpiryTime)

                ' Close and clean up the StreamReader
                accessTokenResponseStream.Close()
                Return True
            End Using
        Catch we As WebException
            Dim errorResponse As String = String.Empty

            Try
                Using sr2 As New StreamReader(we.Response.GetResponseStream())
                    errorResponse = sr2.ReadToEnd()
                    sr2.Close()
                End Using
            Catch
                errorResponse = "Unable to get response"
            End Try

            Me.DrawPanelForFailure(statusPanel, errorResponse & Environment.NewLine & we.ToString())
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.Message)
        Finally
            If postStream IsNot Nothing Then
                postStream.Close()
            End If

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
    ''' Neglect the ssl handshake error with authentication server 
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
    ''' Read access token file and validate the access token
    ''' </summary>
    ''' <returns>true/false; true if access token is valid, else false</returns>
    Private Function ReadAndGetAccessToken() As Boolean
        Dim result As Boolean = True

        If Me.ReadAccessTokenFile() = False Then
            result = Me.GetAccessToken(AccessType.ClientCredential)
        Else
            Dim tokenValidity As String = Me.IsTokenValid()
            If tokenValidity = "REFRESH_TOKEN" Then
                result = Me.GetAccessToken(AccessType.RefreshToken)
            ElseIf String.Compare(tokenValidity, "INVALID_ACCESS_TOKEN") = 0 Then
                result = Me.GetAccessToken(AccessType.ClientCredential)
            End If
        End If

        If String.IsNullOrEmpty(Me.accessToken) Then
            result = False
        End If

        Return result
    End Function

#End Region

#Region "Display status Functions"

    ''' <summary>
    ''' Display success message
    ''' </summary>
    ''' <param name="panelParam">Panel to draw success message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForSuccess(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Clear()
        End If

        Dim table As New Table()
        table.CssClass = "successWide"
        table.Font.Name = "Sans-serif"
        table.Font.Size = 9
        Dim rowOne As New TableRow()
        Dim rowOneCellOne As New TableCell()
        rowOneCellOne.Font.Bold = True
        rowOneCellOne.Text = "SUCCESS:"
        rowOne.Controls.Add(rowOneCellOne)
        table.Controls.Add(rowOne)
        Dim rowTwo As New TableRow()
        Dim rowTwoCellTwo As New TableCell()
        rowTwoCellTwo.Text = message.ToString()
        rowTwo.Controls.Add(rowTwoCellTwo)
        table.Controls.Add(rowTwo)
        panelParam.Controls.Add(table)
    End Sub

    ''' <summary>
    ''' Displays error message
    ''' </summary>
    ''' <param name="panelParam">Panel to draw success message</param>
    ''' <param name="message">Message to display</param>
    Private Sub DrawPanelForFailure(ByVal panelParam As Panel, ByVal message As String)
        If panelParam.HasControls() Then
            panelParam.Controls.Clear()
        End If

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
        panelParam.Controls.Add(table)
    End Sub

#End Region

#Region "Speech Service Functions"

    ''' <summary>
    ''' Verifies whether the given file satisfies the criteria for speech api
    ''' </summary>
    ''' <param name="file">Name of the sound file</param>
    ''' <returns>true/false; true if valid file, else false</returns>
    Private Function IsValidFile(ByVal file As String) As Boolean
        Dim isValid As Boolean = False

        ' Verify File Extension
        Dim extension As String = System.IO.Path.GetExtension(file)

        If Not String.IsNullOrEmpty(extension) AndAlso (extension.Equals(".wav") OrElse extension.Equals(".amr")) Then
            isValid = True
        Else
            Me.DrawPanelForFailure(statusPanel, "Invalid file specified. Valid file formats are .wav and .amr")
        End If

        Return isValid
    End Function

    ''' <summary>
    ''' Content type based on the file extension.
    ''' </summary>
    ''' <param name="extension">file extension</param>
    ''' <returns>the Content type mapped to the extension"/> summed memory stream</returns>
    Private Function MapContentTypeFromExtension(ByVal extension As String) As String
        Dim extensionToContentTypeMapping As New Dictionary(Of String, String)() From { _
         {".jpg", "image/jpeg"}, _
         {".bmp", "image/bmp"}, _
         {".mp3", "audio/mp3"}, _
         {".m4a", "audio/m4a"}, _
         {".gif", "image/gif"}, _
         {".3gp", "video/3gpp"}, _
         {".3g2", "video/3gpp2"}, _
         {".wmv", "video/x-ms-wmv"}, _
         {".m4v", "video/x-m4v"}, _
         {".amr", "audio/amr"}, _
         {".mp4", "video/mp4"}, _
         {".avi", "video/x-msvideo"}, _
         {".mov", "video/quicktime"}, _
         {".mpeg", "video/mpeg"}, _
         {".wav", "audio/wav"}, _
         {".aiff", "audio/x-aiff"}, _
         {".aifc", "audio/x-aifc"}, _
         {".midi", ".midi"}, _
         {".au", "audio/basic"}, _
         {".xwd", "image/x-xwindowdump"}, _
         {".png", "image/png"}, _
         {".tiff", "image/tiff"}, _
         {".ief", "image/ief"}, _
         {".txt", "text/plain"}, _
         {".html", "text/html"}, _
         {".vcf", "text/x-vcard"}, _
         {".vcs", "text/x-vcalendar"}, _
         {".mid", "application/x-midi"}, _
         {".imy", "audio/iMelody"} _
        }
        If extensionToContentTypeMapping.ContainsKey(extension) Then
            Return extensionToContentTypeMapping(extension)
        Else
            Throw New ArgumentException("invalid attachment extension")
        End If
    End Function

    ''' <summary>
    ''' This function invokes api SpeechToText to convert the given wav amr file and displays the result.
    ''' </summary>
    Private Sub ConvertToSpeech()
        Dim postStream As Stream = Nothing
        Dim audioFileStream As FileStream = Nothing
        Try
            Dim mmsFilePath As String = Me.fileToConvert
            audioFileStream = New FileStream(mmsFilePath, FileMode.Open, FileAccess.Read)
            Dim reader As New BinaryReader(audioFileStream)
            Dim binaryData As Byte() = reader.ReadBytes(CInt(audioFileStream.Length))
            reader.Close()
            audioFileStream.Close()
            If binaryData IsNot Nothing Then
                Dim httpRequest As HttpWebRequest = DirectCast(WebRequest.Create(String.Empty & Me.fqdn & "/rest/1/SpeechToText"), HttpWebRequest)
                httpRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                httpRequest.Headers.Add("X-SpeechContext", "Generic")

                Dim contentType As String = Me.MapContentTypeFromExtension(Path.GetExtension(mmsFilePath))
                httpRequest.ContentLength = binaryData.Length
                httpRequest.ContentType = contentType
                httpRequest.Accept = "application/json"
                httpRequest.Method = "POST"
                httpRequest.KeepAlive = True

                postStream = httpRequest.GetRequestStream()
                postStream.Write(binaryData, 0, binaryData.Length)
                postStream.Close()

                Dim speechResponse As HttpWebResponse = DirectCast(httpRequest.GetResponse(), HttpWebResponse)
                Using streamReader As New StreamReader(speechResponse.GetResponseStream())
                    Dim speechResponseData As String = streamReader.ReadToEnd()
                    If Not String.IsNullOrEmpty(speechResponseData) Then
                        Dim deserializeJsonObject As New JavaScriptSerializer()
                        Dim deserializedJsonObj As SpeechResponse = DirectCast(deserializeJsonObject.Deserialize(speechResponseData, GetType(SpeechResponse)), SpeechResponse)
                        If deserializedJsonObj IsNot Nothing Then
                            resultsPanel.Visible = True
                            Me.DrawPanelForSuccess(statusPanel, "Response Parameters listed below")
                            Me.DisplayResult(deserializedJsonObj)
                        Else
                            Me.DrawPanelForFailure(statusPanel, "Empty speech to text response")
                        End If
                    Else
                        Me.DrawPanelForFailure(statusPanel, "Empty speech to text response")
                    End If

                    streamReader.Close()
                End Using
            Else
                Me.DrawPanelForFailure(statusPanel, "Empty speech to text response")
            End If
        Catch we As WebException
            Dim errorResponse As String = String.Empty

            Try
                Using sr2 As New StreamReader(we.Response.GetResponseStream())
                    errorResponse = sr2.ReadToEnd()
                    sr2.Close()
                End Using
            Catch
                errorResponse = "Unable to get response"
            End Try

            Me.DrawPanelForFailure(statusPanel, errorResponse & Environment.NewLine & we.ToString())
        Catch ex As Exception
            Me.DrawPanelForFailure(statusPanel, ex.ToString())
        Finally
            If (Me.deleteFile = True) AndAlso (File.Exists(Me.fileToConvert)) Then
                File.Delete(Me.fileToConvert)
                Me.deleteFile = False
            End If
            If postStream IsNot Nothing Then
                postStream.Close()
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Displays the result onto the page
    ''' </summary>
    ''' <param name="speechResponse">SpeechResponse received from api</param>
    Private Sub DisplayResult(ByVal speechResponse As SpeechResponse)
        lblResponseId.Text = speechResponse.Recognition.ResponseId
        For Each nbest As NBest In speechResponse.Recognition.NBest
            lblHypothesis.Text = nbest.Hypothesis
            lblLanguageId.Text = nbest.LanguageId
            lblResultText.Text = nbest.ResultText
            lblGrade.Text = nbest.Grade
            lblConfidence.Text = nbest.Confidence.ToString()

            Dim strText As String = "["
            For Each word As String In nbest.Words
                strText += """" & word & """, "
            Next
            strText = strText.Substring(0, strText.LastIndexOf(","))
            strText = strText & "]"

            lblWords.Text = If(nbest.Words IsNot Nothing, strText, String.Empty)

            lblWordScores.Text = "[" & String.Join(", ", nbest.WordScores.ToArray()) & "]"
        Next
    End Sub

#End Region
End Class

#Region "Access Token and Speech Response Data Structures"

''' <summary>
''' Access Token Data Structure
''' </summary>
Public Class AccessTokenResponse
    ''' <summary>
    ''' Gets or sets Access Token ID
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
    ''' Gets or sets Refresh Token ID
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
    ''' Gets or sets Expires in milli seconds
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
''' Speech Response to an audio file
''' </summary>
Public Class SpeechResponse
    ''' <summary>
    ''' Gets or sets the Recognition value returned by api
    ''' </summary>
    Public Property Recognition() As Recognition
        Get
            Return m_Recognition
        End Get
        Set(ByVal value As Recognition)
            m_Recognition = Value
        End Set
    End Property
    Private m_Recognition As Recognition
End Class

''' <summary>
''' Recognition returned by the server for Speech to text request.
''' </summary>
Public Class Recognition
    ''' <summary>
    ''' Gets or sets a unique string that identifies this particular transaction.
    ''' </summary>
    Public Property ResponseId() As String
        Get
            Return m_ResponseId
        End Get
        Set(ByVal value As String)
            m_ResponseId = Value
        End Set
    End Property
    Private m_ResponseId As String

    ''' <summary>
    ''' Gets or sets NBest Complex structure that holds the results of the transcription. Supports multiple transcriptions.
    ''' </summary>
    Public Property NBest() As List(Of NBest)
        Get
            Return m_NBest
        End Get
        Set(ByVal value As List(Of NBest))
            m_NBest = Value
        End Set
    End Property
    Private m_NBest As List(Of NBest)
End Class

''' <summary>
''' Complex structure that holds the results of the transcription. Supports multiple transcriptions.
''' </summary>
Public Class NBest
    ''' <summary>
    ''' Gets or sets the transcription of the audio. 
    ''' </summary>
    Public Property Hypothesis() As String
        Get
            Return m_Hypothesis
        End Get
        Set(ByVal value As String)
            m_Hypothesis = Value
        End Set
    End Property
    Private m_Hypothesis As String

    ''' <summary>
    ''' Gets or sets the language used to decode the Hypothesis. 
    ''' Represented using the two-letter ISO 639 language code, hyphen, two-letter ISO 3166 country code in lower case, e.g. “en-us”.
    ''' </summary>
    Public Property LanguageId() As String
        Get
            Return m_LanguageId
        End Get
        Set(ByVal value As String)
            m_LanguageId = Value
        End Set
    End Property
    Private m_LanguageId As String

    ''' <summary>
    ''' Gets or sets the confidence value of the Hypothesis, a value between 0.0 and 1.0 inclusive.
    ''' </summary>
    Public Property Confidence() As Double
        Get
            Return m_Confidence
        End Get
        Set(ByVal value As Double)
            m_Confidence = Value
        End Set
    End Property
    Private m_Confidence As Double

    ''' <summary>
    ''' Gets or sets a machine-readable string indicating an assessment of utterance/result quality and the recommended treatment of the Hypothesis. 
    ''' The assessment reflects a confidence region based on prior experience with similar results. 
    ''' accept - the hypothesis value has acceptable confidence
    ''' confirm - the hypothesis should be independently confirmed due to lower confidence
    ''' reject - the hypothesis should be rejected due to low confidence
    ''' </summary>
    Public Property Grade() As String
        Get
            Return m_Grade
        End Get
        Set(ByVal value As String)
            m_Grade = Value
        End Set
    End Property
    Private m_Grade As String

    ''' <summary>
    ''' Gets or sets a text string prepared according to the output domain of the application package. 
    ''' The string will generally be a formatted version of the hypothesis, but the words may have been altered through 
    ''' insertions/deletions/substitutions to make the result more readable or usable for the client.  
    ''' </summary>
    Public Property ResultText() As String
        Get
            Return m_ResultText
        End Get
        Set(ByVal value As String)
            m_ResultText = Value
        End Set
    End Property
    Private m_ResultText As String

    ''' <summary>
    ''' Gets or sets the words of the Hypothesis split into separate strings.  
    ''' May omit some of the words of the Hypothesis string, and can be empty.  Never contains words not in hypothesis string.  
    ''' </summary>
    Public Property Words() As List(Of String)
        Get
            Return m_Words
        End Get
        Set(ByVal value As List(Of String))
            m_Words = Value
        End Set
    End Property
    Private m_Words As List(Of String)

    ''' <summary>
    ''' Gets or sets the confidence scores for each of the strings in the words array.  Each value ranges from 0.0 to 1.0 inclusive.
    ''' </summary>
    Public Property WordScores() As List(Of Double)
        Get
            Return m_WordScores
        End Get
        Set(ByVal value As List(Of Double))
            m_WordScores = Value
        End Set
    End Property
    Private m_WordScores As List(Of Double)
End Class
#End Region
