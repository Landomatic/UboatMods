// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UBOAT.Game.Core.Serialization;

namespace UBOAT.Mods.HydrophoneSurfaceNoiseFix
{
    [NonSerializedInGameState]
    internal static class ModDiagnostics
    {
        internal const string Build = "1.3.2-beta-DIAG1";
        private enum Level { Info, Debug, Trace }
        private static Level level = Level.Info;
        private static float interval = 2f;
        private static string session;
        private static bool stopped;
        private static int errors;
        private static int repeatedErrors;
        private static readonly HashSet<string> errorKeys = new HashSet<string>();
        private static readonly Dictionary<string, float> nextSamples = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        internal static Func<bool> DataProbe;
        internal static Action Shutdown;
        internal static bool DebugEnabled { get { return level >= Level.Debug; } }

        internal static void Begin()
        {
            session = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string path = Path.Combine(Application.persistentDataPath, "NimmBittePlatzDiagnostics.txt");
            Event("START", "build=" + Build);
            try
            {
                if (!File.Exists(path))
                    File.WriteAllText(path, "# Restart UBOAT after changes. Info, Debug, or Trace.\nLogLevel = Info\nTraceIntervalSeconds = 2\n");
                foreach (string row in File.ReadAllLines(path))
                {
                    string line = row.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;
                    int split = line.IndexOf('=');
                    if (split < 0) continue;
                    string key = line.Substring(0, split).Trim();
                    string value = line.Substring(split + 1).Trim();
                    if (key.Equals("LogLevel", StringComparison.OrdinalIgnoreCase))
                    {
                        Level parsed;
                        if (Enum.TryParse<Level>(value, true, out parsed) && Enum.IsDefined(typeof(Level), parsed)) level = parsed;
                        else Event("WARN", "Invalid LogLevel; keeping " + level);
                    }
                    if (key.Equals("TraceIntervalSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        float parsed;
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                            && !float.IsNaN(parsed) && !float.IsInfinity(parsed)) interval = Mathf.Clamp(parsed, 0.25f, 60f);
                        else Event("WARN", "Invalid TraceIntervalSeconds; keeping " + interval);
                    }
                }
            }
            catch (Exception ex) { Error("config", ex); }
            Event("CONFIG", "path=\"" + path + "\" level=" + level + " traceInterval=" + F(interval));
            try
            {
                GameObject host = new GameObject("NimmBittePlatzDiagnosticsHost");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<DiagnosticsLifecycle>();
            }
            catch (Exception ex) { Error("lifecycle", ex); }
        }

        internal static string F(float value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
        private static string Prefix(string tag)
        {
            return "[HydrophoneSurfaceNoiseFix][" + tag + "] session=" + session + " t=" + F(Time.realtimeSinceStartup) + " ";
        }
        internal static void Event(string tag, string message) { UnityEngine.Debug.Log(Prefix(tag) + message); }
        internal static void Detail(string tag, string message)
        {
            if (DebugEnabled) Event(tag, message);
        }
        internal static bool SampleDue(string tag)
        {
            if (level != Level.Trace) return false;
            float next;
            if (nextSamples.TryGetValue(tag, out next) && Time.unscaledTime < next) return false;
            nextSamples[tag] = Time.unscaledTime + interval;
            return true;
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
            DataProbe = null;
            Shutdown = null;
            List<string> summary = new List<string>();
            foreach (KeyValuePair<string, int> item in counters) summary.Add(item.Key + "=" + item.Value);
            summary.Sort(StringComparer.Ordinal);
            Event("STOP", "reason=" + reason + " errors=" + errors + " repeatedErrors=" + repeatedErrors
                + " " + string.Join(" ", summary.ToArray()));
        }
    }

    [NonSerializedInGameState]
    public sealed class DiagnosticsLifecycle : MonoBehaviour
    {
        private float nextProbe;
        private void Update()
        {
            if (ModDiagnostics.DataProbe == null || Time.unscaledTime < nextProbe) return;
            nextProbe = Time.unscaledTime + 2f;
            try
            {
                if (ModDiagnostics.DataProbe()) ModDiagnostics.DataProbe = null;
                else if (ModDiagnostics.SampleDue("DATA-WAIT")) ModDiagnostics.Event("DATA-WAIT", "waiting for paired equipment database");
            }
            catch (Exception ex) { ModDiagnostics.Error("data-probe", ex); ModDiagnostics.DataProbe = null; }
        }
        private void OnApplicationQuit() { ModDiagnostics.Stop("application-quit"); }
        private void OnDestroy() { ModDiagnostics.Stop("lifecycle-destroyed"); }
    }
}
