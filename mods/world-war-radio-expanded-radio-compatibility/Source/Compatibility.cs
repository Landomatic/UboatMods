// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic.
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game;
using UBOAT.Game.Core;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Core.Time;
using UBOAT.Game.Sandbox;
using UBOAT.Game.Sandbox.Messages;
using UnityEngine;

namespace WWRERMCompatibility
{
    [NonSerializedInGameState]
    public sealed class Loader : IUserMod
    {
        private const string PatchId = "landomatic.wwr-erm-compat";
        private const string ExpandedRadioOwner = "com.sqparadox.uboat.ExpandedRadio";
        private const string TimerPatchClass = "ExpandedRadio.Patches.SandboxEntityStartTransmissionPatch";
        private static bool installed;

        public void OnLoaded()
        {
            if (installed) return;
            var host = new GameObject("WWRERMCompatibilityBootstrap");
            host.hideFlags = HideFlags.DontSave;
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<CompatibilityBootstrap>();
        }

        internal static bool TryInstall()
        {
            if (installed) return true;
            if (!AccessTools.AllAssemblies().Any(a => a.GetName().Name == "3342143865"))
                return false;

            var start = AccessTools.DeclaredMethod(typeof(SandboxEntity), "StartTransmission", new[] { typeof(RadioTransmission) });
            if (start == null)
            {
                Debug.LogWarning("[WWRERMCompat] INACTIVE Game transmission start method unavailable.");
                return true;
            }

            var original = Harmony.GetPatchInfo(start);
            var radioHooks = original == null ? new Patch[0] : original.Postfixes.Where(IsTimerHook).ToArray();
            if (radioHooks.Length == 0)
                return false;
            if (!radioHooks.All(IsLegacyTimer))
            {
                Debug.Log("[WWRERMCompat] INACTIVE The ERM-A timer hook has changed.");
                return true;
            }

            var harmony = new Harmony(PatchId);
            MethodInfo update = null;
            try
            {
                update = BroadcastTimer.Initialize();
                var replacement = AccessTools.DeclaredMethod(typeof(BroadcastTimer), "AfterStart");
                var order = radioHooks[0];
                harmony.Patch(start, postfix: new HarmonyMethod(replacement)
                {
                    priority = order.priority,
                    before = order.before,
                    after = order.after
                });

                harmony.Patch(update, prefix: new HarmonyMethod(
                    AccessTools.DeclaredMethod(typeof(BroadcastTimer), "BeforeUpdate")));

                foreach (var method in radioHooks.Select(p => p.PatchMethod).Distinct())
                    harmony.Unpatch(start, method);

                var current = Harmony.GetPatchInfo(start);
                if (current == null || current.Postfixes.Any(IsTimerHook)
                    || !current.Postfixes.Any(p => p.owner == PatchId && p.PatchMethod == replacement))
                    throw new InvalidOperationException("Could not verify the timer hook swap.");

                installed = true;
                Debug.Log("[WWRERMCompat] READY version=0.1.2 replacedTimerHooks=" + radioHooks.Length);
            }
            catch (Exception ex)
            {
                try
                {
                    RestoreTimers(start, radioHooks);
                    harmony.Unpatch(start, HarmonyPatchType.Postfix, PatchId);
                    if (update != null) harmony.Unpatch(update, HarmonyPatchType.Prefix, PatchId);
                    Debug.LogWarning("[WWRERMCompat] INACTIVE " + ex.GetType().Name + ": " + ex.Message);
                }
                catch (Exception restoreError)
                {
                    Debug.LogError("[WWRERMCompat] Timer hooks could not be restored. Disable this companion and restart UBOAT. " + restoreError);
                }
            }
            return true;
        }

        private static bool IsTimerHook(Patch patch)
        {
            var method = patch.PatchMethod;
            return patch.owner == ExpandedRadioOwner && method != null && method.DeclaringType != null
                && method.DeclaringType.FullName == TimerPatchClass && method.Name == "StartTransmission_Postfix";
        }

        private static bool IsLegacyTimer(Patch patch)
        {
            var method = patch.PatchMethod;
            var parameters = method.GetParameters();
            return method.IsStatic && method.ReturnType == typeof(void) && parameters.Length == 2
                && parameters[0].ParameterType == typeof(SandboxEntity)
                && parameters[1].ParameterType == typeof(RadioTransmission);
        }

        private static void RestoreTimers(MethodInfo start, Patch[] originals)
        {
            var current = Harmony.GetPatchInfo(start);
            var harmony = new Harmony(ExpandedRadioOwner);
            foreach (var group in originals.GroupBy(p => p.PatchMethod))
            {
                int present = current == null ? 0 : current.Postfixes.Count(p => p.owner == ExpandedRadioOwner
                    && p.PatchMethod == group.Key);
                foreach (var original in group.Skip(present))
                    harmony.Patch(start, postfix: new HarmonyMethod(original.PatchMethod)
                    {
                        priority = original.priority,
                        before = original.before,
                        after = original.after
                    });
            }
        }
    }

