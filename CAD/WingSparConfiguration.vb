Friend Class WingSparConfiguration
    Public Property ChordFraction As Double
    Public Property OuterDiameter As Double
    Public Property WallThickness As Double
    Public Property RibCutoutDiameter As Double

    Public Sub New()
        ChordFraction = 0.3
        OuterDiameter = 30.0
        WallThickness = 1.5
        RibCutoutDiameter = 31.0
    End Sub

    Public Shared Function CreateDefault() As WingSparConfiguration
        Return New WingSparConfiguration()
    End Function

    Public ReadOnly Property InnerDiameter As Double
        Get
            Return OuterDiameter - (2.0 * WallThickness)
        End Get
    End Property
End Class
