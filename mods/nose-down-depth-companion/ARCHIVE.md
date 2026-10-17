# Nose Down Depth Companion — archive snapshot

Snapshot identifier: `1.0.20`.
Role: Landomatic's companion to stock Nose Down by Kugler, with optional settings menus for three separate Kugler mods.

## Change and validation

- Adds three default-OFF experimental controls: VII entry-pitch assistance, nonhistorical II/VII precise PD arrival (2–10 m), and surfaced planes-station availability for pre-dive assignment.
- All three hosted Kugler menus distinguish detected session patches from saved config; Seakeeping's runtime master is shown independently. Existing provider names and feature keys remain stable.
- Includes the requested GitHub source/history references inside LICENSE, preserving the prior license text and credits.
- Full compilation against 184 supplied game references passed; 1125 offline assertions passed. Fixtures exercise control bounds/gates and the real supplied upstream config parsers and Community Patch 0.38.1 row/reset contract.
- Gameplay status: **not run**. Native patch ordering, UI/pathfinding and actual improvement remain unverified. No CI results are claimed.

## Limits

- Entry assistance covers ordinary VIIB/VIIC/VIIC41 entry and excludes alarm/crash dives and blows. It is an attempt to remedy leveling, not a demonstrated fix.
- Precise arrival needs the existing II/VII shallow plane/trim handover; Type II requires a seated officer. It replaces other arrival/hold offsets and deeper-PD bias on those hulls. Deeper orders and Type IX retain existing behavior.
- Pre-dive station availability is shared with crew slots and assigns nobody automatically; other station restrictions remain. Turning it OFF restores the normal surface restriction.
- Most upstream switch changes still require a restart. Status is based on patch registration and readable current config, not certification of every mod feature. Status-only rows ignore writes because the host has no read-only row kind.
- Entry leveling, prolonged PD oscillation and surfacing propulsion interruption remain open. Section 106's intentional test-dive overshoot changes how initial overshoot is interpreted; this build does not recreate that procedure.

## Integrity

`history/nose-down-depth-companion/1.0.20/update.json` records project file hashes and the delivered ZIP fingerprint. Earlier records remain unchanged. Private telemetry, saves, game assemblies, user configs and downloaded reference bundles are excluded.

Source and project license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
Companion by Landomatic; upstream Nose Down by Kugler (https://github.com/Isamylov), Workshop 3729673507. Upstream files remain separate.
