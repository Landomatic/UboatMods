// Offline test doubles. Not part of the installed mod.
using System;
using System.Reflection;
using System.Collections.Generic;
using Landomatic.TorpedoRoom;
using PinguUboatMultiplayer;
namespace HarmonyLib {
 public class Harmony { public Harmony(string id){} public void Patch(MethodInfo m,HarmonyMethod p){} }
 public class HarmonyMethod { public HarmonyMethod(Type t,string n){} }
 public static class AccessTools {
  const BindingFlags F=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  public static int TypeLookups;
  public static Type TypeByName(string n){TypeLookups++;return typeof(AccessTools).Assembly.GetType(n);}
  public static FieldInfo Field(Type t,string n){return t==null?null:t.GetField(n,F);}
  public static PropertyInfo Property(Type t,string n){return t==null?null:t.GetProperty(n,F);}
  public static MethodInfo Method(Type t,string n){return t==null?null:t.GetMethod(n,F);}
 }
}
namespace UnityEngine {
 public enum KeyCode {None=0,Escape=27,T=116,K=107,F8=289,F9=290,RightShift=303,LeftShift=304,RightControl=305,LeftControl=306,RightAlt=307,LeftAlt=308,RightCommand=309,LeftCommand=310,LeftWindows=311,RightWindows=312,AltGr=313,Mouse0=323,Mouse1=324}
 public static class Input {
  public static HashSet<KeyCode> Down=new HashSet<KeyCode>(),Held=new HashSet<KeyCode>();
  public static bool GetKey(KeyCode k){return Held.Contains(k);}
  public static bool GetKeyDown(KeyCode k){return Down.Contains(k);}
 }
 public static class Time { public static float unscaledTime=100; public static double realtimeSinceStartupAsDouble=100; }
 public static class Debug { public static void Log(object x){} public static void LogWarning(object x){} }
}
namespace UBOAT.Game.Scene.Items {
 public class TorpedoLauncherGroup {public string TypeName="Bow";}
 public class FakeMissile {public int Pistol;public int PistolSetting {get{return Pistol;}}}
 public class FakeItem {public string TypeName="T3 LUT";public PreparationStatus Saved=new PreparationStatus();public FakeMissile Missile=new FakeMissile();}
 public class FakeTube {public TorpedoLauncherGroup Group=new TorpedoLauncherGroup();public FakeItem Torpedo=new FakeItem();}
}
namespace Landomatic.TorpedoRoom {
 using UBOAT.Game.Scene.Items;
 internal static class MpScopeFire {internal static void Install(HarmonyLib.Harmony h,Type t){} }
 internal static class Config {public static RoomAccess Access;public static ViewportMode Viewport=ViewportMode.Synchronized;public static UnityEngine.KeyCode ShortcutKey=UnityEngine.KeyCode.T;public static bool ShortcutCtrl=true,ShortcutAlt=true,ShortcutShift;}
 public class PreparationStatus {public Preparation Value=new Preparation();}
 public class FakeShip {public FakeTube[] TorpedoLaunchers={new FakeTube()};}
 internal static class Runtime {
  public static string Epoch=Guid.NewGuid().ToString("N"),Message;
 public static bool Loading=false;
  public static int Accepted,ViewportAccepted,PistolWrites;
  public static FakeShip Ship=new FakeShip();
  public static void Say(string s){Message=s;}
  public static PreparationStatus Status(FakeItem item,bool create){return item.Saved;}
  public static FakeMissile Missile(FakeItem item){return item.Missile;}
  public static void SetPistol(FakeMissile m,int p){PistolWrites++;m.Pistol=p;}
  public static Snapshot Snapshot(string message){return new Snapshot {Epoch=Epoch,Enabled=true,Notice=message,Tubes=new[]{new TubeSnapshot{Index=0,Type="T3 LUT",Settings=Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy()}}};}
  public static bool AcceptViewport(TubeSnapshot[] edits,object peer){
   if(!Bridge.AllowedViewport(peer)||edits.Length!=1||edits[0].Index!=0)return false;
   var current=Ship.TorpedoLaunchers[0].Torpedo.Saved.Value;var edit=edits[0].Settings;
   if(!Rules.ViewportEdit(current,edit)||Rules.Check(current,edit,PistolPolicy.Free,true,true,true)!=null)return false;
   ViewportAccepted++;edit.Revision++;Ship.TorpedoLaunchers[0].Torpedo.Saved.Value=edit;return true;
  }
  public static bool Accept(int i,Preparation p,object peer){
   var current=Ship.TorpedoLaunchers[0].Torpedo.Saved.Value;
   if(!Bridge.Allowed(peer,new TorpedoLauncherGroup())||Rules.Check(current,p,PistolPolicy.Free,false,true,true)!=null)return false;
   Accepted++;p.Revision++;Ship.TorpedoLaunchers[0].Torpedo.Saved.Value=p;return true;
  }
 }
}
namespace PinguUboatMultiplayer {
 internal enum MpRole {Idle,Hosting,Joining,Joined}
 internal enum MpSessionMode {SharedBoat,OwnBoats}
 internal class MpPeer {public object Endpoint=new object();public byte PeerId=1;public MpPeerState State=MpPeerState.Synced;public MpSessionMode Mode=MpSessionMode.SharedBoat;}
 internal static class MpMode {public static bool SharedBoat=true;}
 internal static class MpOfficers {public static string ClaimOf(MpPeer p){return "Engineer";}}
 internal static class MpStations {
  internal class User {public byte Peer=1;public string Station="TUBES:Bow";public double Seen=100;}
  internal static bool TubesHiddenAtScopes {get;set;}
  internal static List<User> Users=new List<User>{new User()};
 }
 internal static partial class MpBuild {public const string Version="1.11.5";}
 internal static class MpSession {
  public static MpRole Role=MpRole.Hosting;
  public static List<MpPeer> Peers=new List<MpPeer>();
  public static object _host=new object();
  public static List<byte[]> Sent=new List<byte[]>();
  static MpPeer FindByEndpoint(object endpoint){return Peers.Find(p=>Equals(p.Endpoint,endpoint));}
  static void OnHostPacket(object endpoint,MpMsg type,MpReader r){}
  static void OnJoinerPacket(object endpoint,MpMsg type,MpReader r){}
  static bool Capture(MpWriter writer){var bytes=new byte[writer.Length];Array.Copy(writer.Buffer,bytes,bytes.Length);Sent.Add(bytes);return true;}
  public static bool SendToHost(MpWriter writer,bool reliable){return Capture(writer);}
  public static bool SendWriter(object endpoint,MpWriter writer,bool reliable){return Capture(writer);}
 }
}
public static class BridgeChecks {
 static int count;
 static void Check(bool b,string s){count++;if(!b)throw new Exception("FAILED: "+s);}
 static MpReader Body(byte[] bytes){return new MpReader(bytes,bytes.Length);}
 static object[] Args(object endpoint,byte[] bytes){return new object[]{endpoint,(MpMsg)240,Body(bytes)};}
 public static int Run(){
  count=0;Bridge.Install(new HarmonyLib.Harmony("test"));
  MpStations.TubesHiddenAtScopes=true;Check(Bridge.RestrictedScopes,"effective MP restriction is read from installed station API");MpStations.TubesHiddenAtScopes=false;Check(!Bridge.RestrictedScopes,"unrestricted or solo MP state does not activate optional Feuer");
  Check(Bridge.Status.Contains("ready"),"bridge exact-version installation");
  var peer=new MpPeer();MpSession.Peers.Add(peer);
  byte[] hello=Wire.Pack(Wire.Hello,w=>{});
  Check(!Bridge.HostPacket(Args(peer.Endpoint,hello)),"consume extension hello");
  Check(MpSession.Sent.Count==1,"authenticated hello gets snapshot");
  var frame=MpSession.Sent[0];var read=new MpReader(frame,frame.Length);MpMsg msg;ushort ver;read.TryHeader(out msg,out ver);
  byte op;using(var r=Wire.Open(read.CopyRemaining(),out op)){var state=Wire.Decode(r);Check(state.Epoch==Runtime.Epoch,"host epoch sent through real writer");}
  int sent=MpSession.Sent.Count;
  Bridge.HostPacket(Args(new object(),hello));Check(MpSession.Sent.Count==sent,"unknown endpoint ignored");
  peer.Mode=MpSessionMode.OwnBoats;Bridge.HostPacket(Args(peer.Endpoint,hello));Check(MpSession.Sent.Count==sent,"own-boat peer not given shared-boat settings");peer.Mode=MpSessionMode.SharedBoat;
  Check(Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup()),"claimed officer at correct tube group");
  MpStations.Users[0].Station="SCOPE:Attack Periscope";Check(!Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup()),"scope does not grant tube prep");
  MpStations.Users[0].Station="TUBES:Stern";Check(!Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup()),"other compartment read-only");
  MpStations.Users[0].Station="TUBES:Bow";MpStations.Users[0].Seen=90;Check(!Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup()),"expired station refused");MpStations.Users[0].Seen=100;
  var edit=Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy();edit.Pistol=1;
  Func<string,byte[]> packet=epoch=>Wire.Pack(Wire.Edit,w=>{w.Write(epoch);w.Write((byte)0);Wire.Write(w,edit);});
  Bridge.HostPacket(Args(peer.Endpoint,packet(Runtime.Epoch)));Check(Runtime.Accepted==1,"confirmed edit reaches host transaction");
  Bridge.HostPacket(Args(peer.Endpoint,packet(Runtime.Epoch)));Check(Runtime.Accepted==1,"immediate duplicate rate limited");
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,packet(Runtime.Epoch)));Check(Runtime.Accepted==1,"stale edit rejected after rate window");
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,packet(Guid.NewGuid().ToString("N"))));Check(Runtime.Accepted==1,"old world request cannot edit new world");
  Check(Bridge.HostPacket(new object[]{peer.Endpoint,(MpMsg)25,Body(new byte[0])}),"unrelated packet untouched");
  Bridge.ResetWorld();MpSession.Role=MpRole.Joined;
  Check(!Bridge.Request(0,edit),"client cannot edit before host handshake");
  var snapshot=Runtime.Snapshot("accepted");snapshot.Policy=PistolPolicy.DryTubePreparation;
  Check(!Bridge.ClientPacket(Args(new object(),Wire.Encode(snapshot)))&&Bridge.Last==null,"non-host snapshot ignored");
  Bridge.ClientPacket(Args(MpSession._host,Wire.Encode(snapshot)));Check(Bridge.Synced&&Bridge.Last.Policy==PistolPolicy.DryTubePreparation,"host policy accepted");
  Check(Bridge.Request(0,Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy()),"handshaken edit sent");
  UnityEngine.Time.unscaledTime+=9;Check(!Bridge.Synced,"expired host confirmation disables edit readiness");
  Bridge.ResetWorld();Check(Bridge.Last==null,"world reset clears old snapshot");
  // Cache regression: an installed adapter must never repeat assembly/type scans.
  MpSession.Role=MpRole.Idle;Bridge.Tick();int lookups=HarmonyLib.AccessTools.TypeLookups;
  for(int i=0;i<1000;i++){UnityEngine.Time.unscaledTime+=.02f;Bridge.Tick();}
  Check(HarmonyLib.AccessTools.TypeLookups==lookups,"1000 idle ticks perform no new type lookups");
  MpStations.Users[0].Seen=UnityEngine.Time.realtimeSinceStartupAsDouble;MpStations.Users[0].Station="TUBES:Bow";
  Check(!Bridge.AllowedViewport(peer),"tube room cannot forge viewport-edit route");
  MpStations.Users[0].Station="SCOPE:Attack Periscope";Check(Bridge.AllowedViewport(peer),"fresh claimed scope permits program-only route");
  MpStations.Users[0].Station="TDC";Check(Bridge.AllowedViewport(peer),"fresh claimed TDC permits program-only route");
  MpStations.Users[0].Seen=90;Check(!Bridge.AllowedViewport(peer),"stale viewport permission refused");MpStations.Users[0].Seen=100;
  Bridge.ResetWorld();MpSession.Role=MpRole.Hosting;Bridge.HostPacket(Args(peer.Endpoint,hello));
  var viewport=Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy();viewport.ProgramOverride=false;viewport.Distance=5000;
  Func<byte[]> vp=()=>Wire.Pack(Wire.Viewport,w=>{w.Write(Runtime.Epoch);w.Write((byte)1);w.Write((byte)0);Wire.Write(w,viewport);});
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,vp()));Check(Runtime.ViewportAccepted==1,"authorized viewport batch reaches host transaction");
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,vp()));Check(Runtime.ViewportAccepted==1,"stale viewport batch refused");
  viewport=Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy();viewport.Pistol=1-viewport.Pistol;
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,vp()));Check(Runtime.ViewportAccepted==2,"synchronized viewport pistol edit reaches policy validation");
  viewport=Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy();viewport.StraightRun=!viewport.StraightRun;
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,vp()));Check(Runtime.ViewportAccepted==2,"viewport route cannot change run mode");
  Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.ProgramOverride=true;
  viewport=Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value.Copy();viewport.Distance=500;
  UnityEngine.Time.unscaledTime+=1;Bridge.HostPacket(Args(peer.Endpoint,vp()));Check(Runtime.ViewportAccepted==3,"synchronized host accepts per-tube program update");
  MpSession.Role=MpRole.Joined;Bridge.ClientPacket(Args(MpSession._host,Wire.Encode(Runtime.Snapshot(""))));
  int writes=Runtime.PistolWrites;var original=Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value;
  for(int i=0;i<100;i++)Bridge.ApplySnapshot();
  Check(Runtime.PistolWrites==writes&&ReferenceEquals(original,Runtime.Ship.TorpedoLaunchers[0].Torpedo.Saved.Value),"unchanged snapshots do not rewrite pistol or allocate preparation copies");
  MpSession.Role=MpRole.Hosting;MpStations.Users[0].Seen=UnityEngine.Time.realtimeSinceStartupAsDouble;
  MpStations.Users[0].Station="TUBES:Bow";Config.Access=RoomAccess.AllEditable;Check(Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup{TypeName="Stern"}),"all-editable allows another real tube-room station");
  Config.Access=RoomAccess.OppositeReadOnly;Check(!Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup{TypeName="Stern"}),"opposite read-only refused by host");
  MpStations.Users[0].Station="TUBES:Invented";Config.Access=RoomAccess.AllEditable;Check(!Bridge.Allowed(peer,new UBOAT.Game.Scene.Items.TorpedoLauncherGroup{TypeName="Stern"}),"invented room cannot exploit all-editable rule");
  MpStations.Users[0].Station="SCOPE:Attack Periscope";MpStations.TubesHiddenAtScopes=true;Check(!Bridge.AllowedViewport(peer),"MP scopes-panel restriction takes precedence");MpStations.TubesHiddenAtScopes=false;
  Config.Viewport=ViewportMode.ReadOnly;Check(!Bridge.AllowedViewport(peer),"read-only host rejects viewport edits");Config.Viewport=ViewportMode.Hidden;Check(!Bridge.AllowedViewport(peer),"hidden host rejects viewport edits");
  Console.WriteLine("PASS: "+count+" multiplayer-adapter assertions (simulated session/stations; real protocol framing).");return count;
 }
}
