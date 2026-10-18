# Nose Down Depth Companion — compatibility archive

Snapshot: `1.0.22`. Companion by Landomatic to Nose Down by Kugler
(https://github.com/Isamylov), Workshop 3729673507.

Updates menus for supplied Nose Down 2.1.5 and Proper Ballast 1.2.0: trim-water,
Fast dive and three flood-factor sliders. Supported older runtime config
contracts retain their menus. Shared provider names/keys and user settings
persist; boolean reset preserves numbers. New settings save for restart.
Adds a read-only upstream bow-lift counter-moment to entry diagnostics.
Existing companion control formulas, defaults, LICENSE and credits are retained.

Full companion and supplied upstream source compilation passed. Separate offline
runs passed 1249 current-contract and 1152 legacy-contract assertions. Native
Harmony smoke execution in the host .NET 8 process exited 139 and did not
validate dispatch. Gameplay: **not run** for 1.0.22. No CI result is claimed.
See `Docs/VALIDATION.txt` for the distinction and `Docs/KUGLER-SWITCHES.md` for
the new upstream-version baseline before adding extra pitch assistance.

The new upstream bow-lift/controller and skill-based flooding changes mean older
telemetry is not an identical baseline. Entry leveling, PD bias and surfacing
interruption remain unresolved. Source-history acceptance is not a tested release.
Always Load Sea Floor has no config switches; Seakeeping remains based on 1.2.2.

`history/nose-down-depth-companion/1.0.22/update.json` records source/package
hashes. Earlier records are unchanged. Private telemetry, saves, assemblies,
upstream source bundles and user configs are excluded.

Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
