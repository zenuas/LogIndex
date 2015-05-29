Imports System
Imports System.Text.RegularExpressions


Public Class LogQuery(Of T)

    Public Overridable Property Name As String
    Public Overridable Property [Operator] As String
    Public Overridable Property Value As T
    Private Shared expr_reg_ As New Regex("([^=<>]+)([=<>]+)(\S+)")


    Public Shared Function Parse(query As String) As LogQuery(Of T)

        Dim self As New LogQuery(Of T)

        Dim m = expr_reg_.Match(query)
        If Not m.Success Then Throw New Exception("query format error")

        self.Name = m.Groups(1).Value
        self.Operator = m.Groups(2).Value
        self.Value = Types.Cast(Of String, T)(m.Groups(3).Value)

        Return self
    End Function

End Class
