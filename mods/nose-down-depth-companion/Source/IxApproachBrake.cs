// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // v1.0.10 (ixDepthFix): Type IX ordinary-dive approach brake, any manning. Type IX otherwise
    // stays on stock Nose Down. Kugler holds the game's planes at zero on the IX; when the boat
    // sinks faster than v(gap) = max(ixBrakeMinRate, sqrt(2*ixBrakeDecel*gap)) within ixBrakeWindow m
    // of the order, a share w (0..1) of the game's plane torque is given back and the game's depth
    // target is set level with the boat, so the game's planes check the sink. Kugler's own trim
    // law keeps running. w ramps in/out; at w = 0 nothing is touched.
    // Never: crash dive, alarm, emergency blow/rise, blows (incl. decks awash), ascents, afloat.
    internal static class IxApproachBrake
    {
        private const float MinOrder = 2f;
        private const float MinDeck = 1.0f;
        private const float GapMin = -1.0f;
        private const float RampUp = 1.0f, RampDown = 0.5f; // w per second

        private static bool _active;
        private static float _w, _lastTime = -1f, _nextTrace;
        internal static float LastWeight { get { return _w; } }

        private static string Gate(PlayerShip ship, out float gap, out float sink)
        {
            gap = 0f; sink = 0f;
            if (!CompanionConfig.IxDepthFix) return "disabled";
            float order = ship.TargetDepth;
            if (order < MinOrder || order > CompanionConfig.ClampMaxDepth) return "order";
            if (NoseDownBridge.IsProtected(ship)) return "protected";
            string ph = NoseDownBridge.PhaseName();
            if (ph != "OrdinaryDive" && ph != "Hold") return "phase"; // unreadable phase = off
            var rb = ship.RigidBody;
            float vy = rb != null ? rb.velocity.y : 0f;
            sink = -vy;
            gap = order - ship.DeckDepth;
            if (ship.DeckDepth < MinDeck) return "afloat";
            if (gap > CompanionConfig.IxBrakeWindow) return "window";
            // v1.0.11: past the order keep braking while still sinking faster than pdBrakeHoldSink.
            if (gap < GapMin && sink <= CompanionConfig.PdBrakeHoldSink) return "pastOrderSlow";
            if (sink <= 0f) return "rising";
            return null;
        }

        private static float Envelope(float gap)
        {
            return Mathf.Max(CompanionConfig.IxBrakeMinRate, Mathf.Sqrt(2f * CompanionConfig.IxBrakeDecel * Mathf.Max(0f, gap)));
        }

        internal static void Apply(PlayerShip ship, ref float torque, ref float internalRaw, float orderRaw)
        {
            try
            {
                float now = Time.time;
                float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                _lastTime = now;
                float gap, sink;
                string block = Gate(ship, out gap, out sink);
                float goal = 0f, vlim = 0f;
                if (block == null)
                {
                    vlim = Envelope(gap);
                    if (!_active && sink > vlim * CompanionConfig.IxBrakeEngageFactor)
                    {
                        _active = true;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.IxBrake] ENGAGE order={0:F2} deck={1:F2} gap={2:F2} vy={3:F3} vlim={4:F3} phase={5}",
                            ship.TargetDepth, ship.DeckDepth, gap, -sink, vlim, NoseDownBridge.PhaseName()));
                    }
                    if (_active)
                        goal = Mathf.Clamp(CompanionConfig.IxBrakeGain * (sink - vlim), 0f, 1f);
                }
                else if (_active && block != "window" && block != "pastOrderSlow" && block != "rising")
                {
                    Release(block); // hard gate: protected, phase, order, afloat, disabled
                    return;
                }
                if (!_active)
                    return;
                float step = (goal > _w ? RampUp : RampDown) * dt;
                _w = goal > _w ? Math.Min(goal, _w + step) : Math.Max(goal, _w - step);
                if (_w > 0.001f)
                {
                    float held = torque;
                    float vanilla = held;
                    if (NoseDownBridge.ReleasePlanesHold(ship, ref vanilla))
                        torque = held * (1f - _w) + vanilla * _w;
                    // Game depth target level with the boat (raw: deeper = more negative), never deeper than the order.
                    float boatRaw = internalRaw;

                    // v1.0.11: absolute, not cumulative. v1.0.10 added gap*w to the game's own target every
                    // FixedUpdate, which ran the target above the sea (raw -61 -> +17 in 0.5 s) and drove
                    // the boat back to the surface. Now: order + w*(boat - order), never above the boat.
                    internalRaw = orderRaw + Math.Max(0f, gap) * _w;
                    if (CompanionConfig.TraceLogging && now >= _nextTrace)
                    {
                        _nextTrace = now + 0.5f;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.IxBrake] t={0:F1} gap={1:F2} vy={2:F3} vlim={3:F3} goal={4:F2} w={5:F2} internal {6:F2}->{7:F2} gate={8}",
                            now, gap, -sink, vlim, goal, _w, boatRaw, internalRaw, block ?? "on"));
                    }
                }
                else if (goal <= 0f)
                {
                    Release(block ?? "atEnvelope");
                }
            }
            catch { }
        }

        internal static void Release(string reason)
        {
            if (_active)
            {
                _active = false;
                Debug.Log("[NoseDownCompanion.IxBrake] RELEASE reason=" + reason
                    + " w=" + _w.ToString("F2", CultureInfo.InvariantCulture));
            }
            _w = 0f;
        }
    }
}
