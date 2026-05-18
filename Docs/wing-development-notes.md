# Wing Development Notes

Date: 2026-05-13

This note records the CATIA V5 / VB.NET wing-generation design and implementation state. It is intended as a handoff document for future AI agents or team members.

## Current Code Organization

The CAD generation code is split across focused files in the `CAD` folder:

```text
CAD/GenerateAirfoil.vb          Public facade used by the UI button
CAD/WingGenerator.vb            Stage 1 through Stage 4C wing generation
CAD/WingDefinition.vb           Wing, rib, spar, cutout, aileron, and NACA 4415 constants
CAD/WingStation.vb              Wing station/profile data structures
CAD/NacaAirfoil.vb              General NACA 4-digit coordinate generation
CAD/AirfoilCoordinate.vb        Airfoil coordinate data structure
CAD/CatiaInterop.vb             CATIA COM helper functions
CAD/Naca2412SliceGenerator.vb   Previous standalone 3 mm NACA 2412 test slice
```

The UI is not meant to contain CAD logic. The button handler should only call:

```vb
GenerateAirfoil.Run()
```

At the time of this note, `Run()` calls the Stage 4C physical rib, main spar, cutout, and aileron generator.

## Current Implementation

`GenerateAirfoil.vb` currently contains:

- Public entry points only.
- `Run()`, which delegates to Stage 4C.
- Compatibility wrappers for Stage 1, Stage 2, Stage 3, Stage 4A, Stage 4B, Stage 4C, and the NACA 2412 test slice.

The active workflow is Stage 4C.

## Wing Concept

The goal is a basic UAV wing model with:

- One full-span outer wing shape.
- Physical internal ribs.
- One hollow main spar at 30% local chord.
- Circular rib cutouts for spar clearance and weight reduction.
- Two tail-style aileron control surfaces, one left and one right.
- No twist.
- No dihedral.
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
Airfoil: NACA 4415 at every station
Rib thickness: 3 mm
Ribs: 29 total
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
Forward cutout: 15% chord, preferred diameter 22 mm
Middle cutout: 50% chord, preferred diameter 34 mm
Aft cutout: 70% chord, preferred diameter 22 mm
Minimum edge margin to airfoil skin: 6 mm
Minimum generated cutout diameter: 8 mm
```

Cutout diameters are computed per station. If a rib is too thin for a preferred diameter, the cutout is reduced to preserve the minimum edge margin.

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
Span per aileron: Rib_08 to Rib_14 = 759.35 mm
Semi-span fraction: 42.86%
Tip margin: none
Inner aileron boundary: +/-1012.471 mm at Rib_08
Outer aileron boundary: +/-1771.825 mm at Rib_14 / wing tip
Fixed wing panel aft edge: X = 261.8 mm
Rear hinge spar: X = 261.8 mm to X = 273.02 mm
Aileron body: X = 280.5 mm to local trailing edge
Clearance between rear spar and aileron: 7.48 mm
```

The aileron chord stations are constant global X values, so the fixed cut, rear hinge spar, and aileron leading edge remain parallel to the straight wing leading edge. The aileron span is rib-to-rib instead of exactly 40% semi-span so the control surface ends on structural rib stations.

## Coordinate System

The generated wing uses this coordinate convention:

```text
X = chord direction, from leading edge to trailing edge
Y = span direction, from left tip through center to right tip
Z = airfoil thickness direction
```

The leading edge is straight and unswept:

```text
X = 0 at every span station
```

The taper is created only by reducing chord length toward the tips:

```text
Center/root chord = 586 mm
Tip chord = 374 mm
```

So the trailing edge moves forward toward each tip.

The wing uses NACA 4415 at every station. Only the chord length changes along the span.

NACA 4415 parameters used in code:

```text
Maximum camber: 0.04
Maximum camber position: 0.4
Maximum thickness: 0.15
```

## Stage 1: Planform And Rib Stations

