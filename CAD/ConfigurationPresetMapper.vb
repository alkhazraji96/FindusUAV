Imports System.Data.SQLite

Friend Module ConfigurationPresetMapper
    Friend Function ReadConfiguration(ByVal reader As SQLiteDataReader) As AircraftConfiguration
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
        tail.LighteningCutoutsEnabled = ReadBoolean(reader, "tail_lightening_cutouts_enabled")
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

    Friend Sub SavePreset(ByVal connection As SQLiteConnection,
                          ByVal transaction As SQLiteTransaction,
                          ByVal presetName As String,
                          ByVal configuration As AircraftConfiguration,
                          ByVal createdUtc As String,
                          ByVal updatedUtc As String,
                          ByVal isLastUsed As Boolean,
                          ByVal schemaVersion As Integer)
        Using command As New SQLiteCommand(BuildSavePresetSql(), connection, transaction)
            Dim wing As WingConfiguration = configuration.Wing
            Dim tail As TailConfiguration = configuration.Tail

            AddParameter(command, "@preset_name", presetName)
            AddParameter(command, "@schema_version", schemaVersion)
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
            AddParameter(command, "@tail_lightening_cutouts_enabled", If(tail.LighteningCutoutsEnabled, 1, 0))
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
End Module
