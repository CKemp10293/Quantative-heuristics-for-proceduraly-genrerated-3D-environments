using UnityEngine;

/// <summary>
/// Represents a single biome/terrain texture layer and its blending properties.
/// </summary>
[System.Serializable]
public class TextureLayer
{
  [Header("Appearance")]
  public Texture2D texture;
  public Color tint = Color.white;
  public float textureScale = 10f;

  [Header("Height placement")]
  [Range(0,1)]
  public float startHeight;

  [Range(0,1)]
  public float blendStrenght;
}

/// <summary>
/// ScriptableObject containing configuration for all texture layers applied to the procedural terrain.
/// Extracts and sends this data to the terrain shader.
/// </summary>
[CreateAssetMenu(menuName ="Texture data")]
public class TextureData : ScriptableObject
{
    public TextureLayer[] layers;

    float savedMinHeight;
    float savedMaxHeight;

    /// <summary>
    /// Sends the global minimum and maximum height thresholds to the shader.
    /// </summary>
    public void UpdateMeshHeights(Material material,float minHeight,float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight;

        Shader.SetGlobalFloat("minHeight", minHeight);
        Shader.SetGlobalFloat("maxHeight", maxHeight);
    }

    /// <summary>
    /// Parses the layer configurations into flat arrays and uploads them to the GPU.
    /// </summary>
    public void ApplyToMat(Material material)
    {
        int layerCount = layers.Length;
        Vector4[] baseColours = new Vector4[layerCount];
        float[] baseStartHeights = new float[layerCount];
        float[] baseBlends = new float[layerCount];
        float[] baseTextureScales = new float[layerCount];

        // Loop through our config and extract the data
        for (int i = 0; i < layerCount; i++)
        {
            baseColours[i] = layers[i].tint;
            baseStartHeights[i] = layers[i].startHeight;
            baseBlends[i] = layers[i].blendStrenght;
            baseTextureScales[i] = layers[i].textureScale;
        }

        // Send the flat arrays to the global shader
        Shader.SetGlobalFloat("layerCount", layerCount);
        Shader.SetGlobalVectorArray("baseColours", baseColours);
        Shader.SetGlobalFloatArray("baseStartHeights", baseStartHeights);
        Shader.SetGlobalFloatArray("baseBlends", baseBlends);
        Shader.SetGlobalFloatArray("baseTextureScales", baseTextureScales);
        Texture2DArray texturesArray = GenerateTextureArray(layers);

        material.SetTexture("baseTextures",texturesArray);

        UpdateMeshHeights(material,savedMinHeight,savedMaxHeight);
    }

    /// <summary>
    /// Packs individual Texture2D objects into a single Texture2DArray for the shader.
    /// </summary>
    Texture2DArray GenerateTextureArray(TextureLayer[] layers)
    {
        if (layers == null || layers.Length == 0 || layers[0].texture == null) return null;

        int tSize = layers[0].texture.width;
        TextureFormat format = TextureFormat.RGBA32;

        Texture2DArray texture2DArray = new Texture2DArray(tSize, tSize, layers.Length, format, true);

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].texture != null)
            {
                texture2DArray.SetPixels(layers[i].texture.GetPixels(), i);
            }
        }

        texture2DArray.Apply();
        return texture2DArray;
    }
}
