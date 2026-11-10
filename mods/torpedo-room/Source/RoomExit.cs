// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI;
namespace Landomatic.TorpedoRoom
{
    internal static partial class NativeSkin
    {
        static readonly System.Reflection.FieldInfo ExitField=AccessTools.Field(typeof(BasicManualModeUI),"leaveStationButton");
        static RectTransform exitButton,exitHolder;static Transform exitParent;
        static Vector2 exitMin,exitMax,exitPivot,exitPosition,exitSize;static Vector3 exitScale;static Quaternion exitRotation;static int exitSibling;static float nextExitSearch;
        // Move the ORIGINAL button, including its native tooltip/listener/permissions.
        internal static void RoomExit(Transform target)
        {
            if(target==null){RestoreExit();return;}
            if(exitButton==null){
                if(Time.unscaledTime<nextExitSearch)return;nextExitSearch=Time.unscaledTime+1;
                foreach(var ui in Resources.FindObjectsOfTypeAll<BasicManualModeUI>()){
                    if(ui==null||!ui.enabled||!ui.gameObject.scene.IsValid()||!ui.gameObject.activeSelf)continue;
                    var b=ExitField==null?null:ExitField.GetValue(ui) as Button;if(b==null||!b.gameObject.activeSelf)continue;
                    var rect=b.transform as RectTransform;if(rect==null)continue;
                    exitButton=rect;exitParent=rect.parent;exitSibling=rect.GetSiblingIndex();exitMin=rect.anchorMin;exitMax=rect.anchorMax;exitPivot=rect.pivot;exitPosition=rect.anchoredPosition;exitSize=rect.sizeDelta;exitScale=rect.localScale;exitRotation=rect.localRotation;
                    var size=rect.rect.size;
                    exitHolder=(RectTransform)new GameObject("Native Leave position",typeof(RectTransform)).transform;exitHolder.SetParent(target,false);
                    exitHolder.anchorMin=exitHolder.anchorMax=exitHolder.pivot=new Vector2(1,0);exitHolder.anchoredPosition=new Vector2(-16,16);exitHolder.sizeDelta=Vector2.zero;
                    rect.SetParent(exitHolder,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=size;rect.localScale=exitScale;rect.localRotation=exitRotation;break;
                }
            }
            if(exitHolder!=null){UiScale.SetScale(exitHolder,UiScale.Between(exitParent,exitHolder.parent));exitHolder.SetAsLastSibling();}
        }
        static void RestoreExit()
        {
            if(exitButton!=null){exitButton.SetParent(exitParent,false);exitButton.anchorMin=exitMin;exitButton.anchorMax=exitMax;exitButton.pivot=exitPivot;exitButton.anchoredPosition=exitPosition;exitButton.sizeDelta=exitSize;exitButton.localScale=exitScale;exitButton.localRotation=exitRotation;exitButton.SetSiblingIndex(exitSibling);}
            exitButton=null;exitParent=null;if(exitHolder!=null)UnityEngine.Object.Destroy(exitHolder.gameObject);exitHolder=null;nextExitSearch=0;
        }
    }
}
