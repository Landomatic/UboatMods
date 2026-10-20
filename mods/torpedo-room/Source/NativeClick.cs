// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using UnityEngine;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    internal static partial class NativeSkin
    {
        internal static void AlignRoot(RectTransform copy,RectTransform source,bool rotating)
        {
            copy.anchorMin=copy.anchorMax=new Vector2(.5f,.5f);
            // The art includes a projecting handle. Its authored pivot is the
            // knob hub, not necessarily the centre of the entire image bounds.
            copy.pivot=rotating?source.pivot:new Vector2(.5f,.5f);
            copy.anchoredPosition=Vector2.zero;
        }
        internal static void ClickArea(RectTransform rect,Action action)
        {
            if(rect==null)return;
            // TMP text is already a Graphic. Unity permits only one Graphic on
            // an object: adding Image to that text object returns null.
            var hit=rect.GetComponent<Graphic>();
            if(hit==null){var image=rect.gameObject.AddComponent<Image>();if(image==null)return;image.color=Color.clear;hit=image;}
            hit.raycastTarget=true;
            var button=rect.GetComponent<Button>();if(button==null)button=rect.gameObject.AddComponent<Button>();
            if(button==null)return;
            button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            button.onClick.AddListener(()=>{if(action!=null)action();});
        }
    }
}
