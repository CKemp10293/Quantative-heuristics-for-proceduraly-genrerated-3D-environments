using UnityEngine;
[CreateAssetMenu (fileName = "New Map Config",menuName = "Map generator config")]
public class MapConfig : ScriptableObject
{
    public string configName = "New config";

    public bool enableOcean = true;

    [Header("Preset prefrences")]
    public NoisePreset noisePreset;
    public BiomePreset biomePreset;
    public TreePreset treePreset;
}
