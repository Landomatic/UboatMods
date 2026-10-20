using System;
using System.IO;
using System.Collections.Generic;
using Landomatic.TorpedoRoom;
using PinguUboatMultiplayer;

// The upstream MpProtocol.cs is a local reference input, not redistributed here.
namespace PinguUboatMultiplayer { internal static partial class MpBuild { public const ushort ProtocolVersion=54; } }
public static class Checks
{
    static int count;
    static void Check(bool value,string name){count++;if(!value)throw new Exception("FAILED: "+name);}
    static void Bad(Action action,string name){bool bad=false;try{action();}catch(Exception){bad=true;}Check(bad,name);}
    static Snapshot Roundtrip(byte[] bytes){byte op;using(var r=Wire.Open(bytes,out op)){Check(op==Wire.State,"state operation");return Wire.Decode(r);}}
    public static int Run()
    {
        count=0;
        var current=new Preparation();var edit=current.Copy();edit.Pistol=1;
        Check(current.Valid(),"valid initial record");
        Check(current.Pistol==0 && edit.Pistol==1,"draft does not mutate confirmed record");
        var pinned=current.Copy();pinned.ProgramOverride=true;pinned.StraightRun=true;pinned.Distance=3500;pinned.Angle=123;pinned.Right=true;
        Check(Rules.Check(current,pinned,PistolPolicy.Free,false,false,true,true)==null,"pin may be inserted at sea");
        Check(Rules.Check(current,pinned,PistolPolicy.Free,false,false,true,false)!=null,"host can prohibit adding pin");
        Check(Rules.Check(pinned,pinned.Copy(),PistolPolicy.Free,false,false,true,false)==null,"dormant pin does not block other edits");
        var unpinned=pinned.Copy();unpinned.StraightRun=false;
        Check(Rules.Check(pinned,unpinned,PistolPolicy.Free,false,false,true,true)==null,"pin removable at sea");
        Check(unpinned.Distance==3500&&unpinned.Angle==123&&unpinned.Right,"pin removal preserves program");
        Check(Rules.Check(current,pinned,PistolPolicy.Free,false,false,false,true)!=null,"no pin fitted to straight runner");
        Check(Rules.Check(current,edit,PistolPolicy.Free,false,false,false)==null,"free pistol at sea");
        Check(Rules.Check(current,edit,(PistolPolicy)1,true,true,false)==null,"retired loaded-pistol lock has no enforcement");
        Check(Rules.Check(current,edit,PistolPolicy.DryTubePreparation,false,false,false)!=null,"dry preparation refuses flooded");
        Check(Rules.Check(current,edit,PistolPolicy.DryTubePreparation,false,true,false)==null,"dry preparation at sea");
        edit=current.Copy();edit.ProgramOverride=true;edit.Distance=3500;edit.Right=true;edit.Angle=271;
        foreach(PistolPolicy policy in Enum.GetValues(typeof(PistolPolicy)))
            Check(Rules.Check(current,edit,policy,false,false,true)==null,"FAT/LUT at sea under "+policy);
        Check(Rules.Check(current,edit,PistolPolicy.Free,false,true,false)!=null,"no program added to straight runner");
        edit=current.Copy();edit.Id=Guid.NewGuid().ToString("N");
        Check(Rules.Check(current,edit,PistolPolicy.Free,true,true,true)!=null,"replacement identity refuses stale edit");
        edit=current.Copy();edit.Revision++;
        Check(Rules.Check(current,edit,PistolPolicy.Free,true,true,true)!=null,"concurrent revision refuses stale edit");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1,0,499,15001}){
            edit=current.Copy();edit.Distance=bad;Check(!edit.Valid(),"bad distance "+bad);
        }
        foreach(float bad in new[]{float.NaN,float.NegativeInfinity,-.1f,360,1000}){
            edit=current.Copy();edit.Angle=bad;Check(!edit.Valid(),"bad angle "+bad);
        }
        foreach(float good in new[]{500f,1500f,15000f}){edit=current.Copy();edit.Distance=good;Check(edit.Valid(),"distance endpoint "+good);}
        edit=current.Copy();edit.Pistol=2;Check(!edit.Valid(),"invalid pistol enum");
        edit=current.Copy();edit.InitialPistol=-1;Check(!edit.Valid(),"invalid initial pistol");
        edit=current.Copy();edit.Id="tube1";Check(!edit.Valid(),"not a per-torpedo guid");
        var snapshots=new List<TubeSnapshot>();
        for(int i=0;i<16;i++)snapshots.Add(new TubeSnapshot {Index=i,Type="G7e T3 Pi2 LUT",State=i%9,Flooded=i%2==0,Flooding=i/16f,Settings=new Preparation {Pistol=i%2,Right=i%2!=0,StraightRun=i%3==0,ProgramOverride=true,Distance=500+i*500,Angle=i*20,Revision=i}});
        var state=new Snapshot {Epoch=Guid.NewGuid().ToString("N"),Enabled=true,Policy=PistolPolicy.DryTubePreparation,Delay=30,Notice="Accepted",SolutionSpeed=20.57f,SpeedTube=3,Tubes=snapshots.ToArray()};
        byte[] payload=Wire.Encode(state);var decoded=Roundtrip(payload);
        Check(decoded.Epoch==state.Epoch && decoded.Enabled && decoded.Policy==state.Policy && decoded.Delay==30,"host policy roundtrip");
        Check(decoded.SolutionSpeed==20.57f&&decoded.SpeedTube==3,"host solution speed and room focus round trip");
        var trc5=Wire.Pack(Wire.Hello,w=>{});trc5[3]=(byte)'5';Bad(()=>{byte op;using(var q=Wire.Open(trc5,out op)){}},"TRC5 peer is refused by TRC8");
        var malformed=new Snapshot{Epoch=Guid.NewGuid().ToString("N"),SolutionSpeed=float.NaN};Bad(()=>Roundtrip(Wire.Encode(malformed)),"nonfinite host solution speed refused");
        malformed.SolutionSpeed=101;Bad(()=>Roundtrip(Wire.Encode(malformed)),"out of range host solution speed refused");
        malformed.SolutionSpeed=20;malformed.SpeedTube=16;Bad(()=>Roundtrip(Wire.Encode(malformed)),"invalid host room speed focus refused");
        Check(decoded.Tubes.Length==16,"all tube count");
        for(int i=0;i<16;i++)Check(decoded.Tubes[i].State==i%9&&decoded.Tubes[i].Flooded==(i%2==0)&&decoded.Tubes[i].Flooding==i/16f,"authoritative water state "+i);
        for(int i=0;i<16;i++){
            Check(decoded.Tubes[i].Settings.Id==state.Tubes[i].Settings.Id,"torpedo identity "+i);
            Check(decoded.Tubes[i].Settings.Revision==i && decoded.Tubes[i].Settings.Right==state.Tubes[i].Settings.Right,"per tube settings "+i);
            Check(decoded.Tubes[i].Settings.StraightRun==state.Tubes[i].Settings.StraightRun,"per tube pin roundtrip "+i);
        }
        // Pass our payload through the supplied multiplayer's actual writer and reader.
        var writer=new MpWriter();writer.Begin((MpMsg)240);foreach(byte b in payload)writer.WriteByte(b);
        var reader=new MpReader(writer.Buffer,writer.Length);MpMsg kind;ushort version;
        Check(reader.TryHeader(out kind,out version)&&((byte)kind)==240&&version==54,"actual multiplayer header");
        var transport=Roundtrip(reader.CopyRemaining());Check(transport.Tubes[15].Settings.Distance==8000,"actual upstream envelope preserves last tube");
        // Session SendWriter normally bundles a small companion message.
        var hello=Wire.Pack(Wire.Hello,w=>{});writer.Begin((MpMsg)240);foreach(byte b in hello)writer.WriteByte(b);
        var bundle=new MpBundle();bundle.Add(writer.Buffer,writer.Length);
        var bw=new MpWriter();bundle.Build(bw,100,0);
        uint stamp;ushort seq;byte flags;
        Check(MpBundle.ReadHeader(bw.Buffer,bw.Length,out stamp,out seq,out flags),"actual bundle header");
        int pos=MpBundle.HeaderBytes,offset,length;
        Check(MpBundle.NextEntry(bw.Buffer,bw.Length,ref pos,out kind,out offset,out length)&&(byte)kind==240,"actual bundle extension type");
        var br=new MpReader(bw.Buffer,offset,length);byte operation;
        using(var r=Wire.Open(br.CopyRemaining(),out operation)){Check(operation==Wire.Hello,"bundled hello operation");Wire.Finish(r);}
        for(int cut=0;cut<payload.Length;cut+=Math.Max(1,payload.Length/25)){
            var truncated=new byte[cut];Array.Copy(payload,truncated,cut);Bad(()=>Roundtrip(truncated),"truncated state "+cut);
        }
        var trailing=new byte[payload.Length+1];Array.Copy(payload,trailing,payload.Length);Bad(()=>Roundtrip(trailing),"trailing bytes");
        var corrupt=(byte[])payload.Clone();corrupt[0]^=1;Bad(()=>Roundtrip(corrupt),"unknown protocol");
        state.Tubes[1].Index=0;Bad(()=>Roundtrip(Wire.Encode(state)),"duplicate tube index");state.Tubes[1].Index=1;
        state.Tubes[1].Settings.Revision=-1;Bad(()=>Roundtrip(Wire.Encode(state)),"negative revision");state.Tubes[1].Settings.Revision=1;
        state.Delay=float.NaN;Bad(()=>Roundtrip(Wire.Encode(state)),"invalid host delay");state.Delay=0;
        state.SpeedSync=false;Check(!Roundtrip(Wire.Encode(state)).SpeedSync,"independent speed sync OFF roundtrip");state.SpeedSync=true;Check(Roundtrip(Wire.Encode(state)).SpeedSync,"independent speed sync ON roundtrip");
        state.Policy=(PistolPolicy)1;Bad(()=>Roundtrip(Wire.Encode(state)),"retired loaded-lock wire policy refused");
        state.Policy=(PistolPolicy)2;Bad(()=>Roundtrip(Wire.Encode(state)),"retired port-only wire policy refused");
        state.Policy=(PistolPolicy)200;Bad(()=>Roundtrip(Wire.Encode(state)),"invalid host policy");state.Policy=PistolPolicy.Free;
        state.Enabled=false;Check(!Roundtrip(Wire.Encode(state)).Enabled,"disabled master replicated");
        state.AllowLockout=false;Check(!Roundtrip(Wire.Encode(state)).AllowLockout,"host lockout policy replicated");
        var oldProtocol=(byte[])payload.Clone();oldProtocol[3]=(byte)'1';Bad(()=>Roundtrip(oldProtocol),"old companion protocol refused");
        var source=new Preparation{ProgramOverride=true,Right=false,Distance=3500,Angle=215,StraightRun=true};
        var merged=source.Copy();Check(ProgramValues.Merge(merged,new ProgramValues(false,1000,0),new ProgramValues(true,1000,0)),"direction edit detected");
        Check(merged.Right&&merged.Distance==3500&&merged.Angle==215&&merged.StraightRun,"single stock dimension preserves other tube data and pin");
        Check(!ProgramValues.Merge(merged,new ProgramValues(true,1000,0),new ProgramValues(true,1000,0)),"display refresh not an edit");
        Check(!ProgramValues.Merge(merged,new ProgramValues(true,1000,0),new ProgramValues(true,float.NaN,float.PositiveInfinity)),"invalid shared values ignored");
        Check(ProgramValues.SamePreparation(source,source.Copy()),"equal preparation recognized");
        var unequal=source.Copy();unequal.Distance++;Check(!ProgramValues.SamePreparation(source,unequal),"changed data invalidates equal revision cache");
        Check(!ProgramValues.ShowFields(source,true),"inserted active pin hides program data");
        Check(ProgramValues.ShowFields(source,false),"disabled host lockout ignores saved pin");
        source.StraightRun=false;Check(ProgramValues.ShowFields(source,true),"removed pin restores program fields");
        Check(ProgramValues.PolicyLabel(PistolPolicy.Free).Contains("unrestricted"),"free label explains pistol policy");
        Check(ProgramValues.WaterLabel(2,false,0)=="DRY","instant drain visible as dry");
        Check(ProgramValues.WaterLabel(8,true,.5f).Contains("DRAINING"),"drain animation distinguished from flooded");
        Check(ProgramValues.WaterLabel(5,false,.5f).Contains("FLOODING"),"flood animation distinguished from dry");
        Check(ProgramValues.WaterLabel(6,true,1)=="FLOODED","flood complete visible");
        source.ProgramOverride=false;merged=source.Copy();merged.Distance=9000;Check(Rules.ViewportEdit(source,merged),"program-only viewport changes allowed");
        merged.Pistol=1-source.Pistol;Check(Rules.ViewportEdit(source,merged),"synchronized viewport pistol change remains subject to host Rules.Check");
        merged=source.Copy();merged.StraightRun=!source.StraightRun;Check(!Rules.ViewportEdit(source,merged),"viewport cannot remove pin");
        merged=source.Copy();merged.ProgramOverride=true;Check(!Rules.ViewportEdit(source,merged),"viewport cannot take ownership of room program");
        source.ProgramOverride=true;merged=source.Copy();merged.Distance=500;Check(Rules.ViewportEdit(source,merged),"synchronized viewport may update a per-tube program");source.ProgramOverride=false;
        merged=source.Copy();merged.InitialPistol=1;Check(!Rules.ViewportEdit(source,merged),"viewport cannot change initial pistol");
        state.Tubes[0].State=9;Bad(()=>Roundtrip(Wire.Encode(state)),"unknown live tube state");state.Tubes[0].State=0;
        state.Tubes[0].Flooding=float.NaN;Bad(()=>Roundtrip(Wire.Encode(state)),"nonfinite flood fraction");state.Tubes[0].Flooding=0;
        var previousProtocol=(byte[])payload.Clone();previousProtocol[3]=(byte)'2';Bad(()=>Roundtrip(previousProtocol),"TRC2 snapshot refused by new companion");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,0f,.5f,.75f,14.1f}){var d=new Preparation{Depth=bad};Check(!d.Valid(),"invalid launch depth "+bad);}
        foreach(float depth in new[]{1f,1.25f,4f,14f}){var d=new Preparation{Depth=depth,DepthOverride=true};Check(d.Valid(),"valid launch depth "+depth);state.Tubes[0].Settings=d;var q=Roundtrip(Wire.Encode(state));Check(q.Tubes[0].Settings.Depth==depth&&q.Tubes[0].Settings.DepthOverride,"depth wire roundtrip "+depth);}
        state.Access=RoomAccess.AllEditable;state.Viewport=ViewportMode.Hidden;var modes=Roundtrip(Wire.Encode(state));Check(modes.Access==RoomAccess.AllEditable&&modes.Viewport==ViewportMode.Hidden,"host UI modes roundtrip");
        state.Access=(RoomAccess)3;Bad(()=>Roundtrip(Wire.Encode(state)),"invalid room mode rejected");state.Access=RoomAccess.CurrentCompartment;
        state.Viewport=(ViewportMode)3;Bad(()=>Roundtrip(Wire.Encode(state)),"invalid viewport mode rejected");
        Check(DialValues.Snap(3.14f,.5f,14,.1f)>3.09f&&DialValues.Snap(3.14f,.5f,14,.1f)<3.11f,"depth precision snaps to 0.1 m");
        Check(DialValues.AtAngle(-90,.5f,14,.1f)==.5f&&DialValues.AtAngle(90,.5f,14,.1f)==14,"depth dial endpoints");
        Check(DialValues.Nudge(3,1,.5f,14,.1f)>3.09f&&DialValues.Nudge(3,-1,.5f,14,.1f)<2.91f,"depth scroll moves in tenths");
        Check(DialValues.Nudge(14,1,.5f,14,.1f)==14&&DialValues.Nudge(.5f,-1,.5f,14,.1f)==.5f,"scroll respects depth bounds");
        Check(DialValues.AtAngle(0,500,15000,100)==7800,"initial-run dial rounds midpoint to 100 m step");
        Check(DialValues.Nudge(359.9f,1,0,359.9f,.1f)==359.9f,"heading respects exclusive 360 limit");
        Check(ReportText.Tube(false,false,false,false,1500,0,3.1f)=="3.1 m","ordinary torpedo report contains depth alone");
        Check(ReportText.Tube(true,true,true,false,1500,0,3.1f)=="STRAIGHT · 3.1 m","disabled capable torpedo reports straight and depth");
        Check(ReportText.Tube(true,true,false,true,2500,90,4.2f)=="LUT 2500 m R · 90°T\n4.2 m","LUT report keeps run direction heading and depth");
        Check(ReportText.Tube(true,false,false,false,1500,0,3)=="FAT 1500 m L\n3.0 m","FAT report omits redundant tube flood and pistol fields");
        Check(!ProgramValues.DoorClosed(false,6),"dry loaded tube shows open door");
        Check(ProgramValues.DoorClosed(true,6),"flooded tube shows closed door");
        Check(ProgramValues.DoorClosed(false,5),"flooding tube closes door like native UI");
        Check(ProgramValues.DoorClosed(false,7),"native state seven retains closed door");
        Check(!ProgramValues.DoorClosed(false,0),"empty unflooded tube has open door");
        var speeds=SpeedValues.Options(23.15f,15.43f,20.57f,15.43f,0,float.NaN);
        Check(speeds.Length==3&&speeds[0]==15.43f&&speeds[2]==23.15f,"native supported speeds sorted/deduplicated");
        Check(SpeedValues.Options(15.43f,15.43f,15.43f).Length==1,"electric fixed speed still supplies one visible choice");
        Check(SpeedValues.Nearest(speeds,21)==20.57f&&SpeedValues.Nudge(speeds,15.43f,1)==20.57f,"speed dial snaps and scrolls discrete supported presets");
        Check(SpeedValues.Nudge(speeds,23.15f,1)==23.15f&&SpeedValues.Nudge(new[]{15.43f},15.43f,-1)==15.43f,"speed selection respects supported endpoints and fixed weapons");
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1f,101f,0f}){var q=new Preparation{SpeedOverride=true,Speed=bad};Check(!q.Valid(),"invalid speed override "+bad);}
        Check(new Preparation{Speed=0,SpeedOverride=false}.Valid(),"legacy inherited speed remains valid without migration");
        Check(!SpeedValues.Supports(speeds,18)&&SpeedValues.Supports(speeds,20.57f),"unsupported in-range speed is rejected");
        var speedRecord=new Preparation{SpeedOverride=true,Speed=20.57f};state.Viewport=ViewportMode.Synchronized;state.Tubes[0].Settings=speedRecord;
        var speedCopy=Roundtrip(Wire.Encode(state));Check(speedCopy.Tubes[0].Settings.SpeedOverride&&speedCopy.Tubes[0].Settings.Speed==20.57f,"per-tube speed wire roundtrip");
        var speedOther=speedRecord.Copy();speedOther.Speed=15.43f;Check(!ProgramValues.SamePreparation(speedRecord,speedOther),"speed changes invalidate snapshot equality cache");
        Check(Rules.ViewportEdit(speedRecord,speedOther),"synchronized speed edits pass through host speed validation");
        var speedOnly=current.Copy();speedOnly.SpeedOverride=true;speedOnly.Speed=20.57f;
        Check(Rules.SpeedOnlyEdit(current,speedOnly),"trusted native TDC route accepts speed only");
        speedOnly.Pistol=1-current.Pistol;Check(!Rules.SpeedOnlyEdit(current,speedOnly),"TDC route cannot bypass report policy for pistol");
        speedOnly=current.Copy();speedOnly.Distance+=500;Check(!Rules.SpeedOnlyEdit(current,speedOnly),"TDC route cannot bypass report policy for FAT/LUT");
        speedOnly=current.Copy();speedOnly.DepthOverride=!current.DepthOverride;Check(!Rules.SpeedOnlyEdit(current,speedOnly),"TDC route cannot bypass report policy for depth");
        var trc4=Wire.Pack(Wire.Hello,w=>{});trc4[3]=(byte)'4';Bad(()=>{byte op;using(var q=Wire.Open(trc4,out op)){}},"TRC4 peer cannot parse TRC8 speed payload");
        Console.WriteLine("PASS: "+count+" policy/protocol assertions (actual upstream writer/reader/bundler).");
        return count;
    }
}
public static class ShortcutChecks {
 static int count;
 static void Check(bool b,string s){count++;if(!b)throw new Exception("FAILED: "+s);}
 public static int Run(){
  count=0;
  foreach(var k in new[]{UnityEngine.KeyCode.F8,UnityEngine.KeyCode.F9,UnityEngine.KeyCode.Escape,UnityEngine.KeyCode.None,UnityEngine.KeyCode.LeftControl,UnityEngine.KeyCode.RightShift,UnityEngine.KeyCode.LeftAlt,UnityEngine.KeyCode.Mouse0,UnityEngine.KeyCode.Mouse1,(UnityEngine.KeyCode)9999})
   Check(!Shortcut.Allowed(k),"reserved/invalid key refused "+k);
  Check(Shortcut.Allowed(UnityEngine.KeyCode.T)&&Shortcut.Allowed(UnityEngine.KeyCode.K),"ordinary keys allowed");
  for(int mask=0;mask<8;mask++){
   bool ctrl=(mask&1)!=0,alt=(mask&2)!=0,shift=(mask&4)!=0;
   Check(Shortcut.Matches(true,ctrl,alt,shift,false,true,true,false)==(ctrl&&alt&&!shift),"exact modifier chord "+mask);
  }
  Check(!Shortcut.Matches(false,true,true,false,false,true,true,false),"held key not retriggered");
  Check(!Shortcut.Matches(true,true,true,false,true,true,true,false),"extra meta refused");
  UnityEngine.Input.Down.Add(UnityEngine.KeyCode.T);
  UnityEngine.Input.Held.Add(UnityEngine.KeyCode.RightControl);UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftAlt);
  Check(Shortcut.Pressed(),"right Ctrl left Alt T");
  UnityEngine.Input.Held.Clear();UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftControl);UnityEngine.Input.Held.Add(UnityEngine.KeyCode.RightAlt);
  Check(Shortcut.Pressed(),"left Ctrl right Alt T");
  UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftShift);Check(!Shortcut.Pressed(),"extra Shift ignored");
  Config.ShortcutShift=true;Check(Shortcut.Pressed(),"configured Shift accepted");
  Config.ShortcutKey=UnityEngine.KeyCode.F8;UnityEngine.Input.Down.Add(UnityEngine.KeyCode.F8);Check(!Shortcut.Pressed(),"even hand-edited F8 never triggers");
  Config.ShortcutKey=UnityEngine.KeyCode.F9;UnityEngine.Input.Down.Add(UnityEngine.KeyCode.F9);Check(!Shortcut.Pressed(),"even hand-edited F9 never triggers");
  Config.ShortcutKey=UnityEngine.KeyCode.T;Config.ShortcutShift=false;
  Check(Shortcut.Display=="Ctrl+Alt+T","live shortcut label");
  Config.ShortcutCtrl=Config.ShortcutAlt=Config.ShortcutShift=false;UnityEngine.Input.Held.Clear();UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftAlt);
  Check(Shortcut.Pressed(true),"room T works while Alt displays the game cursor");
  Check(!Shortcut.Pressed(),"strict callers retain exact modifier matching");
  UnityEngine.Input.Held.Add(UnityEngine.KeyCode.LeftShift);Check(!Shortcut.Pressed(true),"cursor Alt allowance does not loosen Shift requirements");
  UnityEngine.Input.Held.Clear();Config.ShortcutAlt=true;Check(!Shortcut.Pressed(true),"configured Alt remains required");
  Console.WriteLine("PASS: "+count+" shortcut assertions (actual shortcut code with input fixture).");return count;
 }
}
