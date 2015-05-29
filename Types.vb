Public Class Types

    Public Shared Function Cast(Of T, R)(x As T) As R

        Return CType(CType(x, Object), R)
    End Function

    Public Shared Function CastDefault(Of T, R)(x As T, def As R) As R

        Try
            Return Cast(Of T, R)(x)

        Catch
            Return def

        End Try
    End Function


End Class
