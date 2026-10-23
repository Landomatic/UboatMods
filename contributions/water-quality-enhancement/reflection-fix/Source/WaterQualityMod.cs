using System;
using System.Reflection;
using HarmonyLib;
using PlayWay.Water;
using UBOAT.Game;
using UBOAT.Game.Core.Data;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.Scene.Effects;
using UBOAT.Game.Scene.Environment;
using UnityEngine;

namespace WaterQualityMod
{
    public class Main : IUserMod
    {
        public void OnLoaded()
        {
            try
            {
                var harmony = new Harmony("com.uboat.waterquality");
                harmony.PatchAll(typeof(Main).Assembly);
                Debug.Log("[WaterQualityMod] Water Quality Enhancement loaded - 19 patches applied");
            }
            catch (Exception ex)
            {
                Debug.LogError("[WaterQualityMod] Failed to apply patches: " + ex);
            }
        }
    }

    internal static class Cfg
    {
        public static int FFTResolution = 512;
        public static bool ForceFullFFT = true;
        public static bool ForceHighPrecision = true;
        public static bool DisableLooping = false;
        public static float DisplacementScale = 0.65f;
        public static float NormalMipBias = -0.4f;
        public static int SSSResolution = 256;
        public static float SSSIntensityBoost = 1.8f;
        public static float SSSContrastBoost = 1.5f;
        public static float ForwardScatteringBoost = 1.6f;
        public static bool OverrideAbsorptionColor = true;
        public static float FresnelBoost = 1.4f;
        public static float DetailFadeScale = 0.65f;
        public static float FoamSupersampling = 1f;
        public static float FoamThresholdScale = 0.8f;
        public static float FoamFadingOverride = 0.92f;
        public static float FoamWarmth = 0.05f;
        public static bool ForceSpray = true;
        public static int SSSUpdateInterval = 2;
        public static int FoamUpdateInterval = 2;
        public static int ReflectionUpdateInterval = 4;
        public static bool AdaptiveQuality = true;
        public static float AdaptiveTargetMs = 16.6f;
    }

