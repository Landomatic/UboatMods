TORPEDO ROOM -- 0.1.22 BETA
By Landomatic. Copyright (c) 2026 Landomatic.
Source: https://github.com/Landomatic/UboatMods/tree/main/mods/torpedo-room
License: CC BY-NC 4.0. See LICENSE and NOTICE.txt.

INSTALL / UPDATE
Extract TorpedoRoomControls into your local UBOAT mods folder, enable Torpedo Room
in the launcher and fully restart. Replace the old Source folder completely.
All shared-boat players need this same version (TRC9; older companions cannot exchange settings).
Do not mix .20 and .21 companions. Keep a separate save while testing Beta.
Manifest targets the existing Torpedo Room Workshop item 3823181610:
https://steamcommunity.com/sharedfiles/filedetails/?id=3823181610
Keep that steamFileId and retain the preview from your publishing folder.

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

MP-ONLY FEUER OPTION (DEFAULT OFF)
In the host's Torpedo Room settings, enable:
  MP: Show Feuer when scope tubes are restricted
Standalone config: mpScopeFeuer=True
In MP's host settings, keep TUBES PANEL AT THE SCOPES set OFF (scope_tubes=false).
With another synchronized shared-boat player aboard, Feuer alone automatically
appears at permitted periscopes/UZO once native aim context is available. No
need to click the disabled torpedo icon. This applies independently of our
Synchronized/Read-only/Hidden Reports choice. Solo behavior is unchanged.

This host opt-in makes an exception to MP's panel-only restriction, not to
station access or actual firing readiness. Both native torpedo icons remain
restricted by MP; tube reports, selection, dispersion and preparations stay
hidden. Firing uses native fire-control selection and the normal native button,
confirmation and MP dispatch. Physical TDC and direct tube levers stay native.
All players need .22/TRC9. The adapter targets supplied MP 1.11.5 only.

OFF, no other shared-boat player, or MP's scope panel allowed: normal display
rules apply. Panels automatically opened by this option close when it ends;
player-opened panels are retained. A genuine native close (escape/dialog/lost
native aim context) is respected; re-enter the scope to resume. Presets do not
change this toggle. No config migration, new dependency or fire packet.
Feuer is raised above the bottom icon strip in both solo Hidden Reports and MP.

T5 DEFENSIVE VIEWING-INTERFACE FIRE
The game's viewing-panel blocker normally appears below 18 m boat depth.
An all-T5 flooded selection (including the stock prototype) can use the native viewing
Feuer button down to 50 m boat depth. Select and flood the intended tubes first.
A flooded mixed selection cannot borrow this exception. Dry/empty checked tubes
do not veto it: draining a checked non-T5 restores T5 eligibility; reflooding it
blocks the mixed selection again. Native marks are retained. Unknown/mod-renamed
equipment keys retain the normal restriction.
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

Full supplied-game reference compilation and 217 targeted offline checks passed
for .22 (85 stock/depth, 82 viewing fire/layout, 50 MP display). Other prior
suites were not re-run. No live game or network test was run for .22. The owner reported successful .20
periscope firing in Read-only and Hidden Reports after fire-control selection
and compartment flooding. Aft and TDC/bridge firing remain pending. Test the new
host option with both host/client, join/leave, all report modes, target lock
changes and station switches. Larger-stack compatibility remains ongoing.

Configuration-help revision: generated config and Dusty menu explain both host
settings, default OFF, solo behavior and .22/TRC9 requirement. Existing configs
gain comments on the next normal settings save; no automatic migration is added.

