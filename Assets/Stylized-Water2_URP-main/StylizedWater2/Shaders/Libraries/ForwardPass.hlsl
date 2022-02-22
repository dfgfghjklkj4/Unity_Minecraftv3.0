//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//#define DEBUG_NORMALS

//#define RESAMPLE_REFRACTION_DEPTH
//#define CAMERA_DEPTH_ABSORPTION

//Note: Throws an error about a BLENDWEIGHTS vertex attribute on GLES when VR is enabled (fixed in URP 10+)
//Possibly related to: https://issuetracker.unity3d.com/issues/oculus-a-non-system-generated-input-signature-parameter-blendindices-cannot-appear-after-a-system-generated-value
#if SHADER_API_GLES3 && SHADER_LIBRARY_VERSION_MAJOR < 10
#define FRONT_FACE_SEMANTIC_REAL VFACE
#else
#define FRONT_FACE_SEMANTIC_REAL FRONT_FACE_SEMANTIC
#endif

#if UNDERWATER_ENABLED
half4 ForwardPassFragment(Varyings input, FRONT_FACE_TYPE vertexFace : FRONT_FACE_SEMANTIC_REAL) : SV_Target
#else
half4 ForwardPassFragment(Varyings input) : SV_Target
#endif
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	#if UNDERWATER_ENABLED
	//return float4(0,1,0,1);
	#endif
	
	float3 finalColor = 0;
	float alpha = 1;

	float vFace = 1.0;
	//0 = back face
	#if UNDERWATER_ENABLED
	vFace = IS_FRONT_VFACE(vertexFace, true, false);
	//finalColor = lerp(float3(1,0,0), float3(0,1,0), IS_FRONT_VFACE(vFace, true, false));
	//return float4(finalColor.rgb, 1);
	#endif
	
	float4 vertexColor = input.color; //Mask already applied in vertex shader
	//return float4(vertexColor.aaa, 1);

	//Vertex normal in world-space
	float3 normalWS = normalize(input.normal.xyz);
// #if _NORMALMAP
// 	float3 WorldTangent = input.tangent.xyz;
// 	float3 WorldBiTangent = input.bitangent.xyz;
// 	float3 wPos = float3(input.normal.w, input.tangent.w, input.bitangent.w);
// #else
// 	float3 wPos = input.wPos;
// #endif
	float3 wPos = input.wPos;
	//Not normalized for depth-pos reconstruction. Normalization required for lighting (otherwise breaks on mobile)
	float3 viewDir = (_WorldSpaceCameraPos - wPos);
	float3 viewDirNorm = SafeNormalize(viewDir);
	//return float4(viewDir, 1);
	
	half VdotN = 1.0 - saturate(dot(viewDirNorm, normalWS));
	
	#if _FLAT_SHADING
	float3 dpdx = ddx(wPos.xyz);
	float3 dpdy = ddy(wPos.xyz);
	normalWS = normalize(cross(dpdy, dpdx));
	#endif

	//Returns mesh or world-space UV
	float2 uv = GetSourceUV(input.uv.xy, wPos.xz, _WorldSpaceUV);
	float2 flowMap = float2(1, 1);

	half slope = 0;
	#if _RIVER
	slope = GetSlopeInverse(normalWS);
	//return float4(slope, slope, slope, 1);
	#endif

	// Waves
 	float height = 0;
 	float3 waveNormal = normalWS;
// #if _WAVES
// 	WaveInfo waves = GetWaveInfo(uv, TIME * _WaveSpeed, _WaveFadeDistance.x, _WaveFadeDistance.y);
// 	#if !_FLAT_SHADING
// 		//Flatten by blue vertex color weight
// 		waves.normal = lerp(waves.normal, normalWS, lerp(0, 1, vertexColor.b));
// 		//Blend wave/vertex normals in world-space
// 		waveNormal = BlendNormalWorldspaceRNM(waves.normal, normalWS, UP_VECTOR);
// 	#endif
// 	//return float4(waveNormal.xyz, 1);
// 	height = waves.position.y * 0.5 + 0.5;
// 	height *= lerp(1, 0, vertexColor.b);
// 	//return float4(height, height, height, 1);
//
// 	//vertices are already displaced on XZ, in this case the world-space UV needs the same treatment
// 	if(_WorldSpaceUV == 1) uv.xy -= waves.position.xz * HORIZONTAL_DISPLACEMENT_SCALAR * _WaveHeight;
// 	//return float4(frac(uv.xy), 0, 1);
// #endif

	float4 ShadowCoords = float4(0, 0, 0, 0);
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && !defined(UNLIT)
	ShadowCoords = input.shadowCoord;
	#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(UNLIT)
	ShadowCoords = TransformWorldToShadowCoord(wPos);
	#endif

	Light mainLight = GetMainLight(ShadowCoords);

	half shadowMask = 1;
	#if _ADVANCED_SHADING
	shadowMask = lerp(1.0, GetShadows(wPos.xyz), vFace);
	//return float4(shadowMask,shadowMask,shadowMask,1);
	#endif

	//Normals
	float3 NormalsCombined = float3(0.5, 0.5, 1);
	float3 worldTangentNormal = waveNormal;
	
	
