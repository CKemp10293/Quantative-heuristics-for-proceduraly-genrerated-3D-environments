using Unity.VisualScripting;
using UnityEngine;

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

[CreateAssetMenu(menuName ="Texture data")]
public class TextureData : ScriptableObject
{
    public TextureLayer[] layers;

    float savedMinHeight;
    float savedMaxHeight;

    public void UpdateMeshHeights(Material material,float minHeight,float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight;

        Shader.SetGlobalFloat("minHeight", minHeight);
        Shader.SetGlobalFloat("maxHeight", maxHeight);
    }

    public void ApplyToMat(Material material)
    {
        int layerCount = layers.Length;
        Vector4[] baseColours = new Vector4[layerCount];
        float[] baseStartHeights = new float[layerCount];
        float[] baseBlends = new float[layerCount];
        float[] baseTextureScales = new float[layerCount];

        // 2. Loop through our config and extract the data
        for (int i = 0; i < layerCount; i++)
        {
            baseColours[i] = layers[i].tint;
            baseStartHeights[i] = layers[i].startHeight;
            baseBlends[i] = layers[i].blendStrenght;
            baseTextureScales[i] = layers[i].textureScale;
        }

        // 3. Send the flat arrays to the Material
        Shader.SetGlobalFloat("layerCount", layerCount);
        Shader.SetGlobalVectorArray("baseColours", baseColours);
        Shader.SetGlobalFloatArray("baseStartHeights", baseStartHeights);
        Shader.SetGlobalFloatArray("baseBlends", baseBlends);
        Shader.SetGlobalFloatArray("baseTextureScales", baseTextureScales);
        Texture2DArray texturesArray = GenerateTextureArray(layers);

        material.SetTexture("baseTextures",texturesArray);

        UpdateMeshHeights(material,savedMinHeight,savedMaxHeight);
    }

    Texture2DArray GenerateTextureArray(TextureLayer[] layers)
    {
        Texture2D[] textures = new Texture2D[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            textures[i] = layers[i].texture;
        }

        int tSize = textures[0].width;

        TextureFormat format = TextureFormat.RGBA32;

        Texture2DArray texture2DArray = new Texture2DArray(tSize,tSize,textures.Length,format,true);

        for (int i = 0; i < textures.Length; i++)
        {
            texture2DArray.SetPixels(textures[i].GetPixels(),i);
        }

        texture2DArray.Apply();
        return texture2DArray;
    }
}
