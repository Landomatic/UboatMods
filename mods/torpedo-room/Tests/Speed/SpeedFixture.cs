// Actual SpeedControl.cs against independent native API fixtures. Not installed.
using System;
using System.Reflection;
using System.Collections.Generic;
using Landomatic.TorpedoRoom;
using UBOAT.Game.Scene.Items;
namespace HarmonyLib {
 public class Harmony {public static List<string> Targets=new List<string>();public void Patch(object target,HarmonyMethod prefix=null,HarmonyMethod postfix=null,HarmonyMethod transpiler=null,HarmonyMethod finalizer=null){Targets.Add(((MethodInfo)target).Name);}}
 public class HarmonyMethod {public int priority;public HarmonyMethod(Type t,string name){}}
 public static class Priority {public const int First=800;}
 public static class AccessTools {public static Type TypeByName(string name){return null;}public static FieldInfo Field(Type t,string name){return t.GetField(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);}public static MethodInfo PropertySetter(Type t,string name){return t.GetProperty(name).GetSetMethod();}public static MethodInfo Method(Type t,string name){return t.GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);}}
}
namespace UnityEngine {
 public static class Time {public static float unscaledTime=100;}
 public class GameObject {public bool activeSelf=true;public void SetActive(bool value){activeSelf=value;}}
 public class Transform {public Vector3 localEulerAngles;public Transform parent;public GameObject gameObject=new GameObject();}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public static class Mathf {public static float InverseLerp(float lo,float hi,float value){return hi==lo?0:(value-lo)/(hi-lo);}}
}
namespace UnityEngine.UI {public class Slider {
 float val,minimum=10,maximum=24;public UnityEngine.GameObject gameObject=new UnityEngine.GameObject();public Action<float> Callback;public int Notifications;
 public float value{get{return val;}set{Set(value,true);}}public float minValue{get{return minimum;}set{minimum=value;maximum=Math.Max(minimum,maximum);Set(val,true);}}public float maxValue{get{return maximum;}set{maximum=value;minimum=Math.Min(minimum,maximum);Set(val,true);}}
 void Set(float value,bool notify){float next=Math.Max(minimum,Math.Min(maximum,value));if(next==val)return;val=next;if(notify){Notifications++;if(Callback!=null)Callback(next);}}
 public void SetValueWithoutNotify(float v){Set(v,false);}
 }}
namespace TMPro {public class TextMeshProUGUI {public string text;public UnityEngine.Transform transform=new UnityEngine.Transform{parent=new UnityEngine.Transform()};}}
namespace UBOAT.Game.Scene.Entities {
 public class Torpedo {}
 public class PlayerShipTorpedoAttackSettings {public float TorpedoSpeed=23.15f;}
 public class PlayerShip {public PlayerShipTorpedoAttackSettings TorpedoAttackSettings=new PlayerShipTorpedoAttackSettings();public TorpedoLauncher[] TorpedoLaunchers;public TorpedoCalculator Computer;public T GetEquipment<T>() where T:class{return Computer as T;}public void LaunchAllTorpedoes(){}}
}
namespace UBOAT.Game.Core.Data {public class UserSettings {public Gameplay GameplaySettings=new Gameplay();}public class Gameplay {public int units=1;}}
namespace UBOAT.Game.Core {public static class UnitsUtility {public static UBOAT.Game.Core.Data.UserSettings userSettings=new UBOAT.Game.Core.Data.UserSettings();}}
namespace UBOAT.Game.Scene.Items {
 public class TorpedoCalculatorTorpedoSpeedDial {public struct Preset {public int Value;}public Preset[] presets={new Preset{Value=30},new Preset{Value=40},new Preset{Value=44}};public int Value{get{return 0;}}}

