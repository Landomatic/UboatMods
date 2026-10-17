using System;
using System.Collections.Generic;
using DWS.Common.InjectionFramework;
using UBOAT.Game.Core;
using UBOAT.Game.Sandbox;
using UBOAT.Game.Sandbox.Missions;
using UnityEngine;

namespace WolfpackAtlantik
{
    // Lifecycle adapter; called by the existing one-second runtime cadence. Native mission IDs
    // make assignments stable through save/load. Entity eligibility is checked by the world owner.
    public static class WolfpackPatrolService
    {
        private static WolfpackCampaignState _bound;
        private static PlayerCareer _career;
        private static float _nextAllocationCheck;
        private static bool _sameMissionStationRecoveryPending;

        internal static Mission CurrentNativePatrol
        {
            get
            {
                if (_career == null) _career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
                Mission mission = _career != null ? _career.CurrentAssignment as Mission : null;
                return mission is PatrolAssignment || mission is GroupPatrolAssignment ? mission : null;
            }
        }

        public static void Tick(WolfpackCampaignState state)
        {
            if (state == null) return;
            if (_bound != state)
            {
                _bound = state;
                _career = null;
                _nextAllocationCheck = 0f;
                _sameMissionStationRecoveryPending = false;
            }
            if (_career == null) _career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
            if (_career == null) return;
            WolfpackPatrolAccess.Initialize(state, false);
            if (!WolfpackPatrolHistory.Tick(state)) return;
            PlayerPatrolServiceRecord p = state.patrolService;
            string home = _career.FlotillaDock != null ? _career.FlotillaDock.Name : WorldBoatAdapter.CurrentPortId;
            if (!string.IsNullOrEmpty(home) && !string.Equals(p.homePortId, home, StringComparison.OrdinalIgnoreCase))
            { p.homePortId = home; WolfpackSaveStore.Save(state); }
            Mission rawAssignment = _career.CurrentAssignment as Mission;
            bool nativePatrolAssignment = WolfpackPatrolHistory.IsNativePatrolAssignment(rawAssignment);
            Mission mission = nativePatrolAssignment ? rawAssignment : null;
            long missionId = WolfpackPatrolHistory.StablePatrolId(mission);
            if (mission != null && p.currentPatrolActive && p.currentPatrolJournalIndex == 0
                && mission.Id > 0L && p.currentMissionId == mission.Id)
            {
                // Upgrade a saved active manifest's identity without rerolling assignments.
                p.currentMissionId = missionId;
                p.currentPatrolJournalIndex = WolfpackPatrolHistory.JournalIndex(mission);
                WolfpackSaveStore.Save(state);
            }
            // Non-patrol assignments (including story/theatre/special missions) and a temporarily
            // null CurrentAssignment are not evidence that the underlying Wolfpack patrol ended.
            // Preserve the participant/station manifest until vanilla positively resolves another
            // PatrolAssignment/GroupPatrolAssignment. A different resolved patrol identity is the
            // replacement signal handled below.
            if (p.currentPatrolActive && !nativePatrolAssignment)
            {
                return;
            }
            if (p.currentPatrolActive && mission != null && missionId > 0L && missionId != p.currentMissionId)
            {
                // The accepted-report patch marks success synchronously before this cadence.
                // Replacement/abandonment is deliberately not counted as a completed patrol.
                RestorePatrolStations(state);
                WolfpackPatrolAccess.CompletePatrol(state, p.currentMissionId, false, true, GameTimeAdapter.CurrentTicks);
                WolfpackSaveStore.Save(state);
                WolfpackCommanderWindow.NotifyStateChanged();
            }
            if (mission != null && string.IsNullOrEmpty(p.currentPatrolName) && p.currentMissionId == missionId)
                p.currentPatrolName = MissionName(mission);
            if (mission == null || missionId <= 0L) return;
            if (p.currentMissionId != missionId)
            {
                // Knowing a native assignment exists is distinct from having a safe roster.
                // Briefings remain usable during delayed entity/navigation binding.
                p.currentMissionId = missionId; p.currentPatrolActive = true;
                p.currentPatrolJournalIndex = WolfpackPatrolHistory.JournalIndex(mission);
                p.currentPatrolName = MissionName(mission);
                p.currentPatrolGrid = null; p.currentPatrolDeparted = false;
                p.allocationFinalized = false; p.currentParticipantBoatIds.Clear();
                p.stationAssignments.Clear();
                if (p.theatreExcludedBoatIds == null) p.theatreExcludedBoatIds = new List<string>();
                p.theatreExcludedBoatIds.Clear();
                p.allocationIntegrityNote = "Patrol assigned; waiting for eligible existing boats and valid water stations.";
                _nextAllocationCheck = 0f;
                WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged();
            }
            if (p.currentMissionId == missionId)
            {
                if (p.currentPatrolActive && !p.currentPatrolDeparted && !WorldBoatAdapter.IsPlayerDocked)
                { p.currentPatrolDeparted = true; WolfpackSaveStore.Save(state); }
                if (!p.currentPatrolActive)
                {
                    if (!TryRecoverSameMissionPatrol(state, mission, missionId))
                    {
                        return;
                    }
                    p = state.patrolService;
                }

                // A recovery marker survives a quit/relaunch by leaving allocationFinalized=false
                // while retaining the participant manifest.  Normal unallocated patrols have no
                // participants yet, so this cannot steal the ordinary allocation path.
                if (!_sameMissionStationRecoveryPending && p.currentPatrolActive && !p.allocationFinalized
                    && p.currentParticipantBoatIds != null && p.currentParticipantBoatIds.Count > 0
                    && (p.stationAssignments == null || p.stationAssignments.Count == 0))
                {
                    _sameMissionStationRecoveryPending = true;
                }

                if (p.allocationFinalized && !_sameMissionStationRecoveryPending)
                {
                    if (Time.unscaledTime >= _nextAllocationCheck)
                    {
                        _nextAllocationCheck = Time.unscaledTime + 15f;
                        ReconcilePatrolTheatre(state);
                    }
                    return;
                }
            }
            if (Time.unscaledTime < _nextAllocationCheck) return;
            _nextAllocationCheck = Time.unscaledTime + (_sameMissionStationRecoveryPending ? 1f : 15f);
            WorldBoatAdapter.PatrolFormationPlan formation;
            if (!WorldBoatAdapter.TryBuildPatrolFormation(out formation) || formation == null || !formation.missionAssigned) return;

            if (_sameMissionStationRecoveryPending)
            {
                if (!CanRecoverAllPatrolStations(state, formation)) return;
                AssignPatrolStations(state, formation);
                p.allocationFinalized = true;
                _sameMissionStationRecoveryPending = false;
                p.allocationIntegrityNote = AppendIntegrityNote(p.allocationIntegrityNote,
                    "Recovered the existing patrol manifest after a transient save-load assignment gap; companions retained and stations rebuilt.");
                WolfpackSaveStore.Save(state);
                WolfpackCommanderWindow.NotifyStateChanged();
                return;
            }
            int quota = WolfpackEstimatedPatrolAllocation.PatrolQuota(GameTimeAdapter.CurrentCampaignDate);
            WolfpackAtlantikRuntime runtime = WolfpackAtlantikRuntimeHost.ActiveRuntime;
            if (runtime != null && runtime.FlotillaRoster.PreparePatrolReserve(quota))
            {
                p.allocationIntegrityNote = "Preparing a compatible local boat from the fleet establishment; no cross-theatre transfer.";
                WolfpackCommanderWindow.NotifyStateChanged();
                return;
            }
            var eligible = new List<RecruitedBoatRecord>();
            int excluded = 0;
            string firstExclusion = null;
            if (state.boats != null)
                foreach (RecruitedBoatRecord boat in state.boats)
                {
                    string reason = "bounded candidate pool";
                    // A boat already dropped from this patrol by the theatre check is not offered
                    // again; re-selecting it produced the 15-second remove/re-add loop in v1.0.0.
                    if (boat != null && p.theatreExcludedBoatIds != null && p.theatreExcludedBoatIds.Contains(boat.persistentBoatId))
                    { excluded++; if (firstExclusion == null) firstExclusion = "station outside the native patrol area"; continue; }
                    if (eligible.Count < WolfpackPatrolAccess.MaximumAssignmentPool
                        && WorldBoatAdapter.CanAssignToPlayerPatrol(boat, out reason)) eligible.Add(boat);
                    else { excluded++; if (firstExclusion == null) firstExclusion = reason; }
                }
            // A delayed world bind must not permanently freeze an empty participant manifest.
            if (eligible.Count == 0 && quota > 0)
            {
                string waiting = "No eligible boats available in this patrol theatre. " + firstExclusion
                    + " The fleet reserve requires a native-supported local origin; no cross-theatre transfer is permitted.";
                if (p.allocationIntegrityNote != waiting)
                {
                    p.allocationIntegrityNote = waiting;
                    WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged();
                }
                return;
            }
            string note = "Date-based patrol estimate, not an exact historical local roster. Eligible active boats and commissioned fleet reserves; "
                + excluded + " excluded" + (firstExclusion != null ? " (including " + firstExclusion + ")" : string.Empty) + ".";
            if (WolfpackPatrolAccess.BeginPatrol(state, missionId, formation.centerGrid, home, eligible, quota,
                WolfpackPreferences.AlwaysIncludeRecruitedSkippersOnPatrol, excluded, note))
            {
                AssignPatrolStations(state, formation);
                WolfpackSaveStore.Save(state);
                WolfpackCommanderWindow.NotifyStateChanged();
            }
        }

