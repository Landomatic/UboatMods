// Independently authored hover regressions; no live Unity rendering.
using System;
using System.Reflection;
using Landomatic.TorpedoRoom;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI.Periscope;
public static class TooltipChecks
{
    static int count;
    static void Check(bool ok,string label){count++;if(!ok)throw new Exception("FAILED: "+label);}
    static T Field<T>(string n){return (T)typeof(FireHint).GetField(n,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);}
    static void Frame(TorpedoLaunchersUI ui){
        typeof(ViewingFire).GetMethod("BeforeUpdate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        ui.fireButton.interactable=true;ui.firingBlockade.SetActive(Runtime.Ship.Depth>18);
        typeof(ViewingFire).GetMethod("AfterUpdate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{ui});
    }
    public static int Run()
    {
        foreach(var mode in new[]{ViewportMode.Synchronized,ViewportMode.ReadOnly,ViewportMode.Hidden}){
            NativePresentation.Restore();Runtime.Active=true;Runtime.Loading=false;Runtime.ViewingStation=true;Runtime.MpScopeFeuer=false;Runtime.Viewport=mode;
            var t5=new TorpedoLauncher{IsFlooded=true};t5.Torpedo.Data.Name="G7es Zaunkönig Torpedo T5 - Pi4";
            var ordinary=new TorpedoLauncher{IsFlooded=true};ordinary.Torpedo.Data.Name="G7a Torpedo T1 - Pi1";
            Runtime.Ship=new PlayerShip{Depth=45,TorpedoLaunchers=new[]{t5,ordinary},Boxes=new TorpedoFireControlBoxes{Selected=99}};
            var ui=new TorpedoLaunchersUI();ui.transform.SetParent(new GameObject("canvas").transform,false);ui.fireButton.transform.SetParent(ui.transform,false);
            Frame(ui);var hint=Field<GameObject>("hint");var label=Field<TextMeshProUGUI>("text");var hover=Field<TorpedoHover>("hover");
            Check(hint!=null&&!hint.activeSelf,"tooltip only shows on hover in "+mode);
            Check(!ui.fireButton.interactable&&!ui.firingBlockade.activeSelf,"depth still gates Feuer without covering panel in "+mode);
            hover.Enter();Check(hint.activeSelf&&label.text.StartsWith("Too deep"),"disabled Feuer depth explanation appears in "+mode);
            Check(!hint.GetComponent<Image>().raycastTarget&&!label.raycastTarget,"tooltip cannot intercept button or other controls in "+mode);
            Check(!ui.fireButton.interactable,"hover never enables restricted fire in "+mode);
            ordinary.IsMarkedForLaunch=false;Runtime.Ship.Boxes.Selected=1;Frame(ui);
            Check(ui.fireButton.interactable&&!label.text.Contains("Too deep"),"valid T5 selection immediately clears depth explanation in "+mode);
            Runtime.Ship.Depth=51;Frame(ui);Check(hint.activeSelf&&label.text.StartsWith("Too deep"),"crossing depth limit updates while pointer remains over button in "+mode);
            Runtime.Ship.Depth=5;Frame(ui);Check(ui.fireButton.interactable&&!label.text.Contains("Too deep"),"ascending clears stale depth restriction while hovered in "+mode);
            if(mode!=ViewportMode.Hidden)Check(!hint.activeSelf,"ordinary vanilla mode has no misleading ready-depth tooltip in "+mode);
            hover.Exit();Check(!hint.activeSelf,"pointer exit hides explanation in "+mode);
            Runtime.Ship.Depth=51;Frame(ui);hover.Enter();FireHint.Close(ui.fireButton);Check(Field<GameObject>("hint")==null&&!hint.activeSelf,"panel close clears tooltip in "+mode);
        }
        NativePresentation.Restore();Console.WriteLine("PASS: "+count+" Feuer tooltip assertions (actual classes; independent fixtures).");return count;
    }
}
