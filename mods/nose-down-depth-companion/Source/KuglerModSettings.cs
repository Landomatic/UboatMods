// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Optional controls for separate mods by Kugler (https://github.com/Isamylov).
using System;
using System.Globalization;

namespace NoseDownCompanion.KuglerMods
{
    // Hosted by this companion; no edits to Kugler's source or manifests are needed.
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Kugler diving mods"; } }
        public static string Category { get { return "Submarine / Diving"; } }
        public static string Description
        {
            get { return "Controls for Kugler's separate diving mods, hosted by Landomatic. Proper Ballast must be enabled for the casing switch to appear. Changes apply live and save in Proper Ballast's config. Reset enables the casing fix; other Proper Ballast settings are kept."; }
        }

        public static object[] GetSettings()
        {
            if (!ProperBallastSettings.Available) return new object[0];
            return new object[]
            {
                new object[] { "bool", "fixCasingFlooding", "Casing flooding fix (Proper Ballast)",
                    "Kugler's casing-water correction. Saves fixCasingFlooding in ProperBallastConfig.txt. Applies live; for a clean comparison reload the same starting save after changing it. Leave ON for the current officer-brake test.",
                    new Func<object>(() => ProperBallastSettings.CasingFixEnabled),
                    new Action<object>(value => ProperBallastSettings.SetCasingFix(Convert.ToBoolean(value, CultureInfo.InvariantCulture))) }
            };
        }

        public static void ResetToDefaults()
        {
            if (ProperBallastSettings.Available) ProperBallastSettings.SetCasingFix(true);
        }
    }
}
