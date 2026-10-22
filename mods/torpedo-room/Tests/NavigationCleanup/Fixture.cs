// Independently authored offline doubles; never shipped in the player package.
using System;using System.Collections.Generic;using System.Reflection;using System.Runtime.CompilerServices;
namespace HarmonyLib {public static class Priority {public const int Last=0,First=800;}public class HarmonyMethod {public int priority;public HarmonyMethod(MethodInfo m){}}public class Harmony {public bool Fail;public int Installs,Removed;public void Patch(MethodInfo m,HarmonyMethod prefix){Installs++;if(Fail)throw new Exception("synthetic patch failure");}public void Unpatch(MethodBase m,MethodInfo p){Removed++;}} public static class AccessTools {public static Type TypeByName(string n){return typeof(DustysCommunityPatch.Core.QolConfig).Assembly.GetType(n);}public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);}public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);}}}
namespace UnityEngine {
 public class Object {public static void Destroy(GameObject go){go.Destroyed=true;go.SetActive(false);}}
 public class Component {public GameObject gameObject;public Transform transform{get{return gameObject.transform;}}public T GetComponent<T>(){return gameObject.GetComponent<T>();}}
 public class MonoBehaviour:Component {public bool enabled=true;}
 public class GameObject:Object {public static List<GameObject> All=new List<GameObject>();public bool Destroyed,activeSelf=true;public string name;public Scene scene=new Scene();public RectTransform transform;Dictionary<Type,object> parts=new Dictionary<Type,object>();public bool activeInHierarchy{get{return activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);}}public GameObject(string n,params Type[] types){name=n;transform=new RectTransform{gameObject=this};All.Add(this);foreach(var t in types)if(t!=typeof(RectTransform))Add(t);}object Add(Type t){var v=Activator.CreateInstance(t);parts[t]=v;var c=v as Component;if(c!=null)c.gameObject=this;return v;}public T AddComponent<T>(){return (T)Add(typeof(T));}public T GetComponent<T>(){object v;return parts.TryGetValue(typeof(T),out v)?(T)v:default(T);}public void SetActive(bool v){activeSelf=v;}}
 public class Scene {public bool Valid=true;public bool IsValid(){return Valid;}}
 public class Transform {public T GetComponent<T>(){return gameObject.GetComponent<T>();}public GameObject gameObject;public Transform parent;public Vector3 localScale;public Quaternion localRotation;public int Sibling;public void SetParent(Transform p,bool world){parent=p;}public void SetAsLastSibling(){Sibling=999;}public int GetSiblingIndex(){return Sibling;}public void SetSiblingIndex(int n){Sibling=n;}}
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta,offsetMin,offsetMax;public Rect rect{get{return new Rect{size=sizeDelta};}}}
 public struct Rect {public Vector2 size;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero{get{return new Vector2(0,0);}}public static Vector2 one{get{return new Vector2(1,1);}}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public struct Quaternion {public float w;}
 public struct Color {public Color(float r,float g,float b,float a){}public static Color white{get{return new Color(1,1,1,1);}}}
 public class Canvas:Component {public RenderMode renderMode;public int sortingOrder;}
 public enum RenderMode {ScreenSpaceOverlay}
 public static class Time {public static float unscaledTime;public static int frameCount;}
 public enum KeyCode {Escape}
 public static class Input {public static bool EscapeHeld;public static bool GetKey(KeyCode key){return EscapeHeld;}}
 public static class Resources {public static T[] FindObjectsOfTypeAll<T>(){var list=new List<T>();foreach(var go in GameObject.All){var c=go.GetComponent<T>();if(c!=null)list.Add(c);}return list.ToArray();}}
 public static class Debug {public static void LogWarning(object o){}}
}
namespace UnityEngine.UI {
 public class Graphic:UnityEngine.Component {}
 public class Image:Graphic {public UnityEngine.Color color;}
 public class Button:UnityEngine.Component {public bool interactable=true;public Graphic targetGraphic;public Click onClick=new Click();}
 public class Click {public Action Action;public void AddListener(Action a){Action+=a;}public void Invoke(){if(Action!=null)Action();}}
 public class CanvasScaler:UnityEngine.Component {public enum ScaleMode {ScaleWithScreenSize}public ScaleMode uiScaleMode;public UnityEngine.Vector2 referenceResolution;public float matchWidthOrHeight;}
 public class GraphicRaycaster:UnityEngine.Component {}
 public class ScrollRect:UnityEngine.Component {public UnityEngine.RectTransform content;public void StopMovement(){}}
}
namespace TMPro {public enum TextAlignmentOptions {Center}public class TextMeshProUGUI:UnityEngine.Component {public string text;public float fontSize;public UnityEngine.Color color;public bool enableWordWrapping,richText,raycastTarget;public TextAlignmentOptions alignment;}}
namespace UBOAT.Game.UI {public class BasicManualModeUI:UnityEngine.MonoBehaviour {public UnityEngine.UI.Button leaveStationButton;}}
namespace UBOAT.Game.Core.Input {public class GameInput {public System.Collections.Generic.HashSet<object> Blockades=new System.Collections.Generic.HashSet<object>();public void AddBlockade(object o){Blockades.Add(o);}public void RemoveBlockade(object o){Blockades.Remove(o);}}}
namespace UBOAT.Game.Core {public enum LocalizedStringFlags {Unlocalized}public class LocalizedString {public LocalizedString(string s,LocalizedStringFlags f){}}}
namespace UBOAT.Game.Core.Serialization {public class SavesManager {public System.Collections.Generic.HashSet<object> Blocks=new System.Collections.Generic.HashSet<object>();public void AddSaveBlockade(object o,UBOAT.Game.Core.LocalizedString s){Blocks.Add(o);}public void RemoveSaveBlockade(object o){Blocks.Remove(o);}}}
namespace UBOAT.Game.Scene {public enum CameraMode {FPP,Generic}}
namespace UBOAT.Game.Scene.Characters.Activators {public class Activator {}}
namespace UBOAT.Game.Scene.Characters.Actions {public class CharacterAction {public int Completed;public void CompleteAction(){Completed++;}}public class UseManuallyOperableDeviceAction:CharacterAction {}public class LoadTorpedoesAction:CharacterAction {public bool warmTorpedo;}public class WarmUpTorpedoAction:CharacterAction {}}
namespace UBOAT.Game.Scene.Characters {public class PlayableCharacter {public bool IsImpersonated=true,IsInteracting,IsSwapping;public UBOAT.Game.Scene.Characters.Actions.CharacterAction Action,LastRequest;public UBOAT.Game.Scene.Characters.Activators.Activator LastActivator;public int Starts,Cancelled;public void StartAction(UBOAT.Game.Scene.Characters.Activators.Activator a,UBOAT.Game.Scene.Characters.Actions.CharacterAction c){Starts++;LastRequest=c;LastActivator=a;}}}
namespace UBOAT.Game.Scene.Items {public class TorpedoLauncherGroup {public object TorpedoStorage=new object();}}
namespace UBOAT.Game.Scene.Camera {public class Generic {public UBOAT.Game.Scene.Characters.PlayableCharacter Character=new UBOAT.Game.Scene.Characters.PlayableCharacter();public object ExternalController=new object();}public enum CameraTransitionType {Instant}public class MainCamera {public static MainCamera Instance=new MainCamera();public Generic GenericController=new Generic();public UBOAT.Game.Scene.CameraMode CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;public bool StopAction,IsInTransition;public int Handoffs;public void SetFPPController(UBOAT.Game.Scene.Characters.PlayableCharacter c,CameraTransitionType t,bool stop,float scale){Handoffs++;StopAction=stop;CurrentMode=UBOAT.Game.Scene.CameraMode.FPP;}}}
namespace UBOAT.Game.UI.Storage {public enum StorageUIMode {Auto}public class StorageUI {public event Action Closed;public void Close(){IsOpened=false;if(Closed!=null)Closed();}public bool IsOpened;public int Opens;public object Character,Storage;public Action BeforeOpen;public void Open(object c,object s,StorageUIMode mode,object a,object b,object d,bool e){if(BeforeOpen!=null)BeforeOpen();Character=c;Storage=s;Opens++;IsOpened=true;}}}
namespace Landomatic.TorpedoRoom {
 internal static partial class NativeSkin {internal static void RestoreStations(){RoomExit(null);}}
 internal static class NativePresentation {public static bool Hidden=true;internal static void Room(bool v){Hidden=v;}}
 internal static class UiScale {internal static UnityEngine.Vector3 Between(UnityEngine.Transform a,UnityEngine.Transform b){return new UnityEngine.Vector3(2,2,1);}internal static void SetScale(UnityEngine.RectTransform r,UnityEngine.Vector3 v){r.localScale=v;}}
 internal static class Config {public static bool Interface=true;}
 internal static class Bridge {public static bool IsRemote;}
 public sealed partial class Runtime {public static UBOAT.Game.UI.Storage.StorageUI Storage;public static object Ship=new object();public static UBOAT.Game.Core.Input.GameInput Inputs=new UBOAT.Game.Core.Input.GameInput();public static UBOAT.Game.Core.Serialization.SavesManager Saves=new UBOAT.Game.Core.Serialization.SavesManager();public static T Get<T>() where T:class{return Storage as T??Inputs as T??Saves as T;}internal float nextRoomMask,nextGridRender;internal void TestTick(){TickOrdnanceReturn();}internal void TestCancel(){CancelOrdnanceHandoff();}internal void TestLate(){FinishOrdnanceOrderFrame();TickDeferredOrdnance();}internal bool InitialAvailable{get{return roomHandoff.CanShow(CurrentGroup,RoomStationToken,false,false);}}public static UBOAT.Game.Scene.Items.TorpedoLauncherGroup CurrentGroup;public static bool Loading;public static bool Active=true;internal bool roomTakeover=true;internal UnityEngine.GameObject gridRoot=new UnityEngine.GameObject("room canvas");internal int Closed;void CloseGrid(){Closed++;}internal void TestOpen(){OpenOrdnance();}internal bool Dismissed{get{return roomHandoff.Dismissed;}}public static Runtime Instance=new Runtime();public UnityEngine.Transform transform=new UnityEngine.GameObject("runtime").transform;public static string Notice;public static void Say(string s){Notice=s;}}
 public static class CommunityPatchSettings {public static object[] GetSettings(){return new object[0];}}
}
namespace DustysCommunityPatch.Core {
 public static class QolConfig {public static Dictionary<string,bool> Values=new Dictionary<string,bool>();public static int Saves;public static void SetBool(string k,bool v){Values[k]=v;}public static void SetString(string k,string v){}public static void Save(){Saves++;}}
 public static class QolPanel {public static UnityEngine.GameObject _settingsPane;public static void RefreshSettingsPane(){}}
 public static class ExternalMods {[MethodImpl(MethodImplOptions.NoInlining)]public static bool ApplyStoredValues(){return Landomatic.TorpedoRoom.MenuIntegration.RestoringSettings;}}
}
public static class NavigationCleanupChecks {
 static int count;static void Check(bool b,string m){count++;if(!b)throw new Exception(m);}
 static UnityEngine.GameObject Latest(string n){for(int i=UnityEngine.GameObject.All.Count-1;i>=0;i--)if(UnityEngine.GameObject.All[i].name==n)return UnityEngine.GameObject.All[i];throw new Exception(n);}
 public static string Run(){
  Check(!Landomatic.TorpedoRoom.MenuIntegration.RestoringSettings,"normal request is not restoration");
  Check(DustysCommunityPatch.Core.ExternalMods.ApplyStoredValues(),"Dusty startup restoration identified");
  Check(!Landomatic.TorpedoRoom.MenuIntegration.RestoringSettings,"restoration does not latch");
  DustysCommunityPatch.Core.QolConfig.SetBool("Torpedo_Room.removeRecords",true);DustysCommunityPatch.Core.QolConfig.SetBool("OtherMod.enabled",true);
  Landomatic.TorpedoRoom.MenuIntegration.ResetCleanupFlag();Landomatic.TorpedoRoom.MenuIntegration.Tick();
  Check(!DustysCommunityPatch.Core.QolConfig.Values["Torpedo_Room.removeRecords"],"one-shot value cleared after wrapper persists true");Check(DustysCommunityPatch.Core.QolConfig.Values["OtherMod.enabled"],"other config untouched");Check(DustysCommunityPatch.Core.QolConfig.Saves==1,"reset saved once");
  Landomatic.TorpedoRoom.MenuIntegration.Tick();Check(DustysCommunityPatch.Core.QolConfig.Saves==1,"no per-frame config writes");
  int removed=0;Landomatic.TorpedoRoom.CleanupFeedback.Confirm("Confirm","Instructions",()=>removed++);Check(removed==0,"opening confirmation does not cleanup");
  var first=Latest("TorpedoRoom.CleanupResult");Latest("Cancel").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(removed==0,"cancel preserves records");Check(first.Destroyed,"cancel dismisses");
  Landomatic.TorpedoRoom.CleanupFeedback.Confirm("Confirm","Instructions",()=>{removed++;Landomatic.TorpedoRoom.CleanupFeedback.Show("Complete","Save separately");});
  Latest("Remove records").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(removed==1,"confirmation performs action once");Check(Latest("TorpedoRoom.CleanupResult").activeSelf,"completion remains visible");
  Latest("OK").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(removed==1,"OK does not repeat cleanup");
  var native=new UnityEngine.GameObject("native");var ui=native.AddComponent<UBOAT.Game.UI.BasicManualModeUI>();var go=new UnityEngine.GameObject("leave");var button=go.AddComponent<UnityEngine.UI.Button>();ui.leaveStationButton=button;
  var originalParent=new UnityEngine.GameObject("old parent").transform;var rect=go.transform;rect.SetParent(originalParent,false);rect.anchorMin=new UnityEngine.Vector2(.2f,.3f);rect.anchorMax=new UnityEngine.Vector2(.8f,.9f);rect.pivot=new UnityEngine.Vector2(.1f,.4f);rect.anchoredPosition=new UnityEngine.Vector2(30,40);rect.sizeDelta=new UnityEngine.Vector2(80,60);rect.localScale=new UnityEngine.Vector3(1.5f,1.25f,1);rect.localRotation=new UnityEngine.Quaternion{w=.7f};rect.Sibling=7;
  int exits=0;button.onClick.AddListener(()=>exits++);button.interactable=false;var target=new UnityEngine.GameObject("overlay").transform;
  Landomatic.TorpedoRoom.NativeSkin.RoomExit(target);Check(rect.parent!=originalParent&&rect.parent.parent==target,"native exit moved above room overlay");Check(ui.leaveStationButton==button,"original button retained");Check(!button.interactable,"native permission retained");Check(exits==0,"moving does not invoke exit");Check(rect.sizeDelta.x==80&&rect.sizeDelta.y==60,"authored size retained");
  Check(rect.parent.gameObject.name=="Native Leave position","bottom-right holder created");Check(((UnityEngine.RectTransform)rect.parent).anchorMin.x==1,"holder anchored right");Check(((UnityEngine.RectTransform)rect.parent).anchorMin.y==0,"holder anchored bottom");
  button.onClick.Invoke();Check(exits==1,"original exit listener intact");
  Landomatic.TorpedoRoom.NativeSkin.RoomExit(null);Check(rect.parent==originalParent,"exit parent restored");Check(rect.anchorMin.x==.2f&&rect.anchorMax.y==.9f,"anchors restored");Check(rect.pivot.x==.1f&&rect.anchoredPosition.y==40,"pivot position restored");Check(rect.localScale.x==1.5f&&rect.localRotation.w==.7f,"scale rotation restored");Check(rect.Sibling==7,"sibling restored");
  var failedHook=new HarmonyLib.Harmony{Fail=true};Landomatic.TorpedoRoom.OrdnanceDispatch.Install(failedHook);Check(!Landomatic.TorpedoRoom.OrdnanceDispatch.Available&&failedHook.Removed==1,"failed optional hook removed; ordnance shortcut fails closed");
  Landomatic.TorpedoRoom.OrdnanceRelease.Install(new HarmonyLib.Harmony());
  Landomatic.TorpedoRoom.OrdnanceDispatch.Install(new HarmonyLib.Harmony());Check(Landomatic.TorpedoRoom.OrdnanceDispatch.Available,"native order hook installed");
  var room=new Landomatic.TorpedoRoom.Runtime();Landomatic.TorpedoRoom.Runtime.Instance=room;var inventory=new UBOAT.Game.UI.Storage.StorageUI();Landomatic.TorpedoRoom.Runtime.Storage=inventory;Landomatic.TorpedoRoom.Runtime.CurrentGroup=new UBOAT.Game.Scene.Items.TorpedoLauncherGroup();
  inventory.BeforeOpen=()=>{Check(room.Closed==1,"Prep/input close before ordnance open");Check(!room.gridRoot.activeSelf,"room overlay gone before ordnance open");Check(Landomatic.TorpedoRoom.NativePresentation.Hidden,"native torpedo panel stays masked during inventory handoff");};
  room.TestOpen();Check(inventory.Opens==1&&inventory.IsOpened,"native inventory opens");Check(room.Dismissed&&!room.roomTakeover,"handoff latches full room dismissal");Check(inventory.Storage==Landomatic.TorpedoRoom.Runtime.CurrentGroup.TorpedoStorage,"current compartment storage passed");Check(inventory.Character==UBOAT.Game.Scene.Camera.MainCamera.Instance.GenericController.Character,"inventory bound to native station camera character; native prompt enabled");
  inventory.Close();Check(!room.Dismissed&&room.InitialAvailable,"inventory close returns to initial station display");
  Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==1,"inventory close guards Escape before camera can consume it");
  room.TestTick();Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==1,"guard survives closing frame");
  UnityEngine.Time.frameCount=1;room.TestTick();Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==1,"guard survives next update order");
  UnityEngine.Time.frameCount=2;UnityEngine.Input.EscapeHeld=true;room.TestTick();Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==1,"held Escape does not exit station");
  UnityEngine.Input.EscapeHeld=false;room.TestTick();Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==0,"guard releases after close and key release");
  Landomatic.TorpedoRoom.Runtime.Loading=true;room.TestOpen();Check(inventory.Opens==1,"loading blocks new ordnance request");Landomatic.TorpedoRoom.Runtime.Loading=false;
  inventory.BeforeOpen=null;room.TestOpen();Landomatic.TorpedoRoom.Runtime.Ship=new object();inventory.Close();Check(room.Dismissed,"closing inventory from a different world cannot reopen stale station");Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==0,"world mismatch adds no return guard");
  room.TestOpen();room.TestCancel();inventory.Close();Check(room.Dismissed,"load/teardown detaches inventory close callback");
  room.TestOpen();inventory.Close();Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==1,"second valid close installs guard");room.TestCancel();Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==0,"load/teardown releases return guard");
  var camera=UBOAT.Game.Scene.Camera.MainCamera.Instance;var actor=camera.GenericController.Character;
  var before=typeof(Landomatic.TorpedoRoom.OrdnanceDispatch).GetMethod("BeforeOrder",BindingFlags.Static|BindingFlags.NonPublic);
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();
  var activation=new UBOAT.Game.Scene.Characters.Activators.Activator();
  bool allowed=(bool)before.Invoke(null,new object[]{actor,activation,new UBOAT.Game.Scene.Characters.Actions.CharacterAction()});Check(allowed&&camera.Handoffs==0,"unrelated action untouched");
  before.Invoke(null,new object[]{new UBOAT.Game.Scene.Characters.PlayableCharacter(),activation,new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction()});Check(camera.Handoffs==0,"other character untouched");
  var nativeGroup=Landomatic.TorpedoRoom.Runtime.CurrentGroup;Landomatic.TorpedoRoom.Runtime.CurrentGroup=new UBOAT.Game.Scene.Items.TorpedoLauncherGroup();before.Invoke(null,new object[]{actor,activation,new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction()});Check(camera.Handoffs==0,"different station untouched");Landomatic.TorpedoRoom.Runtime.CurrentGroup=nativeGroup;
  var load=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction{warmTorpedo=true};allowed=(bool)before.Invoke(null,new object[]{actor,activation,load});Check(!allowed,"exact request deferred before native task starts");Check(Landomatic.TorpedoRoom.Runtime.Saves.Blocks.Count==1,"short handoff blocks saving half-dispatched order");Check(camera.Handoffs==1&&!camera.StopAction,"old station released without starting new action");Check(actor.Starts==0&&actor.Cancelled==0,"no direct crew cancellation or premature task");Check(inventory.IsOpened,"inventory not destroyed inside its Update");
  room.TestLate();Check(!inventory.IsOpened&&room.Dismissed,"inventory closes at safe frame while request retained");Check(Landomatic.TorpedoRoom.Runtime.Inputs.Blockades.Count==0,"work does not install Escape return guard");
  UnityEngine.Time.unscaledTime=.22f;room.OrdnanceCameraChanging(actor);Check(actor.Starts==0,"delayed second camera return cannot cancel unstarted request");
  UnityEngine.Time.unscaledTime=.3f;room.TestLate();Check(actor.Starts==0,"camera change resets quiet interval");
  UnityEngine.Time.unscaledTime=.79f;room.TestLate();Check(actor.Starts==0,"full continuous calm interval required");
  UnityEngine.Time.unscaledTime=.81f;room.TestLate();Check(actor.Starts==1&&actor.LastRequest==load&&actor.LastActivator==activation,"replay uses exact action activator and maintenance choice");
  room.TestLate();Check(actor.Starts==1,"replay once only");Check(Landomatic.TorpedoRoom.Runtime.Saves.Blocks.Count==0,"replay releases save blockade");
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var maintain=new UBOAT.Game.Scene.Characters.Actions.WarmUpTorpedoAction();allowed=(bool)before.Invoke(null,new object[]{actor,activation,maintain});room.TestLate();UnityEngine.Time.unscaledTime=1.4f;room.TestLate();Check(!allowed&&actor.Starts==2&&actor.LastRequest==maintain,"maintenance uses same settled handoff");
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var plain=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction();before.Invoke(null,new object[]{actor,activation,plain});room.TestLate();actor.IsInteracting=true;UnityEngine.Time.unscaledTime=2;room.TestLate();Check(actor.Starts==2,"active physical interaction blocks dispatch");actor.IsInteracting=false;UnityEngine.Time.unscaledTime=2.1f;room.TestLate();UnityEngine.Time.unscaledTime=2.61f;room.TestLate();Check(actor.Starts==3&&actor.LastRequest==plain&&!plain.warmTorpedo,"load-only choice retained");
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var cancelled=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction();before.Invoke(null,new object[]{actor,activation,cancelled});room.TestLate();room.OrdnanceCameraChanging(new UBOAT.Game.Scene.Characters.PlayableCharacter());UnityEngine.Time.unscaledTime=4;room.TestLate();Check(actor.Starts==3&&cancelled.Completed==1,"switching actor aborts and releases held order");Check(Landomatic.TorpedoRoom.Runtime.Saves.Blocks.Count==0,"abort releases save blockade");
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var timed=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction();before.Invoke(null,new object[]{actor,activation,timed});camera.IsInTransition=true;room.TestLate();UnityEngine.Time.unscaledTime=12.1f;room.TestLate();Check(actor.Starts==3&&timed.Completed==1,"timeout completes only unstarted held action");Check(Landomatic.TorpedoRoom.Runtime.Saves.Blocks.Count==0,"timeout releases save blockade");camera.IsInTransition=false;
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var superseded=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction();before.Invoke(null,new object[]{actor,activation,superseded});room.TestLate();actor.Action=new UBOAT.Game.Scene.Characters.Actions.CharacterAction();room.TestLate();Check(superseded.Completed==1&&actor.Starts==3,"new crew task supersedes deferred request");actor.Action=null;
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var disabled=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction();before.Invoke(null,new object[]{actor,activation,disabled});room.TestLate();Landomatic.TorpedoRoom.Config.Interface=false;room.TestLate();Check(disabled.Completed==1&&actor.Starts==3,"disabling interface abandons only held request");Check(Landomatic.TorpedoRoom.Runtime.Saves.Blocks.Count==0,"disable releases save guard");Landomatic.TorpedoRoom.Config.Interface=true;
  camera.CurrentMode=UBOAT.Game.Scene.CameraMode.Generic;room.TestOpen();var stale=new UBOAT.Game.Scene.Characters.Actions.LoadTorpedoesAction();before.Invoke(null,new object[]{actor,activation,stale});room.TestLate();Landomatic.TorpedoRoom.Runtime.Ship=new object();room.TestLate();Check(stale.Completed==0&&actor.Starts==3,"changed world is not mutated by stale request");Check(Landomatic.TorpedoRoom.Runtime.Saves.Blocks.Count==0,"world reset releases save guard");
  var gate=new Landomatic.TorpedoRoom.OrdnanceSettle(0);Check(!gate.Ready(0,true),"gate starts unready");Check(!gate.Ready(.4f,true),"gate rejects short calm interval");gate.Disturbed(.45f);Check(!gate.Ready(.8f,true),"disturbance resets calm");Check(!gate.Ready(1.2f,false),"interaction resets calm");Check(!gate.Ready(1.3f,true),"new calm must last full duration");Check(gate.Ready(1.81f,true),"gate accepts stable interval");Check(!gate.TimedOut(7.99f)&&gate.TimedOut(8),"bounded timeout exact boundary");
  var state=new Landomatic.TorpedoRoom.RoomHandoff();var fore=new object();var aft=new object();var foreView=new object();var aftView=new object();
  Check(state.CanShow(fore,foreView,false,false),"initial station shows room");state.Dismiss(fore,foreView);
  Check(!state.CanShow(fore,foreView,true,false),"inventory excludes room");Check(!state.CanShow(null,null,true,true),"inventory rejects T and incidental camera exit");
  var returnView=new object();Check(!state.CanShow(fore,returnView,false,false),"closing inventory absorbs controller changes without reopening");Check(!state.CanShow(fore,returnView,false,false),"same station remains closed");
  Check(state.CanShow(aft,aftView,false,false),"switching fore to aft restores initial display");
  state.Dismiss(aft,aftView);Check(!state.CanShow(aft,aftView,true,false),"second inventory entry closes UI");Check(!state.CanShow(aft,aftView,false,false),"second inventory close stays closed");
  Check(!state.CanShow(null,null,false,false),"leaving station has no room display");Check(state.CanShow(aft,aftView,false,false),"re-entering same compartment restores display");
  state.Dismiss(fore,foreView);Check(state.CanShow(fore,new object(),false,false),"new station controller restores display even if group unchanged");
  state.Dismiss(fore,foreView);Check(state.CanShow(fore,foreView,false,true),"T explicitly reopens current station");
  state.Dismiss(fore,foreView);state.Reset();Check(state.CanShow(fore,foreView,false,false),"world reset clears dismissal");
  return count+" assertions passed";
 }
}

namespace Landomatic.TorpedoRoom {internal static class OrdnanceDiagnostics {internal static void Mark(string s){}}}
