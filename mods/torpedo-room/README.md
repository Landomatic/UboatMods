0.1.32 TEST BUILD: native tube inventory operator binding plus deferred handoff; live eligibility validation required. See Docs/ORDNANCE-DIAGNOSTICS.txt.

# Torpedo Room — Beta 0.1.32 Test

By **Landomatic** · Copyright (c) 2026 Landomatic · [CC BY-NC 4.0](LICENSE)

Local torpedo preparations for first-person and multiplayer station-restricted play.

[Download the .32 test player ZIP](../../packages/torpedo-room/0.1.32-beta-test/TorpedoRoom_v0.1.32-beta-test.zip) · [Initial .18 release](https://github.com/Landomatic/UboatMods/releases/tag/torpedo-room-v0.1.18-beta) · [Testing checklist](TESTING.txt)

## Features

- Per-tube depth, supported speed, game-selectable AZ/MZ, and FAT/LUT programming.
- Full-circle LUT heading and depth in 0.25 m steps from 1 to 14 m.
- Native torpedo artwork, game UI scaling, larger dials and a prominent Apply button.
- Current compartment only, read-only opposite tubes, or all tubes editable.
- Synchronized, read-only or hidden viewing-station reports.
- Independent host toggle for bidirectional tube/TDC speed synchronization; ON by default and unaffected by interface presets. OFF preserves independent inputs.
- Follow the game's knots/km/h preference in room and overridden viewport speed readouts.
- Type/model hover in the dock and editor, plus live maintenance benefit when Happy Torpedo Time is installed.
- Unrestricted or optional timed dry-tube pistol preparation, with a 30-game-second default. Port-only and locked-on-load rules are removed.
- Optional straight running for programmable torpedoes, without an added failure mechanic.

Physical flooding, draining and firing controls remain in use. The mod adds no mixed-speed or TDC-mismatch firing interlocks; those conditions only warn. Native readiness and multiplayer station/firing permissions still apply. Native targeting and manual gyro relays remain unchanged.

## Install and use

1. Download the install ZIP and extract its `TorpedoRoomControls` folder into UBOAT's local mods folder. Replace an older installation completely.
2. Enable **Torpedo Room** in the launcher and restart the game. UBOAT compiles the bundled source at startup.
3. At a tube station, press **T** (configurable), choose a torpedo, edit and click **Apply**.

Optional Dusty's Community Patch provides the settings menu. Standalone settings use `TorpedoRoomConfig.txt` in UBOAT's persistent-data folder (`AppData/LocalLow/Deep Water Studio/UBOAT` on Windows). No configuration migration is performed.

Solo play does not require multiplayer or Community Patch. The supplied multiplayer adapter targets Uboats: Coop Multiplayer **1.11.5 / protocol 54**. All shared-boat players must use this same build (**TRC9**).

## 0.1.25 changes and validation

Hovering depth-restricted Feuer now explains: **Too Deep to Fire! Ascend to minimum Tube depth: 18m** (50m for a T5-only readied selection) This works in Synchronized, Read-only, Hidden Reports and optional MP-only Feuer. The message updates during hover when depth/selection changes, without enabling firing. Hidden Reports retains its other selection/readiness hints.

Individual loaded-tube depth/FAT/LUT reports also append **Launch limit: 18 m (50 m for T5)** when boat depth exceeds that weapon's viewing limit (18 m normally; T5 through 50 m). This does not replace native/HTT hovers and remains hidden under Hidden Reports. Physical levers remain native.

Retained from .23:

A brass border identifies the active Prep card, without intercepting input. Multi-tube Prep editing is not added.

The whole-panel depth overlay is hidden while the mod controls viewing UI; depth gates only Feuer. At 45 m, uncheck/recheck a T5 without draining or moving the fire-control selector. A flooded mixed selection still cannot fire at depth, but its checkboxes remain accessible. Synchronized/Read-only preserve native checkbox selection; Hidden Reports uses native fire-control box mapping.

Hidden/MP Feuer is 1.5× its authored size and click area, follows the torpedo opener on the right with scaled spacing, and moves above neighbouring controls when crowded. Missing icon uses a higher fallback. The original button remains outside the hidden/disabled icon hierarchy. Live rendering across scales and resolutions is pending.

Retained from .22:

The T5 viewing-depth exception now checks only marked, flooded tubes containing weapons, matching native launch-candidate counting. A dry/empty checked tube cannot keep a T5 blocked. Flooding a selected non-T5 blocks a deep mixed selection; draining it restores eligibility without deselecting it. Confirmation rechecks live state. Native marks, flooding controls, physical levers and other readiness checks remain unchanged.

Retained from .21:

A separate host opt-in, **MP: Show Feuer when scope tubes are restricted**, defaults OFF. With another synchronized shared-boat player aboard and MP's **TUBES PANEL AT THE SCOPES** OFF, permitted periscopes/UZO automatically show only the original Feuer button and native confirmation. The disabled launcher icon is not needed. This option is independent of Synchronized/Read-only/Hidden Reports, interface presets and speed sync; solo behavior is unchanged. Standalone key: `mpScopeFeuer=True`.

MP's icon disabling stays in effect. The adapter exempts only its UI opening gate and panel closing in two known MP UI cleanup callbacks. Native station entry, firing readiness/confirmation and MP dispatch stay in use; physical TDC/tube lever code is unchanged. Genuine native close is respected; re-enter the scope to resume. Automatically opened panels close when the condition ends; player-opened panels are retained. Feuer is raised above the bottom icons.

All peers need **.24/TRC9**. The adapter targets supplied MP **1.11.5**. There is no config migration, new dependency or custom fire packet. Prior running-depth settings remain 1–14 m in quarter-metre steps, with the separate all-T5 viewing exception through 50 m boat depth.

Full supplied-reference compilation and **316 targeted offline assertions passed** for .24: 85 stock/depth, 99 viewing fire/layout, 50 MP display, 35 UI affordance and 32 Feuer tooltip and 15 individual tube report checks. Other prior suites were not re-run. **No new live game/network test was run.** The owner reported .20 periscope firing successful in Read-only and Hidden Reports after fire-control selection/flooding; aft and TDC/bridge firing remain pending. See [TESTING.txt](TESTING.txt) and [VALIDATION.txt](VALIDATION.txt).

The clean player ZIP contains 28 source files and five player/manifest/license files. The [source archive](../../packages/torpedo-room/0.1.26-beta/TorpedoRoom_v0.1.26-beta_source-archive.zip) contains tests and development records. No game assemblies, downloaded upstream source or private diagnostics are bundled. Existing .18 release and prior packages remain unchanged; no release/tag/Workshop publication. Retain assigned Workshop ID/preview when updating the publishing folder.

## Credits

Existing [license](LICENSE) and [upstream notices](NOTICE.txt) are retained. Multiplayer compatibility credit: **Pingu565, Kugler and Dusty**. Thanks to Dusty for the optional settings interface. UBOAT, Unity, Harmony and third-party assets remain their respective owners' work. No game assemblies/assets, upstream mod sources, raw private logs or saves are distributed here.


Configuration-help revision: generated config and Dusty menu explain both host
settings, default OFF, solo behavior and .24/TRC9 requirement. Existing configs
gain comments on the next normal settings save; no automatic migration is added.


The .22 owner logs record a Tube V launch audit verified=True but also the whole-panel lockout and half-full flood graphic. This .24 build retains the fix for the depth overlay lockout; the flood graphic issue remains under investigation. No live .24 result is claimed.

Uninstall cleanup: while your game is loaded, use Remove preparation records before uninstalling in Dusty's Torpedo Room settings. A dismissible result reports completion, record count or failure. OFF is the action's resting state, not proof of completion. After success, save to a NEW slot, exit, disable the mod in the launcher and restart to test that save. Keep the original save; never disable the launcher mod before cleanup and saving.

CURRENT .29 TEST: ORDNANCE
Manage Ordnance opens the native inventory bound to the character currently
selected by the native station view. This preserves native Load / Load and
maintain choices. The game still determines job eligibility and dispatch.
On a native loading or maintenance order, only the manual camera is released:
SetFPPController uses stopCurrentAction=false before the original order runs.
The mod neither starts nor cancels the crew action in its order hook. Native
task animations and progress remain the game's responsibility. Inventory
closes after dispatch; maintenance closes in LateUpdate to avoid clearing
widgets in the middle of StorageUI.Update. Escape without an order still
returns to our initial station display. Native/MP eligibility remains intact.
The log distinguishes Load torpedo, Load and maintain, and Maintain torpedo.

FEUER HOVER
Too Deep to Fire! Ascend to minimum Tube depth: 18m
Use 50m for T5-only selected flooded launch candidates. Mixed ordinary/T5 uses
18m. Applies to Synchronized, Read-only and Hidden reports. Other readiness
hints, per-tube printed labels and actual launch restrictions are unchanged.

Later viewport single-tube/checked-tube dial editing remains queued. Exit crash,
physical aft-door/flood mismatch and Feuer clipping remain unresolved.
