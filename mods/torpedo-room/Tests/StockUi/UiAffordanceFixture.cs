// Independently authored UI regression checks; not installed with the mod.
using System;
using Landomatic.TorpedoRoom;
using UnityEngine;
using UnityEngine.UI;
using UBOAT.Game.UI.Periscope;
public static class UiAffordanceChecks
{
    static int count;
    static void Check(bool ok,string label){count++;if(!ok)throw new Exception("FAILED: "+label);}
    static bool Overlap(FireDock.Box a,FireDock.Box b){return a.Left<b.Right&&a.Right>b.Left&&a.Bottom<b.Top&&a.Top>b.Bottom;}
    public static int Run()
    {
        var a=PrepSelection.Create(new GameObject("tube one").transform,new Color(1,1,0));
        var b=PrepSelection.Create(new GameObject("tube five").transform,new Color(1,1,0));
        Check(!a.Root.activeSelf&&!b.Root.activeSelf,"unselected Prep borders initially hidden");
        Check(a.Edges.Length==4,"four independent border edges around transparent/native artwork");
        foreach(var edge in a.Edges)Check(!edge.raycastTarget,"outline cannot intercept torpedo/pistol/hover input");
        a.Select(true);Check(a.Root.activeSelf&&!b.Root.activeSelf,"only first edited tube outlined");
        a.Select(false);b.Select(true);Check(!a.Root.activeSelf&&b.Root.activeSelf,"outline moves to new edited tube");
        b.Select(false);Check(!a.Root.activeSelf&&!b.Root.activeSelf,"closing/unloading clears selected outline");
        var root=(RectTransform)a.Root.transform;Check(root.anchorMin.x==0&&root.anchorMax.x==1&&root.sizeDelta.x==-6,"border follows card size with inset");
        foreach(float scale in new[]{.5f,1f,1.5f,2f}){
            var icon=new FireDock.Box(100*scale,20*scale,140*scale,60*scale);
            var bounds=new FireDock.Box(0,0,800*scale,600*scale);float gap=12*scale;
            var free=FireDock.Beside(icon,150*scale,60*scale,gap,bounds,new FireDock.Box[0]);
            Check(Math.Abs(free.Left-(icon.Right+gap))<.01,"right-side scaled gap at "+scale);
            Check(Math.Abs((free.Top+free.Bottom)/2-(icon.Top+icon.Bottom)/2)<.01,"vertical icon alignment at "+scale);
            var neighbour=new FireDock.Box(145*scale,15*scale,220*scale,65*scale);
            var raised=FireDock.Beside(icon,150*scale,60*scale,gap,bounds,new[]{neighbour});
            Check(!Overlap(raised,neighbour)&&!Overlap(raised,icon),"neighbouring icon has clear enlarged Feuer at "+scale);
            Check(raised.Bottom>=neighbour.Top+gap-.01f,"crowded row fallback sits above tray at "+scale);
            var edge=FireDock.Beside(new FireDock.Box(750*scale,20*scale,790*scale,60*scale),150*scale,60*scale,gap,bounds,new FireDock.Box[0]);
            Check(edge.Right<=bounds.Right-gap+.01f&&!Overlap(edge,new FireDock.Box(750*scale,20*scale,790*scale,60*scale)),"right screen boundary and opener remain clear at "+scale);
        }
        // Actual follow path retains the original button outside a disabled icon.
        var canvas=new GameObject("canvas");((RectTransform)canvas.transform).sizeDelta=new Vector2(800,600);
        var panel=new TorpedoLaunchersUI();panel.transform.SetParent(canvas.transform,false);
        var fire=panel.fireButton.transform as RectTransform;fire.SetParent(canvas.transform,false);fire.sizeDelta=new Vector2(100,40);fire.localScale=new Vector3(1.5f,1.5f,1);
        var scope=new PeriscopeUI{selectTorpedoButton=new Toggle()};var iconRect=(RectTransform)scope.selectTorpedoButton.transform;iconRect.SetParent(canvas.transform,false);iconRect.position=new Vector3(100,40,0);iconRect.sizeDelta=new Vector2(40,40);iconRect.pivot=new Vector2(.5f,.5f);
        Runtime.ScopeUi=scope;Time.unscaledTime=100;FireDock.Clear();FireDock.Follow(panel,fire);
        Check(fire.parent==canvas.transform&&!fire.IsChildOf(iconRect),"Feuer stays outside icon/panel masking hierarchy");
        Check(Math.Abs(fire.localPosition.x-130)<.01,"actual follow anchors right of current icon rectangle");
        iconRect.position=new Vector3(200,40,0);FireDock.Follow(panel,fire);Check(Math.Abs(fire.localPosition.x-230)<.01,"moving icon moves Feuer without reopening");
        Check(fire.localScale.x==1.5f,"repeated layout does not compound 1.5 scale");
        scope.selectTorpedoButton.gameObject.SetActive(false);Time.unscaledTime=101;FireDock.Follow(panel,fire);Check(fire.gameObject.activeSelf,"missing/hidden opener cannot hide the live fire button");
        Runtime.ScopeUi=null;FireDock.Clear();
        Console.WriteLine("PASS: "+count+" Prep-outline/Feuer-layout assertions (actual helpers; no live rendering).");return count;
    }
}