// #if _NORMALMAP
// 	NormalsCombined = SampleNormals(uv * _NormalTiling, wPos, TIME, flowMap, _NormalSpeed, slope);
// 	//return float4((NormalsCombined.x * 0.5 + 0.5), (NormalsCombined.y * 0.5 + 0.5), 1, 1);
//
// 	worldTangentNormal = normalize(TransformTangentToWorld(NormalsCombined, half3x3(WorldTangent, WorldBiTangent, waveNormal)));
//
// 	#ifdef DEBUG_NORMALS
// 	return float4(SRGBToLinear(float3(NormalsCombined.x * 0.5 + 0.5, NormalsCombined.y * 0.5 + 0.5, 1)), 1.0);
// 	#endif
// #endif

#ifdef SCREEN_POS
	float4 ScreenPos = input.screenPos;
#else
	float4 ScreenPos = 0;
#endif

	#ifdef UNDERWATER_ENABLED
	ClipSurface(ScreenPos, wPos, input.positionCS, vFace);
	#endif
	//return float4(depth.linear01, depth.linear01, depth.linear01, 1);

	#if _REFRACTION
	float4 refractedScreenPos = ScreenPos.xyzw + (float4(worldTangentNormal.xz, 0, 0) * (_RefractionStrength * lerp(0.1, 0.01,  unity_OrthoParams.w)));
	#endif

	float3 opaqueWorldPos = wPos;
	float opaqueDist = 1;
	float surfaceDepth = opaqueDist;
	
#if !_DISABLE_DEPTH_TEX
	SceneDepth depth = SampleDepth(ScreenPos);
	opaqueWorldPos = ReconstructViewPos(ScreenPos, viewDir, depth);
	//return float4(frac(opaqueWorldPos.xyz), 1);

	//Invert normal when viewing backfaces
	float normalSign = ceil(dot(viewDirNorm, normalWS));
	normalSign = normalSign == 0 ? -1 : 1;
	
	opaqueDist = DepthDistance(wPos, opaqueWorldPos, normalWS * normalSign);
	//return float4(opaqueDist,opaqueDist,opaqueDist,1);
	
#if _ADVANCED_SHADING && _REFRACTION
	SceneDepth depthRefracted = SampleDepth(refractedScreenPos);
	float3 opaqueWorldPosRefracted = ReconstructViewPos(refractedScreenPos, viewDir, depthRefracted);

	//Reject any offset pixels above water
	float refractionMask = saturate((wPos - opaqueWorldPosRefracted).y) + (1-vFace);
	//return float4(refractionMask.xxx, 1.0);
	refractedScreenPos = lerp(ScreenPos, refractedScreenPos, refractionMask);
	
	//Double sample depth to avoid depth discrepancies (though this doesn't always offer the best result)
	#ifdef RESAMPLE_REFRACTION_DEPTH
	surfaceDepth = SurfaceDepth(depthRefracted, input.positionCS);
	#else
	//surfaceDepth = depth.eye - LinearEyeDepth(input.positionCS.z, _ZBufferParams);
	surfaceDepth = SurfaceDepth(depth, input.positionCS);
	#endif
	
#else
	surfaceDepth = SurfaceDepth(depth, input.positionCS);
#endif
#endif

	float waterDensity = 1;
#if !_DISABLE_DEPTH_TEX

	float distanceAttenuation = 1.0 - exp(-surfaceDepth * _DepthVertical * lerp(0.1, 0.01, unity_OrthoParams.w));
	float heightAttenuation = saturate(lerp(opaqueDist * _DepthHorizontal, 1.0 - exp(-opaqueDist * _DepthHorizontal), _DepthExp));
	
	waterDensity = max(distanceAttenuation, heightAttenuation);

	#if UNDERWATER_ENABLED
	waterDensity = lerp(1, waterDensity, vFace);
	#endif
	
	//return float4(waterDensity.xxx, 1.0);
