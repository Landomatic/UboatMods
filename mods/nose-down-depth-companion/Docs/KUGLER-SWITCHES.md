# Kugler switch menus — companion 1.0.19

These controls are supplied by Landomatic's companion for Kugler's separate mods. Only their config files are updated. Source and existing licenses remain separate.

All switches save immediately. `(restart)` rows apply at the next game launch; only the casing fix is applied live when compatible Proper Ballast is loaded. Numeric values are retained.

## Nose Down 2.1.4

| Menu switch | Config key | Apply |
| --- | --- | --- |
| First-person camera tilt | `enableFPPTilt` | Restart |
| Periscope trim | `enablePeriscopeTrim` | Restart |
| Section-view trim | `enableSectionViewTrim` | Restart |
| Trim status icon | `enableStatusIcon` | Restart |
| Diagnostic telemetry | `enableTelemetry` | Restart |
| Command-file experiments | `enableExperiments` | Restart |

## Proper Ballast 1.1.0

| Menu switch | Config key | Apply |
| --- | --- | --- |
| Type IX deck-awash fix | `fixDeckAwashIX` | Restart |
| Flood-opening height fix | `fixHoleHeight` | Restart |
| Casing flooding fix | `fixCasingFlooding` | Live if loaded |
| Accurate valve text | `honestValveText` | Restart |
| Buoyancy HUD percentage | `buoyancyHudPercent` | Restart |
| Surface-air warning | `surfaceAirWarning` | Restart |
| Emergency-blow button | `emergencyBlowButton` | Restart |
| Surface icon | `surfaceIcon` | Restart |
| Diesel ballast blowing | `dieselBlowing` | Restart |
| Automatic diesel blowing | `dieselBlowAuto` | Restart |
| Diesel-blow task row | `dieselBlowRow` | Restart |
| Diesel-blow icon | `dieselBlowIcon` | Restart |
| Tank details in buoyancy tooltip | `buoyancyTooltipTanks` | Restart |
| Low-air ribbon when needed | `lowAirRibbonAtNeed` | Restart |
| Diagnostic recording | `diagnostic` | Restart |

## Seakeeping 1.2.2

| Menu switch | Config key | Apply |
| --- | --- | --- |
| Enable Seakeeping corrections | `enabled` | Restart |
| Wind-speed unit correction | `windSpeedUnitFixEnabled` | Restart |
| Depth-gated sea-state reading | `depthGatedSeaStateEnabled` | Restart |
| Diagnostic telemetry | `telemetry` | Restart |
| Disable forced-depth correction | `forcedDepthKeepingOverrideEnabled` | Restart |
| Sea clock follows physics | `seaClockPhysicsRateEnabled` | Restart |
| CPU wave-sampling resolution fix | `cpuSamplingResolutionEnabled` | Restart |
| Wave-force depth fade | `waveForceDepthFadeEnabled` | Restart |
| Optional CPU diagnostic probe | `probeEnabled` | Restart |
| Optional yaw diagnostic probe | `yawProbeEnabled` | Restart |

Seakeeping's `waveAnimationCapEnabled` is an alias of `seaClockPhysicsRateEnabled`; both spellings are kept consistent when saving, with one menu control. The retired `waveAnimationCapMultiplier` and Nose Down bookkeeping fields are not controls.

Optional CPU/yaw probe switches are saved for upstream builds that include those diagnostics. They do not install a probe. A menu cannot load a mod disabled in UBOAT's launcher.

Reset affects only the selected mod's boolean switches. Keep the companion's officer brake OFF, Proper Ballast casing fix ON, and Seakeeping/Patch Profiler disabled for the current isolation run.
