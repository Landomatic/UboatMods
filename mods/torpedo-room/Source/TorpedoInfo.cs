// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using HarmonyLib;
using UBOAT.Game.Scene.Items;
namespace Landomatic.TorpedoRoom
{
    internal static class TorpedoInfo
    {
        static readonly bool Htt=AccessTools.TypeByName("UBOAT.Mods.HappyTorpedoTime.HappyTorpedoTimeRuntime")!=null;
        internal static string Format(string type,bool htt,bool maintained,float progress,float duration)
        {
            string text=string.IsNullOrEmpty(type)?"Torpedo":type;
            if(!htt)return text;
            if(!maintained)return text+"\nMaintenance: required";
            if(!Preparation.Finite(progress)||!Preparation.Finite(duration)||duration<=0)return text+"\nMaintained";
            double seconds=Math.Max(0,Math.Min(1,1-progress))*duration;
            long minutes=(long)Math.Ceiling(seconds/60);
            return text+"\nMaintenance benefit: "+(minutes>=60?minutes/60+" h "+minutes%60+" min":minutes+" min")+" remaining";
        }
        internal static string Read(StoredTorpedo item)
        {
            if(item==null)return "";
            try{return Format(item.TypeName,Htt,item.IsMaintained,item.MaintenanceCooldownProgress,item.MaintenanceCooldown.Value);}
            catch{return item.TypeName??"Torpedo";}
        }
    }
}
