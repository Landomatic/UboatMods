// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source/history: https://github.com/Landomatic/UboatMods/tree/main/mods/nimm-bitte-platz
using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UBOAT.Game.Core.Serialization;

namespace UBOAT.Mods.HydrophoneSurfaceNoiseFix
{
    internal enum DiagnosticLevel { Info, Debug, Trace }

    [NonSerializedInGameState]
    internal static class ModSettings
    {
        private static bool loaded;
        private static DiagnosticLevel level = DiagnosticLevel.Info;
        private static float traceInterval = 2f;

        internal static string ConfigPath
        {
            get { return Path.Combine(Application.persistentDataPath, "NimmBittePlatzDiagnostics.txt"); }
        }
        internal static DiagnosticLevel Level { get { EnsureLoaded(); return level; } }
        internal static float TraceInterval { get { EnsureLoaded(); return traceInterval; } }
        internal static string Summary
        {
            get { return "level=" + Level + " traceInterval=" + ModDiagnostics.F(TraceInterval); }
        }

        internal static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(ConfigPath)) { Save(); return; }
                foreach (string row in File.ReadAllLines(ConfigPath))
                {
                    string line = row.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;
                    int split = line.IndexOf('=');
                    if (split < 0) continue;
                    string key = line.Substring(0, split).Trim();
                    string value = line.Substring(split + 1).Trim();
                    // Legacy Enabled values are intentionally ignored. The launcher
                    // controls activation; diagnostics cannot disable the correction.
                    if (key.Equals("LogLevel", StringComparison.OrdinalIgnoreCase))
                    {
                        DiagnosticLevel parsed;
                        if (Enum.TryParse<DiagnosticLevel>(value, true, out parsed) && Enum.IsDefined(typeof(DiagnosticLevel), parsed)) level = parsed;
                        else ModDiagnostics.Warn("Invalid LogLevel; keeping " + level);
                    }
                    else if (key.Equals("TraceIntervalSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        float parsed;
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                            && !float.IsNaN(parsed) && !float.IsInfinity(parsed)) traceInterval = Mathf.Clamp(parsed, 0.25f, 60f);
                        else ModDiagnostics.Warn("Invalid TraceIntervalSeconds; keeping " + ModDiagnostics.F(traceInterval));
                    }
                }
            }
            catch (Exception ex) { ModDiagnostics.Error("config-read", ex); }
        }

        internal static void SetLevel(int value)
        {
            EnsureLoaded();
            if (value < 0 || value > 2) value = 0;
            DiagnosticLevel next = (DiagnosticLevel)value;
            if (level == next) return;
            level = next;
            Save();
            ModDiagnostics.SettingsChanged();
        }
        internal static void SetTraceInterval(float value)
        {
            EnsureLoaded();
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            value = Mathf.Clamp(value, 0.25f, 60f);
            if (traceInterval == value) return;
            traceInterval = value;
            Save();
            ModDiagnostics.SettingsChanged();
        }
        internal static void ResetToDefaults()
        {
            EnsureLoaded();
            if (level == DiagnosticLevel.Info && traceInterval == 2f) return;
            level = DiagnosticLevel.Info;
            traceInterval = 2f;
            Save();
            ModDiagnostics.SettingsChanged();
        }
        private static void Save()
        {
            try
            {
                File.WriteAllText(ConfigPath,
                    "# Nimm Bitte Platz: Hydrophone Fix settings.\n"
                    + "# Dusty's Other Mods menu applies and saves changes immediately.\n"
                    + "# Restart UBOAT after hand-editing this file.\n"
                    + "LogLevel = " + level + "\n"
                    + "TraceIntervalSeconds = " + traceInterval.ToString("0.###", CultureInfo.InvariantCulture) + "\n");
            }
            catch (Exception ex) { ModDiagnostics.Error("config-save", ex); }
        }
    }
}
