Friend Class VerticalTailConfiguration
    Public Property RootChord As Double
    Public Property TipChord As Double
    Public Property Span As Double
    Public Property RibCount As Integer
    Public Property Airfoil As AirfoilConfiguration

    Public Sub New()
        RootChord = 150.0
        TipChord = 75.0
        Span = 250.0
        RibCount = 4
        Airfoil = AirfoilConfiguration.CreateNaca0012()
    End Sub

    Public Shared Function CreateDefault() As VerticalTailConfiguration
        Return New VerticalTailConfiguration()
    End Function
End Class
