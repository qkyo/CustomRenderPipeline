/*
 * @Author: Qkyo
 * @Date: 2022-12-22 15:03:01
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-02-06 17:15:06
 * @FilePath: \CustomRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipeline.cs
 * @Description: CustomRenderPipeline
 */
 
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{	
    CameraRenderer renderer = new CameraRenderer();
    ShadowSettings shadowSettings;
    bool allowHDR, useDynamicBatching, useGPUInstancing, useLightsPerObject;
	PostFXSettings postFXSettings;
    int colorLUTResolution;

	public CustomRenderPipeline (bool allowHDR,
                                 bool useDynamicBatching, 
                                 bool useGPUInstancing, 
                                 bool useSRPBatcher,
                                 bool useLightsPerObject,
                                 ShadowSettings shadowSettings,
                                 PostFXSettings postFXSettings,
                                 int colorLUTResolution) 
    {
        this.allowHDR = allowHDR;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
		this.useLightsPerObject = useLightsPerObject;
		this.postFXSettings = postFXSettings;
		this.colorLUTResolution = colorLUTResolution;
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
			renderer.Render(context, camera, allowHDR, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSettings, colorLUTResolution);
		}
    }

}
