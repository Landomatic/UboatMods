// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source/history: https://github.com/Landomatic/UboatMods/tree/main/mods/nimm-bitte-platz
using System;
using HarmonyLib;
using UnityEngine;
using UBOAT.Game;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Scene.Audio;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Camera.ExternalControllers;
using UBOAT.Game.Scene.Entities;

namespace UBOAT.Mods.HydrophoneSurfaceNoiseFix
{
    internal static class ModInfo
    {
        internal const string Version = "1.4.2";
        internal const string Build = "20261024.3";
    }

    [NonSerializedInGameState]
    public class HydrophoneSurfaceNoiseFixLoader : IUserMod
    {
        private static bool patched;

        public void OnLoaded()
        {
            if (patched) return;
            ModDiagnostics.Begin();
            Harmony harmony = new Harmony("Landomatic.HydrophoneSurfaceNoiseFix");
            try
            {
                harmony.PatchAll(typeof(HydrophoneSurfaceNoiseFixLoader).Assembly);
                ModDiagnostics.Shutdown = HydrophoneFixState.Shutdown;
                patched = true;
                ModDiagnostics.Event("READY", "version=" + ModInfo.Version + " build=" + ModInfo.Build
                    + " stationHooks=3 correctionEnabled=" + ModSettings.Enabled);
            }
            catch (Exception ex)
            {
                patched = false;
                bool rolledBack = false;
                try { harmony.UnpatchAll("Landomatic.HydrophoneSurfaceNoiseFix"); rolledBack = true; }
                catch (Exception rollback) { ModDiagnostics.Error("installation-rollback", rollback); }
                ModDiagnostics.Error("installation", ex);
                ModDiagnostics.Event("FAILED", "stationPatchRollback=" + rolledBack);
            }
        }
    }

    internal static class HydrophoneFixState
    {
        private static HydrophoneController controller;
        private static GenericCameraController generic;
        private static MainCamera camera;
        private static DeepAudioListener listener;
        private static PlayerShip ship;
        private static bool active;
        private static bool failed;
        private static bool ownsOverride;
        private static bool previousInsideBoat;

