using System;
using HarmonyLib;
using TLDLoader;
using UnityEngine;

namespace Car06Diesel
{
    public sealed class CustomEngineMod : Mod
    {
        private Harmony patches;
        public override string ID => "skidster.car06diesel";
        public override string Name => "Car06 1.9 Turbodiesel Conversion";
        public override string Author => "Skid-ster";
        public override string Version => "2.0.0";
        public override bool LoadInDB => true;

        [Obsolete("Legacy TLDLoader hook; use DbLoad instead.")]
        public override void dbLoad() { RegisterDonor(); }
        public override void DbLoad() { RegisterDonor(); }
        public override void OnLoad()
        {
            if (patches == null)
            {
                patches = new Harmony(ID);
                patches.PatchAll(typeof(CustomEngineMod).Assembly);
            }
            RegisterDonor();
        }

        private static void RegisterDonor()
        {
            var db = itemdatabase.d;
            if (db == null) return;
            var donor = db.gcar06enginebenzin;
            if (donor == null || donor.GetComponent<carscript>() != null ||
                donor.GetComponent<enginescript>() == null)
            {
                Debug.LogError("[Car06Diesel] Car06 petrol engine donor missing; no replacement made.");
                return;
            }
            // Hijack in place: all item-array references and the vanilla save ID stay intact.
            donor.name = "Car06 1.9 Turbodiesel";
            if (donor.GetComponent<EngineInitializer>() == null)
                donor.AddComponent<EngineInitializer>();
        }
    }

    public sealed class EngineInitializer : MonoBehaviour
    {
        internal const float IdleRpm = 850f;
        internal const float RedlineRpm = 4500f;
        internal enginescript Engine { get; private set; }
        internal bool Installed => Engine != null && Engine.GetComponentInParent<carscript>() != null;

        private void Awake()
        {
            Engine = GetComponent<enginescript>();
            if (Engine == null) { enabled = false; return; }
            var db = itemdatabase.d;
            var dieselPrefab = db == null ? null : db.gcar06enginediesel;
            var diesel = dieselPrefab == null ? null : dieselPrefab.GetComponent<enginescript>();

            Engine.alapRpm = IdleRpm;
            Engine.maxRpm = RedlineRpm;
            Engine.shutdownRpm = 450f;
            Engine.starterRpm = 650f;
            Engine.autoShiftClutchRpm = 1200f;
            Engine.autoShiftClutchRpmRange = 300f;
            Engine.maxNm = 180f;
            // The game evaluates this curve directly in RPM -> Nm, NOT normalized values.
            Engine.torqueCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(450f, 35f),
                new Keyframe(850f, 75f), new Keyframe(1200f, 115f),
                new Keyframe(1700f, 165f), new Keyframe(2200f, 180f),
                new Keyframe(2800f, 175f), new Keyframe(3400f, 165f),
                new Keyframe(3800f, 155f), new Keyframe(4200f, 125f),
                new Keyframe(4500f, 0f), new Keyframe(5000f, 0f));
            // Smooth rev response and stronger overrun, without ever writing the live RPM.
            Engine.rpmChangeModifier = diesel != null ? diesel.rpmChangeModifier : 0.75f;
            Engine.motorBrakeModifier = diesel != null ? diesel.motorBrakeModifier : 0.25f;
            Engine.twostroke = false;
            Engine.Oilfluid = mainscript.fluidenum.oil;
            Engine.noOverHeat = false;
            Engine.maxEngineVibrate = 0f;
            Engine.maxEngineRotate = 0f;
            ConfigureFuel(diesel);
            ConfigureAudio(diesel);
        }

        private void ConfigureFuel(enginescript diesel)
        {
            // Copy consumption calibration, never alias a shared prefab's mutable fluid list.
            var source = diesel != null ? diesel.FuelConsumption : Engine.FuelConsumption;
            float amount = source == null ? 1f : Mathf.Max(0.0001f, source.GetAmount());
            Engine.FuelConsumption = new mainscript.fluidcontainer();
            Engine.FuelConsumption.maxC = source == null ? amount : source.maxC;
            Engine.FuelConsumption.fluids.Add(new mainscript.fluid(mainscript.fluidenum.diesel, amount));
            Engine.FuelConsumption.GetAmount();
            if (diesel != null) Engine.consumptionM = diesel.consumptionM;
            // Deliberately do NOT replace or refill any real oil/fuel tank contents.
        }