Stage 1 created reference geometry only:

- Full-span straight leading edge.
- Tapered trailing edge on left and right halves.
- 29 chordwise rib station lines.

This stage has been verified in CATIA.

The rib station lines are currently useful visual scaffolding. They help confirm spacing, chord length, taper, and leading-edge alignment. They may later be hidden or kept in a reference geometrical set.

## Stage 2: Airfoil Station Profiles

Stage 2 adds NACA 4415 airfoil profiles at the same 29 rib stations.

Current behavior:

- Creates a new CATIA Part.
- Creates a geometrical set for planform and rib station lines.
- Creates a separate geometrical set for NACA 4415 station profiles.
- Builds 29 airfoil station splines:
  - 14 left side profiles.
  - 1 center profile.
  - 14 right side profiles.
- Keeps the leading edge at `X = 0`.
- Uses local chord length based on span position.
- Places each profile in an X-Z airfoil plane at its Y station.

## Stage 3: Outer Wing Skin

Stage 3 creates the outer wing skin:

- Use the 29 NACA 4415 station profiles.
- Create a CATIA Multi-Section Surface through those profiles.
- Keep the skin as a surface with no thickness for now.

Current behavior:

- Creates a new CATIA Part.
- Creates a geometrical set for planform and rib station lines.
- Creates a geometrical set for NACA 4415 station profiles.
- Creates a geometrical set for the outer wing skin.
- Builds one lofted outer skin surface through the 29 profiles.
- Uses a consistent profile closing point to help CATIA align the loft sections.

Manual wrapper for this stage:

```vb
CreateWingStage3OuterWingSkin()
```

## Stage 4A: Physical Ribs

Stage 4A creates physical ribs:

- Use the same 29 station shapes.
- Create 3 mm solid rib plates centered on each station.
- Keep each rib as a separate named CATIA body.
- Keep the outer skin as a surface with no thickness.
- Keep the Stage 1 planform, Stage 2 station profiles, and Stage 3 outer skin in the same generated part for reference.

Current behavior:

- Creates a new CATIA Part.
- Creates the planform and rib station reference geometry.
- Creates the 29 NACA 4415 station profiles.
- Creates one lofted outer wing skin surface through those profiles.
- Creates one mid-plane per rib station.
- Creates one smooth closed rib sketch per station, with a polyline fallback if CATIA cannot create the 2D sketch spline.
- Pads each rib sketch into a 3 mm centered solid rib body.
- Converts each intended global X/Z rib point into CATIA sketch-local coordinates using the sketch's actual axis data. This avoids flipped or perpendicular ribs caused by CATIA's default `PlaneZX` local axis orientation.
- Uses required CATIA updates for Stage 4A geometry so failures produce explicit errors instead of silently leaving a partial model.

Manual wrapper for this stage:

```vb
CreateWingStage4APhysicalRibs()
```

## Stage 4B: Main Spar And Rib Cutouts

Stage 4B is the previous active generator.

It creates:

- The Stage 1 planform and rib station reference geometry.
- The Stage 2 NACA 4415 station profiles.
- The Stage 3 outer wing skin surface.
- The Stage 4A physical rib bodies.
- One 30% chord hollow circular main spar.
- One spar clearance hole in each rib.
- Three circular lightening cutouts in each rib.

Main spar behavior:

- The spar follows 30% of local chord, so its X location changes with taper.
- The spar center follows the NACA 4415 mean camber line.
- The generated spar is modeled as a hollow circular tube with 30 mm outer diameter and 1.5 mm wall thickness.
- The spar is generated as left and right spanwise rib features that meet at the center rib.

Rib cutout behavior:

- Each rib sketch includes the spar clearance hole before the rib is padded.
- Each rib sketch includes three lightening cutout circles before the rib is padded.
- The cutouts are parameterized by chord fraction and diameter rules, not fixed coordinates.
- The cutout centers follow the NACA 4415 mean camber line at their chord fractions.
- Cutout diameters automatically shrink if needed to keep the minimum edge margin.

