// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace NoseDownCompanion
{
    internal static class CompanionConfig
    {
        internal static bool Enabled = true;
        // Config-only test controls: Dusty's saved rows must not restore these.
        internal static bool DiagnosticOnly = false;
        internal static bool DiagnosticDisableNoseDownCasing = false;
        internal static bool CorrectionsEnabled { get { return Enabled && !DiagnosticOnly; } }
        internal static bool DiveEntryAssist = false;
        internal static bool PreciseDepthArrival = false;
        internal static bool PreDivePlanes = false;
        internal static bool ViiTorqueBoost = false;
        internal static float ViiTorqueFactor = 1.35f;
        internal static bool PdDepthBias = false;
        internal static float PdBiasMeters = 0.45f;
        internal static bool PdBiasOfficerOnly = true;
        internal static float PdBiasSlew = 0.1f;
        internal static bool PdWaveForce = false;
        internal static float PdWaveForceFactor = 1f;
        internal static float ClampMaxDepth = 320f;
        internal static bool IidOfficerClamp = true;
        internal static bool MannedAscentBrake = true;
        internal static bool ShallowStandDown = true;
        internal static bool ShallowTrimStandDown = true;
        internal static bool StandDownClampedOnly = false;
        internal static bool IiAfloatTrim = true;
        internal static string[] StandDownHulls; // null = every allowed hull (VIIB, VIIC, VIIC41, IIA, IID)
        internal static string StandDownHullsText = "VIIB,VIIC,VIIC41,IIA,IID";
        internal static bool TraceLogging = false;
        internal static bool OfficerPdBrake = true;
        internal static float IiTrimFadeSeconds = 6f;
        internal static float IiTrimFadeRate = 1f;
        internal static float IiTrimFadePitch = 3f;
        internal static bool C41PlaneDamping = true;
        internal static float C41DampBand = 1.5f;
        internal static float C41DampMin = 0.5f;
        internal static bool IxDepthFix = true;
        internal static bool SurfaceBrakeOn = true;
        internal static string[] SurfaceBrakeHulls = { "TypeVIIC41", "TypeVIIB" };
        internal static string SurfaceBrakeHullsText = "VIIC41,VIIB";
        internal static bool SurfaceTrimBrake = true;
        internal static bool SurfaceHoldOn = true;
        internal static float SurfaceHoldDeck = 5.5f;
        internal static float SurfaceHoldTop = 9f;
        internal static float SurfaceHoldShare = 1f;
        internal static float SurfaceHoldRampUp = 0.5f;
        internal static float SurfaceHoldRampDown = 2f;
        internal static float SurfaceHoldMaxSeconds = 40f;
        internal static float SurfaceBrakeTop = 8f;
        internal static float SurfaceBrakeStop = 4.5f;
        internal static float SurfaceBrakeDecel = 0.05f;
        internal static float SurfaceBrakeMinRate = 0.05f;
        internal static float SurfaceBrakeGain = 3f;
        internal static float SurfaceBrakeMaxCut = 0.8f;
        internal static float IxBrakeWindow = 15f;
        internal static float IxBrakeDecel = 0.067f;
        internal static float IxBrakeMinRate = 0.12f;
        internal static float IxBrakeEngageFactor = 1.05f;
        internal static float IxBrakeGain = 2.0f;
        internal static float PdBrakeWindow = 2.5f;
        internal static float PdBrakeDecel = 0.067f;
        internal static float PdBrakeMinRate = 0.12f;
        internal static float PdBrakeEngageFactor = 1.05f;
        internal static float PdBrakeGain = 4.0f;
        internal static float PdBrakeMaxOffset = 2.0f;
        internal static float PdBrakeHoldSink = 0.15f;
        // v1.0.15 (A): VIIB/VIIC/VIIC41 stand-down handover by deck depth, Nose Down trim eased out.
        internal static bool ViiDeckHandover = true;
        internal static float ViiHandoverDeck = 0.75f;
        internal static float ViiTrimFadeSeconds = 5f;
        // v1.0.15 (C/D): PD-hold damping after first reaching the order.
        internal static bool PdHoldDamp = true;
        internal static string[] PdDampHulls = { "TypeIIA", "TypeIID", "TypeIXA" };
        internal static string PdDampHullsText = "IIA,IID,IXA";
        internal static bool PdDampOfficerOnly = true;
        internal static float PdDampKd = 2.5f;
        internal static float PdDampCap = 0.5f;
        internal static float PdDampSlew = 1.0f;
        // v1.0.16: Type IX ordinary-dive target pin.
        internal static bool IxTargetPin = true;
        internal static string[] IxPinHulls = { "TypeIXA", "TypeIXC", "TypeIXC40" };
        internal static string IxPinHullsText = "IXA,IXC,IXC40";
        internal static readonly string[] IxPinAllowed = { "TypeIXA", "TypeIXC", "TypeIXC40" };
        internal static float IxPinMinOrder = 10f;
        internal static readonly string[] PdDampAllowed = { "TypeIIA", "TypeIID", "TypeIXA", "TypeVIIB", "TypeVIIC", "TypeVIIC41" };

        // Capture field initializers before any saved config is loaded. Arrays are cloned.
        private static readonly Dictionary<FieldInfo, object> FactoryDefaults = CaptureDefaults();

        private static Dictionary<FieldInfo, object> CaptureDefaults()
        {
            var values = new Dictionary<FieldInfo, object>();
            foreach (FieldInfo field in typeof(CompanionConfig).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsInitOnly || field.IsLiteral) continue;
                Type type = field.FieldType;
                if (type != typeof(bool) && type != typeof(float) && type != typeof(string) && type != typeof(string[])) continue;
                values.Add(field, CopySetting(field.GetValue(null)));
            }
            return values;
        }

        private static object CopySetting(object value)
        {
            Array array = value as Array;
            return array != null ? array.Clone() : value;
        }

        internal static void ResetDefaults()
        {
            foreach (var item in FactoryDefaults)
                item.Key.SetValue(null, CopySetting(item.Value));
        }

        internal static void SaveCurrent()
        {
            try { Save(PathName); }
            catch (Exception e) { Debug.LogWarning(CompanionLoader.Tag + " config save failed: " + e.Message); }
        }

        private static string PathName
        {
            get { return Path.Combine(Application.persistentDataPath, "NoseDownCompanionConfig.txt"); }
        }

        internal static void Load()
        {
            string p = PathName;
            try
            {
                if (File.Exists(p))
                {
                    foreach (string raw in File.ReadAllLines(p))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#"))
                            continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0)
                            continue;
                        string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                        string v = line.Substring(eq + 1).Trim();
                        switch (k)
                        {
                            case "enabled": Enabled = Bool(v, Enabled); break;
                            case "diagnosticonly": DiagnosticOnly = Bool(v, DiagnosticOnly); break;
                            case "diagnosticdisablenosedowncasing": DiagnosticDisableNoseDownCasing = Bool(v, DiagnosticDisableNoseDownCasing); break;
                            case "diveentryassist": DiveEntryAssist = Bool(v, DiveEntryAssist); break;
                            case "precisedeptharrival": PreciseDepthArrival = Bool(v, PreciseDepthArrival); break;
                            case "prediveplanes": PreDivePlanes = Bool(v, PreDivePlanes); break;
                            case "viitorqueboost": ViiTorqueBoost = Bool(v, ViiTorqueBoost); break;
                            case "viitorquefactor": ViiTorqueFactor = Num(v, ViiTorqueFactor, 1f, 1.5f); break;
                            case "pddepthbias": PdDepthBias = Bool(v, PdDepthBias); break;
                            case "pdbiasmeters": PdBiasMeters = Num(v, PdBiasMeters, 0f, 0.5f); break;
                            case "pdbiasofficeronly": PdBiasOfficerOnly = Bool(v, PdBiasOfficerOnly); break;
                            case "pdbiasslew": PdBiasSlew = Num(v, PdBiasSlew, 0.02f, 1f); break;
                            case "pdwaveforce": PdWaveForce = Bool(v, PdWaveForce); break;
                            case "pdwaveforcefactor": PdWaveForceFactor = Num(v, PdWaveForceFactor, 0f, 1f); break;
                            case "clampmaxdepth":
                                float f;
                                if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f) && !float.IsNaN(f) && !float.IsInfinity(f))
                                    ClampMaxDepth = Mathf.Clamp(f, 10f, 400f);
                                break;
                            case "iidofficerclamp": IidOfficerClamp = Bool(v, IidOfficerClamp); break;
                            case "mannedascentbrake": MannedAscentBrake = Bool(v, MannedAscentBrake); break;
                            case "shallowstanddown": ShallowStandDown = Bool(v, ShallowStandDown); break;
                            case "shallowtrimstanddown": ShallowTrimStandDown = Bool(v, ShallowTrimStandDown); break;
                            case "standdownhulls": ParseHulls(v); break;
                            case "iiafloattrim": IiAfloatTrim = Bool(v, IiAfloatTrim); break;
                            case "officerpdbrake": OfficerPdBrake = Bool(v, OfficerPdBrake); break;
                            case "pdbrakewindow": PdBrakeWindow = Num(v, PdBrakeWindow, 0.5f, 5f); break;
                            case "pdbrakedecel": PdBrakeDecel = Num(v, PdBrakeDecel, 0.01f, 0.5f); break;
                            case "pdbrakeminrate": PdBrakeMinRate = Num(v, PdBrakeMinRate, 0.02f, 1f); break;
                            case "pdbrakeengagefactor": PdBrakeEngageFactor = Num(v, PdBrakeEngageFactor, 1f, 2f); break;
                            case "pdbrakegain": PdBrakeGain = Num(v, PdBrakeGain, 0.5f, 10f); break;
                            case "pdbrakeholdsink": PdBrakeHoldSink = Num(v, PdBrakeHoldSink, 0.02f, 1f); break;
                            case "pdbrakemaxoffset": PdBrakeMaxOffset = Num(v, PdBrakeMaxOffset, 0.1f, 3f); break;
                            case "iitrimfadeseconds": IiTrimFadeSeconds = Num(v, IiTrimFadeSeconds, 0.5f, 10f); break;
                            case "iitrimfaderate": IiTrimFadeRate = Num(v, IiTrimFadeRate, 0.2f, 10f); break;
                            case "iitrimfadepitch": IiTrimFadePitch = Num(v, IiTrimFadePitch, 0.5f, 8f); break;
                            case "c41planedamping": C41PlaneDamping = Bool(v, C41PlaneDamping); break;
                            case "c41dampband": C41DampBand = Num(v, C41DampBand, 0.3f, 3f); break;
                            case "c41dampmin": C41DampMin = Num(v, C41DampMin, 0.2f, 1f); break;
                            case "surfacebrake": SurfaceBrakeOn = Bool(v, SurfaceBrakeOn); break;
                            case "surfacetrimbrake": SurfaceTrimBrake = Bool(v, SurfaceTrimBrake); break;
                            case "surfacehold": SurfaceHoldOn = Bool(v, SurfaceHoldOn); break;
                            case "surfaceholddeck": SurfaceHoldDeck = Num(v, SurfaceHoldDeck, 3f, 9f); break;
                            case "surfaceholdtop": SurfaceHoldTop = Num(v, SurfaceHoldTop, 5f, 20f); break;
                            case "surfaceholdshare": SurfaceHoldShare = Num(v, SurfaceHoldShare, 0.1f, 1f); break;
                            case "surfaceholdrampup": SurfaceHoldRampUp = Num(v, SurfaceHoldRampUp, 0.05f, 5f); break;
                            case "surfaceholdrampdown": SurfaceHoldRampDown = Num(v, SurfaceHoldRampDown, 0.05f, 10f); break;
                            case "surfaceholdmaxseconds": SurfaceHoldMaxSeconds = Num(v, SurfaceHoldMaxSeconds, 5f, 120f); break;
                            case "surfacebrakehulls": ParseSurfaceHulls(v); break;
                            case "surfacebraketop": SurfaceBrakeTop = Num(v, SurfaceBrakeTop, 5f, 15f); break;
                            case "surfacebrakestop": SurfaceBrakeStop = Num(v, SurfaceBrakeStop, 2f, 7f); break;
                            case "surfacebrakedecel": SurfaceBrakeDecel = Num(v, SurfaceBrakeDecel, 0.01f, 0.5f); break;
                            case "surfacebrakeminrate": SurfaceBrakeMinRate = Num(v, SurfaceBrakeMinRate, 0.01f, 0.5f); break;
                            case "surfacebrakegain": SurfaceBrakeGain = Num(v, SurfaceBrakeGain, 0.5f, 10f); break;
                            case "surfacebrakemaxcut": SurfaceBrakeMaxCut = Num(v, SurfaceBrakeMaxCut, 0.1f, 1f); break;
                            case "ixdepthfix": IxDepthFix = Bool(v, IxDepthFix); break;
                            case "ixbrakewindow": IxBrakeWindow = Num(v, IxBrakeWindow, 2f, 30f); break;
                            case "ixbrakedecel": IxBrakeDecel = Num(v, IxBrakeDecel, 0.01f, 0.5f); break;
                            case "ixbrakeminrate": IxBrakeMinRate = Num(v, IxBrakeMinRate, 0.02f, 1f); break;
                            case "ixbrakeengagefactor": IxBrakeEngageFactor = Num(v, IxBrakeEngageFactor, 1f, 2f); break;
                            case "ixbrakegain": IxBrakeGain = Num(v, IxBrakeGain, 0.2f, 10f); break;
                            case "viideckhandover": ViiDeckHandover = Bool(v, ViiDeckHandover); break;
                            case "viihandoverdeck": ViiHandoverDeck = Num(v, ViiHandoverDeck, 0.3f, 3f); break;
                            case "viitrimfadeseconds": ViiTrimFadeSeconds = Num(v, ViiTrimFadeSeconds, 0.5f, 10f); break;
                            case "pdholddamp": PdHoldDamp = Bool(v, PdHoldDamp); break;
                            case "pddamphulls": ParseDampHulls(v); break;
                            case "pddampofficeronly": PdDampOfficerOnly = Bool(v, PdDampOfficerOnly); break;
                            case "pddampkd": PdDampKd = Num(v, PdDampKd, 0.5f, 5f); break;
                            case "pddampcap": PdDampCap = Num(v, PdDampCap, 0.1f, 1f); break;
                            case "pddampslew": PdDampSlew = Num(v, PdDampSlew, 0.2f, 5f); break;
                            case "ixtargetpin": IxTargetPin = Bool(v, IxTargetPin); break;
                            case "ixpinhulls": ParseIxPinHulls(v); break;
                            case "ixpinminorder": IxPinMinOrder = Num(v, IxPinMinOrder, 2f, 200f); break;
                            case "tracelogging": TraceLogging = Bool(v, TraceLogging); break;
                            default:
                                Debug.LogWarning(CompanionLoader.Tag + " config: unknown key '" + k + "' ignored");
                                break;
                        }
                    }
                }
                Save(p);
            }
            catch (Exception e)
            {
                Debug.LogWarning(CompanionLoader.Tag + " config read/write failed, defaults used: " + e.Message);
            }
        }

        private static bool Bool(string v, bool def)
        {
            bool b;
            return bool.TryParse(v, out b) ? b : def;
        }

        private static float Num(string v, float def, float lo, float hi)
        {
            float f;
            return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f) && !float.IsNaN(f) && !float.IsInfinity(f) ? Mathf.Clamp(f, lo, hi) : def;
        }

        private static string F(float f)
        {
            return f.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void Save(string p)
        {
            string nl = "\r\n";
            string s =
                "# Config-only diagnostic lock: disables all Companion corrections regardless of menu master." + nl
                + "diagnosticOnly=" + (DiagnosticOnly ? "true" : "false") + nl
                + "# Test-only Nose Down 2.1.5 casing bypass. Requires diagnosticOnly=true. Restart and reload the same save between A/B runs." + nl
                + "# Affects only Nose Down casing correction for the observed player tank, never Proper Ballast or base-game flooding." + nl
                + "diagnosticDisableNoseDownCasing=" + (DiagnosticDisableNoseDownCasing ? "true" : "false") + nl + nl
                + "# Nose Down Depth Companion (Landomatic). Requires Nose Down by Kugler." + nl
                + "# Master switch. All companion corrections stand aside when false." + nl
                + "enabled=" + (Enabled ? "true" : "false") + nl + nl
                + "# Experimental VII ordinary-dive entry torque supplement; default OFF. Combined torque cap 1.5 x stock." + nl
                + "diveEntryAssist=" + (DiveEntryAssist ? "true" : "false") + nl
                + "# Nonhistorical gameplay aid: try to reduce II/VII 2-10 m arrival overshoot. Not the test-dive procedure in section 106." + nl
                + "# Uses a bounded velocity-preview depth target, replaces other II/VII arrival/hold offsets while enabled. Default OFF." + nl
                + "preciseDepthArrival=" + (PreciseDepthArrival ? "true" : "false") + nl
                + "# Allow the player's planes station before a dive order (officers and crew); no automatic assignment. Default OFF." + nl
                + "preDivePlanes=" + (PreDivePlanes ? "true" : "false") + nl + nl
                + "# Optional settings panel: Community Patch > Other Mods > Nose Down Depth Companion." + nl
                + "# Panel setters also save this file. Community Patch restores its saved panel rows on first opening." + nl
                + "# After using the panel, change its displayed settings there; hull lists and advanced keys stay here." + nl
                + "# Reset restores coded settings (not your first-open values) and keeps the master switch unchanged." + nl + nl
                + "# Experimental VII assistance, OFF by default: final output cap only, first 1 m of ordinary dives." + nl
                + "# VIIB/VIIC/VIIC41 only. Stock integrator, alarm/crash dives and blows stay untouched." + nl
                + "viiTorqueBoost=" + (ViiTorqueBoost ? "true" : "false") + nl
                + "# Multiplier 1.0-1.5; 1.35 is the initial test value. Does not guarantee removal of the level-off." + nl
                + "viiTorqueFactor=" + F(ViiTorqueFactor) + nl + nl
                + "# Experimental deeper PD hold, OFF by default: IIA/IID/IXA, ordinary 2-10 m Hold after arrival." + nl
                + "# Extra depth is an absolute offset from the visible order, not accumulated each frame." + nl
                + "# It adds to existing settled-depth error and reduces periscope clearance. Try 0.30 m first." + nl
                + "# No sea, swell, ballast, valve or visible-order values are written." + nl
                + "pdDepthBias=" + (PdDepthBias ? "true" : "false") + nl
                + "pdBiasMeters=" + F(PdBiasMeters) + nl
                + "# true = officer required (initial test scope); false also permits sailor/unmanned holds, unvalidated." + nl
                + "pdBiasOfficerOnly=" + (PdBiasOfficerOnly ? "true" : "false") + nl
                + "# Bias transition speed, m per game second, 0.02-1. Hard gates release immediately." + nl
                + "pdBiasSlew=" + F(PdBiasSlew) + nl + nl
                + "# Experimental local wave-force reduction, OFF by default; requires the stock Nose Down planner." + nl
                + "# Recognised II/VII/IX player boat, Hold, 2-10 m order, 2-12 m deck depth, near the order only." + nl
                + "# Full reduction within 1 m of the order; fades out at 2 m. One-second entry ramp; hard gates release." + nl
                + "# Scales the explicit wave-force term for one physics call, then restores it even on exceptions." + nl
                + "# Wind conversion, depth fade, visible waves, sea-state display and buoyancy are not changed." + nl
                + "# Crests/troughs and buoyancy can still move the boat or expose a detector, even at factor 0." + nl
                + "pdWaveForce=" + (PdWaveForce ? "true" : "false") + nl
                + "# 0-1: 1 = unchanged, 0.5 = half explicit wave force at full engagement, 0 = none of that term." + nl
                + "pdWaveForceFactor=" + F(PdWaveForceFactor) + nl + nl
                + "# Upper limit, m, of the depth-target stop on every clamped hull. Allowed 10 to 400." + nl
                + "clampMaxDepth=" + ClampMaxDepth.ToString("0.##", CultureInfo.InvariantCulture) + nl + nl
                + "# Type IID gets the target stop and ascent brake while an officer is seated at the planes. true or false." + nl
                + "iidOfficerClamp=" + (IidOfficerClamp ? "true" : "false") + nl + nl
                + "# Ease fast officer-manned ascents into 5-12 m orders on clamped hulls. true or false." + nl
                + "mannedAscentBrake=" + (MannedAscentBrake ? "true" : "false") + nl + nl
                + "# Let the game's own planes work ordinary 2-10 m orders on clamped hulls (Nose Down's plane hold released). true or false." + nl
                + "shallowStandDown=" + (ShallowStandDown ? "true" : "false") + nl + nl
                + "# Also hold Nose Down's own trim torque off for those 2-10 m orders (as the NCT8f test build did), so the" + nl
                + "# game's planes alone take the boat to PD. Needs shallowStandDown. true or false." + nl
                + "shallowTrimStandDown=" + (ShallowTrimStandDown ? "true" : "false") + nl + nl
                + "# Hulls that get the 2-10 m stand-down (any manning): comma list from VIIB,VIIC,VIIC41,IIA,IID," + nl
                + "# or all (= that list), or clamped (only while the depth clamp is active). Type IX and unknown" + nl
                + "# hulls are always exempt and stay on stock Nose Down." + nl
                + "standDownHulls=" + StandDownHullsText + nl + nl
                + "# Type IIA/IID: keep Nose Down's 2 deg afloat trim (with the game's planes) until the deck is" + nl
                + "# 0.5 m under, then stand down as usual. true or false." + nl
                + "iiAfloatTrim=" + (IiAfloatTrim ? "true" : "false") + nl + nl
                + "# Type VIIB/VIIC/VIIC41/IIA/IID (IID needs iidOfficerClamp) with an OFFICER at the planes only: ease the last metres of an ordinary" + nl
                + "# 2-10 m dive so the boat does not overshoot PD. Never Type IX, sailors or unmanned planes," + nl
                + "# crash dive, alarm, blows, decks awash or ascents. true or false." + nl
                + "officerPdBrake=" + (OfficerPdBrake ? "true" : "false") + nl
                + "# Brake window, m above the order (0.5-5)." + nl
                + "pdBrakeWindow=" + F(PdBrakeWindow) + nl
                + "# Envelope deceleration, m/s2: allowed sink = sqrt(2*decel*gap) (0.01-0.5). 0.067 = 0.58 m/s at 2.5 m." + nl
                + "pdBrakeDecel=" + F(PdBrakeDecel) + nl
                + "# Lowest allowed sink rate in the envelope, m/s (0.02-1)." + nl
                + "pdBrakeMinRate=" + F(PdBrakeMinRate) + nl
                + "# Engage when sink exceeds envelope by this factor (1-2)." + nl
                + "pdBrakeEngageFactor=" + F(PdBrakeEngageFactor) + nl
                + "# Target lift, m per m/s of excess sink (0.5-10)." + nl
                + "pdBrakeGain=" + F(PdBrakeGain) + nl
                + "# Largest target lift, m (0.1-3)." + nl
                + "pdBrakeMaxOffset=" + F(PdBrakeMaxOffset) + nl
                + "# Past the order, keep braking while sinking faster than this, m/s (0.02-1)." + nl
                + "pdBrakeHoldSink=" + F(PdBrakeHoldSink) + nl + nl
                + "# Type IIA/IID afloat-trim handover: once the deck is 0.5 m under, ease Nose Down's trim out over" + nl
                + "# this many seconds (0.5-10), or sooner once |pitch| is under iiTrimFadePitch deg (0.5-8)." + nl
                + "iiTrimFadeSeconds=" + F(IiTrimFadeSeconds) + nl
                + "iiTrimFadePitch=" + F(IiTrimFadePitch) + nl
                + "# ...and only while the pitch changes slower than this, deg/s (0.2-10)." + nl
                + "iiTrimFadeRate=" + F(IiTrimFadeRate) + nl + nl
                + "# Type VIIC41 only: in Nose Down's Hold, after first reaching the order, scale the game's plane" + nl
                + "# torque down within c41DampBand m of the order (0.3-3) to c41DampMin at the order (0.2-1). true or false." + nl
                + "c41PlaneDamping=" + (C41PlaneDamping ? "true" : "false") + nl
                + "c41DampBand=" + F(C41DampBand) + nl
                + "c41DampMin=" + F(C41DampMin) + nl + nl
                + "# Type IX, ordinary dives only (any manning): when the boat sinks faster than sqrt(2*ixBrakeDecel*gap)" + nl
                + "# within ixBrakeWindow m of the order, give back a share of the game's planes to check the sink." + nl
                + "# Never crash dive, alarm, blows, decks awash or ascents. true or false." + nl
                + "ixDepthFix=" + (IxDepthFix ? "true" : "false") + nl
                + "ixBrakeWindow=" + F(IxBrakeWindow) + nl
                + "ixBrakeDecel=" + F(IxBrakeDecel) + nl
                + "ixBrakeMinRate=" + F(IxBrakeMinRate) + nl
                + "ixBrakeEngageFactor=" + F(IxBrakeEngageFactor) + nl
                + "# Planes share per m/s of excess sink (0.2-10); the share is capped at 1." + nl
                + "ixBrakeGain=" + F(IxBrakeGain) + nl + nl
                + "# Surfacing brake: ordinary surface order (0 m) only, before the game starts blowing. Between" + nl
                + "# surfaceBrakeTop and surfaceBrakeStop m deck depth, Nose Down's bow-up trim is eased (keeps at least" + nl
                + "# 1 - surfaceBrakeMaxCut) while rising faster than sqrt(2*surfaceBrakeDecel*(deck-stop)), so the bridge" + nl
                + "# does not break surface and sink back. Released as soon as any blow starts. Never emergency blow," + nl
                + "# alarm, crash dive, decks awash or blowing valve modes; blow timing untouched. true or false." + nl
                + "surfaceBrake=" + (SurfaceBrakeOn ? "true" : "false") + nl
                + "# Hulls: comma list from VIIB,VIIC,VIIC41,IIA,IID (Type IX never)." + nl
                + "surfaceBrakeHulls=" + SurfaceBrakeHullsText + nl
                + "surfaceBrakeTop=" + F(SurfaceBrakeTop) + nl
                + "surfaceBrakeStop=" + F(SurfaceBrakeStop) + nl
                + "surfaceBrakeDecel=" + F(SurfaceBrakeDecel) + nl
                + "surfaceBrakeMinRate=" + F(SurfaceBrakeMinRate) + nl
                + "surfaceBrakeGain=" + F(SurfaceBrakeGain) + nl
                + "surfaceBrakeMaxCut=" + F(SurfaceBrakeMaxCut) + nl
                + "# false = surfaceBrake runs only the plane-hold below, not the trim easing." + nl
                + "surfaceTrimBrake=" + (SurfaceTrimBrake ? "true" : "false") + nl
                + "# v1.0.14 plane-hold, same hulls, any manning: in Ascent to a 0 m order, before any blow, the game's" + nl
                + "# planes (share surfaceHoldShare) hold deck at surfaceHoldDeck m from surfaceHoldTop m up. Released" + nl
                + "# at once when the blow starts (mass -0.2%, blowing valves, alarm, emergency), phase change, order" + nl
                + "# change, or after surfaceHoldMaxSeconds. Blow timing untouched. true or false." + nl
                + "surfaceHold=" + (SurfaceHoldOn ? "true" : "false") + nl
                + "surfaceHoldDeck=" + F(SurfaceHoldDeck) + nl
                + "surfaceHoldTop=" + F(SurfaceHoldTop) + nl
                + "surfaceHoldShare=" + F(SurfaceHoldShare) + nl
                + "# Share per second in / out." + nl
                + "surfaceHoldRampUp=" + F(SurfaceHoldRampUp) + nl
                + "surfaceHoldRampDown=" + F(SurfaceHoldRampDown) + nl
                + "surfaceHoldMaxSeconds=" + F(SurfaceHoldMaxSeconds) + nl + nl
                + "# v1.0.15 VIIB/VIIC/VIIC41 dive handover: on 2-10 m orders Nose Down keeps its dive trim (and holds" + nl
                + "# the game's planes) until the deck is viiHandoverDeck m under (0.3-3), then its trim is eased out and" + nl
                + "# the game's planes eased in over viiTrimFadeSeconds (0.5-10). false = v1.0.14 behaviour (hand over" + nl
                + "# as soon as the flood valve opens). true or false." + nl
                + "viiDeckHandover=" + (ViiDeckHandover ? "true" : "false") + nl
                + "viiHandoverDeck=" + F(ViiHandoverDeck) + nl
                + "viiTrimFadeSeconds=" + F(ViiTrimFadeSeconds) + nl + nl
                + "# v1.0.15 PD-hold damping: 2-10 m orders, Nose Down Hold phase, only after the boat has first" + nl
                + "# reached the order. The game's depth target is offset by pdDampKd x vertical speed (s, 0.5-5)," + nl
                + "# capped at +/- pdDampCap m (0.1-1), slewed at pdDampSlew m/s (0.2-5), so the boat stops bobbing" + nl
                + "# about the order. Never crash dive, alarm, blows, decks awash, or while the PD/IX brake acts." + nl
                + "# Hulls: comma list from IIA,IID,IXA,VIIB,VIIC,VIIC41 (other Type IX never). true or false." + nl
                + "pdHoldDamp=" + (PdHoldDamp ? "true" : "false") + nl
                + "pdDampHulls=" + PdDampHullsText + nl
                + "# true = only with an officer at the planes." + nl
                + "pdDampOfficerOnly=" + (PdDampOfficerOnly ? "true" : "false") + nl
                + "pdDampKd=" + F(PdDampKd) + nl
                + "pdDampCap=" + F(PdDampCap) + nl
                + "pdDampSlew=" + F(PdDampSlew) + nl + nl
                + "# v1.0.16 Type IX target pin: on an ordinary dive order deeper than ixPinMinOrder m (2-200, default 10, so" + nl
                + "# periscope-depth holds are untouched), once the boat has first crossed the order the game's depth target is" + nl
                + "# pinned at the order until the order changes (it otherwise runs ~3 m past it and the boat creeps deeper)." + nl
                + "# Never crash dive, alarm, blows, decks awash or ascents. Hulls: comma list from IXA,IXC,IXC40. true or false." + nl
                + "ixTargetPin=" + (IxTargetPin ? "true" : "false") + nl
                + "ixPinHulls=" + IxPinHullsText + nl
                + "ixPinMinOrder=" + F(IxPinMinOrder) + nl + nl
                + "# Diagnostic lines in output_log.txt about twice a second. Leave off for normal play." + nl
                + "traceLogging=" + (TraceLogging ? "true" : "false") + nl;
            File.WriteAllText(p, s);
        }
        private static void ParseIxPinHulls(string v)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (string part in (v ?? "").Split(','))
            {
                string p = part.Trim().ToUpperInvariant().Replace("TYPE", "").Replace("/", "").Replace(" ", "");
                if (p.Length == 0) continue;
                string key = "Type" + p;
                if (Array.IndexOf(IxPinAllowed, key) >= 0 && !list.Contains(key))
                    list.Add(key);
            }
            IxPinHulls = list.ToArray();
            IxPinHullsText = list.Count == 0 ? "none" : string.Join(",", list.ConvertAll(k => k.Substring(4)).ToArray());
        }

        private static void ParseDampHulls(string v)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (string part in (v ?? "").Split(','))
            {
                string p = part.Trim().ToUpperInvariant().Replace("TYPE", "").Replace("/", "");
                if (p.Length == 0) continue;
                string key = "Type" + p;
                if (Array.IndexOf(PdDampAllowed, key) >= 0 && !list.Contains(key))
                    list.Add(key);
            }
            PdDampHulls = list.ToArray();
            PdDampHullsText = list.Count == 0 ? "none" : string.Join(",", list.ConvertAll(k => k.Substring(4)).ToArray());
        }

        private static void ParseSurfaceHulls(string v)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (string part in (v ?? "").Split(','))
            {
                string p = part.Trim().ToUpperInvariant().Replace("TYPE", "").Replace("/", "");
                if (p.Length == 0) continue;
                string key = "Type" + p;
                if (Array.IndexOf(HullKey.StandDownAllowed, key) >= 0 && !list.Contains(key))
                    list.Add(key);
            }
            SurfaceBrakeHulls = list.ToArray();
            SurfaceBrakeHullsText = list.Count == 0 ? "none" : string.Join(",", list.ConvertAll(k => k.Substring(4)).ToArray());
        }

        private static void ParseHulls(string v)
        {
            string s = (v ?? "").Trim();
            string low = s.ToLowerInvariant();
            StandDownClampedOnly = low == "clamped";
            if (low == "all" || low == "clamped" || s.Length == 0)
            {
                StandDownHulls = null;
                StandDownHullsText = low == "clamped" ? "clamped" : "VIIB,VIIC,VIIC41,IIA,IID";
                return;
            }
            var list = new System.Collections.Generic.List<string>();
            foreach (string part in s.Split(','))
            {
                string p = part.Trim().ToUpperInvariant().Replace("TYPE", "").Replace("/", "");
                if (p.Length == 0) continue;
                string key = "Type" + p;
                if (Array.IndexOf(HullKey.StandDownAllowed, key) >= 0 && !list.Contains(key))
                    list.Add(key);
            }
            StandDownHulls = list.ToArray();
            StandDownHullsText = string.Join(",", list.ConvertAll(k => k.Substring(4)).ToArray());
            if (StandDownHullsText.Length == 0) StandDownHullsText = "none";
        }

    }
}