        private void ConfigureAudio(enginescript diesel)
        {
            // Use genuine vanilla diesel loops when available, retaining the existing sources.
            if (diesel != null)
            {
                if (Engine.SMotor != null && diesel.SMotor != null && diesel.SMotor.clip != null)
                    Engine.SMotor.clip = diesel.SMotor.clip;
                if (Engine.SAlap != null && diesel.SAlap != null && diesel.SAlap.clip != null)
                    Engine.SAlap.clip = diesel.SAlap.clip;
                if (diesel.SStart != null) Engine.SStart = diesel.SStart;
                if (diesel.SStop != null) Engine.SStop = diesel.SStop;
            }
            // Vanilla Update interpolates these once per frame: no cumulative pitch scaling.
            Engine.minMotorPitch = 0.7f;
            Engine.maxMotorPitch = 1.3f;
            Engine.minAlapPitch = 0.65f;
            Engine.maxAlapPitch = 1.05f;
            Engine.SMinAlapRpm = 650f;
            Engine.SMaxAlapRpm = 1600f;
            Engine.SMinMotorRpm = 900f;
            Engine.SMaxMotorRpm = 3800f;
        }
    }

    // Release a held starter ONLY after combustion has actually caught. Never bypass fuel,
    // ignition, oil, temperature or vanilla Running()'s recent-combustion test.
    [HarmonyPatch(typeof(enginescript), "inputs")]
    internal static class EngineInputsPatch
    {
        private static void Postfix(enginescript __instance)
        {
            var conversion = __instance.GetComponent<EngineInitializer>();
            if (conversion == null) return;
            if (!conversion.Installed)
            {
                __instance.start = false;
                __instance.ignition = false;
                __instance.running = false;
                __instance.canRun = false;
                return;
            }
            if (__instance.running && __instance.rpm >= EngineInitializer.IdleRpm &&
                __instance.canRun && __instance.ignition)
                __instance.start = false;
        }
    }

    // Vanilla Update briefly remembers the last combustion event. Prevent that grace period
    // from keeping a removed engine running, but leave installed engines fully vanilla.
    [HarmonyPatch(typeof(enginescript), "Running")]
    internal static class EngineRunningPatch
    {
        private static bool Prefix(enginescript __instance, ref bool __result)
        {
            var conversion = __instance.GetComponent<EngineInitializer>();
            if (conversion == null || conversion.Installed) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(enginescript), "Update")]
    internal static class EngineUpdatePatch
    {
        private static void Postfix(enginescript __instance)
        {
            var conversion = __instance.GetComponent<EngineInitializer>();
            if (conversion == null || conversion.Installed) return;
            __instance.start = false;
            __instance.running = false;
            __instance.canRun = false;
            __instance.ignition = false;
            if (__instance.SMotor != null) __instance.SMotor.Stop();
            if (__instance.SAlap != null) __instance.SAlap.Stop();
        }
    }

    [HarmonyPatch(typeof(carscript), "AutoShift")]
    internal static class DieselAutoShiftPatch
    {
        internal struct ShiftSettings
        {
            internal bool Applied;
            internal float Min;
            internal float Max;
        }

        private static void Prefix(carscript __instance, out ShiftSettings __state)
        {
            __state = new ShiftSettings();
            if (__instance.Engine == null || __instance.Engine.GetComponent<EngineInitializer>() == null) return;
            __state.Applied = true;
            __state.Min = __instance.autoShiftMinRpm;
            __state.Max = __instance.autoShiftMaxRpm;
            __instance.autoShiftMinRpm = 1400f;
            __instance.autoShiftMaxRpm = 3800f;
        }

        private static void Postfix(carscript __instance, ShiftSettings __state)
        {
            if (!__state.Applied) return;
            // Scope the change to this shift decision; swapping engines restores stock behaviour.
            __instance.autoShiftMinRpm = __state.Min;
            __instance.autoShiftMaxRpm = __state.Max;
        }
    }

}
