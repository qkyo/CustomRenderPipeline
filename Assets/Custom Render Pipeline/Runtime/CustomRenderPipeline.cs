/*
 * @Author: Qkyo
 * @Date: 2022-12-22 15:03:01
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-30 11:42:59
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipeline.cs
 * @Description: CustomRenderPipeline
 */
 
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{	
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;

	public CustomRenderPipeline (bool useDynamicBatching, 
                                 bool useGPUInstancing, 
                                 bool useSRPBatcher,
                                 bool useLightsPerObject,
                                 ShadowSettings shadowSettings) 
    {
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
		this.useLightsPerObject = useLightsPerObject;
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
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings);
		}
    }

}
