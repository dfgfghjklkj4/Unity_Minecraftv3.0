#ifndef HBAO_COMPOSITE_INCLUDED
#define HBAO_COMPOSITE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "HBAO_Common.hlsl"

inline half4 FetchOcclusion(float2 uv) {
    return SAMPLE_TEXTURE2D_X(_HBAOTex, sampler_LinearClamp, uv * _TargetScale.zw);
}

inline half4 FetchSceneColor(float2 uv) {
    //return LOAD_TEXTURE2D_X(_MainTex, positionSS); // load not supported on GLES2
    return SAMPLE_TEXTURE2D_X(_MainTex, sampler_PointClamp, uv);
}

inline half3 MultiBounceAO(float visibility, half3 albedo) {
    half3 a = 2.0404 * albedo - 0.3324;
    half3 b = -4.7951 * albedo + 0.6417;
    half3 c = 2.7552 * albedo + 0.6903;

    float x = visibility;
    return max(x, ((x * a + b) * x + c) * x);
}

float4 Composite_Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    //uint2 positionSS = input.uv * _ScreenSize.xy;
    float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

    half4 ao = FetchOcclusion(uv);

    ao.a = saturate(pow(abs(ao.a), _Intensity));

    #if LIT_AO
    half3 aoColor = ao.aaa;
    half4 col = ao.aaaa;
    #else
    half3 aoColor = lerp(_BaseColor.rgb, half3(1.0, 1.0, 1.0), ao.a);
    half4 col = FetchSceneColor(uv);

    #if MULTIBOUNCE
    aoColor = lerp(aoColor, MultiBounceAO(ao.a, lerp(col.rgb, _BaseColor.rgb, _BaseColor.rgb)), _MultiBounceInfluence);
    #endif

    col.rgb *= aoColor;

    #if COLOR_BLEEDING
    //col.rgb += 1 - ao.rgb;
    col.rgb += ao.rgb;
    #endif
    #endif // LIT_AO

    #if DEBUG_AO
    col.rgb = aoColor;
    #elif DEBUG_COLORBLEEDING && COLOR_BLEEDING
    //col.rgb = 1 - ao.rgb;
    col.rgb = ao.rgb;
    #elif DEBUG_NOAO_AO || DEBUG_AO_AOONLY || DEBUG_NOAO_AOONLY
    if (uv.x <= 0.4985) {
    #if DEBUG_NOAO_AO || DEBUG_NOAO_AOONLY
        col = FetchSceneColor(uv);
    #endif // DEBUG_NOAO_AO || DEBUG_NOAO_AOONLY
        return col;
    }
    if (uv.x > 0.4985 && uv.x < 0.5015) {
        return half4(0, 0, 0, 1);
    }
    #if DEBUG_AO_AOONLY || DEBUG_NOAO_AOONLY
    col.rgb = aoColor;
    #endif // DEBUG_AO_AOONLY) || DEBUG_NOAO_AOONLY
    #endif // DEBUG_AO
    return col;
}

#endif // HBAO_COMPOSITE_INCLUDED
