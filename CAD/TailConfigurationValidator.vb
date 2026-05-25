Imports System.Globalization

Friend Module TailConfigurationValidator
    Private Const TailDistanceOffsetMinimum As Double = 0.0
    Private Const TailDistanceOffsetMaximum As Double = 10000.0
    Private Const TailPointCountMinimum As Integer = 15
    Private Const TailPointCountMaximum As Integer = 121
    Private Const TailRibThicknessMinimum As Double = 0.5
    Private Const TailRibThicknessMaximum As Double = 20.0
    Private Const HorizontalChordMinimum As Double = 30.0
    Private Const HorizontalChordMaximum As Double = 1000.0
    Private Const HorizontalHalfSpanMinimum As Double = 50.0
    Private Const HorizontalHalfSpanMaximum As Double = 3000.0
    Private Const HorizontalRibCountMinimum As Integer = 2
    Private Const HorizontalRibCountMaximum As Integer = 60
    Private Const VerticalRootChordMinimum As Double = 30.0
    Private Const VerticalRootChordMaximum As Double = 1000.0
    Private Const VerticalTipChordMinimum As Double = 20.0
    Private Const VerticalTipChordMaximum As Double = 1000.0
    Private Const VerticalSpanMinimum As Double = 50.0
    Private Const VerticalSpanMaximum As Double = 3000.0
    Private Const VerticalRibCountMinimum As Integer = 2
    Private Const VerticalRibCountMaximum As Integer = 60
    Private Const TailMainSparDiameterMinimum As Double = 1.0
    Private Const TailMainSparDiameterMaximum As Double = 100.0
    Private Const TailMainSparChordFraction As Double = 0.25
    Private Const TailForwardLighteningCutoutChordFraction As Double = 0.35
    Private Const TailForwardLighteningCutoutRadiusFraction As Double = 0.03
    Private Const TailMiddleLighteningCutoutChordFraction As Double = 0.48
    Private Const TailMiddleLighteningCutoutRadiusFraction As Double = 0.035
    Private Const TailAftLighteningCutoutChordFraction As Double = 0.6
    Private Const TailAftLighteningCutoutRadiusFraction As Double = 0.025
    Private Const TailLighteningCutoutMinimumEdgeMargin As Double = 1.0
    Private Const TailControlSurfaceStartFraction As Double = 0.77
    Private Const MinimumTipControlSurfaceChord As Double = 10.0
    Private Const ComparisonTolerance As Double = 0.000001

    Friend Function Validate(ByVal configuration As TailConfiguration) As ConfigurationValidationResult
        Dim result As New ConfigurationValidationResult()

        If configuration Is Nothing Then
            result.AddError("Tail", "Tail configuration is required.")
            Return result
        End If

        ValidateCommonTailSettings(configuration, result)
        ValidateHorizontalStabilizer(configuration, result)
        ValidateVerticalStabilizer(configuration, result)
        ValidateMainSpar(configuration, result)
        ValidateLighteningCutouts(configuration, result)
        ValidateRudderClearance(configuration, result)

        Return result
    End Function

    Private Sub ValidateCommonTailSettings(ByVal configuration As TailConfiguration,
                                           ByVal result As ConfigurationValidationResult)
        ValidateIntegerRange(result, "Tail.PointCountPerSurface", configuration.PointCountPerSurface, TailPointCountMinimum, TailPointCountMaximum)
        ValidateDoubleRange(result, "Tail.DistanceOffset", configuration.DistanceOffset, TailDistanceOffsetMinimum, TailDistanceOffsetMaximum, "mm")
        ValidateDoubleRange(result, "Tail.RibThickness", configuration.RibThickness, TailRibThicknessMinimum, TailRibThicknessMaximum, "mm")
    End Sub

    Private Sub ValidateHorizontalStabilizer(ByVal configuration As TailConfiguration,
                                             ByVal result As ConfigurationValidationResult)
        If configuration.HorizontalStabilizer Is Nothing Then
            result.AddError("Tail.HorizontalStabilizer", "Horizontal stabilizer configuration is required.")
            Return
        End If

        Dim horizontalTail As HorizontalTailConfiguration = configuration.HorizontalStabilizer

        ValidateDoubleRange(result, "Tail.HorizontalStabilizer.Chord", horizontalTail.Chord, HorizontalChordMinimum, HorizontalChordMaximum, "mm")
        ValidateDoubleRange(result, "Tail.HorizontalStabilizer.HalfSpan", horizontalTail.HalfSpan, HorizontalHalfSpanMinimum, HorizontalHalfSpanMaximum, "mm")
        ValidateIntegerRange(result, "Tail.HorizontalStabilizer.RibCount", horizontalTail.RibCount, HorizontalRibCountMinimum, HorizontalRibCountMaximum)
        ValidateAirfoil("Tail.HorizontalStabilizer.Airfoil", horizontalTail.Airfoil, result)

        If IsFinite(horizontalTail.HalfSpan) AndAlso
            IsFinite(configuration.RibThickness) AndAlso
            horizontalTail.RibCount > 1 Then
            Dim ribSpacing As Double = horizontalTail.FullSpan / CDbl(horizontalTail.RibCount - 1)

            If ribSpacing <= configuration.RibThickness Then
                result.AddError("Tail.HorizontalStabilizer.RibCount", "Horizontal tail rib spacing must be larger than tail rib thickness.")
            End If
        End If
    End Sub

    Private Sub ValidateVerticalStabilizer(ByVal configuration As TailConfiguration,
                                           ByVal result As ConfigurationValidationResult)
        If configuration.VerticalStabilizer Is Nothing Then
            result.AddError("Tail.VerticalStabilizer", "Vertical stabilizer configuration is required.")
            Return
        End If

        Dim verticalTail As VerticalTailConfiguration = configuration.VerticalStabilizer

        ValidateDoubleRange(result, "Tail.VerticalStabilizer.RootChord", verticalTail.RootChord, VerticalRootChordMinimum, VerticalRootChordMaximum, "mm")
        ValidateDoubleRange(result, "Tail.VerticalStabilizer.TipChord", verticalTail.TipChord, VerticalTipChordMinimum, VerticalTipChordMaximum, "mm")
        ValidateDoubleRange(result, "Tail.VerticalStabilizer.Span", verticalTail.Span, VerticalSpanMinimum, VerticalSpanMaximum, "mm")
        ValidateIntegerRange(result, "Tail.VerticalStabilizer.RibCount", verticalTail.RibCount, VerticalRibCountMinimum, VerticalRibCountMaximum)
        ValidateAirfoil("Tail.VerticalStabilizer.Airfoil", verticalTail.Airfoil, result)

        If IsFinite(verticalTail.RootChord) AndAlso
            IsFinite(verticalTail.TipChord) AndAlso
            verticalTail.RootChord < verticalTail.TipChord Then
            result.AddError("Tail.VerticalStabilizer.RootChord", "Vertical stabilizer root chord must be greater than or equal to tip chord for the current tapered tail model.")
        End If

        If IsFinite(verticalTail.Span) AndAlso
            IsFinite(configuration.RibThickness) AndAlso
            verticalTail.RibCount > 1 Then
            Dim ribSpacing As Double = verticalTail.Span / CDbl(verticalTail.RibCount - 1)

            If ribSpacing <= configuration.RibThickness Then
                result.AddError("Tail.VerticalStabilizer.RibCount", "Vertical tail rib spacing must be larger than tail rib thickness.")
            End If
        End If

        If IsFinite(verticalTail.TipChord) Then
            Dim tipControlSurfaceChord As Double =
                verticalTail.TipChord * (1.0 - TailControlSurfaceStartFraction)

            If tipControlSurfaceChord < MinimumTipControlSurfaceChord Then
                result.AddError("Tail.VerticalStabilizer.TipChord", "Vertical stabilizer tip chord is too small for the current rudder geometry.")
            End If
        End If
    End Sub

    Private Sub ValidateMainSpar(ByVal configuration As TailConfiguration,
                                 ByVal result As ConfigurationValidationResult)
        If configuration.MainSpar Is Nothing Then
            result.AddError("Tail.MainSpar", "Tail main spar configuration is required.")
            Return
        End If

        ValidateDoubleRange(result, "Tail.MainSpar.MainSparDiameter", configuration.MainSpar.MainSparDiameter, TailMainSparDiameterMinimum, TailMainSparDiameterMaximum, "mm")

        If configuration.HorizontalStabilizer IsNot Nothing Then
            ValidateTailSparFitsAirfoil(result,
                                        "Tail.MainSpar.MainSparDiameter",
                                        configuration.MainSpar.MainSparDiameter,
                                        configuration.HorizontalStabilizer.Chord,
                                        configuration.HorizontalStabilizer.Airfoil,
                                        "horizontal stabilizer")
        End If

        If configuration.VerticalStabilizer IsNot Nothing Then
            ValidateTailSparFitsAirfoil(result,
                                        "Tail.MainSpar.MainSparDiameter",
                                        configuration.MainSpar.MainSparDiameter,
                                        configuration.VerticalStabilizer.TipChord,
                                        configuration.VerticalStabilizer.Airfoil,
                                        "vertical stabilizer tip")
        End If
    End Sub

    Private Sub ValidateRudderClearance(ByVal configuration As TailConfiguration,
                                        ByVal result As ConfigurationValidationResult)
        If Not IsFinite(configuration.RudderClearance) Then
            result.AddError("Tail.RudderClearance", "Value must be a finite number.")
            Return
        End If

        If configuration.RudderClearance < 0.0 Then
            result.AddError("Tail.RudderClearance", "Rudder clearance must be greater than or equal to 0 mm.")
            Return
        End If

        If configuration.VerticalStabilizer Is Nothing OrElse
            Not IsFinite(configuration.VerticalStabilizer.Span) Then
            Return
        End If

        If configuration.RudderClearance >= configuration.VerticalStabilizer.Span Then
            result.AddError("Tail.RudderClearance", "Rudder clearance must be less than vertical stabilizer span.")
        End If

        If configuration.RudderClearance > (configuration.VerticalStabilizer.Span * 0.5) Then
            result.AddError("Tail.RudderClearance", "Rudder clearance should not exceed half of the vertical stabilizer span in Plan A1.")
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

    Private Sub ValidateTailSparFitsAirfoil(ByVal result As ConfigurationValidationResult,
                                            ByVal fieldName As String,
                                            ByVal sparDiameter As Double,
                                            ByVal chordLength As Double,
                                            ByVal airfoil As AirfoilConfiguration,
                                            ByVal context As String)
        If airfoil Is Nothing OrElse
            Not IsFinite(sparDiameter) OrElse
            Not IsFinite(chordLength) Then
            Return
        End If

        Dim availableDiameter As Double =
            2.0 * GetAirfoilHalfThicknessAtChordFraction(airfoil,
                                                         TailMainSparChordFraction,
                                                         chordLength)

        If sparDiameter > availableDiameter Then
            result.AddError(fieldName, "Tail main spar diameter does not fit inside the " & context & " airfoil at 25 percent chord.")
        End If
    End Sub

    Private Sub ValidateLighteningCutouts(ByVal configuration As TailConfiguration,
                                          ByVal result As ConfigurationValidationResult)
        If Not configuration.LighteningCutoutsEnabled Then
            Return
        End If

        If configuration.HorizontalStabilizer IsNot Nothing AndAlso
            configuration.HorizontalStabilizer.RibCount > 2 Then
            ValidateTailCutoutPatternFitsAirfoil(result,
                                                 "Tail.HorizontalStabilizer.Airfoil",
                                                 configuration.HorizontalStabilizer.Chord,
                                                 configuration.HorizontalStabilizer.Airfoil,
                                                 "horizontal tail ribs")
        End If

        If configuration.VerticalStabilizer IsNot Nothing AndAlso
            configuration.VerticalStabilizer.RibCount > 2 Then
            ValidateTailCutoutPatternFitsAirfoil(result,
                                                 "Tail.VerticalStabilizer.Airfoil",
                                                 GetSmallestVerticalInteriorRibChord(configuration.VerticalStabilizer),
                                                 configuration.VerticalStabilizer.Airfoil,
                                                 "vertical tail ribs")
        End If
    End Sub

    Private Sub ValidateTailCutoutPatternFitsAirfoil(ByVal result As ConfigurationValidationResult,
                                                     ByVal fieldName As String,
                                                     ByVal chordLength As Double,
                                                     ByVal airfoil As AirfoilConfiguration,
                                                     ByVal context As String)
        Dim parsedAirfoil As AirfoilConfiguration = Nothing

        If airfoil Is Nothing OrElse
            Not NacaAirfoilParser.TryParse(airfoil.NacaCode, parsedAirfoil) Then
            Return
        End If

        ValidateTailLighteningCutoutFitsAirfoil(result,
                                                fieldName,
                                                chordLength,
                                                airfoil,
                                                TailForwardLighteningCutoutChordFraction,
                                                TailForwardLighteningCutoutRadiusFraction,
                                                context,
                                                "forward")
        ValidateTailLighteningCutoutFitsAirfoil(result,
                                                fieldName,
                                                chordLength,
                                                airfoil,
                                                TailMiddleLighteningCutoutChordFraction,
                                                TailMiddleLighteningCutoutRadiusFraction,
                                                context,
                                                "middle")
        ValidateTailLighteningCutoutFitsAirfoil(result,
                                                fieldName,
                                                chordLength,
                                                airfoil,
                                                TailAftLighteningCutoutChordFraction,
                                                TailAftLighteningCutoutRadiusFraction,
                                                context,
                                                "aft")
    End Sub

    Private Sub ValidateTailLighteningCutoutFitsAirfoil(ByVal result As ConfigurationValidationResult,
                                                        ByVal fieldName As String,
                                                        ByVal chordLength As Double,
                                                        ByVal airfoil As AirfoilConfiguration,
                                                        ByVal chordFraction As Double,
                                                        ByVal radiusFraction As Double,
                                                        ByVal context As String,
                                                        ByVal cutoutName As String)
        If airfoil Is Nothing OrElse
            Not IsFinite(chordLength) OrElse
            Not IsFinite(chordFraction) OrElse
            Not IsFinite(radiusFraction) Then
            Return
        End If

        Dim availableRadius As Double =
            GetAirfoilHalfThicknessAtChordFraction(airfoil,
                                                   chordFraction,
                                                   chordLength)
        Dim requiredRadius As Double =
            (chordLength * radiusFraction) + TailLighteningCutoutMinimumEdgeMargin

        If requiredRadius > availableRadius Then
            result.AddError(fieldName,
                            "The fixed " & cutoutName &
                            " lightening cutout does not fit inside the " &
                            context & " for this airfoil/chord. Use a thicker NACA airfoil or a larger chord.")
        End If
    End Sub

    Private Function GetSmallestVerticalInteriorRibChord(ByVal verticalTail As VerticalTailConfiguration) As Double
        If verticalTail Is Nothing Then
            Return 0.0
        End If

        If verticalTail.RibCount <= 2 Then
            Return verticalTail.TipChord
        End If

        Dim lastInteriorRibIndex As Integer = verticalTail.RibCount - 2
        Dim normalizedSpan As Double =
            CDbl(lastInteriorRibIndex) / CDbl(verticalTail.RibCount - 1)

        Return verticalTail.RootChord +
            (normalizedSpan * (verticalTail.TipChord - verticalTail.RootChord))
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
