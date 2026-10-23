using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TLDLoader;
using UnityEngine;

namespace Car04RaceRestomod
{
    public sealed class Car04RaceRestomod : Mod
    {
        private const string ModId = "arena.car04-race-restomod";
        private static Harmony _harmony;

        public override string ID { get { return ModId; } }
        public override string Name { get { return "Car 04 Race Restomod"; } }
        public override string Author { get { return "Arena TLD Mod Factory"; } }
        public override string Version { get { return "1.0.0"; } }
        public override bool LoadInDB { get { return true; } }

        [Obsolete("Required by TLDLoader for database prefab registration.")]
        public override void dbLoad()
        {
            GameObject donor = Car04Donor.FindPrefab();
            if (donor == null)
            {
                Debug.LogError("[Car 04 Race Restomod] itemdatabase.d.gcar04 is missing or has no carscript. No donor was modified.");
                return;
            }

            donor.name = "Car 04 Race Restomod";
            if (donor.GetComponent<Car04RaceRestomodInitializer>() == null)
            {
                donor.AddComponent<Car04RaceRestomodInitializer>();
            }

            AddDonorToItemDatabase(donor);
            Debug.Log("[Car 04 Race Restomod] Registered the car04 race donor: " + donor.name);
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

        public override void OnGUI()
        {
            if (mainscript.M == null || mainscript.M.player == null)
            {
                return;
            }

            carscript car = mainscript.M.player.Car;
            if (car == null || car.GetComponent<Car04RaceRestomodInitializer>() == null)
            {
                return;
            }

            Car04LiveHud.Draw(car);
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

    internal static class Car04Donor
    {
        internal static GameObject FindPrefab()
        {
            itemdatabase database = itemdatabase.d;
            if (database == null)
            {
                return null;
            }

            GameObject candidate = database.gcar04;
            return candidate != null && candidate.GetComponent<carscript>() != null ? candidate : null;
        }
    }

    public sealed class Car04RaceRestomodInitializer : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<carscript>() == null)
            {
                return;
            }

            ApplyRaceLivery();

            enginescript[] engines = GetComponentsInChildren<enginescript>(true);
            for (int i = 0; i < engines.Length; i++)
            {
                enginescript engine = engines[i];
                if (engine == null)
                {
                    continue;
                }

                Car04RaceRestomodEngine core = engine.GetComponent<Car04RaceRestomodEngine>();
                if (core == null)
                {
                    core = engine.gameObject.AddComponent<Car04RaceRestomodEngine>();
                }

                core.Initialize();
            }
        }

        private void ApplyRaceLivery()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer targetRenderer = renderers[rendererIndex];
                if (targetRenderer == null)
                {
                    continue;
                }

                string objectName = targetRenderer.gameObject.name.ToLowerInvariant();
                Material[] materials = targetRenderer.materials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null || !material.HasProperty("_Color"))
                    {
                        continue;
                    }

                    string materialName = material.name.ToLowerInvariant();
                    string combinedName = objectName + " " + materialName;
                    if (ContainsAny(combinedName, ExcludedLiveryParts))
                    {
                        continue;
                    }

                    bool accentPart = ContainsAny(combinedName, AccentLiveryParts);
                    if (material.color.a < 0.9f)
                    {
                        continue;
                    }

