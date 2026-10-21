// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // A short, bounded torque supplement before the existing VII handover.
    // No changes to the upstream integral, planner, transform, ballast or vertical velocity.
    internal static class DiveEntryAssist
    {
        private static PlayerShip _ship;
        private static float _nextTrace;
        internal static double LastAdded { get; private set; }
        internal static void BeginFrame(PlayerShip ship) { _ship = ship; LastAdded = 0; }
        internal static void EndFrame() { _ship = null; }

        internal static double Correction(double pitchRad, double rateRad, double inertia, double limit, double deck)
        {
            if (!Finite(pitchRad) || !Finite(rateRad) || !Finite(inertia) || !Finite(limit) || !Finite(deck)
                || inertia <= 0 || limit <= 0 || deck >= 1 || pitchRad >= 3 * Math.PI / 180) return 0;
            double error = Math.Max(0, 2 * Math.PI / 180 - pitchRad);
            double demand = inertia * (0.9 * error + 1.4 * Math.Max(0, -rateRad));
            double fade = Math.Max(0, Math.Min(1, (1 - deck) / 0.5));
            return Math.Min(0.5 * limit, demand) * fade;
        }

        internal static void Apply(double targetRad, double pitchRad, double pitchRateRad,
            double inertia, double limit, ref double result)
        {
            PlayerShip ship = _ship;
            if (!CompanionConfig.CorrectionsEnabled || !CompanionConfig.DiveEntryAssist || ship == null
                || !NoseDownBridge.Present || !HullKey.IsVIIClampHull || ShallowStandDown.TrimScale <= 0.001f
                || ship.TargetDepth < 2 || ship.TargetDepth > CompanionConfig.ClampMaxDepth
                || ship.TargetDepth - ship.DeckDepth < 2 || ship.DeckDepth >= 1
                || NoseDownBridge.PhaseName() != "OrdinaryDive" || NoseDownBridge.IsProtected(ship)
                || ship.RigidBody == null || ship.RigidBody.isKinematic || Time.timeScale > 10
                || !Finite(targetRad) || targetRad <= 0 || targetRad > 4 * Math.PI / 180
                || !Finite(result) || !Finite(ship.DeckDepth)) return;
            double add = Correction(pitchRad, pitchRateRad, inertia, limit, ship.DeckDepth);
            // Both torque options share a maximum total of 1.5 times stock authority.
            double next = Math.Min(1.5 * limit, result + add);
            if (next <= result || !Finite(next)) return;
            LastAdded = next - result;
            result = next;
            if (CompanionConfig.TraceLogging && Time.time >= _nextTrace)
            {
                _nextTrace = Time.time + 0.5f;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.EntryAssist] t={0:F1} deck={1:F2} pitch={2:F2} rate={3:F2} extra={4:F0} total={5:F0} cap={6:F0}",
                    Time.time, ship.DeckDepth, pitchRad * 180 / Math.PI, pitchRateRad * 180 / Math.PI,
                    LastAdded, result, 1.5 * limit));
            }
        }

        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }
}

