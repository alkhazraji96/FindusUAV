Friend Class HorizontalTailConfiguration
    Public Property Chord As Double
    Public Property HalfSpan As Double
    Public Property RibCount As Integer
    Public Property Airfoil As AirfoilConfiguration

    Public Sub New()
        Chord = 150.0
        HalfSpan = 350.0
        RibCount = 8
        Airfoil = AirfoilConfiguration.CreateNaca0012()
    End Sub

    Public Shared Function CreateDefault() As HorizontalTailConfiguration
        Return New HorizontalTailConfiguration()
    End Function

    Public ReadOnly Property FullSpan As Double
        Get
            Return HalfSpan * 2.0
        End Get
    End Property
End Class
