/*
 * @Author: Qkyo
 * @Date: 2022-12-28 16:54:15
 * @LastEditors: Qkyo
 * @LastEditTime: 2023-01-03 17:27:57
 * @FilePath: \QkyosRenderPipeline\Assets\Custom Render Pipeline\Runtime\ShadowSettings.cs
 * @Description: Configure parameter about shadow quality
 */
 
using UnityEngine;

[System.Serializable]
public class ShadowSettings {

	/// <summary>
    /// How far away we will render shadows
	/// </summary>
	[Min(0.001f)]
	public float maxDistance = 100f;

	[Range(0.001f, 1f)]
	public float distanceFade = 0.1f;
	
	/// <summary>
    /// Resolution, or how big our shadow map will be.
	/// </summary>
    public enum TextureSize {
		_256 = 256, _512 = 512, _1024 = 1024,
		_2048 = 2048, _4096 = 4096, _8192 = 8192
	}
	
	/// <summary>
	/// All directional light use the same filter.
	/// By default it is PCF2x2
	/// </summary>
	public enum FilterMode {
		PCF2x2, PCF3x3, PCF5x5, PCF7x7
	}

	/// <summary>
    /// Reserve shadow property related to Directional light into an Directional struct,<para />
    /// That way we automatically get an hierarchical configuration in the inspector.<para />
	/// </summary>
    [System.Serializable]
	public struct Directional {
        // Use a single texture to contain multiple shadow maps
		public TextureSize atlasSize;
		public FilterMode filter;

		[Range(1, 4)]
		public int cascadeCount;

		[Range(0f, 1f)]
		public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
		
		public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

		// Fade between cascade
		[Range(0.001f, 1f)]
		public float cascadeFade;
		public enum CascadeBlendMode {
			Hard, Soft, Dither
		}
	
		public CascadeBlendMode cascadeBlend;
	}

	public Directional directional = new Directional {
		atlasSize = TextureSize._1024,
		filter = FilterMode.PCF2x2,
		cascadeCount = 4,
		cascadeRatio1 = 0.1f,
		cascadeRatio2 = 0.25f,
		cascadeRatio3 = 0.5f,
		cascadeFade = 0.1f,
		cascadeBlend = Directional.CascadeBlendMode.Hard
	};

}