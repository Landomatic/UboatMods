TORPEDO ROOM -- 0.1.18-test
Author: Landomatic
Source/history reference: https://github.com/Landomatic/UboatMods
This new project is not yet published in that repository.
License: LICENSE (CC BY-NC 4.0); upstream notices: NOTICE.txt.

INSTALL
Replace the previous TorpedoRoomControls folder, rather than layering Source
files. Enable Torpedo Room in the launcher and fully restart UBOAT. Keep a
separate, untouched save. No DLL is bundled; UBOAT compiles Source on startup.
Check output_log.txt for [TorpedoRoom] 0.1.18-test loaded.
All shared-boat players must install the same 0.1.18 build. Wire format TRC7 is
incompatible with earlier companion builds. The optional multiplayer adapter
supports the supplied Uboats: Coop Multiplayer 1.11.5 / protocol 54 only.

0.1.18 INITIAL-PUBLICATION CANDIDATE -- TEST TDC BEFORE PUBLISHING
Copyright (c) 2026 Landomatic. Existing license and upstream credits are retained.
Workshop description draft: WORKSHOP_DESCRIPTION.txt. Release gate: PUBLISHING.txt.
This package is prepared for testing, not a claim of a validated public release.

SPEED AND FIRING -- 0.1.18
- Remove this mod's mixed-speed and TDC-mismatch launch holds entirely. Physical
  tube levers no longer depend on our selected solution or speed focus. Native
  readiness and MP access permissions still apply at every firing station.
- Mixed/mismatched speeds show informational warnings only. No automatic speed
  correction occurs merely because a weapon fires; its applied speed is honored.
- Host option: Bidirectional torpedo / TDC speed sync (speedSync=True by default).
  ON: room Apply updates native shared/TDC speed; TDC speed inputs update selected
  tube preparations. With no marks, the last explicitly applied speed tube is the
  focus. Marked selections take precedence; replaced weapons invalidate focus.
  OFF: keep room preparations and TDC inputs independent, including player errors.
- The speed option is independent of Synchronized / Read-only / Hidden reports.
  Casual and Hardcore presets leave it unchanged. Explicit Reset enables it.
- A single TDC solution cannot describe different speeds simultaneously. Applying
  a TDC speed to a group uses its common supported presets; merely firing never
  forces the group to one speed. The selected target-speed estimate is preserved
  when our synchronization changes weapon speed. Native bearing/AOB/range, gyro
  relays, target tracking and aim-point behavior are unchanged.
- TDC clicks resolve the closest authored native dial slot, avoiding integer
  truncation/fallback oscillation. Forward and reverse cycling wrap normally.
- Room speed labels, dials and overridden viewport readouts follow the live game
  knots / km/h setting. Units are presentation only: records remain m/s and the
  native computer remains knots. A unit switch preserves an unapplied draft.
- MP host validates changes. TRC7 carries the independent rule, speed and focus;
  native multiplayer transport still carries TDC inputs. All peers need 0.1.18.

MINIMAL TORPEDO HOVER
Hover a tube card in either the initial dock or the expanded editor for model/type.
With Happy Torpedo Time installed, also show maintenance state / remaining benefit
from the live weapon fields. No extra depth, speed, pistol or program text is added
on hover. Empty/replaced tubes and station exits clear the tooltip. No HTT source,
assets or DLL is bundled and HTT is optional.

PISTOL POLICY
Port-only has been removed. Choose unrestricted AZ/MZ (Free), locked on load, or
optional dry-tube preparation (30 game seconds by default). FAT/LUT remains
adjustable at sea without delay. Existing config files are not migrated or rewritten
on load; retired policy value 2 has no port restriction and is treated as Free.

LARGER CONTROLS / APPLY
- Enlarge instruments inside the panel from 100 to 124 UI units, increase numeric
  readouts and labels, and reserve space inside the backing for the larger controls.
  Tube artwork is larger too. Native station shortcut size remains unchanged.
