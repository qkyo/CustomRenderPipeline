using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
	// if the parameter is a part of the UnityPerMaterial buffer it can be configured per instance.
	[SerializeField] Color baseColor = Color.white;
	[SerializeField, Range(0f, 1f)] float cutoff = 0.5f;

	static int baseColorId = Shader.PropertyToID("_BaseColor");
	static int cutoffId = Shader.PropertyToID("_Cutoff");

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
		block.SetFloat(cutoffId, cutoff);
		GetComponent<Renderer>().SetPropertyBlock(block);
	}
}
