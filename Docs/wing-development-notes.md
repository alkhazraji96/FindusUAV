# Wing Development Notes

Date: 2026-05-23

This note records the CATIA V5 / VB.NET wing-generation design and implementation state. It is intended as a handoff document for future AI agents or team members.

## Current Code Organization

The CAD generation code is split across focused files in the `CAD` folder:

```text
CAD/AircraftConfiguration.vb    Wing and tail configuration defaults
CAD/GenerateAirfoil.vb          Public facade used by the UI button
CAD/WingGenerator.vb            Wing geometry generation
CAD/WingDefinition.vb           Wing definition values derived from active configuration
CAD/WingConfigurationValidator.vb Wing configuration validation rules
CAD/TailConfigurationValidator.vb Tail configuration validation rules
CAD/WingStation.vb              Wing station/profile data structures
CAD/NacaAirfoil.vb              General NACA 4-digit coordinate generation
CAD/AirfoilCoordinate.vb        Airfoil coordinate data structure
CAD/CatiaInterop.vb             CATIA COM helper functions
CAD/Naca2412SliceGenerator.vb   Previous standalone 3 mm NACA 2412 test slice
```

The UI is not meant to contain CAD logic. The generation button handlers should only build/validate configuration and call the CAD facades:

```vb
GenerateAirfoil.Run()
GenerateAirfoil.Run(currentConfiguration.Wing)
TailGenerator.Run(currentConfiguration.Tail)
```

At the time of this note, `Run()` calls the physical rib, main spar, cutout, and aileron generator.

## Current Implementation

`GenerateAirfoil.vb` currently contains:

- Public entry points only.
- `Run()`, which delegates to the complete wing generator.
- Compatibility wrappers for planform, airfoil stations, outer skin, physical ribs, ribs/main spar, complete wing, and the NACA 2412 test slice.

The active workflow is the complete wing generator with ribs, main spar, lightening cutouts, and ailerons.

## Wing Concept

The goal is a basic UAV wing model with:

- One full-span outer wing shape.
- Physical internal ribs.
- One hollow main spar at 30% local chord.
- Circular rib cutouts for spar clearance and weight reduction.
- Two tail-style aileron control surfaces, one left and one right.
- No twist.
- No skin thickness yet.
- No aileron hinge hardware or joints yet.

The fixed outer skin is a surface, while the ribs, main spar, aileron rear hinge spars, and aileron control surfaces are physical solids.

## Wing Inputs

All CATIA model dimensions are in millimeters.

```text
Full span: 3.54365 m = 3543.65 mm
Half span: 1771.825 mm
Root chord: 0.586 m = 586 mm
Tip chord: 0.374 m = 374 mm
Sweep angle: 0 degrees by default, configurable from 0 to 30 degrees swept back only
Dihedral angle: 0 degrees by default, configurable from 0 to 8 degrees upward only
Airfoil: selected NACA 4-digit airfoil, default NACA 4415 at every station
Rib thickness: 3 mm
Ribs: 29 total by default
```

Main spar:

```text
Spar type: hollow circular tube
Spar location: 30% local chord
Outer diameter: 30 mm
Wall thickness: 1.5 mm
Inner diameter: 27 mm
Rib spar cutout diameter: 31 mm
```

Rib lightening cutouts:

```text
Enabled: true by default
Forward cutout: 15% chord, preferred diameter 22 mm
Middle cutout: 50% chord, preferred diameter 34 mm
Aft cutout: 70% chord, preferred diameter 20 mm
Internal validation margin to airfoil skin: 6 mm
```

The cutout enable flag, chord fractions, and preferred diameters are configurable and stored in presets. Cutout diameters are generated exactly as entered by the user. Validation checks the limiting tip rib against a fixed internal 6 mm skin margin and rejects a cutout that is too large instead of shrinking or omitting it. Sweep translates the cutout global X positions with each rib station leading edge; the cutout fit remains based on local chord and airfoil thickness.

Rib distribution:

```text
1 shared center rib at Y = 0
14 ribs on the left half
14 ribs on the right half
29 ribs total
```

Rib spacing per half:

```text
1771.825 / 14 = 126.5589 mm
```

In code, `HalfSpan` is derived from `FullSpan / 2.0` so the two values cannot drift apart.

Ailerons:

```text
Count: 2 total, one per semi-span
Span per aileron: 708.73 mm by default
Semi-span fraction: 40% by default, configurable in Plan A1
Tip margin: none
Inner aileron boundary: +/-1063.095 mm by default, between Rib_08 and Rib_09
Outer aileron boundary: +/-1771.825 mm at Rib_14 / wing tip
Fixed wing panel aft edge: X = 261.8 mm
Rear hinge spar: X = 261.8 mm to X = 273.02 mm
Aileron body: X = 280.5 mm to local trailing edge
Clearance between rear spar and aileron: 7.48 mm
```

