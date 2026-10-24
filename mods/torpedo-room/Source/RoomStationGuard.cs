// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using Cysharp.Threading.Tasks;
using UBOAT.Game.Scene.Characters.Actions;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI.Banners;
using UnityEngine;
namespace Landomatic.TorpedoRoom
{
    // The station banner is a world-space activator, separate from the manual
    // panel. Only suppress re-entering the currently displayed tube station.
    // Individual tubes, breeches, storage, feed valves and launch levers retain
    // their native actions and eligibility.
    internal static class RoomStationGuard
    {
        internal static void Install(Harmony harmony)
        {
            TryPatch(harmony,typeof(BaseActivatorBanner),"ValidateVisibility","AfterVisibility",true);
            TryPatch(harmony,typeof(UseManuallyOperableDeviceAction),"OnPreStart","BeforeEnter",false);
        }
        static void TryPatch(Harmony harmony,Type type,string name,string hook,bool postfix)
        {
            var target=AccessTools.Method(type,name);var patch=AccessTools.Method(typeof(RoomStationGuard),hook);
            try{
                if(target==null)throw new MissingMethodException(type.Name,name);
                harmony.Patch(target,postfix?null:new HarmonyMethod(patch),postfix?new HarmonyMethod(patch):null);
            }catch(Exception e){
                if(target!=null)try{harmony.Unpatch(target,patch);}catch{}
                Debug.LogWarning("[TorpedoRoom] Optional station prompt guard unavailable ("+name+"): "+e.GetBaseException().Message);
            }
        }
        internal static bool Covered(object equipment)
        {return Runtime.RoomOverviewActive&&equipment is TorpedoLauncherGroup&&ReferenceEquals(equipment,Runtime.CurrentGroup);}
        static void AfterVisibility(BaseActivatorBanner __instance)
        {
            var activator=__instance.Activator;
            var group=__instance.GetComponent<CanvasGroup>();
            // A marker component avoids altering unrelated pre-existing groups.
            var marker=__instance.GetComponent<RoomStationMask>();
            if(Covered(activator==null?null:activator.AssociatedEquipment)){
                if(marker==null){marker=__instance.gameObject.AddComponent<RoomStationMask>();marker.Bind(group);}
                marker.Hide();
            }else if(marker!=null)marker.Restore();
        }
        static bool BeforeEnter(UseManuallyOperableDeviceAction __instance,ref UniTask<bool> __result)
        {
            if(!Covered(__instance.Activator==null?null:__instance.Activator.AssociatedEquipment))return true;
            __result=new UniTask<bool>(false);return false;
        }
    }
    [UBOAT.Game.Core.Serialization.NonSerializedInGameState]
    public sealed class RoomStationMask:MonoBehaviour
    {
        CanvasGroup group;float alpha;bool interactable,blocks;bool hidden;
        internal void Bind(CanvasGroup existing)
        {group=existing!=null?existing:gameObject.AddComponent<CanvasGroup>();alpha=group.alpha;interactable=group.interactable;blocks=group.blocksRaycasts;}
        internal void Hide(){if(group==null)return;hidden=true;group.alpha=0;group.interactable=false;group.blocksRaycasts=false;}
        internal void Restore(){if(!hidden||group==null)return;hidden=false;group.alpha=alpha;group.interactable=interactable;group.blocksRaycasts=blocks;}
        void LateUpdate(){if(!Runtime.RoomOverviewActive)Restore();}
        void OnDisable(){Restore();}
    }
}
