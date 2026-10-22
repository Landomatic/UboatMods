// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Scene;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Characters.Actions;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI.Storage;
namespace Landomatic.TorpedoRoom
{
    // Only the synchronous native tube-action Open call is eligible. Ordinary
    // unbound inventories and intentional global crew orders remain native.
    internal static class TubeOrdnanceBinding
    {
        internal sealed class Scope
        {
            internal Scope Previous;internal PlayableCharacter Actor;
            internal TorpedoLauncherGroup Group;internal object Ship;internal bool Restored;
        }
        [ThreadStatic]static Scope current;
        internal static bool Available {get;private set;}
        internal static void Install(Harmony harmony)
        {
            var enter=AccessTools.Method(typeof(OpenTorpedoStorageOnTubeAction),"OnPreStart");
            var open=AccessTools.Method(typeof(StorageUI),"Open");
            var before=AccessTools.Method(typeof(TubeOrdnanceBinding),"BeforeTube");
            var after=AccessTools.Method(typeof(TubeOrdnanceBinding),"AfterTube");
            var final=AccessTools.Method(typeof(TubeOrdnanceBinding),"FinishTube");
            var bind=AccessTools.Method(typeof(TubeOrdnanceBinding),"BeforeOpen");
            var adopt=AccessTools.Method(typeof(TubeOrdnanceBinding),"AfterOpen");
            try{
                if(enter==null||open==null)throw new MissingMethodException("Native tube inventory entry point");
                harmony.Patch(enter,prefix:new HarmonyMethod(before){priority=Priority.Last},postfix:new HarmonyMethod(after),finalizer:new HarmonyMethod(final));
                harmony.Patch(open,prefix:new HarmonyMethod(bind){priority=Priority.Last},postfix:new HarmonyMethod(adopt){priority=Priority.Last});
                Available=true;UnityEngine.Debug.Log("[TorpedoRoom] Native tube inventory operator binding installed; no crew eligibility override or global fallback added.");
            }catch(Exception e){
                Available=false;
                if(enter!=null)foreach(var patch in new[]{before,after,final})try{harmony.Unpatch(enter,patch);}catch{}
                if(open!=null)foreach(var patch in new[]{bind,adopt})try{harmony.Unpatch(open,patch);}catch{}
                UnityEngine.Debug.LogWarning("[TorpedoRoom] Native tube inventory binding unavailable: "+e.Message);
            }
        }
        internal static Scope Begin(OpenTorpedoStorageOnTubeAction action)
        {
            if(!Runtime.Active||Runtime.Loading||!Config.Interface||!OrdnanceDispatch.Available||!OrdnanceRelease.Available||Runtime.Instance==null)return null;
            var camera=MainCamera.Instance;var group=Runtime.CurrentGroup;
            if(camera==null||camera.CurrentMode!=CameraMode.Generic||camera.GenericController==null||group==null||group.ParentEntity!=Runtime.Ship)return null;
            var actor=camera.GenericController.Character;
            if(actor==null||action==null||action.Activator==null||!object.ReferenceEquals(action.Activator.AssociatedEquipment,group))return null;
            var scope=new Scope{Previous=current,Actor=actor,Group=group,Ship=Runtime.Ship};current=scope;return scope;
        }
        internal static void Restore(Scope scope)
        {if(scope==null||scope.Restored)return;scope.Restored=true;if(object.ReferenceEquals(current,scope))current=scope.Previous;}
        internal static Scope Bind(object[] args)
        {
            var scope=current;
            if(scope==null||scope.Restored||args==null||args.Length<2||args[0]!=null||scope.Actor==null)return null;
            if(!Runtime.Active||Runtime.Loading||!Config.Interface||!object.ReferenceEquals(scope.Ship,Runtime.Ship)||!object.ReferenceEquals(scope.Group,Runtime.CurrentGroup)||!object.ReferenceEquals(args[1],scope.Group.TorpedoStorage))return null;
            var camera=MainCamera.Instance;
            if(camera==null||camera.CurrentMode!=CameraMode.Generic||camera.GenericController==null||!object.ReferenceEquals(camera.GenericController.Character,scope.Actor))return null;
            // The same camera actor used by our button, never a substituted engineer.
            // Native inventory builds its own eligibility/warnings for this actor.
            args[0]=scope.Actor;return scope;
        }
        static bool BeforeTube(OpenTorpedoStorageOnTubeAction __instance,out Scope __state)
        {
            __state=Begin(__instance);
            // An unrelated nested tube call must not inherit an outer binding.
            if(__state==null&&current!=null){__state=new Scope{Previous=current};current=__state;}
            return true;
        }
        static void AfterTube(Scope __state){Restore(__state);}
        static Exception FinishTube(Exception __exception,Scope __state){Restore(__state);return __exception;}
        static bool BeforeOpen(ref IPlayableCharacter __0,IItemStorage __1,out Scope __state)
        {
            // UBOAT's Harmony 2.2.2 may expose an argument snapshot to __args
            // observers. Set the actual native parameter by reference, not only
            // that snapshot; verify the stored inventory actor in the postfix.
            object[] args={__0,__1};__state=Bind(args);
            if(__state!=null)__0=__state.Actor;
            return true;
        }
        static void AfterOpen(StorageUI __instance,Scope __state)
        {
            if(__state==null||!__instance.IsOpened||Runtime.Instance==null)return;
            if(!object.ReferenceEquals(__instance.Character,__state.Actor)){
                OrdnanceDiagnostics.Mark("BIND FAILED: actual inventory actor differs; close before any crew order");
                UnityEngine.Debug.LogWarning("[TorpedoRoom] Native tube inventory actor binding failed. Inventory closed; no global fallback allowed. Use Manage Ordnance.");
                __instance.Close();return;
            }
            Runtime.Instance.AdoptTubeOrdnance(__instance,__state.Actor,__state.Group,__state.Ship);
        }
    }
    public sealed partial class Runtime
    {
        internal void AdoptTubeOrdnance(StorageUI ui,PlayableCharacter actor,TorpedoLauncherGroup group,object ship)
        {
            if(Loading||!Active||!Config.Interface||deferredOrdnance!=null||!object.ReferenceEquals(ship,Ship)||!object.ReferenceEquals(group,CurrentGroup)||!object.ReferenceEquals(ui,Get<StorageUI>())||!ui.IsOpened||!object.ReferenceEquals(ui.Character,actor))return;
            if(ordnanceOwner!=null)ordnanceOwner.Closed-=OnOrdnanceClosed;
            ordnanceOwner=null;
            DismissRoom();ObserveOrdnance();
            OrdnanceDiagnostics.Mark("BIND native tube inventory to its camera operator; native eligibility retained; no global fallback");
        }
    }
}
