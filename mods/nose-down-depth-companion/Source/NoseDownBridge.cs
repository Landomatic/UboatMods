// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace NoseDownCompanion
{
    // Read-only bridge to Kugler's Nose Down. Finds his FixedUpdate patch class through
    // Harmony's patch registry (owner "com.kugler.divepitch"), so no assembly scan is needed.
    internal static class NoseDownBridge
    {
        internal const string KuglerHarmonyId = "com.kugler.divepitch";
        private static bool _resolved;
        private static int _attempts;
        private static float _nextAttempt;
        private static bool _absentLogged;

        internal static bool Present { get; private set; }
        private static object _planner;
        private static PropertyInfo _phaseProp;
        private static FieldInfo _heldShipField;
        private static FieldInfo _heldTorqueField;
        private static FieldInfo _pitchField;

        private static PlayerShip _valveOwner;
        private static FloodingAndBlowingValve _valve;

        internal static void EnsureResolved()
        {
            if (_resolved || Time.realtimeSinceStartup < _nextAttempt)
                return;
            _attempts++;
            _nextAttempt = Time.realtimeSinceStartup + 5f;
            try
            {
                MethodBase original = AccessTools.Method(typeof(PlayableSubmarine), "FixedUpdate");
                Patches info = original != null ? Harmony.GetPatchInfo(original) : null;
                Type t = null;
                if (info != null)
                    foreach (Patch p in info.Prefixes)
                        if (p.owner == KuglerHarmonyId && p.PatchMethod != null)
                            t = p.PatchMethod.DeclaringType;
                if (t != null)
                {
                    FieldInfo pf = AccessTools.Field(t, "Planner");
                    _planner = pf != null ? pf.GetValue(null) : null;
                    _phaseProp = _planner != null ? AccessTools.Property(_planner.GetType(), "Phase") : null;
                    _heldShipField = AccessTools.Field(t, "_planesHeldShip");
                    _heldTorqueField = AccessTools.Field(t, "_planesTorque");
                    Present = true;
                    _resolved = true;
                    PatchTrimLaw(t);
                    try { Type ps = t.Assembly.GetType("DivePitch.PitchState"); _pitchField = ps != null ? AccessTools.Field(ps, "PitchDeg") : null; } catch { _pitchField = null; }
                    Debug.Log(CompanionLoader.Tag + " Nose Down found (" + t.FullName + "). planner phase="
                        + (_phaseProp != null ? "readable" : "UNREADABLE, using alarm/blow-mode fallback")
                        + " planesHold=" + (_heldShipField != null && _heldTorqueField != null ? "readable" : "UNREADABLE, shallow stand-down off"));
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(CompanionLoader.Tag + " Nose Down lookup failed: " + e.Message);
            }
            if (_attempts >= 3)
            {
                _resolved = true;
                Present = false;
                if (!_absentLogged)
                {
                    _absentLogged = true;
                    Debug.LogWarning(CompanionLoader.Tag + " Nose Down by Kugler (Workshop 3729673507) is not active. Companion is idle (no-op). Subscribe to Nose Down and load this companion after it.");
                }
            }
        }

        // v1.0.5: Kugler's PitchControlLaw.Compute is called only from his FixedUpdate Postfix (stock 2.1.4).
        // A prefix returns 0 torque (and bleeds his integrator, as his own Standby branch does) while
        // ShallowStandDown has flagged this frame. Runtime patch only; his files are untouched.
        internal static bool TrimLawPatched { get; private set; }

        private static void PatchTrimLaw(Type patchType)
        {
            try
            {
                Type law = patchType.Assembly.GetType("DivePitch.PitchControlLaw");
                MethodInfo compute = law != null ? AccessTools.Method(law, "Compute") : null;
                MethodInfo prefix = AccessTools.Method(typeof(ShallowStandDown), "LawComputePrefix");
                if (compute == null || prefix == null || CompanionLoader.Instance == null)
                {
                    Debug.LogWarning(CompanionLoader.Tag + " Nose Down trim law not found; shallow trim stand-down off.");
                    return;
                }
                MethodInfo postfix = AccessTools.Method(typeof(ShallowStandDown), "LawComputePostfix");
                CompanionLoader.Instance.Patch(compute, new HarmonyMethod(prefix), postfix != null ? new HarmonyMethod(postfix) : null);
                TrimLawPatched = true;
                ViiTorqueAssist.Bind(patchType, law);
                Debug.Log(CompanionLoader.Tag + " shallow trim stand-down hooked (" + law.FullName + ".Compute).");
            }
            catch (Exception e)
            {
                Debug.LogWarning(CompanionLoader.Tag + " shallow trim stand-down unavailable: " + e.Message);
            }
        }

        // v1.0.10: Kugler's measured hull pitch (deg, his sign: + = bow down), or null if unreadable.
        internal static float? PitchDeg()
        {
            try
            {
                if (_pitchField == null)
                    return null;
                return (float)_pitchField.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        // Kugler planner phase name, or null if unreadable.
        internal static string PhaseName()
        {
            try
            {
                if (_phaseProp == null || _planner == null)
                    return null;
                object v = _phaseProp.GetValue(_planner, null);
                return v != null ? v.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        internal static FloodingAndBlowingValve Valve(PlayerShip ship)
        {
            if (ReferenceEquals(ship, _valveOwner))
                return _valve;
            _valveOwner = ship;
            try { _valve = ship.GetEquipment<FloodingAndBlowingValve>(); } catch { _valve = null; }
            return _valve;
        }

        // Crash/alarm dive, emergency blow/rise, any blowing incl. decks-awash blow.
        internal static bool IsProtected(PlayerShip ship)
        {
            if (ship == null || ship.Alarmed)
                return true;
            string ph = PhaseName();
            if (ph == "AlarmEntry" || ph == "AlarmDeep" || ph == "EmergencyBlow")
                return true;
            var valve = Valve(ship);
            if (valve != null)
            {
                var m = valve.Mode;
                if (m == FloodingAndBlowingValve.OperatingMode.FullBlowing
                    || m == FloodingAndBlowingValve.OperatingMode.BlowingDeckAwash
                    || m == FloodingAndBlowingValve.OperatingMode.OptimalBlowing)
                    return true;
            }
            return false;
        }

        // Restore the vanilla plane torque Kugler's Prefix zeroed this frame, if he holds it.
        internal static bool ReleasePlanesHold(PlayableSubmarine sub, ref float verticalSteersTorque)
        {
            if (_heldShipField == null || _heldTorqueField == null)
                return false;
            try
            {
                object held = _heldShipField.GetValue(null);
                if (!ReferenceEquals(held, sub))
                    return false;
                verticalSteersTorque = (float)_heldTorqueField.GetValue(null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void ResetShip()
        {
            _valveOwner = null;
            _valve = null;
        }
    }
}
