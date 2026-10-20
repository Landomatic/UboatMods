// Offline configuration/provider checks. Not installed with the mod.
using System;
using System.IO;
using System.Collections.Generic;
using Landomatic.TorpedoRoom;
using System.Reflection;
namespace HarmonyLib {public static class AccessTools {
 public static Type TypeByName(string name){return typeof(AccessTools).Assembly.GetType(name);}
 public static FieldInfo Field(Type type,string name){return type.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);}
 public static MethodInfo Method(Type type,string name){return type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);}
}}
namespace DustysCommunityPatch.Core {
 internal static class QolConfig {internal static Dictionary<string,string> Values=new Dictionary<string,string>();internal static void SetString(string key,string value){Values[key]=value;}internal static void SetBool(string key,bool value){Values[key]=value.ToString();}internal static void Save(){} }
 internal static class QolPanel {internal static UnityEngine.GameObject _settingsPane=new UnityEngine.GameObject();internal static int Refreshes;internal static void RefreshSettingsPane(){Refreshes++;_settingsPane.Scroll.content=new UnityEngine.RectTransform();}}
}
namespace UnityEngine.UI {public class ScrollRect {public UnityEngine.RectTransform content=new UnityEngine.RectTransform();public int Stops;public void StopMovement(){Stops++;}}}
namespace UnityEngine {
 public class GameObject {public UnityEngine.UI.ScrollRect Scroll=new UnityEngine.UI.ScrollRect();public T GetComponent<T>() where T:class {return Scroll as T;}}
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero{get{return new Vector2(0,0);}}}
 public class RectTransform {public Vector2 anchoredPosition;}

 public enum KeyCode {None=0,Escape=27,T=116,K=107,F8=289,F9=290,RightShift=303,LeftShift=304,RightControl=305,LeftControl=306,RightAlt=307,LeftAlt=308,RightCommand=309,LeftCommand=310,LeftWindows=311,RightWindows=312,AltGr=313,Mouse0=323,Mouse1=324}
 public static class Input {
  public static HashSet<KeyCode> Down=new HashSet<KeyCode>(),Held=new HashSet<KeyCode>();
  public static bool GetKey(KeyCode k){return Held.Contains(k);}
  public static bool GetKeyDown(KeyCode k){return Down.Contains(k);}
 }
 public static class Time { public static float unscaledTime=100; public static double realtimeSinceStartupAsDouble=100; }
 public static class Debug { public static void Log(object x){} public static void LogWarning(object x){} }
 public static class Application { public static string persistentDataPath; }
 public static class Mathf { public static float Clamp(float x,float lo,float hi){return Math.Max(lo,Math.Min(hi,x));} }
}

