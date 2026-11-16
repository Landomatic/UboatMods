World War Radio - Expanded Radio Compatibility v0.1.2

Compatibility companion by Landomatic.
World War Radio by MaGlas: https://steamcommunity.com/sharedfiles/filedetails/?id=3342143865
Expanded Radio Messages [Audio] by Link: https://steamcommunity.com/sharedfiles/filedetails/?id=2331494749

When both mods are enabled, the companion replaces the known ERM-A
transmission-start timer hook. Only ERM-A's dated audio broadcasts get its
countdown. WWR keeps its campaign-date playlist handoffs. A dated reel
that has already expired is stopped before a timer is queued. Existing WWR
countdowns are retired when the game next tries to run them.

Install
1. Close UBOAT and the launcher.
2. Extract this ZIP into:
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT
3. Enable World War Radio - Expanded Radio Compatibility after both
   World War Radio and Expanded Radio Messages [Audio]. Use one enabled
   copy of ERM-A. Use the original ERM-A source with this companion.
4. Look for [WWRERMCompat] READY in output_log.txt. INACTIVE means
   the required mods or the supported ERM-A hook were unavailable. The
   companion waits up to 20 seconds for their startup hooks.

Test with an existing career
1. Use the same save and mod setup for both runs. Load once with this
   companion disabled, note a nearby WWR station and an ERM-A reel, and
   copy output_log.txt before relaunching. Then enable the companion
   and reload the same save. Keep the other mod settings the same.
2. With Radio Schedule Diagnostics enabled, press F8 after the boat loads
   and again after advancing campaign time. WWR should be available when
   in range. Its remaining duration should stay steady while a current
   ERM-A broadcast counts down. RETIRED_WWR_TIMER shows an old callback
   was removed. Verify a dated reel still ends as expected.
3. If you have a save close to a WWR playlist change, cross that date
   in the same session and check that the next playlist takes over.
4. In the patched log, look for [WWRERMCompat] SCHEDULED only on dated
   ERM-A reels. Historical reels can log EXPIRED before queue. Compare
   against the baseline for zero-duration reschedules and repeated
   SandboxEntity.UpdateTransmission exceptions.

The companion does not change datasheets or saved transmission durations.
An already-missing station may need separate investigation. The July 1944
save that still listed CBS is useful for testing playback, but it has not
been established as a case of a WWR timer expiring.

Built against installed UBOAT 2026.1. Offline checks covered 937 ERM-A and
203 WWR rows, timer eligibility, stale callbacks, and duplicate expiry.
The in-game test is pending.

Source and history: https://github.com/Landomatic/UboatMods
