// Independently authored test doubles. Not installed with the mod.
using System;
using System.Collections.Generic;
using System.Reflection;
using Landomatic.TorpedoRoom;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
namespace HarmonyLib {
 public class Harmony {
 public string Fail;public int Unpatches;public List<MethodInfo> Attempts=new List<MethodInfo>(),Installed=new List<MethodInfo>();
 public void Patch(object m,HarmonyMethod prefix=null,HarmonyMethod postfix=null,HarmonyMethod transpiler=null,HarmonyMethod finalizer=null){var method=m as MethodInfo;if(method==null)return;Attempts.Add(method);Installed.Add(method);if(method.Name==Fail)throw new ArgumentException("synthetic Mono emitter failure");}
 public void Unpatch(MethodBase method,MethodInfo prefix){Unpatches++;Installed.Remove((MethodInfo)method);}
 }
 public static class Priority {public const int First=800;}
 public class HarmonyMethod {public int priority;public HarmonyMethod(Type t,string n){}public HarmonyMethod(MethodInfo m){} }
 public static class AccessTools {
  public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);}
  public static Type TypeByName(string n){return n=="TorpedoTypeIcons.TorpedoIconSprites"?typeof(TorpedoTypeIcons.TorpedoIconSprites):typeof(UBOAT.Game.UI.TorpedoLaunchersDepthDialUI);}public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);}
 }
}
namespace UnityEngine {
 public class Object {public static void Destroy(object o){} public static T FindObjectOfType<T>() where T:class{return null;} }
 public class Component {public GameObject gameObject=new GameObject("fixture");public Transform transform {get{return gameObject.transform;}}public T GetComponent<T>() where T:new(){return gameObject.GetComponent<T>();}public T[] GetComponentsInChildren<T>(bool all){return gameObject.Children<T>();} }
 public class MonoBehaviour:Component {}
 public class GameObject {
  public T[] Children<T>(){var list=new List<T>();foreach(var v in components.Values)if(v is T)list.Add((T)v);return list.ToArray();}
  public T[] GetComponentsInChildren<T>(bool all){return Children<T>();}
  readonly Dictionary<Type,object> components=new Dictionary<Type,object>();public bool activeSelf=true;public Transform transform=new RectTransform();
  public bool activeInHierarchy {get{return activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);}}
  public string name;public GameObject(string s,params Type[] t){name=s;transform.gameObject=this;foreach(var type in t)if(type!=typeof(RectTransform))components[type]=Activator.CreateInstance(type);}public void SetActive(bool b){activeSelf=b;}
  public T GetComponent<T>() where T:new(){object value;return components.TryGetValue(typeof(T),out value)?(T)value:default(T);}
  public T AddComponent<T>() where T:new(){var value=new T();components[typeof(T)]=value;return value;}
 }
 public class CanvasGroup {public bool interactable=true,blocksRaycasts=true;public float alpha=1;}
 public class Sprite {}
 public static class Debug {public static void LogWarning(object o){} }
 public class Transform {public T[] GetComponentsInChildren<T>(bool all){return gameObject.GetComponentsInChildren<T>(all);}public Vector3 localPosition;public Vector3 TransformPoint(Vector3 v){return new Vector3(position.x+v.x*localScale.x,position.y+v.y*localScale.y,position.z+v.z*localScale.z);}public Vector3 InverseTransformPoint(Vector3 v){return new Vector3((v.x-position.x)/localScale.x,(v.y-position.y)/localScale.y,(v.z-position.z)/localScale.z);}public Transform parent;public GameObject gameObject;public Vector3 localScale=new Vector3(1,1,1),position;public Quaternion localRotation;public int Sibling;public void SetParent(Transform t,bool b){parent=t;}public int GetSiblingIndex(){return Sibling;}public void SetSiblingIndex(int i){Sibling=i;}public void SetAsLastSibling(){Sibling=999;}public bool IsChildOf(Transform p){for(var x=this;x!=null;x=x.parent)if(x==p)return true;return false;} }
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;public void GetWorldCorners(Vector3[] c){var r=rect;c[0]=TransformPoint(new Vector3(-pivot.x*r.size.x,-pivot.y*r.size.y,0));c[1]=TransformPoint(new Vector3(-pivot.x*r.size.x,(1-pivot.y)*r.size.y,0));c[2]=TransformPoint(new Vector3((1-pivot.x)*r.size.x,(1-pivot.y)*r.size.y,0));c[3]=TransformPoint(new Vector3((1-pivot.x)*r.size.x,-pivot.y*r.size.y,0));}public Rect rect {get{return new Rect{size=sizeDelta};}}}
 public class CanvasRenderer {}
 public struct Vector2 {public static Vector2 zero {get{return new Vector2(0,0);}}public float x,y;public Vector2(float a,float b){x=a;y=b;} }
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Quaternion {public float w;}public struct Rect {public Vector2 size;}
 public struct Color {public Color(float a,float b,float c){}public Color(float a,float b,float c,float d){} }
 public static class Time {public static float unscaledTime;}
}
namespace UnityEngine.UI { public class Selectable:UnityEngine.Component {} public class Toggle:Selectable {public bool isOn;public int Notifications;public void SetIsOnWithoutNotify(bool value){isOn=value;}} public class Slider {
 public Action<float> Callback;bool whole=true;float min=2,max=28,current=8;
 public bool wholeNumbers {get{return whole;}set{whole=value;Update(current);}}
 public float minValue {get{return min;}set{min=value;Update(current);}}
 public float value {get{return current;}set{Update(value);}}
 public void SetValueWithoutNotify(float v){current=Math.Max(min,Math.Min(max,v));}
 void Update(float v){v=Math.Max(min,Math.Min(max,v));if(whole)v=(float)Math.Round(v);if(v!=current){current=v;if(Callback!=null)Callback(v);}}
} public class Image:UnityEngine.Component {public UnityEngine.Color color;public bool raycastTarget;public UnityEngine.Sprite sprite;public bool enabled=true;} public class Button:Selectable {public bool interactable=true;} }
namespace TMPro {
 public enum TextAlignmentOptions {Bottom}
 public class TextMeshProUGUI:UnityEngine.Component {public object font;public float fontSize,fontSizeMin,fontSizeMax;public bool enableAutoSizing,raycastTarget,richText;public UnityEngine.Color color;public TextAlignmentOptions alignment;public UnityEngine.RectTransform rectTransform=new UnityEngine.RectTransform();public string text;}
 public class TMP_InputField:UnityEngine.Component {public bool isFocused;public string text;public void SetTextWithoutNotify(string value){text=value;}}
 public static class TMP_Settings {public static object defaultFontAsset;}
}
namespace UBOAT.Game.Core.Serialization {public class NonSerializedInGameState:Attribute {} }
namespace UBOAT.Game.Scene.Entities {
 public class Torpedo {public enum TorpedoProgramType {None,FAT,LUT}public TorpedoProgramType ProgramType=TorpedoProgramType.LUT;}
 public class PlayerShipTorpedoAttackSettings {public float TorpedoDepth=3;}
 public class PlayerShip {public float Depth;public bool IsPreparingToLaunchTorpedoes,IsLaunchingTorpedoes;public TorpedoFireControlBoxes Boxes;public T GetEquipment<T>() where T:class{return Boxes as T;}public PlayerShipTorpedoAttackSettings TorpedoAttackSettings=new PlayerShipTorpedoAttackSettings();public bool TorpedoProgramDirection;public float TorpedoProgramDistance=1500,TorpedoProgramAngleAbsolute;public TorpedoLauncher[] TorpedoLaunchers;}
}
namespace UBOAT.Game.Scene.Items {
 public class EquipmentData {public string Name;}
 public class StoredTorpedo {public EquipmentData Data=new EquipmentData();public string Name="G7e Torpedo T3 - Pi2 - LUT",name="G7e Torpedo T3 - Pi2 - LUT";public PreparationStatus Saved=new PreparationStatus();public Torpedo Missile=new Torpedo();}
 public class TorpedoFireControlBoxes {public bool UnsupportedMapping;public int Calls,Selected=1;public void ValidateMarkedLaunchersFromSalvoSettings(){Calls++;if(UnsupportedMapping)return;var tubes=Landomatic.TorpedoRoom.Runtime.Ship.TorpedoLaunchers;for(int i=0;i<tubes.Length;i++)if(tubes[i]!=null)tubes[i].IsMarkedForLaunch=Selected==i+1||Selected==99;}}
 public class TorpedoLauncher {public bool IsFlooded,IsMarkedForLaunch=true,IsLoaded=true;public StoredTorpedo Torpedo=new StoredTorpedo();}
}
namespace UBOAT.Game.UI {public class GameUI {public UBOAT.Game.UI.Periscope.TorpedoLaunchersUI CurrentTorpedoLaunchersUI;}
 public class TorpedoAttackStationsViewSwitchUI:UnityEngine.MonoBehaviour {}
 public class TorpedoLaunchersSpeedDialUI:UnityEngine.MonoBehaviour {}
 public class TorpedoLaunchersDepthDialUI:UnityEngine.MonoBehaviour {void OnEnable(){}void Update(){}void OnValueChanged(float value){}}
 public class TorpedoLauncherUI:UnityEngine.MonoBehaviour {public UnityEngine.UI.Image torpedoImage=new UnityEngine.UI.Image();public UnityEngine.UI.Button pistolTypeButton=new UnityEngine.UI.Button();public TorpedoLauncher Launcher;} }
