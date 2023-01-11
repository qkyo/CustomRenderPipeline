/// This is an example of a simple macro that just defines an identifier.
/// If it exists then it means that our file has been included.
#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface {
	float3 normal;					// The normal from normal map
	float3 position;
	float3 interpolatedNormal;		// The original surface normal to bias shadow sampling
	float3 viewDirection;
	float depth;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float fresnelStrength;
	float dither;
	float occlusion;		// Small receded areas like gaps and holes are mostly shadowed by the rest of an object
							// Occlusion only applies to indirect environmental lighting. Direct light is unaffected
};

#endif