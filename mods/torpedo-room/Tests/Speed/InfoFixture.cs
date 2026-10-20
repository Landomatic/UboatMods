// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
using System;
using Landomatic.TorpedoRoom;
using UBOAT.Game.Scene.Items;
public static class InfoChecks {
 static int count;static void Check(bool ok,string label){count++;if(!ok)throw new Exception(label);}
 public static int Run(){
 Check(TorpedoInfo.Format("T1",false,true,.5f,3600)=="T1","no HTT means type only");
 Check(TorpedoInfo.Format("T1",true,false,0,3600)=="T1\nMaintenance: required","maintenance status");
 Check(TorpedoInfo.Format("T1",true,true,.5f,3600).EndsWith("30 min remaining"),"remaining benefit uses live progress and duration");
 Check(TorpedoInfo.Format("T1",true,true,0,7200).EndsWith("2 h 0 min remaining"),"hours formatted");
 Check(TorpedoInfo.Format("T1",true,true,2,7200).EndsWith("0 min remaining"),"elapsed clamped");
 Check(TorpedoInfo.Format("T1",true,true,-1,7200).EndsWith("2 h 0 min remaining"),"negative progress clamped");
 Check(TorpedoInfo.Format("T1",true,true,float.NaN,3600)=="T1\nMaintained","invalid progress safe");
 Check(TorpedoInfo.Format("T1",true,true,0,0)=="T1\nMaintained","missing duration safe");
 Check(TorpedoInfo.Read(null)=="","empty tube no tooltip");
 Check(TorpedoInfo.Read(new StoredTorpedo())=="G7a T1","optional HTT missing type still works");
 Check(TorpedoInfo.Format(null,false,false,0,0)=="Torpedo","missing type fallback");
 Console.WriteLine("PASS: "+count+" minimal torpedo information assertions.");return count;
 }
}
