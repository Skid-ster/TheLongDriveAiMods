# 05 Engine Variants

A TLDLoader/HarmonyLib mod that gives the **car 05 engine** three distinct variants:

| Variant | Fuel | Appearance | Character |
| --- | --- | --- | --- |
| **05 Gas Engine (Orange)** | Gasoline | Bright orange paint | Responsive gasoline engine with a slightly sharper stock sound. |
| **05 Diesel Engine (Dark Orange)** | Diesel | Dark burnt-orange paint and subdued embers | More torque, a lower rev limit, and a deep, angry diesel growl with sub-bass and idle knock. |
| **05 Racing Diesel Engine** | Diesel | Blackened dark-orange metallic paint, hot-orange emission, and a racing glow | Higher power and revs, a harder mid-tone diesel note, aggressive growl, and a rising turbo whistle. |

The gasoline variant replaces the `gcar05engine` donor in place. The two diesel variants are registered as separate TLDLoader items, so the game's item-database rebuild does not erase them. All three retain the stock engine footprint and use the existing 05 engine model.

## Installation

1. Go to the GitHub Actions tab of this repository.
2. Click the latest successful workflow run (green checkmark).
3. Scroll down to Artifacts and download the **Car05EngineVariants-DLL.zip** file.
4. Extract the `.dll` from the zip.
5. Copy the `.dll` into your game's Mods folder: `C:\Program Files (x86)\Steam\steamapps\common\The Long Drive\Mods\`.
6. Launch the game, open the MultiTool spawner, and search for the new item. Search for **05 Gas**, **05 Diesel**, or **05 Racing Diesel**. The diesel variants are in this mod's TLDLoader item category.
7. This mod does not add an attachable part. Spawn the engine you want, install it in car 05 using the normal engine-mounting interaction, and start the engine.

## Notes

- Use gasoline in the gas variant and diesel in either diesel variant. Oil is topped up as an engine fluid; blood is never used as fuel.
- The vanilla `SMotor` audio source remains active. The procedural growl, sub-bass, heartbeat, and turbo layers are blended quietly with it rather than replacing it.
- The engine must be installed in a car for the running-state rescue and layered audio to activate. An engine sitting on the floor does not play the added audio.
- The racing diesel's glow is emission/light based; the mod never moves an installed engine to simulate vibration.

## Building locally

The project targets **.NET Framework 4.8** and uses the checked-in assemblies in `/libs`:

```powershell
dotnet restore src/Car05EngineVariants/Car05EngineVariants.csproj
dotnet build src/Car05EngineVariants/Car05EngineVariants.csproj -c Release
```

The compiled DLL is written to `src/Car05EngineVariants/bin/Release/net48/Car05EngineVariants.dll`.
