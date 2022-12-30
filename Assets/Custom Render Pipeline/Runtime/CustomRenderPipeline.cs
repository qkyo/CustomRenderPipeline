/*
 * @Author: Qkyo
 * @Date: 2022-12-22 15:03:01
 * @LastEditors: Qkyo
 * @LastEditTime: 2022-12-30 13:40:00
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipeline.cs
 * @Description: CustomRenderPipeline
 */
 
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{	
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;
    bool useDynamicBatching, useGPUInstancing;

	public CustomRenderPipeline (bool useDynamicBatching, 
                                 bool useGPUInstancing, 
                                 bool useSRPBatcher,
                                 ShadowSettings shadowSettings) 
    {
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        
        // Convert light's intensity to linear space.
		GraphicsSettings.lightsUseLinearIntensity = true;
	}

    /// <summary>
    /// Each frame Unity invokes Render on the RP instance
    /// </summary>
    protected override void Render (ScriptableRenderContext context, Camera[] cameras) 
    {
        foreach (Camera camera in cameras) {
			renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);
		}
    }

}
