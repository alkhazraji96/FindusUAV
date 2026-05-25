Imports System.Collections.Generic

Friend Module WingAirfoilProfileBuilder
    Friend Function ClipAirfoilProfileByX(ByVal airfoilCoordinates As List(Of AirfoilCoordinate),
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

    Friend Function GetAirfoilSurfacePointAtLocalX(ByVal chordLength As Double,
                                                   ByVal localX As Double,
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

            If SegmentCrossesLocalX(startPoint, endPoint, localX) Then
                intersectionZValues.Add(InterpolateAirfoilPointAtX(startPoint, endPoint, localX).Y)
            End If
        Next

        If intersectionZValues.Count = 0 Then
            Throw New InvalidOperationException("No " & GetWingAirfoilLabel() & " surface point was found at local X = " & localX.ToString() & " mm.")
        End If

        Dim surfaceZ As Double = intersectionZValues(0)

        For Each intersectionZ As Double In intersectionZValues
            If upperSurface Then
                surfaceZ = Math.Max(surfaceZ, intersectionZ)
            Else
                surfaceZ = Math.Min(surfaceZ, intersectionZ)
            End If
        Next

        Return New AirfoilCoordinate(localX, surfaceZ)
    End Function

    Friend Function BuildFixedWingOutboardSkinProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                     0.0,
                                                     WingDefinition.AileronFixedPanelEndX)
    End Function

    Friend Function BuildAileronSkinProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return RotateProfileCoordinatesToMaximumX(
            BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                  WingDefinition.AileronPanelStartX,
                                                  chordLength))
    End Function

    Friend Function BuildAileronRearHingeSparProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                     WingDefinition.AileronFixedPanelEndX,
                                                     WingDefinition.AileronRearSparEndX)
    End Function

    Friend Function BuildAirfoilSurfaceProfileCoordinates(ByVal chordLength As Double,
                                                          ByVal upperSurface As Boolean) As List(Of AirfoilCoordinate)
        Dim profileCoordinates As New List(Of AirfoilCoordinate)()
        Dim segmentCount As Integer = Math.Max(24, WingDefinition.PointCountPerSurface - 1)

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim localX As Double = chordLength * ratio

            AddAirfoilCoordinateIfDistinct(profileCoordinates,
                                           GetAirfoilSurfacePointAtLocalX(chordLength,
                                                                         localX,
                                                                         upperSurface))
        Next

        Return profileCoordinates
    End Function

    Friend Function BuildAirfoilSegmentProfileCoordinates(ByVal chordLength As Double,
                                                          ByVal startX As Double,
                                                          ByVal endX As Double) As List(Of AirfoilCoordinate)
        If startX < 0.0 OrElse endX > chordLength OrElse endX <= startX Then
            Throw New InvalidOperationException("Invalid airfoil segment X limits.")
        End If

        Dim profileCoordinates As New List(Of AirfoilCoordinate)()
        Dim segmentCount As Integer = 24

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim localX As Double = startX + ((endX - startX) * ratio)
            AddAirfoilCoordinateIfDistinct(profileCoordinates,
                                           GetAirfoilSurfacePointAtLocalX(chordLength,
                                                                         localX,
                                                                         True))
        Next

        AddChordwiseCutEdgePoints(profileCoordinates, chordLength, endX, True, False)

        For pointIndex As Integer = segmentCount To 0 Step -1
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim localX As Double = startX + ((endX - startX) * ratio)
            AddAirfoilCoordinateIfDistinct(profileCoordinates,
                                           GetAirfoilSurfacePointAtLocalX(chordLength,
                                                                         localX,
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
            GetAirfoilSurfacePointAtLocalX(chordLength,
                                           cutX,
                                           True)
        Dim lowerCutPoint As AirfoilCoordinate =
            GetAirfoilSurfacePointAtLocalX(chordLength,
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

    Private Function SegmentCrossesLocalX(ByVal startPoint As AirfoilCoordinate,
                                          ByVal endPoint As AirfoilCoordinate,
                                          ByVal localX As Double) As Boolean
        Dim minimumX As Double = Math.Min(startPoint.X, endPoint.X)
        Dim maximumX As Double = Math.Max(startPoint.X, endPoint.X)

        Return localX >= (minimumX - 0.000001) AndAlso
            localX <= (maximumX + 0.000001)
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

End Module
