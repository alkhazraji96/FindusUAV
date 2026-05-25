Imports System.Collections.Generic

Friend Module TailAirfoilProfileGenerator
    Friend Function GeneratePartialNaca(ByVal chord As Double,
                                        ByVal airfoil As AirfoilConfiguration,
                                        ByVal pointCount As Integer,
                                        ByVal xOffset As Double,
                                        ByVal startPercent As Double,
                                        ByVal endPercent As Double) As List(Of TailPoint3D)
        If airfoil Is Nothing Then
            Throw New ArgumentNullException("airfoil")
        End If

        Return GeneratePartialNacaProfile(chord,
                                          airfoil.MaximumCamber,
                                          airfoil.MaximumCamberPosition,
                                          airfoil.MaximumThickness,
                                          pointCount,
                                          xOffset,
                                          startPercent,
                                          endPercent)
    End Function

    Private Function GeneratePartialNacaProfile(ByVal chord As Double,
                                                ByVal maximumCamber As Double,
                                                ByVal maximumCamberPosition As Double,
                                                ByVal maximumThickness As Double,
                                                ByVal pointCount As Integer,
                                                ByVal xOffset As Double,
                                                ByVal startPercent As Double,
                                                ByVal endPercent As Double) As List(Of TailPoint3D)
        Dim profilePoints As New List(Of TailPoint3D)()
        Dim upperX As New List(Of Double)()
        Dim upperY As New List(Of Double)()
        Dim lowerX As New List(Of Double)()
        Dim lowerY As New List(Of Double)()

        For pointIndex As Integer = 0 To pointCount - 1
            Dim beta As Double = Math.PI * (pointIndex / CDbl(pointCount - 1))
            Dim rawX As Double = 0.5 * (1.0 - Math.Cos(beta))
            Dim normalizedX As Double = startPercent + rawX * (endPercent - startPercent)

            Dim upperPoint As TailPoint3D = Nothing
            Dim lowerPoint As TailPoint3D = Nothing
            CalculateNacaSurfacePoints(normalizedX,
                                       chord,
                                       xOffset,
                                       maximumCamber,
                                       maximumCamberPosition,
                                       maximumThickness,
                                       upperPoint,
                                       lowerPoint)

            upperX.Add(upperPoint.X)
            upperY.Add(upperPoint.Y)
            lowerX.Add(lowerPoint.X)
            lowerY.Add(lowerPoint.Y)
        Next

        For pointIndex As Integer = pointCount - 1 To 0 Step -1
            profilePoints.Add(New TailPoint3D(upperX(pointIndex), upperY(pointIndex), 0.0))
        Next

        Dim startIndex As Integer = If(startPercent = 0.0, 1, 0)

        For pointIndex As Integer = startIndex To pointCount - 1
            profilePoints.Add(New TailPoint3D(lowerX(pointIndex), lowerY(pointIndex), 0.0))
        Next

        Return profilePoints
    End Function

    Private Sub CalculateNacaSurfacePoints(ByVal normalizedX As Double,
                                           ByVal chord As Double,
                                           ByVal xOffset As Double,
                                           ByVal maximumCamber As Double,
                                           ByVal maximumCamberPosition As Double,
                                           ByVal maximumThickness As Double,
                                           ByRef upperPoint As TailPoint3D,
                                           ByRef lowerPoint As TailPoint3D)
        Dim thickness As Double = 5.0 * maximumThickness *
            ((0.2969 * Math.Sqrt(normalizedX)) -
             (0.126 * normalizedX) -
             (0.3516 * Math.Pow(normalizedX, 2.0)) +
             (0.2843 * Math.Pow(normalizedX, 3.0)) -
             (0.1015 * Math.Pow(normalizedX, 4.0)))
        Dim camber As Double = 0.0
        Dim camberSlope As Double = 0.0

        If maximumCamber > 0.0 AndAlso maximumCamberPosition > 0.0 Then
            If normalizedX <= maximumCamberPosition Then
                camber = (maximumCamber / Math.Pow(maximumCamberPosition, 2.0)) *
                    ((2.0 * maximumCamberPosition * normalizedX) - Math.Pow(normalizedX, 2.0))
                camberSlope = ((2.0 * maximumCamber) / Math.Pow(maximumCamberPosition, 2.0)) *
                    (maximumCamberPosition - normalizedX)
            Else
                Dim aftCamberLength As Double = 1.0 - maximumCamberPosition

                If aftCamberLength > 0.0 Then
                    camber = (maximumCamber / Math.Pow(aftCamberLength, 2.0)) *
                        ((1.0 - (2.0 * maximumCamberPosition)) +
                         (2.0 * maximumCamberPosition * normalizedX) -
                         Math.Pow(normalizedX, 2.0))
                    camberSlope = ((2.0 * maximumCamber) / Math.Pow(aftCamberLength, 2.0)) *
                        (maximumCamberPosition - normalizedX)
                End If
            End If
        End If

        Dim theta As Double = Math.Atan(camberSlope)
        Dim upperX As Double = normalizedX - (thickness * Math.Sin(theta))
        Dim upperY As Double = camber + (thickness * Math.Cos(theta))
        Dim lowerX As Double = normalizedX + (thickness * Math.Sin(theta))
        Dim lowerY As Double = camber - (thickness * Math.Cos(theta))

        upperPoint = New TailPoint3D((upperX * chord) + xOffset, upperY * chord, 0.0)
        lowerPoint = New TailPoint3D((lowerX * chord) + xOffset, lowerY * chord, 0.0)
    End Sub
End Module
