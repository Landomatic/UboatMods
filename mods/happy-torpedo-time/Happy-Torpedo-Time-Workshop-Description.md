# Happy Torpedo Time

**By Landomatic**

Spend more time setting up your attack and less time chasing maintenance timers.

Happy Torpedo Time combines longer maintenance windows, selected range improvements, and a broader blast footprint in one torpedo overhaul. Steam torpedoes retain their maintenance benefit for a base 12 hours; electric torpedoes retain theirs for a base 4 hours. Crew bonuses can affect the actual duration.

Damage is tuned around a tradeoff: lower base damage than the supplied vanilla build, with a much larger damage radius. The wider blast is intended to spread damage across nearby compartments. Results depend on the target and where the torpedo strikes.

The 1.1 test release adds range information to inventory/shop descriptions, launch-screen tube tooltips, physical tube and launch-lever tooltips, and fore/aft station loadouts. Loaded-tube displays show the current maintained/unmaintained state. Distances and speeds follow Metric, Metric with knots, or Nautical preferences; nautical ranges retain fractional precision.

| Setting | Supplied vanilla | Improved Maintenance | UBE Torpedo Tweaks | Happy Torpedo Time |
| --- | --- | --- | --- | --- |
| T1 maintenance benefit | 35 min | 12 h | 4 h | 12 h |
| T2–T5 maintenance benefit | 35 min | 30 min | 4 h | 4 h |
| T1 unmaintained range, fast / medium / slow | 5 / 7.5 / 12 km | 5 / 7.5 / 12 km | 5 / 7.5 / 12 km | 5 / 7.5 / 12 km |
| T1 maintained range, fast / medium / slow | 5 / 7.5 / 12 km | 6 / 8 / 14 km | 5 / 7.5 / 12 km | 6 / 8 / 14 km |
| T2 unmaintained → maintained range | 5 → 5 km | 4.5 → 7.5 km | 5 → 5 km | 5 → 7.5 km |
| T3 unmaintained → maintained range | 7.5 → 7.5 km | 4.5 → 7.5 km | 7.5 → 7.5 km | 7.5 → 7.5 km |
| T5 unmaintained → maintained range | 6.5 → 6.5 km | 4.5 → 5.7 km | 6.5 → 6.5 km | 6.5 → 6.5 km |
| Base dud settings, T1 / T2 / T3 / T5 (Equipment) | 19 / 30 / 16 / 14% | 19 / 30 / 16 / 14% | 19 / 30 / 16 / 14% | 19 / 30 / 16 / 14% |
| Damage, Pi1 / Pi1.1 / Pi3 / Pi2 / Pi4 (Equipment) | 7 / 7 / 7 / 7 / 7 | 4.9 / 4.9 / 4.9 / 4.9 / 4.9 | 4.9 / 5.39 / 6.1 / 6.1 / 5.39 | 5 / 5.4 / 6.1 / 6.1 / 5.4 |
| DamageRadius, Equipment / Types | 6.5 / 5.8 | Equipment: 40; no Types override | 40 / 35 | 40 / 40 |
| CrewDamage, Equipment / Types | 0.8 / 0.9 | Equipment: 0.8; no Types override | 0.8 / 0.9 | 0.9 / 0.9 |

**Comparison notes**

The table compares the supplied vanilla Entities.xlsx, the supplied Improved Torpedo Maintenance B129 workbook, the supplied UBE Torpedo Tweaks version 1.0 workbook, and Happy Torpedo Time. These are verified data entries, not measured running-distance or total-damage benchmarks.

UBE Torpedo Tweaks supplies the companion torpedo-stat overrides used for the UBE comparison above. The newly supplied Tweaks workbook matches the archived copy exactly. The base UBE Entities.xlsx references torpedoes in Slots and has older torpedo entries under #Disabled Data; those disabled entries are not active comparison values. Happy Torpedo Time supplies both Equipment and Types overrides and replaces the Tweaks torpedo balance.

Happy Torpedo Time uses the same base dud, damage, crew-damage, radius, and range settings in its paired Equipment and Types records, with the appropriate speed units for each sheet. Vanilla and the older Tweaks workbook have different dud settings in Types: 25 / 40 / 20 / 15%. Base dud settings are not guaranteed failure rates; maintenance, difficulty, crew effects, pistol choice, and impact geometry can affect the outcome.

T1 ranges are listed from fastest to slowest running mode. T3 and T5 retain the same range in both maintenance states; maintaining them still affects their reliability. Damage and radius figures are internal game settings.

**Installation and compatibility**

With Uboat Expanded, load Happy Torpedo Time after/below UBE. Happy already replaces the torpedo data supplied by UBE Torpedo Tweaks, so Tweaks is not required. If you retain Tweaks, use this order: UBE → UBE Torpedo Tweaks → Happy Torpedo Time last. Avoid another torpedo overhaul loading after Happy. Improved Torpedo Maintenance is also a data source, not a required installation.

The 1.1 runtime features require the mod's Reflection permission. This is a test release: game compilation, UI layout, maintenance transitions, and firing behavior still require an in-game check.

**Credits and data sources**

Improved Torpedo Maintenance — Sabre_One
https://steamcommunity.com/sharedfiles/filedetails/?id=2548679647

UBE Torpedo Tweaks — Fokka
https://steamcommunity.com/sharedfiles/filedetails/?id=3040977385

Uboat Expanded — the UBE team, for source balance data carried through the earlier Tweaks workbook.

Selected data from these sources was adapted and combined by Landomatic. This release is maintained independently. Source credits are separate from required items.


Diagnostic build 1.1.1-DIAG1: startup/shutdown and launch diagnostics; tooltip background sizing repair.
Happy Torpedo Time! by Landomatic. Licensed CC BY-NC 4.0: https://creativecommons.org/licenses/by-nc/4.0/ . See LICENSE for visible-credit requirements.
