/*
 * @Author: Qkyo
 * @Date: 2022-12-28 17:40:12
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-31 17:17:56
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
	bool useShadowMask;
	Vector4 atlasSizes;

    /// For sending to GPU
    static int 
		// Refer to the directional shadow atlas
		dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
		dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
		otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas"),
		otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices"),
		otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles"),
		cascadeCountId = Shader.PropertyToID("_CascadeCount"),
		cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
		cascadeDataId = Shader.PropertyToID("_CascadeData"),
		shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
		shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),
		shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");				// whether pancaking is active via a global shader property
		
		
	/// <summary>
	/// This is an array of sphere bounding box. 
	/// The shadow of object is rendered by corresponding shadow map, 
	/// if the object is in any bounding box.
	/// </summary>
	static Vector4[] 
		cascadeCullingSpheres = new Vector4[maxCascades],
		cascadeData = new Vector4[maxCascades],
		otherShadowTiles = new Vector4[maxShadowedOtherLightCount];

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
	static string[] otherFilterKeywords = {
		"_OTHER_PCF3",
		"_OTHER_PCF5",
		"_OTHER_PCF7",
	};

	const int 
		// How many shadowed directional lights there can be
		maxShadowedDirectionalLightCount = 4, 
		maxShadowedOtherLightCount = 16,
		maxCascades = 4;
	int ShadowedDirectionalLightCount, ShadowedOtherLightCount;

	static Matrix4x4[]
		// Each cascade requires its own transformation matrix
		dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades],
		otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
	
	/// <summary>
	/// Keep track of visible light that will get shadows
	/// and some more data per shadowed light later
	/// </summary>
	struct ShadowedDirectionalLight {
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float nearPlaneOffset;
	}

	/// <summary>
	/// To render the shadows of a spot light we need to know its visible light index, slope scale bias, and normal bias.
	/// </summary>
	struct ShadowedOtherLight {
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float normalBias;
		public bool isPoint;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights = 
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
		
	ShadowedOtherLight[] shadowedOtherLights =
		new ShadowedOtherLight[maxShadowedOtherLightCount];

	public void Setup (ScriptableRenderContext context, CullingResults cullingResults,
		               ShadowSettings settings) 
    {
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
        ShadowedDirectionalLightCount = ShadowedOtherLightCount = 0;
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
		if (ShadowedOtherLightCount > 0) {
			RenderOtherShadows();
		}
		else {
			buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
		}
		
		// Enable or distable the keyword at the end of Render.
		// We have to do this even if we end up not rendering any realtime shadows, 
		// because the shadow mask isn't realtime.
		buffer.BeginSample(bufferName);		
		SetKeywords(shadowMaskKeywords, useShadowMask ?
			QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 :
			-1
		);
		// Send the cascade count to the GPU after rendering the cascades.
		buffer.SetGlobalInt(cascadeCountId, ShadowedDirectionalLightCount > 0 ? settings.directional.cascadeCount : 0);
		// Used in last cascades (fade effect).
		float f = 1f - settings.directional.cascadeFade;
		// Make the transition smoother by linearly fading the cutting off shadows at the max distance
		buffer.SetGlobalVector(shadowDistanceFadeId, 
							   new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));
		buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);
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
		buffer.SetGlobalFloat(shadowPancakingId, 1f);
		buffer.BeginSample(bufferName);
		ExecuteBuffer();

		int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < ShadowedDirectionalLightCount; i++) {
			RenderDirectionalShadows(i, split, tileSize);
		}
		
		// Send the cascade sphere to the GPU after rendering the cascades.
		buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
		// Used for solving shadow acne, which is related to the size of world-space texel size.
		buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
		// Once all shadowed lights are rendered send the matrices to the GPU
		buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
		
		SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
		SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
		// Store the atlas size in its X component and texel size in its Y component for directional shadow.
		atlasSizes.x = atlasSize;
		atlasSizes.y = 1f / atlasSize;

		buffer.EndSample(bufferName);
		ExecuteBuffer();
    }

	void RenderOtherShadows () {
		int atlasSize = (int)settings.other.atlasSize;
		atlasSizes.z = atlasSize;
		atlasSizes.w = 1f / atlasSize;
		buffer.GetTemporaryRT(
			otherShadowAtlasId, atlasSize, atlasSize,
			32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
		);
		buffer.SetRenderTarget(
			otherShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.ClearRenderTarget(true, false, Color.clear);
		buffer.SetGlobalFloat(shadowPancakingId, 0f);
		buffer.BeginSample(bufferName);
		ExecuteBuffer();

		int tiles = ShadowedOtherLightCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;

		for (int i = 0; i < ShadowedOtherLightCount;) 
		{
			if (shadowedOtherLights[i].isPoint) {
				RenderPointShadows(i, split, tileSize);
				i += 6;
			}
			else{
				RenderSpotShadows(i, split, tileSize);
				i += 1;
			}
		}

		buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
		buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
		SetKeywords(otherFilterKeywords, (int)settings.other.filter - 1);
		
		buffer.EndSample(bufferName);
		ExecuteBuffer();
	}

	void RenderPointShadows (int index, int split, int tileSize) 
	{
		ShadowedOtherLight light = shadowedOtherLights[index];
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
		
		float texelSize = 2f / tileSize;
		float filterSize = texelSize * ((float)settings.other.filter + 1f);
		float bias = light.normalBias * filterSize * 1.4142136f;
		float tileScale = 1f / split;
		// it has to render six times instead of just once
		for (int i = 0; i < 6; i++) {
			// Solution of discontinuity between faces of a cube map because the orientation of the texture plane suddenly changes 90°.
			// Reduce these artifacts by increasing the field of view 
			float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
			// This method requires two extra arguments after the light index: a CubemapFace index and a bias
			cullingResults.ComputePointShadowMatricesAndCullingPrimitives(
				light.visibleLightIndex, (CubemapFace)i, fovBias,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData
			);
			viewMatrix.m11 = -viewMatrix.m11;
			viewMatrix.m12 = -viewMatrix.m12;
			viewMatrix.m13 = -viewMatrix.m13;
			
			/// How shadow-casting objects should be culled
			shadowSettings.splitData = splitData;
			int tileIndex = index + i;
			
			// Solution for the shadow acne of spot light
			Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
			SetOtherTileData(tileIndex, offset, tileScale, bias);
			
			/// Reserve and set conversion matrix
			otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
				projectionMatrix * viewMatrix, offset, tileScale
			);
			buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
			
			// Depth bias to solve shadow acne
			buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);

			ExecuteBuffer();
			context.DrawShadows(ref shadowSettings);
			buffer.SetGlobalDepthBias(0f, 0f);
		}
	}

	/**
	* @description: Doing the same as the RenderDirectionalShadows method with parameters, 
	*               except that it doesn't loop over multiple tiles, has no cascades, and no culling factor.
	* @return {*}
	*/
	void RenderSpotShadows (int index, int split, int tileSize) 
	{
		ShadowedOtherLight light = shadowedOtherLights[index];
		/// Specifies which set of shadow casters to draw, and how to draw them.
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

		cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(
			light.visibleLightIndex, out Matrix4x4 viewMatrix,
			out Matrix4x4 projectionMatrix, out ShadowSplitData splitData
		);

		/// How shadow-casting objects should be culled
		shadowSettings.splitData = splitData;
		
		// Solution for the shadow acne of spot light
		float texelSize = 2f / (tileSize * projectionMatrix.m00);
		float filterSize = texelSize * ((float)settings.other.filter + 1f);
		float bias = light.normalBias * filterSize * 1.4142136f;
		Vector2 offset = SetTileViewport(index, split, tileSize);
		float tileScale = 1f / split;
		SetOtherTileData(index, offset, tileScale, bias);

		/// Reserve and set conversion matrix
		otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
		buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
		
		// Depth bias to solve shadow acne
		buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
		ExecuteBuffer();
		context.DrawShadows(ref shadowSettings);
		buffer.SetGlobalDepthBias(0f, 0f);
	}
	
	/**
	* @description: For each shadowed directional light, render directional shadows
	* @return {*}
	*/

	/// <summary>
	/// For each shadowed directional light, render directional shadows
	/// </summary>
	/// <param name="tileSize"> how. </param>
	/// <returns> none </returns>
	void RenderDirectionalShadows (int index, int split, int tileSize) 
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
		/// Specifies which set of shadow casters to draw, and how to draw them.
		var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
		
		int cascadeCount = settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = settings.directional.CascadeRatios;
		
		float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

		float tileScale = 1f / split;
		
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
																tileScale);
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
	Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, float scale) {
		// Negate the Z dimension if a reversed Z buffer is used. 
		if (SystemInfo.usesReversedZBuffer) {
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}

		// Remap the coordinate from (0, 1) to (-1, 1) and apply scale and offset
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
		if (ShadowedOtherLightCount > 0) {
			buffer.ReleaseTemporaryRT(otherShadowAtlasId);
		}
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

	public Vector4 ReserveOtherShadows (Light light, int visibleLightIndex) {
		//  Immediately returns when the light doesn't have shadows.
		if (light.shadows == LightShadows.None || light.shadowStrength <= 0f) {
			return new Vector4(0f, 0f, 0f, -1f);
		}

		float maskChannel = -1f;
		LightBakingOutput lightBaking = light.bakingOutput;
		if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
			lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask) 
		{
			useShadowMask = true;
			maskChannel = lightBaking.occlusionMaskChannel;
		}

		bool isPoint = light.type == LightType.Point;
		// Point light will take up six tiles in the shadow atlas.
		int newLightCount = ShadowedOtherLightCount + (isPoint ? 6 : 1);

		// Check whether increasing the light count would go over the max
		// or if there are no shadows to render for this light.
		if (newLightCount > maxShadowedOtherLightCount || 
			!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) 
		{
			// If so return with a negative shadow strength and the mask channel, so baked shadows are used when appropriate
			return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
		}

		shadowedOtherLights[ShadowedOtherLightCount] = new ShadowedOtherLight {
			visibleLightIndex = visibleLightIndex,
			slopeScaleBias = light.shadowBias,
			normalBias = light.shadowNormalBias,
			isPoint = isPoint
		};

		// Always returns the shadow strength and channel.
		Vector4 data = new Vector4(
			light.shadowStrength, ShadowedOtherLightCount,
			isPoint ? 1f : 0f, maskChannel
		);
		ShadowedOtherLightCount = newLightCount;
		return data;
	}

	/// <summary>
	/// Put the bias in the last component of a vector
	/// </summary>
	void SetOtherTileData (int index, Vector2 offset, float scale, float bias) {
		float border = atlasSizes.w * 0.5f;
		Vector4 data = Vector4.zero;
		data.w = bias;
		// Clamped Sampling, 
		data.x = offset.x * scale + border;
		data.y = offset.y * scale + border;
		data.z = scale - border - border;
		otherShadowTiles[index] = data;
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