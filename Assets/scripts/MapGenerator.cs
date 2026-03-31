using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using System.Linq;
using Unity.VisualScripting;
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NOISEMAP,COLOURMAP,MESH}
    public DrawMode drawMode;
    public const int mapChunkSize= 239; // unity imposes a max number of vertcies as 255^2,
                                        // we need out width have have a nice ammount of factors
                                        // for the chunking to work. 240 has factors: 2,4,6,8,10,12

    [Range(0,6)] // Clamp variable. mulitply by two to get the 12.
    public int EditorLevelOfDetail;
    public bool autoUpdate;
    public Vector2 offset;

    [Header("Preset Configuration")]
    // The list of all possible presets.
    public List<MapConfig> availablePresets;
    
    // The currently selected index 
    [HideInInspector] public int activePresetIndex = 0;
    private BiomePreset biomePreset;
    private NoisePreset noisePreset;
    private TreePreset treePreset;
    public Material terrainMaterial;
    public GameObject oceanObject;

    public bool useGPUInstancing = true;
    // queue for map info (colours and look ect.)
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    // queue for mesh info (height and curves ect.)
    Queue<MapThreadInfo<MeshData>> meshDataTheadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {

        UpdateEnvSetting();
        MapData mapData = GenerateMap(Vector2.zero);
        MapDisplay display = FindFirstObjectByType<MapDisplay>();
        if (drawMode == DrawMode.NOISEMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        } else if (drawMode == DrawMode.COLOURMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap,mapChunkSize,mapChunkSize));
        } else if (drawMode == DrawMode.MESH)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap,noisePreset.settings[0].meshHeightMultiplier
            ,noisePreset.settings[0].meshHeightCurve,EditorLevelOfDetail),TextureGenerator.TextureFromColourMap(mapData.colourMap,mapChunkSize,mapChunkSize));
        }
    }

    public void RequestMapData(Vector2 center,Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center,callback);
        };
        new Thread (threadStart).Start();
    }

    void MapDataThread(Vector2 center,Action<MapData> callback)
    {
        MapData mapData = GenerateMap(center);
        // prevent race conditions by locking queue.
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));

        }
    }

    public void RequestMeshData(MapData mapData,int LOD,Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData,LOD,callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData,int LOD, Action<MeshData> callback)
    {
        MapConfig config = availablePresets[mapData.presetIndex];
        NoisePreset noisePreset = config.noisePreset;

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,
        noisePreset.settings[0].meshHeightMultiplier,
        noisePreset.settings[0].meshHeightCurve,LOD);
        lock (meshDataTheadInfoQueue)
        {
            meshDataTheadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }

    void Update()
    {
        while(mapDataThreadInfoQueue.Count > 0)
        {
            MapThreadInfo<MapData> threadInfoMap = mapDataThreadInfoQueue.Dequeue();
            threadInfoMap.callback(threadInfoMap.parameter);
        }
        while(meshDataTheadInfoQueue.Count > 0)
        {
            MapThreadInfo<MeshData> threadInfoMesh = meshDataTheadInfoQueue.Dequeue();
            threadInfoMesh.callback(threadInfoMesh.parameter);
        }    
    }
    void Awake()
    {
        UpdateEnvSetting();
        // 1. Grab the active config preset FIRST so we can read its data
        if (availablePresets != null && availablePresets.Count > 0)
        {
            activePresetIndex = Mathf.Clamp(activePresetIndex, 0, availablePresets.Count - 1);
            noisePreset = availablePresets[activePresetIndex].noisePreset;
        }

    }

    MapData GenerateMap(Vector2 centre)
    {
        activePresetIndex = Mathf.Clamp(activePresetIndex, 0, availablePresets.Count - 1);
        // Pull the sub-files from the Master Config
        MapConfig activeConfig = availablePresets[activePresetIndex];
        noisePreset = activeConfig.noisePreset;
        biomePreset = activeConfig.biomePreset;
        treePreset = activeConfig.treePreset;

        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2,noisePreset.settings[0].noiseScale, noisePreset.settings[0].octaves, noisePreset.settings[0].persistance,
         noisePreset.settings[0].lacunarity,noisePreset.settings[0].seed, centre + offset,noisePreset.settings[0].normalisationMode );

        Color[] colourMap = new Color[mapChunkSize*mapChunkSize];
        
        // Get how many types of prefabs we wish to generate.  0 if none available
        int prefabTypeCount = (treePreset != null && treePreset.prefabConfigs != null) ? treePreset.prefabConfigs.Length : 0;
        List<Matrix4x4>[] prefabMatrices = new List<Matrix4x4>[prefabTypeCount];
        for (int i = 0; i < prefabTypeCount; i++)
        {
            prefabMatrices[i] = new List<Matrix4x4>();
        }
        
        // So the trees are the same if the seed is the same
        int chunkSeed = centre.GetHashCode() + noisePreset.settings[0].seed;
        System.Random treeRNG = new System.Random(chunkSeed);

        AnimationCurve heightCurve = new AnimationCurve(noisePreset.settings[0].meshHeightCurve.keys);
        float heightMultiplier = noisePreset.settings[0].meshHeightMultiplier;

        float topLeftX = (mapChunkSize - 1) / -2f;
        float topLeftZ = (mapChunkSize - 1) / 2f;
        
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x + 1,y + 1];
                
                int currentBiomeIndex = -1; // Track which biome we are in
                if ( !object.ReferenceEquals(biomePreset,null) && biomePreset.regions != null)
                {
                    for (int i = 0; i < biomePreset.regions.Length; i++)
                {
                    if (currentHeight >= biomePreset.regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = biomePreset.regions[i].colour;
                        currentBiomeIndex = i; // Save index
                    }
                    else
                    {
                        break;
                    }
                }
                }
                // Adding tree logic
                if(!object.ReferenceEquals(treePreset,null))
                {
                    for (int i = 0; i < prefabTypeCount; i++)
                    {
                        TreeConfig config = treePreset.prefabConfigs[i];

                        if(config == null || object.ReferenceEquals(config.prefab,null)) continue;
                        if(currentBiomeIndex != config.spawnOnBiomeIndex) continue;

                        if(treeRNG.NextDouble() < config.density)
                        {
                            float localx = topLeftX + x;
                            float localz = topLeftZ - y;
                            float localy = heightCurve.Evaluate(currentHeight) * heightMultiplier;

                            Vector3 position = new Vector3(centre.x + localx,localy + config.heightOffset,centre.y + localz);
                            quaternion rotation = quaternion.Euler(0,(float)treeRNG.NextDouble() * 360f,0);
                            float scaleValue = Mathf.Lerp(config.minScale,config.maxScale,(float)treeRNG.NextDouble());
                            Vector3 scale = Vector3.one * scaleValue;

                            if(prefabMatrices[i] == null ) prefabMatrices[i] = new List<Matrix4x4>();

                            prefabMatrices[i].Add(Matrix4x4.TRS(position,rotation,scale));
                            break;
                        }
                    }
                }
                
            }
        }
        return new MapData(noiseMap,colourMap,prefabMatrices,activePresetIndex);
    }

    public void UpdateEnvSetting()
    {
        if (availablePresets == null || availablePresets.Count == 0)
        {
            return;
        }

        activePresetIndex = Mathf.Clamp(activePresetIndex,0,availablePresets.Count);
        MapConfig activeConfig = availablePresets[activePresetIndex];

        if(oceanObject != null)
        {
            oceanObject.SetActive(activeConfig.enableOcean);
        }

        StarterAssets.FirstPersonController player = FindFirstObjectByType<StarterAssets.FirstPersonController>();
        if (player != null)
        {
            player.oceanEnabled = activeConfig.enableOcean;
        }

        if (activeConfig.textureData != null && terrainMaterial != null && activeConfig.noisePreset != null)
        {
            // Send the arrays to the GPU
            activeConfig.textureData.ApplyToMat(terrainMaterial);

            // Send the min/max heights to the GPU so the shader blends accurately
            float maxHeight = activeConfig.noisePreset.settings[0].meshHeightMultiplier;
            float minHeight = 0f; 
            activeConfig.textureData.UpdateMeshHeights(terrainMaterial, minHeight, maxHeight);
        }

        if (player != null && player.ambientAudioSource != null && activeConfig.mapAudio != null)
        {
            // Only swap and restart the audio if it's actually a different biome track.
            if (player.ambientAudioSource.clip != activeConfig.mapAudio)
            {
                player.ambientAudioSource.clip = activeConfig.mapAudio;
                player.ambientAudioSource.Play();
            }
        }

        if (player != null && player.windParticleSystem != null)
        {
            if (activeConfig.enableWind)
            {
                // Turn it on if it was off
                if (!player.windParticleSystem.isPlaying) player.windParticleSystem.Play();

                

                // Inject the settings into the Particle System modules
                var mainModule = player.windParticleSystem.main;
                var emissionModule = player.windParticleSystem.emission;
                var velocityModule = player.windParticleSystem.velocityOverLifetime;

                mainModule.startSize = activeConfig.particleWindSize;

                mainModule.startColor = activeConfig.windColor;
                emissionModule.rateOverTime = activeConfig.windThickness;
                
                // Multiply the baseline X and Z velocity we set in the editor
                velocityModule.speedModifier = activeConfig.windSpeedMultiplier;
            }
            else
            {
                // If this biome shouldn't have wind (like an underwater or indoor scene), turn it off
                player.windParticleSystem.Stop();
                player.windParticleSystem.Clear();
            }
        }

        BirdSpawner birdSpawner = player.GetComponent<BirdSpawner>();
        if (birdSpawner != null)
        {
            birdSpawner.currentBirdPrefab = activeConfig.birdPrefab;
            birdSpawner.spawnRate = activeConfig.birdSpawnRate;
        }
    }

    // Generic struct to hold map and mesh information for threading.
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;
    public readonly List<Matrix4x4>[] treeMatrices; // New: Holds tree data
    public readonly int presetIndex;

    public MapData(float[,] heightMap,Color[] colourMap,List<Matrix4x4>[] treeMatrices,int presetIndex)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
        this.treeMatrices = treeMatrices;
        this.presetIndex = presetIndex;
    }
}


