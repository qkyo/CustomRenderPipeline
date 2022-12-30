/// Define the buffer we need for saving light data in GPU,
/// and read lighting info from buffer.
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

/// Define buffer for single lighting Data
// CBUFFER_START(_CustomLight)
// float3 _DirectionalLightColor;
// float3 _DirectionalLightDirection;
// CBUFFER_END

/// Define buffer for multiple lighting Data
CBUFFER_START(_CustomLight)
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};


/// For single lighting Data
// Light GetDirectionalLight() {
// 	Light light;
// 	light.color = _DirectionalLightColor;
// 	light.direction = _DirectionalLightDirection;
// 	return light;
// }

DirectionalShadowData GetDirectionalShadowData (int lightIndex, ShadowData shadowData) 
{
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData) 
{
	Light light;
	
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	// Debug only. Make cascade transition borders easier to spot.
	// light.attenuation = shadowData.cascadeIndex * 0.25;

	return light;
}

int GetDirectionalLightCount () 
{
	return _DirectionalLightCount;
}


#endif