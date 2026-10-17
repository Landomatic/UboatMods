Happy Torpedo Time! -- 1.1.1-DIAG1
Author: Landomatic

Changes in this build
- START, CONFIG, READY, DATA, LAUNCH-BEFORE, LAUNCH-AFTER, ERROR and STOP markers.
- Configurable Info/Debug/Trace logging; sampled Trace and deduplicated exceptions.
- The launch-display tooltip background now resizes to its actual text using
  FloatingTooltip's native width, text measurement, padding and screen limiter.
- Current range and maintenance benefit information remain visible in the tooltip.
- The supplied Entities.xlsx and range calculation/launch replacement are unchanged.
  T2 remains 5 km unmaintained and 7.5 km warmed, with a base four-hour benefit.
- CC BY-NC 4.0 license and visible author credit follow the supplied Nimm Bitte
  Platz license template. Existing contributor credits remain below.

Install
Fully exit UBOAT. Back up the existing active mod folder outside active mod
locations, then replace it with this package's happy-torpedo-time folder.
Use one enabled copy and retain Workshop ID 3809600697. Load it after UBE and
any retained UBE Torpedo Tweaks. Enable Reflection and IO permissions.
Do not leave RP1 or older runtime source in the replacement folder.

First check
Confirm [HappyTorpedoTime][START] build=1.1.1-DIAG1, then [READY]. After equipment
is available, DATA records all paired Equipment ranges, and DATA-READY reports
validPairs=13. These are loaded Equipment values, not a claim that a projectile
has run its whole intended distance. Each launch logs actual launchedRange_m.
Hover a loaded T2 on the launch display. Its extra lines should now be inside
the enlarged background. Check with your usual UI scale and unit preference.
We cannot render UBOAT's live UI here; screenshot verification remains in-game.

See Docs/DIAGNOSTICS.md for config, logging tags and troubleshooting.
See Docs/GITHUB_LAYOUT.md for a simple source repository layout.
No new career is required by this update, which adds no saved range modifiers.
Keep periscope/depth testing separate from a torpedo range test.

Validation
Source syntax, supplied native members/patch targets and unchanged control
bodies were checked. The tooltip sizing matches the native game calculations.
The new packages were not compiled or executed in UBOAT here. Validation.json
records static checks; HISTORY keeps the prior build's documentation.

Credits and license
Happy Torpedo Time! by Landomatic
https://steamcommunity.com/id/landomatic/myworkshopfiles/
CC BY-NC 4.0: https://creativecommons.org/licenses/by-nc/4.0/ -- see LICENSE.
Changes in this diagnostic build are identified above. Redistribution or an
adaptation must retain visible credit as specified in LICENSE.
Sabre_One -- Improved Torpedo Maintenance
Fokka -- UBE Torpedo Tweaks
UBE team -- earlier source balance data
Integration and update: Landomatic
