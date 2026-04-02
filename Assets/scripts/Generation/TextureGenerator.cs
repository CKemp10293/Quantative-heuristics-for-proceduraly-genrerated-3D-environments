using UnityEngine;

/// <summary>
/// Static utility class to convert mathematical map data (colors or heights) into 2D Textures.
/// </summary>
public static class TextureGenerator
{
    /// <summary>
    /// Creates a 2D Texture from a flat array of colors.
    /// </summary>
   public static Texture2D TextureFromColourMap(Color[] colourMap,int width,int height)
    {
        // Object initializer syntax for cleaner instantiation
        Texture2D texture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        
        texture.SetPixels(colourMap);
        texture.Apply();
        
        return texture;
    }

    /// <summary>
    /// Converts a 2D float array of height values (0 to 1) into a grayscale Texture2D.
    /// </summary>
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];

        for (int y = 0; y < height;y++){
            for (int x = 0; x < width;x++){
                // Calculate the 1D array index from 2D coordinates (y * width + x)
                colourMap[y * width + x] = Color.Lerp(Color.black,Color.white,heightMap[x,y]);

            }
        }
        return TextureFromColourMap(colourMap, width,height);
    }
}