#endif
	
	float intersection = 0;
#if _SHARP_INERSECTION || _SMOOTH_INTERSECTION
	float interSecGradient = 1-saturate(exp(opaqueDist) / _IntersectionLength);

	// #if _DISABLE_DEPTH_TEX
	// interSecGradient = 0;
	// #endif
	
	if (_IntersectionSource == 1) interSecGradient = vertexColor.r;
	if (_IntersectionSource == 2) interSecGradient = saturate(interSecGradient + vertexColor.r);

	intersection = SampleIntersection(uv.xy, interSecGradient, TIME * _IntersectionSpeed);
	intersection *= _IntersectionColor.a;

	#if UNDERWATER_ENABLED
	intersection *= vFace;
	#endif

	// #if _WAVES
	// //Prevent from peering through waves when camera is at the water level
	// if(wPos.y < opaqueWorldPos.y) intersection = 0;
	// #endif
	
	//Flatten normals on intersection foam
	waveNormal = lerp(waveNormal, normalWS, intersection);
#endif
	//return float4(intersection,intersection,intersection,1);

	//FOAM
	float foam = 0;
	#if _FOAM

	#if !_RIVER
	float foamMask = lerp(1, saturate(height), _FoamWaveMask);
	foamMask = pow(abs(foamMask), _FoamWaveMaskExp);
	#else
	float foamMask = 1;
	#endif

	foam = SampleFoam(uv * _FoamTiling, TIME, flowMap, _FoamSize, foamMask, slope);

	#if _RIVER
	foam *= saturate(_FoamColor.a + 1-slope + vertexColor.a);
	#else
	foam *= saturate(_FoamColor.a + vertexColor.a);
	#endif

	#if UNDERWATER_ENABLED
	//foam *= vFace;
	#endif
	
	//return float4(foam, foam, foam, 1);
	#endif

	#if WAVE_SIMULATION
	SampleWaveSimulationFoam(wPos, foam);
	#endif

	//Albedo
	float4 baseColor = lerp(_ShallowColor, _BaseColor, waterDensity);
	baseColor.rgb += _WaveTint * height;
	
	finalColor.rgb = baseColor.rgb;
	alpha = baseColor.a;

	float3 sparkles = 0;
#if _NORMALMAP
	float NdotL = saturate(dot(UP_VECTOR, worldTangentNormal));
	half sunAngle = saturate(dot(UP_VECTOR, mainLight.direction));
	half angleMask = saturate(sunAngle * 10); /* 1.0/0.10 = 10 */
	sparkles = saturate(step(_SparkleSize, (saturate(NormalsCombined.y) * NdotL))) * _SparkleIntensity * mainLight.color * angleMask;
	
	finalColor.rgb += sparkles.rgb;
#endif
	//return float4(baseColor.rgb, alpha);

	half4 sunSpec = 0;
#ifndef _SPECULARHIGHLIGHTS_OFF
	float3 sunReflectionNormals = worldTangentNormal;

	#if _FLAT_SHADING //Use face normals
	sunReflectionNormals = waveNormal;
	#endif
	
	//Blinn-phong reflection
	sunSpec = SunSpecular(mainLight, viewDirNorm, sunReflectionNormals, _SunReflectionDistortion, _SunReflectionSize, _SunReflectionStrength);
	sunSpec.rgb *=  saturate((1-foam) * (1-intersection) * shadowMask); //Hide
#endif

	//Reflection probe
#ifndef _ENVIRONMENTREFLECTIONS_OFF
	float3 refWorldTangentNormal = lerp(waveNormal, normalize(waveNormal + worldTangentNormal), _ReflectionDistortion);

	#if _FLAT_SHADING //Skip, not a good fit
	refWorldTangentNormal = waveNormal;
	#endif
	
	float3 reflectionVector = reflect(-viewDirNorm , refWorldTangentNormal);
	float2 reflectionPerturbation = lerp(waveNormal.xz * 0.5, worldTangentNormal.xy, _ReflectionDistortion).xy;
	float3 reflections = SampleReflections(reflectionVector, _ReflectionBlur, _PlanarReflectionsParams, _PlanarReflectionsEnabled, ScreenPos.xyzw, wPos, refWorldTangentNormal, viewDirNorm, reflectionPerturbation);
	
	half reflectionFresnel = ReflectionFresnel(refWorldTangentNormal, viewDirNorm, _ReflectionFresnel);
	//return float4(reflectionFresnel.xxx, 1);
	finalColor.rgb = lerp(finalColor.rgb, reflections, _ReflectionStrength * reflectionFresnel * vFace);
	//return float4(finalColor.rgb, 1);
