Imports System.Reflection
Imports System.Runtime.InteropServices
Imports INFITF
Imports MECMOD
Imports PARTITF
Imports System.Math

Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Console.WriteLine("Hello World!")
    End Sub

    Private Sub btnCATIA_Click(sender As Object, e As EventArgs) Handles btnCATIA.Click
        Dim generator As New AirfoilGenerator()

        ' Parameters for the wing
        Dim chordLength As Double = 100.0 ' mm
        Dim spanThickness As Double = 500.0 ' mm (Extrusion length)
        Dim resolution As Integer = 50 ' Points per surface

        ' 1. Generate the 2D profile coordinates
        Dim profilePoints = generator.GenerateNACA2415Profile(chordLength, resolution)

        ' 2. Send to CATIA to draw the spline and pad it
        DrawAndPadInCatia(profilePoints, spanThickness)

    End Sub


    ''' <summary>
    ''' Connects to CATIA, draws the 2D spline, and pads it into a 3D solid.
    ''' </summary>
    ''' <summary>
    ''' Connects to CATIA, draws the 2D profile inside a Sketch, and pads it.
    ''' </summary>
    Private Sub DrawAndPadInCatia(points As List(Of Point3D), padThickness As Double)
        Dim catiaApp As Application

        ' Connect to CATIA
        Try
            catiaApp = CType(Marshal.GetActiveObject("CATIA.Application"), Application)
        Catch ex As Exception
            MessageBox.Show("Could not connect to CATIA. Ensure it is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Exit Sub
        End Try

        Dim partDoc As PartDocument = CType(catiaApp.ActiveDocument, PartDocument)
        Dim activePart As Part = partDoc.Part

        ' 1. Get the XY Plane to draw our Sketch on
        Dim originElements As OriginElements = activePart.OriginElements
        Dim xyPlane As Reference = activePart.CreateReferenceFromObject(originElements.PlaneXY)

        ' 2. Create the Sketch in the Main Body
        Dim sketches As Sketches = activePart.MainBody.Sketches
        Dim airfoilSketch As Sketch = sketches.Add(xyPlane)
        airfoilSketch.Name = "NACA2415_Sketch"

        ' Open Sketch to access the 2D Factory
        Dim factory2D As Factory2D = airfoilSketch.OpenEdition()

        ' 3. Create the 2D Points
        Dim lastIdx As Integer = points.Count - 1
        Dim controlPoints(lastIdx) As Object ' CATIA expects an Object Array for Splines

        For i As Integer = 0 To lastIdx
            controlPoints(i) = factory2D.CreatePoint(points(i).X, points(i).Y)
        Next

        ' 4. Create an OPEN Spline through the points
        ' This generates the smooth curve of the airfoil
        Dim spline2D As Spline2D = factory2D.CreateSpline(controlPoints)

        ' 5. Close the Blunt Trailing Edge with a straight Line
        ' This prevents the spline from looping or crossing itself at the tail
        Dim teLine As Line2D = factory2D.CreateLine(points(lastIdx).X, points(lastIdx).Y, points(0).X, points(0).Y)
        teLine.StartPoint = CType(controlPoints(lastIdx), Point2D)
        teLine.EndPoint = CType(controlPoints(0), Point2D)

        ' Close the Sketch
        airfoilSketch.CloseEdition()
        activePart.Update() ' Lock in the Sketch

        ' 6. Create the Solid Pad
        Dim solidFactory As ShapeFactory = CType(activePart.ShapeFactory, ShapeFactory)
        Dim sketchRef As Reference = activePart.CreateReferenceFromObject(airfoilSketch)

        ' Because it's a closed Sketch, AddNewPadFromRef will now work perfectly!
        Dim airfoilPad As Pad = solidFactory.AddNewPadFromRef(sketchRef, padThickness)
        airfoilPad.Name = "Airfoil_Solid"

        activePart.Update()

        MessageBox.Show("Solid 3D Airfoil successfully generated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class


' 1. Simple structure to hold our 3D coordinates
Public Structure Point3D
    Public X As Double
    Public Y As Double
    Public Z As Double

    Public Sub New(x__1 As Double, y__1 As Double, z__1 As Double)
        X = x__1
        Y = y__1
        Z = z__1
    End Sub
End Structure

' 2. The Math Generator for the NACA 2415
Public Class AirfoilGenerator

    ''' <summary>
    ''' Generates a continuous 2D point profile for a NACA 2415 Airfoil.
    ''' </summary>
    Public Function GenerateNACA2415Profile(chord As Double, numPoints As Integer) As List(Of Point3D)

        ' NACA 2415 parameters
        Dim m As Double = 0.02   ' Maximum camber (2%)
        Dim p As Double = 0.4    ' Position of maximum camber (40%)
        Dim t As Double = 0.15   ' Maximum thickness (15%)

        Dim upperX As New List(Of Double)()
        Dim upperY As New List(Of Double)()
        Dim lowerX As New List(Of Double)()
        Dim lowerY As New List(Of Double)()

        For i As Integer = 0 To numPoints - 1
            ' Use cosine spacing for higher point density at the leading edge
            Dim beta As Double = PI * (i / CDbl(numPoints - 1))
            Dim x As Double = 0.5 * (1.0 - Cos(beta))

            ' Thickness distribution (yt)
            Dim yt As Double = 5.0 * t * (0.2969 * Sqrt(x) - 0.126 * x - 0.3516 * Pow(x, 2) + 0.2843 * Pow(x, 3) - 0.1015 * Pow(x, 4))

            ' Camber line (yc) and gradient (dyc_dx)
            Dim yc As Double = 0
            Dim dyc_dx As Double = 0

            If x >= 0 AndAlso x < p Then
                yc = (m / Pow(p, 2)) * (2.0 * p * x - Pow(x, 2))
                dyc_dx = (2.0 * m / Pow(p, 2)) * (p - x)
            ElseIf x >= p AndAlso x <= 1.0 Then
                yc = (m / Pow(1.0 - p, 2)) * (1.0 - 2.0 * p + 2.0 * p * x - Pow(x, 2))
                dyc_dx = (2.0 * m / Pow(1.0 - p, 2)) * (p - x)
            End If

            Dim theta As Double = Atan(dyc_dx)

            ' Calculate upper and lower surface coordinates
            upperX.Add((x - yt * Sin(theta)) * chord)
            upperY.Add((yc + yt * Cos(theta)) * chord)

            lowerX.Add((x + yt * Sin(theta)) * chord)
            lowerY.Add((yc - yt * Cos(theta)) * chord)
        Next

        Dim profilePoints As New List(Of Point3D)()

        ' Trailing Edge -> Leading Edge (Upper surface)
        For i As Integer = numPoints - 1 To 0 Step -1
            profilePoints.Add(New Point3D(upperX(i), upperY(i), 0))
        Next

        ' Leading Edge -> Trailing Edge (Lower surface)
        For i As Integer = 1 To numPoints - 1
            profilePoints.Add(New Point3D(lowerX(i), lowerY(i), 0))
        Next

        Return profilePoints
    End Function

End Class