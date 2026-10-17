// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Optional settings bridge to Proper Ballast by Kugler (https://github.com/Isamylov).
// Proper Ballast remains a separate mod; its source is not included here.
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NoseDownCompanion
{
    internal static class ProperBallastSettings
    {
        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool TryGet(out object config, out FieldInfo option)
        {
            config = null;
            option = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type loader = assembly.GetType("BallastReworked.ModLoader", false);
                    FieldInfo configField = loader != null ? loader.GetField("Config", StaticFields) : null;
                    object candidate = configField != null ? configField.GetValue(null) : null;
                    FieldInfo field = candidate != null ? candidate.GetType().GetField("FixCasingFlooding", InstanceFields) : null;
                    if (field == null || field.FieldType != typeof(bool) || field.IsInitOnly || field.IsLiteral) continue;
                    // Refuse ambiguous duplicate installations instead of controlling the wrong one.
                    if (config != null) { config = null; option = null; return false; }
                    config = candidate;
                    option = field;
                }
                catch { /* Missing or incompatible optional mod: leave its settings alone. */ }
            }
            return config != null;
        }

        internal static bool Available
        {
            get { object config; FieldInfo option; return TryGet(out config, out option); }
        }

        internal static bool CasingFixEnabled
        {
            get
            {
                object config; FieldInfo option;
                return TryGet(out config, out option) && (bool)option.GetValue(config);
            }
        }

        internal static void SetCasingFix(bool enabled)
        {
            object config; FieldInfo option;
            if (!TryGet(out config, out option))
                throw new InvalidOperationException("Proper Ballast casing setting is unavailable.");
            bool previous = (bool)option.GetValue(config);
            if (previous == enabled) return;
            string path = Path.Combine(Application.persistentDataPath, "ProperBallastConfig.txt");
            option.SetValue(config, enabled);
            try
            {
                string original = File.Exists(path) ? File.ReadAllText(path) : "";
                string updated = UpdateText(original, enabled);
                SaveAtomically(path, updated);
            }
            catch
            {
                option.SetValue(config, previous);
                throw;
            }
            Debug.Log(CompanionLoader.Tag + " Proper Ballast fixCasingFlooding=" + enabled + " (saved)");
        }

        internal static string UpdateText(string text, bool enabled)
        {
            string value = enabled ? "true" : "false";
            bool found = false;
            string updated = Regex.Replace(text,
                @"^([ \t]*fixCasingFlooding[ \t]*=[ \t]*)[^#\r\n]*([^\r\n]*)",
                match =>
                {
                    found = true;
                    string comment = match.Groups[2].Value;
                    return match.Groups[1].Value + value + (comment.Length > 0 ? " " + comment : "");
                }, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (found) return updated;
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            return text + (text.Length > 0 && !text.EndsWith("\n") ? newline : "")
                + "fixCasingFlooding=" + value + newline;
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
    }
}
