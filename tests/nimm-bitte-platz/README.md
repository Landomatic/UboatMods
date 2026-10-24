# Nimm Bitte Platz offline tests

Run `dotnet run --project tests/nimm-bitte-platz/NimmBittePlatz.OfflineTests.csproj --configuration Release` from the repository root.

The harness compiles the actual mod source as C# 5 and executes it against small
test doubles for Unity, Harmony and UBOAT. It covers config migration/validation,
Dusty's reflected provider shape and live setters, listener ownership/guard
transitions, failure rollback, shutdown and diagnostic sampling.

This is **stub-reference compilation and behavioral testing**, not compilation
against the installed game. It does not validate Unity object lifetimes, real
Harmony patch installation, Dusty's actual UI, audio playback or multiplayer.
No game assemblies or third-party implementation source are included.

Test source is by Landomatic under the existing project [LICENSE](../../mods/nimm-bitte-platz/LICENSE).
