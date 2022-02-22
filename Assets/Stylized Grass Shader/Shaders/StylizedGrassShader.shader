/* Configuration: VegetationStudio */

//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

Shader "Universal Render Pipeline/Nature/Stylized Grass"
{
	Properties
	{
		//Lighting
		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		//[Header(Shading)]
		[MainColor] _BaseColor("Color", Color) = (0.49, 0.89, 0.12, 1.0)
		_HueVariation("Hue Variation (Alpha = Intensity)", Color) = (1, 0.63, 0, 0.15)
		_ColorMapStrength("Colormap Strength", Range(0.0, 1.0)) = 0.0
		_ColorMapHeight("Colormap Height", Range(0.0, 1.0)) = 1.0
		_ScalemapInfluence("Scale influence", vector) = (0,1,0,0)
		_OcclusionStrength("Ambient Occlusion", Range(0.0, 1.0)) = 0.25
		_VertexDarkening("Random Darkening", Range(0, 1)) = 0.1
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.0
		_Translucency("Translucency (Direct)", Range(0.0, 1.0)) = 0.2
		_TranslucencyIndirect("Translucency (Indirect)", Range(0.0, 1.0)) = 0.0
		_TranslucencyFalloff("Translucency Falloff", Range(1.0, 8.0)) = 4.0
		_TranslucencyOffset("Translucency Offset", Range(0.0, 1.0)) = 0.0
		
		//TODO: Convert to individual parameters
		_NormalParams("Normals", Vector) = (0,1,0,0)
		//X: Spherify normals
		//Y: Flatten normals (lighting)
		//Z: Spherify vertex color mask
		//W: Flatten geometry normals

		_BumpMap("Normal Map", 2D) = "bump" {}
		_BendPushStrength("Push Strength", Range(0.0, 1.0)) = 1.0
		[MaterialEnum(PerVertex,0,PerObject,1)]_BendMode("Bend Mode", Float) = 0.0
		_BendFlattenStrength("Flatten Strength", Range(0.0, 1.0)) = 1.0
		_BendTint("Bending tint", Color) = (0.8, 0.8, 0.8, 1.0)
		_PerspectiveCorrection("Perspective Correction", Range(0.0, 1.0)) = 0.0

		//[Header(Wind)]
		_WindAmbientStrength("Ambient Strength", Range(0.0, 1.0)) = 0.2
		_WindSpeed("Speed", Range(0.0, 10.0)) = 3.0
		_WindDirection("Direction", vector) = (1,0,0,0)
		_WindVertexRand("Vertex randomization", Range(0.0, 1.0)) = 0.6
		_WindObjectRand("Object randomization", Range(0.0, 1.0)) = 0.5
		_WindRandStrength("Random per-object strength", Range(0.0, 1.0)) = 0.5
		_WindSwinging("Swinging", Range(0.0, 1.0)) = 0.15
		_WindGustStrength("Gusting strength", Range(0.0, 1.0)) = 0.2
		_WindGustFreq("Gusting frequency", Range(0.0, 10.0)) = 4
		[NoScaleOffset] _WindMap("Wind map", 2D) = "black" {}
		_WindGustTint("Gusting tint", Range(0.0, 1.0)) = 0.066

		//[Header(Rendering)]
		_FadeParams("Fade params (X=Start, Y=End, Z=Toggle", vector) = (50, 100, 0, 0)
		[MaterialEnum(Both,0,Front,1,Back,2)] _Cull("Render faces", Float) = 0
		[Toggle] _AlphaToCoverage("Alpha to coverage", Float) = 0.0
		[MaterialEnum(Unlit,0,Simple,1,Advanced,2)]_LightingMode("Lighting Mode", Float) = 2.0
		[Toggle] _Scalemap("Scale grass by scalemap", Float) = 0.0
		[Toggle] _Billboard("Billboard", Float) = 0.0
		[Toggle] _AngleFading("Angle Fading", Float) = 0.0
		//[Toggle] _DisableDecals("Disable decals", Float) = 0
		[ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
		// Editmode props
		[HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

		/* start CurvedWorld */
		//[CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)
		/* end CurvedWorld */

	}

	SubShader
	{
		Tags{
			"RenderType" = "Opaque"
			"Queue" = "AlphaTest"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
			"NatureRendererInstancing" = "True"
		}
		
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
		#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
		ENDHLSL

		// ------------------------------------------------------------------
		//  Forward pass. Shades all light in a single pass. GI + emission + Fog
		Pass
		{
			Name "ForwardLit"
			Tags{ "LightMode" = "UniversalForward" }

			AlphaToMask [_AlphaToCoverage]
			Blend One Zero, One Zero
			Cull [_Cull]
			ZWrite On

			HLSLPROGRAM
			#pragma target 3.5

			//In place for projects that use a custom RP or modified URP and require specific behaviour for vegetation
			#define VEGETATION_SHADER
            #pragma multi_compile_fog
			// -------------------------------------
			// Material Keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE //Note: Cannot use _fragment suffix, unity_LODFade conflicts with unity_RenderingLayer in cBuffer somehow
			#pragma shader_feature_local_vertex _SCALEMAP
			#pragma shader_feature_local_vertex _BILLBOARD
			#pragma shader_feature_local_fragment _ANGLE_FADING
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local _ _SIMPLE_LIGHTING _ADVANCED_LIGHTING
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			
			// -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS	
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

			#if !_SIMPLE_LIGHTING && !_ADVANCED_LIGHTING
			#define _UNLIT
			#endif
			
			// -------------------------------------
			// Unity defined keywords
			//#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			//VVV Stripped on older URP versions VVV

			//URP 10+
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK 
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION 
			
			//URP 12+
			//#pragma shader_feature_local_fragment _DISABLE_DECALS
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3 
            #pragma multi_compile_fragment _ _LIGHT_LAYERS 
			#pragma multi_compile_fragment _ _LIGHT_COOKIES 
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE //Deprecated?
			#pragma multi_compile _ _CLUSTERED_RENDERING 
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON //URP 12+
			#pragma multi_compile_fragment _ DEBUG_DISPLAY //URP 12+

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON //Hybrid Renderer v2

			
			
			//Constants
			#define _ALPHATEST_ON
			#define SHADERPASS_FORWARD
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

			#include "Libraries/Input.hlsl"
			#include "Libraries/Common.hlsl"
			#include "Libraries/Color.hlsl"
			#include "Libraries/Lighting.hlsl"

			/* start VegetationStudio */
			#include "Libraries/VS_InstancedIndirect.cginc"
			#pragma instancing_options assumeuniformscaling renderinglayer procedural:setup
			/* end VegetationStudio */

			/* start GPUInstancer */
//			/* include GPUInstancer */
//			#include "Assets/GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
//			#pragma instancing_options procedural:setupGPUI
			/* end GPUInstancer */

			/* start NatureRenderer */
//			/* include NatureRenderer */
//#include "Assets/ThirdParty/Visual Design Cafe/Nature Shaders/Common/Nodes/Integrations/Nature Renderer.cginc"
//			#pragma instancing_options assumeuniformscaling procedural:SetupNatureRenderer
			/* end NatureRenderer */

			#pragma vertex LitPassVertex
			#pragma fragment LightingPassFragment
			#include "LightingPass.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			Cull[_Cull]

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma target 3.5

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma shader_feature_local_vertex _SCALEMAP
			#pragma shader_feature_local_vertex _BILLBOARD
			#pragma shader_feature_local_fragment _ANGLE_FADING
			#define _ALPHATEST_ON

			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON
			
			#define SHADERPASS_SHADOWCASTER
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#include "Libraries/Input.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

			#include "Libraries/Common.hlsl"
			/* start VegetationStudio */
			#include "Libraries/VS_InstancedIndirect.cginc"
			#pragma instancing_options assumeuniformscaling renderinglayer procedural:setup
			/* end VegetationStudio */

			/* start GPUInstancer */
//			/* include GPUInstancer */
//			#include "Assets/GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
//			#pragma instancing_options procedural:setupGPUI
			/* end GPUInstancer */

			/* start NatureRenderer */
//			/* include NatureRenderer */
//#include "Assets/ThirdParty/Visual Design Cafe/Nature Shaders/Common/Nodes/Integrations/Nature Renderer.cginc"
//			#pragma instancing_options assumeuniformscaling procedural:SetupNatureRenderer
			/* end NatureRenderer */

			#include "ShadowPass.hlsl"

			//#endif
			ENDHLSL
		}
		
		Pass
        {
            Name "GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            //AlphaToMask [_AlphaToCoverage] //Not supported in deferred
			Blend One Zero, One Zero
			Cull [_Cull]
			ZWrite On

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma target 3.5

			//In place for projects that use a custom RP or modified URP and require specific behaviour for vegetation
			#define VEGETATION_SHADER

			// -------------------------------------
			// Material Keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma shader_feature_local_vertex _SCALEMAP
			#pragma shader_feature_local_vertex _BILLBOARD
			#pragma shader_feature_local_fragment _ANGLE_FADING
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
		
			//URP12+
			#pragma shader_feature_local_fragment _DISABLE_DECALS

			//Disable features
			#undef _ALPHAPREMULTIPLY_ON
			#undef _EMISSION
			#undef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#undef _OCCLUSIONMAP
			#undef _METALLICSPECGLOSSMAP

			// -------------------------------------
			// Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			//Note: has to be on a new line in older versions
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

			//URP 10+
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

			// -------------------------------------
			// Unity defined keywords
			//#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog
			//Accurate G-buffer normals option in renderer settings
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			//URP12+
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON //Realtime GI
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3 //Decal support
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED //Stencil support
			
			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			//Constants
			#define _ALPHATEST_ON
			#define SHADERPASS_DEFERRED

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

			#include "Libraries/Input.hlsl"
			#include "Libraries/Common.hlsl"
			#include "Libraries/Color.hlsl"
			#include "Libraries/Lighting.hlsl"

			/* start VegetationStudio */
			#include "Libraries/VS_InstancedIndirect.cginc"
			#pragma instancing_options assumeuniformscaling renderinglayer procedural:setup
			/* end VegetationStudio */

			/* start GPUInstancer */
//			/* include GPUInstancer */
//			#include "Assets/GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
//			#pragma instancing_options procedural:setupGPUI
			/* end GPUInstancer */

			/* start NatureRenderer */
//			/* include NatureRenderer */
//#include "Assets/ThirdParty/Visual Design Cafe/Nature Shaders/Common/Nodes/Integrations/Nature Renderer.cginc"
//			#pragma instancing_options assumeuniformscaling procedural:SetupNatureRenderer
			/* end NatureRenderer */

			#pragma vertex LitPassVertex
			#pragma fragment LightingPassFragment
			
			#include "LightingPass.hlsl"

			ENDHLSL
        }

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma target 3.5

			#define SHADERPASS_DEPTHONLY
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			// -------------------------------------
			// Material Keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma shader_feature_local_vertex _SCALEMAP
			#pragma shader_feature_local_vertex _BILLBOARD
			#pragma shader_feature_local_fragment _ANGLE_FADING
			#define _ALPHATEST_ON

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Libraries/Input.hlsl"

			/* start CurvedWorld */
			//#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
			//#define CURVEDWORLD_BEND_ID_1
			//#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
			//#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			//#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
			/* end CurvedWorld */

			#include "Libraries/Common.hlsl"
			/* start VegetationStudio */
			#include "Libraries/VS_InstancedIndirect.cginc"
			#pragma instancing_options assumeuniformscaling renderinglayer procedural:setup
			/* end VegetationStudio */

			/* start GPUInstancer */
//			/* include GPUInstancer */
//			#include "Assets/GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
//			#pragma instancing_options procedural:setupGPUI
			/* end GPUInstancer */

			/* start NatureRenderer */
//			/* include NatureRenderer */
//#include "Assets/ThirdParty/Visual Design Cafe/Nature Shaders/Common/Nodes/Integrations/Nature Renderer.cginc"
//			#pragma instancing_options assumeuniformscaling procedural:SetupNatureRenderer
			/* end NatureRenderer */

			#include "DepthPass.hlsl"
			ENDHLSL
		}

		// This pass is used when drawing to a _CameraNormalsTexture texture
		Pass
		{
			Name "DepthNormals"
			Tags{"LightMode" = "DepthNormals"}

			ZWrite On
			Cull[_Cull]

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma target 3.5

			#pragma vertex DepthOnlyVertex
			#define SHADERPASS_DEPTH_ONLY
			#define SHADERPASS_DEPTHNORMALS
			//Only URP 10.0.0+ this amounts to the actual fragment shader, otherwise a dummy is used
			#pragma fragment DepthNormalsFragment

			// -------------------------------------
			// Material Keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma shader_feature_local_vertex _SCALEMAP
			#pragma shader_feature_local_vertex _BILLBOARD
			#pragma shader_feature_local_fragment _ANGLE_FADING

			#define _ALPHATEST_ON

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Libraries/Input.hlsl"

			/* start CurvedWorld */
            //#define CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
            //#define CURVEDWORLD_BEND_ID_1
            //#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
            //#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
            //#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
            /* end CurvedWorld */
            			
			#include "Libraries/Common.hlsl"

			//* start VegetationStudio */
			#include "Libraries/VS_InstancedIndirect.cginc"
			#pragma instancing_options assumeuniformscaling renderinglayer procedural:setup
			/* end VegetationStudio */

			/* start GPUInstancer */
//			/* include GPUInstancer */
//			#include "GPUInstancer/Shaders/Include/GPUInstancerInclude.cginc"
//			#pragma instancing_options procedural:setupGPUI
			/* end GPUInstancer */

			/* start NatureRenderer */
//			/* include NatureRenderer */
//#include "Assets/ThirdParty/Visual Design Cafe/Nature Shaders/Common/Nodes/Integrations/Nature Renderer.cginc"
//			#pragma instancing_options assumeuniformscaling procedural:SetupNatureRenderer
			/* end NatureRenderer */

			#include "DepthPass.hlsl"
			ENDHLSL
		}

		// Used for Baking GI. This pass is stripped from build.
		//Disabled, breaks SRP batcher, shader doesnt have the exact same properties as the Lit shader
		//UsePass "Universal Render Pipeline/Lit/Meta"

	}//Subshader

	FallBack "Hidden/Universal Render Pipeline/FallbackError"
	CustomEditor "StylizedGrass.MaterialUI"

}//Shader
