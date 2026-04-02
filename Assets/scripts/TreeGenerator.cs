using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class TreeGenerator : MonoBehaviour
{
    private static int s_initLogCount;
    private static bool s_loggedDrawAttempt;

    [HideInInspector][SerializeField] private Mesh treeMesh;
    [HideInInspector][SerializeField] private List<Material> treeMaterials = new List<Material>();
    [SerializeField] private List<Matrix4x4> allTransforms = new List<Matrix4x4>();

    private Matrix4x4[][] batchArrays;
    private int[] batchCounts;
    private bool isBatched = false;
    public void Initialise(List<Matrix4x4> transforms, GameObject prefab)
    {
        this.allTransforms = new List<Matrix4x4>(transforms);
        
        // Search inside children for the renderer
        // Synty assets often put the mesh on a child object named "Mesh" or "LOD0"
        MeshRenderer mr = prefab.GetComponentInChildren<MeshRenderer>();
        MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();

        if (mr == null || mf == null) 
        {
            Debug.LogError("Could not find MeshRenderer or MeshFilter on the tree prefab!");
            return;
        }

        this.treeMesh = mf.sharedMesh;
        this.treeMaterials = new List<Material>(mr.sharedMaterials);

        BuildBatches();
    }

    void BuildBatches()
    {
        if (allTransforms == null || allTransforms.Count == 0) return;

        // Build the chunks using temporary lists
        List<List<Matrix4x4>> tempBatches = new List<List<Matrix4x4>>();
        tempBatches.Add(new List<Matrix4x4>());

        int batchIndex = 0;
        int count = 0;

        foreach (var t in allTransforms)
        {
            tempBatches[batchIndex].Add(t);
            count++;
            if (count >= 1023) // Unity's strict instancing limit
            {
                tempBatches.Add(new List<Matrix4x4>());
                batchIndex++;
                count = 0;
            }
        }

        // Convert to permanent arrays once
        batchArrays = new Matrix4x4[tempBatches.Count][];
        batchCounts = new int[tempBatches.Count];

        for (int i = 0; i < tempBatches.Count; i++)
        {
            batchArrays[i] = tempBatches[i].ToArray(); // Allocation happens ONCE here
            batchCounts[i] = tempBatches[i].Count;
        }

        isBatched = true;
    }

    void Update()
    {
       if (!isBatched && allTransforms != null && allTransforms.Count > 0)
        {
            BuildBatches();
        }

        if (isBatched && treeMesh != null && treeMaterials.Count > 0)
        {
            // Using a standard 'for' loop avoids the hidden enumerator allocation of 'foreach'
            for (int b = 0; b < batchArrays.Length; b++)
            {
                for (int i = 0; i < treeMesh.subMeshCount; i++)
                {
                    Material matToUse = (i < treeMaterials.Count) ? treeMaterials[i] : treeMaterials[0];
                    Graphics.DrawMeshInstanced(
                        treeMesh, 
                        i, 
                        matToUse, 
                        batchArrays[b], // Passing the pre-built array (0 allocations)
                        batchCounts[b], 
                        null,
                        ShadowCastingMode.Off, 
                        true,
                        gameObject.layer,
                        null,
                        LightProbeUsage.BlendProbes,
                        null
                    );
                }
            }
        }
    }
}
