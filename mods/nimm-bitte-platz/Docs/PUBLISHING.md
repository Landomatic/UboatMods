# Publishing 1.4.1

This package is prepared for validation and updating the existing Workshop item
**3810668782**. It has not been uploaded to Steam. The stable version name is a
numbering choice; see Validation.json for the actual test status.

1. Fully quit UBOAT. Back up the active folder outside the mod directories.
   Replace the whole local folder with `nimm-bitte-platz-hydrophone-fix` from
   `NimmBittePlatz_v1.4.1.zip`; enable only that copy and allow Reflection/IO.
2. Launch UBOAT and confirm `START` has `version=1.4.1 build=20261024.2`, followed
   by `READY`. Check for compilation or installation errors in the same log.
3. With Dusty enabled, open **Other Mods → Nimm Bitte Platz**. Confirm only
   Logging level and Trace interval appear, with Reset to defaults available.
   Switch to Trace, then back to Info and confirm Trace output stops. Restart
   and verify the saved values; reset should restore Info/two seconds. If an old
   1.4.0 config has Enabled=false, confirm the hydrophone correction still works.
4. Recheck submerged station entry; map/section/orbit and officer-selection
   returns; captain FPP return; ascent/surface; station exit and re-entry. Check
   sound as well as the log. Test without Dusty enabled to confirm standalone
   config operation. Include a host/client multiplayer pass for that setup;
   this package has no multiplayer-specific code changes.
5. Quit normally and confirm one `STOP`. Keep the output log for this build.
   Record the game, hull, equipment and Dusty version tested. Other hulls and
   equipment remain unverified until tested.
6. Update the existing Workshop item using this mod folder. Use the text in
   Workshop-Description.txt and Workshop-ChangeNotes.txt as the publishing copy.
   Keep the existing Workshop ID, preview, author credit and license.

No personal AppData config is bundled. A pre-existing DIAG config set to Trace
continues to request Trace until changed. Use Info for the normal publishing
smoke test; changing your config does not change subscribers' preferences.
