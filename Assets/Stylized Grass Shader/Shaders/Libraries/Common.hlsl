//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

float4 _ColorMapUV;
TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap);
float4 _ColorMap_TexelSize;

#if !defined(SHADERPASS_SHADOWCASTER ) || !defined(SHADERPASS_DEPTHONLY)
#define LIGHTING_PASS
#else
//Never any normal maps in depth/shadow passes
#undef _NORMALMAP
#endif

//Vertex color channels used as masks
#define AO_MASK input.color.r
#define BEND_MASK input.color.r

#if VERSION_GREATER_EQUAL(12,0)
#define bakedLightmapUV staticLightmapUV
#else
#define bakedLightmapUV lightmapUV
#endif

//Attributes shared per pass, varyings declared separately per pass
struct Attributes
{
	float4 positionOS   : POSITION;
	float4 color		: COLOR0;
#ifdef LIGHTING_PASS
	float3 normalOS     : NORMAL;
#endif 
#if defined(_NORMALMAP) || defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
	float4 tangentOS    : TANGENT;
	float4 uv           : TEXCOORD0;
	//XY: Basemap UV
	//ZW: Bumpmap UV
#else
	float2 uv           : TEXCOORD0;
#endif
	
	float2 bakedLightmapUV   : TEXCOORD1;
	float2 dynamicLightmapUV  : TEXCOORD2;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//7.1.8: true
//7.2.0: false
//7.3.1: false
//7.4.1: false
//8.0.1: true
//8.1.0: true
//9.0.1: true
#if VERSION_EQUAL(8,0) || VERSION_EQUAL(8,1) || VERSION_EQUAL(9,0)
#define FLIP_UV
#endif
#if VERSION_EQUAL(7,2) || VERSION_EQUAL(7,3) || VERSION_EQUAL(7,4) 
#undef FLIP_UV
#endif

#include "Bending.hlsl"
#include "Wind.hlsl"

//---------------------------------------------------------------//
//Utils

float3 CameraPositionWS(float3 wPos)
{
	return GetCameraPositionWS();

	/*
	//_WorldSpaceCameraPos doesn't have correct values during shadow and vertex passes, fixed in URP 8.1.0
	// Shadows will flicker if Distance Fading or Perspective Correction is used *shrug*
	//https://issuetracker.unity3d.com/issues/shadows-flicker-by-moving-the-camera-when-shader-is-using-worldspacecamerpos-and-terrain-has-draw-enabled-for-trees-and-details
	*/
}

#if VERSION_LOWER(9,0) //Not already available in earlier versions
// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
	if (unity_OrthoParams.w == 0)
	{
		// Perspective
		return GetCameraPositionWS() - positionWS;
	}
	else
	{
		// Orthographic
		float4x4 viewMat = GetWorldToViewMatrix();
		return -viewMat[2].xyz;
	}
}
#endif

float ObjectPosRand01() {
	return frac(UNITY_MATRIX_M[0][3] + UNITY_MATRIX_M[1][3] + UNITY_MATRIX_M[2][3]);
}

float3 GetPivotPos() {
	return float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3] + 0.25, UNITY_MATRIX_M[2][3]);
}

float DistanceFadeFactor(float3 wPos, float4 params)
{
	if (params.z == 0) return 0;

	float pixelDist = length(CameraPositionWS(wPos).xyz - wPos.xyz);

	//Distance based scalar
	float f = saturate((pixelDist - params.x) / params.y);

	//Invert
	if(params.w == 1) f = 1-f;

	return f;
}

void ApplyLODCrossfade(float2 clipPos)
{
#if LOD_FADE_CROSSFADE
	float hash = GenerateHashedRandomFloat(clipPos.xy);

	float sign = CopySign(hash, unity_LODFade.x);
	
	#if defined(SHADERPASS_SHADOWCASTER)
	//Uncertain what is happening here, shadow casting pass doesn't appear to set up the correct unity_LODFade values
	float f = lerp(hash, unity_LODFade.x - sign, sign);
	#else
	float f = unity_LODFade.x - sign;
	#endif

	clip(f);
#endif
}

