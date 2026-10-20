// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.UI;
namespace Landomatic.TorpedoRoom
{
    internal static class SpeedControl
    {
        internal static float[] Options(StoredTorpedo item)
        {return item==null?new float[0]:SpeedValues.Options(item.Speed1,item.Speed2,item.Speed3);}
        internal static float Effective(StoredTorpedo item,Preparation value,float shared)
        {var options=Options(item);return SpeedValues.Nearest(options,value!=null&&value.SpeedOverride?value.Speed:shared);}
        internal static float[] MarkedOptions()
        {
            var ship=Runtime.Ship;if(ship==null)return new float[0];List<float> common=null;
            foreach(var tube in ship.TorpedoLaunchers){
                if(tube==null||!tube.IsMarkedForLaunch||!Runtime.Stable(tube)||Runtime.Missile(tube.Torpedo)==null)continue;
                var options=Options(tube.Torpedo);if(common==null)common=new List<float>(options);else for(int n=common.Count-1;n>=0;n--)if(!SpeedValues.Supports(options,common[n]))common.RemoveAt(n);
            }
            return common==null?new float[0]:common.ToArray();
        }
        sealed class Bounds {internal float Minimum,Maximum;}
        static readonly Dictionary<Slider,Bounds> originalBounds=new Dictionary<Slider,Bounds>();
        internal static void RestoreBounds(bool force=false)
        {
            if(!force&&Runtime.Active)return;Runtime.Applying++;
            try{foreach(var entry in originalBounds)if(entry.Key!=null){entry.Key.minValue=entry.Value.Minimum;entry.Key.maxValue=entry.Value.Maximum;}}
            finally{originalBounds.Clear();Runtime.Applying--;}
        }
        static void Presets(List<float> buffer,Slider slider,float[] options)
        {
            if(buffer!=null){buffer.Clear();buffer.AddRange(options);}
            if(slider==null||options.Length==0)return;
            if(!originalBounds.ContainsKey(slider))originalBounds[slider]=new Bounds{Minimum=slider.minValue,Maximum=slider.maxValue};
            if(slider.minValue!=options[0])slider.minValue=options[0];
            if(slider.maxValue!=options[options.Length-1])slider.maxValue=options[options.Length-1];
        }
        internal static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersSpeedDialUI),"OnValueChanged"),new HarmonyMethod(typeof(SpeedControl),"BeforeEdit"),new HarmonyMethod(typeof(SpeedControl),"AfterEdit"));
            harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersSpeedDialUI),"ValidateState"),new HarmonyMethod(typeof(SpeedControl),"Project"),new HarmonyMethod(typeof(SpeedControl),"Display"),finalizer:new HarmonyMethod(typeof(SpeedControl),"Finally"));
        }
        static bool BeforeEdit(float value,bool ___uiValidate,out bool __state,List<float> ___speedSettingsBuffer=null,Slider ___slider=null)
        {
            __state=false;if(!Runtime.Active||Runtime.Loading||Runtime.Applying>0||___uiValidate)return true;
            if(Runtime.RoomEditorOpen)return false;
            if(Runtime.CurrentGroup!=null)return true;
            if(!Runtime.ViewingStation||Runtime.Viewport!=ViewportMode.Synchronized)return false;
            if(!Preparation.Finite(value)||value<=0)return false;
            var choices=MarkedOptions();if(choices.Length==0)return false;
            Runtime.Applying++;try{Presets(___speedSettingsBuffer,___slider,choices);}finally{Runtime.Applying--;}
            __state=true;return true;
        }
        static void AfterEdit(bool __state,Slider ___slider)
        {
            var ship=Runtime.Ship;if(!__state||ship==null||___slider==null)return;
            var edits=new List<TubeSnapshot>();var tubes=ship.TorpedoLaunchers;int targets=0;
            for(int i=0;i<tubes.Length;i++){
                var tube=tubes[i];if(tube==null||!tube.IsMarkedForLaunch||!Runtime.Stable(tube))continue;
                var status=Runtime.Status(tube.Torpedo,!Bridge.IsRemote);if(status==null)continue;
                float speed=___slider.value;if(!SpeedValues.Supports(Options(tube.Torpedo),speed))return;targets++;
                if(status.Value.SpeedOverride&&Math.Abs(status.Value.Speed-speed)<.001f)continue;
                var edit=status.Value.Copy();edit.SpeedOverride=true;edit.Speed=speed;edits.Add(new TubeSnapshot{Index=i,Settings=edit});
            }
            if(targets>0)ship.TorpedoAttackSettings.TorpedoSpeed=___slider.value;
            if(edits.Count==0)return;if(Bridge.IsRemote)Bridge.RequestViewport(edits.ToArray());else Runtime.AcceptViewport(edits.ToArray(),null);
            TdcSpeed.Sync();
        }
        internal sealed class Projection
        {internal PlayerShipTorpedoAttackSettings Settings;internal float Original,Value;internal bool Restored,Mixed;}
        static void Project(out Projection __state)
        {
            __state=null;var ship=Runtime.Ship;
            if(!Runtime.Active||Runtime.Loading||ship==null||(!Runtime.ViewingStation&&Runtime.CurrentGroup==null))return;
            bool found=false,mixed=false;float value=0;
            foreach(var tube in ship.TorpedoLaunchers){
                if(tube==null||!tube.IsMarkedForLaunch||!Runtime.Stable(tube)||Runtime.Missile(tube.Torpedo)==null)continue;
                var status=Runtime.Status(tube.Torpedo,!Bridge.IsRemote);if(status==null)continue;
                float next=Effective(tube.Torpedo,status.Value,ship.TorpedoAttackSettings.TorpedoSpeed);if(next<=0)continue;
                if(found&&Math.Abs(value-next)>.02f)mixed=true;else if(!found)value=next;found=true;
            }
            if(!found)return;
            __state=new Projection{Settings=ship.TorpedoAttackSettings,Original=ship.TorpedoAttackSettings.TorpedoSpeed,Value=value,Mixed=mixed};
            Runtime.Applying++;if(!mixed)ship.TorpedoAttackSettings.TorpedoSpeed=value;
        }
        static void Display(Projection __state,Slider ___slider,TextMeshProUGUI ___label,Transform ___pointer,List<float> ___speedSettingsBuffer=null,GameObject ___blockade=null)
        {
            try{
                if(__state==null)return;
                var choices=MarkedOptions();Presets(___speedSettingsBuffer,___slider,choices);
                if(___blockade!=null)___blockade.SetActive(choices.Length==0);
                if(___slider!=null)___slider.gameObject.SetActive(choices.Length>1);
                if(___label!=null&&___label.transform.parent!=null)___label.transform.parent.gameObject.SetActive(choices.Length>0);
                if(__state.Mixed){if(___label!=null)___label.text="MIXED";return;}
                if(___slider!=null){___slider.SetValueWithoutNotify(__state.Value);if(___pointer!=null)___pointer.localEulerAngles=new Vector3(0,0,-180*Mathf.InverseLerp(___slider.minValue,___slider.maxValue,__state.Value));}
                if(___label!=null)___label.text=SpeedUnits.Format(__state.Value);
            }finally{Restore(__state);}
        }
        static void Restore(Projection state)
        {if(state==null||state.Restored)return;state.Restored=true;try{state.Settings.TorpedoSpeed=state.Original;}finally{Runtime.Applying--;}}
        static Exception Finally(Exception __exception,Projection __state){Restore(__state);return __exception;}
    }
}
