using System;
using System.Collections.Generic;
using DWS.Common.InjectionFramework;
using UBOAT.Game.Core;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Sandbox;
using UBOAT.Game.Sandbox.Missions;

namespace WolfpackAtlantik
{
    // Vanilla completes the mission before showing the report animation, then accepts rewards
    // afterwards. CompletionFlagsChanged does not raise StateChanged. Read the same persisted
    // mission register used by CareerOverviewUI, never a guessed PlayerCareer assignment total.
    internal static class WolfpackPatrolHistory
    {
        private static WolfpackCampaignState _bound;
        private static SavesManager _saves;
        private static GameState _game;
        private static MissionsRegister _missions;
        private static bool _dirty = true;
        private static bool _loadFinalizing;
        private static bool _postLoadAssignmentBarrier;
        private static bool _assignmentResolutionWaitLogged;

        internal static void RequestReconciliation() { _dirty = true; }

        // During save restoration, native assignment identity can lag behind the persisted mission
        // register. Non-patrol assignments may also temporarily overlay the ordinary patrol without
        // abandoning it. Only a positively resolved patrol assignment can prove patrol identity.
        internal static bool IsNativePatrolAssignment(Mission mission)
        {
            return mission is PatrolAssignment || mission is GroupPatrolAssignment;
        }

        // A previously corrupted state may already be inactive before this process loads, so the
        // normal campaign-bind barrier cannot arm. Same-mission recovery re-arms assignment
        // resolution before persisted completion flags are trusted again.
        internal static void RearmAssignmentBarrierForRecovery()
        {
            _postLoadAssignmentBarrier = true;
            _assignmentResolutionWaitLogged = false;
            _dirty = true;
        }

        internal static int JournalIndex(Mission mission)
        {
            PatrolAssignment patrol = mission as PatrolAssignment;
            if (patrol != null) return patrol.JournalMissionIndex;
            GroupPatrolAssignment group = mission as GroupPatrolAssignment;
            return group != null ? group.JournalMissionIndex : 0;
        }

        internal static long StablePatrolId(Mission mission)
        { return WolfpackPatrolHistoryPolicy.StablePatrolId(JournalIndex(mission)); }

        internal static bool IsCompletedPatrol(Mission mission)
        {
            if (mission == null) return false;
            IAssignmentMission assignment = mission as IAssignmentMission;
            return WolfpackPatrolHistoryPolicy.IsCompletedPatrolEvidence(
                mission is PatrolAssignment || mission is GroupPatrolAssignment,
                assignment != null && mission.ParentMission == null, mission.State == MissionState.Completed,
                assignment != null && assignment.AssignmentObjectiveState == AssignmentObjectiveState.Completed,
                (mission.CompletionFlags & MissionCompletionFlags.RewardAccepted) != 0, JournalIndex(mission));
        }

        // False means that scene/save restoration is not ready; the caller must not mutate
        // patrol membership while the native register is being emptied or reconstructed.
        internal static bool Tick(WolfpackCampaignState state, bool importHistory = true)
        {
            if (state == null) return false;
            if (_bound != state)
            {
                _bound = state;
                _dirty = true;
                PlayerPatrolServiceRecord boundPatrol = state.patrolService;
                if (boundPatrol != null && boundPatrol.currentPatrolActive && boundPatrol.currentPatrolJournalIndex > 0)
                {
                    _postLoadAssignmentBarrier = true;
                    _assignmentResolutionWaitLogged = false;
                }
            }
            if (_saves == null)
            {
                _saves = InjectionFramework.Instance.GetInstance<SavesManager>();
                if (_saves != null)
                {
                    _saves.LoadingStarted += OnLoadingStarted;
                    _saves.Loaded += OnLoaded;
                }
            }
            if (_game == null) _game = InjectionFramework.Instance.GetInstance<GameState>();
            if (_missions == null) _missions = ScriptableObjectSingleton.LoadSingleton<MissionsRegister>();
            if (_saves == null || _game == null || _missions == null || _saves.IsLoading || _loadFinalizing
                || _game.CurrentState != GameState.State.Gameplay)
            { _dirty = true; return false; }
            // RuntimeHost can still hold the previous campaign until its next identity tick.
            // Never import one skipper's native history into another campaign's JSON.
            if (_dirty && !string.Equals(state.campaignId, CampaignIdentityAdapter.CurrentKey(), StringComparison.Ordinal)) return false;
            if (_dirty && importHistory) Reconcile(state, 0);
            return true;
        }

