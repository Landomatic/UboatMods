// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // After Kugler's Prefix zeroes vanilla plane torque, give it back for ordinary 2-10 m
    // orders on clamped hulls, so the game's own depth keeping (with our target stop) runs.
    // v1.0.5: also holds his trim law at 0 torque for those orders (PitchControlLaw.Compute prefix).
    // v1.0.10: Type II afloat-trim handover eases his trim out (TrimScale) instead of cutting it;
    // VIIC41 plane damping in Hold near the order; Type IX ordinary-dive approach brake (IxApproachBrake).
    [HarmonyPatch(typeof(PlayableSubmarine), "FixedUpdate")]
    internal static class ShallowStandDown
    {
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(NoseDownBridge.KuglerHarmonyId)]
        private static void Prefix(PlayableSubmarine __instance, ref float ___verticalSteersTorque, ref float ___internalTargetDepth, float ___targetDepth)
        {
            var ship = __instance as PlayerShip;
            ViiTorqueAssist.BeginFrame(ship);
            DiveEntryAssist.BeginFrame(ship);
            try
            {
                if (!CompanionConfig.Enabled || !NoseDownBridge.Present)
                    return;
                if (ship == null)
                    return;
                if (HullKey.Current == "TypeIX")
                {
                    IxApproachBrake.Apply(ship, ref ___verticalSteersTorque, ref ___internalTargetDepth, ___targetDepth);
                    // v1.0.15 (D): IXA PD-hold damping, only while the approach brake is idle.
                    if (IxApproachBrake.LastWeight <= 0.001f)
                    {
                        float d = PdHoldDamper.Offset(ship);
                        if (PdHoldDamper.Engaged)
                        {
                            float baseRaw = Math.Abs(___internalTargetDepth - ___targetDepth) < 0.75f ? ___targetDepth : ___internalTargetDepth;
                            ___internalTargetDepth = baseRaw + d;
                            _ixDampWrote = true;
                        }
                        else if (_ixDampWrote)
                        {
                            // put the game's target back on the order once, unless a protected mode (alarm/blow) owns it
                            _ixDampWrote = false;
                            if (!NoseDownBridge.IsProtected(ship) && Math.Abs(___internalTargetDepth - ___targetDepth) < 1.5f)
                                ___internalTargetDepth = ___targetDepth;
                        }
                    }
                    // v1.0.16: IX ordinary-dive target pin (after the boat first crosses the order).
                    IxTargetPin.Pre(ship, ___targetDepth, ref ___internalTargetDepth);
                    return;
                }
                // v1.0.14: surfacing plane-hold (game planes, absolute internal target at holdDeck).
                SurfaceHold.Apply(ship, ref ___verticalSteersTorque, ref ___internalTargetDepth);
                // v1.0.13: surfacing brake (order 0, listed hulls, before any blow) scales Kugler's trim.
                float keep = SurfaceBrake.Scale(ship);
                if (keep < 0.999f && NoseDownBridge.TrimLawPatched)
                {
                    TrimScale = keep;
                    return;
                }
                if (!CompanionConfig.ShallowStandDown || ship.TargetDepth < 2f || ship.TargetDepth > 10f)
                {
                    ResetDiveState();
                    return;
                }
                // v1.0.6: any manning (not tied to the clamp's officer gate); light IIA/IID hulls otherwise
                // sit at 0.3 m under Nose Down's 2 deg afloat hold. standDownHulls=clamped restores the 1.0.5 gate.
                // v1.0.7: VIIB/VIIC/VIIC41/IIA/IID only; Type IX and unknown hulls stay stock.
                if (!HullKey.IsStandDownHull)
                    return;
                if (CompanionConfig.StandDownClampedOnly ? !DepthTargetClamp.EligibleNow(ship) : NoseDownBridge.IsProtected(ship))
                {
                    ResetDiveState();
                    return;
                }
                // v1.0.15 (A): VIIB/VIIC/VIIC41 keep Nose Down's dive trim until the deck is viiHandoverDeck m
                // under, then ease his trim out and the game's planes in over viiTrimFadeSeconds.
                if (CompanionConfig.ViiDeckHandover && HullKey.IsVIIClampHull)
                {
                    float w = ViiHandover(ship);
                    if (w <= 0f)
                        return; // his trim and plane hold stay as they are
                    float held = ___verticalSteersTorque;
                    float vanilla = held;
                    if (NoseDownBridge.ReleasePlanesHold(__instance, ref vanilla))
                        ___verticalSteersTorque = held * (1f - w) + vanilla * w;
                    C41Damp(ship, ref ___verticalSteersTorque);
                    if (CompanionConfig.ShallowTrimStandDown && NoseDownBridge.TrimLawPatched)
                        TrimScale = 1f - w;
                    return;
                }
                NoseDownBridge.ReleasePlanesHold(__instance, ref ___verticalSteersTorque);
                C41Damp(ship, ref ___verticalSteersTorque);
                if (!(CompanionConfig.ShallowTrimStandDown && NoseDownBridge.TrimLawPatched))
                    return;
                // v1.0.8: a Type II still afloat (deck < 0.5 m) keeps Nose Down's 2 deg afloat trim.
                // v1.0.10: once under, his trim is eased out over iiTrimFadeSeconds (or until |pitch| < iiTrimFadePitch).
                if (CompanionConfig.IiAfloatTrim && HullKey.IsTypeII)
                    TrimScale = IiHandover(ship);
                else
                    TrimScale = 0f;
            }
            catch { }
            finally
            {
                if (ship != null) PdDepthBias.Apply(ship, ___targetDepth, ref ___internalTargetDepth);
            }
        }

        // Share of Kugler's trim-law torque kept this FixedUpdate: 1 = his, 0 = held off.
        // Reset to 1 in EndFrame (DepthTargetClamp's last-running Postfix).
        internal static float TrimScale = 1f;
        private const float IiAfloatDeck = 0.5f;
        private static bool _loggedState;
        private static int _lastState = -1;

        private static float _fadeStart = float.NaN;
        private static bool _fadeDone;
        private static float _lastPitch = float.NaN, _lastPitchTime, _pitchRate;
        private static bool _c41Arrived;
        private static float _c41LastOrder = float.NaN;
        private static float _nextDampTrace;

        internal static void ResetState(string reason)
        {
            ResetDiveState();
            TrimScale = 1f;
            _ixDampWrote = false;
            _c41LastOrder = float.NaN;
            SurfaceBrake.ResetState(reason);
            SurfaceHold.ResetState(reason);
        }

        private static void ResetDiveState()
        {
            _fadeStart = float.NaN;
            _fadeDone = false;
            _c41Arrived = false;
            ViiReset(null);
        }

        // v1.0.15 (A): 0 = Nose Down keeps everything, 1 = fully handed to the game's planes.
        private static bool _ixDampWrote;
        private static float _viiStart = float.NaN;
        private static bool _viiDone;
        private static float _nextViiTrace;

        private static void ViiReset(PlayerShip ship)
        {
            if (!float.IsNaN(_viiStart) && CompanionConfig.TraceLogging)
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.VIIHandover] RESET class={0} deck={1}", HullKey.Current,
                    ship != null ? ship.DeckDepth.ToString("F2", CultureInfo.InvariantCulture) : "n/a"));
            _viiStart = float.NaN;
            _viiDone = false;
        }

        private static float ViiHandover(PlayerShip ship)
        {
            if (ship.DeckDepth < CompanionConfig.ViiHandoverDeck)
            {
                ViiReset(ship);
                return 0f;
            }
            if (_viiDone)
                return 1f;
            float now = Time.time;
            float? pitch = NoseDownBridge.PitchDeg();
            string ps = pitch.HasValue ? pitch.Value.ToString("F1", CultureInfo.InvariantCulture) : "n/a";
            if (float.IsNaN(_viiStart))
            {
                _viiStart = now;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.VIIHandover] START class={0} order={1:F2} deck={2:F2} pitch={3} phase={4} fade={5:F1}s",
                    HullKey.Current, ship.TargetDepth, ship.DeckDepth, ps, NoseDownBridge.PhaseName(), CompanionConfig.ViiTrimFadeSeconds));
            }
            float w = (now - _viiStart) / Math.Max(0.1f, CompanionConfig.ViiTrimFadeSeconds);
            if (w >= 1f)
            {
                _viiDone = true;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.VIIHandover] DONE after={0:F1}s deck={1:F2} pitch={2}", now - _viiStart, ship.DeckDepth, ps));
                return 1f;
            }
            if (CompanionConfig.TraceLogging && now >= _nextViiTrace)
            {
                _nextViiTrace = now + 0.5f;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.VIIHandover] t={0:F1} w={1:F2} deck={2:F2} pitch={3}", now, w, ship.DeckDepth, ps));
            }
            return Math.Max(0f, w);
        }

        private static float IiHandover(PlayerShip ship)
        {
            if (ship.DeckDepth < IiAfloatDeck)
            {
                _fadeStart = float.NaN;
                _fadeDone = false;
                return 1f;
            }
            if (_fadeDone)
                return 0f;
            float now = Time.time;
            float? pitch = NoseDownBridge.PitchDeg();
            if (float.IsNaN(_fadeStart))
            {
                _fadeStart = now;
                _lastPitch = float.NaN;
                _pitchRate = 0f;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.IIHandover] START class={0} deck={1:F2} pitch={2} fade={3:F1}s",
                    HullKey.Current, ship.DeckDepth, pitch.HasValue ? pitch.Value.ToString("F1", CultureInfo.InvariantCulture) : "n/a",
                    CompanionConfig.IiTrimFadeSeconds));
            }
            // v1.0.12: track pitch rate; finish on pitch only once the bow has stopped falling fast
            // (1.0.11 finished at 3 deg while still falling at ~3.9 deg/s, and pitch overshot to 0.3 deg).
            if (pitch.HasValue)
            {
                if (!float.IsNaN(_lastPitch) && now - _lastPitchTime > 0.05f)
                {
                    _pitchRate = (pitch.Value - _lastPitch) / (now - _lastPitchTime);
                    _lastPitch = pitch.Value;
                    _lastPitchTime = now;
                }
                else if (float.IsNaN(_lastPitch))
                {
                    _lastPitch = pitch.Value;
                    _lastPitchTime = now;
                    _pitchRate = 0f;
                }
            }
            float s = 1f - (now - _fadeStart) / Math.Max(0.1f, CompanionConfig.IiTrimFadeSeconds);
            string reason = null;
            if (pitch.HasValue && Math.Abs(pitch.Value) < CompanionConfig.IiTrimFadePitch && Math.Abs(_pitchRate) < CompanionConfig.IiTrimFadeRate && now - _fadeStart >= 0.5f) // v1.0.14: >= 0.5 s before rate-based finish
                reason = "pitch";
            else if (s <= 0f)
                reason = "time";
            if (reason != null)
            {
                _fadeDone = true;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.IIHandover] DONE reason={0} after={1:F1}s deck={2:F2} pitch={3} rate={4:F2}",
                    reason, now - _fadeStart, ship.DeckDepth, pitch.HasValue ? pitch.Value.ToString("F1", CultureInfo.InvariantCulture) : "n/a", _pitchRate));
                return 0f;
            }
            return Math.Max(0f, Math.Min(1f, s));
        }

        // v1.0.10: VIIC41 only. In Kugler's Hold, after the boat has first reached the order, scale the
        // game's plane torque down near the order (factor c41DampMin at the order, 1 at the band edge).
        private static void C41Damp(PlayerShip ship, ref float torque)
        {
            if (!CompanionConfig.C41PlaneDamping || HullKey.Current != "TypeVIIC41")
                return;
            float order = ship.TargetDepth;
            if (float.IsNaN(_c41LastOrder) || Math.Abs(order - _c41LastOrder) > 0.05f || ship.DeckDepth < IiAfloatDeck)
                _c41Arrived = false;
            _c41LastOrder = order;
            float gap = order - ship.DeckDepth;
            if (gap <= 0.3f)
                _c41Arrived = true;
            if (!_c41Arrived || NoseDownBridge.PhaseName() != "Hold")
                return;
            float band = Math.Max(0.1f, CompanionConfig.C41DampBand);
            float a = Math.Abs(gap);
            if (a >= band)
                return;
            float f = CompanionConfig.C41DampMin + (1f - CompanionConfig.C41DampMin) * (a / band);
            torque *= f;
            if (CompanionConfig.TraceLogging && Time.time >= _nextDampTrace)
            {
                _nextDampTrace = Time.time + 0.5f;
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "[NoseDownCompanion.C41Damp] t={0:F1} gap={1:F2} factor={2:F2}", Time.time, gap, f));
            }
        }

        internal static void EndFrame()
        {
            int st = TrimScale >= 0.999f ? 0 : (TrimScale <= 0.001f ? 2 : 1);
            if (CompanionConfig.TraceLogging && (!_loggedState || st != _lastState))
            {
                _loggedState = true;
                _lastState = st;
                Debug.Log("[NoseDownCompanion.StandDown] Nose Down trim torque "
                    + (st == 2 ? "HELD OFF (2-10 m order, game planes only)" : (st == 1 ? "SCALED (Type II/VII handover or surfacing brake)" : "active")));
            }
            TrimScale = 1f;
            ViiTorqueAssist.EndFrame();
            DiveEntryAssist.EndFrame();
        }

        // Prefix on DivePitch.PitchControlLaw.Compute (patched at runtime by NoseDownBridge).
        private static bool LawComputePrefix(object __instance, double dt, ref double __result)
        {
            if (TrimScale > 0.001f)
                return true; // full or partial: his law runs, the Postfix scales the result
            try
            {
                MethodInfo bleed = AccessTools.Method(__instance.GetType(), "Bleed");
                if (bleed != null)
                    bleed.Invoke(__instance, new object[] { dt });
            }
            catch { }
            __result = 0.0;
            return false;
        }

        private static void LawComputePostfix(object __instance, double targetRad, double pitchRad,
            double pitchRateRad, double inertia, ref double __result)
        {
            ViiTorqueAssist.AfterCompute(__instance, ref __result);
            double limit;
            if (ViiTorqueAssist.TryLimit(__instance, out limit))
                DiveEntryAssist.Apply(targetRad, pitchRad, pitchRateRad, inertia, limit, ref __result);
            if (TrimScale > 0.001f && TrimScale < 0.999f)
                __result *= TrimScale;
        }
    }
}
