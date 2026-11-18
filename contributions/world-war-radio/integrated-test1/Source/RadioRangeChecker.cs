using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;

public class RadioRangeChecker : MonoBehaviour
{
    private PlayerShipRadio playerShipRadio;
    private bool isRadioInitialized = false;

    private float checkInterval = 5f;
    private float nextCheckTime = 0f;

    private float radioMannedTimestamp = 0f;

    // Globaler Status für SoundMixer
    public static bool StationIsManned = false;

    // Zeitpunkte für Grace-Period (Echtzeit!)
    public static float LastMannedTimestamp = 0f;
    public static float LastUnmannedTimestamp = 0f;

    // Tracken, ob vorher bemannt
    private bool wasMannedLastFrame = false;

    void Start()
    {
        StartCoroutine(InitializePlayerShipRadioCoroutine());
    }

    void Update()
    {
        if (!isRadioInitialized)
            return;

        // Funkerstatus jedes Frame aktualisieren
        bool manned = IsRadioManned();
        StationIsManned = manned;

        // Übergang unbemannt → bemannt
        if (manned && !wasMannedLastFrame)
        {
            radioMannedTimestamp = Time.unscaledTime;
            LastMannedTimestamp = radioMannedTimestamp;
        }

        // Übergang bemannt → unbemannt
        if (!manned && wasMannedLastFrame)
        {
            LastUnmannedTimestamp = Time.unscaledTime;
        }

        wasMannedLastFrame = manned;

        // Reichweitenprüfung alle 5 Sekunden
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            CheckRadioTransmissionRange();
        }
    }

    private IEnumerator InitializePlayerShipRadioCoroutine()
    {
        while (!isRadioInitialized)
        {
            playerShipRadio = FindObjectOfType<PlayerShipRadio>();

            if (playerShipRadio != null)
            {
                isRadioInitialized = true;
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void CheckRadioTransmissionRange()
    {
        if (playerShipRadio == null)
            return;

        SpeakerSystem speakerSystem = playerShipRadio.ParentEntity.GetDevice<SpeakerSystem>();
        if (speakerSystem == null)
            return;

        if (speakerSystem.LoudspeakersMode != LoudspeakersMode.Music)
            return;

        string currentMusicStation = speakerSystem.MusicStationName;
        if (string.IsNullOrEmpty(currentMusicStation))
            return;

        bool isInRange = playerShipRadio.NearbyTransmissions
            .Exists(transmission => transmission.Key == currentMusicStation);

        bool isRadioManned = StationIsManned;

        if (!isInRange)
        {
            if (isRadioManned)
            {
                // Grace-Period von 2 Sekunden nach Hinsetzen (Echtzeit)
                if (Time.unscaledTime - LastMannedTimestamp < 2f)
                    return;

                speakerSystem.StopLoudspeakers();
            }
            else
            {
                // Unbemannt → Radio bleibt an
            }
        }
    }

    private bool IsRadioManned()
    {
        if (playerShipRadio == null)
            return false;

        var chars = playerShipRadio.Interactable.InteractingCharacters;
        return chars.Count > 0;
    }
}
