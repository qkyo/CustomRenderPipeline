Shader "Custom Render Pipeline/Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        // Metallic workflow
        _Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5

        // Blend Modes:  whether we replace anything that was drawn before 
        //               or combine with the previous result to produce a see-through effect.
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0

        // Transparent rendering usually doesn't write to the depth buffer.
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

        // Alpha clipping Modes
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Shadows
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Tags {
                "LightMode" = "CustomLit"
            }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

            HLSLPROGRAM
            // Loops with a variable length used to be a problem for shaders 
            // using the OpenGL ES 2.0 and WebGL 1.0 graphics APIs.
            // So we turn off support in builds
			#pragma target 3.5

            // Tell Unity to compile a different version of our shader 
            // based on whether the keyword is defined or not.
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _RECEIVE_SHADOWS
            // underscore for the no-keyword option matching the 2×2 filter.
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            // underscore for the no-keyword option matching the CASCADE_BLEND_HARD mode.
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            // Enable GPU Instancing
            #pragma multi_compile_instancing        
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"
            ENDHLSL
        }

        Pass 
        {
			Tags {
				"LightMode" = "ShadowCaster"
			}

            // Only need to write depth, disable writing color data
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
    // Use an instance of the CustomShaderGUI class to draw the inspector for materials that use the Lit shader.
	CustomEditor "CustomShaderGUI"
}
