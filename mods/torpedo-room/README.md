# Torpedo Room — initial Beta 0.1.18

By **Landomatic** · Copyright (c) 2026 Landomatic · [CC BY-NC 4.0](LICENSE)

Local torpedo preparations for first-person and multiplayer station-restricted play.

[Download the initial Beta](https://github.com/Landomatic/UboatMods/releases/tag/torpedo-room-v0.1.18-beta) · [Archived install ZIP](../../packages/torpedo-room/0.1.18-beta/TorpedoRoom_v0.1.18-test.zip) · [Testing checklist](TESTING.txt)

## Features

- Per-tube depth, supported speed, game-selectable AZ/MZ, and FAT/LUT programming.
- Full-circle LUT heading and depth in 0.25 m steps from 0.5 to 14 m.
- Native torpedo artwork, game UI scaling, larger dials and a prominent Apply button.
- Current compartment only, read-only opposite tubes, or all tubes editable.
- Synchronized, read-only or hidden viewing-station reports.
- Independent host toggle for bidirectional tube/TDC speed synchronization; ON by default and unaffected by interface presets. OFF preserves independent inputs.
- Follow the game's knots/km/h preference in room and overridden viewport speed readouts.
- Type/model hover in the dock and editor, plus live maintenance benefit when Happy Torpedo Time is installed.
- Free, locked-on-load or timed dry-tube pistol policies; the optional dry-tube delay defaults to 30 game seconds. No port-only policy.
- Optional straight running for programmable torpedoes, without an added failure mechanic.

Physical flooding, draining and firing controls remain in use. The mod adds no mixed-speed or TDC-mismatch firing interlocks; those conditions only warn. Native readiness and multiplayer station/firing permissions still apply. Native targeting and manual gyro relays remain unchanged.

## Install and use

1. Download the install ZIP and extract its `TorpedoRoomControls` folder into UBOAT's local mods folder. Replace an older installation completely.
2. Enable **Torpedo Room** in the launcher and restart the game. UBOAT compiles the bundled source at startup.
3. At a tube station, press **T** (configurable), choose a torpedo, edit and click **Apply**.

Optional Dusty's Community Patch provides the settings menu. Standalone settings use `TorpedoRoomConfig.txt` in UBOAT's persistent-data folder (`AppData/LocalLow/Deep Water Studio/UBOAT` on Windows). No configuration migration is performed.

Solo play does not require multiplayer or Community Patch. The supplied multiplayer adapter targets Uboats: Coop Multiplayer **1.11.5 / protocol 54**. All shared-boat players must use this same build (**TRC7**).

## Beta status

The download is the **exact 0.1.18-test package** previously prepared for testing; its manifest stays `0.1.18` and log/build identity stays `0.1.18-test`. GitHub labels it as the initial Beta. No runtime source or package bytes were changed for publication.

Full compilation against the supplied game references and **700 offline assertions passed**. Live Unity/Harmony, torpedo-flight and multiplayer testing of this revision remains pending. Earlier reduced-stack MP feedback does not establish compatibility with larger stacks.

First tests: manually fire fore/aft tubes with the opposite fire-control compartment selected, repeat on Type II without a TDC operator, then test mixed speeds, TDC cycling, both speed-sync states and host/client exchange. See [TESTING.txt](TESTING.txt) and [VALIDATION.txt](VALIDATION.txt).

The `.txt` publication drafts and `UPDATE.json` inside the original package record its preparation-time state. This README and the GitHub release describe the subsequent Beta publication; they do not upgrade its gameplay validation status.

## Credits

Existing [license](LICENSE) and [upstream notices](NOTICE.txt) are retained. Multiplayer compatibility credit: **Pingu565, Kugler and Dusty**. Thanks to Dusty for the optional settings interface. UBOAT, Unity, Harmony and third-party assets remain their respective owners' work. No game assemblies/assets, upstream mod sources, raw private logs or saves are distributed here.
