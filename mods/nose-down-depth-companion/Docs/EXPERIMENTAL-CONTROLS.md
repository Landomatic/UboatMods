# Experimental controls in 1.0.20

All three new options default OFF. Existing saved companion settings are retained. Install with a game restart; the new options then apply live. Compare from the same starting save because settings cannot rewind motion, flooding or crew assignments.

| Menu control | Scope | What it attempts |
| --- | --- | --- |
| Preserve VII dive-entry pitch | VIIB, VIIC, VIIC/41; ordinary dives | Add a short, bounded torque correction when the bow begins losing its initial down angle. |
| Precise PD arrival (nonhistorical) | II/VII ordinary 2–10 m orders; Type II requires a seated officer | Reduce overshoot and subsequent cycling using a velocity-preview internal target. |
| Allow planes assignment before diving | Player boat's planes station | Remove its surfaced-depth restriction so an officer can be ordered there before a dive order. Crew slots share station availability. |

These are attempts requiring gameplay validation, not established fixes. This version has compiled and passed offline checks but has not run inside UBOAT.

## Dive-entry assistance

The preceding VIIB run lost its initial bow-down pitch while Nose Down was still requesting 2°, before the companion's handover and engine change. The new supplement uses pitch error below 2° and an upward pitching rate to act before that dip completes. It leaves Nose Down's requested angle and integral untouched.

The supplement is limited to half the stock torque limit. Together with the earlier VII torque-cap option, total torque cannot be increased beyond 1.5 times the stock limit. It fades between 0.5 and 1 m deck depth and follows the existing trim handover scaling. It is inactive in Hold, ascent, crash/alarm dives, blows, kinematic physics, above 10× time compression, at pitch of 3° or more, or when the upstream requested angle is outside 0–4°. A readable matching stock torque law is required. It never sets boat rotation or position directly.

## Precise PD arrival and the historical caveat

Section 106 of the 1943 *Diving Regulations*, under **Test dive**, describes a deliberate 2–5 m depth overshoot, two pitching cycles to clear ballast-tank air, and a return to the order from below. This optional gameplay aid tries to reduce arrival overshoot instead; it does **not** reproduce that historical test-dive procedure. Turning it OFF retains the companion's existing behavior and does not create a historical procedure either.

Reference: https://www.uboatarchive.net/Diving/DivingRegulations.htm (§§106–108). That procedure alone does not establish that the game implements it or explain indefinite depth cycling.

The preview uses vertical velocity over a three-second look-ahead, capped at ±2 m of internal target offset and slewed at 0.75 m/s. It acts within 6 m of the order, with full weighting within 4 m, after deck immersion reaches 1.5 m. It does not change the visible order or apply direct position/velocity corrections. OrdinaryDive, OrdinaryDive/level and Hold are the permitted phases. Protected modes, new orders, disabled options and unsupported hulls release its state.

The existing shallow plane/trim stand-down must be enabled for the selected hull. While selected, this controller takes precedence over the II/VII officer approach brake, ascent brake, hold damper and deeper-PD bias; these offsets are not added together. Their saved settings are retained for when this option is OFF. Type IX and deeper orders retain their existing controls. The game's other depth-keeping mechanisms and environmental forces remain active, so exact arrival and a steady PD hold are not guaranteed.

## Pre-dive planes availability

The base game disables the planes station before a dive order. The earlier instruction to seat an officer first was therefore incorrect without a mod changing that restriction.

This option removes only that station's **Depth** disable flag on the player boat. Damage, docking, power and other flags, officer qualifications and occupied slots remain subject to the game's checks. The player still orders the officer normally; the mod neither teleports nor assigns anyone. Station availability is shared with its crew slots. The ordinary officer action retains its existing force modifier and depth-keeping behavior.

Turning the option or companion master OFF restores the normal surfaced restriction and may end an assignment. The station is refreshed on the next submarine physics update, including after loading a boat. Native UI, pathfinding and scheduling require in-game verification.

## Suggested comparisons

1. **Entry first:** same VIIB save, speed, ordinary PD order and 1× time. Keep the previous run's casing fix ON, VII torque cap ON at 1.35, officer brake OFF, and Seakeeping/Patch Profiler disabled. Turn only the new entry assist ON; leave precise arrival and pre-dive availability OFF. Assign the officer after ordering the dive, as the base game permits.
2. **Staffing separately:** enable pre-dive availability, order the officer to the station while surfaced, and check that he reaches it before ordering a dive. No automatic assignment is expected. Check that disabling the option restores the normal restriction.
3. **Arrival separately:** repeat from the same starting save with precise PD arrival ON. Keep other controls and staffing consistent with the comparison run. Assess entry pitch, first arrival and prolonged hold separately.

Retain diagnostics for comparisons. Do not interpret compilation or offline checks as a passing gameplay result.

Source and project license: https://github.com/Landomatic/UboatMods/tree/main/mods/nose-down-depth-companion
Companion by Landomatic; requires stock Nose Down by Kugler (https://github.com/Isamylov). No upstream files are included or edited.