        private static List<WorldBoatAdapter.PatrolFormationCell> BuildPatrolStationCells(WorldBoatAdapter.PatrolFormationPlan formation)
        {
            var stations = new List<WorldBoatAdapter.PatrolFormationCell>();
            // A PATROL LINE across the convoy route, not an escort formation. Real packs strung
            // boats out at intervals so that one of them would sight the convoy; they converged only
            // after a contact report, which the base game's own wolfpack machinery now handles.
            // Stations therefore run north-south, across the east-west Atlantic traffic, starting two
            // squares out so nobody shares the player's own square.
            // The formation grid is 9x9 and CellAt rejects anything outside -4..+4, so the picket
            // offsets must live inside that. Outermost first, and the x sweep is the OUTER loop so
            // the first boats form a clean north-south line before any stagger is used.
            // MEASURED: one formation cell is a depth-3 naval square, 166.98 x 100.19 km in the
            // BD band. Mission areas are authored in depth-2 squares of 500.94 x 300.56 km, so a
            // typical box of four of them is 1002 x 601 km = exactly 6 x 6 cells. Offsets of +/-3
            // therefore span the assigned area; +/-4 would throw boats clean outside it. The old
            // stations all used offsets of 1, which covered a third of the box in each axis and
            // about a ninth of its area - which is what "the unit is too small" looked like.
            int[] pickets = { 3, -3, 2, -2, 1, -1 };
            int[] stagger = { 0, 1, -1, 2, -2 };
            foreach (int x in stagger) foreach (int y in pickets)
            {
                if (x == 0 && y == 0) continue;
                WorldBoatAdapter.PatrolFormationCell cell = formation.CellAt(x, y);
                if (cell == null || !cell.valid || cell.landMask != 0 || string.IsNullOrEmpty(cell.gridName)) continue;
                // Stations are deliberately NOT limited to the mission's own squares any more. A
                // patrol line has to straddle the approaches, so the boats sit across the grids
                // ADJACENT to the assigned area rather than crowding inside the box with the player.
                if (stations.Contains(cell)) continue;
                stations.Add(cell);
            }
            return stations;
        }

