Run actual MenuIntegration.cs, CleanupFeedback.cs RoomExit.cs, RoomOrdnance.cs and RoomHandoff.cs against Fixture.cs using the Roslyn/coreclr offline runner. Fixtures emulate Dusty restoration/persistence and Unity UI only; no live rendering/event-system or save test. 59 assertions. Supplied third-party source is inspected locally but not copied into this archive.

.28 adds inventory Closed/Escape guard/cleanup and null-character dispatch tests. Fixtures cannot prove native AI task continuation or event ordering in game.
