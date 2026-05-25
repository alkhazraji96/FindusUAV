Friend Class AirfoilConfiguration
    Public Property NacaCode As String
    Public Property MaximumCamber As Double
    Public Property MaximumCamberPosition As Double
    Public Property MaximumThickness As Double

    Public Sub New()
        Me.New("NACA 0012", 0.0, 0.0, 0.12)
    End Sub

    Public Sub New(ByVal nacaCode As String,
                   ByVal maximumCamber As Double,
                   ByVal maximumCamberPosition As Double,
                   ByVal maximumThickness As Double)
        Me.NacaCode = nacaCode
        Me.MaximumCamber = maximumCamber
        Me.MaximumCamberPosition = maximumCamberPosition
        Me.MaximumThickness = maximumThickness
    End Sub

    Public Shared Function CreateNaca4415() As AirfoilConfiguration
        Return NacaAirfoilParser.Parse("NACA 4415")
    End Function

    Public Shared Function CreateNaca0012() As AirfoilConfiguration
        Return NacaAirfoilParser.Parse("NACA 0012")
    End Function

    Public Shared Function FromNacaCode(ByVal nacaCode As String) As AirfoilConfiguration
        Return NacaAirfoilParser.Parse(nacaCode)
    End Function
End Class
