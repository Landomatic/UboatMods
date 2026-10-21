// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    internal static class MenuIntegration
    {
        static MethodInfo setString,setBool,save,refresh;
        static FieldInfo settingsPane;
        static bool checkedTypes,pending,clearCleanup;
        // Dusty's ExternalRows persists every bool, then ExternalMods.ApplyStoredValues
        // calls its setter again at startup. A one-shot action must not replay it.
        internal static bool RestoringSettings
        {
            get {var frames=new System.Diagnostics.StackTrace(false).GetFrames();if(frames!=null)foreach(var frame in frames){var m=frame.GetMethod();if(m!=null&&m.Name=="ApplyStoredValues"&&m.DeclaringType!=null&&m.DeclaringType.FullName=="DustysCommunityPatch.Core.ExternalMods")return true;}return false;}
        }
        internal static void ResetCleanupFlag(){clearCleanup=true;}
        static void Find()
        {
            if(checkedTypes)return;checkedTypes=true;
            var config=AccessTools.TypeByName("DustysCommunityPatch.Core.QolConfig");var panel=AccessTools.TypeByName("DustysCommunityPatch.Core.QolPanel");
            if(config!=null){setString=AccessTools.Method(config,"SetString");setBool=AccessTools.Method(config,"SetBool");save=AccessTools.Method(config,"Save");}
            if(panel!=null){refresh=AccessTools.Method(panel,"RefreshSettingsPane");settingsPane=AccessTools.Field(panel,"_settingsPane");}
        }
        internal static void Publish()
        {
            Find();if(setString==null||setBool==null||Bridge.IsRemote)return;
            try{
                foreach(object value in CommunityPatchSettings.GetSettings()){
                    var row=(object[])value;string key=(string)row[1];if(key!="preset"&&key!="roomAccess"&&key!="viewportDisplay"&&key!="dry"&&key!="allowStraightRun"&&key!="speedSync"&&key!="mpScopeFeuer")continue;
                    object current=((Func<object>)row[4])();string full="Torpedo_Room."+key;
                    if((string)row[0]=="choice")setString.Invoke(null,new object[]{full,((string[])row[6])[Convert.ToInt32(current)]});
                    else setBool.Invoke(null,new object[]{full,Convert.ToBoolean(current)});
                }
                if(save!=null)save.Invoke(null,null);pending=true;
            }catch(Exception e){Runtime.Say("Community Patch labels need reopening: "+e.Message);}
        }
        internal static void Tick()
        {
            if(clearCleanup){
                clearCleanup=false;Find();
                try{if(setBool!=null){setBool.Invoke(null,new object[]{"Torpedo_Room.removeRecords",false});if(save!=null)save.Invoke(null,null);}}
                catch(Exception e){Debug.LogWarning("[TorpedoRoom] Cleanup checkbox reset: "+e.Message);}
            }
            if(!pending)return;pending=false;
            try{
                if(refresh==null)return;
                var pane=settingsPane==null?null:settingsPane.GetValue(null) as GameObject;
                var scroll=pane==null?null:pane.GetComponent<ScrollRect>();
                bool remember=scroll!=null&&scroll.content!=null;
                Vector2 offset=remember?scroll.content.anchoredPosition:Vector2.zero;
                refresh.Invoke(null,null);
                // Dusty replaces the content but retains the pane/ScrollRect.
                // Its rows are positioned/sized synchronously, so restore the
                // pixel offset after rebuilding instead of reopening at the top.
                if(remember&&scroll!=null&&scroll.content!=null){scroll.StopMovement();scroll.content.anchoredPosition=offset;}
            }catch(Exception e){Runtime.Say("Reopen Community Patch to refresh labels: "+e.Message);}
        }
    }
}
