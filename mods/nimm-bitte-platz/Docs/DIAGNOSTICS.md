# Settings and diagnostics — 1.4.1

With Dusty's Community Patch, open **Other Mods → Nimm Bitte Platz**. Changes to
the logging level and Trace interval apply immediately and persist.
Dusty is optional; no assembly reference to it is required.

The config remains:
`%USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\NimmBittePlatzDiagnostics.txt`

| Setting | Default | Behavior |
| --- | --- | --- |
| `LogLevel` | `Info` | Lifecycle, config, station entry/exit, warnings and errors |
| `LogLevel = Debug` | — | Info plus override acquisition/release decisions |
| `LogLevel = Trace` | — | Debug plus sampled hydrophone guard, depth and listener state |
| `TraceIntervalSeconds` | `2` | Real-time sample interval, clamped to 0.25–60 seconds; used only at Trace |

The file is created if missing and read once at startup. Existing DIAG1 files are
accepted. The old 1.4.0 `Enabled` config and `FixEnabled` menu values are ignored;
only the launcher controls whether the mod runs. Existing Debug or Trace
preferences are preserved. Choose **Info** or **Reset to defaults** for normal
play. Invalid values are reported and retain the default or last valid value.

Hand edits require a full game restart. Dusty also stores menu values in its own
config; on its first panel scan it applies any values it already has for this
mod. When using Dusty, change these settings through its menu so both copies stay
in agreement. Unchanged setters do not rewrite the mod's file.

The output log uses `[HydrophoneSurfaceNoiseFix][TAG]`, a UTC session ID and
elapsed real seconds. `START` identifies version `1.4.1` and build `20261024.2`.
`CONFIG` reports effective settings. `READY` appears only after patch installation
succeeds. A failed installation attempts to roll back this mod's Harmony patches
and reports whether that rollback succeeded; it never prints `READY`.

`STATION-START` and `STATION-STOP` record station sessions. `ERROR` includes the
first full exception for an area/type; subsequent repeats are counted. A failing
station stops further correction attempts until a new station session begins.

`STOP` reports counters once on normal application exit or lifecycle-host
destruction. Shutdown releases an owned override where possible and clears the
station references even if restoration throws. A crash or forced termination can
prevent `STOP`; its absence alone does not prove a mod failure.

There is no diagnostics `Update()` callback, file polling or scene-wide search
on each frame. The existing station UpdateController hook still checks ownership
while the station is active. Trace samples are throttled and constructed only
when due. The mod writes settings only at initial creation or a changed menu
setting; Unity sends messages to the game's existing output log.

For a report, include the log from the same session after quitting, UBOAT version,
hull, hydrophone equipment, depth, enabled mods and the view sequence that failed.
Specify whether the symptom is persistent wash, a brief crackle, silence or FPP
vibration. This correction targets persistent wash.
