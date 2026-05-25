Friend Class TailSparConfiguration
    Public Property MainSparDiameter As Double

    Public Sub New()
        MainSparDiameter = 6.0
    End Sub

    Public Shared Function CreateDefault() As TailSparConfiguration
        Return New TailSparConfiguration()
    End Function

    Public ReadOnly Property MainSparRadius As Double
        Get
            Return MainSparDiameter / 2.0
        End Get
    End Property
End Class
