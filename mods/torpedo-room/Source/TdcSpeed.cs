// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
namespace Landomatic.TorpedoRoom
{
    // The TDC uses knots; stored weapons and shared attack settings use m/s.
    // Only the host commits tube preparations. Existing MP carries TDC controls.
    internal static class TdcSpeed
    {
        static int writing;
        static readonly FieldInfo Presets=AccessTools.Field(typeof(TorpedoCalculatorTorpedoSpeedDial),"presets");
        static readonly FieldInfo PresetValue=Presets==null?null:AccessTools.Field(Presets.FieldType.GetElementType(),"Value");
        static float remoteRequested,remoteUntil;
        static PlayerShip owner;
        static StoredTorpedo focus;
        static TorpedoCalculator calculator;
        internal static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.PropertySetter(typeof(TorpedoCalculator),"TorpedoSpeed"),new HarmonyMethod(typeof(TdcSpeed),"BeforeDisplayWrite"));
            harmony.Patch(AccessTools.PropertySetter(typeof(TorpedoCalculator),"TorpedoSpeedTarget"),new HarmonyMethod(typeof(TdcSpeed),"BeforeDisplayWrite"),new HarmonyMethod(typeof(TdcSpeed),"AfterTarget"));
            harmony.Patch(AccessTools.Method(typeof(TorpedoCalculatorTorpedoSpeedDial),"get_Value"),postfix:new HarmonyMethod(typeof(TdcSpeed),"PresetIdentity"));
        }
        internal static void Reset(){owner=null;calculator=null;focus=null;remoteRequested=remoteUntil=0;}
        static TorpedoCalculator Computer()
        {
            var ship=Runtime.Ship;
            if(owner!=ship){owner=ship;calculator=null;focus=null;remoteRequested=remoteUntil=0;}
            if(ship!=null&&calculator==null)calculator=ship.GetEquipment<TorpedoCalculator>();
            return calculator!=null&&calculator.enabled?calculator:null;
        }
        static bool Own(TorpedoCalculator value){return value!=null&&value==Computer();}
        static bool BeforeDisplayWrite(TorpedoCalculator __instance)
        {return writing>0||Runtime.Applying==0||!Runtime.Active||Runtime.Loading||!Own(__instance);}
        static bool Usable {get{return Runtime.Active&&!Runtime.Loading&&Runtime.Ship!=null;}}
        internal static bool IsFocus(TorpedoLauncher tube){return focus!=null&&tube!=null&&tube.Torpedo==focus;}
        internal static int FocusIndex {get{var ship=Runtime.Ship;if(ship!=null)for(int i=0;i<ship.TorpedoLaunchers.Length;i++)if(IsFocus(ship.TorpedoLaunchers[i]))return i;return -1;}}
        internal static float ReportSpeed {get{float speed;bool mixed;return Selection(out speed,out mixed)&&!mixed?speed:0;}}
        internal static void Applied(TorpedoLauncher tube)
        {
            if(!Usable||Bridge.IsRemote||tube==null)return;
            Computer();focus=tube.Torpedo;Sync();
        }
        internal static bool Selection(out float speed,out bool mixed)
        {
            speed=0;mixed=false;if(!Usable)return false;bool found=false,marked=false;
            foreach(var tube in Runtime.Ship.TorpedoLaunchers){
                if(tube==null||!tube.IsMarkedForLaunch)continue;marked=true;
                if(!Runtime.Stable(tube)||Runtime.Missile(tube.Torpedo)==null)continue;
                var status=Runtime.Status(tube.Torpedo,!Bridge.IsRemote);
                float next=SpeedControl.Effective(tube.Torpedo,status==null?null:status.Value,Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed);
                if(next<=0)continue;if(found&&Math.Abs(next-speed)>.02f)mixed=true;else if(!found)speed=next;found=true;
            }
            if(!found&&!marked&&focus!=null)foreach(var tube in Runtime.Ship.TorpedoLaunchers){
                if(!IsFocus(tube)||!Runtime.Stable(tube))continue;
                var status=Runtime.Status(focus,false);if(status==null||!status.Value.SpeedOverride)break;
                speed=status.Value.Speed;found=true;break;
            }
            return found;
        }
        static float[] Options()
        {
            var options=SpeedControl.MarkedOptions();
            if(options.Length==0){bool marked=false;foreach(var tube in Runtime.Ship.TorpedoLaunchers)if(tube!=null&&tube.IsMarkedForLaunch)marked=true;
                if(!marked&&FocusIndex>=0&&Runtime.Stable(Runtime.Ship.TorpedoLaunchers[FocusIndex]))return SpeedControl.Options(focus);}
            return options;
        }
        static void Write(float speed)
        {
            var ship=Runtime.Ship;if(ship==null||!Preparation.Finite(speed)||speed<=0)return;
            var tdc=Computer();float knots=speed*SpeedValues.Knots;
            writing++;Runtime.Applying++;
            try{
                if(Math.Abs(ship.TorpedoAttackSettings.TorpedoSpeed-speed)>.001f)ship.TorpedoAttackSettings.TorpedoSpeed=speed;
                if(tdc==null)return;
                // Native setter rescales TargetSpeed as a side effect. A weapon
                // speed update must preserve the target estimate already entered.
                float target=tdc.TargetSpeed;
                try{
                    if(Math.Abs(tdc.TorpedoSpeedTarget-knots)>.005f)tdc.TorpedoSpeedTarget=knots;
                    if(Math.Abs(tdc.TorpedoSpeed-knots)>.005f)tdc.TorpedoSpeed=knots;
                }finally{if(tdc.TargetSpeed!=target)tdc.TargetSpeed=target;}
            }finally{Runtime.Applying--;writing--;}
        }
        internal static void Sync()
        {
            if(!Usable||!Runtime.SpeedSync||writing>0||Runtime.Applying>0||(Bridge.IsRemote&&!Bridge.Synced))return;
            float speed;bool mixed;
            if(Bridge.IsRemote){
                if(Bridge.Last==null||Bridge.Last.SolutionSpeed<=0)return;
                int index=Bridge.Last.SpeedTube;
                if(index>=0&&index<Runtime.Ship.TorpedoLaunchers.Length&&Runtime.Ship.TorpedoLaunchers[index]!=null){Computer();focus=Runtime.Ship.TorpedoLaunchers[index].Torpedo;}
                speed=Bridge.Last.SolutionSpeed;
            }else if(!Selection(out speed,out mixed)||mixed)return;
            if(Bridge.IsRemote&&remoteRequested>0){
                if(Math.Abs(speed-remoteRequested)<.02f||UnityEngine.Time.unscaledTime>=remoteUntil)remoteRequested=0;
                else return; // Let the upstream MP TDC edit reach the host first.
            }
            Write(speed);
        }
        static bool NoMarked(){foreach(var tube in Runtime.Ship.TorpedoLaunchers)if(tube!=null&&tube.IsMarkedForLaunch)return false;return true;}
        static void AfterTarget(TorpedoCalculator __instance,float value)
        {
            if(!Usable||writing>0||Runtime.Applying>0||!Own(__instance)||!Runtime.SpeedSync||!Preparation.Finite(value)||value<=0)return;
            var options=Options();if(options.Length==0)return;
            float speed=SpeedValues.Nearest(options,value/SpeedValues.Knots);
            if(Bridge.IsRemote){if(Bridge.Synced){remoteRequested=speed;remoteUntil=UnityEngine.Time.unscaledTime+3;}return;}
            var edits=new List<TubeSnapshot>();var tubes=Runtime.Ship.TorpedoLaunchers;
            for(int i=0;i<tubes.Length;i++){
                var tube=tubes[i];if(tube==null||(!tube.IsMarkedForLaunch&&!(FocusIndex==i&&NoMarked()))||!Runtime.Stable(tube)||Runtime.Missile(tube.Torpedo)==null)continue;
                var status=Runtime.Status(tube.Torpedo,true);if(status==null)return;
                if(status.Value.SpeedOverride&&Math.Abs(status.Value.Speed-speed)<.001f)continue;
                var edit=status.Value.Copy();edit.SpeedOverride=true;edit.Speed=speed;edits.Add(new TubeSnapshot{Index=i,Settings=edit});
            }
            if(edits.Count>0&&!Runtime.AcceptViewport(edits.ToArray(),null,true))return;
            Write(speed);
        }
        internal static string Warning()
        {
            float speed;bool mixed;
            if(!Selection(out speed,out mixed))return "";
            if(mixed)return "MIXED SPEEDS: one targeting solution may not match every tube.";
            var tdc=Computer();
            return tdc!=null&&Math.Abs(tdc.TorpedoSpeed-speed*SpeedValues.Knots)>.3f?"TDC speed differs from tube settings. Firing remains available.":"";
        }
        // Return the identity of the nearest authored dial slot. Physical m/s
        // presets are often rounded separately from the engraved knot values.
        static void PresetIdentity(TorpedoCalculatorTorpedoSpeedDial __instance,TorpedoCalculator ___calculator,ref int __result)
        {
            if(!Usable||!Runtime.SpeedSync||!Own(___calculator)||Presets==null||PresetValue==null)return;
            float value=___calculator.TorpedoSpeedTarget;if(!Preparation.Finite(value))return;
            var presets=Presets.GetValue(__instance) as Array;if(presets==null||presets.Length==0)return;
            float distance=float.MaxValue;int nearest=__result;
            foreach(var preset in presets){int slot=(int)PresetValue.GetValue(preset);float delta=Math.Abs(slot-value);if(delta<distance){distance=delta;nearest=slot;}}
            __result=nearest;
        }
    }
}
