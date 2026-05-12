Imports System.Collections.Generic

Friend Module WingGenerator
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
        part.Update()
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

        part.Update()
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

        part.Update()
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
        TryUpdateObject(part, outerSkinLoft)

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
End Module
