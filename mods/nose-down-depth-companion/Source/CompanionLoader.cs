// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using HarmonyLib;
using UBOAT.Game;
using UBOAT.Game.Core.Mods;
using UBOAT.Game.Core.Serialization;
using UnityEngine;

namespace NoseDownCompanion
{
    [NonSerializedInGameState]
    public class CompanionLoader : IUserMod
    {
        internal const string Tag = "[NoseDownCompanion]";
        internal const string BuildId = "v1.0.21";
        internal const string HarmonyId = "com.landomatic.nosedown.companion";
        internal static Harmony Instance;

        public void OnLoaded()
        {
            try
            {
                CompanionConfig.Load();
                var harmony = new Harmony(HarmonyId);
                Instance = harmony;
                harmony.CreateClassProcessor(typeof(DepthTargetClamp)).Patch();
                harmony.CreateClassProcessor(typeof(ShallowStandDown)).Patch();
                PdWaveForce.Install(harmony);
                harmony.CreateClassProcessor(typeof(PreDivePlanes)).Patch();
                Debug.Log(Tag + " initialized [" + BuildId + "] by Landomatic, companion to Nose Down by Kugler. "
                    + "enabled=" + CompanionConfig.Enabled
                    + " diveEntryAssist=" + CompanionConfig.DiveEntryAssist
                    + " preciseDepthArrival=" + CompanionConfig.PreciseDepthArrival
                    + " preDivePlanes=" + CompanionConfig.PreDivePlanes + " clampMaxDepth=" + CompanionConfig.ClampMaxDepth
                    + " iidOfficerClamp=" + CompanionConfig.IidOfficerClamp
                    + " mannedAscentBrake=" + CompanionConfig.MannedAscentBrake
                    + " shallowStandDown=" + CompanionConfig.ShallowStandDown
                    + " shallowTrimStandDown=" + CompanionConfig.ShallowTrimStandDown
                    + " standDownHulls=" + CompanionConfig.StandDownHullsText
                    + " iiAfloatTrim=" + CompanionConfig.IiAfloatTrim
                    + " officerPdBrake=" + CompanionConfig.OfficerPdBrake
                    + " pdBrake(window/decel/minRate/engage/gain/max)=" + CompanionConfig.PdBrakeWindow + "/" + CompanionConfig.PdBrakeDecel
                    + "/" + CompanionConfig.PdBrakeMinRate + "/" + CompanionConfig.PdBrakeEngageFactor + "/" + CompanionConfig.PdBrakeGain
                    + "/" + CompanionConfig.PdBrakeMaxOffset + " pdBrakeHoldSink=" + CompanionConfig.PdBrakeHoldSink
                    + " iiTrimFade=" + CompanionConfig.IiTrimFadeSeconds + "s/" + CompanionConfig.IiTrimFadePitch + "deg/" + CompanionConfig.IiTrimFadeRate + "deg/s"
                    + " c41PlaneDamping=" + CompanionConfig.C41PlaneDamping + "(" + CompanionConfig.C41DampBand + "m," + CompanionConfig.C41DampMin + ")"
                    + " ixDepthFix=" + CompanionConfig.IxDepthFix + "(window/decel/minRate/engage/gain=" + CompanionConfig.IxBrakeWindow + "/" + CompanionConfig.IxBrakeDecel
                    + "/" + CompanionConfig.IxBrakeMinRate + "/" + CompanionConfig.IxBrakeEngageFactor + "/" + CompanionConfig.IxBrakeGain + ")"
                    + " ixTargetPin=" + CompanionConfig.IxTargetPin + "(" + CompanionConfig.IxPinHullsText + " minOrder=" + CompanionConfig.IxPinMinOrder + ")"
                    + " surfaceBrake=" + CompanionConfig.SurfaceBrakeOn + "(" + CompanionConfig.SurfaceBrakeHullsText + " top/stop/decel/minRate/gain/maxCut="
                    + CompanionConfig.SurfaceBrakeTop + "/" + CompanionConfig.SurfaceBrakeStop + "/" + CompanionConfig.SurfaceBrakeDecel + "/"
                    + CompanionConfig.SurfaceBrakeMinRate + "/" + CompanionConfig.SurfaceBrakeGain + "/" + CompanionConfig.SurfaceBrakeMaxCut + " trim=" + CompanionConfig.SurfaceTrimBrake + " hold=" + CompanionConfig.SurfaceHoldOn + "@" + CompanionConfig.SurfaceHoldDeck + "m share=" + CompanionConfig.SurfaceHoldShare + " max=" + CompanionConfig.SurfaceHoldMaxSeconds + "s)"
                    + " viiTorqueBoost=" + CompanionConfig.ViiTorqueBoost + "@" + CompanionConfig.ViiTorqueFactor
                    + " pdDepthBias=" + CompanionConfig.PdDepthBias + "@" + CompanionConfig.PdBiasMeters + "m officerOnly=" + CompanionConfig.PdBiasOfficerOnly
                    + " pdWaveForce=" + CompanionConfig.PdWaveForce + "@" + CompanionConfig.PdWaveForceFactor + " hook=" + PdWaveForce.Ready
                    + " traceLogging=" + CompanionConfig.TraceLogging
                    + ". Clamp hulls: TypeVIIB, TypeVIIC, TypeVIIC41 (any manning); TypeIIA, TypeIID (officer seated). Type IX uses only its configured approach brake, target pin and IXA hold options; other hulls exempt. "
                    + "Nose Down presence is checked on the first submarine update.");
            }
            catch (Exception e)
            {
                Debug.LogError(Tag + " failed to initialize; companion is off.");
                Debug.LogException(e);
            }
        }
    }
}
