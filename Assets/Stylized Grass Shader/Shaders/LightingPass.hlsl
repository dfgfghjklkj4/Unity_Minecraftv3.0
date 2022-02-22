//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//#define DEBUG_BEND_VECTORS
//#define DEBUG_BEND_AREA

#if VERSION_GREATER_EQUAL(12,0)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#endif

#if VERSION_GREATER_EQUAL(12,0)
#define bakedLightmapUV staticLightmapUV
#else
#define bakedLightmapUV lightmapUV
#endif

struct Varyings
{
	//half fogFactor : TEXCOORD0;
	float4 uv                       : TEXCOORD0;
	DECLARE_LIGHTMAP_OR_SH(bakedLightmapUV, vertexSH, 1); //Called staticLightmapUV in URP12+
	
//#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR) //Always needed
	float3 positionWS               : TEXCOORD2;
//#endif
	
	half3  normalWS                 : TEXCOORD3;

#ifdef _NORMALMAP
	half4 tangentWS                 : TEXCOORD4;  // xyz: tangent, w: sign
#endif

	half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord              : TEXCOORD8; // compute shadow coord per-vertex for the main light
#endif

	#ifdef DYNAMICLIGHTMAP_ON
	float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
	#endif
		
	float4 positionCS               : SV_POSITION;
	float4 color					: COLOR0;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;
	
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	float posOffset = ObjectPosRand01();

	WindSettings wind = PopulateWindSettings(_WindAmbientStrength, _WindSpeed, _WindDirection, _WindSwinging, BEND_MASK, _WindObjectRand, _WindVertexRand, _WindRandStrength, _WindGustStrength, _WindGustFreq);
	BendSettings bending = PopulateBendSettings(_BendMode, BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);

	//Object space position, normals (and tangents)
	VertexInputs vertexInputs = GetVertexInputs(input, _NormalParams.y);

	//Original vertex normals should be perpendicular to the vertex face!
	//For lighting, force the normals straight up. Later in the LitPassVertex the normals can be modified through parameters

	vertexInputs.normalOS = lerp(vertexInputs.normalOS, normalize(vertexInputs.positionOS.xyz), _NormalParams.x * lerp(1, BEND_MASK, _NormalParams.z));
	//Apply transformations and bending/wind (Can't use GetVertexPositionInputs, because it would amount to double matrix transformations)
	VertexOutput vertexData = GetVertexOutput(vertexInputs, posOffset, wind, bending);
	
	//Vertex color
	output.color = input.color;
	output.color = ApplyVertexColor(input.positionOS, vertexData.positionWS.xyz, _BaseColor.rgb, AO_MASK, _OcclusionStrength, _VertexDarkening, _HueVariation, posOffset);

	half fogFactor = ComputeFogFactor(vertexData.positionCS.z);

	half3 viewDirWS = GetWorldSpaceViewDir(vertexData.positionWS);
	output.normalWS = vertexData.normalWS;
	//output.viewDirWS = viewDirWS;

#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	real sign = input.tangentOS.w * GetOddNegativeScale();
	half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	output.tangentWS = tangentWS;
#endif
	
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
	output.viewDirTS = viewDirTS;
#endif

	//Lightmap UV resolves to "staticLightmapUV" in URP12+
	OUTPUT_LIGHTMAP_UV(input.bakedLightmapUV, unity_LightmapST, output.bakedLightmapUV);
	#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
	
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	//Apply per-vertex light if enabled in pipeline
	//Pass to fragment shader to apply in Lighting function
	half3 vertexLight = VertexLighting(vertexData.positionWS, vertexData.normalWS);
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
	output.fogFactorAndVertexLight.x = fogFactor;
	output.fogFactorAndVertexLight.yzw = 0;
#endif

	//#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	output.positionWS = vertexData.positionWS;
	//#endif
	
#if _NORMALMAP || defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
	output.uv.zw = TRANSFORM_TEX(input.uv, _BumpMap);
	output.tangentWS = vertexData.tangentWS;
#else
	//Initialize with 0
	output.uv.zw = 0;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	output.shadowCoord = TransformWorldToShadowCoord(vertexData.positionWS);
#endif

