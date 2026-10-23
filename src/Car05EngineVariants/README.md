# 05 Engine Variants

A TLDLoader/HarmonyLib mod that gives the **car 05 engine** three distinct variants:

| Variant | Fuel | Appearance | Character |
| --- | --- | --- | --- |
| **05 Gas Engine (Orange)** | Gasoline | Bright orange paint | Responsive gasoline engine with a slightly sharper stock sound. |
| **05 Diesel Engine (Dark Orange)** | Diesel | Dark burnt-orange paint and subdued embers | More torque, a lower rev limit, and a deep, angry diesel growl with sub-bass and idle knock. |
| **05 Racing Diesel Engine** | Diesel | Blackened dark-orange metallic paint, hot-orange emission, and a racing glow | Higher power and revs, a harder mid-tone diesel note, aggressive growl, and a rising turbo whistle. |

The gasoline variant replaces the `gcar05engine` donor in place. The two diesel variants are registered as separate TLDLoader items, so the game's item-database rebuild does not erase them. All three retain the stock engine footprint and use the existing 05 engine model.

## Direct download

**[Download Car05EngineVariants.dll](https://github.com/Skid-ster/TheLongDriveAiMods/releases/download/v1.0.0/Car05EngineVariants.dll)**

This is the compiled mod, not the source code. Save the file, then follow the installation steps below. The release page also includes the build commit and a SHA-256 checksum.

## Installation

1. Download **`Car05EngineVariants.dll`** using the direct link above (or open the [v1.0.0 release](https://github.com/Skid-ster/TheLongDriveAiMods/releases/tag/v1.0.0)).
2. Copy the DLL into your game's Mods folder. The usual Steam path is:
   `C:\Program Files (x86)\Steam\steamapps\common\The Long Drive\Mods\`
3. Make sure **TLDLoader** and its required dependencies are installed and enabled. Do not rename the DLL to a `.zip` or place it inside another mod's folder.
4. Launch the game and load a save.
5. Open the MultiTool spawner and search for **05 Gas**, **05 Diesel**, or **05 Racing Diesel**. The diesel variants are in this mod's TLDLoader item category.
6. Spawn the engine you want, install it in car 05 using the normal engine-mounting interaction, and start the engine. This mod does not add an attachable part.

## Notes

- Use gasoline in the gas variant and diesel in either diesel variant. When a variant is installed, its fuel and oil tanks are kept compatible and topped up every 0.5 seconds; blood is never used as fuel.
- The vanilla `SMotor` audio source remains active. The procedural growl, sub-bass, heartbeat, and turbo layers are blended quietly with it rather than replacing it.
- The engine must be installed in a car for the running-state rescue and layered audio to activate. An engine sitting on the floor is silent.
- The racing diesel's glow is emission/light based; the mod never moves an installed engine to simulate vibration.
- The mod targets the supplied game/TLDLoader assemblies. Back up your save before installing or removing mods.

## Building locally

The project targets **.NET Framework 4.8** and uses the checked-in assemblies in `/libs`:

```powershell
dotnet restore src/Car05EngineVariants/Car05EngineVariants.csproj
dotnet build src/Car05EngineVariants/Car05EngineVariants.csproj -c Release
```

The compiled DLL is written to `src/Car05EngineVariants/bin/Release/net48/Car05EngineVariants.dll`.

## Publishing a release from GitHub Actions

Maintainers can open **Actions → Build Mods → Run workflow**, enable **Publish the DLL as a GitHub release**, and choose a release tag. The workflow builds the same Release DLL, uploads the normal build artifact, and publishes the raw DLL as a release asset. No local .NET installation is needed for that route.
