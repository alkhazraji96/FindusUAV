Imports System.Data.SQLite
Imports System.Globalization
Imports System.IO

Friend NotInheritable Class ConfigurationPresetRepository
    Private Const SchemaVersion As Integer = 5
    Friend Const LastUsedPresetName As String = "Last Used"
    Private Const MaximumPresetNameLength As Integer = 64

    Private ReadOnly databasePath As String

    Public Sub New()
        Me.New(GetDefaultDatabasePath())
    End Sub

    Public Sub New(ByVal databasePath As String)
        Me.databasePath = databasePath
    End Sub

    Public ReadOnly Property DatabaseFilePath As String
        Get
            Return databasePath
        End Get
    End Property

    Public Function TryLoadLastUsed(ByRef configuration As AircraftConfiguration,
                                    ByRef failureMessage As String) As Boolean
        failureMessage = String.Empty
        configuration = Nothing

        Try
            EnsureDatabase()

            Using connection As SQLiteConnection = OpenConnection()
                Using command As New SQLiteCommand(BuildSelectLastUsedSql(), connection)
                    AddParameter(command, "@preset_name", LastUsedPresetName)

                    Using reader As SQLiteDataReader = command.ExecuteReader()
                        If Not reader.Read() Then
                            Return False
                        End If

                        configuration = ReadConfiguration(reader)
                        Return True
                    End Using
                End Using
            End Using
        Catch ex As Exception
            failureMessage = ex.Message
            configuration = Nothing
            Return False
        End Try
    End Function

    Public Function TryListNamedPresets(ByRef presetNames As List(Of String),
                                        ByRef failureMessage As String) As Boolean
        failureMessage = String.Empty
        presetNames = New List(Of String)()

        Try
            EnsureDatabase()

            Using connection As SQLiteConnection = OpenConnection()
                Using command As New SQLiteCommand(BuildSelectNamedPresetNamesSql(), connection)
                    AddParameter(command, "@last_used_preset_name", LastUsedPresetName)

                    Using reader As SQLiteDataReader = command.ExecuteReader()
                        While reader.Read()
                            presetNames.Add(ReadString(reader, "preset_name"))
                        End While
                    End Using
                End Using
            End Using

            Return True
        Catch ex As Exception
            failureMessage = ex.Message
            presetNames = New List(Of String)()
            Return False
        End Try
    End Function

    Public Function TryLoadNamedPreset(ByVal presetName As String,
                                       ByRef configuration As AircraftConfiguration,
                                       ByRef failureMessage As String) As Boolean
        failureMessage = String.Empty
        configuration = Nothing

        Dim normalizedPresetName As String = String.Empty

        If Not TryNormalizeNamedPresetName(presetName, normalizedPresetName, failureMessage) Then
            Return False
        End If

        Try
            EnsureDatabase()

            Using connection As SQLiteConnection = OpenConnection()
                Using command As New SQLiteCommand(BuildSelectNamedPresetSql(), connection)
                    AddParameter(command, "@preset_name", normalizedPresetName)
                    AddParameter(command, "@last_used_preset_name", LastUsedPresetName)

                    Using reader As SQLiteDataReader = command.ExecuteReader()
                        If Not reader.Read() Then
                            failureMessage = "Preset '" & normalizedPresetName & "' was not found."
                            Return False
                        End If

                        configuration = ReadConfiguration(reader)
                        Return True
                    End Using
                End Using
            End Using
        Catch ex As Exception
            failureMessage = ex.Message
            configuration = Nothing
            Return False
        End Try
    End Function

    Public Function TrySaveLastUsed(ByVal configuration As AircraftConfiguration,
                                    ByRef failureMessage As String) As Boolean
        failureMessage = String.Empty

        If configuration Is Nothing Then
            failureMessage = "No configuration was provided."
            Return False
        End If

        Try
            EnsureDatabase()

            Using connection As SQLiteConnection = OpenConnection()
                Using transaction As SQLiteTransaction = connection.BeginTransaction()
                    Dim createdUtc As String = GetExistingCreatedUtc(connection,
                                                                     transaction,
                                                                     LastUsedPresetName)
                    Dim updatedUtc As String = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)

                    ClearLastUsedFlag(connection, transaction)
                    SavePreset(connection,
                               transaction,
                               LastUsedPresetName,
                               configuration,
                               createdUtc,
                               updatedUtc,
                               True,
                               SchemaVersion)

                    transaction.Commit()
                End Using
            End Using

            Return True
        Catch ex As Exception
            failureMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function TrySaveNamedPreset(ByVal presetName As String,
                                       ByVal configuration As AircraftConfiguration,
                                       ByRef failureMessage As String) As Boolean
        failureMessage = String.Empty

        If configuration Is Nothing Then
            failureMessage = "No configuration was provided."
            Return False
        End If

        Dim normalizedPresetName As String = String.Empty

        If Not TryNormalizeNamedPresetName(presetName, normalizedPresetName, failureMessage) Then
            Return False
        End If

        Try
            EnsureDatabase()

            Using connection As SQLiteConnection = OpenConnection()
                Using transaction As SQLiteTransaction = connection.BeginTransaction()
                    Dim existingPresetName As String =
                        GetExistingPresetName(connection, transaction, normalizedPresetName)
                    Dim savePresetName As String =
                        If(String.IsNullOrWhiteSpace(existingPresetName),
                           normalizedPresetName,
                           existingPresetName)
                    Dim createdUtc As String = GetExistingCreatedUtc(connection,
                                                                     transaction,
                                                                     savePresetName)
                    Dim updatedUtc As String = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)

                    SavePreset(connection,
                               transaction,
                               savePresetName,
                               configuration,
                               createdUtc,
                               updatedUtc,
                               False,
                               SchemaVersion)

                    transaction.Commit()
                End Using
            End Using

            Return True
        Catch ex As Exception
            failureMessage = ex.Message
            Return False
        End Try
    End Function

    Public Function TryDeleteNamedPreset(ByVal presetName As String,
                                         ByRef failureMessage As String) As Boolean
        failureMessage = String.Empty

        Dim normalizedPresetName As String = String.Empty

        If Not TryNormalizeNamedPresetName(presetName, normalizedPresetName, failureMessage) Then
            Return False
        End If

        Try
            EnsureDatabase()

            Using connection As SQLiteConnection = OpenConnection()
                Using command As New SQLiteCommand(BuildDeleteNamedPresetSql(), connection)
                    AddParameter(command, "@preset_name", normalizedPresetName)
                    AddParameter(command, "@last_used_preset_name", LastUsedPresetName)

                    If command.ExecuteNonQuery() = 0 Then
                        failureMessage = "Preset '" & normalizedPresetName & "' was not found."
                        Return False
                    End If
                End Using
            End Using

            Return True
        Catch ex As Exception
            failureMessage = ex.Message
            Return False
        End Try
    End Function

    Private Shared Function GetDefaultDatabasePath() As String
        Dim appDataPath As String =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

        Return Path.Combine(appDataPath, "FindusUAV", "configuration-presets.sqlite")
    End Function

    Private Sub EnsureDatabase()
        Dim folderPath As String = Path.GetDirectoryName(databasePath)

        If Not String.IsNullOrWhiteSpace(folderPath) Then
            Directory.CreateDirectory(folderPath)
        End If

        Using connection As SQLiteConnection = OpenConnection()
            Using command As New SQLiteCommand(BuildCreateSchemaSql(), connection)
                command.ExecuteNonQuery()
            End Using

            EnsureSchemaVersion(connection, SchemaVersion)
        End Using
    End Sub

    Private Function OpenConnection() As SQLiteConnection
        Dim connectionString As New SQLiteConnectionStringBuilder()
        connectionString.DataSource = databasePath
        connectionString.Version = 3

        Dim connection As New SQLiteConnection(connectionString.ToString())
        connection.Open()
        Return connection
    End Function

    Private Shared Function GetExistingCreatedUtc(ByVal connection As SQLiteConnection,
                                                  ByVal transaction As SQLiteTransaction,
                                                  ByVal presetName As String) As String
        Using command As New SQLiteCommand("SELECT created_utc FROM configuration_presets WHERE preset_name = @preset_name;",
                                           connection,
                                           transaction)
            AddParameter(command, "@preset_name", presetName)

            Dim existingValue As Object = command.ExecuteScalar()

            If existingValue IsNot Nothing AndAlso existingValue IsNot DBNull.Value Then
                Return Convert.ToString(existingValue, CultureInfo.InvariantCulture)
            End If
        End Using

        Return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function GetExistingPresetName(ByVal connection As SQLiteConnection,
                                                  ByVal transaction As SQLiteTransaction,
                                                  ByVal presetName As String) As String
        Using command As New SQLiteCommand("SELECT preset_name FROM configuration_presets WHERE preset_name = @preset_name COLLATE NOCASE LIMIT 1;",
                                           connection,
                                           transaction)
            AddParameter(command, "@preset_name", presetName)

            Dim existingValue As Object = command.ExecuteScalar()

            If existingValue IsNot Nothing AndAlso existingValue IsNot DBNull.Value Then
                Return Convert.ToString(existingValue, CultureInfo.InvariantCulture)
            End If
        End Using

        Return String.Empty
    End Function

    Private Shared Sub ClearLastUsedFlag(ByVal connection As SQLiteConnection,
                                         ByVal transaction As SQLiteTransaction)
        Using command As New SQLiteCommand("UPDATE configuration_presets SET is_last_used = 0;",
                                           connection,
                                           transaction)
            command.ExecuteNonQuery()
        End Using
    End Sub

    Friend Shared Function IsReservedPresetName(ByVal presetName As String) As Boolean
        Return String.Equals(If(presetName, String.Empty).Trim(),
                             LastUsedPresetName,
                             StringComparison.OrdinalIgnoreCase)
    End Function

    Friend Shared Function TryNormalizeNamedPresetName(ByVal presetName As String,
                                                       ByRef normalizedPresetName As String,
                                                       ByRef failureMessage As String) As Boolean
        normalizedPresetName = If(presetName, String.Empty).Trim()
        failureMessage = String.Empty

        If String.IsNullOrWhiteSpace(normalizedPresetName) Then
            failureMessage = "Preset name is required."
            Return False
        End If

        If normalizedPresetName.Length > MaximumPresetNameLength Then
            failureMessage = "Preset name must be " &
                             MaximumPresetNameLength.ToString(CultureInfo.InvariantCulture) &
                             " characters or fewer."
            Return False
        End If

        If IsReservedPresetName(normalizedPresetName) Then
            failureMessage = "'" & LastUsedPresetName & "' is reserved for automatic storage."
            Return False
        End If

        For Each character As Char In normalizedPresetName
            If Char.IsControl(character) Then
                failureMessage = "Preset name cannot contain control characters."
                Return False
            End If
        Next

        Return True
    End Function
End Class
