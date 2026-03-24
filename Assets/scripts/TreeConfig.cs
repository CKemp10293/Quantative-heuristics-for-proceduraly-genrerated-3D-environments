using UnityEngine;

[System.Serializable]
public class TreeConfig
{
    public string name =  "New Prefab";
    public GameObject prefab;
    public GameObject collisionPrefab;

    [Tooltip("Which biome would you like to spawn THIS prefab on")]
    public int spawnOnBiomeIndex = 0;

    [Range(0f,1f)] public float density = 0.1f;
    public float minScale = 1f;
    public float maxScale = 2f;
    public float heightOffset = 0.5f;
}

[CreateAssetMenu (fileName = "New prefab preset", menuName = "Prefab preset")]
public class TreePreset : ScriptableObject
{

    [Header("Prefab Variant")]
    public TreeConfig[] prefabConfigs;
}