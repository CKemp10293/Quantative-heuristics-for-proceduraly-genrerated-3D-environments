using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NOISEMAP,COLOURMAP,MESH}
    public DrawMode drawMode;
    const int mapChunkSize= 241; // unity imposes a max number of vertcies as 255^2, we need out width have have a nice ammount of factors
                                 // for the chunking to work. 240 has factors: 2,4,6,8,10,12
    [Range(0,6)] // Clamp variable. mulitply by two to get the 12.
    public int levelOfDetail;
    public float noiseScale;
    public bool autoUpdate;
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public BiomePreset biomePreset;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, seed);
        Color[] colourMap = new Color[mapChunkSize*mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x,y];
                if ( biomePreset != null && biomePreset.regions != null)
                {
                    for (int i = 0; i < biomePreset.regions.Length; i++)
                {
                    if (currentHeight <= biomePreset.regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = biomePreset.regions[i].colour;
                        break;
                    }
                }
                }
                
            }
        }

        MapDisplay display = FindFirstObjectByType<MapDisplay>();
        if (drawMode == DrawMode.NOISEMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        } else if (drawMode == DrawMode.COLOURMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap,mapChunkSize,mapChunkSize));
        } else if (drawMode == DrawMode.MESH)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap,meshHeightMultiplier,meshHeightCurve,levelOfDetail),TextureGenerator.TextureFromColourMap(colourMap,mapChunkSize,mapChunkSize));
        }

        
    }

    void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
    }
}


