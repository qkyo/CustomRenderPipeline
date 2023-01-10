using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectLODProperties : MonoBehaviour
{   
    [SerializeField] float crossFadeAnimationDuration = 0.5f;

	void Update () 
    {
        LODGroup.crossFadeAnimationDuration = crossFadeAnimationDuration;
	}
}
