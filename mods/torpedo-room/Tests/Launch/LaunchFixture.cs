// Actual LaunchAuthority.cs against independent native API fixtures. Not installed.
using System;
using System.Collections.Generic;
using System.Reflection;
using Landomatic.TorpedoRoom;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Entities;
namespace HarmonyLib {
 public class Harmony {public void Patch(MethodBase m,HarmonyMethod pre=null,HarmonyMethod post=null){} }
 public class HarmonyMethod {public int priority;public HarmonyMethod(Type t,string n){} }
 public static class Priority {public const int First=800,Last=0;}
 public static class AccessTools {public static MethodInfo PropertySetter(Type t,string n){return t.GetProperty(n).GetSetMethod();}public static MethodInfo Method(Type t,string n){return t.GetMethod(n);} }
}
namespace UnityEngine {
 public static class Time {public static float unscaledTime=100;}
 public static class Mathf {public static float Clamp(float f,float lo,float hi){return Math.Max(lo,Math.Min(hi,f));}public static float Abs(float f){return Math.Abs(f);} }
 public static class Debug {public static List<string> Logs=new List<string>();public static int Warnings;public static void Log(object s){Logs.Add(s.ToString());}public static void LogWarning(object s){Warnings++;Logs.Add(s.ToString());} }
}
namespace UBOAT.Game.Scene.Items {
 public class StoredTorpedo {public PreparationStatus Saved=new PreparationStatus();public Torpedo Missile;public StoredTorpedo(){Missile=new Torpedo{Item=this};} }
 public class TorpedoLauncherGroup {public object ParentEntity;}
 public class TorpedoLauncher {public StoredTorpedo Torpedo=new StoredTorpedo();public TorpedoLauncherGroup Group=new TorpedoLauncherGroup();public bool IsFlooded=true;
  public void Launch(bool torpedoProgramDirection,float torpedoProgramDistance,float torpedoProgramAngleAdjust){} }
}
namespace UBOAT.Game.Scene.Entities {
 public class Torpedo {public enum TorpedoProgramType {None,FAT,LUT}public TorpedoProgramType ProgramType=TorpedoProgramType.LUT;public int Pistol;public float TargetDepth {get;set;}public StoredTorpedo Item;public T GetComponent<T>() where T:class {return Item as T;}public void SetProgramParameters(){} }
}
namespace Landomatic.TorpedoRoom {
 public class PreparationStatus {public Preparation Value=new Preparation {ProgramOverride=true,Distance=2500,Right=true,Angle=210};}
 internal static class Bridge {public static bool IsRemote=false;}
 internal static class Runtime {
  public static bool Active=true,Loading=false,LockoutEnabled=true;public static object Ship=new object();
  public static Torpedo Missile(StoredTorpedo item){return item==null?null:item.Missile;}
  public static PreparationStatus Status(StoredTorpedo item,bool create){return item==null?null:item.Saved;}
  public static void SetPistol(Torpedo missile,int p){missile.Pistol=p;}public static int IndexOf(Torpedo t){return 1;}public static string Roman(int i){return (i+1).ToString();}
 }
}
public static class LaunchChecks {
 static int count;
 static void Check(bool b,string s){count++;if(!b)throw new Exception("FAILED: "+s);}
 static LaunchAuthority.Audit Set(Torpedo torpedo,ref bool right,ref float distance,ref float angle,ref Torpedo.TorpedoProgramType type)
 {LaunchAuthority.Audit audit;LaunchAuthority.BeforeProgram(torpedo,ref right,ref distance,ref angle,ref type,out audit);return audit;}
 public static int Run(){
  var tube=new TorpedoLauncher();tube.Group.ParentEntity=Runtime.Ship;var item=tube.Torpedo;var torpedo=item.Missile;
  LaunchAuthority.Clear();LaunchAuthority.Install(new HarmonyLib.Harmony());LaunchAuthority.Capture(tube);
  item.Saved.Value.DepthOverride=true;item.Saved.Value.Depth=7;LaunchAuthority.Capture(tube);float depth=3;LaunchAuthority.LaunchDepth(tube,ref depth);Check(depth==7,"per-tube launch depth overrides native shared depth");
  depth=2;LaunchAuthority.FinalDepth(torpedo,ref depth);Check(depth==7,"final depth setter protects captured depth");
  float speed=23;LaunchAuthority.LaunchSpeed(tube,ref speed);Check(speed==23,"inherited speed leaves native launch input intact");
  item.Saved.Value.SpeedOverride=true;item.Saved.Value.Speed=15.43f;LaunchAuthority.Capture(tube);speed=23;LaunchAuthority.LaunchSpeed(tube,ref speed);Check(speed==15.43f,"captured per-tube speed overrides shared launch argument");
  item.Saved.Value.Speed=20.57f;speed=23;LaunchAuthority.LaunchSpeed(tube,ref speed);Check(speed==15.43f,"later edits cannot replace captured speed");
  Runtime.Loading=true;speed=23;LaunchAuthority.LaunchSpeed(tube,ref speed);Check(speed==23,"loading guard prevents speed override");Runtime.Loading=false;
  item.Saved.Value.SpeedOverride=false;LaunchAuthority.Capture(tube);
  bool right=false;float distance=500,angle=0;var type=Torpedo.TorpedoProgramType.LUT;
  LaunchAuthority.OverrideLauncher(tube,ref right,ref distance,ref angle);
  Check(right&&distance==2500&&angle==210,"typed launcher references replace UZO 500-left with room 2500-right");
  right=false;distance=500;angle=0;
  var audit=Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(audit!=null&&right&&distance==2500&&angle==210,"final torpedo setter independently overrides stale incoming program");
  LaunchAuthority.AfterProgram(audit,type,right,distance-100,angle);
  Check(UnityEngine.Debug.Logs[UnityEngine.Debug.Logs.Count-1].Contains("verified=True"),"native LUT minus-100 distance audited correctly");
  Check(UnityEngine.Debug.Logs[UnityEngine.Debug.Logs.Count-1].Contains("requested=2500m/R"),"launch log names authoritative requested values");
  LaunchAuthority.AfterProgram(audit,type,false,400,0);Check(UnityEngine.Debug.Warnings==1,"native mismatch explicitly warned");
  var source=item.Saved;source.Value.Distance=9000;source.Value.Right=false;tube.Torpedo=null;item.Saved=null;
  right=false;distance=500;angle=0;audit=Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(audit!=null&&right&&distance==2500,"capture survives item detachment and later status change/removal");
  right=false;distance=500;angle=0;audit=Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(right&&distance==2500,"late setter replay cannot replace captured room program");
  item.Saved=source;tube.Torpedo=item;source.Value.ProgramOverride=false;source.Value.StraightRun=false;LaunchAuthority.Capture(tube);
  right=false;distance=500;angle=0;audit=Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(!right&&distance==500&&angle==0,"per-tube OFF preserves stock shared arguments");
  LaunchAuthority.AfterProgram(audit,type,false,400,0);Check(UnityEngine.Debug.Logs[UnityEngine.Debug.Logs.Count-1].Contains("source=STOCK"),"stock ownership reported in audit");
  source.Value.StraightRun=true;LaunchAuthority.Capture(tube);Runtime.LockoutEnabled=false;
  type=Torpedo.TorpedoProgramType.LUT;right=false;distance=500;angle=0;audit=Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(type==Torpedo.TorpedoProgramType.None,"launch pin remains authoritative after later host setting change");
  LaunchAuthority.AfterProgram(audit,type,false,500,0);Check(UnityEngine.Debug.Logs[UnityEngine.Debug.Logs.Count-1].Contains("source=STRAIGHT"),"straight-run source logged");
  LaunchAuthority.Capture(tube);type=Torpedo.TorpedoProgramType.LUT;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(type==Torpedo.TorpedoProgramType.LUT,"new attempt captures changed host pin availability");
  source.Value.ProgramOverride=true;source.Value.Distance=2500;source.Value.Right=true;source.Value.StraightRun=false;
  LaunchAuthority.Capture(tube);Runtime.Active=false;LaunchAuthority.Capture(tube);distance=500;right=false;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(distance==500&&!right,"disabled mod clears an old un-fired attempt");Runtime.Active=true;
  tube.IsFlooded=false;LaunchAuthority.Capture(tube);distance=500;right=false;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(distance==500&&!right,"dry-tube attempt cannot capture a launch");tube.IsFlooded=true;
  tube.Group.ParentEntity=new object();LaunchAuthority.Capture(tube);distance=500;right=false;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(distance==500&&!right,"non-player tube has no authority capture");tube.Group.ParentEntity=Runtime.Ship;
  LaunchAuthority.Capture(tube);UnityEngine.Time.unscaledTime+=21;LaunchAuthority.Tick();distance=500;right=false;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(distance==500&&!right,"bounded launch capture expires without retaining stale objects");
  LaunchAuthority.Capture(tube);LaunchAuthority.Clear();distance=500;right=false;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(distance==500&&!right,"world reset clears captured authority");
  LaunchAuthority.Capture(tube);Runtime.Loading=true;distance=500;right=false;Set(torpedo,ref right,ref distance,ref angle,ref type);
  Check(distance==500&&!right,"load/save guard suppresses program override");Runtime.Loading=false;
  Console.WriteLine("PASS: "+count+" launch-authority assertions (actual LaunchAuthority.cs, native API fixtures).");return count;
 }
}
