# Nose Down Depth Companion — archive snapshot

Snapshot identifier: `1.0.18`.
Role: Landomatic mod.

## Status and provenance

- This update exposes the existing officer PD brake and a separate Kugler diving mods menu for Proper Ballast's casing option. Both switches apply live and persist. Kugler's source is not edited or bundled.
- Full source compilation against the supplied game assemblies passed; 448 offline assertions passed, including menu parsing, actual brake release, casing persistence, separate resets and save-failure rollback.
- This build has not run inside UBOAT. The earlier 1.0.17 VIIB results remain partial: small dive-entry level-off, PD overshoot and sustained PD oscillation. The officer-brake-off isolation test is pending.
- Requires stock Nose Down by Kugler, Workshop 3729673507; its upstream files are not included.

## Known limitations

- Small dive-entry pitch level-off remains.
- PD overshoot and prolonged depth oscillation remain unresolved.
- Surfacing engine-power interruption is under investigation; no cause is confirmed.

## Integrity record

The corresponding `history/nose-down-depth-companion/1.0.18/update.json` records file SHA-256 hashes and the delivered package fingerprint. Earlier snapshots and their recorded validation remain in Git history.
The existing CC BY-NC 4.0 license and upstream notices are preserved. Source and project license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
