using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using UBOAT.Game.Scene.Audio;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Sandbox.Messages;
using DWS.Common.InjectionFramework;

public class SoundMixer : MonoBehaviour
{
    public static SoundMixer Instance { get; private set; }

    public static bool NeedsResetAfterLoad = false;

    [Inject] private static ISoundEffectsDatabase soundEffectsDatabase;

    private PlayerShipRadio playerShipRadio;
    private SpeakerSystem speakerSystem;

    private List<DeepAudioSource> staticSources = new List<DeepAudioSource>();
    private List<DeepAudioSource> effectSources = new List<DeepAudioSource>();
    private List<AudioSource> allAudioSources = new List<AudioSource>();

    private List<ISoundEffect> staticEffects = new List<ISoundEffect>();

    private DeepAudioSource[] musicSpeakers;
    private Dictionary<DeepAudioSource, float> baseMusicVolume = new Dictionary<DeepAudioSource, float>();

    private string lastStation = null;

    private static float globalVolumeFactor = 1f;
    private const float minVolume = 0.2f;
    private const float maxVolume = 1.0f;
    private const float volumeStep = 0.1f;

    private float fadeInFactor = 1f;
    private Coroutine fadeCoroutine;

    public bool shouldRandomizeNextTrack = false;

    private float currentSignal = 1f;
    private float lastKnownSignal = 1f;
    private float musicNoiseMultiplier = 1f;

    public Dictionary<string, int> stationTrackCount = new Dictionary<string, int>();
    private AudioClip lastClip = null;
    private float lastTrackTime = 0f;

    private float stationLostTimestamp = -1f;
    private const float stationLostThreshold = 1f;

    void Start()
    {
        Instance = this;
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        while (playerShipRadio == null)
        {
            playerShipRadio = FindObjectOfType<PlayerShipRadio>();
            yield return null;
        }

        while (speakerSystem == null)
        {
            speakerSystem = playerShipRadio.ParentEntity.GetDevice<SpeakerSystem>();
            yield return null;
        }

        var speakersField = AccessTools.Field(typeof(SpeakerSystem), "speakers");
        musicSpeakers = speakersField.GetValue(speakerSystem) as DeepAudioSource[];

        foreach (var sp in musicSpeakers)
            if (sp != null)
                baseMusicVolume[sp] = sp.volume;

        allAudioSources.AddRange(FindObjectsOfType<AudioSource>());

        CreateEffectSpeakers();
        CreateStaticSpeakers();
        CreateStaticNoiseInstances();
        StartStaticNoise();
    }

    // ⭐ Reset wird nur ausgeführt, wenn NeedsResetAfterLoad = true
    public void ResetAllSpeakers()
    {
        foreach (var src in staticSources)
            if (src != null) Destroy(src.gameObject);

        foreach (var src in effectSources)
            if (src != null) Destroy(src.gameObject);

        staticSources.Clear();
        effectSources.Clear();
        staticEffects.Clear();

        CreateEffectSpeakers();
        CreateStaticSpeakers();
        CreateStaticNoiseInstances();
        StartStaticNoise();
    }

    private void CreateEffectSpeakers()
    {
        var deepSources = FindObjectsOfType<DeepAudioSource>();

        foreach (var ds in deepSources)
        {
            if (ds.name == "Loudspeaker")
            {
                GameObject goEffect = new GameObject("EffectSpeaker");
                goEffect.transform.SetParent(ds.transform);
                goEffect.transform.localPosition = Vector3.zero;

                DeepAudioSource effect = goEffect.AddComponent<DeepAudioSource>();
                effect.spatialBlend = 1f;
                effect.minDistance = 0.5f;
                effect.maxDistance = 15f;
                effect.volume = 1f;

                effectSources.Add(effect);
            }
        }
    }

    private void CreateStaticSpeakers()
    {
        var deepSources = FindObjectsOfType<DeepAudioSource>();

        foreach (var ds in deepSources)
        {
            if (ds.name == "Loudspeaker")
            {
                GameObject goStatic = new GameObject("StaticSpeaker");
                goStatic.transform.SetParent(ds.transform);
                goStatic.transform.localPosition = Vector3.zero;

                DeepAudioSource stat = goStatic.AddComponent<DeepAudioSource>();
                stat.spatialBlend = 1f;
                stat.minDistance = 0.5f;
                stat.maxDistance = 15f;
                stat.volume = 1f;

                staticSources.Add(stat);
            }
        }
    }

