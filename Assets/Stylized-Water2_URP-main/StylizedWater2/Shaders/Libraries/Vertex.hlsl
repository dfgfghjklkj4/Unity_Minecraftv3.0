//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

struct Attributes
{
	float4 positionOS 	: POSITION;
	float4 uv 			: TEXCOORD0;
	float4 normalOS 	: NORMAL;
	float4 tangentOS 	: TANGENT;
	float4 color 		: COLOR0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS 	: SV_POSITION;
	float4 uv 			: TEXCOORD0;
	float4 color 		: COLOR0;
	half4 fogFactorAndVertexLight : TEXCOORD1;
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord 	: TEXCOORD2;
	#endif
	//wPos.x in w-component
	float4 normal 		: NORMAL;
	// #if _NORMALMAP
	// //wPos.y in w-component
	// float4 tangent 		: TANGENT;
	// //wPos.z in w-component
	// float4 bitangent 	: TEXCOORD3;
	// #else
	// float3 wPos 		: TEXCOORD4;
	// #endif
	float3 wPos 		: TEXCOORD4;

	#if defined(SCREEN_POS)
	float4 screenPos 	: TEXCOORD5;
	#endif
	float4 lightmapUVOrVertexSH : TEXCOORD6;
	UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.uv.xy = input.uv.xy;
	output.uv.z = _TimeParameters.x;
	output.uv.w = 0;

// #if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON) 
// #if defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
// 	CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(input.positionOS, input.normalOS.xyz, input.tangentOS)
// #else
//     CURVEDWORLD_TRANSFORM_VERTEX(input.positionOS)
// #endif
// #endif

	/* Test basic waves for rivers
	float waveFrequency = frac(input.uv.y ) * 20 + frac(-input.uv.x)  * 30 ;
	input.positionOS.xyz += (sin(waveFrequency +  _TimeParameters.x * 20) * 0.5 + 0.5) * input.normalOS * 0.05;;
	*/
	
	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS.xyz, input.tangentOS);
	
	float4 vertexColor = GetVertexColor(input.color.rgba, _VertexColorMask.rgba);
	
#if _WAVES && !defined(TESSELLATION_ON)
	//Returns mesh or world-space UV
	float2 uv = GetSourceUV(output.uv.xy, positionWS.xz, _WorldSpaceUV);

	//Vertex animation
	WaveInfo waves = GetWaveInfo(uv, TIME_VERTEX * _WaveSpeed,  _WaveFadeDistance.x, _WaveFadeDistance.y);
	//Offset in direction of normals (only when using mesh uv)
	if(_WorldSpaceUV == 0) waves.position *= normalInput.normalWS.xyz;
	positionWS.xz += waves.position.xz * HORIZONTAL_DISPLACEMENT_SCALAR * _WaveHeight;
	positionWS.y += waves.position.y * _WaveHeight * lerp(1, 0, vertexColor.b);
#endif

	//SampleWaveSimulationVertex(positionWS, positionWS.y);

	output.positionCS = TransformWorldToHClip(positionWS);
	half fogFactor = CalculateFogFactor(output.positionCS.xyz);

#ifdef SCREEN_POS
	output.screenPos = ComputeScreenPos(output.positionCS);
#endif
	output.normal = float4(normalInput.normalWS, positionWS.x);
// #if _NORMALMAP
// 	output.tangent = float4(normalInput.tangentWS, positionWS.y);
// 	output.bitangent = float4(normalInput.bitangentWS, positionWS.z);
// #else
// 	output.wPos = positionWS.xyz;
	output.wPos = positionWS.xyz;

	OUTPUT_SH(normalInput.normalWS.xyz, output.lightmapUVOrVertexSH.xyz);

	//Lambert shading
	half3 vertexLight = 0;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	vertexLight = VertexLighting(positionWS, normalInput.normalWS);
#endif

	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
	output.color = vertexColor;

// #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
// 	VertexPositionInputs vertexInput = (VertexPositionInputs)0;
// 	vertexInput.positionWS = positionWS;
// 	vertexInput.positionCS = output.positionCS;
// 	output.shadowCoord = GetShadowCoord(vertexInput);
// #endif
	return output;
}