Current 0.1.18: 700 offline assertions; see VALIDATION.txt for exact groups/limits.
Compile actual SpeedControl + SpeedUnits + TdcSpeed + TorpedoInfo + Model with
Tests/Speed/SpeedFixture.cs, Tests/Speed/InfoFixture.cs and Tests/Tdc/TdcFixture.cs;
run SpeedChecks.Run(), TdcChecks.Run(), InfoChecks.Run(). Other fixture groups
remain separate. Runtime/Grid are checked against real game references; these
fixtures do not prove live Unity, Harmony, networking or torpedo flight behavior.
TRC7 adds the independent host speed-sync rule. All MP peers must update.

Previous test records (historical):

Current 0.1.17: 659 offline assertions, including 39 actual TdcSpeed checks.
Compile Source/Model.cs + Source/SpeedControl.cs + Source/TdcSpeed.cs with
Tests/Speed/SpeedFixture.cs + Tests/Tdc/TdcFixture.cs as one isolated fixture.
Run SpeedChecks.Run() and TdcChecks.Run(). Other fixture groups remain separate.
TRC6 adds host solution speed and no-checkmark room focus; all MP peers update.
Real game-reference compilation is separate from these independent API doubles.

Offline checks

Checks.cs exercises the actual Model.cs rules and wire codec. It also uses the
unmodified MpProtocol.cs from the user-supplied Uboats: Coop Multiplayer 1.11.5
as a local compilation input, with a fixture supplying only ProtocolVersion 54.
That upstream file is NOT included in this package. Credit for the transport:
Pingu565, Kugler and Dusty; CC BY-NC-SA 4.0; Workshop item 3815285055.

These checks do not simulate Unity physics or prove multiplayer gameplay.
They cover independent drafts, pistol policies, mandatory at-sea FAT/LUT edits,
replaced torpedoes, concurrent revisions, numeric bounds, host policy and
sixteen-tube snapshots, packet truncation/corruption, and the actual upstream
single-message and bundled transport framing.

BridgeFixture.cs additionally runs the actual Bridge.cs against simulated session,
station and Harmony objects using the upstream packet implementation. This checks
version gating, known-peer handshakes, world tokens, station freshness/group
permission, draft revisions, message routing, readiness expiry and reset. It does
not exercise Steam networking or actual Harmony dispatch inside Unity.

0.1.1 additionally checks per-torpedo straight-run flags and host availability
round trips, retained programs after pin removal, old-protocol refusal and the
actual shortcut class with a keyboard fixture. The supplied Dusty external
provider parser was inspected to verify its native "key" row contract.

0.1.2 adds Config/ConfigFixture.cs. Compile it SEPARATELY with actual Model.cs,
Settings.cs and Shortcut.cs, without BridgeFixture.cs or Checks.cs. It provides
isolated Unity/bridge/runtime doubles and runs ConfigChecks.Run(). It creates
and removes its own temporary directory and never touches a player config.
28 assertions cover first config creation, existing preference preservation,
atomic save cleanup, legacy defaults, reserved shortcuts, actual provider rows,
key setter persistence and host-rule rejection on clients.

The complete revised mod was compiled without UnityEngine.IMGUIModule using
the references identified in the supplied Mod.Compile plus UIModule for Canvas.
The old source fails with matching GUI errors under the same limited set.
This is an offline reference check, not execution of UBOAT's compiler/runtime.

0.1.3:
201 model/policy/codec, 31 bridge, 28 shortcut, 33 config/provider and 19 stock
viewport bridge assertions: 312 total. The installed mod has seven source files.
StockUi/StockUiFixture.cs is another SEPARATE fixture build: compile with actual
Model.cs and StockUi.cs, without other fixtures, and run StockUiChecks.Run().
The native API fixtures verify logic paths, not Unity rendering/Harmony dispatch.
BridgeFixture uses actual upstream WriteRaw batching and checks no additional
type scans during 1000 installed idle ticks. ConfigFixture checks the renamed
provider and migration of preferences to TorpedoRoom.txt.

0.1.4 current totals: 203 model/policy/codec + 32 adapter + 28 shortcut +
33 config/provider + 22 stock UI + 18 launch-authority = 336 assertions.
Launch/LaunchFixture.cs is a SEPARATE build with actual Model.cs and
LaunchAuthority.cs; run LaunchChecks.Run(). It simulates native setter fields
and independently exercises captured authority, both typed argument hooks,
UZO 500-left vs room 2500-right, item detach/status removal, native result audit,
late replay, disabled/dry/other-ship guards and plan expiry/reset. These fixtures
do not execute Harmony patches or real torpedo physics inside UBOAT.
The installed source now comprises eight files.

0.1.5 current totals: 222 model/codec + 38 adapter + 28 shortcut + 40 config +
34 native UI/depth/icon-provider + 20 launch = 382 assertions. UI fixture build
now includes actual StockUi.cs, NativePresentation.cs and DepthControl.cs.
TRC4 adds per-tube depth and host presentation/access modes. Revised tests follow
the explicit synchronized-mode policy instead of 0.1.4's unconditional room-only
program protection. These fixtures do not execute Runtime.Accept inside a game.

0.1.6: 232 model/codec/report/dial mapping + 38 adapter + 28 shortcut +
48 settings/integration + 41 native UI/depth + 20 launch + 15 dial event checks
= 422 assertions. The config fixture now includes actual MenuIntegration.cs and
fake Dusty persistence/refresh APIs. The dial fixture separately compiles actual
Model.cs and DialControl.cs with Dial/DialFixture.cs. NativeSkin compiles against
real game APIs; its runtime hierarchy, art, hotspot layout and rendering are not
exercised by these fixtures. No native game scripts/listeners are cloned.