namespace Landomatic.TorpedoRoom {
 internal static class Bridge { public static bool IsRemote; public static Snapshot Last; }
 internal static class Runtime {public static string Message;public static void Say(string s){Message=s;}public static void RemoveRecords(){} }
}
public static class ConfigChecks {
 static int count;
 static void Check(bool ok,string name){count++;if(!ok)throw new Exception("FAILED: "+name);}
 public static int Run(){
  string dir=Path.Combine(Path.GetTempPath(),"TorpedoRoomConfig-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  UnityEngine.Application.persistentDataPath=dir;string file=Path.Combine(dir,"TorpedoRoomConfig.txt");
  try{
   Config.Load();Check(File.Exists(file),"first load creates config without menu interaction");
   string initial=File.ReadAllText(file);Check(initial.Contains("shortcutKey=T")&&initial.Contains("shortcutCtrl=False")&&initial.Contains("shortcutAlt=False"),"created shortcut defaults");
   Check(initial.Contains("preparationSeconds=30"),"new config has thirty-second timed pistol default");
   Check(initial.Contains("allowStraightRun=True"),"created host lockout default");
   Config.ShortcutKey=UnityEngine.KeyCode.K;Config.Delay=35;Config.Enabled=false;Config.Save();
   Config.Delay=0;Config.Enabled=true;Config.ShortcutKey=UnityEngine.KeyCode.T;Config.Load();
   Check(Config.ShortcutKey==UnityEngine.KeyCode.K&&Config.Delay==35&&!Config.Enabled,"saved preferences restored");
   Check(!File.Exists(file+".tmp"),"atomic save leaves no temporary file");
   File.WriteAllText(file,"enabled=True\nshortcutKey=F8\n");Config.Load();Check(Config.ShortcutKey==UnityEngine.KeyCode.None,"reserved saved key disabled");
   Config.ShortcutKey=UnityEngine.KeyCode.T;Config.ShortcutCtrl=Config.ShortcutAlt=true;Config.ShortcutShift=false;
   File.WriteAllText(file,"enabled=True\n");Config.Load();Check(Config.ShortcutKey==UnityEngine.KeyCode.T&&Config.ShortcutCtrl&&Config.ShortcutAlt,"legacy config inherits default shortcut");
   Check(CommunityPatchSettings.Category=="Submarine / Torpedoes","actual Dusty tab name");
   Check(CommunityPatchSettings.ModName=="Torpedo Room","Dusty provider name");
   object[] rows=CommunityPatchSettings.GetSettings();Check(rows.Length==15,"provider exposes all rows");
   object[] key=null,master=null;
   foreach(object value in rows){var row=(object[])value;Check(row.Length>=6&&row[4] is Func<object>&&row[5] is Action<object>,"provider row delegates "+row[1]);((Func<object>)row[4])();if((string)row[1]=="shortcutKey")key=row;if((string)row[1]=="enabled")master=row;}
   Check(key!=null&&(string)key[0]=="key","native key picker contract");
   ((Action<object>)key[5])(UnityEngine.KeyCode.K);Check(Config.ShortcutKey==UnityEngine.KeyCode.K&&File.ReadAllText(file).Contains("shortcutKey=K"),"key picker setter persists");
   ((Action<object>)key[5])(UnityEngine.KeyCode.F9);Check(Config.ShortcutKey==UnityEngine.KeyCode.K,"key picker rejects quickload");
   Bridge.IsRemote=true;Bridge.Last=new Snapshot{Enabled=true};((Action<object>)master[5])(false);Check(Config.Enabled,"client cannot change host master");
   Bridge.IsRemote=false;CommunityPatchSettings.ResetToDefaults();Check(Config.ShortcutKey==UnityEngine.KeyCode.T&&!Config.ShortcutCtrl&&!Config.ShortcutAlt&&!Config.ShortcutShift&&Config.Enabled,"reset writes defaults");
   Check(Config.Delay==30,"reset restores thirty-second pistol preparation time");
   File.Delete(file);string legacy=Path.Combine(dir,"LandomaticTorpedoRoom.txt"),previous=Path.Combine(dir,"TorpedoRoom.txt");
   string oldText="enabled=False\nshortcutKey=K\noverview=True\n";File.WriteAllText(legacy,oldText);File.WriteAllText(previous,oldText);
   Config.Enabled=true;Config.ShortcutKey=UnityEngine.KeyCode.T;Config.Overview=false;Config.Load();
   Check(File.Exists(file)&&Config.Enabled&&Config.ShortcutKey==UnityEngine.KeyCode.T&&!Config.Overview,"new config name starts with defaults, without importing either prior filename");
   Check(File.ReadAllText(legacy)==oldText&&File.ReadAllText(previous)==oldText,"older config files remain untouched");
   File.WriteAllText(file,"enabled=False\nshortcutKey=K\n");Config.Enabled=true;Config.ShortcutKey=UnityEngine.KeyCode.T;Config.Load();
   Check(!Config.Enabled&&Config.ShortcutKey==UnityEngine.KeyCode.K,"only TorpedoRoomConfig.txt supplies preferences");
   Check(CommunityPatchSettings.GetSettings().Length==15,"diagnostic timing option exposed");
   Config.Access=RoomAccess.AllEditable;Config.Viewport=ViewportMode.Hidden;Config.Save();Config.Access=RoomAccess.CurrentCompartment;Config.Viewport=ViewportMode.ReadOnly;Config.Load();
   Check(Config.Access==RoomAccess.AllEditable&&Config.Viewport==ViewportMode.Hidden,"new room and viewport modes persisted");
   object[] preset=null,access=null;foreach(object value in CommunityPatchSettings.GetSettings()){var row=(object[])value;if((string)row[1]=="preset")preset=row;if((string)row[1]=="roomAccess")access=row;}
   Check(preset!=null&&access!=null&&(string)access[0]=="choice"&&((string[])access[6]).Length==3,"native Dusty three-choice contract");
   ((Action<object>)preset[5])(0);Check(Config.Access==RoomAccess.AllEditable&&Config.Viewport==ViewportMode.Synchronized,"Casual preset");
   ((Action<object>)preset[5])(1);Check(Config.Access==RoomAccess.CurrentCompartment&&Config.Viewport==ViewportMode.Hidden,"Hardcore preset");
   Bridge.IsRemote=true;Bridge.Last=new Snapshot{Access=RoomAccess.CurrentCompartment};((Action<object>)access[5])(2);Check(Config.Access==RoomAccess.CurrentCompartment,"client cannot loosen host room rule");Bridge.IsRemote=false;
   ((Action<object>)preset[5])(1);((Action<object>)preset[5])(2);Check((int)((Func<object>)preset[4])()==2,"Hardcore to Custom advances without reverting the displayed choice");
   ((Action<object>)preset[5])(0);Check(DustysCommunityPatch.Core.QolConfig.Values["Torpedo_Room.roomAccess"]=="All tubes editable"&&DustysCommunityPatch.Core.QolConfig.Values["Torpedo_Room.viewportDisplay"]=="Synchronized controls","preset updates dependent Dusty stored values");
   int refreshes=DustysCommunityPatch.Core.QolPanel.Refreshes;var scroll=DustysCommunityPatch.Core.QolPanel._settingsPane.Scroll;scroll.content.anchoredPosition=new UnityEngine.Vector2(0,420);MenuIntegration.Tick();Check(DustysCommunityPatch.Core.QolPanel.Refreshes==refreshes+1,"preset refreshes dependent rows after current callback completes");MenuIntegration.Tick();Check(DustysCommunityPatch.Core.QolPanel.Refreshes==refreshes+1,"no per-frame settings rebuild");Check(scroll.content.anchoredPosition.y==420&&scroll.Stops==1,"dependent-label rebuild preserves scroll offset and stops old momentum");
   ((Action<object>)access[5])(0);Check(Config.Preset==2&&DustysCommunityPatch.Core.QolConfig.Values["Torpedo_Room.preset"]=="Custom","manual rule edit marks and persists Custom");
   Config.Preset=0;Config.Load();Check(Config.Preset==2,"explicit custom preset persisted in own config");
   object[] locked=null,port=null,dry=null,sync=null;foreach(object value in CommunityPatchSettings.GetSettings()){var row=(object[])value;if((string)row[1]=="locked")locked=row;if((string)row[1]=="port")port=row;if((string)row[1]=="dry")dry=row;if((string)row[1]=="speedSync")sync=row;}
   Check(locked==null&&port==null&&sync!=null,"retired port policy absent and independent speed rule present");
   ((Action<object>)dry[5])(true);Check(Config.Policy==PistolPolicy.DryTubePreparation,"optional timed dry-tube policy remains selectable");
   ((Action<object>)preset[5])(1);((Action<object>)preset[5])(2);Check(Config.Access==RoomAccess.CurrentCompartment&&Config.Viewport==ViewportMode.Hidden,"Custom preserves existing settings");
   Check(Config.SpeedSync,"new speed rule defaults ON");
   ((Action<object>)sync[5])(false);Config.SpeedSync=true;Config.Load();Check(!Config.SpeedSync,"speed rule persists independently");
   ((Action<object>)preset[5])(0);((Action<object>)preset[5])(1);Check(!Config.SpeedSync,"presets preserve independent speed rule");
   Bridge.IsRemote=true;Bridge.Last=new Snapshot{SpeedSync=true};Check((bool)((Func<object>)sync[4])(),"client displays host speed rule");((Action<object>)sync[5])(true);Check(!Config.SpeedSync,"client cannot edit host speed rule");Bridge.IsRemote=false;
   string retired="pistolPolicy=2\n";File.WriteAllText(file,retired);Config.Policy=PistolPolicy.DryTubePreparation;Config.Load();Check(Config.Policy==PistolPolicy.Free&&File.ReadAllText(file)==retired,"retired port policy falls back to free without rewriting settings");
   File.WriteAllText(file,"pistolPolicy=1\n");Config.Policy=PistolPolicy.DryTubePreparation;Config.Load();Check(Config.Policy==PistolPolicy.Free&&File.ReadAllText(file)=="pistolPolicy=1\n","retired loaded lock is free at runtime without config migration");
   CommunityPatchSettings.ResetToDefaults();Check(Config.SpeedSync,"reset enables speed sync");
   Console.WriteLine("PASS: "+count+" configuration/provider assertions (actual Settings.cs, isolated temporary directory).");return count;
  } finally {Directory.Delete(dir,true);}
 }
}
