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

#ifndef HBAO_AO_INCLUDED
#define HBAO_AO_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "HBAO_Common.hlsl"

inline float3 FetchLayerViewPos(float2 uv) {
    float depth = LinearizeDepth(SAMPLE_TEXTURE2D_X(_DepthTex, sampler_PointClamp, uv).r);
    return float3((uv * _UVToView.xy + _UVToView.zw) * depth, depth);
}

inline float Falloff(float distanceSquare) {
    // 1 scalar mad instruction
    return distanceSquare * _NegInvRadius2 + 1.0;
}

inline float ComputeAO(float3 P, float3 N, float3 S) {
    float3 V = S - P;
    float VdotV = dot(V, V);
    float NdotV = dot(N, V) * rsqrt(VdotV);

    // Use saturate(x) instead of max(x,0.f) because that is faster on Kepler
    return saturate(NdotV - _AngleBias) * saturate(Falloff(VdotV));
}

inline float2 RotateDirections(float2 dir, float2 rot) {
    return float2(dir.x * rot.x - dir.y * rot.y,
        dir.x * rot.y + dir.y * rot.x);
}

inline float InterleavedGradientNoise(float2 screenPos) {
    // http://www.iryoku.com/downloads/Next-Generation-Post-Processing-in-Call-of-Duty-Advanced-Warfare-v18.pptx (slide 123)
    float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
    return frac(magic.z * frac(dot(screenPos, magic.xy)));
}

inline float2 FetchNoise(float2 screenPos) {
    #if INTERLEAVED_GRADIENT_NOISE
    // Use Jorge Jimenez's IGN noise and GTAO spatial offsets distribution
    // https://blog.selfshadow.com/publications/s2016-shading-course/activision/s2016_pbs_activision_occlusion.pdf (slide 93)
    return float2(InterleavedGradientNoise(screenPos), SAMPLE_TEXTURE2D(_NoiseTex, sampler_PointRepeat, screenPos / 4.0).g);
    #else
    // (cos(alpha), sin(alpha), jitter)
    return SAMPLE_TEXTURE2D(_NoiseTex, sampler_PointRepeat, screenPos / 4.0).rg;
    #endif
}

float4 AO_Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
    //uint2 positionSS = input.uv * _ScreenSize.xy;

    #ifdef DEINTERLEAVED
    float3 P = FetchLayerViewPos(uv);
    #else
    float3 P = FetchViewPos(uv);
    #endif

    #ifndef DEBUG_VIEWNORMALS
    clip(_MaxDistance - P.z);
    #endif

    float stepSize = min((_Radius / P.z), _MaxRadiusPixels) / (STEPS + 1.0);

    #ifdef DEINTERLEAVED
    float3 N = SAMPLE_TEXTURE2D_X(_NormalsTex, sampler_PointClamp, uv).rgb * 2.0 - 1.0;
    float2 rand = _Jitter; // angle, jitter
    #else
    float3 N = FetchViewNormals(uv, _AO_TexelSize.xy, P);
    //float2 rand = FetchNoise(positionSS);
    //float2 rand = FetchNoise(input.positionCS.xy);
    float2 rand = FetchNoise(uv * _AO_TexelSize.zw);
    #endif

    const float alpha = 2.0 * PI / DIRECTIONS;
    float ao = 0;

    #if COLOR_BLEEDING
    static float2 cbUVs[DIRECTIONS * STEPS];
    static float cbContribs[DIRECTIONS * STEPS];
    #endif

    UNITY_UNROLL
    for (int d = 0; d < DIRECTIONS; ++d) {
        float angle = alpha * (float(d) + rand.x + _TemporalParams.x);

        // Compute normalized 2D direction
        float cosA, sinA;
        sincos(angle, sinA, cosA);
        float2 direction = float2(cosA, sinA);

        // Jitter starting sample within the first step
        float rayPixels = (frac(rand.y + _TemporalParams.y) * stepSize + 1.0);

        UNITY_UNROLL
        for (int s = 0; s < STEPS; ++s) {

            #ifdef DEINTERLEAVED
            float2 snappedUV = round(rayPixels * direction) * _DeinterleavedAO_TexelSize.xy + uv;
            float3 S = FetchLayerViewPos(snappedUV);
            #else
            float2 snappedUV = round(rayPixels * direction) * _Input_TexelSize.xy + uv;
            float3 S = FetchViewPos(snappedUV);
            #endif

            rayPixels += stepSize;

            float contrib = ComputeAO(P, N, S);
            #if OFFSCREEN_SAMPLES_CONTRIBUTION
            float2 offscreenAmount = _OffscreenSamplesContrib * (snappedUV - saturate(snappedUV) != 0 ? 1 : 0);
            contrib = max(contrib, offscreenAmount.x);
            contrib = max(contrib, offscreenAmount.y);
            #endif
            ao += contrib;

            #if COLOR_BLEEDING
            int sampleIdx = d * s;
            cbUVs[sampleIdx] = snappedUV;
            cbContribs[sampleIdx] = contrib;
            #endif
        }
    }

    #ifdef DEBUG_VIEWNORMALS
    N = float3(N.x, -N.y, N.z);
    return float4(N * 0.5 + 0.5, 1);
    #else

    #if COLOR_BLEEDING
    half3 col = half3(0, 0, 0);
    UNITY_UNROLL
    for (int s = 0; s < DIRECTIONS * STEPS; s += 2) {
        half3 emission = SAMPLE_TEXTURE2D_X_LOD(_MainTex, sampler_LinearClamp, cbUVs[s], 0).rgb;
        half average = (emission.x + emission.y + emission.z) / 3;
        half scaledAverage = saturate((average - _ColorBleedBrightnessMaskRange.x) / (_ColorBleedBrightnessMaskRange.y - _ColorBleedBrightnessMaskRange.x + 1e-6));
        half maskMultiplier = 1 - (scaledAverage * _ColorBleedBrightnessMask);
        col += emission * cbContribs[s] * maskMultiplier;
    }
    float4 aoOutput = float4(col, ao);
    #else
    float aoOutput = ao;
    #endif

    // apply bias multiplier
    aoOutput *= (_AOmultiplier / (STEPS * DIRECTIONS));

    float fallOffStart = _MaxDistance - _DistanceFalloff;
    float distFactor = saturate((P.z - fallOffStart) / (_MaxDistance - fallOffStart));

    #if COLOR_BLEEDING
    //aoOutput.rgb = saturate(1 - lerp(dot(aoOutput.rgb, 0.333).xxx, aoOutput.rgb, _ColorBleedSaturation));
    aoOutput.rgb = saturate(lerp(dot(aoOutput.rgb, 0.333).xxx, aoOutput.rgb, _ColorBleedSaturation));
    aoOutput = lerp(saturate(float4(aoOutput.rgb, 1 - aoOutput.a)), float4(0, 0, 0, 1), distFactor);
    return aoOutput;
    #else
    aoOutput = lerp(saturate(1 - aoOutput), 1, distFactor);
    return float4(EncodeFloatRG(saturate(P.z * (1.0 / _ProjectionParams.z))), 1.0, aoOutput);
    #endif
       
    #endif // DEBUG_VIEWNORMALS
}

#endif // HBAO_AO_INCLUDED
