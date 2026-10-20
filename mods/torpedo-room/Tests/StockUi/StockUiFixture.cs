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
 public class Object {public static void Destroy(object o){} }
 public class Component {public GameObject gameObject=new GameObject("fixture");public Transform transform=new RectTransform();public T GetComponent<T>() where T:new(){return gameObject.GetComponent<T>();}public T[] GetComponentsInChildren<T>(bool all){return gameObject.Children<T>();} }
 public class MonoBehaviour:Component {}
 public class GameObject {
  public T[] Children<T>(){var list=new List<T>();foreach(var v in components.Values)if(v is T)list.Add((T)v);return list.ToArray();}
  readonly Dictionary<Type,object> components=new Dictionary<Type,object>();public bool activeSelf=true;public Transform transform=new RectTransform();
  public GameObject(string s,params Type[] t){}public void SetActive(bool b){activeSelf=b;}
  public T GetComponent<T>() where T:new(){object value;return components.TryGetValue(typeof(T),out value)?(T)value:default(T);}
  public T AddComponent<T>() where T:new(){var value=new T();components[typeof(T)]=value;return value;}
 }
 public class CanvasGroup {public bool interactable=true,blocksRaycasts=true;public float alpha=1;}
 public class Sprite {}
 public static class Debug {public static void LogWarning(object o){} }
 public class Transform {public Transform parent;public GameObject gameObject;public void SetParent(Transform t,bool b){parent=t;} }
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;}
 public class CanvasRenderer {}
 public struct Vector2 {public Vector2(float a,float b){} }
 public struct Color {public Color(float a,float b,float c){} }
 public static class Time {public static float unscaledTime;}
}
namespace UnityEngine.UI { public class Toggle {public bool isOn;public int Notifications;public void SetIsOnWithoutNotify(bool value){isOn=value;}} public class Slider {
 public Action<float> Callback;bool whole=true;float min=2,max=28,current=8;
 public bool wholeNumbers {get{return whole;}set{whole=value;Update(current);}}
 public float minValue {get{return min;}set{min=value;Update(current);}}
 public float value {get{return current;}set{Update(value);}}
 public void SetValueWithoutNotify(float v){current=Math.Max(min,Math.Min(max,v));}
 void Update(float v){v=Math.Max(min,Math.Min(max,v));if(whole)v=(float)Math.Round(v);if(v!=current){current=v;if(Callback!=null)Callback(v);}}
} public class Image:UnityEngine.Component {public UnityEngine.Sprite sprite;public bool enabled=true;} public class Button:UnityEngine.Component {} }
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
 public class PlayerShip {public PlayerShipTorpedoAttackSettings TorpedoAttackSettings=new PlayerShipTorpedoAttackSettings();public bool TorpedoProgramDirection;public float TorpedoProgramDistance=1500,TorpedoProgramAngleAbsolute;public TorpedoLauncher[] TorpedoLaunchers;}
}
namespace UBOAT.Game.Scene.Items {
 public class StoredTorpedo {public string Name="G7e Torpedo T3 - Pi2 - LUT",name="G7e Torpedo T3 - Pi2 - LUT";public PreparationStatus Saved=new PreparationStatus();public Torpedo Missile=new Torpedo();}
 public class TorpedoLauncher {public bool IsMarkedForLaunch=true,IsLoaded=true;public StoredTorpedo Torpedo=new StoredTorpedo();}
}
namespace UBOAT.Game.UI {public class GameUI {public UBOAT.Game.UI.Periscope.TorpedoLaunchersUI CurrentTorpedoLaunchersUI;}
 public class TorpedoLaunchersSpeedDialUI:UnityEngine.MonoBehaviour {}
 public class TorpedoLaunchersDepthDialUI:UnityEngine.MonoBehaviour {void OnEnable(){}void Update(){}void OnValueChanged(float value){}}
 public class TorpedoLauncherUI:UnityEngine.MonoBehaviour {public UnityEngine.UI.Image torpedoImage=new UnityEngine.UI.Image();public UnityEngine.UI.Button pistolTypeButton=new UnityEngine.UI.Button();public TorpedoLauncher Launcher;} }
