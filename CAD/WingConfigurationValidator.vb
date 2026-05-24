Imports System.Collections.Generic
Imports System.Globalization

Friend Module WingConfigurationValidator
    Private Const WingFullSpanMinimum As Double = 500.0
    Private Const WingFullSpanMaximum As Double = 10000.0
    Private Const WingChordMinimum As Double = 50.0
    Private Const WingChordMaximum As Double = 2000.0
    Private Const WingSweepAngleDegreesMinimum As Double = 0.0
    Private Const WingSweepAngleDegreesMaximum As Double = 30.0
    Private Const WingDihedralAngleDegreesMinimum As Double = 0.0
    Private Const WingDihedralAngleDegreesMaximum As Double = 8.0
    Private Const WingRibCountPerSideMinimum As Integer = 2
    Private Const WingRibCountPerSideMaximum As Integer = 80
    Private Const WingRibThicknessMinimum As Double = 0.5
    Private Const WingRibThicknessMaximum As Double = 20.0
    Private Const WingPointCountMinimum As Integer = 15
    Private Const WingPointCountMaximum As Integer = 121
    Private Const WingSparChordFractionMinimum As Double = 0.15
    Private Const WingSparChordFractionMaximum As Double = 0.6
    Private Const WingSparOuterDiameterMinimum As Double = 2.0
    Private Const WingSparOuterDiameterMaximum As Double = 150.0
    Private Const WingSparWallThicknessMinimum As Double = 0.2
    Private Const WingSparWallThicknessMaximum As Double = 25.0
    Private Const WingSparCutoutExtraClearanceMaximum As Double = 20.0
    Private Const WingAileronSpanFractionMinimum As Double = 0.15
    Private Const WingAileronSpanFractionMaximum As Double = 0.6
    Private Const WingLighteningCutoutRequiredEdgeMargin As Double = 6.0
    Private Const ComparisonTolerance As Double = 0.000001

    Friend Function Validate(ByVal configuration As WingConfiguration) As ConfigurationValidationResult
        Dim result As New ConfigurationValidationResult()

        If configuration Is Nothing Then
            result.AddError("Wing", "Wing configuration is required.")
            Return result
        End If

        ValidatePlanform(configuration, result)
        ValidateAirfoil("Wing.Airfoil", configuration.Airfoil, result)
        ValidateRibs(configuration, result)
        ValidateMainSpar(configuration, result)
        ValidateLighteningCutouts(configuration, result)
        ValidateAileron(configuration, result)

        Return result
    End Function

    Private Sub ValidatePlanform(ByVal configuration As WingConfiguration,
                                 ByVal result As ConfigurationValidationResult)
        ValidateDoubleRange(result, "Wing.FullSpan", configuration.FullSpan, WingFullSpanMinimum, WingFullSpanMaximum, "mm")
        ValidateDoubleRange(result, "Wing.RootChord", configuration.RootChord, WingChordMinimum, WingChordMaximum, "mm")
        ValidateDoubleRange(result, "Wing.TipChord", configuration.TipChord, WingChordMinimum, WingChordMaximum, "mm")
        ValidateDoubleRange(result, "Wing.SweepAngleDegrees", configuration.SweepAngleDegrees, WingSweepAngleDegreesMinimum, WingSweepAngleDegreesMaximum, "degrees")
        ValidateDoubleRange(result, "Wing.DihedralAngleDegrees", configuration.DihedralAngleDegrees, WingDihedralAngleDegreesMinimum, WingDihedralAngleDegreesMaximum, "degrees")
        ValidateIntegerRange(result, "Wing.PointCountPerSurface", configuration.PointCountPerSurface, WingPointCountMinimum, WingPointCountMaximum)

        If IsFinite(configuration.RootChord) AndAlso
            IsFinite(configuration.TipChord) AndAlso
            configuration.RootChord < configuration.TipChord Then
            result.AddError("Wing.RootChord", "Root chord must be greater than or equal to tip chord for the current tapered wing model.")
        End If
    End Sub

    Private Sub ValidateAirfoil(ByVal fieldName As String,
                                ByVal airfoil As AirfoilConfiguration,
                                ByVal result As ConfigurationValidationResult)
        If airfoil Is Nothing Then
            result.AddError(fieldName, "Airfoil configuration is required.")
            Return
        End If

        Dim parsedAirfoil As AirfoilConfiguration = Nothing
        Dim parseError As String = Nothing

        If Not NacaAirfoilParser.TryParse(airfoil.NacaCode, parsedAirfoil, parseError) Then
            result.AddError(fieldName, parseError)
            Return
        End If

        ValidateDoubleRange(result, fieldName & ".MaximumCamber", airfoil.MaximumCamber, 0.0, 0.09, "")
        ValidateDoubleRange(result, fieldName & ".MaximumCamberPosition", airfoil.MaximumCamberPosition, 0.0, 0.9, "")
        ValidateDoubleRange(result, fieldName & ".MaximumThickness", airfoil.MaximumThickness, 0.01, 0.99, "")

        If Not AreClose(airfoil.MaximumCamber, parsedAirfoil.MaximumCamber) OrElse
            Not AreClose(airfoil.MaximumCamberPosition, parsedAirfoil.MaximumCamberPosition) OrElse
            Not AreClose(airfoil.MaximumThickness, parsedAirfoil.MaximumThickness) Then
            result.AddError(fieldName, "Airfoil numeric values must match the NACA 4-digit code.")
        End If
    End Sub

    Private Sub ValidateRibs(ByVal configuration As WingConfiguration,
                             ByVal result As ConfigurationValidationResult)
        If configuration.Ribs Is Nothing Then
            result.AddError("Wing.Ribs", "Wing rib configuration is required.")
            Return
        End If

        ValidateIntegerRange(result, "Wing.Ribs.CountPerSide", configuration.Ribs.CountPerSide, WingRibCountPerSideMinimum, WingRibCountPerSideMaximum)
        ValidateDoubleRange(result, "Wing.Ribs.Thickness", configuration.Ribs.Thickness, WingRibThicknessMinimum, WingRibThicknessMaximum, "mm")

        If IsFinite(configuration.FullSpan) AndAlso
            IsFinite(configuration.Ribs.Thickness) AndAlso
            configuration.Ribs.CountPerSide > 0 Then
            Dim ribSpacing As Double = configuration.HalfSpan / CDbl(configuration.Ribs.CountPerSide)

            If ribSpacing <= configuration.Ribs.Thickness Then
                result.AddError("Wing.Ribs.CountPerSide", "Rib spacing must be larger than rib thickness.")
            End If
        End If
    End Sub

    Private Sub ValidateMainSpar(ByVal configuration As WingConfiguration,
                                 ByVal result As ConfigurationValidationResult)
        If configuration.MainSpar Is Nothing Then
            result.AddError("Wing.MainSpar", "Wing main spar configuration is required.")
            Return
        End If

        ValidateDoubleRange(result, "Wing.MainSpar.ChordFraction", configuration.MainSpar.ChordFraction, WingSparChordFractionMinimum, WingSparChordFractionMaximum, "")
        ValidateDoubleRange(result, "Wing.MainSpar.OuterDiameter", configuration.MainSpar.OuterDiameter, WingSparOuterDiameterMinimum, WingSparOuterDiameterMaximum, "mm")
        ValidateDoubleRange(result, "Wing.MainSpar.WallThickness", configuration.MainSpar.WallThickness, WingSparWallThicknessMinimum, WingSparWallThicknessMaximum, "mm")

        If IsFinite(configuration.MainSpar.OuterDiameter) AndAlso
            IsFinite(configuration.MainSpar.WallThickness) AndAlso
            (2.0 * configuration.MainSpar.WallThickness) >= configuration.MainSpar.OuterDiameter Then
            result.AddError("Wing.MainSpar.WallThickness", "Two wall thicknesses must be less than the spar outer diameter.")
        End If

        If IsFinite(configuration.MainSpar.InnerDiameter) AndAlso
            configuration.MainSpar.InnerDiameter <= 0.0 Then
            result.AddError("Wing.MainSpar.WallThickness", "Main spar inner diameter must be positive.")
        End If

        If IsFinite(configuration.MainSpar.RibCutoutDiameter) AndAlso
            IsFinite(configuration.MainSpar.OuterDiameter) Then
            If configuration.MainSpar.RibCutoutDiameter < configuration.MainSpar.OuterDiameter Then
                result.AddError("Wing.MainSpar.RibCutoutDiameter", "Rib cutout diameter must be greater than or equal to the main spar outer diameter.")
            End If

            If configuration.MainSpar.RibCutoutDiameter >
                configuration.MainSpar.OuterDiameter + WingSparCutoutExtraClearanceMaximum Then
                result.AddError("Wing.MainSpar.RibCutoutDiameter", "Rib cutout diameter should not exceed spar outer diameter by more than 20 mm in Plan A1.")
            End If
        Else
            result.AddError("Wing.MainSpar.RibCutoutDiameter", "Rib cutout diameter must be a finite number.")
        End If

        ValidateSparCutoutFitsPlanformAngles(configuration, result)
        ValidateSparFitsTipAirfoil(configuration, result)
        ValidateSparFitsAileronForwardPanel(configuration, result)
    End Sub

    Private Sub ValidateLighteningCutouts(ByVal configuration As WingConfiguration,
                                          ByVal result As ConfigurationValidationResult)
        If configuration.Ribs Is Nothing OrElse Not configuration.Ribs.LighteningCutoutsEnabled Then
            Return
        End If

        If configuration.Ribs.LighteningCutouts Is Nothing OrElse configuration.Ribs.LighteningCutouts.Count = 0 Then
            result.AddError("Wing.Ribs.LighteningCutouts", "Lightening cutouts are enabled, so at least one cutout definition is required.")
            Return
        End If

        configuration.Ribs.EnsureLighteningCutoutSlots()
        ValidateLighteningCutoutOrder(configuration, result)
        ValidateLighteningCutoutSpacing(configuration, result)

        For cutoutIndex As Integer = 0 To configuration.Ribs.LighteningCutouts.Count - 1
            Dim cutout As RibLighteningCutoutConfiguration = configuration.Ribs.LighteningCutouts(cutoutIndex)
            Dim fieldPrefix As String = "Wing.Ribs.LighteningCutouts[" & cutoutIndex.ToString(CultureInfo.InvariantCulture) & "]"

            If cutout Is Nothing Then
                result.AddError(fieldPrefix, "Cutout definition is required.")
                Continue For
            End If

            ValidateDoubleRange(result, fieldPrefix & ".ChordFraction", cutout.ChordFraction, 0.05, 0.85, "")
            ValidateDoubleRange(result, fieldPrefix & ".PreferredDiameter", cutout.PreferredDiameter, 1.0, 200.0, "mm")
            ValidateLighteningCutoutFitsTipAirfoil(configuration, cutout, fieldPrefix, result)

            If configuration.MainSpar IsNot Nothing AndAlso
                IsFinite(cutout.ChordFraction) AndAlso
                IsFinite(cutout.PreferredDiameter) AndAlso
                IsFinite(configuration.MainSpar.ChordFraction) AndAlso
                IsFinite(configuration.MainSpar.RibCutoutDiameter) AndAlso
                DoCutoutsOverlapAtTip(configuration, cutout) Then
                result.AddError(fieldPrefix, "Lightening cutout overlaps the main spar cutout at the wing tip.")
            End If
        Next
    End Sub

    Private Sub ValidateLighteningCutoutOrder(ByVal configuration As WingConfiguration,
                                              ByVal result As ConfigurationValidationResult)
        Dim forwardCutout As RibLighteningCutoutConfiguration =
            configuration.Ribs.GetForwardLighteningCutout()
        Dim middleCutout As RibLighteningCutoutConfiguration =
            configuration.Ribs.GetMiddleLighteningCutout()
        Dim aftCutout As RibLighteningCutoutConfiguration =
            configuration.Ribs.GetAftLighteningCutout()

        If Not IsFinite(forwardCutout.ChordFraction) OrElse
            Not IsFinite(middleCutout.ChordFraction) OrElse
            Not IsFinite(aftCutout.ChordFraction) Then
            Return
        End If

        If forwardCutout.ChordFraction >= middleCutout.ChordFraction Then
            result.AddError("Wing.Ribs.LighteningCutouts[0].ChordFraction",
                            "Forward lightening cutout chord fraction must be less than the middle lightening cutout chord fraction.")
        End If

        If middleCutout.ChordFraction >= aftCutout.ChordFraction Then
            result.AddError("Wing.Ribs.LighteningCutouts[1].ChordFraction",
                            "Middle lightening cutout chord fraction must be less than the aft lightening cutout chord fraction.")
        End If
    End Sub

    Private Sub ValidateLighteningCutoutSpacing(ByVal configuration As WingConfiguration,
                                                ByVal result As ConfigurationValidationResult)
        If Not IsFinite(configuration.TipChord) Then
            Return
        End If

        Dim cutouts As List(Of RibLighteningCutoutConfiguration) = configuration.Ribs.LighteningCutouts

        For firstIndex As Integer = 0 To cutouts.Count - 2
            Dim firstCutout As RibLighteningCutoutConfiguration = cutouts(firstIndex)

            If firstCutout Is Nothing Then
                Continue For
            End If

            If Not IsFinite(firstCutout.ChordFraction) OrElse
                Not IsFinite(firstCutout.PreferredDiameter) OrElse
                firstCutout.PreferredDiameter <= 0.0 Then
                Continue For
            End If

            Dim firstDiameter As Double = firstCutout.PreferredDiameter

            For secondIndex As Integer = firstIndex + 1 To cutouts.Count - 1
                Dim secondCutout As RibLighteningCutoutConfiguration = cutouts(secondIndex)

                If secondCutout Is Nothing Then
                    Continue For
                End If

                If Not IsFinite(secondCutout.ChordFraction) OrElse
                    Not IsFinite(secondCutout.PreferredDiameter) OrElse
                    secondCutout.PreferredDiameter <= 0.0 Then
                    Continue For
                End If

                Dim secondDiameter As Double = secondCutout.PreferredDiameter

                Dim firstCenterX As Double = configuration.TipChord * firstCutout.ChordFraction
                Dim secondCenterX As Double = configuration.TipChord * secondCutout.ChordFraction
                Dim centerDistance As Double = Math.Abs(firstCenterX - secondCenterX)
                Dim requiredDistance As Double = (firstDiameter / 2.0) + (secondDiameter / 2.0)

                If centerDistance <= requiredDistance Then
                    result.AddError("Wing.Ribs.LighteningCutouts[" & firstIndex.ToString(CultureInfo.InvariantCulture) & "]",
                                    "Lightening cutouts overlap each other at the wing tip.")
                    Return
                End If
            Next
        Next
    End Sub

    Private Sub ValidateAileron(ByVal configuration As WingConfiguration,
                                ByVal result As ConfigurationValidationResult)
        If configuration.Aileron Is Nothing Then
            result.AddError("Wing.Aileron", "Aileron configuration is required.")
            Return
        End If

        ValidateDoubleRange(result, "Wing.Aileron.SpanFraction", configuration.Aileron.SpanFraction, WingAileronSpanFractionMinimum, WingAileronSpanFractionMaximum, "")

        If configuration.Ribs IsNot Nothing AndAlso
            IsFinite(configuration.Aileron.SpanFraction) AndAlso
            configuration.Ribs.CountPerSide > 0 Then
            Dim actualRibsInsideAileron As Integer = CountActualRibStationsInsideAileron(configuration)

            If actualRibsInsideAileron < 1 Then
                result.AddError("Wing.Aileron.SpanFraction", "Aileron span must include at least one actual rib station on each side.")
            End If
        End If

        If IsFinite(configuration.AileronPanelStartX) AndAlso
            IsFinite(configuration.TipChord) AndAlso
            configuration.AileronPanelStartX >= configuration.TipChord Then
            result.AddError("Wing.Aileron.SpanFraction", "Computed aileron panel start must remain forward of the tip trailing edge.")
        End If
    End Sub

    Private Sub ValidateSparFitsTipAirfoil(ByVal configuration As WingConfiguration,
                                           ByVal result As ConfigurationValidationResult)
        If configuration.Airfoil Is Nothing OrElse configuration.MainSpar Is Nothing Then
            Return
        End If

        If Not IsFinite(configuration.TipChord) OrElse
            Not IsFinite(configuration.MainSpar.ChordFraction) OrElse
            Not IsFinite(configuration.MainSpar.OuterDiameter) OrElse
            Not IsFinite(configuration.MainSpar.RibCutoutDiameter) Then
            Return
        End If

        Dim availableDiameter As Double =
            2.0 * GetAirfoilHalfThicknessAtChordFraction(configuration.Airfoil,
                                                         configuration.MainSpar.ChordFraction,
                                                         configuration.TipChord)

        If configuration.MainSpar.OuterDiameter > availableDiameter Then
            result.AddError("Wing.MainSpar.OuterDiameter", "Main spar outer diameter does not fit inside the tip airfoil at the selected chord fraction.")
        End If

        If configuration.MainSpar.RibCutoutDiameter > availableDiameter Then
            result.AddError("Wing.MainSpar.RibCutoutDiameter", "Main spar rib cutout diameter does not fit inside the tip airfoil at the selected chord fraction.")
        End If
    End Sub

    Private Sub ValidateSparCutoutFitsPlanformAngles(ByVal configuration As WingConfiguration,
                                                     ByVal result As ConfigurationValidationResult)
        If configuration.MainSpar Is Nothing Then
            Return
        End If

        If Not IsFinite(configuration.SweepAngleDegrees) OrElse
            Not IsFinite(configuration.DihedralAngleDegrees) OrElse
            Not IsFinite(configuration.MainSpar.OuterDiameter) OrElse
            Not IsFinite(configuration.MainSpar.RibCutoutDiameter) Then
            Return
        End If

        Dim sweepTangent As Double = Math.Tan(configuration.SweepAngleDegrees * Math.PI / 180.0)
        Dim dihedralTangent As Double = Math.Tan(configuration.DihedralAngleDegrees * Math.PI / 180.0)
        Dim projectedSparDiameter As Double =
            configuration.MainSpar.OuterDiameter *
            Math.Sqrt(1.0 + (sweepTangent * sweepTangent) + (dihedralTangent * dihedralTangent))

        If configuration.MainSpar.RibCutoutDiameter < (projectedSparDiameter - ComparisonTolerance) Then
            result.AddError("Wing.MainSpar.RibCutoutDiameter",
                            "Rib cutout diameter must be at least " &
                            FormatNumber(projectedSparDiameter) &
                            " mm to clear the swept/dihedral main spar at the selected planform angles.")
        End If
    End Sub

    Private Sub ValidateSparFitsAileronForwardPanel(ByVal configuration As WingConfiguration,
                                                    ByVal result As ConfigurationValidationResult)
        If configuration.MainSpar Is Nothing Then
            Return
        End If

        If Not IsFinite(configuration.TipChord) OrElse
            Not IsFinite(configuration.MainSpar.ChordFraction) OrElse
            Not IsFinite(configuration.MainSpar.RibCutoutDiameter) Then
            Return
        End If

        Dim sparCenterXAtTip As Double = configuration.TipChord * configuration.MainSpar.ChordFraction
        Dim sparCutoutAftXAtTip As Double = sparCenterXAtTip + (configuration.MainSpar.RibCutoutDiameter / 2.0)

        If sparCutoutAftXAtTip >= configuration.AileronFixedPanelEndX Then
            result.AddError("Wing.MainSpar.ChordFraction", "Main spar cutout must remain fully inside the forward fixed wing panel in the aileron span.")
        End If
    End Sub

    Private Function CountActualRibStationsInsideAileron(ByVal configuration As WingConfiguration) As Integer
        Dim count As Integer = 0
        Dim stationSpacing As Double = configuration.HalfSpan / CDbl(configuration.Ribs.CountPerSide)

        For ribIndex As Integer = 1 To configuration.Ribs.CountPerSide
            Dim ribSpanPosition As Double = stationSpacing * CDbl(ribIndex)

            If ribSpanPosition > (configuration.AileronInnerSpanPosition + ComparisonTolerance) AndAlso
                ribSpanPosition <= (configuration.AileronOuterSpanPosition + ComparisonTolerance) Then
                count += 1
            End If
        Next

        Return count
    End Function

    Private Function DoCutoutsOverlapAtTip(ByVal configuration As WingConfiguration,
                                           ByVal cutout As RibLighteningCutoutConfiguration) As Boolean
        Dim cutoutCenterX As Double = configuration.TipChord * cutout.ChordFraction
        Dim sparCenterX As Double = configuration.TipChord * configuration.MainSpar.ChordFraction
        Dim centerDistance As Double = Math.Abs(cutoutCenterX - sparCenterX)
        Dim requiredDistance As Double = (cutout.PreferredDiameter / 2.0) + (configuration.MainSpar.RibCutoutDiameter / 2.0)

        Return centerDistance <= requiredDistance
    End Function

    Private Sub ValidateLighteningCutoutFitsTipAirfoil(ByVal configuration As WingConfiguration,
                                                       ByVal cutout As RibLighteningCutoutConfiguration,
                                                       ByVal fieldPrefix As String,
                                                       ByVal result As ConfigurationValidationResult)
        Dim availableDiameter As Double = GetAvailableLighteningCutoutDiameterAtTip(configuration, cutout)

        If Not IsFinite(availableDiameter) Then
            Return
        End If

        If availableDiameter <= ComparisonTolerance Then
            result.AddError(fieldPrefix & ".ChordFraction",
                            "No lightening cutout can fit at the wing tip at this chord fraction while keeping " &
                            FormatNumber(WingLighteningCutoutRequiredEdgeMargin) &
                            " mm of rib material above and below the hole.")
            Return
        End If

        If IsFinite(cutout.PreferredDiameter) AndAlso
            cutout.PreferredDiameter > (availableDiameter + ComparisonTolerance) Then
            result.AddError(fieldPrefix & ".PreferredDiameter",
                            "Diameter is too large for the wing tip at this chord fraction. Maximum safe exact diameter is " &
                            FormatNumber(availableDiameter) &
                            " mm with " &
                            FormatNumber(WingLighteningCutoutRequiredEdgeMargin) &
                            " mm of rib material above and below the hole.")
        End If
    End Sub

    Private Function GetAvailableLighteningCutoutDiameterAtTip(ByVal configuration As WingConfiguration,
                                                               ByVal cutout As RibLighteningCutoutConfiguration) As Double
        If configuration.Airfoil Is Nothing OrElse configuration.Ribs Is Nothing OrElse cutout Is Nothing Then
            Return Double.NaN
        End If

        If Not IsFinite(configuration.TipChord) OrElse
            Not IsFinite(cutout.ChordFraction) OrElse
            cutout.ChordFraction <= 0.0 OrElse
            cutout.ChordFraction >= 1.0 Then
            Return Double.NaN
        End If

        Dim airfoilHalfThickness As Double =
            GetAirfoilHalfThicknessAtChordFraction(configuration.Airfoil,
                                                   cutout.ChordFraction,
                                                   configuration.TipChord)
        Return (2.0 * airfoilHalfThickness) - (2.0 * WingLighteningCutoutRequiredEdgeMargin)
    End Function

    Private Function GetAirfoilHalfThicknessAtChordFraction(ByVal airfoil As AirfoilConfiguration,
                                                            ByVal chordFraction As Double,
                                                            ByVal chordLength As Double) As Double
        If airfoil Is Nothing OrElse chordFraction <= 0.0 OrElse chordFraction >= 1.0 Then
            Return 0.0
        End If

        Dim halfThickness As Double = 5.0 * airfoil.MaximumThickness *
            ((0.2969 * Math.Sqrt(chordFraction)) -
             (0.1260 * chordFraction) -
             (0.3516 * Math.Pow(chordFraction, 2.0)) +
             (0.2843 * Math.Pow(chordFraction, 3.0)) -
             (0.1036 * Math.Pow(chordFraction, 4.0)))
        Dim camberSlope As Double = GetMeanCamberSlopeAtChordFraction(airfoil, chordFraction)

        Return halfThickness * Math.Cos(Math.Atan(camberSlope)) * chordLength
    End Function

    Private Function GetMeanCamberSlopeAtChordFraction(ByVal airfoil As AirfoilConfiguration,
                                                       ByVal chordFraction As Double) As Double
        If airfoil Is Nothing OrElse
            airfoil.MaximumCamber <= 0.0 OrElse
            airfoil.MaximumCamberPosition <= 0.0 Then
            Return 0.0
        End If

        If chordFraction < airfoil.MaximumCamberPosition Then
            Return ((2.0 * airfoil.MaximumCamber) /
                    (airfoil.MaximumCamberPosition * airfoil.MaximumCamberPosition)) *
                (airfoil.MaximumCamberPosition - chordFraction)
        End If

        Dim aftCamberLength As Double = 1.0 - airfoil.MaximumCamberPosition

        If aftCamberLength <= 0.0 Then
            Return 0.0
        End If

        Return ((2.0 * airfoil.MaximumCamber) / (aftCamberLength * aftCamberLength)) *
            (airfoil.MaximumCamberPosition - chordFraction)
    End Function

    Private Sub ValidateDoubleRange(ByVal result As ConfigurationValidationResult,
                                    ByVal fieldName As String,
                                    ByVal value As Double,
                                    ByVal minimum As Double,
                                    ByVal maximum As Double,
                                    ByVal unitLabel As String)
        If Not IsFinite(value) Then
            result.AddError(fieldName, "Value must be a finite number.")
            Return
        End If

        If value < minimum OrElse value > maximum Then
            Dim suffix As String = If(String.IsNullOrWhiteSpace(unitLabel), "", " " & unitLabel)
            result.AddError(fieldName,
                            "Value must be between " & FormatNumber(minimum) & suffix &
                            " and " & FormatNumber(maximum) & suffix & ".")
        End If
    End Sub

    Private Sub ValidateIntegerRange(ByVal result As ConfigurationValidationResult,
                                     ByVal fieldName As String,
                                     ByVal value As Integer,
                                     ByVal minimum As Integer,
                                     ByVal maximum As Integer)
        If value < minimum OrElse value > maximum Then
            result.AddError(fieldName,
                            "Value must be between " & minimum.ToString(CultureInfo.InvariantCulture) &
                            " and " & maximum.ToString(CultureInfo.InvariantCulture) & ".")
        End If
    End Sub

    Private Function IsFinite(ByVal value As Double) As Boolean
        Return Not Double.IsNaN(value) AndAlso Not Double.IsInfinity(value)
    End Function

    Private Function AreClose(ByVal first As Double,
                              ByVal second As Double) As Boolean
        Return Math.Abs(first - second) <= ComparisonTolerance
    End Function

    Private Function FormatNumber(ByVal value As Double) As String
        Return value.ToString("0.###", CultureInfo.InvariantCulture)
    End Function
End Module
