using UnityEngine;

[CreateAssetMenu (fileName = "New prefab preset", menuName = "Prefab preset")]
public class TreePreset : ScriptableObject
{

    [Header("Prefab Variant")]
    public TreeConfig[] prefabConfigs;
}