LANDOMATIC PATCH PROFILER - diagnostic only, remove after testing

INSTALL
1. Copy the folder "LandomaticPatchProfiler" (Manifest.json + Source\PatchProfiler.cs) to
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\Mods\
2. Start UBOAT, enable "Landomatic Patch Profiler" in the mod manager (load order does not matter:
   it scans 20 s after start and again 15 s after every scene load, so it also catches patches added late).
3. Reproduce: load a save, then load the most recent save again (second load), map closed, wait ~60 s,
   then open the map and wait ~30 s.
4. Send output_log.txt (also %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\).

CONFIG (created on first run): ...\LocalLow\Deep Water Studio\UBOAT\LandomaticPatchProfiler.txt
   enabled, threshold_ms (5), hitch_ms (50), summary_seconds (10), start_delay_seconds (20),
   rescan_after_scene_load_seconds (15), time_patch_methods, time_mod_unity_callbacks, exclude (a;b;c)

WHAT TO SEARCH FOR IN output_log.txt  (all lines start with [PatchProf])
   "[PatchProf] loaded"             - mod compiled and ran
   "[PatchProf] scan ("             - how many methods were instrumented; one per scan
   "[PatchProf] could not instrument" - skipped methods (first 10 only)
   "[PatchProf] 23.4 ms owner=..."  - a single call over the threshold (max 1/s per method, 20/s total)
   "[PatchProf] HITCH"              - frame over 50 ms + top 5 timed things in that frame
   "[PatchProf] summary"            - every 10 s: top 10 by total time and by max
Labels: "owner=<harmony ids> method=<Type.Method>" = the whole patched method (original + all patches);
        "(prefix of X)" / "(postfix of X)" = one mod's individual patch method;
        "(Unity callback)" = Update/LateUpdate/FixedUpdate/OnGUI of a mod MonoBehaviour.