- Apply is now 340 x 48 UI units with bold 21-point text and a highlighted pending
  state. It reports unapplied changes, applied settings or waiting for host. A host
  reply or timeout releases the wait so rejection cannot leave it disabled forever.
  Pistol/other drafts still commit only through Apply and existing preparation rules.

VALIDATION / RELEASE STATUS
Full supplied-reference compilation and 700 offline assertions passed. New TDC
behavior, physical launches, larger layout and shared-boat exchange still require
in-game testing. The user reports reduced-stack MP worked well; larger-stack
interaction troubleshooting remains separate. No general compatibility fix is
claimed. Do not publish until the focused checklist in TESTING.txt passes.

Previous patch details (historical; superseded where stated above):

0.1.16 COMPASS HEADING / CONSISTENT CONTROLS
- LUT heading uses a complete clockwise compass: top 0/360, right 90, bottom
  180, left 270. Click, drag and scroll agree with the needle; scrolling wraps
  through north. Typed 360 is accepted as 0 when Apply confirms the setting.
- Tube-top AZ/MZ labels show the applied mode. Changing either pistol selector
  edits the lower editor's draft until Apply; timed preparation still retains
  the current mode until its existing 30-game-second default delay completes.
- Steam speed click/drag, wheel and < > buttons select the same native Speed1/2/3
  presets and use one decimal place in knots, matching our native display bridge.
  Exact native metres-per-second presets are stored; speed numeric entry stays off.
- Native depth sliders now accept 0.5 m, matching the room's 0.5-14 m range with
  0.25 m steps. This is a runtime slider change, not a game-file edit. Restore
  the original minimum/stepping on disable/unload; range callbacks cannot create
  preparation edits or alter the shared depth during display setup/restoration.
- Enlarge the room preparation panel and collapsed tube dock by 10 percent.
  The game UI scale remains the baseline; live station shortcuts keep native size.
- TRC5 and existing configs remain; no setting migration or extra dependency.
Full reference compilation and 612 offline assertions pass. Revised Unity visuals,
Harmony behavior and shared-boat MP still require in-game testing.

HOST LOG REVIEW
The supplied host log reports Start Script (UBOAT.Game.Sandbox.Missions.StartScript)
as the remaining loading blockade at 90, 180, 270, 360 and 450 seconds. Earlier,
a NullReferenceException occurs in BuildStaticEntity.OnFinished, through HQ task
completion and HeadquartersTaskHandler.AddDateListener_Patch1. The supplied game
method searches a public group and an entity, then enables it. The stack does not
name the missing construction target or establish that this exception caused the
remaining start-script wait.
The host has hq_tasks_take_time=False. The supplied MP 1.11.5 source patches that
scheduling method, but explicitly skips automatic handlers. This failure stack
includes automatic assignment, so the patched method name/setting alone cannot
identify MP as the cause. For isolation, repeat the same career/save startup with
only Torpedo Room disabled and compare the exception and blockade. No HQ/MP start
code is changed in this patch; the loading hang is not claimed fixed.
Torpedo Room initializes successfully and continues updating in the log, without
an attributed exception. That does not rule out a compatibility interaction.
There are also independent startup errors for Expanded Radio Messages (old Harmony
API) and Workshop item 3270955106's Bundles/Manifest.json treated as an asset bundle.
Those errors deserve cleanup but are not proven causes of this loading blockade.

Previous patch details:

0.1.15 VIEWPORT SPEED PRESETS / LARGER ROTATING KNOBS
- Rebuild native speed choices from marked, loaded stable weapons, including dry
  tubes. Keep the intersection of their supported presets: a mixed steam/electric
  selection still permits only common speeds. Mark just the T1s to choose their
  highest shared setting. Existing host/presentation/MP permissions still apply.
- Refresh native speedSettingsBuffer and slider min/max before a manual speed
  callback and after native validation. Restore native selector/readout visibility
  for supported dry tubes; the native flooded-only scan must not leave a dry T1
  stuck with an empty/electric-only list or a clamped slider maximum.
