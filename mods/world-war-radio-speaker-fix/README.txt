# World War Radio - Speaker Fix 0.1.1

Local companion for MaGlas's World War Radio 1.0, Workshop item 3342143865,
on UBOAT 2026.1. Tested against installed game build 24913406 at compile time.
The companion reuses World War Radio's installed assets and data.

Use existing saves where World War Radio is already working. The fix binds its
live range checker and mixer to the current player boat after a load and starts
new static loops at zero volume. World War Radio still handles normal volume
control and weak-signal static once the station is valid. It does not initialize station data in a career started
without World War Radio, and it adds no custom save data. The collector and
the companion are independently optional.

## Install

1. Close UBOAT and the launcher.
2. If you installed version 0.1.0 locally, move Mods\LandomaticWorldWarRadioLoadFix
   out of Mods before enabling this version. Run one copy of the fix at a time.
   You can remove the old Temp\LandomaticWorldWarRadioLoadFix.dll after closing UBOAT.
3. Extract this ZIP into
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT
   The mod folder is Mods\WorldWarRadioSpeakerFix.
   The compiled DLL is Temp\WorldWarRadioSpeakerFix.dll.
4. In the launcher, leave World War Radio enabled and enable World War Radio -
   Speaker Fix. Keep DIAG3 enabled if you want a trace.
5. Launch normally. Confirm [WWRSpeakerFix] READY in output_log.txt.
   An INACTIVE line means the dependency was missing or its code did not
   match the patch. Disable the companion in the launcher to roll it back.

## Verify using the same existing saves

1. Load the clean IXA save. Play the radio. Press F10 if DIAG3 is enabled.
2. With the radio playing, load the IID save that previously produced the
   immediate raster. Wait for control to return, then press F10.
3. Turn the radio off and press F10 again. The static should be silent.
   With DIAG3, RADIO_STATE should report radioMatches=True,
   speakersMatch=True and rangeMatches=True once the IID settles.
   Verify the live and cached ship radio IDs agree.
4. Turn the radio back on and load the IXA again. Check normal playback,
   then repeat the same-save reload if you want to exercise reuse.
   Compare [WWRSpeakerFix] BOUND and [AudioLife] RADIO_STATE.
5. A valid weak signal can still produce intentional static. The bug under
   test is the loud static that continues after switching off the radio.

## Credits and license

World War Radio - Speaker Fix by Landomatic.
Requires World War Radio by MaGlas.
https://steamcommunity.com/sharedfiles/filedetails/?id=3342143865

The preview adapts the original World War Radio Workshop artwork.
Landomatic's original Speaker Fix code, compiled DLL, and documentation are licensed under Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0).
See Mods\WorldWarRadioSpeakerFix\LICENSE and NOTICE.txt for the license text
and artwork credit.

Source and history: https://github.com/Landomatic/UboatMods
