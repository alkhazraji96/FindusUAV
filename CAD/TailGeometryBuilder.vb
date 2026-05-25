Imports INFITF
Imports MECMOD
Imports PARTITF
Imports HybridShapeTypeLib

Friend Module TailGeometryBuilder
    Private Const ForwardLighteningCutoutChordFraction As Double = 0.35
    Private Const MiddleLighteningCutoutChordFraction As Double = 0.48
    Private Const AftLighteningCutoutChordFraction As Double = 0.6

    Friend Sub HideTailConstructionGeometry(ByVal partDocument As PartDocument,
                                            ByVal activePart As Part,
                                            ByVal planeSet As HybridBody,
                                            ByVal skinSet As HybridBody)
        TryHideObject(partDocument, planeSet)
        TryHideSketchesInBodies(partDocument, activePart)
        TryHideHybridShapesByNameEnding(partDocument, skinSet, "_Surface")
    End Sub

    Friend Sub ObliterateRudderTriangle(activePart As Part, planeRef As Reference, tailOffset As Double, chord As Double)
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

        Dim l1 = factory2D.CreateLine(z1, x1, z2, x2)
        l1.StartPoint = pt1
        l1.EndPoint = pt2

        Dim l2 = factory2D.CreateLine(z2, x2, z3, x3)
        l2.StartPoint = pt2
        l2.EndPoint = pt3

        Dim l3 = factory2D.CreateLine(z3, x3, z1, x1)
        l3.StartPoint = pt3
        l3.EndPoint = pt1

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

    Friend Sub WrapSkin(factory As HybridShapeFactory, skinSet As HybridBody, activePart As Part, sketches As List(Of Sketch), name As String)
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

    Friend Function DrawCylinderSketch(activePart As Part, planeRef As Reference, aircraftX As Double, aircraftY As Double, aircraftZ As Double, radius As Double, name As String, tailType As String) As Sketch
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

    Friend Function DrawGenericSketch(activePart As Part, planeRef As Reference, points As List(Of TailPoint3D), name As String, tailType As String) As Sketch
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

    Friend Function DrawRibSketch(activePart As Part, planeRef As Reference, points As List(Of TailPoint3D), name As String, tailType As String, drawHoles As Boolean, offset As Double, chord As Double, Optional airfoil As AirfoilConfiguration = Nothing) As Sketch
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

    Friend Function GetAirfoilMeanCamberY(ByVal chord As Double,
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

    Friend Sub CreateSolidLoftSecurely(activePart As Part, factory As HybridShapeFactory, skinSet As HybridBody, rootSketch As Sketch, tipSketch As Sketch, name As String)
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
End Module
