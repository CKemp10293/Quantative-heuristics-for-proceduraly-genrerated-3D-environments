using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the infinite generation, level of detail , and rendering of terrain chunks
/// around a specified viewer.
/// </summary>
public class EndlessTerrain : MonoBehaviour
{
    private static int s_updateTreesLogCount;

    const float scale = 1f;
    float thresholdForViewerMoveChunkUpdate = 15f;
    float sqrthresholdForViewerMoveChunkUpdate;

    public levelOfDetailInfo[] levelsOfDetail;
    public Material mapMaterial;
    public Transform viewer;

    public static float maxViewDistance = 600;
    public static float maxViewDistanceSquared;
    public static Vector2 viewerPosition;

    static MapGenerator mapGenerator;


    Vector2 viewerOldPosition;
    int chunkSize;
    int numberOfChunksVisibleInViewDistance;

    Dictionary<Vector2,TerrainChunk> terrianChunkDictionary = new Dictionary<Vector2,TerrainChunk>();
    HashSet<TerrainChunk> terrainChunksVisibleLastUpdates = new HashSet<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindFirstObjectByType<MapGenerator>();

        chunkSize = MapGenerator.mapChunkSize - 1;
        numberOfChunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
        maxViewDistance = levelsOfDetail[^1].levelOfDetailVisibleThreshold;
        maxViewDistanceSquared = maxViewDistance * maxViewDistance;
        sqrthresholdForViewerMoveChunkUpdate = thresholdForViewerMoveChunkUpdate * thresholdForViewerMoveChunkUpdate;

        viewerPosition = new Vector2 (viewer.position.x,viewer.position.z) / scale;
        updateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2 (viewer.position.x,viewer.position.z) / scale;

