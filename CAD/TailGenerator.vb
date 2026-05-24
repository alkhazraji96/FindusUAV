Imports INFITF
Imports MECMOD
Imports PARTITF
Imports HybridShapeTypeLib
Imports KnowledgewareTypeLib
Imports System.Runtime.InteropServices

Public Class TailGenerator
    Private Const TailOperationName As String = "Tail generation"
    Private Const TailMainSparChordFraction As Double = 0.25
    Private Const ForwardLighteningCutoutChordFraction As Double = 0.35
    Private Const MiddleLighteningCutoutChordFraction As Double = 0.48
    Private Const AftLighteningCutoutChordFraction As Double = 0.6

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

    ' =====================================================
    ' MAIN RUN FUNCTION (Now Shared)
    ' =====================================================
    Public Shared Sub Run()
        Run(TailConfiguration.CreateDefault())
    End Sub

    Friend Shared Sub Run(ByVal configuration As TailConfiguration,
                          Optional ByVal progressReporter As IGenerationProgressReporter = Nothing)
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Const totalSteps As Integer = 10

        ReportTailStarting(activeProgressReporter)

        Try
        ReportTailStep(activeProgressReporter, "Validation", "Validating tail configuration.", 1, totalSteps)
        Dim validationResult As ConfigurationValidationResult =
            TailConfigurationValidator.Validate(configuration)
        validationResult.ThrowIfInvalid()

        Dim resolution As Integer = configuration.PointCountPerSurface
        Dim ribThickness As Double = configuration.RibThickness
        Dim mainSparRadius As Double = configuration.MainSpar.MainSparRadius

        ' =====================================================
        ' AIRCRAFT PARAMETERS
        ' =====================================================
        Dim tailDistanceOffset As Double = configuration.DistanceOffset
        Dim horizChord As Double = configuration.HorizontalStabilizer.Chord

        ' Horizontal Stabilizer
        Dim horizHalfSpan As Double = configuration.HorizontalStabilizer.HalfSpan
        Dim horizRibCount As Integer = configuration.HorizontalStabilizer.RibCount
        Dim horizOffset As Double = configuration.HorizontalOffset
        Dim horizontalAirfoil As AirfoilConfiguration = configuration.HorizontalStabilizer.Airfoil

        ' Vertical Stabilizer (Tapered)
        Dim vertRootChord As Double = configuration.VerticalStabilizer.RootChord
        Dim vertTipChord As Double = configuration.VerticalStabilizer.TipChord
        Dim vertSpan As Double = configuration.VerticalStabilizer.Span
        Dim vertRibCount As Integer = configuration.VerticalStabilizer.RibCount
        Dim verticalAirfoil As AirfoilConfiguration = configuration.VerticalStabilizer.Airfoil

        ' Sweep Logic
        Dim vertRootOffset As Double = configuration.VerticalRootOffset
        Dim trailingEdgeX As Double = configuration.VerticalTrailingEdgeX
        Dim vertTipOffset As Double = configuration.VerticalTipOffset

        ' Rudder Clearance (Raises the bottom of the rudder to clear the elevator)
        Dim rudderClearanceZ As Double = configuration.RudderClearance

        ' =====================================================
        ' CONNECT TO CATIA
        ' =====================================================
        ReportTailStep(activeProgressReporter, "CATIA setup", "Connecting to CATIA and reading the active part.", 2, totalSteps)
        Dim catiaApp As Application
        Try
            ' Explicitly defining System.Runtime.InteropServices to prevent Marshal errors
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

        ' =====================================================
        ' GEOMETRY SETS
        ' =====================================================
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

        ' =====================================================
        ' HORIZONTAL TAIL
        ' =====================================================
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

            ' Calling Shared method directly without instantiation
            Dim mainPts = AirfoilGenerator.GeneratePartialNACA(horizChord, horizontalAirfoil, resolution, horizOffset, 0.0, 0.72)

            ' 1. STRUCTURAL RIB
            Dim isEdgeRib As Boolean = (i = 0 OrElse i = horizRibCount - 1)
            Dim structSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Horiz_StructRib_" & i, "Horizontal", Not isEdgeRib, horizOffset, horizChord, horizontalAirfoil)

            activePart.InWorkObject = activePart.MainBody
            Dim pad As Pad = shapeFactory.AddNewPad(structSketch, ribThickness)
            pad.Name = "Horiz_Rib_" & i

            ' -> THE SYMMETRY FIX <-
            If i = horizRibCount - 1 Then
                pad.FirstLimit.Dimension.Value = 0.0
                pad.SecondLimit.Dimension.Value = ribThickness
            End If
            activePart.UpdateObject(pad)

            ' 2. AERODYNAMIC SKETCH (No Holes) -> Sent to Skin Lofter
            Dim aeroSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Horiz_AeroProfile_" & i, "Horizontal", False, 0, 0)
            horizMainSketches.Add(aeroSketch)
        Next

        ' A. MAIN CYLINDRICAL SPAR (Horizontal)
        ReportTailStep(activeProgressReporter, "Horizontal tail", "Creating horizontal tail main and rear spars.", 5, totalSteps)
        Dim hMainX As Double = horizOffset + (horizChord * TailMainSparChordFraction)
        Dim hMainCamberY As Double = GetAirfoilMeanCamberY(horizChord, horizontalAirfoil, TailMainSparChordFraction)
        Dim hMainSparSk = DrawCylinderSketch(activePart, zxPlaneRef, hMainX, 0.0, hMainCamberY, mainSparRadius, "Horiz_Main_Spar_Cyl", "Horizontal")
        activePart.InWorkObject = activePart.MainBody
        Dim hMainSparPad As Pad = shapeFactory.AddNewPad(hMainSparSk, horizHalfSpan)
        hMainSparPad.SecondLimit.Dimension.Value = horizHalfSpan
        hMainSparPad.Name = "Horizontal_Main_Spar"
        activePart.UpdateObject(hMainSparPad)

        ' B. REAR HINGE SPAR (Horizontal)
        Dim hRearSparPts = AirfoilGenerator.GeneratePartialNACA(horizChord, horizontalAirfoil, resolution, horizOffset, 0.72, 0.75)
        Dim hRearSparSk = DrawGenericSketch(activePart, zxPlaneRef, hRearSparPts, "Horiz_Rear_Spar_Sketch", "Horizontal")
        activePart.InWorkObject = activePart.MainBody
        Dim hRearSparPad As Pad = shapeFactory.AddNewPad(hRearSparSk, horizHalfSpan)
        hRearSparPad.SecondLimit.Dimension.Value = horizHalfSpan
        hRearSparPad.Name = "Horizontal_Rear_Spar"
        activePart.UpdateObject(hRearSparPad)

        ' =====================================================
        ' VERTICAL TAIL
        ' =====================================================
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

            Dim mainPts = AirfoilGenerator.GeneratePartialNACA(localChord, verticalAirfoil, resolution, localOffset, 0.0, 0.72)

            ' 1. STRUCTURAL RIB 
            Dim isEdgeRib As Boolean = (i = 0 OrElse i = vertRibCount - 1)
            Dim structSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Vert_StructRib_" & i, "Vertical", Not isEdgeRib, localOffset, localChord, verticalAirfoil)

            activePart.InWorkObject = activePart.MainBody
            Dim pad As Pad = shapeFactory.AddNewPad(structSketch, ribThickness)
            pad.Name = "Vert_Rib_" & i

            If i = vertRibCount - 1 Then
                pad.FirstLimit.Dimension.Value = 0.0
                pad.SecondLimit.Dimension.Value = ribThickness
            End If
            activePart.UpdateObject(pad)

            ' 2. AERODYNAMIC SKETCH
            Dim aeroSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Vert_AeroProfile_" & i, "Vertical", False, 0, 0)
            vertMainSketches.Add(aeroSketch)
        Next

        Dim vertTipPlane As HybridShapePlaneOffset = hybridShapeFactory.AddNewPlaneOffset(xyPlaneRef, vertSpan, False)
        planeSet.AppendHybridShape(vertTipPlane)
        activePart.UpdateObject(vertTipPlane)
        Dim vertTipPlaneRef As Reference = activePart.CreateReferenceFromObject(vertTipPlane)

        ' A. MAIN CYLINDRICAL SPAR (Vertical - Tapered Loft)
        ReportTailStep(activeProgressReporter, "Vertical tail", "Creating vertical tail main and rear spars.", 7, totalSteps)
        Dim vMainRootX As Double = vertRootOffset + (vertRootChord * TailMainSparChordFraction)
        Dim vMainTipX As Double = vertTipOffset + (vertTipChord * TailMainSparChordFraction)
        Dim vMainRootCamberY As Double = GetAirfoilMeanCamberY(vertRootChord, verticalAirfoil, TailMainSparChordFraction)
        Dim vMainTipCamberY As Double = GetAirfoilMeanCamberY(vertTipChord, verticalAirfoil, TailMainSparChordFraction)
        Dim vMainSparRootSk = DrawCylinderSketch(activePart, xyPlaneRef, vMainRootX, vMainRootCamberY, 0.0, mainSparRadius, "Vert_Main_Spar_Root", "Vertical")
        Dim vMainSparTipSk = DrawCylinderSketch(activePart, vertTipPlaneRef, vMainTipX, vMainTipCamberY, vertSpan, mainSparRadius, "Vert_Main_Spar_Tip", "Vertical")
        CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vMainSparRootSk, vMainSparTipSk, "Vertical_Main_Spar")

        ' B. REAR HINGE SPAR (Vertical)
        Dim vRearSparRootPts = AirfoilGenerator.GeneratePartialNACA(vertRootChord, verticalAirfoil, resolution, vertRootOffset, 0.72, 0.75)
        Dim vRearSparTipPts = AirfoilGenerator.GeneratePartialNACA(vertTipChord, verticalAirfoil, resolution, vertTipOffset, 0.72, 0.75)
        Dim vRearSparRootSk = DrawGenericSketch(activePart, xyPlaneRef, vRearSparRootPts, "Vert_Rear_Spar_Root", "Vertical")
        Dim vRearSparTipSk = DrawGenericSketch(activePart, vertTipPlaneRef, vRearSparTipPts, "Vert_Rear_Spar_Tip", "Vertical")
        CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vRearSparRootSk, vRearSparTipSk, "Vertical_Rear_Spar")

        ' =====================================================
        ' C. VERTICAL RUDDER (MODIFIED FOR PITCH CLEARANCE)
        ' =====================================================
        ReportTailStep(activeProgressReporter, "Control surfaces", "Creating rudder clearance, rudder, and elevator geometry.", 8, totalSteps)
        Dim tRudder As Double = rudderClearanceZ / vertSpan
        Dim vRudLocalRootChord As Double = vertRootChord + tRudder * (vertTipChord - vertRootChord)
        Dim vRudLocalRootOffset As Double = vertRootOffset + tRudder * (vertTipOffset - vertRootOffset)

        Dim rudderClearancePlane As HybridShapePlaneOffset = hybridShapeFactory.AddNewPlaneOffset(xyPlaneRef, rudderClearanceZ, False)
        planeSet.AppendHybridShape(rudderClearancePlane)
        activePart.UpdateObject(rudderClearancePlane)
        Dim rudderClearancePlaneRef As Reference = activePart.CreateReferenceFromObject(rudderClearancePlane)

        Dim vRudRootPts = AirfoilGenerator.GeneratePartialNACA(vRudLocalRootChord, verticalAirfoil, resolution, vRudLocalRootOffset, 0.77, 1.0)
        Dim vRudTipPts = AirfoilGenerator.GeneratePartialNACA(vertTipChord, verticalAirfoil, resolution, vertTipOffset, 0.77, 1.0)

        Dim vRudRootSketch = DrawRibSketch(activePart, rudderClearancePlaneRef, vRudRootPts, "Vert_Rudder_Root", "Vertical", False, 0, 0)
        Dim vRudTipSketch = DrawRibSketch(activePart, vertTipPlaneRef, vRudTipPts, "Vert_Rudder_Tip", "Vertical", False, 0, 0)
        CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vRudRootSketch, vRudTipSketch, "Vertical_Rudder")

        ' =====================================================
        ' 3. DESTROY TRIANGLE COMPLETELY (RUDDER CLEARANCE)
        ' =====================================================
        ObliterateRudderTriangle(activePart, zxPlaneRef, tailDistanceOffset, vertRootChord)

        ' =====================================================
        ' 4. HORIZONTAL ELEVATOR GENERATION
        ' =====================================================
        Dim hElevPts = AirfoilGenerator.GeneratePartialNACA(horizChord, horizontalAirfoil, resolution, horizOffset, 0.77, 1.0)
        Dim hElevSketch = DrawRibSketch(activePart, zxPlaneRef, hElevPts, "Horizontal_Elevator_Sketch", "Horizontal", False, 0, 0)
        activePart.InWorkObject = activePart.MainBody
        Dim hElevPad As Pad = shapeFactory.AddNewPad(hElevSketch, horizHalfSpan)
        hElevPad.SecondLimit.Dimension.Value = horizHalfSpan
        hElevPad.Name = "Horizontal_Elevator"
        activePart.UpdateObject(hElevPad)

        ' =====================================================
        ' WRAP SKINS
        ' =====================================================
        ReportTailStep(activeProgressReporter, "Skins", "Wrapping horizontal and vertical tail skins.", 9, totalSteps)
        WrapSkin(hybridShapeFactory, skinSet, activePart, horizMainSketches, "Horizontal_Main_Skin")
        WrapSkin(hybridShapeFactory, skinSet, activePart, vertMainSketches, "Vertical_Main_Skin")

        ReportTailStep(activeProgressReporter, "Final update", "Updating CATIA part.", 10, totalSteps)
        HideTailConstructionGeometry(partDoc,
                                     activePart,
                                     planeSet,
                                     skinSet)
        activePart.Update()
        ReportTailCompleted(activeProgressReporter)
        MessageBox.Show("Generation Complete! Rudder clearance added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            ReportTailFailed(activeProgressReporter, ex)
            Throw
        End Try
    End Sub

    ' =====================================================
    ' ALL HELPER METHODS MUST NOW BE SHARED AS WELL
    ' =====================================================
    Private Shared Sub HideTailConstructionGeometry(ByVal partDocument As PartDocument,
                                                    ByVal activePart As Part,
                                                    ByVal planeSet As HybridBody,
                                                    ByVal skinSet As HybridBody)
        TryHideObject(partDocument, planeSet)
        TryHideSketchesInBodies(partDocument, activePart)
        TryHideHybridShapesByNameEnding(partDocument, skinSet, "_Surface")
    End Sub

    Private Shared Sub ObliterateRudderTriangle(activePart As Part, planeRef As Reference, tailOffset As Double, chord As Double)
        Dim sketch As Sketch = activePart.MainBody.Sketches.Add(planeRef)
        sketch.Name = "Triangle_Cut_Sketch"
        Dim factory2D As Factory2D = sketch.OpenEdition()

        Dim x1 As Double = tailOffset + (chord * 0.76)
        Dim z1 As Double = -10.0
        Dim x2 As Double = tailOffset + chord + 50
        Dim z2 As Double = -10.0
        Dim x3 As Double = tailOffset + chord + 50
        Dim z3 As Double = 80.0

        Dim pt1 = factory2D.CreatePoint(z1, x1)
        Dim pt2 = factory2D.CreatePoint(z2, x2)
        Dim pt3 = factory2D.CreatePoint(z3, x3)

        Dim l1 = factory2D.CreateLine(z1, x1, z2, x2) : l1.StartPoint = pt1 : l1.EndPoint = pt2
        Dim l2 = factory2D.CreateLine(z2, x2, z3, x3) : l2.StartPoint = pt2 : l2.EndPoint = pt3
        Dim l3 = factory2D.CreateLine(z3, x3, z1, x1) : l3.StartPoint = pt3 : l3.EndPoint = pt1

        sketch.CloseEdition()
        activePart.UpdateObject(sketch)

        activePart.InWorkObject = activePart.MainBody
        Dim shapeFactory As ShapeFactory = CType(activePart.ShapeFactory, ShapeFactory)
        Dim pocket As Pocket = shapeFactory.AddNewPocket(sketch, 1000.0)
        pocket.FirstLimit.Dimension.Value = 1000.0
        pocket.SecondLimit.Dimension.Value = 1000.0
        pocket.Name = "Triangle_Clearance_Pocket"
        activePart.UpdateObject(pocket)
    End Sub

    Private Shared Sub WrapSkin(factory As HybridShapeFactory, skinSet As HybridBody, activePart As Part, sketches As List(Of Sketch), name As String)
        Dim skinLoft As HybridShapeLoft = factory.AddNewLoft()
        skinLoft.SectionCoupling = 1
        Dim nullRef As Reference = CType(Nothing, Reference)
        For Each sk As Sketch In sketches
            skinLoft.AddSectionToLoft(activePart.CreateReferenceFromObject(sk), 1, nullRef)
        Next
        skinLoft.Name = name
        skinSet.AppendHybridShape(skinLoft)
        activePart.UpdateObject(skinLoft)
    End Sub

    Private Shared Function DrawCylinderSketch(activePart As Part, planeRef As Reference, aircraftX As Double, aircraftY As Double, aircraftZ As Double, radius As Double, name As String, tailType As String) As Sketch
        Dim sketch As Sketch = activePart.MainBody.Sketches.Add(planeRef)
        sketch.Name = name
        Dim factory2D As Factory2D = sketch.OpenEdition()

        If tailType = "Horizontal" Then
            factory2D.CreateClosedCircle(aircraftZ, aircraftX, radius)
        Else
            factory2D.CreateClosedCircle(aircraftX, aircraftY, radius)
        End If

        sketch.CloseEdition()
        activePart.UpdateObject(sketch)
        Return sketch
    End Function

    Private Shared Function DrawGenericSketch(activePart As Part, planeRef As Reference, points As List(Of Point3D), name As String, tailType As String) As Sketch
        Dim sketch As Sketch = activePart.MainBody.Sketches.Add(planeRef)
        sketch.Name = name

        Dim factory2D As Factory2D = sketch.OpenEdition()
        Dim lastIdx As Integer = points.Count - 1
        Dim controlPoints(lastIdx) As Object

        For i As Integer = 0 To lastIdx
            If tailType = "Horizontal" Then
                controlPoints(i) = factory2D.CreatePoint(points(i).Y, points(i).X)
            Else
                controlPoints(i) = factory2D.CreatePoint(points(i).X, points(i).Y)
            End If
        Next

        Dim spline2D As Spline2D = factory2D.CreateSpline(controlPoints)
        Dim teLine As Line2D = If(tailType = "Horizontal", factory2D.CreateLine(points(lastIdx).Y, points(lastIdx).X, points(0).Y, points(0).X), factory2D.CreateLine(points(lastIdx).X, points(lastIdx).Y, points(0).X, points(0).Y))

        teLine.StartPoint = CType(controlPoints(lastIdx), Point2D)
        teLine.EndPoint = CType(controlPoints(0), Point2D)

        sketch.CloseEdition()
        activePart.UpdateObject(sketch)
        Return sketch
    End Function

    Private Shared Function DrawRibSketch(activePart As Part, planeRef As Reference, points As List(Of Point3D), name As String, tailType As String, drawHoles As Boolean, offset As Double, chord As Double, Optional airfoil As AirfoilConfiguration = Nothing) As Sketch
        Dim sketch As Sketch = activePart.MainBody.Sketches.Add(planeRef)
        sketch.Name = name
        Dim factory2D As Factory2D = sketch.OpenEdition()

        Dim lastIdx As Integer = points.Count - 1
        Dim controlPoints(lastIdx) As Object
        For i As Integer = 0 To lastIdx
            If tailType = "Horizontal" Then
                controlPoints(i) = factory2D.CreatePoint(points(i).Y, points(i).X)
            Else
                controlPoints(i) = factory2D.CreatePoint(points(i).X, points(i).Y)
            End If
        Next
        Dim spline2D As Spline2D = factory2D.CreateSpline(controlPoints)

        Dim teLine As Line2D
        If tailType = "Horizontal" Then
            teLine = factory2D.CreateLine(points(lastIdx).Y, points(lastIdx).X, points(0).Y, points(0).X)
        Else
            teLine = factory2D.CreateLine(points(lastIdx).X, points(lastIdx).Y, points(0).X, points(0).Y)
        End If
        teLine.StartPoint = CType(controlPoints(lastIdx), Point2D)
        teLine.EndPoint = CType(controlPoints(0), Point2D)

        If drawHoles Then
            Dim r1 As Double = chord * 0.03
            Dim r2 As Double = chord * 0.035
            Dim r3 As Double = chord * 0.025

            Dim c1X As Double = offset + (chord * ForwardLighteningCutoutChordFraction)
            Dim c2X As Double = offset + (chord * MiddleLighteningCutoutChordFraction)
            Dim c3X As Double = offset + (chord * AftLighteningCutoutChordFraction)

            Dim c1Y As Double = GetAirfoilMeanCamberY(chord, airfoil, ForwardLighteningCutoutChordFraction)
            Dim c2Y As Double = GetAirfoilMeanCamberY(chord, airfoil, MiddleLighteningCutoutChordFraction)
            Dim c3Y As Double = GetAirfoilMeanCamberY(chord, airfoil, AftLighteningCutoutChordFraction)

            If tailType = "Horizontal" Then
                factory2D.CreateClosedCircle(c1Y, c1X, r1)
                factory2D.CreateClosedCircle(c2Y, c2X, r2)
                factory2D.CreateClosedCircle(c3Y, c3X, r3)
            Else
                factory2D.CreateClosedCircle(c1X, c1Y, r1)
                factory2D.CreateClosedCircle(c2X, c2Y, r2)
                factory2D.CreateClosedCircle(c3X, c3Y, r3)
            End If
        End If

        sketch.CloseEdition()
        activePart.UpdateObject(sketch)
        Return sketch
    End Function

    Private Shared Function GetAirfoilMeanCamberY(ByVal chord As Double,
                                                  ByVal airfoil As AirfoilConfiguration,
                                                  ByVal chordFraction As Double) As Double
        If airfoil Is Nothing OrElse
            chord <= 0.0 OrElse
            chordFraction <= 0.0 OrElse
            chordFraction >= 1.0 OrElse
            airfoil.MaximumCamber <= 0.0 OrElse
            airfoil.MaximumCamberPosition <= 0.0 Then
            Return 0.0
        End If

        Dim normalizedCamber As Double = 0.0

        If chordFraction <= airfoil.MaximumCamberPosition Then
            normalizedCamber = (airfoil.MaximumCamber / Math.Pow(airfoil.MaximumCamberPosition, 2.0)) *
                ((2.0 * airfoil.MaximumCamberPosition * chordFraction) - Math.Pow(chordFraction, 2.0))
        Else
            Dim aftCamberLength As Double = 1.0 - airfoil.MaximumCamberPosition

            If aftCamberLength <= 0.0 Then
                Return 0.0
            End If

            normalizedCamber = (airfoil.MaximumCamber / Math.Pow(aftCamberLength, 2.0)) *
                ((1.0 - (2.0 * airfoil.MaximumCamberPosition)) +
                 (2.0 * airfoil.MaximumCamberPosition * chordFraction) -
                 Math.Pow(chordFraction, 2.0))
        End If

        Return normalizedCamber * chord
    End Function

    Private Shared Sub CreateSolidLoftSecurely(activePart As Part, factory As HybridShapeFactory, skinSet As HybridBody, rootSketch As Sketch, tipSketch As Sketch, name As String)
        Dim surfLoft As HybridShapeLoft = factory.AddNewLoft()
        surfLoft.SectionCoupling = 1
        Dim nullRef As Reference = CType(Nothing, Reference)

        surfLoft.AddSectionToLoft(activePart.CreateReferenceFromObject(rootSketch), 1, nullRef)
        surfLoft.AddSectionToLoft(activePart.CreateReferenceFromObject(tipSketch), 1, nullRef)
        surfLoft.Name = name & "_Surface"
        skinSet.AppendHybridShape(surfLoft)
        activePart.UpdateObject(surfLoft)

        Dim shapeFactory As ShapeFactory = CType(activePart.ShapeFactory, ShapeFactory)
        activePart.InWorkObject = activePart.MainBody
        Dim closeSurf As CloseSurface = shapeFactory.AddNewCloseSurface(activePart.CreateReferenceFromObject(surfLoft))
        closeSurf.Name = name
        activePart.UpdateObject(closeSurf)
    End Sub

End Class

' =====================================================
' POINT STRUCTURE
' =====================================================
Public Structure Point3D
        Public X As Double
        Public Y As Double
        Public Z As Double
        Public Sub New(x__1 As Double, y__1 As Double, z__1 As Double)
            X = x__1
            Y = y__1
            Z = z__1
        End Sub
    End Structure

    ' =====================================================
    ' AIRFOIL GENERATOR
    ' =====================================================
    Public Class AirfoilGenerator
        Friend Shared Function GeneratePartialNACA(chord As Double,
                                                   airfoil As AirfoilConfiguration,
                                                   numPoints As Integer,
                                                   xOffset As Double,
                                                   startPercent As Double,
                                                   endPercent As Double) As List(Of Point3D)
            If airfoil Is Nothing Then
                Throw New ArgumentNullException("airfoil")
            End If

            Return GeneratePartialNacaProfile(chord,
                                              airfoil.MaximumCamber,
                                              airfoil.MaximumCamberPosition,
                                              airfoil.MaximumThickness,
                                              numPoints,
                                              xOffset,
                                              startPercent,
                                              endPercent)
        End Function

        ' Now also marked as Shared so you don't need to create an instance of AirfoilGenerator
        Public Shared Function GeneratePartialSymmetricNACA(chord As Double, thicknessRatio As Double, numPoints As Integer, xOffset As Double, startPercent As Double, endPercent As Double) As List(Of Point3D)
            Return GeneratePartialNacaProfile(chord,
                                              0.0,
                                              0.0,
                                              thicknessRatio,
                                              numPoints,
                                              xOffset,
                                              startPercent,
                                              endPercent)
        End Function

        Private Shared Function GeneratePartialNacaProfile(chord As Double,
                                                           maximumCamber As Double,
                                                           maximumCamberPosition As Double,
                                                           maximumThickness As Double,
                                                           numPoints As Integer,
                                                           xOffset As Double,
                                                           startPercent As Double,
                                                           endPercent As Double) As List(Of Point3D)
            Dim profilePoints As New List(Of Point3D)
            Dim upperX As New List(Of Double)
            Dim upperY As New List(Of Double)
            Dim lowerX As New List(Of Double)
            Dim lowerY As New List(Of Double)

            For i As Integer = 0 To numPoints - 1
                Dim beta As Double = Math.PI * (i / CDbl(numPoints - 1))
                Dim rawX As Double = 0.5 * (1.0 - Math.Cos(beta))
                Dim x As Double = startPercent + rawX * (endPercent - startPercent)

                Dim upperPoint As Point3D = Nothing
                Dim lowerPoint As Point3D = Nothing
                CalculateNacaSurfacePoints(x,
                                           chord,
                                           xOffset,
                                           maximumCamber,
                                           maximumCamberPosition,
                                           maximumThickness,
                                           upperPoint,
                                           lowerPoint)

                upperX.Add(upperPoint.X)
                upperY.Add(upperPoint.Y)
                lowerX.Add(lowerPoint.X)
                lowerY.Add(lowerPoint.Y)
            Next

            For i As Integer = numPoints - 1 To 0 Step -1
                profilePoints.Add(New Point3D(upperX(i), upperY(i), 0))
            Next

            Dim startIdx As Integer = If(startPercent = 0.0, 1, 0)
            For i As Integer = startIdx To numPoints - 1
                profilePoints.Add(New Point3D(lowerX(i), lowerY(i), 0))
            Next

            Return profilePoints
        End Function

        Private Shared Sub CalculateNacaSurfacePoints(normalizedX As Double,
                                                      chord As Double,
                                                      xOffset As Double,
                                                      maximumCamber As Double,
                                                      maximumCamberPosition As Double,
                                                      maximumThickness As Double,
                                                      ByRef upperPoint As Point3D,
                                                      ByRef lowerPoint As Point3D)
            Dim thickness As Double = 5.0 * maximumThickness *
                ((0.2969 * Math.Sqrt(normalizedX)) -
                 (0.126 * normalizedX) -
                 (0.3516 * Math.Pow(normalizedX, 2)) +
                 (0.2843 * Math.Pow(normalizedX, 3)) -
                 (0.1015 * Math.Pow(normalizedX, 4)))
            Dim camber As Double = 0.0
            Dim camberSlope As Double = 0.0

            If maximumCamber > 0.0 AndAlso maximumCamberPosition > 0.0 Then
                If normalizedX <= maximumCamberPosition Then
                    camber = (maximumCamber / Math.Pow(maximumCamberPosition, 2)) *
                        ((2.0 * maximumCamberPosition * normalizedX) - Math.Pow(normalizedX, 2))
                    camberSlope = ((2.0 * maximumCamber) / Math.Pow(maximumCamberPosition, 2)) *
                        (maximumCamberPosition - normalizedX)
                Else
                    Dim aftCamberLength As Double = 1.0 - maximumCamberPosition

                    If aftCamberLength > 0.0 Then
                        camber = (maximumCamber / Math.Pow(aftCamberLength, 2)) *
                            ((1.0 - (2.0 * maximumCamberPosition)) +
                             (2.0 * maximumCamberPosition * normalizedX) -
                             Math.Pow(normalizedX, 2))
                        camberSlope = ((2.0 * maximumCamber) / Math.Pow(aftCamberLength, 2)) *
                            (maximumCamberPosition - normalizedX)
                    End If
                End If
            End If

            Dim theta As Double = Math.Atan(camberSlope)
            Dim upperX As Double = normalizedX - (thickness * Math.Sin(theta))
            Dim upperY As Double = camber + (thickness * Math.Cos(theta))
            Dim lowerX As Double = normalizedX + (thickness * Math.Sin(theta))
            Dim lowerY As Double = camber - (thickness * Math.Cos(theta))

            upperPoint = New Point3D((upperX * chord) + xOffset, upperY * chord, 0.0)
            lowerPoint = New Point3D((lowerX * chord) + xOffset, lowerY * chord, 0.0)
        End Sub
    End Class
