Imports System.Reflection
Imports System.Runtime.InteropServices
Imports INFITF
Imports MECMOD
Imports PARTITF
Imports HybridShapeTypeLib
Imports System.Math
Imports System.Collections.Generic

Public Class MainForm

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Console.WriteLine("Professional Tail Geometry Generator Ready!")
    End Sub

    Private Sub btnCATIA_Click(sender As Object, e As EventArgs) Handles btnCATIA.Click

        Dim generator As New AirfoilGenerator()

        Dim resolution As Integer = 50
        Dim ribThickness As Double = 2.0
        Dim mainSparRadius As Double = 3.0 ' 6mm overall cylinder diameter

        ' =====================================================
        ' AIRCRAFT PARAMETERS
        ' =====================================================
        Dim tailDistanceOffset As Double = 1500.0
        Dim horizChord As Double = 150.0

        ' Horizontal Stabilizer
        Dim horizHalfSpan As Double = 350.0
        Dim horizRibCount As Integer = 8
        Dim horizOffset As Double = tailDistanceOffset

        ' Vertical Stabilizer (Tapered)
        Dim vertRootChord As Double = horizChord
        Dim vertTipChord As Double = 75.0
        Dim vertSpan As Double = 250.0
        Dim vertRibCount As Integer = 4

        ' Sweep Logic
        Dim vertRootOffset As Double = tailDistanceOffset
        Dim trailingEdgeX As Double = tailDistanceOffset + vertRootChord
        Dim vertTipOffset As Double = trailingEdgeX - vertTipChord

        ' Rudder Clearance (Raises the bottom of the rudder to clear the elevator)
        Dim rudderClearanceZ As Double = 25.0 ' Adjust this value to increase/decrease pitch clearance

        ' =====================================================
        ' CONNECT TO CATIA
        ' =====================================================
        Dim catiaApp As Application
        Try
            catiaApp = CType(Marshal.GetActiveObject("CATIA.Application"), Application)
        Catch ex As Exception
            MessageBox.Show("Could not connect to CATIA.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Exit Sub
        End Try

        If catiaApp.Documents.Count = 0 OrElse TypeName(catiaApp.ActiveDocument) <> "PartDocument" Then
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

            Dim mainPts = generator.GeneratePartialSymmetricNACA(horizChord, 0.12, resolution, horizOffset, 0.0, 0.72)

            ' 1. STRUCTURAL RIB
            Dim isEdgeRib As Boolean = (i = 0 OrElse i = horizRibCount - 1)
            Dim structSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Horiz_StructRib_" & i, "Horizontal", Not isEdgeRib, horizOffset, horizChord)

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
        Dim hMainX As Double = horizOffset + (horizChord * 0.25)
        Dim hMainSparSk = DrawCylinderSketch(activePart, zxPlaneRef, hMainX, 0.0, 0.0, mainSparRadius, "Horiz_Main_Spar_Cyl", "Horizontal")
        activePart.InWorkObject = activePart.MainBody
        Dim hMainSparPad As Pad = shapeFactory.AddNewPad(hMainSparSk, horizHalfSpan)
        hMainSparPad.SecondLimit.Dimension.Value = horizHalfSpan
        hMainSparPad.Name = "Horizontal_Main_Spar"
        activePart.UpdateObject(hMainSparPad)

        ' B. REAR HINGE SPAR (Horizontal)
        Dim hRearSparPts = generator.GeneratePartialSymmetricNACA(horizChord, 0.12, resolution, horizOffset, 0.72, 0.75)
        Dim hRearSparSk = DrawGenericSketch(activePart, zxPlaneRef, hRearSparPts, "Horiz_Rear_Spar_Sketch", "Horizontal")
        activePart.InWorkObject = activePart.MainBody
        Dim hRearSparPad As Pad = shapeFactory.AddNewPad(hRearSparSk, horizHalfSpan)
        hRearSparPad.SecondLimit.Dimension.Value = horizHalfSpan
        hRearSparPad.Name = "Horizontal_Rear_Spar"
        activePart.UpdateObject(hRearSparPad)

        ' =====================================================
        ' VERTICAL TAIL
        ' =====================================================
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

            Dim mainPts = generator.GeneratePartialSymmetricNACA(localChord, 0.12, resolution, localOffset, 0.0, 0.72)

            ' 1. STRUCTURAL RIB 
            Dim isEdgeRib As Boolean = (i = 0 OrElse i = vertRibCount - 1)
            Dim structSketch = DrawRibSketch(activePart, localPlaneRef, mainPts, "Vert_StructRib_" & i, "Vertical", Not isEdgeRib, localOffset, localChord)

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
        Dim vMainRootX As Double = vertRootOffset + (vertRootChord * 0.25)
        Dim vMainTipX As Double = vertTipOffset + (vertTipChord * 0.25)
        Dim vMainSparRootSk = DrawCylinderSketch(activePart, xyPlaneRef, vMainRootX, 0.0, 0.0, mainSparRadius, "Vert_Main_Spar_Root", "Vertical")
        Dim vMainSparTipSk = DrawCylinderSketch(activePart, vertTipPlaneRef, vMainTipX, 0.0, vertSpan, mainSparRadius, "Vert_Main_Spar_Tip", "Vertical")
        CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vMainSparRootSk, vMainSparTipSk, "Vertical_Main_Spar")

        ' B. REAR HINGE SPAR (Vertical)
        Dim vRearSparRootPts = generator.GeneratePartialSymmetricNACA(vertRootChord, 0.12, resolution, vertRootOffset, 0.72, 0.75)
        Dim vRearSparTipPts = generator.GeneratePartialSymmetricNACA(vertTipChord, 0.12, resolution, vertTipOffset, 0.72, 0.75)
        Dim vRearSparRootSk = DrawGenericSketch(activePart, xyPlaneRef, vRearSparRootPts, "Vert_Rear_Spar_Root", "Vertical")
        Dim vRearSparTipSk = DrawGenericSketch(activePart, vertTipPlaneRef, vRearSparTipPts, "Vert_Rear_Spar_Tip", "Vertical")
        CreateSolidLoftSecurely(activePart, hybridShapeFactory, skinSet, vRearSparRootSk, vRearSparTipSk, "Vertical_Rear_Spar")

        ' =====================================================
        ' C. VERTICAL RUDDER (MODIFIED FOR PITCH CLEARANCE)
        ' =====================================================
        ' 1. Calculate local taper/sweep for the raised rudder plane
        Dim tRudder As Double = rudderClearanceZ / vertSpan
        Dim vRudLocalRootChord As Double = vertRootChord + tRudder * (vertTipChord - vertRootChord)
        Dim vRudLocalRootOffset As Double = vertRootOffset + tRudder * (vertTipOffset - vertRootOffset)

        ' 2. Create the new raised reference plane
        Dim rudderClearancePlane As HybridShapePlaneOffset = hybridShapeFactory.AddNewPlaneOffset(xyPlaneRef, rudderClearanceZ, False)
        planeSet.AppendHybridShape(rudderClearancePlane)
        activePart.UpdateObject(rudderClearancePlane)
        Dim rudderClearancePlaneRef As Reference = activePart.CreateReferenceFromObject(rudderClearancePlane)

        ' 3. Generate points mapping to the tapered contour
        Dim vRudRootPts = generator.GeneratePartialSymmetricNACA(vRudLocalRootChord, 0.12, resolution, vRudLocalRootOffset, 0.77, 1.0)
        Dim vRudTipPts = generator.GeneratePartialSymmetricNACA(vertTipChord, 0.12, resolution, vertTipOffset, 0.77, 1.0)

        ' 4. Draw sketches & Loft
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
        Dim hElevPts = generator.GeneratePartialSymmetricNACA(horizChord, 0.12, resolution, horizOffset, 0.77, 1.0)
        Dim hElevSketch = DrawRibSketch(activePart, zxPlaneRef, hElevPts, "Horizontal_Elevator_Sketch", "Horizontal", False, 0, 0)
        activePart.InWorkObject = activePart.MainBody
        Dim hElevPad As Pad = shapeFactory.AddNewPad(hElevSketch, horizHalfSpan)
        hElevPad.SecondLimit.Dimension.Value = horizHalfSpan
        hElevPad.Name = "Horizontal_Elevator"
        activePart.UpdateObject(hElevPad)

        ' =====================================================
        ' WRAP SKINS
        ' =====================================================
        WrapSkin(hybridShapeFactory, skinSet, activePart, horizMainSketches, "Horizontal_Main_Skin")
        WrapSkin(hybridShapeFactory, skinSet, activePart, vertMainSketches, "Vertical_Main_Skin")

        activePart.Update()
        MessageBox.Show("Generation Complete! Rudder clearance added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' =====================================================
    ' OBLITERATE PURE TRIANGLE (RUDDER ONLY)
    ' =====================================================
    Private Sub ObliterateRudderTriangle(activePart As Part, planeRef As Reference, tailOffset As Double, chord As Double)
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

    ' =====================================================
    ' WRAP SKIN
    ' =====================================================
    Private Sub WrapSkin(factory As HybridShapeFactory, skinSet As HybridBody, activePart As Part, sketches As List(Of Sketch), name As String)
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

    ' =====================================================
    ' DRAW CYLINDER SKETCH
    ' =====================================================
    Private Function DrawCylinderSketch(activePart As Part, planeRef As Reference, aircraftX As Double, aircraftY As Double, aircraftZ As Double, radius As Double, name As String, tailType As String) As Sketch
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

    ' =====================================================
    ' DRAW GENERIC SKETCH
    ' =====================================================
    Private Function DrawGenericSketch(activePart As Part, planeRef As Reference, points As List(Of Point3D), name As String, tailType As String) As Sketch
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

    ' =====================================================
    ' DRAW RIB SKETCH
    ' =====================================================
    Private Function DrawRibSketch(activePart As Part, planeRef As Reference, points As List(Of Point3D), name As String, tailType As String, drawHoles As Boolean, offset As Double, chord As Double) As Sketch
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

            Dim c1X As Double = offset + (chord * 0.35)
            Dim c2X As Double = offset + (chord * 0.48)
            Dim c3X As Double = offset + (chord * 0.6)

            If tailType = "Horizontal" Then
                factory2D.CreateClosedCircle(0, c1X, r1)
                factory2D.CreateClosedCircle(0, c2X, r2)
                factory2D.CreateClosedCircle(0, c3X, r3)
            Else
                factory2D.CreateClosedCircle(c1X, 0, r1)
                factory2D.CreateClosedCircle(c2X, 0, r2)
                factory2D.CreateClosedCircle(c3X, 0, r3)
            End If
        End If

        sketch.CloseEdition()
        activePart.UpdateObject(sketch)
        Return sketch
    End Function

    ' =====================================================
    ' CREATE SOLID LOFT
    ' =====================================================
    Private Sub CreateSolidLoftSecurely(activePart As Part, factory As HybridShapeFactory, skinSet As HybridBody, rootSketch As Sketch, tipSketch As Sketch, name As String)
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
    Public Function GeneratePartialSymmetricNACA(chord As Double, thicknessRatio As Double, numPoints As Integer, xOffset As Double, startPercent As Double, endPercent As Double) As List(Of Point3D)
        Dim profilePoints As New List(Of Point3D)
        Dim upperX As New List(Of Double)
        Dim upperY As New List(Of Double)
        Dim lowerX As New List(Of Double)
        Dim lowerY As New List(Of Double)

        For i As Integer = 0 To numPoints - 1
            Dim beta As Double = PI * (i / CDbl(numPoints - 1))
            Dim rawX As Double = 0.5 * (1.0 - Cos(beta))
            Dim x As Double = startPercent + rawX * (endPercent - startPercent)

            Dim yt As Double = 5.0 * thicknessRatio * (0.2969 * Sqrt(x) - 0.126 * x - 0.3516 * Pow(x, 2) + 0.2843 * Pow(x, 3) - 0.1015 * Pow(x, 4))
            upperX.Add((x * chord) + xOffset)
            upperY.Add(yt * chord)
            lowerX.Add((x * chord) + xOffset)
            lowerY.Add(-yt * chord)
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
End Class