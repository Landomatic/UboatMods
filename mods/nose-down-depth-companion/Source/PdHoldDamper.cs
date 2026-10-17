// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // v1.0.15 (C/D): PD-hold damping. The game's depth keeping pushes in proportion to depth error only
    // (no rate term), and an officer doubles that push, so the hull rises and falls about the order
    // (IIA: ~17 s, +/-0.6 m; IXA: ~15 s, +/-0.4 m in the v1.0.14 log). After the boat has first reached
    // the order, the game's depth target is offset by -pdDampKd x vy (raw: deeper = more negative; rising
    // -> target deeper, sinking -> target shallower), capped at +/- pdDampCap m and slewed at pdDampSlew m/s.
    // Hulls from pdDampHulls (IIA, IID, IXA by default; other Type IX never). 2-10 m orders, Nose Down Hold
    // phase only. Off while the PD or IX approach brake acts. Never crash dive, alarm, emergency blow/rise,
    // blowing valve modes (incl. decks awash). Only acts after arrival, so approach speed is unchanged.
    internal static class PdHoldDamper
    {
        private const float MinOrder = 2f, MaxOrder = 10f;
        private const float MinDeck = 0.5f;
        private const float ArriveBand = 0.1f;
        private const float OrderBand = 0.05f;

        private static bool _arrived, _engaged;
        private static float _lastOrder = float.NaN, _lastGap = float.NaN;
        private static float _offset, _lastTime = -1f, _nextTrace;
        internal static bool Engaged { get { return _engaged; } }

        internal static bool HullListed
        {
            get
            {
                string v = HullKey.Variant ?? HullKey.Current;
                return v != null && Array.IndexOf(CompanionConfig.PdDampHulls, v) >= 0;
            }
        }

        private static string Gate(PlayerShip ship, out float gap, out float vy)
        {
            gap = 0f; vy = 0f;
            if (!CompanionConfig.PdHoldDamp) return "disabled";
            if (ship == null || !HullListed) return "hull";
            if (CompanionConfig.PdDampOfficerOnly && !PlanesManning.OfficerSeated) return "noOfficer";
            float order = ship.TargetDepth;
            if (order < MinOrder || order > MaxOrder) return "order";
            if (NoseDownBridge.IsProtected(ship)) return "protected";
            if (NoseDownBridge.PhaseName() != "Hold") return "phase";
            if (ship.DeckDepth < MinDeck) return "afloat";
            var rb = ship.RigidBody;
            vy = rb != null ? rb.velocity.y : 0f;
            gap = order - ship.DeckDepth;
            if (PdApproachBrake.LastOffset > 0.001f) return "pdBrake";
            if (IxApproachBrake.LastWeight > 0.001f) return "ixBrake";
            return null;
        }

        // Metres added to the raw internal target (positive = shallower).
        internal static float Offset(PlayerShip ship)
        {
            try
            {
                float now = Time.time;
                if (now == _lastTime)
                    return _offset; // Prefix and Postfix both ask in one FixedUpdate
                float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                _lastTime = now;

                float order = ship != null ? ship.TargetDepth : float.NaN;
                if (float.IsNaN(_lastOrder) || Math.Abs(order - _lastOrder) > OrderBand)
                {
                    if (_engaged) Release("orderChange");
                    _arrived = false;
                    _lastGap = float.NaN;
                }
                _lastOrder = order;

                float gap, vy;
                string block = Gate(ship, out gap, out vy);
                if (block != null && block != "pdBrake" && block != "ixBrake")
                {
                    _arrived = false;
                    _lastGap = float.NaN;
                    if (_engaged) Release(block);
                    return 0f;
                }
                if (!_arrived && block == null)
                {
                    // first reach: within the band, or gap changed sign since last frame
                    if (Math.Abs(gap) <= ArriveBand || (!float.IsNaN(_lastGap) && (gap > 0f) != (_lastGap > 0f)))
                    {
                        _arrived = true;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.PdDamp] ARRIVED class={0} order={1:F2} deck={2:F2} vy={3:F3} officer={4}",
                            HullKey.Variant ?? HullKey.Current, order, ship.DeckDepth, vy, PlanesManning.OfficerSeated));
                    }
                }
                if (block == null)
                    _lastGap = gap;

                float goal = 0f;
                if (_arrived && block == null)
                {
                    goal = Mathf.Clamp(-CompanionConfig.PdDampKd * vy, -CompanionConfig.PdDampCap, CompanionConfig.PdDampCap);
                    if (!_engaged)
                    {
                        _engaged = true;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.PdDamp] ENGAGE class={0} kd={1:F2} cap={2:F2}",
                            HullKey.Variant ?? HullKey.Current, CompanionConfig.PdDampKd, CompanionConfig.PdDampCap));
                    }
                }
                if (!_engaged)
                    return 0f;
                float step = CompanionConfig.PdDampSlew * dt;
                _offset = goal > _offset ? Math.Min(goal, _offset + step) : Math.Max(goal, _offset - step);
                if (CompanionConfig.TraceLogging && now >= _nextTrace)
                {
                    _nextTrace = now + 0.5f;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.PdDamp] t={0:F1} gap={1:F2} vy={2:F3} goal={3:F2} offset={4:F2} gate={5}",
                        now, gap, vy, goal, _offset, block ?? "on"));
                }
                return _offset;
            }
            catch
            {
                return 0f;
            }
        }

        internal static void ResetState(string reason)
        {
            Release(reason);
            _arrived = false;
            _lastOrder = float.NaN;
            _lastGap = float.NaN;
            _lastTime = -1f;
        }

        internal static void Release(string reason)
        {
            if (_engaged)
            {
                _engaged = false;
                Debug.Log("[NoseDownCompanion.PdDamp] RELEASE reason=" + reason
                    + " offset=" + _offset.ToString("F2", CultureInfo.InvariantCulture));
            }
            _offset = 0f;
        }
    }
}
