// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Optional diagnostic adapter for Nose Down by Kugler; contains no upstream code.
using System;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace NoseDownCompanion
{
    internal static class DiagnosticIsolation
    {
        private static PlayerShip _ship;
        private static bool _ready, _warned;
        private static float _retry;
        private static long _bypassCalls, _observedCalls;
        internal static bool Ready { get { return _ready; } }
        internal static long BypassCalls { get { return _bypassCalls; } }
        internal static long ObservedCalls { get { return _observedCalls; } }

        internal static bool CanBypass(bool locked, bool requested, bool ready, bool playerTank)
        {
            return locked && requested && ready && playerTank;
        }

        internal static bool AppliedTo(PlayerShip ship)
        {
            return CanBypass(CompanionConfig.DiagnosticOnly,
                CompanionConfig.DiagnosticDisableNoseDownCasing, _ready,
                ReferenceEquals(ship, _ship) && _bypassCalls > 0);
        }

        internal static void Observe(PlayerShip ship)
        {
            if (!ReferenceEquals(ship, _ship))
            {
                _ship = ship;
                _bypassCalls = 0;
                _observedCalls = 0;
                _retry = 0;
            }
            if (_ready || ship == null || CompanionLoader.Instance == null || Time.unscaledTime < _retry) return;
            _retry = Time.unscaledTime + 2f;
            try
            {
                var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(HullInternalTank), "FixedUpdate"));
                if (info == null) return;
                foreach (var patch in info.Postfixes)
                {
                    var target = patch.PatchMethod;
                    if (target == null || target.DeclaringType == null
                        || target.DeclaringType.FullName != "DivePitch.CasingFlooding") continue;
                    var loader = target.DeclaringType.Assembly.GetType("DivePitch.ModLoader");
                    var version = loader == null ? null : AccessTools.Field(loader, "BuildId");
                    var parameters = target.GetParameters();
                    if (version == null || !version.IsLiteral || !Equals(version.GetRawConstantValue(), "v2.1.5")
                        || !target.IsStatic || target.ReturnType != typeof(void)
                        || parameters.Length != 1 || parameters[0].ParameterType != typeof(HullInternalTank))
                    {
                        Warn("unsupported Nose Down casing contract; bypass unavailable");
                        return;
                    }
                    CompanionLoader.Instance.Patch(target,
                        prefix: new HarmonyMethod(typeof(DiagnosticIsolation), "BeforeCasing"));
                    _ready = true;
                    Debug.Log(CompanionLoader.Tag + " diagnostic casing hook ready (Nose Down v2.1.5; player tank only)");
                    return;
                }
            }
            catch (Exception e) { Warn("casing hook unavailable: " + e.GetType().Name); }
        }

        // This is a prefix on the upstream postfix method, not on base-game flooding.
        // __0 names its first argument; __instance would mean the static method's null receiver.
        private static bool BeforeCasing(HullInternalTank __0)
        {
            bool playerTank = _ship != null && __0 != null && ReferenceEquals(__0, _ship.HullInternalTank);
            if (playerTank) _observedCalls++;
            if (!CanBypass(CompanionConfig.DiagnosticOnly,
                CompanionConfig.DiagnosticDisableNoseDownCasing, _ready, playerTank)) return true;
            _bypassCalls++;
            return false;
        }

        private static void Warn(string message)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(CompanionLoader.Tag + " " + message);
        }
    }
}
