# Car06 1.9 Turbodiesel Conversion

Replaces the Car06 **petrol** engine (`gcar06enginebenzin`) with a diesel-inspired 1.9-litre turbodiesel tune. The existing vanilla Car06 diesel donor supplies its fuel-consumption calibration and sound clips where available; it is not itself modified. These are gameplay tuning targets, not a claim to simulate a particular real production engine.

## Diesel characteristics

| Setting | Tune |
| --- | --- |
| Idle | 850 RPM |
| Starter / stall threshold | 650 / 450 RPM |
| Governed RPM target | 4,500 RPM |
| Peak torque | 180 Nm at 2,200 RPM |
| Torque band | 165–180 Nm from 1,700–3,400 RPM |
| Approximate peak power | 62 kW / 83 hp at 3,800 RPM, before game losses |
| Automatic clutch engagement band | 900–1,200 RPM |
| Fuel | Diesel, not petrol or two-stroke mixture |

Torque falls sharply above 4,200 RPM and reaches zero at 4,500 RPM. This is a fuel/torque governor, **not a hard physics RPM clamp**: wheel-driven over-rev is still possible. Manual drivers should shift around 3,500–3,800 RPM. Stock gear ratios and final drive are unchanged. In the supplied game build, automatic gear selection evaluates the engine torque curve rather than the unused car RPM-threshold fields. It therefore sees the new diesel torque band automatically; no fixed shift points are promised.

The game evaluates the torque curve directly in RPM and Nm. Live RPM is never forced. Throttle, clutch, ignition, starting, stalling, fuel mixtures, consumption, oil and cooling remain controlled by the game. The starter is released after the engine catches, rather than forcing the engine to run. A detached engine is kept off.

Vanilla diesel motor/idle/start/stop clips are reused where supplied. Existing motor and idle AudioSources remain in use, with lower pitch ranges and RPM-matched crossfades. No synthetic reactor/turbo layer, neon materials, random sizing, torque boost or unlimited fluids remain. Physical size and stock appearance are unchanged. Transform vibration is disabled to avoid disturbing installed bolts.

## Install / upgrade from Singularity

1. **Back up your save and exit the game.**
2. Remove the old **Singularity.dll** from your TLDLoader Mods directory. Do not run both versions: they replace the same donor.
3. Open this repository's **Actions → Build Car06Diesel**, select a successful run, and download the **Car06Diesel** artifact (GitHub sign-in may be required).
4. Extract **Car06Diesel.dll** into your installed TLDLoader's Mods directory, enable it, and restart the game.
5. Find/spawn a **Car06 petrol engine** using normal loot or your usual spawn tool. The donor retains its original ID, so spawn menus may still label it petrol.
6. **Drain petrol from the vehicle tank and fill it with diesel.** Keep normal engine oil and radiator coolant topped up, then start with the usual ignition/starter controls.

Fuel is deliberately not converted automatically, and tanks are never refilled or erased. Petrol alone will not run this tune; mixtures follow the game's normal fuel compatibility logic. Already-spawned engines from before registration may stay stock; use a fresh session/engine for testing. No new spawn menu is included.

Requires TLDLoader and Harmony compatible with the supplied `libs/` game assemblies. Install only the mod DLL, never those reference DLLs. Avoid other mods replacing Car06 petrol engines or overriding their engine logic. To uninstall, quit, remove the DLL and restart; the donor returns to petrol behaviour, so change the tank fuel accordingly.

## Build

```sh
dotnet build src/Car06Diesel/Car06Diesel.csproj --configuration Release --warnaserror
```

Target: .NET Framework 4.8. On Windows install the .NET SDK and .NET Framework 4.8 targeting pack. All seven reference DLLs resolve from repository-root `libs/`. Output: `src/Car06Diesel/bin/Release/net48/Car06Diesel.dll`.

`build.yml` builds every push/PR; `build-car06diesel.yml` is manual-only. Both fail on warnings and upload only the mod DLL.

## In-game acceptance checklist

Compilation cannot validate driving feel or sound balance. Test in The Long Drive:

- Cold-start with diesel, oil and coolant; idle near 850 RPM and release the starter.
- Confirm pure petrol and an empty tank do not sustain combustion; ignition-off stops it.
- Pull through 1,700–3,400 RPM; verify torque tails off near the 4,500 RPM governor.
- Check automatic gear selection and manual clutch operation; verify stock engines remain unaffected.
- Check deeper diesel idle/load audio without runaway pitch; remove the engine and check silence.
- Drive long enough to observe fuel/oil consumption and normal temperature behaviour.
- Save/reload, verify normal size and correct fuel compatibility; confirm other engines remain stock.
