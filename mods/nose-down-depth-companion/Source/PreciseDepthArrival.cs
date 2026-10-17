// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // Optional velocity preview, in the game's raw signed target coordinates.
    // This is a gameplay aid, not the deliberate overshoot/air-clearing test dive in §106.
    // The existing clamp calls it only after latching on an eligible II/VII hull.
    internal static class PreciseDepthArrival
    {
        private static float _offset, _lastTime = -1f, _lastOrder = float.NaN, _nextTrace;
        internal static float LastOffset { get { return _offset; } }

        internal static bool Selected(PlayerShip ship)
        {
            return CompanionConfig.Enabled && CompanionConfig.PreciseDepthArrival && ship != null
                && CompanionConfig.ShallowStandDown && CompanionConfig.ShallowTrimStandDown && HullKey.IsStandDownHull
                && (HullKey.IsVIIClampHull || (HullKey.IsIIClampHull && PlanesManning.OfficerSeated))
                && ship.TargetDepth >= 2f && ship.TargetDepth <= 10f;
        }

        internal static float Goal(float gap, float vy)
        {
            if (!Finite(gap) || !Finite(vy)) return 0f;
            if (Math.Abs(gap) < 0.2f && Math.Abs(vy) < 0.03f) return 0f;
            float weight = Mathf.Clamp((6f - Math.Abs(gap)) / 2f, 0f, 1f);
            return Mathf.Clamp(-3f * vy, -2f, 2f) * weight;
        }

        internal static float Offset(PlayerShip ship)
        {
            string phase = NoseDownBridge.PhaseName();
            if (!Selected(ship) || !NoseDownBridge.Present || ship.DeckDepth < 1.5f
                || NoseDownBridge.IsProtected(ship) || ship.RigidBody == null || ship.RigidBody.isKinematic
                || (phase != "OrdinaryDive" && phase != "OrdinaryDive/level" && phase != "Hold"))
            { Reset(); return 0f; }
            float now = Time.time;
            if (!Finite(now) || !Finite(ship.TargetDepth) || !Finite(ship.DeckDepth) || !Finite(ship.RigidBody.velocity.y))
            { Reset(); return 0f; }
            if (!Finite(_lastOrder) || Math.Abs(_lastOrder - ship.TargetDepth) > 0.05f || now < _lastTime)
            { Reset(); _lastOrder = ship.TargetDepth; }
            if (now == _lastTime) return _offset;
            float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
            _lastTime = now;
            float gap = ship.TargetDepth - ship.DeckDepth;
            float goal = Goal(gap, ship.RigidBody.velocity.y);
            float step = 0.75f * dt;
            _offset += Mathf.Clamp(goal - _offset, -step, step);
            if (CompanionConfig.TraceLogging && now >= _nextTrace)
            {
                _nextTrace = now + 0.5f;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.PreciseArrival] t={0:F1} gap={1:F2} vy={2:F3} goal={3:F2} offset={4:F2}",
                    now, gap, ship.RigidBody.velocity.y, goal, _offset));
            }
            return _offset;
        }

        internal static void Reset() { _offset = 0f; _lastTime = -1f; _lastOrder = float.NaN; }
        private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
    }
}