#endif

#if _CAUSTICS
	float3 caustics = SampleCaustics(opaqueWorldPos.xz + lerp(waveNormal.xz, NormalsCombined.xz, _CausticsDistortion), TIME * _CausticsSpeed, _CausticsTiling) * _CausticsBrightness;

	#if _ADVANCED_SHADING
	//Mask by shadows
	caustics *= shadowMask;
	#endif
	//return float4(caustics, caustics, caustics, 1);

	float causticsMask = waterDensity;
	causticsMask = saturate(causticsMask + intersection + 1-vFace);

	#if _RIVER
	//Reduce caustics visibility by supposed water turbulence
	causticsMask = lerp(1, causticsMask, slope);
	#endif
	finalColor = lerp(finalColor + caustics, finalColor, causticsMask);
#endif

	// Translucency
	TranslucencyData translucencyData = (TranslucencyData)0;
#if _TRANSLUCENCY
	float waveHeight = saturate(height);
	#if !_WAVES || _FLAT_SHADING
	waveHeight = 1;
	#endif
	
	//Note value is subtracted
	float transmissionMask = saturate((foam * 0.25) + (1-shadowMask)); //Foam isn't 100% opaque
	//transmissionMask = 0;
	//return float4(transmissionMask, transmissionMask, transmissionMask, 1);

	translucencyData = PopulateTranslucencyData(_ShallowColor.rgb, mainLight.direction, mainLight.color, viewDirNorm, lerp(UP_VECTOR, waveNormal, vFace), worldTangentNormal, transmissionMask, _TranslucencyParams);
	#if UNDERWATER_ENABLED
	translucencyData.strength *= lerp(_UnderwaterFogBrightness * _UnderwaterSubsurfaceStrength, 1, vFace);
	#endif
