//----------------------------------------------------------------------------------
//
// Copyright (c) 2014, NVIDIA CORPORATION. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//  * Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
//  * Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//  * Neither the name of NVIDIA CORPORATION nor the names of its
//    contributors may be used to endorse or promote products derived
//    from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ``AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
// OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//----------------------------------------------------------------------------------

#ifndef HBAO_BLUR_INCLUDED
#define HBAO_BLUR_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "HBAO_Common.hlsl"

#if COLOR_BLEEDING

inline void FetchAoAndDepth(float2 uv, inout half4 ao, inout float depth) {
    ao = SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_PointClamp, uv, 0);
    depth = LinearizeDepth(FetchRawDepth(uv));
}

inline float CrossBilateralWeight(float r, float d, float d0) {
    const float BlurSigma = (float)KERNEL_RADIUS * 0.5;
    const float BlurFalloff = 1.0 / (2.0*BlurSigma*BlurSigma);

    float dz = (d0 - d) * _BlurSharpness;
    return exp2(-r*r*BlurFalloff - dz*dz);
}

inline void ProcessSample(float4 ao, float z, float r, float d0, inout half4 totalAO, inout float totalW) {
    float w = CrossBilateralWeight(r, d0, z);
    totalW += w;
    totalAO += w * ao;
}

inline void ProcessRadius(float2 uv0, float2 deltaUV, float d0, inout half4 totalAO, inout float totalW) {
    half4 ao;
    float z;
    float2 uv;
    UNITY_UNROLL
    for (int r = 1; r <= KERNEL_RADIUS; r++) {
        uv = uv0 + r * deltaUV;
        FetchAoAndDepth(uv, ao, z);
        ProcessSample(ao, z, r, d0, totalAO, totalW);
    }
}

inline half4 ComputeBlur(float2 uv0, float2 deltaUV) {
    half4 totalAO;
    float depth;
    FetchAoAndDepth(uv0, totalAO, depth);
    float totalW = 1.0;

    ProcessRadius(uv0, -deltaUV, depth, totalAO, totalW);
    ProcessRadius(uv0, deltaUV, depth, totalAO, totalW);

    totalAO /= totalW;
    return totalAO;
}

#else

inline void FetchAoAndDepth(float2 uv, inout half ao, inout float2 depth) {
    float3 aod = SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_PointClamp, uv, 0).rga;
    ao = aod.z;
    depth = aod.xy;
}

inline float CrossBilateralWeight(float r, float d, float d0) {
    const float BlurSigma = (float)KERNEL_RADIUS * 0.5;
    const float BlurFalloff = 1.0 / (2.0*BlurSigma*BlurSigma);

    float dz = (d0 - d) * _ProjectionParams.z * _BlurSharpness;
    return exp2(-r*r*BlurFalloff - dz*dz);
}

inline void ProcessSample(float2 aoz, float r, float d0, inout half totalAO, inout float totalW) {
    float w = CrossBilateralWeight(r, d0, aoz.y);
    totalW += w;
    totalAO += w * aoz.x;
}

inline void ProcessRadius(float2 uv0, float2 deltaUV, float d0, inout half totalAO, inout float totalW) {
    half ao;
    float z;
    float2 d, uv;
    UNITY_UNROLL
    for (int r = 1; r <= KERNEL_RADIUS; r++) {
        uv = uv0 + r * deltaUV;
        FetchAoAndDepth(uv, ao, d);
        z = DecodeFloatRG(d);
        ProcessSample(float2(ao, z), r, d0, totalAO, totalW);
    }
}

inline float4 ComputeBlur(float2 uv0, float2 deltaUV) {
    half totalAO;
    float2 depth;
    FetchAoAndDepth(uv0, totalAO, depth);
    float d0 = DecodeFloatRG(depth);
    float totalW = 1.0;
    
    ProcessRadius(uv0, -deltaUV, d0, totalAO, totalW);
    ProcessRadius(uv0, deltaUV, d0, totalAO, totalW);

    totalAO /= totalW;
    return float4(depth, 1.0, totalAO);
}
#endif // COLOR_BLEEDING

float4 Blur_Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

    return ComputeBlur(uv, _BlurDeltaUV);
}

#endif // HBAO_BLUR_INCLUDED