    [HarmonyPatch(typeof(WaterQualitySettings), "SetQualityLevel")]
    internal static class QualitySettingsPatch
    {
        static readonly FieldInfo qualityLevelsField = AccessTools.Field(typeof(WaterQualitySettings), "qualityLevels");
        [HarmonyPrefix]
        static void Prefix(WaterQualitySettings __instance)
        {
            var levels = (WaterQualityLevel[])qualityLevelsField.GetValue(__instance);
            if (levels == null) return;
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i].maxSpectrumResolution = Cfg.FFTResolution;
                levels[i].allowHighPrecisionTextures = Cfg.ForceHighPrecision;
                levels[i].allowHighQualityNormalMaps = true;
                levels[i].wavesMode = Cfg.ForceFullFFT ? WaterWavesMode.AllowAll : levels[i].wavesMode;
                levels[i].allowSpray = Cfg.ForceSpray || levels[i].allowSpray;
                levels[i].foamQuality = 1f;
                levels[i].maxTesselationFactor = 1f;
                levels[i].tileSizeScale = 1f;
                levels[i].maxVertexCount = 500000;
                levels[i].maxTesselatedVertexCount = 200000;
                levels[i].allowAlphaBlending = true;
            }
            qualityLevelsField.SetValue(__instance, levels);
        }
    }

    [HarmonyPatch(typeof(WaterSettingsController), "UserSettings_Changed")]
    internal static class WaterSettingsPatch
    {
        [HarmonyPostfix]
        static void Postfix(WaterSettingsController __instance)
        {
            var water = __instance.GetComponent<Water>();
            if (water == null || water.WindWaves == null) return;
            if (water.WindWaves.Resolution != Cfg.FFTResolution)
                water.WindWaves.Resolution = Cfg.FFTResolution;
            if (Cfg.ForceSpray)
            {
                var spray = __instance.GetComponent<Spray>();
                if (spray != null && !spray.enabled) spray.enabled = true;
            }
        }
    }

    [HarmonyPatch(typeof(WindWaves), MethodType.Constructor, typeof(Water), typeof(WindWaves.Data))]
    internal static class WindWavesCtorPatch
    {
        static readonly FieldInfo dataField = AccessTools.Field(typeof(WindWaves), "data");
        static readonly FieldInfo loopField = AccessTools.Field(typeof(WindWaves.Data), "LoopDuration");
        static readonly FieldInfo mipField = AccessTools.Field(typeof(WindWaves.Data), "MipBias");
        [HarmonyPostfix]
        static void Postfix(WindWaves __instance)
        {
            object data = dataField.GetValue(__instance);
            if (data == null) return;
            if (Cfg.DisableLooping) loopField.SetValue(data, 0f);
            if (Cfg.NormalMipBias < 0f) mipField.SetValue(data, Cfg.NormalMipBias);
        }
    }

    [HarmonyPatch(typeof(WindWaves), "Validate")]
    internal static class WindWavesValidatePatch
    {
        [HarmonyPostfix]
        static void Postfix(WindWaves __instance)
        {
            if (Cfg.NormalMipBias >= 0f) return;
            var fft = __instance.WaterWavesFFT;
            if (fft == null) return;
            var maps = fft.NormalMaps;
            if (maps == null) return;
            for (int i = 0; i < maps.Length; i++)
                if (maps[i] != null) maps[i].mipMapBias = Cfg.NormalMipBias;
        }
    }

    [HarmonyPatch(typeof(WavesRendererFFT), "OnWaterRender", typeof(Camera))]
    internal static class DisplacementScalePatch
    {
        static readonly FieldInfo waterField = AccessTools.Field(typeof(WavesRendererFFT), "water");
        static readonly FieldInfo matField = AccessTools.Field(typeof(Water), "materials");
        static readonly FieldInfo hdsField = AccessTools.Field(typeof(WaterMaterials), "horizontalDisplacementScale");
        [HarmonyPrefix]
        static void Prefix(WavesRendererFFT __instance, ref float __state)
        {
            float scale = Cfg.DisplacementScale;
            if (scale >= 1f) { __state = float.NaN; return; }
            var water = (Water)waterField.GetValue(__instance);
            if (water == null) { __state = float.NaN; return; }
            var materials = (WaterMaterials)matField.GetValue(water);
            if (materials == null) { __state = float.NaN; return; }
            float original = (float)hdsField.GetValue(materials);
            __state = original;
            hdsField.SetValue(materials, original * scale);
        }
        [HarmonyPostfix]
        static void Postfix(WavesRendererFFT __instance, float __state)
        {
            if (float.IsNaN(__state)) return;
            var water = (Water)waterField.GetValue(__instance);
            if (water == null) return;
            var materials = (WaterMaterials)matField.GetValue(water);
            if (materials == null) return;
            hdsField.SetValue(materials, __state);
        }
    }

    [HarmonyPatch(typeof(WaterSubsurfaceScattering), "Enable")]
    internal static class SubsurfaceScatteringPatch
    {
        static readonly FieldInfo resField = AccessTools.Field(typeof(WaterSubsurfaceScattering), "ambientResolution");
        [HarmonyPrefix]
        static void Prefix(WaterSubsurfaceScattering __instance)
        {
            int current = (int)resField.GetValue(__instance);
            if (current < Cfg.SSSResolution) resField.SetValue(__instance, Cfg.SSSResolution);
        }
    }

    [HarmonyPatch(typeof(WaterSubsurfaceScattering), "ResolveProfileData")]
    internal static class SSSBoostPatch
    {
        static readonly FieldInfo paramsField = AccessTools.Field(typeof(WaterSubsurfaceScattering), "shaderParams");
        [HarmonyPostfix]
        static void Postfix(WaterSubsurfaceScattering __instance)
        {
            var p = (Vector4)paramsField.GetValue(__instance);
            p.x *= Cfg.SSSIntensityBoost;
            p.y *= Cfg.SSSContrastBoost;
            paramsField.SetValue(__instance, p);
        }
    }

    [HarmonyPatch(typeof(WaterMaterials), "OnProfilesChanged")]
    internal static class WaterProfilePatch
    {
        static readonly FieldInfo waterField = AccessTools.Field(typeof(WaterMaterials), "water");
        static readonly int _AbsorptionColor = Shader.PropertyToID("_AbsorptionColor");
        static readonly int _SpecColor = Shader.PropertyToID("_SpecColor");
        static readonly int _SubsurfaceScatteringShoreColor = Shader.PropertyToID("_SubsurfaceScatteringShoreColor");
        static readonly int _DetailFadeFactor = Shader.PropertyToID("_DetailFadeFactor");
        static readonly int _WrapSubsurfaceScatteringPack = Shader.PropertyToID("_WrapSubsurfaceScatteringPack");
        static readonly int _FoamDiffuseColor = Shader.PropertyToID("_FoamDiffuseColor");
        [HarmonyPostfix]
        static void Postfix(WaterMaterials __instance)
        {
            var water = (Water)waterField.GetValue(__instance);
            if (water == null) return;
            var mpb = water.Renderer.PropertyBlock;
            if (Cfg.OverrideAbsorptionColor)
                mpb.SetVector(_AbsorptionColor, new Vector4(0.42f, 0.088f, 0.018f, 1f));
            if (Cfg.FresnelBoost > 1f)
            {
                Color sc = (Color)mpb.GetVector(_SpecColor);
                sc.r *= Cfg.FresnelBoost; sc.g *= Cfg.FresnelBoost; sc.b *= Cfg.FresnelBoost;
                mpb.SetColor(_SpecColor, sc);
            }
            if (Cfg.ForwardScatteringBoost > 1f)
            {
                Color shore = (Color)mpb.GetVector(_SubsurfaceScatteringShoreColor);
                float boosted = shore.a * Cfg.ForwardScatteringBoost;
                shore = new Color(Mathf.Lerp(shore.r, 0.08f, 0.3f), Mathf.Lerp(shore.g, 0.38f, 0.3f), Mathf.Lerp(shore.b, 0.32f, 0.3f), boosted);
                mpb.SetColor(_SubsurfaceScatteringShoreColor, shore);
            }
            if (Cfg.DetailFadeScale < 1f)
                mpb.SetFloat(_DetailFadeFactor, mpb.GetFloat(_DetailFadeFactor) * Cfg.DetailFadeScale);
            Vector4 wrap = mpb.GetVector(_WrapSubsurfaceScatteringPack);
            wrap.x *= 1.3f; wrap.z *= 1.3f;
            mpb.SetVector(_WrapSubsurfaceScatteringPack, wrap);
            if (Cfg.FoamWarmth > 0f)
            {
                Color fc = (Color)mpb.GetVector(_FoamDiffuseColor);
                fc.r = Mathf.Min(1f, fc.r + Cfg.FoamWarmth);
                fc.g = Mathf.Min(1f, fc.g + Cfg.FoamWarmth * 0.7f);
                mpb.SetColor(_FoamDiffuseColor, fc);
            }
        }
    }

    [HarmonyPatch(typeof(Foam), MethodType.Constructor, typeof(Water), typeof(Foam.Data))]
    internal static class FoamCtorPatch
    {
        static readonly FieldInfo resField = AccessTools.Field(typeof(Foam), "resolution");
        static readonly FieldInfo wwField = AccessTools.Field(typeof(Foam), "windWaves");
        static readonly FieldInfo dataField = AccessTools.Field(typeof(Foam), "data");
        static readonly FieldInfo ssField = AccessTools.Field(typeof(Foam.Data), "Supersampling");
        [HarmonyPostfix]
        static void Postfix(Foam __instance)
        {
            if (Cfg.FoamSupersampling <= 1f) return;
            object data = dataField.GetValue(__instance);
            if (data == null) return;
            ssField.SetValue(data, Cfg.FoamSupersampling);
            var ww = (WindWaves)wwField.GetValue(__instance);
            if (ww != null) resField.SetValue(__instance, Mathf.RoundToInt(ww.FinalResolution * Cfg.FoamSupersampling));
        }
    }

    [HarmonyPatch(typeof(Foam), "OnProfilesChanged")]
    internal static class FoamTuningPatch
    {
        static readonly FieldInfo waterField = AccessTools.Field(typeof(Foam), "water");
        static readonly FieldInfo threshField = AccessTools.Field(typeof(Foam), "foamThreshold");
        static readonly FieldInfo fadingField = AccessTools.Field(typeof(Foam), "foamFadingFactor");
        static readonly FieldInfo intensField = AccessTools.Field(typeof(Foam), "foamIntensity");
        static readonly FieldInfo shoreField = AccessTools.Field(typeof(Foam), "foamShoreExtent");
        static readonly FieldInfo resField = AccessTools.Field(typeof(Foam), "resolution");
        static readonly int _FoamParameters = Shader.PropertyToID("_FoamParameters");
        [HarmonyPostfix]
        static void Postfix(Foam __instance)
        {
            float thresh = (float)threshField.GetValue(__instance) * Cfg.FoamThresholdScale;
            threshField.SetValue(__instance, thresh);
            fadingField.SetValue(__instance, Cfg.FoamFadingOverride);
            var water = (Water)waterField.GetValue(__instance);
            if (water == null) return;
            float intensity = (float)intensField.GetValue(__instance);
            float shore = (float)shoreField.GetValue(__instance);
            int res = (int)resField.GetValue(__instance);
            water.Renderer.PropertyBlock.SetVector(_FoamParameters, new Vector4(
                intensity * 0.6f, thresh * res / 2048f * 0.5f, shore * shore / 150f, Cfg.FoamFadingOverride));
        }
    }

    [HarmonyPatch(typeof(WindWaves), "OnProfilesChanged")]
    internal static class WaveSpectrumPatch
    {
        static readonly FieldInfo dirField = AccessTools.Field(typeof(WindWaves), "spectrumDirectionality");
        static readonly FieldInfo windField = AccessTools.Field(typeof(WindWaves), "windSpeedMagnitude");
        [HarmonyPostfix]
        static void Postfix(WindWaves __instance)
        {
            float wind = (float)windField.GetValue(__instance);
            float t = Mathf.Clamp01((wind - 3f) / 15f);
            float physical = Mathf.Lerp(0.3f, 0.85f, t);
            float current = (float)dirField.GetValue(__instance);
            dirField.SetValue(__instance, Mathf.Lerp(current, physical, 0.6f));
        }
    }

    [HarmonyPatch(typeof(Weather), "UpdateVisualAspects")]
    internal static class WeatherPatch
    {
        static readonly FieldInfo seaField = AccessTools.Field(typeof(Weather), "sea");
        static float smoothedDt;
        static bool degraded;
        [HarmonyPostfix]
        static void Postfix(Weather __instance)
        {
            var sea = (Water)seaField.GetValue(__instance);
            if (sea == null || sea.DynamicWater == null) return;
            if (Cfg.AdaptiveQuality)
            {
                smoothedDt = Mathf.Lerp(smoothedDt, Time.unscaledDeltaTime, 0.1f);
                float target = Cfg.AdaptiveTargetMs / 1000f;
                if (smoothedDt > target * 1.2f) { sea.DynamicWater.FoamResolutionScale = 1.5f; degraded = true; }
                else if (degraded && smoothedDt < target * 0.9f) { sea.DynamicWater.FoamResolutionScale = 2f; degraded = false; }
                else if (!degraded) sea.DynamicWater.FoamResolutionScale = 2f;
            }
            else
            {
                sea.DynamicWater.FoamResolutionScale = 2f;
            }
        }
    }

    [HarmonyPatch(typeof(Weather), "RenderReflectionProbe", new Type[0])]
    internal static class ReflectionPatch
    {
        static readonly FieldInfo probeField = AccessTools.Field(typeof(Weather), "mainReflectionProbe");
        static int frameCounter;
        [HarmonyPrefix]
        static bool Prefix(Weather __instance)
        {
            var probe = (ReflectionProbe)probeField.GetValue(__instance);
            if (probe != null && probe.resolution < 512) probe.resolution = 512;
            frameCounter++;
            return frameCounter % Cfg.ReflectionUpdateInterval == 0;
        }
    }

    [HarmonyPatch(typeof(SectionViewCameraController), "SetDistancePreset")]
    internal static class TessellationPatch
    {
        static readonly FieldInfo seaField = AccessTools.Field(typeof(SectionViewCameraController), "sea");
        [HarmonyPostfix]
        static void Postfix(SectionViewCameraController __instance)
        {
            var sea = (Water)seaField.GetValue(__instance);
            if (sea != null) sea.Materials.TesselationFactor = 1f;
        }
    }

    [HarmonyPatch(typeof(HullEffectsRenderer), "Entity_LevelOfDetailChanged")]
    internal static class HullWetnessPatch
    {
        static readonly FieldInfo usField = AccessTools.Field(typeof(HullEffectsRenderer), "userSettings");
        static readonly FieldInfo gsField = AccessTools.Property(typeof(UserSettings), "GraphicsSettings") != null
            ? null : AccessTools.Field(typeof(UserSettings), "graphicsSettings");
        static readonly PropertyInfo gsProp = AccessTools.Property(typeof(UserSettings), "GraphicsSettings");
        static GraphicsSettings GetGS(object us)
        {
            if (gsProp != null) return (GraphicsSettings)gsProp.GetValue(us);
            if (gsField != null) return (GraphicsSettings)gsField.GetValue(us);
            return null;
        }
        [HarmonyPrefix]
        static void Prefix(ref QualitySetting __state)
        {
            __state = QualitySetting.Simple;
            object us = usField?.GetValue(null);
            if (us == null) return;
            var gs = GetGS(us);
            if (gs == null) return;
            __state = gs.water;
            if (gs.water < QualitySetting.VeryGood) gs.water = QualitySetting.VeryGood;
        }
        [HarmonyPostfix]
        static void Postfix(QualitySetting __state)
        {
            object us = usField?.GetValue(null);
            if (us == null) return;
            var gs = GetGS(us);
            if (gs != null) gs.water = __state;
        }
    }

    [HarmonyPatch(typeof(LightWaterEffects), "OnEnable")]
    internal static class CausticsPatch
    {
        static readonly FieldInfo intField = AccessTools.Field(typeof(LightWaterEffects), "intensity");
        static readonly FieldInfo surfField = AccessTools.Field(typeof(LightWaterEffects), "surfaceCausticsIntensity");
        [HarmonyPostfix]
        static void Postfix(LightWaterEffects __instance)
        {
            if ((float)intField.GetValue(__instance) < 1.5f) intField.SetValue(__instance, 1.5f);
            if ((float)surfField.GetValue(__instance) < 0.75f) surfField.SetValue(__instance, 0.75f);
        }
    }

    [HarmonyPatch(typeof(WaterSubsurfaceScattering), "OnWaterRender", typeof(Camera), typeof(UnityEngine.Rendering.ScriptableRenderContext?))]
    internal static class SSSThrottlePatch
    {
        static int fc;
        [HarmonyPrefix]
        static bool Prefix() { fc++; return Cfg.SSSUpdateInterval <= 1 || fc % Cfg.SSSUpdateInterval == 0; }
    }

    [HarmonyPatch(typeof(WaterSubsurfaceScattering), "OnWaterRender", typeof(Camera))]
    internal static class SSSThrottleLegacyPatch
    {
        static int fc;
        [HarmonyPrefix]
        static bool Prefix() { fc++; return Cfg.SSSUpdateInterval <= 1 || fc % Cfg.SSSUpdateInterval == 0; }
    }

    [HarmonyPatch(typeof(Foam), "Update")]
    internal static class FoamThrottlePatch
    {
        static int fc;
        [HarmonyPrefix]
        static bool Prefix() { fc++; return Cfg.FoamUpdateInterval <= 1 || fc % Cfg.FoamUpdateInterval == 0; }
    }

    [HarmonyPatch(typeof(WavesRendererFFT), MethodType.Constructor, typeof(Water), typeof(WindWaves), typeof(WavesRendererFFT.Data))]
    internal static class ComputeShaderForcePatch
    {
        static readonly FieldInfo dataField = AccessTools.Field(typeof(WavesRendererFFT), "data");
        static readonly FieldInfo fpsField = AccessTools.Field(typeof(WavesRendererFFT.Data), "ForcePixelShader");
        [HarmonyPostfix]
        static void Postfix(WavesRendererFFT __instance)
        {
            object data = dataField.GetValue(__instance);
            if (data != null) fpsField.SetValue(data, false);
        }
    }

    [HarmonyPatch(typeof(TPPCameraController), "LateUpdate")]
    internal static class TPPSprayPatch
    {
        static readonly FieldInfo spField = AccessTools.Field(typeof(TPPCameraController), "seaSpray");
        [HarmonyPostfix]
        static void Postfix(TPPCameraController __instance)
        {
            if (!Cfg.ForceSpray) return;
            var sp = (Spray)spField?.GetValue(__instance);
            if (sp != null && !sp.enabled) sp.enabled = true;
        }
    }

    [HarmonyPatch(typeof(FreeFlyCameraController), "Update")]
    internal static class FreeFlySprayPatch
    {
        static readonly FieldInfo spField = AccessTools.Field(typeof(FreeFlyCameraController), "seaSpray");
        [HarmonyPostfix]
        static void Postfix(FreeFlyCameraController __instance)
        {
            if (!Cfg.ForceSpray) return;
            var sp = (Spray)spField?.GetValue(__instance);
            if (sp != null && !sp.enabled) sp.enabled = true;
        }
    }

    [HarmonyPatch(typeof(FPPCameraController), "Update")]
    internal static class FPPSprayPatch
    {
        static readonly FieldInfo spField = AccessTools.Field(typeof(FPPCameraController), "seaSpray");
        [HarmonyPostfix]
        static void Postfix(FPPCameraController __instance)
        {
            if (!Cfg.ForceSpray) return;
            var sp = (Spray)spField?.GetValue(__instance);
            if (sp != null && !sp.enabled) sp.enabled = true;
        }
    }
}
