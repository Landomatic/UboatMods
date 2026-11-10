// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
using System;
using Landomatic.TorpedoRoom;
public static class UiAffordanceChecks
{
    static int count;
    static void Check(bool b,string name){count++;if(!b)throw new Exception(name);}
    static Snapshot Decode(byte[] data){byte op;using(var reader=Wire.Open(data,out op)){Check(op==Wire.State,"state operation");return Wire.Decode(reader);}}
    static void Refused(byte[] data){bool bad=false;try{Decode(data);}catch(FormatException){bad=true;}catch(System.IO.EndOfStreamException){bad=true;}Check(bad,"malformed/incompatible progress refused");}
    public static int Run()
    {
        var state=new Snapshot{Epoch=Guid.NewGuid().ToString("N"),Delay=30,Tubes=new[]{new TubeSnapshot{Index=0,Type="T3",Settings=new Preparation()}}};
        foreach(float seconds in new[]{0f,.01f,1f,29.5f,30f,120f}){
            state.Tubes[0].PistolSeconds=seconds;var result=Decode(Wire.Encode(state));
            Check(result.Tubes[0].PistolSeconds==seconds,"host progress round trip");
            Check(result.Tubes[0].Settings.Pistol==0,"progress cannot change committed pistol");
            Check(state.Tubes[0].PistolSeconds==seconds,"reading/refresh cannot mutate host progress");
        }
        foreach(float bad in new[]{-1f,121f,float.NaN,float.PositiveInfinity}){state.Tubes[0].PistolSeconds=bad;Refused(Wire.Encode(state));}
        state.Tubes[0].PistolSeconds=30;var packet=Wire.Encode(state);
        var old=(byte[])packet.Clone();old[3]=(byte)'9';Refused(old);
        for(int i=5;i<packet.Length;i+=7){var truncated=new byte[i];Array.Copy(packet,truncated,i);Refused(truncated);}
        var extra=new byte[packet.Length+1];Array.Copy(packet,extra,packet.Length);Refused(extra);
        Console.WriteLine("PASS: "+count+" preparation protocol assertions; no live network.");return count;
    }
}