    private void CreateStaticNoiseInstances()
    {
        staticEffects.Clear();

        foreach (var src in staticSources)
        {
            var fx = soundEffectsDatabase.GetClip("RadioStaticSound", src.transform);
            if (fx == null)
                continue;

            fx.loop = true;
            staticEffects.Add(fx);
        }
    }

    private void StartStaticNoise()
    {
        foreach (var fx in staticEffects)
            fx.Play();
    }

    void Update()
    {
        // ⭐ Reset nur hier, wenn Szene vollständig geladen ist
        if (NeedsResetAfterLoad)
        {
            NeedsResetAfterLoad = false;
            ResetAllSpeakers();
        }

        if (playerShipRadio == null || speakerSystem == null)
            return;

        string station = speakerSystem.MusicStationName;

        if (string.IsNullOrEmpty(station))
        {
            if (stationLostTimestamp < 0f)
                stationLostTimestamp = Time.unscaledTime;
        }
        else
        {
            stationLostTimestamp = -1f;
        }

        DetectTrackChange();
        HandleVolumeHotkeys();
        ApplyEffectVolume();
        DetectNeedleScratch();
        HandleRadioSwitch();
        UpdateSignalStrength();
        UpdateStaticNoiseVolume();
        ApplyMusicFadeVolume();

        if (speakerSystem.LoudspeakersMode == LoudspeakersMode.Music &&
            fadeInFactor == 1f &&
            !string.IsNullOrEmpty(station) &&
            lastStation == null)
        {
            if (!stationTrackCount.ContainsKey(station))
                stationTrackCount[station] = 0;

            StartFadeIn();
        }
    }

    private void DetectTrackChange()
    {
        if (stationLostTimestamp >= 0f &&
            Time.unscaledTime - stationLostTimestamp < stationLostThreshold)
            return;

        if (!RadioRangeChecker.StationIsManned &&
            Time.unscaledTime - RadioRangeChecker.LastUnmannedTimestamp < 1f)
            return;

        if (musicSpeakers == null || musicSpeakers.Length == 0)
            return;

        var sp = musicSpeakers[0];
        if (sp == null || sp.clip == null)
            return;

        string station = speakerSystem.MusicStationName;

        if (sp.clip != lastClip)
        {
            if (!string.IsNullOrEmpty(station))
            {
                if (!stationTrackCount.ContainsKey(station))
                    stationTrackCount[station] = 0;

                stationTrackCount[station]++;
            }

            lastClip = sp.clip;
        }

        if (sp.time < lastTrackTime - 0.1f)
        {
            if (!string.IsNullOrEmpty(station))
            {
                if (!stationTrackCount.ContainsKey(station))
                    stationTrackCount[station] = 0;

                stationTrackCount[station]++;
            }
        }

        lastTrackTime = sp.time;
    }

    private void HandleRadioSwitch()
    {
        if (speakerSystem.LoudspeakersMode != LoudspeakersMode.Music)
        {
            lastStation = null;
            return;
        }

        string current = speakerSystem.MusicStationName;
        bool lastEmpty = string.IsNullOrEmpty(lastStation);
        bool currentEmpty = string.IsNullOrEmpty(current);

        if (lastEmpty && !currentEmpty)
        {
            PlayEffect("RadioSwitch");
            stationTrackCount[current] = 0;
            StartFadeIn();
        }
        else if (!lastEmpty && current != lastStation)
        {
            PlayEffect("RadioSwitch");
            stationTrackCount[current] = 0;
            StartFadeIn();
        }

        lastStation = current;
    }

    private void PlayEffect(string effectName)
    {
        if (effectSources.Count == 0)
            return;

        var src = effectSources[0];
        var fx = soundEffectsDatabase.GetClip(effectName, src.transform);

        if (fx != null)
        {
            fx.loop = false;
            fx.Play();
            fx.volume = globalVolumeFactor;
        }
    }

