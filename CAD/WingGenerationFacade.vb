Public Class WingGenerationFacade
    Public Sub Generate()
        Run()
    End Sub

    Public Shared Sub Run()
        Run(WingConfiguration.CreateDefault())
    End Sub

    Friend Shared Sub Run(ByVal configuration As WingConfiguration,
                          Optional ByVal progressReporter As IGenerationProgressReporter = Nothing)
        WingGenerator.CreatePhysicalRibsMainSparAndAilerons(configuration, progressReporter)
    End Sub

    Public Shared Function CreateWingPlanform() As Object
        Return CreateWingPlanform(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingPlanform(ByVal configuration As WingConfiguration,
                                              Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Return WingGenerator.CreatePlanform(configuration, progressReporter)
    End Function

    Public Shared Function CreateWingAirfoilStations() As Object
        Return CreateWingAirfoilStations(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingAirfoilStations(ByVal configuration As WingConfiguration,
                                                     Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Return WingGenerator.CreateAirfoilStations(configuration, progressReporter)
    End Function

    Public Shared Function CreateWingOuterWingSkin() As Object
        Return CreateWingOuterWingSkin(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingOuterWingSkin(ByVal configuration As WingConfiguration,
                                                   Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Return WingGenerator.CreateOuterWingSkin(configuration, progressReporter)
    End Function

    Public Shared Function CreateWingPhysicalRibs() As Object
        Return CreateWingPhysicalRibs(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingPhysicalRibs(ByVal configuration As WingConfiguration,
                                                  Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Return WingGenerator.CreatePhysicalRibs(configuration, progressReporter)
    End Function

    Public Shared Function CreateWingPhysicalRibsAndMainSpar() As Object
        Return CreateWingPhysicalRibsAndMainSpar(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingPhysicalRibsAndMainSpar(ByVal configuration As WingConfiguration,
                                                             Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Return WingGenerator.CreatePhysicalRibsAndMainSpar(configuration, progressReporter)
    End Function

    Public Shared Function CreateWingPhysicalRibsMainSparAndAilerons() As Object
        Return CreateWingPhysicalRibsMainSparAndAilerons(WingConfiguration.CreateDefault())
    End Function

    Friend Shared Function CreateWingPhysicalRibsMainSparAndAilerons(ByVal configuration As WingConfiguration,
                                                                     Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Return WingGenerator.CreatePhysicalRibsMainSparAndAilerons(configuration, progressReporter)
    End Function

End Class
