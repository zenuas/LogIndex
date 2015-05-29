Imports System
Imports System.Text.RegularExpressions


Public Class LogColumn

    Public Overridable Property Index As Integer
    Public Overridable Property Name As String
    Public Overridable Property Type As System.Type
    Private Shared ssv_reg_ As New Regex("<(Int|Date|String)>")

    Public Shared Function Parse(index As Integer, s As String) As LogColumn

        Dim self As New LogColumn
        self.Index = index
        self.Type = GetType(String)
        self.Name = ssv_reg_.Replace(s,
            Function(m) As String

                Select Case m.Value.ToLower

                    Case "<int>" : self.Type = GetType(Integer)
                    Case "<date>" : self.Type = GetType(Date)
                    Case "<string>" : self.Type = GetType(String)

                    Case Else
                        Throw New Exception(String.Format("unknown type name {0}", m.Value))

                End Select

                Return ""
            End Function)

        Return self
    End Function

    Public Overrides Function ToString() As String

        Return String.Format("{0}<{1}>", Me.Name, Me.Type.Name)
    End Function

End Class
