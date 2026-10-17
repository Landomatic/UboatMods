// Landomatic Patch Profiler - diagnostic UBOAT mod. Times every Harmony-patched game method (all owners),
// each individual prefix/postfix/finalizer, and Update/LateUpdate/FixedUpdate of MonoBehaviours in mod
// assemblies, and reports slow calls, 50 ms frame hitches and a 10 s top-10 to output_log.txt as [PatchProf].
// Timing uses UnityEngine.Time.realtimeSinceStartupAsDouble on the main thread only (no Stopwatch).
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UBOAT.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LandomaticPatchProfiler
{
    public sealed class LandomaticPatchProfilerMod : IUserMod
    {
        private static bool _loaded;

        public void OnLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                PPConfig.Load();
                if (!PPConfig.Enabled) { Debug.Log("[PatchProf] disabled in " + PPConfig.PathText); return; }
                PPCore.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                GameObject go = new GameObject("LandomaticPatchProfiler");
                try { go.tag = "Unserializable"; } catch { }
                go.hideFlags = HideFlags.DontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<PPHost>();
                Debug.Log("[PatchProf] loaded; threshold " + PPConfig.ThresholdMs + " ms, hitch " + PPConfig.HitchMs
                    + " ms, first scan in " + PPConfig.StartDelay + " s, config " + PPConfig.PathText);
            }
            catch (Exception ex) { Debug.Log("[PatchProf] load failed: " + ex); }
        }
    }

    internal static class PPConfig
    {
        internal static bool Enabled = true;
        internal static double ThresholdMs = 5.0;
        internal static double HitchMs = 50.0;
        internal static float SummarySeconds = 10f;
        internal static float StartDelay = 20f;
        internal static float RescanAfterSceneLoad = 15f;
        internal static bool TimePatchMethods = true;
        internal static bool TimeModCallbacks = true;
        internal static string[] Exclude = new string[0];
        internal static string PathText = "";

        internal static void Load()
        {
            string path = Path.Combine(Application.persistentDataPath, "LandomaticPatchProfiler.txt");
            PathText = path;
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path,
                        "# Landomatic Patch Profiler settings (restart the game after editing)\r\n"
                        + "enabled=true\r\n"
                        + "threshold_ms=5\r\n"
                        + "hitch_ms=50\r\n"
                        + "summary_seconds=10\r\n"
                        + "start_delay_seconds=20\r\n"
                        + "rescan_after_scene_load_seconds=15\r\n"
                        + "time_patch_methods=true\r\n"
                        + "time_mod_unity_callbacks=true\r\n"
                        + "# exclude: ';'-separated text; any method label containing one is not instrumented\r\n"
                        + "exclude=\r\n");
                    return;
                }
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string v = line.Substring(eq + 1).Trim();
                    double d;
                    bool b = v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
                    switch (k)
                    {
                        case "enabled": Enabled = b; break;
                        case "threshold_ms": if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) ThresholdMs = d; break;
                        case "hitch_ms": if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) HitchMs = d; break;
                        case "summary_seconds": if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) SummarySeconds = Mathf.Max(2f, (float)d); break;
                        case "start_delay_seconds": if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) StartDelay = Mathf.Max(1f, (float)d); break;
                        case "rescan_after_scene_load_seconds": if (double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) RescanAfterSceneLoad = (float)d; break;
                        case "time_patch_methods": TimePatchMethods = b; break;
                        case "time_mod_unity_callbacks": TimeModCallbacks = b; break;
                        case "exclude": Exclude = v.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); break;
                    }
                }
            }
            catch (Exception ex) { Debug.Log("[PatchProf] config read failed, defaults used: " + ex.Message); }
        }
    }

    internal sealed class PPStat
    {
        internal string Label;
        internal int Calls, WindowCalls;
        internal double Total, Max, WindowTotal, WindowMax;
        internal float NextSlowLog;
        internal int FrameStamp = -1;
        internal double FrameMs;
    }

    internal static class PPCore
    {
        internal const string HarmonyId = "landomatic.patchprofiler";
        internal static int MainThreadId;
        internal static bool Active;
        internal static bool Busy; // our own logging / bookkeeping: never time it (Debug.Log may itself be patched)
        internal static int Frame;
        internal static bool MapOpen;
        internal static readonly Dictionary<MethodBase, PPStat> Stats = new Dictionary<MethodBase, PPStat>();
        internal static readonly List<PPStat> FrameHits = new List<PPStat>();
        internal static readonly List<PPStat> PrevFrameHits = new List<PPStat>();
        private static readonly HashSet<MethodBase> Instrumented = new HashSet<MethodBase>();
        private static readonly HashSet<MethodBase> Failed = new HashSet<MethodBase>();
        private static Harmony _harmony;
        private static HarmonyMethod _pre, _post;
        private static int _logsThisSecond;
        private static float _logSecond;

        // ------------------------------------------------------------------ the timing pair (runs inside game code)

        public static void TimePrefix(out double __state)
        {
            __state = 0;
            if (!Active || Busy) return;
            try
            {
                if (System.Threading.Thread.CurrentThread.ManagedThreadId != MainThreadId) return;
                __state = Time.realtimeSinceStartupAsDouble;
            }
            catch { __state = 0; }
        }

        public static void TimePostfix(MethodBase __originalMethod, double __state)
        {
            if (__state <= 0 || Busy) return;
            try
            {
                double ms = (Time.realtimeSinceStartupAsDouble - __state) * 1000.0;
                Record(__originalMethod, ms);
            }
            catch { }
        }

        private static void Record(MethodBase m, double ms)
        {
            PPStat s;
            if (m == null || !Stats.TryGetValue(m, out s)) return;
            s.Calls++; s.WindowCalls++;
            s.Total += ms; s.WindowTotal += ms;
            if (ms > s.Max) s.Max = ms;
            if (ms > s.WindowMax) s.WindowMax = ms;
            if (ms >= 0.25)
            {
                if (s.FrameStamp != Frame) { s.FrameStamp = Frame; s.FrameMs = 0; FrameHits.Add(s); }
                s.FrameMs += ms;
            }
            if (ms >= PPConfig.ThresholdMs)
            {
                float now = Time.realtimeSinceStartup;
                if (now < s.NextSlowLog) return;
                if (now - _logSecond >= 1f) { _logSecond = now; _logsThisSecond = 0; }
                if (++_logsThisSecond > 20) return;
                s.NextSlowLog = now + 1f;
                Log(ms.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms " + s.Label
                    + " frame=" + Frame + " mapOpen=" + MapOpen);
            }
        }

        internal static void Log(string text)
        {
            bool was = Busy;
            Busy = true;
            try { Debug.Log("[PatchProf] " + text); }
            catch { }
            finally { Busy = was; }
        }

        // ------------------------------------------------------------------ instrumenting

        internal static void Scan(string why)
        {
            bool was = Busy;
            Busy = true;
            int added = 0, failed = 0, originals = 0, patches = 0, callbacks = 0;
            double t0 = Time.realtimeSinceStartupAsDouble;
            try
            {
                if (_harmony == null)
                {
                    _harmony = new Harmony(HarmonyId);
                    MethodInfo pre = AccessTools.Method(typeof(PPCore), "TimePrefix");
                    MethodInfo post = AccessTools.Method(typeof(PPCore), "TimePostfix");
                    _pre = new HarmonyMethod(pre) { priority = Priority.First };
                    _post = new HarmonyMethod(post) { priority = Priority.Last };
                }
                List<MethodBase> originalsList = new List<MethodBase>(Harmony.GetAllPatchedMethods());
                foreach (MethodBase original in originalsList)
                {
                    Patches info = null;
                    try { info = Harmony.GetPatchInfo(original); } catch { }
                    if (info == null) continue;
                    List<string> owners = new List<string>();
                    foreach (string o in info.Owners) if (o != HarmonyId) owners.Add(o);
                    if (owners.Count == 0) continue;
                    originals++;
                    if (Instrument(original, "owner=" + string.Join(",", owners.ToArray()) + " method=" + Name(original), ref added, ref failed)) { }
                    if (!PPConfig.TimePatchMethods) continue;
                    patches += InstrumentPatchList(info.Prefixes, "prefix", original, ref added, ref failed);
                    patches += InstrumentPatchList(info.Postfixes, "postfix", original, ref added, ref failed);
                    patches += InstrumentPatchList(info.Finalizers, "finalizer", original, ref added, ref failed);
                }
                if (PPConfig.TimeModCallbacks) callbacks = InstrumentModCallbacks(ref added, ref failed);
            }
            catch (Exception ex) { Log("scan failed: " + ex.Message); }
            finally { Busy = was; }
            Active = true;
            Log("scan (" + why + "): " + originals + " patched originals, " + patches + " patch methods, " + callbacks
                + " mod Unity callbacks; newly instrumented " + added + ", skipped/failed " + failed + ", total " + Instrumented.Count
                + " (" + ((Time.realtimeSinceStartupAsDouble - t0) * 1000.0).ToString("0") + " ms)");
        }

        private static int InstrumentPatchList(IEnumerable<Patch> list, string kind, MethodBase original, ref int added, ref int failed)
        {
            int n = 0;
            if (list == null) return 0;
            foreach (Patch p in list)
            {
                if (p == null || p.owner == HarmonyId || p.PatchMethod == null) continue;
                n++;
                Instrument(p.PatchMethod, "owner=" + p.owner + " method=" + Name(p.PatchMethod) + " (" + kind + " of " + Name(original) + ")", ref added, ref failed);
            }
            return n;
        }

        private static readonly string[] SystemPrefixes = {
            "mscorlib", "System", "Mono.", "Unity", "UnityEngine", "com.uboat", "Assembly-CSharp", "DWS", "0Harmony", "Harmony",
            "netstandard", "TextMeshPro", "Unity.TextMeshPro", "UniTask", "Newtonsoft", "Microsoft", "Roslyn", "Trivial", "nunit",
            "Cinemachine", "DOTween", "Anonymously Hosted", "MonoMod", "Mono", "ReportGenerator", "Sirenix", "Rewired", "Steamworks",
            "com.rlabrecque", "FMOD", "Facepunch", "Bee.", "ICSharpCode", "Ionic", "LibTessDotNet", "Purchasing" };

        private static int InstrumentModCallbacks(ref int added, ref int failed)
        {
            int n = 0;
            Assembly own = typeof(PPCore).Assembly;
            foreach (Assembly asm in AccessTools.AllAssemblies())
            {
                if (asm == null || asm == own) continue;
                string an;
                try { if (asm.IsDynamic) continue; an = asm.GetName().Name ?? ""; } catch { continue; }
                bool system = false;
                foreach (string p in SystemPrefixes) if (an.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { system = true; break; }
                if (system) continue;
                Type[] types;
                try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types; } catch { continue; }
                foreach (Type t in types)
                {
                    try
                    {
                        if (t == null || t.ContainsGenericParameters || !typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                        foreach (string cb in new[] { "Update", "LateUpdate", "FixedUpdate", "OnGUI" })
                        {
                            MethodInfo m = t.GetMethod(cb, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                            if (m == null) continue;
                            n++;
                            Instrument(m, "owner=" + an + " method=" + Name(m) + " (Unity callback)", ref added, ref failed);
                        }
                    }
                    catch { }
                }
            }
            return n;
        }

        private static bool Instrument(MethodBase m, string label, ref int added, ref int failed)
        {
            if (m == null || Instrumented.Contains(m) || Failed.Contains(m)) return false;
            try
            {
                Type dt = m.DeclaringType;
                if (dt == null || dt.Assembly == typeof(PPCore).Assembly || dt.ContainsGenericParameters || m.IsGenericMethodDefinition
                    || m.ContainsGenericParameters || m.IsAbstract || m.IsConstructor
                    || (m.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0
                    || (m.Attributes & MethodAttributes.PinvokeImpl) != 0)
                { Failed.Add(m); failed++; return false; }
                foreach (string ex in PPConfig.Exclude) if (label.IndexOf(ex, StringComparison.OrdinalIgnoreCase) >= 0) { Failed.Add(m); failed++; return false; }
                Stats[m] = new PPStat { Label = label };
                _harmony.Patch(m, prefix: _pre, postfix: _post);
                Instrumented.Add(m);
                added++;
                return true;
            }
            catch (Exception ex)
            {
                Stats.Remove(m);
                Failed.Add(m);
                failed++;
                if (failed <= 10) Log("could not instrument " + Name(m) + ": " + ex.GetType().Name + " " + ex.Message);
                return false;
            }
        }

        internal static string Name(MethodBase m)
        {
            try { return (m.DeclaringType != null ? m.DeclaringType.FullName : "?") + "." + m.Name; } catch { return "?"; }
        }

        // ------------------------------------------------------------------ reports

        internal static void Summary(float seconds)
        {
            bool was = Busy;
            Busy = true;
            try
            {
                List<PPStat> all = new List<PPStat>();
                foreach (PPStat s in Stats.Values) if (s.WindowCalls > 0) all.Add(s);
                all.Sort((a, b) => b.WindowTotal.CompareTo(a.WindowTotal));
                StringBuilder sb = new StringBuilder();
                sb.Append("summary ").Append(seconds.ToString("0")).Append(" s mapOpen=").Append(MapOpen).Append(" | top by total:");
                for (int i = 0; i < all.Count && i < 10; i++) Append(sb, i + 1, all[i]);
                all.Sort((a, b) => b.WindowMax.CompareTo(a.WindowMax));
                sb.Append(" | top by max:");
                for (int i = 0; i < all.Count && i < 10; i++) Append(sb, i + 1, all[i]);
                Log(sb.ToString());
                foreach (PPStat s in Stats.Values) { s.WindowCalls = 0; s.WindowTotal = 0; s.WindowMax = 0; }
            }
            catch { }
            finally { Busy = was; }
        }

        private static void Append(StringBuilder sb, int rank, PPStat s)
        {
            System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
            sb.Append("\n    ").Append(rank).Append(") total=").Append(s.WindowTotal.ToString("0.0", ci)).Append(" ms max=")
              .Append(s.WindowMax.ToString("0.0", ci)).Append(" ms calls=").Append(s.WindowCalls).Append(' ').Append(s.Label);
        }

        internal static void Hitch(double frameMs, int frame)
        {
            bool was = Busy;
            Busy = true;
            try
            {
                PrevFrameHits.Sort((a, b) => b.FrameMs.CompareTo(a.FrameMs));
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
                StringBuilder sb = new StringBuilder();
                double sum = 0;
                foreach (PPStat s in PrevFrameHits) sum += s.FrameMs;
                sb.Append("HITCH ").Append(frameMs.ToString("0.0", ci)).Append(" ms frame=").Append(frame).Append(" mapOpen=").Append(MapOpen)
                  .Append(" timed=").Append(sum.ToString("0.0", ci)).Append(" ms (inclusive, may overlap)");
                for (int i = 0; i < PrevFrameHits.Count && i < 5; i++)
                    sb.Append("\n    ").Append(PrevFrameHits[i].FrameMs.ToString("0.0", ci)).Append(" ms ").Append(PrevFrameHits[i].Label);
                if (PrevFrameHits.Count == 0) sb.Append(" - nothing instrumented took >= 0.25 ms in that frame");
                Log(sb.ToString());
            }
            catch { }
            finally { Busy = was; }
        }
    }

    internal sealed class PPHost : MonoBehaviour
    {
        private float _scanAt, _summaryAt;
        private int _hitchesThisSecond;
        private float _hitchSecond;
        private MethodInfo _modeGetter;
        private FieldInfo _instanceField;
        private bool _mapProbeTried;

        private void Awake()
        {
            _scanAt = Time.realtimeSinceStartup + PPConfig.StartDelay;
            _summaryAt = _scanAt + PPConfig.SummarySeconds;
            try { SceneManager.sceneLoaded += OnSceneLoaded; } catch { }
        }

        private void OnDestroy()
        {
            try { SceneManager.sceneLoaded -= OnSceneLoaded; } catch { }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (PPConfig.RescanAfterSceneLoad <= 0) return;
                float at = Time.realtimeSinceStartup + PPConfig.RescanAfterSceneLoad;
                if (_scanAt <= 0 || at < _scanAt) _scanAt = at;
            }
            catch { }
        }

        private void Update()
        {
            try
            {
                double dtMs = Time.unscaledDeltaTime * 1000.0;
                int frame = PPCore.Frame;
                // swap buckets: what was timed since the last Update is the frame that just ended
                PPCore.PrevFrameHits.Clear();
                PPCore.PrevFrameHits.AddRange(PPCore.FrameHits);
                PPCore.FrameHits.Clear();
                PPCore.Frame = Time.frameCount;
                PPCore.MapOpen = ProbeMapOpen();
                float now = Time.realtimeSinceStartup;
                if (PPCore.Active && dtMs >= PPConfig.HitchMs)
                {
                    if (now - _hitchSecond >= 1f) { _hitchSecond = now; _hitchesThisSecond = 0; }
                    if (++_hitchesThisSecond <= 5) PPCore.Hitch(dtMs, frame);
                }
                if (_scanAt > 0 && now >= _scanAt)
                {
                    _scanAt = 0;
                    PPCore.Scan(PPCore.Active ? "after scene load" : "start");
                    _summaryAt = now + PPConfig.SummarySeconds;
                }
                if (PPCore.Active && now >= _summaryAt)
                {
                    PPCore.Summary(PPConfig.SummarySeconds + (now - _summaryAt));
                    _summaryAt = now + PPConfig.SummarySeconds;
                }
            }
            catch { }
        }

        private bool ProbeMapOpen()
        {
            try
            {
                if (!_mapProbeTried)
                {
                    _mapProbeTried = true;
                    Type t = AccessTools.TypeByName("UBOAT.Game.Scene.Camera.MainCamera");
                    if (t != null)
                    {
                        _instanceField = AccessTools.Field(t, "Instance");
                        PropertyInfo p = AccessTools.Property(t, "CurrentMode");
                        _modeGetter = p != null ? p.GetGetMethod(true) : null;
                    }
                }
                if (_instanceField == null || _modeGetter == null) return false;
                object cam = _instanceField.GetValue(null);
                if (cam == null || (cam is UnityEngine.Object && !(UnityEngine.Object)cam)) return false;
                object mode = _modeGetter.Invoke(cam, null);
                return mode != null && mode.ToString() == "Map";
            }
            catch { return false; }
        }
    }
}
