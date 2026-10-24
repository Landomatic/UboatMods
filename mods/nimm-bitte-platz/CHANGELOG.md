# Changes

## 1.4.1 — build 20261024.2

- Remove the hydrophone correction on/off switch and its backing config setting.
  The launcher controls mod activation; existing station/audio guards still apply.
- Ignore retired Enabled/FixEnabled values from 1.4.0, including saved false values.
- Keep Dusty controls for logging level, Trace interval and diagnostics reset.
- Update instructions, publishing text and regression checks for this behavior.


## 1.4.0 — build 20261024.1

- Add optional Dusty **Other Mods** menu controls for the hydrophone correction,
  Info/Debug/Trace logging and Trace interval, with live changes and saved settings.
- Release an owned audio override immediately when the correction is turned off.
- Accept existing `NimmBittePlatzDiagnostics.txt` files and preserve preferences;
  fresh settings default to correction on, Info and two seconds.
- Use consistent `major.minor.patch` versioning; remove Beta/DIAG naming from the
  current manifest while retaining the separate build ID.
- Remove the unused diagnostics frame callback. Keep lifecycle/station logs,
  throttled Trace sampling and repeated-error suppression.
- Report rollback failure accurately and clear shutdown references even if audio
  restoration throws. Retain the existing three hooks, ownership/submerged guards
  and per-session failure cutoff.
- Preserve Workshop ID 3810668782, the preview, license and Landomatic credit.
  Add source links, publishing notes and targeted offline behavior tests.

The underlying correction remains the same. No detection-range, crew, camera/FOV
or multiplayer synchronization changes are included. UBOAT compilation and live
menu/audio validation of this package remain pending; see Validation.json.

## 1.3.2-beta-DIAG1

Added lifecycle and station diagnostics, optional Debug/Trace, throttled samples,
installation rollback and shutdown cleanup to the existing repair. Its source
and record remain in repository history.

## 1.3.1

Prior Workshop version. Earlier repair/testing notes are retained in
HISTORY/README-1.3.1-beta.txt.
