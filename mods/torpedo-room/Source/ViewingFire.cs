// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI.Periscope;
namespace Landomatic.TorpedoRoom
{
    // UI entry points only. No patches to physical fire levers, LaunchAllTorpedoes,
    // gyro angles, salvo timing, native readiness, or multiplayer transport.
    internal static class ViewingFire
    {
        static readonly System.Reflection.FieldInfo Fire=AccessTools.Field(typeof(TorpedoLaunchersUI),"fireButton"),Blockade=AccessTools.Field(typeof(TorpedoLaunchersUI),"firingBlockade");
        static TorpedoLaunchersUI depthOwner;
        static GameObject depthMask;static bool depthMaskWasActive;
        internal static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),"Update"),new HarmonyMethod(typeof(ViewingFire),"BeforeUpdate"),new HarmonyMethod(typeof(ViewingFire),"AfterUpdate"));
            foreach(var name in new[]{"OnFire","DoFire"})harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),name),new HarmonyMethod(typeof(ViewingFire),"BeforeFire"));
            foreach(var name in new[]{"Close","OnDisable","OnDestroy"})harmony.Patch(AccessTools.Method(typeof(TorpedoLaunchersUI),name),new HarmonyMethod(typeof(ViewingFire),"Closed"));
        }
        static bool Enabled {get{return Runtime.Active&&!Runtime.Loading&&Runtime.ViewingStation&&Runtime.Ship!=null;}}
        static bool Hidden {get{return Enabled&&(Runtime.Viewport==ViewportMode.Hidden||MpScopeFire.Active);}}
        // Exact equipment keys, including the stock prototype. Do not infer T5
        // from a localized tooltip, icon colour, or another acoustic weapon.
        internal static bool IsT5(string key)
        {return key=="G7es Zaunkönig Torpedo T5 - Pi4"||key=="G7es Zaunkönig Torpedo T5 - Pi4 - Prototype"
            ||key=="G7es Zaunkönig Torpedo T5 - Pi4 (Warmed)"||key=="G7es Zaunkönig Torpedo T5 - Pi4 - Prototype (Warmed)";}
        internal static bool DeepPermission(float depth,int floodedSelected,bool allT5)
        {return Preparation.Finite(depth)&&depth>18&&depth<=50&&floodedSelected>0&&allT5;}
        internal static string TubeDepthWarning(float boatDepth,string equipmentKey)
        {return Preparation.Finite(boatDepth)&&boatDepth>(IsT5(equipmentKey)?50:18)?(IsT5(equipmentKey)?"Tube Depth <= 50m":"Tube Depth <= 18m"):"";}
        static bool HasSelected(out bool allT5)
        {
            allT5=true;bool any=false;var ship=Runtime.Ship;
            if(ship==null||ship.TorpedoLaunchers==null){allT5=false;return false;}
            // Check the live flooded launch candidates, not every remembered check mark.
            // A dry/empty marked tube cannot launch; draining an ordinary weapon
            // must release its depth veto without changing the native selection.
            // Flooding it again reinstates the veto at Update and confirmation.
            foreach(var tube in ship.TorpedoLaunchers)if(tube!=null&&tube.IsMarkedForLaunch&&tube.IsFlooded&&tube.Torpedo!=null){
                any=true;
                if(!IsT5(tube.Torpedo.Data==null?null:tube.Torpedo.Data.Name))allT5=false;
            }
            return any;
        }
        static bool DeepAllowed(){bool all;bool any=HasSelected(out all);return DeepPermission(Runtime.Ship.Depth,any?1:0,all);}
        internal static bool SelectFromBoxes()
        {
            var ship=Runtime.Ship;if(ship.IsPreparingToLaunchTorpedoes||ship.IsLaunchingTorpedoes)return false;
            var boxes=ship.GetEquipment<TorpedoFireControlBoxes>();if(boxes==null)return false;
            // Use the game's hull-specific selector/salvo mapping. Never choose
            // an arbitrary ready tube or write selector values from hidden marks.
            if(ship.TorpedoLaunchers==null)return false;
            foreach(var tube in ship.TorpedoLaunchers)if(tube!=null)tube.IsMarkedForLaunch=false;
            boxes.ValidateMarkedLaunchersFromSalvoSettings();return true;
        }
        static void BeforeUpdate(){RestoreDepthMask();if(Hidden)SelectFromBoxes();}
        static Button Button(TorpedoLaunchersUI panel){return Fire==null?null:Fire.GetValue(panel) as Button;}
        internal static void AwaitNativeUpdate(TorpedoLaunchersUI panel){var button=Button(panel);if(button!=null)button.interactable=false;}
        static void AfterUpdate(TorpedoLaunchersUI __instance)
        {
            if(!Enabled){FireHint.Clear();NativePresentation.Refresh(__instance);return;}
            bool mapped=!Hidden||SelectFromBoxes();bool all;bool selected=HasSelected(out all);
            bool deep=DeepAllowed();
            if(Blockade!=null){
                var mask=Blockade.GetValue(__instance) as GameObject;
                if(mask!=null){depthOwner=__instance;depthMask=mask;depthMaskWasActive=mask.activeSelf;mask.SetActive(false);}
            }
            // The depth overlay blocks the entire panel, including selection. Mask it
            // regardless of current marks, and transfer only its depth rule to
            // Feuer in EVERY report mode. Never enable a native-disabled button.
            // This lets users deselect/reselect T5 without a flood/drain cycle.
            var button=Button(__instance);
            if(button!=null&&((Hidden&&(!mapped||!selected))||Runtime.Ship.Depth>18&&!deep))button.interactable=false;
            NativePresentation.Refresh(__instance);
            HiddenLayout.Reason=!mapped?"Fire-control selection is unavailable.":!selected?"Ready, Select and Flood launch-tubes via Torpedo Room and Fire Control Boxes to fire.":Runtime.Ship.Depth>18&&!deep?"Viewing-station firing is blocked at this depth. Only flooded T5 launch candidates are permitted down to 50 m.":button!=null&&!button.interactable?"Native firing requirements are not met.":"Fire the flooded tubes selected at the fire-control boxes.";
            bool allT5;bool any=HasSelected(out allT5);
            FireHint.Refresh(button,DepthHint(Runtime.Ship.Depth,deep,Hidden?HiddenLayout.Reason:"",any&&allT5));
        }
        internal static string DepthHint(float depth,bool permitted,string fallback,bool allSelectedT5=false)
        {return depth>18&&!permitted?(allSelectedT5?"Too Deep to Fire! Ascend to minimum Tube depth: 50m":"Too Deep to Fire! Ascend to minimum Tube depth: 18m"):fallback;}
        static bool BeforeFire(TorpedoLaunchersUI __instance)
        {
            if(!Enabled)return true;
            if(Hidden){
                bool all;if(!SelectFromBoxes()||!HasSelected(out all))return false;
            }
            // Also recheck on native confirmation, after any selector/depth change.
            // This is not a launch-level interlock and never affects tube/TDC levers.
            if(Runtime.Ship.Depth>18&&!DeepAllowed())return false;
            var button=Button(__instance);return !Hidden||button!=null&&button.interactable&&!__instance.DisallowFire;
        }
        static void RestoreDepthMask()
        {if(depthMask!=null)depthMask.SetActive(depthMaskWasActive);depthMask=null;depthOwner=null;}
        static void Closed(TorpedoLaunchersUI __instance)
        {if(MpScopeFire.KeepPanel(__instance))return;FireHint.Close(Button(__instance));HiddenLayout.Closed(__instance);if(depthOwner==__instance)RestoreDepthMask();}
    }
}

