// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Collections.Generic;
using UBOAT.Game.Core.Data;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Interactions;
using UBOAT.Game.Scene.Items;
using UnityEngine;

namespace NoseDownCompanion
{
    // NCT8b: reads whether the diving-planes station is manned, via the same
    // public path vanilla uses for depth reports (PlayerShip.cs):
    //   ship.GetEquipment<DivingPlanesStation>().GetComponent<Interactable>().Workers
    // Workers = characters currently interacting with a performance-providing slot.
    // NCT8c FIX: an officer ordered to the planes runs ManageDivingPlanesStationAction,
    // which seats him in the station's "Officer" slot. That slot does not provide
    // performance (only slots named "*Work*" do), so the officer is NEVER in Workers -
    // NCT8b only ever saw the sailors and logged "/crew". The officer is in
    // Interactable.InteractingCharacters, and while seated the action sets
    // PlayableSubmarine.VerticalSteersForceScale scale-modifier "Officer" = 2
    // (1 while walking to/away from the slot) and ForcedDepthKeeping = true.
    // IsManned() now means "officer working the planes" (the 2x plane-force case
    // the ascent brake was built for); sailors-only is reported but not braked.
    internal static class PlanesManning
    {
        private static PlayerShip _ship;
        private static Interactable _interactable;
        private static bool _resolved;
        private static bool _failed;
        private static int _lastState;
        // NCT8d: latest "officer seated at the planes" state, refreshed every FixedUpdate.
        internal static bool OfficerSeated { get; private set; }
        private static bool _haveLast;
        private static int _loggedMode = -1; // 0 rate-only, 1 manned+rate
        private static float _nextRetry;

        // true = manning field resolved; false = brake falls back to rate-only.
        internal static bool Available
        {
            get { return _resolved && !_failed; }
        }

        internal static void Reset()
        {
            _ship = null;
            _interactable = null;
            _resolved = false;
            _failed = false;
            _haveLast = false;
            OfficerSeated = false;
            _loggedMode = -1;
            _nextRetry = 0f;
        }

        private static void Resolve(PlayerShip ship)
        {
            _ship = ship;
            _interactable = null;
            _resolved = false;
            _failed = false;
            _haveLast = false;
            try
            {
                var station = ship.GetEquipment<DivingPlanesStation>();
                if (station != null)
                    _interactable = ((Component)station).GetComponent<Interactable>();
                _resolved = _interactable != null && _interactable.Workers != null;
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogWarning("[NoseDownCompanion.Planes] manning lookup failed: " + ex.Message);
            }
            int mode = Available ? 1 : 0;
            if (mode != _loggedMode)
            {
                _loggedMode = mode;
                Debug.Log("[NoseDownCompanion.Planes] ascent-brake gate mode="
                    + (Available ? "MANNED+RATE (DivingPlanesStation Interactable.Workers)" : "RATE-ONLY (manning not resolvable)"));
            }
        }

        // Returns true/false when known; null when the manning field is unavailable.
        internal static bool? IsManned(PlayerShip ship)
        {
            if (ship == null)
                return null;
            if (!ReferenceEquals(ship, _ship))
            {
                Resolve(ship);
                _nextRetry = Time.time + 2f;
            }
            else if (!_resolved && !_failed && Time.time >= _nextRetry)
            {
                // Station may not exist yet right after load; retry every 2 s.
                _nextRetry = Time.time + 2f;
                Resolve(ship);
            }
            if (!Available)
            {
                OfficerSeated = false;
                return null;
            }
            try
            {
                if (_interactable == null)
                {
                    _failed = true;
                    return null;
                }
                List<InteractiveCharacter> workers = _interactable.Workers;
                List<InteractiveCharacter> interacting = _interactable.InteractingCharacters;
                PlayableCharacter officer = FindOfficer(interacting);
                float forceScale = OfficerForceScale(ship);
                // Seated officer: in the Officer slot AND his 2x force modifier active.
                bool officerOn = officer != null && forceScale > 1.5f;
                bool crewOn = workers != null && workers.Count > 0;
                int state = (officerOn ? 2 : 0) + (crewOn ? 1 : 0);
                if ((!_haveLast || state != _lastState) && CompanionConfig.TraceLogging)
                {
                    _haveLast = true;
                    _lastState = state;
                    string op = officerOn
                        ? ((UnityEngine.Object)officer).name + "/officer"
                        : (officer != null ? ((UnityEngine.Object)officer).name + "/officer(not seated)" : "none");
                    Debug.Log("[NoseDownCompanion.Planes] manned=" + officerOn
                        + " operator=" + op
                        + " crew=" + Describe(workers)
                        + " officerForceScale=" + forceScale.ToString("F1")
                        + " (occupant change; brake/II-clamp gate=officer)");
                }
                _haveLast = true;
                _lastState = state;
                OfficerSeated = officerOn;
                return officerOn;
            }
            catch (Exception ex)
            {
                _failed = true;
                OfficerSeated = false;
                Debug.LogWarning("[NoseDownCompanion.Planes] manning read failed, switching to RATE-ONLY: " + ex.Message);
                return null;
            }
        }

        private static PlayableCharacter FindOfficer(List<InteractiveCharacter> chars)
        {
            if (chars == null)
                return null;
            for (int i = 0; i < chars.Count; i++)
            {
                var pc = chars[i] as PlayableCharacter;
                if (pc != null && pc.IsOfficer)
                    return pc;
            }
            return null;
        }

        // 0 = no officer action; 1 = action running (walking); 2 = officer seated.
        private static float OfficerForceScale(PlayerShip ship)
        {
            Modifier m = ship.VerticalSteersForceScale.GetScaleModifier("Officer");
            return m != null ? m.Value : 0f;
        }

        private static string Describe(List<InteractiveCharacter> workers)
        {
            if (workers == null || workers.Count == 0)
                return "none";
            var parts = new List<string>();
            for (int i = 0; i < workers.Count; i++)
            {
                var c = workers[i];
                if (c == null)
                    continue;
                var pc = c as PlayableCharacter;
                string role = pc == null ? "npc" : (pc.IsOfficer ? "officer" : "crew");
                parts.Add(((UnityEngine.Object)c).name + "/" + role);
            }
            return parts.Count == 0 ? "unknown" : string.Join(",", parts.ToArray());
        }
    }
}
