Happy Torpedo Time — 1.1.0-test
Author: Landomatic

This update preserves the supplied 1.0 Equipment balance and adds runtime handling for the intended maintenance-dependent ranges and information displays.

Install
Replace the existing Happy-TorpedoTime mod folder with the folder in this ZIP. Use one copy. Retain the current Workshop item identity when updating an existing published item; the supplied manifest's steamFileId was 0 and remains 0 here. With UBE, load Happy Torpedo Time after/below UBE. It replaces UBE Torpedo Tweaks; if you retain Tweaks, load UBE → UBE Torpedo Tweaks → Happy Torpedo Time last. Enable the mod's Reflection permission. Fully restart UBOAT after replacing Source or changing the enabled version.

Changes
- All 26 torpedo variants remain in Equipment and Types. Equipment cells, prices, dates, magnetic/pistol settings, damage, radii, cold ranges, and maintenance durations are preserved.
- Types speeds now use the Equipment speeds converted to km/h, including their summary speed field. Types' Range (km) field now records each variant's maximum configured range (for T1 this is the slow-running mode).
- Native warming changes reliability and cooldown but does not itself switch the stored range parameters. Runtime range reads and the two native launch methods now use the current maintained state and the paired Equipment definitions. The temporary calculated values are not attached as saved range modifiers.
- Inventory/shop descriptions show paired ranges and base settings. Tube, launch-screen, manual launch-lever, and fore/aft station tooltips show current effective ranges. Active tube tooltips refresh at most four times per second; inventory/shop information refreshes after a unit-setting change.
- Unit preferences are read live from the game's formatter/settings. Metric with knots keeps metric distances and nautical speeds. Nautical ranges display up to two decimals instead of silently truncating nautical miles. English explanatory labels accompany the game's localized unit suffixes.
- The old nested Data Sheets/Backup workbook is omitted from the active mod folder. Keep the original uploaded ZIP separately for rollback.

Balance
T1: 5/7.5/12 km unmaintained, 6/8/14 km maintained; base 12-hour benefit.
T2: 5 km unmaintained, 7.5 km maintained; base 4-hour benefit.
T3: 7.5 km in either state; base 4-hour benefit.
T5: 6.5 km in either state; base 4-hour benefit.
Lower base damage is paired with a wider blast radius. This is a balance tradeoff; no fixed total-damage increase is claimed.

UBE
The UBE Torpedo Tweaks companion sheet supplies the active torpedo overrides used for the UBE comparison. The newly supplied Tweaks file is byte-for-byte identical to the archived copy already audited. Happy Torpedo Time replaces those torpedo overrides in both Equipment and Types. The supplied base UBE Entities.xlsx has older torpedo entries under #Disabled Data; this update leaves those inactive and does not copy unrelated UBE Slots/Units.

Small in-game check
Use a separate test save. A broad test of cold running distances is not requested.
1. Start the game and confirm the log contains: [HappyTorpedoTime] Loaded 1.1.0-test. Confirm there are no HappyTorpedoTime compiler or installation errors.
2. Check a loaded T1 and T2 before/after maintenance. T1's current ranges should change from 5/7.5/12 km to 6/8/14 km; T2 should change from 5 km to 7.5 km. Check inventory/shop, launch-screen tube hover, physical tube/lever hover, and fore/aft station loadout.
3. Switch Metric, Metric with knots, and Nautical preferences. Reopen or hover each display. Examples: 7.5 km ≈ 4.05 nmi and 14 km ≈ 7.56 nmi. Switching units must change the display, not the physical range.
4. With base crew effects, let a maintained T2 expire, or use a test save near expiry, and confirm its displayed range returns to 5 km.
5. One clear-water test shot beyond the old 5 km limit but within the maintained T2's 7.5 km range confirms the new launch path. A matching T1 boundary shot is useful if convenient. Range figures here are data-backed; actual game behavior remains to be checked.

Validation
Static checks cover the supplied game assembly's public APIs, private field names, UI method signatures, and both launch methods' range-read instruction patterns; 26 paired data records; 468 range/state/source/multiplier cases; and numerical unit conversions. Equipment cells and all Types cells outside C, K, and S are preserved. Validation.json records the supplied assembly fingerprint and remaining limitations.
No C# compiler or running UBOAT process is available in this workspace. Game compilation, in-game layout, and actual firing/expiry behavior have not been executed. If runtime installation fails, its patches are rolled back and a clear error is logged; the datasheet changes remain active.

Credits
Sabre_One — Improved Torpedo Maintenance
Fokka — UBE Torpedo Tweaks
UBE team — earlier source balance data
Integration and update: Landomatic
