# FindusUAV Configuration And Validation Reference

Date: 2026-05-25

This document started as the Plan A1/A2 configuration plan. It now reflects the current implemented behavior after the production cleanup pass.

## Current Status

Implemented:

- Typed wing and tail configuration models.
- NACA 4-digit airfoil parsing and validation.
- Wing and tail validators.
- UI inputs for current supported wing and tail parameters.
- Progress reporting.
- SQLite named presets and last-used preset storage.
- Wing sweep angle, swept back only.
- Wing dihedral angle, upward only.
- Dynamic wing lightening cutout settings.
- Tail lightening cutout enable/disable setting.
- CATIA tree cleanup that hides construction geometry.
- Display refresh suspension during generation.
- Production cleanup of old debug/timing logs and test-only generation paths.

Still deferred:

- Wing twist.
- Forward sweep.
- Mixed airfoils along the wing span.
- Arbitrary aileron start/end locations.
- Custom hinge gap, rear spar width, or control-surface chord fractions.
- Skin thickness.
- Hinge hardware.
- PowerCopy conversion.
- Optional cancellation support.

## Core Rules

- User inputs must be explicit, typed, and validated before CATIA generation starts.
- Wing and tail configuration stay separate.
- Wing, horizontal tail, and vertical tail may use different NACA 4-digit airfoils.
- Defaults must remain the known-good geometry.
- Invalid input should show a clear validation message and should not create partial CATIA geometry.
- The generator should respect accepted user values exactly, especially cutout diameters.

## Configuration Models

Current model files:

```text
AircraftConfiguration
WingConfiguration
WingRibConfiguration
WingSparConfiguration
TailConfiguration
HorizontalTailConfiguration
VerticalTailConfiguration
TailSparConfiguration
AirfoilConfiguration
ControlSurfaceConfiguration
RibLighteningCutoutConfiguration
```

The SQLite preset mapping is split into:

```text
ConfigurationPresetRepository
ConfigurationPresetSql
ConfigurationPresetSchema
ConfigurationPresetMapper
ConfigurationPresetCommandHelpers
```

Current SQLite schema version: `5`

## Wing Inputs

| Input | Type | Default | Validation |
| --- | --- | ---: | --- |
| Full span | Double, mm | 3543.65 | 500 to 10000 |
| Root chord | Double, mm | 586.0 | 50 to 2000 |
| Tip chord | Double, mm | 374.0 | 50 to 2000 |
| Sweep angle | Double, degrees | 0.0 | 0 to 30, swept back only |
| Dihedral angle | Double, degrees | 0.0 | 0 to 8, upward only |
| Wing airfoil | NACA 4-digit string | NACA 4415 | 4-digit NACA with non-zero thickness |
| Rib count per side | Integer | 14 | 2 to 80 |
| Rib thickness | Double, mm | 3.0 | 0.5 to 20 |
| Airfoil point count per surface | Integer | 41 | 15 to 121 |
| Main spar chord fraction | Double, 0-1 | 0.30 | 0.15 to 0.60 |
| Main spar outer diameter | Double, mm | 30.0 | 2 to 150 |
| Main spar wall thickness | Double, mm | 1.5 | 0.2 to 25 and leaves positive inner diameter |
| Main spar rib cutout diameter | Double, mm | 31.0 | At least spar OD and fits planform angles |
| Wing lightening cutouts enabled | Boolean | True | Disables cutout inputs when false |
| Forward cutout chord fraction | Double, 0-1 | 0.15 | 0.05 to 0.85 |
| Forward cutout diameter | Double, mm | 22.0 | 1 to 200 and must fit |
| Middle cutout chord fraction | Double, 0-1 | 0.50 | 0.05 to 0.85 |
| Middle cutout diameter | Double, mm | 34.0 | 1 to 200 and must fit |
| Aft cutout chord fraction | Double, 0-1 | 0.70 | 0.05 to 0.85 |
| Aft cutout diameter | Double, mm | 20.0 | 1 to 200 and must fit |
| Aileron span fraction | Double, 0-1 | 0.40 | 0.15 to 0.60 |

## Tail Inputs

| Input | Type | Default | Validation |
| --- | --- | ---: | --- |
| Tail distance offset | Double, mm | 1500.0 | Positive and within safe range |
| Tail point count per surface | Integer | 50 | Valid point-count range |
| Tail rib thickness | Double, mm | 2.0 | 0.5 to 20 |
| Tail lightening cutouts enabled | Boolean | True | Enables/disables built-in tail rib cutouts |
| Tail main spar diameter | Double, mm | 6.0 | 1 to 100 and fits selected airfoils |
| Rudder clearance | Double, mm | 25.0 | 0 to vertical span * 0.5 |
| Horizontal stabilizer chord | Double, mm | 150.0 | 30 to 1000 |
| Horizontal stabilizer half span | Double, mm | 350.0 | 50 to 3000 |
| Horizontal stabilizer rib count | Integer | 8 | 2 to 60 |
| Horizontal stabilizer airfoil | NACA 4-digit string | NACA 0012 | 4-digit NACA with non-zero thickness |
| Vertical stabilizer root chord | Double, mm | 150.0 | 30 to 1000 |
| Vertical stabilizer tip chord | Double, mm | 75.0 | 20 to 1000 |
| Vertical stabilizer span | Double, mm | 250.0 | 50 to 3000 |
| Vertical stabilizer rib count | Integer | 4 | 2 to 60 |
| Vertical stabilizer airfoil | NACA 4-digit string | NACA 0012 | 4-digit NACA with non-zero thickness |