namespace UBOAT.Game.UI.Periscope {public class PeriscopeUI:UnityEngine.MonoBehaviour {public UnityEngine.UI.Toggle selectTorpedoButton,selectTorpedoButton2;} public class TorpedoLaunchersUI:UnityEngine.MonoBehaviour {
  public UnityEngine.GameObject programParameters=new UnityEngine.GameObject("params"),dataPanel=new UnityEngine.GameObject("data");public TMPro.TextMeshProUGUI programSettingsName=new TMPro.TextMeshProUGUI();public UnityEngine.UI.Button fireButton=new UnityEngine.UI.Button();public UnityEngine.GameObject fireConfirmationPopup=new UnityEngine.GameObject("popup"),firingBlockade=new UnityEngine.GameObject("depth blocker");public bool DisallowFire;public int OpenCalls,CloseCalls;public Action OpenProbe;void Update(){}void OnFire(){}void DoFire(){}public void Open(bool extended,int min,int max,bool close){OpenCalls++;if(OpenProbe!=null)OpenProbe();IsOpened=true;gameObject.SetActive(true);}public void Close(){CloseCalls++;IsOpened=false;gameObject.SetActive(false);}void OnDisable(){}void OnDestroy(){}void ValidateBearingSource(){}void OnSalvoSpreadChanged(){}void UpdateProgramTargetingValues(){}
  public UBOAT.Game.UI.TorpedoLauncherUI[] Launchers;public bool IsOpened=true;public int Calls;public bool FailOnce;
  public UnityEngine.UI.Toggle programLeft=new UnityEngine.UI.Toggle(),programRight=new UnityEngine.UI.Toggle();
  public TMPro.TMP_InputField programDistance=new TMPro.TMP_InputField(),programAngle=new TMPro.TMP_InputField();
  void SetProgramDisplayValues(){Calls++;if(FailOnce){FailOnce=false;throw new Exception("synthetic transient refresh error");}programDistance.text=Landomatic.TorpedoRoom.Runtime.Ship.TorpedoProgramDistance.ToString();programAngle.text=Landomatic.TorpedoRoom.Runtime.Ship.TorpedoProgramAngleAbsolute.ToString();}
 } }
