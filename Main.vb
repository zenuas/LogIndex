Imports System
Imports System.IO
Imports System.Collections.Generic


Public Class Main

    Public Shared Sub Main(args() As String)

        Dim self As New Main
        self.Run(CommandLineParser.Parse(self, args))
    End Sub

    Public Overridable Sub Run(args() As String)

        If args.Length > 0 Then

            For Each arg In args

                Me.Run(arg, Me.Query, Me.Rebuild, Me.OrderBy)
            Next
        Else
            Me.Run(".", Me.Query, Me.Rebuild, Me.OrderBy)
        End If
    End Sub

    Public Overridable Sub Run(current As String, where As String, force As Boolean, orderby As String)

        current = Path.GetFullPath(current)
        Dim config = IndexConfig.Parse(Me.GetConfig(current))

        Dim query = If(Me.Query.Length > 0, LogQuery(Of String).Parse(where), Nothing)

        If orderby.Length > 0 Then

            Dim indexes As New List(Of LogIndex)
            Me.IndexRebuild(current, config, force,
                Sub(index)

                    If query Is Nothing OrElse Not index.Exists(query) Then Return
                    indexes.Add(index)
                End Sub)
            Me.Match(indexes.ToArray, query, orderby)
        Else

            Me.IndexRebuild(current, config, force,
                Sub(index)

                    If query Is Nothing OrElse Not index.Exists(query) Then Return
                    Me.Match(New LogIndex() {index}, query)
                End Sub)
        End If
    End Sub

    Public Overridable Function GetConfig(current As String) As String

        Dim p = Path.Combine(current, "log-index.config")
        If File.Exists(p) Then

            Return p
        Else

            Dim parent = Path.GetDirectoryName(current)
            If parent Is Nothing Then Throw New Exception("log-index.config is not found")
            Return Me.GetConfig(parent)
        End If
    End Function

    Public Overridable Sub IndexRebuild(path As String, config As IndexConfig, force As Boolean, f As Action(Of LogIndex))

        For Each x In Me.GetFiles(path, config.LogFile)

            Dim db = x + ".db"
            Dim index As LogIndex
            Dim write_need As Boolean = False
            If force OrElse Not File.Exists(db) Then

                index = New LogIndex(x)
                index.Build(x, config)
                write_need = True
            Else

                index = LogIndex.Load(x, db)
                If File.GetLastWriteTime(x) > File.GetLastWriteTime(db) Then

                    index.Build(x, config)
                    write_need = True
                End If
            End If

            Try
                f(index)

            Finally
                If write_need Then index.Write(db)

            End Try
        Next
    End Sub

    Public Overridable Iterator Function GetFiles(path As String, pattern As String) As IEnumerable(Of String)

        If String.IsNullOrEmpty(pattern) Then pattern = "*"

        Dim FileList As Func(Of String, IEnumerable(Of String)) =
            Iterator Function(x As String)

                For Each f In Directory.GetFiles(x, pattern)

                    Yield f
                Next

                For Each d In Directory.GetDirectories(x)

                    For Each f In FileList(d)

                        Yield f
                    Next
                Next
            End Function

        For Each x In FileList(path)

            Yield x
        Next
    End Function

    Public Overridable Sub Match(indexes() As LogIndex, query As LogQuery(Of String), Optional orderby As String = "")

        'Dim merge_index(indexes.Length - 1) As Integer

        'Do While True

        '    Dim first = -1
        '    Exit Do
        'Loop

        For Each index In indexes

            Using in_ As New BufferSeekReader(index.Path)

                For Each xs In index.GetIndex(query)

                    For Each pos In xs.Value

                        in_.Seek(pos, SeekOrigin.Begin)
                        Console.WriteLine(in_.ReadLine)
                    Next
                Next
            End Using
        Next

    End Sub


    <Argument("f"c, , "force")>
    Public Overridable Sub ForceRebuild()

        Me.Rebuild = True
    End Sub

    <Argument("", "rebuild")>
    Public Overridable Property Rebuild As Boolean = False

    <Argument("q"c, , "query")>
    Public Overridable Property Query As String = ""

    <Argument("b"c, , "orderby")>
    Public Overridable Property OrderBy As String = ""

    <Argument("h"c, , "help")>
    Public Overridable Sub Help()

        Console.WriteLine(CommandLineParser.HelpMessage(Me))
    End Sub

End Class
