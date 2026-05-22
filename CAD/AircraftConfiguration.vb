Imports System.Collections.Generic

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

Friend Class WingConfiguration
    Private Const AileronFixedPanelEndFraction As Double = 0.7
    Private Const AileronRearSparWidthFraction As Double = 0.03
    Private Const AileronClearanceGapFraction As Double = 0.02

    Public Property FullSpan As Double
    Public Property RootChord As Double
    Public Property TipChord As Double
    Public Property Airfoil As AirfoilConfiguration
    Public Property PointCountPerSurface As Integer
    Public Property Ribs As WingRibConfiguration
    Public Property MainSpar As WingSparConfiguration
    Public Property Aileron As ControlSurfaceConfiguration

    Public Sub New()
        FullSpan = 3543.65
        RootChord = 586.0
        TipChord = 374.0
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

Friend Class WingRibConfiguration
    Public Property CountPerSide As Integer
    Public Property Thickness As Double
    Public Property LighteningCutoutsEnabled As Boolean
    Public Property LighteningCutoutEdgeMargin As Double
    Public Property LighteningCutoutMinimumDiameter As Double
    Public Property LighteningCutouts As List(Of RibLighteningCutoutConfiguration)

    Public Sub New()
        CountPerSide = 14
        Thickness = 3.0
        LighteningCutoutsEnabled = True
        LighteningCutoutEdgeMargin = 6.0
        LighteningCutoutMinimumDiameter = 8.0
        LighteningCutouts = RibLighteningCutoutConfiguration.CreateDefaultWingCutouts()
    End Sub

    Public Shared Function CreateDefault() As WingRibConfiguration
        Return New WingRibConfiguration()
    End Function
End Class

Friend Class WingSparConfiguration
    Public Property ChordFraction As Double
    Public Property OuterDiameter As Double
    Public Property WallThickness As Double
    Public Property RibCutoutDiameter As Double

    Public Sub New()
        ChordFraction = 0.3
        OuterDiameter = 30.0
        WallThickness = 1.5
        RibCutoutDiameter = 31.0
    End Sub

    Public Shared Function CreateDefault() As WingSparConfiguration
        Return New WingSparConfiguration()
    End Function

    Public ReadOnly Property InnerDiameter As Double
        Get
            Return OuterDiameter - (2.0 * WallThickness)
        End Get
    End Property
End Class

Friend Class RibLighteningCutoutConfiguration
    Public Property Name As String
    Public Property ChordFraction As Double
    Public Property PreferredDiameter As Double

    Public Sub New(ByVal name As String,
                   ByVal chordFraction As Double,
                   ByVal preferredDiameter As Double)
        Me.Name = name
        Me.ChordFraction = chordFraction
        Me.PreferredDiameter = preferredDiameter
    End Sub

    Public Shared Function CreateDefaultWingCutouts() As List(Of RibLighteningCutoutConfiguration)
        Return New List(Of RibLighteningCutoutConfiguration) From {
            New RibLighteningCutoutConfiguration("Forward lightening cutout", 0.15, 22.0),
            New RibLighteningCutoutConfiguration("Middle lightening cutout", 0.5, 34.0),
            New RibLighteningCutoutConfiguration("Aft lightening cutout", 0.7, 22.0)
        }
    End Function
End Class

Friend Class ControlSurfaceConfiguration
    Public Property SpanFraction As Double

    Public Sub New()
        Me.New(0.4)
    End Sub

    Public Sub New(ByVal spanFraction As Double)
        Me.SpanFraction = spanFraction
    End Sub
End Class

Friend Class TailConfiguration
    Public Property PointCountPerSurface As Integer
    Public Property DistanceOffset As Double
    Public Property RibThickness As Double
    Public Property MainSpar As TailSparConfiguration
    Public Property HorizontalStabilizer As HorizontalTailConfiguration
    Public Property VerticalStabilizer As VerticalTailConfiguration
    Public Property RudderClearance As Double

    Public Sub New()
        PointCountPerSurface = 50
        DistanceOffset = 1500.0
        RibThickness = 2.0
        MainSpar = TailSparConfiguration.CreateDefault()
        HorizontalStabilizer = HorizontalTailConfiguration.CreateDefault()
        VerticalStabilizer = VerticalTailConfiguration.CreateDefault()
        RudderClearance = 25.0
    End Sub

    Public Shared Function CreateDefault() As TailConfiguration
        Return New TailConfiguration()
    End Function

    Public ReadOnly Property HorizontalOffset As Double
        Get
            Return DistanceOffset
        End Get
    End Property

    Public ReadOnly Property VerticalRootOffset As Double
        Get
            Return DistanceOffset
        End Get
    End Property

    Public ReadOnly Property VerticalTrailingEdgeX As Double
        Get
            Return VerticalRootOffset + VerticalStabilizer.RootChord
        End Get
    End Property

    Public ReadOnly Property VerticalTipOffset As Double
        Get
            Return VerticalTrailingEdgeX - VerticalStabilizer.TipChord
        End Get
    End Property
End Class

Friend Class HorizontalTailConfiguration
    Public Property Chord As Double
    Public Property HalfSpan As Double
    Public Property RibCount As Integer
    Public Property Airfoil As AirfoilConfiguration

    Public Sub New()
        Chord = 150.0
        HalfSpan = 350.0
        RibCount = 8
        Airfoil = AirfoilConfiguration.CreateNaca0012()
    End Sub

    Public Shared Function CreateDefault() As HorizontalTailConfiguration
        Return New HorizontalTailConfiguration()
    End Function

    Public ReadOnly Property FullSpan As Double
        Get
            Return HalfSpan * 2.0
        End Get
    End Property
End Class

Friend Class VerticalTailConfiguration
    Public Property RootChord As Double
    Public Property TipChord As Double
    Public Property Span As Double
    Public Property RibCount As Integer
    Public Property Airfoil As AirfoilConfiguration

    Public Sub New()
        RootChord = 150.0
        TipChord = 75.0
        Span = 250.0
        RibCount = 4
        Airfoil = AirfoilConfiguration.CreateNaca0012()
    End Sub

    Public Shared Function CreateDefault() As VerticalTailConfiguration
        Return New VerticalTailConfiguration()
    End Function
End Class

Friend Class TailSparConfiguration
    Public Property MainSparDiameter As Double

    Public Sub New()
        MainSparDiameter = 6.0
    End Sub

    Public Shared Function CreateDefault() As TailSparConfiguration
        Return New TailSparConfiguration()
    End Function

    Public ReadOnly Property MainSparRadius As Double
        Get
            Return MainSparDiameter / 2.0
        End Get
    End Property
End Class
