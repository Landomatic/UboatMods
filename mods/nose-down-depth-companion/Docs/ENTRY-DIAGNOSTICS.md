# Dive-entry observations — 1.0.22

Source/history: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
License and existing upstream credits: ../LICENSE. Companion by Landomatic to Nose Down by Kugler.

This build adds observation only; it retains 1.0.20 assistance formulas, gains,
limits and defaults. Full game restart is required after replacing the source
package. Keep the existing NoseDownCompanionConfig.txt. Enable its existing
traceLogging option (the companion menu's diagnostic logging switch when Dusty's
is available, or traceLogging=true in that config while the game is closed).
Check the initialized [v1.0.22] line and EntryPhysics rows in output_log.txt.
No game validation of this diagnostic build has yet occurred.

## Test sequence

Use a VIIB, forward propulsion, fixed telegraph and 1x time. Use sailors at the
valves and no officer at the planes in every comparison. Start surfaced, order
the same available PD, leave controls unchanged until submerged and settled,
then surface normally. Capture the first immersion and both engine changes.
For this entry investigation a minute at PD is enough; no four-run PD repeat is
needed. Do not change weather, ballast staffing, assistance or speed mid-run.

1. **Vanilla:** disable EVERY mod in the mod list and restart UBOAT. Use a fresh
   test/quick-mission scenario if the existing save depends on mods. Record a
   short external-view video showing the pitch, depth and speed through entry;
   preserve output_log.txt separately. Note hull, order, telegraph and sea state.
   With every mod off, our telemetry and Proper Ballast CSV recording are absent.
2. **Instrumented current setup:** install 1.0.22 and restart with the usual
   diving mods. With Nose Down 2.1.5 / Proper Ballast 1.2.0, start with our
   entry assistance OFF, VII torque boost OFF and Fast dive OFF. Keep Seakeeping
   OFF, casing fix ON, officer brake OFF, precise arrival ON, pre-dive planes
   OFF, PD bias OFF and wave reduction OFF. Keep diagnostics enabled. Match
   hull, speed, order and sea conditions as closely as the scenario allows.
   Preserve output_log.txt and the new ProperBallast folder (ZIP) as
   Instrumented-Modded. Reintroduce Fast dive, then extra torque separately;
   restart between upstream changes. See KUGLER-SWITCHES.md. If deliberately
   comparing old upstream builds, retain that older test's settings and label
   its versions rather than mixing it with the new-version baseline.
3. **If the vanilla video also shows the dip:** a useful further comparison is
   to enable ONLY this companion, leave enabled=false and traceLogging=true in
   NoseDownCompanionConfig.txt, keep Nose Down and every other mod disabled,
   and restart. This produces EntryPhysics rows with phase=absent and no law
   output. It is observation of vanilla physics with a diagnostic patch present,
   not literally an all-mods-off run. It does not need Proper Ballast or Dusty's.
   Preserve the log as Diagnostic-Baseline. The loader's normal Nose Down
   absence warning is expected; the recorder should still produce rows.

Full restarts are necessary between mod-list changes. Do not rely on the
upstream menus' Saved flags to unload a running mod. A modded save or a changed
weather provider can alter the starting state; label such differences, and do
not treat a changed result alone as proof that a particular mod caused it.
There is no need to repeat a run if the log clearly reports missing diagnostic
rows or an initialization error: send that log so the recorder can be fixed.

## What the rows mean

EntryPhysics rows use invariant numeric formatting and Unity Time.time in
seconds; sampling is at most 10 Hz in the shallow window (-3..12 m deck depth,
0..50 m order, timeScale above zero and no greater than 10). This includes
surface observation and PD; stop trace logging when not collecting a test.

- comPre/comPost: rigidbody LOCAL centre of mass, metres, x/y/z. The boundaries
  surround the existing patched PlayableSubmarine.FixedUpdate call chain;
  changes can come from the game or patches. They do not isolate vanilla alone.
- hydroDragPre/Post: game Drag.AngularDrag, distinct from rbDragPre/Post, which
  is Unity Rigidbody.angularDrag. balance/customBalance are game fields at the
  final boundary. n/a means unreadable, never an assumed zero.
- pitch/rateDeg: signed local-X angle (degrees) and local-X angular velocity
  (degrees/second). Positive pitch is bow-down in the supplied hull convention.
- yPre/yPost: direct rigidbody world Y at the update boundaries, metres;
  vy: vertical velocity in metres/second. Changes inside this call are not a
  complete solver step and should not be interpreted as integrated motion.
- engine/power/powerModifier and signed speed (m/s): engine identity and observed
  output at the sampled boundary. These help time a swap; they do not measure
  propeller thrust or every intermediate swap event.
- forcedPre/Post and wasSubmergedPre/Post: game forced-depth-keeping flags.
  These help distinguish direct depth corrections from velocity-driven heave.
- planeAuthorityBoundary and steer are game control fields at the boundary,
  not measured plane force or net torque.
- lawCalls: observed Nose Down law calls this update. When zero, law fields are
  omitted. Otherwise lawBefore is the result before our existing boost/entry/
  trim scaling, and lawAfter is after our existing hook; these are controller
  outputs, not total hydrodynamic torque. Other patches may affect final output.
  entryExtraBeforeScale is the added entry term before trimScale is applied.
  lawLimit=0 means an upstream limit was unavailable; do not interpret it as a
  measured zero ceiling. Multiple calls retain the last call and report count.

The recorder cannot directly observe all buoyancy, drag and wave torques. A
COM/drag change coincident with leveling is a candidate mechanism, not proof.
A baseline comparison is still required to attribute behavior to vanilla.
Private logs, saves and CSV recordings are not included in the public archive.


## New upstream versions

The original sequence above describes the earlier 2.1.4/1.1.0 setup. For Nose
Down 2.1.5 / Proper Ballast 1.2.0, first follow the upstream-version baseline
in KUGLER-SWITCHES.md: our entry assistance and torque boost OFF, Fast dive OFF,
restart, and keep all other choices fixed. Reintroduce one change at a time.
The upstream pitch controller, bow-lift compensation and flooding behavior
changed, so older recordings are not an identical baseline. bowLiftMoment
observes the separate upstream compensation when available; it is not included
in lawAfter and does not measure net torque. The log row version is 1.0.22.