        // Only update chunk if player moves past the threshold. 
        if((viewerOldPosition - viewerPosition).sqrMagnitude > sqrthresholdForViewerMoveChunkUpdate)
        {
            viewerOldPosition = viewerPosition;
            updateVisibleChunks();
        }
    }

    /// <summary>
    /// Scans the grid around the viewer, instantiating new chunks or updating the LOD of existing ones.
    /// </summary>
    void updateVisibleChunks()
    {
        // Update chunks that were visible last frame; remove any that have moved out of range.
        // RemoveWhere iterates once with no allocations — O(1) membership vs O(n) List.Contains.
        terrainChunksVisibleLastUpdates.RemoveWhere(chunk =>
        {
            chunk.UpdateChunk();
            return !chunk.isVisible();
        });

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // Loop through all grid coordinates within the view distance
        for(int yOffset = -numberOfChunksVisibleInViewDistance;yOffset <= numberOfChunksVisibleInViewDistance;yOffset++)
        {
            for(int xOffset =  -numberOfChunksVisibleInViewDistance;xOffset <= numberOfChunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoords = new Vector2(currentChunkCoordX + xOffset,currentChunkCoordY + yOffset);

                if (terrianChunkDictionary.TryGetValue(viewedChunkCoords, out TerrainChunk existingChunk))
                {
                    existingChunk.UpdateChunk();
                    if (existingChunk.isVisible())
                        terrainChunksVisibleLastUpdates.Add(existingChunk); // HashSet ignores duplicates
                } else
                {
                    // chunk doesnt exist yet,  create a new one 
                    TerrainChunk newChunk = new TerrainChunk(viewedChunkCoords,chunkSize,transform,mapMaterial,levelsOfDetail);
                    terrianChunkDictionary.Add(viewedChunkCoords,newChunk);

                    newChunk.UpdateChunk();
                    if (newChunk.isVisible())
                    {
                        terrainChunksVisibleLastUpdates.Add(newChunk);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents an individual square piece of the world, handling its own mesh, LODs, and tree population.
    /// </summary>
    public class TerrainChunk
    {
        GameObject meshObject;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;
        MapData mapData;

        List<TreeGenerator> treeGenerators = new List<TreeGenerator>();
        GameObject treeColliderTrigger;
        bool treeTriggerGenerated = false;        
        bool mapDataRecieved;
        Bounds bounds;
        Vector2 postion;
        levelOfDetailInfo[] levelsOfDetail;
        LODMesh[] lODMeshes;
        int previousLODIndex = -1; 

        // [lodIndex][prefabIndex] — filtered matrices per LOD, built once on map data receipt
        private List<Matrix4x4>[][] cachedLODTreeMatrices;
        private bool treeMatricesCached = false;

        // Tree and collision data
        public TerrainChunk(Vector2 coord,int size,Transform parent,Material material,levelOfDetailInfo[] levelsOfDetail)
        {
            this.levelsOfDetail = levelsOfDetail;
            postion = coord * size;
            Vector3 postionV3 = new Vector3(postion.x,0,postion.y);
            bounds = new Bounds(postionV3, Vector3.one * size);

            // init unity components
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = postionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;

            // Initialize Tree Trigger Container
            treeColliderTrigger = new GameObject("Tree Trigger");
            treeColliderTrigger.transform.parent= meshObject.transform;
            treeColliderTrigger.transform.localPosition = Vector3.zero;

            setVisible(false);

            // Setup LOD Meshes callbacks
            lODMeshes = new LODMesh[levelsOfDetail.Length];
            for (int i = 0; i < levelsOfDetail.Length;  i++)
            {
              lODMeshes[i] = new LODMesh(levelsOfDetail[i].levelOfDetail,UpdateChunk);  
            }

            mapGenerator.RequestMapData(postion,OnMapDataRecieved);
        }
        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            mapDataRecieved = true;
            PrecomputeTreeMatrices(); // Build all LOD variants once before first UpdateChunk
            UpdateChunk();
        }

        /// <summary>
        /// Pre-filters tree matrices for every LOD level so LOD transitions are O(1).
        /// Called once per chunk when map data first arrives.
        /// </summary>
        void PrecomputeTreeMatrices()
        {
            if (mapGenerator.availablePresets.Count <= mapData.presetIndex) return;
            TreePreset preset = mapGenerator.availablePresets[mapData.presetIndex].treePreset;
            if (preset == null || preset.prefabConfigs == null || mapData.treeMatrices == null) return;

            int numLODs    = levelsOfDetail.Length;
            int numPrefabs = mapData.treeMatrices.Length;
            Matrix4x4 globalScaleMatrix = Matrix4x4.Scale(Vector3.one * scale);

            cachedLODTreeMatrices = new List<Matrix4x4>[numLODs][];

            for (int lod = 0; lod < numLODs; lod++)
            {
                cachedLODTreeMatrices[lod] = new List<Matrix4x4>[numPrefabs];
                int skipFactor = 1 << lod;

                for (int i = 0; i < numPrefabs; i++)
                {
                    List<Matrix4x4> raw = mapData.treeMatrices[i];
                    var filtered = new List<Matrix4x4>(raw.Count / skipFactor + 1);
                    for (int k = 0; k < raw.Count; k++)
                    {
                        if (k % skipFactor == 0)
                            filtered.Add(globalScaleMatrix * raw[k]);
                    }
                    cachedLODTreeMatrices[lod][i] = filtered;
                }
            }
            treeMatricesCached = true;
        }

        /// <summary>
        /// Calculates distance to player and assigns the appropriate LOD mesh, or hides the chunk entirely.
        /// </summary>
        public void UpdateChunk()
        {
            if (mapDataRecieved)
            {
                float viewerDistanceFromNearestEdge = bounds.SqrDistance(new Vector3(viewerPosition.x, 0, viewerPosition.y));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistanceSquared;

                if (visible)
                {
                    int LODindex = 0;
                    for (int i = 0; i < levelsOfDetail.Length-1; i++)
                    {
                        if(viewerDistanceFromNearestEdge > levelsOfDetail[i].levelOfDetailVisibleThreshold)
                        {
                            LODindex = i + 1;
                        }
                        else
                        {
                            break; // Viewer is within the threshold for this LOD, no need to check further
                        }
                    }
                    if (LODindex != previousLODIndex)
                    {
                        LODMesh lodMesh = lODMeshes[LODindex];
                        if (lodMesh.hasReceievedMesh)
                        {
                            previousLODIndex = LODindex;
                            meshFilter.mesh = lodMesh.mesh;

                            // Only generate physics for the chunks closest to the player (LOD 0)
                            MeshCollider collider = meshObject.GetComponent<MeshCollider>();
                            if (collider != null)
                            {
                                if (LODindex == 0)
                                    collider.sharedMesh = lodMesh.mesh;
                                else
                                    collider.sharedMesh = null;
                            }
                            updateTrees(LODindex);
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
                setVisible(visible);
            }
        }

        /// <summary>
        /// Switches the active LOD on each TreeGenerator. The first call also initialises
        /// each generator with pre-cached matrices for all LOD levels so subsequent
        /// transitions are a free index swap with no allocations or batch rebuilds.
        /// </summary>
        public void updateTrees(int LODindex)
        {
            if (!treeMatricesCached) return;
            if (mapGenerator.availablePresets.Count <= mapData.presetIndex) return;

            TreePreset preset = mapGenerator.availablePresets[mapData.presetIndex].treePreset;
            if (preset == null || preset.prefabConfigs == null) return;

            if (LODindex == 0)
            {
                treeColliderTrigger.SetActive(true);
                if (!treeTriggerGenerated)
                {
                    GenerateTreeColliders(preset);
                    treeTriggerGenerated = true;
                }
            }
            else
            {
                treeColliderTrigger.SetActive(false);
            }

            int clampedLOD = Mathf.Clamp(LODindex, 0, cachedLODTreeMatrices.Length - 1);

            for (int i = 0; i < mapData.treeMatrices.Length; i++)
            {
                if (treeGenerators.Count <= i)
                {
                    GameObject genObj = new GameObject("PrefabGen_" + preset.prefabConfigs[i].name);
                    genObj.transform.parent = meshObject.transform;
                    genObj.transform.localPosition = Vector3.zero;
                    treeGenerators.Add(genObj.AddComponent<TreeGenerator>());
                }

                TreeGenerator gen = treeGenerators[i];
                if (!gen.IsInitialised)
                {
                    // Pass all LOD variants at once — batches are built here and never rebuilt
                    var allLODMatrices = new List<Matrix4x4>[cachedLODTreeMatrices.Length];
                    for (int lod = 0; lod < cachedLODTreeMatrices.Length; lod++)
                        allLODMatrices[lod] = cachedLODTreeMatrices[lod][i];
                    gen.Initialise(allLODMatrices, preset.prefabConfigs[i].prefab);
                }
                gen.SetLOD(clampedLOD);
            }
        }

        /// <summary>
        /// Instantiates physical colliders for trees when the player is extremely close (LOD 0).
        /// </summary>
        void GenerateTreeColliders(TreePreset preset)
        {
            Matrix4x4 globalScaleMatrix = Matrix4x4.Scale(Vector3.one * scale);

            for (int i = 0; i < mapData.treeMatrices.Length; i++)
            {
                TreeConfig config = preset.prefabConfigs[i];
                if(config.collisionPrefab == null) continue;

                List<Matrix4x4> rawMatrices = mapData.treeMatrices[i];
                for (int k = 0; k < rawMatrices.Count; k++)
                {
                    Matrix4x4 scaledMatrix = globalScaleMatrix * rawMatrices[k];
                    
                    // Extract exact position and rotation from the GPU matrix
                    Vector3 position = scaledMatrix.GetPosition();
                    Quaternion rotation = scaledMatrix.rotation;
                    
                    // Spawn the invisible collider
                    GameObject colliderObj = Instantiate(config.collisionPrefab, position, rotation);
                    colliderObj.transform.parent = treeColliderTrigger.transform;
                    
                    // Match the scale 
                    colliderObj.transform.localScale = scaledMatrix.lossyScale;

                    TreeColliderData data = colliderObj.GetComponent<TreeColliderData>();
                    if (data!=null)
                    {
                        data.detailedColliderPrefab = config.prefab;
                    }
                }
            }
        }

        public void setVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool isVisible()
        {
            return meshObject.activeSelf;
        }


    }

    /// <summary>
    /// Handles the asynchronous request and storage of mesh data for a specific Level of Detail.
    /// </summary>
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasReceievedMesh;
        int levelOfDetail;
        System.Action updateCallBack;

        public LODMesh(int levelOfDetail,System.Action updateCallBack)
        {
            this.levelOfDetail = levelOfDetail;
            this.updateCallBack = updateCallBack;
        }
        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasReceievedMesh = true;

            updateCallBack();
        } 
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData,levelOfDetail,OnMeshDataRecieved);
        }
    }
    [System.Serializable]
    public struct levelOfDetailInfo
    {
        public int levelOfDetail;
        public float levelOfDetailVisibleThreshold;
    }

}
