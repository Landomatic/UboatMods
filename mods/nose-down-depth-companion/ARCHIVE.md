# Nose Down Depth Companion — diagnostic archive

Snapshot: `1.0.21`. Companion by Landomatic to stock Nose Down by Kugler
(https://github.com/Isamylov), Workshop 3729673507.

Adds read-only entry observations controlled by existing trace logging:
centre of mass and game/Unity angular drag at update boundaries, direct hull Y,
pitch/rate, engine output, forced-depth flags and observed controller output.
It can observe with the companion master off and Nose Down absent. Existing
physics formulas, saved configs, defaults, menu keys and license are retained.

Full compilation against 184 supplied assemblies passed; 1147 offline
assertions passed. Gameplay status is **not run** for this build. No CI result
is claimed. Controller output is not net hydrodynamic torque, and observation
boundaries include patches. See `Docs/ENTRY-DIAGNOSTICS.md` and
`Docs/VALIDATION.txt` for the baseline plan and limitations.

Earlier 1.0.20 VIIB gameplay remains partial: precise arrival reduced measured
PD hull movement in that setup, but entry leveling, a small depth bias and the
separate surfacing interruption remain open. A fresh vanilla comparison has
not yet been supplied. No causal conclusion or universal fix is claimed.

`history/nose-down-depth-companion/1.0.21/update.json` records the delivered
package and source hashes. Prior records remain unchanged. Private logs,
saves, assemblies, reference mod bundles and personal configs are excluded.

Source and existing license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
