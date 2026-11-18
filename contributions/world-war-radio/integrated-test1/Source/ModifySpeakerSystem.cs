using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace ModifySpeakerSystem
{
    // ⭐ Patch 1: Tiefe-Schwelle für Reconnect reduzieren
    [HarmonyPatch(typeof(SpeakerSystem), "TryToContinueMusic")]
    public static class SpeakerSystem_TryToContinueMusic_Patch
    {
        static bool Prefix(SpeakerSystem __instance)
        {
            // PlayerShip korrekt holen
            var playerShip = __instance.ParentEntity.GetComponent<PlayerShip>();
            if (playerShip == null)
                return true;

            // Original: if (Depth >= 12.0) return;
            // Neu: 4.0 m Schwelle (früherer Reconnect)
            if (playerShip.Depth >= 4.0f)
            {
                // Simuliert das Original-"return"
                return false;
            }

            // Rest der Methode soll normal laufen
            return true;
        }
    }

    // ⭐ Patch 2: Alarm soll Musik NICHT ausschalten
    [HarmonyPatch(typeof(SpeakerSystem), "PlayerShipProxy_AlarmStarted")]
    public static class SpeakerSystem_PlayerShipProxy_AlarmStarted_Patch
    {
        public static bool Prefix()
        {
            // Originalmethode komplett überspringen → Musik bleibt an
            return false;
        }
    }
}
