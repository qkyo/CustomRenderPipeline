/*
 * @Author: Qkyo
 * @Date: 2022-12-22 14:55:27
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-30 11:44:08
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\Runtime\CustomRenderPipelineAsset.cs
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

    /// Get pipeline object instance.
    protected override RenderPipeline CreatePipeline () {
		return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings);
	}
}
