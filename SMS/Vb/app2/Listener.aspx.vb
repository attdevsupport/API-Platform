' <copyright file="Listener.aspx.vb" company="AT&amp;T">
' Licensed by AT&amp;T under 'Software Development Kit Tools Agreement.' 2012
' TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION: http://developer.att.com/sdk_agreement/
' Copyright 2012 AT&amp;T Intellectual Property. All rights reserved. http://developer.att.com
' For more information contact developer.support@att.com
' </copyright>

#Region "References"
Imports System.Configuration
Imports System.IO
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization
#End Region

''' <summary>
''' Listener class for saving message counts.
''' </summary>
Partial Public Class Listener
    Inherits System.Web.UI.Page
#Region "Events"
    ''' <summary>
    ''' This method called when the page is loaded into the browser. This method requests input stream and parses it to get message counts.
    ''' </summary>
    ''' <param name="sender">object, which invoked this method</param>
    ''' <param name="e">EventArgs, which specifies arguments specific to this method</param>
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)

        Dim stream As System.IO.Stream = Request.InputStream

        If stream IsNot Nothing Then
            Dim bytes As Byte() = New Byte(stream.Length - 1) {}
            stream.Position = 0
            stream.Read(bytes, 0, CInt(stream.Length))
            Dim responseData As String = Encoding.ASCII.GetString(bytes)

            Dim serializeObject As New JavaScriptSerializer()
            Dim message As InboundSMSMessage = DirectCast(serializeObject.Deserialize(responseData, GetType(InboundSMSMessage)), InboundSMSMessage)

            If message IsNot Nothing Then
                Me.SaveMessageCount(message)
                Me.SaveMessage(message)
            End If
        End If
        '
        '        string inputStreamContents;
        '        int stringLength;
        '        int strRead;
        '        System.IO.Stream str = Request.InputStream;
        '        stringLength = Convert.ToInt32(str.Length);
        '        byte[] stringArray = new byte[stringLength];
        '        strRead = str.Read(stringArray, 0, stringLength);
        '        inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray);
        '        System.IO.File.WriteAllText(Request.MapPath("~\\UAT\\Csharp-RESTful\\sms\\app2\\ListenerLog.txt"), inputStreamContents);
        ' 


    End Sub
#End Region
#Region "Method to store the received message to file"
    ''' <summary>
    ''' This method reads the incoming message and stores the received message details.
    ''' </summary>
    ''' <param name="message">InboundSMSMessage, message received from Request</param>
    Private Sub SaveMessage(ByVal message As InboundSMSMessage)
        Dim filePath As String = ConfigurationManager.AppSettings("MessagesFilePath")

        Dim messageLineToStore As String = message.DateTime.ToString() & "_-_-" & message.MessageId.ToString() & "_-_-" & message.Message.ToString() & "_-_-" & message.SenderAddress.ToString() & "_-_-" & message.DestinationAddress.ToString()

        Using streamWriter As StreamWriter = File.AppendText(Request.MapPath(filePath))
            streamWriter.WriteLine(messageLineToStore)
            streamWriter.Close()
        End Using
    End Sub
#End Region

#Region "Methods to parse and save the message counts"
    ''' <summary>
    ''' This method reads the incoming message and decides on to which message count needs to be updated.
    ''' This method invokes another method to write the count to file
    ''' </summary>
    ''' <param name="message">InboundSMSMessage, message received from Request</param>
    Private Sub SaveMessageCount(ByVal message As InboundSMSMessage)
        If Not String.IsNullOrEmpty(message.Message) Then
            Dim messageText As String = message.Message.Trim().ToLower()

            Dim filePathConfigKey As String = String.Empty
            Select Case messageText
                Case "basketball"
                    filePathConfigKey = "BasketBallFilePath"
                    Exit Select
                Case "football"
                    filePathConfigKey = "FootBallFilePath"
                    Exit Select
                Case "baseball"
                    filePathConfigKey = "BaseBallFilePath"
                    Exit Select
            End Select

            If Not String.IsNullOrEmpty(filePathConfigKey) Then
                Me.WriteToFile(filePathConfigKey)
            End If
        End If
    End Sub

    ''' <summary>
    ''' This method gets the file name, reads from the file, increments the count(if any) and writes back to the file.
    ''' </summary>
    ''' <param name="filePathConfigKey">string, parameter which specifies the name of the file</param>
    Private Sub WriteToFile(ByVal filePathConfigKey As String)
        Dim filePath As String = ConfigurationManager.AppSettings(filePathConfigKey)

        Dim count As Integer = 0
        Using streamReader As StreamReader = File.OpenText(Request.MapPath(filePath))
            count = Convert.ToInt32(streamReader.ReadToEnd())
            streamReader.Close()
        End Using

        count = count + 1

        Using streamWriter As StreamWriter = File.CreateText(Request.MapPath(filePath))
            streamWriter.Write(count)
            streamWriter.Close()
        End Using
    End Sub
#End Region
End Class

#Region "Message Structure"
''' <summary>
''' Message structure received
''' </summary>
Public Class InboundSMSMessage
    ''' <summary>
    ''' Gets or sets the value of DateTime
    ''' </summary>
    Public Property DateTime() As String
        Get
            Return m_DateTime
        End Get
        Set(ByVal value As String)
            m_DateTime = Value
        End Set
    End Property
    Private m_DateTime As String

    ''' <summary>
    ''' Gets or sets the value of MessageId
    ''' </summary>
    Public Property MessageId() As String
        Get
            Return m_MessageId
        End Get
        Set(ByVal value As String)
            m_MessageId = Value
        End Set
    End Property
    Private m_MessageId As String

    ''' <summary>
    ''' Gets or sets the value of Message
    ''' </summary>
    Public Property Message() As String
        Get
            Return m_Message
        End Get
        Set(ByVal value As String)
            m_Message = Value
        End Set
    End Property
    Private m_Message As String

    ''' <summary>
    ''' Gets or sets the value of SenderAddress
    ''' </summary>
    Public Property SenderAddress() As String
        Get
            Return m_SenderAddress
        End Get
        Set(ByVal value As String)
            m_SenderAddress = Value
        End Set
    End Property
    Private m_SenderAddress As String

    ''' <summary>
    ''' Gets or sets the value of DestinationAddress
    ''' </summary>
    Public Property DestinationAddress() As String
        Get
            Return m_DestinationAddress
        End Get
        Set(ByVal value As String)
            m_DestinationAddress = Value
        End Set
    End Property
    Private m_DestinationAddress As String
End Class
#End Region
