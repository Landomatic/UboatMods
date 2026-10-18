// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
// Config-key inventory for separate mods by Kugler (https://github.com/Isamylov).
namespace NoseDownCompanion
{
    internal sealed class KuglerSwitch
    {
        internal readonly string Key, Field, Label, Help;
        internal readonly bool Default, Live;
        internal readonly string[] Aliases;
        internal KuglerSwitch(string key, string field, string label, string help, bool defaultValue = true,
            bool live = false, params string[] aliases)
        {
            Key = key; Field = field; Label = label; Help = help;
            Default = defaultValue; Live = live; Aliases = aliases;
        }
    }

    internal sealed class KuglerNumber
    {
        internal readonly string Key, Field, Label, Help;
        internal readonly float Default, Min, Max, Step;
        internal KuglerNumber(string key, string field, string label, float defaultValue)
        {
            Key = key; Field = field; Label = label; Default = defaultValue;
            Min = 1; Max = 5; Step = 0.1f;
            Help = "Full-skill flooding factor: 1 is vanilla flow; at zero Diving points flow remains vanilla. Intermediate skill scales the effect. Fast dive must be enabled. Does not disable Fast dive's vent-timing changes. Restart UBOAT after changing.";
        }
    }

    internal sealed class KuglerMod
    {
        internal readonly string Name, Version, OwnerType, FileName, HarmonyId;
        internal readonly bool InlineComments;
        internal readonly KuglerSwitch[] Switches;
        internal KuglerNumber[] Numbers = new KuglerNumber[0];
        internal KuglerMod(string name, string version, string owner, string fileName, string harmonyId, bool inlineComments, params KuglerSwitch[] switches)
        {
            Name = name; Version = version; OwnerType = owner; FileName = fileName;
            HarmonyId = harmonyId; InlineComments = inlineComments; Switches = switches;
        }
    }

    internal static class KuglerSwitchCatalog
    {
        private static readonly KuglerMod NoseDownLegacy = new KuglerMod("Nose Down", "2.1.4", "DivePitch.ModLoader", "NoseDownConfig.txt", "com.kugler.divepitch", true,
            new KuglerSwitch("enableFPPTilt", "enableFPPTilt", "First-person camera tilt", "Let the first-person view follow the boat's pitch and roll."),
            new KuglerSwitch("enablePeriscopeTrim", "enablePeriscopeTrim", "Periscope trim", "Show the boat's trim in the periscope view."),
            new KuglerSwitch("enableSectionViewTrim", "enableSectionViewTrim", "Section-view trim", "Show the boat's trim in the cutaway view."),
            new KuglerSwitch("enableStatusIcon", "enableStatusIcon", "Trim status icon", "Show Nose Down's status-panel icon."),
            new KuglerSwitch("enableTelemetry", "enableTelemetry", "Diagnostic telemetry", "Enable Nose Down's troubleshooting log output.", false),
            new KuglerSwitch("enableExperiments", "enableExperiments", "Command-file experiments", "Allow Nose Down's existing command-file experiment runner. Leave off for normal play.", false));

        private static readonly KuglerMod ProperBallastLegacy = new KuglerMod("Proper Ballast", "1.1.0", "BallastReworked.ModLoader", "ProperBallastConfig.txt", "com.kugler.ballastreworked", true,
            new KuglerSwitch("fixDeckAwashIX", "FixDeckAwashIX", "Type IX deck-awash fix", "Correct Type IX ballast selection for deck awash."),
            new KuglerSwitch("fixHoleHeight", "FixHoleHeight", "Flood-opening height fix", "Judge flood openings using the hull's trim."),
            new KuglerSwitch("fixCasingFlooding", "FixCasingFlooding", "Casing flooding fix", "Kugler's casing-water correction. Reload the same starting save for clean comparisons.", true, true),
            new KuglerSwitch("honestValveText", "HonestValveText", "Accurate valve text", "Show the ballast valve's actual state."),
            new KuglerSwitch("buoyancyHudPercent", "BuoyancyHudPercent", "Buoyancy HUD percentage", "Show Proper Ballast's buoyancy indicator."),
            new KuglerSwitch("surfaceAirWarning", "SurfaceAirWarning", "Surface-air warning", "Show the surface compressed-air warning."),
            new KuglerSwitch("emergencyBlowButton", "EmergencyBlowButton", "Emergency-blow button", "Show the emergency-blow control."),
            new KuglerSwitch("surfaceIcon", "SurfaceIcon", "Surface icon", "Show Proper Ballast's surface icon."),
            new KuglerSwitch("dieselBlowing", "DieselBlowing", "Diesel ballast blowing", "Enable the diesel-exhaust ballast-blowing feature."),
            new KuglerSwitch("dieselBlowAuto", "DieselBlowAuto", "Automatic diesel blowing", "Allow automatic diesel blowing when its conditions are met."),
            new KuglerSwitch("dieselBlowRow", "DieselBlowRow", "Diesel-blow task row", "Show the diesel-blowing task row."),
            new KuglerSwitch("dieselBlowIcon", "DieselBlowIcon", "Diesel-blow icon", "Show the diesel-blowing status icon."),
            new KuglerSwitch("buoyancyTooltipTanks", "BuoyancyTooltipTanks", "Tank details in buoyancy tooltip", "Include ballast-tank details in the buoyancy tooltip."),
            new KuglerSwitch("lowAirRibbonAtNeed", "LowAirRibbonAtNeed", "Low-air ribbon when needed", "Show the low-air ribbon when the boat needs it."),
            new KuglerSwitch("diagnostic", "Diagnostic", "Diagnostic recording", "Enable Proper Ballast's diagnostic recording.", false));

