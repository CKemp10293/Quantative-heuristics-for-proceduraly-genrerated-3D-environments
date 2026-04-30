using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Static utility class responsible for mathematically generating terrain mesh geometry
/// and calculating seamless edge normals via a bordered grid system.
/// </summary>
public static class MeshGenerator 
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap,float heightMultiplier,AnimationCurve _heightCurve,int levelOfDetail)
    {
        // Calculate LOD skip increments
        int simplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;

        // Prevent race conditions: AnimationCurves are not thread-safe to evaluate if modified concurrently. 
        // Instantiating a new one guarantees thread safety.
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * simplificationIncrement;
        int meshSizeUnsimplified = borderedSize -2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int vertciesPerLine = (meshSize - 1) / simplificationIncrement + 1;

        MeshData meshData = new MeshData(vertciesPerLine);
        int[,] vertexIndiciesMap = new int[borderedSize,borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1; // Negative indices are used to identify border vertices

        // Map out the indices grid to separate real mesh vertices from invisible border vertices
        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize -1;
                if (isBorderVertex)
                {
                    vertexIndiciesMap[x,y] = borderVertexIndex;
                    borderVertexIndex--;
                } else
                {
                    vertexIndiciesMap[x,y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // Generate the vertex geometry and triangles
        for (int y = 0; y < borderedSize; y += simplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += simplificationIncrement)
            {
                int vertexIndex = vertexIndiciesMap[x,y];

                // Calculate physical percent based on the simplified mesh scale
                Vector2 percent = new Vector2(
                (x - simplificationIncrement) / (float)(meshSize - 1), 
                (y - simplificationIncrement) / (float)(meshSize - 1));
                
                // Map the percent to the exact fractional indices of the heightmap array
                float exactX = Mathf.Clamp(Mathf.Lerp(1, borderedSize - 2, percent.x), 0, borderedSize - 1);
                float exactY = Mathf.Clamp(Mathf.Lerp(1, borderedSize - 2, percent.y), 0, borderedSize - 1);

                // Bilinear Interpolation (Smoothly mix the 4 closest heightmap pixels so skipped LOD vertices are perfectly accurate)
                int x1 = Mathf.FloorToInt(exactX);
                int y1 = Mathf.FloorToInt(exactY);
                int x2 = Mathf.Min(x1 + 1, borderedSize - 1);
                int y2 = Mathf.Min(y1 + 1, borderedSize - 1);

                float h1 = Mathf.Lerp(heightMap[x1, y1], heightMap[x2, y1], exactX - x1);
                float h2 = Mathf.Lerp(heightMap[x1, y2], heightMap[x2, y2], exactX - x1);
                float preciseHeight = Mathf.Lerp(h1, h2, exactY - y1);

                // Apply the perfectly smoothed height
                float height = heightCurve.Evaluate(preciseHeight) * heightMultiplier;
                Vector3 vertexPostion = new Vector3 (
                topLeftX + percent.x * (meshSizeUnsimplified - 1),
                height, topLeftZ - percent.y * (meshSizeUnsimplified - 1));
                
                meshData.addVertex(vertexPostion,percent,vertexIndex);

                // Ignore the rightmost and bottom edges when generating triangle faces
                if (x < borderedSize - 1 && y < borderedSize -1)
                {
                    int a = vertexIndiciesMap[x,y];
                    int b = vertexIndiciesMap[x + simplificationIncrement,y];
                    int c = vertexIndiciesMap[x,y + simplificationIncrement];
                    int d = vertexIndiciesMap[x + simplificationIncrement,y + simplificationIncrement];
                    meshData.addTriangle(a,d,c);
                    meshData.addTriangle(d,a,b);
                }
            }
        }
        meshData.bakeNormals();
        return meshData;
    }
}

/// <summary>
/// Data container for mesh geometry. Segregates border geometry (used only for normal calculations)
/// from the actual rendered mesh geometry.
/// </summary>
public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    Vector3[] borderVertcies;
    int[] borderTriangles;

    Vector3[] bakedNormals;
    int trianlgeIndex;
    int borderTriangleIndex;

    public MeshData(int verticesPerLine)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine-1) * (verticesPerLine-1) * 6];

        borderVertcies = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    /// <summary>
    /// Adds a vertex. If the index is negative, it routes it to the invisible border arrays.
    /// </summary>
    public void addVertex(Vector3 vertexPostion, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0 )
        {
            // Convert the negative border index into a standard 0-based array index
            borderVertcies[-vertexIndex - 1] = vertexPostion;
        }
        else
        {
            vertices[vertexIndex] = vertexPostion;
            uvs[vertexIndex] = uv;
        }
    }

    /// <summary>
    /// Constructs a triangle face. If any vertex index is negative, the triangle belongs to the border.
    /// </summary>
    public void addTriangle(int a,int b,int c)
    {   if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex +=3;
        }else
        {
            triangles[trianlgeIndex] = a;
            triangles[trianlgeIndex + 1] = b;
            triangles[trianlgeIndex + 2] = c;
            trianlgeIndex +=3;
        }
        
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];

        // Process core mesh triangles
        int triangleCount = triangles.Length / 3; 
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = CalcSurfaceNormalsFromInicies(vertexIndexA,vertexIndexB,vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        // Process border triangles to ensure seamless lighting at chunk edges
        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = CalcSurfaceNormalsFromInicies(vertexIndexA,vertexIndexB,vertexIndexC);

            // Only apply the border normal influence back to the real mesh vertices
            if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0 ) vertexNormals[vertexIndexC] += triangleNormal;
            
            
        }

        // Normalize all calculated vectors
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;

    }

    Vector3 CalcSurfaceNormalsFromInicies(int indexA,int indexB,int indexC)
    {
        Vector3 pointA =(indexA < 0) ? borderVertcies[-indexA-1] : vertices [indexA];
        Vector3 pointB =(indexB < 0) ? borderVertcies[-indexB-1] : vertices [indexB];
        Vector3 pointC =(indexC < 0) ? borderVertcies[-indexC-1] : vertices [indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB,sideAC);
    }

    public void bakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    /// <summary>
    /// Compiles the raw data into a Unity Mesh object. 
    /// Note: Must be called on the Main Thread.
    /// </summary>
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();

        // 16-bit indices will overflow and crash the mesh. This safely upgrades the format to 32-bit.
        if (vertices.Length > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = bakedNormals;

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }
}
