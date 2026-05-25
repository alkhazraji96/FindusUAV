Friend Module ConfigurationPresetSql
    Friend Function BuildCreateSchemaSql() As String
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
               "tail_lightening_cutouts_enabled INTEGER NOT NULL DEFAULT 1, " &
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

    Friend Function BuildSelectNamedPresetNamesSql() As String
        Return "SELECT preset_name FROM configuration_presets " &
               "WHERE preset_name <> @last_used_preset_name COLLATE NOCASE " &
               "ORDER BY preset_name COLLATE NOCASE;"
    End Function

    Friend Function BuildSelectLastUsedSql() As String
        Return "SELECT * FROM configuration_presets " &
               "WHERE is_last_used = 1 OR preset_name = @preset_name " &
               "ORDER BY is_last_used DESC, updated_utc DESC " &
               "LIMIT 1;"
    End Function

    Friend Function BuildSelectNamedPresetSql() As String
        Return "SELECT * FROM configuration_presets " &
               "WHERE preset_name = @preset_name COLLATE NOCASE " &
               "AND preset_name <> @last_used_preset_name COLLATE NOCASE " &
               "LIMIT 1;"
    End Function

    Friend Function BuildDeleteNamedPresetSql() As String
        Return "DELETE FROM configuration_presets " &
               "WHERE preset_name = @preset_name COLLATE NOCASE " &
               "AND preset_name <> @last_used_preset_name COLLATE NOCASE;"
    End Function

    Friend Function BuildSavePresetSql() As String
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
               "tail_lightening_cutouts_enabled, tail_main_spar_diameter, tail_rudder_clearance, horizontal_tail_chord, " &
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
               "@tail_lightening_cutouts_enabled, @tail_main_spar_diameter, @tail_rudder_clearance, @horizontal_tail_chord, " &
               "@horizontal_tail_half_span, @horizontal_tail_rib_count, @horizontal_tail_airfoil, " &
               "@vertical_tail_root_chord, @vertical_tail_tip_chord, @vertical_tail_span, " &
               "@vertical_tail_rib_count, @vertical_tail_airfoil" &
               ");"
    End Function
End Module