        private static void AssignPatrolStations(WolfpackCampaignState state, WorldBoatAdapter.PatrolFormationPlan formation)
        {
            PlayerPatrolServiceRecord p = state.patrolService;
            List<WorldBoatAdapter.PatrolFormationCell> stations = BuildPatrolStationCells(formation);
            var assigned = new List<string>();
            foreach (RecruitedBoatRecord boat in WolfpackPatrolAccess.Participants(state))
            {
                if (assigned.Count >= stations.Count) break;
                string reason;
                if (!WorldBoatAdapter.CanAssignToPlayerPatrol(boat, out reason)) continue;
                // Preserve explicit pre-update commands; selection does not cancel saved return,
                // transfer or other valid legacy orders that the user already issued.
                if (WolfpackPatrolAccess.IsGrandfathered(state, boat))
                { assigned.Add(boat.persistentBoatId); continue; }
                WorldBoatAdapter.PatrolFormationCell cell = stations[assigned.Count];
                var station = new PatrolStationAssignmentRecord
                {
                    boatId = boat.persistentBoatId, previousOrders = CopyOrders(boat.orders),
                    previousJoinPlayerNextPatrol = boat.joinPlayerNextPatrol
                };
                BoatOrders orders = CopyOrders(boat.orders);
                orders.patrolMode = PatrolMode.FixedGrid; orders.patrolGrid = cell.gridName;
                orders.patrolOffsetX = cell.offsetX; orders.patrolOffsetY = cell.offsetY;
                orders.returnToPortRequested = false; orders.supplyExchangeRequested = false;
                orders.issuedGameTicks = GameTimeAdapter.CurrentTicks;
                boat.orders = orders; boat.orderSequence++;
                station.assignedOrders = CopyOrders(orders); station.issuedOrderSequence = boat.orderSequence;
                // This is only departure intent; WorldBoatAdapter owns actual undocking.
                boat.joinPlayerNextPatrol = true;
                p.stationAssignments.Add(station);
                assigned.Add(boat.persistentBoatId);
                WorldBoatAdapter.ApplyOrders(boat);
            }
            p.currentParticipantBoatIds = assigned;
            if (assigned.Count < p.requestedAllocation)
                p.allocationIntegrityNote += " Only " + assigned.Count + " valid existing boats/water stations were available; no replacements spawned.";
        }

