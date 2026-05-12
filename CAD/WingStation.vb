Friend Structure WingStation
    Public ReadOnly Name As String
    Public ReadOnly SpanPosition As Double

    Public Sub New(ByVal name As String, ByVal spanPosition As Double)
        Me.Name = name
        Me.SpanPosition = spanPosition
    End Sub
End Structure

Friend Structure WingStationProfile
    Public ReadOnly Name As String
    Public ReadOnly ProfileSpline As Object
    Public ReadOnly ClosingPointReference As Object

    Public Sub New(ByVal name As String,
                   ByVal profileSpline As Object,
                   ByVal closingPointReference As Object)
        Me.Name = name
        Me.ProfileSpline = profileSpline
        Me.ClosingPointReference = closingPointReference
    End Sub
End Structure
