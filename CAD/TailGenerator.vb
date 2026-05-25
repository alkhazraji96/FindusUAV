Imports INFITF
Imports MECMOD
Imports PARTITF
Imports HybridShapeTypeLib

Public Class TailGenerator
    Private Const TailOperationName As String = "Tail generation"
    Private Const TailMainSparChordFraction As Double = 0.25

    Private Shared Function PrepareProgressReporter(ByVal progressReporter As IGenerationProgressReporter) As IGenerationProgressReporter
        Return GenerationProgress.UseDefaultReporterWhenMissing(progressReporter)
    End Function

    Private Shared Sub ReportTailStarting(ByVal progressReporter As IGenerationProgressReporter)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateStarting(TailOperationName,
                                                                          "Starting tail generation."))
    End Sub

    Private Shared Sub ReportTailStep(ByVal progressReporter As IGenerationProgressReporter,
                                      ByVal stepName As String,
                                      ByVal message As String,
                                      ByVal currentStep As Integer,
                                      ByVal totalSteps As Integer)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateStep(TailOperationName,
                                                                      stepName,
                                                                      message,
                                                                      currentStep,
                                                                      totalSteps))
    End Sub

    Private Shared Sub ReportTailCompleted(ByVal progressReporter As IGenerationProgressReporter)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateCompleted(TailOperationName,
                                                                           "Tail generation complete."))
    End Sub

    Private Shared Sub ReportTailFailed(ByVal progressReporter As IGenerationProgressReporter,
                                        ByVal message As String)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateFailed(TailOperationName,
                                                                        message))
    End Sub

    Private Shared Sub ReportTailFailed(ByVal progressReporter As IGenerationProgressReporter,
                                        ByVal exception As Exception)
        Dim message As String = "Tail generation failed."

        If exception IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(exception.Message) Then
            message &= " " & exception.Message
        End If

        ReportTailFailed(progressReporter, message)
    End Sub

    Public Shared Sub Run()
        Run(TailConfiguration.CreateDefault())
    End Sub

    Friend Shared Sub Run(ByVal configuration As TailConfiguration,
                          Optional ByVal progressReporter As IGenerationProgressReporter = Nothing)
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Const totalSteps As Integer = 10
        Dim displayRefreshScope As IDisposable = Nothing

        ReportTailStarting(activeProgressReporter)

        Try
            ReportTailStep(activeProgressReporter, "Validation", "Validating tail configuration.", 1, totalSteps)
            Dim validationResult As ConfigurationValidationResult =
                TailConfigurationValidator.Validate(configuration)
            validationResult.ThrowIfInvalid()

            Dim resolution As Integer = configuration.PointCountPerSurface
            Dim ribThickness As Double = configuration.RibThickness
            Dim tailLighteningCutoutsEnabled As Boolean = configuration.LighteningCutoutsEnabled
            Dim mainSparRadius As Double = configuration.MainSpar.MainSparRadius

            Dim tailDistanceOffset As Double = configuration.DistanceOffset
            Dim horizChord As Double = configuration.HorizontalStabilizer.Chord

            Dim horizHalfSpan As Double = configuration.HorizontalStabilizer.HalfSpan
            Dim horizRibCount As Integer = configuration.HorizontalStabilizer.RibCount
            Dim horizOffset As Double = configuration.HorizontalOffset
            Dim horizontalAirfoil As AirfoilConfiguration = configuration.HorizontalStabilizer.Airfoil

            Dim vertRootChord As Double = configuration.VerticalStabilizer.RootChord
            Dim vertTipChord As Double = configuration.VerticalStabilizer.TipChord
            Dim vertSpan As Double = configuration.VerticalStabilizer.Span
            Dim vertRibCount As Integer = configuration.VerticalStabilizer.RibCount
            Dim verticalAirfoil As AirfoilConfiguration = configuration.VerticalStabilizer.Airfoil

            Dim vertRootOffset As Double = configuration.VerticalRootOffset
            Dim trailingEdgeX As Double = configuration.VerticalTrailingEdgeX
            Dim vertTipOffset As Double = configuration.VerticalTipOffset

            Dim rudderClearanceZ As Double = configuration.RudderClearance

            ReportTailStep(activeProgressReporter, "CATIA setup", "Connecting to CATIA and reading the active part.", 2, totalSteps)
            Dim catiaApp As Application
            Try
                catiaApp = CType(System.Runtime.InteropServices.Marshal.GetActiveObject("CATIA.Application"), Application)
            Catch ex As Exception
                ReportTailFailed(activeProgressReporter, "Could not connect to CATIA. Ensure CATIA is running.")
                MessageBox.Show("Could not connect to CATIA. Ensure CATIA is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Exit Sub
            End Try

            If catiaApp.Documents.Count = 0 OrElse TypeName(catiaApp.ActiveDocument) <> "PartDocument" Then
                ReportTailFailed(activeProgressReporter, "Please open a CATIA Part Document.")
                MessageBox.Show("Please open a CATIA Part Document.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If

            Dim partDoc As PartDocument = CType(catiaApp.ActiveDocument, PartDocument)
            Dim activePart As Part = partDoc.Part
            Dim originElements As OriginElements = activePart.OriginElements
            Dim hybridShapeFactory As HybridShapeFactory = CType(activePart.HybridShapeFactory, HybridShapeFactory)
            Dim shapeFactory As ShapeFactory = CType(activePart.ShapeFactory, ShapeFactory)
            displayRefreshScope = SuspendCatiaDisplayRefresh(catiaApp)

            ReportTailStep(activeProgressReporter, "CATIA setup", "Preparing tail geometry sets.", 3, totalSteps)
            Dim geoSets As HybridBodies = activePart.HybridBodies
            Dim planeSet As HybridBody

            Try
                planeSet = geoSets.Item("Rib_Reference_Planes")
            Catch ex As Exception
                planeSet = geoSets.Add()
                planeSet.Name = "Rib_Reference_Planes"
            End Try

            Dim skinSet As HybridBody = geoSets.Add()
            skinSet.Name = "Aerodynamic_Skins"

            Dim horizMainSketches As New List(Of Sketch)
            Dim vertMainSketches As New List(Of Sketch)

            ReportTailStep(activeProgressReporter, "Horizontal tail", "Creating horizontal tail ribs and aero profiles.", 4, totalSteps)
            Dim zxPlaneRef As Reference = activePart.CreateReferenceFromObject(originElements.PlaneZX)
            Dim horizSpacing As Double = (horizHalfSpan * 2) / (horizRibCount - 1)

            For i As Integer = 0 To horizRibCount - 1
                Dim currentY As Double = -horizHalfSpan + (i * horizSpacing)
                Dim planeOffset As HybridShapePlaneOffset

                If currentY >= 0 Then
                    planeOffset = hybridShapeFactory.AddNewPlaneOffset(zxPlaneRef, currentY, False)
                Else
                    planeOffset = hybridShapeFactory.AddNewPlaneOffset(zxPlaneRef, Math.Abs(currentY), True)
                End If

                planeSet.AppendHybridShape(planeOffset)
                activePart.UpdateObject(planeOffset)
                Dim localPlaneRef As Reference = activePart.CreateReferenceFromObject(planeOffset)

                Dim mainPts = TailAirfoilProfileGenerator.GeneratePartialNaca(horizChord, horizontalAirfoil, resolution, horizOffset, 0.0, 0.72)

                Dim isEdgeRib As Boolean = (i = 0 OrElse i = horizRibCount - 1)
                Dim structSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Horiz_StructRib_" & i, "Horizontal", tailLighteningCutoutsEnabled AndAlso Not isEdgeRib, horizOffset, horizChord, horizontalAirfoil)

                activePart.InWorkObject = activePart.MainBody
                Dim pad As Pad = shapeFactory.AddNewPad(structSketch, ribThickness)
                pad.Name = "Horiz_Rib_" & i

                If i = horizRibCount - 1 Then
                    pad.FirstLimit.Dimension.Value = 0.0
                    pad.SecondLimit.Dimension.Value = ribThickness
                End If
                activePart.UpdateObject(pad)

                Dim aeroSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Horiz_AeroProfile_" & i, "Horizontal", False, 0, 0)
                horizMainSketches.Add(aeroSketch)
            Next

            ReportTailStep(activeProgressReporter, "Horizontal tail", "Creating horizontal tail main and rear spars.", 5, totalSteps)
            Dim hMainX As Double = horizOffset + (horizChord * TailMainSparChordFraction)
            Dim hMainCamberY As Double = GetAirfoilMeanCamberY(horizChord, horizontalAirfoil, TailMainSparChordFraction)
            Dim hMainSparSk = DrawCylinderSketch(activePart, zxPlaneRef, hMainX, 0.0, hMainCamberY, mainSparRadius, "Horiz_Main_Spar_Cyl", "Horizontal")
            activePart.InWorkObject = activePart.MainBody
            Dim hMainSparPad As Pad = shapeFactory.AddNewPad(hMainSparSk, horizHalfSpan)
            hMainSparPad.SecondLimit.Dimension.Value = horizHalfSpan
            hMainSparPad.Name = "Horizontal_Main_Spar"
            activePart.UpdateObject(hMainSparPad)

            Dim hRearSparPts = TailAirfoilProfileGenerator.GeneratePartialNaca(horizChord, horizontalAirfoil, resolution, horizOffset, 0.72, 0.75)
            Dim hRearSparSk = DrawGenericSketch(activePart, zxPlaneRef, hRearSparPts, "Horiz_Rear_Spar_Sketch", "Horizontal")
            activePart.InWorkObject = activePart.MainBody
            Dim hRearSparPad As Pad = shapeFactory.AddNewPad(hRearSparSk, horizHalfSpan)
            hRearSparPad.SecondLimit.Dimension.Value = horizHalfSpan
            hRearSparPad.Name = "Horizontal_Rear_Spar"
            activePart.UpdateObject(hRearSparPad)

            ReportTailStep(activeProgressReporter, "Vertical tail", "Creating vertical tail ribs and aero profiles.", 6, totalSteps)
            Dim xyPlaneRef As Reference = activePart.CreateReferenceFromObject(originElements.PlaneXY)
            Dim vertSpacing As Double = vertSpan / (vertRibCount - 1)

            For i As Integer = 0 To vertRibCount - 1
                Dim currentZ As Double = i * vertSpacing
                Dim t As Double = currentZ / vertSpan
                Dim localChord As Double = vertRootChord + t * (vertTipChord - vertRootChord)
                Dim localOffset As Double = vertRootOffset + t * (vertTipOffset - vertRootOffset)

                Dim planeOffset As HybridShapePlaneOffset = hybridShapeFactory.AddNewPlaneOffset(xyPlaneRef, currentZ, False)
                planeSet.AppendHybridShape(planeOffset)
                activePart.UpdateObject(planeOffset)
                Dim localPlaneRef As Reference = activePart.CreateReferenceFromObject(planeOffset)

                Dim mainPts = TailAirfoilProfileGenerator.GeneratePartialNaca(localChord, verticalAirfoil, resolution, localOffset, 0.0, 0.72)

                Dim isEdgeRib As Boolean = (i = 0 OrElse i = vertRibCount - 1)
                Dim structSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Vert_StructRib_" & i, "Vertical", tailLighteningCutoutsEnabled AndAlso Not isEdgeRib, localOffset, localChord, verticalAirfoil)

                activePart.InWorkObject = activePart.MainBody
                Dim pad As Pad = shapeFactory.AddNewPad(structSketch, ribThickness)
                pad.Name = "Vert_Rib_" & i

                If i = vertRibCount - 1 Then
                    pad.FirstLimit.Dimension.Value = 0.0
                    pad.SecondLimit.Dimension.Value = ribThickness
                End If
                activePart.UpdateObject(pad)

                Dim aeroSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Vert_AeroProfile_" & i, "Vertical", False, 0, 0)
                vertMainSketches.Add(aeroSketch)
            Next

            Dim vertTipPlane As HybridShapePlaneOffset = hybridShapeFactory.AddNewPlaneOffset(xyPlaneRef, vertSpan, False)
            planeSet.AppendHybridShape(vertTipPlane)
            activePart.UpdateObject(vertTipPlane)
            Dim vertTipPlaneRef As Reference = activePart.CreateReferenceFromObject(vertTipPlane)

            ReportTailStep(activeProgressReporter, "Vertical tail", "Creating vertical tail main and rear spars.", 7, totalSteps)
            Dim vMainRootX As Double = vertRootOffset + (vertRootChord * TailMainSparChordFraction)
            Dim vMainTipX As Double = vertTipOffset + (vertTipChord * TailMainSparChordFraction)
            Dim vMainRootCamberY As Double = GetAirfoilMeanCamberY(vertRootChord, verticalAirfoil, TailMainSparChordFraction)
            Dim vMainTipCamberY As Double = GetAirfoilMeanCamberY(vertTipChord, verticalAirfoil, TailMainSparChordFraction)
            Dim vMainSparRootSk = DrawCylinderSketch(activePart, xyPlaneRef, vMainRootX, vMainRootCamberY, 0.0, mainSparRadius, "Vert_Main_Spar_Root", "Vertical")
            Dim vMainSparTipSk = DrawCylinderSketch(activePart, vertTipPlaneRef, vMainTipX, vMainTipCamberY, vertSpan, mainSparRadius, "Vert_Main_Spar_Tip", "Vertical")
            CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vMainSparRootSk, vMainSparTipSk, "Vertical_Main_Spar")

            Dim vRearSparRootPts = TailAirfoilProfileGenerator.GeneratePartialNaca(vertRootChord, verticalAirfoil, resolution, vertRootOffset, 0.72, 0.75)
            Dim vRearSparTipPts = TailAirfoilProfileGenerator.GeneratePartialNaca(vertTipChord, verticalAirfoil, resolution, vertTipOffset, 0.72, 0.75)
            Dim vRearSparRootSk = DrawGenericSketch(activePart, xyPlaneRef, vRearSparRootPts, "Vert_Rear_Spar_Root", "Vertical")
            Dim vRearSparTipSk = DrawGenericSketch(activePart, vertTipPlaneRef, vRearSparTipPts, "Vert_Rear_Spar_Tip", "Vertical")
            CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vRearSparRootSk, vRearSparTipSk, "Vertical_Rear_Spar")

            ReportTailStep(activeProgressReporter, "Control surfaces", "Creating rudder clearance, rudder, and elevator geometry.", 8, totalSteps)
            Dim tRudder As Double = rudderClearanceZ / vertSpan
            Dim vRudLocalRootChord As Double = vertRootChord + tRudder * (vertTipChord - vertRootChord)
            Dim vRudLocalRootOffset As Double = vertRootOffset + tRudder * (vertTipOffset - vertRootOffset)

            Dim rudderClearancePlane As HybridShapePlaneOffset = hybridShapeFactory.AddNewPlaneOffset(xyPlaneRef, rudderClearanceZ, False)
            planeSet.AppendHybridShape(rudderClearancePlane)
            activePart.UpdateObject(rudderClearancePlane)
            Dim rudderClearancePlaneRef As Reference = activePart.CreateReferenceFromObject(rudderClearancePlane)

            Dim vRudRootPts = TailAirfoilProfileGenerator.GeneratePartialNaca(vRudLocalRootChord, verticalAirfoil, resolution, vRudLocalRootOffset, 0.77, 1.0)
            Dim vRudTipPts = TailAirfoilProfileGenerator.GeneratePartialNaca(vertTipChord, verticalAirfoil, resolution, vertTipOffset, 0.77, 1.0)

            Dim vRudRootSketch = DrawRibSketch(activePart, rudderClearancePlaneRef, vRudRootPts, "Vert_Rudder_Root", "Vertical", False, 0, 0)
            Dim vRudTipSketch = DrawRibSketch(activePart, vertTipPlaneRef, vRudTipPts, "Vert_Rudder_Tip", "Vertical", False, 0, 0)
            CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vRudRootSketch, vRudTipSketch, "Vertical_Rudder")

            ObliterateRudderTriangle(activePart, zxPlaneRef, tailDistanceOffset, vertRootChord)

            Dim hElevPts = TailAirfoilProfileGenerator.GeneratePartialNaca(horizChord, horizontalAirfoil, resolution, horizOffset, 0.77, 1.0)
            Dim hElevSketch = DrawRibSketch(activePart, zxPlaneRef, hElevPts, "Horizontal_Elevator_Sketch", "Horizontal", False, 0, 0)
            activePart.InWorkObject = activePart.MainBody
            Dim hElevPad As Pad = shapeFactory.AddNewPad(hElevSketch, horizHalfSpan)
            hElevPad.SecondLimit.Dimension.Value = horizHalfSpan
            hElevPad.Name = "Horizontal_Elevator"
            activePart.UpdateObject(hElevPad)

            ReportTailStep(activeProgressReporter, "Skins", "Wrapping horizontal and vertical tail skins.", 9, totalSteps)
            WrapSkin(hybridShapeFactory, skinSet, activePart, horizMainSketches, "Horizontal_Main_Skin")
            WrapSkin(hybridShapeFactory, skinSet, activePart, vertMainSketches, "Vertical_Main_Skin")

            ReportTailStep(activeProgressReporter, "Final update", "Updating CATIA part.", 10, totalSteps)
            HideTailConstructionGeometry(partDoc,
                                         activePart,
                                         planeSet,
                                         skinSet)
            activePart.Update()
            If displayRefreshScope IsNot Nothing Then
                displayRefreshScope.Dispose()
                displayRefreshScope = Nothing
            End If

            ReportTailCompleted(activeProgressReporter)
        Catch ex As Exception
            ReportTailFailed(activeProgressReporter, ex)
            Throw
        Finally
            If displayRefreshScope IsNot Nothing Then
                displayRefreshScope.Dispose()
            End If
        End Try
    End Sub

End Class
