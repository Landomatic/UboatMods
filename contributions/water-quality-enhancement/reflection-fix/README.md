# Water Quality Enhancement: reflection compatibility fix

Fixes the ambiguous Harmony target for `Weather.RenderReflectionProbe` by explicitly selecting the no-argument method. It preserves the original visual settings: a minimum reflection-probe resolution of 512 and rendering every fourth call. Owner reports an in-game test with no reported errors; visual changes and performance remain unconfirmed.

Install Water Quality Enhancement first. Close UBOAT, back up its `Source/WaterQualityMod.cs`, then replace that file with the one in the ZIP. Preserve the installed manifest, preview and other files. Restart and check that the RenderReflectionProbe ambiguous-match startup error is gone. This is a replacement source file, not a standalone mod.

Original project: https://steamcommunity.com/sharedfiles/filedetails/?id=3744423004
Original supplied manifest credits: UBOAT Modder (Water Quality Enhancement 1.0.0).
Compatibility fix hosted by Landomatic: https://github.com/Landomatic/UboatMods
The supplied archive contains no explicit license. This contribution does not change upstream ownership or grant a new license for the original source.

Validation: compiled against the supplied game assemblies. ZIP CRC and equality with the compiled source checked. Owner reports an in-game run without reported errors; no follow-up log was supplied to verify patch installation. No game assemblies or private logs included.
