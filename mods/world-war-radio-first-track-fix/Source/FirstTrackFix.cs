// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic.
// Companion for World War Radio by MaGlas (Steam Workshop 3342143865).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DWS.Common.Resources;
using HarmonyLib;
using UBOAT.Game;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Scene;
using UBOAT.Game.Scene.Audio;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Stories;
using UnityEngine;

namespace WorldWarRadioFirstTrackFix
{
    [NonSerializedInGameState]
    public sealed class Loader : IUserMod
    {
        private const string PatchId = "worldwarradio.first-track-fix";
        private static bool installed;

        public void OnLoaded()
        {
            if (installed) return;
            var harmony = new Harmony(PatchId);
            try
            {
                var wwr = AccessTools.AllAssemblies().FirstOrDefault(a => a.GetName().Name == "3342143865");
                if (wwr == null)
                {
                    Debug.Log("[WWRFirstTrackFix] INACTIVE World War Radio assembly unavailable. Load after World War Radio.");
                    return;
                }

                if (wwr.GetType("Patch_SpeakerSystem_OnTrackLoaded") == null
                    || wwr.GetType("CaptureRandomStartForTrackRequest") != null)
                {
                    Debug.Log("[WWRFirstTrackFix] INACTIVE original random-start hook absent or integrated fix detected.");
                    return;
                }

                FirstTrack.Install(harmony, wwr.GetType("SoundMixer", true));
                installed = true;
                Debug.Log("[WWRFirstTrackFix] READY version=0.1.0");
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(PatchId);
                Debug.LogWarning("[WWRFirstTrackFix] INACTIVE " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }

    [NonSerializedInGameState]
    internal static class FirstTrack
    {
        private sealed class TrackRequest
        {
            internal uint Generation;
        }

        [ThreadStatic] private static bool tunePending;
        private static bool callbackHookInstalled;
        private static PropertyInfo mixerInstance;
        private static FieldInfo legacyRandomStart, speakers;
        private static MethodInfo trackLoaded;
        private static Action<SpeakerSystem, Resource<AudioClip>> originalTrackLoaded;
        private static readonly ConditionalWeakTable<SpeakerSystem, TrackRequest> Requests =
            new ConditionalWeakTable<SpeakerSystem, TrackRequest>();

        internal static void Install(Harmony harmony, Type mixer)
        {
            mixerInstance = AccessTools.DeclaredProperty(mixer, "Instance");
            legacyRandomStart = AccessTools.DeclaredField(mixer, "shouldRandomizeNextTrack");
            if (mixerInstance == null || mixerInstance.PropertyType != mixer || legacyRandomStart == null
                || legacyRandomStart.FieldType != typeof(bool))
                throw new MissingMemberException(mixer.FullName, "Instance/shouldRandomizeNextTrack");

            speakers = AccessTools.DeclaredField(typeof(SpeakerSystem), "speakers");
            if (speakers == null || speakers.FieldType != typeof(DeepAudioSource[]))
                throw new MissingFieldException(typeof(SpeakerSystem).FullName, "speakers");

            trackLoaded = RequiredMethod(typeof(SpeakerSystem), "OnTrackLoaded", typeof(Resource<AudioClip>));
            originalTrackLoaded = (Action<SpeakerSystem, Resource<AudioClip>>)Delegate.CreateDelegate(
                typeof(Action<SpeakerSystem, Resource<AudioClip>>), trackLoaded);
            var tune = RequiredMethod(typeof(RadioStory), "OnPlayMusicStationX", typeof(Story.Decision));
            var next = RequiredMethod(typeof(SpeakerSystem), "PlayNextTrack");

            harmony.Patch(next, transpiler: Hook("TranspileTrackRequest"));
            if (!callbackHookInstalled)
                throw new InvalidOperationException("The game track callback pattern was not found.");
            harmony.Patch(tune, prefix: Hook("BeginTune"), finalizer: Hook("EndTune"));
            harmony.Patch(trackLoaded, prefix: Hook("ClearLegacySeek"));
        }

        private static MethodInfo RequiredMethod(Type type, string name, params Type[] parameters)
        {
            var method = AccessTools.DeclaredMethod(type, name, parameters);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return method;
        }

        private static HarmonyMethod Hook(string name)
        {
            return new HarmonyMethod(typeof(FirstTrack), name);
        }

        private static void BeginTune(out bool __state)
        {
            __state = tunePending;
            tunePending = true;
        }

        private static void EndTune(bool __state)
        {
            tunePending = __state;
        }

        private static void ClearLegacySeek()
        {
            if (!callbackHookInstalled) return;
            var mixer = mixerInstance.GetValue(null, null);
            if (mixer != null)
                legacyRandomStart.SetValue(mixer, false);
        }

        private static IEnumerable<CodeInstruction> TranspileTrackRequest(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var capture = RequiredMethod(typeof(FirstTrack), "Capture", typeof(SpeakerSystem));
            var constructor = typeof(Action<Resource<AudioClip>>).GetConstructor(new[] { typeof(object), typeof(IntPtr) });
            int index = code.FindIndex(instruction => instruction.opcode == OpCodes.Ldftn && Equals(instruction.operand, trackLoaded));
            callbackHookInstalled = index >= 1 && index + 1 < code.Count && code[index - 1].opcode == OpCodes.Ldarg_0
                && code[index + 1].opcode == OpCodes.Newobj && Equals(code[index + 1].operand, constructor);
            if (!callbackHookInstalled)
                return code;

            // The replacement delegate belongs to the requested clip, even if loading is asynchronous.
            code[index].opcode = OpCodes.Call;
            code[index].operand = capture;
            code[index + 1].opcode = OpCodes.Nop;
            code[index + 1].operand = null;
            return code;
        }

        private static Action<Resource<AudioClip>> Capture(SpeakerSystem system)
        {
            var request = Requests.GetValue(system, _ => new TrackRequest());
            uint generation = ++request.Generation;
            if (!tunePending)
                return resource => originalTrackLoaded(system, resource);

            tunePending = false;
            string station = system.MusicStationName;
            return resource =>
            {
                originalTrackLoaded(system, resource);
                if (request.Generation != generation || system.LoudspeakersMode != LoudspeakersMode.Music
                    || system.Tracks == null || system.MusicStationName != station || string.IsNullOrEmpty(station))
                    return;

                AudioClip clip = resource;
                if (clip == null || clip.length <= 0f) return;
                var outputs = speakers.GetValue(system) as DeepAudioSource[];
                if (outputs == null) return;

                float offset = UnityEngine.Random.Range(0f, Mathf.Max(0f, clip.length - 0.1f));
                bool applied = false;
                foreach (var speaker in outputs)
                {
                    if (speaker == null || speaker.clip != clip) continue;
                    speaker.time = offset;
                    applied = true;
                }
                if (applied)
                    Debug.Log(string.Format("[WWRFirstTrackFix] RANDOM_START station='{0}' offsetSec={1:F2}", station, offset));
            };
        }
    }
}
