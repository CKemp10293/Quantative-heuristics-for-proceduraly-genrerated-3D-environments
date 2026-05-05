using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using Unity.Mathematics;
using System.Diagnostics;

/// <summary>
/// Handles the procedural generation, threading, and environment setup for terrain chunks.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NOISEMAP,COLOURMAP,MESH}
    public DrawMode drawMode;

    // Unity imposes a max number of vertices as 255^2 per mesh.
    // 240 has factors: 2, 4, 6, 8, 10, 12, which makes LOD chunking divide evenly.
    public const int mapChunkSize= 239; 

    [Range(0,6)]
    public int editorLevelOfDetail;
    public bool autoUpdate;
    public bool useGPUInstancing = true;

    public Vector2 offset;

    [Header("Preset Configuration")]
    public List<MapConfig> availablePresets;
    [HideInInspector] public int activePresetIndex = 0;

    public Material terrainMaterial;
    public GameObject oceanObject;

    // Internal states
    private BiomePreset biomePreset;
    private NoisePreset noisePreset;
    private TreePreset treePreset;
    private StarterAssets.FirstPersonController cachedPlayer;

    // queue for map info (colours and look ect.)
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    // queue for mesh info (height and curves ect.)
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public double lastChunkGenerationTime { get; private set; }
    private readonly object timerLock = new object();
    void Awake()
    {
        // Bind the selected biome from the main menu, if it exists
        if (mainMenuController.selectedBiome != null && availablePresets != null)
        {
            for (int i = 0; i < availablePresets.Count; i++)
            {
                if (availablePresets[i] == mainMenuController.selectedBiome)
                {
                    activePresetIndex = i;
                    break; 
                }
            }
        }

        UpdateEnvSetting();

        // 1. Grab the active config preset FIRST so we can read its data
        if (availablePresets != null && availablePresets.Count > 0)
        {
            activePresetIndex = Mathf.Clamp(activePresetIndex, 0, availablePresets.Count - 1);
            noisePreset = availablePresets[activePresetIndex].noisePreset;
        }

    }

    void Update()
    {
        // Process background thread callbacks on the Main Unity Thread
        // Process at most 2 callbacks per queue per frame to prevent frame spikes
        // when several chunks arrive from background threads simultaneously.
        int processed = 0;
        while(mapDataThreadInfoQueue.Count > 0 && processed++ < 2)
        {
            MapThreadInfo<MapData> threadInfoMap = mapDataThreadInfoQueue.Dequeue();
            threadInfoMap.callback(threadInfoMap.parameter);
        }
        processed = 0;
        while(meshDataThreadInfoQueue.Count > 0 && processed++ < 2)
        {
            MapThreadInfo<MeshData> threadInfoMesh = meshDataThreadInfoQueue.Dequeue();
            threadInfoMesh.callback(threadInfoMesh.parameter);
        }    
    }

    /// <summary>
    /// Generates and renders a map immediately. Used primarily for Editor visualization.
    /// </summary>
    public void DrawMapInEditor()
    {

        UpdateEnvSetting();
        MapData mapData = GenerateMap(Vector2.zero);
        MapDisplay display = FindFirstObjectByType<MapDisplay>();

        // Cache noise settings to avoid repetitive deep array access
        var activeNoiseSettings = noisePreset.settings[0];

        switch (drawMode)
        {
            case DrawMode.NOISEMAP:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.COLOURMAP:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.MESH:
                MeshData meshData = MeshGenerator.GenerateTerrainMesh(
                    mapData.heightMap,
                    activeNoiseSettings.meshHeightMultiplier,
                    activeNoiseSettings.meshHeightCurve,
                    editorLevelOfDetail
                );
                Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize);
                display.DrawMesh(meshData, texture);
                break;
        }
    }
    /// <summary>
    /// Asynchronously requests raw mathematical map data (heights, colours, tree positions).
    /// </summary>
    public void RequestMapData(Vector2 center,Action<MapData> callback)
    {
        ThreadPool.QueueUserWorkItem(_ => MapDataThread(center, callback));
    }
    void MapDataThread(Vector2 center,Action<MapData> callback)
    {
        MapData mapData = GenerateMap(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));

        }
    }

    /// <summary>
    /// Asynchronously requests the geometric mesh data constructed from the generated MapData.
    /// </summary>
    public void RequestMeshData(MapData mapData,int LOD,Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            ThreadPool.QueueUserWorkItem(_ => MeshDataThread(mapData, LOD, callback));
        };
        new Thread(threadStart).Start();
    }
    void MeshDataThread(MapData mapData,int LOD, Action<MeshData> callback)
    {
        MapConfig config = availablePresets[mapData.presetIndex];
        var activeNoiseSettings = config.noisePreset.settings[0];

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,
        activeNoiseSettings.meshHeightMultiplier,
        activeNoiseSettings.meshHeightCurve,LOD);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
    }

    
    
    /// <summary>
    /// The core generation algorithm. Creates heightmaps, assigns biomes, and calculates tree matrices.
    /// </summary>
    MapData GenerateMap(Vector2 centre)
    {

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();


        activePresetIndex = Mathf.Clamp(activePresetIndex, 0, availablePresets.Count - 1);
        MapConfig activeConfig = availablePresets[activePresetIndex];

        noisePreset = activeConfig.noisePreset;
        biomePreset = activeConfig.biomePreset;
        treePreset = activeConfig.treePreset;
        var activeNoiseSettings = noisePreset.settings[0];

        float[,] noiseMap = Noise.GenerateNoiseMap(
        mapChunkSize + 2,
        mapChunkSize + 2,
        activeNoiseSettings.noiseScale,
        activeNoiseSettings.octaves,
        activeNoiseSettings.persistance,
        activeNoiseSettings.lacunarity,
        activeNoiseSettings.seed,
        centre + offset,
        activeNoiseSettings.normalisationMode);

        Color[] colourMap = new Color[mapChunkSize*mapChunkSize];
        
        // Get how many types of prefabs we wish to generate.  0 if none available
        int prefabTypeCount = (treePreset != null && treePreset.prefabConfigs != null) ? treePreset.prefabConfigs.Length : 0;
        List<Matrix4x4>[] prefabMatrices = new List<Matrix4x4>[prefabTypeCount];
        for (int i = 0; i < prefabTypeCount; i++)
        {
            prefabMatrices[i] = new List<Matrix4x4>();
        }
        
        // Use a seeded RNG to guarantee deterministic tree placement per chunk
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
                int currentBiomeIndex = -1;

                // Biome evaluation
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
                            break; // Because regions are typically ordered by height
                        }
                    }
                }

                // Tree generation logic
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

                            // Use UnityEngine.Quaternion for Matrix4x4 compatibility
                            quaternion rotation = quaternion.Euler(0,(float)treeRNG.NextDouble() * 360f,0);
                            float scaleValue = Mathf.Lerp(config.minScale,config.maxScale,(float)treeRNG.NextDouble());
                            Vector3 scale = Vector3.one * scaleValue;

                            if(prefabMatrices[i] == null ) prefabMatrices[i] = new List<Matrix4x4>();

                            prefabMatrices[i].Add(Matrix4x4.TRS(position,rotation,scale));
                            break; // Only spawn one tree type per coordinate
                        }
                    }
                }
                
            }
        }

        stopwatch.Stop();
        lock (timerLock)
        {
            lastChunkGenerationTime = stopwatch.Elapsed.TotalMilliseconds;
        }

        UnityEngine.Debug.Log("Chunk Generated in: " + lastChunkGenerationTime);
        return new MapData(noiseMap,colourMap,prefabMatrices,activePresetIndex);
    }

    /// <summary>
    /// Applies the active biome's environmental settings (audio, particles, materials) to the scene and player.
    /// </summary>
    public void UpdateEnvSetting()
    {
        if (availablePresets == null || availablePresets.Count == 0)
        {
            return;
        }

        activePresetIndex = Mathf.Clamp(activePresetIndex, 0, availablePresets.Count - 1);
        MapConfig activeConfig = availablePresets[activePresetIndex];

        if(oceanObject != null)
        {
            oceanObject.SetActive(activeConfig.enableOcean);
        }

        // OPTIMIZATION: Cache the player reference to avoid O(N) lookup overhead.
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<StarterAssets.FirstPersonController>();
        }

        if (cachedPlayer != null)
        {
            cachedPlayer.oceanEnabled = activeConfig.enableOcean;
            cachedPlayer.sandMaxHeight = activeConfig.sandMaxHeight;
            cachedPlayer.grassMaxHeight = activeConfig.grassMaxHeight;

            cachedPlayer.snowFootsteps = activeConfig.biomeSnowFootsteps;
            cachedPlayer.grassFootsteps = activeConfig.biomeGrassFootsteps;
            cachedPlayer.sandFootsteps = activeConfig.biomeSandFootsteps;

            if (cachedPlayer.ambientAudioSource != null && activeConfig.mapAudio != null)
            {
                if (cachedPlayer.ambientAudioSource.clip != activeConfig.mapAudio)
                {
                    cachedPlayer.ambientAudioSource.clip = activeConfig.mapAudio;
                    cachedPlayer.ambientAudioSource.Play();
                }
            }

            if (cachedPlayer.windParticleSystem != null)
            {
                if (activeConfig.enableWind)
                {
                    if (!cachedPlayer.windParticleSystem.isPlaying) cachedPlayer.windParticleSystem.Play();

                    var mainModule = cachedPlayer.windParticleSystem.main;
                    var emissionModule = cachedPlayer.windParticleSystem.emission;
                    var velocityModule = cachedPlayer.windParticleSystem.velocityOverLifetime;

                    mainModule.startSize = activeConfig.particleWindSize;
                    mainModule.startColor = activeConfig.windColor;
                    emissionModule.rateOverTime = activeConfig.windThickness;
                    velocityModule.speedModifier = activeConfig.windSpeedMultiplier;
                }
                else
                {
                    cachedPlayer.windParticleSystem.Stop();
                    cachedPlayer.windParticleSystem.Clear();
                }
            }

            if (cachedPlayer.TryGetComponent(out BirdSpawner birdSpawner))
            {
                birdSpawner.currentBirdPrefab = activeConfig.birdPrefab;
                birdSpawner.spawnRate = activeConfig.birdSpawnRate;
            }
        }

        if (activeConfig.textureData != null && terrainMaterial != null && activeConfig.noisePreset != null)
        {
            activeConfig.textureData.ApplyToMat(terrainMaterial);
            float maxHeight = activeConfig.noisePreset.settings[0].meshHeightMultiplier;
            activeConfig.textureData.UpdateMeshHeights(terrainMaterial, 0f, maxHeight);
        }
    }

    /// <summary>
    /// Generic struct to securely pass map and mesh information between threads.
    /// </summary>
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

/// <summary>
/// Immutable container holding the generated mathematical data for a terrain chunk.
/// </summary>
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


