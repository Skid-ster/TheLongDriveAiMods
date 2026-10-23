using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TLDLoader;
using UnityEngine;

namespace Car05EngineVariants
{
    public enum EngineVariantKind
    {
        Gas,
        Diesel,
        RacingDiesel
    }

    public sealed class Car05EngineVariantsMod : Mod
    {
        private const int DieselItemId = 1;
        private const int RacingDieselItemId = 2;

        private static GameObject _originalDonor;
        private static GameObject _gasPrefab;
        private static GameObject _dieselPrefab;
        private static GameObject _racingDieselPrefab;
        private static GameObject _templateHolder;

        public override string ID => "Car05EngineVariants";
        public override string Name => "05 Engine Variants";
        public override string Author => "TLD Mod Factory";
        public override string Version => "1.0.0";
        public override bool LoadInDB => true;
        public override bool UseHarmony => false;

        [Obsolete("TLDLoader compatibility database-load entry point.", false)]
        public override void dbLoad()
        {
            InstallHarmonyPatches();

            itemdatabase database = itemdatabase.d;
            if (database == null)
            {
                Logger.LogWarning("The item database is not available; 05 engine variants were not registered.");
                return;
            }

            if (_gasPrefab != null)
            {
                RestoreGasItemSlot(database);
                EnsureRegisteredVariants();
                return;
            }

            GameObject donor = ItemDatabaseFields.GetCar05Engine(database);
            if (!IsValidEnginePrefab(donor))
            {
                Logger.LogError("gcar05engine is missing, is a whole-car prefab, or has no enginescript. The 05 variants were not installed.");
                return;
            }

            EngineVariantMarker existingMarker = donor.GetComponent<EngineVariantMarker>();
            if (existingMarker != null)
            {
                _originalDonor = donor;
                _gasPrefab = donor;
                RestoreGasItemSlot(database);
                EnsureRegisteredVariants();
                return;
            }

            _originalDonor = donor;

            // Clone before changing the donor so every variant starts with identical stock stats.
            _dieselPrefab = CloneTemplate(donor, EngineVariantSettings.DieselName);
            _racingDieselPrefab = CloneTemplate(donor, EngineVariantSettings.RacingDieselName);

            ConfigurePrefab(donor, EngineVariantKind.Gas, EngineVariantSettings.GasName);
            ConfigurePrefab(_dieselPrefab, EngineVariantKind.Diesel, EngineVariantSettings.DieselName);
            ConfigurePrefab(_racingDieselPrefab, EngineVariantKind.RacingDiesel, EngineVariantSettings.RacingDieselName);

            _gasPrefab = donor;

            // The gas variant is an in-place donor hijack. ItemInit can therefore rebuild
            // its array without discarding the gasoline variant.
            RestoreGasItemSlot(database);
            EnsureRegisteredVariants();
        }

        public override void OnLoad()
        {
            InstallHarmonyPatches();
            RestoreGasItemSlot(itemdatabase.d);
        }

        private void InstallHarmonyPatches()
        {
            EngineFields.Initialize();
            ItemDatabaseFields.Initialize();

            if (HarmonyInstance != null)
                return;

            HarmonyInstance = new Harmony("com." + ID.ToLowerInvariant());

            HarmonyInstance.Patch(
                EngineFields.UpdateMethod,
                new HarmonyMethod(typeof(EngineUpdatePrefixPatch), "Prefix", new[] { typeof(enginescript) }),
                new HarmonyMethod(typeof(EngineUpdatePostfixPatch), "Postfix", new[] { typeof(enginescript) }),
                null,
                null);

            HarmonyInstance.Patch(
                EngineFields.RunningMethod,
                new HarmonyMethod(typeof(EngineRunningPatch), "Prefix", new[] { typeof(enginescript) }),
                null,
                null,
                null);

            HarmonyInstance.Patch(
                ItemDatabaseFields.ItemInitMethod,
                null,
                new HarmonyMethod(typeof(ItemDatabaseRebuildPatch), "Postfix", new[] { typeof(itemdatabase) }),
                null,
                null);
        }

        internal static void RestoreGasItemSlot(itemdatabase database)
        {
            if (database == null || _gasPrefab == null)
                return;

            ItemDatabaseFields.SetCar05Engine(database, _gasPrefab);

            GameObject[] items = database.items;
            if (items == null)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                if (ReferenceEquals(items[i], _originalDonor) || ReferenceEquals(items[i], _gasPrefab))
                    items[i] = _gasPrefab;
            }
        }

