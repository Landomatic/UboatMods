Nimm Bitte Platz: Hydrophone Fix -- 1.4.2
Author: Landomatic | Build: 20261024.3 | Target: UBOAT 2026.1

Corrects exterior surface wash that can persist in the submerged hydrophone
view after returning from another camera view. The existing station, audio
ownership and submerged guards remain in place. Only the audio listener's
inside-boat flag is written; the game calculates normal depth-dependent sound.

This update adds optional Dusty menu controls, keeps lifecycle diagnostics,
and removes an unused diagnostics Update callback. It does not change detection
ranges, contacts, crew controls, camera position or FOV. It does not target the
separate multiplayer FPP vibration or brief view-change crackle.

Install or update
-----------------
Fully exit UBOAT and back up your existing mod folder outside active mod paths.
Replace the entire local folder with this ZIP's nimm-bitte-platz-hydrophone-fix
folder under:
%USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\Mods

Enable one copy in the launcher and grant Reflection and IO permissions. Disable
the Workshop copy while testing the local copy. The existing Workshop identity
3810668782 is retained. The mod adds no saved-game data.

Dusty's menu (optional)
----------------------
Open Dusty's settings panel, select Other Mods, then Nimm Bitte Platz.
  Hydrophone correction: on/off; turning it off releases an owned audio override.
  Logging level: Info (normal), Debug, or Trace.
  Trace interval (seconds): 0.25-60; default 2.
  Reset to defaults: correction on, Info, 2 seconds.

The launcher controls whether the mod loads. If it is disabled there, Dusty
cannot load or activate it. When loaded, the correction toggle defaults to On
and its saved preference is respected.

Changes apply live and save when changed. The correction toggle is the setting
row; Dusty's provider heading may say "always on" because the settings provider
itself stays loaded. Dusty is optional: the same settings work through the config.

Existing DIAG settings
----------------------
Your NimmBittePlatzDiagnostics.txt remains in the normal UBOAT AppData folder.
Existing settings are preserved, including an explicit Debug or Trace choice.
Info is the default for a fresh config, not a forced reset of an old one. For
normal play, choose Info or Reset to defaults in Dusty's menu.

Without Dusty, edit the config and restart UBOAT. There is no config-file polling.
If Dusty already has stored values for this mod, opening its panel applies those
values; use the menu to change them. See Docs/DIAGNOSTICS.md for full details.

Versioning and verification
--------------------------
Versions use major.minor.patch. 1.4.2 restores the correction toggle after the
1.4.1 revision. The earlier development build was 1.3.2-beta-DIAG1, following
Workshop version 1.3.1. Diagnostics are a setting, not a DIAG
version suffix. The independent build ID identifies the exact source build.
Look for [HydrophoneSurfaceNoiseFix][START] version=1.4.2 build=20261024.3 and a
subsequent [READY]. START by itself does not prove successful patch installation.

The original repair was reported clean on VIIB with starter GHG. This package
still needs compilation by UBOAT and in-game menu/audio confirmation before
Workshop upload. Other hulls, equipment and multiplayer combinations have not
been comprehensively validated. See Validation.json and Docs/PUBLISHING.md.
HISTORY/README-1.3.1-beta.txt preserves the earlier testing notes.

Credits and source
------------------
Nimm Bitte Platz: Hydrophone Fix by Landomatic.
Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3810668782
Author: https://steamcommunity.com/id/landomatic/myworkshopfiles/
Source: https://github.com/Landomatic/UboatMods/tree/main/mods/nimm-bitte-platz
History: https://github.com/Landomatic/UboatMods/tree/main/history/nimm-bitte-platz
The existing LICENSE is preserved: CC BY-NC 4.0 and its visible-credit notice.
License: https://creativecommons.org/licenses/by-nc/4.0/
Dusty's Community Patch provides the optional external-mod settings menu.
Dusty's code and game assemblies are not included in this package.
