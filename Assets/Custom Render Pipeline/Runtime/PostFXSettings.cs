using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject 
{ 
    [SerializeField]
	Shader shader = default;

	[System.Serializable]
	public struct BloomSettings {

		[Range(0f, 16f)]
		public int maxIterations;

		[Min(1f)]
		public int downscaleLimit;
		
		[Min(0f)]
		public float intensity;

		[Min(0f)]
		public float threshold;

		[Range(0f, 1f)]
		public float thresholdKnee;

		public bool bicubicUpsampling;
		public bool fadeFireflies;

		public enum Mode { Additive, Scattering }

		public Mode mode;

		[Range(0.05f, 0.95f)]
		public float scatter;
	}

	
	[SerializeField]
	BloomSettings bloom = new BloomSettings {
		scatter = 0.7f
	};
	public BloomSettings Bloom => bloom;

	[System.Serializable]
	public struct ToneMappingSettings {
		// the Reinhard value is zero.
		public enum Mode { None = -1, ACES, Neutral, Reinhard }

		public Mode mode;
	}

	[SerializeField]
	ToneMappingSettings toneMapping = default;
	public ToneMappingSettings ToneMapping => toneMapping;

    // We need a material when rendering
    // We'll create it on demand and set to hide and not save in the project.
    
    [System.NonSerialized]
	Material material;
	public Material Material {
		get {
			if (material == null && shader != null) {
				material = new Material(shader);
				material.hideFlags = HideFlags.HideAndDontSave;
			}
			return material;
		}
	}
}