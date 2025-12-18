using Unity.VisualScripting;
using UnityEngine;

public static class MeshGenerator 
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap,float heightMultiplier,AnimationCurve heightCurve,int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int simplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;
        int vertciesPerLine = (width - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(vertciesPerLine,vertciesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += simplificationIncrement)
        {
            for (int x = 0; x < width; x += simplificationIncrement)
            {
                // for centering purposes
                meshData.vertices[vertexIndex] = new Vector3 (topLeftX + x ,heightCurve.Evaluate(heightMap[x,y]) * heightMultiplier ,topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / ((float)width - 1), y / (float)height);

                // making sure we ignore everything on the very rightmost edge and the very bottom of heightmap
                if (x < width - 1 && y < height -1)
                {
                    meshData.addTriangle(vertexIndex,vertexIndex + vertciesPerLine + 1, vertexIndex + vertciesPerLine);
                    meshData.addTriangle(vertexIndex + vertciesPerLine + 1,vertexIndex,vertexIndex + 1);
                }

                vertexIndex++;
            }
        }
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs; // For adding textures to our mesh. 
    int trianlgeIndex;

    public MeshData(int meshWidth,int meshHight)
    {
        vertices = new Vector3[meshWidth * meshHight];
        uvs = new Vector2[meshWidth * meshHight];
        triangles = new int[(meshWidth-1) * (meshHight-1) * 6];
    }

    public void addTriangle(int a,int b,int c)
    {
        triangles[trianlgeIndex] = a;
        triangles[trianlgeIndex + 1] = b;
        triangles[trianlgeIndex + 2] = c;
        trianlgeIndex +=3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
