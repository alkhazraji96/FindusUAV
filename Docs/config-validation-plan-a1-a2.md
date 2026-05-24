# FindusUAV Configuration And Validation Plan

Date: 2026-05-19

This document defines the agreed Plan A1 and Plan A2 scope for making FindusUAV configurable while keeping the CAD generator stable. The current priority is to expose useful user inputs without allowing high-risk geometry combinations that can break CATIA generation.

## Planning Decision

The plan is balanced and low-risk because it separates the work into two phases:

- Plan A1: typed configuration, validation, and a limited first set of user inputs.
- Plan A2: progress reporting, preset storage, PowerCopy optimization, and production hardening.

Plan A1 must preserve the currently working wing and tail behavior as the default. The first implementation should be able to generate the same aircraft as today when the user leaves all fields unchanged.

## Core Principles

- User inputs must be explicit, typed, and validated before CATIA generation starts.
- Wing and tail must have separate configuration objects.
- The wing and tail may use different NACA airfoils.
- Only NACA 4-digit airfoils are supported in the first configuration pass.
- Ailerons are configurable only by span fraction of semi-span and always start from the wing tip.
- Inputs that change geometry topology too much are deferred.
- Defaults must match the current known-good geometry.
- Invalid input should produce a clear message and should not create partial CATIA geometry.

## Plan A1 Scope

Plan A1 includes:

1. Create typed configuration models.
2. Add validation and preflight checks.
3. Add user inputs for low-risk wing and tail settings.
4. Add constrained aileron span fraction input.

Plan A1 does not include:

- SQL preset storage.
- Progress bar.
- PowerCopy conversion.
- Major production refactoring.
- Twist, forward sweep, or skin thickness.
- Arbitrary control-surface start/end positions.
- Custom hinge gaps, custom rear spar widths, or custom control-surface chord fractions.

## Plan A2 Scope

Plan A2 includes:

1. Progress bar and status messages.
2. SQLite preset storage.
3. PowerCopy optimization.
4. Production hardening.

Plan A2 should begin only after Plan A1 has stable defaults, validation, and UI input flow.

## Proposed Configuration Objects

The implementation should introduce configuration classes or structures similar to these:

- `AircraftConfiguration`
- `WingConfiguration`
- `TailConfiguration`
- `AirfoilConfiguration`
- `WingSparConfiguration`
- `TailSparConfiguration`
- `RibConfiguration`
- `ControlSurfaceConfiguration`
- `CutoutConfiguration`

The wing and tail should not share one large flat config object. Shared reusable types are fine, but wing and tail settings should remain separate because they have different geometry rules.

## Plan A1 User Inputs

### Wing Inputs

These wing inputs are allowed in Plan A1:

| Input | Type | Default | Notes |
| --- | --- | ---: | --- |
| Full span | Double, mm | 3543.65 | Total wing span. Half span is derived. |
| Root chord | Double, mm | 586.0 | Center chord. |
| Tip chord | Double, mm | 374.0 | Tip chord. |
| Sweep angle | Double, degrees | 0.0 | Swept back only, validated from 0 to 30 degrees. |
| Dihedral angle | Double, degrees | 0.0 | Upward only, validated from 0 to 8 degrees. |
| Wing airfoil | NACA 4-digit string | NACA 4415 | Applies to all wing stations. |
| Rib count per side | Integer | 14 | Total ribs = `(2 * perSide) + 1`. |
| Rib thickness | Double, mm | 3.0 | Centered rib pad thickness. |
| Airfoil point count per surface | Integer | 41 | Can be exposed as advanced input or hidden initially. |
| Main spar chord fraction | Double, 0-1 | 0.30 | Position along local chord. |
| Main spar outer diameter | Double, mm | 30.0 | Circular hollow tube outer diameter. |
| Main spar wall thickness | Double, mm | 1.5 | Must leave positive inner diameter. |
| Main spar rib cutout diameter | Double, mm | 31.0 | Should be larger than spar OD. |
| Lightening cutout enabled | Boolean | True | Enables wing rib lightening cutouts. |
| Forward lightening cutout chord fraction | Double, 0-1 | 0.15 | Position along local chord. |
| Forward lightening cutout preferred diameter | Double, mm | 22.0 | Generated exactly when validation passes. |
| Middle lightening cutout chord fraction | Double, 0-1 | 0.50 | Position along local chord. |
| Middle lightening cutout preferred diameter | Double, mm | 34.0 | Generated exactly when validation passes. |
| Aft lightening cutout chord fraction | Double, 0-1 | 0.70 | Position along local chord. |
| Aft lightening cutout preferred diameter | Double, mm | 20.0 | Generated exactly when validation passes. |
| Aileron span fraction | Double, 0-1 | 0.40 | Fraction of semi-span, always starts at tip. |

