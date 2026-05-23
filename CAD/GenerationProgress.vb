Friend Enum GenerationProgressPhase
    Starting
    Running
    Completed
    Failed
End Enum

Friend Interface IGenerationProgressReporter
    Sub Report(ByVal progressUpdate As GenerationProgressUpdate)
End Interface

Friend NotInheritable Class GenerationProgressUpdate
    Public ReadOnly Property Phase As GenerationProgressPhase
    Public ReadOnly Property OperationName As String
    Public ReadOnly Property StageName As String
    Public ReadOnly Property Message As String
    Public ReadOnly Property CurrentStep As Integer
    Public ReadOnly Property TotalSteps As Integer
    Public ReadOnly Property PercentComplete As Integer
    Public ReadOnly Property IsIndeterminate As Boolean

    Private Sub New(ByVal phase As GenerationProgressPhase,
                    ByVal operationName As String,
                    ByVal stageName As String,
                    ByVal message As String,
                    ByVal currentStep As Integer,
                    ByVal totalSteps As Integer,
                    ByVal percentComplete As Integer,
                    ByVal isIndeterminate As Boolean)
        Me.Phase = phase
        Me.OperationName = If(operationName, String.Empty)
        Me.StageName = If(stageName, String.Empty)
        Me.Message = If(message, String.Empty)
        Me.CurrentStep = Math.Max(0, currentStep)
        Me.TotalSteps = Math.Max(0, totalSteps)
        Me.PercentComplete = ClampPercent(percentComplete)
        Me.IsIndeterminate = isIndeterminate
    End Sub

    Public Shared Function CreateStarting(ByVal operationName As String,
                                          Optional ByVal message As String = Nothing) As GenerationProgressUpdate
        Return New GenerationProgressUpdate(GenerationProgressPhase.Starting,
                                            operationName,
                                            String.Empty,
                                            If(message, "Starting generation..."),
                                            0,
                                            0,
                                            0,
                                            True)
    End Function

    Public Shared Function CreateStep(ByVal operationName As String,
                                      ByVal stageName As String,
                                      ByVal message As String,
                                      ByVal currentStep As Integer,
                                      ByVal totalSteps As Integer) As GenerationProgressUpdate
        Dim safeTotalSteps As Integer = Math.Max(0, totalSteps)
        Dim safeCurrentStep As Integer = Math.Max(0, currentStep)
        Dim isIndeterminate As Boolean = safeTotalSteps <= 0
        Dim percentComplete As Integer = 0

        If Not isIndeterminate Then
            percentComplete = CInt(Math.Round((CDbl(safeCurrentStep) / CDbl(safeTotalSteps)) * 100.0))
        End If

        Return New GenerationProgressUpdate(GenerationProgressPhase.Running,
                                            operationName,
                                            stageName,
                                            message,
                                            safeCurrentStep,
                                            safeTotalSteps,
                                            percentComplete,
                                            isIndeterminate)
    End Function

    Public Shared Function CreateIndeterminate(ByVal operationName As String,
                                               ByVal stageName As String,
                                               ByVal message As String) As GenerationProgressUpdate
        Return New GenerationProgressUpdate(GenerationProgressPhase.Running,
                                            operationName,
                                            stageName,
                                            message,
                                            0,
                                            0,
                                            0,
                                            True)
    End Function

    Public Shared Function CreateCompleted(ByVal operationName As String,
                                           Optional ByVal message As String = Nothing) As GenerationProgressUpdate
        Return New GenerationProgressUpdate(GenerationProgressPhase.Completed,
                                            operationName,
                                            String.Empty,
                                            If(message, "Generation complete."),
                                            1,
                                            1,
                                            100,
                                            False)
    End Function

    Public Shared Function CreateFailed(ByVal operationName As String,
                                        ByVal message As String) As GenerationProgressUpdate
        Return New GenerationProgressUpdate(GenerationProgressPhase.Failed,
                                            operationName,
                                            String.Empty,
                                            message,
                                            0,
                                            0,
                                            0,
                                            True)
    End Function

    Public Overrides Function ToString() As String
        Dim prefix As String = OperationName

        If Not String.IsNullOrWhiteSpace(StageName) Then
            If String.IsNullOrWhiteSpace(prefix) Then
                prefix = StageName
            Else
                prefix &= " - " & StageName
            End If
        End If

        If String.IsNullOrWhiteSpace(prefix) Then
            Return Message
        End If

        If String.IsNullOrWhiteSpace(Message) Then
            Return prefix
        End If

        Return prefix & ": " & Message
    End Function

    Private Shared Function ClampPercent(ByVal percentComplete As Integer) As Integer
        If percentComplete < 0 Then
            Return 0
        End If

        If percentComplete > 100 Then
            Return 100
        End If

        Return percentComplete
    End Function
End Class

Friend NotInheritable Class NullGenerationProgressReporter
    Implements IGenerationProgressReporter

    Public Shared ReadOnly Property Instance As IGenerationProgressReporter =
        New NullGenerationProgressReporter()

    Private Sub New()
    End Sub

    Public Sub Report(ByVal progressUpdate As GenerationProgressUpdate) Implements IGenerationProgressReporter.Report
    End Sub
End Class

Friend NotInheritable Class ActionGenerationProgressReporter
    Implements IGenerationProgressReporter

    Private ReadOnly reportAction As Action(Of GenerationProgressUpdate)

    Public Sub New(ByVal reportAction As Action(Of GenerationProgressUpdate))
        If reportAction Is Nothing Then
            Throw New ArgumentNullException("reportAction")
        End If

        Me.reportAction = reportAction
    End Sub

    Public Sub Report(ByVal progressUpdate As GenerationProgressUpdate) Implements IGenerationProgressReporter.Report
        If progressUpdate Is Nothing Then
            Return
        End If

        reportAction(progressUpdate)
    End Sub
End Class

Friend Module GenerationProgress
    Friend ReadOnly Property None As IGenerationProgressReporter
        Get
            Return NullGenerationProgressReporter.Instance
        End Get
    End Property

    Friend Function UseDefaultReporterWhenMissing(ByVal reporter As IGenerationProgressReporter) As IGenerationProgressReporter
        If reporter Is Nothing Then
            Return None
        End If

        Return reporter
    End Function

    Friend Sub Report(ByVal reporter As IGenerationProgressReporter,
                      ByVal progressUpdate As GenerationProgressUpdate)
        If reporter Is Nothing OrElse progressUpdate Is Nothing Then
            Return
        End If

        reporter.Report(progressUpdate)
    End Sub
End Module
