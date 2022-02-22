Shader "Hidden/Nature/Grass Bend Trail"
{
	Properties
	{
		_Params("Parameters", vector) = (1,0,0,0)
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "../Libraries/Bending.hlsl"

	CBUFFER_START(UnityPerMaterial)
		float4 _Params;
	CBUFFER_END

	struct Attributes
	{
		float3 positionOS : POSITION;
		float4 uv : TEXCOORD0;
		float3 normalOS : NORMAL;
		float4 tangentOS : TANGENT;
		float4 color : COLOR;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float3 uv : TEXCOORD0;
		float3 positionWS : TEXCOORD1;
		float4 tangentWS : TEXCOORD2;
		float3 bitangentWS : TEXCOORD3;
		float4 color : TEXCOORD4;
		UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
	};

	Varyings vert(Attributes input)
	{
		Varyings output = (Varyings)0;

		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_TRANSFER_INSTANCE_ID(input, output);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

		output.uv.xy = input.uv.xy;
		output.color = input.color;
		output.positionWS = TransformObjectToWorld(input.positionOS);
		output.positionCS = TransformWorldToHClip(output.positionWS);
		float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
		output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
		output.bitangentWS = -TransformWorldToViewDir(cross(normalWS, output.tangentWS.xyz) * input.tangentOS.w);

		return output;
	}

	half4 frag(Varyings input) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float mask = CreateTrailMask(input.uv.xy, input.color.r);

		float2 sideDir = lerp(input.bitangentWS.xy, -input.bitangentWS.xy, input.uv.y);
		float2 forwardDir = -input.tangentWS.xz;

		//Bounce back
		//sideDir = lerp(sideDir, -sideDir, sin(input.uv.x * 16));

		float dirMask = CreateDirMask(input.uv.xy);
		float2 sumDir = lerp(sideDir, forwardDir, dirMask);

		//Remap from -1.1 to 0.1
		sumDir = (sumDir * _Params.z) * 0.5 + 0.5;

		float height = (input.positionWS.y) + _Params.y;

		float4 color = float4(sumDir.x, height, sumDir.y, mask * _Params.x);

		return color;
	}
	ENDHLSL

	SubShader
	{
		Pass
		{
			Tags { "RenderType" = "GrassBender" "RenderPipeline" = "UniversalPipeline" }

			Blend Off
			ZWrite Off
			ZTest LEqual
			Cull Back

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma multi_compile_instancing
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
		Pass
		{
			Tags { "RenderType" = "GrassBender" "RenderPipeline" = "UniversalPipeline" }

			Blend SrcAlpha  OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Back

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma multi_compile_instancing
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}
	FallBack "Hidden/InternalErrorShader"
}