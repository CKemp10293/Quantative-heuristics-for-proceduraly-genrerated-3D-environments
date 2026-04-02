using UnityEngine;
/// <summary>
/// Class for downsampling so we can actually work on the images
/// </summary>
public static class ImageUtility
{
    public static Texture2D DownsampleTexture(Texture2D source,int targetWidth, int targetHeight)
    {
        // Create a temporary low-res canvas on the GPU
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth,targetHeight,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);

        // Instruct the GPU to stretch/squash the original image onto this small canvas
        Graphics.Blit(source,rt);

        // Store the current active render texture to restore it later
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a new blank Texture2D and read the GPU canvas pixels into it
        Texture2D downsampledTex = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        downsampledTex.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        downsampledTex.Apply();

        // Clean up the GPU memory instantly
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(rt);

        return downsampledTex;
    }
}