// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // v1.0.16 (ixTargetPin): Type IX ordinary-dive target pin. On a dive order deeper than ixPinMinOrder m
    // the game's moving internal depth target runs past the order (v1.0.15 IXA log: 50 m order, internal
    // target about 2.9 m deeper), so once the approach brake lets go the hull creeps to ~52 m. After the boat
    // has first crossed the order (deck depth reaches it, having been above it), the game's internal depth
    // target is pinned at the order until the order changes - the same write the Type VII/II DepthTargetClamp
    // makes (internalTargetDepth only; no ballast, valve, buoyancy, torque or rigidbody writes).
    // Hulls from ixPinHulls (IXA, IXC, IXC40). Orders from ixPinMinOrder (default 10 m, so 2-10 m periscope-depth
    // holds and the IXA pdHoldDamp stay exactly as in v1.0.15) up to clampMaxDepth. Nose Down phase OrdinaryDive
    // or Hold only. Never crash dive, alarm, emergency blow/rise, blowing valve modes (incl. decks awash), ascents
    // (the boat must have been above the order), or afloat. Any manning: it only holds the target at the order,
    // so a manned dive is never slower than an unmanned one.
    internal static class IxTargetPin
    {
        private const float MinDeck = 1.0f;
        private const float ArmGap = 0.25f;
        private const float OrderBand = 0.05f;

        private static bool _armed, _latched;
        private static float _lastOrder = float.NaN, _latchedRaw = float.NaN, _nextTrace;

        internal static bool Latched { get { return _latched; } }

        internal static bool HullListed
        {
            get
            {
                string v = HullKey.Variant ?? HullKey.Current;
                return v != null && Array.IndexOf(CompanionConfig.IxPinHulls, v) >= 0;
            }
        }

        private static string Gate(PlayerShip ship, out float gap)
        {
            gap = 0f;
            if (!CompanionConfig.IxTargetPin) return "disabled";
            if (ship == null || HullKey.Current != "TypeIX" || !HullListed) return "hull";
            float order = ship.TargetDepth;
            if (order <= CompanionConfig.IxPinMinOrder || order > CompanionConfig.ClampMaxDepth) return "order";
            if (NoseDownBridge.IsProtected(ship)) return "protected";
            string ph = NoseDownBridge.PhaseName();
            if (ph != "OrdinaryDive" && ph != "Hold") return "phase"; // unreadable phase = off
            if (ship.DeckDepth < MinDeck) return "afloat";
            gap = order - ship.DeckDepth;
            return null;
        }

        // Called from the IX branch of ShallowStandDown.Prefix (after Nose Down's prefix and the IX brake).
        // targetRaw / internalRaw are the game's raw fields (deeper = more negative).
        internal static void Pre(PlayerShip ship, float targetRaw, ref float internalRaw)
        {
            try
            {
                float order = ship.TargetDepth;
                if (float.IsNaN(_lastOrder) || Math.Abs(order - _lastOrder) > OrderBand)
                {
                    if (_latched) Release("orderChange");
                    _armed = false;
                }
                _lastOrder = order;

                float gap;
                string block = Gate(ship, out gap);
                if (block != null)
                {
                    if (_latched) Release(block);
                    _armed = false;
                    return;
                }
                if (!_latched)
                {
                    if (gap > ArmGap)
                        _armed = true; // boat is above the order: this is a dive
                    else if (_armed && gap <= 0f)
                    {
                        _latched = true;
                        _latchedRaw = targetRaw;
                        var rb = ship.RigidBody;
                        Debug.Log(string.Format(CultureInfo.InvariantCulture,
                            "[NoseDownCompanion.IXPin] LATCH class={0} order={1:F2} deck={2:F2} vy={3:F3} internalDeeperThanOrder={4:F2} brake={5:F2}",
                            HullKey.Variant ?? HullKey.Current, order, ship.DeckDepth, rb != null ? rb.velocity.y : 0f,
                            targetRaw - internalRaw, IxApproachBrake.LastWeight));
                    }
                }
                if (_latched)
                    Write(ship, targetRaw, ref internalRaw, order);
            }
            catch { }
        }

        // Called from DepthTargetClamp.Postfix (runs last, after the game has advanced its target).
        internal static void Post(PlayerShip ship, float targetRaw, ref float internalRaw)
        {
            try
            {
                if (!_latched)
                    return;
                if (Math.Abs(targetRaw - _latchedRaw) > OrderBand || !CompanionConfig.IxTargetPin || NoseDownBridge.IsProtected(ship))
                {
                    Release(NoseDownBridge.IsProtected(ship) ? "protected" : "orderChange");
                    return;
                }
                Write(ship, targetRaw, ref internalRaw, ship.TargetDepth);
            }
            catch { }
        }

        private static void Write(PlayerShip ship, float targetRaw, ref float internalRaw, float order)
        {
            float before = internalRaw;
            internalRaw = targetRaw + (PdHoldDamper.Engaged ? PdHoldDamper.Offset(ship) : 0f);
            if (CompanionConfig.TraceLogging && Time.time >= _nextTrace)
            {
                _nextTrace = Time.time + 0.5f;
                var rb = ship.RigidBody;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.IXPin] t={0:F1} order={1:F2} deck={2:F2} vy={3:F3} internalDeeperThanOrder {4:F2}->{5:F2}",
                    Time.time, order, ship.DeckDepth, rb != null ? rb.velocity.y : 0f, targetRaw - before, targetRaw - internalRaw));
            }
        }

        internal static void Release(string reason)
        {
            if (_latched)
            {
                _latched = false;
                Debug.Log("[NoseDownCompanion.IXPin] RELEASE reason=" + reason);
            }
            _latched = false;
            _latchedRaw = float.NaN;
        }

        internal static void Reset()
        {
            _armed = false;
            _latched = false;
            _lastOrder = float.NaN;
            _latchedRaw = float.NaN;
        }
    }
}