        private void EnsureRegisteredVariants()
        {
            if (ModLoader.Database == null)
            {
                Logger.LogWarning("TLDLoader's item registry is not available for the diesel variants.");
                return;
            }

            if (_dieselPrefab != null && ModLoader.Database.GetItemById(Category, DieselItemId) == null)
                RegisterItem(_dieselPrefab, DieselItemId, EngineVariantSettings.DieselName).Register();

            if (_racingDieselPrefab != null && ModLoader.Database.GetItemById(Category, RacingDieselItemId) == null)
                RegisterItem(_racingDieselPrefab, RacingDieselItemId, EngineVariantSettings.RacingDieselName).Register();
        }

        private static GameObject CloneTemplate(GameObject donor, string variantName)
        {
            if (_templateHolder == null)
            {
                _templateHolder = new GameObject("Car05EngineVariants_Templates");
                if (mainscript.M != null)
                    _templateHolder.transform.SetParent(mainscript.M.transform, false);
                _templateHolder.SetActive(false);
            }

            GameObject clone = UnityEngine.Object.Instantiate(donor);
            clone.name = variantName;
            clone.SetActive(false);
            clone.transform.SetParent(_templateHolder.transform, false);
            return clone;
        }

        private static bool IsValidEnginePrefab(GameObject prefab)
        {
            if (prefab == null)
                return false;

            // Never replace a complete vehicle when looking for an engine donor.
            if (prefab.GetComponent<carscript>() != null || prefab.GetComponentInChildren<carscript>(true) != null)
                return false;

            return prefab.GetComponentInChildren<enginescript>(true) != null;
        }

