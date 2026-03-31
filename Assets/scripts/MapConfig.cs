using UnityEngine;
[CreateAssetMenu (fileName = "New Map Config",menuName = "Map generator config")]
public class MapConfig : ScriptableObject
{
    public string configName = "New config";

    public bool enableOcean = true;

    public AudioClip mapAudio;

    [Header("Wind VFX Settings")]
    public bool enableWind = true;
    public Color windColor = new Color(1f, 1f, 1f, 0.5f); // Default to semi-transparent white
    [Range(10, 500)] public float windThickness = 50f;    // How many particles to spawn
    public float windSpeedMultiplier = 1f;
    public float particleWindSize = 0.04f;

    [Header("Wildlife Settings")]
    public GameObject birdPrefab; // The specific bird for this scene
    public float birdSpawnRate = 5f;

    [Header("Preset prefrences")]
    public NoisePreset noisePreset;
    public BiomePreset biomePreset;
    public TreePreset treePreset;

    public TextureData textureData;
}
