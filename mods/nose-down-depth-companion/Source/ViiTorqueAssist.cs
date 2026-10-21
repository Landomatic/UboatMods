// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using System.Reflection;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // Read the stock law's completed terms and widen ONLY its final output clamp.
    // Its integral, integral limit, gains, planner and speed-angle limit remain stock.
    internal static class ViiTorqueAssist
    {
        private const float MaxDeck = 1f;
        private static object _law;
        private static FieldInfo _ff, _pd, _limit;
        private static PropertyInfo _integral;
        private static PlayerShip _ship;
        private static bool _applied, _engaged, _errorLogged;
        private static float _nextTrace;
        internal static bool Ready { get; private set; }

        internal static void Bind(Type patchType, Type lawType)
        {
            try
            {
                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo owner = patchType.GetField("Law", flags);
                _law = owner != null ? owner.GetValue(null) : null;
                _ff = lawType.GetField("LastFeedForward");
                _pd = lawType.GetField("LastProportionalDerivative");
                _limit = lawType.GetField("LastLimit");
                _integral = lawType.GetProperty("Integral");
                Ready = _law != null && _ff != null && _ff.FieldType == typeof(double)
                    && _pd != null && _pd.FieldType == typeof(double)
                    && _limit != null && _limit.FieldType == typeof(double)
                    && _integral != null && _integral.PropertyType == typeof(double) && _integral.CanRead;
            }
            catch { Ready = false; }
            if (!Ready)
                Debug.LogWarning(CompanionLoader.Tag + " VII torque assist unavailable; stock torque retained.");
        }

        internal static bool TryLimit(object instance, out double limit)
        {
            limit = 0;
            try
            {
                if (!Ready || !ReferenceEquals(instance, _law)) return false;
                limit = (double)_limit.GetValue(instance);
                return Finite(limit) && limit > 0;
            }
            catch { return false; }
        }

        internal static void BeginFrame(PlayerShip ship)
        {
            _ship = ship;
            _applied = false;
        }

        internal static void AfterCompute(object instance, ref double result)
        {
            try
            {
                if (!Ready || !CompanionConfig.CorrectionsEnabled || !CompanionConfig.ViiTorqueBoost
                    || !ReferenceEquals(instance, _law) || _ship == null || !HullKey.IsVIIClampHull
                    || ShallowStandDown.TrimScale <= 0.001f
                    || _ship.TargetDepth < 2f || _ship.TargetDepth > CompanionConfig.ClampMaxDepth
                    || _ship.DeckDepth > MaxDeck || _ship.TargetDepth <= _ship.DeckDepth
                    || NoseDownBridge.PhaseName() != "OrdinaryDive" || NoseDownBridge.IsProtected(_ship))
                    return;
                double factor = CompanionConfig.ViiTorqueFactor;
                if (!Finite(factor) || factor <= 1.0 || factor > 1.5) return;
                double limit = (double)_limit.GetValue(instance);
                double raw = (double)_ff.GetValue(instance) + (double)_pd.GetValue(instance)
                    + (double)_integral.GetValue(instance, null);
                if (!Finite(limit) || limit <= 0.0 || !Finite(raw) || !Finite(result)) return;
                if (Math.Abs(raw) <= limit) return;
                double boostedLimit = limit * factor;
                double boosted = Math.Max(-boostedLimit, Math.Min(boostedLimit, raw));
                if (Math.Abs(boosted) <= Math.Abs(result)) return;
                result = boosted;
                _applied = true;
                if (!_engaged)
                {
                    _engaged = true;
                    if (CompanionConfig.TraceLogging)
                        Debug.Log("[NoseDownCompanion.VIITorque] ENGAGE class=" + HullKey.Current
                            + " factor=" + factor.ToString("F2", CultureInfo.InvariantCulture)
                            + " output-cap-only; stock integrator unchanged");
                }
                if (CompanionConfig.TraceLogging && Time.time >= _nextTrace)
                {
                    _nextTrace = Time.time + 0.5f;
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.VIITorque] deck={0:F2} stockLimit={1:F1} boostedLimit={2:F1} torque={3:F1}",
                        _ship.DeckDepth, limit, boostedLimit, boosted));
                }
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    Debug.LogWarning(CompanionLoader.Tag + " VII torque assist stopped: " + e.Message);
                }
                Ready = false;
            }
        }

        internal static void EndFrame()
        {
            if (_engaged && !_applied)
            {
                _engaged = false;
                if (CompanionConfig.TraceLogging) Debug.Log("[NoseDownCompanion.VIITorque] RELEASE");
            }
            _ship = null;
        }

        internal static void Reset()
        {
            _applied = false;
            EndFrame();
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}

