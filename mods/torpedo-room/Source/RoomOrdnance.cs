// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.UI.Storage;
using UnityEngine;
namespace Landomatic.TorpedoRoom
{
    public sealed partial class Runtime
    {
        readonly RoomHandoff roomHandoff=new RoomHandoff();
        object RoomStationToken {get{var camera=MainCamera.Instance;return camera==null||camera.GenericController==null?null:camera.GenericController.ExternalController;}}
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
            var character=camera==null||camera.GenericController==null?null:camera.GenericController.Character;
            if(group==null||group.TorpedoStorage==null||character==null||ui==null||Loading){Say("Manage Ordnance is unavailable at this station.");return;}
            // Close fully BEFORE the native inventory acquires cursor/input ownership.
            // Closing inventory does not reopen this interface; T or a fresh station visit can.
            DismissRoom();
            try{ui.Open(character,group.TorpedoStorage,StorageUIMode.Auto,null,null,null,false);}
            catch(System.Exception e){Say("Could not open Manage Ordnance: "+e.Message);Debug.LogWarning("[TorpedoRoom] Manage Ordnance: "+e);}
        }
    }
}
