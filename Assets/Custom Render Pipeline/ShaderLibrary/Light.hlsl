/*
	Define the buffer we need for saving light data in GPU,
	and read lighting info from buffer.
*/

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
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
};


/// For single lighting Data
// Light GetDirectionalLight() {
// 	Light light;
// 	light.color = _DirectionalLightColor;
// 	light.direction = _DirectionalLightDirection;
// 	return light;
// }

Light GetDirectionalLight(int index) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	return light;
}

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}

#endif