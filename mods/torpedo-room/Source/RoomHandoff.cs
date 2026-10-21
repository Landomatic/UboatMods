// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
namespace Landomatic.TorpedoRoom
{
    // Inventory closes the editor. A fresh station visit can start a new room UI.
    internal sealed class RoomHandoff
    {
        internal bool Dismissed {get;private set;}
        object group,station;bool leftStation,returningFromStorage;
        internal void Dismiss(object currentGroup,object currentStation)
        {Dismissed=true;group=currentGroup;station=currentStation;leftStation=false;returningFromStorage=false;}
        internal void Reset(){Dismissed=false;group=station=null;leftStation=returningFromStorage=false;}
        internal bool CanShow(object currentGroup,object currentStation,bool storageOpen,bool explicitOpen)
        {
            if(storageOpen){returningFromStorage=true;return false;}
            if(explicitOpen&&currentGroup!=null){Reset();return true;}
            // Absorb camera/controller changes caused by closing inventory itself.
            // This return is not an explicit station entry and must not reopen Prep.
            if(returningFromStorage){returningFromStorage=false;if(object.ReferenceEquals(group,currentGroup))station=currentStation;}
            if(currentGroup==null){if(Dismissed)leftStation=true;return false;}
            if(Dismissed&&(leftStation||!object.ReferenceEquals(group,currentGroup)||!object.ReferenceEquals(station,currentStation)))Reset();
            return !Dismissed;
        }
    }
}