namespace UBOAT.Game.UI.Periscope {public class TorpedoLaunchersUI:UnityEngine.MonoBehaviour {
  public UnityEngine.GameObject programParameters=new UnityEngine.GameObject("params"),dataPanel=new UnityEngine.GameObject("data");public TMPro.TextMeshProUGUI programSettingsName=new TMPro.TextMeshProUGUI();public UnityEngine.GameObject fireButton=new UnityEngine.GameObject("fire");
  public UBOAT.Game.UI.TorpedoLauncherUI[] Launchers;public bool IsOpened=true;public int Calls;public bool FailOnce;
  public UnityEngine.UI.Toggle programLeft=new UnityEngine.UI.Toggle(),programRight=new UnityEngine.UI.Toggle();
  public TMPro.TMP_InputField programDistance=new TMPro.TMP_InputField(),programAngle=new TMPro.TMP_InputField();
  void SetProgramDisplayValues(){Calls++;if(FailOnce){FailOnce=false;throw new Exception("synthetic transient refresh error");}programDistance.text=Landomatic.TorpedoRoom.Runtime.Ship.TorpedoProgramDistance.ToString();programAngle.text=Landomatic.TorpedoRoom.Runtime.Ship.TorpedoProgramAngleAbsolute.ToString();}
 } }
namespace Landomatic.TorpedoRoom {
 public class PreparationStatus {public Preparation Value=new Preparation {ProgramOverride=true};}
 internal static class NativeSkin {public static UnityEngine.GameObject ManualRoot=new UnityEngine.GameObject("manual");}
 internal static class Bridge {public static bool IsRemote;public static TubeSnapshot[] Sent;public static void RequestViewport(TubeSnapshot[] edits){Sent=edits;} }
 internal static class Runtime {
  public static UBOAT.Game.UI.GameUI Ui;public static T Get<T>() where T:class{return Ui as T;}
  public static int Applying;public static bool RoomEditorOpen;public static object CurrentGroup;public static bool ViewingStation=true;public static ViewportMode Viewport=ViewportMode.ReadOnly;public static int IndexOf(Torpedo t){return 0;}
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
  Runtime.Viewport=ViewportMode.Hidden;NativePresentation.Refresh(ui);Check(tubeUi.gameObject.GetComponent<UnityEngine.CanvasGroup>().alpha==0&&!tubeUi.gameObject.GetComponent<UnityEngine.CanvasGroup>().blocksRaycasts,"hidden reports hide native tube icons and clicks");
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
  var slider=new UnityEngine.UI.Slider();depthCall("Tune",new object[]{slider});Check(!slider.wholeNumbers,"native dial permits quarter-metre depth instead of whole slider units");Check(slider.minValue==1,"native lower bound accepts 0.5 metres in double-depth slider units");
  a.Value.Depth=.5f;args=new object[]{null};depthCall("Project",args);slider.SetValueWithoutNotify(ship.TorpedoAttackSettings.TorpedoDepth*2);Check(slider.value==1,"room half-metre depth projects without native one-metre clamping");depthCall("Restore",args);
  object[] halfEdit={1f,false};Check((bool)depthCall("Before",halfEdit)&&(float)halfEdit[0]==1,"native half-metre edit remains selectable");
  ship.TorpedoAttackSettings.TorpedoDepth=3;slider.value=1;slider.Callback=v=>{object[] cb={v,false};if((bool)depthCall("Before",cb)){ship.TorpedoAttackSettings.TorpedoDepth=(float)cb[0]/2;depthCall("After",new object[]{cb[1]});}};
  int revision=a.Value.Revision;Runtime.Active=false;DepthControl.RestoreSteps();Check(slider.wholeNumbers&&slider.minValue==2,"disabling mod restores native dial stepping and minimum");Check(ship.TorpedoAttackSettings.TorpedoDepth==3&&a.Value.Revision==revision&&Runtime.Applying==0,"restoration callback cannot leak shared depth or a preparation edit");Runtime.Active=true;
  slider.wholeNumbers=false;slider.minValue=0;slider.value=.5f;ship.TorpedoAttackSettings.TorpedoDepth=4;depthCall("Tune",new object[]{slider});Check(slider.minValue==1&&ship.TorpedoAttackSettings.TorpedoDepth==4&&Runtime.Applying==0,"tuning minimum guards native range callbacks and preserves shared setting");
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
