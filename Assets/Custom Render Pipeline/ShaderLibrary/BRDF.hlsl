/*
 * @Author: Qkyo
 * @Date: 2022-12-27 17:52:00
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-10 18:58:38
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\ShaderLibrary\BRDF.hlsl
 * @Description: How much light we end up seeing reflected off a surface, 
 * 			     which is a combination of diffuse reflection and specular reflection.
 */

#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
	float perceptualRoughness;				/// As roughness scatters specular reflection 
											/// it not only reduces its intensity but also muddles it, as if it's out of focus. 
											/// This effect gets approximated by Unity by storing blurred versions of the environment map in lower mip levels. 
											/// To access the correct mip level we need to know the perceptual roughness
	float fresnel;
};

/// Define that as the minimum reflectivity 
/// and add a OneMinusReflectivity function that adjusts the range from 0–1 to 0–0.96. 
float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

BRDF GetBRDF (Surface surface, bool applyAlphaToDiffuse = false) {
	// float oneMinusReflectivity = 1.0 - surface.metallic;
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	
	BRDF brdf;
	brdf.diffuse = surface.color * oneMinusReflectivity;
	if (applyAlphaToDiffuse) 
	{
		brdf.diffuse *= surface.alpha;
	}

	// brdf.specular = surface.color - brdf.diffuse;
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);
	// A variant Schlick's approximation for Fresnel.
	brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinusReflectivity);

	return brdf;
}

float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);		// Avoid a division by zero in case the vectors are opposed
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF (Surface surface, BRDF brdf, Light light) {
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 IndirectBRDF (Surface surface, BRDF brdf, float3 diffuse, float3 specular) 
{	
	float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
	float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
	reflection /= brdf.roughness * brdf.roughness + 1.0;
    return diffuse * brdf.diffuse + reflection;
}

#endif