        private static bool TryRecoverSameMissionPatrol(WolfpackCampaignState state, Mission mission, long missionId)
        {
            if (state == null || state.patrolService == null || mission == null || missionId <= 0L) return false;
            PlayerPatrolServiceRecord p = state.patrolService;
            if (p.currentPatrolActive || p.currentMissionId != missionId) return false;
            if (!p.currentPatrolDeparted || !p.allocationFinalized) return false;
            if (p.currentParticipantBoatIds == null || p.currentParticipantBoatIds.Count == 0) return false;
            if (p.stationAssignments != null && p.stationAssignments.Count != 0) return false;
            if (p.previousMissionId != p.currentMissionId || p.previousPatrolSuccessful) return false;
            if (!SameParticipantSet(p.currentParticipantBoatIds, p.previousParticipantBoatIds)) return false;

            // Recover only the narrow false-completion fingerprint: CompletePatrol(false) copied
            // the live participant manifest to previousParticipantBoatIds, marked the same mission
            // as previous, cleared its stations, and left that native assignment itself alive.
            p.currentPatrolActive = true;
            p.allocationFinalized = false;
            if (p.completedPatrolJournalIndices != null && p.currentPatrolJournalIndex > 0)
                p.completedPatrolJournalIndices.Remove(p.currentPatrolJournalIndex);
            if (p.newlyCompletedPatrolJournalIndices != null && p.currentPatrolJournalIndex > 0)
                p.newlyCompletedPatrolJournalIndices.Remove(p.currentPatrolJournalIndex);
            state.playerPatrolsCompleted = WolfpackPatrolHistoryPolicy.CompletedPatrols(state);
            p.allocationIntegrityNote = AppendIntegrityNote(p.allocationIntegrityNote,
                "Recovered a still-running native patrol that was falsely closed during save-load assignment restoration; existing companions retained.");
            _sameMissionStationRecoveryPending = true;
            _nextAllocationCheck = 0f;
            WolfpackPatrolHistory.RearmAssignmentBarrierForRecovery();
            WolfpackSaveStore.Save(state);
            WolfpackCommanderWindow.NotifyStateChanged();
            WolfpackAtlantikLog.Info("Recovered active patrol " + missionId
                + " after save-load state mismatch; existing participant manifest retained.");
            return true;
        }

        private static bool CanRecoverAllPatrolStations(WolfpackCampaignState state, WorldBoatAdapter.PatrolFormationPlan formation)
        {
            if (state == null || state.patrolService == null || state.boats == null || formation == null) return false;
            PlayerPatrolServiceRecord p = state.patrolService;
            if (p.currentParticipantBoatIds == null || p.currentParticipantBoatIds.Count == 0) return false;
            List<WorldBoatAdapter.PatrolFormationCell> stations = BuildPatrolStationCells(formation);
            int neededStations = 0;
            for (int i = 0; i < p.currentParticipantBoatIds.Count; i++)
            {
                string id = p.currentParticipantBoatIds[i];
                RecruitedBoatRecord found = null;
                for (int j = 0; j < state.boats.Count; j++)
                {
                    RecruitedBoatRecord boat = state.boats[j];
                    if (boat != null && string.Equals(boat.persistentBoatId, id, StringComparison.Ordinal))
                    { found = boat; break; }
                }
                if (found == null) return false;
                string reason;
                if (!WorldBoatAdapter.CanAssignToPlayerPatrol(found, out reason)) return false;
                if (!WolfpackPatrolAccess.IsGrandfathered(state, found)) neededStations++;
            }
            return stations.Count >= neededStations;
        }

