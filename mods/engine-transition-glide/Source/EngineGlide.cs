// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Core;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace EngineTransitionGlide
{
    // Game behaviour (PlayerShip.SetActiveEngines / ActiveEngines setter): the outgoing engine gets
    // DeviceDisableFlag.Unmounted -> OnDisable zeroes its PowerModifier and currentFinalPerformance at
    // once; the incoming engine starts from 0 and PlayerShipEngine.DoFixedUpdate ramps it up with
    // lerp factor 0.18*dt (time constant ~5.6 s, ~13 s to 90%). Propeller force = ActiveEngines.Power.
    // That thrust gap is the "brake". This mod seeds the incoming engine with the thrust the outgoing
    // one was actually making (capped by the incoming engine's own preset power and MaxPower) and fades
    // that floor out over glideSeconds; the game's own ramp takes over underneath.
    internal static class Glide
    {
        private static readonly FieldInfo CurrentFinal = AccessTools.Field(typeof(PlayerShipEngine), "currentFinalPerformance");
        private static readonly FieldInfo LoadStationary = AccessTools.Field(typeof(PlayerShipEngine), "loadBatteryStationary");
        private static readonly FieldInfo Queue = AccessTools.Field(typeof(PlayerShipEngine), "executionQueue");
        private static readonly FieldInfo RealPerf = AccessTools.Field(typeof(PlayerShipEngine), "realPerformance");
        private static readonly MethodInfo DoFixed = AccessTools.Method(typeof(PlayerShipEngine), "DoFixedUpdate");

        internal static PlayerShipEngine Outgoing;
        internal static float OutgoingPower;

        private static PlayerShipEngine _target;
        private static float _carried;
        private static float _start;
        private static bool _active;
        internal static PlayerShipEngine RampBoostEngine;

        internal static bool Ready
        {
            get { return CurrentFinal != null && LoadStationary != null && Queue != null && DoFixed != null; }
        }

        // Engines.OnEnable re-enters the ActiveEngines setter during a swap; only the outermost call counts.
        private static int _depth;

        internal static void Capture(PlayerShip ship)
        {
            if (_depth++ > 0)
                return;
            Outgoing = null;
            OutgoingPower = 0f;
            try
            {
                var eng = ship != null ? ship.ActiveEngines as PlayerShipEngine : null;
                if (eng == null || !Ready)
                    return;
                Outgoing = eng;
                OutgoingPower = (float)CurrentFinal.GetValue(eng);
            }
            catch { }
        }

        internal static void Begin(PlayerShip ship)
        {
            if (_depth > 0)
                _depth--;
            if (_depth > 0)
                return;
            try
            {
                if (!GlideLoader.Enabled || !Ready || ship == null || Outgoing == null)
                    return;
                var incoming = ship.ActiveEngines as PlayerShipEngine;
                if (incoming == null || ReferenceEquals(incoming, Outgoing))
                    return;
                string why = Skip(ship, incoming);
                float speed = 0f, expected = 0f, fraction = 0f;
                if (ship.RigidBody != null)
                    speed = Vector3.Dot(ship.RigidBody.velocity, ((Component)ship).transform.forward);
                expected = incoming.ExpectedVelocityOnCurrentGear * (5f / 18f); // km/h -> m/s, as ValidateTargetVelocity
                if (expected > 0.01f)
                    fraction = Mathf.Clamp01(speed / expected);
                float cap = Mathf.Min(Mathf.Abs(incoming.CurrentSpeedPreset.basePower), incoming.MaxPower);
                float carried = Mathf.Sign(OutgoingPower) * Mathf.Min(Mathf.Abs(OutgoingPower), cap);
                if (why == null && Mathf.Abs(OutgoingPower) < 0.02f)
                    why = "outgoing idle";
                if (why == null && Mathf.Sign(OutgoingPower) != Mathf.Sign(incoming.CurrentSpeedPreset.basePower))
                    why = "direction change";
                bool speedMatch = GlideLoader.SpeedMatch;
                float seed = 0f;
                if (why == null && speedMatch)
                {
                    float target = incoming.CurrentSpeedPreset.basePower;
                    float mag = Mathf.Min(fraction * Mathf.Abs(target), Mathf.Min(incoming.MaxPower, Mathf.Abs(OutgoingPower)));
                    seed = Mathf.Sign(target) * mag;
                }
                if (GlideLoader.LogSwaps)
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "{0} swap {1} -> {2} gear={3} mode={4} speed={5:F2} m/s expected={6:F2} m/s fraction={7:F2} outgoingPower={8:F2} incomingPreset={9:F2} start={10:F2} {11}",
                        GlideLoader.Tag, Name(Outgoing), Name(incoming), incoming.GearIndex, speedMatch ? "speedMatch" : "fade",
                        speed, expected, fraction, OutgoingPower, incoming.CurrentSpeedPreset.basePower,
                        why != null ? 0f : (speedMatch ? seed : carried), why == null ? "GLIDE" : "stock (" + why + ")"));
                if (why != null)
                    return;
                RampBoostEngine = incoming;
                if (speedMatch)
                {
                    float current = (float)CurrentFinal.GetValue(incoming);
                    if (Mathf.Abs(current) < Mathf.Abs(seed))
                    {
                        CurrentFinal.SetValue(incoming, seed);
                        incoming.PowerModifier = seed;
                    }
                    Relisten(incoming);
                    return;
                }
                _target = incoming;
                _carried = carried;
                _start = Time.time;
                _active = true;
                Apply(ship);
            }
            catch (Exception e)
            {
                _active = false;
                Debug.LogWarning(GlideLoader.Tag + " swap hook failed: " + e.Message);
            }
            finally
            {
                Outgoing = null;
            }
        }

        private static string Skip(PlayerShip ship, PlayerShipEngine eng)
        {
            if (!((Behaviour)eng).enabled) return "incoming engine disabled (damage/battery/fuel/depth)";
            if (eng.GearIndex == 0) return "stop ordered";
            if (eng.GearsBlockade) return "gears blockade";
            if ((bool)LoadStationary.GetValue(eng)) return "charging";
            if (GlideLoader.SkipDuringAlarm && ship.Alarmed) return "alarm";
            if (Time.timeScale > 5f) return "time compression";
            return null;
        }

        // Called every physics step (PlayableSubmarine.FixedUpdate prefix).
        internal static void Apply(PlayerShip ship)
        {
            if (!_active)
                return;
            try
            {
                float t = (Time.time - _start) / GlideLoader.GlideSeconds;
                if (_target == null || !ReferenceEquals(ship.ActiveEngines, _target) || t >= 1f || Skip(ship, _target) != null)
                {
                    End();
                    return;
                }
                float floor = Mathf.Abs(_carried) * (1f - t);
                float current = (float)CurrentFinal.GetValue(_target);
                if (Mathf.Abs(current) < floor)
                {
                    float v = Mathf.Sign(_carried) * floor;
                    CurrentFinal.SetValue(_target, v);
                    _target.PowerModifier = v; // clamped by the game to the engine's MaxPower
                }
            }
            catch
            {
                _active = false;
            }
        }

        // Hand back to the game's own ramp: make sure its DoFixedUpdate listener is running.
        private static void End()
        {
            _active = false;
            Relisten(_target);
            _target = null;
        }

        private static void Relisten(PlayerShipEngine eng)
        {
            try
            {
                if (eng != null && ((Behaviour)eng).enabled)
                {
                    var q = Queue.GetValue(null) as ExecutionQueue;
                    var d = (Action)Delegate.CreateDelegate(typeof(Action), eng, DoFixed);
                    if (q != null)
                        q.AddFixedUpdateListener(d);
                }
            }
            catch { }
        }

        // Game target of DoFixedUpdate (same formula): enabled && realPerformance > 0.002 ? basePower*realPerformance : 0.
        internal static bool TryTarget(PlayerShipEngine eng, out float target)
        {
            target = 0f;
            if (RealPerf == null || LoadStationary == null || CurrentFinal == null)
                return false;
            if ((bool)LoadStationary.GetValue(eng))
                return false; // charging: PowerModifier is 0, leave stock
            float rp = (float)RealPerf.GetValue(eng);
            target = (((Behaviour)eng).enabled && rp > 0.002f) ? eng.CurrentSpeedPreset.basePower * rp : 0f;
            return true;
        }

        internal static float GetCurrent(PlayerShipEngine eng) { return (float)CurrentFinal.GetValue(eng); }
        internal static void SetCurrent(PlayerShipEngine eng, float v) { CurrentFinal.SetValue(eng, v); }

        private static string Name(PlayerShipEngine e)
        {
            if (e == null) return "none";
            string n = e.TypeName;
            return n != null && n.Contains("Diesel") ? "diesel" : "electric";
        }
    }

    // Optional faster ramp: the game raises power by Lerp(cur, target, fixedDeltaTime*0.18) per step
    // (decreases use 0.9). rampScale adds (rampScale-1) more of that same step on increases only,
    // never past the game's own target. Swapped-in engine only, unless applyRampScaleGlobally.
    [HarmonyPatch(typeof(PlayerShipEngine), "DoFixedUpdate")]
    internal static class RampHook
    {
        private static void Prefix(PlayerShipEngine __instance, out float __state)
        {
            __state = float.NaN;
            try
            {
                if (GlideLoader.Enabled && GlideLoader.RampScale > 1.001f
                    && (GlideLoader.ApplyRampScaleGlobally || ReferenceEquals(__instance, Glide.RampBoostEngine)))
                    __state = Glide.GetCurrent(__instance);
            }
            catch { }
        }

        private static void Postfix(PlayerShipEngine __instance, float __state)
        {
            if (float.IsNaN(__state))
                return;
            try
            {
                float target;
                if (!Glide.TryTarget(__instance, out target))
                    return;
                float after = Glide.GetCurrent(__instance);
                bool rising = Mathf.Abs(after) > Mathf.Abs(__state) && Mathf.Abs(target) >= Mathf.Abs(after) && Mathf.Sign(target) == Mathf.Sign(after);
                if (!rising)
                {
                    if (ReferenceEquals(__instance, Glide.RampBoostEngine) && Mathf.Abs(after - target) < 0.01f)
                        Glide.RampBoostEngine = null; // reached its setting: back to stock
                    return;
                }
                float boosted = after + (after - __state) * (GlideLoader.RampScale - 1f);
                if (Mathf.Abs(boosted) > Mathf.Abs(target))
                    boosted = target;
                Glide.SetCurrent(__instance, boosted);
                __instance.PowerModifier = boosted; // game clamps to MaxPower
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "SetActiveEngines")]
    internal static class SwapHook
    {
        private static void Prefix(PlayerShip __instance) { Glide.Capture(__instance); }
        private static void Postfix(PlayerShip __instance) { Glide.Begin(__instance); }
    }

    [HarmonyPatch(typeof(PlayerShip), "ActiveEngines", MethodType.Setter)]
    internal static class ActiveEnginesSetterHook
    {
        private static void Prefix(PlayerShip __instance) { Glide.Capture(__instance); }
        private static void Postfix(PlayerShip __instance) { Glide.Begin(__instance); }
    }

    [HarmonyPatch(typeof(PlayableSubmarine), "FixedUpdate")]
    internal static class GlideTick
    {
        private static void Prefix(PlayableSubmarine __instance)
        {
            var ship = __instance as PlayerShip;
            if (ship != null)
                Glide.Apply(ship);
        }
    }
}
