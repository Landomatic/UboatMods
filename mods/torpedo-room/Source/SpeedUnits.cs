// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using UBOAT.Game.Core;
using UBOAT.Game.Core.Data;
namespace Landomatic.TorpedoRoom
{
    // Presentation only. Stored settings remain m/s; the native TDC remains knots.
    internal static class SpeedUnits
    {
        static readonly FieldInfo Settings=AccessTools.Field(typeof(UnitsUtility),"userSettings");
        internal static float Factor {get{
            try{var settings=Settings==null?null:Settings.GetValue(null) as UserSettings;
                if(settings!=null&&(int)settings.GameplaySettings.units==0)return 3.6f;
            }catch{ }
            return SpeedValues.Knots;
        }}
        internal static string Suffix {get{return Factor==3.6f?"km/h":"kn";}}
        internal static string Format(float metresPerSecond)
        {return (metresPerSecond*Factor).ToString("0.0",CultureInfo.InvariantCulture)+" "+Suffix;}
    }
}
