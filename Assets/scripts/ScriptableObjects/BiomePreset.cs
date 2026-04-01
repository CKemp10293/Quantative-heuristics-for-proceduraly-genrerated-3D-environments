using UnityEngine;

[CreateAssetMenu(fileName = "New biome preset", menuName = "Biome Generator")]
public class BiomePreset : ScriptableObject
{
    public TypeOfTerrain[] regions;
}

[System.Serializable]
public struct TypeOfTerrain
{
    public string name;
    public float height;
    public Color colour;
}
