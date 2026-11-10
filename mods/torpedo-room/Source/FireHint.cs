// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    // Pointer handlers still receive hover on a Button with interactable=false.
    // This only reports the reason; it never changes clickability or firing.
    internal static class FireHint
    {
        static Button owner;static GameObject hint;static TextMeshProUGUI text;
        static TorpedoHover hover;static bool hovering;static string reason="";
        internal static void Refresh(Button button,string value)
        {
            if(button==null){Clear();return;}
            if(owner!=button){Clear();Attach(button);}
            reason=value??"";Show();
        }
        static void Attach(Button button)
        {
            owner=button;
            hint=new GameObject("Torpedo Room Feuer tooltip",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));hint.transform.SetParent(button.transform,false);
            var rect=(RectTransform)hint.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,8);rect.sizeDelta=new Vector2(350,74);
            var image=hint.GetComponent<Image>();image.color=new Color(.07f,.07f,.06f,.96f);image.raycastTarget=false;
            var label=new GameObject("Reason",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));label.transform.SetParent(hint.transform,false);
            var labelRect=(RectTransform)label.transform;labelRect.anchorMin=new Vector2(0,0);labelRect.anchorMax=new Vector2(1,1);labelRect.sizeDelta=new Vector2(-20,-12);labelRect.anchoredPosition=Vector2.zero;
            text=label.GetComponent<TextMeshProUGUI>();foreach(var original in button.GetComponentsInChildren<TextMeshProUGUI>(true))if(original!=text&&original.font!=null){text.font=original.font;break;}
            if(text.font==null)text.font=TMP_Settings.defaultFontAsset;
            text.fontSize=17;text.color=new Color(.92f,.9f,.82f,1);text.raycastTarget=false;text.richText=false;
            hover=button.gameObject.AddComponent<TorpedoHover>();hover.Enter=()=>{hovering=true;Show();};hover.Exit=()=>{hovering=false;Show();};hint.SetActive(false);
        }
        static void Show(){if(hint==null)return;if(text!=null)text.text=reason;hint.SetActive(hovering&&!string.IsNullOrEmpty(reason));}
        internal static void Close(Button button){if(owner==button)Clear();}
        internal static void Clear()
        {
            if(hover!=null){hover.Enter=null;hover.Exit=null;Object.Destroy(hover);}hover=null;
            if(hint!=null){hint.SetActive(false);Object.Destroy(hint);}hint=null;text=null;owner=null;hovering=false;reason="";
        }
    }
}