- Capture original slider bounds and restore them when disabling/unloading this
  companion. Automatic range/display callbacks remain inside the projection guard
  and must not create edits. Unstable tubes do not cap the stable selection or
  create a false mixed-speed report. Native launch/flood controls are unchanged.
- Enlarge the actual rotating knob artwork, not just the surrounding faces.
  Fit artwork around its authored hub within 90 percent of the instrument diameter,
  retaining its proportions and pivot. This supersedes 0.1.13's native-size cap.
  The whole rotating rectangle remains inside the hit frame; UI scale still follows
  the game. Actual appearance and raster softness require in-game assessment.
- Speed is a plain readout with a dial/scroll and < > preset buttons. Keyboard/text
  entry and its input-box background are disabled for speed. A single-speed weapon
  remains visible as fixed, with no extra presets. Depth/run/heading still allow
  text entry and all room changes still require Apply.
- Wire remains TRC5; no record/config migration, new dependencies or permissions.
  Use the same 0.1.15 build across shared-boat peers for consistent UI behavior.
Full reference compilation and 580 offline assertions pass. The supplied screenshot
and user report describe 0.1.14; actual 0.1.15 Unity/Harmony behavior is untested.

0.1.14 SPEED SELECTOR / FULL INSTRUMENT FACES
- Show Speed (kn) alongside depth whenever a loaded tube is selected, including
  ordinary electric torpedoes. A one-speed weapon shows its actual speed with
  a fixed label; it does not gain unsupported speeds. Multi-speed weapons allow
  dragging, scrolling or typing; Apply snaps to a supported native preset.
- Read supported speeds from the loaded StoredTorpedo.Speed1/2/3 values, remove
  duplicate/nonpositive values and store chosen speed in metres per second.
  Host checks the requested speed against that weapon before committing it.
  Tube cards report effective speed. The room editor edits one selected tube.
- Capture explicit per-tube speed with the launch plan and override the native
  torpedoSpeed argument on supported launcher overloads. No new firing/flooding
  commands or permissions are added. Existing records without speed overrides
  inherit the native default; no config or save migration is performed.
- Synchronized viewport speed edits update marked tubes and preserve the native
  shared-default change. Read-only/hidden modes reject edits; hidden masks also
  include native speed controls. Consistent marked speed is projected temporarily
  into native display/validation, including in the room; mixed speeds say MIXED.
  Programmatic display callbacks do not create preparation edits. The native TDC
  still uses one current speed/solution; no per-tube intercept solver is added.
- Prefer the full launch-panel depth widget as the handle template. Add full-sized
  dark circular instrument faces behind the native rotating handles, instead of
  enlarging the small handle sprites. The faces are original vector UI geometry,
  not copied game dial-face assets. This is a visual sizing repair requiring a
  comparison with the supplied main-launch-interface footage in-game.
- Increase tube artwork height from 86 to 112 UI units; remove the dock's extra
  0.85 reduction. Keep labels below the art, with space for speed. Four instruments
  fit across the editor: depth, speed, initial run and applicable LUT heading.
  The editor is 950x514 UI units before applying the live main-game canvas scale.
  Native station navigation, authored pivots and the modal input shield remain.
- TRC5 adds speed to the existing record. ALL MP peers must install 0.1.14;
  older companion packets are refused. Upstream multiplayer files are unchanged.
Full reference compilation and 568 offline assertions pass. Actual Unity visuals,
Harmony dispatch, firing speed/range and MP gameplay still require testing.
Individual in-menu torpedo hover tooltips remain queued in ROADMAP.txt.

0.1.13 LIVE GAME UI SCALE / NATIVE ARTWORK SIZE
- Match the actual main game canvas pixel scale, pixel density and pixel-perfect
  flag while the room interface is active. Native CanvasUserSettings drives that
  scale from the game's UI preference. Do not multiply by our independent
  screen-resolution scale a second time. Changes apply without rebuilding the
  room editor or discarding a draft; no new config entry or migration is added.
- If the main game canvas is not available, retain the existing screen-resolution
  fallback until it appears. Invalid scales do not replace the last valid scale.
