// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Controls for separate diving mods by Kugler (https://github.com/Isamylov).
namespace NoseDownCompanion.KuglerNoseDown
{
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Kugler: Nose Down"; } }
        public static string Category { get { return "Submarine / Diving"; } }
        public static string Description { get { return KuglerSettingsBridge.Description(KuglerSwitchCatalog.NoseDown); } }
        public static object[] GetSettings() { return KuglerSettingsBridge.Rows(KuglerSwitchCatalog.NoseDown); }
        public static void ResetToDefaults() { KuglerSettingsBridge.Reset(KuglerSwitchCatalog.NoseDown); }
    }
}

namespace NoseDownCompanion.KuglerProperBallast
{
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Kugler: Proper Ballast"; } }
        public static string Category { get { return "Submarine / Diving"; } }
        public static string Description { get { return KuglerSettingsBridge.Description(KuglerSwitchCatalog.ProperBallast); } }
        public static object[] GetSettings() { return KuglerSettingsBridge.Rows(KuglerSwitchCatalog.ProperBallast); }
        public static void ResetToDefaults() { KuglerSettingsBridge.Reset(KuglerSwitchCatalog.ProperBallast); }
    }
}

namespace NoseDownCompanion.KuglerSeakeeping
{
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Kugler: Seakeeping"; } }
        public static string Category { get { return "Submarine / Diving"; } }
        public static string Description { get { return KuglerSettingsBridge.Description(KuglerSwitchCatalog.Seakeeping); } }
        public static object[] GetSettings() { return KuglerSettingsBridge.Rows(KuglerSwitchCatalog.Seakeeping); }
        public static void ResetToDefaults() { KuglerSettingsBridge.Reset(KuglerSwitchCatalog.Seakeeping); }
    }
}
