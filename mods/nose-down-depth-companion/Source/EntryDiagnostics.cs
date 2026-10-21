// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // Observation only. Boundaries surround this companion's existing FixedUpdate chain,
    // not the physics solver. Controller output is NOT total hydrodynamic torque.
    internal static class EntryDiagnostics
    {
        private static PlayerShip _ship;
        private static bool _sample, _warned;
        private static float _next, _lastTime = -1;
        private static Vector3 _com;
        private static float _y, _unityDrag;
        private static string _hydroDrag, _forced, _wasSubmerged;
        private static int _lawCalls;
        private static double _target, _pitch, _rate, _inertia, _stock, _final, _limit, _extra, _scale;
        private static readonly FieldInfo DragField = AccessTools.Field(typeof(PlayableSubmarine), "drag");
        private static readonly FieldInfo ForcedField = AccessTools.Field(typeof(PlayableSubmarine), "forcedDepthKeeping");
        private static readonly FieldInfo SubmergedField = AccessTools.Field(typeof(PlayableSubmarine), "wasFullySubmerged");
        private static readonly FieldInfo BalanceField = AccessTools.Field(typeof(PlayableSubmarine), "currentBalance");
        private static readonly FieldInfo CustomField = AccessTools.Field(typeof(PlayableSubmarine), "customBallance");
        private static readonly FieldInfo PlaneField = AccessTools.Field(typeof(PlayableSubmarine), "verticalSteersTorque");
        private static readonly FieldInfo SteerField = AccessTools.Field(typeof(PlayableSubmarine), "currentDepthSteersRotation");

        internal static void Begin(PlayerShip ship)
        {
            _sample = false;
            _lawCalls = 0;
            if (!ReferenceEquals(_ship, ship) || Time.time < _lastTime) _next = 0;
            _ship = ship;
            _lastTime = Time.time;
            try
            {
                if (!CompanionConfig.TraceLogging || ship == null || ship.RigidBody == null
                    || Time.time < _next || !InWindow(ship.DeckDepth, ship.TargetDepth, Time.timeScale)) return;
                _next = Time.time + 0.1f;
                var rb = ship.RigidBody;
                _com = rb.centerOfMass;
                _y = rb.position.y;
                _unityDrag = rb.angularDrag;
                _hydroDrag = HydroDrag(ship);
                _forced = Read(ForcedField, ship);
                _wasSubmerged = Read(SubmergedField, ship);
                _sample = true;
            }
            catch (Exception e) { Warn(e); }
        }

        internal static bool InWindow(float deck, float order, float timeScale)
        {
            return !float.IsNaN(deck) && !float.IsInfinity(deck)
                && !float.IsNaN(order) && !float.IsInfinity(order)
                && deck >= -3 && deck <= 12 && order >= 0 && order <= 50
                && timeScale > 0 && timeScale <= 10;
        }

        internal static void Law(double target, double pitch, double rate, double inertia,
            double stock, double final, double limit, double extra, double scale)
        {
            if (!_sample) return;
            _lawCalls++;
            _target = target; _pitch = pitch; _rate = rate; _inertia = inertia;
            _stock = stock; _final = final; _limit = limit; _extra = extra; _scale = scale;
        }

        internal static void End(PlayerShip ship)
        {
            if (!_sample || !ReferenceEquals(ship, _ship)) return;
            _sample = false;
            try
            {
                var rb = ship.RigidBody;
                if (rb == null) return;
                var localRate = ship.transform.InverseTransformDirection(rb.angularVelocity);
                var engine = ship.ActiveEngine;
                string engineName = engine == null ? "none" : ReferenceEquals(engine, ship.DieselEngine) ? "diesel"
                    : ReferenceEquals(engine, ship.ElectricEngine) ? "electric" : "other";
                var s = new StringBuilder("[NoseDownCompanion.EntryPhysics] v=1.0.23");
                Add(s, "t", Time.time); Add(s, "dt", Time.fixedDeltaTime); Add(s, "timeScale", Time.timeScale);
                Add(s, "enabled", CompanionConfig.Enabled);
                Add(s, "diagnosticOnly", CompanionConfig.DiagnosticOnly);
                Add(s, "correctionsActive", CompanionConfig.CorrectionsEnabled);
                Add(s, "casingBypassRequested", CompanionConfig.DiagnosticDisableNoseDownCasing);
                Add(s, "casingHookReady", DiagnosticIsolation.Ready);
                Add(s, "casingObservedCalls", DiagnosticIsolation.ObservedCalls);
                Add(s, "casingBypassCalls", DiagnosticIsolation.BypassCalls);
                Add(s, "casingBypassApplied", DiagnosticIsolation.AppliedTo(ship));
                Add(s, "hull", ship.SandboxEntity != null && ship.SandboxEntity.Type != null ? ship.SandboxEntity.Type.Name : "unknown");
                Add(s, "order", ship.TargetDepth); Add(s, "depth", ship.Depth); Add(s, "deck", ship.DeckDepth);
                Add(s, "pitch", Mathf.DeltaAngle(0, rb.rotation.eulerAngles.x));
                Add(s, "rateDeg", localRate.x * 180 / Math.PI); Add(s, "speed", ship.AverageVelocity);
                Add(s, "velocityWorld", Vec(rb.velocity));
                Add(s, "forwardSpeed", Vector3.Dot(rb.velocity, ship.transform.forward));
                Add(s, "yPre", _y); Add(s, "yPost", rb.position.y); Add(s, "vy", rb.velocity.y);
                Add(s, "comPre", Vec(_com)); Add(s, "comPost", Vec(rb.centerOfMass));
                Add(s, "hydroDragPre", _hydroDrag); Add(s, "hydroDragPost", HydroDrag(ship));
                Add(s, "rbDragPre", _unityDrag); Add(s, "rbDragPost", rb.angularDrag);
                Add(s, "uniformBuoyancyCentreLocal", BuoyancyCentre(ship));
                var casing = ship.HullInternalTank;
                Add(s, "casingWater", casing == null ? "n/a" : F(casing.Water));
                Add(s, "casingAir", casing == null ? "n/a" : F(casing.Air));
                Add(s, "casingFuel", casing == null ? "n/a" : F(casing.Fuel));
                Add(s, "casingCapacity", casing == null ? "n/a" : F(casing.Capacity));
                Add(s, "buoyancyRatio", ship.BuoyancyRatio);
                Add(s, "mass", rb.mass); Add(s, "inertiaTensor", Vec(rb.inertiaTensor));
                Add(s, "balance", Read(BalanceField, ship)); Add(s, "customBalance", Read(CustomField, ship));
                Add(s, "planeAuthorityBoundary", Read(PlaneField, ship)); Add(s, "steer", Read(SteerField, ship));
                Add(s, "forcedPre", _forced); Add(s, "forcedPost", Read(ForcedField, ship));
                Add(s, "wasSubmergedPre", _wasSubmerged); Add(s, "wasSubmergedPost", Read(SubmergedField, ship));
                Add(s, "engine", engineName); Add(s, "power", engine == null ? "n/a" : F((float)engine.Power));
                Add(s, "powerModifier", engine == null ? "n/a" : F(engine.PowerModifier));
                float? bowLift = NoseDownBridge.Present ? NoseDownBridge.BowLiftMoment() : null;
                Add(s, "bowLiftMoment", bowLift.HasValue ? F(bowLift.Value) : "n/a");
                Add(s, "phase", NoseDownBridge.Present ? NoseDownBridge.PhaseName() : "absent");
                Add(s, "officer", NoseDownBridge.Present && CompanionConfig.CorrectionsEnabled ? PlanesManning.OfficerSeated.ToString() : "n/a"); Add(s, "entryAssist", CompanionConfig.DiveEntryAssist);
                Add(s, "torqueBoost", CompanionConfig.ViiTorqueBoost); Add(s, "torqueFactor", CompanionConfig.ViiTorqueFactor);
                Add(s, "preciseArrival", CompanionConfig.PreciseDepthArrival); Add(s, "lawCalls", _lawCalls);
                if (_lawCalls > 0)
                {
                    Add(s, "lawTargetDeg", _target * 180 / Math.PI); Add(s, "lawPitchDeg", _pitch * 180 / Math.PI);
                    Add(s, "lawRateDeg", _rate * 180 / Math.PI); Add(s, "lawInertia", _inertia);
                    Add(s, "lawBefore", _stock); Add(s, "lawAfter", _final); Add(s, "lawLimit", _limit);
                    Add(s, "entryExtraBeforeScale", _extra); Add(s, "trimScale", _scale);
                }
                Debug.Log(s.ToString());
            }
            catch (Exception e) { Warn(e); }
        }

        private static string Read(FieldInfo field, object owner)
        {
            try { return field == null ? "n/a" : Convert.ToString(field.GetValue(owner), CultureInfo.InvariantCulture) ?? "n/a"; }
            catch { return "n/a"; }
        }
        private static PlayerShip _physicsOwner;
        private static MonoBehaviour _physics;
        private static FieldInfo _buoyancyCentre;
        private static float _physicsRetry;
        private static string BuoyancyCentre(PlayerShip ship)
        {
            try
            {
                if (!ReferenceEquals(_physicsOwner, ship))
                {
                    _physicsOwner = ship; _physics = null; _buoyancyCentre = null; _physicsRetry = 0;
                }
                if (_physics == null && Time.unscaledTime >= _physicsRetry)
                {
                    _physicsRetry = Time.unscaledTime + 2f;
                    foreach (var behaviour in ship.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (behaviour == null || behaviour.GetType().Name != "NeuralNetworkPhysics") continue;
                        _physics = behaviour;
                        _buoyancyCentre = AccessTools.Field(behaviour.GetType(), "uniformDensityCenterOfMass");
                        break;
                    }
                }
                return _physics == null || _buoyancyCentre == null ? "n/a" : Vec((Vector3)_buoyancyCentre.GetValue(_physics));
            }
            catch { return "n/a"; }
        }

        private static string HydroDrag(PlayerShip ship)
        {
            try
            {
                object drag = DragField == null ? null : DragField.GetValue(ship);
                var property = drag == null ? null : drag.GetType().GetProperty("AngularDrag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return property == null ? "n/a" : Convert.ToString(property.GetValue(drag, null), CultureInfo.InvariantCulture);
            }
            catch { return "n/a"; }
        }
        private static string F(double value) { return value.ToString("F5", CultureInfo.InvariantCulture); }
        private static string Vec(Vector3 value) { return F(value.x) + "/" + F(value.y) + "/" + F(value.z); }
        private static void Add(StringBuilder s, string key, object value)
        {
            s.Append(' ').Append(key).Append('=');
            if (value is float || value is double) s.Append(F(Convert.ToDouble(value, CultureInfo.InvariantCulture)));
            else s.Append(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "n/a");
        }
        private static void Warn(Exception e)
        {
            _sample = false;
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[NoseDownCompanion.EntryPhysics] diagnostic read failed: " + e.GetType().Name);
        }
    }
}

