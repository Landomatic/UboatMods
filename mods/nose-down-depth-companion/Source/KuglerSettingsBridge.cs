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

namespace NoseDownCompanion
{
    internal static class KuglerSettingsBridge
    {
        private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static object RuntimeConfig(KuglerMod mod)
        {
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

        private static string ConfigPath(KuglerMod mod)
        {
            return Path.Combine(Application.persistentDataPath, mod.FileName);
        }

        internal static string Description(KuglerMod mod)
        {
            return mod.Name + " by Kugler. Config switches hosted by Landomatic; supported source version " + mod.Version
                + ". Values save to " + mod.FileName + ". Restart UBOAT to apply rows marked restart. "
                + "The casing fix applies live when Proper Ballast is loaded. Reset restores only this mod's switches; numeric settings are kept. A menu cannot load a mod disabled in the launcher.";
        }

        internal static object[] Rows(KuglerMod mod)
        {
            object config = RuntimeConfig(mod);
            if (config == null && !File.Exists(ConfigPath(mod))) return new object[0];
            var rows = new List<object>();
            foreach (KuglerSwitch setting in mod.Switches)
            {
                KuglerSwitch captured = setting;
                bool live = setting.Live && RuntimeField(config, setting) != null;
                string apply = live ? "Applies live. " : "Saved now; restart UBOAT to apply. ";
                rows.Add(new object[] { "bool", setting.Key, setting.Label + (live ? "" : " (restart)"), apply + setting.Help,
                    new Func<object>(() => Read(mod, captured)),
                    new Action<object>(value => Write(mod, captured, Convert.ToBoolean(value, CultureInfo.InvariantCulture))) });
            }
            return rows.ToArray();
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
            string value = enabled ? "true" : "false";
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            var keys = new List<string> { Regex.Escape(setting.Key) };
            foreach (string alias in setting.Aliases) keys.Add(Regex.Escape(alias));
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
            return text + (text.Length > 0 && !text.EndsWith("\n") ? newline : "") + setting.Key + "=" + value + newline;
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