                    material.color = accentPart ? RaceAccent : RaceBlue;
                    if (accentPart && material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", RaceAccent * 0.12f);
                    }
                }
            }
        }

        private static bool ContainsAny(string value, string[] terms)
        {
            for (int i = 0; i < terms.Length; i++)
            {
                if (value.Contains(terms[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly Color RaceBlue = new Color(0.025f, 0.34f, 0.98f, 1f);
        private static readonly Color RaceAccent = new Color(1f, 0.19f, 0.025f, 1f);
        private static readonly string[] ExcludedLiveryParts = new string[]
        {
            "glass", "window", "windshield", "windscreen", "tire", "tyre", "wheel", "rim",
            "seat", "interior", "dashboard", "steering", "light", "lamp", "mirror", "gauge",
            "engine", "exhaust", "chrome", "license", "licence", "plate", "rubber"
        };
        private static readonly string[] AccentLiveryParts = new string[]
        {
            "stripe", "spoiler", "bumper", "grille", "trim", "number", "raceaccent"
        };
    }

    public sealed class Car04RaceRestomodEngine : MonoBehaviour
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

            Car04EngineRuntime.Register(engine);
            Car04EngineRuntime.ApplySleeperTune(engine);

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
                Car04EngineRuntime.Unregister(engine);
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

    internal static class Car04EngineRuntime
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
            _subBassClip = CreateTone("Car 04 Race Restomod Sub Bass", sampleRate, sampleCount, 43f, 0.65f, false);
            _growlClip = CreateTone("Car 04 Race Restomod Growl", sampleRate, sampleCount, 68f, 0.60f, false);
            _turboClip = CreateTone("Car 04 Race Restomod Turbo", sampleRate, sampleCount, 190f, 0.28f, true);
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

    internal static class Car04LiveHud
    {
        private static readonly object[] GuiArguments = new object[2];
        private static MethodInfo _boxMethod;
        private static bool _lookupComplete;

        internal static void Draw(carscript car)
        {
            if (!_lookupComplete)
            {
                FindBoxMethod();
            }

            if (_boxMethod == null || car == null)
            {
                return;
            }

            enginescript engine = car.Engine;
            float rpm = engine == null ? 0f : Mathf.Max(engine.rpm, 0f);
            float maxRpm = engine == null ? 0f : Mathf.Max(engine.maxRpm, 0f);
            float fuel = GetTankPercent(car.Tank);
            float oil = engine == null ? 0f : GetTankPercent(engine.T);
            float speed = Mathf.Max(car.speed, 0f);
            string gear = car.gear.ToString();
            string content = "CAR 04  /  RACE TELEMETRY\n" +
                "SPEED  " + speed.ToString("0") + " km/h     GEAR  " + gear + "\n" +
                "RPM    " + rpm.ToString("0") + " / " + maxRpm.ToString("0") + "\n" +
                "BOOST  " + MakeBar(maxRpm > 0f ? rpm / maxRpm : 0f) + "\n" +
                "FUEL   " + MakeBar(fuel) + "  " + (fuel * 100f).ToString("0") + "%\n" +
                "OIL    " + MakeBar(oil) + "  " + (oil * 100f).ToString("0") + "%";

            GuiArguments[0] = new Rect(Screen.width - 310f, Screen.height - 188f, 290f, 160f);
            GuiArguments[1] = content;
            _boxMethod.Invoke(null, GuiArguments);
        }

        private static float GetTankPercent(tankscript tank)
        {
            if (tank == null || tank.F == null || tank.F.maxC <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(tank.F.GetAmount() / tank.F.maxC);
        }

        private static string MakeBar(float value)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 10f);
            return "[" + new string('#', filled) + new string('-', 10 - filled) + "]";
        }

        private static void FindBoxMethod()
        {
            _lookupComplete = true;
            Type guiType = Type.GetType("UnityEngine.GUI, UnityEngine.IMGUIModule", false) ??
                           Type.GetType("UnityEngine.GUI, UnityEngine", false);
            if (guiType == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length && guiType == null; i++)
                {
                    guiType = assemblies[i].GetType("UnityEngine.GUI", false);
                }
            }

            if (guiType != null)
            {
                _boxMethod = guiType.GetMethod(
                    "Box",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Rect), typeof(string) },
                    null);
            }
        }
    }

    [HarmonyPatch(typeof(enginescript), "Update")]
    internal static class EngineUpdateHarmonyPatch
    {
        private static void Prefix(enginescript __instance)
        {
            Car04EngineRuntime.BeforeUpdate(__instance);
        }

        private static void Postfix(enginescript __instance)
        {
            Car04EngineRuntime.AfterUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(enginescript), "Running")]
    internal static class EngineRunningHarmonyPatch
    {
        private static bool Prefix(enginescript __instance)
        {
            return Car04EngineRuntime.BeforeRunning(__instance);
        }
    }
}