Tail cutout parameters are not exposed. The user only controls whether tail lightening cutouts are enabled.

## NACA Airfoil Validation

- Accepts `NACA ####` or `####`.
- Only 4-digit NACA airfoils are supported.
- First digit maps to maximum camber percentage.
- Second digit maps to maximum camber position in tenths of chord.
- Last two digits map to maximum thickness percentage.
- Thickness must be greater than zero.
- Symmetric airfoils such as `0012` are valid.
- Invalid examples: `NACA 12`, `441`, `44A5`, `NACA 0000`, `NACA 641212`.

## Wing Planform Validation

- Full span, root chord, and tip chord must be finite and positive.
- Root chord must be greater than or equal to tip chord.
- Sweep angle must be between `0` and `30` degrees.
- Dihedral angle must be between `0` and `8` degrees.
- Rib spacing must be larger than rib thickness.
- Tip chord must be large enough for spar, cutouts, and aileron split geometry.
- Airfoil point count must stay high enough for smooth profiles and low enough for runtime.

Forward sweep is intentionally rejected.

## Wing Spar Validation

- Main spar chord fraction must stay inside the supported chord range.
- Main spar OD and wall thickness must be positive.
- `2 * wallThickness` must be less than OD.
- Inner diameter must be positive.
- Rib cutout diameter must be at least the spar OD.
- Rib cutout diameter must clear the combined sweep/dihedral spar projection through rib planes.
- Main spar must fit within the thinnest relevant airfoil section.
- Main spar must not invalidate the fixed aileron forward panel.

## Wing Lightening Cutout Validation

When wing lightening cutouts are disabled, cutout-specific validation is skipped and the UI disables those controls.

When enabled:

- Chord fractions must be finite and between `0.05` and `0.85`.
- Forward, middle, and aft chord fractions must remain in increasing order.
- Diameters must be finite and between `1 mm` and `200 mm`.
- Diameters are generated exactly as entered when validation passes.
- The validator rejects diameters that cannot keep the fixed internal `6 mm` airfoil-skin margin at the limiting tip rib.
- Cutouts must not overlap each other at the wing tip.
- Cutouts must not collide with the main spar clearance hole.

The old shrink/omit behavior is no longer the user-facing design. The validator must reject invalid exact diameters instead of silently changing them.

## Aileron Validation

- Aileron span fraction must be between `0.15` and `0.60`.
- Ailerons always start at the wing tip and extend inward.
- The inner boundary may fall between ribs.
- At least one actual rib station must exist inside the aileron span on each side.
- Fixed panel end, rear spar width, and aileron panel start are internally controlled.
- The computed aileron panel start must be less than the tip chord trailing edge.

## Tail Validation

- Horizontal and vertical tail airfoils are validated independently.
- Horizontal half span must be positive.
- Horizontal rib count must be at least 2.
- Vertical span must be positive.
- Vertical rib count must be at least 2.
- Vertical root chord and tip chord must be positive.
- Vertical tip chord must be large enough for spar and rudder geometry.
- Tail rib spacing must be larger than tail rib thickness.
- Tail main spar diameter must fit within the selected airfoil thickness.
- Rudder clearance must be greater than or equal to `0.0`.
- Rudder clearance must be less than vertical stabilizer span.
- If tail lightening cutouts are disabled, tail cutout fit validation is skipped.

## UI Behavior

Current UI behavior:

- Wing and tail settings are separated into groups.
- Numeric controls are used for numeric inputs.
- Airfoils are selected from NACA 4-digit options.
- Validation runs before generation.
- The wing lightening cutout inputs are disabled when wing lightening cutouts are off.
- Tail has a single lightening cutouts enabled checkbox.
- Tail generation connection/open-document errors remain visible to the user.
- Tail success popup was removed.

## CATIA Generation Behavior

Wing generation creates a new CATIA part.

Tail generation expects CATIA to already have an active `PartDocument`; this is intentional UX behavior.

Both wing and tail generation hide construction/reference geometry after final update while preserving CATIA dependencies.

## Preset Storage

Preset storage is implemented with SQLite.

Stored preset data includes:

- Wing planform values.
- Wing sweep and dihedral.
- Wing airfoil and point count.
- Wing ribs, spar, cutouts, and aileron values.
- Tail point count, offset, rib thickness, and lightening cutout enable flag.
- Tail spar, rudder clearance, horizontal tail, and vertical tail values.

Existing user databases migrate by adding missing columns. Schema version `5` added `tail_lightening_cutouts_enabled`.

## Production Cleanup Notes

Removed or cleaned:

- Old `GenerateAirfoil.vb` facade name.
- Old standalone NACA 2412 test slice generator.
- Debug wing timing logs.
- CATIA capability-learning debug logs.
- Stage-style generated names.
- Tail completion popup.
- Large mixed-responsibility CAD files.

Current production posture:

- Debug and Release builds should pass with 0 warnings and 0 errors.
- `git diff --check` should pass; CRLF normalization warnings are acceptable in this repo.
- Preset roundtrip should preserve sweep, dihedral, wing cutouts, and tail cutout enable state.
- Final CATIA smoke testing should be done manually for wing and tail before release.
