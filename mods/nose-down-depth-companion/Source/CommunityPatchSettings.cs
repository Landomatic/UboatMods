// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;

namespace NoseDownCompanion
{
    // Dusty's reflection contract: no reference to his assembly is required.
    // The master is a normal setting row so its setter also saves our own config,
    // including when the panel is used before loading a career.
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Nose Down Depth Companion"; } }
        public static string Category { get { return "Submarine / Diving"; } }
        public static string Description
        {
            get { return "Depth control by Landomatic. Requires Kugler's Nose Down. Entry assist, precise PD arrival, pre-dive planes, torque, deeper-PD and PD wave-force options are experimental and start disabled. Reset restores this companion's coded settings and keeps its master switch as it is."; }
        }

        public static object[] GetSettings()
        {
            return new object[]
            {
                Bool("diagnosticLockStatus", "Diagnostic lock active (read-only)",
                    "Status only. Set diagnosticOnly in the config while the game is closed. When ON, all Companion corrections remain off regardless of the master or other menu rows. Explicit diagnostic casing bypass is separate.",
                    () => CompanionConfig.DiagnosticOnly, v => { }),
                Bool("correctionsActiveStatus", "Companion corrections active (read-only)",
                    "Effective master state after the diagnostic lock. OFF means Companion correction gates are closed; an explicitly requested diagnostic casing bypass has its own log status.",
                    () => CompanionConfig.CorrectionsEnabled, v => { }),
                Bool("enabled", "Enable depth companion",
                    "Master switch for Companion corrections. The config-only diagnosticOnly lock overrides this switch; casing bypass is a separate explicit diagnostic test. Nose Down and the other mods keep their own settings.",
                    () => CompanionConfig.Enabled, v =>
                    {
                        if (CompanionConfig.Enabled != v)
                        {
                            CompanionConfig.Enabled = v;
                            DepthTargetClamp.ResetControlState("masterSwitch");
                        }
                    }),
                Bool("diveEntryAssist", "Preserve VII dive-entry pitch (experimental)",
                    "Adds bounded nose-down torque while a VII begins an ordinary dive, fading out by 1 m deck depth. Aims to reduce the early level-off; requires in-game comparison. Alarm/crash dives and blows are excluded. Default off.",
                    () => CompanionConfig.DiveEntryAssist, v => CompanionConfig.DiveEntryAssist = v),
                Bool("preciseDepthArrival", "Precise PD arrival (nonhistorical)",
                    "Optional gameplay aid, default off. Tries to reduce arrival overshoot using a velocity-preview target near ordinary II/VII shallow orders (2-10 m). Type II requires a seated officer. Takes precedence over legacy arrival/hold offsets and deeper-PD bias on those hulls while on. Does not reproduce section 106's deliberate test-dive overshoot and clearing cycles; accuracy is not guaranteed.",
                    () => CompanionConfig.PreciseDepthArrival, v => { CompanionConfig.PreciseDepthArrival = v; PreciseDepthArrival.Reset(); }),
                Bool("preDivePlanes", "Allow planes assignment before diving",
                    "Experimental, default off. Makes the player's planes station available on the surface so you can order an officer there before diving. This also makes its crew slots available. No one is assigned automatically; damage, docking and other restrictions remain. Turning off restores the normal surface restriction and may end the assignment.",
                    () => CompanionConfig.PreDivePlanes, v => CompanionConfig.PreDivePlanes = v),
                Bool("ixTargetPin", "IX depth target pin",
                    "Stops the moving target at ordinary deep-dive orders on IXA, IXC and IXC/40. PD orders and protected dive/blow modes are excluded.",
                    () => CompanionConfig.IxTargetPin, v => CompanionConfig.IxTargetPin = v),
                Bool("viiDeckHandover", "VII dive handover",
                    "Keeps the existing trim-to-planes handover on VIIB, VIIC and VIIC/41.",
                    () => CompanionConfig.ViiDeckHandover, v => CompanionConfig.ViiDeckHandover = v),
                Bool("officerPdBrake", "Officer PD approach brake",
                    "Brakes a sinking approach to ordinary shallow-depth orders with an officer at the planes on II/VII boats. Turn off for the PD oscillation comparison. Turning it off releases its current brake offset.",
                    () => CompanionConfig.OfficerPdBrake, v =>
                    {
                        if (CompanionConfig.OfficerPdBrake != v)
                        {
                            CompanionConfig.OfficerPdBrake = v;
                            PdApproachBrake.Release("menuSwitch");
                        }
                    }),
                Bool("pdHoldDamp", "PD hold damping",
                    "Existing hold damping. Hulls and the officer requirement remain as specified in your companion config.",
                    () => CompanionConfig.PdHoldDamp, v => CompanionConfig.PdHoldDamp = v),
                Bool("viiTorqueBoost", "VII torque assistance",
                    "Experimental. Raises only the final torque cap in the first metre of an ordinary VII dive. Keeps the stock integral limit. Alarm, crash dive and blows are excluded.",
                    () => CompanionConfig.ViiTorqueBoost, v => CompanionConfig.ViiTorqueBoost = v),
                Number("viiTorqueFactor", "VII torque multiplier",
                    "Used only with torque assistance enabled. 1.00 keeps the stock cap; maximum 1.50. A smaller dip is a test goal, not guaranteed.",
                    () => CompanionConfig.ViiTorqueFactor, v => CompanionConfig.ViiTorqueFactor = v, 1f, 1.5f, 0.05f),
                Bool("pdDepthBias", "Deeper PD hold",
                    "Experimental extra clearance on IIA, IID and IXA, after arrival in Hold. The default requires an officer at the planes. Changes the internal target, not the visible order or sea.",
                    () => CompanionConfig.PdDepthBias, v => CompanionConfig.PdDepthBias = v),
                Number("pdBiasMeters", "Extra PD depth (m)",
                    "Used only with deeper PD hold enabled. Adds to any existing settled-depth error and reduces periscope clearance. Start with 0.30 m and observe.",
                    () => CompanionConfig.PdBiasMeters, v => CompanionConfig.PdBiasMeters = v, 0f, 0.5f, 0.05f),
                Bool("pdWaveForce", "Reduce wave force at PD",
                    "Experimental local reduction on recognised II/VII/IX player boats in ordinary PD Hold. Alarm/crash dives, blows and deeper orders are excluded. Seakeeping's wind conversion and depth fade stay in control.",
                    () => CompanionConfig.PdWaveForce, v => { CompanionConfig.PdWaveForce = v; if (!v) PdWaveForce.Reset("optionOff"); }),
                Number("pdWaveForceFactor", "PD wave-force multiplier",
                    "1.00 = unchanged; 0.50 = half the explicit wave-force term after the entry ramp. Visible troughs and buoyancy still act, so detector immersion is not guaranteed. Only used when enabled.",
                    () => CompanionConfig.PdWaveForceFactor, v => CompanionConfig.PdWaveForceFactor = v, 0f, 1f, 0.05f),
                Bool("traceLogging", "Diagnostic trace logging",
                    "Writes sampled troubleshooting lines to output_log, including effective corrections and casing-bypass status. Logging remains available with master OFF or diagnosticOnly=true. Leave off for normal play.",
                    () => CompanionConfig.TraceLogging, v => CompanionConfig.TraceLogging = v)
            };
        }