### Tail Inputs

These tail inputs are allowed in Plan A1:

| Input | Type | Default | Notes |
| --- | --- | ---: | --- |
| Tail distance offset | Double, mm | 1500.0 | Keep as configurable only if validation protects clearances. |
| Horizontal stabilizer chord | Double, mm | 150.0 | Current horizontal tail chord. |
| Horizontal stabilizer half span | Double, mm | 350.0 | Full horizontal span is derived. |
| Horizontal stabilizer rib count | Integer | 8 | Current tail uses total count. |
| Horizontal stabilizer airfoil | NACA 4-digit string | NACA 0012 | Separate from wing airfoil. |
| Vertical stabilizer root chord | Double, mm | 150.0 | Can differ from horizontal tail chord. |
| Vertical stabilizer tip chord | Double, mm | 75.0 | Tapered vertical tail. |
| Vertical stabilizer span | Double, mm | 250.0 | Current vertical height. |
| Vertical stabilizer rib count | Integer | 4 | Current vertical tail rib count. |
| Vertical stabilizer airfoil | NACA 4-digit string | NACA 0012 | Separate from wing and horizontal tail. |
| Tail rib thickness | Double, mm | 2.0 | Current tail rib thickness. |
| Tail main spar diameter | Double, mm | 6.0 | Current code uses radius 3 mm. |
| Rudder clearance | Double, mm | 25.0 | Keep constrained. |

The tail must be allowed to use different airfoils from the wing. The horizontal and vertical tail may also have different airfoils from each other.

### Aileron Input Policy

Plan A1 allows:

- Aileron span fraction of semi-span.
- Example: `0.40` means each aileron starts at the wing tip and extends inward for 40 percent of the semi-span.

Plan A1 does not allow:

- User-defined aileron inner Y location.
- User-defined aileron outer Y location.
- User-defined aileron chord start.
- User-defined hinge gap.
- User-defined rear spar width.
- User-defined control-surface chord percentage.

This keeps the existing stable topology: the aileron always starts from the tip, and the code may continue to create synthetic inner-boundary profiles when the aileron boundary falls between ribs.

## Deferred Inputs

The following inputs are intentionally deferred because they introduce higher geometry risk:

| Deferred input | Reason |
| --- | --- |
| Wing twist | Changes every station orientation and loft behavior. |
| Forward or compound wing sweep | Changes station orientation and can invalidate current rib/aileron assumptions. |
| Mixed airfoils along span | Requires loft compatibility and section matching rules. |
| Arbitrary aileron start/end | Can collide with rib spacing, synthetic station logic, and split skins. |
| Custom hinge gap | Can create invalid rear spar or aileron profiles. |
| Custom rear spar width | Can invert or collapse the aileron/rear spar region. |
| Custom control-surface chord fraction | Requires broader validation of all split profile regions. |
| Custom lightening cutout positions | Can collide with spar, skin, or clipped aileron ribs. |
| Skin thickness | Adds offset/thicken failure cases and material overlap issues. |
| Hinge hardware | Adds assembly and clearance complexity. |

## Validation Rules

Validation should run before CATIA is opened or modified.

### Common Numeric Validation

- All dimensions must be finite numbers.
- No dimension may be `NaN`, infinity, zero, or negative unless explicitly allowed.
- All fractions must be greater than `0.0` and less than `1.0`.
- User-facing units should be millimeters.
- Error messages should name the exact invalid field.

### NACA Airfoil Validation

