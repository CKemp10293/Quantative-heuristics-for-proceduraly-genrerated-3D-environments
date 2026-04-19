using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks;

public class PhotoScoreManager : MonoBehaviour
{
   public static PhotoScoreManager Instance;

    [Header("UI References")]
    public RawImage bigPhotoDisplay;
    public TMP_Text totalScoreText;
    public TMP_Text saturationText;
    public TMP_Text contrastText;
    public TMP_Text symmetryText;
    public TMP_Text harmonyText;
    public TMP_Text penaltyText;

    private Texture2D currentHighResTexture;

    private void Awake()
    {
        Instance = this;
    }

    public async void OpenDetailView(PhotoMetadata data)
    {
        // 1. Map the text data
        totalScoreText.text = $"Total Score: {data.totalScore:F1} / 10";
        
        saturationText.text = $"Saturation: {data.saturationScore:F1}";
        contrastText.text = $"Contrast: {data.contrastScore:F1}";
        symmetryText.text = $"Symmetry: {data.symmetryScore:F1}";
        harmonyText.text = $"Harmony: {data.harmonyScore:F1}";
        
        // Show penalty as a percentage for better readability
        penaltyText.text = $"Monolithic Penalty: -{(data.variancePenalty * 100f):F0}%";

        // 2. Load the high-res image asynchronously
        string filePath = Path.Combine(Application.persistentDataPath, "Photos", data.photoID + ".png");
        
        if (File.Exists(filePath))
        {
            byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));
            
            // Clean up the old texture to prevent VRAM memory leaks!
            if (currentHighResTexture != null) Destroy(currentHighResTexture);

            currentHighResTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            currentHighResTexture.LoadImage(fileData);
            bigPhotoDisplay.texture = currentHighResTexture;
        }

        // 3. Tell PauseManager to transition the UI
        FindAnyObjectByType<PauseManager>().ShowPhotoDetail();
    }

    public void CleanUpMemory()
    {
        if (currentHighResTexture != null) Destroy(currentHighResTexture);
        bigPhotoDisplay.texture = null;
    }
}
