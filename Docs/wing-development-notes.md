# Wing Development Notes

Date: 2026-05-12

This note records the CATIA V5 / VB.NET wing-generation design and implementation state. It is intended as a handoff document for future AI agents or team members.

## Current Code Location

The active CAD generation work is in:

```text
CAD/GenerateAirfoil.vb
```

The UI is not meant to contain CAD logic. The button handler should only call:

```vb
GenerateAirfoil.Run()
```

At the time of this note, `Run()` calls the Stage 3 outer wing skin generator.

## Current Implementation

`GenerateAirfoil.vb` currently contains:

- Stage 1 planform and rib station generation.
- Stage 2 NACA 4415 airfoil station profile generation.
- Stage 3 outer wing skin surface generation.
- A previous standalone `CreateNaca2412Part` helper that can generate a 3 mm padded NACA 2412 test slice.

The active workflow is Stage 3.

## Wing Concept

The goal is a basic UAV wing model with:

- One full-span outer wing shape.
- Physical internal ribs.
- No spars yet.
- No cutouts yet.
- No twist.
- No dihedral.
- No skin thickness yet.

The outer skin will eventually be a surface, while the ribs will be physical 3 mm solid plates.

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

Current `Run()` target:

```vb
CreateWingStage3OuterWingSkin()
```

## Next Intended Stage

Stage 4 should create physical ribs:

- Use the same 29 station shapes.
- Create 3 mm solid rib plates centered on each station.
- Keep ribs as separate bodies or clearly named features.

Spars and cutouts are intentionally deferred until after the skin and ribs are stable.

## Verification Checklist

When running the current Stage 3 code in CATIA, verify:

- Full span is 3543.65 mm.
- Center chord is 586 mm.
- Tip chord is 374 mm.
- Leading edge is straight at X = 0.
- Taper is only from the trailing edge moving forward.
- There are 29 rib stations.
- Every station profile is NACA 4415.
- Profiles are oriented with span along Y and thickness along Z.
- One outer wing skin surface is created through all 29 profiles.
- The skin has no solid thickness yet.
