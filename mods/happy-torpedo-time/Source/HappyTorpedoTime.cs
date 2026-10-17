// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using UBOAT.Game;
using UBOAT.Game.Core;
using UBOAT.Game.Core.Data;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Characters.Activators;
using UBOAT.Game.Scene.Characters.Actions;
using UBOAT.Game.Sandbox;
using UBOAT.Game.Sandbox.Devices;
using UBOAT.Game.UI;
using UBOAT.Game.UI.Storage;

namespace UBOAT.Mods.HappyTorpedoTime
{
    [NonSerializedInGameState]
    public class HappyTorpedoTimeLoader : IUserMod
    {
        private static bool loaded;
        public void OnLoaded()
        {
            if (loaded) return;
            ModDiagnostics.Begin();
            Harmony harmony = new Harmony("Landomatic.HappyTorpedoTime");
            try
            {
                HappyTorpedoTimeRuntime.Install(harmony);
                loaded = true;
                ModDiagnostics.DataProbe = HappyTorpedoTimeRuntime.ReportDataReady;
                ModDiagnostics.Event("READY", "build=" + ModDiagnostics.Build + " launchMethods=2 pairedRangeHandling=True tooltipResize=True");
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll("Landomatic.HappyTorpedoTime");
                ModDiagnostics.Error("installation", ex);
                ModDiagnostics.Event("FAILED", "runtime patches rolled back; datasheets remain active");
            }
        }
    }

    [NonSerializedInGameState]
    public static class HappyTorpedoTimeRuntime
    {
        private const string Marker = "<b>Happy Torpedo Time</b>";
        private static readonly FieldInfo DatabaseField = AccessTools.Field(typeof(Equipment), "equipmentDatabase");
        private static readonly FieldInfo[] RangeFields = new FieldInfo[]
        {
            AccessTools.Field(typeof(StoredTorpedo), "range1"), AccessTools.Field(typeof(StoredTorpedo), "range2"), AccessTools.Field(typeof(StoredTorpedo), "range3")
        };
        private static readonly FieldInfo SettingsField = AccessTools.Field(typeof(UnitsUtility), "userSettings");
        private static readonly FieldInfo LauncherTooltipField = AccessTools.Field(typeof(TorpedoLauncherUI), "tooltip");
        private static readonly FieldInfo FloatingLabel = AccessTools.Field(typeof(FloatingTooltip), "contents");
        private static readonly FieldInfo FloatingRect = AccessTools.Field(typeof(FloatingTooltip), "rectTransform");
        private static readonly FieldInfo FloatingWidth = AccessTools.Field(typeof(FloatingTooltip), "maxTooltipWidth");
        private static readonly MethodInfo LimitTooltip = AccessTools.Method(typeof(FloatingTooltip), "LimitToScreen");
        private static readonly FieldInfo ManualActivatorField = AccessTools.Field(typeof(UseTorpedoLauncherLaunchLever), "hoveredActivator");
        private static readonly FieldInfo AllPlayerField = AccessTools.Field(typeof(UseTorpedoLaunchLever), "playerShip");
        private static readonly PropertyInfo AllLaunchersProperty = AllPlayerField == null ? null : AccessTools.Property(AllPlayerField.FieldType, "TorpedoLaunchers");
        private static readonly HashSet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
        {
            "G7a Torpedo T1 - Pi1", "G7a Torpedo T1 - Pi1.1", "G7a Torpedo T1 - Pi3",
            "G7a Torpedo T1 - Pi3 - FAT", "G7a Torpedo T1 - Pi3 - LUT",
            "G7e Torpedo T2 - Pi1", "G7e Torpedo T2 - Pi1.1", "G7e Torpedo T3 - Pi2",
            "G7e Torpedo T3 - Pi2 - FAT", "G7e Torpedo T3 - Pi2 - LUT",
            "G7e Torpedo T3 - Pi2 - Prototype", "G7es Zaunkönig Torpedo T5 - Pi4",
            "G7es Zaunkönig Torpedo T5 - Pi4 - Prototype"
        };
        private static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>(StringComparer.Ordinal);
        private static readonly ConditionalWeakTable<object, ViewState> Views = new ConditionalWeakTable<object, ViewState>();
        private static readonly HashSet<string> Warnings = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Regex LeadingNumber = new Regex(@"^[+-]?[0-9]+(?:[.,][0-9]+)?");
        private static int launchId;

