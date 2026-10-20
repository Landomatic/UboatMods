TORPEDO ROOM -- 0.1.19 BETA
By Landomatic. Copyright (c) 2026 Landomatic.
Source: https://github.com/Landomatic/UboatMods/tree/main/mods/torpedo-room
License: CC BY-NC 4.0. See LICENSE and NOTICE.txt.

INSTALL / UPDATE
Extract TorpedoRoomControls into your local UBOAT mods folder, enable Torpedo Room
in the launcher and fully restart. Replace the old Source folder completely.
All shared-boat players need this same version (TRC8); older companions cannot
exchange settings with it. Keep a separate save while testing this Beta.
When updating an existing Workshop item, retain its assigned steamFileId and
preview from your publishing folder; the generic manifest cannot supply that ID.

USE
At a tube station press T (configurable). Choose a loaded torpedo, edit its
settings and press Apply. Physical controls still flood, drain and fire.
Depth uses 0.25 m increments from 0.5 to 14 m. Speed offers only the loaded
weapon's supported presets and follows the game knots/km/h preference.
AZ/MZ, FAT/LUT run/direction and LUT heading use the weapon's available controls.
Straight mode disables a programmable torpedo's pattern; it adds no failure rate.
Hover a tube icon for model/type; optional HTT adds maintenance benefit information.

SETTINGS
Optional Dusty's Community Patch supplies the menu. Standalone configuration is
TorpedoRoomConfig.txt in UBOAT persistent data (on Windows, under
AppData/LocalLow/Deep Water Studio/UBOAT). No config migration is performed.
- Room access: current compartment only, opposite tubes read-only, all editable.
- Viewing-station reports: synchronized controls, read-only or hidden.
- Bidirectional tube/TDC speed sync: independent host toggle, ON by default.
  OFF preserves independent settings and player errors. Interface presets do not
  change this option. A single TDC solution cannot represent multiple speeds.
- Pistol modes: unrestricted, or optional timed dry-tube preparation. The timer
  defaults to 30 game seconds. Port-only and locked-on-load rules are removed.
- FAT/LUT remains adjustable at sea without that preparation delay.

TUBE STATE RECOVERY
The editor follows the current weapon after loading, unloading or replacement;
it discards an old weapon's draft and clears empty-tube artwork. Same-weapon
unapplied drafts survive temporary busy states. BUSY displays the native tube
state; controls return when the game reports a permitted stable state. No native
loading/maintenance animation, task, readiness or firing permission is forced.
Diagnostic timing enabled: TUBE STATE entries report changes only, not every frame.

COMPATIBILITY / BETA STATUS
Solo play does not require multiplayer or Community Patch. The supplied MP adapter
targets Uboats: Coop Multiplayer 1.11.5 / protocol 54. No new role or firing rules:
mixed speeds and TDC mismatches only warn. Native targeting/gyro relays remain.
Uses live game art and supports optional Torpedo Type Icons and HTT integration.

Full supplied-game reference compilation and offline checks passed. This revision
still needs live verification of state recovery and the remaining periscope/TDC
speed-cycle tests. Earlier .18 tests reported successful TDC, UZO and direct-tube
firing. Possible cancelled-maintenance visual/state trouble remains unconfirmed;
this patch addresses identified stale-draft/artwork paths without claiming a
reproduced fix for every cancellation case. Larger-stack compatibility is ongoing.
