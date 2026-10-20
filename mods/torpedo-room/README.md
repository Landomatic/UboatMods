# Torpedo Room — Beta 0.1.19

By **Landomatic** · Copyright (c) 2026 Landomatic · [CC BY-NC 4.0](LICENSE)

Local torpedo preparations for first-person and multiplayer station-restricted play.

[Download the clean 0.1.19 player ZIP](../../packages/torpedo-room/0.1.19-beta/TorpedoRoom_v0.1.19-beta.zip) · [Initial .18 release](https://github.com/Landomatic/UboatMods/releases/tag/torpedo-room-v0.1.18-beta) · [Testing checklist](TESTING.txt)

## Features

- Per-tube depth, supported speed, game-selectable AZ/MZ, and FAT/LUT programming.
- Full-circle LUT heading and depth in 0.25 m steps from 0.5 to 14 m.
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

Solo play does not require multiplayer or Community Patch. The supplied multiplayer adapter targets Uboats: Coop Multiplayer **1.11.5 / protocol 54**. All shared-boat players must use this same build (**TRC8**).

## 0.1.19 changes and validation

Remove the loaded-pistol lock; rebind the editor when a tube's weapon/preparation identity changes, preserve same-weapon drafts through temporary busy states, refresh native icons on state changes and clear empty-tube artwork. Busy/read-only controls now show a specific reason. Diagnostic state entries log transitions only. No native task, tube state or firing permission is forced.

The player ZIP contains 22 runtime source files and five player/manifest/license files. Tests, development markdown, publication drafts and historical notes stay in the [separate source archive](../../packages/torpedo-room/0.1.19-beta/TorpedoRoom_v0.1.19-beta_source-archive.zip). The player and source ZIPs are byte-for-byte the delivered .19 packages.

Full compilation against supplied game references and **723 offline assertions passed**. Live verification of this revision remains pending. The earlier .18 user test reported successful TDC, UZO and direct-tube firing; other periscopes and TDC speed-button cycles remain pending. A cancelled-maintenance explanation for transient read-only/artwork behavior was not established by the log. See [TESTING.txt](TESTING.txt) and [VALIDATION.txt](VALIDATION.txt).

The initial .18 GitHub release and its assets remain unchanged. This .19 update archives source and packages; it does not create a new release/tag or upload to Workshop. When updating the existing Workshop item, retain its assigned `steamFileId` and preview; its ID has not been supplied, so the generic manifest keeps `0`.

## Credits

Existing [license](LICENSE) and [upstream notices](NOTICE.txt) are retained. Multiplayer compatibility credit: **Pingu565, Kugler and Dusty**. Thanks to Dusty for the optional settings interface. UBOAT, Unity, Harmony and third-party assets remain their respective owners' work. No game assemblies/assets, upstream mod sources, raw private logs or saves are distributed here.
