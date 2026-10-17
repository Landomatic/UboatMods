Hydrophone Surface Noise Fix (Beta)
Version 1.3.1-beta | UBOAT 2026.1 | Landomatic

Purpose
-------
Fixes exterior surface wash that can persist when returning to the submerged
hydrophone station from the map, orbit view, or officer selection.

The fix restores the audio listener's inside-boat flag only while the submerged
hydrophone station actually owns the listener. The game continues to calculate
normal depth-dependent ambience. The flag is released before the game's normal
station exit, and whenever the submerged/station ownership conditions cease.

It does not restart the hydrophone controller, alter detection ranges, contact
rings or listening arcs, operate crew controls, or mute individual audio sources.
It does not change what F1/F2/F3 or the officer-selection shortcuts do.

Beta status and testing
-----------------------
The repair in test 1.3 was reported clean on a Type VII/B with the starter GHG:
initial entry; map, section and orbit returns; captain FPP return; and officer
selection sequences from both section and orbit. Testing also covered ascent,
periscope depth, surface transitions, mid-dive and a return to 50 m. Normal seat
release was reported. Surface/periscope ambience made wash harder to distinguish.

Other hulls, KDB and GHG-Balkon have not yet been validated. The repair uses the
shared hydrophone controller and has no hull/equipment-name restriction; broader
compatibility remains a beta test item, not a promise.

The slight scratching/crackle during view changes was reproduced with this mod
disabled. This release does not target that separate symptom.

Changes from test 1.3
---------------------
The repair and its guards are unchanged. Diagnostic entry/exit logging and audio
sampling have been removed. Three station hooks remain; there is no ambient
update hook, reflection sampler or per-frame log output. The log contains one
startup message and an error report if intervention fails in a session.

Install or update
------------------
1. Fully quit UBOAT.
2. Disable/remove older Hydrophone Surface Noise Fix test versions.
3. Put the entire "Hydrophone Surface Noise Fix" folder under:
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\Mods
4. Enable "Hydrophone Surface Noise Fix (Beta)" in the UBOAT launcher.
5. Start the game. Enable only one version of this fix.

For the Workshop version, subscribe and enable it in the launcher instead of
installing the local copy. Do not enable both copies together.

No new career is required. The mod adds no saved game data. Quit and disable it
to remove the fix. Other mod combinations have not been comprehensively tested.
The Reflection permission is needed for Harmony's game-method patches.

Report a problem
-----------------
Include your UBOAT version, hull, hydrophone equipment, depth, enabled mods and
the exact sequence that causes the problem (including how you return to the
station). State whether you hear persistent wash, a short crackle, or silence.
Attach the output_log.txt from the same session after quitting the game.
The startup marker is: [HydrophoneSurfaceNoiseFix] Loaded beta 1.3.1.

Build note
-----------
UBOAT compiles the supplied Source folder with its own game/runtime dependencies.
The beta's syntax, referenced game members, patch targets and limited write scope
were checked locally. Full compilation and beta-package playback still require
UBOAT. Runtime evidence so far is from test 1.3 with the same repair logic.
