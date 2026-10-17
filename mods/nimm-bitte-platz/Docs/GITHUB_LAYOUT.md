# Keeping this mod's source on GitHub

Use one repository per mod to start. Put the contents of the mod folder at the
repository root, rather than an extra nested copy of that folder.

| Path | Purpose |
|---|---|
| `Manifest.json` | Identity, version, Workshop ID, required permissions |
| `Source/` | C# source; UBOAT compiles these files |
| `Data Sheets/` | Keep this folder's exact spelling; game data overrides when present |
| `Preview.png` or `Preview.jpg` | Existing Workshop preview |
| `LICENSE` | License and required visible credit |
| `README.txt` | Purpose, install, credits and testing limits |
| `Docs/` | Diagnostics, troubleshooting and repository notes |
| `Validation.json` | Static checks for this build |

Keep the Workshop ID and assembly identity stable across updates. Increase the
manifest version and source build ID together. The build ID in output_log lets
us identify the code that actually ran.

Create a repository, add these files, review the diff and commit the change.
Use a Git tag matching a tested version to identify a release. A release ZIP
must retain the mod's expected folder layout. Hosting source on GitHub does
not automatically update the Steam Workshop item or an installed copy.

The supplied .gitignore excludes temporary output, test logs and archives.
Keep original authors' credits and license files when carrying third-party work.
This package has not created or changed a remote repository.
