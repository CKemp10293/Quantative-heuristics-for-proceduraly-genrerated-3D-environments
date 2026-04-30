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
    List<TerrainChunk> terrainChunksVisibleLastUpdates = new List<TerrainChunk>();

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
        // Update chunks that were visible last frame and hide them if they moved out of range
        for (int i = terrainChunksVisibleLastUpdates.Count - 1; i >= 0; i--)
        {
            terrainChunksVisibleLastUpdates[i].UpdateChunk();
            if (!terrainChunksVisibleLastUpdates[i].isVisible())
            {
                terrainChunksVisibleLastUpdates.RemoveAt(i);
            }
        }

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
                    terrianChunkDictionary[viewedChunkCoords].UpdateChunk();
                    if (terrianChunkDictionary[viewedChunkCoords].isVisible())
                    {
                        if (!terrainChunksVisibleLastUpdates.Contains(terrianChunkDictionary[viewedChunkCoords]))
                        {
                            terrainChunksVisibleLastUpdates.Add(terrianChunkDictionary[viewedChunkCoords]);
                        }
                    }
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
            UpdateChunk();
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
        /// Manages the density and collision of trees based on the chunk's active LOD.
        /// </summary>
        public void updateTrees(int LODindex)
        {
            // Saftey check
            if (mapGenerator.availablePresets.Count <= mapData.presetIndex) return;

            TreePreset preset = mapGenerator.availablePresets[mapData.presetIndex].treePreset;
            if(preset == null || preset.prefabConfigs == null) return ;

            
            // Toggle high-fidelity physics only on LOD 0
            if(LODindex == 0)
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

            Matrix4x4 globalScaleMatrix = Matrix4x4.Scale(Vector3.one * scale);
            int skipTreeDrawFactor = 1 << LODindex;

            //Loop through all tree types in map data
            for (int i = 0; i < mapData.treeMatrices.Length; i++)
            {
                // Ensure we have a treeGenerator for this prefab type
                if (treeGenerators.Count <= i)
                {
                    GameObject genObj = new GameObject("PrefabGen_" + preset.prefabConfigs[i].name);
                    genObj.transform.parent = meshObject.transform;
                    genObj.transform.localPosition = Vector3.zero;
                    treeGenerators.Add(genObj.AddComponent<TreeGenerator>());
                }
                
                // Filter matrix for this sepcific type of prefab.
                List<Matrix4x4> rawMatrices = mapData.treeMatrices[i];
                List<Matrix4x4> filteredPrefabs = new List<Matrix4x4>();

                for (int k = 0; k < rawMatrices.Count; k++)
                {
                    if(k % skipTreeDrawFactor == 0)
                    {
                        filteredPrefabs.Add(globalScaleMatrix * rawMatrices[k]);
                    }
                }
                // init generator for this prefab
                treeGenerators[i].Initialise(filteredPrefabs,preset.prefabConfigs[i].prefab);
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
