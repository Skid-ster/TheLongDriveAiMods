using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TLDLoader;
using UnityEngine;

namespace LadaRestomod
{
    public sealed class LadaRestomod : Mod
    {
        private const string ModId = "arena.lada-restomod";
        private static Harmony _harmony;

        public override string ID { get { return ModId; } }
        public override string Name { get { return "Lada Restomod"; } }
        public override string Author { get { return "Arena TLD Mod Factory"; } }
        public override string Version { get { return "1.0.0"; } }
        public override bool LoadInDB { get { return true; } }

        public override void dbLoad()
        {
            GameObject donor = LadaDonor.FindPrefab();
            if (donor == null)
            {
                Debug.LogError("[Lada Restomod] Could not find a Lada car prefab. No donor was modified.");
                return;
            }

            donor.name = "Lada Restomod";
            if (donor.GetComponent<LadaRestomodInitializer>() == null)
            {
                donor.AddComponent<LadaRestomodInitializer>();
            }

            AddDonorToItemDatabase(donor);
            Debug.Log("[Lada Restomod] Registered the Lada sleeper donor: " + donor.name);
        }

        public override void OnLoad()
        {
            if (_harmony != null)
            {
                return;
            }

            _harmony = new Harmony(ModId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void AddDonorToItemDatabase(GameObject donor)
        {
            if (itemdatabase.d == null)
            {
                return;
            }

            GameObject[] items = itemdatabase.d.items;
            if (items == null)
            {
                itemdatabase.d.items = new GameObject[] { donor };
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == donor)
                {
                    return;
                }
            }

            GameObject[] expanded = new GameObject[items.Length + 1];
            Array.Copy(items, expanded, items.Length);
            expanded[expanded.Length - 1] = donor;
            itemdatabase.d.items = expanded;
        }
    }

    internal static class LadaDonor
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static GameObject FindPrefab()
        {
            itemdatabase database = itemdatabase.d;
            if (database == null)
            {
                return null;
            }

            GameObject[] items = database.items;
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    GameObject candidate = items[i];
                    if (IsCar(candidate) && candidate.name.IndexOf("lada", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return candidate;
                    }
                }
            }

            FieldInfo[] fields = typeof(itemdatabase).GetFields(InstanceFields);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != typeof(GameObject))
                {
                    continue;
                }

                GameObject candidate = fields[i].GetValue(database) as GameObject;
                if (IsCar(candidate) &&
                    (fields[i].Name.IndexOf("lada", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     candidate.name.IndexOf("lada", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return candidate;
                }
            }

            GameObject fallback = GetCarField(database, "gcar06");
            if (fallback != null)
            {
                return fallback;
            }

            fallback = GetCarField(database, "gcar05");
            if (fallback != null)
            {
                return fallback;
            }

            return null;
        }

        private static GameObject GetCarField(itemdatabase database, string fieldName)
        {
            FieldInfo field = typeof(itemdatabase).GetField(fieldName, InstanceFields);
            if (field == null || field.FieldType != typeof(GameObject))
            {
                return null;
            }

            GameObject candidate = field.GetValue(database) as GameObject;
            return IsCar(candidate) ? candidate : null;
        }

        private static bool IsCar(GameObject candidate)
        {
            return candidate != null && candidate.GetComponent<carscript>() != null;
        }
    }

