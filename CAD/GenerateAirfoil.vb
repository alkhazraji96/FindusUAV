Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Class GenerateAirfoil
    Private Const DefaultChordLength As Double = 100.0
    Private Const DefaultPointCountPerSurface As Integer = 81

    Public Sub Generate()
        Run()
    End Sub

    Public Shared Sub Run()
        CreateNaca2412Part()
    End Sub

    Public Shared Function CreateNaca2412Part(Optional ByVal chordLength As Double = DefaultChordLength,
                                              Optional ByVal pointCountPerSurface As Integer = DefaultPointCountPerSurface) As Object
        If chordLength <= 0.0 Then
            Throw New ArgumentOutOfRangeException("chordLength", "Chord length must be greater than zero.")
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

        Dim hybridBodies As Object = part.HybridBodies
        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, "NACA 2412 Airfoil 2D")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline)
        TrySetName(profileSpline, "NACA 2412 profile")

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) = BuildNaca2412Coordinates(chordLength, pointCountPerSurface)
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

        TrySetInWorkObject(part, profileSpline)
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

    Private Shared Sub TrySetSplineOptions(ByVal profileSpline As Object)
        Try
            profileSpline.SetSplineType(0)
        Catch
        End Try

        Try
            profileSpline.SetClosing(0)
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

    Private Shared Sub TryReframe(ByVal catiaApplication As Object)
        Try
            catiaApplication.ActiveWindow.ActiveViewer.Reframe()
        Catch
        End Try
    End Sub

    Private Structure AirfoilCoordinate
        Public ReadOnly X As Double
        Public ReadOnly Y As Double

        Public Sub New(ByVal x As Double, ByVal y As Double)
            Me.X = x
            Me.Y = y
        End Sub
    End Structure
End Class