        [NonSerializedInGameState]
        private sealed class Spec
        {
            public string Name;
            public SandboxEquipmentData ColdData, WarmData;
            public float[] Cold, Warm, Speeds;
            public float Duration, Damage, DudChance;
            public bool Steam { get { return Name.StartsWith("G7a", StringComparison.Ordinal); } }
        }

        [NonSerializedInGameState]
        private sealed class ViewState
        {
            public ViewState() { }
            public float NextUpdate;
            public int TooltipId;
            public string OriginalTooltip, UnitKey;
            public WeakReference Item;
        }

        public static void Install(Harmony harmony)
        {
            if (DatabaseField == null || SettingsField == null || LauncherTooltipField == null || ManualActivatorField == null || AllLaunchersProperty == null)
                throw new MissingFieldException("Required torpedo/UI fields were not found.");
            if (FloatingLabel == null || FloatingRect == null || FloatingWidth == null || LimitTooltip == null)
                throw new MissingFieldException("Required floating tooltip layout members were not found.");
            foreach (FieldInfo field in RangeFields) if (field == null) throw new MissingFieldException("Stored torpedo range field missing.");
            Patch(harmony, AccessTools.PropertyGetter(typeof(StoredTorpedo), "Range1"), "Range1Postfix");
            Patch(harmony, AccessTools.PropertyGetter(typeof(StoredTorpedo), "Range2"), "Range2Postfix");
            Patch(harmony, AccessTools.PropertyGetter(typeof(StoredTorpedo), "Range3"), "Range3Postfix");
            int launches = 0;
            foreach (MethodInfo method in typeof(StoredTorpedo).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name != "Launch") continue;
                harmony.Patch(method,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(HappyTorpedoTimeRuntime), "LaunchBefore")),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(HappyTorpedoTimeRuntime), "LaunchAfter")),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(HappyTorpedoTimeRuntime), "LaunchTranspiler")));
                launches++;
            }
            if (launches != 2) throw new InvalidOperationException("Expected two StoredTorpedo launch methods, found " + launches);
            Patch(harmony, AccessTools.Method(typeof(Locale), "get_Item", new Type[] { typeof(string) }), "LocaleOnePostfix");
            Patch(harmony, AccessTools.Method(typeof(Locale), "get_Item", new Type[] { typeof(string), typeof(string) }), "LocaleTwoPostfix");
            Patch(harmony, AccessTools.Method(typeof(Locale), "get_Item", new Type[] { typeof(string), typeof(string), typeof(string) }), "LocaleThreePostfix");
            Patch(harmony, AccessTools.Method(typeof(LocalizedString), "ToString", Type.EmptyTypes), "LocalizedDescriptionPostfix");
            Patch(harmony, AccessTools.Method(typeof(TorpedoLauncherUI), "OnPointerEnter"), "LauncherEntered");
            Patch(harmony, AccessTools.Method(typeof(TorpedoLauncherUI), "Update"), "LauncherUpdated");
            Patch(harmony, AccessTools.Method(typeof(TorpedoLauncherGroup), "GetStateText"), "StationPostfix");
            Patch(harmony, AccessTools.Method(typeof(TorpedoTubeActivator), "GetStateText"), "TubePostfix");
            Patch(harmony, AccessTools.Method(typeof(UseTorpedoLauncherLaunchLever), "GetTooltipContents"), "ManualLeverPostfix");
            Patch(harmony, AccessTools.Method(typeof(UseTorpedoLaunchLever), "GetTooltipContents"), "AllLeversPostfix");
            Patch(harmony, AccessTools.Method(typeof(StorageUI), "SetDescription"), "StorageDescriptionPostfix");
            Patch(harmony, AccessTools.Method(typeof(StorageUI), "Update"), "StorageUpdated");
        }

        private static void Patch(Harmony harmony, MethodInfo target, string postfix)
        {
            if (target == null) throw new MissingMethodException(postfix);
            harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(HappyTorpedoTimeRuntime), postfix)));
        }

        public static bool ReportDataReady()
        {
            EquipmentDatabase database = DatabaseField.GetValue(null) as EquipmentDatabase;
            if (database == null || database.GetEquipmentByName("G7e Torpedo T2 - Pi1") == null
                || database.GetEquipmentByName("G7e Torpedo T2 - Pi1 (Warmed)") == null) return false;
            int valid = 0;
            foreach (string name in Supported)
            {
                Spec spec = GetSpec(name);
                if (spec == null) { ModDiagnostics.Event("WARN", "missing or invalid paired equipment: " + name); continue; }
                valid++;
                ModDiagnostics.Event("DATA", "name=\"" + name + "\" cold_m=" + Numbers(spec.Cold)
                    + " warm_m=" + Numbers(spec.Warm) + " benefit_s=" + ModDiagnostics.F(spec.Duration));
            }
            ModDiagnostics.Event("DATA-READY", "validPairs=" + valid + " expectedPairs=" + Supported.Count);
            return true;
        }

        private static string Numbers(float[] values)
        {
            return ModDiagnostics.F(values[0]) + "/" + ModDiagnostics.F(values[1]) + "/" + ModDiagnostics.F(values[2]);
        }

        public static void LaunchBefore(StoredTorpedo __instance, ref int __state)
        {
            __state = ++launchId;
            ModDiagnostics.Count("launches");
            try
            {
                float[] raw = new float[3];
                for (int i = 0; i < 3; i++) raw[i] = ((Parameter)RangeFields[i].GetValue(__instance)).Value;
                float[] effective = new float[] { EffectiveRange1(__instance).Value, EffectiveRange2(__instance).Value, EffectiveRange3(__instance).Value };
                ModDiagnostics.Event("LAUNCH-BEFORE", "id=" + __state + " name=\"" + __instance.name
                    + "\" dataName=\"" + (__instance.Data == null ? "null" : __instance.Data.Name)
                    + "\" maintained=" + __instance.IsMaintained + " stored_m=" + Numbers(raw) + " effective_m=" + Numbers(effective));
                if (ModDiagnostics.DebugEnabled)
                    ModDiagnostics.Detail("MAINTENANCE", "id=" + __state + " progress=" + ModDiagnostics.F(__instance.MaintenanceCooldownProgress)
                        + " cooldown_s=" + ModDiagnostics.F(__instance.MaintenanceCooldown.Value));
            }
            catch (Exception ex) { Warn("launch-before", ex); }
        }

        public static void LaunchAfter(StoredTorpedo __instance, int __state)
        {
            try
            {
                Torpedo launched = __instance.GetComponent<Torpedo>();
                if (launched == null) { ModDiagnostics.Event("WARN", "launch id=" + __state + " has no Torpedo component"); return; }
                ModDiagnostics.Event("LAUNCH-AFTER", "id=" + __state + " maintained=" + __instance.IsMaintained
                    + " launchedRange_m=" + ModDiagnostics.F(launched.Range) + " velocity_mps=" + ModDiagnostics.F(launched.Velocity));
            }
            catch (Exception ex) { Warn("launch-after", ex); }
        }

        public static IEnumerable<CodeInstruction> LaunchTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            int[] matches = new int[3];
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Ldfld) continue;
                for (int mode = 0; mode < 3; mode++)
                {
                    FieldInfo field = AccessTools.Field(typeof(StoredTorpedo), "range" + (mode + 1));
                    if (!Equals(code[i].operand, field)) continue;
                    // StoredTorpedo -> Parameter, exactly the original field-read stack contract.
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = AccessTools.Method(typeof(HappyTorpedoTimeRuntime), "EffectiveRange" + (mode + 1));
                    matches[mode]++;
                }
            }
            if (matches[0] != 1 || matches[1] != 1 || matches[2] != 1)
                throw new InvalidOperationException("Launch range reads do not match the verified game build.");
            return code;
        }

        public static Parameter EffectiveRange1(StoredTorpedo torpedo) { return EffectiveRange(torpedo, 0); }
        public static Parameter EffectiveRange2(StoredTorpedo torpedo) { return EffectiveRange(torpedo, 1); }
        public static Parameter EffectiveRange3(StoredTorpedo torpedo) { return EffectiveRange(torpedo, 2); }
        private static Parameter EffectiveRange(StoredTorpedo torpedo, int mode)
        {
            Parameter result = (Parameter)RangeFields[mode].GetValue(torpedo);
            AdjustRange(torpedo, mode, ref result);
            return result;
        }
        public static void Range1Postfix(StoredTorpedo __instance, ref Parameter __result) { AdjustRange(__instance, 0, ref __result); }
        public static void Range2Postfix(StoredTorpedo __instance, ref Parameter __result) { AdjustRange(__instance, 1, ref __result); }
        public static void Range3Postfix(StoredTorpedo __instance, ref Parameter __result) { AdjustRange(__instance, 2, ref __result); }

        private static void AdjustRange(StoredTorpedo torpedo, int mode, ref Parameter result)
        {
            try
            {
                if (torpedo == null || result == null || torpedo.Data == null) return;
                string dataName = torpedo.Data.Name;
                Spec spec = GetSpec(dataName);
                if (spec == null) return;
                float declared = IsWarmedName(dataName) ? spec.Warm[mode] : spec.Cold[mode];
                float desired = torpedo.IsMaintained ? spec.Warm[mode] : spec.Cold[mode];
                if (declared <= 0 || declared == desired) return;
                float value = result.Value * desired / declared;
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0) return;
                // A detached return value; no modifiers or numbers are written into a saved torpedo.
                result = new Parameter(result.Name, value);
            }
            catch (Exception ex) { Warn("range", ex); }
        }

        private static bool IsWarmedName(string name) { return name != null && name.EndsWith(" (Warmed)", StringComparison.Ordinal); }
        private static string ColdName(string name) { return IsWarmedName(name) ? name.Substring(0, name.Length - 9) : name; }

        private static Spec GetSpec(string name)
        {
            name = ColdName(name);
            if (name == null || !Supported.Contains(name)) return null;
            EquipmentDatabase database = DatabaseField.GetValue(null) as EquipmentDatabase;
            if (database == null) return null;
            SandboxEquipmentData cold = database.GetEquipmentByName(name);
            SandboxEquipmentData warm = database.GetEquipmentByName(name + " (Warmed)");
            if (cold == null || warm == null) return null;
            Spec spec;
            if (Specs.TryGetValue(name, out spec) && ReferenceEquals(spec.ColdData, cold) && ReferenceEquals(spec.WarmData, warm)) return spec;
            spec = new Spec { Name = name, ColdData = cold, WarmData = warm, Cold = new float[3], Warm = new float[3], Speeds = new float[3] };
            for (int i = 0; i < 3; i++)
            {
                spec.Cold[i] = Read(cold, "Range" + (i + 1));
                spec.Warm[i] = Read(warm, "Range" + (i + 1));
                spec.Speeds[i] = Read(cold, "Speed" + (i + 1));
                if (!Positive(spec.Cold[i]) || !Positive(spec.Warm[i]) || !Positive(spec.Speeds[i])) return null;
            }
            spec.Duration = Read(warm, "MaintenanceCooldown");
            spec.Damage = Read(cold, "Damage");
            spec.DudChance = Read(cold, "DudChance");
            Specs[name] = spec;
            return spec;
        }

        private static bool Positive(float value) { return value > 0 && !float.IsNaN(value) && !float.IsInfinity(value); }
        private static float Read(SandboxEquipmentData data, string target)
        {
            if (data.Modifiers == null) return float.NaN;
            foreach (AddressedModifier modifier in data.Modifiers)
                if (modifier != null && !modifier.IsMultiplier && modifier.Target == target) return modifier.Value;
            return float.NaN;
        }

        private static string UnitKey()
        {
            UserSettings settings = SettingsField.GetValue(null) as UserSettings;
            return settings == null ? string.Empty : settings.GameplaySettings.units.ToString();
        }

        private static string Distance(float meters)
        {
            float km = meters / 1000f;
            string native = UnitsUtility.KilometersToPreferredUnits(km, false);
            // Native long-range formatting truncates nautical miles. Retain its localized
            // unit and preference, but show two decimals rather than understate the range.
            float value = km;
            if (UnitKey() == "Nautical")
            {
                float nauticalMiles = km * 0.53995680809021f;
                value = Mathf.FloorToInt(nauticalMiles) != 0 ? nauticalMiles : nauticalMiles * 10f;
            }
            else if (km < 1f) value = meters;
            return LeadingNumber.Replace(native, value.ToString("0.##", CultureInfo.CurrentCulture), 1);
        }

        private static string Speed(float metersPerSecond) { return UnitsUtility.KilometersPerHourToPreferredUnits(metersPerSecond * 3.6f); }
        private static string Ranges(Spec spec, float[] ranges)
        {
            if (spec.Speeds[0] == spec.Speeds[1] && spec.Speeds[1] == spec.Speeds[2] && ranges[0] == ranges[1] && ranges[1] == ranges[2])
                return Distance(ranges[0]) + " at " + Speed(spec.Speeds[0]);
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                if (i != 0) text.Append(" / ");
                text.Append(Speed(spec.Speeds[i])).Append(": ").Append(Distance(ranges[i]));
            }
            return text.ToString();
        }

        private static string Catalog(Spec spec)
        {
            string text = "Unmaintained range: " + Ranges(spec, spec.Cold) + "\n"
                + (spec.Steam ? "Maintained range: " : "Warmed range: ") + Ranges(spec, spec.Warm);
            if (Positive(spec.Duration)) text += "\nMaintenance benefit: " + UnitsUtility.FormatTimeSpanShortWithoutSeconds(TimeSpan.FromSeconds(spec.Duration));
            if (Positive(spec.Damage)) text += "\nBase damage: " + spec.Damage.ToString("0.##", CultureInfo.CurrentCulture);
            if (!float.IsNaN(spec.DudChance)) text += "\nBase dud setting: " + spec.DudChance.ToString("0.#%", CultureInfo.CurrentCulture) + " (before maintenance and difficulty effects)";
            return text;
        }

        private static string Current(StoredTorpedo torpedo)
        {
            if (torpedo == null || torpedo.Data == null) return null;
            Spec spec = GetSpec(torpedo.Data.Name);
            if (spec == null) return null;
            float[] ranges = new float[] { EffectiveRange1(torpedo).Value, EffectiveRange2(torpedo).Value, EffectiveRange3(torpedo).Value };
            float[] speeds = new float[] { torpedo.Speed1.Value, torpedo.Speed2.Value, torpedo.Speed3.Value };
            Spec view = new Spec { Speeds = speeds };
            string state = torpedo.IsMaintained ? (spec.Steam ? "maintained" : "warmed") : "unmaintained";
            string text = "Current range (" + state + "): " + Ranges(view, ranges);
            if (torpedo.IsMaintained)
            {
                float seconds = (1f - torpedo.MaintenanceCooldownProgress) * torpedo.MaintenanceCooldown.Value;
                if (seconds >= 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds))
                    text += "\nBenefit remaining: " + UnitsUtility.FormatTimeSpanShortWithoutSeconds(TimeSpan.FromSeconds(seconds));
            }
            return text;
        }

        private static void ReplaceDescription(string key, ref string result)
        {
            if (key == null || !key.EndsWith(" Description", StringComparison.Ordinal)) return;
            string name;
            if (key.StartsWith("Items/", StringComparison.Ordinal)) name = key.Substring(6);
            else if (key.StartsWith("Activators/", StringComparison.Ordinal)) name = key.Substring(11);
            else return;
            name = name.Substring(0, name.Length - 12);
            try
            {
                Spec spec = GetSpec(name);
                if (spec != null) result = Catalog(spec);
            }
            catch (Exception ex) { Warn("description", ex); }
        }

        public static void LocaleOnePostfix(string __0, ref string __result) { ReplaceDescription(__0, ref __result); }
        public static void LocaleTwoPostfix(string __0, string __1, ref string __result)
        {
            if (__1 != null && __1.EndsWith(" Description", StringComparison.Ordinal)) ReplaceDescription(__0 + "/" + __1, ref __result);
        }
        public static void LocaleThreePostfix(string __0, string __1, string __2, ref string __result)
        {
            if (__2 != null && __2.EndsWith(" Description", StringComparison.Ordinal)) ReplaceDescription(__0 + "/" + __1 + "/" + __2, ref __result);
        }
        public static void LocalizedDescriptionPostfix(LocalizedString __instance, ref string __result) { ReplaceDescription(__instance.Keyword, ref __result); }

        public static void LauncherEntered(TorpedoLauncherUI __instance) { UpdateLauncherTooltip(__instance, true); }
        public static void LauncherUpdated(TorpedoLauncherUI __instance) { UpdateLauncherTooltip(__instance, false); }
        private static void UpdateLauncherTooltip(TorpedoLauncherUI ui, bool force)
        {
            try
            {
                ITooltip tip = LauncherTooltipField.GetValue(ui) as ITooltip;
                Behaviour behaviour = tip as Behaviour;
                if (tip == null || (behaviour != null && !behaviour.isActiveAndEnabled) || ui.Launcher == null) return;
                ViewState state = Views.GetOrCreateValue(ui);
                if (!force && Time.unscaledTime < state.NextUpdate) return;
                state.NextUpdate = Time.unscaledTime + 0.25f;
                int id = RuntimeHelpers.GetHashCode(tip);
                if (state.TooltipId != id || force)
                {
                    state.TooltipId = id;
                    state.OriginalTooltip = tip.Contents;
                    int marker = state.OriginalTooltip == null ? -1 : state.OriginalTooltip.IndexOf("\n\n" + Marker, StringComparison.Ordinal);
                    if (marker >= 0) state.OriginalTooltip = state.OriginalTooltip.Substring(0, marker);
                }
                string current = Current(ui.Launcher.Torpedo);
                string desired = state.OriginalTooltip + (current == null ? string.Empty : "\n\n" + Marker + "\n" + current);
                if (tip.Contents != desired)
                {
                    tip.Contents = desired;
                    ResizeLauncherTooltip(tip);
                    ModDiagnostics.Count("tooltipUpdates");
                    if (ModDiagnostics.SampleDue("TOOLTIP"))
                        ModDiagnostics.Event("TOOLTIP", "length=" + desired.Length + " type=" + tip.GetType().Name);
                }
            }
            catch (Exception ex) { Warn("tube tooltip", ex); }
        }

        private static void ResizeLauncherTooltip(ITooltip tip)
        {
            FloatingTooltip floating = tip as FloatingTooltip;
            if (floating == null) return;
            object label = FloatingLabel.GetValue(floating);
            RectTransform rect = FloatingRect.GetValue(floating) as RectTransform;
            if (label == null || rect == null) throw new InvalidOperationException("Floating tooltip has no label/rectangle.");
            float width = (float)FloatingWidth.GetValue(floating);
            MethodInfo measure = AccessTools.Method(label.GetType(), "GetPreferredValues", new Type[] { typeof(float), typeof(float) });
            if (measure == null) throw new MissingMethodException("TMP_Text.GetPreferredValues(float,float)");
            Vector2 preferred = (Vector2)measure.Invoke(label, new object[] { width, float.PositiveInfinity });
            // Match FloatingTooltip.OpenInternal and UpdateLabel's native padding.
            rect.sizeDelta = new Vector2(width + 40f, preferred.y + 33f);
            LimitTooltip.Invoke(null, new object[] { floating.transform.position, floating });
            ModDiagnostics.Detail("TOOLTIP-LAYOUT", "width=" + ModDiagnostics.F(rect.sizeDelta.x)
                + " height=" + ModDiagnostics.F(rect.sizeDelta.y) + " textHeight=" + ModDiagnostics.F(preferred.y));
        }

        public static void StationPostfix(TorpedoLauncherGroup __instance, ref string __result)
        {
            try
            {
                if (__instance.Launchers == null) return;
                StringBuilder text = new StringBuilder();
                for (int i = 0; i < __instance.Launchers.Length; i++)
                {
                    TorpedoLauncher launcher = __instance.Launchers[i];
                    if (launcher == null || !launcher.IsLoaded) continue;
                    string current = Current(launcher.Torpedo);
                    if (current == null) continue;
                    ExtendedTorpedoLauncher extended = launcher as ExtendedTorpedoLauncher;
                    text.Append("\nTube ").Append(extended == null ? (i + 1).ToString() : Roman(extended.LauncherIndex + 1)).Append(": ").Append(current);
                }
                if (text.Length != 0) __result += "\n\n" + Marker + text;
            }
            catch (Exception ex) { Warn("station tooltip", ex); }
        }

        private static string Roman(int value)
        {
            string[] numbers = { "I", "II", "III", "IV", "V", "VI" };
            return value > 0 && value <= numbers.Length ? numbers[value - 1] : value.ToString();
        }

        public static void TubePostfix(TorpedoTubeActivator __instance, ref string __result)
        {
            try
            {
                string current = __instance.Launcher == null ? null : Current(__instance.Launcher.Torpedo);
                if (current != null) __result += "\n" + current;
            }
            catch (Exception ex) { Warn("physical tube tooltip", ex); }
        }
        public static void ManualLeverPostfix(UseTorpedoLauncherLaunchLever __instance, ref string __result)
        {
            try
            {
                Component activator = ManualActivatorField.GetValue(__instance) as Component;
                TorpedoLauncherLaunchLever lever = activator == null ? null : activator.GetComponent<TorpedoLauncherLaunchLever>();
                string current = lever == null || lever.Launcher == null ? null : Current(lever.Launcher.Torpedo);
                if (current != null) __result += "\n\n" + current;
            }
            catch (Exception ex) { Warn("manual launch tooltip", ex); }
        }
        public static void AllLeversPostfix(UseTorpedoLaunchLever __instance, ref string __result)
        {
            try
            {
                object ship = AllPlayerField.GetValue(null);
                Array launchers = ship == null ? null : AllLaunchersProperty.GetValue(ship, null) as Array;
                if (launchers == null) return;
                StringBuilder text = new StringBuilder();
                foreach (object entry in launchers)
                {
                    ExtendedTorpedoLauncher launcher = entry as ExtendedTorpedoLauncher;
                    if (launcher == null || !launcher.IsMarkedForLaunch || !launcher.IsLoaded) continue;
                    string current = Current(launcher.Torpedo);
                    if (current != null) text.Append("\nTube ").Append(Roman(launcher.LauncherIndex + 1)).Append(": ").Append(current);
                }
                if (text.Length != 0) __result += "\n\n" + Marker + text;
            }
            catch (Exception ex) { Warn("manual salvo tooltip", ex); }
        }

        public static void StorageDescriptionPostfix(StorageUI __instance, Equipment __0)
        {
            try
            {
                ViewState state = Views.GetOrCreateValue(__instance);
                state.Item = __0 == null ? null : new WeakReference(__0);
                state.UnitKey = UnitKey();
            }
            catch (Exception ex) { Warn("inventory tracking", ex); }
        }
        public static void StorageUpdated(StorageUI __instance)
        {
            try
            {
                if (!__instance.IsOpened) return;
                ViewState state;
                if (!Views.TryGetValue(__instance, out state) || Time.unscaledTime < state.NextUpdate) return;
                state.NextUpdate = Time.unscaledTime + 0.25f;
                string key = UnitKey();
                if (state.UnitKey == key) return;
                Equipment item = state.Item == null ? null : state.Item.Target as Equipment;
                if (item == null || GetSpec(item.Data == null ? item.TypeName : item.Data.Name) == null) return;
                AccessTools.Method(typeof(StorageUI), "SetDescription").Invoke(__instance, new object[] { item });
                state.UnitKey = key;
            }
            catch (Exception ex) { Warn("inventory refresh", ex); }
        }

        private static void Warn(string area, Exception ex)
        {
            ModDiagnostics.Error(area, ex);
        }
    }
}
