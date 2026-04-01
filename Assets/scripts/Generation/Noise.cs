using UnityEngine;
public static class Noise{
    public enum NormalisationMode{LOCAL,GLOBAL};

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight,float scale, int octaves,
                                            float persistance,float lacunarity,int seed,Vector2 offset,NormalisationMode normalisationMode){
        float[,] noiseMap = new float[mapWidth, mapHeight];

        float localMaxNoiseHeight = float.MinValue;
        float localMinNoiseHeight = float.MaxValue;
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapWidth / 2f;

        System.Random seedprng = new System.Random(seed);
        Vector2[] ocatveOffset = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude  = 1;
        float frequency = 1;
        for (int i = 0;i < octaves; i++)
        {
            float offsetX = seedprng.Next(-100000,100000) + offset.x;
            float offsetY = seedprng.Next(-100000,100000) - offset.y; // to allow for natural movement when scrolling down
            ocatveOffset[i] = new Vector2(offsetX,offsetY);

            maxPossibleHeight += amplitude;
            amplitude*= persistance;
        }


        if (scale <= 0){
            scale = 0.0001f;
        }


        for (int y = 0; y < mapHeight; y++){
            for (int x = 0; x < mapWidth; x++){

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;


                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + ocatveOffset[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + ocatveOffset[i].y) / scale * frequency;
                    // perlinValue in range -1,1
                    float perlinValue = Perlin.perlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > localMaxNoiseHeight)
                {
                    localMaxNoiseHeight = noiseHeight;
                } else if (noiseHeight < localMinNoiseHeight)
                {
                    localMinNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight ; y++)
        { 
            for (int x = 0; x < mapWidth; x++){
            if (normalisationMode == NormalisationMode.LOCAL)
            {
                    noiseMap[x,y] = Mathf.InverseLerp(localMinNoiseHeight,localMaxNoiseHeight,noiseMap[x,y]);
            }
            else
            {
                float normalisedHeight = noiseMap[x,y] + maxPossibleHeight;
                normalisedHeight /= 2f + maxPossibleHeight / 0.9f;
                noiseMap[x,y] = Mathf.Clamp(normalisedHeight,0,int.MaxValue);
            }
        }}
        return noiseMap;
    }
}
