#ifndef HBAO_COMPOSITE_INCLUDED
#define HBAO_COMPOSITE_INCLUDED

#include "UnityCG.cginc"
#include "HBAO_Common.cginc"

inline half4 FetchOcclusion(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HBAOTex, uv * _TargetScale.zw);
}

inline half4 FetchSceneColor(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);
}

inline half4 FetchGBuffer0(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_TempTex, uv);
}

inline half4 FetchGBuffer3(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);
}

inline half3 MultiBounceAO(float visibility, half3 albedo) {
    half3 a = 2.0404 * albedo - 0.3324;
    half3 b = -4.7951 * albedo + 0.6417;
    half3 c = 2.7552 * albedo + 0.6903;

    float x = visibility;
    return max(x, ((x * a + b) * x + c) * x);
}

half4 Composite_Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    //uint2 positionSS = input.uv * _ScreenSize.xy;

    half4 ao = FetchOcclusion(input.uv);

    ao.a = saturate(pow(abs(ao.a), _Intensity));
    half3 aoColor = lerp(_BaseColor.rgb, half3(1.0, 1.0, 1.0), ao.a);

    half4 col = FetchSceneColor(input.uv);

    #if LIGHTING_LOG_ENCODED
    col.rgb = -log2(col.rgb);
    #endif

    #if MULTIBOUNCE
    aoColor = lerp(aoColor, MultiBounceAO(ao.a, lerp(col.rgb, _BaseColor.rgb, _BaseColor.rgb)), _MultiBounceInfluence);
    #endif

    col.rgb *= aoColor;

    #if COLOR_BLEEDING
    //col.rgb += 1 - ao.rgb;
    col.rgb += ao.rgb;
    #endif

    #if LIGHTING_LOG_ENCODED
    col.rgb = exp2(-col.rgb);
    #endif

    #if DEBUG_AO
    col.rgb = aoColor;
    #elif DEBUG_COLORBLEEDING && COLOR_BLEEDING
    //col.rgb = 1 - ao.rgb;
    col.rgb = ao.rgb;
    #elif DEBUG_NOAO_AO || DEBUG_AO_AOONLY || DEBUG_NOAO_AOONLY
    if (input.uv.x <= 0.4985) {
    #if DEBUG_NOAO_AO || DEBUG_NOAO_AOONLY
        col = FetchSceneColor(input.uv);
    #endif // DEBUG_NOAO_AO || DEBUG_NOAO_AOONLY
        return col;
    }
    if (input.uv.x > 0.4985 && input.uv.x < 0.5015) {
        return half4(0, 0, 0, 1);
    }
    #if DEBUG_AO_AOONLY || DEBUG_NOAO_AOONLY
    col.rgb = aoColor;
    #endif // DEBUG_AO_AOONLY) || DEBUG_NOAO_AOONLY
    #endif // DEBUG_AO
    return col;
}

struct CombinedOutput {
    half4 gbuffer0 : SV_Target0;	// albedo (RGB), occlusion (A)
    half4 gbuffer3 : SV_Target1;	// emission (RGB), unused(A)
};

CombinedOutput Composite_Lit_Frag(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 ao = FetchOcclusion(input.uv);

    ao.a = saturate(pow(abs(ao.a), _Intensity));
    half3 aoColor = lerp(_BaseColor.rgb, half3(1.0, 1.0, 1.0), ao.a);

    half4 albedoOcc = FetchGBuffer0(input.uv);
    half4 emission = FetchGBuffer3(input.uv);

    #if LIGHTING_LOG_ENCODED
    emission.rgb = -log2(emission.rgb);
    #endif

    CombinedOutput o;
    o.gbuffer0 = half4(albedoOcc.rgb, albedoOcc.a * ao.a);
    o.gbuffer3 = half4(emission.rgb * lerp(aoColor, half3(1.0, 1.0, 1.0), saturate((emission.r + emission.g + emission.b) / 3)), emission.a);

    #if COLOR_BLEEDING
    //o.gbuffer3.rgb += 1 - ao.rgb;
    o.gbuffer3.rgb += ao.rgb;
    #endif

    #if LIGHTING_LOG_ENCODED
    o.gbuffer3.rgb = exp2(-o.gbuffer3.rgb);
    #endif

    return o;
}

#endif // HBAO_COMPOSITE_INCLUDED
