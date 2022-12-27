using UnityEngine;
using UnityEngine.Rendering;

/*
    The main purpose of the RP asset is to give Unity a way 
    to get a hold of a pipeline object instance that is responsible for rendering.
*/
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
	bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    /// Get pipeline object instance.
    protected override RenderPipeline CreatePipeline () {
		return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher);
	}
}
