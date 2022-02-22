//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//Global parameters
float4 _BendMapUV;
TEXTURE2D(_BendMap); SAMPLER(sampler_BendMap);
float4 _BendMap_TexelSize;

struct BendSettings
{
	uint mode;
	float mask;
	float pushStrength;
	float flattenStrength;
	float perspectiveCorrection;
};

BendSettings PopulateBendSettings(uint mode, float mask, float pushStrength, float flattenStrength, float perspCorrection)
{
	BendSettings s = (BendSettings)0;

	s.mode = mode;
	s.mask = mask;
	s.pushStrength = pushStrength;
	s.flattenStrength = flattenStrength;
	s.perspectiveCorrection = perspCorrection;

	return s;
}

//Bend map UV
float2 GetBendMapUV(in float3 wPos) {
	float2 uv = _BendMapUV.xy / _BendMapUV.z + (_BendMapUV.z / (_BendMapUV.z * _BendMapUV.z)) * wPos.xz;

#ifdef FLIP_UV
	uv.y = 1 - uv.y;
#endif

	return uv;			
}

//https://github.com/Unity-Technologies/Graphics/blob/4641000674e63d10e2f7693e919c78b611f9de27/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl#L170
float BoundsEdgeMask(float2 position)
{
	const float blendDistance = 2;
	//Negate and center
	position = -position + _BendMapUV.z;
	
	const float2 boundsMin = _BendMapUV.xy;
	const float2 boundsMax = _BendMapUV.xy + _BendMapUV.z;
	
	float2 weightDir = min(position - boundsMin, boundsMax - position) / blendDistance;
	
	return saturate(min(weightDir.x, weightDir.y));
}

//Texture sampling
float4 GetBendVector(float3 wPos) 
{
#if _DISABLE_BENDING
	return 0;
#else
	if (_BendMapUV.w == 0) return float4(0.5, wPos.y, 0.5, 0.0);

	float2 uv = GetBendMapUV(wPos);

	float4 v = SAMPLE_TEXTURE2D(_BendMap, sampler_BendMap, uv).rgba;

	v.xz = v.xz * 2.0 - 1.0;

	float edgeMask = BoundsEdgeMask(wPos.xz);
	
	return v * edgeMask;
#endif
}

float4 GetBendVectorLOD(float3 wPos) 
{
#if _DISABLE_BENDING
	return 0;
#else
	if (_BendMapUV.w == 0) return float4(0.5, wPos.y, 0.5, 0.0);

	float2 uv = GetBendMapUV(wPos);

	float4 v = SAMPLE_TEXTURE2D_LOD(_BendMap, sampler_BendMap, uv, 0).rgba;

	//Remap from 0.1 to -1.1
	v.xz = v.xz * 2.0 - 1.0;

	float edgeMask = BoundsEdgeMask(wPos.xz);

	return v * edgeMask;
#endif
}

float CreateDirMask(float2 uv) {
	float center = pow((uv.y * (1 - uv.y)) * 4, 4);

	return saturate(center);
}

//Creates a tube mask from the trail UV.y. Red vertex color represents lifetime strength
float CreateTrailMask(float2 uv, float lifetime)
{
	float center = saturate((uv.y * (1.0 - uv.y)) * 8.0);

	//Mask out the start of the trail, avoids grass instantly bending (assumes UV mode is set to "Stretch")
	float tip = saturate(uv.x * 16.0);

	return center * lifetime * tip;
}

float4 GetBendOffset(float3 wPos, BendSettings b)
{
#if _DISABLE_BENDING
	return 0;
#else

	float4 vec = GetBendVectorLOD(wPos);

	float4 offset = float4(wPos, vec.a);

	float grassHeight = wPos.y;
	float bendHeight = vec.y;
	float dist = grassHeight - bendHeight;

	//Note since 7.1.5 somehow this causes the grass to bend down after the bender reaches a certain height
	//dist = abs(dist); //If bender is below grass, dont bend up

	float weight = saturate(dist);

	offset.xz = vec.xz * b.mask * weight * b.pushStrength;
	offset.y = b.mask * (vec.a * 0.75) * weight * b.flattenStrength;
	
	//Pass the mask, so it can be used to lerp between wind and bend offset vectors
	offset.a = vec.a * weight;

	//Apply mask
	offset.xyz *= offset.a;

	return offset;
#endif
}