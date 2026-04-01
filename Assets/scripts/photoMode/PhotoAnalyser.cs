using UnityEngine;
using UnityEngine.Jobs;

public static class PhotoAnalyser
{
    public static PhotoMetadata AnalysePhoto(Texture2D smalltex, string id)
    {
        Color32[] pixels = smalltex.GetPixels32();
        int pixelCount = pixels.Length;

        float totalSaturation = 0f;
        float totalLuminance = 0f;
        float[] hues = new float[360]; // hue buckets
        float[] pixelLuminences = new float[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            Color color = pixels[i];
            Color.RGBToHSV(color,out float h,out float s,out float v);

            totalSaturation += s;

            // relative luminance formula 
            float lum = (0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b);
            pixelLuminences[i] = lum;
            totalLuminance += lum;

            int hueIndex = Mathf.Clamp(Mathf.RoundToInt(h * 359f), 0, 359);
            hues[hueIndex]++;


        }

        // contrast calcs
        float avgLuminance = totalLuminance / pixelCount;
        float varianceSum = 0f;
        for (int i = 0; i < pixelCount; i++)
        {
            float diff = pixelLuminences[i] - avgLuminance;
            varianceSum += (diff * diff);
        }
        float rmsContrast = Mathf.Sqrt(varianceSum / pixelCount);

        // Symmettry 
        float symmetryDiffSum = 0f;
        int width = smalltex.width;
        int height = smalltex.height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width / 2; x++)
            {
                int leftIndex = (y * width) + x;
                int rightIndex = (y * width) + (width - 1 - x);
                symmetryDiffSum += Mathf.Abs(pixelLuminences[leftIndex] - pixelLuminences[rightIndex]);
            }
        }
        float avgSymmetryDiff = symmetryDiffSum / ((width / 2) * height);

        return GenerateScores(id, totalSaturation / pixelCount, rmsContrast, avgSymmetryDiff, hues, pixelCount);
    }

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