using System;
using System.IO;
using UnityEngine;
public class DiskIOWorker
{
    public static void SavePhotoToDisk(Texture2D photo)
    {
        // Define the Master Path
        // Application.persistentDataPath automatically finds the correct AppData folder on Windows, Mac, or Linux.
        string folderPath = Path.Combine(Application.persistentDataPath, "Photos");

        // Ensure the Directory Exists
        // If this is the first time taking a photo, the folder won't exist yet.
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log("📁 Created new Photos directory at: " + folderPath);
        }

        // Generate a Unique Filename
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string filePath = Path.Combine(folderPath, $"Photo_{timestamp}.png");

        // Encode the Raw Pixels
        // This converts the Texture2D matrix into a lossless, standardized PNG byte array.
        byte[] imageBytes = photo.EncodeToPNG();

        // Write to Disk
        File.WriteAllBytes(filePath, imageBytes);

        Debug.Log($"Photo successfully saved to disk: {filePath}");
    }
}
