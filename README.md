# UboatMods

Source, version history, and test records for Landomatic's UBOAT mods.

## Repository layout

- `mods/<mod-slug>/`: reviewed source, manifests, distributable defaults, documentation, and existing notices.
- `history/<mod-slug>/<version>/update.json`: changes, validation results, known issues, and file hashes.
- `packages/<mod-slug>/<version>/`: mod packages explicitly selected for archiving.

These folders are added as updates are archived.

## Update history

Archive requested updates on `updates/<mod-slug>/<version>` branches unless a later repository workflow specifies otherwise. Repository setup uses `updates/repository/setup`.

Merge accepted changes into `main` to keep the collected mods together. A fix involving several mods can share a descriptive working branch. Use folders to separate mods within the repository.

Record compilation, offline checks, and in-game testing separately. Mark experimental builds clearly and retain unresolved issues in their validation records. Preserve Workshop identity fields such as `steamFileId` when updating an existing mod.

## Credits and licenses

Preserve each mod's existing author, copyright, license, and contributor notices. Landomatic remains the established author/publisher of his own mods. Nose Down Depth Companion is a companion to Kugler's Nose Down; retain Kugler's upstream credit with the related project.

Licensing is handled per mod. This setup adds no repository-wide software license.

## Archive scope

Keep game assemblies, downloaded reference repositories, credentials, browser traces, raw logs, saves, and personal test configurations outside the archive. Include reviewed diagnostic data only when expressly requested.
