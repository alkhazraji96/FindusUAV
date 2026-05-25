# FindusUAV

FindusUAV is a VB.NET Windows Forms tool for generating UAV wing and tail geometry in CATIA V5.

The app lets the user configure wing and tail dimensions, airfoils, ribs, spars, lightening cutouts, sweep, dihedral, and control-surface related settings, then sends the generated geometry to CATIA.

## Requirements

- Windows
- CATIA V5 installed and licensed
- Visual Studio with .NET Framework 4.8 support
- NuGet restore enabled for SQLite packages

## Build

Open `FindusUAV.slnx` or `FindusUAV.vbproj` in Visual Studio and build the project.

The project targets .NET Framework 4.8 and copies the SQLite native runtime during build.

## Run

1. Start CATIA V5.
2. Open FindusUAV.
3. Adjust the wing and tail inputs.
4. Generate the wing or tail.

Tail generation expects an active CATIA PartDocument. Wing generation creates its own CATIA part.

## Project Layout

- `MainForm/` - Windows Forms UI.
- `CAD/` - CATIA generation, configuration models, validation, and preset storage.
- `Docs/` - development notes and planning documents.
- `Models/` - optional model/output workspace.

## Notes

Configuration presets are saved locally in a SQLite database under the user's local application data folder.