    public sealed class LadaRestomodInitializer : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<carscript>() == null)
            {
                return;
            }

            enginescript[] engines = GetComponentsInChildren<enginescript>(true);
            for (int i = 0; i < engines.Length; i++)
            {
                enginescript engine = engines[i];
                if (engine == null)
                {
                    continue;
                }

                LadaRestomodEngine core = engine.GetComponent<LadaRestomodEngine>();
                if (core == null)
                {
                    core = engine.gameObject.AddComponent<LadaRestomodEngine>();
                }

                core.Initialize();
            }
        }
    }

    public sealed class LadaRestomodEngine : MonoBehaviour
    {
        private bool _initialized;
        private RestomodAudioLayer _audioLayer;

        internal void Initialize()
        {
            enginescript engine = GetComponent<enginescript>();
            if (engine == null)
            {
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
                ApplyAwakeScale();
            }

            LadaEngineRuntime.Register(engine);
            LadaEngineRuntime.ApplySleeperTune(engine);

            _audioLayer = GetComponent<RestomodAudioLayer>();
            if (_audioLayer == null)
            {
                _audioLayer = gameObject.AddComponent<RestomodAudioLayer>();
            }
            _audioLayer.Initialize(engine);
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            enginescript engine = GetComponent<enginescript>();
            if (engine != null)
            {
                LadaEngineRuntime.Unregister(engine);
            }
        }

        private void ApplyAwakeScale()
        {
            Vector3 originalScale = transform.localScale;
            float largestAxis = Mathf.Max(Mathf.Abs(originalScale.x), Mathf.Abs(originalScale.y), Mathf.Abs(originalScale.z));
            float capMultiplier = largestAxis > 0f ? 1.8f / largestAxis : 1.8f;
            float randomMultiplier = UnityEngine.Random.Range(1.00f, 1.08f);
            float multiplier = Mathf.Min(randomMultiplier, capMultiplier);
            transform.localScale = originalScale * multiplier;
        }
    }

    internal static class EngineFieldCache
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static readonly FieldInfo Rpm = typeof(enginescript).GetField("rpm", InstanceFields);
        internal static readonly FieldInfo Start = typeof(enginescript).GetField("start", InstanceFields);
        internal static readonly FieldInfo Running = typeof(enginescript).GetField("running", InstanceFields);
        internal static readonly FieldInfo CanRun = typeof(enginescript).GetField("canRun", InstanceFields);
        internal static readonly FieldInfo Ignition = typeof(enginescript).GetField("ignition", InstanceFields);
        internal static readonly FieldInfo SMotor = typeof(enginescript).GetField("SMotor", InstanceFields);
        internal static readonly FieldInfo Modifier = typeof(enginescript).GetField("modifier", InstanceFields);
        internal static readonly FieldInfo Drpm = typeof(enginescript).GetField("drpm", InstanceFields);
        internal static readonly FieldInfo MaxRpm = typeof(enginescript).GetField("maxRpm", InstanceFields);
    }

    internal static class LadaEngineRuntime
    {
        private static readonly HashSet<enginescript> ManagedEngines = new HashSet<enginescript>();
        private static readonly Dictionary<enginescript, float> NextFluidRefresh = new Dictionary<enginescript, float>();
        private const float FluidRefreshInterval = 0.5f;
        private const float SleeperModifier = 4.0f;
        private const float RevHeadroomMultiplier = 1.20f;
        private const float VanillaPitchMultiplier = 1.10f;

        internal static void Register(enginescript engine)
        {
            if (engine != null)
            {
                ManagedEngines.Add(engine);
            }
        }

        internal static void Unregister(enginescript engine)
        {
            if (engine != null)
            {
                ManagedEngines.Remove(engine);
                NextFluidRefresh.Remove(engine);
            }
        }

        internal static bool IsManaged(enginescript engine)
        {
            return engine != null && ManagedEngines.Contains(engine);
        }

        internal static void ApplySleeperTune(enginescript engine)
        {
            if (engine == null)
            {
                return;
            }

            SetFloat(EngineFieldCache.Modifier, engine, SleeperModifier, true);
            float maxRpm = ReadFloat(EngineFieldCache.MaxRpm, engine, 0f);
            if (maxRpm > 0f)
            {
                float drpm = ReadFloat(EngineFieldCache.Drpm, engine, 0f);
                SetFloat(EngineFieldCache.Drpm, engine, Mathf.Max(drpm, maxRpm * RevHeadroomMultiplier), false);
            }
        }

        internal static void BeforeUpdate(enginescript engine)
        {
            if (!IsManaged(engine))
            {
                return;
            }

            carscript installedCar = engine.GetComponentInParent<carscript>();
            if (installedCar == null)
            {
                StopAndSilence(engine);
                return;
            }

            float nextRefresh;
            if (!NextFluidRefresh.TryGetValue(engine, out nextRefresh) || Time.time >= nextRefresh)
            {
                NextFluidRefresh[engine] = Time.time + FluidRefreshInterval;
                RefreshFluids(engine, installedCar);
            }
        }

        private static void RefreshFluids(enginescript engine, carscript installedCar)
        {
            engine.Oilfluid = mainscript.fluidenum.oil;
            FillTank(engine.T, mainscript.fluidenum.oil);
            FillTank(installedCar.Tank, mainscript.fluidenum.gas);
        }

        private static void FillTank(tankscript tank, mainscript.fluidenum fluid)
        {
            if (tank == null || tank.F == null)
            {
                return;
            }

            float missingAmount = tank.F.maxC - tank.F.GetAmount();
            if (missingAmount > 0f)
            {
                tank.F.ChangeOne(missingAmount, fluid);
            }
        }

        internal static void AfterUpdate(enginescript engine)
        {
            if (!IsManaged(engine))
            {
                return;
            }

            carscript installedCar = engine.GetComponentInParent<carscript>();
            if (installedCar == null)
            {
                StopAndSilence(engine);
                return;
            }

            ForceInstalledState(engine);
            float rpm = ReadFloat(EngineFieldCache.Rpm, engine, 0f);
            if (rpm < 10f)
            {
                SetFloat(EngineFieldCache.Rpm, engine, 10f, false);
            }

            float maxRpm = ReadFloat(EngineFieldCache.MaxRpm, engine, 1f);
            float rpmRatio = maxRpm > 1f ? Mathf.Clamp01(rpm / maxRpm) : 0f;
            AudioSource vanillaMotor = GetVanillaMotor(engine);
            if (vanillaMotor != null)
            {
                vanillaMotor.mute = false;
                vanillaMotor.pitch = VanillaPitchMultiplier * (0.85f + rpmRatio * 0.35f);
            }

            RestomodAudioLayer layer = engine.GetComponent<RestomodAudioLayer>();
            if (layer != null)
            {
                layer.SetEngineState(true, rpmRatio);
            }
        }

        internal static bool BeforeRunning(enginescript engine)
        {
            if (!IsManaged(engine))
            {
                return true;
            }

            if (engine.GetComponentInParent<carscript>() == null)
            {
                StopAndSilence(engine);
                return false;
            }

            ForceInstalledState(engine);
            return false;
        }

        private static void ForceInstalledState(enginescript engine)
        {
            SetBool(EngineFieldCache.Start, engine, false);
            SetBool(EngineFieldCache.Running, engine, true);
            SetBool(EngineFieldCache.CanRun, engine, true);
            SetBool(EngineFieldCache.Ignition, engine, true);
        }

        private static void StopAndSilence(enginescript engine)
        {
            SetBool(EngineFieldCache.Start, engine, false);
            SetBool(EngineFieldCache.Running, engine, false);
            SetBool(EngineFieldCache.Ignition, engine, false);

            AudioSource vanillaMotor = GetVanillaMotor(engine);
            if (vanillaMotor != null)
            {
                vanillaMotor.mute = true;
            }

            RestomodAudioLayer layer = engine.GetComponent<RestomodAudioLayer>();
            if (layer != null)
            {
                layer.SetEngineState(false, 0f);
            }
        }

        private static AudioSource GetVanillaMotor(enginescript engine)
        {
            return EngineFieldCache.SMotor == null ? null : EngineFieldCache.SMotor.GetValue(engine) as AudioSource;
        }

        private static float ReadFloat(FieldInfo field, object instance, float fallback)
        {
            if (field == null || instance == null)
            {
                return fallback;
            }

            object value = field.GetValue(instance);
            return value is float ? (float)value : fallback;
        }

        private static void SetFloat(FieldInfo field, object instance, float value, bool onlyIncrease)
        {
            if (field == null || instance == null)
            {
                return;
            }

            float assignedValue = onlyIncrease ? Mathf.Max(ReadFloat(field, instance, value), value) : value;
            field.SetValue(instance, assignedValue);
        }

        private static void SetBool(FieldInfo field, object instance, bool value)
        {
            if (field != null && instance != null)
            {
                field.SetValue(instance, value);
            }
        }
    }

    internal sealed class RestomodAudioLayer : MonoBehaviour
    {
        private static AudioClip _subBassClip;
        private static AudioClip _growlClip;
        private static AudioClip _turboClip;

        private AudioSource _subBass;
        private AudioSource _growl;
        private AudioSource _turbo;
        private bool _initialized;

        internal void Initialize(enginescript engine)
        {
            if (_initialized || engine == null)
            {
                return;
            }

            _initialized = true;
            EnsureClips();
            _subBass = CreateLayer(_subBassClip, 0.15f);
            _growl = CreateLayer(_growlClip, 0.15f);
            _turbo = CreateLayer(_turboClip, 0.20f);
            SetEngineState(false, 0f);
        }

        internal void SetEngineState(bool running, float rpmRatio)
        {
            float ratio = Mathf.Clamp01(rpmRatio);
            SetLayer(_subBass, running, 0.15f, 0.70f + ratio * 0.55f);
            SetLayer(_growl, running, 0.15f, 0.65f + ratio * 0.75f);
            SetLayer(_turbo, running, 0.20f, 0.90f + ratio * 1.10f);
        }

        private AudioSource CreateLayer(AudioClip clip, float volume)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.volume = volume;
            source.pitch = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 4f;
            source.maxDistance = 55f;
            source.Play();
            return source;
        }

        private static void SetLayer(AudioSource source, bool running, float volume, float pitch)
        {
            if (source == null)
            {
                return;
            }

            source.volume = running ? volume : 0f;
            source.pitch = pitch;
        }

        private static void EnsureClips()
        {
            if (_subBassClip != null)
            {
                return;
            }

            const int sampleRate = 22050;
            const int sampleCount = sampleRate * 2;
            _subBassClip = CreateTone("Lada Restomod Sub Bass", sampleRate, sampleCount, 43f, 0.65f, false);
            _growlClip = CreateTone("Lada Restomod Growl", sampleRate, sampleCount, 68f, 0.60f, false);
            _turboClip = CreateTone("Lada Restomod Turbo", sampleRate, sampleCount, 190f, 0.28f, true);
        }

        private static AudioClip CreateTone(string clipName, int sampleRate, int sampleCount, float baseFrequency, float amplitude, bool airy)
        {
            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                float phase = time * baseFrequency * Mathf.PI * 2f;
                float value = Mathf.Sin(phase) * 0.58f + Mathf.Sin(phase * 2f) * 0.24f + Mathf.Sin(phase * 3f) * 0.12f;
                if (airy)
                {
                    value = value * 0.55f + Mathf.Sin(time * 790f * Mathf.PI * 2f) * 0.12f;
                }
                samples[i] = Mathf.Clamp(value * amplitude, -1f, 1f);
            }

            clip.SetData(samples, 0);
            return clip;
        }
    }

    [HarmonyPatch(typeof(enginescript), "Update")]
    internal static class EngineUpdateHarmonyPatch
    {
        private static void Prefix(enginescript __instance)
        {
            LadaEngineRuntime.BeforeUpdate(__instance);
        }

        private static void Postfix(enginescript __instance)
        {
            LadaEngineRuntime.AfterUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(enginescript), "Running")]
    internal static class EngineRunningHarmonyPatch
    {
        private static bool Prefix(enginescript __instance)
        {
            return LadaEngineRuntime.BeforeRunning(__instance);
        }
    }
}
