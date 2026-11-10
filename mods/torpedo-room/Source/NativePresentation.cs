// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI;
using UBOAT.Game.UI.Periscope;

namespace Landomatic.TorpedoRoom
{
    // Borrow live sprites only: no game assets or native behaviour clones are distributed.
    internal static class NativePresentation
    {
        static TorpedoLaunchersUI owner;
        static bool roomTakeover;
        static GameObject[] depthDials=new GameObject[0];
        static readonly FieldInfo Icon=AccessTools.Field(typeof(TorpedoLauncherUI),"torpedoImage");
        static readonly FieldInfo Pistol=AccessTools.Field(typeof(TorpedoLauncherUI),"pistolTypeButton");
        static readonly FieldInfo[] Reports={AccessTools.Field(typeof(TorpedoLaunchersUI),"programParameters"),AccessTools.Field(typeof(TorpedoLaunchersUI),"programSettingsName"),AccessTools.Field(typeof(TorpedoLaunchersUI),"dataPanel")};
        internal static void Panel(TorpedoLaunchersUI panel){if(owner!=panel){Restore();owner=panel;var found=new List<GameObject>();if(panel!=null)foreach(var component in panel.GetComponentsInChildren<MonoBehaviour>(true))if(component!=null&&(component.GetType().Name=="TorpedoLaunchersDepthDialUI"||component.GetType().Name=="TorpedoLaunchersSpeedDialUI"))found.Add(component.gameObject);depthDials=found.ToArray();}}
        static readonly Dictionary<GameObject,Mask> masks=new Dictionary<GameObject,Mask>();
        sealed class Mask
        {
            internal CanvasGroup Group;internal float Alpha;internal bool Interactable,Blocks;
            internal Mask(GameObject go){Group=go.GetComponent<CanvasGroup>();if(Group==null)Group=go.AddComponent<CanvasGroup>();Alpha=Group.alpha;Interactable=Group.interactable;Blocks=Group.blocksRaycasts;}
            internal void Set(bool hide,bool read){if(Group==null)return;Group.alpha=hide?0:Alpha;Group.interactable=hide||read?false:Interactable;Group.blocksRaycasts=hide?false:Blocks;}
        }
        static GameObject ObjectOf(object value){var go=value as GameObject;if(go!=null)return go;var c=value as Component;return c==null?null:c.gameObject;}
        static void Set(GameObject go,bool hide,bool read=false)
        {if(go==null)return;Mask m;if(!hide&&!read){if(masks.TryGetValue(go,out m)){m.Set(false,false);masks.Remove(go);}return;}if(!masks.TryGetValue(go,out m))masks[go]=m=new Mask(go);m.Set(hide,read);}
        internal static void Restore(){FireHint.Clear();HiddenLayout.Restore();foreach(var entry in masks)entry.Value.Set(false,false);masks.Clear();}
        internal static void Room(bool hide)
        {
            roomTakeover=hide;
            var ui=Runtime.Get<GameUI>();var panel=ui==null?null:ui.CurrentTorpedoLaunchersUI;if(panel!=null)Panel(panel);
            if(!Runtime.Active||Runtime.Loading||!Runtime.ViewingStation){FireHint.Clear();HiddenLayout.Restore();}
            if(owner!=null)Set(owner.gameObject,hide||HiddenLayout.Active);
            Set(NativeSkin.ManualRoot,hide);
        }
        static bool iconApiChecked,iconApiFailed;
        static MethodInfo iconApi;
        static readonly Dictionary<string,Sprite> iconCache=new Dictionary<string,Sprite>();
        static Sprite CompatibleIcon(string name)
        {
            if(!iconApiChecked){iconApiChecked=true;var type=AccessTools.TypeByName("TorpedoTypeIcons.TorpedoIconSprites");if(type!=null)iconApi=AccessTools.Method(type,"TryGetLauncherByName");}
            if(iconApi==null||iconApiFailed||string.IsNullOrEmpty(name))return null;
            Sprite sprite;if(iconCache.TryGetValue(name,out sprite)&&sprite!=null)return sprite;
            try{object[] args={name,null};if((bool)iconApi.Invoke(null,args)){sprite=args[1] as Sprite;if(sprite!=null){iconCache[name]=sprite;return sprite;}}}
            catch(Exception e){iconApiFailed=true;Debug.LogWarning("[TorpedoRoom] Optional icon provider unavailable; using native sprites: "+e.Message);}
            return null;
        }
        internal static TorpedoLauncherUI TubeView(int index)
        {if(owner==null||owner.Launchers==null||Runtime.Ship==null||index<0||index>=Runtime.Ship.TorpedoLaunchers.Length)return null;foreach(var view in owner.Launchers)if(view!=null&&view.Launcher==Runtime.Ship.TorpedoLaunchers[index])return view;return null;}
        internal static Sprite SpriteFor(int index)
        {
            var ship=Runtime.Ship;if(ship==null||index<0||index>=ship.TorpedoLaunchers.Length)return null;
            var item=ship.TorpedoLaunchers[index]==null?null:ship.TorpedoLaunchers[index].Torpedo;if(item==null)return null;
            var custom=CompatibleIcon(item.Name);if(custom==null)custom=CompatibleIcon(item.name);if(custom!=null)return custom;
            if(owner==null||owner.Launchers==null)return null;
            foreach(var view in owner.Launchers)if(view!=null&&view.Launcher!=null&&Runtime.Ship!=null&&index<Runtime.Ship.TorpedoLaunchers.Length&&view.Launcher==Runtime.Ship.TorpedoLaunchers[index]){
                var image=Icon==null?null:Icon.GetValue(view) as Image;return image==null?null:image.sprite;
            }
            return null;
        }
        internal static void Refresh(TorpedoLaunchersUI panel)
        {
            if(panel==null)return;Panel(panel);bool viewing=Runtime.ViewingStation&&Runtime.Active&&!Runtime.Loading;
            bool hidden=viewing&&(Runtime.Viewport==ViewportMode.Hidden||MpScopeFire.Active);
            bool read=viewing&&(Runtime.Viewport!=ViewportMode.Synchronized||MpScopeFire.Active);
            HiddenLayout.Apply(panel,hidden&&!roomTakeover&&panel.IsOpened);
            Set(panel.gameObject,roomTakeover||HiddenLayout.Active||MpScopeFire.Active);
            foreach(var field in Reports)if(field!=null)Set(ObjectOf(field.GetValue(panel)),hidden,read);
            if(panel.Launchers!=null)foreach(var tube in panel.Launchers)if(tube!=null){
                Set(tube.gameObject,hidden); // read-only reports still permit stock tube selection/firing.
                if(Pistol!=null)Set(ObjectOf(Pistol.GetValue(tube)),hidden,read);
            }
            // The depth dial is a separate native component; masks do not include the fire button.
            foreach(var dial in depthDials)Set(dial,hidden,read);
            if(roomTakeover){if(owner!=null)Set(owner.gameObject,true);Set(NativeSkin.ManualRoot,true);}
        }
    }
}
