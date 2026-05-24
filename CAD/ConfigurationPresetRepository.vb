Imports System.Data.SQLite
Imports System.Globalization
Imports System.IO

Friend NotInheritable Class ConfigurationPresetRepository
    Private Const SchemaVersion As Integer = 4
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
                               True)

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
                               False)

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

            EnsureSchemaVersion(connection)
        End Using
    End Sub

    Private Shared Sub EnsureSchemaVersion(ByVal connection As SQLiteConnection)
        EnsureColumnExists(connection, "wing_sweep_angle_degrees", "REAL NOT NULL DEFAULT 0")
        EnsureColumnExists(connection, "wing_dihedral_angle_degrees", "REAL NOT NULL DEFAULT 0")
        EnsureColumnExists(connection, "wing_forward_lightening_cutout_chord_fraction", "REAL NOT NULL DEFAULT 0.15")
        EnsureColumnExists(connection, "wing_forward_lightening_cutout_preferred_diameter", "REAL NOT NULL DEFAULT 22")
        EnsureColumnExists(connection, "wing_middle_lightening_cutout_chord_fraction", "REAL NOT NULL DEFAULT 0.5")
        EnsureColumnExists(connection, "wing_middle_lightening_cutout_preferred_diameter", "REAL NOT NULL DEFAULT 34")
        EnsureColumnExists(connection, "wing_aft_lightening_cutout_chord_fraction", "REAL NOT NULL DEFAULT 0.7")
        EnsureColumnExists(connection, "wing_aft_lightening_cutout_preferred_diameter", "REAL NOT NULL DEFAULT 20")

        Using command As New SQLiteCommand("UPDATE configuration_presets SET schema_version = @schema_version WHERE schema_version < @schema_version;", connection)
            AddParameter(command, "@schema_version", SchemaVersion)
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub EnsureColumnExists(ByVal connection As SQLiteConnection,
                                          ByVal columnName As String,
                                          ByVal columnDefinition As String)
        If DoesColumnExist(connection, columnName) Then
            Return
        End If

        Using command As New SQLiteCommand("ALTER TABLE configuration_presets ADD COLUMN " &
                                           columnName & " " & columnDefinition & ";",
                                           connection)
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Function DoesColumnExist(ByVal connection As SQLiteConnection,
                                            ByVal columnName As String) As Boolean
        Using command As New SQLiteCommand("PRAGMA table_info(configuration_presets);", connection)
            Using reader As SQLiteDataReader = command.ExecuteReader()
                While reader.Read()
                    If String.Equals(ReadString(reader, "name"),
                                     columnName,
                                     StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                End While
            End Using
        End Using

        Return False
    End Function

    Private Function OpenConnection() As SQLiteConnection
        Dim connectionString As New SQLiteConnectionStringBuilder()
        connectionString.DataSource = databasePath
        connectionString.Version = 3

        Dim connection As New SQLiteConnection(connectionString.ToString())
        connection.Open()
        Return connection
    End Function

    Private Shared Function BuildCreateSchemaSql() As String
        Return "CREATE TABLE IF NOT EXISTS configuration_presets (" &
               "preset_name TEXT NOT NULL PRIMARY KEY, " &
               "schema_version INTEGER NOT NULL, " &
               "is_last_used INTEGER NOT NULL DEFAULT 0, " &
               "created_utc TEXT NOT NULL, " &
               "updated_utc TEXT NOT NULL, " &
               "wing_full_span REAL NOT NULL, " &
               "wing_root_chord REAL NOT NULL, " &
               "wing_tip_chord REAL NOT NULL, " &
               "wing_sweep_angle_degrees REAL NOT NULL DEFAULT 0, " &
               "wing_dihedral_angle_degrees REAL NOT NULL DEFAULT 0, " &
               "wing_airfoil TEXT NOT NULL, " &
               "wing_point_count_per_surface INTEGER NOT NULL, " &
               "wing_rib_count_per_side INTEGER NOT NULL, " &
               "wing_rib_thickness REAL NOT NULL, " &
               "wing_lightening_cutouts_enabled INTEGER NOT NULL, " &
               "wing_forward_lightening_cutout_chord_fraction REAL NOT NULL DEFAULT 0.15, " &
               "wing_forward_lightening_cutout_preferred_diameter REAL NOT NULL DEFAULT 22, " &
               "wing_middle_lightening_cutout_chord_fraction REAL NOT NULL DEFAULT 0.5, " &
               "wing_middle_lightening_cutout_preferred_diameter REAL NOT NULL DEFAULT 34, " &
               "wing_aft_lightening_cutout_chord_fraction REAL NOT NULL DEFAULT 0.7, " &
               "wing_aft_lightening_cutout_preferred_diameter REAL NOT NULL DEFAULT 20, " &
               "wing_main_spar_chord_fraction REAL NOT NULL, " &
               "wing_main_spar_outer_diameter REAL NOT NULL, " &
               "wing_main_spar_wall_thickness REAL NOT NULL, " &
               "wing_main_spar_rib_cutout_diameter REAL NOT NULL, " &
               "wing_aileron_span_fraction REAL NOT NULL, " &
               "tail_distance_offset REAL NOT NULL, " &
               "tail_point_count_per_surface INTEGER NOT NULL, " &
               "tail_rib_thickness REAL NOT NULL, " &
               "tail_main_spar_diameter REAL NOT NULL, " &
               "tail_rudder_clearance REAL NOT NULL, " &
               "horizontal_tail_chord REAL NOT NULL, " &
               "horizontal_tail_half_span REAL NOT NULL, " &
               "horizontal_tail_rib_count INTEGER NOT NULL, " &
               "horizontal_tail_airfoil TEXT NOT NULL, " &
               "vertical_tail_root_chord REAL NOT NULL, " &
               "vertical_tail_tip_chord REAL NOT NULL, " &
               "vertical_tail_span REAL NOT NULL, " &
               "vertical_tail_rib_count INTEGER NOT NULL, " &
               "vertical_tail_airfoil TEXT NOT NULL" &
               ");" &
               "CREATE INDEX IF NOT EXISTS ix_configuration_presets_last_used " &
               "ON configuration_presets (is_last_used, updated_utc);"
    End Function

    Private Shared Function BuildSelectNamedPresetNamesSql() As String
        Return "SELECT preset_name FROM configuration_presets " &
               "WHERE preset_name <> @last_used_preset_name COLLATE NOCASE " &
               "ORDER BY preset_name COLLATE NOCASE;"
    End Function

    Private Shared Function BuildSelectLastUsedSql() As String
        Return "SELECT * FROM configuration_presets " &
               "WHERE is_last_used = 1 OR preset_name = @preset_name " &
               "ORDER BY is_last_used DESC, updated_utc DESC " &
               "LIMIT 1;"
    End Function

    Private Shared Function BuildSelectNamedPresetSql() As String
        Return "SELECT * FROM configuration_presets " &
               "WHERE preset_name = @preset_name COLLATE NOCASE " &
               "AND preset_name <> @last_used_preset_name COLLATE NOCASE " &
               "LIMIT 1;"
    End Function

    Private Shared Function BuildDeleteNamedPresetSql() As String
        Return "DELETE FROM configuration_presets " &
               "WHERE preset_name = @preset_name COLLATE NOCASE " &
               "AND preset_name <> @last_used_preset_name COLLATE NOCASE;"
    End Function

    Private Shared Function BuildSavePresetSql() As String
        Return "INSERT OR REPLACE INTO configuration_presets (" &
               "preset_name, schema_version, is_last_used, created_utc, updated_utc, " &
               "wing_full_span, wing_root_chord, wing_tip_chord, wing_sweep_angle_degrees, " &
               "wing_dihedral_angle_degrees, wing_airfoil, " &
               "wing_point_count_per_surface, wing_rib_count_per_side, wing_rib_thickness, " &
               "wing_lightening_cutouts_enabled, wing_forward_lightening_cutout_chord_fraction, " &
               "wing_forward_lightening_cutout_preferred_diameter, wing_middle_lightening_cutout_chord_fraction, " &
               "wing_middle_lightening_cutout_preferred_diameter, wing_aft_lightening_cutout_chord_fraction, " &
               "wing_aft_lightening_cutout_preferred_diameter, wing_main_spar_chord_fraction, " &
               "wing_main_spar_outer_diameter, wing_main_spar_wall_thickness, " &
               "wing_main_spar_rib_cutout_diameter, wing_aileron_span_fraction, " &
               "tail_distance_offset, tail_point_count_per_surface, tail_rib_thickness, " &
               "tail_main_spar_diameter, tail_rudder_clearance, horizontal_tail_chord, " &
               "horizontal_tail_half_span, horizontal_tail_rib_count, horizontal_tail_airfoil, " &
               "vertical_tail_root_chord, vertical_tail_tip_chord, vertical_tail_span, " &
               "vertical_tail_rib_count, vertical_tail_airfoil" &
               ") VALUES (" &
               "@preset_name, @schema_version, @is_last_used, @created_utc, @updated_utc, " &
               "@wing_full_span, @wing_root_chord, @wing_tip_chord, @wing_sweep_angle_degrees, " &
               "@wing_dihedral_angle_degrees, @wing_airfoil, " &
               "@wing_point_count_per_surface, @wing_rib_count_per_side, @wing_rib_thickness, " &
               "@wing_lightening_cutouts_enabled, @wing_forward_lightening_cutout_chord_fraction, " &
               "@wing_forward_lightening_cutout_preferred_diameter, @wing_middle_lightening_cutout_chord_fraction, " &
               "@wing_middle_lightening_cutout_preferred_diameter, @wing_aft_lightening_cutout_chord_fraction, " &
               "@wing_aft_lightening_cutout_preferred_diameter, @wing_main_spar_chord_fraction, " &
               "@wing_main_spar_outer_diameter, @wing_main_spar_wall_thickness, " &
               "@wing_main_spar_rib_cutout_diameter, @wing_aileron_span_fraction, " &
               "@tail_distance_offset, @tail_point_count_per_surface, @tail_rib_thickness, " &
               "@tail_main_spar_diameter, @tail_rudder_clearance, @horizontal_tail_chord, " &
               "@horizontal_tail_half_span, @horizontal_tail_rib_count, @horizontal_tail_airfoil, " &
               "@vertical_tail_root_chord, @vertical_tail_tip_chord, @vertical_tail_span, " &
               "@vertical_tail_rib_count, @vertical_tail_airfoil" &
               ");"
    End Function

    Private Shared Function ReadConfiguration(ByVal reader As SQLiteDataReader) As AircraftConfiguration
        Dim configuration As AircraftConfiguration = AircraftConfiguration.CreateDefault()
        Dim wing As WingConfiguration = configuration.Wing
        Dim tail As TailConfiguration = configuration.Tail

        wing.FullSpan = ReadDouble(reader, "wing_full_span")
        wing.RootChord = ReadDouble(reader, "wing_root_chord")
        wing.TipChord = ReadDouble(reader, "wing_tip_chord")
        wing.SweepAngleDegrees = ReadDouble(reader, "wing_sweep_angle_degrees")
        wing.DihedralAngleDegrees = ReadDouble(reader, "wing_dihedral_angle_degrees")
        wing.Airfoil = AirfoilConfiguration.FromNacaCode(ReadString(reader, "wing_airfoil"))
        wing.PointCountPerSurface = ReadInteger(reader, "wing_point_count_per_surface")
        wing.Ribs.CountPerSide = ReadInteger(reader, "wing_rib_count_per_side")
        wing.Ribs.Thickness = ReadDouble(reader, "wing_rib_thickness")
        wing.Ribs.LighteningCutoutsEnabled = ReadBoolean(reader, "wing_lightening_cutouts_enabled")
        wing.Ribs.EnsureLighteningCutoutSlots()
        wing.Ribs.GetForwardLighteningCutout().ChordFraction = ReadDouble(reader, "wing_forward_lightening_cutout_chord_fraction")
        wing.Ribs.GetForwardLighteningCutout().PreferredDiameter = ReadDouble(reader, "wing_forward_lightening_cutout_preferred_diameter")
        wing.Ribs.GetMiddleLighteningCutout().ChordFraction = ReadDouble(reader, "wing_middle_lightening_cutout_chord_fraction")
        wing.Ribs.GetMiddleLighteningCutout().PreferredDiameter = ReadDouble(reader, "wing_middle_lightening_cutout_preferred_diameter")
        wing.Ribs.GetAftLighteningCutout().ChordFraction = ReadDouble(reader, "wing_aft_lightening_cutout_chord_fraction")
        wing.Ribs.GetAftLighteningCutout().PreferredDiameter = ReadDouble(reader, "wing_aft_lightening_cutout_preferred_diameter")
        wing.MainSpar.ChordFraction = ReadDouble(reader, "wing_main_spar_chord_fraction")
        wing.MainSpar.OuterDiameter = ReadDouble(reader, "wing_main_spar_outer_diameter")
        wing.MainSpar.WallThickness = ReadDouble(reader, "wing_main_spar_wall_thickness")
        wing.MainSpar.RibCutoutDiameter = ReadDouble(reader, "wing_main_spar_rib_cutout_diameter")
        wing.Aileron.SpanFraction = ReadDouble(reader, "wing_aileron_span_fraction")

        tail.DistanceOffset = ReadDouble(reader, "tail_distance_offset")
        tail.PointCountPerSurface = ReadInteger(reader, "tail_point_count_per_surface")
        tail.RibThickness = ReadDouble(reader, "tail_rib_thickness")
        tail.MainSpar.MainSparDiameter = ReadDouble(reader, "tail_main_spar_diameter")
        tail.RudderClearance = ReadDouble(reader, "tail_rudder_clearance")
        tail.HorizontalStabilizer.Chord = ReadDouble(reader, "horizontal_tail_chord")
        tail.HorizontalStabilizer.HalfSpan = ReadDouble(reader, "horizontal_tail_half_span")
        tail.HorizontalStabilizer.RibCount = ReadInteger(reader, "horizontal_tail_rib_count")
        tail.HorizontalStabilizer.Airfoil = AirfoilConfiguration.FromNacaCode(ReadString(reader, "horizontal_tail_airfoil"))
        tail.VerticalStabilizer.RootChord = ReadDouble(reader, "vertical_tail_root_chord")
        tail.VerticalStabilizer.TipChord = ReadDouble(reader, "vertical_tail_tip_chord")
        tail.VerticalStabilizer.Span = ReadDouble(reader, "vertical_tail_span")
        tail.VerticalStabilizer.RibCount = ReadInteger(reader, "vertical_tail_rib_count")
        tail.VerticalStabilizer.Airfoil = AirfoilConfiguration.FromNacaCode(ReadString(reader, "vertical_tail_airfoil"))

        Return configuration
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

    Private Shared Sub SavePreset(ByVal connection As SQLiteConnection,
                                  ByVal transaction As SQLiteTransaction,
                                  ByVal presetName As String,
                                  ByVal configuration As AircraftConfiguration,
                                  ByVal createdUtc As String,
                                  ByVal updatedUtc As String,
                                  ByVal isLastUsed As Boolean)
        Using command As New SQLiteCommand(BuildSavePresetSql(), connection, transaction)
            Dim wing As WingConfiguration = configuration.Wing
            Dim tail As TailConfiguration = configuration.Tail

            AddParameter(command, "@preset_name", presetName)
            AddParameter(command, "@schema_version", SchemaVersion)
            AddParameter(command, "@is_last_used", If(isLastUsed, 1, 0))
            AddParameter(command, "@created_utc", createdUtc)
            AddParameter(command, "@updated_utc", updatedUtc)
            AddParameter(command, "@wing_full_span", wing.FullSpan)
            AddParameter(command, "@wing_root_chord", wing.RootChord)
            AddParameter(command, "@wing_tip_chord", wing.TipChord)
            AddParameter(command, "@wing_sweep_angle_degrees", wing.SweepAngleDegrees)
            AddParameter(command, "@wing_dihedral_angle_degrees", wing.DihedralAngleDegrees)
            AddParameter(command, "@wing_airfoil", wing.Airfoil.NacaCode)
            AddParameter(command, "@wing_point_count_per_surface", wing.PointCountPerSurface)
            AddParameter(command, "@wing_rib_count_per_side", wing.Ribs.CountPerSide)
            AddParameter(command, "@wing_rib_thickness", wing.Ribs.Thickness)
            AddParameter(command, "@wing_lightening_cutouts_enabled", If(wing.Ribs.LighteningCutoutsEnabled, 1, 0))
            wing.Ribs.EnsureLighteningCutoutSlots()
            AddParameter(command, "@wing_forward_lightening_cutout_chord_fraction", wing.Ribs.GetForwardLighteningCutout().ChordFraction)
            AddParameter(command, "@wing_forward_lightening_cutout_preferred_diameter", wing.Ribs.GetForwardLighteningCutout().PreferredDiameter)
            AddParameter(command, "@wing_middle_lightening_cutout_chord_fraction", wing.Ribs.GetMiddleLighteningCutout().ChordFraction)
            AddParameter(command, "@wing_middle_lightening_cutout_preferred_diameter", wing.Ribs.GetMiddleLighteningCutout().PreferredDiameter)
            AddParameter(command, "@wing_aft_lightening_cutout_chord_fraction", wing.Ribs.GetAftLighteningCutout().ChordFraction)
            AddParameter(command, "@wing_aft_lightening_cutout_preferred_diameter", wing.Ribs.GetAftLighteningCutout().PreferredDiameter)
            AddParameter(command, "@wing_main_spar_chord_fraction", wing.MainSpar.ChordFraction)
            AddParameter(command, "@wing_main_spar_outer_diameter", wing.MainSpar.OuterDiameter)
            AddParameter(command, "@wing_main_spar_wall_thickness", wing.MainSpar.WallThickness)
            AddParameter(command, "@wing_main_spar_rib_cutout_diameter", wing.MainSpar.RibCutoutDiameter)
            AddParameter(command, "@wing_aileron_span_fraction", wing.Aileron.SpanFraction)
            AddParameter(command, "@tail_distance_offset", tail.DistanceOffset)
            AddParameter(command, "@tail_point_count_per_surface", tail.PointCountPerSurface)
            AddParameter(command, "@tail_rib_thickness", tail.RibThickness)
            AddParameter(command, "@tail_main_spar_diameter", tail.MainSpar.MainSparDiameter)
            AddParameter(command, "@tail_rudder_clearance", tail.RudderClearance)
            AddParameter(command, "@horizontal_tail_chord", tail.HorizontalStabilizer.Chord)
            AddParameter(command, "@horizontal_tail_half_span", tail.HorizontalStabilizer.HalfSpan)
            AddParameter(command, "@horizontal_tail_rib_count", tail.HorizontalStabilizer.RibCount)
            AddParameter(command, "@horizontal_tail_airfoil", tail.HorizontalStabilizer.Airfoil.NacaCode)
            AddParameter(command, "@vertical_tail_root_chord", tail.VerticalStabilizer.RootChord)
            AddParameter(command, "@vertical_tail_tip_chord", tail.VerticalStabilizer.TipChord)
            AddParameter(command, "@vertical_tail_span", tail.VerticalStabilizer.Span)
            AddParameter(command, "@vertical_tail_rib_count", tail.VerticalStabilizer.RibCount)
            AddParameter(command, "@vertical_tail_airfoil", tail.VerticalStabilizer.Airfoil.NacaCode)

            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub AddParameter(ByVal command As SQLiteCommand,
                                    ByVal name As String,
                                    ByVal value As Object)
        Dim parameterValue As Object = value

        If parameterValue Is Nothing Then
            parameterValue = DBNull.Value
        End If

        command.Parameters.AddWithValue(name, parameterValue)
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

    Private Shared Function ReadDouble(ByVal reader As SQLiteDataReader,
                                       ByVal columnName As String) As Double
        Return Convert.ToDouble(reader(columnName), CultureInfo.InvariantCulture)
    End Function

    Private Shared Function ReadInteger(ByVal reader As SQLiteDataReader,
                                        ByVal columnName As String) As Integer
        Return Convert.ToInt32(reader(columnName), CultureInfo.InvariantCulture)
    End Function

    Private Shared Function ReadBoolean(ByVal reader As SQLiteDataReader,
                                        ByVal columnName As String) As Boolean
        Return ReadInteger(reader, columnName) <> 0
    End Function

    Private Shared Function ReadString(ByVal reader As SQLiteDataReader,
                                       ByVal columnName As String) As String
        Return Convert.ToString(reader(columnName), CultureInfo.InvariantCulture)
    End Function
End Class
