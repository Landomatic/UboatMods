// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    // Native shared depth remains available; marked tubes receive explicit per-tube copies.
    internal static class DepthControl
    {
        internal static void Install(Harmony harmony)
        {
            var dial=AccessTools.TypeByName("UBOAT.Game.UI.TorpedoLaunchersDepthDialUI");
            if(dial==null)throw new MissingMemberException("Native depth dial not found.");
            foreach(var method in new[]{"OnEnable","Update"})harmony.Patch(AccessTools.Method(dial,method),new HarmonyMethod(typeof(DepthControl),"Tune"){priority=Priority.First});
            foreach(var method in new[]{"OnEnable","Update"})harmony.Patch(AccessTools.Method(dial,method),new HarmonyMethod(typeof(DepthControl),"Project"),new HarmonyMethod(typeof(DepthControl),"Restore"),finalizer:new HarmonyMethod(typeof(DepthControl),"Finally"));
            harmony.Patch(AccessTools.Method(typeof(UBOAT.Game.UI.Periscope.TorpedoLaunchersUI),"UpdateDataPanel"),new HarmonyMethod(typeof(DepthControl),"Project"),new HarmonyMethod(typeof(DepthControl),"Restore"),finalizer:new HarmonyMethod(typeof(DepthControl),"Finally"));
            harmony.Patch(AccessTools.Method(dial,"OnValueChanged"),new HarmonyMethod(typeof(DepthControl),"Before"),new HarmonyMethod(typeof(DepthControl),"After"));}
        sealed class DialState {internal bool Whole;internal float Minimum;}
        static readonly Dictionary<Slider,DialState> originalSteps=new Dictionary<Slider,DialState>();
        static void Tune(Slider ___slider)
        {
            if(___slider==null||!Runtime.Active||Runtime.Loading)return;
            if(!originalSteps.ContainsKey(___slider))originalSteps[___slider]=new DialState{Whole=___slider.wholeNumbers,Minimum=___slider.minValue};
            // Native slider units are twice metres: 1 means 0.5 m. Range changes
            // can invoke native callbacks, so retain the shared setting as well as
            // guarding preparation transactions during this display-only change.
            var ship=Runtime.Ship;float depth=ship==null?0:ship.TorpedoAttackSettings.TorpedoDepth;
            Runtime.Applying++;
            try{if(___slider.wholeNumbers)___slider.wholeNumbers=false;if(___slider.minValue!=1)___slider.minValue=1;}
            finally{try{if(ship!=null)ship.TorpedoAttackSettings.TorpedoDepth=depth;}finally{Runtime.Applying--;}}
        }
        internal static void RestoreSteps(bool force=false)
        {
            if(!force&&Runtime.Active)return;
            var ship=Runtime.Ship;float depth=ship==null?0:ship.TorpedoAttackSettings.TorpedoDepth;Runtime.Applying++;
            try{foreach(var entry in originalSteps)if(entry.Key!=null){entry.Key.minValue=entry.Value.Minimum;entry.Key.wholeNumbers=entry.Value.Whole;}}
            finally{try{originalSteps.Clear();if(ship!=null)ship.TorpedoAttackSettings.TorpedoDepth=depth;}finally{Runtime.Applying--;}}
        }
        internal sealed class Display {internal PlayerShipTorpedoAttackSettings Settings;internal float Depth;internal bool Restored;}
        static void Project(out Display __state)
        {
            __state=null;if(!Runtime.Active||Runtime.Loading)return;
            var p=StockUi.SingleMarked();var ship=Runtime.Ship;if(p==null||!p.DepthOverride||ship==null)return;
            __state=new Display{Settings=ship.TorpedoAttackSettings,Depth=ship.TorpedoAttackSettings.TorpedoDepth};
            Runtime.Applying++;try{ship.TorpedoAttackSettings.TorpedoDepth=p.Depth;}finally{Runtime.Applying--;}
            // The scope's native slider invokes its setter during refresh. Keep all such
            // callbacks inside a projection guard until this native UI method returns.
            Runtime.Applying++;
        }
        static void Restore(Display __state)
        {if(__state==null||__state.Restored)return;__state.Restored=true;try{__state.Settings.TorpedoDepth=__state.Depth;}finally{Runtime.Applying--;}}
        static Exception Finally(Exception __exception,Display __state){Restore(__state);return __exception;}
        static bool Before(ref float value,out bool __state)
        {
            __state=false;var ship=Runtime.Ship;
            if(!Runtime.Active||Runtime.Loading||Runtime.Applying>0||ship==null||(!Runtime.ViewingStation&&Runtime.CurrentGroup==null))return true;
            if(Runtime.RoomEditorOpen)return false;
            if(!Preparation.Finite(value))return false;
            if(Runtime.CurrentGroup==null&&Runtime.Viewport!=ViewportMode.Synchronized)return false;
            value=DialValues.Snap(value/2,.5f,14,.25f)*2;
            if(Runtime.CurrentGroup!=null)return true;
            float requested=value/2;var selected=StockUi.SingleMarked();float before=selected!=null&&selected.DepthOverride?selected.Depth:ship.TorpedoAttackSettings.TorpedoDepth;
            __state=Preparation.Finite(requested)&&requested>=.5f&&requested<=14&&requested!=before;return true;
        }
        static void After(bool __state,TMPro.TextMeshProUGUI ___label,float value)
        {
            if(___label!=null&&Runtime.Active&&!Runtime.Loading&&Preparation.Finite(value))___label.text=(value/2).ToString("0.0#",System.Globalization.CultureInfo.InvariantCulture)+" m";
            if(!__state)return;var ship=Runtime.Ship;if(ship==null||Runtime.Loading)return;
            var edits=new List<TubeSnapshot>();var tubes=ship.TorpedoLaunchers;
            for(int i=0;i<tubes.Length;i++){
                var tube=tubes[i];if(tube==null||!tube.IsMarkedForLaunch||!Runtime.Stable(tube))continue;
                var p=Runtime.Status(tube.Torpedo,!Bridge.IsRemote);if(p==null)continue;
                var edit=p.Value.Copy();edit.Depth=ship.TorpedoAttackSettings.TorpedoDepth;edit.DepthOverride=true;edits.Add(new TubeSnapshot{Index=i,Settings=edit});
            }
            if(edits.Count>0){if(Bridge.IsRemote)Bridge.RequestViewport(edits.ToArray());else Runtime.AcceptViewport(edits.ToArray(),null);}
        }
    }
}
