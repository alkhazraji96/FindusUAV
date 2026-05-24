Imports System.Collections.Generic

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
    Public ReadOnly ConstructionGeometry As List(Of Object)

    Public Sub New(ByVal name As String,
                   ByVal profileSpline As Object,
                   ByVal closingPointReference As Object)
        Me.New(name, profileSpline, closingPointReference, Nothing)
    End Sub

    Public Sub New(ByVal name As String,
                   ByVal profileSpline As Object,
                   ByVal closingPointReference As Object,
                   ByVal constructionGeometry As List(Of Object))
        Me.Name = name
        Me.ProfileSpline = profileSpline
        Me.ClosingPointReference = closingPointReference

        If constructionGeometry Is Nothing Then
            Me.ConstructionGeometry = New List(Of Object)()
        Else
            Me.ConstructionGeometry = constructionGeometry
        End If
    End Sub
End Structure
