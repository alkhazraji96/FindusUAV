Public Class GenerateAirfoil
    Public Sub Generate()
        Run()
    End Sub

    Public Shared Sub Run()
        WingGenerator.CreateStage4APhysicalRibs()
    End Sub

    Public Shared Function CreateWingStage1Planform() As Object
        Return WingGenerator.CreateStage1Planform()
    End Function

    Public Shared Function CreateWingStage2AirfoilStations() As Object
        Return WingGenerator.CreateStage2AirfoilStations()
    End Function

    Public Shared Function CreateWingStage3OuterWingSkin() As Object
        Return WingGenerator.CreateStage3OuterWingSkin()
    End Function

    Public Shared Function CreateWingStage4APhysicalRibs() As Object
        Return WingGenerator.CreateStage4APhysicalRibs()
    End Function

    Public Shared Function CreateNaca2412Part(Optional ByVal chordLength As Double = 100.0,
                                              Optional ByVal pointCountPerSurface As Integer = 81,
                                              Optional ByVal padLength As Double = 3.0) As Object
        Return Naca2412SliceGenerator.CreatePart(chordLength, pointCountPerSurface, padLength)
    End Function
End Class