        private static readonly KuglerMod NoseDownLatest = NewNoseDown();
        private static readonly KuglerMod ProperBallastLatest = NewProperBallast();
        internal static KuglerMod NoseDown
        {
            get { return KuglerSettingsBridge.LegacyRuntime(NoseDownLatest, "enableTrimWater") ? NoseDownLegacy : NoseDownLatest; }
        }
        internal static KuglerMod ProperBallast
        {
            get { return KuglerSettingsBridge.LegacyRuntime(ProperBallastLatest, "FastDive") ? ProperBallastLegacy : ProperBallastLatest; }
        }

        private static KuglerMod NewNoseDown()
        {
            var switches = (KuglerSwitch[])NoseDownLegacy.Switches.Clone();
            switches[switches.Length - 1] = new KuglerSwitch("enableTrimWater", "enableTrimWater", "Trim-aware compartment water",
                "Let compartment water levels and flow follow the boat's trim. Introduced in Nose Down 2.1.5; this is separate from pitch assistance.");
            return new KuglerMod("Nose Down", "2.1.5", NoseDownLegacy.OwnerType, NoseDownLegacy.FileName, NoseDownLegacy.HarmonyId, true, switches);
        }

        private static KuglerMod NewProperBallast()
        {
            var switches = new KuglerSwitch[ProperBallastLegacy.Switches.Length + 1];
            System.Array.Copy(ProperBallastLegacy.Switches, switches, ProperBallastLegacy.Switches.Length);
            switches[switches.Length - 1] = new KuglerSwitch("fastDive", "FastDive", "Crew-skill faster diving",
                "Use crew Diving points to shorten vent-opening delays and increase flooding flow. Zero points retains vanilla timing and flow. Introduced in Proper Ballast 1.2.0; restart between comparison runs.");
            return new KuglerMod("Proper Ballast", "1.2.0", ProperBallastLegacy.OwnerType, ProperBallastLegacy.FileName, ProperBallastLegacy.HarmonyId, true, switches)
            {
                Numbers = new[] {
                    new KuglerNumber("floodRateVII", "FloodRateVII", "Type VII full-skill flood factor", 4),
                    new KuglerNumber("floodRateIX", "FloodRateIX", "Type IX full-skill flood factor", 2),
                    new KuglerNumber("floodRateII", "FloodRateII", "Type II full-skill flood factor", 3)
                }
            };
        }

        internal static readonly KuglerMod Seakeeping = new KuglerMod("Seakeeping", "1.2.2", "Seakeeping.ConfigProvider", "SeakeepingConfig.txt", "com.kugler.seakeeping", false,
            new KuglerSwitch("enabled", "Enabled", "Seakeeping corrections", "Controls Seakeeping's config master switch; it does not load a mod disabled in the launcher."),
            new KuglerSwitch("windSpeedUnitFixEnabled", "WindSpeedUnitFixEnabled", "Wind-speed unit correction", "Use Seakeeping's wind-speed unit correction."),
            new KuglerSwitch("depthGatedSeaStateEnabled", "DepthGatedSeaStateEnabled", "Depth-gated sea-state reading", "Adjust the sea-state reading by depth."),
            new KuglerSwitch("telemetry", "Telemetry", "Diagnostic telemetry", "Enable Seakeeping's troubleshooting log output.", false),
            new KuglerSwitch("forcedDepthKeepingOverrideEnabled", "ForcedDepthKeepingOverrideEnabled", "Disable forced-depth correction", "Allow Seakeeping to suppress the game's forced-depth-keeping correction."),
            new KuglerSwitch("seaClockPhysicsRateEnabled", "SeaClockPhysicsRateEnabled", "Sea clock follows physics", "Synchronize the sea clock with the physics clock at high time compression.", true, false, "waveAnimationCapEnabled"),
            new KuglerSwitch("cpuSamplingResolutionEnabled", "CpuSamplingResolutionEnabled", "CPU wave-sampling resolution fix", "Use Seakeeping's configured CPU wave-grid resolution correction."),
            new KuglerSwitch("waveForceDepthFadeEnabled", "WaveForceDepthFadeEnabled", "Wave-force depth fade", "Apply Seakeeping's physical wave-force fade with depth."),
            new KuglerSwitch("probeEnabled", "ProbeEnabled", "Optional CPU diagnostic probe", "Saves the existing probe switch. It only works in builds that include the optional probe; it does not install one.", false),
            new KuglerSwitch("yawProbeEnabled", "YawProbeEnabled", "Optional yaw diagnostic probe", "Saves the existing yaw-probe switch. It only works in builds that include that optional probe.", false));
    }
}