float3 DeriveNormal(float3 positionWS)
{
	float3 dpx = ddx(positionWS);
	float3 dpy = ddy(positionWS) * _ProjectionParams.x;
	return normalize(cross(dpx, dpy));
}

float InterleavedNoise(float2 coords, float t)
{
	return t * (InterleavedGradientNoise(coords, 0) + t);
}

#define ANGLE_FADE_DITHER_SIZE 0.49

void AlphaClip(float alpha, float cutoff, float3 clipPos, float3 wPos, float4 fadeParams)
{
	float f = 1;

	f -= DistanceFadeFactor(wPos, fadeParams);

	#if defined(SHADERPASS_SHADOWCASTER)
	//Using clip-space position causes pixel swimming as the camera moves
	ApplyLODCrossfade(wPos.xz * 32);
	#else
	ApplyLODCrossfade(clipPos.xy * 4);
	#endif

	//Don't perform for cast shadows
	#if _ANGLE_FADING && (defined(SHADERPASS_FORWARD) || defined(SHADERPASS_DEFERRED))
	
	float NdotV = saturate(dot(DeriveNormal(wPos), SafeNormalize(GetCameraPositionWS() - wPos)));

	float dither = InterleavedNoise(clipPos.xy, NdotV);

	alpha *= lerp(dither, 1, NdotV);
	#endif

	clip((alpha * f) - cutoff);
}

//UV Utilities
float2 BoundsToWorldUV(in float3 wPos, in float4 b)
{
	return (wPos.xz * b.z) - (b.xy * b.z);
}

//Color map UV
float2 GetColorMapUV(in float3 wPos)
{
	return BoundsToWorldUV(wPos, _ColorMapUV);
}

float4 SampleColorMapTextureLOD(in float3 wPos)
{
	float2 uv = GetColorMapUV(wPos);

	return SAMPLE_TEXTURE2D_LOD(_ColorMap, sampler_ColorMap, uv, 0).rgba;
}

//---------------------------------------------------------------//
//Vertex transformation

struct VertexInputs
{
	float4 positionOS;
	float3 normalOS;
#if defined(_NORMALMAP) || defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
	float4 tangentOS;
#endif
};

VertexInputs GetVertexInputs(Attributes v, float flattenNormals)
{
	VertexInputs i = (VertexInputs)0;
	i.positionOS = v.positionOS;
	i.normalOS = lerp(v.normalOS, float3(0,1,0), flattenNormals);
#if defined(_NORMALMAP) || defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
	i.tangentOS = v.tangentOS;
#endif

	return i;
}

//Struct that holds both VertexPositionInputs and VertexNormalInputs
struct VertexOutput {
	//Positions
	float3 positionWS; // World space position
	float3 positionVS; // View space position
	float4 positionCS; // Homogeneous clip space position
	float4 positionNDC;// Homogeneous normalized device coordinates
	float3 viewDir;// Homogeneous normalized device coordinates

	//Normals
#if defined(_NORMALMAP) || defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON)
	real4 tangentWS;
#endif
	float3 normalWS;
};

//Physically correct, but doesn't look great
//#define RECALC_NORMALS

//Debug
//#define DEFAULT_VERTEX

//#define CAM_UP UNITY_MATRIX_IT_MV[1].xyz
#define CAM_UP unity_CameraToWorld._12_22_32

