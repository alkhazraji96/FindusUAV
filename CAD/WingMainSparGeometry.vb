Friend Module WingMainSparGeometry
    Friend Function AddMainSparBody(ByVal part As Object,
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
End Module
