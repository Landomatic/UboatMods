# Torpedo Room — Beta 0.1.21

By **Landomatic** · Copyright (c) 2026 Landomatic · [CC BY-NC 4.0](LICENSE)

Local torpedo preparations for first-person and multiplayer station-restricted play.

[Download the clean 0.1.21 player ZIP](../../packages/torpedo-room/0.1.21-beta/TorpedoRoom_v0.1.21-beta.zip) · [Initial .18 release](https://github.com/Landomatic/UboatMods/releases/tag/torpedo-room-v0.1.18-beta) · [Testing checklist](TESTING.txt)

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

## 0.1.21 changes and validation

A separate host opt-in, **MP: Show Feuer when scope tubes are restricted**, defaults OFF. With another synchronized shared-boat player aboard and MP's **TUBES PANEL AT THE SCOPES** OFF, permitted periscopes/UZO automatically show only the original Feuer button and native confirmation. The disabled launcher icon is not needed. This option is independent of Synchronized/Read-only/Hidden Reports, interface presets and speed sync; solo behavior is unchanged. Standalone key: `mpScopeFeuer=True`.

MP's icon disabling stays in effect. The adapter exempts only its UI opening gate and panel closing in two known MP UI cleanup callbacks. Native station entry, firing readiness/confirmation and MP dispatch stay in use; physical TDC/tube lever code is unchanged. Genuine native close is respected; re-enter the scope to resume. Automatically opened panels close when the condition ends; player-opened panels are retained. Feuer is raised above the bottom icons.

All peers need **.21/TRC9**. The adapter targets supplied MP **1.11.5**. There is no config migration, new dependency or custom fire packet. Prior running-depth settings remain 1–14 m in quarter-metre steps, with the separate all-T5 viewing exception through 50 m boat depth.

Full supplied-reference compilation and **864 offline assertions passed**. **No new live game/network test was run.** The owner reported .20 periscope firing successful in Read-only and Hidden Reports after fire-control selection/flooding; aft and TDC/bridge firing remain pending. See [TESTING.txt](TESTING.txt) and [VALIDATION.txt](VALIDATION.txt).

The clean player ZIP contains 25 source files and five player/manifest/license files. The [source archive](../../packages/torpedo-room/0.1.21-beta/TorpedoRoom_v0.1.21-beta_source-archive.zip) contains tests and development records. No game assemblies, downloaded upstream source or private diagnostics are bundled. Existing .18 release and prior packages remain unchanged; no release/tag/Workshop publication. Retain assigned Workshop ID/preview when updating the publishing folder.

## Credits

Existing [license](LICENSE) and [upstream notices](NOTICE.txt) are retained. Multiplayer compatibility credit: **Pingu565, Kugler and Dusty**. Thanks to Dusty for the optional settings interface. UBOAT, Unity, Harmony and third-party assets remain their respective owners' work. No game assemblies/assets, upstream mod sources, raw private logs or saves are distributed here.


Configuration-help revision: generated config and Dusty menu explain both host
settings, default OFF, solo behavior and .21/TRC9 requirement. Existing configs
gain comments on the next normal settings save; no automatic migration is added.
