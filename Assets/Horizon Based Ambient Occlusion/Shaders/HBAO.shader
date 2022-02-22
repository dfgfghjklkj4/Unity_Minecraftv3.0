Shader "Hidden/HBAO"
{
	Properties {
		_MainTex ("", any) = "" {}
		_HBAOTex ("", any) = "" {}
        _TempTex("", any) = "" {}
		_NoiseTex("", 2D) = "" {}
		_DepthTex("", any) = "" {}
		_NormalsTex("", any) = "" {}
	}

	CGINCLUDE

    #pragma target 3.0
    #pragma editor_sync_compilation

    #include "UnityCG.cginc"
        
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_HBAOTex);
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_TempTex);
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture);
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraGBufferTexture0); // diffuse color (RGB), occlusion (A)
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraGBufferTexture2); // normal (rgb), --unused-- (a)
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraMotionVectorsTexture);
    UNITY_DECLARE_SCREENSPACE_TEXTURE(_NormalsTex);
    UNITY_DECLARE_TEX2D(_NoiseTex);

    UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
    UNITY_DECLARE_DEPTH_TEXTURE(_DepthTex);

    CBUFFER_START(FrequentlyUpdatedUniforms)
    float4 _Input_TexelSize;
    float4 _AO_TexelSize;
    float4 _DeinterleavedAO_TexelSize;
    float4 _ReinterleavedAO_TexelSize;
    float4 _TargetScale;
	float4 _UVToView;
	float4x4 _WorldToCameraMatrix;
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
	float _AlbedoMultiplier;
	float _ColorBleedBrightnessMask;
	float2 _ColorBleedBrightnessMaskRange;
    float2 _TemporalParams;
	CBUFFER_END

    CBUFFER_START(PerPassUpdatedUniforms)
    float4 _UVTransform;
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
        float3 vertex : POSITION;

        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
        //float2 uvStereo : TEXCOORD1;
        //#if STEREO_INSTANCING_ENABLED
        //uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
        //#endif

        UNITY_VERTEX_OUTPUT_STEREO
    };

    float2 TransformTriangleVertexToUV(float2 vertex)
    {
        float2 uv = (vertex + 1.0) * 0.5;
        return uv;
    }

    Varyings Vert_Default(Attributes input)
    {
        Varyings o;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_OUTPUT(Varyings, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = float4(input.vertex.xy, 0.0, 1.0);
        o.uv = TransformTriangleVertexToUV(input.vertex.xy);

        #if UNITY_UV_STARTS_AT_TOP
        o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

        //o.uvStereo = TransformStereoScreenSpaceTex(o.uv, 1.0);
        o.uv = TransformStereoScreenSpaceTex(o.uv, 1.0);

        return o;
    }

    Varyings Vert_Atlas(Attributes input)
    {
        Varyings o;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_OUTPUT(Varyings, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = float4((input.vertex.xy + float2(-3.0, 1.0)) * (_DeinterleavedAO_TexelSize.zw / _ReinterleavedAO_TexelSize.zw) + 2.0 * _AtlasOffset * _ReinterleavedAO_TexelSize.xy, 0.0, 1.0);
        o.uv = TransformTriangleVertexToUV(input.vertex.xy);

        // flip triangle upside down
        o.vertex.y = 1 - o.vertex.y;

        //o.uvStereo = TransformStereoScreenSpaceTex(o.uv, 1.0);
        o.uv = TransformStereoScreenSpaceTex(o.uv, 1.0);

        return o;
    }

    Varyings Vert_UVTransform(Attributes input)
    {
        Varyings o;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_OUTPUT(Varyings, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = float4(input.vertex.xy, 0.0, 1.0);
        o.uv = TransformTriangleVertexToUV(input.vertex.xy) * _UVTransform.xy + _UVTransform.zw;

        //o.uvStereo = TransformStereoScreenSpaceTex(o.uv, 1.0);
        o.uv = TransformStereoScreenSpaceTex(o.uv, 1.0);

        //#if STEREO_INSTANCING_ENABLED
        //o.stereoTargetEyeIndex = (uint)_DepthSlice;
        //#endif

        return o;
    }

	ENDCG

	SubShader {
        LOD 100
		ZTest Always Cull Off ZWrite Off

		// 0
		Pass {
            Name "HBAO - AO"

			CGPROGRAM
            #pragma multi_compile_local __ DEFERRED_SHADING ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ OFFSCREEN_SAMPLES_CONTRIBUTION
            #pragma multi_compile_local __ NORMALS_CAMERA NORMALS_RECONSTRUCT
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
            #endif

            #if STEPS_2
                #define STEPS       2
            #elif STEPS_3
                #define STEPS       3
            #elif STEPS_4
                #define STEPS       4
            #elif STEPS_6
                #define STEPS       6
            #endif

            #pragma vertex Vert_Default
            #pragma fragment AO_Frag

            #include "HBAO_AO.cginc"
			ENDCG
		}

		// 1
		Pass {
            Name "HBAO - AO Deinterleaved"

			CGPROGRAM
            #pragma multi_compile_local __ DEFERRED_SHADING ORTHOGRAPHIC_PROJECTION
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
            #endif

            #if STEPS_2
                #define STEPS       2
            #elif STEPS_3
                #define STEPS       3
            #elif STEPS_4
                #define STEPS       4
            #elif STEPS_6
                #define STEPS       6
            #endif

            #define DEINTERLEAVED

            #pragma vertex Vert_Default
            #pragma fragment AO_Frag

            #include "HBAO_AO.cginc"
			ENDCG
		}

        // 2
        Pass {
            Name "HBAO - Deinterleave Depth"

            CGPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment DeinterleaveDepth_Frag

            #include "HBAO_Deinterleaving.cginc"
            ENDCG
        }

		// 3
        Pass {
            Name "HBAO - Deinterleave Normals"

            CGPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ NORMALS_CAMERA NORMALS_RECONSTRUCT

            #pragma vertex Vert_Default
            #pragma fragment DeinterleaveNormals_Frag

            #include "HBAO_Deinterleaving.cginc"
            ENDCG
		}

		// 4
		Pass {
            Name "HBAO - Atlas Deinterleaved AO"

			CGPROGRAM
            #pragma vertex Vert_Atlas
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, input.uv);
            }
			ENDCG
		}

		// 5
		Pass {
            Name "HBAO - Reinterleave AO"

			CGPROGRAM
            #pragma vertex Vert_UVTransform
            #pragma fragment ReinterleaveAO_Frag

            #include "HBAO_Deinterleaving.cginc"
			ENDCG
		}

		// 6
		Pass {
            Name "HBAO - Blur"

			CGPROGRAM
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
            #endif

            #pragma vertex Vert_Default
            #pragma fragment Blur_Frag

            #include "HBAO_Blur.cginc"
			ENDCG
		}

        // 7
        Pass {
            Name "HBAO - Temporal Filter"

            CGPROGRAM
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ VARIANCE_CLIPPING_4TAP VARIANCE_CLIPPING_8TAP

            #pragma vertex Vert_Default
            #pragma fragment TemporalFilter_Frag

            #include "HBAO_TemporalFilter.cginc"
            ENDCG
        }

        // 8
        Pass {
            Name "HBAO - Copy"

            CGPROGRAM
            #pragma vertex Vert_Default
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, input.uv);
            }
            ENDCG
        }

        // 9
        Pass {
            Name "HBAO - Composite"

            ColorMask RGB

            CGPROGRAM
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ MULTIBOUNCE
            #pragma multi_compile_local __ DEBUG_AO DEBUG_COLORBLEEDING DEBUG_NOAO_AO DEBUG_AO_AOONLY DEBUG_NOAO_AOONLY

            #pragma vertex Vert_UVTransform
            #pragma fragment Composite_Frag

            #include "HBAO_Composite.cginc"
            ENDCG
        }

        // 10
        Pass {
            Name "HBAO - Composite AfterLighting"

            CGPROGRAM
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ MULTIBOUNCE
            #pragma multi_compile_local __ LIGHTING_LOG_ENCODED

            #pragma vertex Vert_Default
            #pragma fragment Composite_Frag

            #include "HBAO_Composite.cginc"
            ENDCG
        }

        // 11
        Pass {
            Name "HBAO - Composite BeforeReflections"

            CGPROGRAM
            #pragma multi_compile_local __ COLOR_BLEEDING
            #pragma multi_compile_local __ LIGHTING_LOG_ENCODED

            #pragma vertex Vert_Default
            #pragma fragment Composite_Lit_Frag

            #include "HBAO_Composite.cginc"
            ENDCG
        }

        // 12
        Pass {
            Name "HBAO - Debug ViewNormals"

            ColorMask RGB

            CGPROGRAM
            #pragma multi_compile_local __ ORTHOGRAPHIC_PROJECTION
            #pragma multi_compile_local __ NORMALS_CAMERA NORMALS_RECONSTRUCT

            #pragma vertex Vert_UVTransform
            #pragma fragment AO_Frag

            #define DIRECTIONS		1
            #define STEPS			1
            #define DEBUG_VIEWNORMALS
            #include "HBAO_AO.cginc"
            ENDCG
        }
	}

	FallBack off
}
