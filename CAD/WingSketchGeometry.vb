Imports System.Collections.Generic

Friend Module WingSketchGeometry
    Friend Sub CreateSketchCircle(ByVal sketchFactory As Object,
                                  ByVal centerPoint As AirfoilCoordinate,
                                  ByVal radius As Double)
        sketchFactory.CreateClosedCircle(centerPoint.X, centerPoint.Y, radius)
    End Sub

    Friend Function CreateSketchOnPlane(ByVal part As Object,
                                        ByVal sketches As Object,
                                        ByVal sketchPlane As Object) As Object
        Return sketches.Add(part.CreateReferenceFromObject(sketchPlane))
    End Function

    Friend Function ConvertGlobalXzToSketchCoordinates(ByVal airfoilCoordinates As List(Of AirfoilCoordinate),
                                                       ByVal spanPosition As Double,
                                                       ByVal sketchAxis As SketchAxisData) As List(Of AirfoilCoordinate)
        Dim sketchCoordinates As New List(Of AirfoilCoordinate)()

        For Each airfoilPoint As AirfoilCoordinate In airfoilCoordinates
            sketchCoordinates.Add(ConvertGlobalXzToSketchPoint(airfoilPoint, spanPosition, sketchAxis))
        Next

        Return sketchCoordinates
    End Function

    Friend Function ConvertGlobalXzToSketchPoint(ByVal airfoilPoint As AirfoilCoordinate,
                                                 ByVal spanPosition As Double,
                                                 ByVal sketchAxis As SketchAxisData) As AirfoilCoordinate
        Return ConvertGlobalPointToSketchPoint(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                      airfoilPoint.X),
                                               spanPosition,
                                               WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                      airfoilPoint.Y),
                                               sketchAxis)
    End Function

    Friend Function ConvertGlobalPointToSketchPoint(ByVal globalX As Double,
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

    Friend Function GetSketchAxisData(ByVal sketch As Object,
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

    Friend Function AreSketchPointsCoincident(ByVal firstPoint As AirfoilCoordinate,
                                              ByVal secondPoint As AirfoilCoordinate) As Boolean
        Return (Math.Abs(firstPoint.X - secondPoint.X) < 0.000001) AndAlso
            (Math.Abs(firstPoint.Y - secondPoint.Y) < 0.000001)
    End Function

    Friend Sub CreateSmoothClosedRibSketchProfile(ByVal sketchFactory As Object,
                                                  ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        If sketchCoordinates.Count < 3 Then
            Throw New InvalidOperationException("At least three points are required to create a closed rib profile.")
        End If

        Dim sketchPoints As New List(Of Object)()

        For Each sketchCoordinate As AirfoilCoordinate In sketchCoordinates
            sketchPoints.Add(sketchFactory.CreatePoint(sketchCoordinate.X, sketchCoordinate.Y))
        Next

        Dim sketchPointArray(sketchPoints.Count - 1) As Object

        For pointIndex As Integer = 0 To sketchPoints.Count - 1
            sketchPointArray(pointIndex) = sketchPoints(pointIndex)
        Next

        sketchFactory.CreateSpline(sketchPointArray)

        Dim lastPoint As AirfoilCoordinate = sketchCoordinates(sketchCoordinates.Count - 1)
        Dim firstPoint As AirfoilCoordinate = sketchCoordinates(0)
        CreateSketchLineIfDistinct(sketchFactory, lastPoint, firstPoint)
    End Sub

    Friend Sub CreateClosedPolylineRibSketchProfile(ByVal sketchFactory As Object,
                                                    ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        CreatePolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        CreateSketchLineIfDistinct(sketchFactory,
                                   sketchCoordinates(sketchCoordinates.Count - 1),
                                   sketchCoordinates(0))
    End Sub

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
End Module

Friend Structure SketchAxisData
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
