Friend Module WingGenerationNames
    Friend Function GetWingAirfoilLabel() As String
        Dim configuration As WingConfiguration = WingDefinition.Configuration

        If configuration Is Nothing OrElse
            configuration.Airfoil Is Nothing OrElse
            String.IsNullOrWhiteSpace(configuration.Airfoil.NacaCode) Then
            Return "NACA airfoil"
        End If

        Return configuration.Airfoil.NacaCode.Trim()
    End Function

    Friend Function GetWingAirfoilIdentifier() As String
        Return GetWingAirfoilLabel().Replace(" ", "")
    End Function

    Friend Function GetStationProfileSetName() As String
        Return GetWingAirfoilLabel() & " Station Profiles"
    End Function

    Friend Function GetDefaultOuterWingSkinName() As String
        Return GetWingAirfoilLabel() & " outer wing skin"
    End Function
End Module
