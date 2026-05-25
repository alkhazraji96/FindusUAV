Imports System.Collections.Generic

Friend Module WingGenerator
    Private Const WingOperationName As String = "Wing generation"

    Private Sub ApplyWingConfiguration(ByVal configuration As WingConfiguration)
        WingDefinition.UseConfiguration(configuration)
    End Sub

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
                                    ByVal workflowName As String,
                                    Optional ByVal elapsedMilliseconds As Long = -1)
        Dim message As String = workflowName & " complete."

        If elapsedMilliseconds >= 0 Then
            message &= " Elapsed: " & FormatWingElapsedMilliseconds(elapsedMilliseconds) & "."
        End If

        GenerationProgress.Report(progressReporter,
                                  GenerationProgressUpdate.CreateCompleted(WingOperationName,
                                                                           message))
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
        Dim constructionObjects As New List(Of Object)()

        If constructionSets IsNot Nothing Then
            For Each constructionSet As Object In constructionSets
                constructionObjects.Add(constructionSet)
            Next
        End If

        TryHideObjects(partDocument, constructionObjects)
        TryHideSketchesInBodies(partDocument, part)
        TryUpdatePart(part)
    End Sub

    Friend Sub HideWingStationProfileConstruction(ByVal partDocument As Object,
                                                  ByVal stationProfiles As List(Of WingStationProfile))
        If stationProfiles Is Nothing Then
            Return
        End If

        Dim constructionObjects As New List(Of Object)()

        For Each stationProfile As WingStationProfile In stationProfiles
            If stationProfile.ConstructionGeometry Is Nothing Then
                Continue For
            End If

            For Each constructionObject As Object In stationProfile.ConstructionGeometry
                constructionObjects.Add(constructionObject)
            Next
        Next

        TryHideObjects(partDocument, constructionObjects)
    End Sub

    Private Function FormatWingElapsedMilliseconds(ByVal elapsedMilliseconds As Long) As String
        Dim elapsed As TimeSpan = TimeSpan.FromMilliseconds(Math.Max(0, elapsedMilliseconds))

        If elapsed.TotalMinutes >= 1.0 Then
            Return CInt(Math.Floor(elapsed.TotalMinutes)).ToString() &
                "m " &
                elapsed.Seconds.ToString() &
                "s"
        End If

        Return elapsed.TotalSeconds.ToString("0.0") & "s"
    End Function

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
        Dim workflowStopwatch As Stopwatch = Stopwatch.StartNew()

        ReportWingStarting(activeProgressReporter, workflowName)

        Try
            ApplyWingConfiguration(configuration)
            Dim partDocument As Object = CreatePhysicalRibsMainSparAndAileronsCore(activeProgressReporter)
            ReportWingCompleted(activeProgressReporter, workflowName, workflowStopwatch.ElapsedMilliseconds)
            Return partDocument
        Catch ex As Exception
            ReportWingFailed(activeProgressReporter, workflowName, ex)
            Throw
        End Try
    End Function

    Private Function CreatePhysicalRibsMainSparAndAileronsCore(ByVal progressReporter As IGenerationProgressReporter) As Object
        Const totalSteps As Integer = 9

        ReportWingStep(progressReporter, "CATIA setup", "Connecting to CATIA and creating the wing part.", 1, totalSteps)

        Dim catiaApplication As Object = GetOrCreateCatiaApplication()
        catiaApplication.Visible = True
        Dim partDocument As Object = Nothing

        Using displayRefreshScope As IDisposable = SuspendCatiaDisplayRefresh(catiaApplication)
            partDocument = catiaApplication.Documents.Add("Part")
            TrySetPartNumber(partDocument, "Tapered_Wing_Ribs_Spar_And_Ailerons")

            Dim part As Object = partDocument.Part
            TrySetName(part, "Tapered_Wing_Ribs_Spar_And_Ailerons")

            Dim hybridBodies As Object = part.HybridBodies
            ReportWingStep(progressReporter, "Geometry sets", "Creating CATIA geometry sets.", 2, totalSteps)

            Dim planformSet As Object = hybridBodies.Add()
            TrySetName(planformSet, "Planform and Rib Stations")

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

            ReportWingStep(progressReporter, "Wing skins", "Creating split fixed-wing and aileron skin surfaces.", 4, totalSteps)
            CreateSplitSkinSurfaces(partDocument,
                                     part,
                                     hybridShapeFactory,
                                     shapeFactory,
                                     skinSet,
                                     aileronSkinSet,
                                     stations)

            ReportWingStep(progressReporter, "Aileron spars", "Creating aileron rear hinge spars.", 5, totalSteps)
            AddAileronRearHingeSpars(part,
                                      hybridShapeFactory,
                                      shapeFactory,
                                      aileronRearSparSet)

            ReportWingStep(progressReporter, "Aileron references", "Creating aileron cut reference geometry.", 6, totalSteps)
            AddAileronCutReferenceGeometry(part, hybridShapeFactory, aileronReferenceSet)

            ReportWingStep(progressReporter, "Ribs", "Creating physical ribs with spar and lightening cutouts.", 7, totalSteps)
            For Each station As WingStation In stations
                AddPhysicalRibBody(part,
                                   hybridShapeFactory,
                                   shapeFactory,
                                   ribPlaneSet,
                                   station,
                                   True,
                                   WingDefinition.IsWithinAileronSpan(station.SpanPosition))
            Next

            ReportWingStep(progressReporter, "Main spar", "Creating hollow main spar.", 8, totalSteps)
            Dim mainSpar As Object = AddMainSparBody(part,
                                                     hybridShapeFactory,
                                                     shapeFactory,
                                                     sparReferenceSet)

            ReportWingStep(progressReporter, "Final update", "Updating CATIA part.", 9, totalSteps)
            TrySetInWorkObject(part, mainSpar)
            RequireUpdatePart(part, "wing with physical ribs, main spar, and aileron cuts")
            HideWingConstructionGeometry(partDocument,
                                         part,
                                         planformSet,
                                         ribPlaneSet,
                                         sparReferenceSet,
                                         aileronReferenceSet,
                                         aileronRearSparSet)
        End Using

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

End Module
