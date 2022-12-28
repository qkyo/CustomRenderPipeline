/*
	Recieve lighting data from sun in scene 
	and pass lighting data to command buffer(GPU).
*/
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Lighting {

	const string bufferName = "Lighting";
	const int maxDirLightCount = 4;

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
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

	static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount];

	/// When culling Unity also figures out which lights affect the space visible to the camera. 
	/// We can rely on that information.
	CullingResults cullingResults;
	
	public void Setup (ScriptableRenderContext context, CullingResults cullingResults) 
    {
		this.cullingResults = cullingResults;

		buffer.BeginSample(bufferName);
		SetupLights();
		// SetupDirectionalLight();
		buffer.EndSample(bufferName);
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
	
	/// We set up our light relying on global sun
	// void SetupDirectionalLight () 
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
	}

	/// When culling Unity also figures out which lights affect the space visible to the camera. 
	/// We set up our light relying on culling result.
	void SetupLights()
	{
		// It's a struct that acts like an array, but provides a connection to a native memory buffer. 
		// It makes it possible to efficiently share data between managed C# code and the native Unity engine code.
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

		int dirLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++) 
		{
			VisibleLight visibleLight = visibleLights[i];
			
			if (visibleLight.lightType == LightType.Directional) {
				SetupDirectionalLight(dirLightCount++, ref visibleLight);
				if (dirLightCount >= maxDirLightCount){
					break;
				}
			}
		}
		
		buffer.SetGlobalInt(dirLightCountId, dirLightCount);
		buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
	}
}