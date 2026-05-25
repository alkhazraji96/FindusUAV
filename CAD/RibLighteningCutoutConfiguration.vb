Imports System.Collections.Generic

Friend Class RibLighteningCutoutConfiguration
    Public Property Name As String
    Public Property ChordFraction As Double
    Public Property PreferredDiameter As Double

    Public Sub New(ByVal name As String,
                   ByVal chordFraction As Double,
                   ByVal preferredDiameter As Double)
        Me.Name = name
        Me.ChordFraction = chordFraction
        Me.PreferredDiameter = preferredDiameter
    End Sub

    Public Shared Function CreateDefaultWingCutouts() As List(Of RibLighteningCutoutConfiguration)
        Return New List(Of RibLighteningCutoutConfiguration) From {
            New RibLighteningCutoutConfiguration("Forward lightening cutout", 0.15, 22.0),
            New RibLighteningCutoutConfiguration("Middle lightening cutout", 0.5, 34.0),
            New RibLighteningCutoutConfiguration("Aft lightening cutout", 0.7, 20.0)
        }
    End Function
End Class
