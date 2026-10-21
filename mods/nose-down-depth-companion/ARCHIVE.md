# Nose Down Depth Companion — diagnostic archive

Snapshot: `1.0.23`. Companion by Landomatic to Nose Down by Kugler
(https://github.com/Isamylov), Workshop 3729673507.

Adds a config-only lock against all Companion corrections, optional player-only
Nose Down 2.1.5 casing bypass, and casing/buoyancy-reference telemetry.
The bypass requires the diagnostic lock. Both defaults remain off. Hook dispatch
and bypass application have separate runtime counters. Includes A/B test templates.
Removes queued Saved prefixes from hosted Kugler rows without changing keys.

Compilation passed against 184 supplied references. Targeted offline fixtures
passed; native Harmony dispatch and new gameplay have not run. This is not a
proven fix for entry leveling, PD cycling or propulsion slowdown.
See Docs/VALIDATION-1.0.23.txt and Docs/CASING-ISOLATION-TEST.md.

Source/history: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
Existing LICENSE and credits retained. No upstream source, game assemblies,
private logs or saves included. Earlier history records remain unchanged.

