// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using UBOAT.Game.Scene.Camera;
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
        GameInput ordnanceReturnInput;int ordnanceClosedFrame;
        const string OrdnanceReturnOwner="Landomatic.TorpedoRoom.OrdnanceReturn";
        void ObserveOrdnance()
        {
            var ui=Get<StorageUI>();if(ui==null||!ui.IsOpened||ordnanceOwner==ui)return;
            if(ordnanceOwner!=null)ordnanceOwner.Closed-=OnOrdnanceClosed;
            ordnanceOwner=ui;ordnanceShip=Ship;ui.Closed+=OnOrdnanceClosed;
        }
        void OnOrdnanceClosed()
        {
            var ship=ordnanceShip;
            if(ordnanceOwner!=null)ordnanceOwner.Closed-=OnOrdnanceClosed;
            ordnanceOwner=null;ordnanceShip=null;
            if(Loading||!object.ReferenceEquals(ship,Ship))return;
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
            ordnanceOwner=null;ordnanceShip=null;ReleaseOrdnanceReturn();
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
            var group=CurrentGroup;var ui=Get<StorageUI>();
            if(group==null||group.TorpedoStorage==null||ui==null||Loading){Say("Manage Ordnance is unavailable at this station.");return;}
            DismissRoom();
            try {
                // Match OpenTorpedoStorageOnTubeAction.OnPreStart in Generic mode:
                // null character keeps maintenance/loading in native crew dispatch,
                // rather than replacing the current station operator's action.
                ui.Open(null,group.TorpedoStorage,StorageUIMode.Auto,null,null,null,false);
                ObserveOrdnance();
            } catch(System.Exception e){roomHandoff.Reset();Say("Could not open Manage Ordnance: "+e.Message);Debug.LogWarning("[TorpedoRoom] Manage Ordnance: "+e);}

        }
    }
}
