Friend Class AircraftConfiguration
    Public Property Wing As WingConfiguration
    Public Property Tail As TailConfiguration

    Public Sub New()
        Wing = WingConfiguration.CreateDefault()
        Tail = TailConfiguration.CreateDefault()
    End Sub

    Public Shared Function CreateDefault() As AircraftConfiguration
        Return New AircraftConfiguration()
    End Function
End Class
