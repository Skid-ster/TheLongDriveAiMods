# Car06 Singularity Reactor

A pocket-sized catastrophe—or a truck-sized bad decision. Replaces the **Car06 petrol engine** (`gcar06enginebenzin`), not the diesel engine or a whole vehicle.

## What it does

- Rolls a size multiplier from **0.55× to 1.8×** in `Awake`, before physics starts.
- Produces **2.5–5× donor torque**, depending on size. Holding the throttle spools a reactor charge for up to another **50% torque**; releasing it lets charge decay. RPM and the drivetrain remain vanilla-controlled.
- Metallic charcoal finish with cyan-to-magenta emission that pulses with charge. No bolt-breaking position animation.
- Auto-runs **only while parented to a car**; disengages the starter every update. On the floor, engine state and motor/idle audio shut down.
- Replaces engine oil and the installed car's tank contents with full oil/petrol every 0.5 seconds. **This removes any pre-existing mixture.** Overheating is disabled; radiator fluid is not refilled.
- Keeps the vanilla motor sound, pitched from **2.4× (tiny)** to **0.55× (large)**, plus a procedural bass/growl/turbine layer at 15–20% of its volume.

## Download and install

1. Open this repository's **Actions → Build Singularity** and select a successful build for the branch/commit you want.
2. Download the **Singularity** artifact at the bottom of the run page (GitHub sign-in may be required).
3. Extract `Singularity.dll` and place it in the Mods directory used by your installed **TLDLoader**.
4. Enable the mod, restart the game, and find/spawn a Car06 petrol engine using your usual item-spawn tool, or find one through normal loot. This mod does not add a spawn menu or grant a free engine.
5. Install it in a compatible vehicle. Use the throttle normally; ignition-off is intentionally overridden while installed.

Only install the mod DLL. **Do not copy the repository's reference DLLs into the game.** Requires TLDLoader and Harmony compatible with the supplied game assemblies. Avoid other mods replacing the same donor or patching its running/audio logic.

## Saves and limitations

Back up saves first. The vanilla item ID is preserved, so this is a donor replacement, not a separate save item. Existing engines already instantiated before registration may remain vanilla; test in a fresh session. Size and charge are **not saved**: size rerolls when the prefab is instantiated on loading, and charge starts empty. Physical size can still interfere with some engine bays despite the 1.8× cap. No in-game playtest is possible from CI; handling, audio mix, save/reload, installation and removal need verification in The Long Drive.

To uninstall, exit the game and remove the mod DLL. Restart so the vanilla database is restored; do not hot-unload during play.

## Build

On Windows with the .NET SDK and .NET Framework 4.8 targeting pack:

```sh
dotnet build src/Singularity/Singularity.csproj --configuration Release --warnaserror
```

References resolve only from repository-root `libs/`. Output is `src/Singularity/bin/Release/net48/Singularity.dll`. `build.yml` runs on every push/PR; `build-singularity.yml` is a manual-only build entry. Both upload only the mod DLL, never game assemblies. Warnings fail the build.

## In-game acceptance checklist

- Spawn several petrol donors: scale varies, never exceeds 1.8×, and colliders track size.
- Fit one to a car: starter disengages, player throttle changes RPM and car moves.
- Hold/release throttle: glow shifts and torque spools/decays; no position jitter.
- Remove the engine: motor/idle/reactor layers stop; ordinary engines remain unaffected.
- Check oil and fuel after 0.5 seconds; confirm intentional mixture replacement.
- Save/reload and verify the documented size reroll and retained vanilla item ID.
