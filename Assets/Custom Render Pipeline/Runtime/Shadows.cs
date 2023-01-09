/*
 * @Author: Qkyo
 * @Date: 2022-12-28 17:40:12
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-09 17:02:17
 * @FilePath: \CustomRenderPipeline\Assets\Custom Render Pipeline\Runtime\Shadows.cs
 * @Description: Generate shadow map, sample shadow atlas to extract strength    
 */

using UnityEngine;
using UnityEngine.Rendering;

public class Shadows {

	const string bufferName = "Shadows";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	ScriptableRenderContext context;
	CullingResults cullingResults;
	ShadowSettings settings;

    /// For sending to GPU
    static int 
		// Refer to the directional shadow atlas
		dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
		dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
		cascadeCountId = Shader.PropertyToID("_CascadeCount"),
		cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
		cascadeDataId = Shader.PropertyToID("_CascadeData"),
		shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
		shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
		
		
	/// <summary>
	/// This is an array of sphere bounding box. 
	/// The shadow of object is rendered by corresponding shadow map, 
	/// if the object is in any bounding box.
	/// </summary>
	static Vector4[] 
		cascadeCullingSpheres = new Vector4[maxCascades],
		cascadeData = new Vector4[maxCascades];

	/// <summary>
	/// Create shader variants for the new filter modes.
	/// </summary>
	static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};

	static string[] cascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};

	static string[] shadowMaskKeywords = {
		"_SHADOW_MASK_ALWAYS",
		"_SHADOW_MASK_DISTANCE"
	};
	bool useShadowMask;

	const int 
		// How many shadowed directional lights there can be
		maxShadowedDirectionalLightCount = 4, 
		maxCascades = 4;
	int ShadowedDirectionalLightCount;

	static Matrix4x4[]
		// Each cascade requires its own transformation matrix
		dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
	
	/// <summary>
	/// Keep track of visible light that will get shadows
	/// and some more data per shadowed light later
	/// </summary>
	struct ShadowedDirectionalLight {
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float nearPlaneOffset;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights = 
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

	public void Setup (ScriptableRenderContext context, CullingResults cullingResults,
		               ShadowSettings settings) 
    {
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
        ShadowedDirectionalLightCount = 0;
		useShadowMask = false;
	}

    public void Render () 
    {
		if (ShadowedDirectionalLightCount > 0) {
			RenderDirectionalShadows();
		}
		else {
            // Not claiming a texture will lead to problems for WebGL 2.0,
            // So we get a 1×1 dummy texture when no shadows are needed.
			buffer.GetTemporaryRT(
				dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
			);
		}
		
		// Enable or distable the keyword at the end of Render.
		// We have to do this even if we end up not rendering any realtime shadows, 
		// because the shadow mask isn't realtime.
		buffer.BeginSample(bufferName);		
		SetKeywords(shadowMaskKeywords, useShadowMask ?
			QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 :
			-1
		);
		buffer.EndSample(bufferName);
		ExecuteBuffer();
	}

	void RenderDirectionalShadows () 
    {
		int atlasSize = (int)settings.directional.atlasSize;
        // Get a temporary render texture
		buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
			                  32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        // Instruct the GPU to render to this texture instead of the camera's target.
        buffer.SetRenderTarget(
			dirShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
        
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.BeginSample(bufferName);
		ExecuteBuffer();

		int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < ShadowedDirectionalLightCount; i++) {
			RenderDirectionalShadows(i, split, tileSize);
		}
		
		// Send the cascade count and spheres to the GPU after rendering the cascades.
		buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
		buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
		// Used for solving shadow acne, which is related to the size of world-space texel size.
		buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
		// Once all shadowed lights are rendered send the matrices to the GPU
		buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
		// Used in last cascades (fade effect).
		float f = 1f - settings.directional.cascadeFade;
		// Make the transition smoother by linearly fading the cutting off shadows at the max distance
		buffer.SetGlobalVector(shadowDistanceFadeId, 
							   new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
		buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
		
		SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
		SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
		// Store the atlas size in its X component and texel size in its Y component.
		buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
		buffer.EndSample(bufferName);
		ExecuteBuffer();
    }

	
	/**
	* @description: For each shadowed directional light, render directional shadows
	* @return {*}
	*/
	void RenderDirectionalShadows (int index, int split, int tileSize) 
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
		/// Specifies which set of shadow casters to draw, and how to draw them.
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
		
		int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;
		
		float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

		for (int i = 0; i < cascadeCount; i++) {
			cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
				light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData
			);
			// Assign the cascade's culling sphere to the sphere array
			// But we only need to do this for the first light, 
			// as the cascades of all lights are equivalent.
			if (index == 0) {
				SetCascadeData(i, splitData.cullingSphere, tileSize);
			}
			
			/// The value is a factor that modulates the radius of the previous cascade used to perform the culling.
			splitData.shadowCascadeBlendCullingFactor = cullingFactor;

			/// How shadow-casting objects should be culled
			shadowSettings.splitData = splitData;
			int tileIndex = tileOffset + i;

			/// Split our atlas so we can give each light its own tile to render to.
			/// We invoke this method inside below.
			// ? SetTileViewport(index, split, tileSize);
			
			/// Reserve and set conversion matrix
			dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, 
															SetTileViewport(tileIndex, split, tileSize),
															split);
			buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

			// Depth bias to solve shadow acne
			// ? buffer.SetGlobalDepthBias(0f, 3f);
			// ? buffer.SetGlobalDepthBias(500000f, 0f);
			buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
			ExecuteBuffer();
			context.DrawShadows(ref shadowSettings);
			buffer.SetGlobalDepthBias(0f, 0f);
		}
	}

    void SetCascadeData (int index, Vector4 cullingSphere, float tileSize) {
		float texelSize = 2f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)settings.directional.filter + 1f);

		// Avoid sampling outside of the cascade's culling sphere.
		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;


		// Texels are squares. 
		// In the worst case we end up having to offset along the square's diagonal, so let's scale it by √2.
		cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
	}
	
	Vector2 SetTileViewport (int index, int split, float tileSize) {
		Vector2 offset = new Vector2(index % split, index / split);
		buffer.SetViewport(new Rect(
			offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
		));
		return offset;
	}
	
	/// converts from world space to shadow tile space.
	Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split) {
		// Negate the Z dimension if a reversed Z buffer is used. 
		if (SystemInfo.usesReversedZBuffer) {
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}

		// Remap the coordinate from (0, 1) to (-1, 1) and apply scale and offset
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
		
		return m;
	}

	/// <summary>
    /// Release memory for temporary render texture
	/// </summary>
    public void Cleanup () {
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}

	void ExecuteBuffer () 
    {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
	
	/// <summary>
	/// Figure out which light will gets shadows and reserve it.
	/// </summary>
	public Vector4 ReserveDirectionalShadows (Light light, int visibleLightIndex) 
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
			light.shadows != LightShadows.None && light.shadowStrength > 0f )
        {
			float maskChannel = -1;
			// Determine whether the light uses the shadow mask
			LightBakingOutput lightBaking = light.bakingOutput;
			if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
				lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask) 
			{
				useShadowMask = true;
				maskChannel = lightBaking.occlusionMaskChannel;
			}

			// Check whether there aren't realtime shadow casters
			if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) 
			{
				return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
			}

			ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
				new ShadowedDirectionalLight {
					visibleLightIndex = visibleLightIndex,
					slopeScaleBias = light.shadowBias,
					nearPlaneOffset = light.shadowNearPlane
				};
			return new Vector4(light.shadowStrength, 
							   settings.directional.cascadeCount * ShadowedDirectionalLightCount++,
							   light.shadowNormalBias, maskChannel);
		}
		
		return new Vector4(0f, 0f, 0f, -1f);
    }

	/// <summary>
	/// Set PCF Filter keyword
	/// </summary>
	void SetKeywords (string[] keywords, int enabledIndex) 
	{
		for (int i = 0; i < keywords.Length; i++) {
			if (i == enabledIndex) {
				buffer.EnableShaderKeyword(keywords[i]);
			}
			else {
				buffer.DisableShaderKeyword(keywords[i]);
			}
		}
	}

}