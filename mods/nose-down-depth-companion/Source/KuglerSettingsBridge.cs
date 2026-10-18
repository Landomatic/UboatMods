// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Optional config controls for Kugler's separate mods (https://github.com/Isamylov).
// No upstream source, manifests, or licenses are edited or bundled.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using HarmonyLib;

namespace NoseDownCompanion
{
    internal static class KuglerSettingsBridge
    {
        private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static bool Loaded(KuglerMod mod)
        {
            // A saved cfg or a compiled assembly alone is NOT evidence of an active mod.
            try { return Harmony.HasAnyPatches(mod.HarmonyId); }
            catch { return false; }
        }

        private static object RuntimeConfig(KuglerMod mod)
        {
            if (!Loaded(mod)) return null;
            object result = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type owner = assembly.GetType(mod.OwnerType, false);
                    if (owner == null) continue;
                    FieldInfo field = owner.GetField("Config", StaticMembers);
                    PropertyInfo property = owner.GetProperty("Config", StaticMembers);
                    object candidate = field != null ? field.GetValue(null)
                        : property != null && property.GetIndexParameters().Length == 0 ? property.GetValue(null, null) : null;
                    if (candidate == null) continue;
                    if (result != null) return null;
                    result = candidate;
                }
                catch { /* An optional or incompatible mod must not break our menu. */ }
            }
            return result;
        }

        private static FieldInfo RuntimeField(object config, KuglerSwitch setting)
        {
            FieldInfo field = config != null ? config.GetType().GetField(setting.Field, InstanceMembers) : null;
            return field != null && field.FieldType == typeof(bool) && !field.IsInitOnly && !field.IsLiteral ? field : null;
        }

        internal static bool LegacyRuntime(KuglerMod mod, string newField)
        {
            object config = RuntimeConfig(mod);
            return config != null && config.GetType().GetField(newField, InstanceMembers) == null;
        }

        private static FieldInfo NumberField(object config, KuglerNumber setting)
        {
            FieldInfo field = config != null ? config.GetType().GetField(setting.Field, InstanceMembers) : null;
            return field != null && field.FieldType == typeof(float) ? field : null;
        }

        private static string ConfigPath(KuglerMod mod)
        {
            return Path.Combine(Application.persistentDataPath, mod.FileName);
        }

        internal static string Description(KuglerMod mod)
        {
            string status = Loaded(mod) ? "LOADED: runtime patches detected. "
                : "NOT LOADED / NO ACTIVE PATCHES DETECTED. Saved settings only; enable the mod in the launcher and restart to use it. ";
            return status + mod.Name + " by Kugler. This is a settings host, not a launcher switch. "
                + "Status rows are informational; they cannot enable a mod. Other ON/OFF rows are SAVED config values. "
                + "Supported source version " + mod.Version + ". Older loaded config contracts use the legacy menu; with no readable runtime the latest supported menu is shown. Values save to " + mod.FileName
                + ". Restart UBOAT to apply rows marked restart. The casing fix applies live only when Proper Ballast is loaded. "
                + "Reset restores only this mod's saved switches; numeric settings are kept. Patch detection does not certify every feature is working.";
        }

        private static object[] Status(string key, string label, string help, Func<bool> get)
        {
            // Dusty's contract has no read-only kind. Ignore host writes/replayed values;
            // every getter reads current evidence, never the host's persisted status copy.
            return new object[] { "bool", key, label + " (status only)", help,
                new Func<object>(() => get()), new Action<object>(value => { }) };
        }

        internal static bool RestartPending(KuglerMod mod)
        {
            object config = RuntimeConfig(mod);
            if (config == null) return false;
            foreach (KuglerSwitch setting in mod.Switches)
            {
                FieldInfo field = RuntimeField(config, setting);
                if (field != null && (bool)field.GetValue(config) != Read(mod, setting)) return true;
            }
            foreach (KuglerNumber setting in mod.Numbers)
            {
                FieldInfo field = NumberField(config, setting);
                if (field != null && Math.Abs((float)field.GetValue(config) - ReadNumber(mod, setting)) > 0.0001f) return true;
            }
            return false;
        }

        internal static object[] Rows(KuglerMod mod)
        {
            object config = RuntimeConfig(mod);
            var rows = new List<object>();
            rows.Add(Status("sessionLoaded", "Mod loaded this session",
                "ON means this mod's runtime patches are detected. OFF means no patches detected: the mod may be disabled, absent, or failed to initialize. This is not an enable switch. Change the launcher mod list and restart UBOAT.", () => Loaded(mod)));
            rows.Add(Status("restartPending", "Saved settings differ from this session",
                "ON means readable current settings differ from the saved values below. Restart UBOAT to apply them. OFF does not imply the mod is loaded; check the loaded status above.", () => RestartPending(mod)));
            if (mod == KuglerSwitchCatalog.Seakeeping)
                rows.Add(Status("sessionCorrections", "Seakeeping corrections enabled this session",
                    "Current runtime master setting, separate from the saved master below. OFF when Seakeeping is not loaded. Other features have their own switches; this does not certify every correction is working.", () =>
                    {
                        object current = RuntimeConfig(mod);
                        FieldInfo master = RuntimeField(current, mod.Switches[0]);
                        return master != null && (bool)master.GetValue(current);
                    }));
            if (config == null && !File.Exists(ConfigPath(mod))) return rows.ToArray();
            foreach (KuglerSwitch setting in mod.Switches)
            {
                KuglerSwitch captured = setting;
                bool live = setting.Live && RuntimeField(config, setting) != null;
                string apply = live ? "Applies live when loaded. " : "Saved now; restart UBOAT to apply. ";
                string state = Loaded(mod) ? "" : "Mod not loaded: saved settings only. ";
                rows.Add(new object[] { "bool", setting.Key, "Saved: " + setting.Label + (live ? " (live)" : " (restart)"), state + apply + setting.Help,
                    new Func<object>(() => Read(mod, captured)),
                    new Action<object>(value => Write(mod, captured, Convert.ToBoolean(value, CultureInfo.InvariantCulture))) });
            }
            foreach (KuglerNumber setting in mod.Numbers)
            {
                KuglerNumber captured = setting;
                rows.Add(new object[] { "float", setting.Key, "Saved: " + setting.Label + " (restart)",
                    (Loaded(mod) ? "" : "Mod not loaded: saved settings only. ") + setting.Help,
                    new Func<object>(() => ReadNumber(mod, captured)),
                    new Action<object>(value => WriteNumber(mod, captured, Convert.ToSingle(value, CultureInfo.InvariantCulture))),
                    setting.Min, setting.Max, setting.Step });
            }
            return rows.ToArray();
        }

        internal static float ReadNumber(KuglerMod mod, KuglerNumber setting)
        {
            object config = RuntimeConfig(mod);
            FieldInfo field = NumberField(config, setting);
            float value = field != null ? (float)field.GetValue(config) : setting.Default;
            if (float.IsNaN(value) || float.IsInfinity(value)) value = setting.Default;
            string path = ConfigPath(mod);
            if (File.Exists(path)) foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw;
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                int equal = line.IndexOf('=');
                float parsed;
                if (equal > 0 && string.Equals(line.Substring(0, equal).Trim(), setting.Key, StringComparison.OrdinalIgnoreCase)
                    && float.TryParse(line.Substring(equal + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                    && !float.IsNaN(parsed) && !float.IsInfinity(parsed)) value = parsed;
            }
            return Math.Max(setting.Min, Math.Min(setting.Max, value));
        }

        internal static void WriteNumber(KuglerMod mod, KuglerNumber setting, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            value = Math.Max(setting.Min, Math.Min(setting.Max, value));
            string path = ConfigPath(mod);
            string original = File.Exists(path) ? File.ReadAllText(path) : "";
            string updated = UpdateValueText(original, mod, setting.Key, new string[0], value.ToString("R", CultureInfo.InvariantCulture));
            if (updated != original) SaveAtomically(path, updated);
            Debug.Log(CompanionLoader.Tag + " " + mod.Name + " " + setting.Key + "=" + value.ToString(CultureInfo.InvariantCulture)
                + " (saved, restart required)");
        }

        private static bool Matches(KuglerSwitch setting, string key)
        {
            if (string.Equals(setting.Key, key, StringComparison.OrdinalIgnoreCase)) return true;
            foreach (string alias in setting.Aliases)
                if (string.Equals(alias, key, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static bool Read(KuglerMod mod, KuglerSwitch setting)
        {
            object config = RuntimeConfig(mod);
            FieldInfo field = RuntimeField(config, setting);
            bool value = field != null ? (bool)field.GetValue(config) : setting.Default;
            string path = ConfigPath(mod);
            if (!File.Exists(path)) return value;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw;
                if (mod.InlineComments)
                {
                    int hash = line.IndexOf('#');
                    if (hash >= 0) line = line.Substring(0, hash);
                }
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int equal = line.IndexOf('=');
                bool parsed;
                if (equal > 0 && Matches(setting, line.Substring(0, equal).Trim())
                    && bool.TryParse(line.Substring(equal + 1).Trim(), out parsed)) value = parsed;
            }
            return value;
        }

        internal static void Write(KuglerMod mod, KuglerSwitch setting, bool enabled)
        {
            object config = RuntimeConfig(mod);
            FieldInfo field = setting.Live ? RuntimeField(config, setting) : null;
            bool previous = field != null && (bool)field.GetValue(config);
            string path = ConfigPath(mod);
            string original = File.Exists(path) ? File.ReadAllText(path) : "";
            string updated = UpdateText(original, mod, setting, enabled);
            if (field != null) field.SetValue(config, enabled);
            try
            {
                if (updated != original) SaveAtomically(path, updated);
            }
            catch
            {
                if (field != null) field.SetValue(config, previous);
                throw;
            }
            Debug.Log(CompanionLoader.Tag + " " + mod.Name + " " + setting.Key + "=" + enabled
                + (field != null ? " (saved, live)" : " (saved, restart required)"));
        }

        internal static string UpdateText(string text, KuglerMod mod, KuglerSwitch setting, bool enabled)
        {
            return UpdateValueText(text, mod, setting.Key, setting.Aliases, enabled ? "true" : "false");
        }

        private static string UpdateValueText(string text, KuglerMod mod, string key, string[] aliases, string value)
        {
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var keys = new List<string> { Regex.Escape(key) };
            foreach (string alias in aliases) keys.Add(Regex.Escape(alias));
            bool found = false;
            string updated = Regex.Replace(text,
                @"^([ \t]*(?:" + string.Join("|", keys.ToArray()) + @")[ \t]*=[ \t]*)[^#\r\n]*([^\r\n]*)",
                match =>
                {
                    found = true;
                    string comment = match.Groups[2].Value;
                    if (comment.Length > 0 && !mod.InlineComments)
                        return comment + newline + match.Groups[1].Value + value;
                    return match.Groups[1].Value + value + (comment.Length > 0 ? " " + comment : "");
                }, RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (found) return updated;
            return text + (text.Length > 0 && !text.EndsWith("\n") ? newline : "") + key + "=" + value + newline;
        }

        private static void SaveAtomically(string path, string text)
        {
            string temporary = path + ".companion-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        internal static void Reset(KuglerMod mod)
        {
            if (RuntimeConfig(mod) == null && !File.Exists(ConfigPath(mod))) return;
            foreach (KuglerSwitch setting in mod.Switches) Write(mod, setting, setting.Default);
        }
    }
}
