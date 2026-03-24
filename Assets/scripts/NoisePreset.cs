using UnityEngine;

[CreateAssetMenu(fileName = "New noise preset", menuName = "Noise preset")]
public class NoisePreset : ScriptableObject
{
   public TypeOfNoise[] settings;
}

[System.Serializable]
public struct TypeOfNoise
{
    [Range(1,10)]
    public float lacunarity;
    [Range(0,1)]
    public float persistance;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public int seed;
    public int octaves;

    public Noise.NormalisationMode normalisationMode;

}