- Size copied knobs from their native screen-space local-unit size, preserving
  authored pivots and proportions. Small sprites are no longer enlarged to fill
  the 100-unit hit square. Large native artwork may shrink to fit that square;
  the input hit area and quarter-metre dial mapping are unchanged.
- Retain both authored station-bar scale and scale inherited from its original
  parent through a separate layout holder. Update that relationship live and
  restore the native hierarchy on exit. Do not reset the bar to unit scale or
  undo native scale changes when restoring it. Native listeners/MP checks remain.
- This focused scaling build does not add speed control or icon hover tooltips;
  both remain recorded in ROADMAP.txt for the subsequent feature patch.
Full reference compilation and 519 offline assertions pass. Actual sharpness,
UI setting changes and native station transitions require an in-game test.

0.1.12 ROOM ARTWORK / STATION SHORTCUTS
- Retain the game's actual left station-switch bar in the room when its native
  controller is enabled and its own visibility permits it. Temporarily place the
  live bar at the left edge, preserving its listeners, disabled toggles and all
  existing game/MP station checks. Restore its original parent, sibling and
  rectangle on exit, loading, disabling the interface or rebuilding the canvas.
  This does not add stations or grant access in restricted modes.
- Copy only native torpedo and open/closed door artwork into room cards. Do not
  copy native numbers, pistol/status captions, status-panel backgrounds or the
  flood layer that depends on the native masking hierarchy. Colored icons remain
  supported; live flood/drain progress is still shown as status text.
- Fit artwork using its actual image bounds inside the reserved frame, above
  our labels. Keep a separate AZ/MZ button beside each tube number; edits open
  the editor and require Apply as before. Existing permission/policy checks apply.
- Deactivate old cards immediately before deferred destruction on a compartment
  or visibility-mode change, preventing one-frame leftovers.
- Shared depth behavior is unchanged. The native dial writes a boat-wide default;
  tubes without overrides follow it. Synchronized manual viewport changes also
  stamp the marked stable tubes. An unmarked tube with an explicit depth override
  retains that override. The room editor edits one selected tube independently.
Full reference compilation and 504 offline assertions pass. Real station-bar
visibility/layout, transitions and updated graphics require an in-game test.

0.1.11 STARTUP REPAIR
The supplied 0.1.10 log confirms initialization failed while Harmony tried to
patch abstract FPPInteractionObject.OnClick, which has no method body. The loader
then disabled this mod and removed its patches. Dusty could still discover the
settings provider, while the native room UI and 0.5 m steps returned.
This build guards the concrete pointer wrapper instead, rejects abstract or
bodyless targets and isolates supplementary input-guard errors from core startup.
An auxiliary guard warning does not disable the room UI or depth controls.
The full-screen modal raycast shield remains. Quarter-metre depth is retained;
no config migration or change to saved preferences is introduced.
Full reference compilation and 486 offline assertions pass. Actual game startup,
Harmony dispatch and physical input blocking still need in-game verification.

0.1.10 DOORS / KNOB PIVOTS / MODAL INPUT / MENU SCROLL
Based on the supplied 0.1.9 test video and report:
- Match native tube presentation: dry/unflooded shows an open door and torpedo;
  flooded/flooding/native state 7 shows the closed door. Only graphics change.
- Preserve the native knob's authored rotation pivot. The arm projects beyond
  the hub, so centering the entire image made the knob orbit instead of rotate.
- While the editor is open, a full-screen transparent raycast shield blocks UI
  clicks underneath. Local native tube/physical dial pointer handlers are also
  guarded while this modal editor is open, and released when it closes. Flood,
  drain and launch commands/permissions are not patched by these input guards.
- T works while Alt is held for the game's cursor, without requiring Alt. Saved
  required modifiers still apply; Ctrl/Shift matching stays strict. This addresses
  one plausible missed-press cause; the brief video issue is not conclusively
  diagnosed without a current runtime log.
- Dusty dependent settings still refresh, but their scroll-content pixel offset
  is restored after the deferred rebuild instead of jumping to the top.
