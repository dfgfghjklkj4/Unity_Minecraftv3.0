#ifndef HBAO_TEMPORALFILTER_INCLUDED
#define HBAO_TEMPORALFILTER_INCLUDED

#include "HBAO_Common.cginc"

struct HistoryOutput {
    float4 aoHistory           : SV_Target0;
    #if COLOR_BLEEDING
    half4 colorBleedingHistory : SV_Target1;
    #endif
};

#if COLOR_BLEEDING
#define CTYPE half4
#else
#define CTYPE half
#endif

inline void FetchAoAndDepth(float2 uv, inout CTYPE ao, inout float2 depth) {
    #if COLOR_BLEEDING
    ao = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HBAOTex, uv);
    depth = EncodeFloatRG(saturate(LinearizeDepth(FetchRawDepth(uv)) * (1.0 / _ProjectionParams.z)));
    #else
    float3 aod = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HBAOTex, uv).rga;
    ao = aod.z;
    depth = aod.xy;
    #endif
}

inline float2 FetchMotionVectors(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraMotionVectorsTexture, uv * _TargetScale.xy).rg;
}

inline float4 FetchAoHistory(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);
}

inline half4 FetchColorBleedingHistory(float2 uv) {
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_TempTex, uv);
}

inline CTYPE FetchNeighbor(float2 uv, float2 offset) {
    #if COLOR_BLEEDING
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HBAOTex, uv + _AO_TexelSize.xy * offset);
    #else
    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HBAOTex, uv + _AO_TexelSize.xy * offset).a;
    #endif
}

inline half DisocclusionTest(float2 uvm1, float2 depth, float2 depthm1) {

    // disocclusion test
    // https://developer.nvidia.com/sites/default/files/akamai/gamedev/files/gdc12/GDC12_Bavoil_Stable_SSAO_In_BF3_With_STF.pdf (Page 19)
    float z = DecodeFloatRG(depth);
    float zm1 = DecodeFloatRG(depthm1);
    // for fetching zi-1, use clamp-to-border to discard out-of-frame data, with borderZ = 0.f
    // https://developer.nvidia.com/sites/default/files/akamai/gamedev/files/gdc12/GDC12_Bavoil_Stable_SSAO_In_BF3_With_STF.pdf (Page 39)
    // if (uvm1.x < 0 || uvm1.y < 0 || uvm1.x > 1 || uvm1.y > 1) zm1 = 0;
    // if (uvm1.x < 0 || uvm1.y < 0 || uvm1.x > 1 || uvm1.y > 1) => dot(step(half4(uvm1, 1, 1), half4(0, 0, uvm1)), 1) is 1 if out-of-frame, so
    zm1 *= 1.0 - dot(step(float4(uvm1, 1, 1), float4(0, 0, uvm1)), 1);
    // relaxed disocclusion test: abs(1.0 - (z / zm1)) > 0.1 => 10% 
    // float disocclusion = max(sign(abs(1.0 - (z / zm1)) - 0.1), 0.0);
    float disocclusion = abs(1.0 - (z / zm1)) > 0.1;

    return disocclusion;
}

