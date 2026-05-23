Imports System.Collections.Generic
Imports System.Linq

Friend Enum ConfigurationValidationSeverity
    Warning
    [Error]
End Enum

Friend Class ConfigurationValidationMessage
    Public ReadOnly Property Severity As ConfigurationValidationSeverity
    Public ReadOnly Property FieldName As String
    Public ReadOnly Property Message As String

    Public Sub New(ByVal severity As ConfigurationValidationSeverity,
                   ByVal fieldName As String,
                   ByVal message As String)
        Me.Severity = severity
        Me.FieldName = If(fieldName, String.Empty)
        Me.Message = If(message, String.Empty)
    End Sub

    Public Overrides Function ToString() As String
        If String.IsNullOrWhiteSpace(FieldName) Then
            Return Message
        End If

        Return FieldName & ": " & Message
    End Function
End Class

Friend Class ConfigurationValidationResult
    Private ReadOnly validationMessages As List(Of ConfigurationValidationMessage)

    Public Sub New()
        validationMessages = New List(Of ConfigurationValidationMessage)()
    End Sub

    Public ReadOnly Property Messages As IReadOnlyList(Of ConfigurationValidationMessage)
        Get
            Return validationMessages
        End Get
    End Property

    Public ReadOnly Property Errors As IEnumerable(Of ConfigurationValidationMessage)
        Get
            Return validationMessages.Where(Function(message) message.Severity = ConfigurationValidationSeverity.Error)
        End Get
    End Property

    Public ReadOnly Property Warnings As IEnumerable(Of ConfigurationValidationMessage)
        Get
            Return validationMessages.Where(Function(message) message.Severity = ConfigurationValidationSeverity.Warning)
        End Get
    End Property

    Public ReadOnly Property IsValid As Boolean
        Get
            Return Not Errors.Any()
        End Get
    End Property

    Public ReadOnly Property HasWarnings As Boolean
        Get
            Return Warnings.Any()
        End Get
    End Property

    Public Sub AddError(ByVal fieldName As String,
                        ByVal message As String)
        AddMessage(ConfigurationValidationSeverity.Error, fieldName, message)
    End Sub

    Public Sub AddWarning(ByVal fieldName As String,
                          ByVal message As String)
        AddMessage(ConfigurationValidationSeverity.Warning, fieldName, message)
    End Sub

    Public Sub AddMessage(ByVal severity As ConfigurationValidationSeverity,
                          ByVal fieldName As String,
                          ByVal message As String)
        validationMessages.Add(New ConfigurationValidationMessage(severity, fieldName, message))
    End Sub

    Public Sub Merge(ByVal otherResult As ConfigurationValidationResult)
        If otherResult Is Nothing Then
            Return
        End If

        validationMessages.AddRange(otherResult.Messages)
    End Sub

    Public Function GetErrorSummary() As String
        Return String.Join(Environment.NewLine, Errors.Select(Function(message) message.ToString()))
    End Function

    Public Sub ThrowIfInvalid()
        If IsValid Then
            Return
        End If

        Throw New InvalidOperationException(GetErrorSummary())
    End Sub
End Class
