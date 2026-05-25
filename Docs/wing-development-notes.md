# FindusUAV CAD Development Notes

Date: 2026-05-25

This document records the current CATIA V5 / VB.NET CAD generation state after the production cleanup pass.

## Current Code Organization

The CAD code is split by responsibility:

```text
CAD/AircraftConfiguration.vb             Root aircraft configuration model
CAD/WingConfiguration.vb                 Wing defaults and derived values
CAD/WingRibConfiguration.vb              Wing rib and cutout settings
CAD/WingSparConfiguration.vb             Wing main spar settings
CAD/TailConfiguration.vb                 Tail defaults and derived values
CAD/HorizontalTailConfiguration.vb       Horizontal tail settings
CAD/VerticalTailConfiguration.vb         Vertical tail settings
CAD/TailSparConfiguration.vb             Tail spar settings
CAD/AirfoilConfiguration.vb              NACA airfoil configuration
CAD/RibLighteningCutoutConfiguration.vb  Wing lightening cutout configuration
CAD/ControlSurfaceConfiguration.vb       Aileron span configuration

CAD/WingGenerationFacade.vb              Public wing entry points used by the UI
CAD/WingGenerator.vb                     Wing generation workflow
CAD/WingSkinGeometry.vb                  Wing skins, aileron skins, rear hinge spars, and split references
CAD/WingRibGeometry.vb                   Physical wing ribs and rib cutouts
CAD/WingMainSparGeometry.vb              Wing main spar body and path geometry
CAD/WingSketchGeometry.vb                Sketch-axis and sketch-profile helpers
CAD/WingAirfoilProfileBuilder.vb         Wing airfoil segment/profile helpers
CAD/WingDefinition.vb                    Derived wing geometry values
CAD/WingGenerationNames.vb               Shared wing naming helpers
CAD/WingStation.vb                       Wing station/profile data structures

CAD/TailGenerator.vb                     Tail generation workflow
CAD/TailGeometryBuilder.vb               Tail sketch, loft, cleanup, and cut geometry
CAD/TailAirfoilProfileGenerator.vb       Tail airfoil profile point generation
CAD/TailPoint3D.vb                       Tail profile point structure

CAD/WingConfigurationValidator.vb        Wing validation rules
CAD/TailConfigurationValidator.vb        Tail validation rules
CAD/ConfigurationValidationResult.vb     Validation result and messages

CAD/ConfigurationPresetRepository.vb     Preset repository API
CAD/ConfigurationPresetSql.vb            SQLite SQL text
CAD/ConfigurationPresetSchema.vb         SQLite schema migration
CAD/ConfigurationPresetMapper.vb         Preset row-to-configuration mapping
CAD/ConfigurationPresetCommandHelpers.vb SQLite command helpers

CAD/NacaAirfoil.vb                       General NACA 4-digit coordinate generation
CAD/NacaAirfoilParser.vb                 NACA 4-digit parser
CAD/AirfoilCoordinate.vb                 2D airfoil coordinate structure
CAD/CatiaInterop.vb                      CATIA COM helper functions
CAD/GenerationProgress.vb                Progress reporting
```

Removed production clutter:

- The old `GenerateAirfoil.vb` facade was replaced by `WingGenerationFacade.vb`.
- The standalone NACA 2412 test slice generator was removed.
- Debug timing logs and CATIA capability-learning logs were removed.

## Entry Points

The UI should build a typed configuration, validate it, then call:

```vb
WingGenerationFacade.Run(currentConfiguration.Wing, CreateUiProgressReporter())
TailGenerator.Run(currentConfiguration.Tail, CreateUiProgressReporter())
```

Compatibility wing wrapper methods still exist on `WingGenerationFacade` for planform, stations, skin, ribs, ribs plus spar, and complete wing generation.

## Wing Behavior

The complete wing workflow creates:

- Split fixed-wing skin surfaces.
- Left and right aileron skin/support solids.
- Left and right aileron rear hinge spar solids.
- Physical ribs.
- Main hollow spar.
- Main spar rib cutouts.
- Optional wing rib lightening cutouts.
- Reference geometry needed by CATIA during generation, hidden at the end.

Wing defaults:

