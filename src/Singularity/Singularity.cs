using System;
using System.Reflection;
using HarmonyLib;
using TLDLoader;
using UnityEngine;

namespace Singularity
{
    public sealed class CustomEngineMod : Mod
    {
        private Harmony patches;
        public override string ID => "skidster.singularity";
        public override string Name => "Car06 Singularity Reactor";
        public override string Author => "Skid-ster";
        public override string Version => "1.0.0";
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
                Debug.LogError("[Singularity] Car06 petrol engine donor unavailable; no replacement made.");
                return;
            }
            donor.name = "Car06 Singularity Reactor";
            if (donor.GetComponent<EngineInitializer>() == null)
                donor.AddComponent<EngineInitializer>();
            // Keep the original save/spawn ID, never append a duplicate item.
            if (db.items == null) return;
            for (int i = 0; i < db.items.Length; i++)
                if (db.items[i] == donor) db.items[i] = donor;
        }
    }

    internal static class EngineFields
    {
        internal static readonly FieldInfo Rpm = AccessTools.Field(typeof(enginescript), "rpm");
        internal static readonly FieldInfo Start = AccessTools.Field(typeof(enginescript), "start");
        internal static readonly FieldInfo Running = AccessTools.Field(typeof(enginescript), "running");
        internal static readonly FieldInfo CanRun = AccessTools.Field(typeof(enginescript), "canRun");
        internal static readonly FieldInfo Ignition = AccessTools.Field(typeof(enginescript), "ignition");
        internal static readonly FieldInfo SMotor = AccessTools.Field(typeof(enginescript), "SMotor");

        internal static bool Apply(enginescript engine)
        {
            bool installed = engine.GetComponentInParent<carscript>() != null;
            Start.SetValue(engine, false);
            Running.SetValue(engine, installed);
            CanRun.SetValue(engine, installed);
            Ignition.SetValue(engine, installed);
            return installed;
        }
    }

    [HarmonyPatch(typeof(enginescript), "Update")]
    internal static class EngineUpdatePrefixPatch
    {
        private static void Prefix(enginescript __instance)
        {
            var reactor = __instance.GetComponent<EngineInitializer>();
            if (reactor == null) return;
            if (!EngineFields.Apply(__instance)) return;
            reactor.FillFluids();
        }
    }

    [HarmonyPatch(typeof(enginescript), "Update")]
    internal static class EngineUpdatePostfixPatch
    {
        private static void Postfix(enginescript __instance)
        {
            if (__instance.GetComponent<EngineInitializer>() != null) EngineFields.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(enginescript), "Running")]
    internal static class EngineRunningPatch
    {
        private static bool Prefix(enginescript __instance, ref bool __result)
        {
            if (__instance.GetComponent<EngineInitializer>() == null) return true;
            __result = EngineFields.Apply(__instance);
            return false;
        }
    }

    public sealed class EngineInitializer : MonoBehaviour
    {
        // Public fields are copied when Unity clones the donor prefab.
        public bool Prepared;
        public Vector3 OriginalScale;
        public float OriginalTorque;
        public float Size;
        private enginescript engine;
        private float nextFill;
        internal float Charge { get; private set; }
        internal bool Installed => engine != null && engine.GetComponentInParent<carscript>() != null;

        private void Awake()
        {
            engine = GetComponent<enginescript>();
            if (engine == null) { enabled = false; return; }
            if (!Prepared)
            {
                OriginalScale = transform.localScale;
                OriginalTorque = engine.maxNm;
                Prepared = true;
            }
            Size = UnityEngine.Random.Range(0.55f, 1.8f);
            transform.localScale = OriginalScale * Size;
            engine.maxNm = OriginalTorque * Mathf.Lerp(2.5f, 5f, Mathf.InverseLerp(0.55f, 1.8f, Size));
            engine.noOverHeat = true;
            engine.maxEngineVibrate = 0f;
            engine.maxEngineRotate = 0f;
            if (GetComponent<EngineVisualStyler>() == null) gameObject.AddComponent<EngineVisualStyler>();
            if (GetComponent<EngineAudioController>() == null) gameObject.AddComponent<EngineAudioController>();
        }

        private void Update()
        {
            // Spool the reactor with throttle, without writing rpm or touching bolt transforms.
            float target = Installed ? Mathf.Clamp01(engine.gas) : 0f;
            Charge = Mathf.MoveTowards(Charge, target, Time.deltaTime * 0.35f);
            engine.maxNm = OriginalTorque * Mathf.Lerp(2.5f, 5f, Mathf.InverseLerp(0.55f, 1.8f, Size)) *
                (1f + Charge * 0.5f);
        }

        internal void FillFluids()
        {
            if (Time.time < nextFill) return;
            nextFill = Time.time + 0.5f;
            Fill(engine.T, mainscript.fluidenum.oil);
            var car = engine.GetComponentInParent<carscript>();
            if (car != null) Fill(car.Tank, mainscript.fluidenum.gas);
        }

        private static void Fill(tankscript tank, mainscript.fluidenum fluid)
        {
            if (tank == null || tank.F == null || tank.F.maxC <= 0f) return;
            tank.F.fluids.Clear();
            tank.F.fluids.Add(new mainscript.fluid(fluid, tank.F.maxC));
            tank.F.GetAmount();
        }
    }

    public sealed class EngineVisualStyler : MonoBehaviour
    {
        private Renderer[] surfaces;
        private MaterialPropertyBlock block;
        private EngineInitializer reactor;
        private void Awake()
        {
            reactor = GetComponent<EngineInitializer>();
            surfaces = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            // Clone per-renderer materials; never mutate the shared vanilla donor materials.
            foreach (var surface in surfaces)
                foreach (var material in surface.materials)
                    if (material.HasProperty("_EmissionColor")) material.EnableKeyword("_EMISSION");
        }
        private void Update()
        {
            float pulse = reactor.Installed ? 0.5f + reactor.Charge * 2f + 0.25f * Mathf.Sin(Time.time * 11f) : 0f;
            foreach (var surface in surfaces)
            {
                if (surface == null) continue;
                surface.GetPropertyBlock(block);
                block.SetColor("_Color", new Color(0.08f, 0.13f, 0.17f));
                block.SetFloat("_Metallic", 0.95f);
                block.SetFloat("_Glossiness", 0.85f);
                block.SetColor("_EmissionColor", Color.Lerp(Color.cyan, new Color(1f, 0.1f, 0.65f), reactor.Charge) * pulse);
                surface.SetPropertyBlock(block);
            }
        }
        private void OnDestroy()
        {
            if (surfaces == null) return;
            foreach (var surface in surfaces)
                if (surface != null)
                    foreach (var material in surface.materials) Destroy(material);
        }
    }

    public sealed class EngineAudioController : MonoBehaviour
    {
        private EngineInitializer reactor;
        private enginescript engine;
        private AudioSource layer;
        private AudioClip clip;
        private float lastVanillaPitch = 1f;
        private float appliedPitch = -1f;
        private void Awake()
        {
            reactor = GetComponent<EngineInitializer>();
            engine = GetComponent<enginescript>();
            const int rate = 22050;
            var samples = new float[rate];
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / rate;
                samples[i] = (float)(0.5 * Math.Sin(2 * Math.PI * 45 * t) +
                    0.25 * Math.Sin(2 * Math.PI * 90 * t) +
                    0.12 * Math.Sin(2 * Math.PI * 720 * t) * (0.5 + 0.5 * Math.Sin(2 * Math.PI * 12 * t)));
            }
            clip = AudioClip.Create("Singularity growl and turbine", rate, 1, rate, false);
            clip.SetData(samples, 0);
            layer = gameObject.AddComponent<AudioSource>();
            layer.clip = clip;
            layer.loop = true;
            layer.playOnAwake = false;
            layer.spatialBlend = 1f;
            layer.minDistance = 2f;
            layer.maxDistance = 45f;
        }
        private void LateUpdate()
        {
            var motor = (AudioSource)EngineFields.SMotor.GetValue(engine);
            if (!reactor.Installed)
            {
                layer.Stop();
                if (motor != null) motor.Stop();
                if (engine.SAlap != null) engine.SAlap.Stop();
                return;
            }
            float sizePitch = Mathf.Lerp(2.4f, 0.55f, Mathf.InverseLerp(0.55f, 1.8f, reactor.Size));
            if (motor != null)
            {
                motor.enabled = true;
                // Do not multiply yesterday's already-scaled pitch if vanilla skipped its audio update.
                if (!Mathf.Approximately(motor.pitch, appliedPitch)) lastVanillaPitch = motor.pitch;
                appliedPitch = Mathf.Clamp(lastVanillaPitch * sizePitch, 0.1f, 3f);
                motor.pitch = appliedPitch;
                if (!motor.isPlaying && motor.clip != null) motor.Play();
                layer.volume = motor.volume * Mathf.Lerp(0.15f, 0.2f, reactor.Charge);
            }
            else layer.volume = 0f;
            float rpm = (float)EngineFields.Rpm.GetValue(engine);
            layer.pitch = Mathf.Clamp(sizePitch * (0.7f + rpm / Mathf.Max(1f, engine.maxRpm)), 0.25f, 3f);
            if (!layer.isPlaying) layer.Play();
        }
        private void OnDisable() { if (layer != null) layer.Stop(); }
        private void OnDestroy()
        {
            if (layer != null) Destroy(layer);
            if (clip != null) Destroy(clip);
        }
    }
}
