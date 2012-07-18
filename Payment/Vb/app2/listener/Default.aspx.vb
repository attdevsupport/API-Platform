' <copyright file="Default.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"

Imports System.Collections
Imports System.Collections.Generic
Imports System.Configuration
Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Web.Script.Serialization
Imports System.Xml.Serialization

#End Region

''' <summary>
''' Payment App2 Listener class
''' </summary>
Partial Public Class PaymentApp2_Listener
    Inherits System.Web.UI.Page
    ''' <summary>
    ''' Local variables for processing of request stream
    ''' </summary>
    Private apiKey As String, endPoint As String, secretKey As String, accessTokenFilePath As String, accessToken As String, expirySeconds As String, _
     refreshToken As String, accessTokenExpiryTime As String, refreshTokenExpiryTime As String, notificationDetailsFile As String

    ''' <summary>
    ''' Local variables for processing of request stream
    ''' </summary>    
    Private notificationId As String

    ''' <summary>
    ''' Local variables for processing of request stream
    ''' </summary>
    Private notificationDetailsStreamWriter As StreamWriter

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
    ''' Default method, that gets called upon loading the page.
    ''' </summary>
    ''' <param name="sender">object that invoked this method</param>
    ''' <param name="e">Event arguments</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        ServicePointManager.ServerCertificateValidationCallback = New RemoteCertificateValidationCallback(AddressOf CertificateValidationCallBack)

        Try
            Dim inputStream As Stream = Request.InputStream

            'int streamLength = Convert.ToInt32(inputStream.Length);
            'byte[] stringArray = new byte[streamLength];
            'inputStream.Read(stringArray, 0, streamLength);

            'string xmlString = System.Text.Encoding.UTF8.GetString(stringArray);

            'string xmlString = "<?xml version='1.0' encoding='UTF-8'?><hub:notifications xmlns:hub=\"http://hub.amdocs.com\"><hub:notificationId>37231c36-fdf5-464d-9734-aa0389ffc9a4</hub:notificationId></hub:notifications>";
            'XmlSerializer serializer = new XmlSerializer(typeof(Notification));
            'UTF8Encoding encoding = new UTF8Encoding();
            'MemoryStream ms = new MemoryStream(encoding.GetBytes(xmlString));
            'Notifications notificationIds = (Notifications)serializer.Deserialize(ms);

            Dim listOfNotificationIds As New ArrayList()
            listOfNotificationIds = Me.GetNotificationIds(inputStream)

            If Not Me.ReadConfigFile() Then
                'this.LogError("Unable to read config file");
                Return
            End If

            If Not Me.ReadAndGetAccessToken() Then
                'this.LogError("Unable to get access token");
                Return
            End If


            Dim noOfItems As Integer = listOfNotificationIds.Count

            Dim notificationIdIndex As Integer = 0
            While noOfItems > 0
                noOfItems -= 1
                Me.notificationId = listOfNotificationIds(notificationIdIndex).ToString()

                ' Get notification details
                Dim objRequest As WebRequest = DirectCast(System.Net.WebRequest.Create(Me.endPoint & "/rest/3/Commerce/Payment/Notifications/" & Me.notificationId), WebRequest)
                objRequest.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                objRequest.Method = "GET"
                objRequest.ContentType = "application/json"
                Dim notificationResponse As IDictionary(Of String, Object)
                Dim notificationRespone As WebResponse = DirectCast(objRequest.GetResponse(), WebResponse)
                Using notificationResponseStream As New StreamReader(notificationRespone.GetResponseStream())
                    Dim notificationResponseData As String = notificationResponseStream.ReadToEnd()
                    'this.LogError(notificationResponseData);
                    'using (StreamWriter streamWriter = File.AppendText(Request.MapPath(Mess.txt)))
                    '{
                    ' streamWriter.Write(notificationResponseData);
                    ' streamWriter.WriteLine("***************************************");
                    ' streamWriter.Close();
                    ' }
                    Dim deserializeJsonObject As New JavaScriptSerializer()


                    notificationResponse = DirectCast(deserializeJsonObject.Deserialize(notificationResponseData, GetType(IDictionary(Of String, Object))), IDictionary(Of String, Object))
                End Using
                notificationRespone.Close()

                Dim acknowledgeNotificationRequestObj As WebRequest = DirectCast(System.Net.WebRequest.Create(Me.endPoint & "/rest/3/Commerce/Payment/Notifications/" & Me.notificationId), WebRequest)
                acknowledgeNotificationRequestObj.Method = "PUT"
                acknowledgeNotificationRequestObj.Headers.Add("Authorization", "Bearer " & Me.accessToken)
                acknowledgeNotificationRequestObj.ContentType = "application/json"
                Dim encoding As New UTF8Encoding()
                Dim postBytes As Byte() = encoding.GetBytes("")
                acknowledgeNotificationRequestObj.ContentLength = postBytes.Length
                Dim postStream As Stream = acknowledgeNotificationRequestObj.GetRequestStream()
                postStream.Write(postBytes, 0, postBytes.Length)
                postStream.Close()
                Dim acknowledgeNotificationRespone As WebResponse = DirectCast(acknowledgeNotificationRequestObj.GetResponse(), WebResponse)
                Using acknowledgeNotificationResponseStream As New StreamReader(acknowledgeNotificationRespone.GetResponseStream())
                    Dim notificationType As String = String.Empty
                    Dim originalTransactionId As String = String.Empty

                    For Each keyValue As KeyValuePair(Of String, Object) In notificationResponse
                        If keyValue.Key = "GetNotificationResponse" Then
                            Dim getNotificationResponse As IDictionary(Of String, Object) = DirectCast(keyValue.Value, IDictionary(Of String, Object))
                            For Each notificationResponseKeyValue As KeyValuePair(Of String, Object) In getNotificationResponse
                                If notificationResponseKeyValue.Key = "OriginalTransactionId" Then
                                    originalTransactionId = notificationResponseKeyValue.Value.ToString()
                                End If

                                If notificationResponseKeyValue.Key = "NotificationType" Then
                                    notificationType = notificationResponseKeyValue.Value.ToString()
                                End If
                            Next
                            Using notificationDetailsStreamWriter = File.AppendText(Request.MapPath(Me.notificationDetailsFile))
                                notificationDetailsStreamWriter.WriteLine(notificationId & ":" & notificationType & ":" & originalTransactionId & "$")
                                notificationDetailsStreamWriter.Close()
                            End Using
                        End If
                    Next
                    Dim acknowledgeNotificationResponseData As String = acknowledgeNotificationResponseStream.ReadToEnd()
                    'this.LogError("Successfully acknowledge to " + this.notificationId + "***" + "\n");
                    acknowledgeNotificationResponseStream.Close()
                End Using
                notificationIdIndex += 1
            End While
        Catch we As WebException
            If we.Response IsNot Nothing Then
                'this.LogError(new StreamReader(stream).ReadToEnd());                    
                Using stream As Stream = we.Response.GetResponseStream()
                End Using
            End If
            'this.LogError(ex.ToString());
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Logs error message onto file
    ''' </summary>
    ''' <param name="text">Text to be logged</param>
    Private Sub LogError(ByVal text As String)
        File.AppendAllText(Request.MapPath("errorInNotification.txt"), Environment.NewLine & DateTime.Now.ToString() & ": " & text)
    End Sub

    ''' <summary>
    ''' This function reads the Access Token File and stores the values of access token, expiry seconds
    ''' refresh token, last access token time and refresh token expiry time
    ''' This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
    ''' </summary>
    ''' <returns>Returns Boolean</returns>
    Private Function ReadAccessTokenFile() As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamReader As StreamReader = Nothing

        Try
            fileStream = New FileStream(Request.MapPath(Me.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read)
            streamReader = New StreamReader(fileStream)
            Me.accessToken = streamReader.ReadLine()
            Me.expirySeconds = streamReader.ReadLine()
            Me.refreshToken = streamReader.ReadLine()
            Me.accessTokenExpiryTime = streamReader.ReadLine()
            Me.refreshTokenExpiryTime = streamReader.ReadLine()
        Catch
            'this.LogError("Unable to read access token file");
            Return False
        Finally
            If streamReader IsNot Nothing Then
                streamReader.Close()
            End If

            If fileStream IsNot Nothing Then
                fileStream.Close()
            End If
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
    Private Function GetAccessToken(ByVal type As Integer) As Boolean
        Dim fileStream As FileStream = Nothing
        Dim streamWriter As StreamWriter = Nothing

        Try
            Dim currentServerTime As DateTime = DateTime.UtcNow.ToLocalTime()
            Dim accessTokenRequest As WebRequest = Nothing
            If type = 1 Then
                accessTokenRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/token?client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&grant_type=client_credentials&scope=PAYMENT")
            ElseIf type = 2 Then
                accessTokenRequest = System.Net.HttpWebRequest.Create(String.Empty & Me.endPoint & "/oauth/token?grant_type=refresh_token&client_id=" & Me.apiKey & "&client_secret=" & Me.secretKey & "&refresh_token=" & Me.refreshToken)
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
        Catch we As WebException
            If we.Response IsNot Nothing Then
                Using stream As Stream = we.Response.GetResponseStream()
                    'this.LogError(new StreamReader(stream).ReadToEnd());
                    Throw we
                End Using
            End If
        Catch ex As Exception
            Throw ex
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
    Private Function ReadAndGetAccessToken() As Boolean
        Dim result As Boolean = True
        If Me.ReadAccessTokenFile() = False Then
            result = Me.GetAccessToken(1)
        Else
            Dim tokenValidity As String = Me.IsTokenValid()
            If tokenValidity.CompareTo("REFRESH_TOKEN") = 0 Then
                result = Me.GetAccessToken(2)
            ElseIf tokenValidity.Equals("INVALID_ACCESS_TOKEN") Then
                result = Me.GetAccessToken(1)
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

#End Region

    ''' <summary>
    ''' Reads from config file and assigns to local variables
    ''' </summary>
    ''' <returns>true/false; true if able to read all values, false otherwise</returns>
    Private Function ReadConfigFile() As Boolean
        Me.apiKey = ConfigurationManager.AppSettings("api_key").ToString()
        If String.IsNullOrEmpty(Me.apiKey) Then
            Return False
        End If

        Me.endPoint = ConfigurationManager.AppSettings("endPoint").ToString()
        If String.IsNullOrEmpty(Me.endPoint) Then
            Return False
        End If

        Me.secretKey = ConfigurationManager.AppSettings("secret_key").ToString()
        If String.IsNullOrEmpty(Me.secretKey) Then
            Return False
        End If

        Me.accessTokenFilePath = ConfigurationManager.AppSettings("AccessTokenFilePath")
        If String.IsNullOrEmpty(Me.accessTokenFilePath) Then
            Me.accessTokenFilePath = "~\PayApp1AccessToken.txt"
        End If

        Me.notificationDetailsFile = ConfigurationManager.AppSettings("notificationDetailsFile")
        If String.IsNullOrEmpty(Me.notificationDetailsFile) Then
            Me.notificationDetailsFile = "~\notificationDetailsFile.txt"
        End If

        Dim refreshTokenExpires As String = ConfigurationManager.AppSettings("refreshTokenExpiresIn")
        If Not String.IsNullOrEmpty(refreshTokenExpires) Then
            Me.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires)
        Else
            Me.refreshTokenExpiresIn = 24
        End If

        Return True
    End Function

    '''// <summary>
    '''// Represents the List of Notification Ids
    '''// </summary>
    '[System.Xml.Serialization.XmlRoot("Notifications")]
    'public class Notifications
    '{
    '    /// <summary>
    '    /// Gets or sets the list of Notificationids.
    '    /// </summary>
    '    [System.Xml.Serialization.XmlElement("NotificationId")]
    '    public string[] notificationid { get; set; }
    '}

    ''' <summary>
    ''' Method fetches notification ids from the stream.
    ''' </summary>
    ''' <param name="stream">Input stream received from listener</param>
    ''' <returns>List of Notification Ids</returns>
    Public Function GetNotificationIds(ByVal stream As System.IO.Stream) As ArrayList
        Dim listfofNotificationIds As ArrayList = Nothing
        Try
            Dim inputStreamContent As String = String.Empty
            Dim inputStreamContentLength As Integer = Convert.ToInt32(stream.Length)
            Dim inputStreamByte As Byte() = New Byte(inputStreamContentLength - 1) {}
            Dim strRead As Integer = stream.Read(inputStreamByte, 0, inputStreamContentLength)
            inputStreamContent = System.Text.Encoding.UTF8.GetString(inputStreamByte)

            'StreamWriter writer = File.AppendText(Request.MapPath("PaymentApp1ListenerInvoked.txt"));
            'writer.Write(Environment.NewLine + DateTime.Now.ToString() + ":Payment App1 has been invoked" + inputStreamContent);
            'writer.Close();

            Dim findText As String() = {"notificationId>"}

            Dim Ids As String() = inputStreamContent.Split(findText, StringSplitOptions.RemoveEmptyEntries)
            If Ids IsNot Nothing Then
                listfofNotificationIds = New ArrayList()
            End If

            For Each Id As String In Ids
                Dim temp As String = Id.Substring(0, Id.IndexOf("<"c))
                If Not String.IsNullOrEmpty(temp) Then
                    listfofNotificationIds.Add(temp)
                End If
            Next
        Catch ex As Exception
            Throw ex
        End Try

        Return listfofNotificationIds
    End Function
End Class
