World War Radio - First Track Fix v0.1.0

Independent companion by Landomatic for MaGlas's World War Radio.
Required mod: https://steamcommunity.com/sharedfiles/filedetails/?id=3342143865
Project repository: https://github.com/Landomatic/UboatMods

The first track after tuning starts at the same random position on both
speakers. Later tracks start from the beginning. Use an existing WWR career.

Install and test
1. Close UBOAT and its launcher. If you tested the replacement C# files,
   restore the two original World War Radio files from your backup first.
   This companion stands down if it detects the integrated fix.
2. Extract this ZIP into:
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT
   This places the mod in Mods\WorldWarRadioFirstTrackFix and its DLL in Temp.
3. Enable World War Radio - First Track Fix after World War Radio. It can
   be used alongside Speaker Fix and Radio Schedule Diagnostics v0.1.1.
4. Launch and look for [WWRFirstTrackFix] READY in output_log.txt. If it
   still reports the integrated fix after restoring the original source,
   close UBOAT and move only Temp\3342143865.dll out of Temp. UBOAT will
   recompile World War Radio from the restored source on the next launch.
5. Load the same save, select a station, and let three tracks play without
   retuning. There should be one [WWRFirstTrackFix] RANDOM_START on the
   first track. With Radio Schedule Diagnostics enabled, TRACK_NEXT_FRAME
   should show matching speaker offsets for track one and positions at
   or near zero for tracks two and three.

The integrated logic passed live on Warsaw, BBC London, and RN Paris in
an existing solo career. The standalone build was checked offline against
the installed UBOAT and WWR targets. Live and multiplayer tests are pending.

First Track Fix by Landomatic. World War Radio by MaGlas.
The included CC BY-NC 4.0 license covers the original companion.
