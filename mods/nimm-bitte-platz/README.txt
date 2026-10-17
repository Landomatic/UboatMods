Nimm Bitte Platz: Hydrophone Fix -- 1.3.2-beta-DIAG1
Author: Landomatic

The submerged hydrophone correction and ownership/surface guards are retained
from the supplied 1.3.1-beta. The listener's inside-boat flag is the only audio
setting written. This does not alter detection ranges, contacts or crew controls.

New diagnostics
START, CONFIG and READY identify the loaded build. STATION-START and STATION-STOP
record entry/exit. Debug records OVERRIDE-ACQUIRE/OVERRIDE-RELEASE and their reason.
Trace samples guard conditions, deck depth, camera/audio ownership and listener
flags. ERROR includes the first full exception; STOP summarizes counts at normal
application exit. Shutdown also releases an owned listener flag if still live.
The existing fail-closed behavior is retained for a failing station session.

Install
Fully exit UBOAT. Back up the current active folder outside active mod locations.
Replace it with this package's nimm-bitte-platz-hydrophone-fix folder, using one
enabled copy. Workshop ID 3810668782 is retained. Enable Reflection and IO.
Confirm [HydrophoneSurfaceNoiseFix][START] build=1.3.2-beta-DIAG1 followed by [READY].
No new career is required by the logging update.

See Docs/DIAGNOSTICS.md for NimmBittePlatzDiagnostics.txt and logging levels.
For a troublesome view-change sequence, set LogLevel = Trace, restart UBOAT,
repeat the sequence and send the output_log from that session after quitting.
Info is the normal default; there is no every-frame logging.

The original repair was reported clean on VIIB with starter GHG. Other hulls
and hydrophone equipment remain beta validation items. The pre-existing short
scratching/crackle also occurred with the mod disabled and is not targeted here.
HISTORY/README-1.3.1-beta.txt preserves the earlier testing notes and credits.
Source syntax and the native members/patch targets were checked here; this new
package was not compiled or run in UBOAT. See Validation.json.

Credits and license
Nimm Bitte Platz: Hydrophone Fix by Landomatic
https://steamcommunity.com/id/landomatic/myworkshopfiles/
The supplied LICENSE is preserved unchanged: CC BY-NC 4.0 with its specified
visible-credit requirement. Changes in this diagnostic build are listed above.
https://creativecommons.org/licenses/by-nc/4.0/
See Docs/GITHUB_LAYOUT.md for the suggested source repository layout.