        internal static void ReportAccepted(Mission mission, bool newlyAccepted)
        {
            // Child/side-mission rewards must not prematurely finalize a patrol manifest.
            if (!IsCompletedPatrol(mission)) return;
            WolfpackCampaignState state = WolfpackAtlantikRuntimeHost.ActiveState;
            WolfpackCareerStage previousStage = WolfpackPatrolAccess.Stage(state);
            if (!Tick(state, false)) return;
            Reconcile(state, newlyAccepted ? JournalIndex(mission) : 0);
            if (WolfpackPatrolAccess.Stage(state) > previousStage)
                WolfpackNativeUI.ShowToast("The Operations Chief has recorded wider responsibilities: "
                    + WolfpackResponsibilityProgression.Describe(state).Title + ". Discuss your duties in port.");
        }

        private static void Reconcile(WolfpackCampaignState state, int newlyAcceptedJournalIndex)
        {
            WolfpackPatrolAccess.Initialize(state, false);
            var completed = new List<int>();
            Mission activeCompleted = null;
            PlayerPatrolServiceRecord p = state.patrolService;

            // On save load, PlayerCareer.CurrentAssignment can become usable after MissionsRegister
            // already exposes persisted completion/reward flags. Preserve the saved live journal until
            // native assignment identity positively resolves, then compare before importing history.
            PlayerCareer career = ScriptableObjectSingleton.LoadSingleton<PlayerCareer>();
            Mission currentAssignment = career != null ? career.CurrentAssignment as Mission : null;
            bool currentIsPatrol = IsNativePatrolAssignment(currentAssignment);
            int currentJournalIndex = currentIsPatrol ? JournalIndex(currentAssignment) : 0;
            bool currentPatrolIdentityResolved = currentIsPatrol && currentJournalIndex > 0;
            bool postLoadBarrier = _postLoadAssignmentBarrier && p.currentPatrolActive && p.currentPatrolJournalIndex > 0;
            bool explicitCurrentPatrolCompletion = newlyAcceptedJournalIndex > 0
                && p.currentPatrolJournalIndex == newlyAcceptedJournalIndex;

            // A live Wolfpack patrol is preserved through any unresolved or non-patrol assignment.
            // Story, theatre and special assignments can temporarily own PlayerCareer.CurrentAssignment
            // without semantically replacing the underlying patrol. Do not infer abandonment from the
            // assignment's type (or from null); wait for a positively resolved PatrolAssignment or
            // GroupPatrolAssignment and compare its stable identity instead.
            if (p.currentPatrolActive && !currentPatrolIdentityResolved && !explicitCurrentPatrolCompletion)
            {
                if (currentAssignment != null && !_assignmentResolutionWaitLogged)
                {
                    _assignmentResolutionWaitLogged = true;
                    WolfpackAtlantikLog.Info("Assignment " + currentAssignment.GetType().Name
                        + " does not positively replace active patrol journal " + p.currentPatrolJournalIndex
                        + "; patrol history import deferred until patrol identity resolves.");
                }
                _dirty = true;
                return;
            }

            foreach (Mission mission in _missions)
            {
                if (!IsCompletedPatrol(mission)) continue;
                int journalIndex = JournalIndex(mission);
                if (postLoadBarrier && newlyAcceptedJournalIndex != journalIndex && currentJournalIndex > 0
                    && currentJournalIndex == p.currentPatrolJournalIndex
                    && journalIndex == p.currentPatrolJournalIndex)
                {
                    continue;
                }
                completed.Add(journalIndex);
                if (p.currentPatrolActive && (p.currentPatrolJournalIndex > 0
                    ? p.currentPatrolJournalIndex == journalIndex
                    : p.currentMissionId == StablePatrolId(mission) || mission.Id > 0L && p.currentMissionId == mission.Id))
                    activeCompleted = mission;
            }
            _postLoadAssignmentBarrier = false;
            _assignmentResolutionWaitLogged = false;
            bool changed = WolfpackPatrolHistoryPolicy.Reconcile(state, completed, newlyAcceptedJournalIndex);
            if (activeCompleted != null)
            {
                WolfpackPatrolService.RestorePatrolStations(state);
                changed |= WolfpackPatrolAccess.CompletePatrol(state, p.currentMissionId, true, false, GameTimeAdapter.CurrentTicks);
            }
            _dirty = false;
            if (!changed) return;
            WolfpackSaveStore.Save(state);
            WolfpackCommanderWindow.NotifyStateChanged();
        }

        private static void OnLoadingStarted()
        {
            _loadFinalizing = true;
            _dirty = true;
            _postLoadAssignmentBarrier = true;
            _assignmentResolutionWaitLogged = false;
        }
        private static void OnLoaded(Queue<Action> finalizers)
        {
            // SavesManager lowers IsLoading BEFORE its Loaded subscribers/finalizers run.
            // Import only after that native restoration boundary, not from a partial list.
            _loadFinalizing = true;
            _postLoadAssignmentBarrier = true;
            _assignmentResolutionWaitLogged = false;
            finalizers.Enqueue(delegate { _loadFinalizing = false; _dirty = true; });
        }
    }
}
