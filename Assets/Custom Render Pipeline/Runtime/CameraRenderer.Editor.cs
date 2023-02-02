/*
 * @Author: Qkyo
 * @Date: 2022-12-23 13:45:20
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-02-01 13:15:13
 * @FilePath: \CustomRenderPipeline\Assets\Custom Render Pipeline\Runtime\CameraRenderer.Editor.cs
 * @Description: Render camera view, render in editor only.
 */
 
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer
{
	partial void DrawGizmosBeforeFX ();
	partial void DrawGizmosAfterFX ();
    partial void DrawUnsupportedShaders ();
    partial void PrepareForSceneWindow ();
    partial void PrepareBuffer ();

    // #if UNITY_EDITOR || DEVELOPMENT_BUILD 
    #if UNITY_EDITOR

        /// Cover all Unity's default shaders, which are unsupported shaders.
        static ShaderTagId[] legacyShaderTagIds = {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        string SampleName { get; set; }

        static Material errorMaterial;

        partial void DrawGizmosBeforeFX () 
        {
            if (Handles.ShouldRenderGizmos()) {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
        }

        partial void DrawGizmosAfterFX () 
        {
            if (Handles.ShouldRenderGizmos()) {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
	    }

        partial void DrawUnsupportedShaders()
        {
            if (errorMaterial == null) {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var drawingSettings = new DrawingSettings (legacyShaderTagIds[0], new SortingSettings(camera))
            {
                overrideMaterial = errorMaterial
            };

            // Set multiple passes.
            for (int i = 1; i < legacyShaderTagIds.Length; i++) {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }

            var filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );
        }
        
        partial void PrepareForSceneWindow ()
        {
            if (camera.cameraType == CameraType.SceneView) 
            {
                // Explicitly add the UI to the world geometry when rendering for the scene window.
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
        }

        /// Multiple camera: For each camera has its own profiler samples scope in frame debug.
        partial void PrepareBuffer () {
		    Profiler.BeginSample("Editor Only");
            buffer.name = SampleName = camera.name;
		    Profiler.EndSample();
        }

    #else
        string SampleName => bufferName;
    #endif

}
