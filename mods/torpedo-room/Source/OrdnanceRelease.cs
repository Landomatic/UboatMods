// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using UBOAT.Game.Core;
using UBOAT.Game.Core.Serialization;
using UnityEngine;
using UBOAT.Game.Scene;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Characters.Actions;
using Activator = UBOAT.Game.Scene.Characters.Activators.Activator;
namespace Landomatic.TorpedoRoom
{
    internal static class OrdnanceRelease
    {
        internal static bool Available;
        internal static void Install(Harmony harmony)
        {
            var target=AccessTools.Method(typeof(MainCamera),"SetFPPController");
            var prefix=AccessTools.Method(typeof(OrdnanceRelease),"BeforeCamera");
            try{harmony.Patch(target,prefix:new HarmonyMethod(prefix){priority=Priority.First});Available=true;}
            catch(Exception e){Available=false;try{harmony.Unpatch(target,prefix);}catch{}Debug.LogWarning("[TorpedoRoom] Deferred ordnance unavailable: "+e.Message);}
        }
        static void BeforeCamera(PlayableCharacter __0)
        {if(Runtime.Instance!=null)Runtime.Instance.OrdnanceCameraChanging(__0);}
    }
    // Pure timing gate: no task or camera side effects. Require continuous calm
    // state; never infer stability solely from a fixed delay after the first call.
    internal sealed class OrdnanceSettle
    {
        internal const float QuietSeconds=.5f,TimeoutSeconds=8;
        internal float Started,Changed,CalmSince;
        internal OrdnanceSettle(float now){Started=Changed=now;CalmSince=-1;}
        internal void Disturbed(float now){Changed=now;CalmSince=-1;}
        internal bool TimedOut(float now){return now-Started>=TimeoutSeconds;}
        internal bool Ready(float now,bool calm)
        {
            if(!calm){CalmSince=-1;return false;}
            if(CalmSince<0)CalmSince=now;
            return now-Changed>=QuietSeconds&&now-CalmSince>=QuietSeconds;
        }
    }
    public sealed partial class Runtime
    {
        sealed class DeferredOrdnance
        {
            internal PlayableCharacter Actor;internal Activator Activator;internal CharacterAction Action,OldAction;
            internal object Ship;internal OrdnanceSettle Gate;internal SavesManager Saves;
        }
        DeferredOrdnance deferredOrdnance;bool replayingOrdnance;
        internal bool DeferOrdnanceOrder(PlayableCharacter actor,Activator activator,CharacterAction action)
        {
            if(replayingOrdnance||Loading||!Active||!Config.Interface||ordnanceOwner==null||!ordnanceOwner.IsOpened||!object.ReferenceEquals(ordnanceShip,Ship)||!object.ReferenceEquals(actor,ordnanceActor))return false;
            if(!(action is LoadTorpedoesAction)&&!(action is WarmUpTorpedoAction))return false;
            var camera=MainCamera.Instance;
            if(camera==null||camera.CurrentMode!=CameraMode.Generic||camera.GenericController==null||!object.ReferenceEquals(camera.GenericController.Character,actor)||CurrentGroup!=ordnanceGroup)return false;
            // Requests at our inventory return a default/completed UniTask to its
            // fire-and-forget caller. No task is fabricated; retain this same object.
            if(deferredOrdnance!=null){if(!object.ReferenceEquals(deferredOrdnance.Action,action))try{action.CompleteAction();}catch{}Say("An ordnance order is already waiting for the station to release.");return true;}
            if(!OrdnanceRelease.Available){Say("Ordnance handoff unavailable; use the native tube interaction.");return false;}
            deferredOrdnance=new DeferredOrdnance{Actor=actor,Activator=activator,Action=action,OldAction=actor.Action,Ship=Ship,Gate=new OrdnanceSettle(Time.unscaledTime)};
            ordnanceWorkStarting=closeOrdnanceAfterOrder=true;
            
            try{
                deferredOrdnance.Saves=Get<SavesManager>();
                if(deferredOrdnance.Saves==null)throw new InvalidOperationException("save manager unavailable");
                deferredOrdnance.Saves.AddSaveBlockade(deferredOrdnance,new LocalizedString("Wait for the ordnance handoff.",LocalizedStringFlags.Unlocalized));
                camera.SetFPPController(actor,CameraTransitionType.Instant,false,0);Say("Releasing previous station before ordnance work...");}
            catch(Exception e){CancelDeferredOrdnance("camera release failed: "+e.Message,true);}
            return true;
        }
        internal void OrdnanceCameraChanging(PlayableCharacter actor)
        {
            var pending=deferredOrdnance;
            if(pending==null)return;
            if(!object.ReferenceEquals(actor,pending.Actor)){CancelDeferredOrdnance("camera selected another character",true);return;}
            pending.Gate.Disturbed(Time.unscaledTime);
            
        }
        void CancelDeferredOrdnance(string reason,bool releaseOrder)
        {
            var pending=deferredOrdnance;deferredOrdnance=null;
            if(pending==null)return;
            
            if(pending.Saves!=null)pending.Saves.RemoveSaveBlockade(pending);
            // A native carry order may already reserve its storage slots. Complete
            // only this never-started action on a valid world to release its order.
            if(releaseOrder&&!Loading&&object.ReferenceEquals(pending.Ship,Ship))try{pending.Action.CompleteAction();}catch(Exception e){Debug.LogWarning("[TorpedoRoom] Deferred order cleanup: "+e.Message);}
            Say("Ordnance order not started: "+reason+". Retry from the tube when ready.");
        }
        void TickDeferredOrdnance()
        {
            var pending=deferredOrdnance;if(pending==null)return;
            if(Loading||!object.ReferenceEquals(pending.Ship,Ship)){CancelDeferredOrdnance("world changed",false);return;}
            if(!Active||!Config.Interface||pending.Actor==null){CancelDeferredOrdnance("interface disabled or character unavailable",true);return;}
            var camera=MainCamera.Instance;
            if(camera==null){CancelDeferredOrdnance("camera unavailable",true);return;}
            if(Time.unscaledTime-pending.Gate.Started>.5f&&!camera.IsInTransition&&(camera.CurrentMode!=CameraMode.FPP||!pending.Actor.IsImpersonated)){CancelDeferredOrdnance("player left the handoff view",true);return;}
            if(pending.Gate.TimedOut(Time.unscaledTime)){CancelDeferredOrdnance("previous station did not settle",true);return;}
            var current=pending.Actor.Action;
            if(current!=null&&!object.ReferenceEquals(current,pending.OldAction)&&!(current is UseManuallyOperableDeviceAction)){
                CancelDeferredOrdnance("character received another task",true);return;
            }
            bool calm=camera.CurrentMode==CameraMode.FPP&&!camera.IsInTransition&&pending.Actor.IsImpersonated&&!pending.Actor.IsInteracting&&!pending.Actor.IsSwapping;
            if(!pending.Gate.Ready(Time.unscaledTime,calm))return;
            deferredOrdnance=null;replayingOrdnance=true;
            try{pending.Actor.StartAction(pending.Activator,pending.Action);Say("Native ordnance request submitted after station release.");}
            catch(Exception e){Debug.LogWarning("[TorpedoRoom] Deferred ordnance dispatch: "+e);try{pending.Action.CompleteAction();}catch{}Say("Native ordnance dispatch failed; see output_log.txt.");}
            finally{replayingOrdnance=false;if(pending.Saves!=null)pending.Saves.RemoveSaveBlockade(pending);}
        }
    }
}