    private void DetectNeedleScratch()
    {
        foreach (var src in allAudioSources)
        {
            if (src == null) continue;
            if (!src.isPlaying) continue;
            if (src.clip == null) continue;

            if (src.clip.name == "record needle scratch")
            {
                src.Stop();
                PlayEffect("RadioTurnOff");
                lastStation = null;
            }
        }
    }

    private void UpdateSignalStrength()
    {
        if (Time.unscaledTime - RadioRangeChecker.LastMannedTimestamp < 2f)
        {
            currentSignal = lastKnownSignal;
            return;
        }

        if (!RadioRangeChecker.StationIsManned &&
            Time.unscaledTime - RadioRangeChecker.LastUnmannedTimestamp < 2f)
        {
            currentSignal = lastKnownSignal;
            return;
        }

        if (!RadioRangeChecker.StationIsManned)
        {
            currentSignal = lastKnownSignal;
            return;
        }

        string station = speakerSystem.MusicStationName;

        if (string.IsNullOrEmpty(station))
        {
            currentSignal = 0f;
            lastKnownSignal = currentSignal;
            return;
        }

        RadioTransmission active = null;

        foreach (var t in playerShipRadio.NearbyTransmissions)
        {
            if (t == null || t.Source == null)
                continue;

            if (t.Key == station)
            {
                active = t;
                break;
            }
        }

        if (active == null)
        {
            currentSignal = 0f;
            lastKnownSignal = currentSignal;
            return;
        }

        Vector2 receiverPos = playerShipRadio.ParentEntity.SandboxEntity.Position;
        Vector2 transmitterPos = active.Source.Position;

        float distance = Vector2.Distance(transmitterPos, receiverPos);
        float maxRange = playerShipRadio.ReceiveRange * active.SignalStrength;
        float correctedRange = maxRange * 1.30f;

        float x = distance / correctedRange;
        float k = 10f;
        float center = 0.75f;

        currentSignal = 1f / (1f + Mathf.Exp(k * (x - center)));
        lastKnownSignal = currentSignal;
    }

    private float CalculateNoiseLevel()
    {
        if (string.IsNullOrEmpty(speakerSystem.MusicStationName))
            return 0f;

        if (currentSignal >= 0.5f)
            return 0f;

        return Mathf.Clamp01((0.5f - currentSignal) * 2f);
    }

    private void UpdateStaticNoiseVolume()
    {
        float noiseLevel = CalculateNoiseLevel();
        float finalVolume = noiseLevel * globalVolumeFactor;

        for (int i = 0; i < staticEffects.Count; i++)
        {
            var fx = staticEffects[i];
            if (fx != null)
                fx.volume = finalVolume;
        }

        musicNoiseMultiplier = 1f - (noiseLevel * 0.75f);
    }

    private void HandleVolumeHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Insert))
            globalVolumeFactor = Mathf.Clamp(globalVolumeFactor + volumeStep, minVolume, maxVolume);

        if (Input.GetKeyDown(KeyCode.Delete))
            globalVolumeFactor = Mathf.Clamp(globalVolumeFactor - volumeStep, minVolume, maxVolume);
    }

    private void ApplyEffectVolume()
    {
        for (int i = 0; i < effectSources.Count; i++)
        {
            var src = effectSources[i];
            if (src != null)
                src.volume = globalVolumeFactor;
        }
    }

    private void ApplyMusicFadeVolume()
    {
        if (musicSpeakers == null)
            return;

        float combinedFactor = globalVolumeFactor * fadeInFactor * musicNoiseMultiplier;

        for (int i = 0; i < musicSpeakers.Length; i++)
        {
            var sp = musicSpeakers[i];
            if (sp == null)
                continue;

            if (!baseMusicVolume.TryGetValue(sp, out float baseVol))
                continue;

            sp.volume = baseVol * combinedFactor;
        }
    }

    private void StartFadeIn()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        fadeInFactor = 0f;
        float duration = 5f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            fadeInFactor = Mathf.Clamp01(t / duration);
            yield return null;
        }

        fadeInFactor = 1f;
    }
}
