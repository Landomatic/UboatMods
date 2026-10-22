// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Characters.Actions;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI.Storage;
namespace Landomatic.TorpedoRoom
{
    // Temporary observational hooks. No arguments, return values, tasks or camera
    // state are changed. A return from async StartAction is NOT task completion.
    internal static class OrdnanceDiagnostics
    {
        static float until,nextSample;static int remaining,sequence;static string previous;
        static readonly HashSet<object> actors=new HashSet<object>();
        static readonly HashSet<object> actions=new HashSet<object>();
        static bool Window {get{return Time.unscaledTime<until&&remaining>0&&!Runtime.Loading;}}
        static bool Relevant(object o){return o is LoadTorpedoesAction||o is WarmUpTorpedoAction||o is OpenTorpedoStorageOnTubeAction;}
        internal static void Install(Harmony harmony)
        {
            Hook(harmony,typeof(StorageUI),new[]{"Open","Close","ProceedWithCarryOrder","OpenCarryConfirmationUI"});
            Hook(harmony,typeof(PlayableCharacter),new[]{"StartAction","StopCurrentAction","OnJoinedAction"});
            Hook(harmony,typeof(InteractiveCharacter),new[]{"CancelOrders","CancelActiveOrders"});
            Hook(harmony,typeof(CharacterAction),new[]{"CompleteAction","StopAction","StartCharacter","StopCharacter","RemoveCharacter"});
            Hook(harmony,typeof(LoadTorpedoesAction),new[]{"StartMainCharacter","StopMainCharacter","StopAction","OnStageChanged"});
            Hook(harmony,typeof(WarmUpTorpedoAction),new[]{"StartMainCharacter","StopMainCharacter","StopAction"});
            Hook(harmony,typeof(MainCamera),new[]{"SetFPPController","SetGenericController","SetStageController"});
            var queue=AccessTools.TypeByName("UBOAT.Game.Scene.Characters.ActionQueue");
            if(queue!=null)Hook(harmony,queue,new[]{"Add","InsertAction","RemoveCharacter"});
            Debug.Log("[TorpedoRoom][ORD-DIAG] observational diagnostics installed; 90-second/300-line windows per inventory open; async returns are not completion.");
        }
        static void Hook(Harmony harmony,Type type,string[] names)
        {
            foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)){
                if(Array.IndexOf(names,method.Name)<0||method.IsAbstract||method.ContainsGenericParameters)continue;
                try{harmony.Patch(method,prefix:new HarmonyMethod(typeof(OrdnanceDiagnostics),"Before"){priority=Priority.First},postfix:new HarmonyMethod(typeof(OrdnanceDiagnostics),"After"){priority=Priority.Last});}
                catch(Exception e){Debug.LogWarning("[TorpedoRoom][ORD-DIAG] hook unavailable "+type.Name+"."+method.Name+": "+e.GetType().Name);}
            }
        }
        static object Read(object o,string name)
        {if(o==null)return null;try{var p=AccessTools.Property(o.GetType(),name);if(p!=null)return p.GetValue(o,null);var f=AccessTools.Field(o.GetType(),name);return f==null?null:f.GetValue(o);}catch{return null;}}
        static string Describe(object o)
        {
            if(o==null)return "null";
            var actor=o as PlayableCharacter;if(actor!=null)return actor.Name+"#"+actor.GetInstanceID();
            if(o is bool||o is float||o is int||o is Enum)return o.ToString();
            return o.GetType().Name+"#"+System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
        }
        static void Log(string message)
        {if(!Window)return;remaining--;Debug.Log("[TorpedoRoom][ORD-DIAG] #"+sequence+" t="+Time.unscaledTime.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+" frame="+Time.frameCount+" "+message);}
        static void Before(object __instance,MethodBase __originalMethod,object[] __args)
        {try{Observe(__instance,__originalMethod,__args,false);}catch{}}
        static void After(object __instance,MethodBase __originalMethod,object[] __args)
        {try{Observe(__instance,__originalMethod,__args,true);}catch{}}
        static void Observe(object instance,MethodBase method,object[] args,bool after)
        {
            if(!Runtime.Active||Runtime.Loading)return;
            bool storage=instance is StorageUI;
            if(!after&&storage&&method.Name=="Open"&&(Runtime.CurrentGroup!=null||Window)){
                until=Time.unscaledTime+90;remaining=300;sequence++;previous=null;actors.Clear();actions.Clear();
            }
            if(!Window)return;
            bool relevant=storage||instance is MainCamera||Relevant(instance)||actors.Contains(instance)||actions.Contains(instance);
            foreach(var arg in args)if(Relevant(arg)||arg!=null&&actors.Contains(arg))relevant=true;
            if(!relevant)return;
            if(Relevant(instance))actions.Add(instance);
            foreach(var arg in args){if(arg is PlayableCharacter)actors.Add(arg);if(Relevant(arg))actions.Add(arg);}
            var character=Read(instance,"Character") as PlayableCharacter;if(character!=null)actors.Add(character);
            string detail="";var pars=method.GetParameters();
            for(int i=0;i<args.Length;i++)detail+=" "+(i<pars.Length?pars[i].Name:"arg"+i)+"="+Describe(args[i]);
            Log((after?"RETURN ":"ENTER ")+method.DeclaringType.Name+"."+method.Name+" instance="+Describe(instance)+detail);
            if(storage&&after)Snapshot("inventory");
        }
        internal static void Mark(string message)
        {try{Log(message);Snapshot("handoff");}catch{}}
        internal static void Tick()
        {
            if(!Runtime.Active||Runtime.Loading){until=0;actors.Clear();actions.Clear();return;}
            if(!Window||Time.unscaledTime<nextSample)return;nextSample=Time.unscaledTime+.5f;
            try{Snapshot("sample");}catch{}
        }
        static void Snapshot(string reason)
        {
            var camera=MainCamera.Instance;var generic=camera==null?null:camera.GenericController;
            var ui=Runtime.Get<StorageUI>();var group=Runtime.CurrentGroup;
            string s="mode="+(camera==null?"null":camera.CurrentMode.ToString())+" viewActor="+Describe(generic==null?null:generic.Character)+" controller="+Describe(generic==null?null:generic.ExternalController)+" group="+Describe(group)+" inventory="+(ui!=null&&ui.IsOpened)+" inventoryActor="+Describe(ui==null?null:ui.Character);
            foreach(var item in actors){var actor=item as PlayableCharacter;if(actor==null)continue;s+=" [actor="+Describe(actor)+" action="+Describe(Read(actor,"Action"))+" pos="+actor.transform.position.ToString("F2")+"]";}
            // Where the native type exposes occupants, include them without claiming
            // a camera-selected actor is actually stationed at the equipment.
            var occupants=Read(group,"InteractingCharacters") as System.Collections.IEnumerable;
            if(occupants!=null){s+=" occupants=";foreach(var a in occupants)s+=Describe(a)+",";}
            else s+=" occupants=not-exposed";
            if(s==previous&&reason=="sample")return;previous=s;Log("STATE "+reason+" "+s);
        }
    }
}
