# Source repository

Repository: https://github.com/Landomatic/UboatMods

| Repository path | Purpose |
| --- | --- |
| `mods/nimm-bitte-platz/` | Current source, manifest, preview, docs and existing license |
| `history/nimm-bitte-platz/<version>/update.json` | Changes, validation and file hashes |
| `packages/nimm-bitte-platz/<version>/` | Archived player ZIP |
| `tests/nimm-bitte-platz/` | Offline tests using explicit game/Unity/Harmony test doubles |

UBOAT compiles the four `.cs` files under the installed mod's `Source/` folder.
Tests and test doubles stay outside the player ZIP. They do not establish
compatibility with real game assemblies or prove in-game behavior.

Use `major.minor.patch` for the manifest, source version and ZIP name. New features
increment the minor number; fixes to existing features increment the patch number.
A separate build ID appears in the startup log. Logging verbosity is a setting,
not a release-name suffix.

Keep the Workshop ID stable. Source-history commits do not update Steam or any
installed copy. Workshop publishing is a separate step after the game checks in
PUBLISHING.md. Preserve the author/license/credits; keep game assemblies, private
logs, saves and personal configs out of the repository.
