using UnityEngine;

[CreateAssetMenu (fileName = "New tree preset", menuName = "treeSettings")]
public class TreePreset : ScriptableObject
{
    public GameObject treePrefab;
    [Range(0,1)] public float density;
    [Range (0.1f,5f)] public float minScale;
    [Range (0.1f,5f)] public float maxScale;

    public int spawnOnBiomeIndex; 
}
