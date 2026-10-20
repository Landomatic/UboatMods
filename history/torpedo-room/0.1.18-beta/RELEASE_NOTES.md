# Torpedo Room 0.1.18 — Initial BETA

By **Landomatic** · Copyright (c) 2026 Landomatic · CC BY-NC 4.0.

Torpedo preparation controls in the torpedo rooms for solo first-person and station-restricted multiplayer play.

## Included

- Per-tube depth, speed, AZ/MZ and FAT/LUT settings, full-circle LUT heading, 0.25 m depth increments, native artwork and a large Apply button.
- Configurable compartment access and synchronized/read-only/hidden viewing-station reports.
- Independent host-controlled bidirectional TDC speed sync, ON by default. OFF honors independent tube/TDC preparations.
- Native TDC speed-cycle correction and live knots/km/h display support.
- No mod-added firing interlocks for mixed speeds or TDC mismatches; warnings only. Native readiness and MP permissions remain.
- Free, locked-on-load and optional timed dry-tube pistol policies; port-only removed.
- Torpedo type/model hover in overview and editor, with optional HTT maintenance information.

## Install

Download **TorpedoRoom_v0.1.18-test.zip**, extract `TorpedoRoomControls` into your local UBOAT mods folder, enable the mod in the launcher and restart. Fully replace an older installation. Press **T** at a tube station to edit, then **Apply**.

Optional Dusty's Community Patch supplies the settings menu. Config file: `TorpedoRoomConfig.txt` under UBOAT persistent data. No config migration.

The ZIP is byte-for-byte the previously delivered test build: manifest `0.1.18`, log identity `0.1.18-test`. All shared-boat players must update together (**TRC7**). The supplied MP integration targets Uboats: Coop Multiplayer 1.11.5 / protocol 54.

## Validation and limitations

Full supplied-game reference compilation and **700 offline assertions passed**. Live Unity/Harmony, MP and torpedo-flight validation of this revision remains pending. This is a **prerelease Beta**, not a validated stable release. Larger-stack loading/crash interactions remain a separate investigation.

Start with manual fore/aft firing while the opposite fire-control compartment is selected, Type II without a TDC operator, mixed/mismatched speeds, TDC forward/reverse cycling, both sync states and host/client confirmation. The source includes the full testing checklist.

SHA-256 (`TorpedoRoom_v0.1.18-test.zip`):
`5089f33c106c46b45a0a6b4be17f287859085eb3c82d29a264cd3f362f0fd8d7`

[Source and instructions](https://github.com/Landomatic/UboatMods/tree/main/mods/torpedo-room) · [License](https://github.com/Landomatic/UboatMods/blob/main/mods/torpedo-room/LICENSE) · [Credits](https://github.com/Landomatic/UboatMods/blob/main/mods/torpedo-room/NOTICE.txt)
