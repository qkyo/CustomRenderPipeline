Shader "Custom Render Pipeline/Unlit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0, 0.4412, 1.0, 1.0)

        // Blend Modes:  whether we replace anything that was drawn before 
        //               or combine with the previous result to produce a see-through effect.
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
        // Transparent rendering usually doesn't write to the depth buffer.
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

        // Alpha clipping Modes
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Pass
        {
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

            HLSLPROGRAM
            // Tell Unity to compile a different version of our shader 
            // based on whether the keyword is defined or not.
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing        
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
}
