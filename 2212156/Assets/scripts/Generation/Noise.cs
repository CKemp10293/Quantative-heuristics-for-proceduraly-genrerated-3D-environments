using UnityEngine;

/// <summary>
/// Static utility class for generating procedural Perlin noise maps.
/// </summary>
public static class Noise{
    public enum NormalisationMode{LOCAL,GLOBAL};

    /// <summary>
    /// Generates a 2D float array representing a noise map.
    /// </summary>
    /// <param name="mapWidth">Width of the map array.</param>
    /// <param name="mapHeight">Height of the map array.</param>
    /// <param name="scale">Zoom level of the noise.</param>
    /// <param name="octaves">Number of noise layers combined.</param>
    /// <param name="persistence">How much amplitude decreases per octave.</param>
    /// <param name="lacunarity">How much frequency increases per octave.</param>
    /// <param name="seed">Seed for the random number generator.</param>
    /// <param name="offset">Manual offset for scrolling the map.</param>
    /// <param name="normalisationMode">Determines if heights are normalized relative to this specific chunk (LOCAL) or the theoretical global max (GLOBAL).</param>
    /// <returns>A 2D array of normalized noise values.</returns>
    public static float[,] GenerateNoiseMap(
    int mapWidth,
    int mapHeight,
    float scale,
    int octaves,
    float persistance,
    float lacunarity,
    int seed,
    Vector2 offset,
    NormalisationMode normalisationMode){
        float[,] noiseMap = new float[mapWidth, mapHeight];

        float localMaxNoiseHeight = float.MinValue;
        float localMinNoiseHeight = float.MaxValue;
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        System.Random seedprng = new System.Random(seed);
        Vector2[] ocatveOffset = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude  = 1;
        float frequency = 1;
        for (int i = 0;i < octaves; i++)
        {
            float offsetX = seedprng.Next(-100000,100000) + offset.x;
            float offsetY = seedprng.Next(-100000,100000) - offset.y; // Inverted to allow natural movement when scrolling down
            ocatveOffset[i] = new Vector2(offsetX,offsetY);

            maxPossibleHeight += amplitude;
            amplitude*= persistance;
        }

        // Prevent division by zero
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
                
                // Track the local min/max for local normalization
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

        if (normalisationMode == NormalisationMode.LOCAL)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(localMinNoiseHeight, localMaxNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        else // NormalisationMode.GLOBAL
        {
            // Pre-calculate the divisor to save floating point math inside the tight loop
            float globalDivisor = 2f + (maxPossibleHeight / 0.9f);
            
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float normalisedHeight = noiseMap[x, y] + maxPossibleHeight;
                    normalisedHeight /= globalDivisor;
                    noiseMap[x, y] = Mathf.Clamp(normalisedHeight, 0, int.MaxValue);
                }
            }
        }
        return noiseMap;
    }
}
