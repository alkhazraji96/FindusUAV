Imports System.Text.RegularExpressions

Friend Module NacaAirfoilParser
    Private ReadOnly FourDigitNacaPattern As Regex =
        New Regex("^\s*(?:NACA\s*)?(\d{4})\s*$", RegexOptions.IgnoreCase)

    Friend Function TryParse(ByVal input As String,
                             ByRef airfoil As AirfoilConfiguration,
                             Optional ByRef errorMessage As String = Nothing) As Boolean
        airfoil = Nothing
        errorMessage = Nothing

        If String.IsNullOrWhiteSpace(input) Then
            errorMessage = "Airfoil is required. Use a NACA 4-digit code such as NACA 4415 or 0012."
            Return False
        End If

        Dim match As Match = FourDigitNacaPattern.Match(input)

        If Not match.Success Then
            errorMessage = "Airfoil must be a NACA 4-digit code such as NACA 4415 or 0012."
            Return False
        End If

        Dim nacaDigits As String = match.Groups(1).Value
        Dim maximumCamberDigit As Integer = Integer.Parse(nacaDigits.Substring(0, 1))
        Dim maximumCamberPositionDigit As Integer = Integer.Parse(nacaDigits.Substring(1, 1))
        Dim maximumThicknessDigits As Integer = Integer.Parse(nacaDigits.Substring(2, 2))

        If maximumThicknessDigits <= 0 Then
            errorMessage = "NACA airfoil thickness must be greater than zero."
            Return False
        End If

        Dim maximumCamber As Double = CDbl(maximumCamberDigit) / 100.0
        Dim maximumCamberPosition As Double = CDbl(maximumCamberPositionDigit) / 10.0
        Dim maximumThickness As Double = CDbl(maximumThicknessDigits) / 100.0

        If maximumCamber <= 0.0 Then
            maximumCamberPosition = 0.0
        End If

        airfoil = New AirfoilConfiguration("NACA " & nacaDigits,
                                           maximumCamber,
                                           maximumCamberPosition,
                                           maximumThickness)
        Return True
    End Function

    Friend Function Parse(ByVal input As String) As AirfoilConfiguration
        Dim airfoil As AirfoilConfiguration = Nothing
        Dim errorMessage As String = Nothing

        If TryParse(input, airfoil, errorMessage) Then
            Return airfoil
        End If

        Throw New ArgumentException(errorMessage, "input")
    End Function
End Module