        private static bool SameParticipantSet(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count == 0 || a.Count != b.Count) return false;
            var set = new HashSet<string>(a, StringComparer.Ordinal);
            if (set.Count != a.Count) return false;
            for (int i = 0; i < b.Count; i++) if (!set.Contains(b[i])) return false;
            return true;
        }

        private static string AppendIntegrityNote(string existing, string note)
        {
            if (string.IsNullOrEmpty(existing)) return note;
            if (existing.IndexOf(note, StringComparison.Ordinal) >= 0) return existing;
            return existing.TrimEnd() + " " + note;
        }

        private static void ReconcilePatrolTheatre(WolfpackCampaignState state)
        {
            PlayerPatrolServiceRecord p = state.patrolService;
            int removed = 0;
            // Repair old manifests without rerolling valid companions, rebasing, spawning or
            // teleporting boats. Newer explicit player/other-mod orders are never replaced.
            foreach (RecruitedBoatRecord boat in new List<RecruitedBoatRecord>(WolfpackPatrolAccess.Participants(state)))
            {
                string reason;
                bool? eligible = WorldBoatAdapter.PatrolTheatreEligibility(boat, out reason);
                if (!eligible.HasValue) continue;
                if (eligible.Value)
                {
                    // v1.2 picket stations sit on the grids AROUND the mission area on purpose, so under
                    // the v2 patrol model a station outside the native squares is the design, not damage.
                    // Evicting it here sent picket boats home 15 s after sailing (the 5-then-2 symptom again).
                    if (state.patrolModelVersion >= 2) continue;
                    if (boat.orders == null || WorldBoatAdapter.NativePatrolContainsStation(boat.orders.patrolGrid)) continue;
                    // Grandfathered and befriended boats that were never given one of our stations
                    // keep their own orders by design; their grid is not ours to police. Only a
                    // service-issued station that has fallen outside the whitelist is repaired.
                    if (!OwnsStation(p, boat)) continue;
                    reason = "Station is outside the current native patrol-area whitelist.";
                }
                // v1.2: a home port that does not list this grid no longer ejects the boat. That
                // rule was level-design data, not history, and it is what made a patrol open with
                // five or more boats and snap back to two fifteen seconds later. Reach is judged by
                // whether the boat can be given a valid station at all, not by its berth.
                else continue;
                if (p.theatreExcludedBoatIds == null) p.theatreExcludedBoatIds = new List<string>();
                if (!p.theatreExcludedBoatIds.Contains(boat.persistentBoatId)) p.theatreExcludedBoatIds.Add(boat.persistentBoatId);
                for (int i = p.stationAssignments.Count - 1; i >= 0; i--)
                {
                    PatrolStationAssignmentRecord station = p.stationAssignments[i];
                    if (station == null || station.boatId != boat.persistentBoatId) continue;
                    if (WolfpackPatrolTheatre.OwnsStationOrder(station, boat.orders)
                        && boat.operationalState != BoatOperationalState.Lost)
                    {
                        boat.orders = CopyOrders(station.previousOrders);
                        // A previous mission-relative order would recreate the same bad crossing.
                        // Return only our automatic assignment to its unchanged home base.
                        boat.orders.returnToPortRequested = true;
                        boat.orders.supplyExchangeRequested = false;
                        boat.orders.issuedGameTicks = GameTimeAdapter.CurrentTicks;
                        boat.joinPlayerNextPatrol = false;
                        boat.orderSequence++;
                        WorldBoatAdapter.ApplyOrders(boat);
                    }
                    p.stationAssignments.RemoveAt(i);
                }
                p.currentParticipantBoatIds.Remove(boat.persistentBoatId);
                removed++;
                WolfpackAtlantikLog.Info("Removed incompatible patrol assignment for " + boat.boatNumber + ": " + reason);
            }
            if (removed == 0) return;
            if (p.currentParticipantBoatIds.Count == 0) p.allocationFinalized = false;
            p.allocationIntegrityNote += " Removed " + removed + " incompatible theatre assignments; valid companions retained, no replacement boats spawned.";
            WolfpackSaveStore.Save(state);
            WolfpackCommanderWindow.NotifyStateChanged();
        }

        internal static string MissionName(Mission mission)
        {
            if (mission == null) return null;
            try
            {
                string name = mission.LongName != null ? mission.LongName.ToString() : null;
                if (string.IsNullOrEmpty(name) && mission.Name != null) name = mission.Name.ToString();
                return string.IsNullOrEmpty(name) ? null : name.Trim();
            }
            catch { return null; }
        }