        public static void ResetToDefaults()
        {
            bool enabled = CompanionConfig.Enabled;
            bool diagnosticOnly = CompanionConfig.DiagnosticOnly;
            bool casingBypass = CompanionConfig.DiagnosticDisableNoseDownCasing;
            CompanionConfig.ResetDefaults();
            CompanionConfig.Enabled = enabled; // same master-switch convention as Dusty's own reset
            CompanionConfig.DiagnosticOnly = diagnosticOnly;
            CompanionConfig.DiagnosticDisableNoseDownCasing = casingBypass;
            DepthTargetClamp.ResetControlState("defaults");
            CompanionConfig.SaveCurrent();
        }

        private static object[] Bool(string key, string label, string help, Func<bool> get, Action<bool> set)
        {
            return new object[] { "bool", key, label, help,
                new Func<object>(() => get()),
                new Action<object>(value => { set(Convert.ToBoolean(value, CultureInfo.InvariantCulture)); CompanionConfig.SaveCurrent(); }) };
        }

        private static object[] Number(string key, string label, string help, Func<float> get, Action<float> set,
            float min, float max, float step)
        {
            return new object[] { "float", key, label, help,
                new Func<object>(() => get()),
                new Action<object>(value =>
                {
                    float f = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    if (float.IsNaN(f) || float.IsInfinity(f)) return;
                    set(Math.Max(min, Math.Min(max, f)));
                    CompanionConfig.SaveCurrent();
                }), min, max, step };
        }
    }
}