        internal static void Start(HydrophoneController instance, object[] args)
        {
            try
            {
                // A repeated start must not carry a previous override into a new session.
                ReleaseOverride("new session");
                controller = instance;
                generic = args != null && args.Length > 0
                    ? args[0] as GenericCameraController : null;
                camera = generic == null ? null : generic.MainCamera;
                listener = camera == null ? null : camera.AudioListener;
                ship = instance.ListeningRoom == null ? null
                    : instance.ListeningRoom.ParentEntity as PlayerShip;
                // Only a session-start fallback; never search the scene on each frame.
                if (ship == null) ship = UnityEngine.Object.FindObjectOfType<PlayerShip>();
                active = true;
                failed = false;
                ModDiagnostics.Count("stationEntries");
                ModDiagnostics.Event("STATION-START", "ship=\"" + (ship == null ? "null" : ship.name)
                    + "\" generic=" + (generic != null) + " listener=" + (listener != null));
                Maintain(instance);
            }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void Maintain(HydrophoneController instance)
        {
            if (!active || failed || !object.ReferenceEquals(instance, controller)) return;
            try
            {
                if (!ModSettings.Enabled)
                {
                    ReleaseOverride("disabled in settings");
                    return;
                }
                // The known game mode for an external station is Generic (16).
                // Requiring the actual active controller and audio ownership avoids
                // applying this to ordinary FPP, other stations, map, or exterior views.
                bool guarded = camera != null && listener != null && ship != null &&
                    generic != null && (int)camera.CurrentMode == 16 &&
                    object.ReferenceEquals(generic.ExternalController, controller) &&
                    !camera.IsAudioListener && listener.IsHydrophone && ship.Submerged;

                if (ModDiagnostics.SampleDue("HYDROPHONE"))
                    ModDiagnostics.Event("HYDROPHONE", "guarded=" + guarded + " ownsOverride=" + ownsOverride
                        + " depth_m=" + (ship == null ? "null" : ModDiagnostics.F(ship.DeckDepth))
                        + " submerged=" + (ship != null && ship.Submerged)
                        + " mode=" + (camera == null ? "null" : ((int)camera.CurrentMode).ToString())
                        + " cameraOwnsAudio=" + (camera != null && camera.IsAudioListener)
                        + " listenerHydrophone=" + (listener != null && listener.IsHydrophone)
                        + " listenerInside=" + (listener != null && listener.IsInsideBoat));

                if (!guarded)
                {
                    ReleaseOverride("surface or ownership guard");
                    return;
                }

                if (!listener.IsInsideBoat)
                {
                    // Change only this listener flag. MainCamera.UpdateCommonTasks
                    // already calculates the normal inside-boat depth from DeckDepth.
                    // Do not call it here, relocate the listener, or assign Depth.
                    if (!ownsOverride) previousInsideBoat = listener.IsInsideBoat;
                    listener.IsInsideBoat = true;
                    if (!ownsOverride)
                    {
                        ownsOverride = true;
                        ModDiagnostics.Count("overrideAcquires");
                        ModDiagnostics.Detail("OVERRIDE-ACQUIRE", "previousInside=" + previousInsideBoat);
                    }
                }
            }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void Stop(HydrophoneController instance)
        {
            if (!active || !object.ReferenceEquals(instance, controller)) return;
            try
            {
                // Run BEFORE vanilla StopController. Vanilla then restores camera
                // ownership and writes the next owner's inside-boat flag itself.
                ReleaseOverride("normal controller stop");
            }
            catch (Exception ex) { Fail(ex); }
            finally
            {
                ModDiagnostics.Count("stationExits");
                ModDiagnostics.Event("STATION-STOP", "failed=" + failed);
                active = false;
                controller = null;
                generic = null;
                camera = null;
                listener = null;
                ship = null;
            }
        }

        private static void ReleaseOverride(string reason)
        {
            if (!ownsOverride) return;
            ownsOverride = false;
            // If another system already changed the flag, respect that new value.
            if (listener != null && listener.IsInsideBoat)
                listener.IsInsideBoat = previousInsideBoat;
            ModDiagnostics.Count("overrideReleases");
            ModDiagnostics.Detail("OVERRIDE-RELEASE", "reason=" + reason + " restoredInside=" + previousInsideBoat);
        }

        private static void Fail(Exception ex)
        {
            ModDiagnostics.Error("hydrophone-session", ex);
            failed = true;
            // Fail closed for this session rather than running a failing per-frame repair.
            try { ReleaseOverride("error"); } catch { ownsOverride = false; }
        }

        internal static void SettingsChanged()
        {
            if (ModSettings.Enabled) return;
            try { ReleaseOverride("disabled in settings"); }
            catch (Exception ex) { Fail(ex); }
        }

        internal static void Shutdown()
        {
            try { ReleaseOverride("application shutdown"); }
            finally
            {
                active = false;
                controller = null;
                generic = null;
                camera = null;
                listener = null;
                ship = null;
            }
        }
    }

    [HarmonyPatch(typeof(HydrophoneController), "StartController")]
    internal static class HydrophoneStartPatch
    {
        private static void Postfix(HydrophoneController __instance, object[] __args)
        { HydrophoneFixState.Start(__instance, __args); }
    }

    [HarmonyPatch(typeof(HydrophoneController), "UpdateController")]
    internal static class HydrophoneUpdatePatch
    {
        private static void Postfix(HydrophoneController __instance)
        { HydrophoneFixState.Maintain(__instance); }
    }

    [HarmonyPatch(typeof(HydrophoneController), "StopController")]
    internal static class HydrophoneStopPatch
    {
        private static void Prefix(HydrophoneController __instance)
        { HydrophoneFixState.Stop(__instance); }
    }

}
