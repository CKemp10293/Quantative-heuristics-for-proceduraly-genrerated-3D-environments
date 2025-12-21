using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;

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
    [HideInInspector]
    public float persistance;
    [HideInInspector]
    public float lacunarity;
    public int seed;
    [HideInInspector]
    public float meshHeightMultiplier;
    [HideInInspector]
    public AnimationCurve meshHeightCurve;

    public BiomePreset biomePreset;
    public NoisePreset noisePreset;

    // going to add tree functionality next!!!!
    public TreePreset treePreset;

    public bool useGPUInstancing = true;
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, octaves, noisePreset.settings[0].persistance, noisePreset.settings[0].lacunarity, seed);
        Color[] colourMap = new Color[mapChunkSize*mapChunkSize];
        // So the trees are the same if the seed is the same
        System.Random TREERNG = new System.Random(seed);
        List<Matrix4x4> treeMatrcies = new List<Matrix4x4>();

        float topLeftX = (mapChunkSize - 1) / -2f;
        float topLeftZ = (mapChunkSize - 1) / 2f;

        // Getting the tree material/mesh from preset
        Mesh treeMesh = treePreset.treePrefab.GetComponent<MeshFilter>().sharedMesh;

        Material treeMaterial;
        if (treePreset.materialOverride != null)
        {
            treeMaterial = treePreset.materialOverride;
        }
        else
        {
            treeMaterial = treePreset.treePrefab.GetComponent<MeshRenderer>().sharedMaterial;
        }

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x,y];
                int currentBiomeIndex = -1; // Track which biome we are in
                if ( biomePreset != null && biomePreset.regions != null)
                {
                    for (int i = 0; i < biomePreset.regions.Length; i++)
                {
                    if (currentHeight <= biomePreset.regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = biomePreset.regions[i].colour;
                        currentBiomeIndex = i; // Save index
                        break;
                    }
                }
                }
                // Adding tree logic
                if(currentBiomeIndex == treePreset.spawnOnBiomeIndex)
                {
                    if(TREERNG.NextDouble() < treePreset.density)
                    {
                        float posX = topLeftX + x;
                        float posZ = topLeftZ - y;
                    
                        float posY = meshHeightCurve.Evaluate(currentHeight) * meshHeightMultiplier;

                    // 3. Create Vector relative to the Map Generator
                        Vector3 localPosition = new Vector3(posX * 10, posY * 10, posZ * 10);

                        Vector3 worldPos = transform.TransformPoint(localPosition);
                        worldPos.y += treePreset.heightOffset;
                    
                        Quaternion rotation = Quaternion.Euler(0, (float)TREERNG.NextDouble() * 360f, 0);
                        float scaleVal = Mathf.Lerp(treePreset.minScale, treePreset.maxScale, (float)TREERNG.NextDouble());
                        Vector3 scale = Vector3.one * scaleVal;

                        treeMatrcies.Add(Matrix4x4.TRS(worldPos, rotation, scale));
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
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap,noisePreset.settings[0].meshHeightMultiplier,noisePreset.settings[0].meshHeightCurve,levelOfDetail),TextureGenerator.TextureFromColourMap(colourMap,mapChunkSize,mapChunkSize));
        }
        TreeGenerator foliageRenderer = GetComponent<TreeGenerator>();
        if (foliageRenderer == null) foliageRenderer = gameObject.AddComponent<TreeGenerator>();
    
         // Pass the custom Material Override
        Material matToUse = (treePreset.materialOverride != null) ? treePreset.materialOverride : treeMaterial;
        foliageRenderer.Initialise(treeMatrcies, treePreset.treePrefab);

        
    }
}


