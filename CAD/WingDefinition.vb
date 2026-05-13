Friend Module WingDefinition
    Friend Const FullSpan As Double = 3543.65
    Friend Const HalfSpan As Double = FullSpan / 2.0
    Friend Const RootChord As Double = 586.0
    Friend Const TipChord As Double = 374.0
    Friend Const RibCountPerSide As Integer = 14
    Friend Const RibThickness As Double = 3.0
    Friend Const PointCountPerSurface As Integer = 41
    Friend Const AirfoilMaximumCamber As Double = 0.04
    Friend Const AirfoilMaximumCamberPosition As Double = 0.4
    Friend Const AirfoilMaximumThickness As Double = 0.15

    Friend Function GetChordAtSpanPosition(ByVal spanPosition As Double) As Double
        Dim spanRatio As Double = Math.Abs(spanPosition) / HalfSpan
        Return RootChord + ((TipChord - RootChord) * spanRatio)
    End Function
End Module
