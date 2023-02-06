/*
 * @Author: Qkyo
 * @Date: 2022-12-22 14:55:27
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-02-06 17:14:12
 * @FilePath: \CustomRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipelineAsset.cs
 * @Description:  The main purpose of the RP asset is to give Unity a way 
 *                to get a hold of a pipeline object instance that is responsible for rendering.
 */
 
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
	bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true;
	
    [SerializeField]
	ShadowSettings shadows = default;

    [SerializeField]
	PostFXSettings postFXSettings = default;

    [SerializeField]
	bool allowHDR = true;

    // Bake color grading into a lookup table
    // The LUT is a 3D texture, typically 32×32×32. 
    // Filling that texture and sampling it later is much less work than performing color grading directly on the entire image.
    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }

	[SerializeField]
	ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    /// Get pipeline object instance.
    protected override RenderPipeline CreatePipeline () {
		return new CustomRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution);
	}
}
