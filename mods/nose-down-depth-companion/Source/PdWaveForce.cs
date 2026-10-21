// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // The supplied game's Execute reads wavesPart only for its explicit wave-force
    // term. Scale that coefficient for the player only, during this one call.
    // Seakeeping's depth-fade cache, wave spectrum and buoyancy are never written.
    internal static class PdWaveForce
    {
        private static FieldInfo _nnField, _wavesPart;
        private static PlayerShip _ship;
        private static float _lastTime = -1f, _blend, _nextTrace;
        private static bool _engaged, _errorLogged;
        internal static bool Ready { get; private set; }

        internal sealed class CallState
        {
            internal object Network;
            internal float Original;
            internal bool Changed;
        }

        internal static void Install(Harmony harmony)
        {
            try
            {
                _nnField = AccessTools.Field(typeof(PlayableSubmarine), "neuralNetworkPhysics");
                Type type = _nnField != null ? _nnField.FieldType : null;
                _wavesPart = type != null ? AccessTools.Field(type, "wavesPart") : null;
                MethodInfo execute = type != null ? AccessTools.Method(type, "Execute") : null;
                if (_wavesPart == null || _wavesPart.FieldType != typeof(float) || execute == null
                    || !execute.IsStatic || execute.GetParameters().Length != 3
                    || !typeof(IList).IsAssignableFrom(execute.GetParameters()[0].ParameterType))
                    throw new MissingMemberException("player wave-force coefficient / Execute signature");
                harmony.Patch(execute,
                    new HarmonyMethod(AccessTools.Method(typeof(PdWaveForce), "Prefix")),
                    new HarmonyMethod(AccessTools.Method(typeof(PdWaveForce), "Postfix")), null,
                    new HarmonyMethod(AccessTools.Method(typeof(PdWaveForce), "Finalizer")));
                Ready = true;
            }
            catch (Exception e)
            {
                Ready = false;
                Debug.LogWarning(CompanionLoader.Tag + " PD wave-force option unavailable: " + e.Message);
            }
        }

        internal static void ObserveShip(PlayerShip ship) { _ship = ship; }

        private static float Factor()
        {
            PlayerShip ship = _ship;
            float strength = CompanionConfig.PdWaveForceFactor;
            if (!Ready || !CompanionConfig.CorrectionsEnabled || !CompanionConfig.PdWaveForce
                || !NoseDownBridge.Present || ship == null
                || !(HullKey.IsVIIClampHull || HullKey.IsTypeII || HullKey.Current == "TypeIX")
                || ship.TargetDepth < 2f || ship.TargetDepth > 10f || ship.DeckDepth < 2f
                || ship.DeckDepth > 12f || NoseDownBridge.IsProtected(ship)
                || NoseDownBridge.PhaseName() != "Hold"
                || float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f || strength >= 1f)
            {
                Reset("gate");
                return 1f;
            }
            float gap = Math.Abs(ship.DeckDepth - ship.TargetDepth);
            float window = Mathf.Clamp01(2f - gap); // full within 1 m; fade to unchanged at 2 m
            if (window <= 0f) { Reset("outsidePd"); return 1f; }
            float now = Time.time;
            if (now != _lastTime)
            {
                float dt = _lastTime < 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.25f);
                _lastTime = now;
                _blend = Math.Min(1f, _blend + dt); // one game-second entry ramp
            }
            return 1f - (1f - strength) * _blend * window;
        }

        private static void Prefix(IList __0, out CallState __state)
        {
            __state = null;
            try
            {
                float factor = Factor();
                if (factor >= 1f || __0 == null) return;
                object network = _nnField.GetValue(_ship);
                if (network == null || !__0.Contains(network)) return;
                float original = (float)_wavesPart.GetValue(network);
                if (float.IsNaN(original) || float.IsInfinity(original) || original <= 0f) return;
                __state = new CallState { Network = network, Original = original, Changed = true };
                _wavesPart.SetValue(network, original * factor);
                if (!_engaged)
                {
                    _engaged = true;
                    if (CompanionConfig.TraceLogging) Debug.Log("[NoseDownCompanion.PdWaves] ENGAGE player-only PD wave force");
                }
                if (CompanionConfig.TraceLogging && Time.time >= _nextTrace)
                {
                    _nextTrace = Time.time + 1f;
                    Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "[NoseDownCompanion.PdWaves] deck={0:F2} order={1:F2} factor={2:F3}",
                        _ship.DeckDepth, _ship.TargetDepth, factor));
                }
            }
            catch (Exception e)
            {
                Restore(__state);
                Ready = false;
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    Debug.LogWarning(CompanionLoader.Tag + " PD wave-force option stopped: " + e.Message);
                }
            }
        }

        private static void Postfix(CallState __state) { Restore(__state); }
        private static Exception Finalizer(Exception __exception, CallState __state)
        {
            Restore(__state);
            return __exception; // never hide an exception from the game or another mod
        }
        private static void Restore(CallState state)
        {
            if (state == null || !state.Changed) return;
            _wavesPart.SetValue(state.Network, state.Original);
            state.Changed = false;
        }
        internal static void Reset(string reason)
        {
            _lastTime = -1f;
            _blend = 0f;
            if (_engaged && CompanionConfig.TraceLogging)
                Debug.Log("[NoseDownCompanion.PdWaves] RELEASE reason=" + reason);
            _engaged = false;
        }
    }
}