The aileron chord stations are constant global X values, so the fixed cut, rear hinge spar, and aileron leading edge remain parallel to the straight wing leading edge. The aileron span is a configurable fraction of the semi-span and uses a synthetic inner span station when the inner boundary falls between ribs.

## Coordinate System

The generated wing uses this coordinate convention:

```text
X = chord direction, from leading edge to trailing edge
Y = span direction, from left tip through center to right tip
Z = airfoil thickness direction
```

The leading edge is swept back by the configured sweep angle:

```text
LeadingEdgeX = tan(sweep angle) * absolute span position
```

The wing station is lifted by the configured dihedral angle:

```text
DihedralZ = tan(dihedral angle) * absolute span position
GlobalZ = DihedralZ + local airfoil Z
```

The default sweep and dihedral angles are 0 degrees, so the default behavior keeps `X = 0` and `Z = local airfoil Z` at every span station. The taper is still created by reducing chord length toward the tips:

```text
Center/root chord = 586 mm
Tip chord = 374 mm
```

So the trailing edge moves forward toward each tip.

The wing uses one selected NACA 4-digit airfoil at every station. Chord length changes with taper, and station Z changes with the configured dihedral angle.

Default NACA 4415 parameters:

```text
Maximum camber: 0.04
Maximum camber position: 0.4
Maximum thickness: 0.15
```

## Planform And Profiles

The planform and profile workflow creates:

- Swept leading-edge reference lines meeting at the center station, or one full-span line at zero sweep.
- Tapered trailing edge on left and right halves.
- 29 chordwise rib station lines.
- Selected NACA 4-digit airfoil profiles at the same rib stations.
- A CATIA Multi-Section Surface through those profiles for the outer skin.

The rib station lines are useful visual scaffolding. They help confirm spacing, chord length, taper, sweep, dihedral, and leading-edge alignment. They may later be hidden or kept in a reference geometrical set.

Manual wrappers:

```vb
CreateWingPlanform()
CreateWingAirfoilStations()
CreateWingOuterWingSkin()
```

## Physical Ribs And Main Spar

The rib and spar workflow creates:

- 3 mm solid rib plates centered on each station.
- One separate named CATIA body per rib.
- One 30% chord hollow circular main spar.
- One spar clearance hole in each rib.
- Three circular lightening cutouts in each rib.
- Planform, rib station, airfoil profile, skin, rib plane, and spar reference geometry in the same generated part for reference.

Current behavior:

- Creates one mid-plane per rib station.
- Creates one smooth closed rib sketch per station, with a polyline fallback if CATIA cannot create the 2D sketch spline.
- Pads each rib sketch into a 3 mm centered solid rib body.
- Converts each intended global X/Z rib point into CATIA sketch-local coordinates using the sketch's actual axis data. This avoids flipped or perpendicular ribs caused by CATIA's default `PlaneZX` local axis orientation.
- Uses required CATIA updates so failures produce explicit errors instead of silently leaving a partial model.

Main spar behavior:

- The spar follows 30% of local chord, so its X location changes with taper.
- The spar center follows the selected airfoil mean camber line plus the configured dihedral rise.
- The generated spar is modeled as a hollow circular tube with 30 mm outer diameter and 1.5 mm wall thickness.
- The spar is generated as left and right spanwise rib features that meet at the center rib.
- Rib spar cutout validation accounts for the combined sweep and dihedral projection of the spar through each rib plane.

Rib cutout behavior:

- Each rib sketch includes the spar clearance hole before the rib is padded.
- Each rib sketch includes three lightening cutout circles before the rib is padded.
- The cutouts are parameterized by chord fraction and exact requested diameter, not fixed coordinates.
- The cutout centers follow the selected airfoil mean camber line at their chord fractions plus the configured dihedral rise.
- Validation rejects cutout diameters that cannot keep the fixed internal 6 mm airfoil-skin margin at the wing tip.

This parameter structure is intended to stay compatible with a later Power Copy workflow. A future rib Power Copy can expose chord length, rib plane, spar position, spar diameter, and lightening cutout definitions as inputs.

Manual wrappers:

```vb
CreateWingPhysicalRibs()
CreateWingPhysicalRibsAndMainSpar()
```

## Tail-Style Ailerons

The complete wing workflow adds left and right aileron geometry modeled after the tail control-surface approach:

