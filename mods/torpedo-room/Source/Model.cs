// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.IO;
using System.Text;

namespace Landomatic.TorpedoRoom
{
    public enum RoomAccess { CurrentCompartment, OppositeReadOnly, AllEditable }
    public enum ViewportMode { Synchronized, ReadOnly, Hidden }
    public enum PistolPolicy { Free=0, LockedOnLoad=1, DryTubePreparation=3 }
    [Serializable]
    public sealed class Preparation
    {
        public string Id = Guid.NewGuid().ToString("N");
        public int Revision;
        public int Pistol;
        public int InitialPistol;
        public bool ProgramOverride;
        public bool DepthOverride;
        public float Depth = 4;
        public bool SpeedOverride;
        public float Speed; // metres per second; zero is an inherited legacy value
        public bool StraightRun;
        public bool Right;
        public float Distance = 1500;
        public float Angle;
        public Preparation Copy() { return (Preparation)MemberwiseClone(); }
        public bool Valid()
        {
            Guid id;
            return Guid.TryParseExact(Id, "N", out id) && Revision >= 0 && Pistol >= 0 && Pistol <= 1
                && Finite(Speed)&&Speed>=0&&Speed<=100&&(!SpeedOverride||Speed>0)
                && Finite(Depth) && Depth >= .5f && Depth <= 14 && InitialPistol >= 0 && InitialPistol <= 1 && Finite(Distance) && Distance >= 500 && Distance <= 15000
                && Finite(Angle) && Angle >= 0 && Angle < 360;
        }
        public static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
    }
    public static class Rules
    {
        public static bool ViewportEdit(Preparation current,Preparation edit)
        {return current!=null&&edit!=null&&edit.InitialPistol==current.InitialPistol&&edit.StraightRun==current.StraightRun&&edit.ProgramOverride==current.ProgramOverride;}
        public static bool SpeedOnlyEdit(Preparation current,Preparation edit)
        {if(current==null||edit==null)return false;var compare=edit.Copy();compare.Speed=current.Speed;compare.SpeedOverride=current.SpeedOverride;return ProgramValues.SamePreparation(current,compare);}
        public static string Check(Preparation current, Preparation edit, PistolPolicy policy, bool docked, bool dry, bool hasProgram, bool allowLockout=true)
        {
            if (current == null || edit == null || !edit.Valid()) return "Invalid settings.";
            if (current.Id != edit.Id) return "The torpedo in this tube changed. Refresh and retry.";
            if (current.Revision != edit.Revision) return "Another edit was accepted. Refresh and retry.";
            if (current.Revision == int.MaxValue) return "Settings revision limit reached.";
            if (!hasProgram && (edit.StraightRun || edit.ProgramOverride || edit.Right != current.Right || edit.Distance != current.Distance || edit.Angle != current.Angle))
                return "This torpedo has no FAT/LUT program.";
            if(edit.StraightRun && !allowLockout && !current.StraightRun) return "The host has disabled straight running.";
            if (edit.Pistol != current.Pistol)
            {
                if (policy == PistolPolicy.LockedOnLoad) return "Pistol mode is locked when first loaded.";
                if (policy == PistolPolicy.DryTubePreparation && !dry) return "Drain the tube before preparing its pistol mode.";
            }
            return null;
        }
    }
    // Shared viewport controls can change one field without erasing independent tube settings.
    public sealed class ProgramValues
    {
        public bool Right;
        public float Distance,Angle;
        public ProgramValues(bool right,float distance,float angle){Right=right;Distance=distance;Angle=angle;}
        public static bool Merge(Preparation edit,ProgramValues before,ProgramValues after)
        {
            bool changed=false;
            if(before.Right!=after.Right&&edit.Right!=after.Right){edit.Right=after.Right;changed=true;}
            if(before.Distance!=after.Distance&&Preparation.Finite(after.Distance)&&after.Distance>=500&&after.Distance<=15000&&edit.Distance!=after.Distance){edit.Distance=after.Distance;changed=true;}
            if(before.Angle!=after.Angle&&Preparation.Finite(after.Angle)&&after.Angle>=0&&after.Angle<360&&edit.Angle!=after.Angle){edit.Angle=after.Angle;changed=true;}
            return changed;
        }
        public static bool SamePreparation(Preparation a,Preparation b)
        {return a!=null&&b!=null&&a.Id==b.Id&&a.Revision==b.Revision&&a.Pistol==b.Pistol&&a.InitialPistol==b.InitialPistol&&a.DepthOverride==b.DepthOverride&&a.Depth==b.Depth&&a.SpeedOverride==b.SpeedOverride&&a.Speed==b.Speed&&a.ProgramOverride==b.ProgramOverride&&a.StraightRun==b.StraightRun&&a.Right==b.Right&&a.Distance==b.Distance&&a.Angle==b.Angle;}
        public static string WaterLabel(int state,bool flooded,float progress)
        {return state==8?"DRAINING "+(progress*100).ToString("0")+"%":state==5?"FLOODING "+(progress*100).ToString("0")+"%":flooded?"FLOODED":"DRY";}
        public static bool ShowFields(Preparation p,bool lockoutAllowed){return p!=null&&!(lockoutAllowed&&p.StraightRun);}
        public static bool DoorClosed(bool flooded,int state){return flooded||state==5||state==7;}
        public static string PolicyLabel(PistolPolicy p)
        {return p==PistolPolicy.Free?"Pistol: unrestricted AZ/MZ changes":p==PistolPolicy.LockedOnLoad?"Pistol: mode locked on load":"Pistol: dry-tube preparation";}
    }
    public static class DialValues
    {
        public static float Snap(float value,float min,float max,float step)
        {if(!Preparation.Finite(value))return min;return (float)Math.Max(min,Math.Min(max,Math.Round(value/step,MidpointRounding.AwayFromZero)*step));}
        public static float AtAngle(float angle,float min,float max,float step)
        {return Snap(min+(Math.Max(-90,Math.Min(90,angle))+90)/180*(max-min),min,max,step);}
        public static float Compass(float angle,float step)
        {if(!Preparation.Finite(angle))return 0;return (Snap((angle%360+360)%360,0,360,step)%360);}
        public static float Nudge(float value,float ticks,float min,float max,float step)
        {return Snap(value+Math.Sign(ticks)*step,min,max,step);}
    }
    public static class SpeedValues
    {
        public const float Knots=1.94384444f;
        public static float[] Options(params float[] values)
        {var list=new System.Collections.Generic.List<float>();foreach(var value in values){if(!Preparation.Finite(value)||value<=0||value>100)continue;bool duplicate=false;foreach(var old in list)if(Math.Abs(old-value)<.01f)duplicate=true;if(!duplicate)list.Add(value);}list.Sort();return list.ToArray();}
        public static bool Supports(float[] options,float value)
        {if(!Preparation.Finite(value)||options==null)return false;foreach(var option in options)if(Math.Abs(option-value)<.02f)return true;return false;}
        public static float Nearest(float[] options,float value)
        {if(options==null||options.Length==0)return 0;float best=options[0];foreach(var option in options)if(Math.Abs(option-value)<Math.Abs(best-value))best=option;return best;}
        public static float Nudge(float[] options,float value,float ticks)
        {if(options==null||options.Length==0)return value;float nearest=Nearest(options,value);int index=Array.IndexOf(options,nearest);return options[Math.Max(0,Math.Min(options.Length-1,index+Math.Sign(ticks)))];}
    }
    public static class ReportText
    {
        public static string Tube(bool programmed,bool lut,bool straight,bool right,float distance,float angle,float depth)
        {
            string d=depth.ToString("0.0#",System.Globalization.CultureInfo.InvariantCulture)+" m";
            if(!programmed)return d;if(straight)return "STRAIGHT · "+d;
            return (lut?"LUT":"FAT")+" "+distance.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+" m "+(right?"R":"L")+(lut?" · "+angle.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"°T":"")+"\n"+d;
        }
    }
    public sealed class TubeSnapshot
    {
        public int Index;
        public string Type;
        public int State;
        public bool Flooded;
        public float Flooding;
        public Preparation Settings;
    }
    public sealed class Snapshot
    {
        public string Epoch;
        public bool Enabled;
        public PistolPolicy Policy;
        public float Delay;
        public bool AllowLockout=true;
        public bool SpeedSync=true;
        public RoomAccess Access;
        public ViewportMode Viewport=ViewportMode.ReadOnly;
        public string Notice = "";
        public float SolutionSpeed; // m/s; zero when no consistent selected solution
        public int SpeedTube=-1; // last committed room speed, used only without marked tubes
        public TubeSnapshot[] Tubes = new TubeSnapshot[0];
    }
    public static class Wire
    {
        public const byte Hello = 1, State = 2, Edit = 3, Viewport = 4;
        const int Magic = 0x37435254;
        public static byte[] Pack(byte operation, Action<BinaryWriter> body)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            { w.Write(Magic); w.Write(operation); body(w); w.Flush(); return stream.ToArray(); }
        }
        public static BinaryReader Open(byte[] bytes, out byte operation)
        {
            if (bytes == null || bytes.Length < 5 || bytes.Length > 8192) throw new FormatException("Bad packet length.");
            var r = new BinaryReader(new MemoryStream(bytes, false), Encoding.UTF8);
            if (r.ReadInt32() != Magic) { r.Dispose(); throw new FormatException("Unknown companion protocol."); }
            operation = r.ReadByte();
            return r;
        }
        public static void Finish(BinaryReader r) { if (r.BaseStream.Position != r.BaseStream.Length) throw new FormatException("Trailing packet data."); }
        public static void Write(BinaryWriter w, Preparation p)
        { w.Write(p.Id); w.Write(p.Revision); w.Write((byte)p.Pistol); w.Write((byte)p.InitialPistol); w.Write(p.DepthOverride); w.Write(p.Depth);w.Write(p.SpeedOverride);w.Write(p.Speed); w.Write(p.ProgramOverride); w.Write(p.StraightRun); w.Write(p.Right); w.Write(p.Distance); w.Write(p.Angle); }
        public static Preparation Read(BinaryReader r)
        {
            var p = new Preparation { Id = r.ReadString(), Revision = r.ReadInt32(), Pistol = r.ReadByte(), InitialPistol = r.ReadByte(),
                DepthOverride=r.ReadBoolean(), Depth=r.ReadSingle(),SpeedOverride=r.ReadBoolean(),Speed=r.ReadSingle(), ProgramOverride = r.ReadBoolean(), StraightRun=r.ReadBoolean(), Right = r.ReadBoolean(), Distance = r.ReadSingle(), Angle = r.ReadSingle() };
            if (!p.Valid()) throw new FormatException("Invalid torpedo settings.");
            return p;
        }
        public static byte[] Encode(Snapshot s)
        {
            return Pack(State, w => {
                w.Write(s.Epoch); w.Write(s.Enabled); w.Write((byte)s.Policy); w.Write(s.Delay); w.Write(s.AllowLockout);w.Write(s.SpeedSync); w.Write((byte)s.Access); w.Write((byte)s.Viewport); w.Write(s.Notice ?? ""); w.Write(s.SolutionSpeed);w.Write(s.SpeedTube);
                w.Write((byte)s.Tubes.Length);
                foreach (var t in s.Tubes) { w.Write((byte)t.Index); w.Write(t.Type);w.Write((byte)t.State);w.Write(t.Flooded);w.Write(t.Flooding); Write(w, t.Settings); }
            });
        }
        public static Snapshot Decode(BinaryReader r)
        {
            var s = new Snapshot { Epoch = r.ReadString(), Enabled = r.ReadBoolean(), Policy = (PistolPolicy)r.ReadByte(), Delay = r.ReadSingle(), AllowLockout=r.ReadBoolean(),SpeedSync=r.ReadBoolean(), Access=(RoomAccess)r.ReadByte(), Viewport=(ViewportMode)r.ReadByte(), Notice = r.ReadString(),SolutionSpeed=r.ReadSingle(),SpeedTube=r.ReadInt32() };
            Guid epoch;
            if (!Guid.TryParseExact(s.Epoch, "N", out epoch) || (int)s.Access>2 || (int)s.Viewport>2 || ((int)s.Policy!=0&&(int)s.Policy!=1&&(int)s.Policy!=3) || !Preparation.Finite(s.Delay) || s.Delay < 0 || s.Delay > 120 || s.Notice.Length > 512 || !Preparation.Finite(s.SolutionSpeed) || s.SolutionSpeed<0 || s.SolutionSpeed>100 || s.SpeedTube< -1 || s.SpeedTube>15)
                throw new FormatException("Invalid host settings.");
            int count = r.ReadByte();
            if (count > 16) throw new FormatException("Too many tubes.");
            s.Tubes = new TubeSnapshot[count]; int seen = 0;
            for (int i=0; i<count; i++) {
                int index = r.ReadByte(); string type = r.ReadString();int state=r.ReadByte();bool flooded=r.ReadBoolean();float flooding=r.ReadSingle();
                if(state>8||!Preparation.Finite(flooding)||flooding<0||flooding>1)throw new FormatException("Invalid live tube state.");
                if (index >= 16 || (seen & (1 << index)) != 0 || type.Length > 256) throw new FormatException("Invalid tube.");
                seen |= 1 << index;
                s.Tubes[i] = new TubeSnapshot { Index=index, Type=type,State=state,Flooded=flooded,Flooding=flooding, Settings=Read(r) };
            }
            Finish(r); return s;
        }
    }
}
