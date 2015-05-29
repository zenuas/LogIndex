Imports System.Text
Imports System.Collections.Generic


Public Class CsvParser

    Public Shared Function Parse(in_ As BufferSeekReader, sep As Char, quote As Char) As String()

        Dim cols As New List(Of String)
        If in_.EndOfStream Then Return cols.ToArray

        Dim buffer As New StringBuilder
        Dim line = in_.ReadLine
        Dim prev_c = Chars.Null
        Dim isquote = False

        Do While True

            For i = 0 To line.Length - 1

                Dim c = line(i)
                If c = quote AndAlso quote <> Chars.Null Then

                    If Not isquote AndAlso prev_c = quote Then

                        buffer.Append(c)
                    End If
                    isquote = Not isquote

                ElseIf Not isquote AndAlso c = sep Then

                    cols.Add(buffer.ToString)
                    buffer.Clear()
                Else

                    buffer.Append(c)
                End If

                prev_c = c
            Next

            If Not isquote Then Exit Do
            buffer.AppendLine()

            If in_.EndOfStream Then Exit Do
            line = in_.ReadLine
        Loop

        cols.Add(buffer.ToString)
        Return cols.ToArray
    End Function

End Class
