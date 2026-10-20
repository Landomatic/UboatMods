// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using TMPro;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI;
using UBOAT.Game.UI.Periscope;

namespace Landomatic.TorpedoRoom
{
    // Copy graphics only. Never instantiate native scripts, buttons or launch listeners.
    internal static partial class NativeSkin
    {
        static TorpedoLaunchersManualUI manual;
        static TorpedoLaunchersDepthDialUI depth;
        static RectTransform stationBar;
        static Transform stationParent;
        static RectTransform stationHolder;
        static int stationSibling;
        static Vector2 stationMin,stationMax,stationPivot,stationPosition,stationSize;
        static Vector3 stationScale;
        static Quaternion stationRotation;
        static float nextStationSearch;
        // Move the live bar, retaining native listeners, visibility and MP checks.
        // Never instantiate a second station controller or force a toggle enabled.
        internal static void RoomStations(Transform target)
        {
            if(target==null){RestoreStations();return;}
            if(stationBar!=null){SizeStations();return;}
            if(Time.unscaledTime<nextStationSearch)return;nextStationSearch=Time.unscaledTime+1;
            foreach(var candidate in Resources.FindObjectsOfTypeAll<TorpedoAttackStationsViewSwitchUI>()){
                if(candidate==null||!candidate.enabled||!candidate.gameObject.scene.IsValid()||!candidate.gameObject.activeSelf)continue;
                var rect=candidate.transform as RectTransform;if(rect==null||rect.rect.height<=0)continue;
                stationBar=rect;stationParent=rect.parent;stationSibling=rect.GetSiblingIndex();
                stationMin=rect.anchorMin;stationMax=rect.anchorMax;stationPivot=rect.pivot;
                stationPosition=rect.anchoredPosition;stationSize=rect.sizeDelta;stationScale=rect.localScale;stationRotation=rect.localRotation;
                stationHolder=(RectTransform)new GameObject("Native station scale bridge",typeof(RectTransform)).transform;
                stationHolder.SetParent(target,false);stationHolder.anchorMin=stationHolder.anchorMax=stationHolder.pivot=new Vector2(0,.5f);stationHolder.anchoredPosition=new Vector2(16,0);stationHolder.sizeDelta=Vector2.zero;
                var size=rect.rect.size;rect.SetParent(stationHolder,false);rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(0,.5f);
                rect.anchoredPosition=Vector2.zero;rect.sizeDelta=size;rect.localScale=stationScale;rect.localRotation=stationRotation;SizeStations();
                return;
            }
        }
        static void SizeStations()
        {
            if(stationHolder==null||stationBar==null)return;
            UiScale.SetScale(stationHolder,UiScale.Between(stationParent,stationHolder.parent));
            var size=stationSize;var parent=stationParent as RectTransform;
            if(parent!=null){size.x+=parent.rect.width*(stationMax.x-stationMin.x);size.y+=parent.rect.height*(stationMax.y-stationMin.y);}
            if(stationBar.sizeDelta.x!=size.x||stationBar.sizeDelta.y!=size.y)stationBar.sizeDelta=size;
        }
        internal static void RestoreStations()
        {
            if(stationBar!=null){
                stationBar.SetParent(stationParent,false);stationBar.anchorMin=stationMin;stationBar.anchorMax=stationMax;stationBar.pivot=stationPivot;
                stationBar.anchoredPosition=stationPosition;stationBar.sizeDelta=stationSize;
                stationBar.SetSiblingIndex(stationSibling);
            }
            stationBar=null;stationParent=null;if(stationHolder!=null)UnityEngine.Object.Destroy(stationHolder.gameObject);stationHolder=null;nextStationSearch=0;
        }
        internal static Sprite ButtonSprite,FieldSprite;
        static object ship;static TorpedoLauncherUI[] views=new TorpedoLauncherUI[0];
        static readonly FieldInfo Label=AccessTools.Field(typeof(TorpedoLaunchersDepthDialUI),"label"),Pointer=AccessTools.Field(typeof(TorpedoLaunchersDepthDialUI),"pointer"),Blockade=AccessTools.Field(typeof(TorpedoLaunchersDepthDialUI),"blockade");
        internal static GameObject ManualRoot {get{return manual==null?null:manual.gameObject;}}
        internal static void Capture()
        {
            if(!ReferenceEquals(ship,Runtime.Ship)){RestoreStations();ship=Runtime.Ship;manual=null;depth=null;ButtonSprite=FieldSprite=null;views=new TorpedoLauncherUI[0];}
            if(views.Length==0)views=Resources.FindObjectsOfTypeAll<TorpedoLauncherUI>();
            if(ButtonSprite==null||FieldSprite==null)foreach(var view in views){
                if(view==null||!view.gameObject.scene.IsValid())continue;
                if(ButtonSprite==null){var button=AccessTools.Field(typeof(TorpedoLauncherUI),"pistolTypeButton").GetValue(view) as Button;var image=button==null?null:button.targetGraphic as Image;if(image!=null)ButtonSprite=image.sprite;}
                if(FieldSprite==null){var panel=view.GetComponentInParent<TorpedoLaunchersUI>();var field=panel==null?null:AccessTools.Field(typeof(TorpedoLaunchersUI),"programDistance").GetValue(panel) as TMP_InputField;var image=field==null?null:field.targetGraphic as Image;if(image!=null)FieldSprite=image.sprite;}
                if(ButtonSprite!=null&&FieldSprite!=null)break;
            }
            if(manual==null)foreach(var candidate in Resources.FindObjectsOfTypeAll<TorpedoLaunchersManualUI>())if(candidate.gameObject.activeInHierarchy&&candidate.gameObject.scene.IsValid()){manual=candidate;break;}
            var ui=Runtime.Get<GameUI>();var launchPanel=ui==null?null:ui.CurrentTorpedoLaunchersUI;
            if(launchPanel!=null){var full=launchPanel.GetComponentsInChildren<TorpedoLaunchersDepthDialUI>(true);if(full.Length>0)depth=full[0];}
            if(depth==null)foreach(var candidate in Resources.FindObjectsOfTypeAll<TorpedoLaunchersDepthDialUI>())if(candidate.gameObject.activeInHierarchy&&candidate.gameObject.scene.IsValid()){depth=candidate;break;}
        }
        internal static TorpedoLauncherUI TubeView(int index)
        {var view=NativePresentation.TubeView(index);if(view!=null)return view;if(Runtime.Ship==null||index<0||index>=Runtime.Ship.TorpedoLaunchers.Length)return null;foreach(var candidate in views)if(candidate!=null&&candidate.gameObject.scene.IsValid()&&candidate.Launcher==Runtime.Ship.TorpedoLaunchers[index])return candidate;return null;}
        static readonly MethodInfo ValidateIcon=typeof(TorpedoLauncherUI).GetMethod("ValidateTorpedoIcon",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        static readonly FieldInfo TubeIcon=AccessTools.Field(typeof(TorpedoLauncherUI),"torpedoImage");
        internal static void RefreshTubeIcon(int index)
        {
            var view=TubeView(index);if(view==null||ValidateIcon==null)return;
            // Refresh artwork only. ValidateState also changes native tube marks.
            try{ValidateIcon.Invoke(view,null);}catch(Exception e){Debug.LogWarning("[TorpedoRoom] Tube artwork refresh: "+e.GetType().Name);}
        }
        internal static Sprite TubeSprite(int index)
        {var view=TubeView(index);var image=view==null||TubeIcon==null?null:TubeIcon.GetValue(view) as Image;return image==null?null:image.sprite;}
        internal static Sprite Board
        {get{if(manual==null)return null;var image=manual.GetComponent<Image>();if(image==null&&manual.transform.parent!=null)image=manual.transform.parent.GetComponent<Image>();return image==null?null:image.sprite;}}
        internal sealed class Visual
        {
            internal bool TubeArt;internal HashSet<Image> ImageFilter;
            internal Transform DialSource;internal float DialSize;
            internal RectTransform Root,Needle,PistolArea;internal Transform PistolSource;internal TextMeshProUGUI TextFilter,ValueText,PistolText;internal Image Icon,OpenCover,ClosedCover;
            internal readonly List<TextMeshProUGUI> TextSources=new List<TextMeshProUGUI>(),TextCopies=new List<TextMeshProUGUI>();
            internal readonly List<Image> Sources=new List<Image>(),Copies=new List<Image>();
            internal readonly List<GameObject> Nodes=new List<GameObject>(),NodeCopies=new List<GameObject>();
            internal void Sync(){for(int i=1;i<Nodes.Count;i++)if(Nodes[i]!=null&&NodeCopies[i]!=null)NodeCopies[i].SetActive(TubeArt||Nodes[i].activeSelf);for(int i=0;i<TextSources.Count;i++)if(TextSources[i]!=null&&TextCopies[i]!=null)TextCopies[i].text=TextSources[i].text;for(int i=0;i<Sources.Count;i++){var source=Sources[i];var copy=Copies[i];if(source==null||copy==null)continue;copy.sprite=source.sprite;copy.color=source.color;copy.enabled=source.enabled;copy.fillAmount=source.fillAmount;}}
        }
        static RectTransform Copy(Transform source,Transform parent,HashSet<GameObject> skip,Visual result,Transform needle)
        {
            if(source==null||source.name=="TorpedoRoomProgram"||skip.Contains(source.gameObject))return null;
            var original=source as RectTransform;if(original==null)return null;
            if(result.ImageFilter!=null){bool included=false;foreach(var includedImage in result.ImageFilter)if(includedImage!=null&&includedImage.transform.IsChildOf(source)){included=true;break;}if(!included)return null;}
            var go=new GameObject("Native "+source.name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);go.SetActive(result.TubeArt||source.gameObject.activeSelf);
            result.Nodes.Add(source.gameObject);result.NodeCopies.Add(go);
            r.anchorMin=original.anchorMin;r.anchorMax=original.anchorMax;r.pivot=original.pivot;r.anchoredPosition=original.anchoredPosition;r.sizeDelta=original.sizeDelta;r.localScale=original.localScale;r.localRotation=original.localRotation;
            var image=source.GetComponent<Image>();if(image!=null&&(result.ImageFilter==null||result.ImageFilter.Contains(image))){var clone=go.AddComponent<Image>();clone.sprite=image.sprite;clone.material=image.material;clone.color=image.color;clone.type=image.type;clone.fillMethod=image.fillMethod;clone.fillOrigin=image.fillOrigin;clone.fillClockwise=image.fillClockwise;clone.fillAmount=image.fillAmount;clone.preserveAspect=image.preserveAspect;clone.enabled=image.enabled;clone.raycastTarget=false;result.Sources.Add(image);result.Copies.Add(clone);}
            var text=source.GetComponent<TextMeshProUGUI>();if(!result.TubeArt&&text!=null&&(result.TextFilter==null||text==result.TextFilter)){
                var clone=go.AddComponent<TextMeshProUGUI>();clone.font=text.font;clone.fontSharedMaterial=text.fontSharedMaterial;clone.spriteAsset=text.spriteAsset;clone.fontSize=text.fontSize;clone.fontStyle=text.fontStyle;clone.alignment=text.alignment;clone.color=text.color;clone.richText=text.richText;clone.enableAutoSizing=text.enableAutoSizing;clone.fontSizeMin=text.fontSizeMin;clone.fontSizeMax=text.fontSizeMax;clone.margin=text.margin;clone.text=text.text;clone.raycastTarget=false;
                result.TextSources.Add(text);result.TextCopies.Add(clone);if(text==result.TextFilter)result.ValueText=clone;
                if(result.PistolSource!=null&&source.IsChildOf(result.PistolSource))result.PistolText=clone;
            }
            if(source==result.PistolSource)result.PistolArea=r;
            if(source==needle)result.Needle=r;
            for(int i=0;i<source.childCount;i++)Copy(source.GetChild(i),r,skip,result,needle);
            return r;
        }
        static Visual Make(Transform source,Transform target,float width,float height,HashSet<GameObject> skip,Transform needle,Transform pistol=null,TextMeshProUGUI textFilter=null,HashSet<Image> images=null)
        {
            if(source==null)return null;var rect=source as RectTransform;if(rect==null||rect.rect.width<=0||rect.rect.height<=0)return null;
            int children=target.childCount;
            try{
                var v=new Visual{PistolSource=pistol,TextFilter=textFilter,TubeArt=images!=null,ImageFilter=images};v.Root=Copy(source,target,skip,v,needle);if(v.Root==null)return null;
                v.Root.gameObject.SetActive(true);
                AlignRoot(v.Root,rect,source==needle);v.Root.sizeDelta=rect.rect.size;
                float scale=Mathf.Min(width/rect.rect.width,height/rect.rect.height);v.Root.localScale=new Vector3(scale,scale,1);v.Root.localRotation=Quaternion.identity;return v;
            }catch(Exception e){
                for(int i=target.childCount-1;i>=children;i--)UnityEngine.Object.Destroy(target.GetChild(i).gameObject);
                Debug.LogWarning("[TorpedoRoom] Native graphic unavailable ("+source.name+"); using editable fallback: "+e);
                return null;
            }
        }
        // Bound only the visible tube artwork. Native root rectangles can be
        // smaller than their doors, which previously intruded into our labels.
        static void FitTube(Visual visual,float width,float height)
        {
            var corners=new Vector3[4];bool first=true;Vector3 min=Vector3.zero,max=Vector3.zero;
            foreach(var image in visual.Copies){
                image.rectTransform.GetWorldCorners(corners);
                foreach(var corner in corners){var point=visual.Root.InverseTransformPoint(corner);if(first){min=max=point;first=false;}else{min=Vector3.Min(min,point);max=Vector3.Max(max,point);}}
            }
            if(first||max.x<=min.x||max.y<=min.y)return;
            float scale=Mathf.Min(width/(max.x-min.x),height/(max.y-min.y));
            visual.Root.localScale=new Vector3(scale,scale,1);
            visual.Root.anchoredPosition=new Vector2(-(min.x+max.x)*.5f*scale,-(min.y+max.y)*.5f*scale);
        }
        internal static Visual Tube(TorpedoLauncherUI source,Transform target,float width,float height)
        {
            if(source==null)return null;
            // Explicit artwork only: native number/pistol/status panels duplicate
            // our labels. Flood-progress graphics rely on native masks that are
            // not copied; flooding is reported by our live state text instead.
            var fields=new[]{"torpedoImage","coverOpenImage","coverCloseImage"};var images=new HashSet<Image>();
            foreach(var name in fields){var field=AccessTools.Field(typeof(TorpedoLauncherUI),name);var image=field==null?null:field.GetValue(source) as Image;if(image!=null)images.Add(image);}
            if(images.Count==0)return null;
            var visual=Make(source.transform,target,width,height,new HashSet<GameObject>(),null,images:images);if(visual==null)return null;
            for(int n=0;n<fields.Length;n++){
                var native=AccessTools.Field(typeof(TorpedoLauncherUI),fields[n]).GetValue(source) as Image;
                for(int i=0;i<visual.Sources.Count;i++)if(visual.Sources[i]==native){if(n==0)visual.Icon=visual.Copies[i];else if(n==1)visual.OpenCover=visual.Copies[i];else visual.ClosedCover=visual.Copies[i];}
            }
            FitTube(visual,width,height);return visual;
        }
        internal static void SizeDial(Visual visual)
        {
            if(visual==null||visual.Root==null||visual.DialSource==null)return;
            var source=visual.DialSource as RectTransform;if(source==null)return;
            var scale=UiScale.Between(source,visual.Root.parent);
            // Enlarge the rotating artwork itself, not only its background.
            // Fit around the authored hub inside 90% of the instrument diameter,
            // retaining the native proportions and authored hub pivot.
            float x=source.rect.width*scale.x*Mathf.Max(source.pivot.x,1-source.pivot.x),y=source.rect.height*scale.y*Mathf.Max(source.pivot.y,1-source.pivot.y);
            float radius=Mathf.Sqrt(x*x+y*y);if(radius<=0)return;
            float fit=visual.DialSize*.45f/radius;
            UiScale.SetScale(visual.Root,new Vector3(scale.x*fit,scale.y*fit,1));
        }
        internal static Visual Dial(Transform target,float size)
        {
            if(depth==null)return null;
            // The native depth component also owns a slider and a readout outside
            // its own rectangle. Copy only the rotating knob, fitting it to the
            // same square that receives pointer events. Our numeric field remains
            // the single readout/input; no detached stock slider is copied.
            var pointer=Pointer.GetValue(depth) as Transform;
            var visual=Make(pointer,target,size,size,new HashSet<GameObject>(),pointer);
            if(visual!=null){visual.DialSource=pointer;visual.DialSize=size;SizeDial(visual);}return visual;
        }
    }
}
