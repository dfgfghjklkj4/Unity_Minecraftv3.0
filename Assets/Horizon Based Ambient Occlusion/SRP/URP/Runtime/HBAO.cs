using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HorizonBasedAmbientOcclusion.Universal
{
    [ExecuteInEditMode, VolumeComponentMenu("Lighting/HBAO")]
    public class HBAO : VolumeComponent, IPostProcessComponent
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

        public enum Mode
        {
            Normal,
            LitAO
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
            Reconstruct2Samples,
            Reconstruct4Samples,
            Camera
        }

        public enum VarianceClipping
        {
            Disabled,
            _4Tap,
            _8Tap
        }

        [Serializable]
        public sealed class PresetParameter : VolumeParameter<Preset>
        {
            public PresetParameter(Preset value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class ModeParameter : VolumeParameter<Mode>
        {
            public ModeParameter(Mode value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class QualityParameter : VolumeParameter<Quality>
        {
            public QualityParameter(Quality value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class DeinterleavingParameter : VolumeParameter<Deinterleaving>
        {
            public DeinterleavingParameter(Deinterleaving value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class ResolutionParameter : VolumeParameter<Resolution>
        {
            public ResolutionParameter(Resolution value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class NoiseTypeParameter : VolumeParameter<NoiseType>
        {
            public NoiseTypeParameter(NoiseType value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class DebugModeParameter : VolumeParameter<DebugMode>
        {
            public DebugModeParameter(DebugMode value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class PerPixelNormalsParameter : VolumeParameter<PerPixelNormals>
        {
            public PerPixelNormalsParameter(PerPixelNormals value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class VarianceClippingParameter : VolumeParameter<VarianceClipping>
        {
            public VarianceClippingParameter(VarianceClipping value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class BlurTypeParameter : VolumeParameter<BlurType>
        {
            public BlurTypeParameter(BlurType value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        [Serializable]
        public sealed class MinMaxFloatParameter : VolumeParameter<Vector2>
        {
            public float min;
            public float max;

            public MinMaxFloatParameter(Vector2 value, float min, float max, bool overrideState = false)
                : base(value, overrideState)
            {
                this.min = min;
                this.max = max;
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class SettingsGroup : Attribute
        {
            public bool isExpanded = true;
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class ParameterDisplayName : Attribute
        {
            public string name;

            public ParameterDisplayName(string name)
            {
                this.name = name;
            }
        }

        public class Presets : SettingsGroup { }
        public class GeneralSettings : SettingsGroup { }
        public class AOSettings : SettingsGroup { }
        public class TemporalFilterSettings : SettingsGroup { }
        public class BlurSettings : SettingsGroup { }
        public class ColorBleedingSettings : SettingsGroup { }

        [Presets]
        public PresetParameter preset = new PresetParameter(Preset.Normal);

        [Tooltip("The mode of the AO.")]
        [GeneralSettings, Space(6)]
        public ModeParameter mode = new ModeParameter(Mode.Normal);
        [Tooltip("The quality of the AO.")]
        [GeneralSettings, Space(6)]
        public QualityParameter quality = new QualityParameter(Quality.Medium);
        [Tooltip("The deinterleaving factor.")]
        [GeneralSettings]
        public DeinterleavingParameter deinterleaving = new DeinterleavingParameter(Deinterleaving.Disabled);
        [Tooltip("The resolution at which the AO is calculated.")]
        [GeneralSettings]
        public ResolutionParameter resolution = new ResolutionParameter(Resolution.Full);
        [Tooltip("The type of noise to use.")]
        [GeneralSettings, Space(10)]
        public NoiseTypeParameter noiseType = new NoiseTypeParameter(NoiseType.Dither);
        [Tooltip("The debug mode actually displayed on screen.")]
        [GeneralSettings, Space(10)]
        public DebugModeParameter debugMode = new DebugModeParameter(DebugMode.Disabled);

        [Tooltip("AO radius: this is the distance outside which occluders are ignored.")]
        [AOSettings, Space(6)]
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.8f, 0.25f, 5f);
        [Tooltip("Maximum radius in pixels: this prevents the radius to grow too much with close-up " +
                  "object and impact on performances.")]
        [AOSettings]
        public ClampedFloatParameter maxRadiusPixels = new ClampedFloatParameter(128f, 16f, 256f);
        [Tooltip("For low-tessellated geometry, occlusion variations tend to appear at creases and " +
                 "ridges, which betray the underlying tessellation. To remove these artifacts, we use " +
                 "an angle bias parameter which restricts the hemisphere.")]
        [AOSettings]
        public ClampedFloatParameter bias = new ClampedFloatParameter(0.05f, 0f, 0.5f);
        [Tooltip("This value allows to scale up the ambient occlusion values.")]
        [AOSettings]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0, 4f);
        [Tooltip("Enable/disable MultiBounce approximation.")]
        [AOSettings]
        public BoolParameter useMultiBounce = new BoolParameter(false);
        [Tooltip("MultiBounce approximation influence.")]
        [AOSettings]
        public ClampedFloatParameter multiBounceInfluence = new ClampedFloatParameter(1f, 0f, 1f);
        [Tooltip("How much AO affect direct lighting.")]
        [AOSettings]
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0.25f, 0, 1f);
        [Tooltip("The amount of AO offscreen samples are contributing.")]
        [AOSettings]
        public ClampedFloatParameter offscreenSamplesContribution = new ClampedFloatParameter(0f, 0f, 1f);
        [Tooltip("The max distance to display AO.")]
        [AOSettings, Space(10)]
        public FloatParameter maxDistance = new FloatParameter(150f);
        [Tooltip("The distance before max distance at which AO start to decrease.")]
        [AOSettings]
        public FloatParameter distanceFalloff = new FloatParameter(50f);
        [Tooltip("The type of per pixel normals to use.")]
        [AOSettings, Space(10)]
#if URP_10_0_0_OR_NEWER
        public PerPixelNormalsParameter perPixelNormals = new PerPixelNormalsParameter(PerPixelNormals.Camera);
#else
        public PerPixelNormalsParameter perPixelNormals = new PerPixelNormalsParameter(PerPixelNormals.Reconstruct4Samples);
#endif
        [Tooltip("This setting allow you to set the base color if the AO, the alpha channel value is unused.")]
        [AOSettings, Space(10)]
        public ColorParameter baseColor = new ColorParameter(Color.black);

        [TemporalFilterSettings, ParameterDisplayName("Enabled"), Space(6)]
        public BoolParameter temporalFilterEnabled = new BoolParameter(false);
        [Tooltip("The type of variance clipping to use.")]
        [TemporalFilterSettings]
        public VarianceClippingParameter varianceClipping = new VarianceClippingParameter(VarianceClipping._4Tap);

        [Tooltip("The type of blur to use.")]
        [BlurSettings, ParameterDisplayName("Type"), Space(6)]
        public BlurTypeParameter blurType = new BlurTypeParameter(BlurType.Medium);

        [Tooltip("This parameter controls the depth-dependent weight of the bilateral filter, to " +
                 "avoid bleeding across edges. A zero sharpness is a pure Gaussian blur. Increasing " +
                 "the blur sharpness removes bleeding by using lower weights for samples with large " +
                 "depth delta from the current pixel.")]
        [BlurSettings, Space(10)]
        public ClampedFloatParameter sharpness = new ClampedFloatParameter(8f, 0f, 16f);

        [ColorBleedingSettings, ParameterDisplayName("Enabled"), Space(6)]
        public BoolParameter colorBleedingEnabled = new BoolParameter(false);
        [Tooltip("This value allows to control the saturation of the color bleeding.")]
        [ColorBleedingSettings, Space(10)]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1f, 0f, 4f);
        [Tooltip("Use masking on emissive pixels")]
        [ColorBleedingSettings]
        public ClampedFloatParameter brightnessMask = new ClampedFloatParameter(1f, 0f, 1f);
        [Tooltip("Brightness level where masking starts/ends")]
        [ColorBleedingSettings]
        public MinMaxFloatParameter brightnessMaskRange = new MinMaxFloatParameter(new Vector2(0f, 0.5f), 0f, 2f);

        public void EnableHBAO(bool enable)
        {
            intensity.overrideState = enable;
        }

        public Preset GetCurrentPreset()
        {
            return preset.value;
        }

        public void ApplyPreset(Preset preset)
        {
            if (preset == Preset.Custom)
            {
                this.preset.Override(preset);
                return;
            }

            var actualDebugMode = debugMode.value;
            var actualDebugModeOverride = debugMode.overrideState;
            SetAllOverridesTo(false);
            debugMode.overrideState = actualDebugModeOverride;
            debugMode.value = actualDebugMode;

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

            this.preset.Override(preset);
        }

        public Mode GetMode()
        {
            return mode.value;
        }

        public void SetMode(Mode mode)
        {
            this.mode.Override(mode);
        }

        public Quality GetQuality()
        {
            return quality.value;
        }

        public void SetQuality(Quality quality)
        {
            this.quality.Override(quality);
        }

        public Deinterleaving GetDeinterleaving()
        {
            return deinterleaving.value;
        }

        public void SetDeinterleaving(Deinterleaving deinterleaving)
        {
            this.deinterleaving.Override(deinterleaving);
        }

        public Resolution GetResolution()
        {
            return resolution.value;
        }

        public void SetResolution(Resolution resolution)
        {
            this.resolution.Override(resolution);
        }

        public NoiseType GetNoiseType()
        {
            return noiseType.value;
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            this.noiseType.Override(noiseType);
        }

        public DebugMode GetDebugMode()
        {
            return debugMode.value;
        }

        public void SetDebugMode(DebugMode debugMode)
        {
            this.debugMode.Override(debugMode);
        }

        public float GetAoRadius()
        {
            return radius.value;
        }

        public void SetAoRadius(float radius)
        {
            this.radius.Override(Mathf.Clamp(radius, this.radius.min, this.radius.max));
        }

        public float GetAoMaxRadiusPixels()
        {
            return maxRadiusPixels.value;
        }

        public void SetAoMaxRadiusPixels(float maxRadiusPixels)
        {
            this.maxRadiusPixels.Override(Mathf.Clamp(maxRadiusPixels, this.maxRadiusPixels.min, this.maxRadiusPixels.max));
        }

        public float GetAoBias()
        {
            return bias.value;
        }

        public void SetAoBias(float bias)
        {
            this.bias.Override(Mathf.Clamp(bias, this.bias.min, this.bias.max));
        }

        public float GetAoOffscreenSamplesContribution()
        {
            return offscreenSamplesContribution.value;
        }

        public void SetAoOffscreenSamplesContribution(float offscreenSamplesContribution)
        {
            this.offscreenSamplesContribution.Override(Mathf.Clamp(offscreenSamplesContribution, this.offscreenSamplesContribution.min, this.offscreenSamplesContribution.max));
        }

        public float GetAoMaxDistance()
        {
            return maxDistance.value;
        }

        public void SetAoMaxDistance(float maxDistance)
        {
            this.maxDistance.Override(maxDistance);
        }

        public float GetAoDistanceFalloff()
        {
            return distanceFalloff.value;
        }

        public void SetAoDistanceFalloff(float distanceFalloff)
        {
            this.distanceFalloff.Override(distanceFalloff);
        }

        public PerPixelNormals GetAoPerPixelNormals()
        {
            return perPixelNormals.value;
        }

        public void SetAoPerPixelNormals(PerPixelNormals perPixelNormals)
        {
            this.perPixelNormals.Override(perPixelNormals);
        }

        public Color GetAoColor()
        {
            return baseColor.value;
        }

        public void SetAoColor(Color baseColor)
        {
            this.baseColor.Override(baseColor);
        }

        public float GetAoIntensity()
        {
            return intensity.value;
        }

        public void SetAoIntensity(float intensity)
        {
            this.intensity.Override(Mathf.Clamp(intensity, this.intensity.min, this.intensity.max));
        }

        public bool UseMultiBounce()
        {
            return useMultiBounce.value;
        }

        public void EnableMultiBounce(bool enabled = true)
        {
            useMultiBounce.Override(enabled);
        }

        public float GetAoMultiBounceInfluence()
        {
            return multiBounceInfluence.value;
        }

        public void SetAoMultiBounceInfluence(float multiBounceInfluence)
        {
            this.multiBounceInfluence.Override(Mathf.Clamp(multiBounceInfluence, this.multiBounceInfluence.min, this.multiBounceInfluence.max));
        }

        public bool IsTemporalFilterEnabled()
        {
            return temporalFilterEnabled.value;
        }

        public void EnableTemporalFilter(bool enabled = true)
        {
            temporalFilterEnabled.Override(enabled);
        }

        public VarianceClipping GetTemporalFilterVarianceClipping()
        {
            return varianceClipping.value;
        }

        public void SetTemporalFilterVarianceClipping(VarianceClipping varianceClipping)
        {
            this.varianceClipping.Override(varianceClipping);
        }

        public BlurType GetBlurType()
        {
            return blurType.value;
        }

        public void SetBlurType(BlurType blurType)
        {
            this.blurType.Override(blurType);
        }

        public float GetBlurSharpness()
        {
            return sharpness.value;
        }

        public void SetBlurSharpness(float sharpness)
        {
            this.sharpness.Override(Mathf.Clamp(sharpness, this.sharpness.min, this.sharpness.max));
        }

        public bool IsColorBleedingEnabled()
        {
            return colorBleedingEnabled.value;
        }

        public void EnableColorBleeding(bool enabled = true)
        {
            colorBleedingEnabled.Override(enabled);
        }

        public float GetColorBleedingSaturation()
        {
            return saturation.value;
        }

        public void SetColorBleedingSaturation(float saturation)
        {
            this.saturation.Override(Mathf.Clamp(saturation, this.saturation.min, this.saturation.max));
        }

        public float GetColorBleedingBrightnessMask()
        {
            return brightnessMask.value;
        }

        public void SetColorBleedingBrightnessMask(float brightnessMask)
        {
            this.brightnessMask.Override(Mathf.Clamp(brightnessMask, this.brightnessMask.min, this.brightnessMask.max));
        }

        public Vector2 GetColorBleedingBrightnessMaskRange()
        {
            return brightnessMaskRange.value;
        }

        public void SetColorBleedingBrightnessMaskRange(Vector2 brightnessMaskRange)
        {
            brightnessMaskRange.x = Mathf.Clamp(brightnessMaskRange.x, this.brightnessMaskRange.min, this.brightnessMaskRange.max);
            brightnessMaskRange.y = Mathf.Clamp(brightnessMaskRange.y, this.brightnessMaskRange.min, this.brightnessMaskRange.max);
            brightnessMaskRange.x = Mathf.Min(brightnessMaskRange.x, brightnessMaskRange.y);
            this.brightnessMaskRange.Override(brightnessMaskRange);
        }

        public bool IsActive() => intensity.overrideState && intensity.value > 0;

        public bool IsTileCompatible() => true;
    }
}
