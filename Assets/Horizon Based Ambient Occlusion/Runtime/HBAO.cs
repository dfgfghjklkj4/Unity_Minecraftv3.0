using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_VR
using XRSettings = UnityEngine.XR.XRSettings;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace HorizonBasedAmbientOcclusion
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView, AddComponentMenu("Image Effects/HBAO")]
    [RequireComponent(typeof(Camera))]
    public class HBAO : MonoBehaviour
    {
        public enum Preset
        {
            FastestPerformance,
            FastPerformance,
            Normal,
            HighQuality,
            HighestQuality,
            Custom
        }

        public enum PipelineStage
        {
            BeforeImageEffectsOpaque,
            AfterLighting,
            BeforeReflections
        }

        public enum Quality
        {
            Lowest,
            Low,
            Medium,
            High,
            Highest
        }

        public enum Resolution
        {
            Full,
            Half
        }

        public enum NoiseType
        {
            Dither,
            InterleavedGradientNoise,
            SpatialDistribution
        }

        public enum Deinterleaving
        {
            Disabled,
            x4
        }

        public enum DebugMode
        {
            Disabled,
            AOOnly,
            ColorBleedingOnly,
            SplitWithoutAOAndWithAO,
            SplitWithAOAndAOOnly,
            SplitWithoutAOAndAOOnly,
            ViewNormals
        }

        public enum BlurType
        {
            None,
            Narrow,
            Medium,
            Wide,
            ExtraWide
        }

        public enum PerPixelNormals
        {
            GBuffer,
            Camera,
            Reconstruct
        }

        public enum VarianceClipping
        {
            Disabled,
            _4Tap,
            _8Tap
        }

        public Shader hbaoShader;

        [Serializable]
        public struct Presets
        {
            public Preset preset;

            [SerializeField]
            public static Presets defaults
            {
                get
                {
                    return new Presets
                    {
                        preset = Preset.Normal
                    };
                }
            }
        }

        [Serializable]
        public struct GeneralSettings
        {
            [Tooltip("The stage the AO is injected into the rendering pipeline.")]
            [Space(6)]
            public PipelineStage pipelineStage;

            [Tooltip("The quality of the AO.")]
            [Space(10)]
            public Quality quality;

            [Tooltip("The deinterleaving factor.")]
            public Deinterleaving deinterleaving;

            [Tooltip("The resolution at which the AO is calculated.")]
            public Resolution resolution;

            [Tooltip("The type of noise to use.")]
            [Space(10)]
            public NoiseType noiseType;

            [Tooltip("The debug mode actually displayed on screen.")]
            [Space(10)]
            public DebugMode debugMode;

            [SerializeField]
            public static GeneralSettings defaults
            {
                get
                {
                    return new GeneralSettings
                    {
                        pipelineStage = PipelineStage.BeforeImageEffectsOpaque,
                        quality = Quality.Medium,
                        deinterleaving = Deinterleaving.Disabled,
                        resolution = Resolution.Full,
                        noiseType = NoiseType.Dither,
                        debugMode = DebugMode.Disabled
                    };
                }
            }
        }

        [Serializable]
        public struct AOSettings
        {
            [Tooltip("AO radius: this is the distance outside which occluders are ignored.")]
            [Space(6), Range(0.25f, 5f)]
            public float radius;

            [Tooltip("Maximum radius in pixels: this prevents the radius to grow too much with close-up " +
                      "object and impact on performances.")]
            [Range(16, 256)]
            public float maxRadiusPixels;

            [Tooltip("For low-tessellated geometry, occlusion variations tend to appear at creases and " +
                     "ridges, which betray the underlying tessellation. To remove these artifacts, we use " +
                     "an angle bias parameter which restricts the hemisphere.")]
            [Range(0, 0.5f)]
            public float bias;

            [Tooltip("This value allows to scale up the ambient occlusion values.")]
            [Range(0, 4)]
            public float intensity;

            [Tooltip("Enable/disable MultiBounce approximation.")]
            public bool useMultiBounce;

            [Tooltip("MultiBounce approximation influence.")]
            [Range(0, 1)]
            public float multiBounceInfluence;

            [Tooltip("The amount of AO offscreen samples are contributing.")]
            [Range(0, 1)]
            public float offscreenSamplesContribution;

            [Tooltip("The max distance to display AO.")]
            [Space(10)]
            public float maxDistance;

            [Tooltip("The distance before max distance at which AO start to decrease.")]
            public float distanceFalloff;

            [Tooltip("The type of per pixel normals to use.")]
            [Space(10)]
            public PerPixelNormals perPixelNormals;

            [Tooltip("This setting allow you to set the base color if the AO, the alpha channel value is unused.")]
            [Space(10)]
            public Color baseColor;

            [SerializeField]
            public static AOSettings defaults
            {
                get
                {
                    return new AOSettings
                    {
                        radius = 0.8f,
                        maxRadiusPixels = 128f,
                        bias = 0.05f,
                        intensity = 1f,
                        useMultiBounce = false,
                        multiBounceInfluence = 1f,
                        offscreenSamplesContribution = 0f,
                        maxDistance = 150f,
                        distanceFalloff = 50f,
                        perPixelNormals = PerPixelNormals.GBuffer,
                        baseColor = Color.black
                    };
                }
            }
        }

        [Serializable]
        public struct TemporalFilterSettings
        {
            [Space(6)]
            public bool enabled;

            [Tooltip("The type of variance clipping to use.")]
            public VarianceClipping varianceClipping;

            [SerializeField]
            public static TemporalFilterSettings defaults
            {
                get
                {
                    return new TemporalFilterSettings
                    {
                        enabled = false,
                        varianceClipping = VarianceClipping._4Tap
                    };
                }
            }
        }

        [Serializable]
        public struct BlurSettings
        {
            [Tooltip("The type of blur to use.")]
            [Space(6)]
            public BlurType type;

            [Tooltip("This parameter controls the depth-dependent weight of the bilateral filter, to " +
                     "avoid bleeding across edges. A zero sharpness is a pure Gaussian blur. Increasing " +
                     "the blur sharpness removes bleeding by using lower weights for samples with large " +
                     "depth delta from the current pixel.")]
            [Space(10), Range(0, 16)]
            public float sharpness;

            [SerializeField]
            public static BlurSettings defaults
            {
                get
                {
                    return new BlurSettings
                    {
                        type = BlurType.Medium,
                        sharpness = 8f
                    };
                }
            }
        }

        [Serializable]
        public struct ColorBleedingSettings
        {
            [Space(6)]
            public bool enabled;

            [Tooltip("This value allows to control the saturation of the color bleeding.")]
            [Space(10), Range(0, 4)]
            public float saturation;

            [Tooltip("This value allows to scale the contribution of the color bleeding samples.")]
            [Range(0, 32)]
            public float albedoMultiplier;

            [Tooltip("Use masking on emissive pixels")]
            [Range(0, 1)]
            public float brightnessMask;

            [Tooltip("Brightness level where masking starts/ends")]
            [MinMaxSlider(0, 2)]
            public Vector2 brightnessMaskRange;

            [SerializeField]
            public static ColorBleedingSettings defaults
            {
                get
                {
                    return new ColorBleedingSettings
                    {
                        enabled = false,
                        saturation = 1f,
                        albedoMultiplier = 4f,
                        brightnessMask = 1f,
                        brightnessMaskRange = new Vector2(0.0f, 0.5f)
                    };
                }
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class SettingsGroup : Attribute { }

        [SerializeField, SettingsGroup]
        private Presets m_Presets = Presets.defaults;
        public Presets presets
        {
            get { return m_Presets; }
            set { m_Presets = value; }
        }

        [SerializeField, SettingsGroup]
        private GeneralSettings m_GeneralSettings = GeneralSettings.defaults;
        public GeneralSettings generalSettings
        {
            get { return m_GeneralSettings; }
            set { m_GeneralSettings = value; }
        }

        [SerializeField, SettingsGroup]
        private AOSettings m_AOSettings = AOSettings.defaults;
        public AOSettings aoSettings
        {
            get { return m_AOSettings; }
            set { m_AOSettings = value; }
        }

        [SerializeField, SettingsGroup]
        private TemporalFilterSettings m_TemporalFilterSettings = TemporalFilterSettings.defaults;
        public TemporalFilterSettings temporalFilterSettings
        {
            get { return m_TemporalFilterSettings; }
            set { m_TemporalFilterSettings = value; }
        }

        [SerializeField, SettingsGroup]
        private BlurSettings m_BlurSettings = BlurSettings.defaults;
        public BlurSettings blurSettings
        {
            get { return m_BlurSettings; }
            set { m_BlurSettings = value; }
        }

        [SerializeField, SettingsGroup]
        private ColorBleedingSettings m_ColorBleedingSettings = ColorBleedingSettings.defaults;
        public ColorBleedingSettings colorBleedingSettings
        {
            get { return m_ColorBleedingSettings; }
            set { m_ColorBleedingSettings = value; }
        }

        public class MinMaxSliderAttribute : PropertyAttribute
        {
            public readonly float max;
            public readonly float min;

            public MinMaxSliderAttribute(float min, float max)
            {
                this.min = min;
                this.max = max;
            }
        }

        public Preset GetCurrentPreset()
        {
            return m_Presets.preset;
        }

        public void ApplyPreset(Preset preset)
        {
            if (preset == Preset.Custom)
            {
                m_Presets.preset = preset;
                return;
            }

            var debugMode = generalSettings.debugMode;

            m_GeneralSettings = GeneralSettings.defaults;
            m_AOSettings = AOSettings.defaults;
            m_ColorBleedingSettings = ColorBleedingSettings.defaults;
            m_BlurSettings = BlurSettings.defaults;

            SetDebugMode(debugMode);

            switch (preset)
            {
                case Preset.FastestPerformance:
                    SetQuality(Quality.Lowest);
                    SetAoRadius(0.5f);
                    SetAoMaxRadiusPixels(64.0f);
                    SetBlurType(BlurType.ExtraWide);
                    break;
                case Preset.FastPerformance:
                    SetQuality(Quality.Low);
                    SetAoRadius(0.5f);
                    SetAoMaxRadiusPixels(64.0f);
                    SetBlurType(BlurType.Wide);
                    break;
                case Preset.HighQuality:
                    SetQuality(Quality.High);
                    SetAoRadius(1.0f);
                    break;
                case Preset.HighestQuality:
                    SetQuality(Quality.Highest);
                    SetAoRadius(1.2f);
                    SetAoMaxRadiusPixels(256.0f);
                    SetBlurType(BlurType.Narrow);
                    break;
                case Preset.Normal:
                default:
                    break;
            }

            m_Presets.preset = preset;
        }

        public PipelineStage GetPipelineStage()
        {
            return m_GeneralSettings.pipelineStage;
        }

        public void SetPipelineStage(PipelineStage pipelineStage)
        {
            m_GeneralSettings.pipelineStage = pipelineStage;
        }

        public Quality GetQuality()
        {
            return m_GeneralSettings.quality;
        }

        public void SetQuality(Quality quality)
        {
            m_GeneralSettings.quality = quality;
        }

        public Deinterleaving GetDeinterleaving()
        {
            return m_GeneralSettings.deinterleaving;
        }

        public void SetDeinterleaving(Deinterleaving deinterleaving)
        {
            m_GeneralSettings.deinterleaving = deinterleaving;
        }

        public Resolution GetResolution()
        {
            return m_GeneralSettings.resolution;
        }

        public void SetResolution(Resolution resolution)
        {
            m_GeneralSettings.resolution = resolution;
        }

        public NoiseType GetNoiseType()
        {
            return m_GeneralSettings.noiseType;
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            m_GeneralSettings.noiseType = noiseType;
        }

        public DebugMode GetDebugMode()
        {
            return m_GeneralSettings.debugMode;
        }

        public void SetDebugMode(DebugMode debugMode)
        {
            m_GeneralSettings.debugMode = debugMode;
        }

        public float GetAoRadius()
        {
            return m_AOSettings.radius;
        }

        public void SetAoRadius(float radius)
        {
            m_AOSettings.radius = Mathf.Clamp(radius, 0.25f, 5);
        }

        public float GetAoMaxRadiusPixels()
        {
            return m_AOSettings.maxRadiusPixels;
        }

        public void SetAoMaxRadiusPixels(float maxRadiusPixels)
        {
            m_AOSettings.maxRadiusPixels = Mathf.Clamp(maxRadiusPixels, 16, 256);
        }

        public float GetAoBias()
        {
            return m_AOSettings.bias;
        }

        public void SetAoBias(float bias)
        {
            m_AOSettings.bias = Mathf.Clamp(bias, 0, 0.5f);
        }

        public float GetAoOffscreenSamplesContribution()
        {
            return m_AOSettings.offscreenSamplesContribution;
        }

        public void SetAoOffscreenSamplesContribution(float contribution)
        {
            m_AOSettings.offscreenSamplesContribution = Mathf.Clamp01(contribution);
        }

        public float GetAoMaxDistance()
        {
            return m_AOSettings.maxDistance;
        }

        public void SetAoMaxDistance(float maxDistance)
        {
            m_AOSettings.maxDistance = maxDistance;
        }

        public float GetAoDistanceFalloff()
        {
            return m_AOSettings.distanceFalloff;
        }

        public void SetAoDistanceFalloff(float distanceFalloff)
        {
            m_AOSettings.distanceFalloff = distanceFalloff;
        }

        public PerPixelNormals GetAoPerPixelNormals()
        {
            return m_AOSettings.perPixelNormals;
        }

        public void SetAoPerPixelNormals(PerPixelNormals perPixelNormals)
        {
            m_AOSettings.perPixelNormals = perPixelNormals;
        }

        public Color GetAoColor()
        {
            return m_AOSettings.baseColor;
        }

        public void SetAoColor(Color color)
        {
            m_AOSettings.baseColor = color;
        }

        public float GetAoIntensity()
        {
            return m_AOSettings.intensity;
        }

        public void SetAoIntensity(float intensity)
        {
            m_AOSettings.intensity = Mathf.Clamp(intensity, 0, 4);
        }

        public bool UseMultiBounce()
        {
            return m_AOSettings.useMultiBounce;
        }

        public void EnableMultiBounce(bool enabled = true)
        {
            m_AOSettings.useMultiBounce = enabled;
        }

        public float GetAoMultiBounceInfluence()
        {
            return m_AOSettings.multiBounceInfluence;
        }

        public void SetAoMultiBounceInfluence(float multiBounceInfluence)
        {
            m_AOSettings.multiBounceInfluence = Mathf.Clamp01(multiBounceInfluence);
        }

        public bool IsTemporalFilterEnabled()
        {
            return m_TemporalFilterSettings.enabled;
        }

        public void EnableTemporalFilter(bool enabled = true)
        {
            m_TemporalFilterSettings.enabled = enabled;
        }

        public VarianceClipping GetTemporalFilterVarianceClipping()
        {
            return m_TemporalFilterSettings.varianceClipping;
        }

        public void SetTemporalFilterVarianceClipping(VarianceClipping varianceClipping)
        {
            m_TemporalFilterSettings.varianceClipping = varianceClipping;
        }

        public BlurType GetBlurType()
        {
            return m_BlurSettings.type;
        }

        public void SetBlurType(BlurType blurType)
        {
            m_BlurSettings.type = blurType;
        }

        public float GetBlurSharpness()
        {
            return m_BlurSettings.sharpness;
        }

        public void SetBlurSharpness(float sharpness)
        {
            m_BlurSettings.sharpness = Mathf.Clamp(sharpness, 0, 16);
        }

        public bool IsColorBleedingEnabled()
        {
            return m_ColorBleedingSettings.enabled;
        }

        public void EnableColorBleeding(bool enabled = true)
        {
            m_ColorBleedingSettings.enabled = enabled;
        }

        public float GetColorBleedingSaturation()
        {
            return m_ColorBleedingSettings.saturation;
        }

        public void SetColorBleedingSaturation(float saturation)
        {
            m_ColorBleedingSettings.saturation = Mathf.Clamp(saturation, 0, 4);
        }

        public float GetColorBleedingAlbedoMultiplier()
        {
            return m_ColorBleedingSettings.albedoMultiplier;
        }

        public void SetColorBleedingAlbedoMultiplier(float albedoMultiplier)
        {
            m_ColorBleedingSettings.albedoMultiplier = Mathf.Clamp(albedoMultiplier, 0, 32);
        }

        public float GetColorBleedingBrightnessMask()
        {
            return m_ColorBleedingSettings.brightnessMask;
        }

        public void SetColorBleedingBrightnessMask(float brightnessMask)
        {
            m_ColorBleedingSettings.brightnessMask = Mathf.Clamp01(brightnessMask);
        }

        public Vector2 GetColorBleedingBrightnessMaskRange()
        {
            return m_ColorBleedingSettings.brightnessMaskRange;
        }

        public void SetColorBleedingBrightnessMaskRange(Vector2 brightnessMaskRange)
        {
            brightnessMaskRange.x = Mathf.Clamp(brightnessMaskRange.x, 0, 2);
            brightnessMaskRange.y = Mathf.Clamp(brightnessMaskRange.y, 0, 2);
            brightnessMaskRange.x = Mathf.Min(brightnessMaskRange.x, brightnessMaskRange.y);
            m_ColorBleedingSettings.brightnessMaskRange = brightnessMaskRange;
        }

        private static class Pass
        {
            public const int AO = 0;
            public const int AO_Deinterleaved = 1;

            public const int Deinterleave_Depth = 2;
            public const int Deinterleave_Normals = 3;
            public const int Atlas_AO_Deinterleaved = 4;
            public const int Reinterleave_AO = 5;

            public const int Blur = 6;

            public const int Temporal_Filter = 7;

            public const int Copy = 8;

            public const int Composite = 9;
            public const int Composite_AfterLighting = 10;
            public const int Composite_BeforeReflections = 11;

            public const int Debug_ViewNormals = 12;
        }

        private static class ShaderProperties
        {
            public static int mainTex;
            public static int hbaoTex;
            public static int tempTex;
            public static int tempTex2;
            public static int noiseTex;
            public static int depthTex;
            public static int normalsTex;
            public static int[] depthSliceTex;
            public static int[] normalsSliceTex;
            public static int[] aoSliceTex;
            public static int[] deinterleaveOffset;
            public static int atlasOffset;
            public static int jitter;
            public static int uvTransform;
            public static int inputTexelSize;
            public static int aoTexelSize;
            public static int deinterleavedAOTexelSize;
            public static int reinterleavedAOTexelSize;
            public static int uvToView;
            public static int worldToCameraMatrix;
            public static int targetScale;
            public static int radius;
            public static int maxRadiusPixels;
            public static int negInvRadius2;
            public static int angleBias;
            public static int aoMultiplier;
            public static int intensity;
            public static int multiBounceInfluence;
            public static int offscreenSamplesContrib;
            public static int maxDistance;
            public static int distanceFalloff;
            public static int baseColor;
            public static int colorBleedSaturation;
            public static int albedoMultiplier;
            public static int colorBleedBrightnessMask;
            public static int colorBleedBrightnessMaskRange;
            public static int blurDeltaUV;
            public static int blurSharpness;
            public static int temporalParams;

            static ShaderProperties()
            {
                mainTex = Shader.PropertyToID("_MainTex");
                hbaoTex = Shader.PropertyToID("_HBAOTex");
                tempTex = Shader.PropertyToID("_TempTex");
                tempTex2 = Shader.PropertyToID("_TempTex2");
                noiseTex = Shader.PropertyToID("_NoiseTex");
                depthTex = Shader.PropertyToID("_DepthTex");
                normalsTex = Shader.PropertyToID("_NormalsTex");
                depthSliceTex = new int[4 * 4];
                normalsSliceTex = new int[4 * 4];
                aoSliceTex = new int[4 * 4];
                for (int i = 0; i < 4 * 4; i++)
                {
                    depthSliceTex[i] = Shader.PropertyToID("_DepthSliceTex" + i);
                    normalsSliceTex[i] = Shader.PropertyToID("_NormalsSliceTex" + i);
                    aoSliceTex[i] = Shader.PropertyToID("_AOSliceTex" + i);
                }
                deinterleaveOffset = new int[] {
                Shader.PropertyToID("_Deinterleave_Offset00"),
                Shader.PropertyToID("_Deinterleave_Offset10"),
                Shader.PropertyToID("_Deinterleave_Offset01"),
                Shader.PropertyToID("_Deinterleave_Offset11")
            };
                atlasOffset = Shader.PropertyToID("_AtlasOffset");
                jitter = Shader.PropertyToID("_Jitter");
                uvTransform = Shader.PropertyToID("_UVTransform");
                inputTexelSize = Shader.PropertyToID("_Input_TexelSize");
                aoTexelSize = Shader.PropertyToID("_AO_TexelSize");
                deinterleavedAOTexelSize = Shader.PropertyToID("_DeinterleavedAO_TexelSize");
                reinterleavedAOTexelSize = Shader.PropertyToID("_ReinterleavedAO_TexelSize");
                uvToView = Shader.PropertyToID("_UVToView");
                worldToCameraMatrix = Shader.PropertyToID("_WorldToCameraMatrix");
                targetScale = Shader.PropertyToID("_TargetScale");
                radius = Shader.PropertyToID("_Radius");
                maxRadiusPixels = Shader.PropertyToID("_MaxRadiusPixels");
                negInvRadius2 = Shader.PropertyToID("_NegInvRadius2");
                angleBias = Shader.PropertyToID("_AngleBias");
                aoMultiplier = Shader.PropertyToID("_AOmultiplier");
                intensity = Shader.PropertyToID("_Intensity");
                multiBounceInfluence = Shader.PropertyToID("_MultiBounceInfluence");
                offscreenSamplesContrib = Shader.PropertyToID("_OffscreenSamplesContrib");
                maxDistance = Shader.PropertyToID("_MaxDistance");
                distanceFalloff = Shader.PropertyToID("_DistanceFalloff");
                baseColor = Shader.PropertyToID("_BaseColor");
                colorBleedSaturation = Shader.PropertyToID("_ColorBleedSaturation");
                albedoMultiplier = Shader.PropertyToID("_AlbedoMultiplier");
                colorBleedBrightnessMask = Shader.PropertyToID("_ColorBleedBrightnessMask");
                colorBleedBrightnessMaskRange = Shader.PropertyToID("_ColorBleedBrightnessMaskRange");
                blurDeltaUV = Shader.PropertyToID("_BlurDeltaUV");
                blurSharpness = Shader.PropertyToID("_BlurSharpness");
                temporalParams = Shader.PropertyToID("_TemporalParams");
            }

            public static string GetOrthographicOrDeferredKeyword(bool orthographic, GeneralSettings settings)
            {
                // need to check that integrationStage is not BeforeImageEffectOpaque as Gbuffer0 is not available in this case
                return orthographic ? "ORTHOGRAPHIC_PROJECTION" : settings.pipelineStage != PipelineStage.BeforeImageEffectsOpaque ? "DEFERRED_SHADING" : "__";
            }

            public static string GetDirectionsKeyword(GeneralSettings settings)
            {
                switch (settings.quality)
                {
                    case Quality.Lowest:
                        return "DIRECTIONS_3";
                    case Quality.Low:
                        return "DIRECTIONS_4";
                    case Quality.Medium:
                        return "DIRECTIONS_6";
                    case Quality.High:
                        return "DIRECTIONS_8";
                    case Quality.Highest:
                        return "DIRECTIONS_8";
                    default:
                        return "DIRECTIONS_6";
                }
            }

            public static string GetStepsKeyword(GeneralSettings settings)
            {
                switch (settings.quality)
                {
                    case Quality.Lowest:
                        return "STEPS_2";
                    case Quality.Low:
                        return "STEPS_3";
                    case Quality.Medium:
                        return "STEPS_4";
                    case Quality.High:
                        return "STEPS_4";
                    case Quality.Highest:
                        return "STEPS_6";
                    default:
                        return "STEPS_4";
                }
            }

            public static string GetNoiseKeyword(GeneralSettings settings)
            {
                switch (settings.noiseType)
                {
                    case NoiseType.InterleavedGradientNoise:
                        return "INTERLEAVED_GRADIENT_NOISE";
                    case NoiseType.Dither:
                    case NoiseType.SpatialDistribution:
                    default:
                        return "__";
                }
            }

            public static string GetDeinterleavingKeyword(GeneralSettings settings)
            {
                switch (settings.deinterleaving)
                {
                    case Deinterleaving.x4:
                        return "DEINTERLEAVED";
                    case Deinterleaving.Disabled:
                    default:
                        return "__";
                }
            }

            public static string GetDebugKeyword(GeneralSettings settings)
            {
                switch (settings.debugMode)
                {
                    case DebugMode.AOOnly:
                        return "DEBUG_AO";
                    case DebugMode.ColorBleedingOnly:
                        return "DEBUG_COLORBLEEDING";
                    case DebugMode.SplitWithoutAOAndWithAO:
                        return "DEBUG_NOAO_AO";
                    case DebugMode.SplitWithAOAndAOOnly:
                        return "DEBUG_AO_AOONLY";
                    case DebugMode.SplitWithoutAOAndAOOnly:
                        return "DEBUG_NOAO_AOONLY";
                    case DebugMode.Disabled:
                    default:
                        return "__";
                }
            }

            public static string GetMultibounceKeyword(AOSettings settings)
            {
                return settings.useMultiBounce ? "MULTIBOUNCE" : "__";
            }

            public static string GetOffscreenSamplesContributionKeyword(AOSettings settings)
            {
                return settings.offscreenSamplesContribution > 0 ? "OFFSCREEN_SAMPLES_CONTRIBUTION" : "__";
            }

            public static string GetPerPixelNormalsKeyword(AOSettings settings)
            {
                switch (settings.perPixelNormals)
                {
                    case PerPixelNormals.Camera:
                        return "NORMALS_CAMERA";
                    case PerPixelNormals.Reconstruct:
                        return "NORMALS_RECONSTRUCT";
                    case PerPixelNormals.GBuffer:
                    default:
                        return "__";
                }
            }

            public static string GetBlurRadiusKeyword(BlurSettings settings)
            {
                switch (settings.type)
                {
                    case BlurType.Narrow:
                        return "BLUR_RADIUS_2";
                    case BlurType.Medium:
                        return "BLUR_RADIUS_3";
                    case BlurType.Wide:
                        return "BLUR_RADIUS_4";
                    case BlurType.ExtraWide:
                        return "BLUR_RADIUS_5";
                    case BlurType.None:
                    default:
                        return "BLUR_RADIUS_3";
                }
            }

            public static string GetVarianceClippingKeyword(TemporalFilterSettings settings)
            {
                switch (settings.varianceClipping)
                {
                    case VarianceClipping._4Tap:
                        return "VARIANCE_CLIPPING_4TAP";
                    case VarianceClipping._8Tap:
                        return "VARIANCE_CLIPPING_8TAP";
                    case VarianceClipping.Disabled:
                    default:
                        return "__";
                }
            }

            public static string GetColorBleedingKeyword(ColorBleedingSettings settings)
            {
                return settings.enabled ? "COLOR_BLEEDING" : "__";
            }

            public static string GetLightingLogEncodedKeyword(bool hdr)
            {
                return hdr ? "__" : "LIGHTING_LOG_ENCODED";
            }
        }

        private static class MersenneTwister
        {
            // Mersenne-Twister random numbers in [0,1).
            public static float[] Numbers = new float[] {
            //0.463937f,0.340042f,0.223035f,0.468465f,0.322224f,0.979269f,0.031798f,0.973392f,0.778313f,0.456168f,0.258593f,0.330083f,0.387332f,0.380117f,0.179842f,0.910755f,
            //0.511623f,0.092933f,0.180794f,0.620153f,0.101348f,0.556342f,0.642479f,0.442008f,0.215115f,0.475218f,0.157357f,0.568868f,0.501241f,0.629229f,0.699218f,0.707733f
            0.556725f,0.005520f,0.708315f,0.583199f,0.236644f,0.992380f,0.981091f,0.119804f,0.510866f,0.560499f,0.961497f,0.557862f,0.539955f,0.332871f,0.417807f,0.920779f,
            0.730747f,0.076690f,0.008562f,0.660104f,0.428921f,0.511342f,0.587871f,0.906406f,0.437980f,0.620309f,0.062196f,0.119485f,0.235646f,0.795892f,0.044437f,0.617311f
        };
        }

        private static readonly Vector2[] s_jitter = new Vector2[4 * 4];
        private static readonly float[] s_temporalRotations = { 60.0f, 300.0f, 180.0f, 240.0f, 120.0f, 0.0f };
        private static readonly float[] s_temporalOffsets = { 0.0f, 0.5f, 0.25f, 0.75f };

        private Material material { get; set; }
        private Camera hbaoCamera { get; set; }
        private CommandBuffer cmdBuffer { get; set; }
        private int width { get; set; }
        private int height { get; set; }
        private bool stereoActive { get; set; }
        private int numberOfEyes { get; set; }
        private int xrActiveEye { get; set; }
        private XRSettings.StereoRenderingMode stereoRenderingMode { get; set; }
        private int screenWidth { get; set; }
        private int screenHeight { get; set; }
        private int aoWidth { get; set; }
        private int aoHeight { get; set; }
        private int reinterleavedAoWidth { get; set; }
        private int reinterleavedAoHeight { get; set; }
        private int deinterleavedAoWidth { get; set; }
        private int deinterleavedAoHeight { get; set; }
        private int frameCount { get; set; }
        private bool motionVectorsSupported { get; set; }
        private RenderTexture aoHistoryBuffer { get; set; }
        private RenderTexture colorBleedingHistoryBuffer { get; set; }
        private Texture2D noiseTex { get; set; }

        private Mesh fullscreenTriangle
        {
            get
            {
                if (m_FullscreenTriangle != null)
                    return m_FullscreenTriangle;

                m_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                m_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
                m_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                m_FullscreenTriangle.UploadMeshData(false);

                return m_FullscreenTriangle;
            }
        }

        private CameraEvent cameraEvent
        {
            get
            {
                // Forces BeforeImageEffectsOpaque as soon  as normal display mode isn't used,
                // the component still display its integration stage but we force it here to ensure
                // all debug modes display correctly.
                if (generalSettings.debugMode != DebugMode.Disabled)
                    return CameraEvent.BeforeImageEffectsOpaque;

                switch (generalSettings.pipelineStage)
                {
                    case PipelineStage.BeforeReflections:
                        return CameraEvent.BeforeReflections;
                    case PipelineStage.AfterLighting:
                        return CameraEvent.AfterLighting;
                    case PipelineStage.BeforeImageEffectsOpaque:
                    default:
                        return CameraEvent.BeforeImageEffectsOpaque;
                }
            }
        }

        private bool isCommandBufferDirty
        {
            get
            {
                if (m_IsCommandBufferDirty || m_PreviousPipelineStage != generalSettings.pipelineStage || m_PreviousResolution != generalSettings.resolution ||
                    m_PreviousDebugMode != generalSettings.debugMode || m_PreviousAllowHDR != hbaoCamera.allowHDR || m_PreviousWidth != width || m_PreviousHeight != height ||
                    m_PreviousDeinterleaving != generalSettings.deinterleaving || m_PreviousBlurAmount != blurSettings.type ||
                    m_PreviousColorBleedingEnabled != colorBleedingSettings.enabled || m_PreviousTemporalFilterEnabled != temporalFilterSettings.enabled ||
                    m_PreviousRenderingPath != hbaoCamera.actualRenderingPath)
                {
                    m_PreviousPipelineStage = generalSettings.pipelineStage;
                    m_PreviousResolution = generalSettings.resolution;
                    m_PreviousDebugMode = generalSettings.debugMode;
                    m_PreviousAllowHDR = hbaoCamera.allowHDR;
                    m_PreviousWidth = width;
                    m_PreviousHeight = height;
                    m_PreviousDeinterleaving = generalSettings.deinterleaving;
                    m_PreviousBlurAmount = blurSettings.type;
                    m_PreviousColorBleedingEnabled = colorBleedingSettings.enabled;
                    m_PreviousTemporalFilterEnabled = temporalFilterSettings.enabled;
                    m_PreviousRenderingPath = hbaoCamera.actualRenderingPath;

                    return true;
                }

                return false;
            }
            set
            {
                m_IsCommandBufferDirty = value;
            }
        }

        private static RenderTextureFormat defaultHDRRenderTextureFormat
        {
            get
            {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_EDITOR
                RenderTextureFormat format = RenderTextureFormat.RGB111110Float;
#if UNITY_EDITOR
                var target = EditorUserBuildSettings.activeBuildTarget;
                if (target != BuildTarget.Android && target != BuildTarget.iOS && target != BuildTarget.tvOS && target != BuildTarget.Switch)
                    return RenderTextureFormat.DefaultHDR;
#endif // UNITY_EDITOR
                if (SystemInfo.SupportsRenderTextureFormat(format))
                    return format;
#endif // UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_EDITOR
                return RenderTextureFormat.DefaultHDR;
            }
        }

        private RenderTextureFormat sourceFormat { get { return hbaoCamera.allowHDR ? defaultHDRRenderTextureFormat : RenderTextureFormat.Default; } }
        //private static RenderTextureFormat colorFormat { get { return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB64) ? RenderTextureFormat.ARGB64 : RenderTextureFormat.ARGB32; } }
        private static RenderTextureFormat colorFormat { get { return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default; } }
        private static RenderTextureFormat depthFormat { get { return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf; } }
        private static RenderTextureFormat normalsFormat { get { return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010) ? RenderTextureFormat.ARGB2101010 : RenderTextureFormat.Default; } }
        private static bool isLinearColorSpace { get { return QualitySettings.activeColorSpace == ColorSpace.Linear; } }
        private bool renderingInSceneView { get { return hbaoCamera.cameraType == CameraType.SceneView; } }

        private RenderTextureDescriptor m_sourceDescriptor;
        private string[] m_ShaderKeywords;
        private bool m_IsCommandBufferDirty;
        private Mesh m_FullscreenTriangle;
        private PipelineStage? m_PreviousPipelineStage;
        private Resolution? m_PreviousResolution;
        private Deinterleaving? m_PreviousDeinterleaving;
        private DebugMode? m_PreviousDebugMode;
        private NoiseType? m_PreviousNoiseType;
        private BlurType? m_PreviousBlurAmount;
        private int m_PreviousWidth;
        private int m_PreviousHeight;
        private bool m_PreviousAllowHDR;
        private bool m_PreviousColorBleedingEnabled;
        private bool m_PreviousTemporalFilterEnabled;
        private RenderingPath m_PreviousRenderingPath;

        void OnEnable()
        {
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
            {
                Debug.LogWarning("HBAO shader is not supported on this platform.");
                this.enabled = false;
                return;
            }

            if (hbaoShader == null) hbaoShader = Shader.Find("Hidden/HBAO");
            if (hbaoShader == null)
            {
                Debug.LogError("HBAO shader was not found...");
                return;
            }

            if (!hbaoShader.isSupported)
            {
                Debug.LogWarning("HBAO shader is not supported on this platform.");
                this.enabled = false;
                return;
            }

            Initialize();
        }

        void OnDisable()
        {
            ClearCommandBuffer(cmdBuffer);

            ReleaseHistoryBuffers();

            if (material != null)
                DestroyImmediate(material);
            if (noiseTex != null)
                DestroyImmediate(noiseTex);
            if (fullscreenTriangle != null)
                DestroyImmediate(fullscreenTriangle);
        }

        void OnPreRender()
        {
            if (hbaoShader == null || hbaoCamera == null) return;

            FetchRenderParameters();
            CheckParameters();
            UpdateMaterialProperties();
            UpdateShaderKeywords();

            if (isCommandBufferDirty)
            {
                ClearCommandBuffer(cmdBuffer);
                BuildCommandBuffer(cmdBuffer, cameraEvent);
                hbaoCamera.AddCommandBuffer(cameraEvent, cmdBuffer);
                isCommandBufferDirty = false;
            }
        }

        void OnPostRender()
        {
            frameCount++;
        }

        void OnValidate()
        {
            if (hbaoShader == null || hbaoCamera == null) return;

            CheckParameters();
        }

        private void Initialize()
        {
            m_sourceDescriptor = new RenderTextureDescriptor(0, 0);

            hbaoCamera = GetComponent<Camera>();
            hbaoCamera.forceIntoRenderTexture = true;

            material = new Material(hbaoShader);
            material.hideFlags = HideFlags.HideAndDontSave;

            // For platforms not supporting motion vectors texture
            // https://docs.unity3d.com/ScriptReference/DepthTextureMode.MotionVectors.html
            motionVectorsSupported = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf);

            cmdBuffer = new CommandBuffer { name = "HBAO" };
            isCommandBufferDirty = true;
        }

        private void FetchRenderParameters()
        {
#if !UNITY_SWITCH && ENABLE_VR
            if (hbaoCamera.stereoEnabled)
            {
                var xrDesc = XRSettings.eyeTextureDesc;
                stereoRenderingMode = XRSettings.StereoRenderingMode.SinglePass;
                numberOfEyes = 1;

                if (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
                    stereoRenderingMode = XRSettings.StereoRenderingMode.MultiPass;

#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_PS4
                if (xrDesc.dimension == TextureDimension.Tex2DArray)
                    stereoRenderingMode = XRSettings.StereoRenderingMode.SinglePassInstanced;
#endif

                if (stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                    numberOfEyes = 2;

                if (stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
                {
                    numberOfEyes = 2;
                    //xrDesc.width /= 2;
                    xrDesc.vrUsage = VRTextureUsage.None;
                }

                width = xrDesc.width;
                height = xrDesc.height;
                m_sourceDescriptor = xrDesc;

                if (hbaoCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                    xrActiveEye = (int)Camera.StereoscopicEye.Right;

                screenWidth = XRSettings.eyeTextureWidth;
                screenHeight = XRSettings.eyeTextureHeight;

                if (stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
                    screenWidth /= 2;

                stereoActive = true;
            }
            else
#endif
            {
                width = hbaoCamera.pixelWidth;
                height = hbaoCamera.pixelHeight;
                m_sourceDescriptor.width = width;
                m_sourceDescriptor.height = height;
                screenWidth = width;
                screenHeight = height;
                stereoActive = false;
                numberOfEyes = 1;
            }

            var downsamplingFactor = generalSettings.resolution == Resolution.Full ? 1 : generalSettings.deinterleaving == Deinterleaving.Disabled ? 2 : 1;
            if (downsamplingFactor > 1)
            {
                aoWidth = (width + width % 2) / downsamplingFactor;
                aoHeight = (height + height % 2) / downsamplingFactor;
            }
            else
            {
                aoWidth = width;
                aoHeight = height;
            }

            reinterleavedAoWidth = width + (width % 4 == 0 ? 0 : 4 - (width % 4));
            reinterleavedAoHeight = height + (height % 4 == 0 ? 0 : 4 - (height % 4));
            deinterleavedAoWidth = reinterleavedAoWidth / 4;
            deinterleavedAoHeight = reinterleavedAoHeight / 4;
        }

        private void AllocateHistoryBuffers()
        {
            ReleaseHistoryBuffers();

            aoHistoryBuffer = GetScreenSpaceRT(widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
            if (colorBleedingSettings.enabled)
                colorBleedingHistoryBuffer = GetScreenSpaceRT(widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);

            // Clear history buffers to default
            var lastActive = RenderTexture.active;
            RenderTexture.active = aoHistoryBuffer;
            GL.Clear(false, true, Color.white);
            if (colorBleedingSettings.enabled)
            {
                RenderTexture.active = colorBleedingHistoryBuffer;
                GL.Clear(false, true, new Color(0, 0, 0, 1));
            }
            RenderTexture.active = lastActive;

            frameCount = 0;
        }

        private void ReleaseHistoryBuffers()
        {
            if (aoHistoryBuffer != null)
                aoHistoryBuffer.Release();

            if (colorBleedingHistoryBuffer != null)
                colorBleedingHistoryBuffer.Release();
        }

        private void ClearCommandBuffer(CommandBuffer cmd)
        {
            if (cmd != null)
            {
                if (hbaoCamera != null)
                {
                    hbaoCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, cmd);
                    hbaoCamera.RemoveCommandBuffer(CameraEvent.AfterLighting, cmd);
                    hbaoCamera.RemoveCommandBuffer(CameraEvent.BeforeReflections, cmd);
                }
                cmd.Clear();
            }
        }

        private void BuildCommandBuffer(CommandBuffer cmd, CameraEvent cameraEvent)
        {
            // AO
            if (generalSettings.deinterleaving == Deinterleaving.Disabled)
            {
                GetScreenSpaceTemporaryRT(cmd, ShaderProperties.hbaoTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                AO(cmd);
            }
            else
            {
                GetScreenSpaceTemporaryRT(cmd, ShaderProperties.hbaoTex, widthOverride: reinterleavedAoWidth, heightOverride: reinterleavedAoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                DeinterleavedAO(cmd);
            }

            // Blur
            Blur(cmd);

            // Temporal Filter
            TemporalFilter(cmd);

            // Composite
            Composite(cmd, cameraEvent);

            ReleaseTemporaryRT(cmd, ShaderProperties.hbaoTex);

            //Debug.Log("CommandBuffer has been rebuilt");
        }

        private void AO(CommandBuffer cmd)
        {
            BlitFullscreenTriangleWithClear(cmd, BuiltinRenderTextureType.CameraTarget, ShaderProperties.hbaoTex, material, new Color(0, 0, 0, 1), Pass.AO);
        }

        private void DeinterleavedAO(CommandBuffer cmd)
        {
            // Deinterleave depth & normals (4x4)
            for (int i = 0; i < 4; i++)
            {
                var rtsDepth = new RenderTargetIdentifier[] {
                ShaderProperties.depthSliceTex[(i << 2) + 0],
                ShaderProperties.depthSliceTex[(i << 2) + 1],
                ShaderProperties.depthSliceTex[(i << 2) + 2],
                ShaderProperties.depthSliceTex[(i << 2) + 3]
            };
                var rtsNormals = new RenderTargetIdentifier[] {
                ShaderProperties.normalsSliceTex[(i << 2) + 0],
                ShaderProperties.normalsSliceTex[(i << 2) + 1],
                ShaderProperties.normalsSliceTex[(i << 2) + 2],
                ShaderProperties.normalsSliceTex[(i << 2) + 3]
            };

                int offsetX = (i & 1) << 1; int offsetY = (i >> 1) << 1;
                cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[0], new Vector2(offsetX + 0, offsetY + 0));
                cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[1], new Vector2(offsetX + 1, offsetY + 0));
                cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[2], new Vector2(offsetX + 0, offsetY + 1));
                cmd.SetGlobalVector(ShaderProperties.deinterleaveOffset[3], new Vector2(offsetX + 1, offsetY + 1));
                for (int j = 0; j < 4; j++)
                {
                    GetScreenSpaceTemporaryRT(cmd, ShaderProperties.depthSliceTex[j + 4 * i], widthOverride: deinterleavedAoWidth, heightOverride: deinterleavedAoHeight, colorFormat: depthFormat, readWrite: RenderTextureReadWrite.Linear, filter: FilterMode.Point);
                    GetScreenSpaceTemporaryRT(cmd, ShaderProperties.normalsSliceTex[j + 4 * i], widthOverride: deinterleavedAoWidth, heightOverride: deinterleavedAoHeight, colorFormat: normalsFormat, readWrite: RenderTextureReadWrite.Linear, filter: FilterMode.Point);
                }
                BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, rtsDepth, material, Pass.Deinterleave_Depth); // outputs 4 render textures
                BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, rtsNormals, material, Pass.Deinterleave_Normals); // outputs 4 render textures
            }

            // AO on each layer
            for (int i = 0; i < 4 * 4; i++)
            {
                cmd.SetGlobalTexture(ShaderProperties.depthTex, ShaderProperties.depthSliceTex[i]);
                cmd.SetGlobalTexture(ShaderProperties.normalsTex, ShaderProperties.normalsSliceTex[i]);
                cmd.SetGlobalVector(ShaderProperties.jitter, s_jitter[i]);
                GetScreenSpaceTemporaryRT(cmd, ShaderProperties.aoSliceTex[i], widthOverride: deinterleavedAoWidth, heightOverride: deinterleavedAoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear, filter: FilterMode.Point);
                BlitFullscreenTriangleWithClear(cmd, BuiltinRenderTextureType.CameraTarget, ShaderProperties.aoSliceTex[i], material, new Color(0, 0, 0, 1), Pass.AO_Deinterleaved); // ao
                ReleaseTemporaryRT(cmd, ShaderProperties.depthSliceTex[i]);
                ReleaseTemporaryRT(cmd, ShaderProperties.normalsSliceTex[i]);
            }

            // Atlas Deinterleaved AO, 4x4
            GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, widthOverride: reinterleavedAoWidth, heightOverride: reinterleavedAoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
            for (int i = 0; i < 4 * 4; i++)
            {
                cmd.SetGlobalVector(ShaderProperties.atlasOffset, new Vector2(((i & 1) + (((i & 7) >> 2) << 1)) * deinterleavedAoWidth, (((i & 3) >> 1) + ((i >> 3) << 1)) * deinterleavedAoHeight));
                BlitFullscreenTriangle(cmd, ShaderProperties.aoSliceTex[i], ShaderProperties.tempTex, material, Pass.Atlas_AO_Deinterleaved); // atlassing
                ReleaseTemporaryRT(cmd, ShaderProperties.aoSliceTex[i]);
            }

            // Reinterleave AO
            ApplyFlip(cmd);
            BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, ShaderProperties.hbaoTex, material, Pass.Reinterleave_AO); // reinterleave
            ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
        }

        private void Blur(CommandBuffer cmd)
        {
            if (blurSettings.type != BlurType.None)
            {
                GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                cmd.SetGlobalVector(ShaderProperties.blurDeltaUV, new Vector2(1f / width, 0));
                BlitFullscreenTriangle(cmd, ShaderProperties.hbaoTex, ShaderProperties.tempTex, material, Pass.Blur); // blur X
                cmd.SetGlobalVector(ShaderProperties.blurDeltaUV, new Vector2(0, 1f / height));
                BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, ShaderProperties.hbaoTex, material, Pass.Blur); // blur Y
                ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
            }
        }

        private void TemporalFilter(CommandBuffer cmd)
        {
            if (temporalFilterSettings.enabled && !renderingInSceneView)
            {
                AllocateHistoryBuffers();

                if (colorBleedingSettings.enabled)
                {
                    // For Color Bleeding we have 2 history buffers to fill so there are 2 render targets.
                    // AO is still contained in Color Bleeding history buffer (alpha channel) so that we
                    // can use it as a render texture for the composite pass.
                    var rts = new RenderTargetIdentifier[] {
                    aoHistoryBuffer,
                    colorBleedingHistoryBuffer
                };
                    GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                    GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex2, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                    BlitFullscreenTriangle(cmd, aoHistoryBuffer, ShaderProperties.tempTex2, material, Pass.Copy);
                    BlitFullscreenTriangle(cmd, colorBleedingHistoryBuffer, ShaderProperties.tempTex, material, Pass.Copy);
                    BlitFullscreenTriangle(cmd, ShaderProperties.tempTex2, rts, material, Pass.Temporal_Filter);
                    ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
                    ReleaseTemporaryRT(cmd, ShaderProperties.tempTex2);
                    cmd.SetGlobalTexture(ShaderProperties.hbaoTex, colorBleedingHistoryBuffer);
                }
                else
                {
                    // AO history buffer contains ao in aplha channel so we can just use history as
                    // a render texture for the composite pass.
                    GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, widthOverride: aoWidth, heightOverride: aoHeight, colorFormat: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                    BlitFullscreenTriangle(cmd, aoHistoryBuffer, ShaderProperties.tempTex, material, Pass.Copy);
                    BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, aoHistoryBuffer, material, Pass.Temporal_Filter);
                    ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
                    cmd.SetGlobalTexture(ShaderProperties.hbaoTex, aoHistoryBuffer);
                }
            }
        }

        private void Composite(CommandBuffer cmd, CameraEvent cameraEvent)
        {
            if (generalSettings.debugMode == DebugMode.Disabled)
            {
                if (cameraEvent == CameraEvent.BeforeReflections)
                    CompositeBeforeReflections(cmd);
                else if (cameraEvent == CameraEvent.AfterLighting)
                    CompositeAfterLighting(cmd);
                else // if (BeforeImageEffectsOpaque)
                    CompositeBeforeImageEffectsOpaque(cmd);
            }
            else // debug mode
                CompositeBeforeImageEffectsOpaque(cmd, generalSettings.debugMode == DebugMode.ViewNormals ? Pass.Debug_ViewNormals : Pass.Composite);
        }

        // Cases of BeforeReflections & AfterLighting
        // If using HDR there’s no separate rendertarget being created for Emission+lighting buffer (RT3);
        // instead the rendertarget that the Camera renders into (that is, the one that is passed to the image effects) is used as RT3.
        // If non-HDR GBuffer3 texture is available. HDR uses ARGBHalf, non-HDR uses ARGB2101010
        // Emission + lighting buffer(RT3) is logarithmically encoded to provide greater dynamic range than is usually possible with
        // an ARGB32 texture, when the Camera is not using HDR.
        // https://docs.unity3d.com/Manual/RenderTech-DeferredShading.html

        private void CompositeBeforeReflections(CommandBuffer cmd)
        {
            var hdr = hbaoCamera.allowHDR;
            var rts = new RenderTargetIdentifier[] {
            BuiltinRenderTextureType.GBuffer0, // Albedo, Occ
            hdr ? BuiltinRenderTextureType.CameraTarget : BuiltinRenderTextureType.GBuffer3 // Ambient
        };
            GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, colorFormat: RenderTextureFormat.ARGB32);
            GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex2, colorFormat: hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB2101010);
            BlitFullscreenTriangle(cmd, rts[0], ShaderProperties.tempTex, material, Pass.Copy);
            BlitFullscreenTriangle(cmd, rts[1], ShaderProperties.tempTex2, material, Pass.Copy);
            BlitFullscreenTriangle(cmd, ShaderProperties.tempTex2, rts, material, Pass.Composite_BeforeReflections);
            ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
            ReleaseTemporaryRT(cmd, ShaderProperties.tempTex2);
        }

        private void CompositeAfterLighting(CommandBuffer cmd)
        {
            var hdr = hbaoCamera.allowHDR;
            var rt3 = hdr ? BuiltinRenderTextureType.CameraTarget : BuiltinRenderTextureType.GBuffer3;
            GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, colorFormat: hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB2101010);
            BlitFullscreenTriangle(cmd, rt3, ShaderProperties.tempTex, material, Pass.Copy);
            BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, rt3, material, Pass.Composite_AfterLighting);
            ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
        }

        private void CompositeBeforeImageEffectsOpaque(CommandBuffer cmd, int finalPassId = Pass.Composite)
        {
            // This pass can be used to display all debug mode as well
            GetScreenSpaceTemporaryRT(cmd, ShaderProperties.tempTex, colorFormat: sourceFormat);
            if (stereoActive && hbaoCamera.actualRenderingPath != RenderingPath.DeferredShading)
                cmd.Blit(BuiltinRenderTextureType.CameraTarget, ShaderProperties.tempTex);
            else
                BlitFullscreenTriangle(cmd, BuiltinRenderTextureType.CameraTarget, ShaderProperties.tempTex, material, Pass.Copy);
            ApplyFlip(cmd, SystemInfo.graphicsUVStartsAtTop);
            BlitFullscreenTriangle(cmd, ShaderProperties.tempTex, BuiltinRenderTextureType.CameraTarget, material, finalPassId);
            ReleaseTemporaryRT(cmd, ShaderProperties.tempTex);
        }

        private void UpdateMaterialProperties()
        {
            float tanHalfFovY = Mathf.Tan(0.5f * hbaoCamera.fieldOfView * Mathf.Deg2Rad);
            float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (screenHeight / (float)screenWidth));
            float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
            float maxRadInPixels = Mathf.Max(16, aoSettings.maxRadiusPixels * Mathf.Sqrt((screenWidth * numberOfEyes * screenHeight) / (1080.0f * 1920.0f)));
            maxRadInPixels /= (generalSettings.deinterleaving == Deinterleaving.x4 ? 4 : 1);

            var targetScale = generalSettings.deinterleaving == Deinterleaving.x4 ?
                                  new Vector4(reinterleavedAoWidth / (float)width, reinterleavedAoHeight / (float)height, 1.0f / (reinterleavedAoWidth / (float)width), 1.0f / (reinterleavedAoHeight / (float)height)) :
                                  generalSettings.resolution == Resolution.Half /*&& aoSettings.perPixelNormals == PerPixelNormals.Reconstruct*/ ?
                                      new Vector4((width + 0.5f) / width, (height + 0.5f) / height, 1f, 1f) :
                                      Vector4.one;

            material.SetTexture(ShaderProperties.noiseTex, noiseTex);
            material.SetVector(ShaderProperties.inputTexelSize, new Vector4(1f / width, 1f / height, width, height));
            material.SetVector(ShaderProperties.aoTexelSize, new Vector4(1f / aoWidth, 1f / aoHeight, aoWidth, aoHeight));
            material.SetVector(ShaderProperties.deinterleavedAOTexelSize, new Vector4(1.0f / deinterleavedAoWidth, 1.0f / deinterleavedAoHeight, deinterleavedAoWidth, deinterleavedAoHeight));
            material.SetVector(ShaderProperties.reinterleavedAOTexelSize, new Vector4(1f / reinterleavedAoWidth, 1f / reinterleavedAoHeight, reinterleavedAoWidth, reinterleavedAoHeight));
            material.SetVector(ShaderProperties.targetScale, targetScale);
            material.SetVector(ShaderProperties.uvToView, new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));
            material.SetMatrix(ShaderProperties.worldToCameraMatrix, hbaoCamera.worldToCameraMatrix);
            material.SetFloat(ShaderProperties.radius, aoSettings.radius * 0.5f * ((screenHeight / (generalSettings.deinterleaving == Deinterleaving.x4 ? 4 : 1)) / (tanHalfFovY * 2.0f)));
            material.SetFloat(ShaderProperties.maxRadiusPixels, maxRadInPixels);
            material.SetFloat(ShaderProperties.negInvRadius2, -1.0f / (aoSettings.radius * aoSettings.radius));
            material.SetFloat(ShaderProperties.angleBias, aoSettings.bias);
            material.SetFloat(ShaderProperties.aoMultiplier, 2.0f * (1.0f / (1.0f - aoSettings.bias)));
            material.SetFloat(ShaderProperties.intensity, isLinearColorSpace ? aoSettings.intensity : aoSettings.intensity * 0.454545454545455f);
            material.SetColor(ShaderProperties.baseColor, aoSettings.baseColor);
            material.SetFloat(ShaderProperties.multiBounceInfluence, aoSettings.multiBounceInfluence);
            material.SetFloat(ShaderProperties.offscreenSamplesContrib, aoSettings.offscreenSamplesContribution);
            material.SetFloat(ShaderProperties.maxDistance, aoSettings.maxDistance);
            material.SetFloat(ShaderProperties.distanceFalloff, aoSettings.distanceFalloff);
            material.SetFloat(ShaderProperties.blurSharpness, blurSettings.sharpness);
            material.SetFloat(ShaderProperties.colorBleedSaturation, colorBleedingSettings.saturation);
            material.SetFloat(ShaderProperties.albedoMultiplier, colorBleedingSettings.albedoMultiplier);
            material.SetFloat(ShaderProperties.colorBleedBrightnessMask, colorBleedingSettings.brightnessMask);
            material.SetVector(ShaderProperties.colorBleedBrightnessMaskRange, AdjustBrightnessMaskToGammaSpace(new Vector2(Mathf.Pow(colorBleedingSettings.brightnessMaskRange.x, 3), Mathf.Pow(colorBleedingSettings.brightnessMaskRange.y, 3))));
            material.SetVector(ShaderProperties.temporalParams, temporalFilterSettings.enabled && !renderingInSceneView ? new Vector2(s_temporalRotations[frameCount % 6] / 360.0f, s_temporalOffsets[frameCount % 4]) : Vector2.zero);
        }

        private void UpdateShaderKeywords()
        {
            if (m_ShaderKeywords == null || m_ShaderKeywords.Length != 13) m_ShaderKeywords = new string[13];

            m_ShaderKeywords[0] = ShaderProperties.GetOrthographicOrDeferredKeyword(hbaoCamera.orthographic, generalSettings);
            m_ShaderKeywords[1] = ShaderProperties.GetDirectionsKeyword(generalSettings);
            m_ShaderKeywords[2] = ShaderProperties.GetStepsKeyword(generalSettings);
            m_ShaderKeywords[3] = ShaderProperties.GetNoiseKeyword(generalSettings);
            m_ShaderKeywords[4] = ShaderProperties.GetDeinterleavingKeyword(generalSettings);
            m_ShaderKeywords[5] = ShaderProperties.GetDebugKeyword(generalSettings);
            m_ShaderKeywords[6] = ShaderProperties.GetMultibounceKeyword(aoSettings);
            m_ShaderKeywords[7] = ShaderProperties.GetOffscreenSamplesContributionKeyword(aoSettings);
            m_ShaderKeywords[8] = ShaderProperties.GetPerPixelNormalsKeyword(aoSettings);
            m_ShaderKeywords[9] = ShaderProperties.GetBlurRadiusKeyword(blurSettings);
            m_ShaderKeywords[10] = ShaderProperties.GetVarianceClippingKeyword(temporalFilterSettings);
            m_ShaderKeywords[11] = ShaderProperties.GetColorBleedingKeyword(colorBleedingSettings);
            m_ShaderKeywords[12] = ShaderProperties.GetLightingLogEncodedKeyword(hbaoCamera.allowHDR);

            material.shaderKeywords = m_ShaderKeywords;
        }

        private void CheckParameters()
        {
            // Camera textures
            hbaoCamera.depthTextureMode |= DepthTextureMode.Depth;
            if (aoSettings.perPixelNormals == PerPixelNormals.Camera)
                hbaoCamera.depthTextureMode |= DepthTextureMode.DepthNormals;
            if (temporalFilterSettings.enabled)
                hbaoCamera.depthTextureMode |= DepthTextureMode.MotionVectors;

            // Settings to force
            if (hbaoCamera.actualRenderingPath != RenderingPath.DeferredShading && aoSettings.perPixelNormals == PerPixelNormals.GBuffer)
                SetAoPerPixelNormals(PerPixelNormals.Camera);

            if (generalSettings.deinterleaving != Deinterleaving.Disabled && SystemInfo.supportedRenderTargetCount < 4)
                SetDeinterleaving(Deinterleaving.Disabled);

            if (generalSettings.pipelineStage != PipelineStage.BeforeImageEffectsOpaque && hbaoCamera.actualRenderingPath != RenderingPath.DeferredShading)
                SetPipelineStage(PipelineStage.BeforeImageEffectsOpaque);

            if (generalSettings.pipelineStage != PipelineStage.BeforeImageEffectsOpaque && aoSettings.perPixelNormals == PerPixelNormals.Camera)
                SetAoPerPixelNormals(PerPixelNormals.GBuffer);

            if (stereoActive && hbaoCamera.actualRenderingPath != RenderingPath.DeferredShading && aoSettings.perPixelNormals != PerPixelNormals.Reconstruct)
                SetAoPerPixelNormals(PerPixelNormals.Reconstruct);

            if (temporalFilterSettings.enabled && !motionVectorsSupported)
                EnableTemporalFilter(false);

            if (colorBleedingSettings.enabled && temporalFilterSettings.enabled && SystemInfo.supportedRenderTargetCount < 2)
                EnableTemporalFilter(false);

            // Noise texture
            if (noiseTex == null || m_PreviousNoiseType != generalSettings.noiseType)
            {
                if (noiseTex != null) DestroyImmediate(noiseTex);

                CreateNoiseTexture();

                m_PreviousNoiseType = generalSettings.noiseType;
            }
        }

        private RenderTextureDescriptor GetDefaultDescriptor(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
        {
            var modifiedDesc = new RenderTextureDescriptor(m_sourceDescriptor.width, m_sourceDescriptor.height,
                                                           m_sourceDescriptor.colorFormat, depthBufferBits);
            modifiedDesc.dimension = m_sourceDescriptor.dimension;
            modifiedDesc.volumeDepth = m_sourceDescriptor.volumeDepth;
            modifiedDesc.vrUsage = m_sourceDescriptor.vrUsage;
            modifiedDesc.msaaSamples = m_sourceDescriptor.msaaSamples;
            modifiedDesc.memoryless = m_sourceDescriptor.memoryless;

            modifiedDesc.useMipMap = m_sourceDescriptor.useMipMap;
            modifiedDesc.autoGenerateMips = m_sourceDescriptor.autoGenerateMips;
            modifiedDesc.enableRandomWrite = m_sourceDescriptor.enableRandomWrite;
            modifiedDesc.shadowSamplingMode = m_sourceDescriptor.shadowSamplingMode;

            if (hbaoCamera.allowDynamicResolution)
                modifiedDesc.useDynamicScale = true;

            if (colorFormat != RenderTextureFormat.Default)
                modifiedDesc.colorFormat = colorFormat;

            if (readWrite == RenderTextureReadWrite.sRGB)
                modifiedDesc.sRGB = true;
            else if (readWrite == RenderTextureReadWrite.Linear)
                modifiedDesc.sRGB = false;
            else if (readWrite == RenderTextureReadWrite.Default)
                modifiedDesc.sRGB = isLinearColorSpace;

            return modifiedDesc;
        }

        private RenderTexture GetScreenSpaceRT(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
                                               FilterMode filter = FilterMode.Bilinear, int widthOverride = 0, int heightOverride = 0)
        {
            var desc = GetDefaultDescriptor(depthBufferBits, colorFormat, readWrite);
            if (widthOverride > 0)
                desc.width = widthOverride;
            if (heightOverride > 0)
                desc.height = heightOverride;

            //intermediates in VR are unchanged
            if (stereoActive && desc.dimension == TextureDimension.Tex2DArray)
                desc.dimension = TextureDimension.Tex2D;

            var rt = new RenderTexture(desc);
            rt.filterMode = filter;
            return rt;
        }

        private void GetScreenSpaceTemporaryRT(CommandBuffer cmd, int nameID,
                                               int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
                                               FilterMode filter = FilterMode.Bilinear, int widthOverride = 0, int heightOverride = 0)
        {
            var desc = GetDefaultDescriptor(depthBufferBits, colorFormat, readWrite);
            if (widthOverride > 0)
                desc.width = widthOverride;
            if (heightOverride > 0)
                desc.height = heightOverride;

            //intermediates in VR are unchanged
            if (stereoActive && desc.dimension == TextureDimension.Tex2DArray)
                desc.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(nameID, desc, filter);
        }

        private void ReleaseTemporaryRT(CommandBuffer cmd, int nameID)
        {
            cmd.ReleaseTemporaryRT(nameID);
        }

        private void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int pass = 0)
        {
            cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
            cmd.SetRenderTarget(destination);
            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
        }

        private void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, Material material, int pass = 0)
        {
            cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
            cmd.SetRenderTarget(destinations, destinations[0]);
            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
        }

        private void BlitFullscreenTriangleWithClear(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, Color clearColor, int pass = 0)
        {
            cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
            cmd.SetRenderTarget(destination);
            cmd.ClearRenderTarget(false, true, clearColor);
            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, material, 0, pass);
        }

        private static void ApplyFlip(CommandBuffer cmd, bool flip = true)
        {
            if (flip)
                cmd.SetGlobalVector(ShaderProperties.uvTransform, new Vector4(1f, -1f, 0f, 1f));
            else
                cmd.SetGlobalVector(ShaderProperties.uvTransform, new Vector4(1f, 1f, 0f, 0f));
        }

        private static Vector2 AdjustBrightnessMaskToGammaSpace(Vector2 v)
        {
            return isLinearColorSpace ? v : ToGammaSpace(v);
        }

        private static float ToGammaSpace(float v)
        {
            return Mathf.Pow(v, 0.454545454545455f);
        }

        private static Vector2 ToGammaSpace(Vector2 v)
        {
            return new Vector2(ToGammaSpace(v.x), ToGammaSpace(v.y));
        }

        private void CreateNoiseTexture()
        {
            noiseTex = new Texture2D(4, 4, SystemInfo.SupportsTextureFormat(TextureFormat.RGHalf) ? TextureFormat.RGHalf : TextureFormat.RGB24, false, true);
            noiseTex.filterMode = FilterMode.Point;
            noiseTex.wrapMode = TextureWrapMode.Repeat;
            int z = 0;
            for (int x = 0; x < 4; ++x)
            {
                for (int y = 0; y < 4; ++y)
                {
                    float r1 = generalSettings.noiseType != NoiseType.Dither ? 0.25f * (0.0625f * ((x + y & 3) << 2) + (x & 3)) : MersenneTwister.Numbers[z++];
                    float r2 = generalSettings.noiseType != NoiseType.Dither ? 0.25f * ((y - x) & 3) : MersenneTwister.Numbers[z++];
                    Color color = new Color(r1, r2, 0);
                    noiseTex.SetPixel(x, y, color);
                }
            }
            noiseTex.Apply();

            for (int i = 0, j = 0; i < s_jitter.Length; ++i)
            {
                float r1 = MersenneTwister.Numbers[j++];
                float r2 = MersenneTwister.Numbers[j++];
                s_jitter[i] = new Vector2(r1, r2);
            }
        }
    }
}
