// Actual PreparationDial against independent UI event fixtures. Not installed.
using System;
using Landomatic.TorpedoRoom;
namespace UBOAT.Game.Core.Serialization {public class NonSerializedInGameState:Attribute {}}
namespace UnityEngine {
 public class MonoBehaviour {public Transform transform=new RectTransform();}
 public class Transform {public Quaternion localRotation;}
 public struct Rect {public Vector2 center;}
 public class RectTransform:Transform {public Rect rect=new Rect{center=new Vector2(50,-50)};}
 public struct Quaternion {public float Z;public static Quaternion Euler(float x,float y,float z){return new Quaternion{Z=z};}}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 operator -(Vector2 a,Vector2 b){return new Vector2(a.x-b.x,a.y-b.y);}public float sqrMagnitude{get{return x*x+y*y;}}}
 public static class RectTransformUtility {public static bool Valid=true;public static float Scale=1;public static Vector2 Origin;public static bool ScreenPointToLocalPointInRectangle(RectTransform t,Vector2 p,object c,out Vector2 v){v=new Vector2((p.x-Origin.x)/Scale,(p.y-Origin.y)/Scale);return Valid;}}
 public static class Mathf {public const float Rad2Deg=57.29578f;public static float Atan2(float x,float y){return (float)Math.Atan2(x,y);}public static float InverseLerp(float lo,float hi,float v){return Math.Max(0,Math.Min(1,(v-lo)/(hi-lo)));}}
}
namespace UnityEngine.UI {public class InputField {public string text;}}
namespace UnityEngine.EventSystems {
 public interface IPointerDownHandler {void OnPointerDown(PointerEventData e);}public interface IDragHandler {void OnDrag(PointerEventData e);}public interface IScrollHandler {void OnScroll(PointerEventData e);}
 public class PointerEventData {public enum InputButton {Left,Right,Middle}public InputButton button;public UnityEngine.Vector2 position,scrollDelta;public object pressEventCamera;}
}
public static class DialChecks {
 static int count;static void Check(bool ok,string name){count++;if(!ok)throw new Exception("FAILED: "+name);}
 static UnityEngine.Vector2 At(PreparationDial dial,float x,float y){var c=((UnityEngine.RectTransform)dial.transform).rect.center;var scale=UnityEngine.RectTransformUtility.Scale;var o=UnityEngine.RectTransformUtility.Origin;return new UnityEngine.Vector2(o.x+(c.x+x)*scale,o.y+(c.y+y)*scale);}
 public static int Run(){
  bool allowed=true;var input=new UnityEngine.UI.InputField{text="3.0"};var dial=new PreparationDial{Input=input,Minimum=.5f,Maximum=14,Step=.25f,CanEdit=()=>allowed,Needle=new UnityEngine.Transform()};
  var e=new UnityEngine.EventSystems.PointerEventData{scrollDelta=new UnityEngine.Vector2(0,1)};
  dial.OnScroll(e);Check(input.text=="3.25","scroll writes a quarter-metre value into the same text field");
  e.scrollDelta=new UnityEngine.Vector2(0,-1);dial.OnScroll(e);Check(input.text=="3","reverse scroll restores depth");
  allowed=false;dial.OnScroll(e);Check(input.text=="3","read-only guard blocks scrolling");allowed=true;
  e.position=At(dial,50,0);dial.OnDrag(e);Check(input.text=="14","drag right sets upper endpoint");
  e.position=At(dial,-50,0);dial.OnPointerDown(e);Check(input.text=="0.5","click left sets lower endpoint");
  e.position=At(dial,0,50);dial.OnDrag(e);Check(input.text=="7.25","top position maps to rounded midpoint");

  e.position=At(dial,-25,50);dial.OnDrag(e);Check(input.text=="5.25","top-left-pivot dial selects an intermediate lower-quarter depth");
  e.position=At(dial,25,50);dial.OnDrag(e);Check(input.text=="9.25","top-left-pivot dial selects an intermediate upper-quarter depth");
  UnityEngine.RectTransformUtility.Scale=.6375f;UnityEngine.RectTransformUtility.Origin=new UnityEngine.Vector2(430,215);
  e.position=At(dial,-25,50);dial.OnPointerDown(e);Check(input.text=="5.25","translated scaled overlay preserves intermediate dial value");
  e.position=At(dial,0,50);dial.OnDrag(e);Check(input.text=="7.25","drag through top centre stays intermediate on scaled overlay");
  ((UnityEngine.RectTransform)dial.transform).rect=new UnityEngine.Rect{center=new UnityEngine.Vector2(0,0)};
  e.position=At(dial,25,50);dial.OnDrag(e);Check(input.text=="9.25","center-pivot dial maps identically to top-left-pivot dial");
  ((UnityEngine.RectTransform)dial.transform).rect=new UnityEngine.Rect{center=new UnityEngine.Vector2(50,-50)};
  UnityEngine.RectTransformUtility.Scale=1;UnityEngine.RectTransformUtility.Origin=new UnityEngine.Vector2(0,0);
  e.position=At(dial,0,50);dial.OnDrag(e);

  e.position=At(dial,0,0);dial.OnDrag(e);Check(input.text=="7.25","centre dead zone prevents jumps");
  e.button=UnityEngine.EventSystems.PointerEventData.InputButton.Right;e.position=At(dial,50,0);dial.OnPointerDown(e);dial.OnDrag(e);Check(input.text=="7.25","right-click does not change the dial");
  e.button=UnityEngine.EventSystems.PointerEventData.InputButton.Left;UnityEngine.RectTransformUtility.Valid=false;dial.OnDrag(e);Check(input.text=="7.25","failed coordinate conversion cannot edit");UnityEngine.RectTransformUtility.Valid=true;
  input.text="14.0";e.scrollDelta=new UnityEngine.Vector2(0,1);dial.OnScroll(e);Check(input.text=="14","scroll clamps to maximum");
  input.text="bad";dial.OnScroll(e);Check(input.text=="bad","invalid text is preserved for explicit correction");
  input.text="0.5";dial.Refresh();Check(dial.Needle.localRotation.Z==90,"typed minimum moves needle");input.text="14.0";dial.Refresh();Check(dial.Needle.localRotation.Z==-90,"typed maximum moves needle");
  dial.Minimum=500;dial.Maximum=15000;dial.Step=100;input.text="1500";dial.OnScroll(e);Check(input.text=="1600","run dial uses metre increments");
  dial.Minimum=0;dial.Maximum=359.9f;dial.Step=.1f;input.text="90.0";dial.OnScroll(e);Check(input.text=="90.1","heading dial accepts tenths");
  dial.Minimum=500;dial.Maximum=15000;dial.Step=100;e.position=At(dial,-35,35);dial.OnDrag(e);Check(input.text=="4100","run dial chooses intermediate run instead of maximum");
  dial.Minimum=0;dial.Maximum=359.9f;dial.Step=.1f;e.position=At(dial,0,50);dial.OnDrag(e);Check(input.text=="180","heading dial chooses intermediate bearing instead of endpoint");
  dial.Minimum=.5f;dial.Maximum=14;dial.Step=.25f;input.text="3.2";e.scrollDelta=new UnityEngine.Vector2(0,1);dial.OnScroll(e);Check(input.text=="3.5","legacy finer depth scroll snaps to selectable quarter metres");
  Check(ReportText.Tube(false,false,false,false,0,0,3.25f)=="3.25 m","quarter-metre report retains both decimal places");
  dial.Choices=new[]{30f,40f,44f};dial.Minimum=30;dial.Maximum=44;dial.Step=.1f;input.text="30";e.scrollDelta=new UnityEngine.Vector2(0,1);dial.OnScroll(e);Check(input.text=="40","speed wheel advances to next supported preset rather than an unsupported increment");
  dial.OnScroll(e);Check(input.text=="44","speed wheel reaches highest supported preset");dial.OnScroll(e);Check(input.text=="44","speed wheel stays bounded");
  dial.Choices=new[]{30f};dial.Minimum=dial.Maximum=30;input.text="30";dial.OnScroll(e);Check(input.text=="30","fixed-speed weapon keeps a visible fixed selector value");
  // Compass geometry, including north seam and the same native speed labels
  // on click/drag, scroll and preset-button entry points.
  dial.Choices=null;dial.Compass=true;dial.Minimum=0;dial.Maximum=359.9f;dial.Step=.1f;
  float[] x={0,50,0,-50,35,-35},y={50,0,-50,0,35,-35},bearing={0,90,180,270,45,225};
  for(int n=0;n<x.Length;n++){
   e.position=At(dial,x[n],y[n]);dial.OnDrag(e);Check(float.Parse(input.text,System.Globalization.CultureInfo.InvariantCulture)==bearing[n],"compass bearing at direction "+n);
   dial.Refresh();Check(Math.Abs(dial.Needle.localRotation.Z+bearing[n])<.01,"compass needle agrees with selected bearing "+n);
  }
  input.text="359.9";e.scrollDelta=new UnityEngine.Vector2(0,1);dial.OnScroll(e);Check(input.text=="0","heading wheel wraps through north clockwise");
  e.scrollDelta=new UnityEngine.Vector2(0,-1);dial.OnScroll(e);Check(input.text=="359.9","heading wheel wraps through north anticlockwise");
  input.text="360";dial.Refresh();Check(dial.Needle.localRotation.Z==0,"typed 360 points north");
  UnityEngine.RectTransformUtility.Scale=.6375f;UnityEngine.RectTransformUtility.Origin=new UnityEngine.Vector2(430,215);
  e.position=At(dial,-50,0);dial.OnPointerDown(e);Check(input.text=="270","scaled translated compass maps west correctly");
  allowed=false;e.position=At(dial,50,0);dial.OnDrag(e);Check(input.text=="270","compass respects read-only guard");allowed=true;
  UnityEngine.RectTransformUtility.Scale=1;UnityEngine.RectTransformUtility.Origin=new UnityEngine.Vector2(0,0);
  dial.Compass=false;dial.DisplayFormat="0.0";dial.Choices=new[]{15.55f*SpeedValues.Knots,20.57f*SpeedValues.Knots,22.64f*SpeedValues.Knots};dial.Minimum=dial.Choices[0];dial.Maximum=dial.Choices[2];
  for(int n=0;n<3;n++){
   float fraction=(dial.Choices[n]-dial.Minimum)/(dial.Maximum-dial.Minimum),angle=(fraction*180-90)*(float)Math.PI/180;
   e.position=At(dial,(float)Math.Sin(angle)*50,(float)Math.Cos(angle)*50);dial.OnDrag(e);
   var clicked=input.text;Check(clicked==dial.Choices[n].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture),"native speed preset click formatting "+n);
   input.text=dial.Choices[0].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);e.scrollDelta=new UnityEngine.Vector2(0,1);for(int k=0;k<n;k++)dial.OnScroll(e);Check(input.text==clicked,"wheel and click agree on native speed label "+n);
   dial.Write(SpeedValues.Nudge(dial.Choices,dial.Choices[Math.Max(0,n-1)],n==0?0:1));Check(input.text==clicked,"preset button and click agree on native speed label "+n);
  }
  Console.WriteLine("PASS: "+count+" manual dial event assertions (actual PreparationDial; independent UI fixtures).");return count;
 }
}