#endif

	//Foam application on top of everything up to this point
	#if _FOAM
	finalColor.rgb = lerp(finalColor.rgb, _FoamColor.rgb, foam);
	#endif

	#if _SHARP_INERSECTION || _SMOOTH_INTERSECTION
	//Layer intersection on top of everything
	finalColor.rgb = lerp(finalColor.rgb, _IntersectionColor.rgb, intersection);
	#endif

	//Full alpha on intersection and foam
	alpha = saturate(alpha + intersection + foam);

	#if _FLAT_SHADING //Skip, not a good fit
	worldTangentNormal = waveNormal;
	#else
	//At this point, normal strength should affect lighting
	half normalMask = saturate((intersection + foam));
	worldTangentNormal = lerp(waveNormal, worldTangentNormal, saturate(_NormalStrength - normalMask));
	#endif
	
	//return float4(normalMask, normalMask, normalMask, 1);

	//Horizon color (note: not using normals, since they are perturbed by waves)
	float fresnel = saturate(pow(VdotN, _HorizonDistance));
	#if UNDERWATER_ENABLED
	fresnel *= vFace;
	#endif
	finalColor.rgb = lerp(finalColor.rgb, _HorizonColor.rgb, fresnel * _HorizonColor.a);

	#if UNITY_COLORSPACE_GAMMA
	//Gamma-space is likely a choice, enabling this will have the water stand out from non gamma-corrected shaders
	//finalColor.rgb = LinearToSRGB(finalColor.rgb);
	#endif
	
	//Final alpha
	float edgeFade = saturate(opaqueDist / (_EdgeFade * 0.01));

	#if UNDERWATER_ENABLED
	edgeFade = lerp(1.0, edgeFade, vFace);
	#endif

	#if _WAVES
	//Prevent from peering through waves when camera is at the water level
	//Note: only filters pixels above water surface, below is practically impossible
	if(wPos.y <= opaqueWorldPos.y) edgeFade = 1;
	#endif

	alpha *= edgeFade;

	//Not yet implemented, does nothing now
	SampleDiffuseProjectors(finalColor.rgb, wPos, ScreenPos);
	
	SurfaceData surfaceData = (SurfaceData)0;

	float density = 1;
	#if UNDERWATER_ENABLED
	//Match color gradient and alpha to fog for backfaces
	ApplyUnderwaterShading(finalColor.rgb, density, wPos, worldTangentNormal, viewDirNorm, _ShallowColor.rgb, _BaseColor.rgb, 1-vFace);
	#endif
	//return float4(density.rrr, 1.0);

	alpha = lerp(density, alpha, vFace);
	
	surfaceData.albedo = finalColor.rgb;
	surfaceData.specular = sunSpec.rgb;
	//surfaceData.metallic = lerp(0.0, _Metallic, 1-(intersection+foam));
	surfaceData.metallic = 0;
	//surfaceData.smoothness = _Smoothness;
	surfaceData.smoothness = 0;
	surfaceData.normalTS = NormalsCombined;
	surfaceData.emission = 0;
	surfaceData.occlusion = 1;
	surfaceData.alpha = alpha;

	InputData inputData;
	inputData.positionWS = wPos;
	inputData.viewDirectionWS = viewDirNorm;
	inputData.shadowCoord = ShadowCoords;
	//Flatten normals for underwater lighting (distracting, peers through the fog)
	#if UNDERWATER_ENABLED
	inputData.normalWS = lerp(float3(0,1,0), worldTangentNormal, vFace);
	#else
	inputData.normalWS = worldTangentNormal;
	#endif
	inputData.fogCoord = input.fogFactorAndVertexLight.x;
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
	inputData.bakedGI = SAMPLE_GI(input.lightmapUVOrVertexSH.xy, input.lightmapUVOrVertexSH.xyz, inputData.normalWS);

	float4 color = float4(ApplyLighting(surfaceData, inputData, translucencyData, density, _ShadowStrength, vFace), alpha);
	
	#if defined(DEBUG_DISPLAY)
	surfaceData.emission = translucencyData.mask;
	inputData.positionCS = input.positionCS;
	#if _NORMALMAP
	inputData.tangentToWorld = half3x3(WorldTangent, WorldBiTangent, waveNormal);
	#else
	inputData.tangentToWorld = 0;
	#endif
	inputData.shadowMask = TransformWorldToShadowCoord(wPos.xyz);
	inputData.normalizedScreenSpaceUV = ScreenPos.xy / ScreenPos.w;
	inputData.dynamicLightmapUV = input.lightmapUVOrVertexSH.xy;
	inputData.staticLightmapUV = input.lightmapUVOrVertexSH.xy;
	inputData.vertexSH = input.lightmapUVOrVertexSH.xyz;

	inputData.brdfDiffuse = surfaceData.albedo;
	inputData.brdfSpecular = surfaceData.specular;
	inputData.uv = uv;
	inputData.mipCount = 0;
	inputData.texelSize = float4(1/uv.x, 1/uv.y, uv.x, uv.y);
	inputData.mipInfo = 0;
	half4 debugColor;

	if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
	{
		return debugColor;
	}
	#endif
	
	#if _REFRACTION
		float3 sceneColor = SampleSceneColor(refractedScreenPos.xy / refractedScreenPos.w).rgb;
		
#if _ADVANCED_SHADING //Chromatic
		sceneColor.r = SampleSceneColor(refractedScreenPos.xy / refractedScreenPos.w + float2((_ScreenParams.z - 1.0), 0)).r;
		sceneColor.b = SampleSceneColor(refractedScreenPos.xy / refractedScreenPos.w - float2((_ScreenParams.z - 1.0), 0)).b;
#endif

		#if UNDERWATER_ENABLED
		//Skybox reflection probe can serve as reflection for pixels matching the skybox
			#ifndef _ENVIRONMENTREFLECTIONS_OFF
			float3 underwaterReflections = SampleUnderwaterReflections(reflectionVector, 0.0, wPos, inputData.normalWS, viewDirNorm, 0.0);

			#if _ADVANCED_SHADING && !_DISABLE_DEPTH_TEX
			//Use depth resampled with refracted screen UV
			depth.raw = depthRefracted.raw;
			#endif
	
			float skyMask = (Linear01Depth(depth.raw, _ZBufferParams) > 0.99 ? 1 : 0);

			sceneColor = lerp(sceneColor, underwaterReflections, skyMask);
			#endif
		#endif
	
		color.rgb = lerp(sceneColor, color.rgb, alpha);
		alpha = lerp(1.0, edgeFade, vFace);
	#endif

	color.a = alpha * saturate(alpha - vertexColor.g);
	ApplyFog(color.rgb, input.fogFactorAndVertexLight.x, ScreenPos, wPos, vFace);
	
	return color;
}