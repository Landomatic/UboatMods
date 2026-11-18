using System;
using UnityEngine;
using HarmonyLib;
using UBOAT.Game;

namespace WorldWarRadio
{
    public class ModLoader : IUserMod
    {
        public void OnLoaded()
        {
            Debug.Log("[WorldWarRadio] ModLoader: Mod wird geladen...");

            try
            {
                // Harmony aktivieren
                var harmony = new Harmony("WorldWarRadio.Patches");

                // WICHTIG: PatchAll auf das Assembly des ModLoaders
                harmony.PatchAll(typeof(ModLoader).Assembly);
                WorldWarRadioSpeakerFix.Bootstrap.Install();
                WWRERMCompatibility.Loader.Install();
                Debug.Log("[WorldWarRadio] Private integration 1 ready");

                Debug.Log("[WorldWarRadio] Harmony-Patches geladen (PatchAll auf ModLoader.Assembly).");

                // RadioRangeChecker
                GameObject rangeChecker = new GameObject("RadioRangeChecker");
                rangeChecker.AddComponent<RadioRangeChecker>();
                UnityEngine.Object.DontDestroyOnLoad(rangeChecker);
                Debug.Log("[WorldWarRadio] RadioRangeChecker instanziiert.");

                // SoundMixer
                GameObject soundMixer = new GameObject("SoundMixer");
                soundMixer.AddComponent<SoundMixer>();
                UnityEngine.Object.DontDestroyOnLoad(soundMixer);
                Debug.Log("[WorldWarRadio] SoundMixer instanziiert.");

                Debug.Log("[WorldWarRadio] Alle Systeme erfolgreich geladen.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[WorldWarRadio] Fehler beim Laden!");
                Debug.LogException(ex);
            }
        }
    }
}
