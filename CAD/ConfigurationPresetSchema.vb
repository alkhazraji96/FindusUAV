Imports System.Data.SQLite

Friend Module ConfigurationPresetSchema
    Friend Sub EnsureSchemaVersion(ByVal connection As SQLiteConnection,
                                   ByVal schemaVersion As Integer)
        EnsureColumnExists(connection, "wing_sweep_angle_degrees", "REAL NOT NULL DEFAULT 0")
        EnsureColumnExists(connection, "wing_dihedral_angle_degrees", "REAL NOT NULL DEFAULT 0")
        EnsureColumnExists(connection, "wing_forward_lightening_cutout_chord_fraction", "REAL NOT NULL DEFAULT 0.15")
        EnsureColumnExists(connection, "wing_forward_lightening_cutout_preferred_diameter", "REAL NOT NULL DEFAULT 22")
        EnsureColumnExists(connection, "wing_middle_lightening_cutout_chord_fraction", "REAL NOT NULL DEFAULT 0.5")
        EnsureColumnExists(connection, "wing_middle_lightening_cutout_preferred_diameter", "REAL NOT NULL DEFAULT 34")
        EnsureColumnExists(connection, "wing_aft_lightening_cutout_chord_fraction", "REAL NOT NULL DEFAULT 0.7")
        EnsureColumnExists(connection, "wing_aft_lightening_cutout_preferred_diameter", "REAL NOT NULL DEFAULT 20")
        EnsureColumnExists(connection, "tail_lightening_cutouts_enabled", "INTEGER NOT NULL DEFAULT 1")

        Using command As New SQLiteCommand("UPDATE configuration_presets SET schema_version = @schema_version WHERE schema_version < @schema_version;", connection)
            AddParameter(command, "@schema_version", schemaVersion)
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub EnsureColumnExists(ByVal connection As SQLiteConnection,
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

    Private Function DoesColumnExist(ByVal connection As SQLiteConnection,
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
End Module
