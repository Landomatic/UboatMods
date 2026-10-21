# Casing isolation diagnostic — 1.0.23

Companion by Landomatic to stock Nose Down by Kugler, Workshop 3729673507.
Source and license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion

This is an experimental diagnostic build, not a demonstrated dive or PD fix.
It does not add pitch gains, change the normal planner, or smooth propulsion.

## Setup

1. Fully exit UBOAT. Back up the installed Companion folder and existing
   `NoseDownCompanionConfig.txt` outside the game's Mods directory.
2. Replace the Companion folder with this version. Keep stock Nose Down **2.1.5**
   and Companion enabled in the launcher. Disable Proper Ballast, Seakeeping and
   Engine Transition Glide. Leave other mods and crew priorities unchanged.
3. For run A, copy `TestConfigs/Run-A/NoseDownCompanionConfig.txt` to
   `%USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\NoseDownCompanionConfig.txt`.
   These are explicit test templates, not an upgrade replacement for normal play.
4. Start the game and reload the same untouched starting save. Do not use Reset.
   The config-only `diagnosticOnly=true` lock overrides the menu's master and
   every Companion correction gate. The menu may display a restored master ON;
   runtime `correctionsActive=False` is the authoritative effective state.
   The read-only menu rows should show Diagnostic lock active ON and Companion
   corrections active OFF; clicking these status rows does not change them.
5. Repeat the previous ordinary VIIB dive: same diesel start, telegraph,
   officer assignment timing, weather setup and 1x time; order 7.24 m PD.
   Do not change depth/speed. Hold three game minutes after first reaching PD.
6. Exit and preserve `output_log.txt` as run A. A side-view video is useful.
7. While fully exited, install the run B template. Restart and reload the same
   starting save; repeat the identical dive and preserve its log separately.

Run A leaves Nose Down's casing-water correction operating. Run B bypasses
only that correction for the player tank identified on the first player update.
Base-game water/air/fuel behavior continues; other tanks are untouched. This
does not disable Proper Ballast's correction, which is why it must be absent.
Do not continue a campaign from these diagnostic runs or overwrite the source
save. Return to your backed-up normal configuration when finished.

## What establishes a valid comparison

Both runs: startup v1.0.23; `diagnosticOnly=True`, `correctionsActive=False`,
`casingHookReady=True`, and increasing `casingObservedCalls` in EntryPhysics.
Run A: `casingBypassRequested=False`, zero bypass calls, applied False.
Run B: `casingBypassRequested=True`, increasing bypass calls, applied True.
Missing/unsupported contracts do not bypass anything. A hook-ready message
alone does not prove dispatch: the observed/applied counters must advance.
If they do not, preserve the log; do not interpret B as a casing-off result.

The bypass requires the diagnostic lock, independently of the menu master.
Both diagnostic controls are config-only and survive the menu's reset/save
operations. Normal distributed defaults for both are false.

## Measurements and limits

EntryPhysics samples at up to 10 Hz in the existing shallow diagnostic window.
It adds casing water/air/fuel/capacity, buoyancy ratio, instantaneous world
velocity and forward speed, alongside existing COM, mass, inertia, engine,
pitch/rate, position boundaries and controller-output data. `speed` retains
the older game AverageVelocity reading for continuity.

`uniformBuoyancyCentreLocal` reads the game's private
NeuralNetworkPhysics.uniformDensityCenterOfMass vector. It is the game's
uniform-density reference, not a measured instantaneous centre of all buoyant
or hydrodynamic forces. Missing reflection data is `n/a`.
`comPre/comPost` are local Rigidbody COM; `yPre/yPost` are direct world position
at FixedUpdate boundaries, not the complete subsequent physics-solver step.
Controller output and bow-lift compensation are not total applied torque.

Compare entry pitch loss against flooding, COM and engine timing first. A/B
differences identify a contribution in this setup, not a universal causal proof.
Deck depth alone is wave-sensitive; use direct hull position for hull movement.
