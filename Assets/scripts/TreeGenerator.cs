using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class TreeGenerator : MonoBehaviour
{
    [HideInInspector][SerializeField] private Mesh treeMesh;
    [HideInInspector][SerializeField] private List<Material> treeMaterials = new List<Material>();
    [SerializeField] private List<Matrix4x4> allTransforms = new List<Matrix4x4>();

    private List<List<Matrix4x4>> batches = new List<List<Matrix4x4>>();
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
        batches.Clear();
        if (allTransforms == null) return;

        int batchIndex = 0;
        int count = 0;
        batches.Add(new List<Matrix4x4>());

        foreach (var t in allTransforms)
        {
            batches[batchIndex].Add(t);
            count++;
            if (count >= 1023)
            {
                batches.Add(new List<Matrix4x4>());
                batchIndex++;
                count = 0;
            }
        }
    }

    void Update()
    {
        if ((batches == null || batches.Count == 0) && allTransforms != null && allTransforms.Count > 0)
        {
            BuildBatches();
        }

        if (batches.Count > 0 && treeMesh != null && treeMaterials.Count > 0)
        {
            foreach (var batch in batches)
            {
                for (int i = 0; i < treeMesh.subMeshCount; i++)
                {
                    Material matToUse = (i < treeMaterials.Count) ? treeMaterials[i] : treeMaterials[0];
                    Graphics.DrawMeshInstanced(
                        treeMesh, 
                        i, 
                        matToUse, 
                        batch.ToArray(), 
                        batch.Count, 
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
