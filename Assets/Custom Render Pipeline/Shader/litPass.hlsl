/*
	BRDF Lit
*/
#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

/// Textures have to be uploaded to GPU memory
TEXTURE2D(_BaseMap);
/// Define a sampler state for the texture, 
/// which controls how it should be sampled, considering its wrap and filter modes.
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	// Texture tiling(x, y) and offset(z, w)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)		
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	// Alpha clipping cutoff
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)			
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)	
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

/// We need to know the object index when we enable GPU Instancing with "UnityInstancing.hlsl",
/// in where it also assumes that our vertex function has a struct parameter.
/// So we define a struct here - just like a cbuffer,
/// and put in the object index attribute "UNITY_VERTEX_INPUT_INSTANCE_ID"
struct Attributes {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

/// Varyings contains the data can vary between fragments of the same triangle.
struct Varyings  {
	float4 positionCS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float3 normalWS : VAR_NORMAL;
	float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID      
};

Varyings LitPassVertex (Attributes input)
{
    Varyings output;
	// Extracts the index from the input and stores it in a global static variable that the other instancing macros rely on.
    UNITY_SETUP_INSTANCE_ID(input);                                     
	// Copy the index when it exists.          
	UNITY_TRANSFER_INSTANCE_ID(input, output);                        
	output.positionWS = TransformObjectToWorld(input.positionOS);  
	output.positionCS = TransformWorldToHClip(output.positionWS);

	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	output.baseUV = input.baseUV * baseST.xy + baseST.zw;

	output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    return output;
}

float4 LitPassFragment (Varyings input) : SV_TARGET 
{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	// Using UNITY_ACCESS_INSTANCED_PROP to access material property
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);   
	float4 base = baseMap * baseColor;		// Blend result

	// alpha clipping
	#if defined(_CLIPPING)
	clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));			
	#endif

	// Visualize normal length error caused by linear interpolation distortion
	// base.rgb = abs(length(input.normalWS) - 1.0) * 10.0;			

	// Smooth out the interpolation distortion 				
	// base.rgb = normalize(input.normalWS);		
	
	Surface surface;
	surface.normal = normalize(input.normalWS);		
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);								
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
	surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);

	#if defined(_PREMULTIPLY_ALPHA)
		BRDF brdf = GetBRDF(surface, true);
	#else
		BRDF brdf = GetBRDF(surface);
	#endif

	float3 color = GetLighting(surface, brdf);

	return float4(color, surface.alpha);
}

#endif