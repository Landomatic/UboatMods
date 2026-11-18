using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DWS.Common.Resources;
using HarmonyLib;
using UBOAT.Game.Scene.Audio;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Stories;
using UnityEngine;

[HarmonyPatch(typeof(RadioStory), "OnPlayMusicStationX")]
internal static class ArmRandomStartOnTune
{
    static void Prefix(out SoundMixer __state)
    {
        __state = SoundMixer.Instance;
        if (__state != null)
            __state.shouldRandomizeNextTrack = true;
    }

    static void Finalizer(SoundMixer __state)
    {
        if (__state != null)
            __state.shouldRandomizeNextTrack = false;
    }
}

[HarmonyPatch(typeof(SpeakerSystem), "PlayNextTrack")]
internal static class CaptureRandomStartForTrackRequest
{
    private sealed class TrackRequest
    {
        public uint Generation;
    }

    private static readonly ConditionalWeakTable<SpeakerSystem, TrackRequest> Requests =
        new ConditionalWeakTable<SpeakerSystem, TrackRequest>();

    private static readonly FieldInfo Speakers =
        AccessTools.DeclaredField(typeof(SpeakerSystem), "speakers");

    private static readonly Action<SpeakerSystem, Resource<AudioClip>> OnTrackLoaded =
        (Action<SpeakerSystem, Resource<AudioClip>>)Delegate.CreateDelegate(
            typeof(Action<SpeakerSystem, Resource<AudioClip>>),
            AccessTools.DeclaredMethod(typeof(SpeakerSystem), "OnTrackLoaded"));

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        var capture = AccessTools.DeclaredMethod(typeof(CaptureRandomStartForTrackRequest), nameof(Capture));
        if (code.Exists(instruction => instruction.opcode == OpCodes.Call && Equals(instruction.operand, capture)))
            return code;

        var target = AccessTools.DeclaredMethod(typeof(SpeakerSystem), "OnTrackLoaded");
        var constructor = typeof(Action<Resource<AudioClip>>).GetConstructor(
            new[] { typeof(object), typeof(IntPtr) });
        int index = code.FindIndex(instruction =>
            instruction.opcode == OpCodes.Ldftn && Equals(instruction.operand, target));

        if (index < 1 || index + 1 >= code.Count || code[index - 1].opcode != OpCodes.Ldarg_0
            || code[index + 1].opcode != OpCodes.Newobj || !Equals(code[index + 1].operand, constructor))
        {
            Debug.LogWarning("[RandomStart] Track callback pattern unavailable");
            return code;
        }

        // Replace the bound OnTrackLoaded delegate with one carrying this request's tune state.
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
        var mixer = SoundMixer.Instance;
        if (mixer == null || !mixer.shouldRandomizeNextTrack)
            return resource => OnTrackLoaded(system, resource);

        mixer.shouldRandomizeNextTrack = false;
        string station = system.MusicStationName;
        return resource =>
        {
            OnTrackLoaded(system, resource);
            if (request.Generation != generation || system.LoudspeakersMode != LoudspeakersMode.Music
                || system.Tracks == null || system.MusicStationName != station || string.IsNullOrEmpty(station))
                return;

            AudioClip clip = resource;
            if (clip == null || clip.length <= 0f)
                return;

            var speakers = Speakers.GetValue(system) as DeepAudioSource[];
            if (speakers == null)
                return;

            float offset = UnityEngine.Random.Range(0f, Mathf.Max(0f, clip.length - 0.1f));
            bool applied = false;
            foreach (var speaker in speakers)
            {
                if (speaker == null || speaker.clip != clip)
                    continue;
                speaker.time = offset;
                applied = true;
            }

            if (applied)
                Debug.Log($"[RandomStart] Station='{station}' | Clip='{clip.name}' | Start={offset:F2}s");
        };
    }
}
