Friend Class TailConfiguration
    Public Property PointCountPerSurface As Integer
    Public Property DistanceOffset As Double
    Public Property RibThickness As Double
    Public Property LighteningCutoutsEnabled As Boolean
    Public Property MainSpar As TailSparConfiguration
    Public Property HorizontalStabilizer As HorizontalTailConfiguration
    Public Property VerticalStabilizer As VerticalTailConfiguration
    Public Property RudderClearance As Double

    Public Sub New()
        PointCountPerSurface = 50
        DistanceOffset = 1500.0
        RibThickness = 2.0
        LighteningCutoutsEnabled = True
        MainSpar = TailSparConfiguration.CreateDefault()
        HorizontalStabilizer = HorizontalTailConfiguration.CreateDefault()
        VerticalStabilizer = VerticalTailConfiguration.CreateDefault()
        RudderClearance = 25.0
    End Sub

    Public Shared Function CreateDefault() As TailConfiguration
        Return New TailConfiguration()
    End Function

    Public ReadOnly Property HorizontalOffset As Double
        Get
            Return DistanceOffset
        End Get
    End Property

    Public ReadOnly Property VerticalRootOffset As Double
        Get
            Return DistanceOffset
        End Get
    End Property

    Public ReadOnly Property VerticalTrailingEdgeX As Double
        Get
            Return VerticalRootOffset + VerticalStabilizer.RootChord
        End Get
    End Property

    Public ReadOnly Property VerticalTipOffset As Double
        Get
            Return VerticalTrailingEdgeX - VerticalStabilizer.TipChord
        End Get
    End Property
End Class
