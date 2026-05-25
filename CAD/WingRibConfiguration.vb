Imports System.Collections.Generic

Friend Class WingRibConfiguration
    Private Const ForwardLighteningCutoutIndex As Integer = 0
    Private Const MiddleLighteningCutoutIndex As Integer = 1
    Private Const AftLighteningCutoutIndex As Integer = 2

    Public Property CountPerSide As Integer
    Public Property Thickness As Double
    Public Property LighteningCutoutsEnabled As Boolean
    Public Property LighteningCutouts As List(Of RibLighteningCutoutConfiguration)

    Public Sub New()
        CountPerSide = 14
        Thickness = 3.0
        LighteningCutoutsEnabled = True
        LighteningCutouts = RibLighteningCutoutConfiguration.CreateDefaultWingCutouts()
    End Sub

    Public Shared Function CreateDefault() As WingRibConfiguration
        Return New WingRibConfiguration()
    End Function

    Public Function GetForwardLighteningCutout() As RibLighteningCutoutConfiguration
        Return GetLighteningCutout(ForwardLighteningCutoutIndex)
    End Function

    Public Function GetMiddleLighteningCutout() As RibLighteningCutoutConfiguration
        Return GetLighteningCutout(MiddleLighteningCutoutIndex)
    End Function

    Public Function GetAftLighteningCutout() As RibLighteningCutoutConfiguration
        Return GetLighteningCutout(AftLighteningCutoutIndex)
    End Function

    Public Function GetLighteningCutout(ByVal cutoutIndex As Integer) As RibLighteningCutoutConfiguration
        EnsureLighteningCutoutSlots()
        Return LighteningCutouts(cutoutIndex)
    End Function

    Public Sub EnsureLighteningCutoutSlots()
        Dim defaultCutouts As List(Of RibLighteningCutoutConfiguration) =
            RibLighteningCutoutConfiguration.CreateDefaultWingCutouts()

        If LighteningCutouts Is Nothing Then
            LighteningCutouts = New List(Of RibLighteningCutoutConfiguration)()
        End If

        For cutoutIndex As Integer = 0 To defaultCutouts.Count - 1
            If LighteningCutouts.Count <= cutoutIndex Then
                LighteningCutouts.Add(defaultCutouts(cutoutIndex))
            ElseIf LighteningCutouts(cutoutIndex) Is Nothing Then
                LighteningCutouts(cutoutIndex) = defaultCutouts(cutoutIndex)
            End If
        Next
    End Sub
End Class
