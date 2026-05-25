Friend Class ControlSurfaceConfiguration
    Public Property SpanFraction As Double

    Public Sub New()
        Me.New(0.4)
    End Sub

    Public Sub New(ByVal spanFraction As Double)
        Me.SpanFraction = spanFraction
    End Sub
End Class
