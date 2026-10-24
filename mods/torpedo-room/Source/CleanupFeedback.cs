// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Landomatic.TorpedoRoom
{
    // UI only: parented under the nonserialized, persistent runtime; no save data.
    internal static class CleanupFeedback
    {
        static GameObject root;
        internal static void Show(string title,string message){Build(title,message,null);}
        internal static void Confirm(string title,string message,Action confirm){Build(title,message,confirm);}
        static void Build(string title,string message,Action confirm)
        {
            if(root!=null)UnityEngine.Object.Destroy(root);
            root=new GameObject("TorpedoRoom.CleanupResult",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(Runtime.Instance.transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
            var shade=Box(root.transform,"Shade",new Color(0,0,0,.7f));
            shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
            var panel=Box(shade,"Result",new Color(.08f,.09f,.10f,1));panel.anchorMin=new Vector2(.18f,.17f);panel.anchorMax=new Vector2(.82f,.83f);panel.offsetMin=panel.offsetMax=Vector2.zero;
            Label(panel,"Title",title,new Vector2(.05f,.82f),new Vector2(.95f,.96f),28);
            Label(panel,"Message",message,new Vector2(.05f,.22f),new Vector2(.95f,.80f),22);
            if(confirm==null)AddButton(panel,"OK",.36f,.64f,()=>{});
            else {
                AddButton(panel,"Cancel",.08f,.44f,()=>{});
                AddButton(panel,"Remove records",.56f,.92f,confirm);
            }

        }
        static void AddButton(Transform parent,string title,float left,float right,Action action)
        {
            var button=Box(parent,title,new Color(.30f,.25f,.14f,1));button.anchorMin=new Vector2(left,.04f);button.anchorMax=new Vector2(right,.17f);button.offsetMin=button.offsetMax=Vector2.zero;
            var b=button.gameObject.AddComponent<Button>();b.targetGraphic=button.GetComponent<Image>();
            b.onClick.AddListener(()=>{var old=root;root=null;if(old!=null){old.SetActive(false);UnityEngine.Object.Destroy(old);}action();});
            var text=Label(button,"Label",title,Vector2.zero,Vector2.one,24);text.alignment=TextAlignmentOptions.Center;
        }
        static RectTransform Box(Transform parent,string name,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return (RectTransform)go.transform;
        }
        static TextMeshProUGUI Label(Transform parent,string name,string value,Vector2 min,Vector2 max,float size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;text.color=Color.white;text.enableWordWrapping=true;text.richText=false;text.raycastTarget=false;return text;
        }
    }
}
