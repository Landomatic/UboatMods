// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // Optional extra PD clearance. The visible order stays unchanged. Compose an
    // absolute raw target; never add the bias to an already biased target.
    internal static class PdDepthBias
    {
        private const float OrderBand = 0.05f, ArriveBand = 0.15f, SettleBand = 0.75f;
        private static PlayerShip _ship;
        private static float _lastRaw = float.NaN, _lastGap = float.NaN;
        private static float _lastTime = -1f, _offset, _nextTrace;
        private static float _orderChangedAt = float.NegativeInfinity;
        private static PlayerShip _writeShip;
        private static bool _wrote;
        private static float _writtenRaw, _baseRaw, _writeOrder;
        private static bool _arrived, _engaged, _errorLogged;
        internal static float LastOffset { get { return _offset; } }

        internal static bool HullListed
        {
            get
            {
                string hull = HullKey.Variant ?? HullKey.Current;
                return hull == "TypeIIA" || hull == "TypeIID" || hull == "TypeIXA";
            }
        }

        private static string Gate(PlayerShip ship)
        {
            if (!CompanionConfig.Enabled || !CompanionConfig.PdDepthBias) return "disabled";
            if (!NoseDownBridge.Present || ship == null || !HullListed) return "hull";
            if (CompanionConfig.PdBiasOfficerOnly && !PlanesManning.OfficerSeated) return "noOfficer";
            if (ship.TargetDepth < 2f || ship.TargetDepth > 10f) return "order";
            if (NoseDownBridge.IsProtected(ship)) return "protected";
            if (NoseDownBridge.PhaseName() != "Hold") return "phase";
            if (ship.DeckDepth < 0.5f || ship.RigidBody == null) return "afloat";
            if (PdApproachBrake.LastOffset > 0.001f || IxApproachBrake.LastWeight > 0.001f
                || AscentBrake.LastOffset > 0.001f) return "approachBrake";
            float amount = CompanionConfig.PdBiasMeters;
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f || amount > 0.5f)
                return "amount";
            return null;
        }

        internal static void Apply(PlayerShip ship, float targetRaw, ref float internalRaw)
        {
            try
            {
                if (PreciseDepthArrival.Selected(ship))
                {
                    // The arrival clamp owns this target; do not restore a previous bias
                    // over its new write, or stack a different settled-depth objective.
                    _wrote = false;
                    Clear("preciseArrival");
                    return;
                }
                if (!ReferenceEquals(ship, _ship))
                {
                    Reset("shipChange");
                    _ship = ship;
                }
                bool changed = !float.IsNaN(_lastRaw) && Math.Abs(targetRaw - _lastRaw) > OrderBand;
                _lastRaw = targetRaw;
                if (changed)
                {
                    Clear("orderChange");
                    _orderChangedAt = Time.time;
                    _wrote = false;
                    return; // never write on the frame of a new order
                }
                if (Time.time == _orderChangedAt) return;
                string block = Gate(ship);
                if (block != null)
                {
                    // Remove only a target that is still exactly ours. New orders and
                    // protected modes own their targets and must never be overwritten.
                    if (_wrote && ReferenceEquals(ship, _writeShip) && ship != null
                        && !NoseDownBridge.IsProtected(ship) && Math.Abs(targetRaw - _writeOrder) <= OrderBand
                        && Math.Abs(internalRaw - _writtenRaw) <= 0.0001f)
                        internalRaw = CompanionConfig.Enabled ? _baseRaw : targetRaw;
                    _wrote = false;
                    Clear(block);
                    return;
                }
                float now = Time.time;
                if (now != _lastTime)
                {
                    float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                    _lastTime = now;
                    float gap = ship.TargetDepth - ship.DeckDepth;
                    float vy = ship.RigidBody.velocity.y;
                    if (!_arrived)
                    {
                        bool crossed = !float.IsNaN(_lastGap) && (gap > 0f) != (_lastGap > 0f);
                        bool settled = Math.Abs(gap) <= SettleBand && Math.Abs(vy) <= 0.05f;
                        _arrived = Math.Abs(gap) <= ArriveBand || crossed || settled;
                    }
                    _lastGap = gap;
                    if (_arrived)
                    {
                        float step = CompanionConfig.PdBiasSlew * dt;
                        float goal = CompanionConfig.PdBiasMeters;
                        _offset = goal > _offset ? Math.Min(goal, _offset + step) : Math.Max(goal, _offset - step);
                    }
                }
                if (!_arrived || _offset <= 0f) return;
                float damper = PdHoldDamper.Offset(ship);
                internalRaw = targetRaw + damper - _offset;
                _baseRaw = targetRaw + damper;
                _writtenRaw = internalRaw;
                _writeOrder = targetRaw;
                _writeShip = ship;
                _wrote = true;
                if (!_engaged)
                {
                    _engaged = true;
                    if (CompanionConfig.TraceLogging)
                        Debug.Log("[NoseDownCompanion.PdBias] ENGAGE class=" + (HullKey.Variant ?? HullKey.Current)
                            + " order=" + ship.TargetDepth.ToString("F2", CultureInfo.InvariantCulture)
                            + " extraDepth=" + CompanionConfig.PdBiasMeters.ToString("F2", CultureInfo.InvariantCulture));
                }
                if (CompanionConfig.TraceLogging && now >= _nextTrace)
                {
                    _nextTrace = now + 0.5f;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.PdBias] t={0:F1} order={1:F2} deck={2:F2} bias={3:F3} damper={4:F3}",
                        now, ship.TargetDepth, ship.DeckDepth, _offset, damper));
                }
            }
            catch (Exception e)
            {
                Clear("error");
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    Debug.LogWarning(CompanionLoader.Tag + " PD depth bias skipped: " + e.Message);
                }
            }
        }

        private static void Clear(string reason)
        {
            if (_engaged && CompanionConfig.TraceLogging)
                Debug.Log("[NoseDownCompanion.PdBias] RELEASE reason=" + reason);
            _arrived = false;
            _engaged = false;
            _offset = 0f;
            _lastGap = float.NaN;
            _lastTime = -1f;
        }

        internal static void Reset(string reason)
        {
            Clear(reason);
            _ship = null;
            _lastRaw = float.NaN;
            _orderChangedAt = float.NegativeInfinity;
        }
    }
}
