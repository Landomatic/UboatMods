Run actual MenuIntegration.cs, CleanupFeedback.cs RoomExit.cs, RoomOrdnance.cs and RoomHandoff.cs against Fixture.cs using the Roslyn/coreclr offline runner. Fixtures emulate Dusty restoration/persistence and Unity UI only; no live rendering/event-system or save test. 75 assertions. Supplied third-party source is inspected locally but not copied into this archive.

.28 adds inventory Closed/Escape guard/cleanup and null-character dispatch tests. Fixtures cannot prove native AI task continuation or event ordering in game.

.29 adds OrdnanceDispatch.cs and operator-bound inventory / native camera-handoff scoping and deferred-close tests. Actual character task eligibility, stage animations and completion require in-game tests.
