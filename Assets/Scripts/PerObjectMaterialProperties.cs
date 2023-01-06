using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
	// if the parameter is a part of the UnityPerMaterial buffer it can be configured per instance.
	[SerializeField] Color baseColor = Color.white;
	[SerializeField, Range(0f, 1f)] float alphaCutoff = 0.5f, metallic = 0f, smoothness = 0.5f;
		[SerializeField, ColorUsage(false, true)] Color emissionColor = Color.black;

	static int 
		baseColorId = Shader.PropertyToID("_BaseColor"),
		cutoffId = Shader.PropertyToID("_Cutoff"),
		metallicId = Shader.PropertyToID("_Metallic"),
		smoothnessId = Shader.PropertyToID("_Smoothness"),
		emissionColorId = Shader.PropertyToID("_EmissionColor");


    static MaterialPropertyBlock block;

	void Awake () {
		OnValidate();
	}

    /// OnValidate gets invoked in the Unity editor when the component is loaded or changed.
    void OnValidate () {
		if (block == null) {
			block = new MaterialPropertyBlock();
		}
		block.SetColor(baseColorId, baseColor);
		block.SetFloat(cutoffId, alphaCutoff);
		block.SetFloat(metallicId, metallic);
		block.SetFloat(smoothnessId, smoothness);
		block.SetColor(emissionColorId, emissionColor);
		GetComponent<Renderer>().SetPropertyBlock(block);
	}
}
