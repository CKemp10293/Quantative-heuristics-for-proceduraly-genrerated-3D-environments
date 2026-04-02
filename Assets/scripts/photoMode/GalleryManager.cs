using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
/// <summary>
/// The class that actually poplautes the gallery with the objects of Images
/// </summary>
public class GalleryManager : MonoBehaviour
{
    public static GalleryManager Instance; // Simple Singleton for easy access

    [Header("UI References")]
    public Transform gridContent; // The LayoutGroup container
    public GameObject thumbnailPrefab; // The UI Prefab with the ThumbnailUI.cs attached

    private List<GameObject> spawnedThumbnails = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void PopulateGallery()
    {
        // Fetch the lightweight data from the hard drive
        List<PhotoMetadata> allPhotos = PhotoDatabaseManager.LoadAllPhotoData();

        // Clear any old thumbnails just in case
        ClearGallery();

        // Spawn a UI element for every photo
        foreach (PhotoMetadata meta in allPhotos)
        {
            GameObject newThumb = Instantiate(thumbnailPrefab, gridContent);
            
            // Pass the data to the thumbnail so it knows exactly what PNG to look for
            ThumbnailUI uiScript = newThumb.GetComponent<ThumbnailUI>();
            if (uiScript != null)
            {
                uiScript.Initialize(meta);
                uiScript.LoadImageAsync();
            }

            spawnedThumbnails.Add(newThumb);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContent.GetComponent<RectTransform>());
    }

    public void ClearGallery()
    {
        // Destroy all UI elements and clear the list to free up RAM
        foreach (GameObject thumb in spawnedThumbnails)
        {
            Destroy(thumb);
        }
        spawnedThumbnails.Clear();
    }

    // Sweeps memory to keep RAM clear
    public void FlushMemory()
    {
        Resources.UnloadUnusedAssets(); 
    }
}