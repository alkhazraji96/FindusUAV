Imports System.Collections.Generic

Friend Module NacaAirfoil
    Friend Function BuildCoordinates(ByVal chordLength As Double,
                                     ByVal pointCountPerSurface As Integer,
                                     ByVal maximumCamber As Double,
                                     ByVal maximumCamberPosition As Double,
                                     ByVal maximumThickness As Double,
                                     ByVal closedTrailingEdge As Boolean) As List(Of AirfoilCoordinate)
        If pointCountPerSurface < 5 Then
            pointCountPerSurface = 5
        End If

        Dim upperSurface As New List(Of AirfoilCoordinate)()
        Dim lowerSurface As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = 0 To pointCountPerSurface - 1
            Dim beta As Double = Math.PI * CDbl(pointIndex) / CDbl(pointCountPerSurface - 1)
            Dim normalizedX As Double = 0.5 * (1.0 - Math.Cos(beta))

            Dim upperPoint As AirfoilCoordinate = Nothing
            Dim lowerPoint As AirfoilCoordinate = Nothing
            CalculateSurfacePoints(normalizedX,
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

        Dim lowerSurfaceEndIndex As Integer = lowerSurface.Count - 1

        If closedTrailingEdge Then
            lowerSurfaceEndIndex -= 1
        End If

        For pointIndex As Integer = 1 To lowerSurfaceEndIndex
            profileCoordinates.Add(lowerSurface(pointIndex))
        Next

        Return profileCoordinates
    End Function

    Private Sub CalculateSurfacePoints(ByVal normalizedX As Double,
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
             (0.1260 * normalizedX) -
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
End Module
