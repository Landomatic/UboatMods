// Independent hierarchy/geometry fixture. Not installed as mod source.
using System;
using System.Collections.Generic;
using Landomatic.TorpedoRoom;
namespace UnityEngine {
 public class Object {public static void Destroy(GameObject go){go.transform.SetParent(null,false);go.SetActive(false);}}
 public class Component:Object {public GameObject gameObject;public Transform transform{get{return gameObject.transform;}}public string name{get{return gameObject.name;}}public T[] GetComponentsInChildren<T>(bool all) where T:Component{var values=new List<T>();foreach(var go in GameObject.All)if(go.transform.IsChildOf(transform)){var value=go.GetComponent<T>();if(value!=null)values.Add(value);}return values.ToArray();}public T GetComponent<T>() where T:Component{return gameObject.GetComponent<T>();}public T GetComponentInParent<T>() where T:Component{for(var p=transform;p!=null;p=p.parent){var t=p.GetComponent<T>();if(t!=null)return t;}return null;}}
 public class MonoBehaviour:Component {public bool enabled=true;}
 public struct Scene {public bool IsValid(){return true;}}
 public class GameObject:Object {
  public static List<GameObject> All=new List<GameObject>();public string name;public RectTransform transform;public Scene scene;public bool activeSelf=true;public bool activeInHierarchy{get{return activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);}}
  readonly List<Component> components=new List<Component>();
  public GameObject(string n,params Type[] types){name=n;transform=new RectTransform{gameObject=this};components.Add(transform);All.Add(this);foreach(var t in types)if(t!=typeof(RectTransform))AddComponent(t);}
  public void SetActive(bool value){activeSelf=value;}
  Component AddComponent(Type t){var c=(Component)Activator.CreateInstance(t);c.gameObject=this;components.Add(c);return c;}
  public T AddComponent<T>() where T:Component,new(){return (T)AddComponent(typeof(T));}
  public T GetComponent<T>() where T:Component{foreach(var c in components)if(c is T)return (T)c;return null;}
 }
 public class Transform:Component {
  public Transform parent;public Vector3 localScale=Vector3.one;public Quaternion localRotation;readonly List<Transform> children=new List<Transform>();
  public int childCount{get{return children.Count;}}public Transform GetChild(int n){return children[n];}
  public void SetParent(Transform value,bool world){if(parent!=null)parent.children.Remove(this);parent=value;if(parent!=null)parent.children.Add(this);}
  public int GetSiblingIndex(){return parent==null?0:parent.children.IndexOf(this);}public void SetSiblingIndex(int n){if(parent==null)return;parent.children.Remove(this);parent.children.Insert(Math.Min(n,parent.children.Count),this);}
  public bool IsChildOf(Transform t){for(var p=this;p!=null;p=p.parent)if(p==t)return true;return false;}
  public Vector3 TransformPoint(Vector3 p){var r=this as RectTransform;var offset=r==null?Vector2.zero:r.anchoredPosition;var q=new Vector3(p.x*localScale.x+offset.x,p.y*localScale.y+offset.y,p.z);var canvas=GetComponent<Canvas>();if(canvas!=null){q.x*=canvas.scaleFactor;q.y*=canvas.scaleFactor;}return parent==null?q:parent.TransformPoint(q);}
  public Vector3 InverseTransformPoint(Vector3 p){if(parent!=null)p=parent.InverseTransformPoint(p);var canvas=GetComponent<Canvas>();if(canvas!=null){p.x/=canvas.scaleFactor;p.y/=canvas.scaleFactor;}var r=this as RectTransform;var offset=r==null?Vector2.zero:r.anchoredPosition;return new Vector3((p.x-offset.x)/localScale.x,(p.y-offset.y)/localScale.y,p.z);}
 }
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot=new Vector2(.5f,.5f),anchoredPosition,sizeDelta=new Vector2(100,100);public Rect rect{get{return new Rect{width=sizeDelta.x,height=sizeDelta.y};}}public void GetWorldCorners(Vector3[] c){float x=-pivot.x*sizeDelta.x,y=-pivot.y*sizeDelta.y;c[0]=TransformPoint(new Vector3(x,y,0));c[1]=TransformPoint(new Vector3(x,y+sizeDelta.y,0));c[2]=TransformPoint(new Vector3(x+sizeDelta.x,y+sizeDelta.y,0));c[3]=TransformPoint(new Vector3(x+sizeDelta.x,y,0));}}
 public struct Rect {public float width,height;public Vector2 size{get{return new Vector2(width,height);}}}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero{get{return new Vector2(0,0);}}public static float Distance(Vector2 a,Vector2 b){return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one{get{return new Vector3(1,1,1);}}public static Vector3 zero{get{return new Vector3();}}public static Vector3 Min(Vector3 a,Vector3 b){return new Vector3(Math.Min(a.x,b.x),Math.Min(a.y,b.y),Math.Min(a.z,b.z));}public static Vector3 Max(Vector3 a,Vector3 b){return new Vector3(Math.Max(a.x,b.x),Math.Max(a.y,b.y),Math.Max(a.z,b.z));}}
 public struct Quaternion {public static Quaternion identity;}
 public struct Color {public static Color clear;}
 public class Sprite {}
 public enum RenderMode {ScreenSpaceOverlay,ScreenSpaceCamera,WorldSpace}
 public class Camera {}
 public class Canvas:Component {float scale=1,pixels=100;bool perfect;public int Writes;public float scaleFactor{get{return scale;}set{scale=value;Writes++;}}public float referencePixelsPerUnit{get{return pixels;}set{pixels=value;Writes++;}}public bool pixelPerfect{get{return perfect;}set{perfect=value;Writes++;}}public RenderMode renderMode;public Camera worldCamera;}
 public static class RectTransformUtility {public static Vector2 WorldToScreenPoint(Camera camera,Vector3 world){return new Vector2(world.x,world.y);}}
 public static class Mathf {public static float Min(float a,float b){return Math.Min(a,b);}public static float Max(float a,float b){return Math.Max(a,b);}public static float Sqrt(float v){return (float)Math.Sqrt(v);}}
 public static class Time {public static float unscaledTime;}
 public static class Debug {public static void LogWarning(object value){Console.WriteLine(value);}}
 public static class Resources {public static T[] FindObjectsOfTypeAll<T>() where T:Component{var all=new List<T>();foreach(var go in GameObject.All){var c=go.GetComponent<T>();if(c!=null)all.Add(c);}return all.ToArray();}}
}
namespace UnityEngine.UI {
 public class CanvasScaler:UnityEngine.Component {public enum ScaleMode {ConstantPixelSize,ScaleWithScreenSize}public ScaleMode uiScaleMode=ScaleMode.ScaleWithScreenSize;public float scaleFactor=1,referencePixelsPerUnit=100;}
 public class Graphic:UnityEngine.Component {public bool raycastTarget;public UnityEngine.RectTransform rectTransform{get{return (UnityEngine.RectTransform)transform;}}}
 public class Image:Graphic {public UnityEngine.Sprite sprite;public object material;public UnityEngine.Color color;public int type,fillMethod,fillOrigin;public bool fillClockwise,preserveAspect,enabled=true;public float fillAmount;}
 public class Selectable:UnityEngine.MonoBehaviour {public enum Transition{None,ColorTint}public Transition transition;public Graphic targetGraphic;public bool interactable=true;}
 public class Button:Selectable {public Event onClick=new Event();}
 public class Event {Action listeners;public void AddListener(Action a){listeners+=a;}public void Invoke(){if(listeners!=null)listeners();}}
}
namespace TMPro {
 public class TextMeshProUGUI:UnityEngine.UI.Graphic {public object font,fontSharedMaterial,spriteAsset;public int fontStyle,alignment;public float fontSize,fontSizeMin,fontSizeMax;public UnityEngine.Color color;public object margin;public string text;public bool richText,enableAutoSizing;}
 public class TMP_InputField:UnityEngine.UI.Selectable {}
}
namespace HarmonyLib {public static class AccessTools {public static System.Reflection.FieldInfo Field(Type t,string n){return t.GetField(n,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);}}}
namespace UBOAT.Game.UI {
 public class GameUI {public UnityEngine.Canvas mainCanvas;public UBOAT.Game.UI.Periscope.TorpedoLaunchersUI CurrentTorpedoLaunchersUI;}
 public class TorpedoAttackStationsViewSwitchUI:UnityEngine.MonoBehaviour {}
 public class TorpedoLaunchersManualUI:UnityEngine.MonoBehaviour {}
 public class TorpedoLaunchersDepthDialUI:UnityEngine.MonoBehaviour {public TMPro.TextMeshProUGUI label;public UnityEngine.Transform pointer;public UnityEngine.GameObject blockade;}
 public class TorpedoLauncherUI:UnityEngine.MonoBehaviour {public object Launcher;public UnityEngine.UI.Image torpedoImage,coverOpenImage,coverCloseImage,floodingProgress,blockadeImage;public UnityEngine.UI.Button pistolTypeButton;}
}
namespace UBOAT.Game.UI.Periscope {public class TorpedoLaunchersUI:UnityEngine.MonoBehaviour {public TMPro.TMP_InputField programDistance;}}
namespace Landomatic.TorpedoRoom {
 internal class PlayerShip {public object[] TorpedoLaunchers;}
 internal static class Runtime {public static PlayerShip Ship;public static UBOAT.Game.UI.GameUI Ui;public static T Get<T>() where T:class{return Ui as T;}}
 internal static class NativePresentation {public static UBOAT.Game.UI.TorpedoLauncherUI TubeView(int i){return null;}}
}
public static class NativeSkinChecks {
 static int count;static void Check(bool b,string name){count++;if(!b)throw new Exception(name);}
 static UnityEngine.GameObject Node(string n,UnityEngine.Transform p=null,float w=100,float h=100){var go=new UnityEngine.GameObject(n);go.transform.sizeDelta=new UnityEngine.Vector2(w,h);go.transform.SetParent(p,false);return go;}
 static UnityEngine.UI.Image Art(string n,UnityEngine.Transform p){var image=Node(n,p).AddComponent<UnityEngine.UI.Image>();image.sprite=new UnityEngine.Sprite();return image;}
 static int Components<T>(UnityEngine.Transform root) where T:UnityEngine.Component {int n=root.GetComponent<T>()==null?0:1;for(int i=0;i<root.childCount;i++)n+=Components<T>(root.GetChild(i));return n;}
 static void BoundsFit(NativeSkin.Visual v,UnityEngine.Transform parent,float w,float h){var corners=new UnityEngine.Vector3[4];foreach(var image in v.Copies){image.rectTransform.GetWorldCorners(corners);foreach(var corner in corners){var p=parent.InverseTransformPoint(corner);if(Math.Abs(p.x)>w/2+.01||Math.Abs(p.y)>h/2+.01)throw new Exception("art exceeds reserved frame");}}count++;}
 public static int Run(){
  var native=Node("native root",null,80,80);native.AddComponent<UnityEngine.UI.Image>();var tube=native.AddComponent<UBOAT.Game.UI.TorpedoLauncherUI>();
  tube.torpedoImage=Art("torpedo",native.transform);tube.coverOpenImage=Art("open",native.transform);tube.coverCloseImage=Art("closed",native.transform);
  tube.coverOpenImage.rectTransform.sizeDelta=new UnityEngine.Vector2(170,150);tube.coverOpenImage.rectTransform.anchoredPosition=new UnityEngine.Vector2(-25,-20);
  tube.floodingProgress=Art("mask-dependent blue flood",native.transform);tube.blockadeImage=Art("blockade",native.transform);
  var status=Art("stale native status background",native.transform);Node("status label",status.transform).AddComponent<TMPro.TextMeshProUGUI>().text="Angle out of range!";
  Node("native tube number",native.transform).AddComponent<TMPro.TextMeshProUGUI>().text="V";
  var pistol=Art("pistol plate",native.transform);tube.pistolTypeButton=pistol.gameObject.AddComponent<UnityEngine.UI.Button>();
  Node("pistol text",pistol.transform).AddComponent<TMPro.TextMeshProUGUI>().text="MZ";
  var target=Node("card frame",null,136,86);var v=NativeSkin.Tube(tube,target.transform,136,86);
  Check(v!=null&&v.Sources.Count==3,"only the three native tube art images are borrowed");
  Check(Components<TMPro.TextMeshProUGUI>(target.transform)==0,"native number/pistol/status labels do not duplicate room text");
  Check(!v.Sources.Contains(status)&&!v.Sources.Contains(tube.floodingProgress),"status backgrounds and unmasked flood layers never enter room graphic");
  Check(Components<UnityEngine.UI.Image>(target.transform)==3&&Components<UnityEngine.UI.Button>(target.transform)==0,"no extra graphics or native controls copied");
  BoundsFit(v,target.transform,136,86);
  Check(v.Icon.sprite==tube.torpedoImage.sprite&&v.OpenCover!=null&&v.ClosedCover!=null,"native torpedo and both door sprites retained");
  var update=new UnityEngine.Sprite();tube.torpedoImage.sprite=update;tube.coverOpenImage.gameObject.SetActive(false);v.Sync();
  Check(v.Icon.sprite==update&&v.OpenCover.gameObject.activeSelf,"live artwork refresh retains room-owned node visibility");BoundsFit(v,target.transform,136,86);
  Check(NativeSkin.Tube(null,target.transform,136,86)==null,"absent native art can use fallback");
  // Live native station controller is moved, never cloned or granted permissions.
  var host=Node("native host");var before=Node("before",host.transform);var barGo=Node("station bar",host.transform,42,300);var bar=barGo.AddComponent<UBOAT.Game.UI.TorpedoAttackStationsViewSwitchUI>();Node("after",host.transform);
  barGo.transform.anchorMin=new UnityEngine.Vector2(.2f,.3f);barGo.transform.anchorMax=new UnityEngine.Vector2(.4f,.8f);barGo.transform.pivot=new UnityEngine.Vector2(.1f,.7f);barGo.transform.anchoredPosition=new UnityEngine.Vector2(17,-22);barGo.transform.sizeDelta=new UnityEngine.Vector2(42,300);barGo.transform.localScale=new UnityEngine.Vector3(.7f,.8f,1);
  var button=Node("native TDC toggle",barGo.transform).AddComponent<UnityEngine.UI.Button>();button.interactable=false;int clicks=0;button.onClick.AddListener(()=>clicks++);
  var overlay=Node("room canvas");NativeSkin.RoomStations(overlay.transform);
  Check(barGo.transform.parent.parent==overlay.transform&&barGo.GetComponent<UBOAT.Game.UI.TorpedoAttackStationsViewSwitchUI>()==bar,"original station controller retained in room");
  Check(((UnityEngine.RectTransform)barGo.transform.parent).anchorMin.x==0&&((UnityEngine.RectTransform)barGo.transform.parent).anchorMin.y==.5f&&((UnityEngine.RectTransform)barGo.transform.parent).anchoredPosition.x==16,"navigation stays at left edge outside tube editor");
  Check(!button.interactable&&barGo.activeSelf,"native disabled state and listeners are not overridden");button.onClick.Invoke();Check(clicks==1,"existing native navigation action is retained");
  NativeSkin.RoomStations(overlay.transform);NativeSkin.RestoreStations();
  Check(barGo.transform.parent==host.transform&&barGo.transform.GetSiblingIndex()==1,"station exit restores original native parent and sibling");
  Check(barGo.transform.anchorMin.x==.2f&&barGo.transform.anchorMax.y==.8f&&barGo.transform.pivot.x==.1f&&barGo.transform.anchoredPosition.y==-22&&barGo.transform.localScale.x==.7f&&barGo.transform.sizeDelta.y==300,"original station layout restored exactly");
  barGo.SetActive(false);NativeSkin.RoomStations(overlay.transform);Check(barGo.transform.parent==host.transform&&!barGo.activeSelf,"hidden station bar is not force-enabled");
  barGo.SetActive(true);bar.enabled=false;UnityEngine.Time.unscaledTime=2;NativeSkin.RoomStations(overlay.transform);Check(barGo.transform.parent==host.transform&&!bar.enabled,"disabled station controller is not force-enabled");
  bar.enabled=true;UnityEngine.Time.unscaledTime=4;NativeSkin.RoomStations(overlay.transform);NativeSkin.RoomStations(null);Check(barGo.transform.parent==host.transform,"null/disabled room target restores native navigation");
  // Actual native canvas scale is copied without another resolution multiplier.
  var nativeRoot=Node("native canvas");var nativeCanvas=nativeRoot.AddComponent<UnityEngine.Canvas>();nativeCanvas.scaleFactor=.75f;nativeCanvas.referencePixelsPerUnit=120;nativeCanvas.pixelPerfect=true;
  Runtime.Ui=new UBOAT.Game.UI.GameUI{mainCanvas=nativeCanvas};
  var roomRoot=Node("room canvas scaled");var roomCanvas=roomRoot.AddComponent<UnityEngine.Canvas>();var scaler=roomRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
  Check(UiScale.Match(roomCanvas,scaler)&&scaler.uiScaleMode==UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize&&roomCanvas.scaleFactor==.75f&&scaler.scaleFactor==.75f,"room canvas uses actual native pixel scale, without independent screen-resolution multiplication");
  Check(roomCanvas.referencePixelsPerUnit==120&&scaler.referencePixelsPerUnit==120&&roomCanvas.pixelPerfect,"native pixel density/alignment settings are retained");
  int writes=roomCanvas.Writes;UiScale.Match(roomCanvas,scaler);Check(roomCanvas.Writes==writes,"unchanged UI polling performs no canvas writes/rebuild requests");
  nativeCanvas.scaleFactor=1.25f;UiScale.Match(roomCanvas,scaler);Check(roomCanvas.scaleFactor==1.25f&&scaler.scaleFactor==1.25f,"live native UI-scale change reaches same existing canvas");
  nativeCanvas.scaleFactor=float.NaN;Check(!UiScale.Match(roomCanvas,scaler)&&roomCanvas.scaleFactor==1.25f,"invalid native scale leaves last valid layout intact");nativeCanvas.scaleFactor=.75f;UiScale.Match(roomCanvas,scaler);
  Check(!UiScale.Match(null,scaler)&&!UiScale.Match(roomCanvas,null),"unbuilt canvas is safe");
  var author=Node("native dial parent",nativeRoot.transform);author.transform.localScale=new UnityEngine.Vector3(.6f,.8f,1);
  var nativeDepth=author.AddComponent<UBOAT.Game.UI.TorpedoLaunchersDepthDialUI>();var knob=Art("small authored knob",author.transform);knob.rectTransform.sizeDelta=new UnityEngine.Vector2(15,30);knob.rectTransform.pivot=new UnityEngine.Vector2(.5f,.25f);knob.transform.localScale=new UnityEngine.Vector3(.75f,1.25f,1);nativeDepth.pointer=knob.transform;
  typeof(NativeSkin).GetField("depth",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,nativeDepth);
  var hit=Node("dial hit square",roomRoot.transform,100,100);var dial=NativeSkin.Dial(hit.transform,100);
  Check(dial!=null&&Math.Abs(dial.Root.localScale.x-.89004f)<.001&&Math.Abs(dial.Root.localScale.y-1.97787f)<.001,"rotating knob itself enlarges into the reserved hub-centred area with native proportions");
  Check(dial.Root.pivot.y==.25f&&dial.Root.anchoredPosition.x==0&&dial.Root.anchoredPosition.y==0,"native knob hub remains centred with authored pivot");
  BoundsFit(dial,hit.transform,100,100);
  nativeCanvas.scaleFactor=1.5f;UiScale.Match(roomCanvas,scaler);NativeSkin.SizeDial(dial);
  Check(Math.Abs(dial.Root.localScale.x-.89004f)<.001&&Math.Abs(dial.Root.localScale.y-1.97787f)<.001,"matching live canvas scaling keeps normalized knob size consistent");
  author.transform.localScale=new UnityEngine.Vector3(5,5,1);NativeSkin.SizeDial(dial);
  Check(dial.Root.localScale.x*15<=100.01&&dial.Root.localScale.y*30<=100.01,"oversized native knob shrinks to reserved hit square without stretching");
  // Navigation bridge retains both authored bar scale and native ancestor scale.
  host.transform.SetParent(nativeRoot.transform,false);host.transform.localScale=new UnityEngine.Vector3(.6f,.8f,1);UnityEngine.Time.unscaledTime=6;
  NativeSkin.RoomStations(roomRoot.transform);var bridge=barGo.transform.parent;
  Check(Math.Abs(bridge.localScale.x-.6f)<.001&&Math.Abs(bridge.localScale.y-.8f)<.001&&barGo.transform.localScale.x==.7f,"station icons keep native ancestor and authored local scale");
  nativeCanvas.scaleFactor=.8f;UiScale.Match(roomCanvas,scaler);NativeSkin.RoomStations(roomRoot.transform);
  Check(Math.Abs(bridge.localScale.x-.6f)<.001&&Math.Abs(bridge.localScale.y-.8f)<.001,"station bridge follows UI scale without a doubled zoom factor");
  host.transform.localScale=new UnityEngine.Vector3(.4f,.5f,1);barGo.transform.localScale=new UnityEngine.Vector3(.9f,.9f,1);NativeSkin.RoomStations(roomRoot.transform);
  Check(Math.Abs(bridge.localScale.x-.4f)<.001&&barGo.transform.localScale.x==.9f,"live native parent and own scale changes are preserved");
  NativeSkin.RestoreStations();Check(barGo.transform.parent==host.transform&&barGo.transform.localScale.x==.9f,"restoration does not undo native live scale changes");
  Runtime.Ui=null;Check(!UiScale.Match(roomCanvas,scaler)&&roomCanvas.scaleFactor==.8f,"missing native UI keeps prior valid canvas scale");
  var fullPanel=Node("full launch panel").AddComponent<UBOAT.Game.UI.Periscope.TorpedoLaunchersUI>();var fullDepth=Node("full launch depth",fullPanel.transform).AddComponent<UBOAT.Game.UI.TorpedoLaunchersDepthDialUI>();fullDepth.gameObject.SetActive(false);
  Runtime.Ui=new UBOAT.Game.UI.GameUI{mainCanvas=nativeCanvas,CurrentTorpedoLaunchersUI=fullPanel};Runtime.Ship=new PlayerShip{TorpedoLaunchers=new object[0]};NativeSkin.Capture();
  Check(ReferenceEquals(typeof(NativeSkin).GetField("depth",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null),fullDepth),"native knob template prefers full launch interface over smaller manual station widget");
  Console.WriteLine("PASS: "+count+" native skin hierarchy assertions (actual NativeSkin/NativeClick; independent UI geometry fixture).");return count;
 }
}
