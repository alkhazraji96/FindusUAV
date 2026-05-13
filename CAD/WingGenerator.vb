Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Friend Module WingGenerator
    Friend Function CreateStage4APhysicalRibs() As Object
        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Stage_4A_Tapered_Wing_Physical_Ribs")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Stage_4A_Tapered_Wing_Physical_Ribs")

        Dim hybridBodies As Object = part.HybridBodies

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Stage 4A - Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "Stage 4A - NACA 4415 Station Profiles")

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Stage 4A - Outer Wing Skin")

        Dim ribPlaneSet As Object = hybridBodies.Add()
        TrySetName(ribPlaneSet, "Stage 4A - Rib Mid-Planes")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim shapeFactory As Object = part.ShapeFactory
        Dim stations As List(Of WingStation) = BuildStations()

        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In stations
            stationProfiles.Add(AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
        Next

        Dim outerSkin As Object = CreateOuterWingSkinFromProfiles(part,
                                                                  hybridShapeFactory,
                                                                  skinSet,
                                                                  stationProfiles)

        For Each station As WingStation In stations
            AddPhysicalRibBody(part, hybridShapeFactory, shapeFactory, ribPlaneSet, station)
        Next

        TrySetInWorkObject(part, outerSkin)
        RequireUpdatePart(part, "Stage 4A wing with physical ribs")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreateStage3OuterWingSkin() As Object
        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Stage_3_Tapered_Wing_Outer_Skin")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Stage_3_Tapered_Wing_Outer_Skin")

        Dim hybridBodies As Object = part.HybridBodies

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Stage 3 - Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "Stage 3 - NACA 4415 Station Profiles")

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Stage 3 - Outer Wing Skin")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory

        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildStations()
            stationProfiles.Add(AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
        Next

        Dim outerSkin As Object = CreateOuterWingSkinFromProfiles(part,
                                                                  hybridShapeFactory,
                                                                  skinSet,
                                                                  stationProfiles)
        TrySetInWorkObject(part, outerSkin)
        RequireUpdatePart(part, "Stage 3 outer wing skin")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreateStage2AirfoilStations() As Object
        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Stage_2_Tapered_Wing_NACA4415_Stations")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Stage_2_Tapered_Wing_NACA4415_Stations")

        Dim hybridBodies As Object = part.HybridBodies

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Stage 2 - Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "Stage 2 - NACA 4415 Station Profiles")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory

        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        For Each station As WingStation In BuildStations()
            AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station)
        Next

        RequireUpdatePart(part, "Stage 2 airfoil stations")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreateStage1Planform() As Object
        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Stage_1_Tapered_Wing_Planform")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Stage_1_Tapered_Wing_Planform")

        Dim hybridBodies As Object = part.HybridBodies
        Dim stageSet As Object = hybridBodies.Add()
        TrySetName(stageSet, "Stage 1 - Planform and Rib Stations")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory

        AddPlanformGeometry(part, hybridShapeFactory, stageSet)

        RequireUpdatePart(part, "Stage 1 planform")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Private Sub AddPlanformGeometry(ByVal part As Object,
                                    ByVal hybridShapeFactory As Object,
                                    ByVal targetSet As Object)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Leading edge full span",
                        0.0, -WingDefinition.HalfSpan, 0.0, WingDefinition.HalfSpan)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Right tapered trailing edge",
                        WingDefinition.RootChord, 0.0, WingDefinition.TipChord, WingDefinition.HalfSpan)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Left tapered trailing edge",
                        WingDefinition.RootChord, 0.0, WingDefinition.TipChord, -WingDefinition.HalfSpan)

        AddRibStationLine(part, hybridShapeFactory, targetSet, "Rib_00_Center", 0.0)

        For ribIndex As Integer = 1 To WingDefinition.RibCountPerSide
            Dim stationY As Double = (WingDefinition.HalfSpan / CDbl(WingDefinition.RibCountPerSide)) * CDbl(ribIndex)

            AddRibStationLine(part, hybridShapeFactory, targetSet,
                              "Rib_R" & ribIndex.ToString("00"), stationY)
            AddRibStationLine(part, hybridShapeFactory, targetSet,
                              "Rib_L" & ribIndex.ToString("00"), -stationY)
        Next
    End Sub

    Private Sub AddRibStationLine(ByVal part As Object,
                                  ByVal hybridShapeFactory As Object,
                                  ByVal targetSet As Object,
                                  ByVal ribName As String,
                                  ByVal spanPosition As Double)
        Dim localChord As Double = WingDefinition.GetChordAtSpanPosition(spanPosition)

        AddPlanformLine(part, hybridShapeFactory, targetSet, ribName,
                        0.0, spanPosition, localChord, spanPosition)
    End Sub

    Private Sub AddPlanformLine(ByVal part As Object,
                                ByVal hybridShapeFactory As Object,
                                ByVal targetSet As Object,
                                ByVal lineName As String,
                                ByVal startX As Double,
                                ByVal startY As Double,
                                ByVal endX As Double,
                                ByVal endY As Double)
        Dim startPoint As Object = hybridShapeFactory.AddNewPointCoord(startX, startY, 0.0)
        TrySetName(startPoint, lineName & " start")
        targetSet.AppendHybridShape(startPoint)

        Dim endPoint As Object = hybridShapeFactory.AddNewPointCoord(endX, endY, 0.0)
        TrySetName(endPoint, lineName & " end")
        targetSet.AppendHybridShape(endPoint)

        Dim startReference As Object = part.CreateReferenceFromObject(startPoint)
        Dim endReference As Object = part.CreateReferenceFromObject(endPoint)
        Dim planformLine As Object = hybridShapeFactory.AddNewLinePtPt(startReference, endReference)
        TrySetName(planformLine, lineName)
        targetSet.AppendHybridShape(planformLine)
    End Sub

    Private Function BuildStations() As List(Of WingStation)
        Dim stations As New List(Of WingStation)()
        Dim stationSpacing As Double = WingDefinition.HalfSpan / CDbl(WingDefinition.RibCountPerSide)

        For ribIndex As Integer = WingDefinition.RibCountPerSide To 1 Step -1
            stations.Add(New WingStation("Rib_L" & ribIndex.ToString("00"), -stationSpacing * CDbl(ribIndex)))
        Next

        stations.Add(New WingStation("Rib_00_Center", 0.0))

        For ribIndex As Integer = 1 To WingDefinition.RibCountPerSide
            stations.Add(New WingStation("Rib_R" & ribIndex.ToString("00"), stationSpacing * CDbl(ribIndex)))
        Next

        Return stations
    End Function

    Private Function AddAirfoilStationProfile(ByVal part As Object,
                                              ByVal hybridShapeFactory As Object,
                                              ByVal targetSet As Object,
                                              ByVal station As WingStation) As WingStationProfile
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength,
                                         WingDefinition.PointCountPerSurface,
                                         WingDefinition.AirfoilMaximumCamber,
                                         WingDefinition.AirfoilMaximumCamberPosition,
                                         WingDefinition.AirfoilMaximumThickness,
                                         True)

        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline, True)
        TrySetName(profileSpline, station.Name & " profile")
        Dim closingPointReference As Object = Nothing

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim airfoilPoint As Object = hybridShapeFactory.AddNewPointCoord(coordinate.X,
                                                                             station.SpanPosition,
                                                                             coordinate.Y)
            TrySetName(airfoilPoint, station.Name & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(airfoilPoint)

            Dim pointReference As Object = part.CreateReferenceFromObject(airfoilPoint)

            If pointIndex = 0 Then
                closingPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        targetSet.AppendHybridShape(profileSpline)

        Return New WingStationProfile(station.Name, profileSpline, closingPointReference)
    End Function

    Private Function CreateOuterWingSkinFromProfiles(ByVal part As Object,
                                                     ByVal hybridShapeFactory As Object,
                                                     ByVal targetSet As Object,
                                                     ByVal stationProfiles As List(Of WingStationProfile)) As Object
        If stationProfiles.Count < 2 Then
            Throw New InvalidOperationException("At least two airfoil station profiles are required to create the wing skin.")
        End If

        Dim outerSkinLoft As Object = hybridShapeFactory.AddNewLoft()
        TrySetName(outerSkinLoft, "NACA 4415 outer wing skin")
        TrySetLoftOptions(outerSkinLoft)

        For Each stationProfile As WingStationProfile In stationProfiles
            Dim profileReference As Object = part.CreateReferenceFromObject(stationProfile.ProfileSpline)
            AddLoftSection(outerSkinLoft, profileReference, stationProfile.ClosingPointReference)
        Next

        targetSet.AppendHybridShape(outerSkinLoft)
        RequireUpdateObject(part, outerSkinLoft, "outer wing skin loft")

        Return outerSkinLoft
    End Function

    Private Sub AddLoftSection(ByVal loft As Object,
                               ByVal profileReference As Object,
                               ByVal closingPointReference As Object)
        Try
            loft.AddSectionToLoft(profileReference, 1, closingPointReference)
        Catch
            Try
                loft.AddSectionToLoft(profileReference, 1, Nothing)
            Catch
                loft.AddSectionToLoft(profileReference, 1)
            End Try
        End Try
    End Sub

    Private Sub AddPhysicalRibBody(ByVal part As Object,
                                   ByVal hybridShapeFactory As Object,
                                   ByVal shapeFactory As Object,
                                   ByVal ribPlaneSet As Object,
                                   ByVal station As WingStation)
        Dim ribBody As Object = part.Bodies.Add()
        TrySetName(ribBody, station.Name & " 3 mm rib")
        TrySetInWorkObject(part, ribBody)

        Dim ribPlane As Object = CreateRibMidPlane(part, hybridShapeFactory, ribPlaneSet, station)
        Dim ribSketch As Object = CreateRibProfileSketch(part, ribBody, ribPlane, station)
        Dim ribPad As Object = CreateRibPad(part, ribBody, shapeFactory, ribSketch, station.Name)

        TrySetName(ribPad, station.Name & " 3 mm centered rib")
        RequireUpdateObject(part, ribPad, station.Name & " physical rib")
    End Sub

    Private Function CreateRibMidPlane(ByVal part As Object,
                                       ByVal hybridShapeFactory As Object,
                                       ByVal ribPlaneSet As Object,
                                       ByVal station As WingStation) As Object
        Dim zxPlane As Object = part.OriginElements.PlaneZX

        If Math.Abs(station.SpanPosition) < 0.000001 Then
            Return zxPlane
        End If

        Dim zxPlaneReference As Object = part.CreateReferenceFromObject(zxPlane)
        Dim ribPlane As Object = hybridShapeFactory.AddNewPlaneOffset(zxPlaneReference,
                                                                      station.SpanPosition,
                                                                      False)
        TrySetName(ribPlane, station.Name & " mid-plane")
        ribPlaneSet.AppendHybridShape(ribPlane)
        RequireUpdateObject(part, ribPlane, station.Name & " rib mid-plane")

        Return ribPlane
    End Function

    Private Function CreateRibProfileSketch(ByVal part As Object,
                                            ByVal ribBody As Object,
                                            ByVal ribPlane As Object,
                                            ByVal station As WingStation) As Object
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength,
                                         WingDefinition.PointCountPerSurface,
                                         WingDefinition.AirfoilMaximumCamber,
                                         WingDefinition.AirfoilMaximumCamberPosition,
                                         WingDefinition.AirfoilMaximumThickness,
                                         True)

        Dim sketches As Object = ribBody.Sketches
        Dim ribSketch As Object = CreateSketchOnPlane(part, sketches, ribPlane)
        TrySetName(ribSketch, station.Name & " rib profile")
        TrySetInWorkObject(part, ribSketch)

        Dim sketchAxis As SketchAxisData = GetSketchAxisData(ribSketch, station.SpanPosition)
        Dim sketchFactory As Object = ribSketch.OpenEdition()
        Dim sketchCoordinates As List(Of AirfoilCoordinate) =
            ConvertGlobalXzToSketchCoordinates(airfoilCoordinates, station.SpanPosition, sketchAxis)

        CreateSmoothClosedRibSketchProfile(sketchFactory, sketchCoordinates)

        ribSketch.CloseEdition()
        RequireUpdateObject(part, ribSketch, station.Name & " rib sketch")
        RequireUpdatePart(part, station.Name & " rib sketch")

        Return ribSketch
    End Function

    Private Function CreateSketchOnPlane(ByVal part As Object,
                                         ByVal sketches As Object,
                                         ByVal sketchPlane As Object) As Object
        Try
            Dim sketchPlaneReference As Object = part.CreateReferenceFromObject(sketchPlane)
            Return sketches.Add(sketchPlaneReference)
        Catch
            Return sketches.Add(sketchPlane)
        End Try
    End Function

    Private Function ConvertGlobalXzToSketchCoordinates(ByVal airfoilCoordinates As List(Of AirfoilCoordinate),
                                                        ByVal spanPosition As Double,
                                                        ByVal sketchAxis As SketchAxisData) As List(Of AirfoilCoordinate)
        Dim sketchCoordinates As New List(Of AirfoilCoordinate)()

        For Each airfoilPoint As AirfoilCoordinate In airfoilCoordinates
            sketchCoordinates.Add(ConvertGlobalXzToSketchPoint(airfoilPoint, spanPosition, sketchAxis))
        Next

        Return sketchCoordinates
    End Function

    Private Sub CreateSmoothClosedRibSketchProfile(ByVal sketchFactory As Object,
                                                   ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        If sketchCoordinates.Count < 3 Then
            Throw New InvalidOperationException("At least three points are required to create a closed rib profile.")
        End If

        Dim sketchPoints As New List(Of Object)()

        For Each sketchCoordinate As AirfoilCoordinate In sketchCoordinates
            sketchPoints.Add(sketchFactory.CreatePoint(sketchCoordinate.X, sketchCoordinate.Y))
        Next

        Try
            Dim sketchPointArray(sketchPoints.Count - 1) As Object

            For pointIndex As Integer = 0 To sketchPoints.Count - 1
                sketchPointArray(pointIndex) = sketchPoints(pointIndex)
            Next

            sketchFactory.CreateSpline(sketchPointArray)
        Catch
            CreatePolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        End Try

        Dim lastPoint As AirfoilCoordinate = sketchCoordinates(sketchCoordinates.Count - 1)
        Dim firstPoint As AirfoilCoordinate = sketchCoordinates(0)
        CreateSketchLineIfDistinct(sketchFactory, lastPoint, firstPoint)
    End Sub

    Private Sub CreatePolylineRibSketchProfile(ByVal sketchFactory As Object,
                                               ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        For pointIndex As Integer = 0 To sketchCoordinates.Count - 2
            CreateSketchLineIfDistinct(sketchFactory,
                                       sketchCoordinates(pointIndex),
                                       sketchCoordinates(pointIndex + 1))
        Next
    End Sub

    Private Sub CreateSketchLineIfDistinct(ByVal sketchFactory As Object,
                                           ByVal startPoint As AirfoilCoordinate,
                                           ByVal endPoint As AirfoilCoordinate)
        If Not AreSketchPointsCoincident(startPoint, endPoint) Then
            sketchFactory.CreateLine(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y)
        End If
    End Sub

    Private Function CreateRibPad(ByVal part As Object,
                                  ByVal ribBody As Object,
                                  ByVal shapeFactory As Object,
                                  ByVal ribSketch As Object,
                                  ByVal ribName As String) As Object
        Dim halfThickness As Double = WingDefinition.RibThickness / 2.0

        TrySetInWorkObject(part, ribBody)
        RequireUpdateObject(part, ribSketch, ribName & " rib sketch before pad")
        RequireUpdatePart(part, ribName & " rib body before pad")

        Try
            Dim ribPad As Object = shapeFactory.AddNewPad(ribSketch, halfThickness)
            RequireCenteredPad(ribPad, WingDefinition.RibThickness, ribName & " rib pad")

            Return ribPad
        Catch firstException As COMException
            Try
                Dim ribSketchReference As Object = part.CreateReferenceFromObject(ribSketch)
                Dim ribPad As Object = shapeFactory.AddNewPadFromRef(ribSketchReference, halfThickness)
                RequireCenteredPad(ribPad, WingDefinition.RibThickness, ribName & " rib pad")

                Return ribPad
            Catch
                Throw New InvalidOperationException("CATIA could not create the 3 mm rib pad for " & ribName & ". Check that the rib sketch is closed and that its body is active.", firstException)
            End Try
        End Try
    End Function

    Private Function AreSketchPointsCoincident(ByVal firstPoint As AirfoilCoordinate,
                                               ByVal secondPoint As AirfoilCoordinate) As Boolean
        Return (Math.Abs(firstPoint.X - secondPoint.X) < 0.000001) AndAlso _
            (Math.Abs(firstPoint.Y - secondPoint.Y) < 0.000001)
    End Function

    Private Function ConvertGlobalXzToSketchPoint(ByVal airfoilPoint As AirfoilCoordinate,
                                                  ByVal spanPosition As Double,
                                                  ByVal sketchAxis As SketchAxisData) As AirfoilCoordinate
        Dim deltaX As Double = airfoilPoint.X - sketchAxis.OriginX
        Dim deltaY As Double = spanPosition - sketchAxis.OriginY
        Dim deltaZ As Double = airfoilPoint.Y - sketchAxis.OriginZ

        Dim horizontalLengthSquared As Double =
            (sketchAxis.HorizontalX * sketchAxis.HorizontalX) +
            (sketchAxis.HorizontalY * sketchAxis.HorizontalY) +
            (sketchAxis.HorizontalZ * sketchAxis.HorizontalZ)
        Dim verticalLengthSquared As Double =
            (sketchAxis.VerticalX * sketchAxis.VerticalX) +
            (sketchAxis.VerticalY * sketchAxis.VerticalY) +
            (sketchAxis.VerticalZ * sketchAxis.VerticalZ)

        If horizontalLengthSquared < 0.000001 Then
            horizontalLengthSquared = 1.0
        End If

        If verticalLengthSquared < 0.000001 Then
            verticalLengthSquared = 1.0
        End If

        Dim sketchX As Double =
            ((deltaX * sketchAxis.HorizontalX) +
             (deltaY * sketchAxis.HorizontalY) +
             (deltaZ * sketchAxis.HorizontalZ)) / horizontalLengthSquared
        Dim sketchY As Double =
            ((deltaX * sketchAxis.VerticalX) +
             (deltaY * sketchAxis.VerticalY) +
             (deltaZ * sketchAxis.VerticalZ)) / verticalLengthSquared

        Return New AirfoilCoordinate(sketchX, sketchY)
    End Function

    Private Function GetSketchAxisData(ByVal sketch As Object,
                                       ByVal spanPosition As Double) As SketchAxisData
        Try
            Dim axisData(8) As Object
            sketch.GetAbsoluteAxisData(axisData)
            Return CreateSketchAxisData(axisData)
        Catch
        End Try

        Try
            Dim axisData(8) As Double
            sketch.GetAbsoluteAxisData(axisData)
            Return CreateSketchAxisData(axisData)
        Catch
        End Try

        Return New SketchAxisData(0.0, spanPosition, 0.0,
                                  -1.0, 0.0, 0.0,
                                  0.0, 0.0, 1.0)
    End Function

    Private Function CreateSketchAxisData(ByVal axisData() As Object) As SketchAxisData
        Return New SketchAxisData(CDbl(axisData(0)), CDbl(axisData(1)), CDbl(axisData(2)),
                                  CDbl(axisData(3)), CDbl(axisData(4)), CDbl(axisData(5)),
                                  CDbl(axisData(6)), CDbl(axisData(7)), CDbl(axisData(8)))
    End Function

    Private Function CreateSketchAxisData(ByVal axisData() As Double) As SketchAxisData
        Return New SketchAxisData(axisData(0), axisData(1), axisData(2),
                                  axisData(3), axisData(4), axisData(5),
                                  axisData(6), axisData(7), axisData(8))
    End Function

    Private Structure SketchAxisData
        Friend ReadOnly OriginX As Double
        Friend ReadOnly OriginY As Double
        Friend ReadOnly OriginZ As Double
        Friend ReadOnly HorizontalX As Double
        Friend ReadOnly HorizontalY As Double
        Friend ReadOnly HorizontalZ As Double
        Friend ReadOnly VerticalX As Double
        Friend ReadOnly VerticalY As Double
        Friend ReadOnly VerticalZ As Double

        Friend Sub New(ByVal originX As Double,
                       ByVal originY As Double,
                       ByVal originZ As Double,
                       ByVal horizontalX As Double,
                       ByVal horizontalY As Double,
                       ByVal horizontalZ As Double,
                       ByVal verticalX As Double,
                       ByVal verticalY As Double,
                       ByVal verticalZ As Double)
            Me.OriginX = originX
            Me.OriginY = originY
            Me.OriginZ = originZ
            Me.HorizontalX = horizontalX
            Me.HorizontalY = horizontalY
            Me.HorizontalZ = horizontalZ
            Me.VerticalX = verticalX
            Me.VerticalY = verticalY
            Me.VerticalZ = verticalZ
        End Sub
    End Structure
End Module
