// Copyright (c) 2026 Landomatic. See LICENSE and NOTICE.txt.
// Source/history: https://github.com/Landomatic/UboatMods
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Landomatic.TorpedoRoom
{
    internal static class Config
    {
        public static bool Enabled = true, Interface = true, Overview;
        public static bool AllowLockout=true,PerformanceTrace=true,SpeedSync=true;
        public static int Preset=2;
        public static RoomAccess Access;
        public static ViewportMode Viewport=ViewportMode.ReadOnly;
        public static PistolPolicy Policy;
        public static float Delay=30;
        public static KeyCode ShortcutKey=KeyCode.T;
        public static bool ShortcutCtrl,ShortcutAlt,ShortcutShift;
        static string PathName { get { return Path.Combine(Application.persistentDataPath, "TorpedoRoomConfig.txt"); } }
        public static void Load()
        {
            try {
                if (!File.Exists(PathName)) { Save(); Debug.Log("[TorpedoRoom] Default config created: "+PathName); return; }
                foreach (string line in File.ReadAllLines(PathName)) {
                    var kv=line.Split('='); if(kv.Length!=2)continue;
                    bool b; int n; float f;KeyCode key;
                    if(kv[0]=="speedSync"&&bool.TryParse(kv[1],out b))SpeedSync=b;
                    if(kv[0]=="enabled" && bool.TryParse(kv[1],out b))Enabled=b;
                    if(kv[0]=="interface" && bool.TryParse(kv[1],out b))Interface=b;
                    if(kv[0]=="performanceTrace"&&bool.TryParse(kv[1],out b))PerformanceTrace=b;
                    if(kv[0]=="overview" && bool.TryParse(kv[1],out b)){Overview=b;Access=b?RoomAccess.OppositeReadOnly:RoomAccess.CurrentCompartment;}
                    if(kv[0]=="interfacePreset"&&int.TryParse(kv[1],out n)&&n>=0&&n<=2)Preset=n;
                    if(kv[0]=="roomAccess"&&int.TryParse(kv[1],out n)&&n>=0&&n<=2)Access=(RoomAccess)n;
                    if(kv[0]=="viewportDisplay"&&int.TryParse(kv[1],out n)&&n>=0&&n<=2)Viewport=(ViewportMode)n;
                    if((kv[0]=="allowStraightRunLockout"||kv[0]=="allowStraightRun")&&bool.TryParse(kv[1],out b))AllowLockout=b;
                    if(kv[0]=="shortcutKey"){
                        ShortcutKey=Enum.TryParse<KeyCode>(kv[1],true,out key)&&Shortcut.Allowed(key)?key:KeyCode.None;
                        if(ShortcutKey==KeyCode.None)Debug.LogWarning("[TorpedoRoom] Invalid/reserved shortcut disabled; choose a key in Dusty's menu.");
                    }
                    if(kv[0]=="shortcutCtrl"&&bool.TryParse(kv[1],out b))ShortcutCtrl=b;
                    if(kv[0]=="shortcutAlt"&&bool.TryParse(kv[1],out b))ShortcutAlt=b;
                    if(kv[0]=="shortcutShift"&&bool.TryParse(kv[1],out b))ShortcutShift=b;
                    if(kv[0]=="pistolPolicy" && int.TryParse(kv[1],out n) && (n==0||n==1||n==3))Policy=(PistolPolicy)n;
                    if(kv[0]=="pistolPolicy"&&int.TryParse(kv[1],out n)&&n==2)Policy=PistolPolicy.Free;
                    if(kv[0]=="preparationSeconds" && float.TryParse(kv[1],NumberStyles.Float,CultureInfo.InvariantCulture,out f) && Preparation.Finite(f))Delay=Mathf.Clamp(f,0,120);
                }
            } catch(Exception e) { Debug.LogWarning("[TorpedoRoom] Config read: "+e.Message); }
        }
        public static void Save()
        {
            string tmp=PathName+".tmp";
            File.WriteAllText(tmp,"# Torpedo Room Config 0.1.18-test\n# Policy: 0 free, 1 locked on load, 3 dry-tube preparation (2 retired)\n"
                +"speedSync="+SpeedSync+"\nenabled="+Enabled+"\ninterface="+Interface+"\ninterfacePreset="+Preset+"\nroomAccess="+(int)Access+"\nviewportDisplay="+(int)Viewport+"\npistolPolicy="+(int)Policy+"\npreparationSeconds="+Delay.ToString(CultureInfo.InvariantCulture)
                +"\nperformanceTrace="+PerformanceTrace+"\nshortcutKey="+ShortcutKey+"\nshortcutCtrl="+ShortcutCtrl+"\nshortcutAlt="+ShortcutAlt+"\nshortcutShift="+ShortcutShift+"\nallowStraightRun="+AllowLockout+"\n");
            if(File.Exists(PathName))File.Replace(tmp,PathName,null);else File.Move(tmp,PathName);
        }
    }
    // Dusty's documented-by-existing-provider reflection contract; no hard dependency.
    public static class CommunityPatchSettings
    {
        public static string ModName { get { return "Torpedo Room"; } }
        public static string Category { get { return "Submarine / Torpedoes"; } }
        public static string Description { get { return "Experimental by Landomatic. The configurable shortcut (default T) at a tube station opens preparation controls; physical levers still flood/drain/fire. Pistol policies are host-controlled. FAT/LUT stays adjustable at sea. All crew players need this companion. Host policy changes are unavailable to shared-boat clients."; } }
        static object[] Bool(string key,string title,string help,Func<bool> get,Action<bool> set,bool host)
        {
            return new object[]{"bool",key,title,help,new Func<object>(()=>get()),new Action<object>(v=>{
                if(host && Bridge.IsRemote) { Runtime.Say("Only the host can change session rules.");return; }
                bool old=get();var oldPolicy=Config.Policy;int oldPreset=Config.Preset; try {set(Convert.ToBoolean(v));if(host&&(key=="locked"||key=="dry"||key=="allowStraightRun")&&old!=get())Config.Preset=2;Config.Save();if(host)MenuIntegration.Publish();}catch(Exception e){set(old);Config.Policy=oldPolicy;Config.Preset=oldPreset;Runtime.Say("Setting not saved: "+e.Message);}
            })};
        }
        static object[] Choice(string key,string title,string help,Func<int> get,Action<int> set,string[] options)
        {return new object[]{"choice",key,title,help,new Func<object>(()=>get()),new Action<object>(v=>{
            if(Bridge.IsRemote){Runtime.Say("Only the host can change session rules.");return;}
            int n=Convert.ToInt32(v);if(n<0||n>=options.Length)return;int old=get(),preset=Config.Preset;var access=Config.Access;var viewport=Config.Viewport;var policy=Config.Policy;bool straight=Config.AllowLockout;
            try{set(n);if(key!="preset"&&old!=n)Config.Preset=2;Config.Save();MenuIntegration.Publish();}catch(Exception e){set(old);Config.Preset=preset;Config.Access=access;Config.Viewport=viewport;Config.Policy=policy;Config.AllowLockout=straight;Runtime.Say("Setting not saved: "+e.Message);}
        }),options};}
        static RoomAccess EffectiveAccess {get{return Bridge.IsRemote&&Bridge.Last!=null?Bridge.Last.Access:Config.Access;}}
        static ViewportMode EffectiveViewport {get{return Bridge.IsRemote&&Bridge.Last!=null?Bridge.Last.Viewport:Config.Viewport;}}
        public static object[] GetSettings()
        {
            return new object[]{
                Bool("enabled","Enable torpedo preparation","Host/single-player master switch. Turning off stops overrides and restores unrestricted stock mode changes.",()=>Bridge.IsRemote && Bridge.Last!=null?Bridge.Last.Enabled:Config.Enabled,v=>Config.Enabled=v,true),
                Bool("interface","Show preparation interface","Local display only; hiding it does not bypass the host's rules. Default shortcut: T.",()=>Config.Interface,v=>Config.Interface=v,false),
                new object[]{"key","shortcutKey","Preparation shortcut key","Local binding. Combine with the Ctrl/Alt/Shift rows below. F8/F9 are reserved for quicksave/quickload and are refused by this mod.",
                    new Func<object>(()=>Config.ShortcutKey),new Action<object>(v=>{
                        KeyCode k=(KeyCode)v;if(!Shortcut.Allowed(k)){Runtime.Say("Shortcut refused: choose a keyboard key other than F8/F9, Escape or a modifier.");return;}
                        var old=Config.ShortcutKey;try{Config.ShortcutKey=k;Config.Save();}catch(Exception e){Config.ShortcutKey=old;Runtime.Say("Shortcut not saved: "+e.Message);}
                    })},
                Bool("shortcutCtrl","Shortcut requires Ctrl","Local shortcut modifier. Either left or right Ctrl works.",()=>Config.ShortcutCtrl,v=>Config.ShortcutCtrl=v,false),
                Bool("shortcutAlt","Shortcut requires Alt","Local shortcut modifier. Either left or right Alt works.",()=>Config.ShortcutAlt,v=>Config.ShortcutAlt=v,false),
                Bool("shortcutShift","Shortcut requires Shift","Local shortcut modifier. Ctrl/Shift must match; Alt may be held for the game cursor when not required by the shortcut.",()=>Config.ShortcutShift,v=>Config.ShortcutShift=v,false),
                Bool("performanceTrace","Diagnostic timing log (every 10 seconds)","Local test-build logging: Torpedo Room update time, longest frame and process-wide garbage collections. GC/frame figures include other mods; they do not prove the cause of a stutter.",()=>Config.PerformanceTrace,v=>Config.PerformanceTrace=v,false),
                Choice("preset","Interface preset","Host rule. Casual: all tubes editable, synchronized viewport, unrestricted AZ/MZ. Hardcore: current compartment only, hidden viewport reports, unrestricted AZ/MZ. Custom preserves individual choices. Speed synchronization is independent and unchanged by presets. No firing or role permissions are changed.",
                    ()=>Bridge.IsRemote?(EffectiveAccess==RoomAccess.AllEditable&&EffectiveViewport==ViewportMode.Synchronized&&EffectivePolicy==PistolPolicy.Free?0:EffectiveAccess==RoomAccess.CurrentCompartment&&EffectiveViewport==ViewportMode.Hidden&&EffectivePolicy==PistolPolicy.Free?1:2):Config.Preset,
                    v=>{Config.Preset=v;if(v<2){Config.Access=v==0?RoomAccess.AllEditable:RoomAccess.CurrentCompartment;Config.Viewport=v==0?ViewportMode.Synchronized:ViewportMode.Hidden;Config.Policy=PistolPolicy.Free;Config.AllowLockout=true;}},new[]{"Casual","Hardcore","Custom"}),
                Choice("roomAccess","Tube access from torpedo rooms","Host rule. Applies to programming controls; physical flooding and firing remain unchanged.",()=> (int)EffectiveAccess,v=>Config.Access=(RoomAccess)v,new[]{"Current compartment only","Opposite tubes read-only","All tubes editable"}),
                Choice("viewportDisplay","Scope / UZO / TDC torpedo display","Host rule. Synchronized: edits in either interface update marked tubes. Read-only: show settings and status. Hidden: suppress torpedo reports, retaining permitted firing. MP station permissions still apply.",()=> (int)EffectiveViewport,v=>Config.Viewport=(ViewportMode)v,new[]{"Synchronized controls","Read-only reports","Hidden reports"}),
                Bool("speedSync","Bidirectional torpedo / TDC speed sync","Host rule, independent of report visibility. ON: applied tube speeds update the computer and TDC speed inputs update tube preparations. OFF: keep independent settings and player mistakes. Never blocks firing.",()=>Bridge.IsRemote&&Bridge.Last!=null?Bridge.Last.SpeedSync:Config.SpeedSync,v=>Config.SpeedSync=v,true),
                Bool("allowStraightRun","Allow straight running for FAT/LUT","Host rule. Allows a per-torpedo Straight / Pattern choice. Stored pattern settings are retained. This is a gameplay bypass, not a simulated historical pin; no failure mechanic is added.",
                    ()=>Bridge.IsRemote&&Bridge.Last!=null?Bridge.Last.AllowLockout:Config.AllowLockout,v=>Config.AllowLockout=v,true),
                Bool("removeRecords","Remove preparation records before uninstalling","One-shot host/single-player cleanup. Disables this mod and removes its records from torpedoes. Then save to a separate slot before uninstalling. Checkbox returns to OFF.",()=>false,v=>{if(v)Runtime.RemoveRecords();},true),
                Bool("locked","Pistol mode locked when loaded","Host rule. Captures mode when first observed in a loaded tube. Settings follow the torpedo through unloading/reloading. Exclusive with the other pistol policies.",()=>EffectivePolicy==PistolPolicy.LockedOnLoad,v=>{if(v)Config.Policy=PistolPolicy.LockedOnLoad;else if(Config.Policy==PistolPolicy.LockedOnLoad)Config.Policy=PistolPolicy.Free;},true),
                Bool("dry","Require dry tube for pistol preparation","Host rule. Drain first; flooding, unloading or leaving the station cancels pending work. Does not animate withdrawal. Exclusive with locked-on-load policy.",()=>EffectivePolicy==PistolPolicy.DryTubePreparation,v=>{if(v)Config.Policy=PistolPolicy.DryTubePreparation;else if(Config.Policy==PistolPolicy.DryTubePreparation)Config.Policy=PistolPolicy.Free;},true),
                    new object[]{"float","delay","Pistol preparation time (game seconds)","Default 30 game seconds. Used only with dry-tube preparation; 0 is instant. FAT/LUT has no delay.",
                    new Func<object>(()=>Bridge.IsRemote&&Bridge.Last!=null?Bridge.Last.Delay:Config.Delay),new Action<object>(v=>{
                        if(Bridge.IsRemote){Runtime.Say("Only the host can change session rules.");return;}
                        float f=Convert.ToSingle(v,CultureInfo.InvariantCulture);if(!Preparation.Finite(f))return;
                        float old=Config.Delay;try{Config.Delay=Mathf.Clamp(f,0,120);Config.Save();}catch(Exception e){Config.Delay=old;Runtime.Say("Setting not saved: "+e.Message);}
                    }),0f,120f,5f}
            };
        }
        static PistolPolicy EffectivePolicy { get { return Bridge.IsRemote&&Bridge.Last!=null?Bridge.Last.Policy:Config.Policy; } }
        public static void ResetToDefaults()
        {
            Config.Interface=true;Config.Overview=false;Config.PerformanceTrace=true;
            Config.ShortcutKey=KeyCode.T;Config.ShortcutCtrl=Config.ShortcutAlt=false;Config.ShortcutShift=false;
            if(!Bridge.IsRemote){Config.Preset=2;Config.Enabled=true;Config.SpeedSync=true;Config.Policy=PistolPolicy.Free;Config.Delay=30;Config.AllowLockout=true;Config.Access=RoomAccess.CurrentCompartment;Config.Viewport=ViewportMode.ReadOnly;}
            Config.Save();
        }
    }
}