 public class Modifier {public float Value=3600;}
 public class StoredTorpedo {public string TypeName="G7a T1";public bool IsMaintained;public float MaintenanceCooldownProgress;public Modifier MaintenanceCooldown=new Modifier();public float Speed1=23.15f,Speed2=20.57f,Speed3=15.43f;public PreparationStatus Status=new PreparationStatus();}
 public class TorpedoCalculator {
 public static Func<TorpedoCalculator,bool> Before;public static Action<TorpedoCalculator,float> After;
 public bool enabled=true;float speed=30,target=30;public float TargetSpeed=10;public int Changes;
 public float TorpedoSpeed {get{return speed;}set{if(Before!=null&&!Before(this))return;value=Math.Max(1,Math.Min(200,value));if(speed==value)return;TargetSpeed=Math.Min(55,TargetSpeed*value/speed);speed=value;Changes++;}}
 public float TorpedoSpeedTarget {get{return target;}set{if(Before!=null&&!Before(this))return;target=value;if(After!=null)After(this,value);}}
 }
 public class TorpedoLauncherGroup {public object ParentEntity;}
 public class TorpedoLauncher {public TorpedoLauncherGroup Group=new TorpedoLauncherGroup();public void Launch(){}public bool IsMarkedForLaunch=true,IsFlooded,Stable=true;public StoredTorpedo Torpedo=new StoredTorpedo();}
}
namespace UBOAT.Game.UI {public class TorpedoLaunchersSpeedDialUI {void OnValueChanged(){}void ValidateState(){}}}
namespace Landomatic.TorpedoRoom {
 public class PreparationStatus {public Preparation Value=new Preparation();}
 internal static class Runtime {
  public static bool SpeedSync=true;public static bool Active=true,Loading,RoomEditorOpen,ViewingStation=true;public static int Applying,Accepted;public static object CurrentGroup;public static ViewportMode Viewport=ViewportMode.Synchronized;
  public static UBOAT.Game.Scene.Entities.PlayerShip Ship;
  public static bool Stable(TorpedoLauncher t){return t.Stable;}
  public static UBOAT.Game.Scene.Entities.Torpedo Missile(StoredTorpedo t){return t==null?null:new UBOAT.Game.Scene.Entities.Torpedo();}
  public static PreparationStatus Status(StoredTorpedo t,bool create){return t==null?null:t.Status;}
  public static bool Accept=true;public static List<string> Notices=new List<string>();public static void Say(string s){Notices.Add(s);}
  public static bool AcceptViewport(TubeSnapshot[] edits,object peer,bool tdc=false){if(!Accept)return false;Accepted+=edits.Length;foreach(var edit in edits)Ship.TorpedoLaunchers[edit.Index].Torpedo.Status.Value=edit.Settings;return true;}
 }
 internal static class Bridge {public static bool IsRemote,Synced=true;public static Snapshot Last;public static TubeSnapshot[] Sent;public static void RequestViewport(TubeSnapshot[] edits){Sent=edits;}}
}
public static class SpeedChecks {
 static int count;static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
 static object Call(string name,params object[] args){var method=typeof(SpeedControl).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);var values=new object[method.GetParameters().Length];Array.Copy(args,values,args.Length);var result=method.Invoke(null,values);Array.Copy(values,args,args.Length);return result;}
 public static int Run(){
  Runtime.Ship=new UBOAT.Game.Scene.Entities.PlayerShip{TorpedoLaunchers=new[]{new TorpedoLauncher(),new TorpedoLauncher()}};var a=Runtime.Ship.TorpedoLaunchers[0];var b=Runtime.Ship.TorpedoLaunchers[1];
  Check(SpeedControl.Options(a.Torpedo).Length==3,"reads actual stored speed presets");
  a.Torpedo.Speed1=a.Torpedo.Speed2=a.Torpedo.Speed3=15.43f;Check(SpeedControl.Options(a.Torpedo).Length==1&&SpeedControl.Effective(a.Torpedo,a.Torpedo.Status.Value,23)==15.43f,"electric weapon displays its actual fixed speed");
  a.Torpedo.Speed1=23.15f;a.Torpedo.Speed2=20.57f;b.IsMarkedForLaunch=false;
  var args=new object[]{20.57f,false,false};Check((bool)Call("BeforeEdit",args)&&(bool)args[2],"manual synchronized speed edit admitted");
  var slider=new UnityEngine.UI.Slider{value=20.57f};Call("AfterEdit",true,slider);Check(a.Torpedo.Status.Value.SpeedOverride&&a.Torpedo.Status.Value.Speed==20.57f&&!b.Torpedo.Status.Value.SpeedOverride,"viewport imports only marked tube speed");
  Check(Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed==20.57f,"manual native dial retains the normal shared-default change");
  Runtime.Applying=1;args=new object[]{15.43f,false,false};Call("BeforeEdit",args);Check(!(bool)args[2],"automatic display callback cannot create speed edit");Runtime.Applying=0;
  args=new object[]{15.43f,true,false};Call("BeforeEdit",args);Check(!(bool)args[2],"native recursion guard is respected");
  Runtime.Viewport=ViewportMode.ReadOnly;args=new object[]{15.43f,false,false};Check(!(bool)Call("BeforeEdit",args),"read-only viewport blocks native speed changes");Runtime.Viewport=ViewportMode.Hidden;Check(!(bool)Call("BeforeEdit",args),"hidden viewport blocks speed changes");
  Runtime.Viewport=ViewportMode.Synchronized;Runtime.RoomEditorOpen=true;Check(!(bool)Call("BeforeEdit",args),"modal editor blocks native speed clicks behind it");Runtime.RoomEditorOpen=false;
  args=new object[]{float.NaN,false,false};Check(!(bool)Call("BeforeEdit",args),"nonfinite native speed rejected");
  slider.value=18;int accepted=Runtime.Accepted;Call("AfterEdit",true,slider);Check(Runtime.Accepted==accepted,"unsupported in-range native speed produces no edit");
  slider.value=15.43f;Bridge.IsRemote=true;Call("AfterEdit",true,slider);Check(Bridge.Sent.Length==1&&Bridge.Sent[0].Settings.Speed==15.43f&&a.Torpedo.Status.Value.Speed==20.57f,"remote speed awaits host confirmation");Bridge.IsRemote=false;
  Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed=23.15f;args=new object[]{null};Call("Project",args);var state=(SpeedControl.Projection)args[0];Check(state!=null&&Runtime.Applying==1&&Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed==20.57f,"native display temporarily projects marked speed");
  var label=new TMPro.TextMeshProUGUI();var pointer=new UnityEngine.Transform();Call("Display",state,slider,label,pointer);Check(slider.value==20.57f&&label.text.Contains("40.0")&&Runtime.Applying==0&&Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed==23.15f,"speed readout/needle projection restores shared default");
  Call("Finally",null,state);Check(Runtime.Applying==0,"display finalizer restores exactly once");
  b.IsMarkedForLaunch=true;b.Torpedo.Status.Value.SpeedOverride=true;b.Torpedo.Status.Value.Speed=15.43f;args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Call("Display",state,slider,label,pointer);Check(label.text=="MIXED"&&Runtime.Applying==0,"mixed marked speeds are acknowledged rather than claimed equal");
  b.IsMarkedForLaunch=false;args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Call("Finally",new Exception("synthetic display failure"),state);Check(Runtime.Applying==0&&Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed==23.15f,"exception path restores shared speed and guard");
  b.IsMarkedForLaunch=true;b.Torpedo.Speed1=b.Torpedo.Speed2=b.Torpedo.Speed3=15.43f;slider.value=20.57f;accepted=Runtime.Accepted;float shared=Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed;Call("AfterEdit",true,slider);
  Check(Runtime.Accepted==accepted&&Runtime.Ship.TorpedoAttackSettings.TorpedoSpeed==shared,"incompatible later group member prevents partial speed edit and shared-default change");
  b.IsMarkedForLaunch=false;Runtime.CurrentGroup=new object();args=new object[]{15.43f,false,false};Check((bool)Call("BeforeEdit",args)&&!(bool)args[2],"closed native room control keeps native shared behavior without forged viewport edit");Runtime.CurrentGroup=null;
  Runtime.ViewingStation=false;Runtime.CurrentGroup=new object();args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Check(state!=null&&state.Value==a.Torpedo.Status.Value.Speed,"marked-tube speed also projects while operating from the room without requiring a scope visit");Call("Finally",null,state);Runtime.ViewingStation=true;Runtime.CurrentGroup=null;
  a.IsFlooded=false;b.IsMarkedForLaunch=false;var choices=SpeedControl.MarkedOptions();Check(choices.Length==3&&choices[choices.Length-1]==a.Torpedo.Speed1,"stable dry marked T1 contributes all presets before flooding");
  var buffer=new List<float>{15.43f};var nativeSlider=new UnityEngine.UI.Slider();nativeSlider.minValue=nativeSlider.maxValue=15.43f;var blockade=new UnityEngine.GameObject();
  args=new object[]{a.Torpedo.Speed1,false,false,buffer,nativeSlider};Check((bool)Call("BeforeEdit",args)&&buffer.Count==3&&nativeSlider.maxValue==a.Torpedo.Speed1,"manual viewport entry replaces stale fixed-speed buffer and max bound before native clamping");
  nativeSlider.value=SpeedValues.Nearest(buffer.ToArray(),a.Torpedo.Speed1);Call("AfterEdit",args[2],nativeSlider);Check(a.Torpedo.Status.Value.Speed==a.Torpedo.Speed1,"viewport can commit maximum speed for dry marked T1");
  buffer.Clear();nativeSlider.minValue=nativeSlider.maxValue=15.43f;args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Call("Display",state,nativeSlider,label,pointer,buffer,blockade);
  Check(buffer.Count==3&&nativeSlider.value==a.Torpedo.Speed1&&nativeSlider.gameObject.activeSelf&&!blockade.activeSelf,"native refresh cannot collapse dry T1 to electric speed or hide its selector");
  b.IsMarkedForLaunch=true;choices=SpeedControl.MarkedOptions();Check(choices.Length==1&&choices[0]==15.43f,"mixed steam/electric group retains only shared speed");
  args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Call("Display",state,nativeSlider,label,pointer,buffer,blockade);Check(buffer.Count==1&&!nativeSlider.gameObject.activeSelf&&label.transform.parent.gameObject.activeSelf,"one common preset keeps native readout but no unsupported extra slider speeds");
  b.Torpedo.Speed1=b.Torpedo.Speed2=b.Torpedo.Speed3=12;Check(SpeedControl.MarkedOptions().Length==0,"incompatible marked group has no fabricated speed");
  b.Stable=false;Check(SpeedControl.MarkedOptions().Length==3,"unstable tube does not cap stable tube presets");args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Check(state!=null&&!state.Mixed,"unstable tube does not cause a false mixed display");Call("Finally",null,state);b.IsMarkedForLaunch=false;b.Stable=true;
  accepted=Runtime.Accepted;nativeSlider.Callback=value=>{var callback=new object[]{value,false,false,buffer,nativeSlider};if((bool)Call("BeforeEdit",callback)&&(bool)callback[2])Call("AfterEdit",callback[2],nativeSlider);};
  args=new object[]{null};Call("Project",args);state=(SpeedControl.Projection)args[0];Call("Display",state,nativeSlider,label,pointer,buffer,blockade);Check(Runtime.Accepted==accepted&&Runtime.Applying==0,"range/visibility refresh callbacks do not create speed transactions");
  Runtime.Active=false;SpeedControl.RestoreBounds();Check(slider.minValue==10&&slider.maxValue==24&&Runtime.Applying==0,"disabling restores captured vanilla speed-slider bounds");Runtime.Active=true;
  UBOAT.Game.Core.UnitsUtility.userSettings.GameplaySettings.units=0;
  Check(SpeedUnits.Factor==3.6f&&SpeedUnits.Format(20.57f)=="74.1 km/h","metric follows live game preference");
  UBOAT.Game.Core.UnitsUtility.userSettings.GameplaySettings.units=2;Check(SpeedUnits.Format(20.57f)=="40.0 kn","second nautical mode uses knots");
  UBOAT.Game.Core.UnitsUtility.userSettings=null;Check(SpeedUnits.Factor==SpeedValues.Knots,"missing settings falls back without exception");UBOAT.Game.Core.UnitsUtility.userSettings=new UBOAT.Game.Core.Data.UserSettings();
  Console.WriteLine("PASS: "+count+" speed bridge assertions (actual SpeedControl.cs; independent native API fixture).");return count;
 }
}