- The aileron span runs from the wing tip inward for the configured fraction of each semi-span.
- The aileron reaches the outermost rib at the wing tip.
- The default aileron span is 708.73 mm.
- The default aileron inner boundary is at +/-1063.095 mm, between Rib_08 and Rib_09.
- The fixed wing panel ends at constant X = 261.8 mm inside the aileron span.
- A closed rear hinge spar solid occupies constant X = 261.8 mm to X = 273.02 mm.
- The physical aileron body starts at constant X = 280.5 mm and continues to the local trailing edge.
- This leaves a 7.48 mm clearance between the aft face of the rear spar and the aileron leading edge.
- The aileron support loft surfaces and closed physical aileron solids are colored orange.

Rib behavior:

- Ribs inside the aileron span are generated only as forward wing rib sections.
- The forward wing rib section stops at constant X = 261.8 mm.
- No aft leftover rib bodies are generated behind the rear hinge spar.
- Existing spar and lightening cutouts are kept only when their circular profile fits fully inside the generated rib section.

Skin behavior:

- The fixed wing skin remains zero-thickness loft surface geometry.
- The generator does not create one full wing skin over the aileron region.
- The center/inboard fixed wing skin is one full-chord surface between the left and right synthetic aileron inner-boundary stations.
- The left and right outboard fixed wing skins are separate closed loft surfaces from the leading edge to X = 261.8 mm.
- The left and right aileron support surfaces are separate closed colored loft surfaces from X = 280.5 mm to the trailing edge.
- The aileron support surfaces are closed into physical aileron solids.
- The rear hinge spar is generated from closed airfoil slices between X = 261.8 mm and X = 273.02 mm.
- The split profiles use constant-X stations, so CATIA is guided to keep the aileron collection parallel to the leading edge.
- The aileron, outboard fixed skin, and rear hinge spar lofts include synthetic inner-boundary profiles because the configured span boundary is not required to land on a rib.
- The generator adds upper and lower reference curves for the fixed wing rear spar face, rear hinge spar aft face, aileron leading edge, and aileron inner/outer end cuts.
- The aileron skin/support surfaces do not sit on top of another full wing skin surface, so CATIA should not fade or flicker their color against an overlapping skin.
- The fixed wing skin is not thickened yet, so there is no removed thick-skin material.

Cleanup behavior:

- The final wing model hides construction/reference geometrical sets after generation.
- Hidden construction includes planform/rib station references, airfoil profile points/splines, split-skin profile points/splines inside visible skin sets, rib mid-planes, main spar references, aileron cut references, and rear hinge spar construction surfaces.
- Sketches inside physical bodies are hidden after their pads, pockets, ribs, and close-surface features are created.
- Final physical bodies, fixed wing skin surfaces, and aileron skin/support surfaces remain visible.
- The cleanup is non-destructive so the CATIA feature tree keeps its parametric dependencies.

Manual wrapper:

```vb
CreateWingPhysicalRibsMainSparAndAilerons()
```

## Verification Checklist

When running the current complete wing code in CATIA, verify:

- Full span is 3543.65 mm.
- Center chord is 586 mm.
- Tip chord is 374 mm.
- Leading edge is straight at X = 0.
- Taper is only from the trailing edge moving forward.
- There are 29 rib stations.
- Every station profile uses the selected wing NACA 4-digit airfoil.
- Profiles are oriented with span along Y and thickness along Z.
- The generator creates separate fixed wing and aileron surfaces instead of one full wing skin surface over the aileron span.
- The fixed wing skin has no solid thickness yet.
- There are 29 physical rib bodies across 29 rib stations.
- Each rib section is 3 mm thick in the span direction.
- The ribs sit at the same stations as the airfoil profiles.
- Full and forward rib sections have one 31 mm main spar clearance hole when the circular cutout fits fully inside that section.
- Full and split rib sections keep lightening cutouts only when each circular cutout fits fully inside the generated rib section.
- The lightening cutouts are positioned at 15%, 50%, and 70% chord by default.
- The lightening cutouts remain inside the airfoil profile on tip ribs.
- The main spar is a hollow circular tube at 30% local chord.
- The main spar passes through the rib spar holes.
- There are left and right aileron rear hinge spar solids.
- There are left and right physical aileron solids.
- There are left and right aileron reference curves.
- Each aileron has the configured span, default 708.73 mm.
- Each aileron starts at the wing tip and extends inward to the configured inner boundary, default +/-1063.095 mm between Rib_08 and Rib_09.
- The fixed wing panel ends at constant X = 261.8 mm inside the aileron span.
- The rear hinge spar occupies constant X = 261.8 mm to X = 273.02 mm.
- The aileron body starts at constant X = 280.5 mm and reaches the local trailing edge.
- Ribs inside the aileron region, currently Rib_09 through Rib_14 on each side, are trimmed to forward wing rib bodies only; no aft rib leftovers are generated.
- The aileron surfaces and physical aileron solids are visible as separate orange geometry without a full skin underneath them.
- Construction/reference geometry and sketches are hidden after generation; only the final skin surfaces and physical parts should remain visible.
