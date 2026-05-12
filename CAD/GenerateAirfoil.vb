Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Class GenerateAirfoil
    Private Const DefaultChordLength As Double = 100.0
    Private Const DefaultPointCountPerSurface As Integer = 81
    Private Const DefaultPadLength As Double = 3.0
    Private Const WingFullSpan As Double = 3543.65
    Private Const WingHalfSpan As Double = 1771.825
    Private Const WingRootChord As Double = 586.0
    Private Const WingTipChord As Double = 374.0
    Private Const WingRibCountPerSide As Integer = 14
    Private Const WingRibThickness As Double = 3.0
    Private Const WingPointCountPerSurface As Integer = 41
    Private Const WingAirfoilMaximumCamber As Double = 0.04
    Private Const WingAirfoilMaximumCamberPosition As Double = 0.4
    Private Const WingAirfoilMaximumThickness As Double = 0.15

    Public Sub Generate()
        Run()
    End Sub

    Public Shared Sub Run()
        CreateWingStage3OuterWingSkin()
    End Sub

    Public Shared Function CreateWingStage3OuterWingSkin() As Object
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

        AddStagePlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildWingStations()
            stationProfiles.Add(AddWingAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
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

    Public Shared Function CreateWingStage2AirfoilStations() As Object
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

        AddStagePlanformGeometry(part, hybridShapeFactory, planformSet)

        For Each station As WingStation In BuildWingStations()
            AddWingAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station)
        Next

        part.Update()
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Public Shared Function CreateWingStage1Planform() As Object
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

        AddStagePlanformGeometry(part, hybridShapeFactory, stageSet)

        part.Update()
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Private Shared Sub AddStagePlanformGeometry(ByVal part As Object,
                                                ByVal hybridShapeFactory As Object,
                                                ByVal targetSet As Object)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Leading edge full span",
                        0.0, -WingHalfSpan, 0.0, WingHalfSpan)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Right tapered trailing edge",
                        WingRootChord, 0.0, WingTipChord, WingHalfSpan)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Left tapered trailing edge",
                        WingRootChord, 0.0, WingTipChord, -WingHalfSpan)

        AddRibStationLine(part, hybridShapeFactory, targetSet, "Rib_00_Center", 0.0)

        For ribIndex As Integer = 1 To WingRibCountPerSide
            Dim stationY As Double = (WingHalfSpan / CDbl(WingRibCountPerSide)) * CDbl(ribIndex)

            AddRibStationLine(part, hybridShapeFactory, targetSet,
                              "Rib_R" & ribIndex.ToString("00"), stationY)
            AddRibStationLine(part, hybridShapeFactory, targetSet,
                              "Rib_L" & ribIndex.ToString("00"), -stationY)
        Next
    End Sub

    Private Shared Sub AddRibStationLine(ByVal part As Object,
                                         ByVal hybridShapeFactory As Object,
                                         ByVal targetSet As Object,
                                         ByVal ribName As String,
                                         ByVal spanPosition As Double)
        Dim localChord As Double = GetWingChordAtSpanPosition(spanPosition)

        AddPlanformLine(part, hybridShapeFactory, targetSet, ribName,
                        0.0, spanPosition, localChord, spanPosition)
    End Sub

    Private Shared Sub AddPlanformLine(ByVal part As Object,
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

    Private Shared Function GetWingChordAtSpanPosition(ByVal spanPosition As Double) As Double
        Dim spanRatio As Double = Math.Abs(spanPosition) / WingHalfSpan
        Return WingRootChord + ((WingTipChord - WingRootChord) * spanRatio)
    End Function

    Private Shared Function BuildWingStations() As List(Of WingStation)
        Dim stations As New List(Of WingStation)()
        Dim stationSpacing As Double = WingHalfSpan / CDbl(WingRibCountPerSide)

        For ribIndex As Integer = WingRibCountPerSide To 1 Step -1
            stations.Add(New WingStation("Rib_L" & ribIndex.ToString("00"), -stationSpacing * CDbl(ribIndex)))
        Next

        stations.Add(New WingStation("Rib_00_Center", 0.0))

        For ribIndex As Integer = 1 To WingRibCountPerSide
            stations.Add(New WingStation("Rib_R" & ribIndex.ToString("00"), stationSpacing * CDbl(ribIndex)))
        Next

        Return stations
    End Function

    Private Shared Function AddWingAirfoilStationProfile(ByVal part As Object,
                                                         ByVal hybridShapeFactory As Object,
                                                         ByVal targetSet As Object,
                                                         ByVal station As WingStation) As WingStationProfile
        Dim chordLength As Double = GetWingChordAtSpanPosition(station.SpanPosition)

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            BuildNacaCoordinates(chordLength,
                                 WingPointCountPerSurface,
                                 WingAirfoilMaximumCamber,
                                 WingAirfoilMaximumCamberPosition,
                                 WingAirfoilMaximumThickness,
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

    Private Shared Function CreateOuterWingSkinFromProfiles(ByVal part As Object,
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

    Private Shared Sub AddLoftSection(ByVal loft As Object,
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

    Private Shared Function BuildNacaCoordinates(ByVal chordLength As Double,
                                                 ByVal pointCountPerSurface As Integer,
                                                 ByVal maximumCamber As Double,
                                                 ByVal maximumCamberPosition As Double,
                                                 ByVal maximumThickness As Double,
                                                 ByVal closedTrailingEdge As Boolean) As List(Of AirfoilCoordinate)
        Dim upperSurface As New List(Of AirfoilCoordinate)()
        Dim lowerSurface As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = 0 To pointCountPerSurface - 1
            Dim beta As Double = Math.PI * CDbl(pointIndex) / CDbl(pointCountPerSurface - 1)
            Dim normalizedX As Double = 0.5 * (1.0 - Math.Cos(beta))

            Dim upperPoint As AirfoilCoordinate = Nothing
            Dim lowerPoint As AirfoilCoordinate = Nothing
            CalculateNacaSurfacePoints(normalizedX,
                                       chordLength,
                                       maximumCamber,
                                       maximumCamberPosition,
                                       maximumThickness,
                                       closedTrailingEdge,
                                       upperPoint,
                                       lowerPoint)

            upperSurface.Add(upperPoint)
            lowerSurface.Add(lowerPoint)
        Next

        Dim profileCoordinates As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = upperSurface.Count - 1 To 0 Step -1
            profileCoordinates.Add(upperSurface(pointIndex))
        Next

        For pointIndex As Integer = 1 To lowerSurface.Count - 1
            profileCoordinates.Add(lowerSurface(pointIndex))
        Next

        Return profileCoordinates
    End Function

    Private Shared Sub CalculateNacaSurfacePoints(ByVal normalizedX As Double,
                                                  ByVal chordLength As Double,
                                                  ByVal maximumCamber As Double,
                                                  ByVal maximumCamberPosition As Double,
                                                  ByVal maximumThickness As Double,
                                                  ByVal closedTrailingEdge As Boolean,
                                                  ByRef upperPoint As AirfoilCoordinate,
                                                  ByRef lowerPoint As AirfoilCoordinate)
        Dim trailingEdgeCoefficient As Double = If(closedTrailingEdge, 0.1036, 0.1015)

        Dim thickness As Double = 5.0 * maximumThickness *
            ((0.2969 * Math.Sqrt(normalizedX)) -
             (0.126 * normalizedX) -
             (0.3516 * Math.Pow(normalizedX, 2.0)) +
             (0.2843 * Math.Pow(normalizedX, 3.0)) -
             (trailingEdgeCoefficient * Math.Pow(normalizedX, 4.0)))

        Dim camber As Double = 0.0
        Dim camberSlope As Double = 0.0

        If maximumCamber > 0.0 AndAlso maximumCamberPosition > 0.0 Then
            If normalizedX <= maximumCamberPosition Then
                camber = (maximumCamber / Math.Pow(maximumCamberPosition, 2.0)) *
                    ((2.0 * maximumCamberPosition * normalizedX) - Math.Pow(normalizedX, 2.0))
                camberSlope = ((2.0 * maximumCamber) / Math.Pow(maximumCamberPosition, 2.0)) *
                    (maximumCamberPosition - normalizedX)
            Else
                camber = (maximumCamber / Math.Pow(1.0 - maximumCamberPosition, 2.0)) *
                    ((1.0 - (2.0 * maximumCamberPosition)) +
                     (2.0 * maximumCamberPosition * normalizedX) -
                     Math.Pow(normalizedX, 2.0))
                camberSlope = ((2.0 * maximumCamber) / Math.Pow(1.0 - maximumCamberPosition, 2.0)) *
                    (maximumCamberPosition - normalizedX)
            End If
        End If

        Dim theta As Double = Math.Atan(camberSlope)

        Dim upperX As Double = normalizedX - (thickness * Math.Sin(theta))
        Dim upperY As Double = camber + (thickness * Math.Cos(theta))
        Dim lowerX As Double = normalizedX + (thickness * Math.Sin(theta))
        Dim lowerY As Double = camber - (thickness * Math.Cos(theta))

        upperPoint = New AirfoilCoordinate(upperX * chordLength, upperY * chordLength)
        lowerPoint = New AirfoilCoordinate(lowerX * chordLength, lowerY * chordLength)
    End Sub

    Public Shared Function CreateNaca2412Part(Optional ByVal chordLength As Double = DefaultChordLength,
                                              Optional ByVal pointCountPerSurface As Integer = DefaultPointCountPerSurface,
                                              Optional ByVal padLength As Double = DefaultPadLength) As Object
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

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) = BuildNaca2412Coordinates(chordLength, pointCountPerSurface)
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

    Private Shared Function GetOrCreateCatiaApplication() As Object
        Try
            Return Marshal.GetActiveObject("CATIA.Application")
        Catch
            Try
                Return CreateObject("CATIA.Application")
            Catch ex As Exception
                Throw New InvalidOperationException("CATIA V5 could not be found or started.", ex)
            End Try
        End Try
    End Function

    Private Shared Function GetMainBody(ByVal part As Object) As Object
        Try
            Return part.MainBody
        Catch
        End Try

        Try
            Return part.Bodies.Item("PartBody")
        Catch
        End Try

        Return part.Bodies.Item(1)
    End Function

    Private Shared Function CreateAirfoilPadSketch(ByVal part As Object,
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

    Private Shared Function CreateAirfoilPad(ByVal part As Object,
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

    Private Shared Function BuildNaca2412Coordinates(ByVal chordLength As Double,
                                                     ByVal pointCountPerSurface As Integer) As List(Of AirfoilCoordinate)
        Dim upperSurface As New List(Of AirfoilCoordinate)()
        Dim lowerSurface As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = 0 To pointCountPerSurface - 1
            Dim beta As Double = Math.PI * CDbl(pointIndex) / CDbl(pointCountPerSurface - 1)
            Dim normalizedX As Double = 0.5 * (1.0 - Math.Cos(beta))

            Dim upperPoint As AirfoilCoordinate = Nothing
            Dim lowerPoint As AirfoilCoordinate = Nothing
            CalculateNaca2412SurfacePoints(normalizedX, chordLength, upperPoint, lowerPoint)

            upperSurface.Add(upperPoint)
            lowerSurface.Add(lowerPoint)
        Next

        Dim profileCoordinates As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = upperSurface.Count - 1 To 0 Step -1
            profileCoordinates.Add(upperSurface(pointIndex))
        Next

        For pointIndex As Integer = 1 To lowerSurface.Count - 1
            profileCoordinates.Add(lowerSurface(pointIndex))
        Next

        Return profileCoordinates
    End Function

    Private Shared Sub CalculateNaca2412SurfacePoints(ByVal normalizedX As Double,
                                                      ByVal chordLength As Double,
                                                      ByRef upperPoint As AirfoilCoordinate,
                                                      ByRef lowerPoint As AirfoilCoordinate)
        Const maximumCamber As Double = 0.02
        Const maximumCamberPosition As Double = 0.4
        Const maximumThickness As Double = 0.12

        Dim thickness As Double = 5.0 * maximumThickness *
            ((0.2969 * Math.Sqrt(normalizedX)) -
             (0.126 * normalizedX) -
             (0.3516 * Math.Pow(normalizedX, 2.0)) +
             (0.2843 * Math.Pow(normalizedX, 3.0)) -
             (0.1015 * Math.Pow(normalizedX, 4.0)))

        Dim camber As Double
        Dim camberSlope As Double

        If normalizedX <= maximumCamberPosition Then
            camber = (maximumCamber / Math.Pow(maximumCamberPosition, 2.0)) *
                ((2.0 * maximumCamberPosition * normalizedX) - Math.Pow(normalizedX, 2.0))
            camberSlope = ((2.0 * maximumCamber) / Math.Pow(maximumCamberPosition, 2.0)) *
                (maximumCamberPosition - normalizedX)
        Else
            camber = (maximumCamber / Math.Pow(1.0 - maximumCamberPosition, 2.0)) *
                ((1.0 - (2.0 * maximumCamberPosition)) +
                 (2.0 * maximumCamberPosition * normalizedX) -
                 Math.Pow(normalizedX, 2.0))
            camberSlope = ((2.0 * maximumCamber) / Math.Pow(1.0 - maximumCamberPosition, 2.0)) *
                (maximumCamberPosition - normalizedX)
        End If

        Dim theta As Double = Math.Atan(camberSlope)

        Dim upperX As Double = normalizedX - (thickness * Math.Sin(theta))
        Dim upperY As Double = camber + (thickness * Math.Cos(theta))
        Dim lowerX As Double = normalizedX + (thickness * Math.Sin(theta))
        Dim lowerY As Double = camber - (thickness * Math.Cos(theta))

        upperPoint = New AirfoilCoordinate(upperX * chordLength, upperY * chordLength)
        lowerPoint = New AirfoilCoordinate(lowerX * chordLength, lowerY * chordLength)
    End Sub

    Private Shared Sub TrySetSplineOptions(ByVal profileSpline As Object,
                                           Optional ByVal closeSpline As Boolean = False)
        Try
            profileSpline.SetSplineType(0)
        Catch
        End Try

        Try
            profileSpline.SetClosing(If(closeSpline, 1, 0))
        Catch
        End Try
    End Sub

    Private Shared Sub TrySetLoftOptions(ByVal loft As Object)
        Try
            loft.SectionCoupling = 1
        Catch
        End Try

        Try
            loft.Relimitation = 1
        Catch
        End Try

        Try
            loft.CanonicalDetection = 2
        Catch
        End Try
    End Sub

    Private Shared Sub TrySetName(ByVal catiaObject As Object, ByVal name As String)
        Try
            catiaObject.Name = name
        Catch
        End Try
    End Sub

    Private Shared Sub TrySetPartNumber(ByVal partDocument As Object, ByVal partNumber As String)
        Try
            partDocument.Product.PartNumber = partNumber
        Catch
        End Try
    End Sub

    Private Shared Sub TrySetInWorkObject(ByVal part As Object, ByVal inWorkObject As Object)
        Try
            part.InWorkObject = inWorkObject
        Catch
        End Try
    End Sub

    Private Shared Sub TryUpdateObject(ByVal part As Object, ByVal partObject As Object)
        Try
            part.UpdateObject(partObject)
        Catch
        End Try
    End Sub

    Private Shared Sub TryUpdatePart(ByVal part As Object)
        Try
            part.Update()
        Catch
        End Try
    End Sub

    Private Shared Sub TryReframe(ByVal catiaApplication As Object)
        Try
            catiaApplication.ActiveWindow.ActiveViewer.Reframe()
        Catch
        End Try
    End Sub

    Private Structure WingStation
        Public ReadOnly Name As String
        Public ReadOnly SpanPosition As Double

        Public Sub New(ByVal name As String, ByVal spanPosition As Double)
            Me.Name = name
            Me.SpanPosition = spanPosition
        End Sub
    End Structure

    Private Structure WingStationProfile
        Public ReadOnly Name As String
        Public ReadOnly ProfileSpline As Object
        Public ReadOnly ClosingPointReference As Object

        Public Sub New(ByVal name As String,
                       ByVal profileSpline As Object,
                       ByVal closingPointReference As Object)
            Me.Name = name
            Me.ProfileSpline = profileSpline
            Me.ClosingPointReference = closingPointReference
        End Sub
    End Structure

    Private Structure AirfoilCoordinate
        Public ReadOnly X As Double
        Public ReadOnly Y As Double

        Public Sub New(ByVal x As Double, ByVal y As Double)
            Me.X = x
            Me.Y = y
        End Sub
    End Structure
End Class
