// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Actual TdcSpeed/SpeedControl against independent calculator and MP fixtures.
using System;
using System.Reflection;
using Landomatic.TorpedoRoom;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Entities;
public static class TdcChecks
{
 static int count;
 static void Check(bool ok,string label){count++;if(!ok)throw new Exception("FAILED: "+label);}
 static object Call(string name,params object[] args){return typeof(TdcSpeed).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
 static bool Near(float a,float b){return Math.Abs(a-b)<.001f;}
 // Native preset algorithm: exact integer lookup, fallback index zero, then wrap.
 static void Click(TorpedoCalculator t,bool forward)
 {var args=new object[]{new TorpedoCalculatorTorpedoSpeedDial(),t,(int)t.TorpedoSpeedTarget};Call("PresetIdentity",args);int[] presets={30,40,44};int index=Array.IndexOf(presets,(int)args[2]);if(index<0)index=0;t.TorpedoSpeedTarget=presets[(index+(forward?1:2))%3];}
 public static int Run()
 {
  Runtime.Active=true;Runtime.SpeedSync=true;Runtime.Loading=false;Runtime.Applying=0;Runtime.Accept=true;Runtime.Viewport=ViewportMode.Synchronized;Bridge.IsRemote=false;Bridge.Synced=true;
  var a=new TorpedoLauncher();var b=new TorpedoLauncher();b.IsMarkedForLaunch=false;
  var ship=new PlayerShip{TorpedoLaunchers=new[]{a,b},Computer=new TorpedoCalculator()};Runtime.Ship=ship;a.Group.ParentEntity=b.Group.ParentEntity=ship;
  var t=ship.Computer;TdcSpeed.Reset();TdcSpeed.Install(new HarmonyLib.Harmony());
  Check(HarmonyLib.Harmony.Targets.Count==3&&!HarmonyLib.Harmony.Targets.Exists(x=>x.Contains("Launch")),"TDC installation has no launch/salvo patches");
  Check(typeof(TdcSpeed).GetMethod("BeforeLaunch",BindingFlags.NonPublic|BindingFlags.Static)==null&&typeof(TdcSpeed).GetMethod("BeforeSalvo",BindingFlags.NonPublic|BindingFlags.Static)==null,"mixed speed firing interlocks removed entirely");
  TorpedoCalculator.Before=x=>(bool)Call("BeforeDisplayWrite",x);TorpedoCalculator.After=(x,v)=>Call("AfterTarget",x,v);
  a.Torpedo.Status.Value.SpeedOverride=true;a.Torpedo.Status.Value.Speed=23.15f;t.TargetSpeed=12;
  TdcSpeed.Sync();Check(Near(t.TorpedoSpeed,23.15f*SpeedValues.Knots)&&Near(t.TorpedoSpeedTarget,t.TorpedoSpeed),"room speed updates actual and target TDC knots");
  Check(Near(ship.TorpedoAttackSettings.TorpedoSpeed,23.15f)&&Near(t.TargetSpeed,12),"shared m/s updated without changing target estimate");
  int changes=t.Changes;TdcSpeed.Sync();Check(t.Changes==changes&&Runtime.Applying==0,"idle sync avoids repeated setter/events");
  Runtime.Applying++;float previous=t.TorpedoSpeed;t.TorpedoSpeed=30;t.TorpedoSpeedTarget=30;Runtime.Applying--;Check(t.TorpedoSpeed==previous&&t.TorpedoSpeedTarget==previous,"display refresh cannot overwrite computer inputs");
  t.TorpedoSpeedTarget=40;Check(Near(a.Torpedo.Status.Value.Speed,20.57f)&&!b.Torpedo.Status.Value.SpeedOverride,"TDC input updates selected tube only");
  float[] physical={23.15f,15.43f,20.57f,23.15f,15.43f,20.57f};
  foreach(float expected in physical){Click(t,true);Check(Near(a.Torpedo.Status.Value.Speed,expected)&&Near(t.TorpedoSpeedTarget,expected*SpeedValues.Knots),"left click cycles 40→44→30→40 with exact physical speeds");Check(Near(t.TargetSpeed,12),"native cycle preserves target estimate");}
  Click(t,false);Check(Near(a.Torpedo.Status.Value.Speed,15.43f),"reverse click 40→30");Click(t,false);Check(Near(a.Torpedo.Status.Value.Speed,23.15f),"reverse wraps 30→44");
  foreach(ViewportMode mode in Enum.GetValues(typeof(ViewportMode))){
   Runtime.Viewport=mode;t.TorpedoSpeedTarget=40;Check(Near(a.Torpedo.Status.Value.Speed,20.57f),"TDC→tube sync independent of report mode "+mode);
   a.Torpedo.Status.Value.Speed=23.15f;TdcSpeed.Sync();Check(Near(t.TorpedoSpeed,23.15f*SpeedValues.Knots),"tube→TDC sync independent of report mode "+mode);
   Runtime.SpeedSync=false;int accepted=Runtime.Accepted;t.TorpedoSpeedTarget=30;t.TorpedoSpeed=30;TdcSpeed.Sync();
   Check(Runtime.Accepted==accepted&&Near(a.Torpedo.Status.Value.Speed,23.15f)&&t.TorpedoSpeed==30,"OFF retains independent tube/TDC settings "+mode);
   Check(TdcSpeed.Warning().Contains("Firing remains available"),"OFF mismatch is informational only "+mode);Runtime.SpeedSync=true;
  }
  Runtime.Viewport=ViewportMode.Synchronized;b.IsMarkedForLaunch=true;b.Torpedo.Status.Value.SpeedOverride=true;b.Torpedo.Status.Value.Speed=15.43f;
  previous=t.TorpedoSpeed;TdcSpeed.Sync();Check(t.TorpedoSpeed==previous&&TdcSpeed.Warning().Contains("MIXED"),"mixed speeds remain different and produce warning only");
  b.Torpedo.Speed1=b.Torpedo.Speed2=b.Torpedo.Speed3=15.43f;t.TorpedoSpeedTarget=44;
  Check(Near(a.Torpedo.Status.Value.Speed,15.43f)&&Near(b.Torpedo.Status.Value.Speed,15.43f),"explicit group TDC edit uses supported speed intersection");
  b.IsMarkedForLaunch=false;a.Torpedo.Status.Value.Speed=23.15f;TdcSpeed.Sync();int before=Runtime.Accepted;
  Runtime.Loading=true;t.TorpedoSpeedTarget=35;TdcSpeed.Sync();Check(Runtime.Accepted==before,"loading does not edit preparations");Runtime.Loading=false;
  Runtime.Accept=false;t.TorpedoSpeedTarget=30;Check(Runtime.Accepted==before&&Near(a.Torpedo.Status.Value.Speed,23.15f),"host rejection keeps tube settings");Runtime.Accept=true;TdcSpeed.Sync();
  var foreign=new TorpedoCalculator();foreign.TorpedoSpeedTarget=30;Check(Runtime.Accepted==before,"foreign computer cannot edit own tubes");
  Bridge.IsRemote=true;Bridge.Synced=false;t.TorpedoSpeed=30;TdcSpeed.Sync();Check(t.TorpedoSpeed==30,"unsynced joiner does not publish guessed speed");
  Bridge.Synced=true;Bridge.Last=new Snapshot{SolutionSpeed=23.15f};TdcSpeed.Sync();Check(Near(t.TorpedoSpeed,23.15f*SpeedValues.Knots),"joiner uses host-confirmed speed");
  before=Runtime.Accepted;t.TorpedoSpeedTarget=40;TdcSpeed.Sync();Check(t.TorpedoSpeedTarget==40&&Runtime.Accepted==before,"joiner waits for native MP TDC transport");
  a.Torpedo.Status.Value.Speed=20.57f;Bridge.Last.SolutionSpeed=20.57f;TdcSpeed.Sync();Check(Near(t.TorpedoSpeed,20.57f*SpeedValues.Knots),"host confirmation releases pending edit");
  t.TorpedoSpeedTarget=44;UnityEngine.Time.unscaledTime+=3.1f;TdcSpeed.Sync();Check(Near(t.TorpedoSpeedTarget,20.57f*SpeedValues.Knots),"unconfirmed edit expires to host speed");Bridge.IsRemote=false;
  ship.Computer.enabled=false;Runtime.SpeedSync=false;TdcSpeed.Reset();ship.TorpedoAttackSettings.TorpedoSpeed=12;TdcSpeed.Sync();Check(ship.TorpedoAttackSettings.TorpedoSpeed==12,"OFF also independent on boat without active computer");Runtime.SpeedSync=true;TdcSpeed.Sync();Check(Near(ship.TorpedoAttackSettings.TorpedoSpeed,20.57f),"ON updates shared speed without active computer");
  a.IsMarkedForLaunch=b.IsMarkedForLaunch=false;ship.Computer.enabled=true;TdcSpeed.Reset();TdcSpeed.Applied(a);
  Check(Near(t.TorpedoSpeed,20.57f*SpeedValues.Knots)&&TdcSpeed.FocusIndex==0,"Apply without marks establishes speed focus");
  b.Torpedo.Status.Value.Speed=15.43f;TdcSpeed.Applied(b);Check(TdcSpeed.FocusIndex==1&&Near(t.TorpedoSpeed,15.43f*SpeedValues.Knots),"Apply another unmarked tube changes focus");TdcSpeed.Applied(a);
  t.TorpedoSpeedTarget=44;Check(Near(a.Torpedo.Status.Value.Speed,23.15f),"TDC input updates unmarked focus");
  Bridge.IsRemote=true;Bridge.Last=new Snapshot{SolutionSpeed=20.57f,SpeedTube=0};a.Torpedo.Status.Value.Speed=20.57f;TdcSpeed.Reset();TdcSpeed.Sync();Check(Near(t.TorpedoSpeed,20.57f*SpeedValues.Knots),"host focus reaches unmarked joiner");Bridge.IsRemote=false;
  a.Torpedo=new StoredTorpedo();TdcSpeed.Sync();Check(TdcSpeed.ReportSpeed==0,"replacement clears stale focus");
  Runtime.Active=false;ship.TorpedoAttackSettings.TorpedoSpeed=12;TdcSpeed.Sync();Check(ship.TorpedoAttackSettings.TorpedoSpeed==12,"disabled mod releases speed");Runtime.Active=true;
  TdcSpeed.Reset();TorpedoCalculator.Before=null;TorpedoCalculator.After=null;
  Console.WriteLine("PASS: "+count+" TDC speed assertions (actual synchronization; native preset algorithm and MP fixtures).");return count;
 }
}