- Pistol preparation time defaults to 30 GAME seconds, including Reset Defaults.
  It applies to the existing dry-tube preparation policy. Free remains instant;
  locked policy retains its meaning. FAT/LUT stays immediate. Existing
  saved preparationSeconds values are respected: set the time to 30 in Dusty if
  an existing test config still has 0. No config migration is added.

The centre-relative dial input correction, quarter-metre depth and compact
backed layout from 0.1.9 remain. The new repairs require actual in-game checks.
The game's main UI-scale integration is implemented in 0.1.13 above.

The overlay reference resolution is now 1920x1080 rather than 1280x720: at the
same 16:9 resolution the editor is about one-third smaller in each dimension.
The bottom strip has an additional 0.85 scale. Both have dark backing panels
when no native board sprite is available; the dock backing is always present.
The editor is 950x478 UI units and dock backing is 800x164 UI units before
applying the live game scale. The earlier fixed-resolution pixel sizes are
superseded by 0.1.13; the dock retains its relative 0.85 artwork scale.
Actual game layout and cursor/dial interaction still require a new game test.

The compact tube strip appears automatically at a torpedo control station and
replaces the native bottom room display while enabled. T or a tube click opens
the centred editor. Escape, Close or T collapses to our strip, without revealing
stock room graphics. Leaving the station or disabling the mod/interface restores
native presentation. Existing physical levers still flood, drain and fire.
Use the game's cursor visibility control to click the strip in first-person.
No MP roles, firing permissions or flooding actions are added.

0.25 m DEPTH
Base game files are not edited. Depth remains the game's floating-point launch
argument/TargetDepth value; the existing runtime launch hooks supply the saved
per-torpedo override. Native slider step handling and readout precision are
patched at runtime. This specifies the requested running depth, not guaranteed
physical depth-holding accuracy.
Depth selection is 0.5-14 m in quarter-metre steps. Scroll uses 0.25 m; typed
values are rounded to the nearest quarter metre when Apply is pressed. The host
also quantizes newly selected depth overrides, including incoming edits. Reports
and the native depth readout can display 3.25 or 3.75 m without rounding them to
tenths. The heading dial retains 0.1-degree steps; initial run retains 100 m dial
steps. Pre-existing saved depths are not silently rewritten by unrelated edits;
a subsequent depth selection uses quarter-metre steps. No finer depth selection
is committed by the room editor.

CONFIG FILENAME (NO MIGRATION)
Only TorpedoRoomConfig.txt is read/written. If absent, it is created with defaults.
Neither TorpedoRoom.txt nor LandomaticTorpedoRoom.txt is imported, renamed or
deleted. Existing test settings in those files do not carry over automatically.
Dusty's own stored menu values remain owned by Dusty's Community Patch.

Viewport synchronized controls retain the 0.1.7/0.1.8 group-value and LUT-heading
repairs. Matching selected settings display normally; only mixed dimensions
show a dash. The user reported that two matching LUT torpedoes synchronized in
0.1.8. This is a partial result for the prior build, not validation of 0.1.9.
The single-Graphic readout crash fix from 0.1.8 remains in place.

Tube numbering follows the actual ship launcher array: bow 1-4 or Type II 1-3,
with stern tubes numbered in the same boat-wide order where present.

Per-tube controls: depth 0.5-14 m in 0.25 m increments, game-supported AZ/MZ, Straight or the fitted
FAT/LUT program, initial run distance 500-15000 m, first-turn direction, and LUT
bearing where applicable. Drag a dial across its upper semicircle or scroll it;
the same numeric box updates. Typing moves the needle. Depth dials step 0.25 m, heading dials step
0.1 degrees, run distance steps 100 m; numeric entry permits finer run distances. Use
Apply to confirm changes. Native depth dials also accept quarter metres while enabled;
vanilla stepping is restored when the mod is disabled. Fields below an
opposite-compartment tube are read-only unless the host allows all-tube editing.
Straight mode hides pattern fields but preserves their values. Switch back to
the fitted program to restore them. No pins, failure rates or maintenance task
are simulated. The optional host Straight-running switch is retained.
The per-tube source selector retains the previous choice: Source: Per-tube protects the
stored program at launch; Source: Last station uses the native common program
selected through the viewing-station interface. Last station is a clearer label
for the prior Stock/shared mode, not a new frozen historical snapshot. Straight
mode overrides the pattern either way. Depth becomes a per-tube override when
explicitly applied in the room or changed at a synchronized native viewport dial.
Existing records without a depth override continue using native shared depth.

