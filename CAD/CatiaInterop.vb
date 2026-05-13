Imports System.Runtime.InteropServices

Friend Module CatiaInterop
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

    Friend Function TrySetPadSymmetric(ByVal pad As Object) As Boolean
        Try
            pad.IsSymmetric = True
            Return True
        Catch
        End Try

        Return False
    End Function

    Friend Sub TrySetPadFirstLimit(ByVal pad As Object, ByVal length As Double)
        Try
            pad.FirstLimit.Dimension.Value = length
        Catch
        End Try
    End Sub

End Module
