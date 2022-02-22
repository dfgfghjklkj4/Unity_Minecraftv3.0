//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

//Single channel overlay
float BlendOverlay(float a, float b)
{
	return (b < 0.5) ? 2.0 * a * b : 1.0 - 2.0 * (1.0 - a) * (1.0 - b);
}

//RGB overlay
float3 BlendOverlay(float3 a, float3 b)
{
	float3 color;
	color.r = BlendOverlay(a.r, b.r);
	color.g = BlendOverlay(a.g, b.g);
	color.b = BlendOverlay(a.b, b.b);
	return color;
}


float4 SampleColorMapTexture(in float3 wPos) 
{
	float2 uv = GetColorMapUV(wPos);

	return SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, uv).rgba;
}

//---------------------------------------------------------------//

//Shading (RGB=hue - A=brightness)
float4 ApplyVertexColor(in float4 vertexPos, in float3 wPos, in float3 baseColor, in float mask, in float aoAmount, in float darkening, in float4 hue, in float posOffset)
{
	float4 col = float4(baseColor, 1);

	//Apply hue
	col.rgb = lerp(col.rgb, hue.rgb, posOffset * hue.a);
	//Apply darkening
	float rand = frac(vertexPos.r * 4.0);

	float vertexDarkening = lerp(col.a, col.a * rand, darkening * mask); //Only apply to top vertices
	//Apply ambient occlusion
	float ambientOcclusion = lerp(col.a, col.a * mask, aoAmount);

	col.rgb *= vertexDarkening * ambientOcclusion;

	//Pass vertex color alpha-channel to fragment stage. Used in some shading functions such as translucency
	col.a = mask;

	return col;
}

float3 ApplyAmbientOcclusion(in float3 color, in float mask, in float amount) {
	return lerp(color, color * mask, amount);
}

float3 ApplyDarkening(in float3 vertexPos, in float3 color, in float amount)
{
	float rand = frac(vertexPos.r * 4.0);

	return lerp(color, color * rand, amount);
}

float3 ApplyColorMap(float3 wPos, float3 iColor, float s) 
{
	return lerp(iColor, SampleColorMapTexture(wPos).rgb, s);
}

//Apply object and vertex hue colors
float3 ApplyHue(in float4 iColor, in float3 oColor)
{
	return lerp(oColor, iColor.rgb, ObjectPosRand01() * iColor.a);
}

void ApplyObjectHueVariation(in float4 hue, in float3 color, out float3 output) {
	output = lerp(color.rgb, hue.rgb, hue.a * ObjectPosRand01());
}