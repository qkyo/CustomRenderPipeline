// THIS IS A META PASS
// The Meta Pass provides albedo and emission values in texture space. 
// These values are separate from those used in real-time rendering, 
// meaning that you can use the Meta Pass to control 
// how a GameObject looks from the point of view of the lighting baking system 
// without affecting its appearance at runtime. 

#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	float2 lightMapUV : TEXCOORD1;
};

struct Varyings {
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
};

Varyings MetaPassVertex (Attributes input) {
	Varyings output;
    input.positionOS.xy =
		input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;

    // Explicitly uses the Z coordinate
	input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
	output.positionCS = TransformWorldToHClip(input.positionOS);
	output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}

float4 MetaPassFragment (Varyings input) : SV_TARGET {
	InputConfig config = GetInputConfig(input.baseUV);
	float4 base = GetBase(config);
	Surface surface;
	ZERO_INITIALIZE(Surface, surface);
	surface.color = base.rgb;
	surface.metallic = GetMetallic(config);
	surface.smoothness = GetSmoothness(config);
	BRDF brdf = GetBRDF(surface);

	float4 meta = 0.0;
	if (unity_MetaFragmentControl.x) {
		// If the X flag is set then diffuse reflectivity is requested
		meta = float4(brdf.diffuse, 1.0);
		meta.rgb += brdf.specular * brdf.roughness * 0.5;
		meta.rgb = min(
			PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue
		);
	}
	else if (unity_MetaFragmentControl.y) {
		// If the Y flag is set then it is supposed to return the emitted light
		meta = float4(GetEmission(config), 1.0);
	}
	return meta;
}

#endif