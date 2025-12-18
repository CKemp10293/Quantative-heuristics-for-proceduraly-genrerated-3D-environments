using System.IO.IsolatedStorage;
using Unity.Mathematics;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
public class Noise{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight,float scale, int octaves,float persistance,float lacunarity,int seed){
        float[,] noiseMap = new float[mapWidth, mapHeight];

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapWidth / 2f;

        System.Random seedprng = new System.Random(seed);
        Vector2[] ocatveOffset = new Vector2[octaves];
        for (int i = 0;i < octaves; i++)
        {
            float offsetX = seedprng.Next(-100000,100000);
            float offsetY = seedprng.Next(-100000,100000);
            ocatveOffset[i] = new Vector2(offsetX,offsetY);
        }


        if (scale <= 0){
            scale = 0.0001f;
        }


        for (int y = 0; y < mapHeight; y++){
            for (int x = 0; x < mapWidth; x++){

                float amplitude  = 1;
                float frequency = 1;
                float noiseHeight = 0;


                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + ocatveOffset[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + ocatveOffset[i].y;
                    // perlinValue in range -1,1
                    float perlinValue = Perlin.perlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                } else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight ; y++)
       { for (int x = 0; x < mapWidth; x++){
            noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight,maxNoiseHeight,noiseMap[x,y]);
        }}
        return noiseMap;
    }
}