This parameter structure is intended to stay compatible with a later Power Copy workflow. A future rib Power Copy can expose chord length, rib plane, spar position, spar diameter, and lightening cutout definitions as inputs.

Manual wrapper for this stage:

```vb
CreateWingStage4BPhysicalRibsAndMainSpar()
```

## Stage 4C: Tail-Style Ailerons

Stage 4C is the current active generator.

It creates everything from Stage 4B, then adds left and right aileron geometry modeled after the tail control-surface approach:

- The aileron span runs from Rib_08 to Rib_14 on each semi-span.
- The aileron reaches the outermost rib at the wing tip.
- The aileron span is 759.35 mm, or 42.86% of each semi-span.
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
- Stage 4C does not create one full wing skin over the aileron region.
- The center/inboard fixed wing skin is one full-chord surface from left Rib_08 to right Rib_08.
- The left and right outboard fixed wing skins are separate closed loft surfaces from the leading edge to X = 261.8 mm.
- The left and right aileron support surfaces are separate closed colored loft surfaces from X = 280.5 mm to the trailing edge.
- The aileron support surfaces are closed into physical aileron solids.
- The rear hinge spar is generated from closed airfoil slices between X = 261.8 mm and X = 273.02 mm.
- The split profiles use constant-X stations, so CATIA is guided to keep the aileron collection parallel to the leading edge.
- Stage 4C adds upper and lower reference curves for the fixed wing rear spar face, rear hinge spar aft face, aileron leading edge, and aileron inner/outer end cuts.
- The aileron skin/support surfaces do not sit on top of another full wing skin surface, so CATIA should not fade or flicker their color against an overlapping skin.
- The fixed wing skin is not thickened yet, so there is no removed thick-skin material.

Manual wrapper for this stage:

```vb
CreateWingStage4CPhysicalRibsMainSparAndAilerons()
```

## Verification Checklist

When running the current Stage 4C code in CATIA, verify:

- Full span is 3543.65 mm.
- Center chord is 586 mm.
- Tip chord is 374 mm.
- Leading edge is straight at X = 0.
- Taper is only from the trailing edge moving forward.
- There are 29 rib stations.
- Every station profile is NACA 4415.
- Profiles are oriented with span along Y and thickness along Z.
- Stage 4C creates separate fixed wing and aileron surfaces instead of one full wing skin surface over the aileron span.
- The fixed wing skin has no solid thickness yet.
- There are 29 physical rib bodies across 29 rib stations.
- Each rib section is 3 mm thick in the span direction.
- The ribs sit at the same stations as the airfoil profiles.
- Full and forward rib sections have one 31 mm main spar clearance hole when the circular cutout fits fully inside that section.
- Full and split rib sections keep lightening cutouts only when each circular cutout fits fully inside the generated rib section.
- The lightening cutouts are positioned at 15%, 50%, and 70% chord.
- The lightening cutouts remain inside the airfoil profile on tip ribs.
- The main spar is a hollow circular tube at 30% local chord.
- The main spar passes through the rib spar holes.
- There are left and right aileron rear hinge spar solids.
- There are left and right physical aileron solids.
- There are left and right aileron reference curves.
- Each aileron has 759.35 mm span.
- Each aileron starts at Rib_08 and ends at Rib_14 / the wing tip.
- The fixed wing panel ends at constant X = 261.8 mm inside the aileron span.
- The rear hinge spar occupies constant X = 261.8 mm to X = 273.02 mm.
- The aileron body starts at constant X = 280.5 mm and reaches the local trailing edge.
- Ribs inside the aileron region are trimmed to forward wing rib bodies only; no aft rib leftovers are generated.
- The aileron surfaces and physical aileron solids are visible as separate orange geometry without a full skin underneath them.
