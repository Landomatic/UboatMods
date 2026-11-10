// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using UnityEngine;
using UnityEngine.UI;
namespace Landomatic.TorpedoRoom
{
    // Independent border graphics remain visible when native tube artwork is
    // borrowed and the transparent card background has no outlineable pixels.
    internal static class PrepSelection
    {
        internal sealed class Frame
        {
            internal GameObject Root;internal Image[] Edges;
            internal void Select(bool selected){if(Root!=null)Root.SetActive(selected);}
        }
        internal static Frame Create(Transform parent,Color color)
        {
            var root=new GameObject("Selected preparation tube",typeof(RectTransform));root.transform.SetParent(parent,false);
            var r=(RectTransform)root.transform;r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,1);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(-6,-6);
            var f=new Frame{Root=root,Edges=new Image[4]};
            for(int i=0;i<4;i++){
                var go=new GameObject("Selection border "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(root.transform,false);
                var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;f.Edges[i]=image;
                var edge=(RectTransform)go.transform;edge.anchoredPosition=Vector2.zero;
                if(i<2){edge.anchorMin=new Vector2(0,i);edge.anchorMax=new Vector2(1,i);edge.pivot=new Vector2(.5f,i);edge.sizeDelta=new Vector2(0,3);}
                else{edge.anchorMin=new Vector2(i-2,0);edge.anchorMax=new Vector2(i-2,1);edge.pivot=new Vector2(i-2,.5f);edge.sizeDelta=new Vector2(3,0);}
            }
            root.transform.SetAsLastSibling();f.Select(false);return f;
        }
    }
}
