Friend Class WingConfiguration
    Private Const AileronFixedPanelEndFraction As Double = 0.7
    Private Const AileronRearSparWidthFraction As Double = 0.03
    Private Const AileronClearanceGapFraction As Double = 0.02

    Public Property FullSpan As Double
    Public Property RootChord As Double
    Public Property TipChord As Double
    Public Property SweepAngleDegrees As Double
    Public Property DihedralAngleDegrees As Double
    Public Property Airfoil As AirfoilConfiguration
    Public Property PointCountPerSurface As Integer
    Public Property Ribs As WingRibConfiguration
    Public Property MainSpar As WingSparConfiguration
    Public Property Aileron As ControlSurfaceConfiguration

    Public Sub New()
        FullSpan = 3543.65
        RootChord = 586.0
        TipChord = 374.0
        SweepAngleDegrees = 0.0
        DihedralAngleDegrees = 0.0
        Airfoil = AirfoilConfiguration.CreateNaca4415()
        PointCountPerSurface = 41
        Ribs = WingRibConfiguration.CreateDefault()
        MainSpar = WingSparConfiguration.CreateDefault()
        Aileron = New ControlSurfaceConfiguration(0.4)
    End Sub

    Public Shared Function CreateDefault() As WingConfiguration
        Return New WingConfiguration()
    End Function

    Public ReadOnly Property HalfSpan As Double
        Get
            Return FullSpan / 2.0
        End Get
    End Property

    Public ReadOnly Property TotalRibCount As Integer
        Get
            Return (2 * Ribs.CountPerSide) + 1
        End Get
    End Property

    Public ReadOnly Property AileronSpanLength As Double
        Get
            Return HalfSpan * Aileron.SpanFraction
        End Get
    End Property

    Public ReadOnly Property AileronOuterSpanPosition As Double
        Get
            Return HalfSpan
        End Get
    End Property

    Public ReadOnly Property AileronInnerSpanPosition As Double
        Get
            Return AileronOuterSpanPosition - AileronSpanLength
        End Get
    End Property

    Public ReadOnly Property AileronFixedPanelEndX As Double
        Get
            Return TipChord * AileronFixedPanelEndFraction
        End Get
    End Property

    Public ReadOnly Property AileronRearSparWidth As Double
        Get
            Return TipChord * AileronRearSparWidthFraction
        End Get
    End Property

    Public ReadOnly Property AileronRearSparEndX As Double
        Get
            Return AileronFixedPanelEndX + AileronRearSparWidth
        End Get
    End Property

    Public ReadOnly Property AileronClearanceGap As Double
        Get
            Return TipChord * AileronClearanceGapFraction
        End Get
    End Property

    Public ReadOnly Property AileronPanelStartX As Double
        Get
            Return AileronRearSparEndX + AileronClearanceGap
        End Get
    End Property
End Class
