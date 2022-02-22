Shader "Hidden/Universal Render Pipeline/HBAO"
{
    Properties
    {
        _MainTex("", any) = "" {}
        _HBAOTex("", any) = "" {}
        _TempTex("", any) = "" {}
        _NoiseTex("", 2D) = "" {}
        _DepthTex("", any) = "" {}
        _NormalsTex("", any) = "" {}
    }

    HLSLINCLUDE

    #pragma target 3.0
    #pragma prefer_hlslcc gles
    #pragma exclude_renderers d3d11_9x
    #pragma editor_sync_compilation
    //#pragma enable_d3d11_debug_symbols

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D_X(_MainTex);
    TEXTURE2D_X(_HBAOTex);
    TEXTURE2D_X(_TempTex);
    TEXTURE2D_X(_DepthTex);
    TEXTURE2D_X(_NormalsTex);
    //TEXTURE2D_X(_CameraNormalsTexture);
    TEXTURE2D(_NoiseTex);
    SAMPLER(sampler_LinearClamp);
    SAMPLER(sampler_PointRepeat);
    SAMPLER(sampler_PointClamp);

    CBUFFER_START(FrequentlyUpdatedUniforms)
    float4 _Input_TexelSize;
    float4 _AO_TexelSize;
    float4 _DeinterleavedAO_TexelSize;
    float4 _ReinterleavedAO_TexelSize;
    float4 _TargetScale;
    float4 _UVToView;
    //float4x4 _WorldToCameraMatrix;
    float _Radius;
    float _MaxRadiusPixels;
    float _NegInvRadius2;
    float _AngleBias;
    float _AOmultiplier;
    float _Intensity;
    half4 _BaseColor;
    float _MultiBounceInfluence;
    float _OffscreenSamplesContrib;
    float _MaxDistance;
    float _DistanceFalloff;
    float _BlurSharpness;
    float _ColorBleedSaturation;
    float _ColorBleedBrightnessMask;
    float2 _ColorBleedBrightnessMaskRange;
    float2 _TemporalParams;
    CBUFFER_END

    CBUFFER_START(PerPassUpdatedUniforms)
    float2 _BlurDeltaUV;
    CBUFFER_END

    CBUFFER_START(PerPassUpdatedDeinterleavingUniforms)
    float2 _Deinterleave_Offset00;
    float2 _Deinterleave_Offset10;
    float2 _Deinterleave_Offset01;
    float2 _Deinterleave_Offset11;
    float2 _AtlasOffset;
    float2 _Jitter;
    CBUFFER_END

    struct Attributes
    {
        float4 positionOS   : POSITION;
        float2 uv           : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS    : SV_POSITION;
        float2 uv            : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        //output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
        // Note: The pass is setup with a mesh already in CS
        // Therefore, we can just output vertex position
        output.positionCS = float4(input.positionOS.xyz, 1.0);
        #if UNITY_UV_STARTS_AT_TOP
        output.positionCS.y *= -1;
        #endif
        output.uv = input.uv;
        return output;
    }

    Varyings Vert_Atlas(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        //float3 pos = input.positionOS.xyz;
        //pos.xy = pos.xy * (_DeinterleavedAO_TexelSize.zw / _ReinterleavedAO_TexelSize.zw) + _AtlasOffset * _ReinterleavedAO_TexelSize.xy;
        //output.positionCS = TransformObjectToHClip(pos);
        output.positionCS = float4((input.positionOS.xy + float2(-3.0, 1.0)) * (_DeinterleavedAO_TexelSize.zw / _ReinterleavedAO_TexelSize.zw) + 2.0 * _AtlasOffset * _ReinterleavedAO_TexelSize.xy, 0.0, 1.0);
        // flip triangle upside down
        output.positionCS.y = 1 - output.positionCS.y;
        output.uv = input.uv;
        return output;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass // 0
        {
            Name "HBAO - AO"

            HLSLPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ OFFSCREEN_SAMPLES_CONTRIBUTION
            #pragma multi_compile_local __ NORMALS_RECONSTRUCT2 NORMALS_RECONSTRUCT4
            #pragma multi_compile_local __ INTERLEAVED_GRADIENT_NOISE
            #pragma multi_compile_local DIRECTIONS_3 DIRECTIONS_4 DIRECTIONS_6 DIRECTIONS_8
            #pragma multi_compile_local STEPS_2 STEPS_3 STEPS_4 STEPS_6

            #if DIRECTIONS_3
                #define DIRECTIONS  3
            #elif DIRECTIONS_4
                #define DIRECTIONS  4
            #elif DIRECTIONS_6
                #define DIRECTIONS  6
            #elif DIRECTIONS_8
                #define DIRECTIONS  8
            #else
                #define DIRECTIONS  1
            #endif

            #if STEPS_2
                #define STEPS       2
            #elif STEPS_3
                #define STEPS       3
            #elif STEPS_4
                #define STEPS       4
            #elif STEPS_6
                #define STEPS       6
            #else
                #define STEPS       1
            #endif

            #pragma vertex Vert
            #pragma fragment AO_Frag

            #include "HBAO_AO.hlsl"
            ENDHLSL
        }

        Pass // 1
        {
            Name "HBAO - AO Deinterleaved"

            HLSLPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ OFFSCREEN_SAMPLES_CONTRIBUTION
            #pragma multi_compile_local DIRECTIONS_3 DIRECTIONS_4 DIRECTIONS_6 DIRECTIONS_8
            #pragma multi_compile_local STEPS_2 STEPS_3 STEPS_4 STEPS_6

            #if DIRECTIONS_3
                #define DIRECTIONS  3
            #elif DIRECTIONS_4
                #define DIRECTIONS  4
            #elif DIRECTIONS_6
                #define DIRECTIONS  6
            #elif DIRECTIONS_8
                #define DIRECTIONS  8
            #else
                #define DIRECTIONS  1
            #endif

            #if STEPS_2
                #define STEPS       2
            #elif STEPS_3
                #define STEPS       3
            #elif STEPS_4
                #define STEPS       4
            #elif STEPS_6
                #define STEPS       6
            #else
                #define STEPS       1
            #endif

            #define DEINTERLEAVED

            #pragma vertex Vert
            #pragma fragment AO_Frag

            #include "HBAO_AO.hlsl"
            ENDHLSL
        }

        // 2
        Pass {
            Name "HBAO - Deinterleave Depth"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DeinterleaveDepth_Frag

            #include "HBAO_Deinterleaving.hlsl"
            ENDHLSL
        }

        // 3
        Pass {
            Name "HBAO - Deinterleave Normals"

            HLSLPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ NORMALS_RECONSTRUCT2 NORMALS_RECONSTRUCT4

            #pragma vertex Vert
            #pragma fragment DeinterleaveNormals_Frag

            #include "HBAO_Deinterleaving.hlsl"
            ENDHLSL
        }

        // 4
		Pass {
            Name "HBAO - Atlas Deinterleaved AO"

            HLSLPROGRAM
            #pragma vertex Vert_Atlas
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
                return SAMPLE_TEXTURE2D_X(_MainTex, sampler_PointClamp, uv);
            }
            ENDHLSL
		}

		// 5
		Pass {
            Name "HBAO - Reinterleave AO"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ReinterleaveAO_Frag

            #include "HBAO_Deinterleaving.hlsl"
            ENDHLSL
		}

        Pass // 6
        {
            Name "HBAO - Blur"

            HLSLPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local BLUR_RADIUS_2 BLUR_RADIUS_3 BLUR_RADIUS_4 BLUR_RADIUS_5

            #if BLUR_RADIUS_2
                #define KERNEL_RADIUS  2
            #elif BLUR_RADIUS_3
                #define KERNEL_RADIUS  3
            #elif BLUR_RADIUS_4
                #define KERNEL_RADIUS  4
            #elif BLUR_RADIUS_5
                #define KERNEL_RADIUS  5
            #else
                #define KERNEL_RADIUS  0
            #endif

            #pragma vertex Vert
            #pragma fragment Blur_Frag

            #include "HBAO_Blur.hlsl"
            ENDHLSL
        }

        Pass // 7
        {
            Name "HBAO - Temporal Filter"

            HLSLPROGRAM
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ VARIANCE_CLIPPING_4TAP VARIANCE_CLIPPING_8TAP

            #pragma vertex Vert
            #pragma fragment TemporalFilter_Frag

            #include "HBAO_TemporalFilter.hlsl"
            ENDHLSL
        }

        Pass // 8
        {
            Name "HBAO - Copy"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
                return SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }

        Pass // 9
        {
            Name "HBAO - Composite"

            ColorMask RGB

            HLSLPROGRAM
            #pragma multi_compile_local __ LIT_AO
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ MULTIBOUNCE
            #pragma multi_compile_local __ DEBUG_AO DEBUG_COLORBLEEDING DEBUG_NOAO_AO DEBUG_AO_AOONLY DEBUG_NOAO_AOONLY

            #pragma vertex Vert
            #pragma fragment Composite_Frag

            #include "HBAO_Composite.hlsl"
            ENDHLSL
        }

        Pass // 10
        {
            Name "HBAO - Debug ViewNormals"

            ColorMask RGB

            HLSLPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ NORMALS_RECONSTRUCT2 NORMALS_RECONSTRUCT4

            #pragma vertex Vert
            #pragma fragment AO_Frag

            #define DIRECTIONS		1
            #define STEPS			1
            #define DEBUG_VIEWNORMALS

            #include "HBAO_AO.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
