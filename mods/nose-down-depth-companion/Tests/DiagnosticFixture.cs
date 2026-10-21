// Offline fixture: exercises actual CompanionConfig and DiagnosticIsolation with independent game/Harmony doubles.
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NoseDownCompanion;
namespace UnityEngine {
 public static class Application { public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "ndc-"+Guid.NewGuid().ToString("N")); }
 public static class Time { public static float unscaledTime; }
 public static class Debug { public static List<string> Lines=new List<string>(); public static void Log(object x){Lines.Add(x.ToString());} public static void LogWarning(object x){Lines.Add(x.ToString());} }
 public static class Mathf { public static float Clamp(float v,float lo,float hi){return Math.Max(lo,Math.Min(hi,v));} }
}
namespace UBOAT.Game.Scene.Items { public class HullInternalTank { public void FixedUpdate(){} } }
namespace UBOAT.Game.Scene.Entities { public class PlayerShip { public UBOAT.Game.Scene.Items.HullInternalTank HullInternalTank = new UBOAT.Game.Scene.Items.HullInternalTank(); } }
namespace HarmonyLib {
 public class Patch { public MethodInfo PatchMethod; }
 public class Patches { public List<Patch> Postfixes=new List<Patch>(); }
 public class HarmonyMethod { public HarmonyMethod(Type t,string n){} }
 public class Harmony { public static Patches Info; public static int Calls; public static Patches GetPatchInfo(MethodInfo m){return Info;} public void Patch(MethodInfo m,HarmonyMethod prefix){Calls++;} }
 public static class AccessTools { public static MethodInfo Method(Type t,string n){return t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);} public static FieldInfo Field(Type t,string n){return t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);} }
}
namespace DivePitch {
 internal class ModLoader {
#if OLD_CONTRACT
 internal const string BuildId="v2.1.4";
#else
 internal const string BuildId="v2.1.5";
#endif
 }
 internal class CasingFlooding { private static void Postfix(UBOAT.Game.Scene.Items.HullInternalTank tank){} }
}
namespace NoseDownCompanion {
 internal class CompanionLoader { internal const string Tag="[NoseDownCompanion]"; internal static HarmonyLib.Harmony Instance=new HarmonyLib.Harmony(); }
 internal class HullKey { internal static readonly string[] StandDownAllowed={"TypeVIIB","TypeVIIC","TypeVIIC41","TypeIIA","TypeIID"}; }
 internal class DepthTargetClamp { internal static void ResetControlState(string s){} }
 internal class PreciseDepthArrival { internal static void Reset(){} }
 internal class PdApproachBrake { internal static void Release(string s){} }
 internal class PdWaveForce { internal static void Reset(string s){} }
}
public class Checks {
 static int count;
 static void Check(bool yes,string text){count++;if(!yes)throw new Exception(text);}
 static bool Prefix(object tank){return (bool)typeof(DiagnosticIsolation).GetMethod("BeforeCasing",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{tank});}
 public static string Run(){
  Directory.CreateDirectory(UnityEngine.Application.persistentDataPath);
  string cfg=Path.Combine(UnityEngine.Application.persistentDataPath,"NoseDownCompanionConfig.txt");
  for(int master=0;master<2;master++)for(int locked=0;locked<2;locked++){
   CompanionConfig.Enabled=master==1;CompanionConfig.DiagnosticOnly=locked==1;
   Check(CompanionConfig.CorrectionsEnabled==(master==1&&locked==0),"effective correction truth table");
  }
  File.WriteAllText(cfg,"enabled=false\ntraceLogging=true\ndiagnosticOnly=true\ndiagnosticDisableNoseDownCasing=true\nshallowStandDown=false\nshallowTrimStandDown=false\n");
  CompanionConfig.Load();
  Check(CompanionConfig.DiagnosticOnly&&!CompanionConfig.CorrectionsEnabled,"load lock");
  Check(CompanionConfig.TraceLogging&&CompanionConfig.DiagnosticDisableNoseDownCasing,"load trace and bypass");
  CompanionConfig.Enabled=true;CompanionConfig.SaveCurrent();
  Check(!CompanionConfig.CorrectionsEnabled,"restored menu master cannot defeat lock");
  Check(File.ReadAllText(cfg).Contains("diagnosticOnly=true"),"save keeps lock");
  Check(File.ReadAllText(cfg).Contains("diagnosticDisableNoseDownCasing=true"),"save keeps explicit bypass");
  CompanionConfig.DiagnosticOnly=false;CompanionConfig.DiagnosticDisableNoseDownCasing=false;CompanionConfig.Load();
  Check(CompanionConfig.DiagnosticOnly&&CompanionConfig.DiagnosticDisableNoseDownCasing,"roundtrip");
  var menu=CommunityPatchSettings.GetSettings();
  foreach(object[] row in menu){
   if((string)row[1]=="enabled")((Action<object>)row[5])(true);
   if((string)row[1]=="diagnosticLockStatus")((Action<object>)row[5])(false);
   if((string)row[1]=="correctionsActiveStatus")Check(!(bool)((Func<object>)row[4])(),"effective menu status locked");
  }
  Check(CompanionConfig.DiagnosticOnly&&!CompanionConfig.CorrectionsEnabled,"actual provider master/status replay keeps lock");
  CommunityPatchSettings.ResetToDefaults();
  Check(CompanionConfig.DiagnosticOnly&&CompanionConfig.DiagnosticDisableNoseDownCasing&&!CompanionConfig.CorrectionsEnabled,"actual provider reset keeps diagnostics");
  for(int bits=0;bits<16;bits++)Check(DiagnosticIsolation.CanBypass((bits&1)!=0,(bits&2)!=0,(bits&4)!=0,(bits&8)!=0)==(bits==15),"bypass truth table "+bits);
  var ship=new UBOAT.Game.Scene.Entities.PlayerShip();
  HarmonyLib.Harmony.Info=null;DiagnosticIsolation.Observe(ship);Check(!DiagnosticIsolation.Ready,"absent hook");
  UnityEngine.Time.unscaledTime=3;
  HarmonyLib.Harmony.Info=new HarmonyLib.Patches();
  HarmonyLib.Harmony.Info.Postfixes.Add(new HarmonyLib.Patch{PatchMethod=typeof(DivePitch.CasingFlooding).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static)});
  DiagnosticIsolation.Observe(ship);
#if OLD_CONTRACT
  Check(!DiagnosticIsolation.Ready&&HarmonyLib.Harmony.Calls==0,"unsupported version not patched");
  Check(Prefix(ship.HullInternalTank),"unsupported version correction retained");
  return "PASS "+count+" unsupported-version/config assertions; no native patch execution.";
#else
  Check(DiagnosticIsolation.Ready&&HarmonyLib.Harmony.Calls==1,"versioned hook discovered");
  Check(!Prefix(ship.HullInternalTank),"explicit player bypass");
  Check(DiagnosticIsolation.BypassCalls==1&&DiagnosticIsolation.AppliedTo(ship),"applied status");
  Check(DiagnosticIsolation.ObservedCalls==1,"dispatch observer counted");
  Check(Prefix(new UBOAT.Game.Scene.Items.HullInternalTank()),"other tank untouched");
  Check(Prefix(null),"null untouched");
  CompanionConfig.DiagnosticOnly=false;Check(Prefix(ship.HullInternalTank),"bypass inactive without diagnostic lock");
  CompanionConfig.DiagnosticOnly=true;CompanionConfig.DiagnosticDisableNoseDownCasing=false;
  Check(Prefix(ship.HullInternalTank)&&!DiagnosticIsolation.AppliedTo(ship),"baseline runs upstream correction");
  CompanionConfig.DiagnosticDisableNoseDownCasing=true;
  var replacement=new UBOAT.Game.Scene.Entities.PlayerShip();DiagnosticIsolation.Observe(replacement);
  Check(DiagnosticIsolation.BypassCalls==0&&!DiagnosticIsolation.AppliedTo(replacement),"new boat resets counters");
  Check(Prefix(ship.HullInternalTank),"old player tank untouched");
  Check(!Prefix(replacement.HullInternalTank),"new player tank bypassed");
  Check(HarmonyLib.Harmony.Calls==1,"hook not duplicated");
  return "PASS "+count+" actual config/isolation assertions; dispatch uses a Harmony double, not native patch execution.";
#endif
 }
}
