TORPEDO ROOM -- 0.1.20 BETA
By Landomatic. Copyright (c) 2026 Landomatic.
Source: https://github.com/Landomatic/UboatMods/tree/main/mods/torpedo-room
License: CC BY-NC 4.0. See LICENSE and NOTICE.txt.

INSTALL / UPDATE
Extract TorpedoRoomControls into your local UBOAT mods folder, enable Torpedo Room
in the launcher and fully restart. Replace the old Source folder completely.
All shared-boat players need this same version (TRC8 format is unchanged).
Mixed mod versions are not validated. Keep a separate save while testing Beta.
When updating an existing Workshop item, retain its assigned steamFileId and
preview from your publishing folder; the generic manifest cannot supply that ID.

USE
At a tube station press T (configurable). Choose a loaded torpedo, edit its
settings and press Apply. Physical controls still flood, drain and fire.
Depth uses 0.25 m increments from 1 to 14 m. Speed offers only the loaded
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
  Hidden leaves only the native Feuer button in the launch panel; native station
  navigation remains available. It uses the fire-control box/receiver's native
  tube/salvo selection and the flooded tubes. No selection means no shot; there
  is no first-ready fallback. Hover Feuer for a selection/readiness hint.
  Read-only and synchronized retain native selection, dispersion and firing.
- Bidirectional tube/TDC speed sync: independent host toggle, ON by default.
  OFF preserves independent settings and player errors. Interface presets do not
  change this option. A single TDC solution cannot represent multiple speeds.
- Pistol modes: unrestricted, or optional timed dry-tube preparation. The timer
  defaults to 30 game seconds. Port-only and locked-on-load rules are removed.
- FAT/LUT remains adjustable at sea without that preparation delay.

T5 DEFENSIVE VIEWING-INTERFACE FIRE
The game's viewing-panel blocker normally appears below 18 m boat depth.
An all-T5 selection (including the stock prototype) can use the native viewing
Feuer button down to 50 m boat depth. Select and flood the intended tubes first.
A mixed selected salvo cannot borrow this exception, including a selected dry
non-T5. Unknown/mod-renamed equipment keys retain the normal restriction.
This changes the viewing-interface blocker only. Running depth stays 1–14 m;
it does not become 50 m. Native readiness, confirmations and MP permissions
remain. Physical TDC and direct tube levers retain their native behavior.
No engine/guidance modification or historical safety claim is made.

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

Full supplied-game reference compilation and offline checks passed. No new live
Unity or MP test was run for .20. Verify Feuer-only layout at your UI scale,
fore/aft selector and salvo behavior, native confirmation, T5 launches through
50 m and ordinary/mixed selections. See the source archive's TESTING.txt.
The owner reported .19 looked good in solo play. Earlier .18 tests reported
successful TDC, UZO and direct-tube firing; remaining periscope and native TDC
speed-cycle cases are unconfirmed. Larger-stack compatibility is ongoing.
