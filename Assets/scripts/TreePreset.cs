using UnityEngine;

[CreateAssetMenu (fileName = "New tree preset", menuName = "treeSettings")]
public class TreePreset : ScriptableObject
{
    public GameObject treePrefab;
    public Material materialOverride;
    public float heightOffset = -0.2f;
    [Range(0,1)] public float density;
    [Range (0.1f,500f)] public float minScale;
    [Range (0.1f,500f)] public float maxScale;

    public int spawnOnBiomeIndex; 
}