        private static bool OwnsStation(PlayerPatrolServiceRecord p, RecruitedBoatRecord boat)
        {
            if (p == null || p.stationAssignments == null || boat == null) return false;
            for (int i = 0; i < p.stationAssignments.Count; i++)
            {
                PatrolStationAssignmentRecord station = p.stationAssignments[i];
                if (station != null && station.boatId == boat.persistentBoatId) return true;
            }
            return false;
        }

        internal static void RestorePatrolStations(WolfpackCampaignState state)
        {
            if (state == null || state.patrolService == null || state.boats == null || state.patrolService.stationAssignments == null) return;
            foreach (PatrolStationAssignmentRecord station in state.patrolService.stationAssignments)
                foreach (RecruitedBoatRecord boat in state.boats)
                {
                    if (boat == null || station == null || !string.Equals(boat.persistentBoatId, station.boatId, StringComparison.Ordinal)) continue;
                    // Return/combat/manual orders issued since allocation always win. Restore
                    // only the exact service-owned order, never reset a newer captain decision.
                    if (boat.operationalState != BoatOperationalState.Returning && boat.operationalState != BoatOperationalState.Lost
                        && boat.orders != null && !boat.orders.returnToPortRequested && station.assignedOrders != null
                        && boat.orders.issuedGameTicks == station.assignedOrders.issuedGameTicks
                        && boat.orders.patrolMode == station.assignedOrders.patrolMode
                        && string.Equals(boat.orders.patrolGrid, station.assignedOrders.patrolGrid, StringComparison.Ordinal))
                    {
                        boat.orders = CopyOrders(station.previousOrders); boat.orderSequence++;
                        boat.joinPlayerNextPatrol = station.previousJoinPlayerNextPatrol;
                        WorldBoatAdapter.ApplyOrders(boat);
                    }
                    break;
                }
            state.patrolService.stationAssignments.Clear();
        }

        private static BoatOrders CopyOrders(BoatOrders source)
        {
            if (source == null) return new BoatOrders();
            return new BoatOrders { posture = source.posture, patrolMode = source.patrolMode, patrolGrid = source.patrolGrid,
                patrolOffsetX = source.patrolOffsetX, patrolOffsetY = source.patrolOffsetY, shadowDistanceKm = source.shadowDistanceKm,
                convoyResponse = source.convoyResponse, rulesOfEngagement = source.rulesOfEngagement,
                returnToPortRequested = source.returnToPortRequested, supplyExchangeRequested = source.supplyExchangeRequested,
                issuedGameTicks = source.issuedGameTicks };
        }

        public static bool MeetSkipper(RecruitedBoatRecord boat, out string message)
        {
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            if (!WolfpackPatrolAccess.CanRecruit(state))
            { message = "The Operations Chief can arrange skipper introductions after " + WolfpackPatrolAccess.KnownSkippersThreshold + " successful patrols."; return false; }
            if (boat == null || state == null || (!IsCampaignBoat(state, boat) && !IsReserveRecord(state, boat)))
            { message = "That skipper is not on the flotilla ledger."; return false; }
            if (!IsPresentInPort(boat))
            { message = "Both boats must be safely in the same port for a skipper meeting."; return false; }
            if (WolfpackPatrolAccess.IsKnownFriend(state, boat))
            { message = "You already know " + CaptainName(boat) + ". No payment is required for introductions."; return false; }
            string port = WorldBoatAdapter.CurrentPortDisplayName ?? "port";
            bool changed = WolfpackPatrolAccess.Meet(state, boat, GameTimeAdapter.CurrentTicks, "Introduced by the Operations Chief at " + port);
            if (!changed) { message = "No introduction could be recorded for " + CaptainName(boat) + "."; return false; }
            WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged();
            message = "Introduced to " + CaptainName(boat) + " (" + (boat.boatNumber ?? "U-boat")
                + "). Now a known skipper: his boat remains under BdU orders; a shared patrol or a letter can make him a friend.";
            return true;
        }

