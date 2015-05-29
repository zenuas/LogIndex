Imports System
Imports System.Text
Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Linq


<Serializable>
Public Class LogIndex

    Public Const VERSION As String = "Li1.0"

    Public Overridable Property Path As String
    Public Overridable Property LastRead As Long = 0
    Public Overridable Property IntIndexes As New Dictionary(Of String, List(Of KeyValuePair(Of Integer, List(Of Long))))
    Public Overridable Property DateIndexes As New Dictionary(Of String, List(Of KeyValuePair(Of Date, List(Of Long))))
    Public Overridable Property StringIndexes As New Dictionary(Of String, List(Of KeyValuePair(Of String, List(Of Long))))


    Public Sub New(path As String)

        Me.Path = path
    End Sub

    Public Shared Function Load(path As String, db As String) As LogIndex

        Using f As New BinaryReader(New FileStream(db, FileMode.Open, FileAccess.Read))

            Dim self As New LogIndex(path)
            Dim s = Encoding.ASCII.GetString(f.ReadBytes(VERSION.Length))
            If VERSION <> s Then Throw New Exception(String.Format("unmatch version {0}", s))

            self.LastRead = f.ReadInt64

            ' IntIndexes
            Dim index_name_count = f.ReadInt32
            For index_name_i = 0 To index_name_count - 1

                Dim key = f.ReadString
                Dim key_count = f.ReadInt32

                Dim xs = New List(Of KeyValuePair(Of Integer, List(Of Long)))

                For key_i = 0 To key_count - 1

                    Dim index = f.ReadInt32
                    Dim index_count = f.ReadInt32

                    Dim xxs = New List(Of Long)

                    For index_i = 0 To index_count - 1

                        xxs.Add(f.ReadInt64)
                    Next

                    xs.Add(New KeyValuePair(Of Integer, List(Of Long))(index, xxs))
                Next

                self.IntIndexes.Add(key, xs)
            Next

            ' DateIndexes
            index_name_count = f.ReadInt32
            For index_name_i = 0 To index_name_count - 1

                Dim key = f.ReadString
                Dim key_count = f.ReadInt32

                Dim xs = New List(Of KeyValuePair(Of Date, List(Of Long)))

                For key_i = 0 To key_count - 1

                    Dim index = New Date(f.ReadInt64)
                    Dim index_count = f.ReadInt32

                    Dim xxs = New List(Of Long)

                    For index_i = 0 To index_count - 1

                        xxs.Add(f.ReadInt64)
                    Next

                    xs.Add(New KeyValuePair(Of Date, List(Of Long))(index, xxs))
                Next

                self.DateIndexes.Add(key, xs)
            Next

            ' StringIndexes
            index_name_count = f.ReadInt32
            For index_name_i = 0 To index_name_count - 1

                Dim key = f.ReadString
                Dim key_count = f.ReadInt32

                Dim xs = New List(Of KeyValuePair(Of String, List(Of Long)))

                For key_i = 0 To key_count - 1

                    Dim index = f.ReadString
                    Dim index_count = f.ReadInt32

                    Dim xxs = New List(Of Long)

                    For index_i = 0 To index_count - 1

                        xxs.Add(f.ReadInt64)
                    Next

                    xs.Add(New KeyValuePair(Of String, List(Of Long))(index, xxs))
                Next

                self.StringIndexes.Add(key, xs)
            Next
            Return self
        End Using

        'Using f As New FileStream(db, FileMode.Open, FileAccess.Read)

        '    Dim binary As New BinaryFormatter()
        '    Return CType(binary.Deserialize(f), LogIndex)
        'End Using
    End Function

    Public Overridable Sub Write(db As String)

        Using f As New BinaryWriter(New FileStream(db, FileMode.Create, FileAccess.Write))

            f.Write(Encoding.ASCII.GetBytes(VERSION))

            f.Write(Me.LastRead)

            ' IntIndexes
            f.Write(Me.IntIndexes.Keys.Count)
            For Each name In Me.IntIndexes.Keys

                Dim xs = Me.IntIndexes(name)
                f.Write(name)
                f.Write(xs.Count)

                For Each kv In xs

                    f.Write(kv.Key)
                    f.Write(kv.Value.Count)

                    For Each index In kv.Value

                        f.Write(index)
                    Next
                Next
            Next

            ' DateIndexes
            f.Write(Me.DateIndexes.Keys.Count)
            For Each name In Me.DateIndexes.Keys

                Dim xs = Me.DateIndexes(name)
                f.Write(name)
                f.Write(xs.Count)

                For Each kv In xs

                    f.Write(kv.Key.Ticks)
                    f.Write(kv.Value.Count)

                    For Each index In kv.Value

                        f.Write(index)
                    Next
                Next
            Next

            ' StringIndexes
            f.Write(Me.StringIndexes.Keys.Count)
            For Each name In Me.StringIndexes.Keys

                Dim xs = Me.StringIndexes(name)
                f.Write(name)
                f.Write(xs.Count)

                For Each kv In xs

                    f.Write(kv.Key)
                    f.Write(kv.Value.Count)

                    For Each index In kv.Value

                        f.Write(index)
                    Next
                Next
            Next
        End Using

        'Using f As New FileStream(db, FileMode.CreateNew, FileAccess.Write)

        '    Dim binary As New BinaryFormatter()
        '    binary.Serialize(f, Me)
        'End Using
    End Sub

    Public Overridable Sub Build(path As String, config As IndexConfig)

        For Each x In config.Indexes

            If x.Type = GetType(Integer) AndAlso Not Me.IntIndexes.ContainsKey(x.Name) Then Me.IntIndexes.Add(x.Name, New List(Of KeyValuePair(Of Integer, List(Of Long))))
            If x.Type = GetType(Date) AndAlso Not Me.DateIndexes.ContainsKey(x.Name) Then Me.DateIndexes.Add(x.Name, New List(Of KeyValuePair(Of Date, List(Of Long))))
            If x.Type = GetType(String) AndAlso Not Me.StringIndexes.ContainsKey(x.Name) Then Me.StringIndexes.Add(x.Name, New List(Of KeyValuePair(Of String, List(Of Long))))
        Next

        Dim enc = Encoding.GetEncoding(config.Charset)
        Using in_ As New BufferSeekReader(path, enc)

            If Me.LastRead > 0 Then

                in_.Seek(Me.LastRead, SeekOrigin.Begin)
            Else

                For i = 0 To config.Header - 1

                    Dim never_use_var = in_.ReadLine()
                Next
            End If

            Dim sep As Char
            Select Case config.Format

                Case LogFormats.CSV : sep = ","c
                Case LogFormats.TSV : sep = Chars.Tab
                Case LogFormats.SSV : sep = " "c

            End Select

            Do While Not in_.EndOfStream

                If config.CommentHeader.Count > 0 Then

                    Dim header = enc.GetString(New Byte() {CByte(in_.PeekByte)})
                    For Each x In config.CommentHeader

                        If header = x Then
                            
                            Dim never_use_var = in_.ReadLine()
                            Continue Do
                        End If
                    Next
                End If
                Dim pos = in_.Position
                Dim cols = CsvParser.Parse(in_, sep, config.Quote)

                For Each x In config.Indexes

                    Dim v = cols(x.Index)
                    Me.AddIndex(x.Name, v, pos)
                Next
            Loop

            Me.LastRead = in_.Position
        End Using
    End Sub

    Public Overridable Sub AddIndex(name As String, value As String, pos As Long)

        If Me.IntIndexes.ContainsKey(name) Then Me.AddIndex(Me.IntIndexes(name), CInt(value), pos) : Return
        If Me.DateIndexes.ContainsKey(name) Then Me.AddIndex(Me.DateIndexes(name), CDate(value), pos) : Return
        If Me.StringIndexes.ContainsKey(name) Then Me.AddIndex(Me.StringIndexes(name), value, pos) : Return
    End Sub

    Public Overridable Sub AddIndex(Of TKey As IComparable(Of TKey))(
            index As List(Of KeyValuePair(Of TKey, List(Of Long))),
            value As TKey,
            pos As Long
        )

        Dim insert =
            Sub(i As Integer)

                Dim xs = New List(Of Long)
                xs.Add(pos)
                index.Insert(i, New KeyValuePair(Of TKey, List(Of Long))(value, xs))
            End Sub

        If index.Count = 0 Then

            insert(0)
            Return
        End If

        Dim min = 0
        Dim max = index.Count - 1

        Do While True

            Dim pivot = min + (max - min) \ 2
            Dim key = index(pivot).Key
            Dim compare = key.CompareTo(value)

            If compare = 0 Then

                index(pivot).Value.Add(pos)
                Exit Do

            ElseIf compare < 0 Then

                min = pivot + 1
                If min > max Then

                    insert(pivot + 1)
                    Exit Do
                End If
            Else
                max = pivot - 1
                If min > max Then

                    insert(pivot)
                    Exit Do
                End If
            End If
        Loop

    End Sub

    Public Overridable Function Exists(Of TKey As IComparable(Of TKey))(ParamArray queries() As LogQuery(Of TKey)) As Boolean

        Return Me.GetIndex(Of TKey)(queries).Count > 0
    End Function

    Public Overridable Function GetIndex(Of TKey As IComparable(Of TKey))(
            ParamArray queries() As LogQuery(Of TKey)
        ) As ArraySegmentList(Of KeyValuePair(Of TKey, List(Of Long)))

        Dim name = queries(0).Name
        If Me.IntIndexes.ContainsKey(name) Then Return Me.GetIndex(Types.Cast(Of List(Of KeyValuePair(Of Integer, List(Of Long))), List(Of KeyValuePair(Of TKey, List(Of Long))))(Me.IntIndexes(name)), queries)
        If Me.DateIndexes.ContainsKey(name) Then Return Me.GetIndex(Types.Cast(Of List(Of KeyValuePair(Of Date, List(Of Long))), List(Of KeyValuePair(Of TKey, List(Of Long))))(Me.DateIndexes(name)), queries)
        If Me.StringIndexes.ContainsKey(name) Then Return Me.GetIndex(Types.Cast(Of List(Of KeyValuePair(Of String, List(Of Long))), List(Of KeyValuePair(Of TKey, List(Of Long))))(Me.StringIndexes(name)), queries)
        Throw New Exception("index not found")
    End Function

    Public Overridable Function GetIndex(Of TKey As IComparable(Of TKey), TValue)(
            index As List(Of KeyValuePair(Of TKey, TValue)),
            ParamArray queries() As LogQuery(Of TKey)
        ) As ArraySegmentList(Of KeyValuePair(Of TKey, TValue))


        ' 処理イメージ
        ' GetIndex : [(Key, Value)] -> [(Value, Ope)] -> [(Key, Value)]
        ' GetIndex index (x:xs) = GetIndex (GetIndex index xs) x
        '          index x      = f x.Ope $ binarysearch index x.Value
        '            where
        '              f ope xs |
        '                ope = "<=" = xs[index.min  .. xs.min - 1]
        '                ope = "<"  = xs[index.min  .. xs.min]
        '                ope = "="  = xs[xs.min     .. xs.max]
        '                ope = ">"  = xs[xs.max     .. index.min]
        '                ope = ">=" = xs[xs.max + 1 .. index.min]

        Dim min = 0
        Dim max = index.Count - 1
        For Each q In queries

            Dim value = q.Value
            Dim qmin = min
            Dim qmax = max

            Do While True

                Dim pivot = qmin + (qmax - qmin) \ 2
                Dim key = index(pivot).Key
                Dim compare = key.CompareTo(value)

                If compare = 0 Then

                    Select Case q.Operator
                        Case "<=" : max = pivot
                        Case "<" : max = pivot - 1
                        Case "=" : min = pivot : max = pivot
                        Case ">" : min = pivot + 1
                        Case ">=" : min = pivot
                    End Select
                    Exit Do

                ElseIf compare < 0 Then

                    qmin = pivot + 1
                    If qmin > qmax Then

                        Select Case q.Operator
                            Case "<=" : max = qmin - 1
                            Case "<" : max = qmin - 1
                            Case "=" : min = 0 : max = -1 : Exit For
                            Case ">" : min = qmin
                            Case ">=" : min = qmin
                        End Select
                        Exit Do
                    End If
                Else

                    qmax = pivot - 1
                    If qmin > qmax Then

                        Select Case q.Operator
                            Case "<=" : max = qmax
                            Case "<" : max = qmax
                            Case "=" : min = 0 : max = -1 : Exit For
                            Case ">" : min = qmax + 1
                            Case ">=" : min = qmax + 1
                        End Select
                        Exit Do
                    End If
                End If
            Loop
        Next

        Return New ArraySegmentList(Of KeyValuePair(Of TKey, TValue))(index, min, max)
    End Function

End Class
