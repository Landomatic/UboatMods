# UboatMods

Source, version history, and test records for Landomatic's UBOAT mods.

## Torpedo Room

[Initial 0.1.18 Beta release](https://github.com/Landomatic/UboatMods/releases/tag/torpedo-room-v0.1.18-beta) · [Source and instructions](mods/torpedo-room) · [Install ZIP](packages/torpedo-room/0.1.18-beta/TorpedoRoom_v0.1.18-test.zip)

Latest source/package archive: [0.1.23 Beta test player ZIP](packages/torpedo-room/0.1.23-beta/TorpedoRoom_v0.1.23-beta.zip). Adds active Prep outline; depth gates Feuer without covering tube selection; larger Feuer follows the opener. Full supplied-reference compilation and 269 targeted offline checks passed; no live .23 rendering/firing/network validation. Native half-full flood indicator remains under investigation. All multiplayer peers should use .23/TRC9. Workshop item 3823181610 preserved; initial .18 release retained separately.

## Repository layout

- `mods/<mod-slug>/`: reviewed source, manifests, distributable defaults, documentation, and existing notices.
- `history/<mod-slug>/<version>/update.json`: changes, validation results, known issues, and file hashes.
- `packages/<mod-slug>/<version>/`: mod packages explicitly selected for archiving.

- `contributions/`: compatibility work and source contributions to upstream mods, with their original credits.

Original delivered package hashes are recorded in history; original ZIP files are not included in this initial source archive.

## Update history

Archive requested updates on `updates/<mod-slug>/<version>` branches unless a later repository workflow specifies otherwise. Repository setup uses `updates/repository/setup`.

Merge accepted changes into `main` to keep the collected mods together. A fix involving several mods can share a descriptive working branch. Use folders to separate mods within the repository.

Record compilation, offline checks, and in-game testing separately. Mark experimental builds clearly and retain unresolved issues in their validation records. Preserve Workshop identity fields such as `steamFileId` when updating an existing mod.

## Credits and licenses

Preserve each mod's existing author, copyright, license, and contributor notices. Landomatic remains the established author/publisher of his own mods. Nose Down Depth Companion is a companion to Kugler's Nose Down; retain Kugler's upstream credit with the related project.

Licensing is handled per mod. This setup adds no repository-wide software license.

## Archive scope

Keep game assemblies, downloaded reference repositories, credentials, browser traces, raw logs, saves, and personal test configurations outside the archive. Include reviewed diagnostic data only when expressly requested.

## Initial archived snapshots

These are recovered source snapshots, including experimental and diagnostic builds. Test status and remaining issues are recorded per project.

| Project | Snapshot | Role |
| --- | --- | --- |
| [Nose Down Depth Companion](mods/nose-down-depth-companion) | `1.0.17` | Landomatic mod |
| [Nimm Bitte Platz: Hydrophone Fix](mods/nimm-bitte-platz) | `1.3.2-beta-DIAG1` | Landomatic mod |
| [Happy Torpedo Time!](mods/happy-torpedo-time) | `1.1.1-DIAG1` | Landomatic integration |
| [UZO Null Data Guard](mods/uzo-null-data-guard) | `0.2.0-test` | Landomatic mod |
| [Engine Transition Glide](mods/engine-transition-glide) | `0.2.0-draft` | Landomatic mod |
| [Landomatic Patch Profiler](mods/landomatic-patch-profiler) | `1.0.0` | Landomatic diagnostic tool |
| [Wolfpack Atlantik patrol-overlay contribution](contributions/wolfpack-atlantik/universal-patrol-overlay-guard) | `universal-patrol-overlay-guard-candidate` | Contribution to Pingu's upstream mod |
| [Type IXC40 280m - UBE Compatibility](contributions/type-ixc40-280m-ube-compatibility) | `1.0` | Compatibility contribution |

Hydrophone Surface Noise Fix is the earlier name of Nimm Bitte Platz. The original stock Nose Down, Proper Ballast, Seakeeping, Dusty's bundle, UBE, and other downloaded reference mods are not bundled here.




