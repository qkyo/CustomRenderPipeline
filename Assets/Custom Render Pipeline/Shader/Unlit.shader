Shader "Custom Render Pipeline/Unlit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        [HDR] _BaseColor("Color", Color) = (0, 0.4412, 1.0, 1.0)

        // Blend Modes:  whether we replace anything that was drawn before 
        //               or combine with the previous result to produce a see-through effect.
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
        // Transparent rendering usually doesn't write to the depth buffer.
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1

        // Alpha clipping Modes
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        
        // Shadows
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
    }
    SubShader
    {
        HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "UnlitInput.hlsl"
		ENDHLSL
        
        Pass
        {
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

            // Enable GPU Instancing
            #pragma multi_compile_instancing        
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"
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
        
        Pass 
		{
			Tags {
				"LightMode" = "Meta"
			}

			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex MetaPassVertex
			#pragma fragment MetaPassFragment
			#include "MetaPass.hlsl"
			ENDHLSL
		}
    }
    // Use an instance of the CustomShaderGUI class to draw the inspector for materials that use the Unlit shader.
	CustomEditor "CustomShaderGUI"
}
