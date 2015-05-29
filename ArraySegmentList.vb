Imports System
Imports System.Collections.Generic


Public Class ArraySegmentList(Of T)
    Implements IEnumerable(Of T)

    Public Overridable Property Array As List(Of T)
    Public Overridable Property Min As Integer
    Public Overridable Property Max As Integer


    Public Sub New(xs As List(Of T), min As Integer, max As Integer)

        Me.Array = xs
        Me.Min = min
        Me.Max = max
    End Sub

    Public Overridable Iterator Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator

        For i = Me.Min To Me.Max

            Yield Me.Array(i)
        Next
    End Function

    Protected Overridable Function GetEnumerator_() As Collections.IEnumerator Implements Collections.IEnumerable.GetEnumerator

        Return Me.GetEnumerator
    End Function

    Public Overridable Function Count() As Integer

        Return If(Me.Min > Me.Max, 0, Math.Max(Me.Max - Me.Min + 1, 0))
    End Function
End Class
