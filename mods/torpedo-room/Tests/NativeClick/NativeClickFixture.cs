// Independently authored regression fixture; never installed as mod source.
using System;
using System.Collections.Generic;
using Landomatic.TorpedoRoom;
namespace UnityEngine {
 public class Component {public GameObject gameObject;public T GetComponent<T>() where T:Component{return gameObject.GetComponent<T>();}}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero{get{return new Vector2(0,0);}}}
 public class RectTransform:Component {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition;}
 public struct Color {public static Color clear;}
 public class GameObject {
  readonly List<Component> items=new List<Component>();public int RejectedGraphics;
  public T GetComponent<T>() where T:Component {foreach(var item in items)if(item is T)return (T)item;return null;}
  public T AddComponent<T>() where T:Component,new(){
   if(typeof(UnityEngine.UI.Graphic).IsAssignableFrom(typeof(T))&&GetComponent<UnityEngine.UI.Graphic>()!=null){RejectedGraphics++;return null;}
   var item=new T{gameObject=this};items.Add(item);return item;
  }
 }
}
namespace UnityEngine.UI {
 public class Graphic:UnityEngine.Component {public bool raycastTarget;}
 public class Image:Graphic {public UnityEngine.Color color;}
 public class Selectable:UnityEngine.Component {public enum Transition {None,ColorTint}public Transition transition=Transition.ColorTint;}
 public class Button:Selectable {public Graphic targetGraphic;public Event onClick=new Event();}
 public class Event {Action actions;public void AddListener(Action action){actions+=action;}public void Invoke(){if(actions!=null)actions();}}
}
namespace TMPro {public class TextMeshProUGUI:UnityEngine.UI.Graphic {public string text="3.2 m";}}
public static class NativeClickChecks {
 static int count;static void Check(bool condition,string name){count++;if(!condition)throw new Exception(name);}
 public static int Run(){
  var go=new UnityEngine.GameObject();var rect=go.AddComponent<UnityEngine.RectTransform>();var text=go.AddComponent<TMPro.TextMeshProUGUI>();int clicks=0;
  NativeSkin.ClickArea(rect,()=>clicks++);var button=go.GetComponent<UnityEngine.UI.Button>();
  Check(go.RejectedGraphics==0,"TMP readout never receives a second Graphic");
  Check(button!=null&&ReferenceEquals(button.targetGraphic,text),"existing text is the native readout click target");
  Check(text.raycastTarget&&text.text=="3.2 m","readout remains visible and accepts pointer hits");
  Check(button.transition==UnityEngine.UI.Selectable.Transition.None,"button does not recolor native readout text");
  button.onClick.Invoke();Check(clicks==1,"readout invokes its configured editing action");
  var empty=new UnityEngine.GameObject();NativeSkin.ClickArea(empty.AddComponent<UnityEngine.RectTransform>(),null);
  Check(empty.GetComponent<UnityEngine.UI.Image>()!=null&&empty.RejectedGraphics==0,"empty hit area receives one transparent graphic");
  empty.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();NativeSkin.ClickArea(null,null);Check(true,"null action and absent rectangle are safe");
  var source=new UnityEngine.GameObject().AddComponent<UnityEngine.RectTransform>();source.pivot=new UnityEngine.Vector2(.5f,.25f);
  NativeSkin.AlignRoot(rect,source,true);Check(rect.pivot.x==.5f&&rect.pivot.y==.25f,"rotating art retains authored knob-hub pivot");
  Check(rect.anchorMin.x==.5f&&rect.anchorMin.y==.5f&&rect.anchoredPosition.x==0&&rect.anchoredPosition.y==0,"knob hub is anchored to hit-area centre");
  source.pivot=new UnityEngine.Vector2(.2f,.8f);NativeSkin.AlignRoot(rect,source,true);Check(rect.pivot.x==.2f&&rect.pivot.y==.8f,"off-centre native pivot survives fitting");
  NativeSkin.AlignRoot(rect,source,false);Check(rect.pivot.x==.5f&&rect.pivot.y==.5f,"nonrotating tube frames retain centred layout");
  Console.WriteLine("PASS: "+count+" native click target assertions (actual NativeClick.cs; single-Graphic API fixture).");return count;
 }
}
