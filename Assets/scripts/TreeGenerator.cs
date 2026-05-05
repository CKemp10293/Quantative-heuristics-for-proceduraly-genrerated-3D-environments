using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class TreeGenerator : MonoBehaviour
{
    [HideInInspector][SerializeField] private Mesh treeMesh;
    [HideInInspector][SerializeField] private List<Material> treeMaterials = new List<Material>();

    // [lodIndex][batchIndex] — all LOD variants built once at Initialise time
    private Matrix4x4[][][] lodBatchArrays;
    private int[][]         lodBatchCounts;
    private Bounds[][]      lodBatchBounds;  // per-batch AABB for frustum culling
    private int             currentLOD = 0;

    public bool IsInitialised { get; private set; }

    private readonly Plane[] _frustumPlanes = new Plane[6];
    private Camera _mainCamera;

    /// <summary>
    /// Called once per prefab type. Builds and caches draw batches for every LOD level
    /// so that LOD transitions are a free index swap rather than a batch rebuild.
    /// </summary>
    public void Initialise(List<Matrix4x4>[] matricesPerLOD, GameObject prefab)
    {
        MeshRenderer mr = prefab.GetComponentInChildren<MeshRenderer>();
        MeshFilter   mf = prefab.GetComponentInChildren<MeshFilter>();

        if (mr == null || mf == null)
        {
            Debug.LogError("Could not find MeshRenderer or MeshFilter on the tree prefab!");
            return;
        }

        treeMesh      = mf.sharedMesh;
        treeMaterials = new List<Material>(mr.sharedMaterials);

        int numLODs    = matricesPerLOD.Length;
        lodBatchArrays = new Matrix4x4[numLODs][][];
        lodBatchCounts = new int[numLODs][];
        lodBatchBounds = new Bounds[numLODs][];

        for (int lod = 0; lod < numLODs; lod++)
            BuildBatchesForLOD(lod, matricesPerLOD[lod]);

        _mainCamera  = Camera.main;
        IsInitialised = true;
    }

    /// <summary>O(1) LOD switch — just updates the active index.</summary>
    public void SetLOD(int lodIndex)
    {
        currentLOD = Mathf.Clamp(lodIndex, 0, lodBatchArrays != null ? lodBatchArrays.Length - 1 : 0);
    }

    void BuildBatchesForLOD(int lodIndex, List<Matrix4x4> matrices)
    {
        if (matrices == null || matrices.Count == 0)
        {
            lodBatchArrays[lodIndex] = new Matrix4x4[0][];
            lodBatchCounts[lodIndex] = new int[0];
            lodBatchBounds[lodIndex] = new Bounds[0];
            return;
        }

        var tempBatches = new List<List<Matrix4x4>>();
        tempBatches.Add(new List<Matrix4x4>());
        int batchIndex = 0;
        int count      = 0;

        foreach (var t in matrices)
        {
            tempBatches[batchIndex].Add(t);
            if (++count >= 1023)
            {
                tempBatches.Add(new List<Matrix4x4>());
                batchIndex++;
                count = 0;
            }
        }

        int numBatches         = tempBatches.Count;
        lodBatchArrays[lodIndex] = new Matrix4x4[numBatches][];
        lodBatchCounts[lodIndex] = new int[numBatches];
        lodBatchBounds[lodIndex] = new Bounds[numBatches];

        for (int i = 0; i < numBatches; i++)
        {
            lodBatchArrays[lodIndex][i] = tempBatches[i].ToArray();
            lodBatchCounts[lodIndex][i] = tempBatches[i].Count;
            lodBatchBounds[lodIndex][i] = ComputeBatchBounds(tempBatches[i]);
        }
    }

    static Bounds ComputeBatchBounds(List<Matrix4x4> batch)
    {
        Vector3 min = new Vector3( float.MaxValue,  float.MaxValue,  float.MaxValue);
        Vector3 max = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        foreach (var m in batch)
        {
            Vector3 pos = m.GetColumn(3);
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        max.y += 20f; // Vertical padding to cover tree canopy height
        var b = new Bounds();
        b.SetMinMax(min, max);
        return b;
    }

    void Update()
    {
        if (!IsInitialised || treeMesh == null || treeMaterials.Count == 0) return;
        if (lodBatchArrays == null || currentLOD >= lodBatchArrays.Length)  return;

        var currentBatches = lodBatchArrays[currentLOD];
        var currentCounts  = lodBatchCounts[currentLOD];
        var currentBounds  = lodBatchBounds[currentLOD];

        if (currentBatches == null || currentBatches.Length == 0) return;

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);

        for (int b = 0; b < currentBatches.Length; b++)
        {
            // Skip entire 1023-instance batch if it's fully outside the camera frustum
            if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, currentBounds[b])) continue;

            for (int i = 0; i < treeMesh.subMeshCount; i++)
            {
                Material matToUse = (i < treeMaterials.Count) ? treeMaterials[i] : treeMaterials[0];
                Graphics.DrawMeshInstanced(
                    treeMesh,
                    i,
                    matToUse,
                    currentBatches[b],
                    currentCounts[b],
                    null,
                    ShadowCastingMode.Off,
                    true,
                    gameObject.layer,
                    null,
                    LightProbeUsage.Off,  // BlendProbes is expensive at forest density
                    null
                );
            }
        }
    }
}
