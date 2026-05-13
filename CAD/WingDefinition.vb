Imports System.Collections.Generic

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
    Friend Const RibLighteningCutoutEdgeMargin As Double = 6.0
    Friend Const RibLighteningCutoutMinimumDiameter As Double = 8.0

    Friend ReadOnly Property MainSparInnerDiameter As Double
        Get
            Return MainSparOuterDiameter - (2.0 * MainSparWallThickness)
        End Get
    End Property

    Friend Function GetRibLighteningCutouts() As List(Of RibLighteningCutoutDefinition)
        Return New List(Of RibLighteningCutoutDefinition) From {
            New RibLighteningCutoutDefinition("Forward lightening cutout", 0.15, 22.0),
            New RibLighteningCutoutDefinition("Middle lightening cutout", 0.5, 34.0),
            New RibLighteningCutoutDefinition("Aft lightening cutout", 0.7, 22.0)
        }
    End Function

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

    Friend Function GetRibLighteningCutoutCenterX(ByVal spanPosition As Double,
                                                  ByVal cutoutDefinition As RibLighteningCutoutDefinition) As Double
        Return GetChordAtSpanPosition(spanPosition) * cutoutDefinition.ChordFraction
    End Function

    Friend Function GetRibLighteningCutoutCenterZ(ByVal spanPosition As Double,
                                                  ByVal cutoutDefinition As RibLighteningCutoutDefinition) As Double
        Return GetChordAtSpanPosition(spanPosition) *
            GetMeanCamberAtChordFraction(cutoutDefinition.ChordFraction)
    End Function

    Friend Function GetRibLighteningCutoutDiameter(ByVal spanPosition As Double,
                                                   ByVal cutoutDefinition As RibLighteningCutoutDefinition) As Double
        Dim chordLength As Double = GetChordAtSpanPosition(spanPosition)
        Dim airfoilHalfThickness As Double =
            GetAirfoilHalfThicknessAtChordFraction(cutoutDefinition.ChordFraction, chordLength)
        Dim availableDiameter As Double =
            (2.0 * airfoilHalfThickness) - (2.0 * RibLighteningCutoutEdgeMargin)

        If availableDiameter < RibLighteningCutoutMinimumDiameter Then
            Return 0.0
        End If

        Return Math.Min(cutoutDefinition.PreferredDiameter, availableDiameter)
    End Function

    Friend Function GetMeanCamberAtChordFraction(ByVal chordFraction As Double) As Double
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

    Friend Function GetAirfoilHalfThicknessAtChordFraction(ByVal chordFraction As Double,
                                                           ByVal chordLength As Double) As Double
        Dim halfThickness As Double = 5.0 * AirfoilMaximumThickness *
            ((0.2969 * Math.Sqrt(chordFraction)) -
             (0.1260 * chordFraction) -
             (0.3516 * Math.Pow(chordFraction, 2.0)) +
             (0.2843 * Math.Pow(chordFraction, 3.0)) -
             (0.1036 * Math.Pow(chordFraction, 4.0)))
        Dim camberSlope As Double = GetMeanCamberSlopeAtChordFraction(chordFraction)

        Return halfThickness * Math.Cos(Math.Atan(camberSlope)) * chordLength
    End Function

    Private Function GetMeanCamberSlopeAtChordFraction(ByVal chordFraction As Double) As Double
        If AirfoilMaximumCamber <= 0.0 OrElse AirfoilMaximumCamberPosition <= 0.0 Then
            Return 0.0
        End If

        If chordFraction < AirfoilMaximumCamberPosition Then
            Return ((2.0 * AirfoilMaximumCamber) /
                    (AirfoilMaximumCamberPosition * AirfoilMaximumCamberPosition)) *
                (AirfoilMaximumCamberPosition - chordFraction)
        End If

        Dim aftCamberLength As Double = 1.0 - AirfoilMaximumCamberPosition

        If aftCamberLength <= 0.0 Then
            Return 0.0
        End If

        Return ((2.0 * AirfoilMaximumCamber) / (aftCamberLength * aftCamberLength)) *
            (AirfoilMaximumCamberPosition - chordFraction)
    End Function

    Friend Structure RibLighteningCutoutDefinition
        Friend ReadOnly Name As String
        Friend ReadOnly ChordFraction As Double
        Friend ReadOnly PreferredDiameter As Double

        Friend Sub New(ByVal name As String,
                       ByVal chordFraction As Double,
                       ByVal preferredDiameter As Double)
            Me.Name = name
            Me.ChordFraction = chordFraction
            Me.PreferredDiameter = preferredDiameter
        End Sub
    End Structure
End Module
