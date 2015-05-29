Imports System.Text
Imports System.IO
Imports System.Collections.Generic


Public Class BufferSeekReader
    Inherits FileStream

    Public Overridable Property Buffer As Byte()
    Public Overridable Property BufferReadLength As Integer = 0
    Public Overridable Property BufferPosition As Integer = 0
    Public Overridable Property Encoding As Encoding

    Public Sub New(path As String, enc As Encoding, Optional buffer_size As Integer = 1024)
        MyBase.New(path, FileMode.Open, FileAccess.Read)

        Dim buffer(buffer_size) As Byte
        Me.Buffer = buffer
        Me.Encoding = enc
    End Sub

    Public Sub New(path As String, Optional buffer_size As Integer = 1024)
        Me.New(path, Encoding.UTF8, buffer_size)

    End Sub

    Public Overridable Sub BufferClear()

        Me.BufferReadLength = 0
        Me.BufferPosition = 0
    End Sub

    Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long

        Me.BufferClear()
        Return MyBase.Seek(offset, origin)
    End Function

    Public Overrides Property Position As Long
        Get
            Return MyBase.Position - (Me.BufferReadLength - Me.BufferPosition)
        End Get
        Set(value As Long)

            MyBase.Position = value
        End Set
    End Property

    Public Overridable Function ReadBuffer() As Integer

        If Me.Buffer.Length <= Me.BufferPosition Then

            Me.BufferReadLength = 0
            Me.BufferPosition = 0
        End If

        Dim count = MyBase.Read(Me.Buffer, Me.BufferReadLength, Me.Buffer.Length - Me.BufferReadLength)
        Me.BufferReadLength += count
        Return count
    End Function

    Public Overrides Function Read(array() As Byte, offset As Integer, count As Integer) As Integer

        Dim readed = 0

        If Me.BufferPosition < Me.BufferReadLength Then

            Do While count > 0 AndAlso Me.BufferPosition < Me.BufferReadLength

                array(offset) = Me.Buffer(Me.BufferPosition)
                readed += 1
                offset += 1
                Me.BufferPosition += 1
                count -= 1
            Loop

        ElseIf Me.ReadBuffer() = 0 Then

            Return 0
        End If

        If count > 0 Then Return readed + Me.Read(array, offset, count)
        Return readed
    End Function

    Public Overrides Function ReadByte() As Integer

        Dim s(0) As Byte
        Dim count = Me.Read(s, 0, s.Length)
        If count <= 0 Then Return -1
        Return s(0)
    End Function

    Public Overridable Function PeekByte() As Integer

        If Me.EndOfStream Then Return -1
        Return Me.Buffer(Me.BufferPosition)
    End Function

    Public Overridable Function ReadLine() As String

        Dim xs As New List(Of Byte)
        Dim c As Integer

        Do While True

            c = Me.ReadByte
            If c < 0 Then GoTo End_

            If c = 13 OrElse c = 10 Then Exit Do
            xs.Add(CByte(c))
        Loop

        If c = 13 AndAlso Me.PeekByte = 10 Then Me.ReadByte()

End_:
        Return Me.Encoding.GetString(xs.ToArray)
    End Function

    Public Overridable ReadOnly Property EndOfStream() As Boolean
        Get
            Return (Me.BufferPosition >= Me.BufferReadLength AndAlso Me.ReadBuffer() = 0)
        End Get
    End Property

End Class
