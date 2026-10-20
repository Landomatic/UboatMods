// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
namespace Landomatic.TorpedoRoom
{
    internal sealed class TubeUiState
    {
        bool initialized,loaded,stable;int state;string identity;
        internal bool Observe(int nextState,string nextIdentity,bool nextLoaded,bool nextStable)
        {
            bool changed=!initialized||state!=nextState||identity!=nextIdentity||loaded!=nextLoaded||stable!=nextStable;
            initialized=true;state=nextState;identity=nextIdentity;loaded=nextLoaded;stable=nextStable;return changed;
        }
        // A draft belongs to one weapon, never the tube slot after replacement.
        // Keep same-weapon drafts through cancelled work and stable/busy transitions.
        internal static bool NeedsRebind(bool sameWeapon,Preparation draft,Preparation current)
        {return !sameWeapon||(draft==null)!=(current==null)||(draft!=null&&current!=null&&draft.Id!=current.Id);}
        internal static string ReadOnlyReason(bool editable,bool awaiting,bool valid,bool stable,bool permitted,bool synced,string state)
        {
            if(editable)return "";if(awaiting)return "AWAITING HOST";if(!valid)return "CONTENTS CHANGED";
            if(!permitted)return "OPPOSITE · READ ONLY";if(!synced)return "AWAITING HOST STATE";
            return stable?"READ ONLY":"BUSY: "+state;
        }
    }
}
