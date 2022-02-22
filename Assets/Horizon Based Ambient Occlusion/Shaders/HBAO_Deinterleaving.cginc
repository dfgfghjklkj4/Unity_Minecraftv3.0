#ifndef HBAO_DEINTERLEAVING_INCLUDED
#define HBAO_DEINTERLEAVING_INCLUDED

#include "HBAO_Common.cginc"

struct DeinterleavedOutput {
    float4 Z00 : SV_Target0;
    float4 Z10 : SV_Target1;
    float4 Z01 : SV_Target2;
    float4 Z11 : SV_Target3;
};

DeinterleavedOutput DeinterleaveDepth_Frag(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    DeinterleavedOutput o;

    float2 pos = floor(input.uv * _DeinterleavedAO_TexelSize.zw) * 4.0;
    float2 uv00 = (pos + _Deinterleave_Offset00 + 0.5) * _ReinterleavedAO_TexelSize.xy;
    float2 uv10 = (pos + _Deinterleave_Offset10 + 0.5) * _ReinterleavedAO_TexelSize.xy;
    float2 uv01 = (pos + _Deinterleave_Offset01 + 0.5) * _ReinterleavedAO_TexelSize.xy;
    float2 uv11 = (pos + _Deinterleave_Offset11 + 0.5) * _ReinterleavedAO_TexelSize.xy;

    o.Z00 = FetchRawDepth(uv00).rrrr;
    o.Z10 = FetchRawDepth(uv10).rrrr;
    o.Z01 = FetchRawDepth(uv01).rrrr;
    o.Z11 = FetchRawDepth(uv11).rrrr;
    return o;
}

DeinterleavedOutput DeinterleaveNormals_Frag(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    DeinterleavedOutput o;

    float2 pos = floor(input.uv * _DeinterleavedAO_TexelSize.zw) * 4.0;
    float2 uv00 = (pos + _Deinterleave_Offset00 + 0.5) * _ReinterleavedAO_TexelSize.xy;
    float2 uv10 = (pos + _Deinterleave_Offset10 + 0.5) * _ReinterleavedAO_TexelSize.xy;
    float2 uv01 = (pos + _Deinterleave_Offset01 + 0.5) * _ReinterleavedAO_TexelSize.xy;
    float2 uv11 = (pos + _Deinterleave_Offset11 + 0.5) * _ReinterleavedAO_TexelSize.xy;

    o.Z00 = float4(FetchViewNormals(uv00, _ReinterleavedAO_TexelSize.xy) * 0.5 + 0.5, 0);
    o.Z10 = float4(FetchViewNormals(uv10, _ReinterleavedAO_TexelSize.xy) * 0.5 + 0.5, 0);
    o.Z01 = float4(FetchViewNormals(uv01, _ReinterleavedAO_TexelSize.xy) * 0.5 + 0.5, 0);
    o.Z11 = float4(FetchViewNormals(uv11, _ReinterleavedAO_TexelSize.xy) * 0.5 + 0.5, 0);

    return o;
}

half4 ReinterleaveAO_Frag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 offset = fmod(floor(input.uv * _ReinterleavedAO_TexelSize.zw), 4.0);
    float2 uv = (floor(input.uv * _DeinterleavedAO_TexelSize.zw) + (offset * _DeinterleavedAO_TexelSize.zw) + 0.5) * _ReinterleavedAO_TexelSize.xy;

    return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, uv);
}

#endif // HBAO_DEINTERLEAVING_INCLUDED
