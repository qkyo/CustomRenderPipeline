// THIS IS A META PASS
// The Meta Pass provides albedo and emission values in texture space. 
// These values are separate from those used in real-time rendering, 
// meaning that you can use the Meta Pass to control 
// how a GameObject looks from the point of view of the lighting baking system 
// without affecting its appearance at runtime. 

#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

/// Textures have to be uploaded to GPU memory
TEXTURE2D(_BaseMap);
/// Define a sampler state for the texture, 
/// which controls how it should be sampled, considering its wrap and filter modes.
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    // Texture tiling(x, y) and offset(z, w)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    // 	Alpha clipping cutoff
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// Using UNITY_ACCESS_INSTANCED_PROP to access material property
float2 TransformBaseUV (float2 baseUV) {
	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase (float2 baseUV) {
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	return map * color;
}

float GetCutoff (float2 baseUV) {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic (float2 baseUV) {
	return 0.0;
}

float GetSmoothness (float2 baseUV) {
	return 0.0;
}

float3 GetEmission (float2 baseUV) {
	return GetBase(baseUV).rgb;
}

float GetFresnel (float2 baseUV) {
	return 0.0;
}
#endif