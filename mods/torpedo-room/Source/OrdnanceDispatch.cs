// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Characters.Actions;
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
        // Last priority: leave native/MP order dispatch and eligibility unchanged.
        // Never start, replace, cancel or fabricate a crew action here.
        static bool BeforeOrder(PlayableCharacter __instance,CharacterAction __1)
        {
            if(Available&&Runtime.Instance!=null)Runtime.Instance.PrepareOrdnanceOrder(__instance,__1);
            return true;
        }
    }
}
