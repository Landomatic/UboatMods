// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Collections.Generic;
using System.Reflection;
using DWS.Common.InjectionFramework;
using HarmonyLib;
using UBOAT.Game;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Scene;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Camera.DeviceControllers;
using UBOAT.Game.Scene.Camera.ExternalControllers;
using UBOAT.Game.Scene.Entities;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Scene.Items.Statuses;
using UnityEngine;

namespace Landomatic.TorpedoRoom
{
    // Equipment statuses are already written/read by Equipment.CustomSerialization.
    // This status adds no modifiers and remains attached to its particular torpedo.
    [Serializable]
    public sealed class PreparationStatus : EquipmentStatus
    {
        public Preparation Value = new Preparation();
    }
    [NonSerializedInGameState]
    public sealed class Loader : IUserMod
    {
        static bool loaded;
        public void OnLoaded()
        {
            if(loaded)return;loaded=true;
            Harmony harmony=null;
            try {
                Config.Load();
                harmony=new Harmony("com.landomatic.torpedoroom");
                LaunchAuthority.Install(harmony);
                harmony.Patch(AccessTools.PropertySetter(typeof(Torpedo),"PistolSetting"),new HarmonyMethod(typeof(Runtime),"BeforePistol"));
                // Capture identity/baseline on completed loading, without changing loading tasks.
                harmony.Patch(AccessTools.Method(typeof(TorpedoLauncher),"CompleteLoading"),postfix:new HarmonyMethod(typeof(Runtime),"Loaded"));
                var go=new GameObject("Landomatic.TorpedoRoom.Runtime");
                UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<Runtime>();
                Bridge.Install(harmony);
                StockUi.Install(harmony);
                ViewingFire.Install(harmony);DepthControl.Install(harmony);SpeedControl.Install(harmony);TdcSpeed.Install(harmony);
                Debug.Log("[TorpedoRoom] 0.1.22-beta loaded. Feuer-only Hidden Reports; T5 viewing depth exception through 50 m; running depth 1–14 m in quarter metres; TRC9 host rules; optional MP restricted-scope Feuer. T opens editor (cursor Alt allowed). No flooding or firing controls added.");
            } catch(Exception e){
                Config.Enabled=false;
                if(harmony!=null)harmony.UnpatchAll("com.landomatic.torpedoroom");
                if(Runtime.Instance!=null)UnityEngine.Object.Destroy(Runtime.Instance.gameObject);
                Debug.LogError("[TorpedoRoom] Initialization failed; this companion is disabled.");Debug.LogException(e);
            }
        }
    }
    [NonSerializedInGameState]
    public sealed partial class Runtime : MonoBehaviour
    {
        internal static Runtime Instance;
        internal static int Applying;
        internal static string Notice="Use the configured shortcut for preparation. Physical levers still flood, drain and fire.";
        internal static string Epoch=Guid.NewGuid().ToString("N");
        PlayerShip lastShip;
        float nextScan, nextError;
        static readonly Dictionary<StoredTorpedo,Pending> pending=new Dictionary<StoredTorpedo,Pending>();
        sealed class Pending
        {
            public Preparation Edit;
            public long Due;
            public object Peer;
            public TorpedoLauncherGroup Group;
            public int Index;
        }
        internal static T Get<T>() where T:class
        { try{var f=InjectionFramework.Instance;return f==null?null:f.GetInstance<T>();}catch{return null;} }
        internal static PlayerShip Ship { get { return Get<PlayerShip>(); } }
        internal static bool Loading { get { var sm=Get<SavesManager>();return sm==null||sm.IsLoading||sm.IsSaving||sm.IsActivelyDeserializing; } }
        internal static bool LockoutEnabled {get{return Bridge.IsRemote?Bridge.Last!=null&&Bridge.Last.AllowLockout:Config.AllowLockout;}}
        internal static RoomAccess Access {get{return Bridge.IsRemote?Bridge.Last==null?RoomAccess.CurrentCompartment:Bridge.Last.Access:Config.Access;}}
        internal static ViewportMode Viewport {get{return Bridge.IsRemote?Bridge.Last==null?ViewportMode.ReadOnly:Bridge.Last.Viewport:Config.Viewport;}}
        internal static bool RoomEditable(TorpedoLauncherGroup group){return CurrentGroup!=null&&(CurrentGroup==group||Access==RoomAccess.AllEditable);}
        internal static bool Active { get { return Bridge.IsRemote ? Bridge.Last!=null&&Bridge.Last.Enabled : Config.Enabled; } }
        internal static bool MpScopeFeuer {get{return Bridge.IsRemote?Bridge.Last!=null&&Bridge.Last.MpScopeFeuer:Config.MpScopeFeuer;}}
        internal static object ViewingScope {get{var c=MainCamera.Instance;var scope=c!=null&&c.CurrentMode==CameraMode.Generic&&c.GenericController!=null?c.GenericController.ExternalController as PeriscopeController:null;return scope==null?null:scope.Periscope;}}
        internal static bool AimContext {get{var s=Ship;return s!=null&&(s.TorpedoesPeriscopeTargetter!=null||s.TorpedoesTarget!=null);}}
        internal static bool LaunchBusy {get{var s=Ship;return s==null||s.IsPreparingToLaunchTorpedoes||s.IsLaunchingTorpedoes;}}
        internal static bool SpeedSync {get{return Bridge.IsRemote?Bridge.Last!=null&&Bridge.Last.SpeedSync:Config.SpeedSync;}}
        internal static bool ViewingStation
        {get{var c=MainCamera.Instance;var e=c!=null&&c.CurrentMode==CameraMode.Generic&&c.GenericController!=null?c.GenericController.ExternalController:null;return e is PeriscopeController||e is TorpedoCalculatorController||e is TorpedoFireControlBoxesController;}}
        internal static TorpedoLauncherGroup CurrentGroup
        {
            get {
                var camera=MainCamera.Instance;
                if(camera==null || camera.CurrentMode!=CameraMode.Generic || camera.GenericController==null)return null;
                var c=camera.GenericController.ExternalController as TorpedoLauncherController;
                return c==null?null:c.TorpedoLauncherGroup;
            }
        }
        internal static bool Dry(TorpedoLauncher t) { return t.CurrentState==TorpedoLauncher.State.Loaded || t.CurrentState==TorpedoLauncher.State.Preheating; }
        internal static bool Stable(TorpedoLauncher t) { return Dry(t)||t.CurrentState==TorpedoLauncher.State.Flooded; }
        internal static Torpedo Missile(StoredTorpedo item) { return item==null?null:item.GetComponent<Torpedo>(); }
        internal static PreparationStatus Status(StoredTorpedo item,bool create)
        {
            if(item==null || item is StoredMinePacket)return null;
            if(item.Statuses!=null)foreach(var s in item.Statuses){var p=s as PreparationStatus;if(p!=null){
                if(p.Value!=null&&!Bridge.IsRemote&&(!Preparation.Finite(p.Value.Depth)||p.Value.Depth<1||p.Value.Depth>14)){var boat=Ship;p.Value.Depth=p.Value.DepthOverride&&Preparation.Finite(p.Value.Depth)?Mathf.Clamp(p.Value.Depth,1,14):boat==null?4:Mathf.Clamp(boat.TorpedoAttackSettings.TorpedoDepth,1,14);if(p.Value.Revision<int.MaxValue)p.Value.Revision++;}
                return p;}}
            if(!create)return null;
            var torp=Missile(item);if(torp==null)return null;
            var ship=Ship;
            var made=new PreparationStatus { Value=new Preparation { Pistol=(int)torp.PistolSetting,InitialPistol=(int)torp.PistolSetting,ProgramOverride=torp.ProgramType!=Torpedo.TorpedoProgramType.None,
                Depth=ship==null?4:Mathf.Clamp(ship.TorpedoAttackSettings.TorpedoDepth,1,14),
                Right=ship!=null&&ship.TorpedoProgramDirection,Distance=ship==null?1500:Mathf.Clamp(ship.TorpedoProgramDistance,500,15000),
                Angle=ship==null?0:Mathf.Repeat(ship.TorpedoProgramAngleAbsolute,360) } };
            item.AddStatus(made);return made;
        }
        public static void Loaded(TorpedoLauncher __instance)
        { if(!Loading && !Bridge.IsRemote && Active && __instance.Group!=null && __instance.Group.ParentEntity==Ship)Status(__instance.Torpedo,true); }
        internal static int IndexOf(Torpedo missile)
        {
            var ship=Ship;if(ship==null||ship.TorpedoLaunchers==null)return -1;
            for(int i=0;i<ship.TorpedoLaunchers.Length;i++)if(ship.TorpedoLaunchers[i]!=null && Missile(ship.TorpedoLaunchers[i].Torpedo)==missile)return i;
            return -1;
        }
        internal static void Say(string text){Notice=text;Debug.Log("[TorpedoRoom] "+text);}
        internal static void RemoveRecords()
        {
            if(Bridge.IsRemote){Say("Only the host can remove saved preparation records.");return;}
            Config.Enabled=false;pending.Clear();
            foreach(var item in Resources.FindObjectsOfTypeAll<StoredTorpedo>()){
                var status=Status(item,false);if(status!=null)item.RemoveStatus(status);
            }
            Say("Preparation disabled and records removed. Save separately before uninstalling this test mod.");
        }
        internal static bool MarkedHasProgram()
        {var ship=Ship;if(ship==null||ship.TorpedoLaunchers==null)return false;foreach(var tube in ship.TorpedoLaunchers)if(tube!=null&&tube.IsMarkedForLaunch){var missile=Missile(tube.Torpedo);if(missile!=null&&missile.ProgramType!=Torpedo.TorpedoProgramType.None)return true;}return false;}
        internal static string TubeWaterText(TorpedoLauncher tube)
        {
            if(Bridge.IsRemote&&Bridge.Synced&&Ship!=null){
                foreach(var row in Bridge.Last.Tubes)if(row.Index<Ship.TorpedoLaunchers.Length&&Ship.TorpedoLaunchers[row.Index]==tube)
                    return ProgramValues.WaterLabel(row.State,row.Flooded,row.Flooding);
            }
            return ProgramValues.WaterLabel((int)tube.CurrentState,tube.IsFlooded,tube.FloodingProgress);
        }
        void Awake(){Instance=this;}
        double perfTotal,perfMax;
        int perfSamples,perfGc;
        float perfNext,perfFrameMax;
        void RecordTiming(double start)
        {
            if(double.IsNaN(start))return;
            double elapsed=(Time.realtimeSinceStartupAsDouble-start)*1000d;perfTotal+=elapsed;if(elapsed>perfMax)perfMax=elapsed;perfSamples++;
            perfFrameMax=Mathf.Max(perfFrameMax,Time.unscaledDeltaTime*1000);
            if(Time.unscaledTime<perfNext)return;
            if(perfNext>0){
                Debug.Log("[TorpedoRoom] PERF "+(lastShip==null?"menu/no boat":"in game")+" samples="+perfSamples+" updateAvgMs="+(perfTotal/perfSamples).ToString("0.000")+" updateMaxMs="+perfMax.ToString("0.000")+" frameMaxMs="+perfFrameMax.ToString("0.0")+" processGC0="+(GC.CollectionCount(0)-perfGc));
            }
            perfNext=Time.unscaledTime+10;perfTotal=perfMax=0;perfSamples=0;perfFrameMax=0;perfGc=GC.CollectionCount(0);
        }
        void Update()
        {
            double tickStart=Config.PerformanceTrace?Time.realtimeSinceStartupAsDouble:double.NaN;
            try {
                Bridge.Tick();MpScopeFire.Tick();MenuIntegration.Tick();DepthControl.RestoreSteps();SpeedControl.RestoreBounds();
                var ship=Ship;
                if(ship!=lastShip){NativePresentation.Room(false);roomTakeover=false;nextRoomMask=0;lastShip=ship;pending.Clear();Epoch=Guid.NewGuid().ToString("N");ResetGrid();Bridge.ResetWorld();LaunchAuthority.Clear();TdcSpeed.Reset();}
                if(Loading||ship==null){NativeSkin.RestoreStations();NativePresentation.Room(false);roomTakeover=false;CloseGrid();if(gridRoot!=null)gridRoot.SetActive(false);LaunchAuthority.Clear();if(ship==null)TdcSpeed.Reset();return;}
                UpdateGrid();
                if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+0.2f;
                LaunchAuthority.Tick();
                if(!Active){pending.Clear();return;}
                if(Bridge.IsRemote){Bridge.ApplySnapshot();TdcSpeed.Sync();return;}
                foreach(var tube in ship.TorpedoLaunchers)if(tube!=null && tube.Torpedo!=null && tube.IsLoaded)Status(tube.Torpedo,true);
                TickPending(ship);TdcSpeed.Sync();
            } catch(Exception e){
                CloseGrid();
                if(Time.unscaledTime>=nextError){nextError=Time.unscaledTime+10;Debug.LogError("[TorpedoRoom] Runtime: "+e);}
            } finally {RecordTiming(tickStart);}
        }
        internal static bool Request(int index,Preparation edit)
        {
            if(Bridge.IsRemote)return Bridge.Request(index,edit);
            return Accept(index,edit,null);
        }
        internal static bool Accept(int index,Preparation edit,object peer)
        {
            var ship=Ship;
            if(Loading||!Config.Enabled||ship==null||ship.TorpedoLaunchers==null||index<0||index>=ship.TorpedoLaunchers.Length){Reply(peer,"Preparation is unavailable.");return false;}
            var tube=ship.TorpedoLaunchers[index];
            if(tube==null||tube.Torpedo==null||!Stable(tube)){Reply(peer,"Wait until loading/flooding/draining has completed.");return false;}
            if(peer==null&&!RoomEditable(tube.Group)){Reply(peer,"Use an allowed tube compartment station.");return false;}
            if(peer!=null && !Bridge.Allowed(peer,tube.Group)){Reply(peer,"Use this tube compartment's station with your assigned officer.");return false;}
            var status=Status(tube.Torpedo,true);var missile=Missile(tube.Torpedo);
            if(status==null||missile==null)return false;
            string why=Rules.Check(status.Value,edit,Config.Policy,ship.Docked,Dry(tube),missile.ProgramType!=Torpedo.TorpedoProgramType.None,Config.AllowLockout);
            if(why!=null){Reply(peer,why);return false;}
            if(edit.SpeedOverride&&!SpeedValues.Supports(SpeedControl.Options(tube.Torpedo),edit.Speed)){Reply(peer,"Choose a speed supported by this torpedo.");return false;}
            if(pending.ContainsKey(tube.Torpedo)){Reply(peer,"Pistol preparation is already in progress. Drain/leave station to cancel.");return false;}
            bool pistolChanged=edit.Pistol!=status.Value.Pistol;
            if(pistolChanged && Config.Policy==PistolPolicy.DryTubePreparation && Config.Delay>0){
                if(peer==null && !RoomEditable(tube.Group)){Reply(peer,"Use this tube compartment's station for pistol preparation.");return false;}
                var time=Get<UBOAT.Game.Core.Time.GameTime>();if(time==null)return false;
                // FAT/LUT edits take effect immediately; only the pistol operation waits.
                var program=edit.Copy();program.Pistol=status.Value.Pistol;
                Commit(tube,status,program,true);
                var prepared=program.Copy();prepared.Pistol=edit.Pistol;prepared.Revision=status.Value.Revision;
                pending[tube.Torpedo]=new Pending { Edit=prepared,Due=time.StoryTicks+(long)(Config.Delay*TimeSpan.TicksPerSecond),Peer=peer,Group=tube.Group,Index=index };
                Reply(peer,"Pistol preparation started ("+Config.Delay+" game seconds). Stay at this dry tube station.");return true;
            }
            Commit(tube,status,edit,true);Reply(peer,"Tube "+Roman(index)+" settings accepted.");return true;
        }
        internal static bool AcceptViewport(TubeSnapshot[] edits,object peer,bool tdc=false)
        {
            var ship=Ship;if(Loading||!Config.Enabled||ship==null||ship.TorpedoLaunchers==null||edits==null||edits.Length==0||edits.Length>16||(Config.Viewport!=ViewportMode.Synchronized&&!tdc))return false;
            if(peer!=null&&!Bridge.AllowedViewport(peer)){Bridge.SendState(peer,"Use an occupied, assigned observation/TDC station for viewport edits.");return false;}
            int seen=0;
            foreach(var row in edits){
                if(row.Index<0||row.Index>=16||row.Index>=ship.TorpedoLaunchers.Length||(seen&(1<<row.Index))!=0)return false;seen|=1<<row.Index;
                var tube=ship.TorpedoLaunchers[row.Index];if(tube==null||!Stable(tube))return false;
                var status=Status(tube.Torpedo,true);var torpedo=Missile(tube.Torpedo);
                if(status==null||torpedo==null)return false;
                var edit=row.Settings;var current=status.Value;
                if(!tube.IsMarkedForLaunch&&edit.Pistol==current.Pistol&&!(tdc&&peer==null&&TdcSpeed.IsFocus(tube)))return false;
                if(!Rules.ViewportEdit(current,edit)||(tdc&&!Rules.SpeedOnlyEdit(current,edit)))return false;
                if(edit.SpeedOverride&&!SpeedValues.Supports(SpeedControl.Options(tube.Torpedo),edit.Speed))return false;
                if(edit.Pistol!=current.Pistol&&Config.Policy==PistolPolicy.DryTubePreparation&&Config.Delay>0)return false;
                if(Rules.Check(current,edit,Config.Policy,ship.Docked,Dry(tube),torpedo.ProgramType!=Torpedo.TorpedoProgramType.None,Config.AllowLockout)!=null||pending.ContainsKey(tube.Torpedo))return false;
            }
            foreach(var row in edits){var tube=ship.TorpedoLaunchers[row.Index];Commit(tube,Status(tube.Torpedo,true),row.Settings);}
            Reply(peer,"Viewport program changes applied to "+edits.Length+" selected tube(s).");return true;
        }
        static void Commit(TorpedoLauncher tube,PreparationStatus status,Preparation edit,bool focusSpeed=false)
        {
            var next=edit.Copy();next.InitialPistol=status.Value.InitialPistol;next.Revision=status.Value.Revision+1;
            // Quantize newly selected depths on the host as well as in the UI.
            // Unrelated edits do not rewrite a legacy save's unchanged depth.
            if(next.DepthOverride&&(!status.Value.DepthOverride||next.Depth!=status.Value.Depth))next.Depth=DialValues.Snap(next.Depth,1,14,.25f);
            status.Value=next;var missile=Missile(tube.Torpedo);if(missile!=null&&(int)missile.PistolSetting!=next.Pistol)SetPistol(missile,next.Pistol);
            if(focusSpeed&&next.SpeedOverride)TdcSpeed.Applied(tube);else TdcSpeed.Sync();
        }
        internal static void SetPistol(Torpedo missile,int mode)
        { if(missile==null)return;Applying++;try{missile.PistolSetting=(Torpedo.TorpedoPistolSetting)mode;}finally{Applying--;} }
        static void Reply(object peer,string text){if(peer==null)Say(text);else Bridge.SendState(peer,text);}
        static void TickPending(PlayerShip ship)
        {
            if(pending.Count==0)return;
            var time=Get<UBOAT.Game.Core.Time.GameTime>();if(time==null)return;
            var remove=new List<StoredTorpedo>();
            foreach(var pair in pending){
                var p=pair.Value;var tube=p.Index<ship.TorpedoLaunchers.Length?ship.TorpedoLaunchers[p.Index]:null;
                bool cancel=Config.Policy!=PistolPolicy.DryTubePreparation||tube==null||tube.Torpedo!=pair.Key||!Dry(tube)
                    ||(p.Peer==null?!RoomEditable(p.Group):!Bridge.Allowed(p.Peer,p.Group));
                var status=Status(pair.Key,false);
                cancel|=status==null||status.Value.Id!=p.Edit.Id||status.Value.Revision!=p.Edit.Revision;
                if(cancel){remove.Add(pair.Key);Reply(p.Peer,"Pistol preparation cancelled; existing mode retained.");continue;}
                if(time.StoryTicks<p.Due)continue;
                Commit(tube,status,p.Edit);remove.Add(pair.Key);Reply(p.Peer,"Tube "+Roman(p.Index)+" pistol preparation complete.");
            }
            foreach(var item in remove)pending.Remove(item);
        }
        public static bool BeforePistol(Torpedo __instance,Torpedo.TorpedoPistolSetting value)
        {
            if(Applying>0||Loading||!Active)return true;
            int index=IndexOf(__instance);if(index<0)return true;
            if((int)value==(int)__instance.PistolSetting)return true;
            var tube=Ship.TorpedoLaunchers[index];var status=Status(tube.Torpedo,!Bridge.IsRemote);
            if(status==null){Say("Waiting for host torpedo settings.");return false;}
            var edit=status.Value.Copy();edit.Pistol=(int)value;
            if(CurrentGroup==null){
                if(Viewport!=ViewportMode.Synchronized)return false;
                if((Bridge.IsRemote?Bridge.Last.Policy:Config.Policy)==PistolPolicy.DryTubePreparation&&(Bridge.IsRemote?Bridge.Last.Delay:Config.Delay)>0){Say("Use the torpedo room for timed pistol preparation.");return false;}
                var rows=new[]{new TubeSnapshot{Index=index,Settings=edit}};if(Bridge.IsRemote)Bridge.RequestViewport(rows);else AcceptViewport(rows,null);
            }else Request(index,edit);return false;
        }
        internal static Snapshot Snapshot(string message)
        {
            var ship=Ship;var rows=new List<TubeSnapshot>();
            if(Config.Enabled&&ship!=null&&!Loading&&ship.TorpedoLaunchers!=null)for(int i=0;i<Math.Min(16,ship.TorpedoLaunchers.Length);i++){
                var tube=ship.TorpedoLaunchers[i];if(tube==null||tube.Torpedo==null||!tube.IsLoaded)continue;
                var status=Status(tube.Torpedo,true);if(status==null)continue;
                rows.Add(new TubeSnapshot {Index=i,Type=tube.Torpedo.TypeName,State=(int)tube.CurrentState,Flooded=tube.IsFlooded,Flooding=Mathf.Clamp01(tube.FloodingProgress),Settings=status.Value.Copy()});
            }
            return new Snapshot {Epoch=Epoch,Enabled=Config.Enabled,Policy=Config.Policy,Delay=Config.Delay,AllowLockout=Config.AllowLockout,SpeedSync=Config.SpeedSync,MpScopeFeuer=Config.MpScopeFeuer,Access=Config.Access,Viewport=Config.Viewport,Notice=message??"",SolutionSpeed=TdcSpeed.ReportSpeed,SpeedTube=TdcSpeed.FocusIndex,Tubes=rows.ToArray()};
        }
        internal static string Roman(int index)
        { string[] names={"I","II","III","IV","V","VI","VII","VIII"};return index>=0&&index<names.Length?names[index]:(index+1).ToString(); }
        void OnDestroy(){MpScopeFire.Release();NativeSkin.RestoreStations();CloseGrid();NativePresentation.Room(false);NativePresentation.Restore();DepthControl.RestoreSteps(true);SpeedControl.RestoreBounds(true);if(gridRoot!=null)UnityEngine.Object.Destroy(gridRoot);if(Instance==this)Instance=null;}
    }
}

