using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DWS.Common.InjectionFramework;
using HarmonyLib;
using UBOAT.Game;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Scene.Audio;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Landomatic.WorldWarRadioLoadFix
{
    [NonSerializedInGameState]
    public sealed class Loader : IUserMod
    {
        private const string PatchId = "landomatic.world-war-radio-load-fix";

        public void OnLoaded()
        {
            var harmony = new Harmony(PatchId);
            try
            {
                if (RadioLoadFix.Install(harmony))
                    Debug.Log("[WWRLoadFix] READY version=0.1.0 World War Radio 3342143865");
                else
                    Debug.Log("[WWRLoadFix] INACTIVE World War Radio assembly is absent");
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(PatchId);
                Debug.LogWarning("[WWRLoadFix] INACTIVE " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }

    [NonSerializedInGameState]
    internal static class RadioLoadFix
    {
        private static FieldInfo pendingReset, gameSpeakers;
        private static FieldInfo musicSpeakers, baseMusicVolume, allAudioSources, fadeCoroutine, fadeInFactor;
        private static FieldInfo lastStation, stationLostTimestamp, lastClip, lastTrackTime, randomizeTrack;

        private static SavesManager saves;
        private static PlayerShipProxy proxy;
        private static PlayerShipRadio liveRadio;
        private static SpeakerSystem liveSpeakers;
        private static DeepAudioSource[] liveMusic;
        private static int contextFrame = -1;
        private static int contextGeneration;
        private static int rangeGeneration = -1;
        private static float nextLookup;
        private static bool wasLoading;
        private static bool mixerBound;
        private static string signalStation;
        private static bool hasTransmission;

        internal static bool Install(Harmony harmony)
        {
            var assembly = AccessTools.AllAssemblies().FirstOrDefault(a => a.GetName().Name == "3342143865");
            if (assembly == null) return false;

            var mixer = assembly.GetType("SoundMixer", true);
            var range = assembly.GetType("RadioRangeChecker", true);
            pendingReset = Field(mixer, "NeedsResetAfterLoad", typeof(bool));
            gameSpeakers = Field(typeof(SpeakerSystem), "speakers", typeof(DeepAudioSource[]));
            musicSpeakers = Field(mixer, "musicSpeakers", typeof(DeepAudioSource[]));
            baseMusicVolume = Field(mixer, "baseMusicVolume", typeof(Dictionary<DeepAudioSource, float>));
            allAudioSources = Field(mixer, "allAudioSources", typeof(List<AudioSource>));
            fadeCoroutine = Field(mixer, "fadeCoroutine", typeof(Coroutine));
            fadeInFactor = Field(mixer, "fadeInFactor", typeof(float));
            lastStation = Field(mixer, "lastStation", typeof(string));
            stationLostTimestamp = Field(mixer, "stationLostTimestamp", typeof(float));
            lastClip = Field(mixer, "lastClip", typeof(AudioClip));
            lastTrackTime = Field(mixer, "lastTrackTime", typeof(float));
            randomizeTrack = Field(mixer, "shouldRandomizeNextTrack", typeof(bool));

            Prefix(harmony, mixer, "InitializeCoroutine", "BeforeMixerInitialize");
            Prefix(harmony, range, "Start", "BeforeRangeStart");
            Prefix(harmony, mixer, "Update", "BeforeMixerUpdate");
            Prefix(harmony, range, "Update", "BeforeRangeUpdate");
            Prefix(harmony, mixer, "StartStaticNoise", "BeforeStaticStart");
            Prefix(harmony, mixer, "UpdateSignalStrength", "BeforeSignalUpdate");
            return true;
        }

        private static FieldInfo Field(Type owner, string name, Type type)
        {
            var field = AccessTools.DeclaredField(owner, name);
            if (field == null || field.FieldType != type)
                throw new MissingFieldException(owner.FullName, name);
            return field;
        }

        private static void Prefix(Harmony harmony, Type owner, string original, string prefix)
        {
            var target = AccessTools.DeclaredMethod(owner, original, Type.EmptyTypes);
            if (target == null) throw new MissingMethodException(owner.FullName, original);
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(RadioLoadFix), prefix));
        }

        private static IEnumerator EmptyInitialization()
        {
            yield break;
        }

        private static bool BeforeMixerInitialize(ref IEnumerator __result)
        {
            __result = EmptyInitialization();
            return false;
        }

        private static bool BeforeRangeStart()
        {
            return false;
        }

        private static void ReadContext()
        {
            if (contextFrame == Time.frameCount) return;
            contextFrame = Time.frameCount;
            if ((saves == null || proxy == null) && Time.realtimeSinceStartup >= nextLookup)
            {
                nextLookup = Time.realtimeSinceStartup + 0.5f;
                if (saves == null) saves = ScriptableObjectSingleton.LoadSingleton<SavesManager>();
                if (proxy == null) proxy = Object.FindObjectOfType<PlayerShipProxy>();
            }

            bool loading = saves != null && saves.IsLoading;
            if (loading && !wasLoading) contextGeneration++;
            wasLoading = loading;
            var ship = !loading && proxy != null ? proxy.CurrentShip : null;
            var radio = ship == null || !ship.isActiveAndEnabled ? null : ship.GetDevice<PlayerShipRadio>();
            var speakers = radio == null ? null : ship.GetDevice<SpeakerSystem>();
            if (radio != liveRadio || speakers != liveSpeakers)
            {
                contextGeneration++;
                liveMusic = speakers == null ? null : (DeepAudioSource[])gameSpeakers.GetValue(speakers);
            }
            liveRadio = radio;
            liveSpeakers = speakers;
            if (liveSpeakers != null && (liveMusic == null || liveMusic.Length == 0))
                liveMusic = (DeepAudioSource[])gameSpeakers.GetValue(liveSpeakers);
        }

        private static bool BeforeRangeUpdate(ref PlayerShipRadio ___playerShipRadio, ref bool ___isRadioInitialized,
            ref bool ___wasMannedLastFrame, ref float ___nextCheckTime, ref bool ___StationIsManned,
            ref float ___LastMannedTimestamp, ref float ___LastUnmannedTimestamp)
        {
            ReadContext();
            if (rangeGeneration != contextGeneration || ___playerShipRadio != liveRadio)
            {
                rangeGeneration = contextGeneration;
                ___playerShipRadio = liveRadio;
                ___wasMannedLastFrame = false;
                ___StationIsManned = false;
                ___LastMannedTimestamp = 0f;
                ___LastUnmannedTimestamp = 0f;
                ___nextCheckTime = Time.time;
            }
            ___isRadioInitialized = liveRadio != null && liveSpeakers != null
                && liveRadio.Interactable != null && liveRadio.NearbyTransmissions != null;
            return ___isRadioInitialized;
        }

        private static bool BeforeMixerUpdate(MonoBehaviour __instance,
            ref PlayerShipRadio ___playerShipRadio, ref SpeakerSystem ___speakerSystem,
            List<ISoundEffect> ___staticEffects, ref float ___currentSignal,
            ref float ___lastKnownSignal, ref float ___musicNoiseMultiplier)
        {
            ReadContext();
            if (liveRadio == null || liveSpeakers == null || liveMusic == null || liveMusic.Length == 0)
            {
                if (mixerBound)
                {
                    Silence(___staticEffects, true);
                    Debug.Log("[WWRLoadFix] WAIT paused static during load or radio replacement");
                }
                mixerBound = false;
                ___playerShipRadio = null;
                ___speakerSystem = null;
                return false;
            }

            if (!mixerBound || ___playerShipRadio != liveRadio || ___speakerSystem != liveSpeakers
                || (bool)pendingReset.GetValue(null))
            {
                Silence(___staticEffects, true);
                RefreshPlayback(__instance);
                ___playerShipRadio = liveRadio;
                ___speakerSystem = liveSpeakers;
                ___currentSignal = 1f;
                ___lastKnownSignal = 1f;
                ___musicNoiseMultiplier = 1f;
                signalStation = null;
                hasTransmission = false;
                mixerBound = true;
                pendingReset.SetValue(null, true);
                Debug.Log("[WWRLoadFix] BOUND ship=" + liveRadio.ParentEntity.name
                    + " radio=" + liveRadio.GetInstanceID() + " speakers=" + liveSpeakers.GetInstanceID());
            }
            return true;
        }

        private static void RefreshPlayback(MonoBehaviour mixer)
        {
            var fade = (Coroutine)fadeCoroutine.GetValue(mixer);
            if (fade != null) mixer.StopCoroutine(fade);
            fadeCoroutine.SetValue(mixer, null);
            fadeInFactor.SetValue(mixer, 1f);
            lastStation.SetValue(mixer, null);
            stationLostTimestamp.SetValue(mixer, -1f);
            lastClip.SetValue(mixer, null);
            lastTrackTime.SetValue(mixer, 0f);
            randomizeTrack.SetValue(mixer, false);

            var previous = (Dictionary<DeepAudioSource, float>)baseMusicVolume.GetValue(mixer);
            var volumes = new Dictionary<DeepAudioSource, float>();
            foreach (var speaker in liveMusic)
            {
                if (speaker == null) continue;
                float original;
                volumes[speaker] = previous.TryGetValue(speaker, out original) ? original : speaker.volume;
            }
            baseMusicVolume.SetValue(mixer, volumes);
            musicSpeakers.SetValue(mixer, liveMusic);
            var sources = (List<AudioSource>)allAudioSources.GetValue(mixer);
            sources.Clear();
            sources.AddRange(Object.FindObjectsOfType<AudioSource>());
        }

        private static void BeforeStaticStart(List<ISoundEffect> ___staticEffects)
        {
            Silence(___staticEffects, false);
        }

        private static void Silence(List<ISoundEffect> effects, bool stop)
        {
            foreach (var effect in effects)
            {
                if (effect == null || (effect is Object && (Object)effect == null) || !effect.valid) continue;
                effect.volume = 0f;
                if (stop) effect.Stop();
            }
        }

        private static bool BeforeSignalUpdate(PlayerShipRadio ___playerShipRadio, SpeakerSystem ___speakerSystem,
            ref float ___currentSignal, ref float ___lastKnownSignal)
        {
            string station = ___speakerSystem.LoudspeakersMode == LoudspeakersMode.Music
                ? ___speakerSystem.MusicStationName : null;
            if (signalStation != station || string.IsNullOrEmpty(station))
            {
                signalStation = station;
                hasTransmission = false;
                ___currentSignal = 1f;
                ___lastKnownSignal = 1f;
            }
            if (string.IsNullOrEmpty(station)) return false;

            if (___playerShipRadio.ReceiveRange <= 0f || ___playerShipRadio.ParentEntity.SandboxEntity == null)
            {
                hasTransmission = false;
                ___currentSignal = 1f;
                ___lastKnownSignal = 1f;
                return false;
            }

            if (!hasTransmission)
            {
                var transmissions = ___playerShipRadio.NearbyTransmissions;
                hasTransmission = transmissions != null && transmissions.Exists(t => t != null
                    && t.Source != null && t.Key == station && t.SignalStrength > 0f);
            }
            return hasTransmission;
        }
    }
}
