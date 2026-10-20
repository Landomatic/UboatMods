// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using UnityEngine;

namespace Landomatic.TorpedoRoom
{
    internal static class Shortcut
    {
        public static bool Allowed(KeyCode key)
        {
            if(!Enum.IsDefined(typeof(KeyCode),key)||key==KeyCode.None||key==KeyCode.Escape||key==KeyCode.F8||key==KeyCode.F9)return false;
            if((int)key>=(int)KeyCode.Mouse0)return false;
            return key!=KeyCode.LeftControl&&key!=KeyCode.RightControl&&key!=KeyCode.LeftAlt&&key!=KeyCode.RightAlt
                &&key!=KeyCode.LeftShift&&key!=KeyCode.RightShift&&key!=KeyCode.LeftCommand&&key!=KeyCode.RightCommand
                &&key!=KeyCode.LeftWindows&&key!=KeyCode.RightWindows&&key!=KeyCode.AltGr;
        }
        public static bool Matches(bool keyDown,bool ctrl,bool alt,bool shift,bool meta,bool requireCtrl,bool requireAlt,bool requireShift)
        { return keyDown&&!meta&&ctrl==requireCtrl&&alt==requireAlt&&shift==requireShift; }
        public static bool Pressed(bool allowCursorAlt=false)
        {
            return Allowed(Config.ShortcutKey)&&Matches(Input.GetKeyDown(Config.ShortcutKey),
                Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl),
                (!allowCursorAlt||Config.ShortcutAlt)&&(Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)),
                Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift),
                Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand)||Input.GetKey(KeyCode.LeftWindows)||Input.GetKey(KeyCode.RightWindows),
                Config.ShortcutCtrl,Config.ShortcutAlt,Config.ShortcutShift);
        }
        public static string Display
        { get { return (Config.ShortcutCtrl?"Ctrl+":"")+(Config.ShortcutAlt?"Alt+":"")+(Config.ShortcutShift?"Shift+":"")+Config.ShortcutKey; } }
    }
}
