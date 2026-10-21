// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI;
using UBOAT.Game.UI.Periscope;
namespace Landomatic.TorpedoRoom
{
    // Move live controls out of the masked panel. Preserve listeners, native
    // readiness/permissions, confirmation and the exact original layout.
    internal static class HiddenLayout
    {
        static TorpedoLaunchersUI owner,failed;
        static GameObject hint;static TextMeshProUGUI hintText;static TorpedoHover hover;
        internal static string Reason="";
        static readonly List<Placement> moved=new List<Placement>();
        static readonly System.Reflection.FieldInfo Fire=AccessTools.Field(typeof(TorpedoLaunchersUI),"fireButton"),Popup=AccessTools.Field(typeof(TorpedoLaunchersUI),"fireConfirmationPopup");
        internal static bool Active {get{return owner!=null;}}
        sealed class Placement
        {
            readonly RectTransform rect;readonly Transform parent;readonly int sibling;
            readonly Vector2 min,max,pivot,size,position;readonly Vector3 scale;readonly Quaternion rotation;
            internal Placement(RectTransform value,Transform target,bool centre)
            {
                rect=value;parent=rect.parent;sibling=rect.GetSiblingIndex();min=rect.anchorMin;max=rect.anchorMax;pivot=rect.pivot;
                size=rect.sizeDelta;position=rect.anchoredPosition;scale=rect.localScale;rotation=rect.localRotation;
                var actualSize=rect.rect.size;var world=rect.position;
                rect.SetParent(target,true);rect.anchorMin=rect.anchorMax=pivot;rect.sizeDelta=actualSize;rect.position=world;
                if(centre){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,96);}
                rect.SetAsLastSibling();
            }
            internal void Restore()
            {
                if(rect==null)return;rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;
                rect.sizeDelta=size;rect.anchoredPosition=position;rect.localScale=scale;rect.localRotation=rotation;rect.SetSiblingIndex(sibling);
            }
        }
        static RectTransform Rect(object value){var c=value as Component;var go=value as GameObject;return c!=null?c.transform as RectTransform:go==null?null:go.transform as RectTransform;}
        internal static void Apply(TorpedoLaunchersUI panel,bool hidden)
        {
            if(!hidden){Restore();failed=null;return;}if(owner==panel||failed==panel)return;Restore();
            var target=panel.transform.parent;var fire=Fire==null?null:Rect(Fire.GetValue(panel));
            if(target==null||fire==null){failed=panel;Debug.LogWarning("[TorpedoRoom] Hidden Reports requires the native fire button and panel parent; retaining the original panel.");return;}
            try{
                // Move the popup first: it may be parented below the fire button.
                var popup=Popup==null?null:Rect(Popup.GetValue(panel));
                if(popup!=null&&popup.IsChildOf(panel.transform))moved.Add(new Placement(popup,target,false));
                foreach(var bar in panel.GetComponentsInChildren<TorpedoAttackStationsViewSwitchUI>(true))if(bar!=null&&bar.transform.IsChildOf(panel.transform)){
                    var rect=bar.transform as RectTransform;if(rect!=null)moved.Add(new Placement(rect,target,false));
                }
                moved.Add(new Placement(fire,target,true));owner=panel;
                CreateHint(fire);
            }catch(Exception e){Restore();failed=panel;Debug.LogWarning("[TorpedoRoom] Hidden Reports layout restored after an error: "+e.GetBaseException().Message);}
        }
        static void CreateHint(RectTransform fire)
        {
            hint=new GameObject("Torpedo Room fire-control hint",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));hint.transform.SetParent(fire,false);
            var rect=(RectTransform)hint.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,8);rect.sizeDelta=new Vector2(350,74);
            var image=hint.GetComponent<Image>();image.color=new Color(.07f,.07f,.06f,.96f);image.raycastTarget=false;
            var label=new GameObject("Reason",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));label.transform.SetParent(hint.transform,false);
            var labelRect=(RectTransform)label.transform;labelRect.anchorMin=new Vector2(0,0);labelRect.anchorMax=new Vector2(1,1);labelRect.sizeDelta=new Vector2(-20,-12);labelRect.anchoredPosition=Vector2.zero;
            hintText=label.GetComponent<TextMeshProUGUI>();var originals=fire.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach(var original in originals)if(original!=hintText&&original.font!=null){hintText.font=original.font;break;}
            hintText.fontSize=17;hintText.color=new Color(.92f,.9f,.82f,1);hintText.raycastTarget=false;hintText.richText=false;
            hover=fire.gameObject.AddComponent<TorpedoHover>();hover.Enter=()=>{if(hint!=null){hintText.text=Reason;hint.SetActive(!string.IsNullOrEmpty(Reason));}};hover.Exit=()=>{if(hint!=null)hint.SetActive(false);};hint.SetActive(false);
        }
        internal static void Restore()
        {if(hover!=null){hover.Enter=null;hover.Exit=null;UnityEngine.Object.Destroy(hover);hover=null;}if(hint!=null)UnityEngine.Object.Destroy(hint);hint=null;hintText=null;Reason="";for(int i=moved.Count-1;i>=0;i--)moved[i].Restore();moved.Clear();owner=null;}
        internal static void Closed(TorpedoLaunchersUI panel){if(owner==panel)Restore();}
    }
}