	output.uv.xy = TRANSFORM_TEX(input.uv, _BaseMap);
	output.positionCS = vertexData.positionCS;

	return output;
}

void ModifySurfaceData(Varyings input, out SurfaceData surfaceData)
{
	float4 albedoAlpha = SampleAlbedoAlpha(input.uv.xy, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
	#ifdef _ALPHATEST_ON
	AlphaClip(albedoAlpha.a, _Cutoff, input.positionCS.xyz, input.positionWS.xyz, _FadeParams);
	#endif
	
	//Apply hue var and ambient occlusion from vertex stage
	albedoAlpha.rgb *= input.color.rgb;

	//Apply color map per-pixel
	if (_ColorMapUV.w == 1) {
		float mask = smoothstep(_ColorMapHeight, 1.0 + _ColorMapHeight, sqrt(input.color.a));
		albedoAlpha.rgb = lerp(ApplyColorMap(input.positionWS.xyz, albedoAlpha.rgb, _ColorMapStrength), albedoAlpha.rgb, mask);
	}

	float bendingMask = saturate(GetBendVector(input.positionWS.xyz).a);
	albedoAlpha.rgb = lerp(albedoAlpha.rgb, albedoAlpha.rgb * _BendTint.rgb, bendingMask * sqrt(input.color.a) * _BendTint.a);

	//Albedo must be saturated, may cause sparkles otherwise
	surfaceData.albedo = saturate(albedoAlpha.rgb);
	//Not using specular setup, free to use this to pass data
	surfaceData.specular = float3(0, 0, 0);
	surfaceData.metallic = 0.0;
	surfaceData.smoothness = lerp(0.0, _Smoothness, input.color.a);
#ifdef _NORMALMAP
	surfaceData.normalTS = SampleNormal(input.uv.zw, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
#else
	surfaceData.normalTS = float3(0.5, 0.5, 1.0);
#endif
	surfaceData.emission = 0.0;
	surfaceData.occlusion = 1;
	surfaceData.alpha = albedoAlpha.a;
	
	#if VERSION_GREATER_EQUAL(10,0)
	surfaceData.clearCoatMask = 0.0h;
	surfaceData.clearCoatSmoothness = 0.0h;
	#endif

	//Debug
	//surfaceData.albedo = surfaceData.smoothness.xxx;
}

//This function is a testament to how convoluted cross-compatibility between difference URP versions has become
void PopulateLightingInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;
	
	//#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	inputData.positionWS = input.positionWS.xyz;
	//#endif

	half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS.xyz));
	
	half3x3 tangentToWorld = 0;
	#if defined(_NORMALMAP)
	float sgn = input.tangentWS.w; // should be either +1 or -1
	float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);;
	#endif
	
#if defined(_NORMALMAP)
	inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
	inputData.normalWS = input.normalWS;
#endif
	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
	
	inputData.viewDirectionWS = viewDirWS;

	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) //No shadow cascades
	inputData.shadowCoord = input.shadowCoord;
	#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
	#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
	#endif

	#if VERSION_GREATER_EQUAL(12,0)
	inputData.positionCS = input.positionCS;
	inputData.tangentToWorld = tangentToWorld; //Not actually using this value of the InputData struct, but URP12+ dictates it
	#endif

	inputData.fogCoord = input.fogFactorAndVertexLight.x;
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

	#if VERSION_GREATER_EQUAL(10,0)
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.bakedLightmapUV);
	#endif
	
	#if defined(DYNAMICLIGHTMAP_ON) && VERSION_GREATER_EQUAL(12,0)
	inputData.bakedGI = SAMPLE_GI(input.bakedLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
	#else
	inputData.bakedGI = SAMPLE_GI(input.bakedLightmapUV, input.vertexSH, inputData.normalWS);
	#endif
	
#if VERSION_GREATER_EQUAL(10,0)
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.bakedLightmapUV);
#endif

	#if defined(DEBUG_DISPLAY) && VERSION_GREATER_EQUAL(12,0)//URP 12+
	#if defined(DYNAMICLIGHTMAP_ON)
	inputData.dynamicLightmapUV = input.dynamicLightmapUV;
	#endif
	#if defined(LIGHTMAP_ON)
	inputData.bakedLightmapUV = input.bakedLightmapUV;
	#else
	inputData.vertexSH = input.vertexSH;
	#endif
	#endif

}

