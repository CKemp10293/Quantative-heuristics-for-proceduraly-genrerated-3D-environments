using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
    private List<List<Matrix4x4>> batches = new List<List<Matrix4x4>>();

    private Mesh treeMesh;
    private Material treeMaterial;

    public void Initialise(List<Matrix4x4> allTransforms,Mesh mesh,Material material)
    {
        batches.Clear();
        treeMesh = mesh;
        treeMaterial = material;

        int batchIndex = 0;
        int counter = 0;
        batches.Add(new List<Matrix4x4>());
        
        foreach (var t in allTransforms)
        {
            batches[batchIndex].Add(t);
            counter++;
            if (counter >= 1024)
            {
                batches.Add(new List<Matrix4x4>());
                batchIndex++;
                counter = 0;
            }             
        }

    }

    void Update()
    {
        if(batches.Count > 0 && treeMesh != null && treeMaterial != null)
        {
            foreach(var batch in batches)
            {
                Graphics.DrawMeshInstanced(treeMesh,0,treeMaterial,batch.ToArray());
            }
        }
    }
}
