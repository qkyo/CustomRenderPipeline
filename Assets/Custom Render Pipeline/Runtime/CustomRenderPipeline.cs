using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{	
    CameraRenderer renderer = new CameraRenderer();

    /* Each frame Unity invokes Render on the RP instance */
    protected override void Render (ScriptableRenderContext context, Camera[] cameras) 
    {
        foreach (Camera camera in cameras) {
			renderer.Render(context, camera);
		}
    }


    
}
