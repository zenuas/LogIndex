Imports System
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Public Class IndexConfig

    Public Overridable Property LogFile As String = "*"
    Public Overridable Property Format As LogFormats = LogFormats.CSV
    Public Overridable Property Header As Integer = 0
    Public Overridable Property Quote As Char = Chars.Null
    Public Overridable Property CommentHeader As New List(Of Char)
    Public Overridable Property Charset As String = "utf-8"
    Public Overridable Property Columns As LogColumn()
    Public Overridable Property Indexes As LogColumn()
    Public Overridable Property LastUpdate As Date
    Private Shared ssv_reg_ As New Regex("[\s,]+")
    Private Shared kv_reg_ As New Regex("([^=]+)=(\S+)")

    Public Shared Function Parse(path As String) As IndexConfig

        Dim ParseKeyValuePair =
            Function(line As String) As KeyValuePair(Of String, String)

                Dim xs = line.Split(New Char() {":"c}, 2)
                Return New KeyValuePair(Of String, String)(xs(0), xs(1).Trim)
            End Function

        Dim self As New IndexConfig
        Dim ParseConfig =
            Sub(line As String)

                If line.Length = 0 Then Return

                Dim kv = ParseKeyValuePair(line)
                Select Case kv.Key.ToLower

                    Case "log-file" : self.LogFile = kv.Value

                    Case "format"

                        ssv_reg_.Split(kv.Value.ToLower).Select(
                               Function(x)

                                   Dim value = ""
                                   Dim m = kv_reg_.Match(x)
                                   If m.Success Then

                                       x = m.Groups(1).Value
                                       value = m.Groups(2).Value
                                   End If

                                   Select Case x
                                       Case "csv" : self.Format = LogFormats.CSV
                                       Case "tsv" : self.Format = LogFormats.TSV
                                       Case "ssv" : self.Format = LogFormats.SSV

                                       Case "header" : self.Header = Types.CastDefault(x, 1)

                                       Case "quote"
                                           If value.Length <> 1 Then Throw New Exception("quote need char")
                                           self.Quote = value(0)

                                       Case "comment"
                                           If value.Length <> 1 Then Throw New Exception("comment need char")
                                           self.CommentHeader.Add(value(0))

                                       Case Else
                                           Throw New Exception(String.Format("unknown format {0}", kv.Value))
                                   End Select

                                   Return ""
                               End Function
                           ).ToArray()

                    Case "charset" : self.Charset = kv.Value

                    Case "columns" : self.Columns = ssv_reg_.Split(kv.Value).Select(Function(x, i) LogColumn.Parse(i, x)).ToArray

                    Case "index" : self.Indexes = ssv_reg_.Split(kv.Value).Select(Function(x) self.Columns.First(Function(col) col.Name = x)).ToArray
                End Select
            End Sub

        Using in_ = New StreamReader(File.OpenRead(path))

            Dim buffer = ""
            Do While Not in_.EndOfStream

                Dim line = in_.ReadLine
                If line.Length = 0 OrElse line(0) = " "c OrElse line(0) = Chars.Tab Then

                    buffer += line
                Else

                    ParseConfig(buffer)
                    buffer = line
                End If
            Loop
            ParseConfig(buffer)
        End Using

        self.LastUpdate = File.GetLastWriteTime(path)

        Return self
    End Function

End Class
