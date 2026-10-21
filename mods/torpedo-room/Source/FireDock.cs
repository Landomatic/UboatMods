// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI.Periscope;
namespace Landomatic.TorpedoRoom
{
    internal static class FireDock
    {
        static readonly System.Reflection.FieldInfo[] Icons={AccessTools.Field(typeof(PeriscopeUI),"selectTorpedoButton"),AccessTools.Field(typeof(PeriscopeUI),"selectTorpedoButton2")};
        static readonly Selectable[] NoNeighbours=new Selectable[0];
        static readonly System.Reflection.FieldInfo Popup=AccessTools.Field(typeof(TorpedoLaunchersUI),"fireConfirmationPopup");
        static RectTransform icon;static PeriscopeUI scope;static Selectable[] neighbours=NoNeighbours;static readonly List<Box> obstacles=new List<Box>();static float nextProbe;static readonly Vector3[] corners=new Vector3[4];
        internal struct Box {internal float Left,Bottom,Right,Top;internal Box(float l,float b,float r,float t){Left=l;Bottom=b;Right=r;Top=t;}}
        internal static Box Beside(Box anchor,float width,float height,float gap,Box bounds,IList<Box> obstacles)
        {
            float x=anchor.Right+gap,y=(anchor.Bottom+anchor.Top)/2;
            // Prefer directly alongside. If a neighbouring control occupies that
            // space, raise Feuer above the strip while retaining its right offset.
            if(bounds.Right-bounds.Left>=width+2*gap)x=Math.Max(bounds.Left+gap,Math.Min(x,bounds.Right-width-gap));
            for(int pass=0;pass<=obstacles.Count+1;pass++){
                bool overlap=false;
                if(x<anchor.Right+gap&&x+width>anchor.Left-gap&&y-height/2<anchor.Top+gap&&y+height/2>anchor.Bottom-gap){y=anchor.Top+gap+height/2;overlap=true;}
                foreach(var o in obstacles)if(x<o.Right+gap&&x+width>o.Left-gap&&y-height/2<o.Top+gap&&y+height/2>o.Bottom-gap){y=o.Top+gap+height/2;overlap=true;}
                if(!overlap)break;
            }
            return new Box(x,y-height/2,x+width,y+height/2);
        }
        static Box Bounds(RectTransform reference,RectTransform rect)
        {
            rect.GetWorldCorners(corners);float l=float.PositiveInfinity,b=l,r=float.NegativeInfinity,t=r;
            foreach(var corner in corners){var p=reference.InverseTransformPoint(corner);l=Math.Min(l,p.x);r=Math.Max(r,p.x);b=Math.Min(b,p.y);t=Math.Max(t,p.y);}
            return new Box(l,b,r,t);
        }
        internal static void Clear(){icon=null;scope=null;neighbours=NoNeighbours;obstacles.Clear();nextProbe=0;}
        internal static void Follow(TorpedoLaunchersUI panel,RectTransform fire)
        {
            if(fire==null||panel==null)return;
            if(Time.unscaledTime>=nextProbe||icon==null||!icon.gameObject.activeInHierarchy){
                if(Time.unscaledTime<nextProbe)return;nextProbe=Time.unscaledTime+.5f;icon=null;
                if(scope==null)scope=Runtime.Get<PeriscopeUI>();if(scope==null)scope=UnityEngine.Object.FindObjectOfType<PeriscopeUI>();
                if(scope!=null)foreach(var field in Icons){var toggle=field==null?null:field.GetValue(scope) as Toggle;if(toggle!=null&&toggle.gameObject.activeInHierarchy){icon=toggle.transform as RectTransform;break;}}
                neighbours=icon!=null&&icon.parent!=null?icon.parent.GetComponentsInChildren<Selectable>(false):NoNeighbours;
            }
            var parent=fire.parent as RectTransform;
            if(icon==null||parent==null){
                if(parent!=null){fire.anchorMin=fire.anchorMax=fire.pivot=new Vector2(.5f,0);fire.anchoredPosition=new Vector2(0,144);}
                return;
            }
            obstacles.Clear();var popup=Popup==null?null:Popup.GetValue(panel) as GameObject;
            foreach(var control in neighbours){
                if(control==null||!control.gameObject.activeInHierarchy||popup!=null&&control.transform.IsChildOf(popup.transform)||control.transform==icon||control.transform==fire||control.transform.IsChildOf(fire)||control.transform.IsChildOf(panel.transform))continue;
                var rect=control.transform as RectTransform;if(rect!=null)obstacles.Add(Bounds(parent,rect));
            }
            var a=Bounds(parent,icon);var f=Bounds(parent,fire);var p=Bounds(parent,parent);
            float gap=Math.Max(8,(a.Top-a.Bottom)*.25f);
            var box=Beside(a,f.Right-f.Left,f.Top-f.Bottom,gap,p,obstacles);
            // Keep the live button outside the hidden panel and disabled toggle's
            // hierarchy; follow its world rectangle instead of copying a pixel offset.
            fire.pivot=new Vector2(0,.5f);fire.localPosition=new Vector3(box.Left,(box.Top+box.Bottom)/2,0);fire.SetAsLastSibling();
        }
    }
}