namespace Landomatic.TorpedoRoom {
 public class TorpedoHover:UnityEngine.MonoBehaviour {public Action Enter,Exit;}
 public class PreparationStatus {public Preparation Value=new Preparation {ProgramOverride=true};}
 internal static class NativeSkin {public static UnityEngine.GameObject ManualRoot=new UnityEngine.GameObject("manual");}
 internal static class Bridge {public static bool IsRemote,RestrictedScopes;public static TubeSnapshot[] Sent;public static void RequestViewport(TubeSnapshot[] edits){Sent=edits;} }
 internal static class Runtime {
  public static UBOAT.Game.UI.GameUI Ui;public static UBOAT.Game.UI.Periscope.PeriscopeUI ScopeUi;public static T Get<T>() where T:class{return Ui as T??ScopeUi as T;}
  public static int Applying;public static bool MpScopeFeuer,AimContext=true,LaunchBusy;public static object ViewingScope;
  public static bool RoomEditorOpen;public static object CurrentGroup;public static bool ViewingStation=true;public static ViewportMode Viewport=ViewportMode.ReadOnly;public static int IndexOf(Torpedo t){return 0;}
  public static PlayerShip Ship;public static bool Active=true,Loading,LockoutEnabled=true;public static int Accepted;
  public static bool Stable(TorpedoLauncher t){return true;}
  public static Torpedo Missile(StoredTorpedo item){return item==null?null:item.Missile;}
  public static PreparationStatus Status(StoredTorpedo item,bool create){return item==null?null:item.Saved;}
  public static void AcceptViewport(TubeSnapshot[] edits,object peer){Accepted+=edits.Length;foreach(var row in edits){row.Settings.Revision++;Ship.TorpedoLaunchers[row.Index].Torpedo.Saved.Value=row.Settings;} }
  public static string TubeWaterText(TorpedoLauncher tube){return "DRY";}
  public static bool MarkedHasProgram(){return true;}
 }
}
namespace TorpedoTypeIcons {internal static class TorpedoIconSprites {public static int Calls;public static UnityEngine.Sprite Colored=new UnityEngine.Sprite();internal static bool TryGetLauncherByName(string name,out UnityEngine.Sprite sprite){Calls++;sprite=name.StartsWith("G7e")?Colored:null;return sprite!=null;}}}
public abstract class NativePointerFixture {public abstract void OnClick();public void OnPointerClick(){}public void OnDrag(){}}
public class GenericPointerFixture<T> {public void OnDrag(){}}
public static class StockUiChecks {
 static int count;
 static void Check(bool b,string name){count++;if(!b)throw new Exception("FAILED: "+name);}
 static object Call(string name,params object[] args){
  var method=typeof(StockUi).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static);var all=new object[method.GetParameters().Length];Array.Copy(args,all,args.Length);var result=method.Invoke(null,all);Array.Copy(all,args,args.Length);return result;
 }
 static object Before(string name,params object[] args){Call(name,args);return args[args.Length-1];}
 static object DepthInvoke(Type type,string name,object[] args){var m=type.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);var all=new object[m.GetParameters().Length];Array.Copy(args,all,args.Length);var result=m.Invoke(null,all);Array.Copy(all,args,args.Length);return result;}
 public static void OnProgramLeftChanged(bool value){}
 public static int Run(){
  var ship=new PlayerShip{TorpedoLaunchers=new[]{new TorpedoLauncher(),new TorpedoLauncher()}};Runtime.Ship=ship;
  var a=ship.TorpedoLaunchers[0].Torpedo.Saved;var b=ship.TorpedoLaunchers[1].Torpedo.Saved;
  a.Value.Distance=2500;a.Value.Angle=45;b.Value.Distance=5500;b.Value.Angle=220;
  object[] args={false,null};Check(!(bool)Call("BeforeEdit",args)&&args[1]==null,"read-only blocks native program edits before shared writes");
  Runtime.Viewport=ViewportMode.Hidden;Check(!(bool)Call("BeforeEdit",args),"hidden blocks native program edits");
  Runtime.Viewport=ViewportMode.Synchronized;
  object state=Before("BeforeEdit",false,null);ship.TorpedoProgramDirection=true;Call("AfterEdit",state);
  Check(Runtime.Accepted==2&&a.Value.Right&&b.Value.Right,"synchronized direction applies to both marked per-tube programs");
  Check(a.Value.Distance==2500&&b.Value.Distance==5500,"single field edit preserves independent distances");
  ship.TorpedoLaunchers[1].IsMarkedForLaunch=false;ship.TorpedoProgramDistance=1200;
  state=Before("BeforeEdit",false,null);ship.TorpedoProgramDistance=3000;Call("AfterEdit",state);
  Check(a.Value.Distance==3000&&b.Value.Distance==5500,"single marked tube edit leaves unmarked tube unchanged");
  Check(Before("BeforeEdit",true,null)==null,"native validation not imported as user edit");
  var ui=new UBOAT.Game.UI.Periscope.TorpedoLaunchersUI();var panel=ui.programParameters;var title=ui.programSettingsName;var headingRow=new UnityEngine.GameObject("LUT heading row");ui.programAngle.transform.parent=new UnityEngine.Transform{gameObject=headingRow};
  ship.TorpedoProgramDistance=1200;var display=Before("BeforeDisplay",(object)null);
  Check(ship.TorpedoProgramDistance==3000,"display projects per-tube program");
  Call("AfterDisplay",display,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(ship.TorpedoProgramDistance==1200,"display projection restores shared program");
  Check(panel.GetComponent<UnityEngine.CanvasGroup>().interactable,"synchronized per-tube native controls editable");
  Runtime.Viewport=ViewportMode.ReadOnly;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(!panel.GetComponent<UnityEngine.CanvasGroup>().interactable,"read-only native program controls disabled");
  a.Value.StraightRun=true;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);Check(!panel.activeSelf&&title.text.Contains("STRAIGHT"),"straight mode hides pattern fields");a.Value.StraightRun=false;
  ui.programDistance.isFocused=true;Check(!StockUi.Refresh(ui),"refresh preserves focused input");ui.programDistance.isFocused=false;
  ui.IsOpened=false;Check(!StockUi.Refresh(ui),"closed panel does no refresh work");ui.IsOpened=true;Runtime.Loading=true;Check(!StockUi.Refresh(ui),"loading stops refresh");Runtime.Loading=false;
  display=Before("BeforeDisplay",(object)null);var error=new Exception("synthetic");Call("DisplayFinally",error,display);Check(ship.TorpedoProgramDistance==1200,"exception restores display projection");

  // Regression: equal settings on a group must be shown, rather than blanked
  // merely because more than one tube is marked. Only mixed dimensions blank.
  Runtime.Viewport=ViewportMode.Synchronized;
  ship.TorpedoLaunchers[1].IsMarkedForLaunch=true;
  a.Value.Distance=b.Value.Distance=1000;a.Value.Angle=b.Value.Angle=40;a.Value.Right=b.Value.Right=false;
  display=Before("BeforeDisplay",(object)null);ui.programDistance.text=ship.TorpedoProgramDistance.ToString();ui.programAngle.text=ship.TorpedoProgramAngleAbsolute.ToString();
  Call("AfterDisplay",display,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(ui.programDistance.text=="1000 m"&&ui.programAngle.text=="40°","group display reports matching per-tube run and LUT heading");
  Check(ship.TorpedoProgramDistance==1200&&ship.TorpedoProgramAngleAbsolute==0,"group projection never replaces common launch program");
  Check(ui.programLeft.isOn&&!ui.programRight.isOn,"matching group direction reflected silently");
  b.Value.Distance=2500;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(ui.programDistance.text=="—"&&ui.programAngle.text=="40°","only differing dimensions show mixed marker");
  b.Value.Angle=225;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(ui.programAngle.text=="—","mixed LUT headings are explicitly mixed");
  ship.TorpedoLaunchers[0].IsMarkedForLaunch=ship.TorpedoLaunchers[1].IsMarkedForLaunch=false;
  Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(!panel.activeSelf&&!headingRow.activeSelf,"deselection hides program rows");
  ship.TorpedoLaunchers[0].IsMarkedForLaunch=ship.TorpedoLaunchers[1].IsMarkedForLaunch=true;
  // Native ValidateProgramUIs may have hidden the child from its flooded-only
  // counters. Our actual display postfix must restore it for selected LUTs.
  headingRow.SetActive(false);Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(panel.activeSelf&&headingRow.activeSelf,"LUT bearing row returns after deselect/reselect");
  b.Value.StraightRun=true;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(panel.activeSelf&&headingRow.activeSelf&&ui.programAngle.text=="40°"&&title.text.Contains("includes straight"),"straight group member does not hide another LUT's program");
  a.Value.StraightRun=true;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(!panel.activeSelf&&title.text.Contains("STRAIGHT"),"all-straight selection hides pattern controls");
  a.Value.StraightRun=b.Value.StraightRun=false;b.Value.ProgramOverride=false;ship.TorpedoProgramDistance=3300;ship.TorpedoProgramAngleAbsolute=120;
  ship.TorpedoLaunchers[0].IsMarkedForLaunch=false;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(ui.programDistance.text=="3300 m"&&ui.programAngle.text=="120°","Last station selection reports common effective values");
  b.Value.ProgramOverride=true;ship.TorpedoLaunchers[0].IsMarkedForLaunch=true;
  ship.TorpedoLaunchers[0].Torpedo.Missile.ProgramType=Torpedo.TorpedoProgramType.FAT;
  a.Value.Angle=300;b.Value.Angle=225;Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);
  Check(headingRow.activeSelf&&ui.programAngle.text=="225°","FAT member's unused angle does not create a mixed LUT heading");
  ship.TorpedoLaunchers[1].Torpedo.Missile.ProgramType=Torpedo.TorpedoProgramType.FAT;
  Call("AfterDisplay",null,panel,title,ui.programDistance,ui.programAngle,ui);Check(!headingRow.activeSelf,"FAT-only selection has no LUT bearing row");
  ship.TorpedoLaunchers[0].Torpedo.Missile.ProgramType=ship.TorpedoLaunchers[1].Torpedo.Missile.ProgramType=Torpedo.TorpedoProgramType.LUT;
  a.Value.Distance=3000;b.Value.Distance=5500;a.Value.Right=false;b.Value.Right=true;
  // Clicking Left must set both to Left even when the common setting already
  // was Left; a diff against that common bool alone would miss this action.
  object[] directionArgs={false,null,typeof(StockUiChecks).GetMethod("OnProgramLeftChanged"),new object[]{true}};
  Call("BeforeEdit",directionArgs);ship.TorpedoProgramDirection=false;Call("AfterEdit",directionArgs[1]);
  Check(!a.Value.Right&&!b.Value.Right,"explicit direction click resolves mixed group even if common value is unchanged");
  Check(a.Value.Distance==3000&&b.Value.Distance==5500,"group direction click preserves independent run distances");
  directionArgs=new object[]{false,null,typeof(StockUiChecks).GetMethod("OnProgramLeftChanged"),new object[]{false}};Call("BeforeEdit",directionArgs);
  Check(directionArgs[1]==null,"toggle off callback is not a direction edit");
  ship.TorpedoLaunchers[1].IsMarkedForLaunch=false;ship.TorpedoProgramDistance=1200;ship.TorpedoProgramAngleAbsolute=0;

  Runtime.Viewport=ViewportMode.Synchronized;Bridge.IsRemote=true;state=Before("BeforeEdit",false,null);ship.TorpedoProgramDistance=3500;Call("AfterEdit",state);
  Check(Bridge.Sent.Length==1&&Bridge.Sent[0].Settings.Distance==3500,"remote viewport submits updated per-tube program");Check(a.Value.Distance==3000,"remote edit awaits host confirmation");Bridge.IsRemote=false;
  // Actual native-presentation masks, with independent GameObject/CanvasGroup fixtures.
  var tubeUi=new UBOAT.Game.UI.TorpedoLauncherUI{Launcher=ship.TorpedoLaunchers[0]};ui.Launchers=new[]{tubeUi};Runtime.Ui=new UBOAT.Game.UI.GameUI{CurrentTorpedoLaunchersUI=ui};
  var nativeSpeed=ui.gameObject.AddComponent<UBOAT.Game.UI.TorpedoLaunchersSpeedDialUI>();
  NativePresentation.Room(true);Check(NativeSkin.ManualRoot.GetComponent<UnityEngine.CanvasGroup>().alpha==0,"separate native room dials masked");Check(ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0,"room replacement masks native panel");NativePresentation.Room(false);Check(ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==1,"closing room restores native panel");Check(NativeSkin.ManualRoot.GetComponent<UnityEngine.CanvasGroup>().alpha==1,"closing room restores separate native dials");
  ui.transform.SetParent(new UnityEngine.GameObject("canvas").transform,false);ui.fireButton.transform.SetParent(ui.transform,false);ui.fireConfirmationPopup.transform.SetParent(ui.transform,false);
  Runtime.Viewport=ViewportMode.Hidden;NativePresentation.Refresh(ui);Check(ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0&&HiddenLayout.Active,"hidden reports masks the complete native panel");Check(ui.fireButton.transform.parent==ui.transform.parent,"original Feuer moved outside hidden root");Check(tubeUi.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0&&!tubeUi.gameObject.GetComponent<UnityEngine.CanvasGroup>().blocksRaycasts,"hidden reports hide native tube icons and clicks");
  Check(nativeSpeed.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0&&!nativeSpeed.gameObject.GetComponent<UnityEngine.CanvasGroup>().blocksRaycasts,"hidden policy masks native speed reports/input too");
  Check(ui.fireButton.GetComponent<UnityEngine.CanvasGroup>()==null,"firing control is outside report masks");
  Runtime.Viewport=ViewportMode.ReadOnly;NativePresentation.Refresh(ui);Check(tubeUi.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==1,"read-only restores visible tube icons");Check(!tubeUi.pistolTypeButton.gameObject.GetComponent<UnityEngine.CanvasGroup>().interactable,"read-only pistol button disabled");
  Check(!nativeSpeed.gameObject.GetComponent<UnityEngine.CanvasGroup>().interactable,"read-only policy disables native speed dial");
  Runtime.Viewport=ViewportMode.Synchronized;NativePresentation.Refresh(ui);Check(tubeUi.pistolTypeButton.gameObject.GetComponent<UnityEngine.CanvasGroup>().interactable,"synchronized restores pistol interaction");
  Check(nativeSpeed.gameObject.GetComponent<UnityEngine.CanvasGroup>().interactable,"synchronized policy restores native speed dial");
  // Closing the custom editor no longer releases room takeover. Native panel
  // refresh must preserve the mask until station exit/interface disable.
  NativePresentation.Room(true);NativePresentation.Refresh(ui);
  Check(ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0,"native refresh cannot reveal room panel during takeover");
  Check(NativeSkin.ManualRoot.GetComponent<UnityEngine.CanvasGroup>().alpha==0,"native refresh cannot reveal separate bottom room graphic");
  NativePresentation.Room(false);NativePresentation.Refresh(ui);
  Check(ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==1&&NativeSkin.ManualRoot.GetComponent<UnityEngine.CanvasGroup>().alpha==1,"station exit or disabled interface restores stock graphics");
  // Actual depth bridge: only manual dial changes become transactions.
  var dt=typeof(DepthControl);Func<string,object[],object> depthCall=(name,x)=>DepthInvoke(dt,name,x);
  args=new object[]{14f,false};Runtime.Viewport=ViewportMode.ReadOnly;Check(!(bool)depthCall("Before",args),"read-only rejects depth dial edit");
  Runtime.Viewport=ViewportMode.Synchronized;Check((bool)depthCall("Before",args)&&(bool)args[1],"synchronized accepts changed dial depth");ship.TorpedoAttackSettings.TorpedoDepth=7;depthCall("After",new object[]{args[1]});Check(a.Value.DepthOverride&&a.Value.Depth==7,"depth dial updates selected tube record");Check(!b.Value.DepthOverride,"depth dial leaves unselected tube alone");
  ship.TorpedoAttackSettings.TorpedoDepth=3;args=new object[]{6f,false};depthCall("Before",args);Check((bool)args[1],"dial can change a tube back to the shared depth despite different displayed override");
  a.Value.Depth=9;ship.TorpedoAttackSettings.TorpedoDepth=3;args=new object[]{null};depthCall("Project",args);Check(ship.TorpedoAttackSettings.TorpedoDepth==9&&Runtime.Applying==1,"native depth display projects per-tube depth with refresh guard");
  object[] callback={18f,false};Check((bool)depthCall("Before",callback)&&!(bool)callback[1],"display refresh does not generate depth transaction");
  depthCall("Restore",args);depthCall("Finally",new object[]{error,args[0]});Check(ship.TorpedoAttackSettings.TorpedoDepth==3&&Runtime.Applying==0,"depth projection restores once including finalizer");
  var slider=new UnityEngine.UI.Slider();depthCall("Tune",new object[]{slider});Check(!slider.wholeNumbers,"native dial permits quarter-metre depth instead of whole slider units");Check(slider.minValue==2,"native lower bound is one metre in double-depth slider units");
  ship.TorpedoAttackSettings.TorpedoDepth=.5f;depthCall("Tune",new object[]{slider});Check(ship.TorpedoAttackSettings.TorpedoDepth==1,"native shared legacy depth also respects the one metre minimum");ship.TorpedoAttackSettings.TorpedoDepth=3;
  a.Value.Depth=1.25f;args=new object[]{null};depthCall("Project",args);slider.SetValueWithoutNotify(ship.TorpedoAttackSettings.TorpedoDepth*2);Check(slider.value==2.5f,"room quarter-metre depth projects into native dial");depthCall("Restore",args);
  object[] halfEdit={1f,false};Check((bool)depthCall("Before",halfEdit)&&(float)halfEdit[0]==2,"native half-metre edit is clamped to one metre");
  ship.TorpedoAttackSettings.TorpedoDepth=3;slider.value=1;slider.Callback=v=>{object[] cb={v,false};if((bool)depthCall("Before",cb)){ship.TorpedoAttackSettings.TorpedoDepth=(float)cb[0]/2;depthCall("After",new object[]{cb[1]});}};
  int revision=a.Value.Revision;Runtime.Active=false;DepthControl.RestoreSteps();Check(slider.wholeNumbers&&slider.minValue==2,"disabling mod restores native dial stepping and minimum");Check(ship.TorpedoAttackSettings.TorpedoDepth==3&&a.Value.Revision==revision&&Runtime.Applying==0,"restoration callback cannot leak shared depth or a preparation edit");Runtime.Active=true;
  slider.wholeNumbers=false;slider.minValue=0;slider.value=.5f;ship.TorpedoAttackSettings.TorpedoDepth=4;depthCall("Tune",new object[]{slider});Check(slider.minValue==2&&ship.TorpedoAttackSettings.TorpedoDepth==4&&Runtime.Applying==0,"tuning minimum guards native range callbacks and preserves shared setting");
  Runtime.Active=false;DepthControl.RestoreSteps();Check(!slider.wholeNumbers&&slider.minValue==0,"restore preserves actual original bounds and stepping rather than assuming defaults");Runtime.Active=true;
  args=new object[]{6.28f,false};depthCall("Before",args);Check(Math.Abs((float)args[0]-6.5f)<.001,"native manual dial input quantized to 0.25 m");
  args=new object[]{float.NaN,false};Check(!(bool)depthCall("Before",args),"invalid native dial value rejected");
  Runtime.ViewingStation=false;Runtime.CurrentGroup=new object();args=new object[]{6.28f,false};Check((bool)depthCall("Before",args)&&Math.Abs((float)args[0]-6.5f)<.001&&!(bool)args[1],"native room shared-depth dial also uses quarter metres without a forged viewport transaction");Runtime.ViewingStation=true;Runtime.CurrentGroup=null;
  var depthLabel=new TMPro.TextMeshProUGUI();depthCall("After",new object[]{false,depthLabel,6.5f});Check(depthLabel.text=="3.25 m","native depth readout displays quarter metres without tenth-metre rounding");
  var colored=NativePresentation.SpriteFor(0);Check(ReferenceEquals(colored,TorpedoTypeIcons.TorpedoIconSprites.Colored),"optional installed icon provider supplies colored launcher sprite");
  int iconCalls=TorpedoTypeIcons.TorpedoIconSprites.Calls;NativePresentation.SpriteFor(0);Check(TorpedoTypeIcons.TorpedoIconSprites.Calls==iconCalls,"colored sprite lookup cached");
  // A transient native update error must not permanently disable synchronization.
  var refresher=new StockPanelRefresh{Owner=ui};refresher.Reset();ui.FailOnce=true;UnityEngine.Time.unscaledTime=0;
  var update=typeof(StockPanelRefresh).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);int calls=ui.Calls;
  update.Invoke(refresher,null);Check(ui.Calls==calls+1,"refresh attempts native update before transient error");
  UnityEngine.Time.unscaledTime=1.01f;update.Invoke(refresher,null);Check(ui.Calls==calls+2,"native synchronization retries after transient error");
  Check((bool)Call("BeforeRoomPointer"),"native pointer handlers remain usable when editor is closed");
  Runtime.RoomEditorOpen=true;Check(!(bool)Call("BeforeRoomPointer"),"open editor suppresses native pointer handlers behind overlay");
  args=new object[]{false,null};Check(!(bool)Call("BeforeEdit",args),"modal editor blocks native program clicks underneath");
  args=new object[]{6.5f,false};Check(!(bool)depthCall("Before",args),"modal editor blocks native depth click underneath");
  Runtime.RoomEditorOpen=false;Check((bool)Call("BeforeRoomPointer"),"closing editor releases native pointer handlers");
  var installer=new HarmonyLib.Harmony();
  Check(!StockUi.SupportsInputGuard(typeof(NativePointerFixture).GetMethod("OnClick")),"abstract OnClick has no patchable method body");
  Check(!StockUi.SupportsInputGuard(null)&&!StockUi.SupportsInputGuard(typeof(GenericPointerFixture<>).GetMethod("OnDrag")),"missing and open generic targets are skipped");
  Call("InstallInputGuard",installer,typeof(NativePointerFixture),"OnClick");Check(installer.Attempts.Count==0,"installer never sends abstract OnClick to Harmony");
  Check(StockUi.SupportsInputGuard(typeof(NativePointerFixture).GetMethod("OnPointerClick")),"concrete pointer entry is patchable");
  installer.Fail="OnPointerClick";Call("InstallInputGuard",installer,typeof(NativePointerFixture),"OnPointerClick");Check(installer.Unpatches==1&&installer.Installed.Count==0,"emitter failure cleans auxiliary guard without throwing out of installer");
  installer.Fail=null;Call("InstallInputGuard",installer,typeof(NativePointerFixture),"OnDrag");Check(installer.Installed.Count==1&&Runtime.Active,"later guards and core runtime continue after auxiliary failure");
  NativePresentation.Restore();
  Console.WriteLine("PASS: "+count+" native UI/depth assertions (actual stock, mask and depth classes; independent API fixtures).");return count;
 }
}

