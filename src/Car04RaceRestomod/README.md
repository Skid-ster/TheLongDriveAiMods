# Car 04 Race Restomod

A race-themed, high-performance mod built specifically on `itemdatabase.d.gcar04` in **The Long Drive**. It reuses the game's car prefab and does not substitute a Lada or another donor.

## Features

- Renames the `gcar04` donor prefab to **Car 04 Race Restomod**, checks that it has a `carscript`, and ensures it is present in `itemdatabase.d.items`.
- Applies an electric-blue paint treatment with orange racing accents to opaque body-like renderers, while skipping named glass, wheels, lights, interior and chrome parts.
- Gives the donor engine the sleeper performance tune, stall rescue, and installed-car-only running behavior. An engine removed from the car is stopped and muted.
- Keeps the vanilla engine audio and adds quiet procedural sub-bass, growl, and turbo layers.
- Tops up the engine oil and car gas every 0.5 seconds while the managed engine is installed.
- Shows a live telemetry panel while driving the modified car: speed, gear, RPM, fuel, and oil.

## Build and installation

The .NET Framework 4.8 project is `Car04RaceRestomod.csproj`. GitHub Actions builds it on `windows-latest`, copies `M_Car04RaceRestomod.dll` into `mods/Car 04 Race Restomod/`, commits an updated DLL on branch pushes, and uploads the packaged `mods` folder as a downloadable artifact. Install by copying the DLL into the game's TLDLoader mods folder.

The project uses the game and loader assemblies in `../../libs`.
