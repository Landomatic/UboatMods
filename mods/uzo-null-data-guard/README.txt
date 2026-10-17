UZO NULL DATA GUARD - 0.2.0 TEST

Purpose
-------
Experimental workaround for the managed exception recorded during UZO entry:
PeriscopeController.StartController, IL offset 0x003b1, game assembly MVID
96ae717b-5625-410e-a5bd-7f2dfd4fbfb5.

The vanilla camera checks whether Equipment.Data.Name equals "Binocular".
When Data is null, that field read throws. This test keeps the normal Data.Name
result for healthy equipment; only when Data is absent does it use the existing
equipment TypeName. This lets the camera distinguish the UZO ("Targeting Sight")
from handheld binoculars ("Binocular") without reading through a null object.

It does not rebuild missing equipment-table entries, change illumination,
replace sight textures, suppress arbitrary exceptions, or edit save files.
It targets one camera check and one optics UI initialization check. It adds
limited logs on first entry per scope and when optics UI initialization returns.
There is no dependency on Dust's patch or the hydrophone fix.

Install and test
----------------
1. Fully exit UBOAT. Keep a separate backup of your campaign save.
2. Extract the UZO-Null-Data-Guard folder to:
   %USERPROFILE%\AppData\LocalLow\Deep Water Studio\UBOAT\Mods\
   The resulting file must be:
   ...\Mods\UZO-Null-Data-Guard\Manifest.json
3. Enable "UZO Null Data Guard - Test" in the UBOAT launcher.
4. Keep the mod list and settings from your latest run that could load the
   campaign, including Reticle Illumination switched off. Add only this test.
   If you already have a loadable setup with Dust's whole bundle disabled,
   this guard can be used in that setup too.
5. Restart, load the affected save, and enter the UZO once.
6. Send output_log.txt from this run, whether entry succeeds or fails.

Replacing 0.1.0
---------------
Fully exit UBOAT and replace the existing UZO-Null-Data-Guard folder with this
version. Keep a single copy enabled; do not install both test versions.
Author and Harmony ownership references now use Landomatic.

What changed in 0.2.0
--------------------
output_log(10).txt confirms that the camera guard works, but the live database
has 498 entries and lacks both Targeting Sight and Binocular. Optics UI
OnEnable still throws. Its vanilla code reads Equipment.Data.Name again to
choose the observation/attack elevation icon before completing UI setup.
This version guards that second name read using the same type-name fallback.
It preserves the original behavior when equipment data exists. It logs UI
return and a remaining UI failure without suppressing exceptions.

The UI failure is consistent with incomplete or stale sight controls, but
its precise runtime throwing instruction is not present in the patched stack.
This is a targeted next test, not proof that every visual problem shares that
cause. The database definitions remain missing and need separate investigation.

Keep Dust's Reticle Illumination OFF for the first test. Compare UZO,
handheld binoculars and periscope entry, then send the new log. If you later
turn illumination on, distinguish the module's enable switch from F's
separate global lit/unlit toggle.

Expected startup messages:
[UzoNullDataGuard] Matched one camera equipment-name check.
[UzoNullDataGuard] Matched one UI initialization equipment-name check.
[UzoNullDataGuard] Loaded 0.2.0-test...

The first binocular/UZO camera-name check also reports:
- equipment object/type and whether its live Data is null;
- underlying reticle colour field and effective colour returned by patches;
- live equipment database entry count and whether the equipment type exists;
- Harmony patch owners for reticle colour and optics entry.

If compilation fails or the exact instruction match is absent, the guard is
inactive; the log should identify this. The game compiles the included source.

Successful UZO entry would show that the guard avoids this particular exception.
It does not by itself establish that equipment data or the entire campaign is
healthy. Use a separate save slot if you later choose to save after testing.

Remove
------
Fully exit UBOAT, disable this test in the launcher, then restart.
Removing its local folder after disabling is optional. No save cleanup code or
configuration files are installed by this mod.

Validation limits
-----------------
C# syntax parsed successfully. Referenced public game symbols, exact faulting
assembly identity, both matched IL sequences, branch placement and stack
contract were checked against the supplied game DLL. Static source inspection
confirmed that the guard makes no save-file, colour, texture, equipment-data,
event-subscription or scene-component writes.

No full compilation against Unity/Harmony libraries or in-game runtime test
of 0.2.0 was available here. The earlier 0.1.0 camera guard was confirmed
to load and run in the supplied output_log(10).txt. This is an experimental test build, not a confirmed fix
for the process crash or a repaired save.

See ARCHIVE.md for the archival test status and included scope.
