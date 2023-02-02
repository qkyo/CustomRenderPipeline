/*
 * @Author: Qkyo
 * @Date: 2022-12-22 15:03:01
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-02-01 12:18:22
 * @FilePath: \CustomRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipeline.cs
 * @Description: CustomRenderPipeline
 */
 
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{	
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
	PostFXSettings postFXSettings;

	public CustomRenderPipeline (bool useDynamicBatching, 
                                 bool useGPUInstancing, 
                                 bool useSRPBatcher,
                                 bool useLightsPerObject,
                                 ShadowSettings shadowSettings,
                                 PostFXSettings postFXSettings) 
    {
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
		this.useLightsPerObject = useLightsPerObject;
		this.postFXSettings = postFXSettings;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        
        // Convert light's intensity to linear space.
		GraphicsSettings.lightsUseLinearIntensity = true;
        
        InitializeForEditor();
	}

    /// <summary>
    /// Each frame Unity invokes Render on the RP instance
    /// </summary>
    protected override void Render (ScriptableRenderContext context, Camera[] cameras) 
    {
        foreach (Camera camera in cameras) {
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSettings);
		}
    }

}