        private static void ConfigurePrefab(GameObject prefab, EngineVariantKind kind, string variantName)
        {
            prefab.name = variantName;
            enginescript[] engines = prefab.GetComponentsInChildren<enginescript>(true);
            for (int i = 0; i < engines.Length; i++)
            {
                enginescript engine = engines[i];
                EngineVariantMarker marker = engine.GetComponent<EngineVariantMarker>();
                if (marker == null)
                    marker = engine.gameObject.AddComponent<EngineVariantMarker>();
                marker.Kind = kind;

                if (engine.GetComponent<EngineInitializer>() == null)
                    engine.gameObject.AddComponent<EngineInitializer>();
            }
        }
    }

    internal static class ItemDatabaseFields
    {
        internal static FieldInfo Car05Engine;
        internal static MethodInfo ItemInitMethod;

        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Car05Engine = typeof(itemdatabase).GetField("gcar05engine", flags);
            ItemInitMethod = typeof(itemdatabase).GetMethod("ItemInit", flags);

            if (Car05Engine == null)
                throw new MissingFieldException("itemdatabase", "gcar05engine");
            if (ItemInitMethod == null)
                throw new MissingMethodException("itemdatabase", "ItemInit");

            _initialized = true;
        }

        internal static GameObject GetCar05Engine(itemdatabase database)
        {
            return Car05Engine.GetValue(database) as GameObject;
        }

        internal static void SetCar05Engine(itemdatabase database, GameObject prefab)
        {
            Car05Engine.SetValue(database, prefab);
        }
    }

    internal static class EngineFields
    {
        internal static FieldInfo Rpm;
        internal static FieldInfo Start;
        internal static FieldInfo Running;
        internal static FieldInfo CanRun;
        internal static FieldInfo Ignition;
        internal static FieldInfo SMotor;
        internal static FieldInfo AlapRpm;
        internal static FieldInfo IwRpm;
        internal static FieldInfo MaxRpm;
        internal static FieldInfo Modifier;
        internal static FieldInfo MaxNm;
        internal static FieldInfo RpmChangeModifier;
        internal static FieldInfo StartChance;

        internal static MethodInfo UpdateMethod;
        internal static MethodInfo RunningMethod;

        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized)
                return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Rpm = FindField(flags, "rpm");
            Start = FindField(flags, "start");
            Running = FindField(flags, "running");
            CanRun = FindField(flags, "canRun", "canrun");
            Ignition = FindField(flags, "ignition");
            SMotor = FindField(flags, "SMotor", "smotor");
            AlapRpm = FindField(flags, "alapRpm", "alaprpm");
            IwRpm = FindField(flags, "iwrpm");
            MaxRpm = FindField(flags, "maxRpm", "maxrpm");
            Modifier = FindField(flags, "modifier");
            MaxNm = FindField(flags, "maxNm", "maxnm");
            RpmChangeModifier = FindField(flags, "rpmChangeModifier", "rpmchangemodifier");
            StartChance = FindField(flags, "startChance", "startchance");
            UpdateMethod = typeof(enginescript).GetMethod("Update", flags);
            RunningMethod = typeof(enginescript).GetMethod("Running", flags);

            RequireField(Rpm, "rpm");
            RequireField(Start, "start");
            RequireField(Running, "running");
            RequireField(CanRun, "canRun");
            RequireField(Ignition, "ignition");
            RequireField(SMotor, "SMotor");
            RequireField(AlapRpm, "alapRpm");
            RequireField(IwRpm, "iwrpm");
            RequireField(MaxRpm, "maxRpm");
            RequireField(Modifier, "modifier");
            RequireField(MaxNm, "maxNm");
            RequireField(RpmChangeModifier, "rpmChangeModifier");
            RequireField(StartChance, "startChance");

            if (UpdateMethod == null)
                throw new MissingMethodException("enginescript", "Update");
            if (RunningMethod == null)
                throw new MissingMethodException("enginescript", "Running");

            _initialized = true;
        }

        internal static float ReadFloat(FieldInfo field, enginescript engine)
        {
            return (float)field.GetValue(engine);
        }

        internal static void WriteFloat(FieldInfo field, enginescript engine, float value)
        {
            field.SetValue(engine, value);
        }

        internal static AudioSource ReadMotor(enginescript engine)
        {
            return SMotor.GetValue(engine) as AudioSource;
        }

        private static FieldInfo FindField(BindingFlags flags, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = typeof(enginescript).GetField(names[i], flags);
                if (field != null)
                    return field;
            }
            return null;
        }

        private static void RequireField(FieldInfo field, string name)
        {
            if (field == null)
                throw new MissingFieldException("enginescript", name);
        }
    }

    internal static class EngineVariantSettings
    {
        internal const string GasName = "05 Gas Engine (Orange)";
        internal const string DieselName = "05 Diesel Engine (Dark Orange)";
        internal const string RacingDieselName = "05 Racing Diesel Engine";

        internal static float ScaleFor(EngineVariantKind kind)
        {
            // Keep the stock footprint: the request changes color, fuel, power, and sound,
            // not engine size. The clamp also protects future scale tuning.
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                case EngineVariantKind.RacingDiesel:
                case EngineVariantKind.Gas:
                default:
                    return 1.0f;
            }
        }

        internal static float PowerMultiplierFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 1.18f;
                case EngineVariantKind.RacingDiesel:
                    return 1.60f;
                default:
                    return 1.05f;
            }
        }

        internal static float MaxRpmMultiplierFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 0.88f;
                case EngineVariantKind.RacingDiesel:
                    return 1.12f;
                default:
                    return 1.0f;
            }
        }

        internal static float TorqueMultiplierFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 1.28f;
                case EngineVariantKind.RacingDiesel:
                    return 1.50f;
                default:
                    return 1.05f;
            }
        }

        internal static float RpmChangeMultiplierFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 0.92f;
                case EngineVariantKind.RacingDiesel:
                    return 1.18f;
                default:
                    return 1.0f;
            }
        }

        internal static mainscript.fluidenum FuelFor(EngineVariantKind kind)
        {
            return kind == EngineVariantKind.Gas ? mainscript.fluidenum.gas : mainscript.fluidenum.diesel;
        }

        internal static float PitchFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 0.62f;
                case EngineVariantKind.RacingDiesel:
                    return 0.90f;
                default:
                    return 1.08f;
            }
        }

        internal static float GrowlFrequencyFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 48.0f;
                case EngineVariantKind.RacingDiesel:
                    return 78.0f;
                default:
                    return 85.0f;
            }
        }

        internal static float SubBassFrequencyFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 28.0f;
                case EngineVariantKind.RacingDiesel:
                    return 42.0f;
                default:
                    return 38.0f;
            }
        }

        internal static float HeartbeatFrequencyFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 42.0f;
                case EngineVariantKind.RacingDiesel:
                    return 72.0f;
                default:
                    return 65.0f;
            }
        }

        internal static float TurboFrequencyFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 480.0f;
                case EngineVariantKind.RacingDiesel:
                    return 900.0f;
                default:
                    return 700.0f;
            }
        }

        internal static Color PaintFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return new Color(0.42f, 0.075f, 0.008f, 1.0f);
                case EngineVariantKind.RacingDiesel:
                    return new Color(0.16f, 0.035f, 0.010f, 1.0f);
                default:
                    return new Color(1.0f, 0.30f, 0.025f, 1.0f);
            }
        }

        internal static Color EmissionFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return new Color(0.12f, 0.008f, 0.001f, 1.0f);
                case EngineVariantKind.RacingDiesel:
                    return new Color(1.0f, 0.075f, 0.003f, 1.0f);
                default:
                    return new Color(0.08f, 0.005f, 0.001f, 1.0f);
            }
        }

        internal static float MetallicFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 0.55f;
                case EngineVariantKind.RacingDiesel:
                    return 0.90f;
                default:
                    return 0.35f;
            }
        }

        internal static float SmoothnessFor(EngineVariantKind kind)
        {
            switch (kind)
            {
                case EngineVariantKind.Diesel:
                    return 0.55f;
                case EngineVariantKind.RacingDiesel:
                    return 0.85f;
                default:
                    return 0.45f;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class EngineVariantMarker : MonoBehaviour
    {
        public EngineVariantKind Kind = EngineVariantKind.Gas;
        public bool StatsConfigured;
        internal float NextFluidRefill;
    }

    [DisallowMultipleComponent]
    public sealed class EngineInitializer : MonoBehaviour
    {
        private enginescript _engine;
        private bool _configured;

        private void Awake()
        {
            _engine = GetComponent<enginescript>();
            if (_engine == null)
                return;

            EngineVariantMarker marker = GetComponent<EngineVariantMarker>();
            EngineVariantKind kind = marker == null ? EngineVariantKind.Gas : marker.Kind;

            // Scale before physics starts, and never exceed the safe 1.8x limit.
            float scale = Mathf.Clamp(EngineVariantSettings.ScaleFor(kind), 0.2f, 1.8f);
            transform.localScale = transform.localScale * scale;
        }

        private void Start()
        {
            if (_engine == null || _configured)
                return;

            EngineVariantMarker marker = GetComponent<EngineVariantMarker>();
            if (marker == null)
                return;

            // This flag is serialized on the prefab. It prevents a template's Start
            // from multiplying stats a second time on every spawned copy.
            if (!marker.StatsConfigured)
            {
                ConfigureStats(marker.Kind);
                ConfigureFuel(marker.Kind);
                marker.StatsConfigured = true;
            }
            _configured = true;

            if (GetComponent<EngineVisualStyler>() == null)
                gameObject.AddComponent<EngineVisualStyler>();
            if (GetComponent<EngineAudioController>() == null)
                gameObject.AddComponent<EngineAudioController>();
        }

        private void ConfigureStats(EngineVariantKind kind)
        {
            float stockMaxRpm = EngineFields.ReadFloat(EngineFields.MaxRpm, _engine);
            float stockModifier = EngineFields.ReadFloat(EngineFields.Modifier, _engine);
            float stockMaxNm = EngineFields.ReadFloat(EngineFields.MaxNm, _engine);
            float stockRpmChange = EngineFields.ReadFloat(EngineFields.RpmChangeModifier, _engine);

            EngineFields.WriteFloat(EngineFields.MaxRpm, _engine, stockMaxRpm * EngineVariantSettings.MaxRpmMultiplierFor(kind));
            EngineFields.WriteFloat(EngineFields.Modifier, _engine, stockModifier * EngineVariantSettings.PowerMultiplierFor(kind));
            EngineFields.WriteFloat(EngineFields.MaxNm, _engine, stockMaxNm * EngineVariantSettings.TorqueMultiplierFor(kind));
            EngineFields.WriteFloat(EngineFields.RpmChangeModifier, _engine, stockRpmChange * EngineVariantSettings.RpmChangeMultiplierFor(kind));
            EngineFields.WriteFloat(EngineFields.StartChance, _engine, 1.0f);
            _engine.currentRandomStart = 1.0f;
        }

        private void ConfigureFuel(EngineVariantKind kind)
        {
            _engine.Oilfluid = mainscript.fluidenum.oil;

            if (_engine.FuelConsumption == null)
                _engine.FuelConsumption = new mainscript.fluidcontainer();
            if (_engine.FuelConsumption.fluids == null)
                _engine.FuelConsumption.fluids = new List<mainscript.fluid>();

            _engine.FuelConsumption.fluids.Clear();
            _engine.FuelConsumption.fluids.Add(new mainscript.fluid(EngineVariantSettings.FuelFor(kind), 1.0f));
        }
    }

    internal static class EngineVariantRuntime
    {
        internal static EngineVariantMarker GetInstalledMarker(enginescript engine)
        {
            if (engine == null)
                return null;

            EngineVariantMarker marker = engine.GetComponent<EngineVariantMarker>();
            if (marker == null || engine.GetComponentInParent<carscript>() == null)
                return null;

            return marker;
        }

        internal static void RefillFluids(enginescript engine, EngineVariantMarker marker)
        {
            if (Time.time < marker.NextFluidRefill)
                return;

            marker.NextFluidRefill = Time.time + 0.5f;

            tankscript fuelTank = engine.Fuel != null ? engine.Fuel : engine.iFuel;
            if (fuelTank != null)
                FillTank(fuelTank, EngineVariantSettings.FuelFor(marker.Kind));

            if (engine.T != null)
                FillTank(engine.T, mainscript.fluidenum.oil);
        }

        internal static void ForceRunning(enginescript engine)
        {
            engine.start = false;
            engine.running = true;
            engine.canRun = true;
            engine.ignition = true;
        }

        private static void FillTank(tankscript tank, mainscript.fluidenum fluidType)
        {
            if (tank.F == null || tank.F.maxC <= 0.0f)
                return;
            if (tank.F.fluids == null)
                tank.F.fluids = new List<mainscript.fluid>();

            float missing = tank.F.maxC - tank.F.GetAmount();
            if (missing > 0.001f)
                tank.F.ChangeOne(missing, fluidType);
        }
    }

    public static class EngineUpdatePrefixPatch
    {
        public static void Prefix(enginescript __instance)
        {
            EngineVariantMarker marker = EngineVariantRuntime.GetInstalledMarker(__instance);
            if (marker != null)
                EngineVariantRuntime.RefillFluids(__instance, marker);
        }
    }

    public static class EngineUpdatePostfixPatch
    {
        public static void Postfix(enginescript __instance)
        {
            EngineVariantMarker marker = EngineVariantRuntime.GetInstalledMarker(__instance);
            if (marker == null)
                return;

            EngineVariantRuntime.ForceRunning(__instance);

            // Rescue a stall only. RPM is otherwise left to the vanilla throttle simulation.
            if (__instance.rpm < 10.0f)
            {
                __instance.rpm = __instance.alapRpm;
                __instance.iwrpm = __instance.alapRpm;
            }
        }
    }

    public static class EngineRunningPatch
    {
        public static void Prefix(enginescript __instance)
        {
            if (EngineVariantRuntime.GetInstalledMarker(__instance) != null)
                EngineVariantRuntime.ForceRunning(__instance);
        }
    }

    public static class ItemDatabaseRebuildPatch
    {
        public static void Postfix(itemdatabase __instance)
        {
            Car05EngineVariantsMod.RestoreGasItemSlot(__instance);
        }
    }

    [DisallowMultipleComponent]
    public sealed class EngineVisualStyler : MonoBehaviour
    {
        private enginescript _engine;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propertyBlock;
        private Color _paint;
        private Color _emission;
        private float _metallic;
        private float _smoothness;
        private bool _isRacingDiesel;
        private Light _raceLight;

        private void Start()
        {
            _engine = GetComponent<enginescript>();
            EngineVariantMarker marker = GetComponent<EngineVariantMarker>();
            if (_engine == null || marker == null)
                return;

            _paint = EngineVariantSettings.PaintFor(marker.Kind);
            _emission = EngineVariantSettings.EmissionFor(marker.Kind);
            _metallic = EngineVariantSettings.MetallicFor(marker.Kind);
            _smoothness = EngineVariantSettings.SmoothnessFor(marker.Kind);
            _isRacingDiesel = marker.Kind == EngineVariantKind.RacingDiesel;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _propertyBlock = new MaterialPropertyBlock();

            ApplyMaterials(1.0f);

            if (_isRacingDiesel)
            {
                Transform existingLight = transform.Find("RacingDiesel_Glow");
                if (existingLight != null)
                    _raceLight = existingLight.GetComponent<Light>();

                if (_raceLight == null)
                {
                    GameObject lightObject = new GameObject("RacingDiesel_Glow");
                    lightObject.transform.SetParent(transform, false);
                    _raceLight = lightObject.AddComponent<Light>();
                }

                _raceLight.type = LightType.Point;
                _raceLight.color = new Color(1.0f, 0.075f, 0.003f, 1.0f);
                _raceLight.range = 2.5f;
                _raceLight.intensity = 0.30f;
                _raceLight.shadows = LightShadows.None;
            }
        }

        private void Update()
        {
            if (!_isRacingDiesel || _engine == null || _renderers == null)
                return;

            float rpmRatio = Mathf.Clamp01(_engine.rpm / Mathf.Max(1.0f, _engine.maxRpm));
            float pulse = 0.85f + Mathf.Sin(Time.time * 18.0f) * 0.15f;
            float strength = pulse * (0.65f + rpmRatio * 0.80f);
            ApplyEmission(strength);

            if (_raceLight != null)
                _raceLight.intensity = 0.20f + rpmRatio * 0.75f;
        }

        private void ApplyMaterials(float emissionStrength)
        {
            if (_renderers == null || _propertyBlock == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer target = _renderers[i];
                if (target == null)
                    continue;

                _propertyBlock.Clear();
                target.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_Color", _paint);
                _propertyBlock.SetColor("_EmissionColor", _emission * emissionStrength);
                _propertyBlock.SetFloat("_Metallic", _metallic);
                _propertyBlock.SetFloat("_Glossiness", _smoothness);
                _propertyBlock.SetFloat("_Smoothness", _smoothness);
                target.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ApplyEmission(float emissionStrength)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer target = _renderers[i];
                if (target == null)
                    continue;

                _propertyBlock.Clear();
                target.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_EmissionColor", _emission * emissionStrength);
                target.SetPropertyBlock(_propertyBlock);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class EngineAudioController : MonoBehaviour
    {
        private const float GrowlVolume = 0.20f;
        private const float SubBassVolume = 0.15f;
        private const float HeartbeatVolume = 0.12f;
        private const float TurboVolume = 0.22f;

        private enginescript _engine;
        private AudioSource _motor;
        private AudioSource _growl;
        private AudioSource _subBass;
        private AudioSource _heartbeat;
        private AudioSource _turbo;
        private float _pitchMul;

        private void Start()
        {
            _engine = GetComponent<enginescript>();
            EngineVariantMarker marker = GetComponent<EngineVariantMarker>();
            if (_engine == null || marker == null)
                return;

            _motor = EngineFields.ReadMotor(_engine);
            _pitchMul = EngineVariantSettings.PitchFor(marker.Kind);
            AudioClip[] clips = EngineAudioClipFactory.GetClips(marker.Kind);

            _growl = CreateLayer("Growl", clips[0], GrowlVolume);
            _subBass = CreateLayer("SubBass", clips[1], SubBassVolume);
            _heartbeat = CreateLayer("Heartbeat", clips[2], HeartbeatVolume);
            _turbo = CreateLayer("Turbo", clips[3], TurboVolume);
        }

        private void LateUpdate()
        {
            if (_engine == null)
                return;

            float rpmRatio = Mathf.Clamp01(_engine.rpm / Mathf.Max(1.0f, _engine.maxRpm));

            // Keep the vanilla motor source and blend the procedural layers with it.
            if (_motor != null)
                _motor.pitch = _pitchMul * (0.85f + rpmRatio * 0.35f);

            bool installed = _engine.GetComponentInParent<carscript>() != null;
            bool audible = installed && _engine.running && _engine.rpm >= 10.0f;
            float loadGain = 0.55f + rpmRatio * 0.45f;

            UpdateLayer(_growl, audible, GrowlVolume * loadGain, 0.75f + rpmRatio * 0.45f);
            UpdateLayer(_subBass, audible, SubBassVolume * (0.70f + rpmRatio * 0.30f), 0.85f + rpmRatio * 0.25f);
            UpdateLayer(_heartbeat, audible, HeartbeatVolume * (1.0f - rpmRatio * 0.25f), 0.80f + rpmRatio * 0.40f);

            float turboGain = Mathf.Clamp01((rpmRatio - 0.20f) / 0.80f);
            UpdateLayer(_turbo, audible && turboGain > 0.01f, TurboVolume * turboGain, 0.75f + rpmRatio * 1.20f);
        }

        private AudioSource CreateLayer(string layerName, AudioClip clip, float volume)
        {
            string objectName = "05Engine_" + layerName;
            Transform existingLayer = transform.Find(objectName);
            AudioSource source = existingLayer == null ? null : existingLayer.GetComponent<AudioSource>();

            if (source == null)
            {
                GameObject layerObject = new GameObject(objectName);
                layerObject.transform.SetParent(transform, false);
                source = layerObject.AddComponent<AudioSource>();
            }

            // Reuse layers already present on a cloned template instead of doubling them.
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume;
            source.pitch = 1.0f;
            source.spatialBlend = 0.65f;
            source.dopplerLevel = 0.0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.5f;
            source.maxDistance = 30.0f;
            return source;
        }

        private static void UpdateLayer(AudioSource source, bool audible, float volume, float pitch)
        {
            if (source == null)
                return;

            source.volume = volume;
            source.pitch = pitch;
            if (audible)
            {
                if (!source.isPlaying)
                    source.Play();
            }
            else if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }

    internal static class EngineAudioClipFactory
    {
        private const int SampleRate = 22050;
        private static readonly Dictionary<EngineVariantKind, AudioClip[]> Cache = new Dictionary<EngineVariantKind, AudioClip[]>();

        internal static AudioClip[] GetClips(EngineVariantKind kind)
        {
            AudioClip[] clips;
            if (Cache.TryGetValue(kind, out clips) && ClipsAreValid(clips))
                return clips;

            string prefix = kind.ToString();
            clips = new[]
            {
                CreateClip(prefix + "_Growl", EngineVariantSettings.GrowlFrequencyFor(kind), 0),
                CreateClip(prefix + "_SubBass", EngineVariantSettings.SubBassFrequencyFor(kind), 1),
                CreateClip(prefix + "_Heartbeat", EngineVariantSettings.HeartbeatFrequencyFor(kind), 2),
                CreateClip(prefix + "_Turbo", EngineVariantSettings.TurboFrequencyFor(kind), 3)
            };
            Cache[kind] = clips;
            return clips;
        }

        private static bool ClipsAreValid(AudioClip[] clips)
        {
            if (clips == null || clips.Length != 4)
                return false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    return false;
            }
            return true;
        }

        private static AudioClip CreateClip(string clipName, float frequency, int style)
        {
            int sampleCount = SampleRate;
            float[] samples = new float[sampleCount];
            float filteredNoise = 0.0f;
            float phaseFrequency = Mathf.Max(1.0f, Mathf.Round(frequency));
            System.Random random = new System.Random(7919 + style * 101 + (int)phaseFrequency);

            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / SampleRate;
                float phase = 2.0f * Mathf.PI * phaseFrequency * time;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float filterAmount = style == 3 ? 0.18f : 0.06f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, filterAmount);
                float sample;

                switch (style)
                {
                    case 0:
                        sample = Mathf.Sin(phase) * 0.55f
                            + Mathf.Sin(phase * 2.01f) * 0.25f
                            + Mathf.Sin(phase * 3.97f) * 0.15f
                            + filteredNoise * 0.25f;
                        break;
                    case 1:
                        sample = Mathf.Sin(phase) * 0.80f
                            + Mathf.Sin(phase * 0.50f) * 0.20f
                            + filteredNoise * 0.08f;
                        break;
                    case 2:
                        float beatPhase = Mathf.Repeat(time * 2.0f, 1.0f);
                        float envelope = Mathf.Exp(-beatPhase * 12.0f);
                        sample = (Mathf.Sin(phase) + Mathf.Sin(phase * 2.10f) * 0.35f + filteredNoise * 0.15f) * envelope;
                        break;
                    default:
                        sample = Mathf.Sin(phase) * 0.28f
                            + Mathf.Sin(phase * 1.98f) * 0.12f
                            + filteredNoise * 0.35f;
                        break;
                }

                samples[i] = Mathf.Clamp(sample, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
