using HarmonyLib;
using UnityEngine;
using UBOAT.Game.Scene.Entities;

namespace WorldWarRadio.Patches
{
    [HarmonyPatch(typeof(PlayerShip), "SavesManagerOnLoaded")]
    public static class Patch_PlayerShip_SavesManagerOnLoaded
    {
        static void Postfix()
        {
            Debug.Log("[WorldWarRadio] Save loaded → scheduling speaker reset");
            SoundMixer.NeedsResetAfterLoad = true;
        }
    }
}