public static class ViewingChecks {
 static int count;
 static void Check(bool value,string name){count++;if(!value)throw new Exception("FAILED: "+name);}
 static object Call(string name,params object[] args){return typeof(ViewingFire).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
 static void Frame(UBOAT.Game.UI.Periscope.TorpedoLaunchersUI ui,bool ready=true){
  Call("BeforeUpdate");ui.firingBlockade.SetActive(Runtime.Ship.Depth>18);ui.fireButton.interactable=ready;Call("AfterUpdate",ui);
 }
 public static int Run(){
  NativePresentation.Restore();Runtime.Active=true;Runtime.Loading=false;Runtime.ViewingStation=true;Runtime.Viewport=ViewportMode.Hidden;
  var a=new TorpedoLauncher{IsFlooded=true};var b=new TorpedoLauncher{IsFlooded=true};a.Torpedo.Name="G7es Zaunkönig Torpedo T5 - Pi4";b.Torpedo.Name="G7a Torpedo T1 - Pi1";a.Torpedo.Data.Name=a.Torpedo.Name;b.Torpedo.Data.Name=b.Torpedo.Name;
  var ship=Runtime.Ship=new PlayerShip{TorpedoLaunchers=new[]{a,b},Boxes=new TorpedoFireControlBoxes()};
  var ui=new UBOAT.Game.UI.Periscope.TorpedoLaunchersUI{Launchers=new UBOAT.Game.UI.TorpedoLauncherUI[0]};
  var canvas=new UnityEngine.GameObject("main canvas");ui.transform.SetParent(canvas.transform,false);
  var button=ui.fireButton;var originalParent=new UnityEngine.GameObject("authored button parent");originalParent.transform.SetParent(ui.transform,false);button.transform.SetParent(originalParent.transform,false);
  var rect=(UnityEngine.RectTransform)button.transform;rect.anchorMin=new UnityEngine.Vector2(.2f,.3f);rect.anchorMax=new UnityEngine.Vector2(.6f,.7f);rect.pivot=new UnityEngine.Vector2(.4f,.8f);rect.sizeDelta=new UnityEngine.Vector2(101,43);rect.anchoredPosition=new UnityEngine.Vector2(72,89);rect.localScale=new UnityEngine.Vector3(2,3,1);rect.localRotation=new UnityEngine.Quaternion{w=.4f};rect.Sibling=7;
  ui.fireConfirmationPopup.transform.SetParent(button.transform,false);ui.fireConfirmationPopup.SetActive(false);
  var nav=ui.gameObject.AddComponent<UBOAT.Game.UI.TorpedoAttackStationsViewSwitchUI>();nav.transform.SetParent(ui.transform,false);
  Check(ViewingFire.IsT5(a.Torpedo.Name)&&ViewingFire.IsT5(a.Torpedo.Name+" - Prototype"),"T5 and stock prototype equipment keys recognized");
  Check(ViewingFire.IsT5(a.Torpedo.Name+" (Warmed)")&&ViewingFire.IsT5(a.Torpedo.Name+" - Prototype (Warmed)"),"maintained T5 and prototype data keys recognized");
  foreach(var key in new[]{null,"",b.Torpedo.Name,"T5","G7es Torpedo T4 - Pi4",a.Torpedo.Name+" typo"})Check(!ViewingFire.IsT5(key),"unrecognized keys never bypass depth: "+key);
  foreach(float depth in new[]{18.01f,30f,50f})Check(ViewingFire.DeepPermission(depth,1,true),"deep T5 permission at "+depth);
  foreach(float depth in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,0f,18f,50.01f})Check(!ViewingFire.DeepPermission(depth,1,true),"no deep exception at "+depth);
  Check(!ViewingFire.DeepPermission(30,0,true)&&!ViewingFire.DeepPermission(30,2,false),"empty and mixed selections have no depth exception");
  // Physical box mapping controls marks; no first-ready fallback even though both tubes are flooded.
  a.IsMarkedForLaunch=false;b.IsMarkedForLaunch=true;Frame(ui);Check(a.IsMarkedForLaunch&&!b.IsMarkedForLaunch,"hidden Feuer uses box selection, replacing stale marks");
  Check((bool)Call("BeforeFire",ui),"native ready hidden selection can fire");
  Check(HiddenLayout.Active&&ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0,"all native background and controls masked at root");
  Check(ui.fireButton==button&&button.transform.parent==canvas.transform,"same Feuer instance moved outside hidden root");
  Check(rect.localScale.x==3&&rect.localScale.y==4.5f,"Feuer visual and clickable rectangle enlarged 1.5 times");
  Check(ui.fireConfirmationPopup.transform.parent==canvas.transform&&!ui.fireConfirmationPopup.activeSelf,"native confirmation moved without auto-confirming or opening");
  Check(nav.transform.parent==canvas.transform,"native station navigation stays outside mask");
  var masks=ui.gameObject.GetComponent<UnityEngine.CanvasGroup>();Check(!masks.interactable&&!masks.blocksRaycasts,"hidden panel cannot accept stray tube/dispersion clicks");
  ship.Boxes.UnsupportedMapping=true;Frame(ui);Check(!a.IsMarkedForLaunch&&!b.IsMarkedForLaunch&&!button.interactable,"unsupported native mapping cannot retain stale marks");ship.Boxes.UnsupportedMapping=false;
  ship.Boxes.Selected=2;Frame(ui);Check(!a.IsMarkedForLaunch&&b.IsMarkedForLaunch,"aft selection replaces bow selection");
  ship.Boxes.Selected=0;Frame(ui);Check(!a.IsMarkedForLaunch&&!b.IsMarkedForLaunch&&!button.interactable&&!(bool)Call("BeforeFire",ui),"no selection never launches a random flooded tube");
  ship.Boxes.Selected=99;Frame(ui);Check(a.IsMarkedForLaunch&&b.IsMarkedForLaunch&&(bool)Call("BeforeFire",ui),"mixed salvo at ordinary depth remains native; no speed gate");
  int calls=ship.Boxes.Calls;ship.IsPreparingToLaunchTorpedoes=true;ship.Boxes.Selected=1;Call("BeforeUpdate");Check(ship.Boxes.Calls==calls&&b.IsMarkedForLaunch,"no remapping during an active preparation");Check(!(bool)Call("BeforeFire",ui),"no duplicate fire during preparation");ship.IsPreparingToLaunchTorpedoes=false;
  ship.IsLaunchingTorpedoes=true;Call("BeforeUpdate");Check(ship.Boxes.Calls==calls,"no remapping an executing salvo");ship.IsLaunchingTorpedoes=false;
  ship.Boxes=null;Frame(ui);Check(!button.interactable&&!(bool)Call("BeforeFire",ui),"missing box fails closed without selecting a fallback tube");ship.Boxes=new TorpedoFireControlBoxes();
  ship.Depth=30;Frame(ui);Check(!ui.firingBlockade.activeSelf&&button.interactable&&(bool)Call("BeforeFire",ui),"T5 can use native Feuer at 30 m boat depth");
  Frame(ui,false);Check(!button.interactable&&!(bool)Call("BeforeFire",ui),"T5 exception never enables a native-disabled fire button");
  Frame(ui);ui.DisallowFire=true;Check(!(bool)Call("BeforeFire",ui),"native station permission still applies");ui.DisallowFire=false;
  a.Torpedo.Data.Name+=" (Warmed)";a.Torpedo.Name="localized display";Frame(ui);Check(button.interactable,"T5 classification uses equipment data and accepts warmed variant");
  ship.Depth=50;Frame(ui);Check(!ui.firingBlockade.activeSelf&&(bool)Call("BeforeFire",ui),"T5 defensive exception includes exactly 50 m");
  ship.Depth=50.01f;Frame(ui);Check(!ui.firingBlockade.activeSelf&&!button.interactable&&!(bool)Call("BeforeFire",ui),"exception ends above 50 m");
  ship.Depth=30;ship.Boxes.Selected=2;Frame(ui);Check(!ui.firingBlockade.activeSelf&&!button.interactable&&!(bool)Call("BeforeFire",ui),"ordinary torpedo keeps native depth restriction");
  ship.Boxes.Selected=99;Frame(ui);Check(!(bool)Call("BeforeFire",ui),"mixed selected salvo cannot borrow T5 exception");
  b.IsFlooded=false;Frame(ui);Check(button.interactable&&!ui.firingBlockade.activeSelf&&(bool)Call("BeforeFire",ui),"draining marked non-T5 restores deep T5 Feuer without deselection");
  b.IsFlooded=true;Check(!(bool)Call("BeforeFire",ui),"confirmation rejects newly flooded non-T5 before another display frame");
  Frame(ui);Check(!button.interactable&&!ui.firingBlockade.activeSelf,"reflooding marked non-T5 reinstates deep restriction");
  b.IsFlooded=false;Frame(ui);Check(button.interactable&&(bool)Call("BeforeFire",ui),"second drain recovers without closing the panel");
  a.IsFlooded=false;Frame(ui);Check(!button.interactable&&!(bool)Call("BeforeFire",ui),"all dry selection cannot borrow deep T5 exception");
  a.IsFlooded=true;Frame(ui);Check(button.interactable&&(bool)Call("BeforeFire",ui),"reflooded T5 recovers with other selected tube still dry");
  ship.Boxes.Selected=1;Frame(ui);ship.Boxes.Selected=2;Check(!(bool)Call("BeforeFire",ui),"native confirmation rechecks current box selection");
  ship.Boxes.Selected=1;Frame(ui);ship.Depth=5;Frame(ui);Check(!ui.firingBlockade.activeSelf&&button.interactable,"return to shallow depth cannot restore a stale deep mask");
  // Both other policies retain native checkbox selection and native callbacks.
  foreach(var mode in new[]{ViewportMode.ReadOnly,ViewportMode.Synchronized}){
   Runtime.Viewport=mode;a.IsMarkedForLaunch=false;b.IsMarkedForLaunch=true;calls=ship.Boxes.Calls;Frame(ui);
   Check(ship.Boxes.Calls==calls&&!a.IsMarkedForLaunch&&b.IsMarkedForLaunch,"native selection untouched in "+mode);
   Check(!HiddenLayout.Active&&button.transform.parent==originalParent.transform&&nav.transform.parent==ui.transform,"authored button/navigation parents restored in "+mode);
   Check(ui.fireConfirmationPopup.transform.parent==button.transform&&!ui.fireConfirmationPopup.activeSelf,"authored confirmation hierarchy restored in "+mode);
   Check(rect.anchorMin.x==.2f&&rect.anchorMax.y==.7f&&rect.pivot.y==.8f&&rect.sizeDelta.x==101&&rect.anchoredPosition.x==72&&rect.localScale.y==3&&rect.localRotation.w==.4f&&rect.Sibling==7,"exact authored button layout restored in "+mode);
   button.interactable=false;Check((bool)Call("BeforeFire",ui),"ordinary native callbacks unchanged in "+mode);
  }
  Runtime.Viewport=ViewportMode.ReadOnly;ship.Depth=30;a.IsMarkedForLaunch=true;b.IsMarkedForLaunch=false;Frame(ui);Check(!ui.firingBlockade.activeSelf,"T5 UI depth exception also available in read-only mode");
  foreach(var mode in new[]{ViewportMode.ReadOnly,ViewportMode.Synchronized}){
   Runtime.Viewport=mode;a.IsMarkedForLaunch=b.IsMarkedForLaunch=true;b.IsFlooded=true;Frame(ui);
   Check(!ui.firingBlockade.activeSelf&&!button.interactable&&!(bool)Call("BeforeFire",ui),"flooded mixed selection blocked at depth in "+mode);
   b.IsFlooded=false;Frame(ui);Check(!ui.firingBlockade.activeSelf&&(bool)Call("BeforeFire",ui)&&a.IsMarkedForLaunch&&b.IsMarkedForLaunch,"drain restores deep T5 without rewriting native marks in "+mode);
   a.IsFlooded=false;Frame(ui);Check(!ui.firingBlockade.activeSelf&&!button.interactable&&!(bool)Call("BeforeFire",ui),"no flooded candidates has no deep exception in "+mode);a.IsFlooded=true;
  }
  foreach(var mode in new[]{ViewportMode.ReadOnly,ViewportMode.Synchronized}){
   Runtime.Viewport=mode;ship.Depth=45;ship.Boxes.Selected=2;a.IsFlooded=true;b.IsFlooded=true;a.IsMarkedForLaunch=true;b.IsMarkedForLaunch=false;calls=ship.Boxes.Calls;Frame(ui);
   Check(button.interactable&&!ui.firingBlockade.activeSelf,"T5 selected at 45 m can fire in "+mode);
   a.IsMarkedForLaunch=false;Frame(ui);Check(!button.interactable&&!ui.firingBlockade.activeSelf,"unchecking sole T5 blocks Feuer without covering checkbox in "+mode);
   a.IsMarkedForLaunch=true;Frame(ui);Check(button.interactable&&!ui.firingBlockade.activeSelf&&ship.Boxes.Calls==calls&&ship.Boxes.Selected==2,"recheck restores T5 immediately without moving selector or flooding in "+mode);
   b.IsMarkedForLaunch=true;Frame(ui);Check(!button.interactable&&!ui.firingBlockade.activeSelf,"mixed flooded selection leaves selection controls accessible in "+mode);
   b.IsMarkedForLaunch=false;Frame(ui);Check(button.interactable&&!ui.firingBlockade.activeSelf,"uncheck non-T5 restores firing without drain in "+mode);
   ship.Depth=5;Frame(ui);Check(button.interactable&&!ui.firingBlockade.activeSelf,"ascending restores ordinary native firing in "+mode);
   b.IsMarkedForLaunch=true;Frame(ui);Check((bool)Call("BeforeFire",ui),"shallow mixed selection still fires in "+mode);
   ship.Depth=51;Frame(ui);Check(!button.interactable&&!ui.firingBlockade.activeSelf&&!(bool)Call("BeforeFire",ui),"over 50 m denies firing without trapping selection in "+mode);
  }
  ship.Depth=30;
  Runtime.Active=false;Frame(ui);Check(ui.firingBlockade.activeSelf&&!HiddenLayout.Active,"disable restores depth blocker and original UI");Runtime.Active=true;
  Runtime.Viewport=ViewportMode.Hidden;ship.Depth=5;Frame(ui);ui.fireConfirmationPopup.SetActive(true);Call("Closed",ui);Check(!HiddenLayout.Active&&button.transform.parent==originalParent.transform&&ui.fireConfirmationPopup.activeSelf,"closing restores live button/popup without altering native active state");ui.fireConfirmationPopup.SetActive(false);
  Frame(ui);Runtime.Loading=true;Frame(ui);Check(!HiddenLayout.Active,"loading restores detached controls");Runtime.Loading=false;
  Frame(ui);Runtime.ViewingStation=false;Frame(ui);Check(!HiddenLayout.Active,"station exit restores detached controls");Runtime.ViewingStation=true;
  Frame(ui);ui.IsOpened=false;Frame(ui);Check(!HiddenLayout.Active,"closed panel cannot leak a floating Feuer");ui.IsOpened=true;
  NativePresentation.Restore();ui.transform.SetParent(null,false);HiddenLayout.Apply(ui,true);Check(!HiddenLayout.Active&&button.transform.parent==originalParent.transform,"missing panel parent never hides or strands Feuer");ui.transform.SetParent(canvas.transform,false);HiddenLayout.Apply(ui,true);Check(!HiddenLayout.Active,"failed layout is not retried/allocated every frame");HiddenLayout.Apply(ui,false);HiddenLayout.Apply(ui,true);Check(HiddenLayout.Active,"switching policy allows a recovered native layout to retry");HiddenLayout.Restore();
  var harmony=new HarmonyLib.Harmony();ViewingFire.Install(harmony);Check(harmony.Attempts.Count==6,"six native UI entry/lifecycle hooks installed");foreach(var method in harmony.Attempts)Check(method.DeclaringType==typeof(UBOAT.Game.UI.Periscope.TorpedoLaunchersUI),"hook excludes physical TDC and tube levers: "+method.Name);
  Console.WriteLine("PASS: "+count+" viewing fire/layout assertions (actual classes; independent native/Unity fixtures, no live game).");return count;
 }
}