#if defined(SHADERPASS_DEFERRED) && VERSION_GREATER_EQUAL(12,0)
FragmentOutput LightingPassFragment(Varyings input)
#else
half4 LightingPassFragment(Varyings input) : SV_Target
#endif
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	WindSettings wind;
	if(_WindGustTint > 0) wind = PopulateWindSettings(_WindAmbientStrength, _WindSpeed, _WindDirection, _WindSwinging, AO_MASK, _WindObjectRand, _WindVertexRand, _WindRandStrength, _WindGustStrength, _WindGustFreq);

	SurfaceData surfaceData;
	//Can't use standard function, since including LitInput.hlsl breaks the SRP batcher
	ModifySurfaceData(input, surfaceData);
	
	InputData inputData;
	//Standard URP function barely changes, but adds things like clear coat and detail normals
	PopulateLightingInputData(input, surfaceData.normalTS, inputData);

	#if VERSION_GREATER_EQUAL(12,0)
	#if defined(DEBUG_DISPLAY)
	SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);
	#endif
	#endif

	#ifdef DEBUG_BEND_VECTORS
	return saturate(float4(GetBendVector(input.positionWS).xyz * 0.5 + 0.5, 1.0));
	#endif

	#ifdef DEBUG_BEND_AREA
	float2 bendUV = GetBendMapUV(input.positionWS);
	float edgeMask = BoundsEdgeMask(input.positionWS.xz);
	return float4(edgeMask.xxx, 1.0);
	return float4(lerp(float3(1,0,0), float3(0,1,0), edgeMask > 0 ? 1 : 0), 1.0);
	#endif

	//Get main light first, need attenuation to mask wind gust
	Light mainLight = GetMainLight(inputData.shadowCoord);

	//Tint by wind gust
	if(_WindGustTint > 0)
	{
		wind.gustStrength = 1;
		float gust = SampleGustMap(input.positionWS.xyz, wind);
		surfaceData.albedo += gust * _WindGustTint * 10 * (mainLight.shadowAttenuation) * input.color.a;
		surfaceData.albedo = saturate(surfaceData.albedo);
	}

	float translucencyMask = input.color.a * _Translucency;

	#if VERSION_GREATER_EQUAL(12,0)
	#ifdef _DBUFFER
	#if !_DISABLE_DECALS
	ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
	#endif
	#endif
	#endif

	TranslucencyData tData = (TranslucencyData)0;
	tData.strengthDirect = _Translucency;
	tData.strengthIndirect = _TranslucencyIndirect;
	tData.exponent = _TranslucencyFalloff;
	tData.thickness = input.color.a;
	tData.offset = _TranslucencyOffset;
	tData.light = mainLight;
	
	//Deferred
#if defined(SHADERPASS_DEFERRED) && VERSION_GREATER_EQUAL(12,0)
	// in LitForwardPass GlobalIllumination (and temporarily LightingPhysicallyBased) are called inside UniversalFragmentPBR
	// in Deferred rendering we store the sum of these values (and of emission as well) in the GBuffer
	BRDFData brdfData;
	InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
	
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
	half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);

	ApplyTranslucency(surfaceData, inputData, tData);
	
	return BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);

	//Forward
#else

	float3 finalColor = ApplyLighting(surfaceData, inputData, tData);
	finalColor = MixFog(finalColor, inputData.fogCoord);

	return half4(finalColor, surfaceData.alpha);
#endif
}