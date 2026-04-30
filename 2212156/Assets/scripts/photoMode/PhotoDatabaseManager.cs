using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class PhotoDatabaseManager
{
    // The exact path to our JSON metadata file
    private static string GetDatabasePath()
    {
        return Path.Combine(Application.persistentDataPath, "Photos", "photos_metadata.json");
    }

    public static List<PhotoMetadata> LoadAllPhotoData()
    {
        string path = GetDatabasePath();
        
        // Fact Check: If the player hasn't taken any photos yet, the file won't exist.
        if (!File.Exists(path))
        {
            Debug.Log("No photo database found. Returning empty list.");
            return new List<PhotoMetadata>();
        }

        // Read the text from the hard drive
        string json = File.ReadAllText(path);
        
        // Convert the JSON text back into a C# object
        PhotoDatabase database = JsonUtility.FromJson<PhotoDatabase>(json);
        
        return database.allPhotos;
    }
}