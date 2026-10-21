// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // Depth-target stop (NCT3-NCT8f behaviour, Landomatic). Vanilla's moving internal depth
    // target on Type VII/II hulls overshoots the order and reverses (the PD/depth hunt); Type IX
    // pins it at the order. Once internalTargetDepth first reaches/crosses the order, pin it
    // there until the order changes. Only internalTargetDepth is written: no ballast, valve,
    // buoyancy, torque or rigidbody writes. Ascent brake (5-12 m orders, officer seated) may
    // shift the pinned target up to 3 m deeper only while the rise exceeds the unmanned envelope,
    // so a manned ascent is never slower than an unmanned one.
    [HarmonyPatch(typeof(PlayableSubmarine), "FixedUpdate")]
    internal static class DepthTargetClamp
    {
        private const float MinOrder = 2f;
        private const float ResetBand = 0.05f;

        private static PlayerShip _ship;
        private static float _preTarget, _preInternal;
        private static bool _preValid, _latched;
        private static bool _disabled;
        private static float _latchedTarget = float.NaN;
        private static bool _gateLogged, _gateOn;
        private static float _nextTrace;
        private static float _lastOrderRaw = float.NaN;

        internal static bool EligibleNow(PlayerShip ship)
        {
            if (!CompanionConfig.CorrectionsEnabled || ship == null || !NoseDownBridge.Present)
                return false;
            float t = ship.TargetDepth;
            if (t < MinOrder || t > CompanionConfig.ClampMaxDepth)
                return false;
            bool hull = HullKey.IsVIIClampHull || (HullKey.IsIIClampHull && PlanesManning.OfficerSeated);
            return hull && !NoseDownBridge.IsProtected(ship);
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(PlayableSubmarine __instance, ref float ___targetDepth, ref float ___internalTargetDepth)
        {
            var ship = __instance as PlayerShip;
            if (ship == null)
                return;
            DiagnosticIsolation.Observe(ship);
            EntryDiagnostics.Begin(ship);
            PreDivePlanes.Observe(ship);
            try
            {
                NoseDownBridge.EnsureResolved();
                if (!NoseDownBridge.Present)
                    return;
                if (!CompanionConfig.CorrectionsEnabled)
                {
                    if (!_disabled) ResetControlState("disabled");
                    _disabled = true;
                    return;
                }
                _disabled = false;
                if (!ReferenceEquals(ship, _ship))
                {
                    _ship = ship;
                    HullKey.Reset();
                    PlanesManning.Reset();
                    NoseDownBridge.ResetShip();
                    ResetControlState("shipChange");
                }
                HullKey.Update(ship);
                PlanesManning.IsManned(ship);
                PdWaveForce.ObserveShip(ship);

                bool eligible = EligibleNow(ship);
                LogGate(ship, eligible);
                float oldOrderRaw = _lastOrderRaw;
                bool orderChanged = !float.IsNaN(oldOrderRaw) && Math.Abs(___targetDepth - oldOrderRaw) > ResetBand;
                _lastOrderRaw = ___targetDepth;
                if (!eligible || (_latched && Math.Abs(___targetDepth - _latchedTarget) > ResetBand))
                    ResetClamp();
                // v1.0.3: order changed while the game's moving internal target is still heading
                // for the old order and is already past the new one (e.g. 51 m -> 36 m mid-dive):
                // pin it at the new order now instead of waiting for it to drift back across.
                if (eligible && orderChanged && !_latched)
                    RetargetIfOvershooting(ship, oldOrderRaw, ___targetDepth, ref ___internalTargetDepth);

                _preTarget = ___targetDepth;
                _preInternal = ___internalTargetDepth;
                _preValid = eligible;
                if (_latched)
                    ___internalTargetDepth = ___targetDepth + ArrivalOffset(ship);
            }
            catch { }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PlayableSubmarine __instance, ref float ___targetDepth, ref float ___internalTargetDepth)
        {
            var ship = __instance as PlayerShip;
            EntryDiagnostics.End(ship);
            if (ship == null || !NoseDownBridge.Present)
                return;
            ShallowStandDown.EndFrame();
            try
            {
                if (!CompanionConfig.CorrectionsEnabled) return;
                // v1.0.16: Type IX target pin (own gate; IX is never an EligibleNow hull).
                if (HullKey.Current == "TypeIX")
                    IxTargetPin.Post(ship, ___targetDepth, ref ___internalTargetDepth);
                if (!EligibleNow(ship))
                {
                    ResetClamp();
                    return;
                }
                if (_latched && Math.Abs(___targetDepth - _latchedTarget) > ResetBand)
                    ResetClamp();

                if (!_latched && _preValid)
                {
                    float before = _preInternal - _preTarget;
                    float after = ___internalTargetDepth - ___targetDepth;
                    if ((before < 0f && after >= 0f) || (before > 0f && after <= 0f) || Math.Abs(after) <= 0.0001f)
                    {
                        _latched = true;
                        _latchedTarget = ___targetDepth;
                        if (CompanionConfig.TraceLogging)
                            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                                "[NoseDownCompanion.Clamp] LATCH t={0:F2} class={1} target={2:F2} preInternal={3:F3} vanillaInternal={4:F3}",
                                Time.time, HullKey.Current, ___targetDepth, _preInternal, ___internalTargetDepth));
                    }
                }
                if (_latched)
                    ___internalTargetDepth = ___targetDepth + ArrivalOffset(ship);

                if (CompanionConfig.TraceLogging && Time.time >= _nextTrace)
                {
                    _nextTrace = Time.time + 0.5f;
                    var rb = ship.RigidBody;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.Trace] t={0:F1} class={1} order={2:F2} deck={3:F2} vy={4:F3} internal={5:F3} clamp={6} officer={7} phase={8} brake={9:F2} pdBrake={10:F2}",
                        Time.time, HullKey.Current, ship.TargetDepth, ship.DeckDepth, rb != null ? rb.velocity.y : 0f,
                        ___internalTargetDepth, _latched ? 1 : 0, PlanesManning.OfficerSeated, NoseDownBridge.PhaseName() ?? "n/a", AscentBrake.LastOffset, PdApproachBrake.LastOffset));
                }
            }
            catch { }
            finally
            {
                PdDepthBias.Apply(ship, ___targetDepth, ref ___internalTargetDepth);
            }
        }

        // Select one arrival controller: do not stack the preview with legacy brakes/damping.
        private static float ArrivalOffset(PlayerShip ship)
        {
            if (PreciseDepthArrival.Selected(ship))
            {
                AscentBrake.Release("preciseArrival");
                PdApproachBrake.Release("preciseArrival");
                PdHoldDamper.ResetState("preciseArrival");
                return PreciseDepthArrival.Offset(ship);
            }
            PreciseDepthArrival.Reset();
            return -AscentBrake.Offset(ship) + PdApproachBrake.Offset(ship) + PdHoldDamper.Offset(ship);
        }

        // Raw signed coordinates (game fields: deeper = more negative).
        private static void RetargetIfOvershooting(PlayerShip ship, float oldOrderRaw, float newOrderRaw, ref float internalRaw)
        {
            float dInternal = internalRaw - newOrderRaw;
            float dOld = oldOrderRaw - newOrderRaw;
            float dBoat = -(ship.DeckDepth - ship.TargetDepth); // same sign convention as raw
            if (Math.Abs(dInternal) <= 0.0001f || Math.Sign(dInternal) != Math.Sign(dOld))
                return; // internal target is not on the old order's side of the new order
            // v1.0.4: only when the new order lies between the boat and the internal target (a
            // shallower order given mid-dive, or a deeper one mid-climb). A plain deeper order from a
            // held depth (internal still at the old order, boat just past it) keeps the game's ramp.
            if (Math.Sign(dBoat) == 0 || Math.Sign(dInternal) == Math.Sign(dBoat))
                return;
            float before = internalRaw;
            internalRaw = newOrderRaw;
            _latched = true;
            _latchedTarget = newOrderRaw;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[NoseDownCompanion.Clamp] RETARGET class={0} order {1:F1} -> {2:F1} m deck={3:F1} internal {4:F1} -> {2:F1} m",
                HullKey.Current, -oldOrderRaw + (ship.TargetDepth + newOrderRaw), ship.TargetDepth, ship.DeckDepth,
                -before + (ship.TargetDepth + newOrderRaw)));
        }

        private static void LogGate(PlayerShip ship, bool on)
        {
            if (!HullKey.IsIIClampHull)
                return;
            if (_gateLogged && on == _gateOn)
                return;
            _gateLogged = true;
            _gateOn = on;
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[NoseDownCompanion.IIOfficerClamp] {0} class={1} order={2:F2} deck={3:F2} officerSeated={4}",
                on ? "ENGAGE" : "DISENGAGE", HullKey.Current, ship.TargetDepth, ship.DeckDepth, PlanesManning.OfficerSeated));
        }

        internal static void ResetControlState(string reason)
        {
            ResetClamp();
            ResetShipState();
            IxTargetPin.Reset();
            IxApproachBrake.Release(reason);
            PdHoldDamper.ResetState(reason);
            ShallowStandDown.ResetState(reason);
            PdDepthBias.Reset(reason);
            ViiTorqueAssist.Reset();
            PreciseDepthArrival.Reset();
            PdWaveForce.Reset(reason);
        }

        internal static void ResetShipState()
        {
            _lastOrderRaw = float.NaN;
        }

        private static void ResetClamp()
        {
            _preValid = false;
            _latched = false;
            _latchedTarget = float.NaN;
            PreciseDepthArrival.Reset();
            AscentBrake.Release("reset");
            PdApproachBrake.Release("reset");
        }
    }

    internal static class AscentBrake
    {
        private const float WindowMax = 6.0f, WindowMin = -0.3f;
        private const float MinOrder = 5.0f, MaxOrder = 12.0f;
        private const float EngageFactor = 1.10f, Gain = 4.0f, MaxOffset = 3.0f;
        private static bool _active;
        internal static float LastOffset { get; private set; }

        private static float Envelope(float h)
        {
            return Mathf.Max(0.35f, 0.10f + Mathf.Sqrt(2f * 0.07f * Mathf.Max(0f, h)));
        }

        internal static float Offset(PlayerShip ship)
        {
            try
            {
                if (!CompanionConfig.MannedAscentBrake || ship == null)
                    return Release("disabled");
                bool? manned = PlanesManning.IsManned(ship);
                if (manned.HasValue && !manned.Value)
                    return Release("unmanned");
                float order = ship.TargetDepth, deck = ship.DeckDepth;
                var rb = ship.RigidBody;
                float vy = rb != null ? rb.velocity.y : 0f;
                float h = deck - order;
                if (order < MinOrder || order > MaxOrder || vy <= 0f || h < WindowMin || h > WindowMax)
                    return Release("window");
                float vlim = Envelope(h);
                if (!_active)
                {
                    if (vy <= vlim * EngageFactor)
                        return 0f;
                    _active = true;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.AscentBrake] ENGAGE class={0} order={1:F2} deck={2:F2} vy={3:F3} vlim={4:F3} gate={5}",
                        HullKey.Current, order, deck, vy, vlim, manned.HasValue ? "officer" : "rateOnly"));
                }
                else if (vy <= vlim)
                {
                    return Release("atEnvelope");
                }
                LastOffset = Mathf.Clamp(Gain * (vy - vlim), 0f, MaxOffset);
                return LastOffset;
            }
            catch
            {
                return 0f;
            }
        }

        internal static float Release(string reason)
        {
            if (_active)
            {
                _active = false;
                Debug.Log("[NoseDownCompanion.AscentBrake] RELEASE reason=" + reason
                    + " lastOffset=" + LastOffset.ToString("F2", CultureInfo.InvariantCulture));
            }
            LastOffset = 0f;
            return 0f;
        }
    }
}

