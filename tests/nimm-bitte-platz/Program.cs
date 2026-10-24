// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. Executes mod source against offline test doubles.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UBOAT.Mods.HydrophoneSurfaceNoiseFix;
using UBOAT.Game.Scene.Audio;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Camera.ExternalControllers;
using UBOAT.Game.Scene.Entities;

internal static class Program
{
    private static int checks;
    private static int scenarios;
    private static string root;
    private static readonly BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private sealed class Station
    {
        internal PlayerShip Ship = new PlayerShip();
        internal DeepAudioListener Listener = new DeepAudioListener();
        internal MainCamera Camera = new MainCamera();
        internal GenericCameraController Generic = new GenericCameraController();
        internal HydrophoneController Controller = new HydrophoneController();
        internal Station()
        {
            Camera.AudioListener = Listener;
            Generic.MainCamera = Camera;
            Generic.ExternalController = Controller;
            Controller.ListeningRoom = new ListeningRoom { ParentEntity = Ship };
        }
        internal void Start() { HydrophoneFixState.Start(Controller, new object[] { Generic }); }
    }
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("FAILED: " + message);
    }
    private static void Set(Type type, string name, object value) { type.GetField(name, StaticPrivate).SetValue(null, value); }
    private static object Get(Type type, string name) { return type.GetField(name, StaticPrivate).GetValue(null); }
    private static int Logged(string text) { return Debug.Messages.FindAll(delegate(string line) { return line.Contains(text); }).Count; }
    private static void Reset()
    {
        try { HydrophoneFixState.Shutdown(); } catch { }
        Set(typeof(ModSettings), "loaded", false);
        Set(typeof(ModSettings), "enabled", true);
        Set(typeof(ModSettings), "level", DiagnosticLevel.Info);
        Set(typeof(ModSettings), "traceInterval", 2f);
        Set(typeof(ModDiagnostics), "begun", false);
        Set(typeof(ModDiagnostics), "stopped", false);
        Set(typeof(ModDiagnostics), "session", null);
        Set(typeof(ModDiagnostics), "errors", 0);
        Set(typeof(ModDiagnostics), "repeatedErrors", 0);
        ((HashSet<string>)Get(typeof(ModDiagnostics), "errorKeys")).Clear();
        ((Dictionary<string, float>)Get(typeof(ModDiagnostics), "nextSamples")).Clear();
        ((Dictionary<string, int>)Get(typeof(ModDiagnostics), "counters")).Clear();
        ModDiagnostics.Shutdown = null;
        Set(typeof(HydrophoneSurfaceNoiseFixLoader), "patched", false);
        Harmony.ThrowPatch = false;
        Harmony.ThrowRollback = false;
        Harmony.PatchCalls = 0;
        Harmony.RolledBackId = "";
        GameObject.Created = 0;
        UnityEngine.Object.FindCalls = 0;
        Time.unscaledTime = 0f;
        Time.realtimeSinceStartup = 0f;
        Debug.Messages.Clear();
        Application.persistentDataPath = Path.Combine(root, scenarios.ToString());
        Directory.CreateDirectory(Application.persistentDataPath);
    }
    private static void Run(string name, Action test)
    {
        scenarios++;
        Reset();
        test();
        Console.WriteLine("PASS " + name);
    }
    private static void Guard(string name, Action<Station> breakGuard)
    {
        Run(name, delegate
        {
            Station s = new Station(); s.Start();
            Check(s.Listener.IsInsideBoat, "valid owner acquires before guard loss");
            breakGuard(s);
            HydrophoneFixState.Maintain(s.Controller);
            Check(!s.Listener.IsInsideBoat, "guard loss releases the listener flag");
        });
    }
    public static int Main()
    {
        root = Path.Combine(Path.GetTempPath(), "NimmBittePlatzTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            Run("fresh defaults and config", delegate
            {
                Check(ModSettings.Enabled, "correction defaults on");
                Check(ModSettings.Level == DiagnosticLevel.Info, "Info default");
                Check(ModSettings.TraceInterval == 2f, "two-second default");
                Check(File.ReadAllText(ModSettings.ConfigPath).Contains("LogLevel = Info"), "default config written");
                Check(!ModDiagnostics.SampleDue("HYDROPHONE"), "Info never samples");
            });
            Run("legacy DIAG1 config", delegate
            {
                File.WriteAllText(ModSettings.ConfigPath, "# old config\nLogLevel = Trace\nTraceIntervalSeconds = 5\n");
                Check(ModSettings.Enabled, "missing Enabled retains correction");
                Check(ModSettings.Level == DiagnosticLevel.Trace && ModSettings.TraceInterval == 5f, "explicit legacy diagnostics retained");
            });
            Run("invalid config and finite interval", delegate
            {
                File.WriteAllText(ModSettings.ConfigPath, "Enabled = invalid\nLogLevel = 99\nTraceIntervalSeconds = NaN\nTraceIntervalSeconds = Infinity\n");
                Check(ModSettings.Enabled && ModSettings.Level == DiagnosticLevel.Info && ModSettings.TraceInterval == 2f, "invalid input retains defaults");
                Check(Logged("[WARN]") == 4, "invalid values reported");
            });
            Run("disabled config and interval clamp", delegate
            {
                File.WriteAllText(ModSettings.ConfigPath, "Enabled = false\nLogLevel = debug\nTraceIntervalSeconds = 0\n");
                Station s = new Station(); s.Start();
                Check(!s.Listener.IsInsideBoat, "disabled config prevents acquisition");
                Check(ModSettings.Level == DiagnosticLevel.Debug && ModSettings.TraceInterval == 0.25f, "case and low bound supported");
                ModSettings.SetTraceInterval(1000f);
                Check(ModSettings.TraceInterval == 60f, "high bound clamped");
                ModSettings.SetTraceInterval(float.NaN);
                Check(ModSettings.TraceInterval == 60f, "nonfinite menu value ignored");
            });
            Run("Dusty provider contract and live controls", delegate
            {
                Type provider = typeof(CommunityPatchSettings);
                Check(provider.Name == "CommunityPatchSettings" && provider.IsPublic, "provider discoverable");
                Check(CommunityPatchSettings.ModName == "Nimm Bitte Platz" && CommunityPatchSettings.Category == "Audio", "stable menu identity");
                object[] rows = (object[])provider.GetMethod("GetSettings").Invoke(null, null);
                Check(rows.Length == 3, "three setting rows");
                HashSet<string> keys = new HashSet<string>();
                foreach (object item in rows)
                {
                    object[] row = (object[])item;
                    Check(row.Length >= 6 && row[4] is Func<object> && row[5] is Action<object>, "row delegates match contract");
                    Check(keys.Add((string)row[1]) && ((string)row[1]).IndexOf('.') < 0, "unique simple keys");
                }
                Station s = new Station(); s.Start();
                ((Action<object>)((object[])rows[0])[5])(false);
                Check(!s.Listener.IsInsideBoat && !ModSettings.Enabled, "menu disable releases immediately");
                ((Action<object>)((object[])rows[0])[5])(true);
                HydrophoneFixState.Maintain(s.Controller);
                Check(s.Listener.IsInsideBoat, "reenable reacquires during same healthy session");
                ((Action<object>)((object[])rows[1])[5])(2);
                ((Action<object>)((object[])rows[2])[5])(3.25f);
                Check(ModSettings.Level == DiagnosticLevel.Trace && ModSettings.TraceInterval == 3.25f, "menu changes live");
                string config = File.ReadAllText(ModSettings.ConfigPath);
                Check(config.Contains("LogLevel = Trace") && config.Contains("TraceIntervalSeconds = 3.25"), "menu changes persist");
                CommunityPatchSettings.ResetToDefaults();
                Check(ModSettings.Enabled && ModSettings.Level == DiagnosticLevel.Info && ModSettings.TraceInterval == 2f, "reset restores defaults");
                Check(!ModDiagnostics.SampleDue("HYDROPHONE"), "reset stops trace immediately");
            });
            Run("unchanged menu values do not rewrite config", delegate
            {
                ModSettings.EnsureLoaded();
                File.WriteAllText(ModSettings.ConfigPath, "# sentinel\nLogLevel = Info\n");
                ModSettings.SetEnabled(true); ModSettings.SetLevel(0); ModSettings.SetTraceInterval(2f);
                Check(File.ReadAllText(ModSettings.ConfigPath).StartsWith("# sentinel"), "unchanged setters do not write");
            });
            Run("trace sampling and live transition to Info", delegate
            {
                ModSettings.SetLevel(2);
                Check(ModDiagnostics.SampleDue("HYDROPHONE"), "first trace due");
                Time.unscaledTime = 1.99f;
                Check(!ModDiagnostics.SampleDue("HYDROPHONE"), "trace throttled");
                Time.unscaledTime = 2f;
                Check(ModDiagnostics.SampleDue("HYDROPHONE"), "next trace due at boundary");
                ModSettings.SetLevel(0);
                Check(!ModDiagnostics.SampleDue("HYDROPHONE"), "Info disables samples live");
            });
            Guard("surfacing releases", delegate(Station s) { s.Ship.Submerged = false; });
            Guard("view change releases", delegate(Station s) { s.Camera.CurrentMode = CameraMode.FPP; });
            Guard("audio camera ownership releases", delegate(Station s) { s.Camera.IsAudioListener = true; });
            Guard("hydrophone ownership loss releases", delegate(Station s) { s.Listener.IsHydrophone = false; });
            Guard("external controller change releases", delegate(Station s) { s.Generic.ExternalController = new HydrophoneController(); });
            Run("normal stop and nonowner callbacks", delegate
            {
                Station s = new Station(); s.Start();
                HydrophoneFixState.Stop(new HydrophoneController());
                Check(s.Listener.IsInsideBoat, "another controller cannot stop owner");
                HydrophoneFixState.Stop(s.Controller);
                Check(!s.Listener.IsInsideBoat && !(bool)Get(typeof(HydrophoneFixState), "active"), "normal stop releases and deactivates");
            });
            Run("preexisting inside flag is not owned", delegate
            {
                Station s = new Station(); s.Listener.IsInsideBoat = true; s.Start();
                HydrophoneFixState.Stop(s.Controller);
                Check(s.Listener.IsInsideBoat, "preexisting true flag preserved");
            });
            Run("external flag update is respected", delegate
            {
                Station s = new Station(); s.Start(); s.Listener.IsInsideBoat = false;
                int before = s.Listener.Writes;
                HydrophoneFixState.Stop(s.Controller);
                Check(!s.Listener.IsInsideBoat && s.Listener.Writes == before, "already released flag not overwritten");
            });
            Run("repeated start releases prior station", delegate
            {
                Station old = new Station(); old.Start(); Station next = new Station(); next.Start();
                Check(!old.Listener.IsInsideBoat && next.Listener.IsInsideBoat, "override transfers through release");
            });
            Run("station failure stops repeated intervention", delegate
            {
                Station s = new Station(); s.Listener.ThrowNextWrite = true; s.Start();
                int writes = s.Listener.Writes;
                HydrophoneFixState.Maintain(s.Controller);
                Check(!s.Listener.IsInsideBoat && s.Listener.Writes == writes, "failure stops further writes in session");
                s.Start();
                Check(s.Listener.IsInsideBoat, "fresh session can recover");
            });
            Run("disable still releases when saving fails", delegate
            {
                Station s = new Station(); s.Start();
                File.Delete(ModSettings.ConfigPath); Directory.CreateDirectory(ModSettings.ConfigPath);
                ModSettings.SetEnabled(false);
                Check(!ModSettings.Enabled && !s.Listener.IsInsideBoat, "release precedes failed config save");
                Check(Logged("area=config-save") == 1, "save failure reported");
            });
            Run("initialization and lifecycle are installed once", delegate
            {
                HydrophoneSurfaceNoiseFixLoader loader = new HydrophoneSurfaceNoiseFixLoader();
                loader.OnLoaded(); loader.OnLoaded();
                Check(Harmony.PatchCalls == 1 && GameObject.Created == 1, "one patch installation and lifecycle host");
                Check(Logged("[READY]") == 1 && Logged("version=1.4.0") == 2, "version and ready markers");
                Check(typeof(DiagnosticsLifecycle).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic) == null, "no lifecycle frame callback");
            });
            Run("failed installation rolls back without READY", delegate
            {
                Harmony.ThrowPatch = true;
                new HydrophoneSurfaceNoiseFixLoader().OnLoaded();
                Check(Harmony.RolledBackId == "Landomatic.HydrophoneSurfaceNoiseFix", "only own patches rolled back");
                Check(Logged("[READY]") == 0 && Logged("stationPatchRollback=True") == 1, "failure never says ready");
                Harmony.ThrowPatch = false;
                new HydrophoneSurfaceNoiseFixLoader().OnLoaded();
                Check(Logged("[READY]") == 1 && GameObject.Created == 1, "retry succeeds without extra lifecycle host");
            });
            Run("rollback failure remains visible", delegate
            {
                Harmony.ThrowPatch = true; Harmony.ThrowRollback = true;
                new HydrophoneSurfaceNoiseFixLoader().OnLoaded();
                Check(Logged("area=installation-rollback") == 1 && Logged("stationPatchRollback=False") == 1, "rollback failure accurately reported");
                Check(Logged("[READY]") == 0, "rollback failure not ready");
            });
            Run("shutdown restores listener and runs once", delegate
            {
                new HydrophoneSurfaceNoiseFixLoader().OnLoaded();
                Station s = new Station(); s.Start();
                ModDiagnostics.Stop("application-quit"); ModDiagnostics.Stop("lifecycle-destroyed");
                Check(!s.Listener.IsInsideBoat && Get(typeof(HydrophoneFixState), "listener") == null, "shutdown releases and clears references");
                Check(Logged("[STOP]") == 1, "one STOP for both lifecycle callbacks");
            });
            Run("shutdown clears state even if restoration throws", delegate
            {
                new HydrophoneSurfaceNoiseFixLoader().OnLoaded(); Station s = new Station(); s.Start();
                s.Listener.ThrowNextWrite = true; ModDiagnostics.Stop("application-quit");
                Check(!(bool)Get(typeof(HydrophoneFixState), "active") && Get(typeof(HydrophoneFixState), "listener") == null, "finally clears failed shutdown state");
                Check(Logged("area=shutdown") == 1 && Logged("[STOP]") == 1, "shutdown error and summary recorded");
            });
            Run("repeated errors are counted without log spam", delegate
            {
                for (int i = 0; i < 5; i++) ModDiagnostics.Error("test", new InvalidOperationException("same failure"));
                ModDiagnostics.Stop("test");
                Check(Logged("area=test") == 1, "one full error");
                Check(Logged("errors=5 repeatedErrors=4") == 1, "summary counts repeats");
            });
            Run("hydrophone updates do not reread config or search scene", delegate
            {
                Station s = new Station(); s.Start();
                File.WriteAllText(ModSettings.ConfigPath, "Enabled = false\nLogLevel = Trace\n");
                for (int i = 0; i < 1000; i++) HydrophoneFixState.Maintain(s.Controller);
                Check(ModSettings.Enabled && ModSettings.Level == DiagnosticLevel.Info, "hand edits wait for restart");
                Check(UnityEngine.Object.FindCalls == 0 && Logged("[HYDROPHONE]") == 0, "no scene search or trace output during Info updates");
            });
            Console.WriteLine("PASS: " + scenarios + " scenarios, " + checks + " assertions. Stub-reference compilation only; in-game validation remains required.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
