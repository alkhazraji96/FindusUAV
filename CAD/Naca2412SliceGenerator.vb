Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Friend Module Naca2412SliceGenerator
    Friend Function CreatePart(Optional ByVal chordLength As Double = 100.0,
                               Optional ByVal pointCountPerSurface As Integer = 81,
                               Optional ByVal padLength As Double = 3.0) As Object
        If chordLength <= 0.0 Then
            Throw New ArgumentOutOfRangeException("chordLength", "Chord length must be greater than zero.")
        End If

        If padLength <= 0.0 Then
            Throw New ArgumentOutOfRangeException("padLength", "Pad length must be greater than zero.")
        End If

        If pointCountPerSurface < 5 Then
            pointCountPerSurface = 5
        End If

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "NACA_2412_Airfoil")

        Dim part As Object = partDocument.Part
        TrySetName(part, "NACA_2412_Airfoil")

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength, pointCountPerSurface, 0.02, 0.4, 0.12, False)
        Dim mainBody As Object = GetMainBody(part)
        TrySetInWorkObject(part, mainBody)

        Dim padSketch As Object = CreateAirfoilPadSketch(part, mainBody, airfoilCoordinates)
        Dim shapeFactory As Object = part.ShapeFactory
        Dim airfoilPad As Object = CreateAirfoilPad(part, mainBody, shapeFactory, padSketch, padLength)
        TrySetName(airfoilPad, "NACA 2412 3mm rigid slice")

        Dim hybridBodies As Object = part.HybridBodies
        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "NACA 2412 Airfoil 2D")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline)
        TrySetName(profileSpline, "NACA 2412 profile")

        Dim firstPointReference As Object = Nothing
        Dim lastPointReference As Object = Nothing

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim airfoilPoint As Object = hybridShapeFactory.AddNewPointCoord(coordinate.X, coordinate.Y, 0.0)
            TrySetName(airfoilPoint, "NACA2412_Point_" & (pointIndex + 1).ToString("000"))

            airfoilSet.AppendHybridShape(airfoilPoint)
            Dim pointReference As Object = part.CreateReferenceFromObject(airfoilPoint)

            If pointIndex = 0 Then
                firstPointReference = pointReference
            End If

            If pointIndex = airfoilCoordinates.Count - 1 Then
                lastPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        airfoilSet.AppendHybridShape(profileSpline)

        If firstPointReference IsNot Nothing AndAlso lastPointReference IsNot Nothing Then
            Dim trailingEdgeLine As Object = hybridShapeFactory.AddNewLinePtPt(lastPointReference, firstPointReference)
            TrySetName(trailingEdgeLine, "NACA 2412 trailing edge")
            airfoilSet.AppendHybridShape(trailingEdgeLine)
        End If

        TrySetInWorkObject(part, airfoilPad)
        part.Update()
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Private Function CreateAirfoilPadSketch(ByVal part As Object,
                                            ByVal mainBody As Object,
                                            ByVal airfoilCoordinates As List(Of AirfoilCoordinate)) As Object
        Dim sketches As Object = mainBody.Sketches
        Dim xyPlane As Object = part.OriginElements.PlaneXY
        Dim padSketch As Object = sketches.Add(xyPlane)
        TrySetName(padSketch, "NACA 2412 pad profile")
        TrySetInWorkObject(part, padSketch)

        Dim sketchFactory As Object = padSketch.OpenEdition()

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim startPoint As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim endPoint As AirfoilCoordinate

            If pointIndex = airfoilCoordinates.Count - 1 Then
                endPoint = airfoilCoordinates(0)
            Else
                endPoint = airfoilCoordinates(pointIndex + 1)
            End If

            sketchFactory.CreateLine(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y)
        Next

        padSketch.CloseEdition()
        TryUpdateObject(part, padSketch)
        TryUpdatePart(part)

        Return padSketch
    End Function

    Private Function CreateAirfoilPad(ByVal part As Object,
                                      ByVal mainBody As Object,
                                      ByVal shapeFactory As Object,
                                      ByVal padSketch As Object,
                                      ByVal padLength As Double) As Object
        TrySetInWorkObject(part, mainBody)
        TryUpdateObject(part, padSketch)
        TryUpdatePart(part)

        Try
            Return shapeFactory.AddNewPad(padSketch, padLength)
        Catch firstException As COMException
            Try
                Dim padSketchReference As Object = part.CreateReferenceFromObject(padSketch)
                Return shapeFactory.AddNewPadFromRef(padSketchReference, padLength)
            Catch
                Throw New InvalidOperationException("CATIA could not create the 3 mm pad. Check that the sketch profile is closed and that PartBody is the active body.", firstException)
            End Try
        End Try
    End Function
End Module