    [NonSerializedInGameState]
    internal sealed class CompatibilityBootstrap : MonoBehaviour
    {
        private float nextAttempt;
        private float deadline;

        private void Start()
        {
            deadline = Time.realtimeSinceStartup + 20f;
        }

        private void Update()
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextAttempt) return;
            nextAttempt = now + 0.5f;
            if (Loader.TryInstall())
                UnityEngine.Object.Destroy(gameObject);
            else if (now >= deadline)
            {
                Debug.LogWarning("[WWRERMCompat] INACTIVE WWR or the supported ERM-A timer hook was unavailable after startup.");
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }

    [NonSerializedInGameState]
    internal static class BroadcastTimer
    {
        private const string DateMarker = " Radio Broadcast ";
        private static FieldInfo executionQueue, gameTime;
        private static MethodInfo updateTransmission;

        internal static MethodInfo Initialize()
        {
            executionQueue = AccessTools.DeclaredField(typeof(SandboxEntity), "executionQueue");
            gameTime = AccessTools.DeclaredField(typeof(SandboxEntity), "gameTime");
            updateTransmission = AccessTools.DeclaredMethod(typeof(SandboxEntity), "UpdateTransmission", Type.EmptyTypes);
            if (executionQueue == null || executionQueue.FieldType != typeof(ExecutionQueue)
                || gameTime == null || gameTime.FieldType != typeof(GameTime)
                || updateTransmission == null || updateTransmission.ReturnType != typeof(float))
                throw new InvalidOperationException("Game transmission timer layout changed.");
            return updateTransmission;
        }

        private static bool BeforeUpdate(SandboxEntity __instance, ref float __result)
        {
            var transmission = __instance.Transmission;
            if (!IsWorldWarRadioStation(transmission))
                return true;

            // The execution queue uses NaN to retire this timed callback.
            __result = float.NaN;
            Debug.Log("[WWRERMCompat] RETIRED_WWR_TIMER key='" + transmission.Key + "'");
            return false;
        }

        private static bool IsWorldWarRadioStation(RadioTransmission transmission)
        {
            if (transmission == null || transmission.Type != RadioTransmissionType.RadioStation
                || transmission.Tracks == null)
                return false;

            string key = transmission.Key;
            return key != null && key.Length > 5 && key[0] == '(' && key[3] == ')' && key[4] == ' '
                && key[1] >= 'A' && key[1] <= 'Z' && key[2] >= 'A' && key[2] <= 'Z'
                && transmission.Tracks.Any(track => !string.IsNullOrEmpty(track)
                    && track.TrimStart('/').StartsWith("Radio/", StringComparison.OrdinalIgnoreCase));
        }

        private static void AfterStart(SandboxEntity __instance, RadioTransmission transmission, bool __result)
        {
            try
            {
                if (!__result || transmission == null || transmission.Type != RadioTransmissionType.RadioStation
                    || !ReferenceEquals(__instance.Transmission, transmission))
                    return;

                DateTime date;
                if (!TryGetBroadcastDate(transmission, out date))
                    return;

                var clock = gameTime.GetValue(null) as GameTime;
                if (transmission.RemainingTransmissionDuration <= 0f
                    || (clock != null && (clock.CurrentDateTime - date).TotalSeconds >= transmission.TransmissionDuration))
                {
                    transmission.RemainingTransmissionDuration = 0f;
                    __instance.StopTransmission();
                    Debug.Log("[WWRERMCompat] EXPIRED before queue key='" + transmission.Key + "'");
                    return;
                }

                var queue = executionQueue.GetValue(null) as ExecutionQueue;
                if (queue == null) return;

                var update = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), __instance, updateTransmission);
                transmission.Flags &= ~RadioTransmissionFlags.ContinuousBroadcast;
                queue.AddTimedUpdateListener(update, 0f, TimedUpdatesQueue.Normal);
                Debug.Log("[WWRERMCompat] SCHEDULED key='" + transmission.Key + "' remainingSec="
                    + transmission.RemainingTransmissionDuration.ToString("R", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WWRERMCompat] Timer hook error: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool TryGetBroadcastDate(RadioTransmission transmission, out DateTime date)
        {
            date = default(DateTime);
            string key = transmission.Key;
            if (string.IsNullOrEmpty(key) || transmission.Tracks == null)
                return false;

            int marker = key.LastIndexOf(DateMarker, StringComparison.Ordinal);
            return marker > 0 && DateTime.TryParseExact(key.Substring(marker + DateMarker.Length),
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                && transmission.Tracks.Any(IsBroadcastTrack);
        }

        private static bool IsBroadcastTrack(string track)
        {
            if (string.IsNullOrEmpty(track)) return false;
            string path = track.TrimStart('/');
            return path.StartsWith("Music/Allied-Radio-Station/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Music/Axis-Radio-Station/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
