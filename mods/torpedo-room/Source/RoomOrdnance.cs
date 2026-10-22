// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene;
using UBOAT.Game.Scene.Characters;
using UBOAT.Game.Scene.Characters.Actions;
using UBOAT.Game.Scene.Items;
using HarmonyLib;
using UBOAT.Game.Core.Input;
using UBOAT.Game.UI.Storage;
using UnityEngine;
namespace Landomatic.TorpedoRoom
{
    public sealed partial class Runtime
    {
        readonly RoomHandoff roomHandoff=new RoomHandoff();
        object RoomStationToken {get{var camera=MainCamera.Instance;return camera==null||camera.GenericController==null?null:camera.GenericController.ExternalController;}}
        StorageUI ordnanceOwner;object ordnanceShip;
        PlayableCharacter ordnanceActor;TorpedoLauncherGroup ordnanceGroup;
        bool ordnanceWorkStarting,closeOrdnanceAfterOrder;
        GameInput ordnanceReturnInput;int ordnanceClosedFrame;
        const string OrdnanceReturnOwner="Landomatic.TorpedoRoom.OrdnanceReturn";
        void ObserveOrdnance()
        {
            var ui=Get<StorageUI>();if(ui==null||!ui.IsOpened||ordnanceOwner==ui)return;
            if(ordnanceOwner!=null)ordnanceOwner.Closed-=OnOrdnanceClosed;
            ordnanceOwner=ui;ordnanceShip=Ship;ordnanceActor=ui.Character as PlayableCharacter;ordnanceGroup=CurrentGroup;ordnanceWorkStarting=false;ui.Closed+=OnOrdnanceClosed;
        }
        void OnOrdnanceClosed()
        {
            var ship=ordnanceShip;bool workStarting=ordnanceWorkStarting;
            if(ordnanceOwner!=null)ordnanceOwner.Closed-=OnOrdnanceClosed;
            ordnanceOwner=null;ordnanceShip=null;ordnanceActor=null;ordnanceGroup=null;ordnanceWorkStarting=closeOrdnanceAfterOrder=false;
            if(Loading||!object.ReferenceEquals(ship,Ship))return;
            if(workStarting){NativePresentation.Room(false);return;}
            roomHandoff.Reset();nextRoomMask=0;nextGridRender=0;
            // Storage removes its blockade before raising Closed. Keep Escape
            // from reaching GenericCameraController on this same closing frame.
            ordnanceReturnInput=Get<GameInput>();ordnanceClosedFrame=Time.frameCount;
            if(ordnanceReturnInput!=null)ordnanceReturnInput.AddBlockade(OrdnanceReturnOwner);
        }
        void TickOrdnanceReturn()
        {
            if(ordnanceReturnInput!=null&&Time.frameCount>ordnanceClosedFrame+1&&!Input.GetKey(KeyCode.Escape))ReleaseOrdnanceReturn();
        }
        void ReleaseOrdnanceReturn()
        {
            if(ordnanceReturnInput!=null)ordnanceReturnInput.RemoveBlockade(OrdnanceReturnOwner);
            ordnanceReturnInput=null;
        }
        void CancelOrdnanceHandoff()
        {
            if(ordnanceOwner!=null)ordnanceOwner.Closed-=OnOrdnanceClosed;
            ordnanceOwner=null;ordnanceShip=null;ordnanceActor=null;ordnanceGroup=null;ordnanceWorkStarting=closeOrdnanceAfterOrder=false;ReleaseOrdnanceReturn();
        }
        internal void PrepareOrdnanceOrder(PlayableCharacter actor,CharacterAction action)
        {
            if(Loading||!Active||!Config.Interface||ordnanceOwner==null||!ordnanceOwner.IsOpened||!object.ReferenceEquals(ordnanceShip,Ship)||!object.ReferenceEquals(actor,ordnanceActor))return;
            if(!(action is LoadTorpedoesAction)&&!(action is WarmUpTorpedoAction))return;
            var camera=MainCamera.Instance;
            if(camera==null||camera.CurrentMode!=CameraMode.Generic||camera.GenericController==null||!object.ReferenceEquals(camera.GenericController.Character,actor)||CurrentGroup!=ordnanceGroup)return;
            // Inventory has already chosen the actor and the native command.
            // Stop only the manual camera view; StartAction below still owns all
            // task/animation/maintenance choices. Never CancelOrders here.
            ordnanceWorkStarting=closeOrdnanceAfterOrder=true;
            try {
                camera.SetFPPController(actor,CameraTransitionType.Instant,false,0);
                string order="Maintain torpedo";
                if(action is LoadTorpedoesAction){var field=AccessTools.Field(typeof(LoadTorpedoesAction),"warmTorpedo");order=field==null?"Load torpedo (native maintenance choice)":(bool)field.GetValue(action)?"Load and maintain":"Load torpedo";}
                Say("Native ordnance order: "+order+". Camera handed to the native task.");
            } catch(System.Exception e){Debug.LogWarning("[TorpedoRoom] Native ordnance camera handoff: "+e.Message);}
        }
        void FinishOrdnanceOrderFrame()
        {
            if(!closeOrdnanceAfterOrder)return;closeOrdnanceAfterOrder=false;
            // Maintenance can be dispatched from StorageUI.Update. Wait until
            // LateUpdate to close it rather than clear its widgets mid-method.
            if(ordnanceOwner!=null&&ordnanceOwner.IsOpened)ordnanceOwner.Close();
        }
        bool OrdnanceOpen {get{var ui=Get<StorageUI>();return ui!=null&&ui.IsOpened;}}
        void DismissRoom()
        {
            roomHandoff.Dismiss(CurrentGroup,RoomStationToken);CloseGrid();roomTakeover=false;
            NativeSkin.RestoreStations();NativePresentation.Room(Config.Interface&&Active);
            if(gridRoot!=null)gridRoot.SetActive(false);
        }
        void OpenOrdnance()
        {
            var group=CurrentGroup;var ui=Get<StorageUI>();var camera=MainCamera.Instance;
            var actor=camera==null||camera.GenericController==null?null:camera.GenericController.Character;
            if(!OrdnanceDispatch.Available){CleanupFeedback.Show("Manage Ordnance unavailable","The native order handoff could not be installed. Use the game's normal ordnance interaction; see output_log.txt.");return;}
            if(group==null||group.TorpedoStorage==null||ui==null||actor==null||Loading){Say("Manage Ordnance is unavailable at this station.");return;}
            DismissRoom();
            try {
                // Bind to the character already selected by the native station
                // switcher. This preserves native Load / Load and maintain choices.
                ui.Open(actor,group.TorpedoStorage,StorageUIMode.Auto,null,null,null,false);
                ObserveOrdnance();
            } catch(System.Exception e){roomHandoff.Reset();Say("Could not open Manage Ordnance: "+e.Message);Debug.LogWarning("[TorpedoRoom] Manage Ordnance: "+e);}

        }
    }
}