        // A skipper you have already met, or who has just sailed with you, can be written to at
        // any base. This is the post-patrol path to a friendship without needing a shared berth.
        public static bool SendLetter(RecruitedBoatRecord boat, out string message)
        {
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            string reason;
            if (!WolfpackSkipperRelationship.CanWriteLetter(state, boat, out reason)) { message = reason; return false; }
            int cost = WolfpackPatrolAccess.RecruitmentCost(state, boat);
            if (!Befriend(boat, "letter:" + GameTimeAdapter.CurrentTicks, "Befriended by letter", out message)) return false;
            message = "Your letter to " + CaptainName(boat) + " (" + (boat.boatNumber ?? "U-boat")
                + ") was answered. He will keep in touch: befriended for " + (cost == 0 ? "no favour" : cost + " RP")
                + ". His boat stays under BdU orders.";
            return true;
        }

        // Shared by letters and the mess: befriend a skipper the player already knows. The
        // roster's Commission owns the RP charge, the command-capacity check and the flag.
        public static bool Befriend(RecruitedBoatRecord boat, string evidence, string reason, out string message)
        {
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            WolfpackAtlantikRuntime runtime = WolfpackAtlantikRuntimeHost.ActiveRuntime;
            if (state == null || runtime == null || runtime.FlotillaRoster == null)
            { message = "The flotilla ledger is not available."; return false; }
            if (!WorldBoatAdapter.IsPlayerDocked)
            { message = "Friendships are sealed in port, after the patrol."; return false; }
            if (boat == null || WolfpackPatrolAccess.IsFriendly(boat))
            { message = "You already correspond with that skipper."; return false; }
            if (!IsCampaignBoat(state, boat))
            {
                // Reserve establishment skippers join the active ledger when befriended.
                if (!IsReserveRecord(state, boat)) { message = "That skipper is not on the flotilla ledger."; return false; }
                state.boats.Add(boat);
                if (!runtime.FlotillaRoster.Commission(boat)) { state.boats.Remove(boat); message = runtime.FlotillaRoster.LastActionMessage; return false; }
                WorldBoatAdapter.ActivateFleetBoat(boat);
            }
            else if (!runtime.FlotillaRoster.Commission(boat)) { message = runtime.FlotillaRoster.LastActionMessage; return false; }
            WolfpackPatrolAccess.RecordFriendshipService(state, boat, evidence, 1, GameTimeAdapter.CurrentTicks, reason);
            SkipperFriendshipRecord friend = WolfpackPatrolAccess.Friendship(state, boat, true);
            WolfpackPatrolAccess.AddRapport(friend, 10);
            WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged();
            message = runtime.FlotillaRoster.LastActionMessage;
            return true;
        }

        // Introductions happen at a shared berth. Establishment records and boats without a
        // bound world entity are met at their own base; live boats must actually be alongside.
        public static bool IsPresentInPort(RecruitedBoatRecord boat)
        {
            if (boat == null || boat.operationalState == BoatOperationalState.Lost) return false;
            if (WorldBoatAdapter.CanRefitBoat(boat)) return true;
            if (!WorldBoatAdapter.IsPlayerDocked || boat.operationalState != BoatOperationalState.InPort) return false;
            if (!boat.isVirtualRecordOnly && !string.IsNullOrEmpty(boat.linkedWorldEntityId)) return false;
            return SameBase(boat.homePortId, WorldBoatAdapter.CurrentPortId);
        }

        internal static bool SameBase(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(NormalizeBase(a), NormalizeBase(b), StringComparison.Ordinal);
        }

        private static string NormalizeBase(string value)
        {
            string v = value.Trim().ToLowerInvariant();
            if (v.StartsWith("port of ", StringComparison.Ordinal)) v = v.Substring(8);
            if (v.EndsWith(" port", StringComparison.Ordinal)) v = v.Substring(0, v.Length - 5);
            if (v.EndsWith(" harbour", StringComparison.Ordinal)) v = v.Substring(0, v.Length - 8);
            return v.Replace(" ", string.Empty);
        }

        internal static bool IsCampaignBoat(WolfpackCampaignState state, RecruitedBoatRecord boat)
        { return state != null && state.boats != null && boat != null && state.boats.Contains(boat); }

        internal static bool IsReserveRecord(WolfpackCampaignState state, RecruitedBoatRecord boat)
        {
            if (state == null || state.fleetRoster == null || state.fleetRoster.entries == null || boat == null) return false;
            for (int i = 0; i < state.fleetRoster.entries.Count; i++)
            {
                FleetRosterEntry entry = state.fleetRoster.entries[i];
                if (entry != null && ReferenceEquals(entry.boat, boat)) return true;
            }
            return false;
        }