namespace PinguUboatMultiplayer {internal static class MpStations {internal static bool BeforeTubesOpen(){return false;}internal static void AfterPeriscopeLateUpdate(){}internal static void CloseTubesOffStation(){} }}
public static class MpScopeChecks {
 static int count;
 static void Check(bool b,string name){count++;if(!b)throw new Exception("FAILED: "+name);}
 static object Call(string n,params object[] args){return typeof(MpScopeFire).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);}
 public static int Run(){
  Runtime.Active=true;Runtime.Loading=false;Runtime.MpScopeFeuer=false;Runtime.ViewingStation=true;Runtime.Viewport=ViewportMode.ReadOnly;Runtime.ViewingScope=new object();Runtime.AimContext=true;Runtime.LaunchBusy=false;Bridge.RestrictedScopes=true;
  var ui=new UBOAT.Game.UI.Periscope.TorpedoLaunchersUI{IsOpened=false,Launchers=new UBOAT.Game.UI.TorpedoLauncherUI[0]};var canvas=new UnityEngine.GameObject("canvas");ui.transform.SetParent(canvas.transform,false);ui.fireButton.transform.SetParent(ui.transform,false);ui.fireConfirmationPopup.transform.SetParent(ui.transform,false);ui.gameObject.SetActive(false);
  Runtime.Ui=new UBOAT.Game.UI.GameUI{CurrentTorpedoLaunchersUI=ui};var tube=new TorpedoLauncher{IsFlooded=true};Runtime.Ship=new PlayerShip{TorpedoLaunchers=new[]{tube},Boxes=new TorpedoFireControlBoxes()};
  var nativeButton=ui.fireButton;var failure=new HarmonyLib.Harmony{Fail="AfterPeriscopeLateUpdate"};MpScopeFire.Install(failure,typeof(PinguUboatMultiplayer.MpStations));Check(!MpScopeFire.Active&&failure.Installed.Count==0&&failure.Unpatches>0,"failed optional installation removes partial hooks without enabling exception");var h=new HarmonyLib.Harmony();MpScopeFire.Install(h,typeof(PinguUboatMultiplayer.MpStations));Check(h.Attempts.Count==7,"MP adapter installs only three MP UI hooks and native close/open-write guards");
  MpScopeFire.Tick();Check(!MpScopeFire.Active&&ui.OpenCalls==0,"default off leaves native MP restriction untouched");
  object[] open={false};Check((bool)Call("OpenCheck",open)&&!(bool)open[0],"MP gate unchanged while option off");
  Runtime.MpScopeFeuer=true;Bridge.RestrictedScopes=false;MpScopeFire.Tick();Check(!MpScopeFire.Active&&ui.OpenCalls==0,"no others aboard or unrestricted MP never activates feature");
  Bridge.RestrictedScopes=true;Runtime.ViewingScope=null;MpScopeFire.Tick();Check(!MpScopeFire.Active,"no scope station, no automatic Feuer");Runtime.ViewingScope=new object();
  Runtime.Loading=true;MpScopeFire.Tick();Check(!MpScopeFire.Active,"loading cannot expose a detached control");Runtime.Loading=false;
  Runtime.Active=false;MpScopeFire.Tick();Check(!MpScopeFire.Active,"master off disables MP feature");Runtime.Active=true;
  Check(MpScopeFire.Active,"host opt-in plus effective MP restriction at a scope activates feature");open=new object[]{false};Check(!(bool)Call("OpenCheck",open)&&(bool)open[0],"only restricted-scope display gate permits native open under opt-in");
  Runtime.AimContext=false;MpScopeFire.Tick();Check(ui.OpenCalls==0&&!HiddenLayout.Active,"no native aiming context defers opening");Runtime.AimContext=true;Runtime.LaunchBusy=true;MpScopeFire.Tick();Check(ui.OpenCalls==0,"joining during active salvo does not enable native tube widgets");Runtime.LaunchBusy=false;
  int probe=0;ui.OpenProbe=()=>{probe++;Check(ui.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0,"full panel is masked BEFORE native Open");Check(!ui.fireButton.interactable,"new display starts disabled until native readiness update");Check(!(bool)Call("OpeningWrites"),"automatic open suppresses changes to bearing/program/spread inputs");};
  MpScopeFire.Tick();Check(ui.OpenCalls==1&&ui.IsOpened&&probe==1,"native open owns normal initialization and listeners");Check(HiddenLayout.Active&&ui.fireButton==nativeButton&&nativeButton.transform.parent==canvas.transform,"only original Feuer detached from hidden native panel");Check(((UnityEngine.RectTransform)nativeButton.transform).anchoredPosition.y==144,"Feuer raised above bottom icon strip");
  Check(Runtime.Viewport==ViewportMode.ReadOnly,"MP display does not rewrite solo report policy");
  for(int i=0;i<100;i++)MpScopeFire.Tick();Check(ui.OpenCalls==1,"steady polling never reopens an already open native panel");
  Check(!(bool)Call("OpeningWrites"),"Feuer-only display and deferred Start cannot write hidden source/program/spread inputs");
  // Same viewing-fire bridge as hidden reports, even when the report policy is synchronized.
  Runtime.Viewport=ViewportMode.Synchronized;tube.IsMarkedForLaunch=false;typeof(ViewingFire).GetMethod("BeforeUpdate",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[0]);Check(tube.IsMarkedForLaunch,"forced MP Feuer uses fire-control selection despite synchronized reports");
  // Only the two MP UI-cleanup callbacks hold Close, with balanced finalizers.
  object[] state={false};Call("BeginScope",state);Check(!(bool)Call("OffStation"),"MP off-station UI cleanup is skipped without repeated close logging");Check((bool)state[0]&&MpScopeFire.KeepPanel(ui)&&!(bool)Call("BeforeClose",ui),"known MP UI cleanup cannot close permitted Feuer-only panel");
  var error=new Exception("synthetic upstream failure");Check(Call("EndScope",error,state[0])==error&&!MpScopeFire.KeepPanel(ui),"upstream exceptions preserve error and release temporary close guard");
  Check((bool)Call("BeforeClose",ui),"native escape/story close remains allowed outside MP cleanup");ui.Close();MpScopeFire.Tick();Check(ui.OpenCalls==1&&!ui.IsOpened,"genuine native close is not immediately reopened");
  Runtime.ViewingScope=new object();MpScopeFire.Tick();Check(ui.OpenCalls==2&&ui.IsOpened,"re-entering another scope resumes optional Feuer");
  Runtime.MpScopeFeuer=false;MpScopeFire.Tick();Check(!ui.IsOpened&&!HiddenLayout.Active&&nativeButton.transform.parent==ui.transform,"host disables toggle: auto-opened panel closes and exact button parent restored");Check(Runtime.Viewport==ViewportMode.Synchronized,"disabling MP option preserves report policy");Check((bool)Call("OpeningWrites"),"ordinary native input handlers restored outside forced MP mode");Check((bool)Call("OffStation"),"native MP off-station cleanup resumes outside opted-in mode");
  Runtime.MpScopeFeuer=true;MpScopeFire.Tick();Check(ui.OpenCalls==3,"host can re-enable display in same station");Bridge.RestrictedScopes=false;MpScopeFire.Tick();Check(!ui.IsOpened&&!HiddenLayout.Active,"last shared-boat peer leaves or MP permits panel: automatic display goes away");
  // Return to normal click-to-open behavior; already-open panels are retained.
  ui.OpenProbe=null;ui.Open(true,0,1,false);Bridge.RestrictedScopes=true;MpScopeFire.Tick();int closes=ui.CloseCalls;Bridge.RestrictedScopes=false;MpScopeFire.Tick();Check(ui.IsOpened&&ui.CloseCalls==closes,"panel opened by the player is not closed on returning to normal mode");
  Bridge.RestrictedScopes=true;MpScopeFire.Tick();Runtime.ViewingScope=null;Runtime.ViewingStation=false;MpScopeFire.Tick();Check(!HiddenLayout.Active&&nativeButton.transform.parent==ui.transform,"leaving scopes restores original control hierarchy");
  Runtime.MpScopeFeuer=false;Runtime.ViewingStation=true;Runtime.Viewport=ViewportMode.Hidden;NativePresentation.Refresh(ui);Check(HiddenLayout.Active&&ui.IsOpened,"solo Hidden Reports retains normal player-opened Feuer-only layout");MpScopeFire.Release();NativePresentation.Restore();
  foreach(var method in h.Attempts)Check(method.DeclaringType==typeof(PinguUboatMultiplayer.MpStations)||method.DeclaringType==typeof(UBOAT.Game.UI.Periscope.TorpedoLaunchersUI),"adapter hook never targets native physical TDC/tube fire levers: "+method.Name);
  Console.WriteLine("PASS: "+count+" MP scope Feuer assertions (actual adapter, layout and presentation; independent MP/native fixtures, no live network).");return count;
 }
}