```text
Full span: 3543.65 mm
Root chord: 586.0 mm
Tip chord: 374.0 mm
Sweep angle: 0 degrees, valid 0 to 30 degrees swept back only
Dihedral angle: 0 degrees, valid 0 to 8 degrees upward only
Airfoil: NACA 4415
Point count per surface: 41
Ribs per side: 14
Rib thickness: 3.0 mm
Main spar chord fraction: 0.30
Main spar OD: 30.0 mm
Main spar wall thickness: 1.5 mm
Main spar rib cutout diameter: 31.0 mm
Aileron span fraction: 0.40
```

Sweep and dihedral are dynamic and apply across planform, skins, ribs, spar, aileron references, and cutouts:

```text
LeadingEdgeX = tan(sweep angle) * absolute span position
DihedralZ = tan(dihedral angle) * absolute span position
GlobalZ = DihedralZ + local airfoil Z
```

Only swept-back wing sweep is supported. Forward sweep remains intentionally unsupported.

## Wing Lightening Cutouts

Wing lightening cutouts are enabled by default and can be turned off in the UI. When disabled, the cutout parameter controls are disabled because they are not used.

Default cutouts:

```text
Forward: 15% chord, 22.0 mm diameter
Middle: 50% chord, 34.0 mm diameter
Aft: 70% chord, 20.0 mm diameter
```

Current behavior:

- The user-requested diameter is generated exactly when validation passes.
- The generator does not shrink the diameter silently.
- Validation rejects cutouts that cannot preserve the fixed internal 6 mm airfoil-skin margin at the limiting tip rib.
- Validation checks cutout order, overlap between cutouts, and overlap with the main spar clearance hole.
- Cutout positions follow local chord fraction and current sweep/dihedral transforms.
- Aileron-region ribs are clipped to the forward wing panel; cutouts are only created if the circle fits inside that clipped rib section.

## Tail Behavior

Tail generation intentionally expects CATIA to already have an active `PartDocument`.

Tail defaults:

```text
Tail distance offset: 1500.0 mm
Point count per surface: 50
Tail rib thickness: 2.0 mm
Tail lightening cutouts enabled: true
Tail main spar diameter: 6.0 mm
Rudder clearance: 25.0 mm
Horizontal tail chord: 150.0 mm
Horizontal tail half span: 350.0 mm
Horizontal tail ribs: 8
Horizontal tail airfoil: NACA 0012
Vertical tail root chord: 150.0 mm
Vertical tail tip chord: 75.0 mm
Vertical tail span: 250.0 mm
Vertical tail ribs: 4
Vertical tail airfoil: NACA 0012
```

Tail lightening cutouts have a single enable/disable option. There are no user-facing tail cutout diameter or position parameters.

The tail completion popup was removed. Connection and active-document error messages remain because they are actionable UX errors.

## CATIA Tree Cleanup

Both wing and tail generation hide construction/reference geometry after generation.

Hidden items include:

- Reference geometry sets.
- Rib mid-planes.
- Planform/rib station references.
- Airfoil profile construction points and splines.
- Split-skin profile construction geometry.
- Body sketches used to create pads, pockets, ribs, and close-surface features.
- Tail construction surfaces ending with `_Surface`.

Visible final output should be limited to physical parts and final skin/control surfaces. Cleanup is non-destructive so CATIA dependencies remain intact.

## Performance Notes

Current low-risk performance work:

- CATIA display refresh is suspended during active wing and tail generation.
- UI progress updates avoid `Application.DoEvents`.
- Construction geometry hiding is batched where practical.
- Unused station profile generation was removed from the complete wing path.

The previous detailed wing timing debug logs were useful during tuning but are not part of production code.

## Presets

Configuration presets are stored in SQLite under the user's local application data folder.

Current schema version: `5`

Stored current-generation fields include:

- Wing sweep angle.
- Wing dihedral angle.
- Wing lightening cutout enable flag.
- Wing cutout chord fractions and exact requested diameters.
- Tail lightening cutout enable flag.

## Verification Checklist

Before release, smoke-test these flows in CATIA:

- Generate the default wing.
- Generate a wing with non-zero swept-back sweep.
- Generate a wing with non-zero dihedral.
- Generate a wing with lightening cutouts disabled.
- Generate a wing with valid custom lightening cutout positions and diameters.
- Confirm invalid cutout diameters are rejected before CATIA generation.
- Confirm only final wing skins, ailerons, spars, and ribs remain visible.
- Generate the default tail with an active CATIA `PartDocument`.
- Generate the tail with tail lightening cutouts disabled.
- Confirm tail generation finishes without the old completion popup.
- Confirm only final tail skins, spars, ribs, rudder, and elevator remain visible.
