Public Class GenerateAirfoil
    Public Sub Generate()
        Run()
    End Sub

    Public Shared Sub Run()
        Run(WingConfiguration.CreateDefault())
    End Sub

    Friend Shared Sub Run(ByVal configuration As WingConfiguration)
        WingGenerator.CreateStage4CPhysicalRibsMainSparAndAilerons(configuration)
    End Sub

    Public Shared Function CreateWingStage1Planform() As Object
        Return CreateWingStage1Planform(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingStage1Planform(ByVal configuration As WingConfiguration) As Object
        Return WingGenerator.CreateStage1Planform(configuration)
    End Function

    Public Shared Function CreateWingStage2AirfoilStations() As Object
        Return CreateWingStage2AirfoilStations(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingStage2AirfoilStations(ByVal configuration As WingConfiguration) As Object
        Return WingGenerator.CreateStage2AirfoilStations(configuration)
    End Function

    Public Shared Function CreateWingStage3OuterWingSkin() As Object
        Return CreateWingStage3OuterWingSkin(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingStage3OuterWingSkin(ByVal configuration As WingConfiguration) As Object
        Return WingGenerator.CreateStage3OuterWingSkin(configuration)
    End Function

    Public Shared Function CreateWingStage4APhysicalRibs() As Object
        Return CreateWingStage4APhysicalRibs(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingStage4APhysicalRibs(ByVal configuration As WingConfiguration) As Object
        Return WingGenerator.CreateStage4APhysicalRibs(configuration)
    End Function

    Public Shared Function CreateWingStage4BPhysicalRibsAndMainSpar() As Object
        Return CreateWingStage4BPhysicalRibsAndMainSpar(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingStage4BPhysicalRibsAndMainSpar(ByVal configuration As WingConfiguration) As Object
        Return WingGenerator.CreateStage4BPhysicalRibsAndMainSpar(configuration)
    End Function

    Public Shared Function CreateWingStage4CPhysicalRibsMainSparAndAilerons() As Object
        Return CreateWingStage4CPhysicalRibsMainSparAndAilerons(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingStage4CPhysicalRibsMainSparAndAilerons(ByVal configuration As WingConfiguration) As Object
        Return WingGenerator.CreateStage4CPhysicalRibsMainSparAndAilerons(configuration)
    End Function

    Public Shared Function CreateNaca2412Part(Optional ByVal chordLength As Double = 100.0,
                                              Optional ByVal pointCountPerSurface As Integer = 81,
                                              Optional ByVal padLength As Double = 3.0) As Object
        Return Naca2412SliceGenerator.CreatePart(chordLength, pointCountPerSurface, padLength)
    End Function
End Class
