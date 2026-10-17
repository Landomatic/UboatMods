# Nose Down Depth Companion — archive snapshot

Snapshot identifier: `1.0.19`.
Role: Landomatic mod and optional menu host for Kugler's separate diving mods.

## Status and provenance

- Adds three grouped menus exposing all 31 boolean switches in supplied Nose Down 2.1.4, Proper Ballast 1.1.0 and Seakeeping 1.2.2. Numeric settings and upstream source remain separate.
- Full game-reference compilation passed; 985 offline assertions passed, including actual upstream parser round-trips for every switch, host parsing/reset, config-only mode and save-failure rollback.
- This build has not run inside UBOAT. In-game menu display and native patch dispatch remain unverified.
- Requires stock Nose Down by Kugler, Workshop 3729673507; optional menus do not install or activate absent mods.

## Known limitations

- Most upstream switch changes save for restart, as labelled; existing cached effects are not claimed to be undone live. Officer brake and loaded Proper Ballast casing switches remain live.
- Earlier small dive-entry level-off, PD overshoot, prolonged oscillation and surfacing engine-power interruption remain unresolved; no cause is confirmed.
- Optional diagnostic probes require an upstream build that includes them.

## Integrity record

`history/nose-down-depth-companion/1.0.19/update.json` records file hashes and the delivered package fingerprint. Earlier versions and their validation remain in Git history.
The existing CC BY-NC 4.0 project license and upstream credits are preserved.
Source and project license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
