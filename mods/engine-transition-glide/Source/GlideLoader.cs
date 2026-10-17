// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
using System;
using System.Globalization;
using System.IO;
using HarmonyLib;
using UBOAT.Game;
using UBOAT.Game.Core.Serialization;
using UnityEngine;

namespace EngineTransitionGlide
{
    [NonSerializedInGameState]
    public class GlideLoader : IUserMod
    {
        internal const string Tag = "[EngineTransitionGlide]";
        internal const string BuildId = "v0.2.0";

        internal static bool Enabled = true;
        internal static float GlideSeconds = 4f;
        internal static bool SkipDuringAlarm = true;
        internal static bool LogSwaps = true;
        internal static bool SpeedMatch = true; // transitionMode=speedMatch|fade
        internal static float RampScale = 1f;
        internal static bool ApplyRampScaleGlobally = false;

        public void OnLoaded()
        {
            try
            {
                LoadConfig();
                var h = new Harmony("com.landomatic.enginetransitionglide");
                h.CreateClassProcessor(typeof(SwapHook)).Patch();
                h.CreateClassProcessor(typeof(ActiveEnginesSetterHook)).Patch();
                h.CreateClassProcessor(typeof(GlideTick)).Patch();
                h.CreateClassProcessor(typeof(RampHook)).Patch();
                Debug.Log(Tag + " initialized [" + BuildId + "] by Landomatic. enabled=" + Enabled
                    + " glideSeconds=" + GlideSeconds.ToString("0.##", CultureInfo.InvariantCulture)
                    + " transitionMode=" + (SpeedMatch ? "speedMatch" : "fade")
                    + " rampScale=" + RampScale.ToString("0.##", CultureInfo.InvariantCulture)
                    + " applyRampScaleGlobally=" + ApplyRampScaleGlobally
                    + " skipDuringAlarm=" + SkipDuringAlarm + " logSwaps=" + LogSwaps);
            }
            catch (Exception e)
            {
                Debug.LogError(Tag + " failed to initialize; mod is off.");
                Debug.LogException(e);
            }
        }

        private static void LoadConfig()
        {
            string p = Path.Combine(Application.persistentDataPath, "EngineTransitionGlideConfig.txt");
            try
            {
                if (File.Exists(p))
                {
                    foreach (string raw in File.ReadAllLines(p))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#")) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string k = line.Substring(0, eq).Trim().ToLowerInvariant();
                        string v = line.Substring(eq + 1).Trim();
                        bool b; float f;
                        switch (k)
                        {
                            case "enabled": if (bool.TryParse(v, out b)) Enabled = b; break;
                            case "glideseconds":
                                if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                                    GlideSeconds = Mathf.Clamp(f, 0.5f, 15f);
                                break;
                            case "skipduringalarm": if (bool.TryParse(v, out b)) SkipDuringAlarm = b; break;
                            case "logswaps": if (bool.TryParse(v, out b)) LogSwaps = b; break;
                            case "transitionmode":
                                if (v.Equals("fade", StringComparison.OrdinalIgnoreCase)) SpeedMatch = false;
                                else if (v.Equals("speedmatch", StringComparison.OrdinalIgnoreCase)) SpeedMatch = true;
                                break;
                            case "rampscale":
                                if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                                    RampScale = Mathf.Clamp(f, 1f, 5f);
                                break;
                            case "applyrampscaleglobally": if (bool.TryParse(v, out b)) ApplyRampScaleGlobally = b; break;
                        }
                    }
                }
                string nl = "\r\n";
                File.WriteAllText(p,
                    "# Engine Transition Glide (Landomatic)." + nl
                    + "# speedMatch: the incoming engine starts at the share of its power that matches the boat's" + nl
                    + "# current speed (game's own expected-speed table for the ordered gear). fade: carry the outgoing" + nl
                    + "# engine's thrust and fade it out over glideSeconds." + nl
                    + "transitionMode=" + (SpeedMatch ? "speedMatch" : "fade") + nl + nl
                    + "# Speed-up of the game's power ramp (stock ~5.6 s time constant). 1 = stock, allowed 1 to 5." + nl
                    + "rampScale=" + RampScale.ToString("0.##", CultureInfo.InvariantCulture) + nl + nl
                    + "# false: rampScale only for the engine just swapped in until it reaches its setting. true: every throttle change." + nl
                    + "applyRampScaleGlobally=" + (ApplyRampScaleGlobally ? "true" : "false") + nl + nl
                    + "# Master switch. true or false." + nl
                    + "# (fade mode) Carry the outgoing engine's thrust into the incoming one and fade it out across the swap. true or false." + nl
                    + "enabled=" + (Enabled ? "true" : "false") + nl + nl
                    + "# (fade mode) Seconds over which the carried thrust fades to the new engine's own ramp. Allowed 0.5 to 15." + nl
                    + "glideSeconds=" + GlideSeconds.ToString("0.##", CultureInfo.InvariantCulture) + nl + nl
                    + "# No glide while the alarm (crash dive) is on. true or false." + nl
                    + "skipDuringAlarm=" + (SkipDuringAlarm ? "true" : "false") + nl + nl
                    + "# One line per engine swap in output_log.txt. true or false." + nl
                    + "logSwaps=" + (LogSwaps ? "true" : "false") + nl);
            }
            catch (Exception e)
            {
                Debug.LogWarning(Tag + " config read/write failed, defaults used: " + e.Message);
            }
        }
    }
}
