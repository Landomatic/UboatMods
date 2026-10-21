// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
using System;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace NoseDownCompanion
{
    // Remove only the surfaced-depth restriction from the player's planes station.
    // Damage, docking, power, action/character qualifications and slot occupancy remain vanilla.
    // Station availability is shared by crew and officers; this does not assign anyone.
    [HarmonyPatch(typeof(Device), "SetDisableFlag", new Type[] { typeof(DeviceDisableFlag), typeof(bool) })]
    internal static class PreDivePlanes
    {
        private static PlayerShip _ship;
        private static bool _lastEnabled, _touched;
        private static float _nextRetry;

        private static void Prefix(Device __instance, DeviceDisableFlag __0, ref bool __1)
        {
            if (!CompanionConfig.CorrectionsEnabled || !CompanionConfig.PreDivePlanes || __0 != DeviceDisableFlag.Depth) return;
            var station = __instance as DivingPlanesStation;
            if (station != null && station.ParentEntity is PlayerShip) __1 = false;
        }

        // Refresh on ship/config changes, including when the option is turned OFF.
        internal static void Observe(PlayerShip ship)
        {
            if (ship == null) return;
            bool on = CompanionConfig.CorrectionsEnabled && CompanionConfig.PreDivePlanes;
            if (!ReferenceEquals(ship, _ship)) _touched = false;
            if (!on && !_touched) return;
            if (ReferenceEquals(ship, _ship) && on == _lastEnabled && Time.time < _nextRetry) return;
            bool changed = !ReferenceEquals(ship, _ship) || on != _lastEnabled;
            _ship = ship; _lastEnabled = on; _nextRetry = Time.time + 2f;
            try
            {
                var station = ship.GetEquipment<DivingPlanesStation>();
                if (station == null) return;
                station.SetDisableFlag(DeviceDisableFlag.Depth, !on && !ship.SubmergedOrGoingToSubmerge);
                _touched = on;
                if (changed && CompanionConfig.TraceLogging)
                    Debug.Log(CompanionLoader.Tag + " pre-dive planes=" + on + " stationFlags=" + station.DisableFlags);
            }
            catch (Exception e)
            {
                Debug.LogWarning(CompanionLoader.Tag + " pre-dive planes refresh failed: " + e.Message);
            }
        }
    }
}

