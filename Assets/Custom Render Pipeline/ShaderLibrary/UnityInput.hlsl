/// We have to add these matrices to our shader, 
/// but because they're always the same we'll put the standard input 
/// provided by Unity in a separate HLSL file, 
/// both to keep code structured and to be able to include the code in other shaders

#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED
   
// Include all transformation group for SRP Batcher being compatible.
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
real4 unity_WorldTransformParams;
CBUFFER_END

// View-projection matrix: World Space to Clip Space.
float4x4 unity_MatrixVP;              
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 unity_PrevObjectToWorld;
float4x4 unity_PrevWorldToObject;

float3 _WorldSpaceCameraPos;

#endif