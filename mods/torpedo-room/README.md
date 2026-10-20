# Torpedo Room — Beta 0.1.20

By **Landomatic** · Copyright (c) 2026 Landomatic · [CC BY-NC 4.0](LICENSE)

Local torpedo preparations for first-person and multiplayer station-restricted play.

[Download the clean 0.1.20 player ZIP](../../packages/torpedo-room/0.1.20-beta/TorpedoRoom_v0.1.20-beta.zip) · [Initial .18 release](https://github.com/Landomatic/UboatMods/releases/tag/torpedo-room-v0.1.18-beta) · [Testing checklist](TESTING.txt)

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

Solo play does not require multiplayer or Community Patch. The supplied multiplayer adapter targets Uboats: Coop Multiplayer **1.11.5 / protocol 54**. All shared-boat players must use this same build (**TRC8**).

## 0.1.20 changes and validation

Hidden Reports now masks the full native launch panel while retaining the original **Feuer** button and confirmation popup. Native station navigation remains available. Hidden Feuer resolves the native fire-control box/receiver selection, then fires the eligible flooded tubes through the normal launch path. There is no stale-checkbox or first-ready fallback; read-only/synchronized selection, dispersion and firing remain native. Physical TDC and direct tube levers retain their native paths.

Running-depth settings are **1–14 m in 0.25 m steps**. Separately, an all-T5 selected set (stock prototype and warmed variants included) can use the viewing Feuer interface through **50 m boat depth**. Ordinary, mixed and unrecognized selections keep the normal viewing restriction. This does not change engine/guidance behavior or make 50 m a running-depth setting.

The clean player ZIP contains 24 runtime source files and five player/manifest/license files. Tests, developer notes and full validation stay in the [source archive](../../packages/torpedo-room/0.1.20-beta/TorpedoRoom_v0.1.20-beta_source-archive.zip). No game assemblies/assets or private diagnostics are included.

Full supplied-game reference compilation and **802 offline assertions passed**. **No new live game/MP test was run.** Tests use actual mod classes with independent native/Unity fixtures; they do not prove real firing, rendering or network behavior. The owner reported .19 looks good in solo; remaining periscopes/native TDC speed clicks and broader stack compatibility remain pending. See [TESTING.txt](TESTING.txt) and [VALIDATION.txt](VALIDATION.txt).

The initial .18 release and prior packages remain unchanged. This is a test patch/source archive, with no new GitHub release/tag or Workshop upload. Keep the existing Workshop `steamFileId` and preview when updating the publishing folder; the generic manifest still uses `0` because the assigned ID was not supplied.

## Credits

Existing [license](LICENSE) and [upstream notices](NOTICE.txt) are retained. Multiplayer compatibility credit: **Pingu565, Kugler and Dusty**. Thanks to Dusty for the optional settings interface. UBOAT, Unity, Harmony and third-party assets remain their respective owners' work. No game assemblies/assets, upstream mod sources, raw private logs or saves are distributed here.

