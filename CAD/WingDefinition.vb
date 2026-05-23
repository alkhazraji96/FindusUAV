Imports System.Collections.Generic

Friend Module WingDefinition
    Private activeConfiguration As WingConfiguration = WingConfiguration.CreateDefault()

    Friend ReadOnly Property Configuration As WingConfiguration
        Get
            Return activeConfiguration
        End Get
    End Property

    Friend Sub UseConfiguration(ByVal configuration As WingConfiguration)
        Dim validationResult As ConfigurationValidationResult =
            WingConfigurationValidator.Validate(configuration)
        validationResult.ThrowIfInvalid()

        activeConfiguration = configuration
    End Sub

    Friend Sub ResetToDefaultConfiguration()
        activeConfiguration = WingConfiguration.CreateDefault()
    End Sub

    Friend ReadOnly Property FullSpan As Double
        Get
            Return activeConfiguration.FullSpan
        End Get
    End Property

    Friend ReadOnly Property HalfSpan As Double
        Get
            Return activeConfiguration.HalfSpan
        End Get
    End Property

    Friend ReadOnly Property RootChord As Double
        Get
            Return activeConfiguration.RootChord
        End Get
    End Property

    Friend ReadOnly Property TipChord As Double
        Get
            Return activeConfiguration.TipChord
        End Get
    End Property

    Friend ReadOnly Property RibCountPerSide As Integer
        Get
            Return activeConfiguration.Ribs.CountPerSide
        End Get
    End Property

    Friend ReadOnly Property RibThickness As Double
        Get
            Return activeConfiguration.Ribs.Thickness
        End Get
    End Property

    Friend ReadOnly Property PointCountPerSurface As Integer
        Get
            Return activeConfiguration.PointCountPerSurface
        End Get
    End Property

    Friend ReadOnly Property AirfoilMaximumCamber As Double
        Get
            Return activeConfiguration.Airfoil.MaximumCamber
        End Get
    End Property

    Friend ReadOnly Property AirfoilMaximumCamberPosition As Double
        Get
            Return activeConfiguration.Airfoil.MaximumCamberPosition
        End Get
    End Property

    Friend ReadOnly Property AirfoilMaximumThickness As Double
        Get
            Return activeConfiguration.Airfoil.MaximumThickness
        End Get
    End Property

    Friend ReadOnly Property MainSparChordFraction As Double
        Get
            Return activeConfiguration.MainSpar.ChordFraction
        End Get
    End Property

    Friend ReadOnly Property MainSparOuterDiameter As Double
        Get
            Return activeConfiguration.MainSpar.OuterDiameter
        End Get
    End Property

    Friend ReadOnly Property MainSparWallThickness As Double
        Get
            Return activeConfiguration.MainSpar.WallThickness
        End Get
    End Property

    Friend ReadOnly Property MainSparCutoutDiameter As Double
        Get
            Return activeConfiguration.MainSpar.RibCutoutDiameter
        End Get
    End Property

    Friend ReadOnly Property RibLighteningCutoutEdgeMargin As Double
        Get
            Return activeConfiguration.Ribs.LighteningCutoutEdgeMargin
        End Get
    End Property

    Friend ReadOnly Property RibLighteningCutoutMinimumDiameter As Double
        Get
            Return activeConfiguration.Ribs.LighteningCutoutMinimumDiameter
        End Get
    End Property

    Friend ReadOnly Property AileronSpanFraction As Double
        Get
            Return activeConfiguration.Aileron.SpanFraction
        End Get
    End Property

    Friend ReadOnly Property AileronFixedPanelEndX As Double
        Get
            Return activeConfiguration.AileronFixedPanelEndX
        End Get
    End Property

    Friend ReadOnly Property AileronRearSparWidth As Double
        Get
            Return activeConfiguration.AileronRearSparWidth
        End Get
    End Property

    Friend ReadOnly Property AileronRearSparEndX As Double
        Get
            Return activeConfiguration.AileronRearSparEndX
        End Get
    End Property

    Friend ReadOnly Property AileronClearanceGap As Double
        Get
            Return activeConfiguration.AileronClearanceGap
        End Get
    End Property

    Friend ReadOnly Property AileronPanelStartX As Double
        Get
            Return activeConfiguration.AileronPanelStartX
        End Get
    End Property

    Friend ReadOnly Property AileronRibStationTolerance As Double
        Get
            Return RibThickness / 2.0
        End Get
    End Property

    Friend ReadOnly Property MainSparInnerDiameter As Double
        Get
            Return activeConfiguration.MainSpar.InnerDiameter
        End Get
    End Property

    Friend ReadOnly Property AileronSpanLength As Double
        Get
            Return activeConfiguration.AileronSpanLength
        End Get
    End Property

    Friend ReadOnly Property AileronOuterSpanPosition As Double
        Get
            Return activeConfiguration.AileronOuterSpanPosition
        End Get
    End Property

    Friend ReadOnly Property AileronInnerSpanPosition As Double
        Get
            Return activeConfiguration.AileronInnerSpanPosition
        End Get
    End Property

    Friend Function GetRibLighteningCutouts() As List(Of RibLighteningCutoutDefinition)
        Dim cutoutDefinitions As New List(Of RibLighteningCutoutDefinition)()

        If Not activeConfiguration.Ribs.LighteningCutoutsEnabled Then
            Return cutoutDefinitions
        End If

        For Each cutout As RibLighteningCutoutConfiguration In activeConfiguration.Ribs.LighteningCutouts
            cutoutDefinitions.Add(New RibLighteningCutoutDefinition(cutout.Name,
                                                                    cutout.ChordFraction,
                                                                    cutout.PreferredDiameter))
        Next

        Return cutoutDefinitions
    End Function

    Friend Function GetChordAtSpanPosition(ByVal spanPosition As Double) As Double
        Dim spanRatio As Double = Math.Abs(spanPosition) / HalfSpan
        Return RootChord + ((TipChord - RootChord) * spanRatio)
    End Function

    Friend Function GetRibSpanPosition(ByVal ribIndex As Integer) As Double
        Return (HalfSpan / CDbl(RibCountPerSide)) * CDbl(ribIndex)
    End Function

    Friend Function GetMainSparCenterXAtSpanPosition(ByVal spanPosition As Double) As Double
        Return GetChordAtSpanPosition(spanPosition) * MainSparChordFraction
    End Function

    Friend Function GetMainSparCenterZAtSpanPosition(ByVal spanPosition As Double) As Double
        Return GetChordAtSpanPosition(spanPosition) *
            GetMeanCamberAtChordFraction(MainSparChordFraction)
    End Function

    Friend Function GetAileronFixedPanelEndXAtSpanPosition(ByVal spanPosition As Double) As Double
        Return AileronFixedPanelEndX
    End Function

    Friend Function GetAileronRearSparEndXAtSpanPosition(ByVal spanPosition As Double) As Double
        Return AileronRearSparEndX
    End Function

    Friend Function GetAileronPanelStartXAtSpanPosition(ByVal spanPosition As Double) As Double
        Return AileronPanelStartX
    End Function

    Friend Function IsWithinAileronSpan(ByVal spanPosition As Double) As Boolean
        Dim absoluteSpanPosition As Double = Math.Abs(spanPosition)

        If absoluteSpanPosition < 0.000001 Then
            Return False
        End If

        Return absoluteSpanPosition >= (AileronInnerSpanPosition - AileronRibStationTolerance) AndAlso
            absoluteSpanPosition <= (AileronOuterSpanPosition + AileronRibStationTolerance)
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