inline CTYPE VarianceClipping(float2 uv, CTYPE ao, CTYPE aom1, float velocityWeight) {

    // neighborhood clamping
    // http://twvideo01.ubm-us.net/o1/vault/gdc2016/Presentations/Pedersen_LasseJonFuglsang_TemporalReprojectionAntiAliasing.pdf // (pages 26-28)
    // superseded by variance clipping
    // http://developer.download.nvidia.com/gameworks/events/GDC2016/msalvi_temporal_supersampling.pdf (page 23-29)
    #if VARIANCE_CLIPPING_4TAP
    CTYPE cT = FetchNeighbor(uv, float2(0, 1));
    CTYPE cR = FetchNeighbor(uv, float2(1, 0));
    CTYPE cB = FetchNeighbor(uv, float2(0, -1));
    CTYPE cL = FetchNeighbor(uv, float2(-1, 0));
    // compute 1st and 2nd color moments
    CTYPE m1 = ao + cT + cR + cB + cL;
    CTYPE m2 = ao * ao + cT * cT + cR * cR + cB * cB + cL * cL;
    // aabb from mean u and variance sigma2
    CTYPE mu = m1 / 5.0;
    CTYPE sigma = sqrt(m2 / 5.0 - mu * mu);

    #elif VARIANCE_CLIPPING_8TAP
    CTYPE cTL = FetchNeighbor(uv, float2(-1, 1));
    CTYPE cT = FetchNeighbor(uv, float2(0, 1));
    CTYPE cTR = FetchNeighbor(uv, float2(1, 1));
    CTYPE cR = FetchNeighbor(uv, float2(1, 0));
    CTYPE cBR = FetchNeighbor(uv, float2(1, -1));
    CTYPE cB = FetchNeighbor(uv, float2(0, -1));
    CTYPE cBL = FetchNeighbor(uv, float2(-1, -1));
    CTYPE cL = FetchNeighbor(uv, float2(-1, 0));
    // compute 1st and 2nd color moments
    CTYPE m1 = ao + cTL + cT + cTR + cR + cBR + cB + cBL + cL;
    CTYPE m2 = ao * ao + cTL * cTL + cT * cT + cTR * cTR + cR * cR + cBR * cBR + cB * cB + cBL * cBL + cL * cL;
    // aabb from mean u and variance sigma2
    CTYPE mu = m1 / 9.0;
    CTYPE sigma = sqrt(m2 / 9.0 - mu * mu);
    #endif

    #if VARIANCE_CLIPPING_4TAP || VARIANCE_CLIPPING_8TAP
    float gamma = lerp(75.0, 0.75, velocityWeight); // scale down sigma for reduced ghosting 
    CTYPE cmin = mu - gamma * sigma;
    CTYPE cmax = mu + gamma * sigma;

    // clipping
    return clamp(aom1, cmin, cmax);
    #else
    return aom1;
    #endif
}

HistoryOutput TemporalFilter_Frag(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // fetch current frame data
    CTYPE ao; float2 depth;
    FetchAoAndDepth(input.uv, ao, depth);

    // fetch motion vectors, calculate previous frame uv
    float2 mv = FetchMotionVectors(input.uv);
    float2 uvm1 = input.uv - mv;
    float mvl = length(mv);

    // fetch history
    float4 aoHistory = FetchAoHistory(uvm1);
    #if COLOR_BLEEDING
    half4 colorBleedingHistory = FetchColorBleedingHistory(uvm1);
    CTYPE aom1 = colorBleedingHistory;
    #else
    CTYPE aom1 = aoHistory.w;
    #endif
    float2 depthm1 = aoHistory.xy;
    float mvlm1 = aoHistory.z;

    // velocity weight
    float velocityWeight = saturate(abs(mvl - mvlm1) * 300.0);

    // do disocclusion test
    half disocclusion = DisocclusionTest(uvm1, depth, depthm1);

    // apply velocity weight and disocclusion
    #if COLOR_BLEEDING
    aom1.a = aom1.a + saturate(dot(float2(velocityWeight, disocclusion), 1.0)) * (ao.a - aom1.a);
    #else
    aom1 = aom1 + saturate(dot(float2(velocityWeight, disocclusion), 1.0)) * (ao - aom1);
    #endif

    // do variance clipping
    aom1 = VarianceClipping(input.uv, ao, aom1, velocityWeight);

    // exponential accumulation buffer
    // http://www.klayge.org/material/4_11/Filmic%20SMAA%20v7.pdf (page 54)
    // http://developer.download.nvidia.com/gameworks/events/GDC2016/msalvi_temporal_supersampling.pdf (page 13)
    ao = aom1 + 0.1 * (ao - aom1);

    HistoryOutput o;
    #if COLOR_BLEEDING
    o.aoHistory = float4(depth, mvl, ao.a);
    o.colorBleedingHistory = ao;
    #else
    o.aoHistory = float4(depth, mvl, ao);
    #endif

    return o;   
}

#endif // HBAO_TEMPORALFILTER_INCLUDED
