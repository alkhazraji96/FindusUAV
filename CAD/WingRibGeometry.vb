Imports System.Collections.Generic

Friend Module WingRibGeometry
    Private Enum RibProfileRegion
        Full
        ForwardWingPanel
    End Enum

    Friend Sub AddPhysicalRibBody(ByVal part As Object,
                                  ByVal hybridShapeFactory As Object,
                                  ByVal shapeFactory As Object,
                                  ByVal ribPlaneSet As Object,
                                  ByVal station As WingStation,
                                  Optional ByVal includeRibCutouts As Boolean = False,
                                  Optional ByVal includeAileronSplit As Boolean = False)
        Dim ribPlane As Object = CreateRibMidPlane(part, hybridShapeFactory, ribPlaneSet, station)

        If includeAileronSplit Then
            AddPhysicalRibBodySection(part,
                                      shapeFactory,
                                      ribPlane,
                                      station,
                                      includeRibCutouts,
                                      RibProfileRegion.ForwardWingPanel)
            Return
        End If

        AddPhysicalRibBodySection(part,
                                  shapeFactory,
                                  ribPlane,
                                  station,
                                  includeRibCutouts,
                                  RibProfileRegion.Full)
    End Sub

    Private Sub AddPhysicalRibBodySection(ByVal part As Object,
                                          ByVal shapeFactory As Object,
                                          ByVal ribPlane As Object,
                                          ByVal station As WingStation,
                                          ByVal includeRibCutouts As Boolean,
                                          ByVal ribProfileRegion As RibProfileRegion)
        Dim ribBody As Object = part.Bodies.Add()
        TrySetName(ribBody, station.Name & GetRibBodyNameSuffix(ribProfileRegion))
        TrySetInWorkObject(part, ribBody)

        Dim ribSketch As Object = CreateRibProfileSketch(part,
                                                         ribBody,
                                                         ribPlane,
                                                         station,
                                                         includeRibCutouts,
                                                         ribProfileRegion)
        Dim ribName As String = station.Name & GetRibPadNameSuffix(ribProfileRegion)
        Dim ribPad As Object = CreateRibPad(part, ribBody, shapeFactory, ribSketch, ribName)

        TrySetName(ribPad, ribName & " 3 mm centered rib")
        RequireUpdateObject(part, ribPad, ribName & " physical rib")
    End Sub

    Private Function GetRibBodyNameSuffix(ByVal ribProfileRegion As RibProfileRegion) As String
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return " 3 mm forward wing rib"
            Case Else
                Return " 3 mm rib"
        End Select
    End Function

    Private Function GetRibPadNameSuffix(ByVal ribProfileRegion As RibProfileRegion) As String
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return " forward wing section"
            Case Else
                Return String.Empty
        End Select
    End Function

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
                                            ByVal station As WingStation,
                                            ByVal includeRibCutouts As Boolean,
                                            ByVal ribProfileRegion As RibProfileRegion) As Object
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength,
                                         WingDefinition.PointCountPerSurface,
                                         WingDefinition.AirfoilMaximumCamber,
                                         WingDefinition.AirfoilMaximumCamberPosition,
                                         WingDefinition.AirfoilMaximumThickness,
                                         True)

        If ribProfileRegion = RibProfileRegion.ForwardWingPanel Then
            airfoilCoordinates = ClipAirfoilProfileByX(airfoilCoordinates,
                                                       WingDefinition.GetAileronFixedPanelEndXAtSpanPosition(station.SpanPosition),
                                                       True)
        End If

        If airfoilCoordinates.Count < 3 Then
            Throw New InvalidOperationException("The aileron rib split left fewer than three points for " & station.Name & ".")
        End If

        Dim sketches As Object = ribBody.Sketches
        Dim ribSketch As Object = CreateSketchOnPlane(part, sketches, ribPlane)
        TrySetName(ribSketch, station.Name & " rib profile")
        TrySetInWorkObject(part, ribSketch)

        Dim sketchAxis As SketchAxisData = GetSketchAxisData(ribSketch, station.SpanPosition)
        Dim sketchFactory As Object = ribSketch.OpenEdition()
        Dim sketchCoordinates As List(Of AirfoilCoordinate) =
            ConvertGlobalXzToSketchCoordinates(airfoilCoordinates, station.SpanPosition, sketchAxis)

        If ribProfileRegion = RibProfileRegion.Full Then
            CreateSmoothClosedRibSketchProfile(sketchFactory, sketchCoordinates)
        Else
            CreateClosedPolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        End If

        If includeRibCutouts Then
            CreateMainSparCutoutSketchProfile(sketchFactory, station, sketchAxis, ribProfileRegion)
            CreateRibLighteningCutoutSketchProfiles(sketchFactory, station, sketchAxis, ribProfileRegion)
        End If

        ribSketch.CloseEdition()
        RequireUpdateObject(part, ribSketch, station.Name & " rib sketch")
        RequireUpdatePart(part, station.Name & " rib sketch")

        Return ribSketch
    End Function

    Private Sub CreateMainSparCutoutSketchProfile(ByVal sketchFactory As Object,
                                                  ByVal station As WingStation,
                                                  ByVal sketchAxis As SketchAxisData,
                                                  ByVal ribProfileRegion As RibProfileRegion)
        Dim localCenterX As Double = WingDefinition.GetMainSparCenterLocalXAtSpanPosition(station.SpanPosition)
        Dim globalCenterX As Double = WingDefinition.GetGlobalXAtSpanPosition(station.SpanPosition,
                                                                              localCenterX)
        Dim radius As Double = WingDefinition.MainSparCutoutDiameter / 2.0

        If Not IsCircularCutoutInsideRibRegion(localCenterX,
                                               radius,
                                               station.SpanPosition,
                                               ribProfileRegion) Then
            Return
        End If

        Dim sparCenter As AirfoilCoordinate =
            ConvertGlobalPointToSketchPoint(globalCenterX,
                                            station.SpanPosition,
                                            WingDefinition.GetMainSparCenterZAtSpanPosition(station.SpanPosition),
                                            sketchAxis)

        CreateSketchCircle(sketchFactory,
                           sparCenter,
                           radius)
    End Sub

    Private Sub CreateRibLighteningCutoutSketchProfiles(ByVal sketchFactory As Object,
                                                        ByVal station As WingStation,
                                                        ByVal sketchAxis As SketchAxisData,
                                                        ByVal ribProfileRegion As RibProfileRegion)
        For Each cutoutDefinition As WingDefinition.RibLighteningCutoutDefinition In WingDefinition.GetRibLighteningCutouts()
            Dim cutoutDiameter As Double =
                WingDefinition.GetRibLighteningCutoutDiameter(station.SpanPosition, cutoutDefinition)

            If cutoutDiameter <= 0.0 Then
                Continue For
            End If

            Dim radius As Double = cutoutDiameter / 2.0
            Dim localCenterX As Double =
                WingDefinition.GetRibLighteningCutoutCenterLocalX(station.SpanPosition, cutoutDefinition)

            If Not IsCircularCutoutInsideRibRegion(localCenterX,
                                                   radius,
                                                   station.SpanPosition,
                                                   ribProfileRegion) Then
                Continue For
            End If

            Dim globalCenterX As Double =
                WingDefinition.GetGlobalXAtSpanPosition(station.SpanPosition, localCenterX)
            Dim cutoutCenter As AirfoilCoordinate =
                ConvertGlobalPointToSketchPoint(globalCenterX,
                                                station.SpanPosition,
                                                WingDefinition.GetRibLighteningCutoutCenterZ(station.SpanPosition, cutoutDefinition),
                                                sketchAxis)

            CreateSketchCircle(sketchFactory, cutoutCenter, radius)
        Next
    End Sub

    Private Function IsCircularCutoutInsideRibRegion(ByVal localCenterX As Double,
                                                     ByVal radius As Double,
                                                     ByVal spanPosition As Double,
                                                     ByVal ribProfileRegion As RibProfileRegion) As Boolean
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return (localCenterX + radius) <
                    (WingDefinition.GetAileronFixedPanelEndXAtSpanPosition(spanPosition) - 0.000001)
            Case Else
                Return True
        End Select
    End Function

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
            Dim ribSketchReference As Object = part.CreateReferenceFromObject(ribSketch)
            Dim ribPad As Object = shapeFactory.AddNewPadFromRef(ribSketchReference,
                                                                 halfThickness)
            RequireCenteredPad(ribPad, WingDefinition.RibThickness, ribName & " rib pad")

            Return ribPad
        Catch ex As Exception
            Throw New InvalidOperationException("CATIA could not create the 3 mm rib pad for " & ribName & ". Check that the rib sketch is closed and that its body is active.", ex)
        End Try
    End Function
End Module
