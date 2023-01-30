/*
 * @Author: Qkyo
 * @Date: 2022-12-27 14:06:09
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-30 12:05:43
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\Runtime\Lighting.cs
 * @Description: Recieve lighting data from sun in scene 
 * 		     	 and pass lighting data to command buffer(GPU).
 */

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Lighting {

	const string bufferName = "Lighting";
	const int maxDirLightCount = 4, maxOtherLightCount = 64;

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	/// For single light setting
    // static int
	// 	dirLightColorId = Shader.PropertyToID("_DirectionalLightColor"),
	// 	dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

	/// For multiple light setting
	static int
		dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
		// Vector 2 ( Shadow strength, Shadow tile offset )
		dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

	static int
		otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
		otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
		otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions"),
		otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections"),
		otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles"),
		otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
	static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount],
		dirLightShadowData = new Vector4[maxDirLightCount];

	static Vector4[]
		otherLightColors = new Vector4[maxOtherLightCount],
		otherLightPositions = new Vector4[maxOtherLightCount],
		otherLightDirections = new Vector4[maxOtherLightCount],
		otherLightSpotAngles = new Vector4[maxOtherLightCount],
		otherLightShadowData = new Vector4[maxOtherLightCount];

	static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

	/// When culling Unity also figures out which lights affect the space visible to the camera. 
	/// We can rely on that information.
	CullingResults cullingResults;

	Shadows shadows = new Shadows();
	
	public void Setup (ScriptableRenderContext context, CullingResults cullingResults,
					   ShadowSettings shadowSettings, bool useLightsPerObject) 
    {
		this.cullingResults = cullingResults;

		buffer.BeginSample(bufferName);
		shadows.Setup(context, cullingResults, shadowSettings);
		SetupLights(useLightsPerObject);
		shadows.Render();
		// SetupSingleDirectionalLight();
		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
	
	/// We set up our light relying on global sun
	// void SetupSingleDirectionalLight () 
    // {
	// 	Light light = RenderSettings.sun;

	// 	// The color is the light's color in linear space.
	// 	buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
	// 	// The direction is the light transformation's forward vector negated.
	// 	buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
    // }  

	/// We set up multiple lights relying on culling result.
	void SetupDirectionalLight (int index, ref VisibleLight visibleLight) {
		dirLightColors[index] = visibleLight.finalColor;
		dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}
	
	void SetupPointLight (int index, ref VisibleLight visibleLight) {
		otherLightColors[index] = visibleLight.finalColor;
		// The position works like the directional light's direction
		Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		// Store 1 / (light range^2)
		position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		otherLightPositions[index] = position;
		otherLightSpotAngles[index] = new Vector4(0f, 1f);
		Light light = visibleLight.light;
		otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
	}
	
	void SetupSpotLight (int index, ref VisibleLight visibleLight) {
		otherLightColors[index] = visibleLight.finalColor;
		Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		otherLightPositions[index] = position;
		otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

		Light light = visibleLight.light;
		float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
		float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
		float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
		otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
		otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
	}

	/// When culling Unity also figures out which lights affect the space visible to the camera. 
	/// We set up our light relying on culling result.
	void SetupLights(bool useLightsPerObject)
	{
		// It's a struct that acts like an array, but provides a connection to a native memory buffer. 
		// It makes it possible to efficiently share data between managed C# code and the native Unity engine code.

		// This list includes all lights regardless of their visibility and also contains directional lights. 
		// We have to sanitize these lists so only the indices of visible non-directional lights remain. 
		NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

		int dirLightCount = 0, otherLightCount = 0;
		int i;
		for (i = 0; i < visibleLights.Length; i++) 
		{
			int newIndex = -1;
			VisibleLight visibleLight = visibleLights[i];
			
			switch (visibleLight.lightType) {
				case LightType.Directional:
					if (dirLightCount < maxDirLightCount) {
						SetupDirectionalLight(dirLightCount++, ref visibleLight);
					}
					break;
				case LightType.Point:
					if (otherLightCount < maxOtherLightCount) {
						newIndex = otherLightCount;
						SetupPointLight(otherLightCount++, ref visibleLight);
					}
					break;
				case LightType.Spot:
					if (otherLightCount < maxOtherLightCount) {
						newIndex = otherLightCount;
						SetupSpotLight(otherLightCount++, ref visibleLight);
					}
					break;
			}
			if (useLightsPerObject) {
				indexMap[i] = newIndex;
			}
		}

		if (useLightsPerObject) {
			for (; i < indexMap.Length; i++) {
				indexMap[i] = -1;
			}
			cullingResults.SetLightIndexMap(indexMap);
			indexMap.Dispose();
			Shader.EnableKeyword(lightsPerObjectKeyword);
		}
		else {
			Shader.DisableKeyword(lightsPerObjectKeyword);
		}
		
		// Copy the array to the GPU 
		buffer.SetGlobalInt(dirLightCountId, dirLightCount);
		if (dirLightCount > 0) {
			buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
			buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
			buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
		}

		buffer.SetGlobalInt(otherLightCountId, otherLightCount);
		if (otherLightCount > 0) {
			buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
			buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
			buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
			buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);	
			buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
		}
	}

	public void Cleanup () 
	{
		shadows.Cleanup();
	}

}