//Combination of GetVertexPositionInputs and GetVertexNormalInputs with bending
VertexOutput GetVertexOutput(VertexInputs input, float rand, WindSettings s, BendSettings b)
{
	VertexOutput data = (VertexOutput)0;

#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON) && !defined(DEFAULT_VERTEX)
#if defined(CURVEDWORLD_NORMAL_TRANSFORMATION_ON) && defined(LIGHTING_PASS)
	CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(input.positionOS, input.normalOS, input.tangentOS)
#else
	CURVEDWORLD_TRANSFORM_VERTEX(input.positionOS)
#endif
#endif

#if _BILLBOARD	
	//Local vector towards camera
	float3 camDir = normalize(input.positionOS.xyz - TransformWorldToObject(_WorldSpaceCameraPos.xyz));
	camDir.y = 0; //Cylindrical billboarding
	
	float3 forward = camDir;
	float3 right = normalize(cross(float3(0,1,0), forward));
	float3 up = cross(forward, right);

	float4x4 lookatMatrix = {
		right.x,            up.x,            forward.x,       0,
        right.y,            up.y,            forward.y,       0,
        right.z,            up.z,            forward.z,       0,
        0, 0, 0,  1
    };
	
	input.normalOS = normalize(mul(float4(input.normalOS , 0.0), lookatMatrix)).xyz;
	input.positionOS.xyz = mul((float4x4)lookatMatrix, input.positionOS.xyzw).xyz;	
#endif
	
	float3 wPos = TransformObjectToWorld(input.positionOS.xyz);

#if _SCALEMAP 
	float scaleMap = SampleColorMapTextureLOD(wPos).a;

	//Scale axes in object-space
	input.positionOS.x = lerp(input.positionOS.x, input.positionOS.x * scaleMap, _ScalemapInfluence.x);
	input.positionOS.y = lerp(input.positionOS.y, input.positionOS.y * scaleMap, _ScalemapInfluence.y);
	input.positionOS.z = lerp(input.positionOS.z, input.positionOS.z * scaleMap, _ScalemapInfluence.z);
	wPos = TransformObjectToWorld(input.positionOS.xyz);
#else
	float scaleMap = 1.0;
#endif

	#if !defined(DEFAULT_VERTEX)
	float3 worldPos = lerp(wPos, GetPivotPos(), b.mode);
	float4 windVec = GetWindOffset(input.positionOS.xyz, wPos, rand, s) * scaleMap; //Less wind on shorter grass
	float4 bendVec = GetBendOffset(worldPos, b);

	float3 offsets = lerp(windVec.xyz, bendVec.xyz, bendVec.a);

	//Perspective correction
	data.viewDir = normalize(CameraPositionWS(wPos).xyz - wPos);
	float NdotV = dot(float3(0, 1, 0), data.viewDir);

	//Avoid pushing grass straight underneath the camera in a falloff of 4 units (1.0/4.0)
	float dist = saturate(distance(wPos.xz, CameraPositionWS(wPos).xz) * 0.25);
	
	//Push grass away from camera position
	float2 pushVec = -data.viewDir.xz;

	//This looks better, however during the shadow casting pass, the projection matrix is that of the light
	//pushVec = mul((float3x3)unity_CameraToWorld, float3(0,1,0)).xz;
	
	float perspMask = b.mask * b.perspectiveCorrection * dist * NdotV;
	offsets.xz += pushVec.xy * perspMask;
	
	//Apply bend offset
	wPos.xz += offsets.xz;
	wPos.y -= offsets.y;
	#endif

	//Vertex positions in various coordinate spaces
	data.positionWS = wPos;
	data.positionVS = TransformWorldToView(data.positionWS);
	data.positionCS = TransformWorldToHClip(data.positionWS);                       
	
	float4 ndc = data.positionCS * 0.5f;
	data.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
	data.positionNDC.zw = data.positionCS.zw;

#if !defined(SHADERPASS_SHADOWCASTER) && !defined(SHADERPASS_DEPTHONLY) //Skip normal derivative during shadow and depth passes

#if _ADVANCED_LIGHTING && defined(RECALC_NORMALS)
	float3 oPos = TransformWorldToObject(wPos); //object-space position after displacement in world-space
	float3 bentNormals = lerp(input.normalOS, normalize(oPos - input.positionOS.xyz), abs(offsets.x + offsets.z) * 0.5); //weight is length of wind/bend vector
#else
	float3 bentNormals = input.normalOS;
#endif

	data.normalWS = TransformObjectToWorldNormal(bentNormals);
#ifdef _NORMALMAP
	data.tangentWS.xyz = TransformObjectToWorldDir(input.tangentOS.xyz);
	real sign = input.tangentOS.w * GetOddNegativeScale();
	data.tangentWS.w = sign;
#endif
#endif

	return data;
}