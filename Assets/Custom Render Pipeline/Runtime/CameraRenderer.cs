/*
 * @Author: Qkyo
 * @Date: 2022-12-22 16:13:47
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-10 18:04:05
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\Runtime\CameraRenderer.cs
 * @Description: Render camera view, for released apps.
 */

using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    /// ScriptableRenderContext is a class that acts as an interface 
    /// between the custom C# code in the render pipeline 
    /// and Unity’s low-level graphics code.
	ScriptableRenderContext context;

    Camera camera;
    CullingResults cullingResults;

    /// Some tasks, like drawing the skybox, can be issued via a dedicated method, 
    /// but other commands have to be issued indirectly, via a separate command buffer.
    /// We need such a buffer to draw the other geometry in the scene.
	const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer {
		name = bufferName                        // Default buffer name
	};

    /// Indicate which kind of shader passes are allowed.
    static ShaderTagId 
        // The default ShaderTagId is SRPDefaultUnlit if we don't specified the Lightmode in pass
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),      
        litShaderTagId = new ShaderTagId("CustomLit");
    
    Lighting lighting = new Lighting();

    /// <summary>
    ///  Draw all geometry that the camera can see.
    /// </summary>
	public void Render (ScriptableRenderContext context, Camera camera, 
                        bool useDynamicBatching, bool useGPUInstancing,
                        ShadowSettings shadowSettings) 
    {
		this.context = context;
		this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();    
	    
        if (!Cull(shadowSettings.maxDistance)) 
        {
			return;
		}

		buffer.BeginSample(SampleName);
		ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
		buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();       // In Unity Editor only.
        DrawGizmos();                   // In Unity Editor only.
		lighting.Cleanup();
        Submit();
	}

    void DrawVisibleGeometry (bool useDynamicBatching, bool useGPUInstancing) 
    {
        /// Draw sort: opaque -> skybox -> transparent
        // 1. Draw Opaque object
		var sortingSettings = new SortingSettings(camera)
        {
            // more-or-less drawn front-to-back, also consider the render queue and materials.
            criteria = SortingCriteria.CommonOpaque
        };
		var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing,
			perObjectData = PerObjectData.ReflectionProbes |
                            PerObjectData.Lightmaps | PerObjectData.ShadowMask | 
                            PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                            PerObjectData.LightProbeProxyVolume |
				            PerObjectData.OcclusionProbeProxyVolume
        };
        // Add Lit to the passes to be rendered
        drawingSettings.SetShaderPassName(1, litShaderTagId);   

		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

        // 2. Draw Opaque object
        // Only used to determine whether the skybox should be drawn at all.
		context.DrawSkybox(camera);     

        // 3. Draw Transparent object
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

    void Setup () 
    {
        /// Set up the view-projection matrix as well as some other properties.
		context.SetupCameraProperties(camera);      	
		CameraClearFlags flags = camera.clearFlags;
        /// From 1 to 4 they are Skybox, Color, Depth, and Nothing.
        buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? 
                    camera.backgroundColor.linear : Color.clear
		);
        // buffer.ClearRenderTarget(true, true, Color.clear);
        /// Inject profiler samples (Like debugger)
		buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void Submit () 
    {
		buffer.EndSample(SampleName);
        ExecuteBuffer();
        // The context delays the actual rendering until we submit it.
		context.Submit();               
	}

    void ExecuteBuffer ()
    {
        // Copies the commands from the buffer but doesn't clear it
		context.ExecuteCommandBuffer(buffer);   
		buffer.Clear();
    }

    bool Cull (float maxShadowDistance) {
        // Figuring out which objects can be culled. 
        // We need to culling those that fall outside of the view frustum of the camera.
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}
}
