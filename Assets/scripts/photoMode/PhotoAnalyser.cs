using UnityEngine;
using UnityEngine.Jobs;

/// <summary>
/// Utility class for calculating aesthetic and technical metrics from texture data.
/// </summary>
public static class PhotoAnalyser
{
    // Constants for luminance calculation (Rec. 601 standard)
    private const float LuminanceRedWeight = 0.299f;
    private const float LuminanceGreenWeight = 0.587f;
    private const float LuminanceBlueWeight = 0.114f;

    /// <summary>
    /// Analyzes a texture to generate aesthetic metrics including saturation, contrast, symmetry, and color dominance.
    /// </summary>
    /// <param name="smallTex">The downsampled texture to analyze.</param>
    /// <param name="id">The unique identifier for the photo.</param>
    /// <returns>A populated PhotoMetadata object containing scores from 0 to 10.</returns>
    public static PhotoMetadata AnalysePhoto(Texture2D smalltex, string id)
    {
        // Using GetPixels32 is highly optimized compared to GetPixels
        Color32[] pixels = smalltex.GetPixels32();
        int pixelCount = pixels.Length;

        float totalSaturation = 0f;
        float totalLuminance = 0f;

        // Hue buckets for color dominance calculation
        float[] hues = new float[360]; 

        // Cache pixel luminances to avoid recalculating during variance and symmetry passes
        float[] pixelLuminences = new float[pixelCount];

        // Pass 1: Gather Saturation, Luminance, and Hue Distributions
        for (int i = 0; i < pixelCount; i++)
        {
            // Implicitly casts Color32 (bytes) to Color (floats 0-1) once
            Color color = pixels[i];
            Color.RGBToHSV(color,out float h,out float s,out float v);

            totalSaturation += s;

            // relative luminance formula 
            float lum = (LuminanceRedWeight * color.r) + (LuminanceGreenWeight * color.g) + (LuminanceBlueWeight * color.b);
            pixelLuminences[i] = lum;
            totalLuminance += lum;

            int hueIndex = Mathf.Clamp((int)(h * 360f), 0, 359);
            hues[hueIndex]++;


        }

        // Pass 2: RMS Contrast Calculation
        float avgLuminance = totalLuminance / pixelCount;
        float varianceSum = 0f;

        for (int i = 0; i < pixelCount; i++)
        {
            float diff = pixelLuminences[i] - avgLuminance;
            varianceSum += (diff * diff);
        }
        float rmsContrast = Mathf.Sqrt(varianceSum / pixelCount);

        // Pass 3: Symmetry Calculation
        float symmetryDiffSum = 0f;
        int width = smalltex.width;
        int height = smalltex.height;
        int halfWidth = width / 2;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;
            int rowEnd = rowStart + width - 1;

            for (int x = 0; x < halfWidth; x++)
            {
                symmetryDiffSum += Mathf.Abs(pixelLuminences[rowStart + x] - pixelLuminences[rowEnd - x]);
            }
        }

        float avgSymmetryDiff = symmetryDiffSum / ((width / 2) * height);

        return GenerateScores(id, totalSaturation / pixelCount, rmsContrast, avgSymmetryDiff, hues, pixelCount);
    }

    /// <summary>
    /// Normalizes raw image data into a standardized 1-10 scoring system.
    /// </summary>
    private static PhotoMetadata GenerateScores(string id, float avgSat, float contrast, float symmetryDiff, float[] hues, int totalPixels)
    {
        PhotoMetadata meta = new PhotoMetadata { photoID = id };

        // Saturation (Raw is 0 to 1) -> Directly multiply by 10.
        meta.saturationScore = Mathf.Clamp(avgSat * 10f, 0f, 10f);

        // Contrast (RMS usually caps around 0.3 to 0.5 in normal photos)
        // Divide by 0.35f as an artificial "perfect contrast" ceiling, then scale to 10.
        meta.contrastScore = Mathf.Clamp((contrast / 0.35f) * 10f, 0f, 10f);

        // Symmetry (0 diff is perfect. A diff of 0.3 is very chaotic)
        // Inverse scale: 1.0 minus the normalized difference.
        meta.symmetryScore = Mathf.Clamp((1f - (symmetryDiff / 0.3f)) * 10f, 0f, 10f);

        // Color Dominance (What % of the image is the dominant color?)
        float maxHuePixels = 0;
        for (int i = 0; i < 360; i++) if (hues[i] > maxHuePixels) maxHuePixels = hues[i];
        float dominantPercentage = maxHuePixels / totalPixels;
        
        // If 15% of the image is the exact same hue bucket, that's highly dominant.
        meta.colorScore = Mathf.Clamp((dominantPercentage / 0.15f) * 10f, 0f, 10f);

        meta.totalScore = (meta.saturationScore + meta.contrastScore + meta.symmetryScore + meta.colorScore) / 4f;
        return meta;
    }
}