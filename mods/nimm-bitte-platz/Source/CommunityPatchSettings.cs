// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source/history: https://github.com/Landomatic/UboatMods/tree/main/mods/nimm-bitte-platz
using System;
using UBOAT.Game.Core.Serialization;

namespace UBOAT.Mods.HydrophoneSurfaceNoiseFix
{
    // Dusty's reflection contract: no reference to the Community Patch assembly.
    [NonSerializedInGameState]
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Nimm Bitte Platz"; } }
        public static string Category { get { return "Audio"; } }
        public static string Description
        {
            get { return "Hydrophone Fix " + ModInfo.Version + " by Landomatic. Corrects lingering exterior wash in the submerged hydrophone view."; }
        }

        // A Bool row supplies an immediate setter. Dusty's optional top-level Enabled
        // field has no callback, so it cannot release an owned audio flag immediately.
        public static object[] GetSettings()
        {
            ModSettings.EnsureLoaded();
            return new object[]
            {
                new object[] { "bool", "FixEnabled", "Hydrophone correction",
                    "Correct submerged hydrophone ambience. Turning this off immediately releases the correction.",
                    new Func<object>(delegate { return ModSettings.Enabled; }),
                    new Action<object>(delegate (object value) { ModSettings.SetEnabled(Convert.ToBoolean(value)); }) },
                new object[] { "choice", "LogLevel", "Logging level",
                    "Info: startup, shutdown and station events. Debug: correction decisions. Trace: sampled troubleshooting details.",
                    new Func<object>(delegate { return (int)ModSettings.Level; }),
                    new Action<object>(delegate (object value) { ModSettings.SetLevel(Convert.ToInt32(value)); }),
                    new string[] { "Info", "Debug", "Trace" } },
                new object[] { "float", "TraceIntervalSeconds", "Trace interval (seconds)",
                    "Time between detailed samples while using the hydrophone. Used only at Trace level; default 2 seconds.",
                    new Func<object>(delegate { return ModSettings.TraceInterval; }),
                    new Action<object>(delegate (object value) { ModSettings.SetTraceInterval(Convert.ToSingle(value)); }),
                    0.25f, 60f, 0.25f }
            };
        }

        public static void ResetToDefaults() { ModSettings.ResetToDefaults(); }
    }
}
