// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
// Independent UI adapter; upstream MP code is neither copied nor distributed.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.UI;
using UBOAT.Game.UI.Periscope;
using UnityEngine;
namespace Landomatic.TorpedoRoom
{
    internal static class MpScopeFire
    {
        static bool supported,opening,releasing,suspended;
        static int closeGuards;
        static object station;
        static TorpedoLaunchersUI owner;
        static bool autoOpened;
        static float retryAt,errorAt;
        internal static bool Active {get{return supported&&Runtime.Active&&!Runtime.Loading&&Runtime.MpScopeFeuer&&Bridge.RestrictedScopes&&Runtime.ViewingScope!=null;}}
        // Called only after Bridge verifies the supplied MP version (1.11.5).
        internal static void Install(Harmony harmony,Type stations)
        {
            if(supported||stations==null)return;
            var added=new List<KeyValuePair<MethodInfo,MethodInfo>>();
            try{
                Patch(harmony,added,AccessTools.Method(stations,"BeforeTubesOpen"),"OpenCheck");
                Patch(harmony,added,AccessTools.Method(stations,"AfterPeriscopeLateUpdate"),"BeginScope", "AfterScope","EndScope");
                Patch(harmony,added,AccessTools.Method(stations,"CloseTubesOffStation"),"OffStation");
                Patch(harmony,added,AccessTools.Method(typeof(TorpedoLaunchersUI),"Close"),"BeforeClose");
                // Native Open still owns lifecycle/listeners. Opening this display
                // must not change the fire-control source or preparation values,
                // including deferred native Start callbacks after activation.
                foreach(var name in new[]{"ValidateBearingSource","OnSalvoSpreadChanged","UpdateProgramTargetingValues"})
                    Patch(harmony,added,AccessTools.Method(typeof(TorpedoLaunchersUI),name),"OpeningWrites");
                supported=true;
            }catch(Exception e){
                foreach(var pair in added)try{harmony.Unpatch(pair.Key,pair.Value);}catch{}
                Debug.LogWarning("[TorpedoRoom] Optional MP Feuer display unavailable: "+e.GetBaseException().Message);
            }
        }
        static void Patch(Harmony h,List<KeyValuePair<MethodInfo,MethodInfo>> added,MethodInfo target,string prefix,string postfix=null,string finalizer=null)
        {
            if(target==null)throw new MissingMethodException("Required native/MP UI method missing.");
            var pre=AccessTools.Method(typeof(MpScopeFire),prefix);added.Add(new KeyValuePair<MethodInfo,MethodInfo>(target,pre));
            MethodInfo post=postfix==null?null:AccessTools.Method(typeof(MpScopeFire),postfix),end=finalizer==null?null:AccessTools.Method(typeof(MpScopeFire),finalizer);
            if(post!=null)added.Add(new KeyValuePair<MethodInfo,MethodInfo>(target,post));if(end!=null)added.Add(new KeyValuePair<MethodInfo,MethodInfo>(target,end));
            h.Patch(target,new HarmonyMethod(pre),post==null?null:new HarmonyMethod(post),finalizer:end==null?null:new HarmonyMethod(end));
        }
        static bool OpenCheck(ref bool __result)
        {if(!Active)return true;__result=true;return false;}
        static bool OpeningWrites(){return !opening&&!Active;}
        static void BeginScope(out bool __state){__state=Active;if(__state)closeGuards++;}
        static Exception EndScope(Exception __exception,bool __state){if(__state&&closeGuards>0)closeGuards--;return __exception;}
        // This callback only closes UI and logs each close. Skip it entirely
        // in the opted-in scope mode to avoid per-frame close-log spam.
        static bool OffStation(){return !Active;}
        static void AfterScope(){Tick();}
        internal static bool KeepPanel(TorpedoLaunchersUI panel)
        {return !releasing&&closeGuards>0&&Active&&panel!=null&&panel==Current();}
        static bool BeforeClose(TorpedoLaunchersUI __instance)
        {
            if(KeepPanel(__instance))return false;
            // Genuine native close (escape, story/dialog, no valid aim context)
            // is respected. Do not immediately reopen over another native UI.
            if(!releasing&&owner==__instance)suspended=true;
            return true;
        }
        static TorpedoLaunchersUI Current(){var ui=Runtime.Get<GameUI>();return ui==null?null:ui.CurrentTorpedoLaunchersUI;}
        internal static void Tick()
        {
            if(!Active){Release();return;}
            var current=Current();var scope=Runtime.ViewingScope;
            if(current==null)return;
            if(!ReferenceEquals(station,scope)||owner!=current){Release();station=scope;owner=current;}
            if(suspended)return;
            // Mask before the native Open enables children; there is no frame
            // exposing reports, checkboxes, dispersion or preparation controls.
            NativePresentation.Refresh(current);
            if(current.IsOpened)return;
            if(!Runtime.AimContext||Runtime.LaunchBusy||Time.unscaledTime<retryAt)return;
            retryAt=Time.unscaledTime+.25f;opening=true;
            try{
                ViewingFire.AwaitNativeUpdate(current);
                current.Open(true,0,Runtime.Ship.TorpedoLaunchers.Length,false);
                autoOpened=current.IsOpened;
                if(autoOpened){ViewingFire.SelectFromBoxes();NativePresentation.Refresh(current);}
            }catch(Exception e){
                if(Time.unscaledTime>=errorAt){errorAt=Time.unscaledTime+10;Debug.LogWarning("[TorpedoRoom] MP Feuer display: "+e.GetBaseException().Message);}
            }finally{opening=false;}
        }
        internal static void Release()
        {
            var previous=owner;bool close=autoOpened;
            owner=null;station=null;autoOpened=false;suspended=false;retryAt=0;
            if(previous==null)return;
            releasing=true;
            try{if(close&&previous.IsOpened)previous.Close();NativePresentation.Refresh(previous);}
            finally{releasing=false;}
        }
    }
}