Flooding, draining, marking and firing remain on the existing physical/game
controls; close the editor to operate physical levers. No new firing, role or station permissions are
added. Existing MP role access and firing rules remain the authority.

DUSTY'S OPTIONAL SETTINGS
Submarine / Torpedoes -> Torpedo Room. No hard dependency is required. Config:
%USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\TorpedoRoomConfig.txt
The file is created automatically with defaults; no old config is imported.

Host/single-player rules:
Tube access: Current compartment only / Opposite tubes read-only / All tubes
editable from either tube compartment. This affects preparation, not physical
levers. Previous overview OFF/ON maps to the first/second option.
Viewport display (scope/UZO/TDC): Synchronized controls / Read-only reports /
Hidden reports. Synchronized native edits update the marked tubes, preserving
unchanged per-tube dimensions; revisions reject conflicting/stale edits.
Read-only shows effective program/depth labels without
allowing native preparation edits. Ordinary torpedoes get depth only. FAT/LUT
torpedoes show their active pattern, run/direction, LUT heading where applicable,
and depth; disabled patterns show STRAIGHT. No duplicate tube number, flood state
or pistol text is added to viewport labels. Tube marking remains available.
Hidden masks the native tube icons, program/depth controls, and data panel and
suppresses our labels; the fire button is outside those masks. It does not
select tubes automatically or enable a firing control blocked by vanilla/MP.
The new room-only Straight/source selector is not added to native viewports;
its result is reported there when reports are enabled. Native program/depth and
pistol controls synchronize where offered. Timed pistol preparation still
requires the room. FAT/LUT has no preparation delay or port restriction.
If MP's Tubes panel at the scopes is OFF with others aboard, our settings do not
reopen it or grant scope editing. Fire-control boxes use MP's TDC station route.

Casual preset: All tubes editable, synchronized reports, unrestricted AZ/MZ.
Hardcore preset: Current compartment only, hidden reports, unrestricted AZ/MZ.
Custom leaves individual choices intact and remains selectable after Hardcore.
Preset changes refresh dependent Dusty rows on the next update and persist their
values together, so cached individual rows cannot silently undo a preset.
Presets do not change vanilla realism,
FPP, torpedo flaws, MP roles, tube selection or firing permissions. Current
upgrades default to read-only reports, preserving 0.1.4's protected-room intent.
Pistol policies remain optional gameplay choices: unrestricted, locked on load,
or dry-tube preparation with an optional game-time delay.

GRAPHICS COMPATIBILITY
Torpedo Type Icons (Workshop 3761351154) is optional. The panel calls its installed
sprite provider when available and otherwise borrows native tube sprites.
Color sprites retain their original colors (no brass tint). Neither its images
nor source are bundled. Leave both mods enabled when testing compatibility.

LIMITS AND UNINSTALL
This is an offline-compiled/tested build, not an in-game-validated release.
Actual 0.1.6 launch depth/program, Unity layout, all hulls, save migration and
multiplayer coexistence need confirmation. The user reports the LUT firing test
passed in 0.1.5, icon colors are retained, and stutter has been gone since 0.1.3.
Those are user-observed results, not instrumented performance/flight validation. Timings are local diagnostic
measurements and do not establish the cause of a stutter. No own-boat wolfpack
replication is added. See TESTING.txt and VALIDATION.txt.
Before uninstalling, use Remove preparation records before uninstalling as host,
then save to a separate slot, quit, and remove this mod. Preserve your baseline.

Modifier multi-selection and mixed-salvo program confirmation are queued in
ROADMAP.txt. They are not implemented in this patch.
