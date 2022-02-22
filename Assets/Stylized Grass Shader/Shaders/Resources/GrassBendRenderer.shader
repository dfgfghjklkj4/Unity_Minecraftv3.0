//I have been removed, but packages can't track deletions, so I've been overwritten with this line. This way updating from <1.0.6 doesn't throw an error

//Will be removed entirely in the future

Shader "Hidden/Nature/Grass BendRenderer"
{
	SubShader
		{
			Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }

			Pass
			{
				HLSLPROGRAM
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

				#pragma vertex vert
				#pragma fragment frag

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "../Libraries/Common.hlsl"

				struct Varyings
				{
					float4 positionCS : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				Varyings vert(Attributes input)
				{
					Varyings output = (Varyings)0;

					output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
					output.uv = input.uv;

					return output;
				}

				half4 frag() : SV_Target
				{
					return 0;
				}
				ENDHLSL
		}
	}
	FallBack "Hidden/InternalErrorShader"
}