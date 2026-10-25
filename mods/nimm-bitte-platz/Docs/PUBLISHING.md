# Publishing 1.4.2

This package is prepared for validation and updating the existing Workshop item
**3810668782**. It has not been uploaded to Steam. The stable version name is a
numbering choice; see Validation.json for the actual test status.

1. Fully quit UBOAT. Back up the active folder outside the mod directories.
   Replace the whole local folder with `nimm-bitte-platz-hydrophone-fix` from
   `NimmBittePlatz_v1.4.2.zip`; enable only that copy and allow Reflection/IO.
2. Launch UBOAT and confirm `START` has `version=1.4.2 build=20261024.3`, followed
   by `READY`. Check for compilation or installation errors in the same log.
3. With Dusty enabled, open **Other Mods → Nimm Bitte Platz**. Confirm the three
   settings appear. At the submerged hydrophone, turn the correction off/on;
   check immediate release and correction on the next station update. Switch to
   Trace, then back to Info. Confirm Trace output stops. Restart and verify the
   saved values, then Reset to defaults and confirm on/Info/two seconds.
4. Recheck submerged station entry; map/section/orbit and officer-selection
   returns; captain FPP return; ascent/surface; station exit and re-entry. Check
   sound as well as the log. Test without Dusty enabled to confirm standalone
   config operation. Include a host/client multiplayer pass for that setup;
   this package has no multiplayer-specific code changes.
5. Check launcher precedence: fully quit, disable this mod in the launcher, then
   restart. The correction must not load or be activated by Dusty. Re-enable it
   in the launcher and restart before continuing.
6. Quit normally and confirm one `STOP`. Keep the output log for this build.
   Record the game, hull, equipment and Dusty version tested. Other hulls and
   equipment remain unverified until tested.
7. Update the existing Workshop item using this mod folder. Use the text in
   Workshop-Description.txt and Workshop-ChangeNotes.txt as the publishing copy.
   Keep the existing Workshop ID, preview, author credit and license.

No personal AppData config is bundled. A pre-existing DIAG config set to Trace
continues to request Trace until changed. Use Info for the normal publishing
smoke test; changing your config does not change subscribers' preferences.
