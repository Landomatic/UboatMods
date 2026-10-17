// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // v1.0.9: officer-only periscope-depth approach brake, Type VIIB/VIIC/VIIC41/IIA/IID (IID only while iidOfficerClamp=true).
    // In the last few metres of an ordinary 2-10 m dive, if the boat sinks faster than a
    // braking envelope v(gap) = max(vMin, sqrt(2*a*gap)), the pinned internal depth target is
    // lifted (shallower) by up to maxOffset m so the game's planes ease off. Ramps in and out,
    // releases once the sink rate is back inside the envelope or the boat has settled.
    // deck >= 0.5 m gate keeps it clear of the IIs' iiAfloatTrim phase. Never active for Type IX, sailor/unmanned planes, crash dive, alarm, blows, decks awash,
    // ascents (vy >= 0), or any order outside 2-10 m. Only internalTargetDepth is touched.
    internal static class PdApproachBrake
    {
        private const float MinOrder = 2f, MaxOrder = 10f;
        private const float GapMin = -0.3f;      // m past the order still covered (overshoot side)
        private const float MinDeck = 0.5f;      // deck must be under; matches ShallowStandDown.IiAfloatDeck
        private const float RampUp = 1.5f, RampDown = 1.0f; // m of offset per second
        private const float SettleVy = 0.05f, SettleGap = 0.3f;

        private static bool _active;
        private static float _offset, _goal, _lastTime = -1f;
        private static float _nextTrace;
        internal static float LastOffset { get { return _offset; } }

        internal static float Envelope(float gap)
        {
            return Mathf.Max(CompanionConfig.PdBrakeMinRate, Mathf.Sqrt(2f * CompanionConfig.PdBrakeDecel * Mathf.Max(0f, gap)));
        }

        private static string Gate(PlayerShip ship, out float gap, out float sink)
        {
            gap = 0f; sink = 0f;
            if (!CompanionConfig.OfficerPdBrake) return "disabled";
            if (ship == null || !(HullKey.IsVIIClampHull || HullKey.IsTypeII)) return "hull"; // VIIB/VIIC/VIIC41/IIA/IID (IID via iidOfficerClamp); IX never
            if (!PlanesManning.OfficerSeated) return "noOfficer";
            float order = ship.TargetDepth;
            if (order < MinOrder || order > MaxOrder) return "order";
            if (NoseDownBridge.IsProtected(ship)) return "protected";
            string ph = NoseDownBridge.PhaseName();
            if (ph != null && ph != "OrdinaryDive" && ph != "OrdinaryDive/level" && ph != "Hold") return "phase";
            var rb = ship.RigidBody;
            float vy = rb != null ? rb.velocity.y : 0f;
            sink = -vy;
            gap = order - ship.DeckDepth;
            if (ship.DeckDepth < MinDeck) return "afloat";
            if (gap > CompanionConfig.PdBrakeWindow) return "window";
            // v1.0.10: past the order (gap < -0.3 m) keep braking while still sinking faster than pdBrakeHoldSink.
            if (gap < GapMin && sink <= CompanionConfig.PdBrakeHoldSink) return "pastOrderSlow";
            if (Math.Abs(vy) < SettleVy && Math.Abs(gap) < SettleGap) return "settled";
            if (sink <= 0f) return "rising";
            return null;
        }

        // Positive result = metres to lift the raw internal target (raw: deeper = more negative).
        internal static float Offset(PlayerShip ship)
        {
            try
            {
                float now = Time.time;
                if (now == _lastTime)
                    return _offset; // already evaluated this frame (Prefix and Postfix both ask)
                float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                _lastTime = now;

                float gap, sink;
                string block = Gate(ship, out gap, out sink);
                if (block != null)
                {
                    _goal = 0f;
                    if (_active && (block != "window" && block != "pastOrderSlow" && block != "settled" && block != "rising"))
                    {
                        _offset = 0f; // hard gate (protected, phase, manning, hull, order): drop at once
                        Release(block);
                        return 0f;
                    }
                }
                else
                {
                    float vlim = Envelope(gap);
                    if (!_active)
                    {
                        if (sink > vlim * CompanionConfig.PdBrakeEngageFactor)
                        {
                            _active = true;
                            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                                "[NoseDownCompanion.PdBrake] ENGAGE class={0} order={1:F2} deck={2:F2} gap={3:F2} vy={4:F3} vlim={5:F3}",
                                HullKey.Current, ship.TargetDepth, ship.DeckDepth, gap, -sink, vlim));
                        }
                    }
                    _goal = _active ? Mathf.Clamp(CompanionConfig.PdBrakeGain * (sink - vlim), 0f, CompanionConfig.PdBrakeMaxOffset) : 0f;
                }

                if (_active)
                {
                    float step = (_goal > _offset ? RampUp : RampDown) * dt;
                    _offset = _goal > _offset ? Math.Min(_goal, _offset + step) : Math.Max(_goal, _offset - step);
                    if (CompanionConfig.TraceLogging && now >= _nextTrace)
                    {
                        _nextTrace = now + 0.5f;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.PdBrake] t={0:F1} gap={1:F2} vy={2:F3} vlim={3:F3} goal={4:F2} offset={5:F2} gate={6}",
                            now, gap, -sink, block == null ? Envelope(gap) : 0f, _goal, _offset, block ?? "on"));
                    }
                    if (_goal <= 0f && _offset <= 0.001f)
                    {
                        _offset = 0f;
                        Release(block ?? "atEnvelope");
                    }
                }
                return _offset;
            }
            catch
            {
                return 0f;
            }
        }

        internal static void Release(string reason)
        {
            if (_active)
            {
                _active = false;
                Debug.Log("[NoseDownCompanion.PdBrake] RELEASE reason=" + reason
                    + " offset=" + _offset.ToString("F2", CultureInfo.InvariantCulture));
            }
            _offset = 0f;
            _goal = 0f;
        }
    }
}