0.1.7: 439 assertions total. The native UI fixture now has 58 assertions, adding
grouped equal/mixed values, effective Last station values, heading visibility
after deselection/reselection, FAT/LUT and straight group handling, explicit
direction clicks when the common value already matches, and retry after a
transient native refresh error. Fixtures invoke the actual prefix/postfix logic
with independent API doubles; they do not execute Harmony dispatch in Unity.
Root canvas layout and room-panel construction still need in-game validation.

0.1.8 adds NativeClick/NativeClickFixture.cs, compiled separately with actual
Source/NativeClick.cs. The independent fixture rejects multiple Graphic objects
on a single GameObject: it reproduces the old failure and verifies TMP readouts
reuse their existing Graphic with a functional click action. NativeSkin's other
partial source is compiled in the full game-reference build, not this fixture.
Stock UI fixture adds room takeover retention across native refresh and restore.
Actual root-canvas layout, persistent dock and input/cursor use need game tests.

Current result: 449 assertions pass. The unmodified 0.1.7 ClickArea body fails
this same single-Graphic fixture with NullReferenceException. The corrected
NativeClick.cs passes all seven click-target assertions. This catches the actual
construction regression that previous fixtures did not cover.

0.1.9: Dial fixture now models actual top-left-pivot layout and transformed
screen coordinates, checks intermediate values and quarter-metre precision.
Config fixture verifies only TorpedoRoomConfig.txt is read, old files are not
imported or modified, and new settings still persist. Native depth fixture checks
quarter-metre quantization and two-decimal native label display. Rendering/native
hierarchy changes still need in-game verification.

Current total: 459 passing assertions (232 model, 38 bridge, 28 shortcut,
48 config, 62 native UI/depth, 20 launch, 24 dial events, 7 native click).
The old pivot assumption fails the realistic layout fixture at the left endpoint;
the centre-relative implementation passes intermediate and scaled cases.

0.1.10 total: 480 assertions. Additional cases check native door presentation,
authored off-centre image pivots anchored to the dial hub, default/reset timed
pistol duration, scroll-content replacement retaining offset, cursor-Alt keys,
and modal gating of native program/depth/pointer input. The real full-screen
shield and actual physical-device Harmony dispatch are game-test requirements.

0.1.11: 486 assertions total. Six added native UI/startup guard assertions invoke
actual SupportsInputGuard/InstallInputGuard with abstract and concrete reflection
targets plus a fake Harmony emitter. They verify invalid targets are never sent
to Harmony, failed supplementary patches are cleaned up, and later installation
continues. These do not execute real Mono/Harmony or validate physical raycasts.
The supplied 0.1.10 runtime log exposed a startup regression that the earlier
pointer-prefix fixture did not test. Actual 0.1.11 startup remains unverified.

0.1.12: 504 assertions total. NativeSkin/NativeSkinFixture.cs is a separate build
with actual Source/NativeSkin.cs and Source/NativeClick.cs. Run NativeSkinChecks.Run().
Its independent UI hierarchy/geometry doubles exercise restricted image copying,
no duplicate text/status/flood layers, fitting oversized/displaced artwork and
live native station-bar borrowing/restoration without changing native listeners,
visibility or disabled state. This fixture does not execute Unity's real layout,
native lifecycle/permissions, Harmony, MP networking or rendering.

0.1.13: 519 assertions total. Include actual Source/UiScale.cs in the separate
NativeSkin fixture build. Its 33 assertions include 15 added native scale cases:
main canvas pixel-scale/density adoption, live setting updates, safe absent/invalid
sources, no additional resolution multiplier, no enlargement of small native
knobs, fit of oversized knobs, preserved pivots, station parent and own scales,
and restoration without reverting native live changes. The fixture projects
simplified transforms; actual Unity canvas geometry/filtering/lifecycle and
real screenshots remain required to validate appearance.

0.1.14: 568 assertions total. Model tests cover supported/fixed speed presets,
TRC5 speed data and malformed/old wire cases. Launch prefix tests now include
speed authority; dial tests include preset stepping; mask tests include native
speed controls. Speed/SpeedFixture.cs is a SEPARATE build with actual Model.cs
and SpeedControl.cs; run SpeedChecks.Run(). Its 21 assertions cover native
projection/restore, read-only/hidden policies, selected tube imports, remote
requests, mixed speeds, unsupported groups and room-side projection. New host
acceptance checks compile in the full build; full Runtime transactions, actual
Harmony, TDC/torpedo physics and multiplayer gameplay are not run by this fixture.

0.1.15: 580 total assertions. Speed fixture has 32 cases and now models actual
slider bounds/clamping, range callbacks and control visibility. New cases check
dry marked-tube presets, stale fixed-speed list/max recovery, max viewport commit,
intersection for mixed weapons, unstable exclusion, guarded automatic changes and
restoration of original bounds. Skin fixture has 35 cases, testing enlarged knob
artwork and its hub-centred frame bounds instead of the superseded native-size cap.
Full Grid compilation covers plain speed readout/preset button construction; actual
rendering, keyboard focus and Harmony/UI lifecycle still require game testing.

0.1.16: 612 total assertions. Dial fixture: 54 cases, adding complete compass
geometry/seam/scale guards and native preset click/wheel/button label consistency.
Stock UI/depth fixture: 82 cases, with actual slider range clamping/callbacks and
half-metre projection/edit/restoration. Full Grid remains reference-compiled;
real Unity visuals, timed pistol labels, Harmony/MP and launches are not emulated.
