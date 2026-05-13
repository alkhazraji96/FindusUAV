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
    Friend Const MainSparChordFraction As Double = 0.3
    Friend Const MainSparOuterDiameter As Double = 30.0
    Friend Const MainSparWallThickness As Double = 1.5
    Friend Const MainSparCutoutDiameter As Double = 31.0

    Friend ReadOnly Property MainSparInnerDiameter As Double
        Get
            Return MainSparOuterDiameter - (2.0 * MainSparWallThickness)
        End Get
    End Property

    Friend Function GetChordAtSpanPosition(ByVal spanPosition As Double) As Double
        Dim spanRatio As Double = Math.Abs(spanPosition) / HalfSpan
        Return RootChord + ((TipChord - RootChord) * spanRatio)
    End Function

    Friend Function GetMainSparCenterXAtSpanPosition(ByVal spanPosition As Double) As Double
        Return GetChordAtSpanPosition(spanPosition) * MainSparChordFraction
    End Function

    Friend Function GetMainSparCenterZAtSpanPosition(ByVal spanPosition As Double) As Double
        Return GetChordAtSpanPosition(spanPosition) *
            GetMeanCamberAtChordFraction(MainSparChordFraction)
    End Function

    Private Function GetMeanCamberAtChordFraction(ByVal chordFraction As Double) As Double
        If AirfoilMaximumCamber <= 0.0 OrElse AirfoilMaximumCamberPosition <= 0.0 Then
            Return 0.0
        End If

        If chordFraction < AirfoilMaximumCamberPosition Then
            Return (AirfoilMaximumCamber / (AirfoilMaximumCamberPosition * AirfoilMaximumCamberPosition)) *
                ((2.0 * AirfoilMaximumCamberPosition * chordFraction) - (chordFraction * chordFraction))
        End If

        Dim aftCamberLength As Double = 1.0 - AirfoilMaximumCamberPosition

        If aftCamberLength <= 0.0 Then
            Return 0.0
        End If

        Return (AirfoilMaximumCamber / (aftCamberLength * aftCamberLength)) *
            ((1.0 - (2.0 * AirfoilMaximumCamberPosition)) +
             (2.0 * AirfoilMaximumCamberPosition * chordFraction) -
             (chordFraction * chordFraction))
    End Function
End Module
