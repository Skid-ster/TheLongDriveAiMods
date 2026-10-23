# Lada Restomod

A stock-looking Lada sleeper for **The Long Drive**, implemented as a TLDLoader mod. It reuses the game's donor car prefab rather than adding custom meshes, keeping the exterior visually close to stock.

## Features

- Finds a Lada-named car prefab in `itemdatabase` where available, validates that the donor has `carscript`, and falls back to the game's `gcar06` entry (then `gcar05`) if this game build uses generic prefab names.
- Renames the donor prefab **in place** to `Lada Restomod`, adds its initializer, and ensures the donor is present in `itemdatabase.d.items`.
- Gives the donor engine a high-output sleeper tune using the engine `modifier` and `drpm` behavior demonstrated by the Nitrous mod. RPM remains throttle-driven; the mod only rescues a stall below 10 RPM.
- Applies a subtle random engine scale during `Awake`, capped at 1.8x. It does not alter installed engine positions.
- Harmony patches the managed engine's `Running` and `Update` methods. The engine is held running only while parented under a car; when removed, it is stopped and muted.
- Leaves vanilla `SMotor` audio enabled and varies its pitch with engine RPM. Adds quiet, procedural sub-bass, growl, and turbo layers.
- Tops up the engine oil container and car gas tank every 0.5 seconds while the managed engine is installed in a car.

## Build and installation

The repository workflow builds the .NET Framework 4.8 assembly on `windows-latest` and publishes `M_LadaRestomod.dll` as the `LadaRestomod` artifact. Download that artifact and place the DLL in the game's TLDLoader mods folder.

The project references the game and loader assemblies from `../../libs`. For local development, keep those libraries in the repository's `libs` directory.

## Compatibility notes

The game does not expose a stable human-readable donor identifier in every version. The mod first searches prefab names for `Lada`; if no matching name exists, it checks the `gcar06` and `gcar05` database fields. Confirm the fallback mapping against the installed game build if its internal car catalog has changed.
