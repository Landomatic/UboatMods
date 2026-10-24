// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source/history: https://github.com/Landomatic/UboatMods/tree/main/mods/nimm-bitte-platz
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UBOAT.Game.Core.Serialization;

namespace UBOAT.Mods.HydrophoneSurfaceNoiseFix
{
    [NonSerializedInGameState]
    internal static class ModDiagnostics
    {
        private static string session;
        private static bool begun;
        private static bool stopped;
        private static int errors;
        private static int repeatedErrors;
        private static readonly HashSet<string> errorKeys = new HashSet<string>();
        private static readonly Dictionary<string, float> nextSamples = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        internal static Action Shutdown;
        internal static bool DebugEnabled { get { return ModSettings.Level >= DiagnosticLevel.Debug; } }

        internal static void Begin()
        {
            if (begun) return;
            begun = true;
            Event("START", "name=NimmBittePlatz version=" + ModInfo.Version + " build=" + ModInfo.Build);
            ModSettings.EnsureLoaded();
            Event("CONFIG", "path=\"" + ModSettings.ConfigPath + "\" " + ModSettings.Summary);
            try
            {
                GameObject host = new GameObject("NimmBittePlatzLifecycle");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<DiagnosticsLifecycle>();
            }
            catch (Exception ex) { Error("lifecycle", ex); }
        }

        internal static string F(float value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
        private static string Prefix(string tag)
        {
            if (session == null) session = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            return "[HydrophoneSurfaceNoiseFix][" + tag + "] session=" + session + " t=" + F(Time.realtimeSinceStartup) + " ";
        }
        internal static void Event(string tag, string message) { UnityEngine.Debug.Log(Prefix(tag) + message); }
        internal static void Warn(string message) { UnityEngine.Debug.LogWarning(Prefix("WARN") + message); }
        internal static void Detail(string tag, string message)
        {
            if (DebugEnabled) Event(tag, message);
        }
        internal static bool SampleDue(string tag)
        {
            if (ModSettings.Level != DiagnosticLevel.Trace) return false;
            float next;
            if (nextSamples.TryGetValue(tag, out next) && Time.unscaledTime < next) return false;
            nextSamples[tag] = Time.unscaledTime + ModSettings.TraceInterval;
            return true;
        }
        internal static void SettingsChanged()
        {
            nextSamples.Clear();
            Event("CONFIG", "changed " + ModSettings.Summary);
        }
        internal static void Count(string key)
        {
            int count;
            counters.TryGetValue(key, out count);
            counters[key] = count + 1;
        }
        internal static void Error(string area, Exception ex)
        {
            errors++;
            if (!errorKeys.Add(area + ":" + ex.GetType().FullName)) { repeatedErrors++; return; }
            UnityEngine.Debug.LogError(Prefix("ERROR") + "area=" + area + " " + ex);
        }
        internal static void Stop(string reason)
        {
            if (stopped) return;
            stopped = true;
            try { if (Shutdown != null) Shutdown(); }
            catch (Exception ex) { Error("shutdown", ex); }
            finally { Shutdown = null; }
            List<string> summary = new List<string>();
            foreach (KeyValuePair<string, int> item in counters) summary.Add(item.Key + "=" + item.Value);
            summary.Sort(StringComparer.Ordinal);
            Event("STOP", "reason=" + reason + " errors=" + errors + " repeatedErrors=" + repeatedErrors
                + " " + string.Join(" ", summary.ToArray()));
        }
    }

    // Only lifecycle callbacks. No Update(), scene scans, timers, or config-file polling.
    [NonSerializedInGameState]
    public sealed class DiagnosticsLifecycle : MonoBehaviour
    {
        private void OnApplicationQuit() { ModDiagnostics.Stop("application-quit"); }
        private void OnDestroy() { ModDiagnostics.Stop("lifecycle-destroyed"); }
    }
}
