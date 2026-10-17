Engine Transition Glide -- 0.2.0 (draft, untested in game)
Author: Landomatic

What it does
On a diesel <-> electric swap the game cuts the outgoing engine's power to zero at once and
starts the incoming engine from zero with a slow ramp (about 5.6 s time constant, ~13 s to 90%).
This mod hands the incoming engine the thrust the outgoing one was actually producing, capped
at the incoming engine's own preset power for the ordered gear, and fades that floor out over
glideSeconds (default 4 s) while the game's own ramp rises underneath.
Stock behaviour is kept when: the incoming engine is disabled (damage, empty battery, no fuel,
diesels at depth, unmanned), stop is ordered, gears are blocked (docked), battery charging,
the order reverses direction, alarm/crash dive is on (skipDuringAlarm), or time compression > 5x.

Install
Copy the EngineTransitionGlide folder to
C:\Users\<you>\AppData\LocalLow\Deep Water Studio\UBOAT\Mods\EngineTransitionGlide\
(Manifest.json directly inside), enable it, clear ...\UBOAT\Temp. Look for
"[EngineTransitionGlide] initialized [v0.2.0]" and one "swap ..." line per engine change.

Modes (transitionMode)
speedMatch (default): at the swap the incoming engine starts at
  fraction = clamp(forward speed / its expected speed for the ordered gear, 0..1)
  of its setting (expected speed = the game's own surface/submerged table blended by depth, as
  ValidateTargetVelocity does). Never above its setting, its MaxPower, or what the outgoing engine
  was actually making. The game's ramp continues from there.
fade: v0.1.0 behaviour - carry the outgoing thrust and fade it out over glideSeconds.

Ramp (rampScale)
The game raises engine power by Lerp(current, target, fixedDeltaTime*0.18) every physics step for
every throttle increase, swap or not (decreases use 0.9): ~5.6 s time constant, ~13 s to 90%.
rampScale (1-5, default 1 = stock) speeds up increases only, never past the game's target. It
applies to the engine just swapped in until it reaches its setting; set applyRampScaleGlobally=true
for every throttle change.

Config: ...\UBOAT\EngineTransitionGlideConfig.txt (enabled, transitionMode, rampScale,
applyRampScaleGlobally, glideSeconds, skipDuringAlarm, logSwaps)

Credits and license
Engine Transition Glide by Landomatic
https://steamcommunity.com/id/landomatic/myworkshopfiles/
CC BY-NC 4.0: https://creativecommons.org/licenses/by-nc/4.0/ -- see LICENSE.
Redistribution or an adaptation must retain visible credit as specified in LICENSE.
