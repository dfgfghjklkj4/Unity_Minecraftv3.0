#ifndef HBAO_COMMON_INCLUDED
#define HBAO_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#if VERSION_GREATER_EQUAL(10, 0)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#endif

inline float FetchRawDepth(float2 uv) {
    return SampleSceneDepth(uv * _TargetScale.xy);
}

inline float LinearizeDepth(float depth) {
    // References: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
#if ORTHOGRAPHIC_PROJECTION
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif // UNITY_REVERSED_Z
    float linearDepth = _ProjectionParams.y + depth * (_ProjectionParams.z - _ProjectionParams.y); // near + depth * (far - near)
#else
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
#endif // ORTHOGRAPHIC_PROJECTION
    return linearDepth;
}

inline float3 FetchViewPos(float2 uv) {
    float depth = LinearizeDepth(FetchRawDepth(uv));
    return float3((uv * _UVToView.xy + _UVToView.zw) * depth, depth);
}

inline float3 MinDiff(float3 P, float3 Pr, float3 Pl) {
    float3 V1 = Pr - P;
    float3 V2 = P - Pl;
    return (dot(V1, V1) < dot(V2, V2)) ? V1 : V2;
}

inline float3 FetchViewNormals(float2 uv, float2 delta, float3 P) {
#if NORMALS_RECONSTRUCT4
    float3 Pr, Pl, Pt, Pb;
    Pr = FetchViewPos(uv + float2(delta.x, 0));
    Pl = FetchViewPos(uv + float2(-delta.x, 0));
    Pt = FetchViewPos(uv + float2(0, delta.y));
    Pb = FetchViewPos(uv + float2(0, -delta.y));
    float3 N = normalize(cross(MinDiff(P, Pr, Pl), MinDiff(P, Pt, Pb)));
#elif NORMALS_RECONSTRUCT2
    float3 Pr, Pt;
    Pr = FetchViewPos(uv + float2(delta.x, 0));
    Pt = FetchViewPos(uv + float2(0, delta.y));
    float3 N = normalize(cross(Pt - P, P - Pr));
#else
#if VERSION_GREATER_EQUAL(10, 0)
    //float3 N = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, uv * _TargetScale.xy).rgb * 2.0 - 1.0;
    float3 N = SampleSceneNormals(uv * _TargetScale.xy);
#else
    float3 N = float3(0, 0, 0);
#endif
    //N = float3(N.x, -N.yz);
    N = float3(N.x, -N.y, N.z);
#endif
    return N;
}

inline float3 FetchViewNormals(float2 uv, float2 delta) {

    float3 P = FetchViewPos(uv);
    return FetchViewNormals(uv, delta, P);
}

// https://aras-p.info/blog/2009/07/30/encoding-floats-to-rgba-the-final/

inline float2 EncodeFloatRG(float v) {
    float2 enc = float2(1.0, 255.0) * v;
    enc = frac(enc);
    enc.x -= enc.y * (1.0 / 255.0);
    return enc;
}

inline float DecodeFloatRG(float2 rg) {
    return dot(rg, float2(1.0, 1 / 255.0));
}

#endif // HBAO_COMMON_INCLUDED
