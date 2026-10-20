// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
using System;
using Landomatic.TorpedoRoom;
public static class TubeUiChecks
{
 static int count;static void Check(bool x,string name){count++;if(!x)throw new Exception(name);}
 public static int Run(){
  var a=new Preparation{Id="old"};var b=new Preparation{Id="new"};
  Check(!TubeUiState.NeedsRebind(true,a,a.Copy()),"same weapon preserves draft through busy/stable transition");
  Check(TubeUiState.NeedsRebind(false,a,b),"reload replacement invalidates old weapon draft");
  Check(TubeUiState.NeedsRebind(true,a,b),"same object with replaced preparation identity rebinds");
  Check(TubeUiState.NeedsRebind(false,a,null),"unloading clears old draft");
  Check(!TubeUiState.NeedsRebind(true,null,null),"empty slot remains empty");
  Check(TubeUiState.NeedsRebind(true,null,b),"new host record or completed loading restores editor");
  Check(TubeUiState.NeedsRebind(true,a,null),"missing host record cannot leave stale settings editable");
  var state=new TubeUiState();Check(state.Observe(2,"old",true,true),"initial state refreshes artwork");
  bool idle=true;for(int i=0;i<100;i++)idle&=!state.Observe(2,"old",true,true);Check(idle,"100 idle polls do not repeat refresh/log");
  Check(state.Observe(4,"old",true,false),"maintenance/unload transition refreshes");
  Check(state.Observe(2,"old",true,true),"cancel returns same weapon to stable with refresh");
  Check(state.Observe(0,null,false,false),"empty clears artwork");
  Check(state.Observe(2,"new",true,true),"replacement in same tube refreshes artwork");
  Check(TubeUiState.ReadOnlyReason(true,false,true,true,true,true,"Loaded")=="","restored stable tube editable");
  Check(TubeUiState.ReadOnlyReason(false,false,true,false,true,true,"Unloading")=="BUSY: Unloading","native busy reason visible");
  Check(TubeUiState.ReadOnlyReason(false,true,true,true,true,true,"Loaded")=="AWAITING HOST","pending request reason");
  Check(TubeUiState.ReadOnlyReason(false,false,false,true,true,true,"Loaded")=="CONTENTS CHANGED","identity reason");
  Check(TubeUiState.ReadOnlyReason(false,false,true,true,false,true,"Loaded").Contains("OPPOSITE"),"compartment permissions retained");
  Check(TubeUiState.ReadOnlyReason(false,false,true,true,true,false,"Loaded")=="AWAITING HOST STATE","network reason");
  Console.WriteLine("PASS: "+count+" tube UI transition assertions.");return count;
 }
}
