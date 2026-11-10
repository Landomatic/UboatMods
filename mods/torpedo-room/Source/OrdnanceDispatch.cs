// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Characters.Actions;
using Activator = UBOAT.Game.Scene.Characters.Activators.Activator;
namespace Landomatic.TorpedoRoom
{
    internal static class OrdnanceDispatch
    {
        internal static bool Available {get;private set;}
        internal static void Install(Harmony harmony)
        {
            var target=AccessTools.Method(typeof(PlayableCharacter),"StartAction");
            var prefix=AccessTools.Method(typeof(OrdnanceDispatch),"BeforeOrder");
            try {
                if(target==null)throw new MissingMethodException("PlayableCharacter.StartAction");
                harmony.Patch(target,prefix:new HarmonyMethod(prefix){priority=Priority.Last});Available=true;
            } catch(Exception e){
                Available=false;
                if(target!=null)try{harmony.Unpatch(target,prefix);}catch{}
                UnityEngine.Debug.LogWarning("[TorpedoRoom] Native ordnance handoff unavailable: "+e.Message);
            }
        }
        // Last priority: earlier native/MP authorization prefixes still run.
        // Only our bound station inventory may defer an exact native request.
        // StorageUI ignores the returned UniTask; replay traverses all native patches.
        static bool BeforeOrder(PlayableCharacter __instance,Activator __0,CharacterAction __1)
        {
            if(Available&&Runtime.Instance!=null){
                if(Runtime.Instance.DeferOrdnanceOrder(__instance,__0,__1))return false;
                Runtime.Instance.PrepareOrdnanceOrder(__instance,__1);
            }
            return true;
        }
    }
}
