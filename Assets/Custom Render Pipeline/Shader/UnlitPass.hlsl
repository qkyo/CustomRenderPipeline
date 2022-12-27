/// This is an example of a simple macro that just defines an identifier.
/// If it exists then it means that our file has been included.
#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

/// Textures have to be uploaded to GPU memory
TEXTURE2D(_BaseMap);
/// Define a sampler state for the texture, 
/// which controls how it should be sampled, considering its wrap and filter modes.
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)		// Texture tiling(x, y) and offset(z, w)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)				// Alpha clipping cutoff
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

/// We need to know the object index when we enable GPU Instancing with "UnityInstancing.hlsl",
/// in where it also assumes that our vertex function has a struct parameter.
/// So we define a struct here - just like a cbuffer,
/// and put in the object index attribute "UNITY_VERTEX_INPUT_INSTANCE_ID"
struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

/// Varyings contains the data can vary between fragments of the same triangle.
struct Varyings  {
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID      
};

Varyings UnlitPassVertex (Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);                                     // Extracts the index from the input 
                                                                        // and stores it in a global static variable that the other instancing macros rely on.
	UNITY_TRANSFER_INSTANCE_ID(input, output);                          // Copy the index when it exists. 
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}


float4 UnlitPassFragment (Varyings input) : SV_TARGET 
{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);   // Using UNITY_ACCESS_INSTANCED_PROP to access material property
	float4 base = baseMap * baseColor;		// Blend result

	#if defined(_CLIPPING)
	clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));			// alpha clipping
	#endif
	
	return base;
}

#endif



//// Simpler version for only-support-SRP-Batcher
// #ifndef CUSTOM_UNLIT_PASS_SRP_BATCHING_INCLUDED
// #define CUSTOM_UNLIT_PASS_SRP_BATCHING_INCLUDED

// #include "../ShaderLibrary/Common.hlsl"

// /*
// /// Not supported on all platforms like OpenGL ES 2.0
// cbuffer UnityPerMaterial {
// 	float _BaseColor;
// };
// */

// /// Take the buffer name as an argument.
// CBUFFER_START(UnityPerMaterial)
// 	float4 _BaseColor;
// CBUFFER_END

// float4 UnlitPassVertex (float3 positionOS : POSITION) : SV_POSITION     // Parameters: object's position in object space
// {
//     float3 positionWS = TransformObjectToWorld(positionOS.xyz);
//     return TransformWorldToHClip(positionWS);
// }

// float4 UnlitPassFragment () : SV_TARGET 
// {
//     return _BaseColor;
// }

// #endif
