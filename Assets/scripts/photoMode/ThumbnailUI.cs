using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using TMPro;
/// <summary>
/// A class to actually allow us to see every image we have taken inside a scroll view 
/// </summary>
public class ThumbnailUI : MonoBehaviour
{
    public RawImage photoDisplay; // Use RawImage for Texture2D
    public TMP_Text scoreText;
    
    private string filePath;
    private Texture2D loadedTexture;
    private bool isLoaded = false;

    // Called by the GalleryManager when spawning the prefab
    public void Initialize(PhotoMetadata meta)
    {
        filePath = Path.Combine(Application.persistentDataPath, "Photos", meta.photoID + ".png");
        scoreText.text = $"Score: {meta.totalScore:F1}/10";
        photoDisplay.color = Color.black; // Show black box while unloaded
    }

    // Call this when the ScrollRect detects this prefab has entered the screen
    public async void LoadImageAsync()
    {
        if (isLoaded || !File.Exists(filePath)) return;
        isLoaded = true; // Set early to prevent double-calls

        // Read bytes on a background CPU thread (Zero lag to the game)
        byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));

        // We are back on the main thread. Apply the texture.
        // (Unity requires Texture creation to happen on the main thread)
        loadedTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        loadedTexture.LoadImage(fileData);
    
        photoDisplay.texture = loadedTexture;
        photoDisplay.color = Color.white;
    }

    // Call this when the prefab scrolls off-screen or the gallery closes
    public void UnloadImage()
    {
        if (!isLoaded) return;
        
        photoDisplay.texture = null;
        photoDisplay.color = Color.black;

        if (loadedTexture != null) Destroy(loadedTexture);
        isLoaded = false;
    }
}