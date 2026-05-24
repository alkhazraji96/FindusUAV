Imports INFITF
Imports HybridShapeTypeLib
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Friend Module WingGenerator
    Private Const WingOperationName As String = "Wing generation"

    Private Enum RibProfileRegion
        Full
        ForwardWingPanel
    End Enum

    Private Structure PointCoordinate3D
        Friend ReadOnly X As Double
        Friend ReadOnly Y As Double
        Friend ReadOnly Z As Double

        Friend Sub New(ByVal x As Double,
                       ByVal y As Double,
                       ByVal z As Double)
            Me.X = x
            Me.Y = y
            Me.Z = z
        End Sub
    End Structure

    Private Sub ApplyWingConfiguration(ByVal configuration As WingConfiguration)
        WingDefinition.UseConfiguration(configuration)
    End Sub

    Private Function GetWingAirfoilLabel() As String
        Dim configuration As WingConfiguration = WingDefinition.Configuration

        If configuration Is Nothing OrElse
            configuration.Airfoil Is Nothing OrElse
            String.IsNullOrWhiteSpace(configuration.Airfoil.NacaCode) Then
            Return "NACA airfoil"
        End If

        Return configuration.Airfoil.NacaCode.Trim()
    End Function

    Private Function GetWingAirfoilIdentifier() As String
        Return GetWingAirfoilLabel().Replace(" ", "")
    End Function

    Private Function GetStationProfileSetName() As String
        Return GetWingAirfoilLabel() & " Station Profiles"
    End Function

    Private Function GetDefaultOuterWingSkinName() As String
        Return GetWingAirfoilLabel() & " outer wing skin"
    End Function

    Private Function PrepareProgressReporter(ByVal progressReporter As IGenerationProgressReporter) As IGenerationProgressReporter
        Return GenerationProgress.UseDefaultReporterWhenMissing(progressReporter)
    End Function

    Private Sub ReportWingStarting(ByVal progressReporter As IGenerationProgressReporter,
                                   ByVal workflowName As String)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateStarting(WingOperationName,
                                                                          "Starting " & workflowName & "."))
    End Sub

    Private Sub ReportWingStep(ByVal progressReporter As IGenerationProgressReporter,
                               ByVal stepName As String,
                               ByVal message As String,
                               ByVal currentStep As Integer,
                               ByVal totalSteps As Integer)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateStep(WingOperationName,
                                                                      stepName,
                                                                      message,
                                                                      currentStep,
                                                                      totalSteps))
    End Sub

    Private Sub ReportWingCompleted(ByVal progressReporter As IGenerationProgressReporter,
                                    ByVal workflowName As String)
        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateCompleted(WingOperationName,
                                                                           workflowName & " complete."))
    End Sub

    Private Sub ReportWingFailed(ByVal progressReporter As IGenerationProgressReporter,
                                 ByVal workflowName As String,
                                 ByVal exception As Exception)
        Dim message As String = workflowName & " failed."

        If exception IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(exception.Message) Then
            message &= " " & exception.Message
        End If

        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateFailed(WingOperationName,
                                                                        message))
    End Sub

    Private Sub HideWingConstructionGeometry(ByVal partDocument As Object,
                                             ByVal part As Object,
                                             ByVal ParamArray constructionSets() As Object)
        If constructionSets IsNot Nothing Then
            For Each constructionSet As Object In constructionSets
                TryHideObject(partDocument, constructionSet)
            Next
        End If

        TryHideSketchesInBodies(partDocument, part)
        TryUpdatePart(part)
    End Sub

    Private Sub HideWingStationProfileConstruction(ByVal partDocument As Object,
                                                   ByVal stationProfiles As List(Of WingStationProfile))
        If stationProfiles Is Nothing Then
            Return
        End If

        For Each stationProfile As WingStationProfile In stationProfiles
            If stationProfile.ConstructionGeometry Is Nothing Then
                Continue For
            End If

            For Each constructionObject As Object In stationProfile.ConstructionGeometry
                TryHideObject(partDocument, constructionObject)
            Next
        Next
    End Sub

    Friend Function CreatePhysicalRibs() As Object
        Return CreatePhysicalRibs(WingConfiguration.CreateDefault())
    End Function

    Friend Function CreatePhysicalRibs(ByVal configuration As WingConfiguration,
                                       Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Dim workflowName As String = "Wing physical ribs"

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreatePhysicalRibsCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreatePhysicalRibsCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 7
        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Tapered_Wing_Physical_Ribs")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Tapered_Wing_Physical_Ribs")

        Dim hybridBodies As Object = part.HybridBodies
        ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, GetStationProfileSetName())

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Outer Wing Skin")

        Dim ribPlaneSet As Object = hybridBodies.Add()
        TrySetName(ribPlaneSet, "Rib Mid-Planes")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim shapeFactory As Object = part.ShapeFactory
        Dim stations As List(Of WingStation) = BuildStations()

        ReportWingStep(progressReporter, "Planform", "Building planform and rib station references.", 3, totalSteps)
        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        ReportWingStep(progressReporter, "Airfoil profiles", "Building airfoil station profiles.", 4, totalSteps)
        For Each station As WingStation In stations
            stationProfiles.Add(AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
        Next

        ReportWingStep(progressReporter, "Outer wing skin", "Creating outer wing skin.", 5, totalSteps)
        Dim outerSkin As Object = CreateOuterWingSkinFromProfiles(part,
                                                                  hybridShapeFactory,
                                                                  skinSet,
                                                                  stationProfiles)

        ReportWingStep(progressReporter, "Ribs", "Creating physical rib bodies.", 6, totalSteps)
        For Each station As WingStation In stations
            AddPhysicalRibBody(part, hybridShapeFactory, shapeFactory, ribPlaneSet, station)
        Next

        ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 7, totalSteps)
        TrySetInWorkObject(part, outerSkin)
        RequireUpdatePart(part, "wing with physical ribs")
        HideWingConstructionGeometry(partDocument,
                                     part,
                                     planformSet,
                                     airfoilSet,
                                     ribPlaneSet)
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreatePhysicalRibsAndMainSpar() As Object
        Return CreatePhysicalRibsAndMainSpar(WingConfiguration.CreateDefault())
    End Function

    Friend Function CreatePhysicalRibsAndMainSpar(ByVal configuration As WingConfiguration,
                                                  Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Dim workflowName As String = "Wing ribs and main spar"

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreatePhysicalRibsAndMainSparCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreatePhysicalRibsAndMainSparCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 8
        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Tapered_Wing_Ribs_And_Main_Spar")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Tapered_Wing_Ribs_And_Main_Spar")

        Dim hybridBodies As Object = part.HybridBodies
        ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, GetStationProfileSetName())

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Outer Wing Skin")

        Dim ribPlaneSet As Object = hybridBodies.Add()
        TrySetName(ribPlaneSet, "Rib Mid-Planes")

        Dim sparReferenceSet As Object = hybridBodies.Add()
        TrySetName(sparReferenceSet, "Main Spar References")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim shapeFactory As Object = part.ShapeFactory
        Dim stations As List(Of WingStation) = BuildStations()

        ReportWingStep(progressReporter, "Planform", "Building planform and rib station references.", 3, totalSteps)
        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        ReportWingStep(progressReporter, "Airfoil profiles", "Building airfoil station profiles.", 4, totalSteps)
        For Each station As WingStation In stations
            stationProfiles.Add(AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
        Next

        ReportWingStep(progressReporter, "Outer wing skin", "Creating outer wing skin.", 5, totalSteps)
        Dim outerSkin As Object = CreateOuterWingSkinFromProfiles(part,
                                                                  hybridShapeFactory,
                                                                  skinSet,
                                                                  stationProfiles)

        ReportWingStep(progressReporter, "Ribs", "Creating physical ribs with cutouts.", 6, totalSteps)
        For Each station As WingStation In stations
            AddPhysicalRibBody(part,
                               hybridShapeFactory,
                               shapeFactory,
                               ribPlaneSet,
                               station,
                               True)
        Next

        ReportWingStep(progressReporter, "Main spar", "Creating hollow main spar.", 7, totalSteps)
        Dim mainSpar As Object = AddMainSparBody(part,
                                                 hybridShapeFactory,
                                                 shapeFactory,
                                                 sparReferenceSet)

        ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 8, totalSteps)
        TrySetInWorkObject(part, mainSpar)
        RequireUpdatePart(part, "wing with physical ribs and main spar")
        HideWingConstructionGeometry(partDocument,
                                     part,
                                     planformSet,
                                     airfoilSet,
                                     ribPlaneSet,
                                     sparReferenceSet)
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreatePhysicalRibsMainSparAndAilerons() As Object
        Return CreatePhysicalRibsMainSparAndAilerons(WingConfiguration.CreateDefault())
    End Function

    Friend Function CreatePhysicalRibsMainSparAndAilerons(ByVal configuration As WingConfiguration,
                                                          Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Dim workflowName As String = "Wing ribs, main spar, and ailerons"

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreatePhysicalRibsMainSparAndAileronsCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreatePhysicalRibsMainSparAndAileronsCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 10
        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Tapered_Wing_Ribs_Spar_And_Ailerons")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Tapered_Wing_Ribs_Spar_And_Ailerons")

        Dim hybridBodies As Object = part.HybridBodies
        ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, GetStationProfileSetName())

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Outer Wing Skin")

        Dim ribPlaneSet As Object = hybridBodies.Add()
        TrySetName(ribPlaneSet, "Rib Mid-Planes")

        Dim sparReferenceSet As Object = hybridBodies.Add()
        TrySetName(sparReferenceSet, "Main Spar References")

        Dim aileronReferenceSet As Object = hybridBodies.Add()
        TrySetName(aileronReferenceSet, "Aileron Cut References")

        Dim aileronRearSparSet As Object = hybridBodies.Add()
        TrySetName(aileronRearSparSet, "Aileron Rear Hinge Spars")

        Dim aileronSkinSet As Object = hybridBodies.Add()
        TrySetName(aileronSkinSet, "Aileron Skins")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory
        Dim shapeFactory As Object = part.ShapeFactory
        Dim stations As List(Of WingStation) = BuildStations()

        ReportWingStep(progressReporter, "Planform", "Building planform and rib station references.", 3, totalSteps)
        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        ReportWingStep(progressReporter, "Airfoil profiles", "Building airfoil station profiles.", 4, totalSteps)
        For Each station As WingStation In stations
            AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station)
        Next

        ReportWingStep(progressReporter, "Wing skins", "Creating split fixed-wing and aileron skin surfaces.", 5, totalSteps)
        CreateSplitSkinSurfaces(partDocument,
                                 part,
                                 hybridShapeFactory,
                                 shapeFactory,
                                 skinSet,
                                 aileronSkinSet,
                                 stations)
        ReportWingStep(progressReporter, "Aileron spars", "Creating aileron rear hinge spars.", 6, totalSteps)
        AddAileronRearHingeSpars(part,
                                  hybridShapeFactory,
                                  shapeFactory,
                                  aileronRearSparSet)
        ReportWingStep(progressReporter, "Aileron references", "Creating aileron cut reference geometry.", 7, totalSteps)
        AddAileronCutReferenceGeometry(part, hybridShapeFactory, aileronReferenceSet)

        ReportWingStep(progressReporter, "Ribs", "Creating physical ribs with spar and lightening cutouts.", 8, totalSteps)
        For Each station As WingStation In stations
            AddPhysicalRibBody(part,
                               hybridShapeFactory,
                               shapeFactory,
                               ribPlaneSet,
                               station,
                               True,
                               WingDefinition.IsWithinAileronSpan(station.SpanPosition))
        Next

        ReportWingStep(progressReporter, "Main spar", "Creating hollow main spar.", 9, totalSteps)
        Dim mainSpar As Object = AddMainSparBody(part,
                                                 hybridShapeFactory,
                                                 shapeFactory,
                                                 sparReferenceSet)

        ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 10, totalSteps)
        TrySetInWorkObject(part, mainSpar)
        RequireUpdatePart(part, "wing with physical ribs, main spar, and aileron cuts")
        HideWingConstructionGeometry(partDocument,
                                     part,
                                     planformSet,
                                     airfoilSet,
                                     ribPlaneSet,
                                     sparReferenceSet,
                                     aileronReferenceSet,
                                     aileronRearSparSet)
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreateOuterWingSkin() As Object
        Return CreateOuterWingSkin(WingConfiguration.CreateDefault())
    End Function

    Friend Function CreateOuterWingSkin(ByVal configuration As WingConfiguration,
                                        Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Dim workflowName As String = "Outer wing skin"

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreateOuterWingSkinCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreateOuterWingSkinCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 6
        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Tapered_Wing_Outer_Skin")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Tapered_Wing_Outer_Skin")

        Dim hybridBodies As Object = part.HybridBodies
        ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, GetStationProfileSetName())

        Dim skinSet As Object = hybridBodies.Add()
        TrySetName(skinSet, "Outer Wing Skin")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory

        ReportWingStep(progressReporter, "Planform", "Building planform and rib station references.", 3, totalSteps)
        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        Dim stationProfiles As New List(Of WingStationProfile)()

        ReportWingStep(progressReporter, "Airfoil profiles", "Building airfoil station profiles.", 4, totalSteps)
        For Each station As WingStation In BuildStations()
            stationProfiles.Add(AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station))
        Next

        ReportWingStep(progressReporter, "Outer wing skin", "Creating outer wing skin.", 5, totalSteps)
        Dim outerSkin As Object = CreateOuterWingSkinFromProfiles(part,
                                                                  hybridShapeFactory,
                                                                  skinSet,
                                                                  stationProfiles)
        ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 6, totalSteps)
        TrySetInWorkObject(part, outerSkin)
        RequireUpdatePart(part, "outer wing skin")
        HideWingConstructionGeometry(partDocument,
                                     part,
                                     planformSet,
                                     airfoilSet)
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreateAirfoilStations() As Object
        Return CreateAirfoilStations(WingConfiguration.CreateDefault())
    End Function

    Friend Function CreateAirfoilStations(ByVal configuration As WingConfiguration,
                                          Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Dim workflowName As String = "Airfoil stations"

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreateAirfoilStationsCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreateAirfoilStationsCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 5
        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Tapered_Wing_" & GetWingAirfoilIdentifier() & "_Stations")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Tapered_Wing_" & GetWingAirfoilIdentifier() & "_Stations")

        Dim hybridBodies As Object = part.HybridBodies
        ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)

        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Planform and Rib Stations")

        Dim airfoilSet As Object = hybridBodies.Add()
        TrySetName(airfoilSet, GetStationProfileSetName())

        Dim hybridShapeFactory As Object = part.HybridShapeFactory

        ReportWingStep(progressReporter, "Planform", "Building planform and rib station references.", 3, totalSteps)
        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        ReportWingStep(progressReporter, "Airfoil profiles", "Building airfoil station profiles.", 4, totalSteps)
        For Each station As WingStation In BuildStations()
            AddAirfoilStationProfile(part, hybridShapeFactory, airfoilSet, station)
        Next

        ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 5, totalSteps)
        RequireUpdatePart(part, "airfoil stations")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Friend Function CreatePlanform() As Object
        Return CreatePlanform(WingConfiguration.CreateDefault())
    End Function

    Friend Function CreatePlanform(ByVal configuration As WingConfiguration,
                                   Optional ByVal progressReporter As IGenerationProgressReporter = Nothing) As Object
        Dim activeProgressReporter As IGenerationProgressReporter = PrepareProgressReporter(progressReporter)
        Dim workflowName As String = "Planform"

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreatePlanformCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreatePlanformCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 4
        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True

        Dim partDocument As Object = catiaApplication.Documents.Add("Part")
        TrySetPartNumber(partDocument, "Tapered_Wing_Planform")

        Dim part As Object = partDocument.Part
        TrySetName(part, "Tapered_Wing_Planform")

        Dim hybridBodies As Object = part.HybridBodies
        ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)
        Dim planformSet As Object = hybridBodies.Add()
        TrySetName(planformSet, "Planform and Rib Stations")

        Dim hybridShapeFactory As Object = part.HybridShapeFactory

        ReportWingStep(progressReporter, "Planform", "Building planform and rib station references.", 3, totalSteps)
        AddPlanformGeometry(part, hybridShapeFactory, planformSet)

        ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 4, totalSteps)
        RequireUpdatePart(part, "planform")
        TryReframe(catiaApplication)

        Return partDocument
    End Function

    Private Sub AddPlanformGeometry(ByVal part As Object,
                                    ByVal hybridShapeFactory As Object,
                                    ByVal targetSet As Object)
        If Math.Abs(WingDefinition.SweepAngleDegrees) < 0.000001 Then
            AddPlanformLine(part, hybridShapeFactory, targetSet, "Leading edge full span",
                            0.0, -WingDefinition.HalfSpan, 0.0, WingDefinition.HalfSpan)
        Else
            AddPlanformLine(part, hybridShapeFactory, targetSet, "Right swept leading edge",
                            WingDefinition.GetLeadingEdgeXAtSpanPosition(0.0),
                            0.0,
                            WingDefinition.GetLeadingEdgeXAtSpanPosition(WingDefinition.HalfSpan),
                            WingDefinition.HalfSpan)
            AddPlanformLine(part, hybridShapeFactory, targetSet, "Left swept leading edge",
                            WingDefinition.GetLeadingEdgeXAtSpanPosition(0.0),
                            0.0,
                            WingDefinition.GetLeadingEdgeXAtSpanPosition(-WingDefinition.HalfSpan),
                            -WingDefinition.HalfSpan)
        End If

        AddPlanformLine(part, hybridShapeFactory, targetSet, "Right tapered trailing edge",
                        WingDefinition.GetTrailingEdgeXAtSpanPosition(0.0),
                        0.0,
                        WingDefinition.GetTrailingEdgeXAtSpanPosition(WingDefinition.HalfSpan),
                        WingDefinition.HalfSpan)
        AddPlanformLine(part, hybridShapeFactory, targetSet, "Left tapered trailing edge",
                        WingDefinition.GetTrailingEdgeXAtSpanPosition(0.0),
                        0.0,
                        WingDefinition.GetTrailingEdgeXAtSpanPosition(-WingDefinition.HalfSpan),
                        -WingDefinition.HalfSpan)

        AddRibStationLine(part, hybridShapeFactory, targetSet, "Rib_00_Center", 0.0)

        For ribIndex As Integer = 1 To WingDefinition.RibCountPerSide
            Dim stationY As Double = (WingDefinition.HalfSpan / CDbl(WingDefinition.RibCountPerSide)) * CDbl(ribIndex)

            AddRibStationLine(part, hybridShapeFactory, targetSet,
                              "Rib_R" & ribIndex.ToString("00"), stationY)
            AddRibStationLine(part, hybridShapeFactory, targetSet,
                              "Rib_L" & ribIndex.ToString("00"), -stationY)
        Next
    End Sub

    Private Sub AddRibStationLine(ByVal part As Object,
                                  ByVal hybridShapeFactory As Object,
                                  ByVal targetSet As Object,
                                  ByVal ribName As String,
                                  ByVal spanPosition As Double)
        Dim localChord As Double = WingDefinition.GetChordAtSpanPosition(spanPosition)

        AddPlanformLine(part, hybridShapeFactory, targetSet, ribName,
                        WingDefinition.GetLeadingEdgeXAtSpanPosition(spanPosition),
                        spanPosition,
                        WingDefinition.GetGlobalXAtSpanPosition(spanPosition, localChord),
                        spanPosition)
    End Sub

    Private Sub AddPlanformLine(ByVal part As Object,
                                ByVal hybridShapeFactory As Object,
                                ByVal targetSet As Object,
                                ByVal lineName As String,
                                ByVal startX As Double,
                                ByVal startY As Double,
                                ByVal endX As Double,
                                ByVal endY As Double)
        Dim startPoint As Object =
            hybridShapeFactory.AddNewPointCoord(startX,
                                                startY,
                                                WingDefinition.GetGlobalZAtSpanPosition(startY, 0.0))
        TrySetName(startPoint, lineName & " start")
        targetSet.AppendHybridShape(startPoint)

        Dim endPoint As Object =
            hybridShapeFactory.AddNewPointCoord(endX,
                                                endY,
                                                WingDefinition.GetGlobalZAtSpanPosition(endY, 0.0))
        TrySetName(endPoint, lineName & " end")
        targetSet.AppendHybridShape(endPoint)

        Dim startReference As Object = part.CreateReferenceFromObject(startPoint)
        Dim endReference As Object = part.CreateReferenceFromObject(endPoint)
        Dim planformLine As Object = hybridShapeFactory.AddNewLinePtPt(startReference, endReference)
        TrySetName(planformLine, lineName)
        targetSet.AppendHybridShape(planformLine)
    End Sub

    Private Function BuildStations() As List(Of WingStation)
        Dim stations As New List(Of WingStation)()
        Dim stationSpacing As Double = WingDefinition.HalfSpan / CDbl(WingDefinition.RibCountPerSide)

        For ribIndex As Integer = WingDefinition.RibCountPerSide To 1 Step -1
            stations.Add(New WingStation("Rib_L" & ribIndex.ToString("00"), -stationSpacing * CDbl(ribIndex)))
        Next

        stations.Add(New WingStation("Rib_00_Center", 0.0))

        For ribIndex As Integer = 1 To WingDefinition.RibCountPerSide
            stations.Add(New WingStation("Rib_R" & ribIndex.ToString("00"), stationSpacing * CDbl(ribIndex)))
        Next

        Return stations
    End Function

    Private Function BuildAileronSurfaceStations(ByVal spanSign As Double,
                                                 ByVal sideName As String) As List(Of WingStation)
        Dim stations As New List(Of WingStation)()
        Dim sideRibPrefix As String = If(spanSign < 0.0, "Rib_L", "Rib_R")

        stations.Add(New WingStation(sideName & "_Aileron_Inner_Boundary",
                                     spanSign * WingDefinition.AileronInnerSpanPosition))

        For ribIndex As Integer = 1 To WingDefinition.RibCountPerSide
            Dim ribSpanPosition As Double = WingDefinition.GetRibSpanPosition(ribIndex)

            If ribSpanPosition > (WingDefinition.AileronInnerSpanPosition + 0.000001) AndAlso
                ribSpanPosition <= (WingDefinition.AileronOuterSpanPosition + 0.000001) Then
                stations.Add(New WingStation(sideRibPrefix & ribIndex.ToString("00"),
                                             spanSign * ribSpanPosition))
            End If
        Next

        Return stations
    End Function

    Private Function AddAirfoilStationProfile(ByVal part As Object,
                                              ByVal hybridShapeFactory As Object,
                                              ByVal targetSet As Object,
                                              ByVal station As WingStation) As WingStationProfile
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength,
                                         WingDefinition.PointCountPerSurface,
                                         WingDefinition.AirfoilMaximumCamber,
                                         WingDefinition.AirfoilMaximumCamberPosition,
                                         WingDefinition.AirfoilMaximumThickness,
                                         True)

        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline, True)
        TrySetName(profileSpline, station.Name & " profile")
        Dim closingPointReference As Object = Nothing
        Dim constructionGeometry As New List(Of Object)()

        For pointIndex As Integer = 0 To airfoilCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = airfoilCoordinates(pointIndex)
            Dim airfoilPoint As Object =
                hybridShapeFactory.AddNewPointCoord(WingDefinition.GetGlobalXAtSpanPosition(station.SpanPosition,
                                                                                            coordinate.X),
                                                    station.SpanPosition,
                                                    WingDefinition.GetGlobalZAtSpanPosition(station.SpanPosition,
                                                                                            coordinate.Y))
            TrySetName(airfoilPoint, station.Name & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(airfoilPoint)
            constructionGeometry.Add(airfoilPoint)

            Dim pointReference As Object = part.CreateReferenceFromObject(airfoilPoint)

            If pointIndex = 0 Then
                closingPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        targetSet.AppendHybridShape(profileSpline)
        constructionGeometry.Add(profileSpline)

        Return New WingStationProfile(station.Name,
                                      profileSpline,
                                      closingPointReference,
                                      constructionGeometry)
    End Function

    Private Function CreateOuterWingSkinFromProfiles(ByVal part As Object,
                                                     ByVal hybridShapeFactory As Object,
                                                     ByVal targetSet As Object,
                                                     ByVal stationProfiles As List(Of WingStationProfile),
                                                     Optional ByVal skinName As String = Nothing,
                                                     Optional ByVal partDocument As Object = Nothing,
                                                     Optional ByVal hideProfileConstruction As Boolean = False) As Object
        If stationProfiles.Count < 2 Then
            Throw New InvalidOperationException("At least two airfoil station profiles are required to create the wing skin.")
        End If

        Dim effectiveSkinName As String = skinName

        If String.IsNullOrWhiteSpace(effectiveSkinName) Then
            effectiveSkinName = GetDefaultOuterWingSkinName()
        End If

        Dim outerSkinLoft As HybridShapeLoft =
            CType(hybridShapeFactory.AddNewLoft(), HybridShapeLoft)
        TrySetName(outerSkinLoft, effectiveSkinName)
        TrySetLoftOptions(outerSkinLoft)

        For Each stationProfile As WingStationProfile In stationProfiles
            Dim profileReference As Reference =
                CType(part.CreateReferenceFromObject(stationProfile.ProfileSpline), Reference)
            AddLoftSection(outerSkinLoft, profileReference, stationProfile.ClosingPointReference)
        Next

        targetSet.AppendHybridShape(outerSkinLoft)
        RequireUpdateObject(part, outerSkinLoft, effectiveSkinName)

        If hideProfileConstruction AndAlso partDocument IsNot Nothing Then
            HideWingStationProfileConstruction(partDocument, stationProfiles)
        End If

        Return outerSkinLoft
    End Function

    Private Sub CreateSplitSkinSurfaces(ByVal partDocument As Object,
                                        ByVal part As Object,
                                        ByVal hybridShapeFactory As Object,
                                        ByVal shapeFactory As Object,
                                        ByVal fixedSkinSet As Object,
                                        ByVal aileronSkinSet As Object,
                                        ByVal stations As List(Of WingStation))
        CreateCenterFixedWingSkinSurface(partDocument,
                                         part,
                                         hybridShapeFactory,
                                         fixedSkinSet,
                                         stations,
                                         True,
                                         "Center fixed wing upper skin")
        CreateCenterFixedWingSkinSurface(partDocument,
                                         part,
                                         hybridShapeFactory,
                                         fixedSkinSet,
                                         stations,
                                         False,
                                         "Center fixed wing lower skin")
        AddOutboardFixedWingSkinSurfaces(partDocument, part, hybridShapeFactory, fixedSkinSet, -1.0, "Left")
        AddOutboardFixedWingSkinSurfaces(partDocument, part, hybridShapeFactory, fixedSkinSet, 1.0, "Right")

        Dim leftAileronSkins As List(Of Object) =
            AddAileronSkinSurfaces(partDocument, part, hybridShapeFactory, shapeFactory, aileronSkinSet, -1.0, "Left")
        Dim rightAileronSkins As List(Of Object) =
            AddAileronSkinSurfaces(partDocument, part, hybridShapeFactory, shapeFactory, aileronSkinSet, 1.0, "Right")

        For Each aileronSkin As Object In leftAileronSkins
            TrySetObjectColor(partDocument, aileronSkin, 255, 145, 0)
        Next

        For Each aileronSkin As Object In rightAileronSkins
            TrySetObjectColor(partDocument, aileronSkin, 255, 145, 0)
        Next

        RequireUpdatePart(part, "split fixed wing and aileron skin surfaces")
    End Sub

    Private Function CreateCenterFixedWingSkinSurface(ByVal partDocument As Object,
                                                      ByVal part As Object,
                                                      ByVal hybridShapeFactory As Object,
                                                      ByVal targetSet As Object,
                                                      ByVal stations As List(Of WingStation),
                                                      ByVal upperSurface As Boolean,
                                                      ByVal skinName As String) As Object
        Dim centerSkinProfiles As New List(Of WingStationProfile)()

        centerSkinProfiles.Add(AddOpenAirfoilSurfaceProfile(part,
                                                            hybridShapeFactory,
                                                            targetSet,
                                                            New WingStation("Aileron_Left_Inner_Boundary",
                                                                            -WingDefinition.AileronInnerSpanPosition),
                                                            upperSurface))

        For Each station As WingStation In stations
            If Math.Abs(station.SpanPosition) <
                (WingDefinition.AileronInnerSpanPosition - 0.000001) Then
                centerSkinProfiles.Add(AddOpenAirfoilSurfaceProfile(part,
                                                                    hybridShapeFactory,
                                                                    targetSet,
                                                                    station,
                                                                    upperSurface))
            End If
        Next

        centerSkinProfiles.Add(AddOpenAirfoilSurfaceProfile(part,
                                                            hybridShapeFactory,
                                                            targetSet,
                                                            New WingStation("Aileron_Right_Inner_Boundary",
                                                                            WingDefinition.AileronInnerSpanPosition),
                                                            upperSurface))

        Return CreateOuterWingSkinFromProfiles(part,
                                               hybridShapeFactory,
                                               targetSet,
                                               centerSkinProfiles,
                                               skinName,
                                               partDocument,
                                               True)
    End Function

    Private Function AddOpenAirfoilSurfaceProfile(ByVal part As Object,
                                                  ByVal hybridShapeFactory As Object,
                                                  ByVal targetSet As Object,
                                                  ByVal station As WingStation,
                                                  ByVal upperSurface As Boolean) As WingStationProfile
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)
        Dim surfaceLabel As String = If(upperSurface, "upper", "lower")
        Dim pointLabel As String = If(upperSurface, "Upper", "Lower")

        Return AddOpenSplitSkinProfile(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       "Center fixed wing " & surfaceLabel & " profile " & station.Name,
                                       "Center_Fixed_Wing_" & pointLabel & "_" & station.Name,
                                       station.SpanPosition,
                                       BuildAirfoilSurfaceProfileCoordinates(chordLength, upperSurface))
    End Function

    Private Sub AddOutboardFixedWingSkinSurfaces(ByVal partDocument As Object,
                                                 ByVal part As Object,
                                                 ByVal hybridShapeFactory As Object,
                                                 ByVal targetSet As Object,
                                                 ByVal spanSign As Double,
                                                 ByVal sideName As String)
        Dim fixedWingProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildAileronSurfaceStations(spanSign, sideName)
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

            fixedWingProfiles.Add(AddClosedSplitSkinProfile(part,
                                                            hybridShapeFactory,
                                                            targetSet,
                                                            sideName & " outboard fixed wing skin profile " & station.Name,
                                                            sideName & "_Fixed_Wing_Skin_" & station.Name,
                                                            station.SpanPosition,
                                                            BuildFixedWingOutboardSkinProfileCoordinates(chordLength)))
        Next

        CreateOuterWingSkinFromProfiles(part,
                                        hybridShapeFactory,
                                        targetSet,
                                        fixedWingProfiles,
                                        sideName & " outboard fixed wing skin",
                                        partDocument,
                                        True)
    End Sub

    Private Function AddAileronSkinSurfaces(ByVal partDocument As Object,
                                            ByVal part As Object,
                                            ByVal hybridShapeFactory As Object,
                                            ByVal shapeFactory As Object,
                                            ByVal targetSet As Object,
                                            ByVal spanSign As Double,
                                            ByVal sideName As String) As List(Of Object)
        Dim aileronSkins As New List(Of Object)()
        Dim aileronProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildAileronSurfaceStations(spanSign, sideName)
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

            aileronProfiles.Add(AddClosedSplitSkinProfile(part,
                                                          hybridShapeFactory,
                                                          targetSet,
                                                          sideName & " aileron skin profile " & station.Name,
                                                          sideName & "_Aileron_Skin_" & station.Name,
                                                          station.SpanPosition,
                                                          BuildAileronSkinProfileCoordinates(chordLength)))
        Next

        Dim aileronSurface As Object =
            CreateOuterWingSkinFromProfiles(part,
                                            hybridShapeFactory,
                                            targetSet,
                                            aileronProfiles,
                                            sideName & " aileron skin surface",
                                            partDocument,
                                            True)
        Dim aileronSolid As Object =
            CreateClosedSurfaceSolid(part,
                                     shapeFactory,
                                     aileronSurface,
                                     sideName & " physical aileron")
        aileronSkins.Add(aileronSurface)
        aileronSkins.Add(aileronSolid)

        Return aileronSkins
    End Function

    Private Sub AddAileronRearHingeSpars(ByVal part As Object,
                                         ByVal hybridShapeFactory As Object,
                                         ByVal shapeFactory As Object,
                                         ByVal targetSet As Object)
        AddAileronRearHingeSparForSide(part, hybridShapeFactory, shapeFactory, targetSet, -1.0, "Left")
        AddAileronRearHingeSparForSide(part, hybridShapeFactory, shapeFactory, targetSet, 1.0, "Right")
        RequireUpdatePart(part, "aileron rear hinge spars")
    End Sub

    Private Function AddAileronRearHingeSparForSide(ByVal part As Object,
                                                    ByVal hybridShapeFactory As Object,
                                                    ByVal shapeFactory As Object,
                                                    ByVal targetSet As Object,
                                                    ByVal spanSign As Double,
                                                    ByVal sideName As String) As Object
        Dim sparProfiles As New List(Of WingStationProfile)()

        For Each station As WingStation In BuildAileronSurfaceStations(spanSign, sideName)
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

            sparProfiles.Add(AddClosedSplitSkinProfile(part,
                                                       hybridShapeFactory,
                                                       targetSet,
                                                       sideName & " rear hinge spar profile " & station.Name,
                                                       sideName & "_Rear_Hinge_Spar_" & station.Name,
                                                       station.SpanPosition,
                                                       BuildAileronRearHingeSparProfileCoordinates(chordLength)))
        Next

        Dim sparSurface As Object =
            CreateOuterWingSkinFromProfiles(part,
                                            hybridShapeFactory,
                                            targetSet,
                                            sparProfiles,
                                            sideName & " aileron rear hinge spar surface")

        Return CreateClosedSurfaceSolid(part,
                                        shapeFactory,
                                        sparSurface,
                                        sideName & " aileron rear hinge spar")
    End Function

    Private Function CreateClosedSurfaceSolid(ByVal part As Object,
                                              ByVal shapeFactory As Object,
                                              ByVal surfaceObject As Object,
                                              ByVal solidName As String) As Object
        Dim solidBody As Object = part.Bodies.Add()
        TrySetName(solidBody, solidName & " body")
        TrySetInWorkObject(part, solidBody)

        Dim closeSurface As Object =
            shapeFactory.AddNewCloseSurface(part.CreateReferenceFromObject(surfaceObject))
        TrySetName(closeSurface, solidName)
        RequireUpdateObject(part, closeSurface, solidName)

        Return closeSurface
    End Function

    Private Function AddClosedSplitSkinProfile(ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal targetSet As Object,
                                               ByVal profileName As String,
                                               ByVal pointPrefix As String,
                                               ByVal spanPosition As Double,
                                               ByVal skinCoordinates As List(Of AirfoilCoordinate)) As WingStationProfile
        If skinCoordinates.Count < 3 Then
            Throw New InvalidOperationException("At least three points are required for " & profileName & ".")
        End If

        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline, True)
        TrySetName(profileSpline, profileName)
        Dim closingPointReference As Object = Nothing
        Dim constructionGeometry As New List(Of Object)()

        For pointIndex As Integer = 0 To skinCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = skinCoordinates(pointIndex)
            Dim profilePoint As Object =
                hybridShapeFactory.AddNewPointCoord(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                            coordinate.X),
                                                    spanPosition,
                                                    WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                            coordinate.Y))
            TrySetName(profilePoint,
                       pointPrefix & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(profilePoint)
            constructionGeometry.Add(profilePoint)

            Dim pointReference As Object = part.CreateReferenceFromObject(profilePoint)

            If pointIndex = 0 Then
                closingPointReference = pointReference
            End If

            profileSpline.AddPoint(pointReference)
        Next

        targetSet.AppendHybridShape(profileSpline)
        constructionGeometry.Add(profileSpline)

        Return New WingStationProfile(profileName,
                                      profileSpline,
                                      closingPointReference,
                                      constructionGeometry)
    End Function

    Private Function AddOpenSplitSkinProfile(ByVal part As Object,
                                             ByVal hybridShapeFactory As Object,
                                             ByVal targetSet As Object,
                                             ByVal profileName As String,
                                             ByVal pointPrefix As String,
                                             ByVal spanPosition As Double,
                                             ByVal skinCoordinates As List(Of AirfoilCoordinate)) As WingStationProfile
        If skinCoordinates.Count < 2 Then
            Throw New InvalidOperationException("At least two points are required for " & profileName & ".")
        End If

        Dim profileSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(profileSpline, False)
        TrySetName(profileSpline, profileName)
        Dim constructionGeometry As New List(Of Object)()

        For pointIndex As Integer = 0 To skinCoordinates.Count - 1
            Dim coordinate As AirfoilCoordinate = skinCoordinates(pointIndex)
            Dim profilePoint As Object =
                hybridShapeFactory.AddNewPointCoord(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                            coordinate.X),
                                                    spanPosition,
                                                    WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                            coordinate.Y))
            TrySetName(profilePoint,
                       pointPrefix & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(profilePoint)
            constructionGeometry.Add(profilePoint)

            profileSpline.AddPoint(part.CreateReferenceFromObject(profilePoint))
        Next

        targetSet.AppendHybridShape(profileSpline)
        constructionGeometry.Add(profileSpline)

        Return New WingStationProfile(profileName,
                                      profileSpline,
                                      Nothing,
                                      constructionGeometry)
    End Function

    Private Sub AddAileronCutReferenceGeometry(ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal targetSet As Object)
        AddAileronCutReferenceGeometryForSide(part, hybridShapeFactory, targetSet, 1.0, "Right")
        AddAileronCutReferenceGeometryForSide(part, hybridShapeFactory, targetSet, -1.0, "Left")
        RequireUpdatePart(part, "aileron cut reference geometry")
    End Sub

    Private Sub AddAileronCutReferenceGeometryForSide(ByVal part As Object,
                                                      ByVal hybridShapeFactory As Object,
                                                      ByVal targetSet As Object,
                                                      ByVal spanSign As Double,
                                                      ByVal sideName As String)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " upper fixed wing rear spar face",
                                       spanSign,
                                       WingDefinition.AileronFixedPanelEndX,
                                       True)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " lower fixed wing rear spar face",
                                       spanSign,
                                       WingDefinition.AileronFixedPanelEndX,
                                       False)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " upper rear hinge spar aft face",
                                       spanSign,
                                       WingDefinition.AileronRearSparEndX,
                                       True)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " lower rear hinge spar aft face",
                                       spanSign,
                                       WingDefinition.AileronRearSparEndX,
                                       False)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " upper aileron leading edge",
                                       spanSign,
                                       WingDefinition.AileronPanelStartX,
                                       True)
        AddAileronSpanwiseSurfaceCurve(part,
                                       hybridShapeFactory,
                                       targetSet,
                                       sideName & " lower aileron leading edge",
                                       spanSign,
                                       WingDefinition.AileronPanelStartX,
                                       False)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " upper aileron inner end cut",
                                         spanSign * WingDefinition.AileronInnerSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         True)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " lower aileron inner end cut",
                                         spanSign * WingDefinition.AileronInnerSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         False)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " upper aileron outer end cut",
                                         spanSign * WingDefinition.AileronOuterSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         True)
        AddAileronChordwiseSurfaceCurve(part,
                                         hybridShapeFactory,
                                         targetSet,
                                         sideName & " lower aileron outer end cut",
                                         spanSign * WingDefinition.AileronOuterSpanPosition,
                                         WingDefinition.AileronPanelStartX,
                                         False)
    End Sub

    Private Sub AddAileronSpanwiseSurfaceCurve(ByVal part As Object,
                                               ByVal hybridShapeFactory As Object,
                                               ByVal targetSet As Object,
                                               ByVal curveName As String,
                                               ByVal spanSign As Double,
                                               ByVal localX As Double,
                                               ByVal upperSurface As Boolean)
        Dim curvePoints As New List(Of PointCoordinate3D)()
        Dim segmentCount As Integer = 12

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim absoluteSpan As Double = WingDefinition.AileronInnerSpanPosition +
                ((WingDefinition.AileronOuterSpanPosition - WingDefinition.AileronInnerSpanPosition) * ratio)
            Dim spanPosition As Double = spanSign * absoluteSpan
            Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(spanPosition)
            Dim surfacePoint As AirfoilCoordinate =
                GetAirfoilSurfacePointAtLocalX(chordLength,
                                               localX,
                                               upperSurface)

            curvePoints.Add(New PointCoordinate3D(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                          surfacePoint.X),
                                                  spanPosition,
                                                  WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                          surfacePoint.Y)))
        Next

        CreateSplineThroughPoints(part, hybridShapeFactory, targetSet, curveName, curvePoints)
    End Sub

    Private Sub AddAileronChordwiseSurfaceCurve(ByVal part As Object,
                                                ByVal hybridShapeFactory As Object,
                                                ByVal targetSet As Object,
                                                ByVal curveName As String,
                                                ByVal spanPosition As Double,
                                                ByVal startX As Double,
                                                ByVal upperSurface As Boolean)
        Dim curvePoints As New List(Of PointCoordinate3D)()
        Dim segmentCount As Integer = 12
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(spanPosition)

        For pointIndex As Integer = 0 To segmentCount
            Dim ratio As Double = CDbl(pointIndex) / CDbl(segmentCount)
            Dim localX As Double = startX + ((chordLength - startX) * ratio)
            Dim surfacePoint As AirfoilCoordinate =
                GetAirfoilSurfacePointAtLocalX(chordLength, localX, upperSurface)

            curvePoints.Add(New PointCoordinate3D(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                          surfacePoint.X),
                                                  spanPosition,
                                                  WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                          surfacePoint.Y)))
        Next

        CreateSplineThroughPoints(part, hybridShapeFactory, targetSet, curveName, curvePoints)
    End Sub

    Private Sub CreateSplineThroughPoints(ByVal part As Object,
                                          ByVal hybridShapeFactory As Object,
                                          ByVal targetSet As Object,
                                          ByVal curveName As String,
                                          ByVal curvePoints As List(Of PointCoordinate3D))
        If curvePoints.Count < 2 Then
            Throw New InvalidOperationException("At least two points are required for " & curveName & ".")
        End If

        Dim curveSpline As Object = hybridShapeFactory.AddNewSpline()
        TrySetSplineOptions(curveSpline)
        TrySetName(curveSpline, curveName)

        For pointIndex As Integer = 0 To curvePoints.Count - 1
            Dim coordinate As PointCoordinate3D = curvePoints(pointIndex)
            Dim curvePoint As Object = hybridShapeFactory.AddNewPointCoord(coordinate.X,
                                                                           coordinate.Y,
                                                                           coordinate.Z)
            TrySetName(curvePoint, curveName & "_P" & (pointIndex + 1).ToString("000"))
            targetSet.AppendHybridShape(curvePoint)
            curveSpline.AddPoint(part.CreateReferenceFromObject(curvePoint))
        Next

        targetSet.AppendHybridShape(curveSpline)
        RequireUpdateObject(part, curveSpline, curveName)
    End Sub

    Private Sub AddLoftSection(ByVal loft As HybridShapeLoft,
                               ByVal profileReference As Reference,
                               ByVal closingPointReference As Object)
        Dim couplingReference As Reference = CType(Nothing, Reference)

        If closingPointReference IsNot Nothing Then
            couplingReference = CType(closingPointReference, Reference)
        End If

        loft.AddSectionToLoft(profileReference, 1, couplingReference)
    End Sub

    Private Sub AddPhysicalRibBody(ByVal part As Object,
                                   ByVal hybridShapeFactory As Object,
                                   ByVal shapeFactory As Object,
                                   ByVal ribPlaneSet As Object,
                                   ByVal station As WingStation,
                                   Optional ByVal includeRibCutouts As Boolean = False,
                                   Optional ByVal includeAileronSplit As Boolean = False)
        Dim ribPlane As Object = CreateRibMidPlane(part, hybridShapeFactory, ribPlaneSet, station)

        If includeAileronSplit Then
            AddPhysicalRibBodySection(part,
                                      shapeFactory,
                                      ribPlane,
                                      station,
                                      includeRibCutouts,
                                      RibProfileRegion.ForwardWingPanel)
            Return
        End If

        AddPhysicalRibBodySection(part,
                                  shapeFactory,
                                  ribPlane,
                                  station,
                                  includeRibCutouts,
                                  RibProfileRegion.Full)
    End Sub

    Private Sub AddPhysicalRibBodySection(ByVal part As Object,
                                          ByVal shapeFactory As Object,
                                          ByVal ribPlane As Object,
                                          ByVal station As WingStation,
                                          ByVal includeRibCutouts As Boolean,
                                          ByVal ribProfileRegion As RibProfileRegion)
        Dim ribBody As Object = part.Bodies.Add()
        TrySetName(ribBody, station.Name & GetRibBodyNameSuffix(ribProfileRegion))
        TrySetInWorkObject(part, ribBody)

        Dim ribSketch As Object = CreateRibProfileSketch(part,
                                                         ribBody,
                                                         ribPlane,
                                                         station,
                                                         includeRibCutouts,
                                                         ribProfileRegion)
        Dim ribName As String = station.Name & GetRibPadNameSuffix(ribProfileRegion)
        Dim ribPad As Object = CreateRibPad(part, ribBody, shapeFactory, ribSketch, ribName)

        TrySetName(ribPad, ribName & " 3 mm centered rib")
        RequireUpdateObject(part, ribPad, ribName & " physical rib")
    End Sub

    Private Function GetRibBodyNameSuffix(ByVal ribProfileRegion As RibProfileRegion) As String
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return " 3 mm forward wing rib"
            Case Else
                Return " 3 mm rib"
        End Select
    End Function

    Private Function GetRibPadNameSuffix(ByVal ribProfileRegion As RibProfileRegion) As String
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return " forward wing section"
            Case Else
                Return String.Empty
        End Select
    End Function

    Private Function CreateRibMidPlane(ByVal part As Object,
                                       ByVal hybridShapeFactory As Object,
                                       ByVal ribPlaneSet As Object,
                                       ByVal station As WingStation) As Object
        Dim zxPlane As Object = part.OriginElements.PlaneZX

        If Math.Abs(station.SpanPosition) < 0.000001 Then
            Return zxPlane
        End If

        Dim zxPlaneReference As Object = part.CreateReferenceFromObject(zxPlane)
        Dim ribPlane As Object = hybridShapeFactory.AddNewPlaneOffset(zxPlaneReference,
                                                                      station.SpanPosition,
                                                                      False)
        TrySetName(ribPlane, station.Name & " mid-plane")
        ribPlaneSet.AppendHybridShape(ribPlane)
        RequireUpdateObject(part, ribPlane, station.Name & " rib mid-plane")

        Return ribPlane
    End Function

    Private Function CreateRibProfileSketch(ByVal part As Object,
                                            ByVal ribBody As Object,
                                            ByVal ribPlane As Object,
                                            ByVal station As WingStation,
                                            ByVal includeRibCutouts As Boolean,
                                            ByVal ribProfileRegion As RibProfileRegion) As Object
        Dim chordLength As Double = WingDefinition.GetChordAtSpanPosition(station.SpanPosition)

        Dim airfoilCoordinates As List(Of AirfoilCoordinate) =
            NacaAirfoil.BuildCoordinates(chordLength,
                                         WingDefinition.PointCountPerSurface,
                                         WingDefinition.AirfoilMaximumCamber,
                                         WingDefinition.AirfoilMaximumCamberPosition,
                                         WingDefinition.AirfoilMaximumThickness,
                                         True)

        If ribProfileRegion = RibProfileRegion.ForwardWingPanel Then
            airfoilCoordinates = ClipAirfoilProfileByX(airfoilCoordinates,
                                                       WingDefinition.GetAileronFixedPanelEndXAtSpanPosition(station.SpanPosition),
                                                       True)
        End If

        If airfoilCoordinates.Count < 3 Then
            Throw New InvalidOperationException("The aileron rib split left fewer than three points for " & station.Name & ".")
        End If

        Dim sketches As Object = ribBody.Sketches
        Dim ribSketch As Object = CreateSketchOnPlane(part, sketches, ribPlane)
        TrySetName(ribSketch, station.Name & " rib profile")
        TrySetInWorkObject(part, ribSketch)

        Dim sketchAxis As SketchAxisData = GetSketchAxisData(ribSketch, station.SpanPosition)
        Dim sketchFactory As Object = ribSketch.OpenEdition()
        Dim sketchCoordinates As List(Of AirfoilCoordinate) =
            ConvertGlobalXzToSketchCoordinates(airfoilCoordinates, station.SpanPosition, sketchAxis)

        If ribProfileRegion = RibProfileRegion.Full Then
            CreateSmoothClosedRibSketchProfile(sketchFactory, sketchCoordinates)
        Else
            CreateClosedPolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        End If

        If includeRibCutouts Then
            CreateMainSparCutoutSketchProfile(sketchFactory, station, sketchAxis, ribProfileRegion)
            CreateRibLighteningCutoutSketchProfiles(sketchFactory, station, sketchAxis, ribProfileRegion)
        End If

        ribSketch.CloseEdition()
        RequireUpdateObject(part, ribSketch, station.Name & " rib sketch")
        RequireUpdatePart(part, station.Name & " rib sketch")

        Return ribSketch
    End Function

    Private Sub CreateMainSparCutoutSketchProfile(ByVal sketchFactory As Object,
                                                  ByVal station As WingStation,
                                                  ByVal sketchAxis As SketchAxisData,
                                                  ByVal ribProfileRegion As RibProfileRegion)
        Dim localCenterX As Double = WingDefinition.GetMainSparCenterLocalXAtSpanPosition(station.SpanPosition)
        Dim globalCenterX As Double = WingDefinition.GetGlobalXAtSpanPosition(station.SpanPosition,
                                                                               localCenterX)
        Dim radius As Double = WingDefinition.MainSparCutoutDiameter / 2.0

        If Not IsCircularCutoutInsideRibRegion(localCenterX,
                                               radius,
                                               station.SpanPosition,
                                               ribProfileRegion) Then
            Return
        End If

        Dim sparCenter As AirfoilCoordinate =
            ConvertGlobalPointToSketchPoint(globalCenterX,
                                            station.SpanPosition,
                                            WingDefinition.GetMainSparCenterZAtSpanPosition(station.SpanPosition),
                                            sketchAxis)

        CreateSketchCircle(sketchFactory,
                           sparCenter,
                           radius)
    End Sub

    Private Sub CreateRibLighteningCutoutSketchProfiles(ByVal sketchFactory As Object,
                                                        ByVal station As WingStation,
                                                        ByVal sketchAxis As SketchAxisData,
                                                        ByVal ribProfileRegion As RibProfileRegion)
        For Each cutoutDefinition As WingDefinition.RibLighteningCutoutDefinition In WingDefinition.GetRibLighteningCutouts()
            Dim cutoutDiameter As Double =
                WingDefinition.GetRibLighteningCutoutDiameter(station.SpanPosition, cutoutDefinition)

            If cutoutDiameter <= 0.0 Then
                Continue For
            End If

            Dim radius As Double = cutoutDiameter / 2.0
            Dim localCenterX As Double =
                WingDefinition.GetRibLighteningCutoutCenterLocalX(station.SpanPosition, cutoutDefinition)

            If Not IsCircularCutoutInsideRibRegion(localCenterX,
                                                   radius,
                                                   station.SpanPosition,
                                                   ribProfileRegion) Then
                Continue For
            End If

            Dim globalCenterX As Double =
                WingDefinition.GetGlobalXAtSpanPosition(station.SpanPosition, localCenterX)
            Dim cutoutCenter As AirfoilCoordinate =
                ConvertGlobalPointToSketchPoint(globalCenterX,
                                                station.SpanPosition,
                                                WingDefinition.GetRibLighteningCutoutCenterZ(station.SpanPosition, cutoutDefinition),
                                                sketchAxis)

            CreateSketchCircle(sketchFactory, cutoutCenter, radius)
        Next
    End Sub

    Private Function IsCircularCutoutInsideRibRegion(ByVal localCenterX As Double,
                                                     ByVal radius As Double,
                                                     ByVal spanPosition As Double,
                                                     ByVal ribProfileRegion As RibProfileRegion) As Boolean
        Select Case ribProfileRegion
            Case RibProfileRegion.ForwardWingPanel
                Return (localCenterX + radius) <
                    (WingDefinition.GetAileronFixedPanelEndXAtSpanPosition(spanPosition) - 0.000001)
            Case Else
                Return True
        End Select
    End Function

    Private Sub CreateSketchCircle(ByVal sketchFactory As Object,
                                   ByVal centerPoint As AirfoilCoordinate,
                                   ByVal radius As Double)
        Try
            sketchFactory.CreateClosedCircle(centerPoint.X, centerPoint.Y, radius)
            Return
        Catch
        End Try

        CreatePolygonCircleSketchProfile(sketchFactory, centerPoint, radius, 48)
    End Sub

    Private Sub CreatePolygonCircleSketchProfile(ByVal sketchFactory As Object,
                                                 ByVal centerPoint As AirfoilCoordinate,
                                                 ByVal radius As Double,
                                                 ByVal segmentCount As Integer)
        If segmentCount < 12 Then
            segmentCount = 12
        End If

        Dim circlePoints As New List(Of AirfoilCoordinate)()

        For pointIndex As Integer = 0 To segmentCount - 1
            Dim angle As Double = (2.0 * Math.PI * CDbl(pointIndex)) / CDbl(segmentCount)
            circlePoints.Add(New AirfoilCoordinate(centerPoint.X + (Math.Cos(angle) * radius),
                                                   centerPoint.Y + (Math.Sin(angle) * radius)))
        Next

        For pointIndex As Integer = 0 To circlePoints.Count - 1
            Dim nextIndex As Integer = (pointIndex + 1) Mod circlePoints.Count
            CreateSketchLineIfDistinct(sketchFactory,
                                       circlePoints(pointIndex),
                                       circlePoints(nextIndex))
        Next
    End Sub

    Private Function CreateSketchOnPlane(ByVal part As Object,
                                         ByVal sketches As Object,
                                         ByVal sketchPlane As Object) As Object
        Try
            Dim sketchPlaneReference As Object = part.CreateReferenceFromObject(sketchPlane)
            Return sketches.Add(sketchPlaneReference)
        Catch
            Return sketches.Add(sketchPlane)
        End Try
    End Function

    Private Function ConvertGlobalXzToSketchCoordinates(ByVal airfoilCoordinates As List(Of AirfoilCoordinate),
                                                        ByVal spanPosition As Double,
                                                        ByVal sketchAxis As SketchAxisData) As List(Of AirfoilCoordinate)
        Dim sketchCoordinates As New List(Of AirfoilCoordinate)()

        For Each airfoilPoint As AirfoilCoordinate In airfoilCoordinates
            sketchCoordinates.Add(ConvertGlobalXzToSketchPoint(airfoilPoint, spanPosition, sketchAxis))
        Next

        Return sketchCoordinates
    End Function

    Private Function ClipAirfoilProfileByX(ByVal airfoilCoordinates As List(Of AirfoilCoordinate),
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

    Private Function GetAirfoilSurfacePointAtLocalX(ByVal chordLength As Double,
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

    Private Function BuildFixedWingOutboardSkinProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                     0.0,
                                                     WingDefinition.AileronFixedPanelEndX)
    End Function

    Private Function BuildAileronSkinProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return RotateProfileCoordinatesToMaximumX(
            BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                  WingDefinition.AileronPanelStartX,
                                                  chordLength))
    End Function

    Private Function BuildAileronRearHingeSparProfileCoordinates(ByVal chordLength As Double) As List(Of AirfoilCoordinate)
        Return BuildAirfoilSegmentProfileCoordinates(chordLength,
                                                     WingDefinition.AileronFixedPanelEndX,
                                                     WingDefinition.AileronRearSparEndX)
    End Function

    Private Function BuildAirfoilSurfaceProfileCoordinates(ByVal chordLength As Double,
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

    Private Function BuildAirfoilSegmentProfileCoordinates(ByVal chordLength As Double,
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

    Private Sub CreateSmoothClosedRibSketchProfile(ByVal sketchFactory As Object,
                                                   ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        If sketchCoordinates.Count < 3 Then
            Throw New InvalidOperationException("At least three points are required to create a closed rib profile.")
        End If

        Dim sketchPoints As New List(Of Object)()

        For Each sketchCoordinate As AirfoilCoordinate In sketchCoordinates
            sketchPoints.Add(sketchFactory.CreatePoint(sketchCoordinate.X, sketchCoordinate.Y))
        Next

        Try
            Dim sketchPointArray(sketchPoints.Count - 1) As Object

            For pointIndex As Integer = 0 To sketchPoints.Count - 1
                sketchPointArray(pointIndex) = sketchPoints(pointIndex)
            Next

            sketchFactory.CreateSpline(sketchPointArray)
        Catch
            CreatePolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        End Try

        Dim lastPoint As AirfoilCoordinate = sketchCoordinates(sketchCoordinates.Count - 1)
        Dim firstPoint As AirfoilCoordinate = sketchCoordinates(0)
        CreateSketchLineIfDistinct(sketchFactory, lastPoint, firstPoint)
    End Sub

    Private Sub CreatePolylineRibSketchProfile(ByVal sketchFactory As Object,
                                               ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        For pointIndex As Integer = 0 To sketchCoordinates.Count - 2
            CreateSketchLineIfDistinct(sketchFactory,
                                       sketchCoordinates(pointIndex),
                                       sketchCoordinates(pointIndex + 1))
        Next
    End Sub

    Private Sub CreateClosedPolylineRibSketchProfile(ByVal sketchFactory As Object,
                                                     ByVal sketchCoordinates As List(Of AirfoilCoordinate))
        CreatePolylineRibSketchProfile(sketchFactory, sketchCoordinates)
        CreateSketchLineIfDistinct(sketchFactory,
                                   sketchCoordinates(sketchCoordinates.Count - 1),
                                   sketchCoordinates(0))
    End Sub

    Private Sub CreateSketchLineIfDistinct(ByVal sketchFactory As Object,
                                           ByVal startPoint As AirfoilCoordinate,
                                           ByVal endPoint As AirfoilCoordinate)
        If Not AreSketchPointsCoincident(startPoint, endPoint) Then
            sketchFactory.CreateLine(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y)
        End If
    End Sub

    Private Function CreateRibPad(ByVal part As Object,
                                  ByVal ribBody As Object,
                                  ByVal shapeFactory As Object,
                                  ByVal ribSketch As Object,
                                  ByVal ribName As String) As Object
        Dim halfThickness As Double = WingDefinition.RibThickness / 2.0

        TrySetInWorkObject(part, ribBody)
        RequireUpdateObject(part, ribSketch, ribName & " rib sketch before pad")
        RequireUpdatePart(part, ribName & " rib body before pad")

        Try
            Dim ribPad As Object = shapeFactory.AddNewPad(ribSketch, halfThickness)
            RequireCenteredPad(ribPad, WingDefinition.RibThickness, ribName & " rib pad")

            Return ribPad
        Catch firstException As COMException
            Try
                Dim ribSketchReference As Object = part.CreateReferenceFromObject(ribSketch)
                Dim ribPad As Object = shapeFactory.AddNewPadFromRef(ribSketchReference, halfThickness)
                RequireCenteredPad(ribPad, WingDefinition.RibThickness, ribName & " rib pad")

                Return ribPad
            Catch
                Throw New InvalidOperationException("CATIA could not create the 3 mm rib pad for " & ribName & ". Check that the rib sketch is closed and that its body is active.", firstException)
            End Try
        End Try
    End Function

    Private Function AddMainSparBody(ByVal part As Object,
                                      ByVal hybridShapeFactory As Object,
                                      ByVal shapeFactory As Object,
                                      ByVal sparReferenceSet As Object) As Object
        If WingDefinition.MainSparInnerDiameter <= 0.0 Then
            Throw New InvalidOperationException("The main spar wall thickness leaves no hollow inside diameter.")
        End If

        Dim sparBody As Object = part.Bodies.Add()
        TrySetName(sparBody, "Main spar 30 percent chord hollow tube")
        TrySetInWorkObject(part, sparBody)

        Dim profileSketch As Object = CreateMainSparProfileSketch(part, sparBody)

        Dim rightPath As Object = CreateMainSparPathLine(part,
                                                         hybridShapeFactory,
                                                         sparReferenceSet,
                                                         "Right main spar 30 percent chord path",
                                                         0.0,
                                                         WingDefinition.HalfSpan)
        Dim leftPath As Object = CreateMainSparPathLine(part,
                                                        hybridShapeFactory,
                                                        sparReferenceSet,
                                                        "Left main spar 30 percent chord path",
                                                        0.0,
                                                        -WingDefinition.HalfSpan)

        CreateMainSparRibFeature(part,
                                 shapeFactory,
                                 sparBody,
                                 profileSketch,
                                 rightPath,
                                 "Right main spar hollow tube")
        CreateMainSparRibFeature(part,
                                 shapeFactory,
                                 sparBody,
                                 profileSketch,
                                 leftPath,
                                 "Left main spar hollow tube")

        RequireUpdatePart(part, "main spar hollow tube")

        Return sparBody
    End Function

    Private Function CreateMainSparProfileSketch(ByVal part As Object,
                                                 ByVal sparBody As Object) As Object
        Dim sketches As Object = sparBody.Sketches
        Dim centerPlane As Object = part.OriginElements.PlaneZX
        Dim profileSketch As Object = CreateSketchOnPlane(part, sketches, centerPlane)
        TrySetName(profileSketch, "Main spar hollow tube profile")
        TrySetInWorkObject(part, profileSketch)

        Dim sketchAxis As SketchAxisData = GetSketchAxisData(profileSketch, 0.0)
        Dim sketchFactory As Object = profileSketch.OpenEdition()
        Dim sparCenter As AirfoilCoordinate =
            ConvertGlobalPointToSketchPoint(WingDefinition.GetMainSparCenterXAtSpanPosition(0.0),
                                            0.0,
                                            WingDefinition.GetMainSparCenterZAtSpanPosition(0.0),
                                            sketchAxis)

        CreateSketchCircle(sketchFactory,
                           sparCenter,
                           WingDefinition.MainSparOuterDiameter / 2.0)
        CreateSketchCircle(sketchFactory,
                           sparCenter,
                           WingDefinition.MainSparInnerDiameter / 2.0)

        profileSketch.CloseEdition()
        RequireUpdateObject(part, profileSketch, "main spar hollow tube profile")
        RequireUpdatePart(part, "main spar hollow tube profile")

        Return profileSketch
    End Function

    Private Function CreateMainSparPathLine(ByVal part As Object,
                                            ByVal hybridShapeFactory As Object,
                                            ByVal targetSet As Object,
                                            ByVal pathName As String,
                                            ByVal startSpanPosition As Double,
                                            ByVal endSpanPosition As Double) As Object
        Dim startPoint As Object = CreateMainSparPathPoint(part,
                                                           hybridShapeFactory,
                                                           targetSet,
                                                           pathName & " start",
                                                           startSpanPosition)
        Dim endPoint As Object = CreateMainSparPathPoint(part,
                                                         hybridShapeFactory,
                                                         targetSet,
                                                         pathName & " end",
                                                         endSpanPosition)

        Dim startReference As Object = part.CreateReferenceFromObject(startPoint)
        Dim endReference As Object = part.CreateReferenceFromObject(endPoint)
        Dim sparPath As Object = hybridShapeFactory.AddNewLinePtPt(startReference, endReference)
        TrySetName(sparPath, pathName)
        targetSet.AppendHybridShape(sparPath)
        RequireUpdateObject(part, sparPath, pathName)

        Return sparPath
    End Function

    Private Function CreateMainSparPathPoint(ByVal part As Object,
                                             ByVal hybridShapeFactory As Object,
                                             ByVal targetSet As Object,
                                             ByVal pointName As String,
                                             ByVal spanPosition As Double) As Object
        Dim sparPoint As Object =
            hybridShapeFactory.AddNewPointCoord(WingDefinition.GetMainSparCenterXAtSpanPosition(spanPosition),
                                                spanPosition,
                                                WingDefinition.GetMainSparCenterZAtSpanPosition(spanPosition))
        TrySetName(sparPoint, pointName)
        targetSet.AppendHybridShape(sparPoint)
        RequireUpdateObject(part, sparPoint, pointName)

        Return sparPoint
    End Function

    Private Function CreateMainSparRibFeature(ByVal part As Object,
                                              ByVal shapeFactory As Object,
                                              ByVal sparBody As Object,
                                              ByVal profileSketch As Object,
                                              ByVal sparPath As Object,
                                              ByVal sparName As String) As Object
        TrySetInWorkObject(part, sparBody)

        Try
            Dim profileReference As Object = part.CreateReferenceFromObject(profileSketch)
            Dim pathReference As Object = part.CreateReferenceFromObject(sparPath)
            Dim sparFeature As Object = shapeFactory.AddNewRibFromRef(profileReference, pathReference)
            TrySetName(sparFeature, sparName)
            RequireUpdateObject(part, sparFeature, sparName)

            Return sparFeature
        Catch firstException As Exception
            Try
                Dim sparFeature As Object = shapeFactory.AddNewRib(profileSketch, sparPath)
                TrySetName(sparFeature, sparName)
                RequireUpdateObject(part, sparFeature, sparName)

                Return sparFeature
            Catch
                Throw New InvalidOperationException("CATIA could not create the " & sparName & ". Check that the main spar profile is closed and that the spar path intersects the profile center.", firstException)
            End Try
        End Try
    End Function

    Private Function AreSketchPointsCoincident(ByVal firstPoint As AirfoilCoordinate,
                                               ByVal secondPoint As AirfoilCoordinate) As Boolean
        Return (Math.Abs(firstPoint.X - secondPoint.X) < 0.000001) AndAlso _
            (Math.Abs(firstPoint.Y - secondPoint.Y) < 0.000001)
    End Function

    Private Function ConvertGlobalXzToSketchPoint(ByVal airfoilPoint As AirfoilCoordinate,
                                                  ByVal spanPosition As Double,
                                                  ByVal sketchAxis As SketchAxisData) As AirfoilCoordinate
        Return ConvertGlobalPointToSketchPoint(WingDefinition.GetGlobalXAtSpanPosition(spanPosition,
                                                                                       airfoilPoint.X),
                                               spanPosition,
                                               WingDefinition.GetGlobalZAtSpanPosition(spanPosition,
                                                                                       airfoilPoint.Y),
                                               sketchAxis)
    End Function

    Private Function ConvertGlobalPointToSketchPoint(ByVal globalX As Double,
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

    Private Function GetSketchAxisData(ByVal sketch As Object,
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

    Private Structure SketchAxisData
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
End Module