        // Skippers who can be met at this berth right now, plus those already known here.
        // Reserve establishment records count as berthed at their home base.
        public static List<RecruitedBoatRecord> SkippersInPort(WolfpackCampaignState state, bool includeKnown)
        {
            List<RecruitedBoatRecord> result = new List<RecruitedBoatRecord>();
            if (state == null || !WorldBoatAdapter.IsPlayerDocked) return result;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            if (state.boats != null)
                foreach (RecruitedBoatRecord boat in state.boats)
                {
                    if (boat == null || WolfpackPatrolAccess.IsFriendly(boat) || !IsPresentInPort(boat)) continue;
                    if (!includeKnown && WolfpackPatrolAccess.IsKnownFriend(state, boat)) continue;
                    if (ids.Add(boat.persistentBoatId)) result.Add(boat);
                }
            if (state.fleetRoster != null && state.fleetRoster.entries != null)
                foreach (FleetRosterEntry entry in state.fleetRoster.entries)
                {
                    if (entry == null || entry.boat == null || entry.activated || !WolfpackFleetPolicy.Available(entry)) continue;
                    if (!SameBase(entry.boat.homePortId, WorldBoatAdapter.CurrentPortId)) continue;
                    if (!includeKnown && WolfpackPatrolAccess.IsKnownFriend(state, entry.boat)) continue;
                    if (ids.Add(entry.boat.persistentBoatId)) result.Add(entry.boat);
                }
            return result;
        }

        public static bool PurchasePatrolFavour(out string message)
        {
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            if (!WolfpackPatrolAccess.CanRecruit(state) || !WorldBoatAdapter.IsPlayerDocked)
            { message = "Discuss patrol companions with the Operations Chief in port after " + WolfpackPatrolAccess.KnownSkippersThreshold + " successful patrols."; return false; }
            if (WolfpackPatrolAccess.FavourLevel(state) >= WolfpackPatrolAccess.MaximumFavourLevel)
            { message = "Your patrol-companion request has full headquarters support. You may nominate one befriended skipper as your regular partner in port."; return false; }
            if (!GameEconomyAdapter.TrySpend(0, 1)) { message = GameEconomyAdapter.LastFailure; return false; }
            state.patrolService.headquartersFavourLevel++;
            WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged();
            message = "Headquarters liaison request recorded (" + state.patrolService.headquartersFavourLevel
                + "/3). Known skippers are more likely on the next patrol; the current assignment is unchanged.";
            return true;
        }

        public static bool DesignateBuddy(RecruitedBoatRecord boat, out string message)
        {
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            if (state == null || !WolfpackPatrolAccess.IsFriendly(boat) || state.boats == null || !state.boats.Contains(boat)
                || !WorldBoatAdapter.IsPlayerDocked || WolfpackPatrolAccess.FavourLevel(state) < WolfpackPatrolAccess.MaximumFavourLevel)
            { message = "With all three headquarters liaison requests approved, nominate one befriended skipper in port."; return false; }
            state.patrolService.permanentBuddyBoatId = boat.persistentBoatId;
            state.patrolService.permanentBuddyChosen = true;
            WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged();
            message = CaptainName(boat) + " is your preferred patrol buddy from the next assignment. Repairs, losses and other mission reservations still take priority.";
            return true;
        }

        // Call only after an authoritative, completed player assistance objective identifies its
        // beneficiary. Automatic sighting ticks are not player assistance and must not call this.
        public static bool RecordAssistance(RecruitedBoatRecord boat, string objectiveId, bool rescue)
        {
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            if (!WolfpackPatrolAccess.HasActivePatrol(state) || string.IsNullOrEmpty(objectiveId)) return false;
            bool changed = WolfpackPatrolAccess.RecordFriendshipService(state, boat,
                (rescue ? "rescue:" : "contact:") + WolfpackPatrolAccess.ActivePatrolId(state) + ":" + objectiveId, rescue ? 2 : 1);
            if (changed) { WolfpackSaveStore.Save(state); WolfpackCommanderWindow.NotifyStateChanged(); }
            return changed;
        }

        internal static string CaptainName(RecruitedBoatRecord boat)
        { return boat != null && boat.captainSnapshot != null && !string.IsNullOrEmpty(boat.captainSnapshot.displayName)
            ? boat.captainSnapshot.displayName : boat != null ? boat.boatNumber ?? "the skipper" : "the skipper"; }
    }
}