- Airfoil must match `NACA ####` or `####`.
- Only 4-digit NACA airfoils are allowed in Plan A1.
- First digit maps to maximum camber percentage.
- Second digit maps to maximum camber position in tenths of chord.
- Last two digits map to maximum thickness percentage.
- Thickness must be greater than zero.
- Symmetric airfoils such as `0012` are valid.
- Invalid examples: `NACA 12`, `441`, `44A5`, `NACA 0000`, `NACA 641212`.

### Wing Planform Validation

- Full span must be within an agreed safe range.
- Root chord must be greater than tip chord or equal to tip chord.
- Sweep angle must be between `0` and `30` degrees.
- Dihedral angle must be between `0` and `8` degrees.
- Tip chord must be large enough to fit spar, cutouts, and aileron split regions.
- Rib count per side must be at least 2.
- Rib count per side should have a practical upper limit to avoid excessive CATIA runtime.
- Rib spacing must be larger than rib thickness.
- Airfoil point count per surface must be high enough to create a smooth profile and low enough to avoid excessive runtime.

Suggested initial ranges:

| Field | Suggested min | Suggested max |
| --- | ---: | ---: |
| Full span | 500 mm | 10000 mm |
| Root chord | 50 mm | 2000 mm |
| Tip chord | 50 mm | 2000 mm |
| Sweep angle | 0 degrees | 30 degrees |
| Dihedral angle | 0 degrees | 8 degrees |
| Rib count per side | 2 | 80 |
| Rib thickness | 0.5 mm | 20 mm |
| Point count per surface | 15 | 121 |

These ranges can be tightened after testing.

### Wing Spar Validation

- Main spar chord fraction must be inside the airfoil, recommended `0.15` to `0.60`.
- Main spar outer diameter must be positive.
- Main spar wall thickness must be positive.
- `2 * wallThickness` must be less than outer diameter.
- Inner diameter must be positive.
- Rib cutout diameter must be greater than or equal to outer diameter plus clearance.
- Rib cutout diameter must clear the combined sweep/dihedral spar projection at the selected planform angles.
- Main spar must fit inside the thinnest relevant wing section with margin.
- Main spar should not collide with the fixed aileron split region.

Suggested initial ranges:

| Field | Suggested min | Suggested max |
| --- | ---: | ---: |
| Spar chord fraction | 0.15 | 0.60 |
| Spar outer diameter | 2 mm | 150 mm |
| Spar wall thickness | 0.2 mm | 25 mm |
| Spar cutout diameter | spar OD | spar OD + 20 mm |

### Wing Cutout Validation

The wing lightening cutout pattern is configurable:

- Forward cutout at 15 percent chord.
- Middle cutout at 50 percent chord.
- Aft cutout at 70 percent chord.

Validation should ensure:

- Chord fractions are finite and between `0.05` and `0.85`.
- Forward, middle, and aft chord fractions remain in increasing order.
- Preferred diameters are finite and between `1 mm` and `200 mm`.
- Requested diameters fit the limiting tip rib while preserving the fixed internal `6 mm` airfoil-skin margin.
- Cutout does not collide with spar clearance hole.
- Cutouts do not overlap each other at the wing tip.
- Cutouts are generated at the exact requested diameter when validation passes.

The current automatic shrink/omit behavior should be preserved.

### Aileron Validation

- Aileron span fraction must be greater than `0.0` and less than `1.0`.
- Suggested range: `0.15` to `0.60`.
- Aileron must always start at the wing tip and extend inward.
- Aileron inner boundary may fall between ribs.
- At least one actual rib station should exist inside the aileron span on each side.
- Aileron fixed panel end, rear spar, and aileron panel start should remain internally controlled in Plan A1.
- The computed aileron panel start must be less than the tip chord trailing edge.

For the current default, `0.40` gives one aileron per side with span equal to 40 percent of semi-span.

### Tail Validation

- Tail horizontal and vertical airfoils must be independently validated as NACA 4-digit airfoils.
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

Suggested initial ranges:

| Field | Suggested min | Suggested max |
| --- | ---: | ---: |
| Horizontal chord | 30 mm | 1000 mm |
| Horizontal half span | 50 mm | 3000 mm |
| Horizontal rib count | 2 | 60 |
| Vertical root chord | 30 mm | 1000 mm |
| Vertical tip chord | 20 mm | 1000 mm |
| Vertical span | 50 mm | 3000 mm |
| Vertical rib count | 2 | 60 |
| Tail rib thickness | 0.5 mm | 20 mm |
| Tail spar diameter | 1 mm | 100 mm |
| Rudder clearance | 0 mm | vertical span * 0.5 |

### Cross-Configuration Validation

The validator should also check relationships between fields:

- Tip chord must support the selected aileron split geometry.
- Spar diameter must be reasonable for both root and tip chord.
- Rib spacing must remain valid after span and rib count changes.
- Tail rudder clearance must not consume too much vertical tail height.
- Tail offsets must keep tail geometry behind the wing if tail offset remains user-configurable.

## UI Behavior

Plan A1 UI should:

- Show wing inputs and tail inputs in separate sections or tabs.
- Show current default values.
- Use numeric controls where possible instead of free text.
- Use a dropdown or masked input for NACA airfoils.
- Show validation errors before running generation.
- Disable generation if required fields are invalid.
- Keep advanced inputs visually separate if they are exposed.
- Provide a reset-to-defaults action.

Recommended initial UI groups:

- Wing Planform
- Wing Airfoil
- Wing Ribs
- Wing Spar
- Wing Aileron
- Horizontal Tail
- Vertical Tail
- Tail Structure

## Implementation Order For Plan A1

1. Add configuration classes with current defaults.
2. Add NACA 4-digit parser.
3. Add validation result type.
4. Add wing configuration validator.
5. Add tail configuration validator.
6. Refactor `WingDefinition` so it can be created from `WingConfiguration` or replaced by an instance-based definition.
7. Update `WingGenerator` to accept a validated wing configuration.
8. Refactor `TailGenerator` to accept a validated tail configuration.
9. Add UI fields using default values.
10. Wire UI values into the configuration objects.
11. Run validation before generation.
12. Keep existing no-argument generation entry points as compatibility wrappers that use default configuration.

## Implementation Order For Plan A2

1. Add progress reporting interface.
2. Add generation status messages by step.
3. Add progress bar UI.
4. Add SQLite preset storage after the configuration schema is stable.
5. Add named presets and last-used preset.
6. Identify stable repeated CATIA features for PowerCopy.
7. Convert low-risk repeated features to PowerCopy usage.
8. Add logging, clearer errors, and production cleanup.

## PowerCopy Candidates For Plan A2

Good candidates:

- Wing ribs.
- Wing rib spar cutout.
- Wing lightening cutout pattern.
- Main spar circular profile.
- Tail horizontal ribs.
- Tail vertical ribs.
- Tail spar profiles.
- Repeated rear spar sections.

Defer:

- Aileron bodies.
- Aileron split skin surfaces.
- Rudder and elevator control surfaces.

Control surfaces are more topology-sensitive, especially when span and station count become configurable. They should remain code-generated until the A1 configuration and validation rules prove stable.

## Production Hardening For Plan A2

Production hardening should include:

- Clear error messages.
- Logging around CATIA generation step execution.
- Validation summaries.
- Compatibility wrappers for default generation.
- Separation of pure geometry math from CATIA COM calls.
- Safer CATIA object naming.
- Better handling of partial CATIA failures.
- Optional cancellation support.
- Basic geometry dry-run checks that do not require CATIA.

## Acceptance Criteria For Plan A1

Plan A1 is complete when:

- Current default wing and tail can still be generated.
- Wing and tail configs are separate.
- Wing, horizontal tail, and vertical tail can use different NACA 4-digit airfoils.
- User can change the allowed Plan A1 inputs from the UI.
- Invalid values are rejected before CATIA generation.
- Aileron span fraction can be changed while always starting from the wing tip.
- No deferred high-risk inputs are exposed.

## Acceptance Criteria For Plan A2

Plan A2 is complete when:

- Generation shows progress and status.
- User inputs can be stored and reloaded from SQLite presets.
- Low-risk repeated CATIA features use PowerCopy where practical.
- The codebase is cleaner, more maintainable, and safer for production use.

