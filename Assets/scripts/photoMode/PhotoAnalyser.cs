using System.Collections.Generic;
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
        int validColorPixels = 0;

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

            // Ignore grayscale, black, and blown-out white pixels for color metrics
            if (s > 0.15f && v > 0.15f && v < 0.95f)
            {
                int hueIndex = Mathf.Clamp((int)(h * 360f), 0, 359);
                hues[hueIndex]++;
                validColorPixels++;
            }
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

        List<int> dominantHues = GetDominantHues(hues, 3);
        float harmonyScoreRaw = CalculateHarmony(dominantHues);
        
        float maxHueCount = dominantHues.Count > 0 ? hues[dominantHues[0]] : 0;
        float variancePenaltyRaw = CalculateMonolithicPenalty(maxHueCount, validColorPixels);

        return GenerateScores(id, totalSaturation / pixelCount, rmsContrast, avgSymmetryDiff, harmonyScoreRaw, variancePenaltyRaw);
    }

    private static List<int> GetDominantHues(float[] hues,int topN)
    {
        List<int> peaks = new List<int>();
        float[] huesCopy = (float[])hues.Clone();

        for (int i = 0; i < topN; i++)
        {
           float maxVal = 0;
           int maxIndex = -1;
           for (int j = 0; j < 360; j++)
           {
            if (huesCopy[j] > maxVal)
            {
                maxVal = huesCopy[j];
                maxIndex = j;
            }
           }

           if(maxIndex == -1 || maxVal == 0) break;

           peaks.Add(maxIndex);

           // Wipe out the peak and its immediate neighbors (e.g., +/- 15 degrees) to find distinct next colors
            for (int w = -15; w <= 15; w++)
            {
                int clearIndex = (maxIndex + w + 360) % 360;
                huesCopy[clearIndex] = 0;
            } 
        }

        return peaks;
    }

    private static float CalculateHarmony(List<int> dominantHues)
    {
        if(dominantHues.Count < 2) return 0f;

        float harmonyScore = 0f;
        float tolerance = 15f;

        // Check pairs for relationships
        for (int i = 0; i < dominantHues.Count; i++)
        {
            for (int j = i + 1; j < dominantHues.Count; j++)
            {
                float angle = Mathf.Min(Mathf.Abs(dominantHues[i] - dominantHues[j]),
                              360 - Mathf.Abs(dominantHues[i] - dominantHues[j]));

                if (Mathf.Abs(angle - 180f) <= tolerance) harmonyScore += 1.0f; // Complementary
                else if (Mathf.Abs(angle - 120f) <= tolerance) harmonyScore += 0.8f; // Triadic
                else if (Mathf.Abs(angle - 30f) <= tolerance) harmonyScore += 0.6f;  // Analogous
            }
        }

        return Mathf.Clamp01(harmonyScore / 2f);
    }

    private static float CalculateMonolithicPenalty(float dominantColorPixelCount, int validColorPixels)
    {
        if (validColorPixels == 0) return 0f; // Grayscale image, no penalty

        float ratio = dominantColorPixelCount / validColorPixels;
        float threshold = 0.60f; // 60%

        if (ratio <= threshold) return 0f;

        // Creates a penalty from 0 to 1 based on how far past 60% it goes
        return Mathf.Clamp01((ratio - threshold) / (1f - threshold));
    }




    /// <summary>
    /// Normalizes raw image data into a standardized 1-10 scoring system.
    /// </summary>
    private static PhotoMetadata GenerateScores(string id, float avgSat, float contrast, float symmetryDiff,float harmonyRaw, float penaltyRaw)
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

        // Scale harmony to 0-10
        meta.harmonyScore = harmonyRaw * 10f; 

        // Weighting integration: Base aesthetics (75%) + Harmony (25%)
        float baseScore = (meta.saturationScore + meta.contrastScore + meta.symmetryScore) / 3f;
        float prePenaltyTotal = (baseScore * 0.75f) + (meta.harmonyScore * 0.25f);

        // Apply monolithic penalty (reduces total score by up to 30%)
        float maxPenaltyMultiplier = 0.30f; 
        meta.totalScore = prePenaltyTotal * (1f - (penaltyRaw * maxPenaltyMultiplier));

        return meta;
    }
}