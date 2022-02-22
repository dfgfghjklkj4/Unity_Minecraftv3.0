#ifndef HBAO_COMMON_INCLUDED
#define HBAO_COMMON_INCLUDED

#include "UnityCG.cginc"

inline float FetchRawDepth(float2 uv) {
    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv * _TargetScale.xy);
}

inline float LinearizeDepth(float depth) {
    // References: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
#if ORTHOGRAPHIC_PROJECTION
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif // UNITY_REVERSED_Z
    float linearDepth = _ProjectionParams.y + depth * (_ProjectionParams.z - _ProjectionParams.y); // near + depth * (far - near)
#else
    float linearDepth = LinearEyeDepth(depth);
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
    #if NORMALS_RECONSTRUCT
    float3 Pr, Pl, Pt, Pb;
    Pr = FetchViewPos(uv + float2(delta.x, 0));
    Pl = FetchViewPos(uv + float2(-delta.x, 0));
    Pt = FetchViewPos(uv + float2(0, delta.y));
    Pb = FetchViewPos(uv + float2(0, -delta.y));
    float3 N = normalize(cross(MinDiff(P, Pr, Pl), MinDiff(P, Pt, Pb)));
    #else
    #if NORMALS_CAMERA
    float3 N = DecodeViewNormalStereo(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthNormalsTexture, uv * _TargetScale.xy));
    #else
    float3 N = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraGBufferTexture2, uv * _TargetScale.xy).rgb * 2.0 - 1.0;
    N = mul((float3x3)_WorldToCameraMatrix, N);
    #endif // NORMALS_CAMERA
    N = float3(N.x, -N.yz);
    #endif // NORMALS_RECONSTRUCT

    return N;
}

inline float3 FetchViewNormals(float2 uv, float2 delta) {

    float3 P = FetchViewPos(uv);
    return FetchViewNormals(uv, delta, P);
}

#endif // HBAO_COMMON_INCLUDED
