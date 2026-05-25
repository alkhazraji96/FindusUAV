Imports INFITF
Imports HybridShapeTypeLib
Imports System.Collections.Generic

Friend Structure PointCoordinate3D
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

Friend Module WingSkinGeometry
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

    Friend Function AddAirfoilStationProfile(ByVal part As Object,
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
        Dim constructionGeometry As New List(Of Object)()

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim airfoilPoint As Object =
                hybridShapeFactory.AddNewPointCoord(WingDefinition.GetGlobalXAtSpanPosition(station.SpanPosition,
                                                                                            coordinate.X),
                                                    station.SpanPosition,
                                                    WingDefinition.GetGlobalZAtSpanPosition(station.SpanPosition,
                                                                                            coordinate.Y))
            TrySetName(airfoilPoint, station.Name & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(airfoilPoint)
            constructionGeometry.Add(airfoilPoint)

            Dim pointReference As Object = part.CreateReferenceFromObject(airfoilPoint)

            If pointIndex = 0 Then
                closingPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        targetSet.AppendHybridShape(profileSpline)
        constructionGeometry.Add(profileSpline)

        Return New WingStationProfile(station.Name,
                                      profileSpline,
                                      closingPointReference,
                                      constructionGeometry)
    End Function

    Friend Function CreateOuterWingSkinFromProfiles(ByVal part As Object,
                                                     ByVal hybridShapeFactory As Object,
                                                     ByVal targetSet As Object,
                                                     ByVal stationProfiles As List(Of WingStationProfile),
                                                     Optional ByVal skinName As String = Nothing,
                                                     Optional ByVal partDocument As Object = Nothing,
                                                     Optional ByVal hideProfileConstruction As Boolean = False) As Object
        If stationProfiles.Count < 2 Then
            Throw New InvalidOperationException("At least two airfoil station profiles are required to create the wing skin.")
        End If

        Dim effectiveSkinName As String = skinName

        If String.IsNullOrWhiteSpace(effectiveSkinName) Then
            effectiveSkinName = GetDefaultOuterWingSkinName()
        End If

        Dim outerSkinLoft As HybridShapeLoft =
            CType(hybridShapeFactory.AddNewLoft(), HybridShapeLoft)
        TrySetName(outerSkinLoft, effectiveSkinName)
        TrySetLoftOptions(outerSkinLoft)

        For Each stationProfile As WingStationProfile In stationProfiles
            Dim profileReference As Reference =
                CType(part.CreateReferenceFromObject(stationProfile.ProfileSpline), Reference)
            AddLoftSection(outerSkinLoft, profileReference, stationProfile.ClosingPointReference)
        Next

        targetSet.AppendHybridShape(outerSkinLoft)
        RequireUpdateObject(part, outerSkinLoft, effectiveSkinName)

        If hideProfileConstruction AndAlso partDocument IsNot Nothing Then
            HideWingStationProfileConstruction(partDocument, stationProfiles)
        End If

        Return outerSkinLoft
    End Function

    Friend Sub CreateSplitSkinSurfaces(ByVal partDocument As Object,
                                        ByVal part As Object,
                                        ByVal hybridShapeFactory As Object,
                                        ByVal shapeFactory As Object,
                                        ByVal fixedSkinSet As Object,
                                        ByVal aileronSkinSet As Object,
                                        ByVal stations As List(Of WingStation))
        CreateCenterFixedWingSkinSurface(partDocument,
                                         part,
                                         hybridShapeFactory,
                                         fixedSkinSet,
                                         stations,
                                         True,
                                         "Center fixed wing upper skin")
        CreateCenterFixedWingSkinSurface(partDocument,
                                         part,
                                         hybridShapeFactory,
                                         fixedSkinSet,
                                         stations,
                                         False,
                                         "Center fixed wing lower skin")
        AddOutboardFixedWingSkinSurfaces(partDocument, part, hybridShapeFactory, fixedSkinSet, -1.0, "Left")
        AddOutboardFixedWingSkinSurfaces(partDocument, part, hybridShapeFactory, fixedSkinSet, 1.0, "Right")

        Dim leftAileronSkins As List(Of Object) =
            AddAileronSkinSurfaces(partDocument, part, hybridShapeFactory, shapeFactory, aileronSkinSet, -1.0, "Left")
        Dim rightAileronSkins As List(Of Object) =
            AddAileronSkinSurfaces(partDocument, part, hybridShapeFactory, shapeFactory, aileronSkinSet, 1.0, "Right")

        For Each aileronSkin As Object In leftAileronSkins
            TrySetObjectColor(partDocument, aileronSkin, 255, 145, 0)
        Next

        For Each aileronSkin As Object In rightAileronSkins
            TrySetObjectColor(partDocument, aileronSkin, 255, 145, 0)
        Next

        RequireUpdatePart(part, "split fixed wing and aileron skin surfaces")
    End Sub

    Private Function CreateCenterFixedWingSkinSurface(ByVal partDocument As Object,
                                                      ByVal part As Object,
                                                      ByVal hybridShapeFactory As Object,
                                                      ByVal targetSet As Object,
                                                      ByVal stations As List(Of WingStation),
                                                      ByVal upperSurface As Boolean,
                                                      ByVal skinName As String) As Object
        Dim centerSkinProfiles As New List(Of WingStationProfile)()

        centerSkinProfiles.Add(AddOpenAirfoilSurfaceProfile(part,
                                                            hybridShapeFactory,
                                                            targetSet,
                                                            New WingStation("Aileron_Left_Inner_Boundary",
                                                                            -WingDefinition.AileronInnerSpanPosition),
                                                            upperSurface))

        For Each station As WingStation In stations
            If Math.Abs(station.SpanPosition) <
                (WingDefinition.AileronInnerSpanPosition - 0.000001) Then
                centerSkinProfiles.Add(AddOpenAirfoilSurfaceProfile(part,
                                                                    hybridShapeFactory,
                                                                    targetSet,
                                                                    station,
                                                                    upperSurface))
            End If
        Next

        centerSkinProfiles.Add(AddOpenAirfoilSurfaceProfile(part,
                                                            hybridShapeFactory,
                                                            targetSet,
                                                            New WingStation("Aileron_Right_Inner_Boundary",
                                                                            WingDefinition.AileronInnerSpanPosition),
                                                            upperSurface))

        Return CreateOuterWingSkinFromProfiles(part,
                                               hybridShapeFactory,
                                               targetSet,
                                               centerSkinProfiles,
                                               skinName,
                                               partDocument,
                                               True)
    End Function

    Private Function AddOpenAirfoilSurfaceProfile(ByVal part As Object,
                                                  ByVal hybridShapeFactory As Object,
                                                  ByVal targetSet As Object,
                                                  ByVal station As WingStation,
                                                  ByVal upperSurface As Boolean) As WingStationProfile
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)
        Dim surfaceLabel As String = If(upperSurface, "upper", "lower")
        Dim pointLabel As String = If(upperSurface, "Upper", "Lower")

        Return AddOpenSplitSkinProfile(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       "Center fixed wing " & surfaceLabel & " profile " & station.Name,
                                       "Center_Fixed_Wing_" & pointLabel & "_" & station.Name,
                                       station.SpanPosition,
                                       BuildAirfoilSurfaceProfileCoordinates(chordLength, upperSurface))
    End Function

    Private Sub AddOutboardFixedWingSkinSurfaces(ByVal partDocument As Object,
                                                 ByVal part As Object,
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
                                        sideName & " outboard fixed wing skin",
                                        partDocument,
                                        True)
    End Sub

    Private Function AddAileronSkinSurfaces(ByVal partDocument As Object,
                                            ByVal part As Object,
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
                                            sideName & " aileron skin surface",
                                            partDocument,
                                            True)
        Dim aileronSolid As Object =
            CreateClosedSurfaceSolid(part,
                                     shapeFactory,
                                     aileronSurface,
                                     sideName & " physical aileron")
        aileronSkins.Add(aileronSurface)
        aileronSkins.Add(aileronSolid)

        Return aileronSkins
    End Function

    Friend Sub AddAileronRearHingeSpars(ByVal part As Object,
                                         ByVal hybridShapeFactory As Object,
                                         ByVal shapeFactory As Object,
                                         ByVal targetSet As Object)
        AddAileronRearHingeSparForSide(part, hybridShapeFactory, shapeFactory, targetSet, -1.0, "Left")
        AddAileronRearHingeSparForSide(part, hybridShapeFactory, shapeFactory, targetSet, 1.0, "Right")
        RequireUpdatePart(part, "aileron rear hinge spars")
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
        Dim constructionGeometry As New List(Of Object)()

        For pointIndex As Integer = 0 To skinCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = skinCoordinates(pointIndex)
            Dim profilePoint As Object =
                hybridShapeFactory.AddNewPointCoord(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                            coordinate.X),
                                                    spanPosition,
                                                    WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                            coordinate.Y))
            TrySetName(profilePoint,
                       pointPrefix & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(profilePoint)
            constructionGeometry.Add(profilePoint)

            Dim pointReference As Object = part.CreateReferenceFromObject(profilePoint)

            If pointIndex = 0 Then
                closingPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        targetSet.AppendHybridShape(profileSpline)
        constructionGeometry.Add(profileSpline)

        Return New WingStationProfile(profileName,
                                      profileSpline,
                                      closingPointReference,
                                      constructionGeometry)
    End Function

    Private Function AddOpenSplitSkinProfile(ByVal part As Object,
                                             ByVal hybridShapeFactory As Object,
                                             ByVal targetSet As Object,
                                             ByVal profileName As String,
                                             ByVal pointPrefix As String,
                                             ByVal spanPosition As Double,
                                             ByVal skinCoordinates As List(Of AirfoilCoordinate)) As WingStationProfile
        If skinCoordinates.Count < 2 Then
            Throw New InvalidOperationException("At least two points are required for " & profileName & ".")
        End If

        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline, False)
        TrySetName(profileSpline, profileName)
        Dim constructionGeometry As New List(Of Object)()

        For pointIndex As Integer = 0 To skinCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = skinCoordinates(pointIndex)
            Dim profilePoint As Object =
                hybridShapeFactory.AddNewPointCoord(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                            coordinate.X),
                                                    spanPosition,
                                                    WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                            coordinate.Y))
            TrySetName(profilePoint,
                       pointPrefix & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(profilePoint)
            constructionGeometry.Add(profilePoint)

            profileSpline.AddPoint(part.CreateReferenceFromObject(profilePoint))
        Next

        targetSet.AppendHybridShape(profileSpline)
        constructionGeometry.Add(profileSpline)

        Return New WingStationProfile(profileName,
                                      profileSpline,
                                      Nothing,
                                      constructionGeometry)
    End Function

    Friend Sub AddAileronCutReferenceGeometry(ByVal part As Object,
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
                                               ByVal localX As Double,
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
                GetAirfoilSurfacePointAtLocalX(chordLength,
                                               localX,
                                               upperSurface)

            curvePoints.Add(New PointCoordinate3D(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                          surfacePoint.X),
                                                  spanPosition,
                                                  WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                          surfacePoint.Y)))
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
            Dim localX As Double = startX + ((chordLength - startX) * ratio)
            Dim surfacePoint As AirfoilCoordinate =
                GetAirfoilSurfacePointAtLocalX(chordLength, localX, upperSurface)

            curvePoints.Add(New PointCoordinate3D(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                          surfacePoint.X),
                                                  spanPosition,
                                                  WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                          surfacePoint.Y)))
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

    Private Sub AddLoftSection(ByVal loft As HybridShapeLoft,
                               ByVal profileReference As Reference,
                               ByVal closingPointReference As Object)
        Dim couplingReference As Reference = CType(Nothing, Reference)

        If closingPointReference IsNot Nothing Then
            couplingReference = CType(closingPointReference, Reference)
        End If

        loft.AddSectionToLoft(profileReference, 1, couplingReference)
    End Sub
End Module
