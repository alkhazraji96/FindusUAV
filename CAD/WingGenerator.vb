Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Friend Module WingGenerator
    Private Enum RibProfileRegion
        Full
        ForwardWingPanel
    End Enum

    Private Structure PointCoordinate3D
        Friend ReadOnly X As Double
        Friend ReadOnly Y As Double
        Friend ReadOnly Z As Double

        Friend Sub New(ByVal x As Double,
                       ByVal y As Double,
                       ByVal z As Double)
            Me.X = x
            Me.Y = y
            Me.Z = z
        End Sub
    End Structure

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

    Friend Function CreateStage4BPhysicalRibsAndMainSpar() As Object
        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Stage_4B_Tapered_Wing_Ribs_And_Main_Spar")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Stage_4B_Tapered_Wing_Ribs_And_Main_Spar")

        Dim hybridBodies As Object = part.HybridBodies

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Stage 4B - Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "Stage 4B - NACA 4415 Station Profiles")

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Stage 4B - Outer Wing Skin")

        Dim ribPlaneSet As Object = hybridBodies.Add()
        TrySetName(ribPlaneSet, "Stage 4B - Rib Mid-Planes")

        Dim sparReferenceSet As Object = hybridBodies.Add()
        TrySetName(sparReferenceSet, "Stage 4B - Main Spar References")

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
            AddPhysicalRibBody(part,
                               hybridShapeFactory,
                               shapeFactory,
                               ribPlaneSet,
                               station,
                               True)
        Next

        Dim mainSpar As Object = AddMainSparBody(part,
                                                 hybridShapeFactory,
                                                 shapeFactory,
                                                 sparReferenceSet)

        TrySetInWorkObject(part, mainSpar)
        RequireUpdatePart(part, "Stage 4B wing with physical ribs and main spar")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreateStage4CPhysicalRibsMainSparAndAilerons() As Object
        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Stage_4C_Tapered_Wing_Ribs_Spar_And_Ailerons")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Stage_4C_Tapered_Wing_Ribs_Spar_And_Ailerons")

        Dim hybridBodies As Object = part.HybridBodies

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Stage 4C - Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "Stage 4C - NACA 4415 Station Profiles")

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Stage 4C - Outer Wing Skin")

        Dim ribPlaneSet As Object = hybridBodies.Add()
        TrySetName(ribPlaneSet, "Stage 4C - Rib Mid-Planes")

        Dim sparReferenceSet As Object = hybridBodies.Add()
        TrySetName(sparReferenceSet, "Stage 4C - Main Spar References")

        Dim aileronReferenceSet As Object = hybridBodies.Add()
        TrySetName(aileronReferenceSet, "Stage 4C - Aileron Cut References")

        Dim aileronRearSparSet As Object = hybridBodies.Add()
        TrySetName(aileronRearSparSet, "Stage 4C - Aileron Rear Hinge Spars")

        Dim aileronSkinSet As Object = hybridBodies.Add()
        TrySetName(aileronSkinSet, "Stage 4C - Aileron Skins")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim shapeFactory As Object = part.ShapeFactory
        Dim stations As List(Of WingStation) = BuildStations()

        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In stations
            stationProfiles.Add(AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
        Next

        CreateStage4CSplitSkinSurfaces(partDocument,
                                        part,
                                        hybridShapeFactory,
                                        shapeFactory,
                                        skinSet,
                                        aileronSkinSet,
                                        stations,
                                        stationProfiles)
        AddAileronRearHingeSpars(part,
                                  hybridShapeFactory,
                                  shapeFactory,
                                  aileronRearSparSet)
        AddAileronCutReferenceGeometry(part, hybridShapeFactory, aileronReferenceSet)

        For Each station As WingStation In stations
            AddPhysicalRibBody(part,
                               hybridShapeFactory,
                               shapeFactory,
                               ribPlaneSet,
                               station,
                               True,
                               WingDefinition.IsWithinAileronSpan(station.SpanPosition))
        Next

        Dim mainSpar As Object = AddMainSparBody(part,
                                                 hybridShapeFactory,
                                                 shapeFactory,
                                                 sparReferenceSet)

        TrySetInWorkObject(part, mainSpar)
        RequireUpdatePart(part, "Stage 4C wing with physical ribs, main spar, and aileron cuts")
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

    Private Function BuildAileronSurfaceStations(ByVal spanSign As Double,
                                                 ByVal sideName As String) As List(Of WingStation)
        Dim stations As New List(Of WingStation)()
        Dim sideRibPrefix As String = If(spanSign < 0.0, "Rib_L", "Rib_R")

        stations.Add(New WingStation(sideName & "_Aileron_Inner_Boundary",
                                     spanSign * WingDefinition.AileronInnerSpanPosition))

        For ribIndex As Integer = 1 To WingDefinition.RibCountPerSide
            Dim ribSpanPosition As Double = WingDefinition.GetRibSpanPosition(ribIndex)

            If ribSpanPosition > (WingDefinition.AileronInnerSpanPosition + 0.000001) AndAlso
                ribSpanPosition <= (WingDefinition.AileronOuterSpanPosition + 0.000001) Then
                stations.Add(New WingStation(sideRibPrefix & ribIndex.ToString("00"),
                                             spanSign * ribSpanPosition))
            End If
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
                                                     ByVal stationProfiles As List(Of WingStationProfile),
                                                     Optional ByVal skinName As String = "NACA 4415 outer wing skin") As Object
        If stationProfiles.Count < 2 Then
            Throw New InvalidOperationException("At least two airfoil station profiles are required to create the wing skin.")
        End If

        Dim outerSkinLoft As Object = hybridShapeFactory.AddNewLoft()
        TrySetName(outerSkinLoft, skinName)
        TrySetLoftOptions(outerSkinLoft)

        For Each stationProfile As WingStationProfile In stationProfiles
            Dim profileReference As Object = part.CreateReferenceFromObject(stationProfile.ProfileSpline)
            AddLoftSection(outerSkinLoft, profileReference, stationProfile.ClosingPointReference)
        Next

        targetSet.AppendHybridShape(outerSkinLoft)
        RequireUpdateObject(part, outerSkinLoft, skinName)

        Return outerSkinLoft
    End Function

    Private Sub CreateStage4CSplitSkinSurfaces(ByVal partDocument As Object,
                                               ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal shapeFactory As Object,
                                               ByVal fixedSkinSet As Object,
                                               ByVal aileronSkinSet As Object,
                                               ByVal stations As List(Of WingStation),
                                               ByVal stationProfiles As List(Of WingStationProfile))
        Dim centerSkinProfiles As New List(Of WingStationProfile)()
        centerSkinProfiles.Add(AddAirfoilStationProfile(part,
                                                        hybridShapeFactory,
                                                        fixedSkinSet,
                                                        New WingStation("Aileron_Left_Inner_Boundary",
                                                                        -WingDefinition.AileronInnerSpanPosition)))

        For stationIndex As Integer = 0 To stations.Count - 1
            If Math.Abs(stations(stationIndex).SpanPosition) <
                (WingDefinition.AileronInnerSpanPosition - 0.000001) Then
                centerSkinProfiles.Add(stationProfiles(stationIndex))
            End If
        Next

        centerSkinProfiles.Add(AddAirfoilStationProfile(part,
                                                        hybridShapeFactory,
                                                        fixedSkinSet,
                                                        New WingStation("Aileron_Right_Inner_Boundary",
                                                                        WingDefinition.AileronInnerSpanPosition)))

        CreateOuterWingSkinFromProfiles(part,
                                        hybridShapeFactory,
                                        fixedSkinSet,
                                        centerSkinProfiles,
                                        "Center fixed wing skin")
        AddOutboardFixedWingSkinSurfaces(part, hybridShapeFactory, fixedSkinSet, -1.0, "Left")
        AddOutboardFixedWingSkinSurfaces(part, hybridShapeFactory, fixedSkinSet, 1.0, "Right")

        Dim leftAileronSkins As List(Of Object) =
            AddAileronSkinSurfaces(part, hybridShapeFactory, shapeFactory, aileronSkinSet, -1.0, "Left")
        Dim rightAileronSkins As List(Of Object) =
            AddAileronSkinSurfaces(part, hybridShapeFactory, shapeFactory, aileronSkinSet, 1.0, "Right")

        For Each aileronSkin As Object In leftAileronSkins
            TrySetObjectColor(partDocument, aileronSkin, 255, 145, 0)
        Next

        For Each aileronSkin As Object In rightAileronSkins
            TrySetObjectColor(partDocument, aileronSkin, 255, 145, 0)
        Next

        RequireUpdatePart(part, "Stage 4C split fixed wing and aileron skin surfaces")
    End Sub

    Private Sub AddOutboardFixedWingSkinSurfaces(ByVal part As Object,
                                                 ByVal hybridShapeFactory As Object,
                                                 ByVal targetSet As Object,
                                                 ByVal spanSign As Double,
                                                 ByVal sideName As String)
        Dim fixedWingProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildAileronSurfaceStations(spanSign, sideName)
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

            fixedWingProfiles.Add(AddClosedSplitSkinProfile(part,
                                                            hybridShapeFactory,
                                                            targetSet,
                                                            sideName & " outboard fixed wing skin profile " & station.Name,
                                                            sideName & "_Fixed_Wing_Skin_" & station.Name,
                                                            station.SpanPosition,
                                                            BuildFixedWingOutboardSkinProfileCoordinates(chordLength)))
        Next

        CreateOuterWingSkinFromProfiles(part,
                                        hybridShapeFactory,
                                        targetSet,
                                        fixedWingProfiles,
                                        sideName & " outboard fixed wing skin")
    End Sub

    Private Function AddAileronSkinSurfaces(ByVal part As Object,
                                            ByVal hybridShapeFactory As Object,
                                            ByVal shapeFactory As Object,
                                            ByVal targetSet As Object,
                                            ByVal spanSign As Double,
                                            ByVal sideName As String) As List(Of Object)
        Dim aileronSkins As New List(Of Object)()
        Dim aileronProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildAileronSurfaceStations(spanSign, sideName)
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

            aileronProfiles.Add(AddClosedSplitSkinProfile(part,
                                                          hybridShapeFactory,
                                                          targetSet,
                                                          sideName & " aileron skin profile " & station.Name,
                                                          sideName & "_Aileron_Skin_" & station.Name,
                                                          station.SpanPosition,
                                                          BuildAileronSkinProfileCoordinates(chordLength)))
        Next

        Dim aileronSurface As Object =
            CreateOuterWingSkinFromProfiles(part,
                                            hybridShapeFactory,
                                            targetSet,
                                            aileronProfiles,
                                            sideName & " aileron skin surface")
        Dim aileronSolid As Object =
            CreateClosedSurfaceSolid(part,
                                     shapeFactory,
                                     aileronSurface,
                                     sideName & " physical aileron")
        aileronSkins.Add(aileronSurface)
        aileronSkins.Add(aileronSolid)

        Return aileronSkins
    End Function

    Private Sub AddAileronRearHingeSpars(ByVal part As Object,
                                         ByVal hybridShapeFactory As Object,
                                         ByVal shapeFactory As Object,
                                         ByVal targetSet As Object)
        AddAileronRearHingeSparForSide(part, hybridShapeFactory, shapeFactory, targetSet, -1.0, "Left")
        AddAileronRearHingeSparForSide(part, hybridShapeFactory, shapeFactory, targetSet, 1.0, "Right")
        RequireUpdatePart(part, "Stage 4C aileron rear hinge spars")
    End Sub

    Private Function AddAileronRearHingeSparForSide(ByVal part As Object,
                                                    ByVal hybridShapeFactory As Object,
                                                    ByVal shapeFactory As Object,
                                                    ByVal targetSet As Object,
                                                    ByVal spanSign As Double,
                                                    ByVal sideName As String) As Object
        Dim sparProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildAileronSurfaceStations(spanSign, sideName)
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

            sparProfiles.Add(AddClosedSplitSkinProfile(part,
                                                       hybridShapeFactory,
                                                       targetSet,
                                                       sideName & " rear hinge spar profile " & station.Name,
                                                       sideName & "_Rear_Hinge_Spar_" & station.Name,
                                                       station.SpanPosition,
                                                       BuildAileronRearHingeSparProfileCoordinates(chordLength)))
        Next

        Dim sparSurface As Object =
            CreateOuterWingSkinFromProfiles(part,
                                            hybridShapeFactory,
                                            targetSet,
                                            sparProfiles,
                                            sideName & " aileron rear hinge spar surface")

        Return CreateClosedSurfaceSolid(part,
                                        shapeFactory,
                                        sparSurface,
                                        sideName & " aileron rear hinge spar")
    End Function

    Private Function CreateClosedSurfaceSolid(ByVal part As Object,
                                              ByVal shapeFactory As Object,
                                              ByVal surfaceObject As Object,
                                              ByVal solidName As String) As Object
        Dim solidBody As Object = part.Bodies.Add()
        TrySetName(solidBody, solidName & " body")
        TrySetInWorkObject(part, solidBody)

        Dim closeSurface As Object =
            shapeFactory.AddNewCloseSurface(part.CreateReferenceFromObject(surfaceObject))
        TrySetName(closeSurface, solidName)
        RequireUpdateObject(part, closeSurface, solidName)

        Return closeSurface
    End Function

    Private Function AddClosedSplitSkinProfile(ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal targetSet As Object,
                                               ByVal profileName As String,
                                               ByVal pointPrefix As String,
                                               ByVal spanPosition As Double,
                                               ByVal skinCoordinates As List(Of AirfoilCoordinate)) As WingStationProfile
        If skinCoordinates.Count < 3 Then
            Throw New InvalidOperationException("At least three points are required for " & profileName & ".")
        End If

        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline, True)
        TrySetName(profileSpline, profileName)
        Dim closingPointReference As Object = Nothing

        For pointIndex As Integer = 0 To skinCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = skinCoordinates(pointIndex)
            Dim profilePoint As Object = hybridShapeFactory.AddNewPointCoord(coordinate.X,
                                                                             spanPosition,
                                                                             coordinate.Y)
            TrySetName(profilePoint,
                       pointPrefix & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(profilePoint)

            Dim pointReference As Object = part.CreateReferenceFromObject(profilePoint)

            If pointIndex = 0 Then
                closingPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        targetSet.AppendHybridShape(profileSpline)

        Return New WingStationProfile(profileName, profileSpline, closingPointReference)
    End Function

    Private Sub AddAileronCutReferenceGeometry(ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal targetSet As Object)
        AddAileronCutReferenceGeometryForSide(part, hybridShapeFactory, targetSet, 1.0, "Right")
        AddAileronCutReferenceGeometryForSide(part, hybridShapeFactory, targetSet, -1.0, "Left")
        RequireUpdatePart(part, "aileron cut reference geometry")
    End Sub

    Private Sub AddAileronCutReferenceGeometryForSide(ByVal part As Object,
                                                      ByVal hybridShapeFactory As Object,
                                                      ByVal targetSet As Object,
                                                      ByVal spanSign As Double,
                                                      ByVal sideName As String)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " upper fixed wing rear spar face",
                                       spanSign,
                                       WingDefinition.AileronFixedPanelEndX,
                                       True)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " lower fixed wing rear spar face",
                                       spanSign,
                                       WingDefinition.AileronFixedPanelEndX,
                                       False)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " upper rear hinge spar aft face",
                                       spanSign,
                                       WingDefinition.AileronRearSparEndX,
                                       True)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " lower rear hinge spar aft face",
                                       spanSign,
                                       WingDefinition.AileronRearSparEndX,
                                       False)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " upper aileron leading edge",
                                       spanSign,
                                       WingDefinition.AileronPanelStartX,
                                       True)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " lower aileron leading edge",
                                       spanSign,
                                       WingDefinition.AileronPanelStartX,
                                       False)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " upper aileron inner end cut",
                                         spanSign * WingDefinition.AileronInnerSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         True)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " lower aileron inner end cut",
                                         spanSign * WingDefinition.AileronInnerSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         False)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " upper aileron outer end cut",
                                         spanSign * WingDefinition.AileronOuterSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         True)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " lower aileron outer end cut",
                                         spanSign * WingDefinition.AileronOuterSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         False)
    End Sub

    Private Sub AddAileronSpanwiseSurfaceCurve(ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal targetSet As Object,
                                               ByVal curveName As String,
                                               ByVal spanSign As Double,
                                               ByVal globalX As Double,
                                               ByVal upperSurface As Boolean)
        Dim curvePoints As New List(Of PointCoordinate3D)()
        Dim segmentCount As Integer = 12

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim absoluteSpan As Double = WingDefinition.AileronInnerSpanPosition +
                ((WingDefinition.AileronOuterSpanPosition - WingDefinition.AileronInnerSpanPosition) * ratio)
            Dim spanPosition As Double = spanSign * absoluteSpan
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(spanPosition)
            Dim surfacePoint As AirfoilCoordinate =
                GetAirfoilSurfacePointAtGlobalX(chordLength,
                                                globalX,
                                                upperSurface)

            curvePoints.Add(New PointCoordinate3D(surfacePoint.X, spanPosition, surfacePoint.Y))
        Next

        CreateSplineThroughPoints(part, hybridShapeFactory, targetSet, curveName, curvePoints)
    End Sub

    Private Sub AddAileronChordwiseSurfaceCurve(ByVal part As Object,
                                                ByVal hybridShapeFactory As Object,
                                                ByVal targetSet As Object,
                                                ByVal curveName As String,
                                                ByVal spanPosition As Double,
                                                ByVal startX As Double,
                                                ByVal upperSurface As Boolean)
        Dim curvePoints As New List(Of PointCoordinate3D)()
        Dim segmentCount As Integer = 12
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(spanPosition)

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim globalX As Double = startX + ((chordLength - startX) * ratio)
            Dim surfacePoint As AirfoilCoordinate =
                GetAirfoilSurfacePointAtGlobalX(chordLength, globalX, upperSurface)

            curvePoints.Add(New PointCoordinate3D(surfacePoint.X, spanPosition, surfacePoint.Y))
        Next

        CreateSplineThroughPoints(part, hybridShapeFactory, targetSet, curveName, curvePoints)
    End Sub

    Private Sub CreateSplineThroughPoints(ByVal part As Object,
                                          ByVal hybridShapeFactory As Object,
                                          ByVal targetSet As Object,
                                          ByVal curveName As String,
                                          ByVal curvePoints As List(Of PointCoordinate3D))
        If curvePoints.Count < 2 Then
            Throw New InvalidOperationException("At least two points are required for " & curveName & ".")
        End If

        Dim curveSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(curveSpline)
        TrySetName(curveSpline, curveName)

        For pointIndex As Integer = 0 To curvePoints.Count - 1
            Dim coordinate As PointCoordinate3D = curvePoints(pointIndex)
            Dim curvePoint As Object = hybridShapeFactory.AddNewPointCoord(coordinate.X,
                                                                           coordinate.Y,
                                                                           coordinate.Z)
            TrySetName(curvePoint, curveName & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(curvePoint)
            curveSpline.AddPoint(part.CreateReferenceFromObject(curvePoint))
        Next

        targetSet.AppendHybridShape(curveSpline)
        RequireUpdateObject(part, curveSpline, curveName)
    End Sub

    Private Sub AddLoftSection(ByVal loft As Object,
                               ByVal profileReference As Object,
                               ByVal closingPointReference As Object)
        If closingPointReference Is Nothing Then
            Throw New InvalidOperationException("A loft section coupling point reference is required.")
        End If

        loft.AddSectionToLoft(profileReference, 1, closingPointReference)
    End Sub

    Private Sub AddPhysicalRibBody(ByVal part As Object,
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
        Dim globalCenterX As Double = WingDefinition.GetMainSparCenterXAtSpanPosition(station.SpanPosition)
        Dim radius As Double = WingDefinition.MainSparCutoutDiameter / 2.0

        If Not IsCircularCutoutInsideRibRegion(globalCenterX,
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

            If cutoutDiameter >= WingDefinition.RibLighteningCutoutMinimumDiameter Then
                Dim radius As Double = cutoutDiameter / 2.0
                Dim globalCenterX As Double =
                    WingDefinition.GetRibLighteningCutoutCenterX(station.SpanPosition, cutoutDefinition)

                If Not IsCircularCutoutInsideRibRegion(globalCenterX,
                                                       radius,
                                                       station.SpanPosition,
                                                       ribProfileRegion) Then
                    Continue For
                End If

                Dim cutoutCenter As AirfoilCoordinate =
                    ConvertGlobalPointToSketchPoint(globalCenterX,
                                                    station.SpanPosition,
                                                    WingDefinition.GetRibLighteningCutoutCenterZ(station.SpanPosition, cutoutDefinition),
                                                    sketchAxis)

                CreateSketchCircle(sketchFactory, cutoutCenter, radius)
            End If
        Next
    End Sub

    Private Function IsCircularCutoutInsideRibRegion(ByVal globalCenterX As Double,
                                                     ByVal radius As Double,
                                                     ByVal spanPosition As Double,
                                                     ByVal ribProfileRegion As RibProfileRegion) As Boolean
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return (globalCenterX + radius) <
                    (WingDefinition.GetAileronFixedPanelEndXAtSpanPosition(spanPosition) - 0.000001)
            Case Else
                Return True
        End Select
    End Function

    Private Sub CreateSketchCircle(ByVal sketchFactory As Object,
                                   ByVal centerPoint As AirfoilCoordinate,
                                   ByVal radius As Double)
        Try
            sketchFactory.CreateClosedCircle(centerPoint.X, centerPoint.Y, radius)
            Return
        Catch
        End Try

        CreatePolygonCircleSketchProfile(sketchFactory, centerPoint, radius, 48)
    End Sub

    Private Sub CreatePolygonCircleSketchProfile(ByVal sketchFactory As Object,
                                                 ByVal centerPoint As AirfoilCoordinate,
                                                 ByVal radius As Double,
                                                 ByVal segmentCount As Integer)
        If segmentCount < 12 Then
            segmentCount = 12
        End If

        Dim circlePoints As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = 0 To segmentCount - 1
            Dim angle As Double = (2.0 * Math.PI * CDbl(pointIndex)) / CDbl(segmentCount)
            circlePoints.Add(New AirfoilCoordinate(centerPoint.X + (Math.Cos(angle) * radius),
                                                   centerPoint.Y + (Math.Sin(angle) * radius)))
        Next

        For pointIndex As Integer = 0 To circlePoints.Count - 1
            Dim nextIndex As Integer = (pointIndex + 1) Mod circlePoints.Count
            CreateSketchLineIfDistinct(sketchFactory,
                                       circlePoints(pointIndex),
                                       circlePoints(nextIndex))
        Next
    End Sub

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

    Private Function ClipAirfoilProfileByX(ByVal airfoilCoordinates As List(Of AirfoilCoordinate),
                                           ByVal cutX As Double,
                                           ByVal keepForward As Boolean) As List(Of AirfoilCoordinate)
        Dim clippedCoordinates As New List(Of AirfoilCoordinate)()

        If airfoilCoordinates.Count < 3 Then
            Return clippedCoordinates
        End If

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim currentPoint As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim nextPoint As AirfoilCoordinate = airfoilCoordinates((pointIndex + 1) Mod airfoilCoordinates.Count)
            Dim currentInside As Boolean = IsPointInsideAileronClip(currentPoint, cutX, keepForward)
            Dim nextInside As Boolean = IsPointInsideAileronClip(nextPoint, cutX, keepForward)

            If currentInside AndAlso nextInside Then
                AddAirfoilCoordinateIfDistinct(clippedCoordinates, nextPoint)
            ElseIf currentInside AndAlso Not nextInside Then
                AddAirfoilCoordinateIfDistinct(clippedCoordinates,
                                               InterpolateAirfoilPointAtX(currentPoint, nextPoint, cutX))
            ElseIf Not currentInside AndAlso nextInside Then
                AddAirfoilCoordinateIfDistinct(clippedCoordinates,
                                               InterpolateAirfoilPointAtX(currentPoint, nextPoint, cutX))
                AddAirfoilCoordinateIfDistinct(clippedCoordinates, nextPoint)
            End If
        Next

        If clippedCoordinates.Count > 1 AndAlso
            AreSketchPointsCoincident(clippedCoordinates(0), clippedCoordinates(clippedCoordinates.Count - 1)) Then
            clippedCoordinates.RemoveAt(clippedCoordinates.Count - 1)
        End If

        Return clippedCoordinates
    End Function

    Private Function GetAirfoilSurfacePointAtGlobalX(ByVal chordLength As Double,
                                                     ByVal globalX As Double,
                                                     ByVal upperSurface As Boolean) As AirfoilCoordinate
        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength,
                                         WingDefinition.PointCountPerSurface,
                                         WingDefinition.AirfoilMaximumCamber,
                                         WingDefinition.AirfoilMaximumCamberPosition,
                                         WingDefinition.AirfoilMaximumThickness,
                                         True)
        Dim intersectionZValues As New List(Of Double)()

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim startPoint As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim endPoint As AirfoilCoordinate = airfoilCoordinates((pointIndex + 1) Mod airfoilCoordinates.Count)

            If SegmentCrossesGlobalX(startPoint, endPoint, globalX) Then
                intersectionZValues.Add(InterpolateAirfoilPointAtX(startPoint, endPoint, globalX).Y)
            End If
        Next

        If intersectionZValues.Count = 0 Then
            Throw New InvalidOperationException("No NACA 4415 surface point was found at X = " & globalX.ToString() & " mm.")
        End If

        Dim surfaceZ As Double = intersectionZValues(0)

        For Each intersectionZ As Double In intersectionZValues
            If upperSurface Then
                surfaceZ = Math.Max(surfaceZ, intersectionZ)
            Else
                surfaceZ = Math.Min(surfaceZ, intersectionZ)
            End If
        Next

        Return New AirfoilCoordinate(globalX, surfaceZ)
    End Function

    Private Function BuildFixedWingOutboardSkinProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                     0.0,
                                                     WingDefinition.AileronFixedPanelEndX)
    End Function

    Private Function BuildAileronSkinProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return RotateProfileCoordinatesToMaximumX(
            BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                  WingDefinition.AileronPanelStartX,
                                                  chordLength))
    End Function

    Private Function BuildAileronRearHingeSparProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                     WingDefinition.AileronFixedPanelEndX,
                                                     WingDefinition.AileronRearSparEndX)
    End Function

    Private Function BuildAirfoilSegmentProfileCoordinates(ByVal chordLength As Double,
                                                           ByVal startX As Double,
                                                           ByVal endX As Double) As List(Of AirfoilCoordinate)
        If startX < 0.0 OrElse endX > chordLength OrElse endX <= startX Then
            Throw New InvalidOperationException("Invalid airfoil segment X limits.")
        End If

        Dim profileCoordinates As New List(Of AirfoilCoordinate)()
        Dim segmentCount As Integer = 24

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim globalX As Double = startX + ((endX - startX) * ratio)
            AddAirfoilCoordinateIfDistinct(profileCoordinates,
                                           GetAirfoilSurfacePointAtGlobalX(chordLength,
                                                                          globalX,
                                                                          True))
        Next

        AddChordwiseCutEdgePoints(profileCoordinates, chordLength, endX, True, False)

        For pointIndex As Integer = segmentCount To 0 Step -1
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim globalX As Double = startX + ((endX - startX) * ratio)
            AddAirfoilCoordinateIfDistinct(profileCoordinates,
                                           GetAirfoilSurfacePointAtGlobalX(chordLength,
                                                                          globalX,
                                                                           False))
        Next

        AddChordwiseCutEdgePoints(profileCoordinates, chordLength, startX, False, True)

        Return profileCoordinates
    End Function

    Private Function RotateProfileCoordinatesToMaximumX(ByVal profileCoordinates As List(Of AirfoilCoordinate)) As List(Of AirfoilCoordinate)
        If profileCoordinates.Count = 0 Then
            Return profileCoordinates
        End If

        Dim maximumX As Double = profileCoordinates(0).X

        For Each coordinate As AirfoilCoordinate In profileCoordinates
            maximumX = Math.Max(maximumX, coordinate.X)
        Next

        Dim startIndex As Integer = 0

        For pointIndex As Integer = 0 To profileCoordinates.Count - 1
            If Math.Abs(profileCoordinates(pointIndex).X - maximumX) < 0.000001 Then
                startIndex = pointIndex
                Exit For
            End If
        Next

        If startIndex = 0 Then
            Return profileCoordinates
        End If

        Dim rotatedCoordinates As New List(Of AirfoilCoordinate)()

        For offset As Integer = 0 To profileCoordinates.Count - 1
            rotatedCoordinates.Add(profileCoordinates((startIndex + offset) Mod profileCoordinates.Count))
        Next

        Return rotatedCoordinates
    End Function

    Private Sub AddChordwiseCutEdgePoints(ByVal profileCoordinates As List(Of AirfoilCoordinate),
                                          ByVal chordLength As Double,
                                          ByVal cutX As Double,
                                          ByVal startAtUpperSurface As Boolean,
                                          ByVal endAtUpperSurface As Boolean)
        Dim upperCutPoint As AirfoilCoordinate =
            GetAirfoilSurfacePointAtGlobalX(chordLength,
                                            cutX,
                                            True)
        Dim lowerCutPoint As AirfoilCoordinate =
            GetAirfoilSurfacePointAtGlobalX(chordLength,
                                            cutX,
                                            False)
        Dim cutSegmentCount As Integer = 12
        Dim startPoint As AirfoilCoordinate = If(startAtUpperSurface, upperCutPoint, lowerCutPoint)
        Dim endPoint As AirfoilCoordinate = If(endAtUpperSurface, upperCutPoint, lowerCutPoint)

        For pointIndex As Integer = 1 To cutSegmentCount - 1
            Dim ratio As Double = CDbl(pointIndex) / CDbl(cutSegmentCount)
            Dim cutZ As Double = startPoint.Y + ((endPoint.Y - startPoint.Y) * ratio)

            AddAirfoilCoordinateIfDistinct(profileCoordinates,
                                           New AirfoilCoordinate(cutX, cutZ))
        Next
    End Sub

    Private Function SegmentCrossesGlobalX(ByVal startPoint As AirfoilCoordinate,
                                           ByVal endPoint As AirfoilCoordinate,
                                           ByVal globalX As Double) As Boolean
        Dim minimumX As Double = Math.Min(startPoint.X, endPoint.X)
        Dim maximumX As Double = Math.Max(startPoint.X, endPoint.X)

        Return globalX >= (minimumX - 0.000001) AndAlso
            globalX <= (maximumX + 0.000001)
    End Function

    Private Function IsPointInsideAileronClip(ByVal point As AirfoilCoordinate,
                                              ByVal cutX As Double,
                                              ByVal keepForward As Boolean) As Boolean
        If keepForward Then
            Return point.X <= (cutX + 0.000001)
        End If

        Return point.X >= (cutX - 0.000001)
    End Function

    Private Function InterpolateAirfoilPointAtX(ByVal startPoint As AirfoilCoordinate,
                                                ByVal endPoint As AirfoilCoordinate,
                                                ByVal cutX As Double) As AirfoilCoordinate
        Dim deltaX As Double = endPoint.X - startPoint.X

        If Math.Abs(deltaX) < 0.000001 Then
            Return New AirfoilCoordinate(cutX, startPoint.Y)
        End If

        Dim interpolationRatio As Double = (cutX - startPoint.X) / deltaX
        Dim interpolatedY As Double = startPoint.Y +
            ((endPoint.Y - startPoint.Y) * interpolationRatio)

        Return New AirfoilCoordinate(cutX, interpolatedY)
    End Function

    Private Sub AddAirfoilCoordinateIfDistinct(ByVal coordinates As List(Of AirfoilCoordinate),
                                               ByVal coordinate As AirfoilCoordinate)
        If coordinates.Count = 0 OrElse
            Not AreSketchPointsCoincident(coordinates(coordinates.Count - 1), coordinate) Then
            coordinates.Add(coordinate)
        End If
    End Sub

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

    Private Sub CreateClosedPolylineRibSketchProfile(ByVal sketchFactory As Object,
                                                     ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        CreatePolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        CreateSketchLineIfDistinct(sketchFactory,
                                   sketchCoordinates(sketchCoordinates.Count - 1),
                                   sketchCoordinates(0))
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

    Private Function AddMainSparBody(ByVal part As Object,
                                      ByVal hybridShapeFactory As Object,
                                      ByVal shapeFactory As Object,
                                      ByVal sparReferenceSet As Object) As Object
        If WingDefinition.MainSparInnerDiameter <= 0.0 Then
            Throw New InvalidOperationException("The main spar wall thickness leaves no hollow inside diameter.")
        End If

        Dim sparBody As Object = part.Bodies.Add()
        TrySetName(sparBody, "Main spar 30 percent chord hollow tube")
        TrySetInWorkObject(part, sparBody)

        Dim profileSketch As Object = CreateMainSparProfileSketch(part, sparBody)

        Dim rightPath As Object = CreateMainSparPathLine(part,
                                                         hybridShapeFactory,
                                                         sparReferenceSet,
                                                         "Right main spar 30 percent chord path",
                                                         0.0,
                                                         WingDefinition.HalfSpan)
        Dim leftPath As Object = CreateMainSparPathLine(part,
                                                        hybridShapeFactory,
                                                        sparReferenceSet,
                                                        "Left main spar 30 percent chord path",
                                                        0.0,
                                                        -WingDefinition.HalfSpan)

        CreateMainSparRibFeature(part,
                                 shapeFactory,
                                 sparBody,
                                 profileSketch,
                                 rightPath,
                                 "Right main spar hollow tube")
        CreateMainSparRibFeature(part,
                                 shapeFactory,
                                 sparBody,
                                 profileSketch,
                                 leftPath,
                                 "Left main spar hollow tube")

        RequireUpdatePart(part, "main spar hollow tube")

        Return sparBody
    End Function

    Private Function CreateMainSparProfileSketch(ByVal part As Object,
                                                 ByVal sparBody As Object) As Object
        Dim sketches As Object = sparBody.Sketches
        Dim centerPlane As Object = part.OriginElements.PlaneZX
        Dim profileSketch As Object = CreateSketchOnPlane(part, sketches, centerPlane)
        TrySetName(profileSketch, "Main spar hollow tube profile")
        TrySetInWorkObject(part, profileSketch)

        Dim sketchAxis As SketchAxisData = GetSketchAxisData(profileSketch, 0.0)
        Dim sketchFactory As Object = profileSketch.OpenEdition()
        Dim sparCenter As AirfoilCoordinate =
            ConvertGlobalPointToSketchPoint(WingDefinition.GetMainSparCenterXAtSpanPosition(0.0),
                                            0.0,
                                            WingDefinition.GetMainSparCenterZAtSpanPosition(0.0),
                                            sketchAxis)

        CreateSketchCircle(sketchFactory,
                           sparCenter,
                           WingDefinition.MainSparOuterDiameter / 2.0)
        CreateSketchCircle(sketchFactory,
                           sparCenter,
                           WingDefinition.MainSparInnerDiameter / 2.0)

        profileSketch.CloseEdition()
        RequireUpdateObject(part, profileSketch, "main spar hollow tube profile")
        RequireUpdatePart(part, "main spar hollow tube profile")

        Return profileSketch
    End Function

    Private Function CreateMainSparPathLine(ByVal part As Object,
                                            ByVal hybridShapeFactory As Object,
                                            ByVal targetSet As Object,
                                            ByVal pathName As String,
                                            ByVal startSpanPosition As Double,
                                            ByVal endSpanPosition As Double) As Object
        Dim startPoint As Object = CreateMainSparPathPoint(part,
                                                           hybridShapeFactory,
                                                           targetSet,
                                                           pathName & " start",
                                                           startSpanPosition)
        Dim endPoint As Object = CreateMainSparPathPoint(part,
                                                         hybridShapeFactory,
                                                         targetSet,
                                                         pathName & " end",
                                                         endSpanPosition)

        Dim startReference As Object = part.CreateReferenceFromObject(startPoint)
        Dim endReference As Object = part.CreateReferenceFromObject(endPoint)
        Dim sparPath As Object = hybridShapeFactory.AddNewLinePtPt(startReference, endReference)
        TrySetName(sparPath, pathName)
        targetSet.AppendHybridShape(sparPath)
        RequireUpdateObject(part, sparPath, pathName)

        Return sparPath
    End Function

    Private Function CreateMainSparPathPoint(ByVal part As Object,
                                             ByVal hybridShapeFactory As Object,
                                             ByVal targetSet As Object,
                                             ByVal pointName As String,
                                             ByVal spanPosition As Double) As Object
        Dim sparPoint As Object =
            hybridShapeFactory.AddNewPointCoord(WingDefinition.GetMainSparCenterXAtSpanPosition(spanPosition),
                                                spanPosition,
                                                WingDefinition.GetMainSparCenterZAtSpanPosition(spanPosition))
        TrySetName(sparPoint, pointName)
        targetSet.AppendHybridShape(sparPoint)
        RequireUpdateObject(part, sparPoint, pointName)

        Return sparPoint
    End Function

    Private Function CreateMainSparRibFeature(ByVal part As Object,
                                              ByVal shapeFactory As Object,
                                              ByVal sparBody As Object,
                                              ByVal profileSketch As Object,
                                              ByVal sparPath As Object,
                                              ByVal sparName As String) As Object
        TrySetInWorkObject(part, sparBody)

        Try
            Dim profileReference As Object = part.CreateReferenceFromObject(profileSketch)
            Dim pathReference As Object = part.CreateReferenceFromObject(sparPath)
            Dim sparFeature As Object = shapeFactory.AddNewRibFromRef(profileReference, pathReference)
            TrySetName(sparFeature, sparName)
            RequireUpdateObject(part, sparFeature, sparName)

            Return sparFeature
        Catch firstException As Exception
            Try
                Dim sparFeature As Object = shapeFactory.AddNewRib(profileSketch, sparPath)
                TrySetName(sparFeature, sparName)
                RequireUpdateObject(part, sparFeature, sparName)

                Return sparFeature
            Catch
                Throw New InvalidOperationException("CATIA could not create the " & sparName & ". Check that the main spar profile is closed and that the spar path intersects the profile center.", firstException)
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
        Return ConvertGlobalPointToSketchPoint(airfoilPoint.X,
                                               spanPosition,
                                               airfoilPoint.Y,
                                               sketchAxis)
    End Function

    Private Function ConvertGlobalPointToSketchPoint(ByVal globalX As Double,
                                                     ByVal globalY As Double,
                                                     ByVal globalZ As Double,
                                                     ByVal sketchAxis As SketchAxisData) As AirfoilCoordinate
        Dim deltaX As Double = globalX - sketchAxis.OriginX
        Dim deltaY As Double = globalY - sketchAxis.OriginY
        Dim deltaZ As Double = globalZ - sketchAxis.OriginZ

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
