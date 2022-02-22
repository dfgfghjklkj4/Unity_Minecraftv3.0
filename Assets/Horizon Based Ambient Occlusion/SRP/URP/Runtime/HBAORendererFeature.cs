using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HorizonBasedAmbientOcclusion.Universal
{
    public class HBAORendererFeature : ScriptableRendererFeature
    {
        private class HBAORenderPass : ScriptableRenderPass
        {
            public HBAO hbao;

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

                public const int Debug_ViewNormals = 10;
            }

            private static class ShaderProperties
            {
                public static int mainTex;
                public static int inputTex;
                public static int hbaoTex;
                public static int tempTex;
                public static int tempTex2;
                public static int noiseTex;
                public static int depthTex;
                public static int normalsTex;
                public static int ssaoTex;
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
                //public static int worldToCameraMatrix;
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
                    inputTex = Shader.PropertyToID("_InputTex");
                    hbaoTex = Shader.PropertyToID("_HBAOTex");
                    tempTex = Shader.PropertyToID("_TempTex");
                    tempTex2 = Shader.PropertyToID("_TempTex2");
                    noiseTex = Shader.PropertyToID("_NoiseTex");
                    depthTex = Shader.PropertyToID("_DepthTex");
                    normalsTex = Shader.PropertyToID("_NormalsTex");
                    ssaoTex = Shader.PropertyToID("_SSAOTex");
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
                    //worldToCameraMatrix = Shader.PropertyToID("_WorldToCameraMatrix");
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

                public static string GetOrthographicProjectionKeyword(bool orthographic)
                {
                    return orthographic ? "ORTHOGRAPHIC_PROJECTION" : "__";
                }

                public static string GetDirectionsKeyword(HBAO.Quality quality)
                {
                    switch (quality)
                    {
                        case HBAO.Quality.Lowest:
                            return "DIRECTIONS_3";
                        case HBAO.Quality.Low:
                            return "DIRECTIONS_4";
                        case HBAO.Quality.Medium:
                            return "DIRECTIONS_6";
                        case HBAO.Quality.High:
                            return "DIRECTIONS_8";
                        case HBAO.Quality.Highest:
                            return "DIRECTIONS_8";
                        default:
                            return "DIRECTIONS_6";
                    }
                }

                public static string GetStepsKeyword(HBAO.Quality quality)
                {
                    switch (quality)
                    {
                        case HBAO.Quality.Lowest:
                            return "STEPS_2";
                        case HBAO.Quality.Low:
                            return "STEPS_3";
                        case HBAO.Quality.Medium:
                            return "STEPS_4";
                        case HBAO.Quality.High:
                            return "STEPS_4";
                        case HBAO.Quality.Highest:
                            return "STEPS_6";
                        default:
                            return "STEPS_4";
                    }
                }

                public static string GetNoiseKeyword(HBAO.NoiseType noiseType)
                {
                    switch (noiseType)
                    {
                        case HBAO.NoiseType.InterleavedGradientNoise:
                            return "INTERLEAVED_GRADIENT_NOISE";
                        case HBAO.NoiseType.Dither:
                        case HBAO.NoiseType.SpatialDistribution:
                        default:
                            return "__";
                    }
                }

                public static string GetDeinterleavingKeyword(HBAO.Deinterleaving deinterleaving)
                {
                    switch (deinterleaving)
                    {
                        case HBAO.Deinterleaving.x4:
                            return "DEINTERLEAVED";
                        case HBAO.Deinterleaving.Disabled:
                        default:
                            return "__";
                    }
                }

                public static string GetDebugKeyword(HBAO.DebugMode debugMode)
                {
                    switch (debugMode)
                    {
                        case HBAO.DebugMode.AOOnly:
                            return "DEBUG_AO";
                        case HBAO.DebugMode.ColorBleedingOnly:
                            return "DEBUG_COLORBLEEDING";
                        case HBAO.DebugMode.SplitWithoutAOAndWithAO:
                            return "DEBUG_NOAO_AO";
                        case HBAO.DebugMode.SplitWithAOAndAOOnly:
                            return "DEBUG_AO_AOONLY";
                        case HBAO.DebugMode.SplitWithoutAOAndAOOnly:
                            return "DEBUG_NOAO_AOONLY";
                        case HBAO.DebugMode.Disabled:
                        default:
                            return "__";
                    }
                }

                public static string GetMultibounceKeyword(bool useMultiBounce, bool litAoModeEnabled)
                {
                    return useMultiBounce && !litAoModeEnabled ? "MULTIBOUNCE" : "__";
                }

                public static string GetOffscreenSamplesContributionKeyword(float offscreenSamplesContribution)
                {
                    return offscreenSamplesContribution > 0 ? "OFFSCREEN_SAMPLES_CONTRIBUTION" : "__";
                }

                public static string GetPerPixelNormalsKeyword(HBAO.PerPixelNormals perPixelNormals)
                {
                    switch (perPixelNormals)
                    {
                        case HBAO.PerPixelNormals.Reconstruct4Samples:
                            return "NORMALS_RECONSTRUCT4";
                        case HBAO.PerPixelNormals.Reconstruct2Samples:
                            return "NORMALS_RECONSTRUCT2";
                        case HBAO.PerPixelNormals.Camera:
                        default:
                            return "__";
                    }
                }

                public static string GetBlurRadiusKeyword(HBAO.BlurType blurType)
                {
                    switch (blurType)
                    {
                        case HBAO.BlurType.Narrow:
                            return "BLUR_RADIUS_2";
                        case HBAO.BlurType.Medium:
                            return "BLUR_RADIUS_3";
                        case HBAO.BlurType.Wide:
                            return "BLUR_RADIUS_4";
                        case HBAO.BlurType.ExtraWide:
                            return "BLUR_RADIUS_5";
                        case HBAO.BlurType.None:
                        default:
                            return "BLUR_RADIUS_3";
                    }
                }

                public static string GetVarianceClippingKeyword(HBAO.VarianceClipping varianceClipping)
                {
                    switch (varianceClipping)
                    {
                        case HBAO.VarianceClipping._4Tap:
                            return "VARIANCE_CLIPPING_4TAP";
                        case HBAO.VarianceClipping._8Tap:
                            return "VARIANCE_CLIPPING_8TAP";
                        case HBAO.VarianceClipping.Disabled:
                        default:
                            return "__";
                    }
                }

                public static string GetColorBleedingKeyword(bool colorBleedingEnabled, bool litAoModeEnabled)
                {
                    return colorBleedingEnabled && !litAoModeEnabled ? "COLOR_BLEEDING" : "__";
                }

                public static string GetModeKeyword(HBAO.Mode mode)
                {
                    return mode == HBAO.Mode.LitAO ? "LIT_AO" : "__";
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
            private RenderTargetIdentifier source { get; set; }
            private CameraData cameraData { get; set; }
            private RenderTextureDescriptor sourceDesc { get; set; }
            private RenderTextureDescriptor aoDesc { get; set; }
            private RenderTextureDescriptor deinterleavedDepthDesc { get; set; }
            private RenderTextureDescriptor deinterleavedNormalsDesc { get; set; }
            private RenderTextureDescriptor deinterleavedAoDesc { get; set; }
            private RenderTextureDescriptor reinterleavedAoDesc { get; set; }
            private int frameCount { get; set; }
            private RenderTextureFormat colorFormat { get; set; }
            private RenderTextureFormat depthFormat { get; set; }
            private RenderTextureFormat normalsFormat { get; set; }
            private bool motionVectorsSupported { get; set; }
            private RenderTexture aoHistoryBuffer { get; set; }
            private RenderTexture colorBleedingHistoryBuffer { get; set; }
            private Texture2D noiseTex { get; set; }
            private int numberOfEyes { get; set; }

            private bool isHistoryBufferDirty
            {
                get
                {
                    if (m_PreviousTemporalFilterEnabled != hbao.temporalFilterEnabled.value ||
                        m_PreviousResolution != hbao.resolution.value || m_PreviousWidth != aoDesc.width || m_PreviousHeight != aoDesc.height ||
                        m_PreviousColorBleedingEnabled != hbao.colorBleedingEnabled.value)
                    {
                        m_PreviousTemporalFilterEnabled = hbao.temporalFilterEnabled.value;
                        m_PreviousResolution = hbao.resolution.value;
                        m_PreviousWidth = aoDesc.width;
                        m_PreviousHeight = aoDesc.height;
                        m_PreviousColorBleedingEnabled = hbao.colorBleedingEnabled.value;

                        return true;
                    }

                    return false;
                }
            }

            private static bool isLinearColorSpace { get { return QualitySettings.activeColorSpace == ColorSpace.Linear; } }
            private bool renderingInSceneView { get { return cameraData.camera.cameraType == CameraType.SceneView; } }

            private bool m_IsHistoryBufferDirty;
            private HBAO.Resolution? m_PreviousResolution;
            private HBAO.NoiseType? m_PreviousNoiseType;
            private int m_PreviousWidth;
            private int m_PreviousHeight;
            private bool m_PreviousTemporalFilterEnabled;
            private bool m_PreviousColorBleedingEnabled;
            private string[] m_ShaderKeywords;

            public void FillSupportedRenderTextureFormats()
            {
                colorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
                depthFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
                normalsFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010) ? RenderTextureFormat.ARGB2101010 : RenderTextureFormat.Default;
                motionVectorsSupported = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf);
            }

            public void Setup(Shader shader, ScriptableRenderer renderer, RenderingData renderingData)
            {
                if (material == null) material = CoreUtils.CreateEngineMaterial(shader);

#if !URP_10_0_0_OR_NEWER
                source = renderer.cameraColorTarget;
                cameraData = renderingData.cameraData;

                // Configures where the render pass should be injected.
                FetchVolumeComponent();
                renderPassEvent = hbao.debugMode.value == HBAO.DebugMode.Disabled ?
                    RenderPassEvent.BeforeRenderingTransparents :
                    RenderPassEvent.AfterRenderingTransparents;
#endif
            }

#if URP_10_0_0_OR_NEWER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                //source = new RenderTargetIdentifier("_CameraColorTexture");
                source = renderingData.cameraData.renderer.cameraColorTarget;
                cameraData = renderingData.cameraData;

                FetchVolumeComponent();

                ConfigureInput(ScriptableRenderPassInput.Depth);
                if (hbao.perPixelNormals.value == HBAO.PerPixelNormals.Camera)
                    ConfigureInput(ScriptableRenderPassInput.Normal);

                // Configures where the render pass should be injected.
                // Rendering after PrePasses is usually correct except when depth priming is in play:
                // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                renderPassEvent = hbao.debugMode.value == HBAO.DebugMode.Disabled ?
                    hbao.mode.value == HBAO.Mode.LitAO ? RenderPassEvent.AfterRenderingPrePasses + 1 : RenderPassEvent.BeforeRenderingTransparents :
                    RenderPassEvent.AfterRenderingTransparents;
            }
#endif

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                float f1 = Time.realtimeSinceStartup;
                if (material == null) return;

                FetchVolumeComponent();

                if (!hbao.IsActive()) return;
             
                FetchRenderParameters(cameraTextureDescriptor);
                CheckParameters();
                UpdateMaterialProperties();
                UpdateShaderKeywords();

                if (isHistoryBufferDirty && hbao.temporalFilterEnabled.value)
                    AllocateHistoryBuffers();
                float f2 = Time.realtimeSinceStartup;
               // Debug.Log(f2-f1);
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null)
                {
                    Debug.LogError("HBAO material has not been correctly initialized...");
                    return;
                }

                if (!hbao.IsActive()) return;

                var cmd = CommandBufferPool.Get("HBAO");
                float t1 = Time.realtimeSinceStartup;
                if (hbao.mode.value == HBAO.Mode.LitAO)
                {
#if URP_10_0_0_OR_NEWER
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
#endif
                    cmd.GetTemporaryRT(ShaderProperties.ssaoTex, aoDesc, FilterMode.Bilinear);
                }
                else
                {
                    cmd.GetTemporaryRT(ShaderProperties.inputTex, sourceDesc, FilterMode.Point);

                    // Source copy
                    CopySource(cmd);
                }
                float t2 = Time.realtimeSinceStartup;
                float t3 = Time.realtimeSinceStartup;
                // AO
                if (hbao.deinterleaving.value == HBAO.Deinterleaving.Disabled)
                {
                    cmd.GetTemporaryRT(ShaderProperties.hbaoTex, aoDesc, FilterMode.Bilinear);
                    AO(cmd);
                }
                else
                {
                    cmd.GetTemporaryRT(ShaderProperties.hbaoTex, reinterleavedAoDesc, FilterMode.Bilinear);
                    DeinterleavedAO(cmd);
                }

                // Blur
                Blur(cmd);
                
                // Temporal Filter
                TemporalFilter(cmd);

                // Composite
                Composite(cmd);

                cmd.ReleaseTemporaryRT(ShaderProperties.hbaoTex);
                float t4 = Time.realtimeSinceStartup;
                if (hbao.mode.value != HBAO.Mode.LitAO) cmd.ReleaseTemporaryRT(ShaderProperties.inputTex);
                float t5 = Time.realtimeSinceStartup;
                context.ExecuteCommandBuffer(cmd);
                float t6 = Time.realtimeSinceStartup;

                if (Time.frameCount%5==0)
                {
                  //  Debug.Log((t2-t1)+"  "+(t4-t3)+"  "+(t6-t5));
                }
                CommandBufferPool.Release(cmd);

                frameCount++;
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (hbao.mode.value == HBAO.Mode.LitAO)
                {
                    cmd.ReleaseTemporaryRT(ShaderProperties.ssaoTex);
#if URP_10_0_0_OR_NEWER
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
#endif
                }
            }

            public void Cleanup()
            {
                ReleaseHistoryBuffers();

                CoreUtils.Destroy(material);
                CoreUtils.Destroy(noiseTex);
            }

            private void FetchVolumeComponent()
            {
                if (hbao == null)
                    hbao = VolumeManager.instance.stack.GetComponent<HBAO>();
            }

            private void FetchRenderParameters(RenderTextureDescriptor cameraTextureDesc)
            {
                cameraTextureDesc.msaaSamples = 1;
                cameraTextureDesc.depthBufferBits = 0;
                sourceDesc = cameraTextureDesc;

                var width = cameraTextureDesc.width;
                var height = cameraTextureDesc.height;
                var downsamplingFactor = hbao.resolution.value == HBAO.Resolution.Full ? 1 : hbao.deinterleaving.value == HBAO.Deinterleaving.Disabled ? 2 : 1;
                if (downsamplingFactor > 1)
                {
                    width = (width + width % 2) / downsamplingFactor;
                    height = (height + height % 2) / downsamplingFactor;
                }

                aoDesc = GetStereoCompatibleDescriptor(width, height, format: colorFormat, readWrite: RenderTextureReadWrite.Linear);

                if (hbao.deinterleaving.value != HBAO.Deinterleaving.Disabled)
                {
                    var reinterleavedWidth = cameraTextureDesc.width + (cameraTextureDesc.width % 4 == 0 ? 0 : 4 - (cameraTextureDesc.width % 4));
                    var reinterleavedHeight = cameraTextureDesc.height + (cameraTextureDesc.height % 4 == 0 ? 0 : 4 - (cameraTextureDesc.height % 4));
                    var deinterleavedWidth = reinterleavedWidth / 4;
                    var deinterleavedHeight = reinterleavedHeight / 4;

                    deinterleavedDepthDesc = GetStereoCompatibleDescriptor(deinterleavedWidth, deinterleavedHeight, format: depthFormat, readWrite: RenderTextureReadWrite.Linear);
                    deinterleavedNormalsDesc = GetStereoCompatibleDescriptor(deinterleavedWidth, deinterleavedHeight, format: normalsFormat, readWrite: RenderTextureReadWrite.Linear);
                    deinterleavedAoDesc = GetStereoCompatibleDescriptor(deinterleavedWidth, deinterleavedHeight, format: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                    reinterleavedAoDesc = GetStereoCompatibleDescriptor(reinterleavedWidth, reinterleavedHeight, format: colorFormat, readWrite: RenderTextureReadWrite.Linear);
                }

                numberOfEyes = 1;
#if ENABLE_VR
                if (XRGraphics.enabled)
                    numberOfEyes = 2;
#endif
            }

            private void AllocateHistoryBuffers()
            {
                ReleaseHistoryBuffers();

                aoHistoryBuffer = new RenderTexture(aoDesc);
                if (hbao.colorBleedingEnabled.value)
                    colorBleedingHistoryBuffer = new RenderTexture(aoDesc);

                // clear history buffers to default
                var lastActive = RenderTexture.active;
                RenderTexture.active = aoHistoryBuffer;
                GL.Clear(false, true, Color.white);
                if (hbao.colorBleedingEnabled.value)
                {
                    RenderTexture.active = colorBleedingHistoryBuffer;
                    GL.Clear(false, true, new Color(0, 0, 0, 1));
                }
                RenderTexture.active = lastActive;

                frameCount = 0;
                //Debug.Log("History Buffers allocated and cleared");
            }

            private void ReleaseHistoryBuffers()
            {
                if (aoHistoryBuffer != null)
                    aoHistoryBuffer.Release();

                if (colorBleedingHistoryBuffer != null)
                    colorBleedingHistoryBuffer.Release();
            }

            private void CopySource(CommandBuffer cmd)
            {
                BlitFullscreenMesh(cmd, source, ShaderProperties.inputTex, material, Pass.Copy);
            }

            private void AO(CommandBuffer cmd)
            {
                BlitFullscreenMeshWithClear(cmd, hbao.mode.value == HBAO.Mode.LitAO ? source : ShaderProperties.inputTex, ShaderProperties.hbaoTex, material, new Color(0, 0, 0, 1), Pass.AO);
            }

            private void DeinterleavedAO(CommandBuffer cmd)
            {
                // Deinterleave depth & normals (4x4)
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
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
                        cmd.GetTemporaryRT(ShaderProperties.depthSliceTex[j + 4 * i], deinterleavedDepthDesc, FilterMode.Point);
                        cmd.GetTemporaryRT(ShaderProperties.normalsSliceTex[j + 4 * i], deinterleavedNormalsDesc, FilterMode.Point);
                    }
                    BlitFullscreenMesh(cmd, BuiltinRenderTextureType.CameraTarget, rtsDepth, material, Pass.Deinterleave_Depth); // outputs 4 render textures
                    BlitFullscreenMesh(cmd, BuiltinRenderTextureType.CameraTarget, rtsNormals, material, Pass.Deinterleave_Normals); // outputs 4 render textures
                }
                cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);

                // AO on each layer
                for (int i = 0; i < 4 * 4; i++)
                {
                    cmd.SetGlobalTexture(ShaderProperties.depthTex, ShaderProperties.depthSliceTex[i]);
                    cmd.SetGlobalTexture(ShaderProperties.normalsTex, ShaderProperties.normalsSliceTex[i]);
                    cmd.SetGlobalVector(ShaderProperties.jitter, s_jitter[i]);
                    cmd.GetTemporaryRT(ShaderProperties.aoSliceTex[i], deinterleavedAoDesc, FilterMode.Point);
                    BlitFullscreenMeshWithClear(cmd, hbao.mode.value == HBAO.Mode.LitAO ? source : ShaderProperties.inputTex, ShaderProperties.aoSliceTex[i], material, new Color(0, 0, 0, 1), Pass.AO_Deinterleaved); // ao
                    cmd.ReleaseTemporaryRT(ShaderProperties.depthSliceTex[i]);
                    cmd.ReleaseTemporaryRT(ShaderProperties.normalsSliceTex[i]);
                }

                // Atlas Deinterleaved AO, 4x4
                cmd.GetTemporaryRT(ShaderProperties.tempTex, reinterleavedAoDesc, FilterMode.Point);
                for (int i = 0; i < 4 * 4; i++)
                {
                    cmd.SetGlobalVector(ShaderProperties.atlasOffset, new Vector2(((i & 1) + (((i & 7) >> 2) << 1)) * deinterleavedAoDesc.width, (((i & 3) >> 1) + ((i >> 3) << 1)) * deinterleavedAoDesc.height));
                    BlitFullscreenMesh(cmd, ShaderProperties.aoSliceTex[i], ShaderProperties.tempTex, material, Pass.Atlas_AO_Deinterleaved); // atlassing
                    cmd.ReleaseTemporaryRT(ShaderProperties.aoSliceTex[i]);
                }

                // Reinterleave AO
                BlitFullscreenMesh(cmd, ShaderProperties.tempTex, ShaderProperties.hbaoTex, material, Pass.Reinterleave_AO); // reinterleave
                cmd.ReleaseTemporaryRT(ShaderProperties.tempTex);
            }

            private void Blur(CommandBuffer cmd)
            {
                if (hbao.blurType.value != HBAO.BlurType.None)
                {
                    cmd.GetTemporaryRT(ShaderProperties.tempTex, aoDesc, FilterMode.Bilinear);
                    cmd.SetGlobalVector(ShaderProperties.blurDeltaUV, new Vector2(1f / sourceDesc.width, 0));
                    BlitFullscreenMesh(cmd, ShaderProperties.hbaoTex, ShaderProperties.tempTex, material, Pass.Blur);
                    cmd.SetGlobalVector(ShaderProperties.blurDeltaUV, new Vector2(0, 1f / sourceDesc.height));
                    BlitFullscreenMesh(cmd, ShaderProperties.tempTex, ShaderProperties.hbaoTex, material, Pass.Blur);
                    cmd.ReleaseTemporaryRT(ShaderProperties.tempTex);
                }
            }

            private void TemporalFilter(CommandBuffer cmd)
            {
                if (hbao.temporalFilterEnabled.value && !renderingInSceneView)
                {
                    if (hbao.colorBleedingEnabled.value)
                    {
                        // For Color Bleeding we have 2 history buffers to fill so there are 2 render targets.
                        // AO is still contained in Color Bleeding history buffer (alpha channel) so that we
                        // can use it as a render texture for the composite pass.
                        var rts = new RenderTargetIdentifier[] {
                        aoHistoryBuffer,
                        colorBleedingHistoryBuffer
                    };
                        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                        cmd.GetTemporaryRT(ShaderProperties.tempTex, aoDesc, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(ShaderProperties.tempTex2, aoDesc, FilterMode.Bilinear);
                        BlitFullscreenMesh(cmd, aoHistoryBuffer, ShaderProperties.tempTex2, material, Pass.Copy);
                        BlitFullscreenMesh(cmd, colorBleedingHistoryBuffer, ShaderProperties.tempTex, material, Pass.Copy);
                        BlitFullscreenMesh(cmd, ShaderProperties.tempTex2, rts, material, Pass.Temporal_Filter);
                        cmd.ReleaseTemporaryRT(ShaderProperties.tempTex);
                        cmd.ReleaseTemporaryRT(ShaderProperties.tempTex2);
                        cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
                        cmd.SetGlobalTexture(ShaderProperties.hbaoTex, colorBleedingHistoryBuffer);
                    }
                    else
                    {
                        // AO history buffer contains ao in aplha channel so we can just use history as
                        // a render texture for the composite pass.
                        cmd.GetTemporaryRT(ShaderProperties.tempTex, aoDesc, FilterMode.Bilinear);
                        BlitFullscreenMesh(cmd, aoHistoryBuffer, ShaderProperties.tempTex, material, Pass.Copy);
                        BlitFullscreenMesh(cmd, ShaderProperties.tempTex, aoHistoryBuffer, material, Pass.Temporal_Filter);
                        cmd.ReleaseTemporaryRT(ShaderProperties.tempTex);
                        cmd.SetGlobalTexture(ShaderProperties.hbaoTex, aoHistoryBuffer);
                    }
                }
            }

            private void Composite(CommandBuffer cmd)
            {
                BlitFullscreenMesh(cmd,
                                   hbao.mode.value == HBAO.Mode.LitAO ? source : ShaderProperties.inputTex,
                                   hbao.mode.value == HBAO.Mode.LitAO && hbao.debugMode.value == HBAO.DebugMode.Disabled ? ShaderProperties.ssaoTex : source,
                                   material,
                                   hbao.debugMode.value == HBAO.DebugMode.ViewNormals ? Pass.Debug_ViewNormals : Pass.Composite
                );

                if (hbao.mode.value == HBAO.Mode.LitAO)
                {
                    cmd.SetGlobalTexture("_ScreenSpaceOcclusionTexture", ShaderProperties.ssaoTex);
                    cmd.SetGlobalVector("_AmbientOcclusionParam", new Vector4(0f, 0f, 0f, hbao.directLightingStrength.value));
                }
            }

            private void UpdateMaterialProperties()
            {
                float f1 = Time.realtimeSinceStartup;
           
                var sourceWidth = cameraData.cameraTargetDescriptor.width;
                var sourceHeight = cameraData.cameraTargetDescriptor.height;

                float tanHalfFovY = Mathf.Tan(0.5f * cameraData.camera.fieldOfView * Mathf.Deg2Rad);
                float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (sourceHeight / (float)sourceWidth));
                float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
                float maxRadInPixels = Mathf.Max(16, hbao.maxRadiusPixels.value * Mathf.Sqrt((sourceWidth * numberOfEyes * sourceHeight) / (1080.0f * 1920.0f)));
                maxRadInPixels /= (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1);

                var targetScale = hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ?
                                      new Vector4(reinterleavedAoDesc.width / (float)sourceWidth, reinterleavedAoDesc.height / (float)sourceHeight, 1.0f / (reinterleavedAoDesc.width / (float)sourceWidth), 1.0f / (reinterleavedAoDesc.height / (float)sourceHeight)) :
                                          hbao.resolution.value == HBAO.Resolution.Half /*&& (settings.perPixelNormals.value == HBAO.PerPixelNormals.Reconstruct2Samples || settings.perPixelNormals.value == HBAO.PerPixelNormals.Reconstruct4Samples)*/ ?
                                              new Vector4((sourceWidth + 0.5f) / sourceWidth, (sourceHeight + 0.5f) / sourceHeight, 1f, 1f) :
                                              Vector4.one;

                material.SetTexture(ShaderProperties.noiseTex, noiseTex);
                material.SetVector(ShaderProperties.inputTexelSize, new Vector4(1f / sourceWidth, 1f / sourceHeight, sourceWidth, sourceHeight));
                material.SetVector(ShaderProperties.aoTexelSize, new Vector4(1f / aoDesc.width, 1f / aoDesc.height, aoDesc.width, aoDesc.height));
                material.SetVector(ShaderProperties.deinterleavedAOTexelSize, new Vector4(1.0f / deinterleavedAoDesc.width, 1.0f / deinterleavedAoDesc.height, deinterleavedAoDesc.width, deinterleavedAoDesc.height));
                material.SetVector(ShaderProperties.reinterleavedAOTexelSize, new Vector4(1f / reinterleavedAoDesc.width, 1f / reinterleavedAoDesc.height, reinterleavedAoDesc.width, reinterleavedAoDesc.height));
                material.SetVector(ShaderProperties.targetScale, targetScale);
                material.SetVector(ShaderProperties.uvToView, new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));
                //material.SetMatrix(ShaderProperties.worldToCameraMatrix, cameraData.camera.worldToCameraMatrix);
                material.SetFloat(ShaderProperties.radius, hbao.radius.value * 0.5f * ((sourceHeight / (hbao.deinterleaving.value == HBAO.Deinterleaving.x4 ? 4 : 1)) / (tanHalfFovY * 2.0f)));
                material.SetFloat(ShaderProperties.maxRadiusPixels, maxRadInPixels);
                material.SetFloat(ShaderProperties.negInvRadius2, -1.0f / (hbao.radius.value * hbao.radius.value));
                material.SetFloat(ShaderProperties.angleBias, hbao.bias.value);
                material.SetFloat(ShaderProperties.aoMultiplier, 2.0f * (1.0f / (1.0f - hbao.bias.value)));
                material.SetFloat(ShaderProperties.intensity, isLinearColorSpace ? hbao.intensity.value : hbao.intensity.value * 0.454545454545455f);
                material.SetFloat(ShaderProperties.multiBounceInfluence, hbao.multiBounceInfluence.value);
                material.SetFloat(ShaderProperties.offscreenSamplesContrib, hbao.offscreenSamplesContribution.value);
                material.SetFloat(ShaderProperties.maxDistance, hbao.maxDistance.value);
                material.SetFloat(ShaderProperties.distanceFalloff, hbao.distanceFalloff.value);
                material.SetColor(ShaderProperties.baseColor, hbao.baseColor.value);
                material.SetFloat(ShaderProperties.blurSharpness, hbao.sharpness.value);
                material.SetFloat(ShaderProperties.colorBleedSaturation, hbao.saturation.value);
                material.SetFloat(ShaderProperties.colorBleedBrightnessMask, hbao.brightnessMask.value);
                material.SetVector(ShaderProperties.colorBleedBrightnessMaskRange, AdjustBrightnessMaskToGammaSpace(new Vector2(Mathf.Pow(hbao.brightnessMaskRange.value.x, 3), Mathf.Pow(hbao.brightnessMaskRange.value.y, 3))));
                material.SetVector(ShaderProperties.temporalParams, hbao.temporalFilterEnabled.value && !renderingInSceneView ? new Vector2(s_temporalRotations[frameCount % 6] / 360.0f, s_temporalOffsets[frameCount % 4]) : Vector2.zero);
                float f2 = Time.realtimeSinceStartup;
               // Debug.Log(f2 - f1);
            }

            private void UpdateShaderKeywords()
            {
                if (m_ShaderKeywords == null || m_ShaderKeywords.Length != 13) m_ShaderKeywords = new string[13];

                m_ShaderKeywords[0] = ShaderProperties.GetOrthographicProjectionKeyword(cameraData.camera.orthographic);
                m_ShaderKeywords[1] = ShaderProperties.GetDirectionsKeyword(hbao.quality.value);
                m_ShaderKeywords[2] = ShaderProperties.GetStepsKeyword(hbao.quality.value);
                m_ShaderKeywords[3] = ShaderProperties.GetNoiseKeyword(hbao.noiseType.value);
                m_ShaderKeywords[4] = ShaderProperties.GetDeinterleavingKeyword(hbao.deinterleaving.value);
                m_ShaderKeywords[5] = ShaderProperties.GetDebugKeyword(hbao.debugMode.value);
                m_ShaderKeywords[6] = ShaderProperties.GetMultibounceKeyword(hbao.useMultiBounce.value, hbao.mode.value == HBAO.Mode.LitAO);
                m_ShaderKeywords[7] = ShaderProperties.GetOffscreenSamplesContributionKeyword(hbao.offscreenSamplesContribution.value);
                m_ShaderKeywords[8] = ShaderProperties.GetPerPixelNormalsKeyword(hbao.perPixelNormals.value);
                m_ShaderKeywords[9] = ShaderProperties.GetBlurRadiusKeyword(hbao.blurType.value);
                m_ShaderKeywords[10] = ShaderProperties.GetVarianceClippingKeyword(hbao.varianceClipping.value);
                m_ShaderKeywords[11] = ShaderProperties.GetColorBleedingKeyword(hbao.colorBleedingEnabled.value, hbao.mode.value == HBAO.Mode.LitAO);
                m_ShaderKeywords[12] = ShaderProperties.GetModeKeyword(hbao.mode.value);

                material.shaderKeywords = m_ShaderKeywords;
            }

            private void CheckParameters()
            {
                if (hbao.deinterleaving.value != HBAO.Deinterleaving.Disabled && SystemInfo.supportedRenderTargetCount < 4)
                    hbao.SetDeinterleaving(HBAO.Deinterleaving.Disabled);

                if (hbao.temporalFilterEnabled.value && !motionVectorsSupported)
                    hbao.EnableTemporalFilter(false);

                if (hbao.colorBleedingEnabled.value && hbao.temporalFilterEnabled.value && SystemInfo.supportedRenderTargetCount < 2)
                    hbao.EnableTemporalFilter(false);

                // Noise texture
                if (noiseTex == null || m_PreviousNoiseType != hbao.noiseType.value)
                {
                    CoreUtils.Destroy(noiseTex);

                    CreateNoiseTexture();

                    m_PreviousNoiseType = hbao.noiseType.value;
                }
            }

            private RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, RenderTextureFormat format = RenderTextureFormat.Default, int depthBufferBits = 0, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
            {
                // Inherit the VR setup from the camera descriptor
                var desc = sourceDesc;
                desc.depthBufferBits = depthBufferBits;
                desc.msaaSamples = 1;
                desc.width = width;
                desc.height = height;
                desc.colorFormat = format;

                if (readWrite == RenderTextureReadWrite.sRGB)
                    desc.sRGB = true;
                else if (readWrite == RenderTextureReadWrite.Linear)
                    desc.sRGB = false;
                else if (readWrite == RenderTextureReadWrite.Default)
                    desc.sRGB = isLinearColorSpace;

                return desc;
            }
            
            public void BlitFullscreenMesh(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, passIndex);
            }

            public void BlitFullscreenMesh(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, Material material, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destinations, destinations[0], 0, CubemapFace.Unknown, -1);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, passIndex);
            }

            public void BlitFullscreenMeshWithClear(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, Color clearColor, int passIndex = 0)
            {
                cmd.SetGlobalTexture(ShaderProperties.mainTex, source);
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.ClearRenderTarget(false, true, clearColor);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, passIndex);
            }

            private Vector2 AdjustBrightnessMaskToGammaSpace(Vector2 v)
            {
                return isLinearColorSpace ? v : ToGammaSpace(v);
            }

            private float ToGammaSpace(float v)
            {
                return Mathf.Pow(v, 0.454545454545455f);
            }

            private Vector2 ToGammaSpace(Vector2 v)
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
                        float r1 = hbao.noiseType.value != HBAO.NoiseType.Dither ? 0.25f * (0.0625f * ((x + y & 3) << 2) + (x & 3)) : MersenneTwister.Numbers[z++];
                        float r2 = hbao.noiseType.value != HBAO.NoiseType.Dither ? 0.25f * ((y - x) & 3) : MersenneTwister.Numbers[z++];
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

        [SerializeField, HideInInspector]
        private Shader shader;
        private HBAORenderPass m_HBAORenderPass;

        void OnDisable()
        {
            if (m_HBAORenderPass != null)
                m_HBAORenderPass.Cleanup();
        }

        public override void Create()
        {
            name = "HBAO";
            float f1 = Time.realtimeSinceStartup;
            m_HBAORenderPass = new HBAORenderPass();
            m_HBAORenderPass.FillSupportedRenderTextureFormats();
            float f2 = Time.realtimeSinceStartup;
          //  Debug.Log(f2-f1);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            shader = Shader.Find("Hidden/Universal Render Pipeline/HBAO");
            if (shader == null)
            {
                Debug.LogWarning("HBAO shader was not found. Please ensure it compiles correctly");
                return;
            }

            if (renderingData.cameraData.postProcessEnabled)
            {
                m_HBAORenderPass.Setup(shader, renderer, renderingData);
                renderer.EnqueuePass(m_HBAORenderPass);
            }
        }
    }
}
