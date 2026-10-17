// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // v1.0.13 (surfaceBrake): ordinary surface order (order 0) from submerged, hull list only
    // (default VIIC41). During Kugler's Ascent, before the game has started blowing, the rise is
    // driven by his bow-up trim alone; on the VIIC41 the bridge breaks surface at ~4 m and sinks
    // back because the blow starts ~21 s after the order (VIIB ~9 s). Between surfaceBrakeTop and
    // surfaceBrakeStop deck depth, if the boat rises faster than v(h) = max(minRate, sqrt(2*decel*h)),
    // h = deck - stop, his trim torque is scaled down (never below 1 - surfaceBrakeMaxCut) so the
    // boat arrives at the stop depth slowly instead of breaching. Released at once when any blow
    // starts (boat mass falls 2 t, or a blowing valve mode / emergency phase is seen), below the
    // stop depth, or when the rise is slow. Blow timing, valves and tanks are never touched.
    internal static class SurfaceBrake
    {
        private const float MassDropShare = 0.998f; // blow seen once mass falls 0.2% (~2.3 t on a 1159 t VIIC41)
        private const float RampUp = 1.0f, RampDown = 0.5f;

        private static bool _active, _blowSeen;
        private static float _w, _lastTime = -1f, _nextTrace, _startMass = float.NaN;
        private static bool _armed; // set once per surface order while still heavy

        internal static bool HullListed
        {
            get
            {
                string c = HullKey.Current;
                if (c == null || c == "TypeIX" || c == "other") return false; // IX always excluded
                return Array.IndexOf(CompanionConfig.SurfaceBrakeHulls, c) >= 0;
            }
        }

        // Returns the trim-law share to keep this frame (1 = untouched).
        internal static float Scale(PlayerShip ship)
        {
            try
            {
                if (!CompanionConfig.SurfaceBrakeOn || !CompanionConfig.SurfaceTrimBrake || ship == null || !HullListed || ship.TargetDepth > 0.05f)
                    return Reset("order");
                float now = Time.time;
                float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                _lastTime = now;
                var rb = ship.RigidBody;
                if (rb == null) return Reset("noBody");
                if (!_armed)
                {
                    _armed = true;
                    _blowSeen = false;
                    _startMass = rb.mass;
                }
                if (!_blowSeen && (rb.mass < _startMass * MassDropShare || NoseDownBridge.IsProtected(ship)))
                {
                    _blowSeen = true; // blow underway (or emergency/alarm/blowing valve): hands off for this order
                    if (_active) Release("blow");
                }
                if (_blowSeen) return 1f;
                string block = null;
                if (NoseDownBridge.PhaseName() != "Ascent") block = "phase";
                float deck = ship.DeckDepth, vy = rb.velocity.y;
                float h = deck - CompanionConfig.SurfaceBrakeStop;
                if (block == null && (deck > CompanionConfig.SurfaceBrakeTop || h <= 0f)) block = "window";
                if (block == null && vy <= CompanionConfig.SurfaceBrakeMinRate) block = "slow";
                float vlim = Mathf.Max(CompanionConfig.SurfaceBrakeMinRate, Mathf.Sqrt(2f * CompanionConfig.SurfaceBrakeDecel * Mathf.Max(0f, h)));
                float goal = 0f;
                if (block == null)
                {
                    if (!_active && vy > vlim * 1.05f)
                    {
                        _active = true;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.SurfaceBrake] ENGAGE class={0} deck={1:F2} vy={2:F3} vlim={3:F3} mass={4:F1}",
                            HullKey.Current, deck, vy, vlim, rb.mass));
                    }
                    if (_active)
                        goal = Mathf.Clamp(CompanionConfig.SurfaceBrakeGain * (vy - vlim), 0f, 1f);
                }
                else if (_active && block == "phase")
                {
                    Release(block);
                    return 1f;
                }
                if (!_active) return 1f;
                float step = (goal > _w ? RampUp : RampDown) * dt;
                _w = goal > _w ? Math.Min(goal, _w + step) : Math.Max(goal, _w - step);
                float keep = 1f - _w * CompanionConfig.SurfaceBrakeMaxCut;
                if (CompanionConfig.TraceLogging && now >= _nextTrace)
                {
                    _nextTrace = now + 0.5f;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.SurfaceBrake] t={0:F1} deck={1:F2} vy={2:F3} vlim={3:F3} w={4:F2} trimKeep={5:F2} gate={6}",
                        now, deck, vy, vlim, _w, keep, block ?? "on"));
                }
                if (_w <= 0.001f && goal <= 0f)
                {
                    Release(block ?? "atEnvelope");
                    return 1f;
                }
                return keep;
            }
            catch
            {
                return 1f;
            }
        }

        private static float Reset(string reason)
        {
            if (_active) Release(reason);
            _armed = false;
            _blowSeen = false;
            _lastTime = -1f;
            return 1f;
        }

        internal static void ResetState(string reason)
        {
            Reset(reason);
            Release(reason);
        }

        internal static void Release(string reason)
        {
            if (_active)
            {
                _active = false;
                Debug.Log("[NoseDownCompanion.SurfaceBrake] RELEASE reason=" + reason
                    + " w=" + _w.ToString("F2", CultureInfo.InvariantCulture));
            }
            _w = 0f;
        }
    }

    // v1.0.14 (surfaceHold): plane-hold for listed hulls (default VIIC41, VIIB), any manning.
    // On an ordinary surface order during Kugler's Ascent, before any blow, a share w of the game's
    // plane torque is handed back and the game's internal depth target is set ABSOLUTE each step
    // at surfaceHoldDeck (raw = VerticalOriginOffset - holdDeck; never accumulated), so the game's
    // planes level the boat just under the surface until the blow lifts it. Released at once when
    // the blow starts (mass -0.2%, blowing valve / alarm / emergency via IsProtected), the phase
    // leaves Ascent, the order changes, or after surfaceHoldMaxSeconds (anti-stall). Blow timing,
    // valves and tanks are never touched.
    internal static class SurfaceHold
    {
        private const float MassDropShare = 0.998f;
        private static bool _armed, _blowSeen, _active, _done;
        private static float _w, _lastTime = -1f, _nextTrace, _startMass = float.NaN, _engagedAt;

        internal static void Apply(PlayerShip ship, ref float torque, ref float internalRaw)
        {
            try
            {
                if (!CompanionConfig.SurfaceBrakeOn || !CompanionConfig.SurfaceHoldOn || ship == null || !SurfaceBrake.HullListed || ship.TargetDepth > 0.05f)
                {
                    Reset("order");
                    return;
                }
                var rb = ship.RigidBody;
                if (rb == null) { Reset("noBody"); return; }
                float now = Time.time;
                float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                _lastTime = now;
                if (!_armed)
                {
                    _armed = true; _blowSeen = false; _done = false; _startMass = rb.mass;
                }
                if (!_blowSeen && (rb.mass < _startMass * MassDropShare || NoseDownBridge.IsProtected(ship)))
                {
                    _blowSeen = true;
                    if (_active) Release("blow");
                }
                if (_blowSeen || _done) return;
                if (NoseDownBridge.PhaseName() != "Ascent")
                {
                    if (_active) { Release("phase"); _done = true; }
                    return;
                }
                float deck = ship.DeckDepth, vy = rb.velocity.y;
                if (!_active)
                {
                    if (deck > CompanionConfig.SurfaceHoldTop || deck < CompanionConfig.SurfaceHoldDeck - 1f) return;
                    _active = true;
                    _engagedAt = now;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.SurfaceHold] ENGAGE class={0} deck={1:F2} vy={2:F3} mass={3:F1} holdDeck={4:F2}",
                        HullKey.Current, deck, vy, rb.mass, CompanionConfig.SurfaceHoldDeck));
                }
                if (now - _engagedAt > CompanionConfig.SurfaceHoldMaxSeconds)
                {
                    Release("timeout");
                    _done = true;
                    return;
                }
                float goal = CompanionConfig.SurfaceHoldShare;
                float rate = goal > _w ? CompanionConfig.SurfaceHoldRampUp : CompanionConfig.SurfaceHoldRampDown;
                _w = goal > _w ? Math.Min(goal, _w + rate * dt) : Math.Max(goal, _w - rate * dt);
                if (_w <= 0.001f) return;
                float held = torque, vanilla = held;
                if (NoseDownBridge.ReleasePlanesHold(ship, ref vanilla))
                    torque = held * (1f - _w) + vanilla * _w;
                float before = internalRaw;
                internalRaw = ship.VerticalOriginOffset - CompanionConfig.SurfaceHoldDeck; // absolute every step
                if (CompanionConfig.TraceLogging && now >= _nextTrace)
                {
                    _nextTrace = now + 0.5f;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.SurfaceHold] t={0:F1} deck={1:F2} vy={2:F3} w={3:F2} internal {4:F2}->{5:F2} mass={6:F1} held={7:F1}s",
                        now, deck, vy, _w, before, internalRaw, rb.mass, now - _engagedAt));
                }
            }
            catch { }
        }

        internal static bool Active { get { return _active; } }

        private static void Reset(string reason)
        {
            if (_active) Release(reason);
            _armed = false; _blowSeen = false; _done = false; _lastTime = -1f;
        }

        internal static void ResetState(string reason)
        {
            Reset(reason);
            Release(reason);
        }

        internal static void Release(string reason)
        {
            if (_active)
            {
                _active = false;
                Debug.Log("[NoseDownCompanion.SurfaceHold] RELEASE reason=" + reason
                    + " w=" + _w.ToString("F2", CultureInfo.InvariantCulture));
            }
            _w = 0f;
        }
    }
}
