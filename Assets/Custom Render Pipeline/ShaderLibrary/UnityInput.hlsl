/// We have to add these matrices to our shader, 
/// but because they're always the same we'll put the standard input 
/// provided by Unity in a separate HLSL file, 
/// both to keep code structured and to be able to include the code in other shaders

#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED
   
// Include all transformation group for SRP Batcher being compatible.
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;

    // The unwrap is scaled and positioned per object in the light map 
    // so each instance gets its own space.
    // Scale and translation are applied to lightmap UV.
	float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;

    // For dynamic object shadow mask (light probes)
	float4 unity_ProbesOcclusion;
	float4 unity_SpecCube0_HDR;

    // The components of the polynomial for red, green, and blue light
    // which are used by Light Probe.
    float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;

    // For Light Probe Proxy Volume used.
    float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

// View-projection matrix: World Space to Clip Space.
float4x4 unity_MatrixVP;              
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 unity_PrevObjectToWorld;
float4x4 unity_PrevWorldToObject;

float3 _WorldSpaceCameraPos;

#endif