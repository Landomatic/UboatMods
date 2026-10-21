// Independently authored offline doubles; never shipped in the player package.
using System;using System.Collections.Generic;using System.Reflection;using System.Runtime.CompilerServices;
namespace HarmonyLib { public static class AccessTools {public static Type TypeByName(string n){return typeof(DustysCommunityPatch.Core.QolConfig).Assembly.GetType(n);}public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);}public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);}}}
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
 public static class Time {public static float unscaledTime;}
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
namespace UBOAT.Game.Scene.Items {public class TorpedoLauncherGroup {public object TorpedoStorage=new object();}}
namespace UBOAT.Game.Scene.Camera {public class Generic {public object Character=new object();public object ExternalController=new object();}public class MainCamera {public static MainCamera Instance=new MainCamera();public Generic GenericController=new Generic();}}
namespace UBOAT.Game.UI.Storage {public enum StorageUIMode {Auto}public class StorageUI {public bool IsOpened;public int Opens;public object Character,Storage;public Action BeforeOpen;public void Open(object c,object s,StorageUIMode mode,object a,object b,object d,bool e){if(BeforeOpen!=null)BeforeOpen();Character=c;Storage=s;Opens++;IsOpened=true;}}}
namespace Landomatic.TorpedoRoom {
 internal static partial class NativeSkin {internal static void RestoreStations(){RoomExit(null);}}
 internal static class NativePresentation {public static bool Hidden=true;internal static void Room(bool v){Hidden=v;}}
 internal static class UiScale {internal static UnityEngine.Vector3 Between(UnityEngine.Transform a,UnityEngine.Transform b){return new UnityEngine.Vector3(2,2,1);}internal static void SetScale(UnityEngine.RectTransform r,UnityEngine.Vector3 v){r.localScale=v;}}
 internal static class Config {public static bool Interface=true;}
 internal static class Bridge {public static bool IsRemote;}
 public sealed partial class Runtime {public static UBOAT.Game.UI.Storage.StorageUI Storage;public static T Get<T>() where T:class{return Storage as T;}public static UBOAT.Game.Scene.Items.TorpedoLauncherGroup CurrentGroup;public static bool Loading;public static bool Active=true;internal bool roomTakeover=true;internal UnityEngine.GameObject gridRoot=new UnityEngine.GameObject("room canvas");internal int Closed;void CloseGrid(){Closed++;}internal void TestOpen(){OpenOrdnance();}internal bool Dismissed{get{return roomHandoff.Dismissed;}}public static Runtime Instance=new Runtime();public UnityEngine.Transform transform=new UnityEngine.GameObject("runtime").transform;public static void Say(string s){}}
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
  var room=new Landomatic.TorpedoRoom.Runtime();var inventory=new UBOAT.Game.UI.Storage.StorageUI();Landomatic.TorpedoRoom.Runtime.Storage=inventory;Landomatic.TorpedoRoom.Runtime.CurrentGroup=new UBOAT.Game.Scene.Items.TorpedoLauncherGroup();
  inventory.BeforeOpen=()=>{Check(room.Closed==1,"Prep/input close before ordnance open");Check(!room.gridRoot.activeSelf,"room overlay gone before ordnance open");Check(Landomatic.TorpedoRoom.NativePresentation.Hidden,"native torpedo panel stays masked during inventory handoff");};
  room.TestOpen();Check(inventory.Opens==1&&inventory.IsOpened,"native inventory opens");Check(room.Dismissed&&!room.roomTakeover,"handoff latches full room dismissal");Check(inventory.Storage==Landomatic.TorpedoRoom.Runtime.CurrentGroup.TorpedoStorage,"current compartment storage passed");Check(inventory.Character==UBOAT.Game.Scene.Camera.MainCamera.Instance.GenericController.Character,"current native character passed");
  inventory.IsOpened=false;Check(room.Dismissed&&!room.gridRoot.activeSelf,"inventory close does not reopen room");
  Landomatic.TorpedoRoom.Runtime.Loading=true;room.TestOpen();Check(inventory.Opens==1,"loading blocks new ordnance request");Landomatic.TorpedoRoom.Runtime.Loading=false;
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
