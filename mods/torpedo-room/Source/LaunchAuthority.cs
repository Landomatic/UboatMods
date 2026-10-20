// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;

namespace Landomatic.TorpedoRoom
{
    // Capture before the launcher detaches its item. Resolve at the actual torpedo setter,
    // independently of wrapper arguments, native viewport values and later UI edits.
    internal static class LaunchAuthority
    {
        internal sealed class Plan
        {
            internal Preparation Value;
            internal bool Locked;
            internal float Expires;
            internal int Tube;
        }
        internal sealed class Audit
        {
            internal Plan Plan;
            internal bool IncomingRight;
            internal float IncomingDistance,IncomingAngle;
        }
        static readonly Dictionary<StoredTorpedo,Plan> plans=new Dictionary<StoredTorpedo,Plan>();
        static readonly List<StoredTorpedo> remove=new List<StoredTorpedo>();
        internal static void Install(Harmony harmony)
        {
            foreach(var method in typeof(TorpedoLauncher).GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance)){
                if(method.Name!="Launch")continue;
                harmony.Patch(method,new HarmonyMethod(typeof(LaunchAuthority),"Capture"){priority=Priority.First});
                harmony.Patch(method,new HarmonyMethod(typeof(LaunchAuthority),"LaunchDepth"){priority=Priority.Last});
                bool speed=false;foreach(var p in method.GetParameters())if(p.Name=="torpedoSpeed")speed=true;
                if(speed)harmony.Patch(method,new HarmonyMethod(typeof(LaunchAuthority),"LaunchSpeed"){priority=Priority.Last});
                bool program=false;foreach(var p in method.GetParameters())if(p.Name=="torpedoProgramDirection")program=true;
                if(program)harmony.Patch(method,new HarmonyMethod(typeof(LaunchAuthority),"OverrideLauncher"){priority=Priority.Last});
            }
            harmony.Patch(AccessTools.PropertySetter(typeof(Torpedo),"TargetDepth"),new HarmonyMethod(typeof(LaunchAuthority),"FinalDepth"){priority=Priority.Last});
            harmony.Patch(AccessTools.Method(typeof(Torpedo),"SetProgramParameters"),new HarmonyMethod(typeof(LaunchAuthority),"BeforeProgram"){priority=Priority.Last},new HarmonyMethod(typeof(LaunchAuthority),"AfterProgram"){priority=Priority.Last});
        }
        internal static void Clear(){plans.Clear();remove.Clear();}
        internal static void Tick()
        {
            if(plans.Count==0)return;remove.Clear();
            foreach(var pair in plans)if(pair.Key==null||Time.unscaledTime>pair.Value.Expires)remove.Add(pair.Key);
            foreach(var key in remove)plans.Remove(key);
        }
        static Plan Get(StoredTorpedo item)
        {Plan plan;return item!=null&&plans.TryGetValue(item,out plan)&&Time.unscaledTime<=plan.Expires?plan:null;}
        static Plan Get(Torpedo torpedo){return torpedo==null?null:Get(torpedo.GetComponent<StoredTorpedo>());}
        internal static void Capture(TorpedoLauncher __instance)
        {
            var item=__instance.Torpedo;
            if(!Runtime.Active||Runtime.Loading||__instance.Group==null||__instance.Group.ParentEntity!=Runtime.Ship||!__instance.IsFlooded){if(item!=null)plans.Remove(item);return;}
            var missile=Runtime.Missile(item);var status=Runtime.Status(item,!Bridge.IsRemote);
            if(item==null||missile==null||status==null)return;
            if(plans.Count>=64)Tick();
            if(plans.Count>=64){Debug.LogWarning("[TorpedoRoom] Launch capture limit reached; capture table reset.");plans.Clear();}
            // Refresh on every admitted attempt, including nested wrappers and retries.
            plans[item]=new Plan{Value=status.Value.Copy(),Locked=Runtime.LockoutEnabled&&status.Value.StraightRun,Tube=Runtime.IndexOf(missile),Expires=Time.unscaledTime+20};
            Runtime.SetPistol(missile,status.Value.Pistol);
        }
        internal static void LaunchDepth(TorpedoLauncher __instance,ref float depth)
        {var plan=Get(__instance.Torpedo);if(!Runtime.Loading&&plan!=null&&plan.Value.DepthOverride)depth=plan.Value.Depth;}
        internal static void LaunchSpeed(TorpedoLauncher __instance,ref float torpedoSpeed)
        {var plan=Get(__instance.Torpedo);if(!Runtime.Loading&&plan!=null&&plan.Value.SpeedOverride)torpedoSpeed=plan.Value.Speed;}
        internal static void FinalDepth(Torpedo __instance,ref float value)
        {var plan=Get(__instance);if(!Runtime.Loading&&plan!=null&&plan.Value.DepthOverride)value=plan.Value.Depth;}
        internal static void OverrideLauncher(TorpedoLauncher __instance,ref bool torpedoProgramDirection,ref float torpedoProgramDistance,ref float torpedoProgramAngleAdjust)
        {
            if(Runtime.Loading)return;var plan=Get(__instance.Torpedo);if(plan==null||!plan.Value.ProgramOverride)return;
            torpedoProgramDirection=plan.Value.Right;torpedoProgramDistance=plan.Value.Distance;torpedoProgramAngleAdjust=plan.Value.Angle;
        }
        internal static void BeforeProgram(Torpedo __instance,ref bool startDirection,ref float startDistance,ref float LUTAbsoluteAngle,ref Torpedo.TorpedoProgramType ___programType,out Audit __state)
        {
            __state=null;if(Runtime.Loading)return;var plan=Get(__instance);if(plan==null)return;
            __state=new Audit{Plan=plan,IncomingRight=startDirection,IncomingDistance=startDistance,IncomingAngle=LUTAbsoluteAngle};
            if(plan.Locked)___programType=Torpedo.TorpedoProgramType.None;
            if(!plan.Value.ProgramOverride)return;
            startDirection=plan.Value.Right;startDistance=plan.Value.Distance;LUTAbsoluteAngle=plan.Value.Angle;
        }
        internal static void AfterProgram(Audit __state,Torpedo.TorpedoProgramType ___programType,bool ___programCourseStartDirection,float ___programCourseStartDistance,float ___programLUTAbsolutePatternAngle)
        {
            if(__state==null)return;var plan=__state.Plan;
            float expected=Mathf.Clamp(plan.Value.ProgramOverride?plan.Value.Distance:__state.IncomingDistance,500,15000)+(___programType==Torpedo.TorpedoProgramType.LUT?-100:0);
            bool right=plan.Value.ProgramOverride?plan.Value.Right:__state.IncomingRight;
            float angle=plan.Value.ProgramOverride?plan.Value.Angle:__state.IncomingAngle;
            bool valid=___programCourseStartDirection==right&&Mathf.Abs(___programCourseStartDistance-expected)<.01f&&Mathf.Abs(___programLUTAbsolutePatternAngle-angle)<.01f&&(!plan.Locked||___programType==Torpedo.TorpedoProgramType.None);
            Debug.Log("[TorpedoRoom] LAUNCH tube="+Runtime.Roman(plan.Tube)+" id="+plan.Value.Id+" rev="+plan.Value.Revision+" source="+(plan.Locked?"STRAIGHT":plan.Value.ProgramOverride?"ROOM":"STOCK")
                +" incoming="+__state.IncomingDistance+"m/"+(__state.IncomingRight?"R":"L")+" requested="+(plan.Value.ProgramOverride?plan.Value.Distance:__state.IncomingDistance)+"m/"+(right?"R":"L")+" angle="+angle
                +" native="+___programCourseStartDistance+"m/"+(___programCourseStartDirection?"R":"L")+" nativeAngle="+___programLUTAbsolutePatternAngle+" type="+___programType+" verified="+valid);
            if(!valid)Debug.LogWarning("[TorpedoRoom] Native torpedo program differs from the captured launch plan. Preserve this log.");
        }
    }
}
