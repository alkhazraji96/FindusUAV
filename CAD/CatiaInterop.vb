Imports System.Runtime.InteropServices

Friend Module CatiaInterop
    Private Const CatVisPropertyNoShow As Integer = 1

    Friend Function GetOrCreateCatiaApplication() As Object
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

    Friend Function GetMainBody(ByVal part As Object) As Object
        Try
            Return part.MainBody
        Catch
        End Try

        Try
            Return part.Bodies.Item("PartBody")
        Catch
        End Try

        Return part.Bodies.Item(1)
    End Function

    Friend Sub TrySetName(ByVal catiaObject As Object, ByVal name As String)
        Try
            catiaObject.Name = name
        Catch
        End Try
    End Sub

    Friend Sub TrySetPartNumber(ByVal partDocument As Object, ByVal partNumber As String)
        Try
            partDocument.Product.PartNumber = partNumber
        Catch
        End Try
    End Sub

    Friend Sub TrySetInWorkObject(ByVal part As Object, ByVal inWorkObject As Object)
        Try
            part.InWorkObject = inWorkObject
        Catch
        End Try
    End Sub

    Friend Sub TryUpdateObject(ByVal part As Object, ByVal partObject As Object)
        Try
            part.UpdateObject(partObject)
        Catch
        End Try
    End Sub

    Friend Sub TryUpdatePart(ByVal part As Object)
        Try
            part.Update()
        Catch
        End Try
    End Sub

    Friend Sub RequireUpdateObject(ByVal part As Object,
                                   ByVal partObject As Object,
                                   ByVal context As String)
        Try
            part.UpdateObject(partObject)
        Catch ex As Exception
            Throw New InvalidOperationException("CATIA failed to update " & context & ".", ex)
        End Try
    End Sub

    Friend Sub RequireUpdatePart(ByVal part As Object,
                                 ByVal context As String)
        Try
            part.Update()
        Catch ex As Exception
            Throw New InvalidOperationException("CATIA failed to update " & context & ".", ex)
        End Try
    End Sub

    Friend Sub TryReframe(ByVal catiaApplication As Object)
        Try
            catiaApplication.ActiveWindow.ActiveViewer.Reframe()
        Catch
        End Try
    End Sub

    Friend Sub TrySetSplineOptions(ByVal profileSpline As Object,
                                   Optional ByVal closeSpline As Boolean = False)
        Try
            profileSpline.SetSplineType(0)
        Catch
        End Try

        Try
            profileSpline.SetClosing(If(closeSpline, 1, 0))
        Catch
        End Try
    End Sub

    Friend Sub TrySetLoftOptions(ByVal loft As Object)
        Try
            loft.SectionCoupling = 1
        Catch
        End Try

        Try
            loft.Relimitation = 1
        Catch
        End Try

        Try
            loft.CanonicalDetection = 2
        Catch
        End Try
    End Sub

    Friend Sub TrySetObjectColor(ByVal partDocument As Object,
                                 ByVal catiaObject As Object,
                                 ByVal red As Integer,
                                 ByVal green As Integer,
                                 ByVal blue As Integer)
        Try
            Dim selection As Object = partDocument.Selection
            selection.Clear()
            selection.Add(catiaObject)
            selection.VisProperties.SetRealColor(red, green, blue, 1)
            selection.Clear()
        Catch
        End Try
    End Sub

    Friend Sub TryHideObject(ByVal partDocument As Object,
                             ByVal catiaObject As Object)
        TrySetObjectShowState(partDocument, catiaObject, CatVisPropertyNoShow)
    End Sub

    Friend Sub TryHideSketchesInBodies(ByVal partDocument As Object,
                                       ByVal part As Object)
        Try
            Dim bodies As Object = part.Bodies

            For bodyIndex As Integer = 1 To CInt(bodies.Count)
                TryHideSketchesInBody(partDocument, bodies.Item(bodyIndex))
            Next
        Catch
        End Try
    End Sub

    Friend Sub TryHideHybridShapesByNameEnding(ByVal partDocument As Object,
                                               ByVal hybridBody As Object,
                                               ByVal nameSuffix As String)
        If String.IsNullOrWhiteSpace(nameSuffix) Then
            Return
        End If

        Try
            Dim hybridShapes As Object = hybridBody.HybridShapes

            For shapeIndex As Integer = 1 To CInt(hybridShapes.Count)
                Dim hybridShape As Object = hybridShapes.Item(shapeIndex)
                Dim hybridShapeName As String = TryGetCatiaObjectName(hybridShape)

                If hybridShapeName.EndsWith(nameSuffix, StringComparison.OrdinalIgnoreCase) Then
                    TryHideObject(partDocument, hybridShape)
                End If
            Next
        Catch
        End Try
    End Sub

    Private Sub TrySetObjectShowState(ByVal partDocument As Object,
                                      ByVal catiaObject As Object,
                                      ByVal showState As Integer)
        If partDocument Is Nothing OrElse catiaObject Is Nothing Then
            Return
        End If

        Try
            Dim selection As Object = partDocument.Selection
            selection.Clear()
            selection.Add(catiaObject)
            selection.VisProperties.SetShow(showState)
            selection.Clear()
        Catch
        End Try
    End Sub

    Private Sub TryHideSketchesInBody(ByVal partDocument As Object,
                                      ByVal body As Object)
        Try
            Dim sketches As Object = body.Sketches

            For sketchIndex As Integer = 1 To CInt(sketches.Count)
                TryHideObject(partDocument, sketches.Item(sketchIndex))
            Next
        Catch
        End Try
    End Sub

    Private Function TryGetCatiaObjectName(ByVal catiaObject As Object) As String
        Try
            Return CStr(catiaObject.Name)
        Catch
            Return String.Empty
        End Try
    End Function

    Friend Sub RequireCenteredPad(ByVal pad As Object,
                                  ByVal thickness As Double,
                                  ByVal context As String)
        Dim halfThickness As Double = thickness / 2.0
        Dim symmetricException As Exception = Nothing

        Try
            pad.FirstLimit.Dimension.Value = halfThickness
            pad.IsSymmetric = True
            Return
        Catch ex As Exception
            symmetricException = ex
        End Try

        Try
            pad.FirstLimit.Dimension.Value = halfThickness
            pad.SecondLimit.Dimension.Value = halfThickness
            Return
        Catch ex As Exception
            Dim innerException As Exception = If(symmetricException IsNot Nothing,
                                                symmetricException,
                                                ex)
            Throw New InvalidOperationException("CATIA could not center " & context & " to " & thickness.ToString() & " mm thickness.", innerException)
        End Try
    End Sub

End Module
