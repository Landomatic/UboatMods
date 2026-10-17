Nose Down Depth Companion -- 1.0.19
Author: Landomatic
Companion to Nose Down by Kugler (https://github.com/Isamylov).
Requires STOCK Nose Down (Workshop 3729673507). Load this companion after it.

Source mod for UBOAT 2026.1. Compiled against the supplied game assemblies;
offline behavior checks passed. This build has not run inside UBOAT.
All three NEW physics options are experimental and OFF by default.

UPGRADE FROM 1.0.18
1. Close UBOAT. Keep 1.0.18 as a rollback copy outside the Mods folder.
2. Replace the installed NoseDownDepthCompanion folder with this folder:
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\Mods\NoseDownDepthCompanion
   Manifest.json should be directly inside, without a doubled folder.
3. Keep stock Nose Down enabled, then load this companion after it.
   Do not install old NCT overlays over Nose Down. No changes to Kugler's files
   or your other diving mods are needed for this upgrade.
4. KEEP NoseDownCompanionConfig.txt. Existing values are loaded; new keys are
   added with comments. Do not replace it with a blank/default config.
5. Confirm output_log.txt contains "[NoseDownCompanion] initialized [v1.0.19]"
   and "[NoseDownCompanion] Nose Down found".
   If an older build still loads, close the game, remove the cached compiled
   companion from UBOAT\Temp, and relaunch. Keep your Saves and configs.

NEW IN 1.0.19: ALL KUGLER CONFIG SWITCHES
Our companion now supplies three separate entries under Other Mods:
 - Kugler: Nose Down -- 6 boolean switches from supplied version 2.1.4.
 - Kugler: Proper Ballast -- 15 boolean switches from supplied version 1.1.0.
 - Kugler: Seakeeping -- 10 boolean switches from supplied version 1.2.2.
This includes their diagnostic/optional-probe keys. Optional probe switches
only work in upstream builds that contain those probes; no probe is added.
See Docs/KUGLER-SWITCHES.md for the complete inventory and config keys.
Numeric tuning values and retired options are not added as boolean controls.

Restart once to install the updated companion. The officer PD approach brake
in our own menu and Proper Ballast's casing switch apply live when that mod
is loaded. All other upstream rows are explicitly marked (restart): they save
now for the next launch, without pretending to undo cached runtime effects.
The checkboxes show saved selections, including changes awaiting restart.

The menus read and update the corresponding upstream config files, preserving
other values and comments. Kugler's source, manifest and license files are
not edited or bundled. Each Kugler reset restores only that mod's boolean
switches, retaining numeric settings. Our companion master/reset stays separate.
There is no new invented master switch for Nose Down or Proper Ballast.
Seakeeping's own config master does not activate a mod disabled in the launcher.
Menus can edit existing configs while a mod is disabled; those changes require
loading that mod and restarting to take effect.

UPGRADING THE 1.0.18 CASING MENU
The former single "Kugler diving mods" entry becomes "Kugler: Proper Ballast".
Its casing selection is retained through ProperBallastConfig.txt. Our own
companion entry and its stored switches retain their identity.

CURRENT ISOLATION TEST
 - Nose Down Depth Companion > Officer PD approach brake: OFF.
 - Kugler: Proper Ballast > Casing flooding fix: ON.
 - Keep Seakeeping and Patch Profiler disabled in the mod launcher.
Reload the same starting save for each comparison: toggling the casing fix
cannot rewind water state already produced.

OPTIONAL COMMUNITY PATCH MENU
With Dusty's Community Patch 0.38.1:
   Other Mods > Nose Down Depth Companion
Main menu and pause menu expose the companion and all three Kugler entries.
The first ROW, "Enable depth companion", is the master switch. A regular row
lets its setter save to our own config immediately. Only this companion is
controlled. Other rows expose the officer PD brake, IX pin, VII handover, PD damping, tracing
and the three new options. Numeric controls are steppers in Dusty's panel;
wave strength uses the same supported numeric row as torque and PD depth.

Reset restores ALL our coded defaults, including advanced settings/hull lists,
and saves them. It keeps the master switch as it is and resets no other mod.
Your supplied surfaceBrakeHulls=VIIC41 and traceLogging=true survive an upgrade.
Explicit reset changes these to the factory VIIC41,VIIB and false.

CONFIG AND SAVE PRECEDENCE
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\NoseDownCompanionConfig.txt
Without Community Patch, edit this file while the game is closed.
Companion panel changes save immediately here and in Community Patch's settings.
Each Kugler entry saves its switches in that mod's config file.
Community Patch also stores those rows; after using a menu, change its
displayed settings there so an older stored menu value does not replace a
manual file edit. Rows marked restart show saved, potentially pending values.
Community Patch restores its saved rows when its panel first opens. After
using the panel, edit its displayed settings there; an older panel value can
overwrite a hand edit. Advanced hull lists stay in our config. Panel reset
updates both copies.

NEW: VII TORQUE ASSISTANCE
   viiTorqueBoost=false
   viiTorqueFactor=1.35     range 1.00-1.50; menu step 0.05
Ordinary VIIB/VIIC/VIIC41 dives only, with deck depth no deeper than 1 m and a
deeper order. Widens only Nose Down's final saturated torque cap, before the
existing trim handover scales it. Stock integral limit, gains, speed/angle
limit and planner stay unchanged. It does nothing in Hold, ascent, protected
modes, or after the trim has handed over. Reduced early level-off is a test goal.

NEW: DEEPER PD HOLD
   pdDepthBias=false
   pdBiasMeters=0.45        range 0-0.50 m; menu step 0.05 m
   pdBiasOfficerOnly=true   config only
   pdBiasSlew=0.1           config only; metres per game second
IIA/IID/IXA, ordinary 2-10 m orders, after arrival in Nose Down's Hold. Requires
an officer by default. Gradually adds bounded extra depth to the internal
target, composed with hold damping; the visible order stays unchanged.
The offset never accumulates each frame. New orders/protected modes clear it.
Release removes only a target still owned by this option, preserving later
writers. Start with 0.30 m. It ADDS to existing settled-depth error and reduces
periscope exposure. Detector immersion through every trough is not guaranteed.

NEW: REDUCE WAVE FORCE AT PD
   pdWaveForce=false
   pdWaveForceFactor=1.00   range 0-1; menu step 0.05
1.00 = unchanged. At full engagement, 0.50 halves the explicit wave-force term;
0 removes that term. Enable the switch AND choose a factor below 1.
Recognised II/VII/IX PLAYER boats, ordinary Hold, 2-10 m order, 2-12 m deck
depth. Full reduction within 1 m of the order; fades out at 2 m. Entry ramps
over one game second. Other ships, deeper orders, ascents, alarm/crash dives
and blows are excluded. Already calculated forces can last a physics interval.

The coefficient is scaled only during the player's wave calculation, then
restored on normal or exceptional exit. Seakeeping's depth-fade fields/cache,
wind conversion, sea-state/depth readout and wave shapes stay unchanged.
Buoyancy, drag, ballast and valve timing are not altered.
THIS DOES NOT FLATTEN TROUGHS. Buoyancy, other wave effects and existing motion
can still move the boat or expose a detector. Try 0.50 separately from bias.

SEAKEEPING CONFIG CHECK (NO FILES EDITED)
The supplied Seakeeping 1.2.2 config has only a header comment. Its parser
and patch source establish:
 - windSpeedUnitFixEnabled=True; windSpeedUnitFixRatio=0.5144:
   wind correction, kept as supplied.
 - forcedDepthKeepingOverrideEnabled=False:
   Seakeeping's removal of forced-depth correction is inactive. This alone
   does not prove the game is actively using forced depth keeping.
 - waveForceDepthFadeEnabled=True: Seakeeping retains physical depth fade.
 - depthGatedSeaStateEnabled=True: separate reading adjustment, kept as supplied.
 - waveAnimationCapEnabled=True: accepted legacy seaClockPhysicsRateEnabled alias.
 - waveAnimationCapMultiplier=4: retired and ignored in this source version.

EXISTING 1.0.16 BEHAVIOR RETAINED
 - Internal-target clamp on ordinary 2-320 m orders: VIIB/VIIC/VIIC41; IIA/IID
   with an officer seated (IID governed by iidOfficerClamp).
 - Existing officer ascent brake, shallow plane/trim stand-down, II afloat
   trim handover, VII deck handover, VIIC/41 damping and surfacing controls.
 - IX ordinary-dive approach brake; IXA/IXC/IXC40 target pin for orders deeper
   than ixPinMinOrder (10 m); IXA PD damping as configured.
 - Existing protection for alarm/crash dive and blow modes. Unknown hulls do
   not receive new controls. Missing Nose Down logs a warning and leaves us idle.
 - New options require a readable stock planner phase; unavailable torque or
   wave hooks fail closed. Advanced controls remain documented in the config.

SMALL IN-GAME CHECK
1. Leave all three new options off. Check one familiar dive/hold and menu entry.
2. Try VII torque assistance alone at 1.35 on one familiar VII dive.
3. Try bias alone at 0.30 m on an officer-manned IIA/IID/IXA PD hold. If desired,
   turn it off and compare wave factor 0.50 separately, under similar conditions.
4. Change the order / enter a protected mode to check release. Try a deeper
   order if testing wave reduction. Check master, persistence and reset when
   convenient; keep a config backup if testing reset of your custom values.
No full dive matrix or new long export is needed just to start.
Trace tags: [NoseDownCompanion.VIITorque], [NoseDownCompanion.PdBias],
[NoseDownCompanion.PdWaves].
See Docs/VALIDATION.txt for checks and limits.

CREDITS AND LICENSE
Nose Down Depth Companion by Landomatic
Source and history:
https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
Project license:
https://github.com/Landomatic/UboatMods/blob/main/mods/nose-down-depth-companion/LICENSE
Workshop profile:
https://steamcommunity.com/id/landomatic/myworkshopfiles/
Depth fixes and integration: Landomatic.
CC BY-NC 4.0: https://creativecommons.org/licenses/by-nc/4.0/ -- see LICENSE.
Redistribution/adaptation must retain visible credit as specified in LICENSE.

Required foundation: Nose Down by Kugler (https://github.com/Isamylov)
https://steamcommunity.com/sharedfiles/filedetails/?id=3729673507
It remains a separate required mod; none of its files are included or edited.
Runtime hooks cooperate with stock Nose Down.

Compatibility references supplied: Seakeeping 1.2.2 and Proper Ballast 1.1.0
by Kugler; Depth Watch 1.0 by EnterElysium; Dusty's Community Patch 0.38.1.
Workshop pages:
https://steamcommunity.com/sharedfiles/filedetails/?id=3780333567
https://steamcommunity.com/sharedfiles/filedetails/?id=3806934448
https://steamcommunity.com/sharedfiles/filedetails/?id=3789419523
https://steamcommunity.com/sharedfiles/filedetails/?id=3806002226
Their source and the game DLLs are not included.
