// Independently authored per-tube report checks; no native/HTT hover patches.
using System;
using System.Reflection;
using Landomatic.TorpedoRoom;
using UnityEngine;
using TMPro;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.UI;
public static class BadgeWarningChecks
{
    static int count;
    static void Check(bool ok,string label){count++;if(!ok)throw new Exception("FAILED: "+label);}
    static void Tick(TubeProgramBadge badge){Time.unscaledTime+=1;typeof(TubeProgramBadge).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(badge,null);}
    public static int Run()
    {
        const string t5="G7es Zaunkönig Torpedo T5 - Pi4";
        Check(ViewingFire.TubeDepthWarning(18,"ordinary")=="","normal viewing limit includes exactly 18 m");
        Check(ViewingFire.TubeDepthWarning(18.01f,"ordinary").Contains("TOO DEEP"),"non-T5 warning beyond 18 m");
        Check(ViewingFire.TubeDepthWarning(45,t5)==""&&ViewingFire.TubeDepthWarning(50,t5)=="","T5 individual report allows through 50 m");
        Check(ViewingFire.TubeDepthWarning(50.01f,t5).Contains("TOO DEEP"),"T5 report warns beyond 50 m without claiming physical lever interlock");
        Check(ViewingFire.TubeDepthWarning(float.NaN,t5)=="","unknown depth does not invent a tube restriction");
        Runtime.Active=true;Runtime.Loading=false;Runtime.CurrentGroup=null;Runtime.Viewport=ViewportMode.ReadOnly;
        var tube=new TorpedoLauncher{IsFlooded=false,IsMarkedForLaunch=false};tube.Torpedo.Data.Name="G7a Torpedo T1 - Pi1";
        Runtime.Ship=new PlayerShip{Depth=45,TorpedoLaunchers=new[]{tube}};
        var badge=new TubeProgramBadge{Owner=new TorpedoLauncherUI{Launcher=tube}};Tick(badge);
        var label=(TextMeshProUGUI)typeof(TubeProgramBadge).GetField("label",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(badge);
        Check(label.text.Contains("TOO DEEP"),"dry unselected loaded weapon gets individual depth warning");
        Check(label.text.StartsWith("LUT ")&&label.text.Contains("3.0 m"),"warning preserves existing program and running-depth report");
        Check(!label.raycastTarget&&!label.richText,"report cannot replace/consume vanilla or HTT hover");
        Check(label.rectTransform.sizeDelta.y==44,"expanded report has room for warning");
        tube.Torpedo.Data.Name=t5;Tick(badge);Check(!label.text.Contains("TOO DEEP")&&label.rectTransform.sizeDelta.y==30,"T5 clears individual warning at 45 m");
        Runtime.Ship.Depth=51;Tick(badge);Check(label.text.Contains("TOO DEEP"),"individual T5 warning updates with boat depth");
        Runtime.Ship.Depth=5;Tick(badge);Check(!label.text.Contains("TOO DEEP"),"ascending clears individual warning");
        Runtime.Ship.Depth=51;Runtime.Viewport=ViewportMode.Hidden;Tick(badge);Check(!label.gameObject.activeSelf,"Hidden Reports does not leak per-tube warning/info");
        Runtime.Viewport=ViewportMode.Synchronized;Tick(badge);Check(label.gameObject.activeSelf&&label.text.Contains("TOO DEEP"),"synchronized reports include individual warning");
        tube.Torpedo=null;Tick(badge);Check(!label.gameObject.activeSelf,"empty tube cannot retain stale warning");
        Console.WriteLine("PASS: "+count+" individual tube-depth report assertions (actual badge; independent fixtures).");return count;
    }
}
