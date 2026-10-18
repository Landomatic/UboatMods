# Kugler switch menus — companion 1.0.22

These controls are supplied by Landomatic's companion for Kugler's separate mods. Only their config files are updated. Source and existing licenses remain separate.

All switches save immediately. `(restart)` rows apply at the next game launch; only the casing fix is applied live when compatible Proper Ballast is loaded. Existing numeric values are retained; three new flood-factor sliders save for restart.

## Session state versus saved configuration

All three menus begin with informational **Mod loaded this session** and **Saved settings differ from this session** rows. Seakeeping additionally reports **Seakeeping corrections enabled this session**, using its current runtime master rather than its saved config master.

Loaded status requires the mod's own Harmony patches to be registered. An assembly or config file alone does not count. No patches means disabled, absent or unsuccessful initialization; patch detection does not certify every feature works. The settings host cannot change the launcher mod list.

Every feature row is prefixed **Saved:**. An ON selection while a mod is not loaded is a saved preference, not an active effect. Restart-labelled rows remain pending until the next launch. A status row ignores setters and the host's persisted/replayed status values. Dusty's supported contract has no read-only row kind: these informational rows look like checkboxes, but clicks cannot change the underlying state.

Opening a menu does not itself rewrite config files. Dusty may replay previously stored feature selections at provider discovery; that pre-existing precedence is unchanged. Status rows never save a value into an upstream config. Provider names and feature keys remain stable across the upgrade.

## Nose Down 2.1.5

| Menu switch | Config key | Apply |
| --- | --- | --- |
| First-person camera tilt | `enableFPPTilt` | Restart |
| Periscope trim | `enablePeriscopeTrim` | Restart |
| Section-view trim | `enableSectionViewTrim` | Restart |
| Trim status icon | `enableStatusIcon` | Restart |
| Diagnostic telemetry | `enableTelemetry` | Restart |
| Trim-aware compartment water | `enableTrimWater` | Restart |

## Proper Ballast 1.2.0

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
| Crew-skill faster diving | `fastDive` | Restart |
| Type VII full-skill flood factor | `floodRateVII` (1–5; default 4) | Restart |
| Type IX full-skill flood factor | `floodRateIX` (1–5; default 2) | Restart |
| Type II full-skill flood factor | `floodRateII` (1–5; default 3) | Restart |

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


## Upstream update and test implications

The supplied upload contains Nose Down 2.1.5, Proper Ballast 1.2.0 and Always
Load Sea Floor 1.5.1. No updated Seakeeping source was supplied; its menu remains
based on 1.2.2. Always Load Sea Floor exposes no user config switches to host.

Nose Down 2.1.5 adds trim-aware compartment water (default ON), retires the
command-file experiment flag, adds a separate bow-lift counter-torque and changes
the controller's authority/target-rate handling. Its Compute method retains the
parameter names used by our existing hooks and adds two optional parameters.
The new counter-torque is outside that law result; our diagnostic bowLiftMoment
reads its LastMoment separately when available, or n/a on older versions.
That observation does not suppress or modify the upstream counter-torque.

Proper Ballast 1.2.0 adds Fast dive (default ON): crew Diving points influence
vent-opening timing and flooding flow. At zero points it retains vanilla timing
and flow. The sliders set full-skill flow factors; intermediate skill scales
the effect. Setting every factor to 1 does NOT disable its vent-timing changes.
Turn Fast dive OFF and restart to exclude both changes from a comparison.
Fast dive is deliberately not applied live: toggling it off mid-dive is not a
clean reversal of previously altered flood openings.

Before tuning our entry assistance against these new versions, make one fresh
comparison with our extra entry assistance and VII torque boost OFF and Fast
dive OFF. Keep the officer OFF the planes, the same hull, speed, weather and
ordinary depth order, and keep precise-arrival selection fixed between runs.
Restart between upstream setting changes. Then test Fast dive ON separately
before reintroducing our extra torque. No saved settings are reset automatically.
This is an upstream-version baseline, distinct from the all-mods-off vanilla run.
Do not mix older/newer upstream versions within a same-build comparison.

## Legacy contracts and resets

A loaded readable config without enableTrimWater selects the Nose Down 2.1.4
catalog, retaining its experiment switch. A loaded readable config without
FastDive selects Proper Ballast 1.1.0 and hides the new switch/sliders. This is
capability detection of supported contracts, not a general version detector.
If no runtime config can be read, the newest supported catalog is shown when a
saved file exists; its label states the supported source version. Saving such a
row does not install or activate an older/disabled mod's missing feature.
Provider names and shared keys remain stable. A retired latest-version key is
not exposed merely because Dusty's persisted an older row value.

Reset restores the selected catalog's boolean defaults and preserves numeric
settings, including the new flood factors. Existing comments, unknown keys and
other mods' config files remain intact. All new controls require a restart.
