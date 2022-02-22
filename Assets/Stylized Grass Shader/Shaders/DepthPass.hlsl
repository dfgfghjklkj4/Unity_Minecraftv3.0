//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

struct Varyings
{
	float2 uv           : TEXCOORD0;
	float4 positionCS   : SV_POSITION;
#ifdef _ALPHATEST_ON
	float4 positionWS   : TEXCOORD1;
#endif

	#ifdef SHADERPASS_DEPTHNORMALS
	float3 normalWS		: TEXCOORD2;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

	float posOffset = ObjectPosRand01();

	WindSettings wind = PopulateWindSettings(_WindAmbientStrength, _WindSpeed, _WindDirection, _WindSwinging, BEND_MASK, _WindObjectRand, _WindVertexRand, _WindRandStrength, _WindGustStrength, _WindGustFreq);
	BendSettings bending = PopulateBendSettings(_BendMode, BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);

	VertexInputs vertexInputs = GetVertexInputs(input, _NormalParams.w);
	VertexOutput vertexData = GetVertexOutput(vertexInputs, posOffset, wind, bending);

	output.positionCS = vertexData.positionCS;
#ifdef _ALPHATEST_ON
	output.positionWS.xyz = vertexData.positionWS;
#endif

	#ifdef SHADERPASS_DEPTHNORMALS
	output.normalWS = vertexData.normalWS;
	#endif

	return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef _ALPHATEST_ON
	float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
	AlphaClip(alpha, _Cutoff, input.positionCS.xyz, input.positionWS.xyz, _FadeParams);
#endif

	return 0;
}

half4 DepthNormalsFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef SHADERPASS_DEPTHNORMALS
	#ifdef _ALPHATEST_ON
	float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
	AlphaClip(alpha, _Cutoff, input.positionCS.xyz, input.positionWS.xyz, _FadeParams);
	#endif

	return half4(input.normalWS, 0.0);
#else
	return DepthOnlyFragment(input);
#endif
}