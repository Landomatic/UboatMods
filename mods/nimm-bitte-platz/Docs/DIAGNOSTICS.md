# Diagnostics for 1.3.2-beta-DIAG1

The output_log uses `[HydrophoneSurfaceNoiseFix][TAG]`, a UTC session ID, and elapsed real seconds.
`START`, `CONFIG`, `READY`, `FAILED`, `ERROR`, and `STOP` are always recorded.
`READY` is emitted only after the mod's patches install successfully.
`STOP` is emitted once on normal application exit or diagnostics-host destruction;
forced termination or a crash may prevent it. An observed STOP is not proof of
all gameplay actions succeeding, and a missing STOP alone is not proof of failure.

Config: `%USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\NimmBittePlatzDiagnostics.txt`
The mod creates this file on first startup. Edit it and fully restart UBOAT.

| Setting | Behavior |
|---|---|
| `LogLevel = Info` | Lifecycle, effective config, key events and errors; default |
| `LogLevel = Debug` | Info plus decisions and intermediate values |
| `LogLevel = Trace` | Debug plus sampled telemetry |
| `TraceIntervalSeconds = 2` | Real-time sample interval; clamped to 0.25-60 seconds |

Repeated errors with the same area and exception type print one full exception;
STOP reports the total and repeat counts. Sampling is per tag, not every frame.
Invalid settings are reported and retain a safe default or the last valid value.
File writes are limited to this diagnostics config. No log file is duplicated:
Unity routes the messages to the game's output_log. Both Reflection and IO
permissions are declared because Harmony and the config reader need them.

Always attach the output_log from the same game session after quitting.
