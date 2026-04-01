using System.Collections.Generic;
using UnityEngine;
using System.Linq; 

public class MainMenuGallery : MonoBehaviour
{
    [Header("High Score UI")]
    [Tooltip("Drag the 3 UI templates here for 1st, 2nd, and 3rd place")]
    public ThumbnailUI[] topThreeDisplays = new ThumbnailUI[3];

    private void Start()
    {
        LoadHighScores();
    }

    public void LoadHighScores()
    {
        // Fetch the lightweight data
        List<PhotoMetadata> allPhotos = PhotoDatabaseManager.LoadAllPhotoData();

        if (allPhotos == null || allPhotos.Count == 0)
        {
            Debug.Log("No photos found. High score podium remains empty.");
            return;
        }

        // OrderByDescending sorts the list from highest score to lowest.
        // Take(3) grabs only the top 3 results.
        List<PhotoMetadata> topPhotos = allPhotos.OrderByDescending(p => p.totalScore).Take(3).ToList();

        Debug.Log($"Found {topPhotos.Count} high scores. Attempting to populate {topThreeDisplays.Length} UI displays.");

        // Inject the data into the UI
        for (int i = 0; i < topPhotos.Count; i++)
        {
            // Ensure we don't try to fill a display that doesn't exist
            if (i < topThreeDisplays.Length && topThreeDisplays[i] != null)
            {
                topThreeDisplays[i].Initialize(topPhotos[i]);
                
                // Force the image to load for the podium
                topThreeDisplays[i].LoadImageAsync(); 
            }
        }
    }
}
