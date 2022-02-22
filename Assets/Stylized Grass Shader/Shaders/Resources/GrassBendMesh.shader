Shader "Hidden/Nature/Grass Bend Mesh"
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
		float3 normalOS : NORMAL;
		float4 tangentOS : TANGENT;
		float4 color : COLOR;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float3 positionWS : TEXCOORD0;
		float3 normalWS : TEXCOORD1;
		float4 color : TEXCOORD2;
		UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
	};

	Varyings vert(Attributes input)
	{
		Varyings output = (Varyings)0;

		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_TRANSFER_INSTANCE_ID(input, output);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

		input.positionOS *= _Params.w;

		output.positionWS = TransformObjectToWorld(input.positionOS);
		output.positionCS = TransformWorldToHClip(output.positionWS);
		output.normalWS = TransformObjectToWorldNormal(input.normalOS);
		output.color = input.color;

		return output;
	}

	half4 frag(Varyings input) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		//Bottom-facing normals
		float mask = -input.normalWS.y * _Params.x * input.color.r;

		float height = ((input.positionWS.y) + _Params.y);
		float2 dir = (input.normalWS.xz * _Params.z) * 0.5 + 0.5;

		return float4(dir.x, height, dir.y, mask);
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
			Cull Front

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
			Cull Front

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