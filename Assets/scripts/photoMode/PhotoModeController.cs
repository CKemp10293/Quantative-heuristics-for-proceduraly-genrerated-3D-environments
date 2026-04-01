using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;
using System.Collections;

public class PhotoModeController : MonoBehaviour
{
    public CanvasGroup viewFinderUI;
    public CanvasGroup invUI;
    public FirstPersonController playerController;

    public bool isPhotoModeActive = false;
    public bool hasCameraUnlocked = true;

    private float originalMoveSpeed;
    private float originalSprintSpeed;

    public CanvasGroup screenFlashUI;
    public AudioSource cameraAudioSource;
    public AudioClip shutterSound;

    private void Start()
    {
        if (viewFinderUI != null) SetViewFinderState(false);
        
        if (screenFlashUI != null) screenFlashUI.alpha = 0f;

        if (playerController != null)
        {
            originalMoveSpeed = playerController.MoveSpeed;
            originalSprintSpeed = playerController.SprintSpeed;
        }
    }

    private void Update()
    {
        if(isPhotoModeActive && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(CapturePhotoRoutine());
        }
    }

    public void TogglePhotoMode()
    {
        isPhotoModeActive = !isPhotoModeActive;
        SetViewFinderState(isPhotoModeActive);

        if (isPhotoModeActive)
        {
            playerController.MoveSpeed = 0;
            playerController.SprintSpeed = 0;
            Debug.Log("Photo mode on");
        }
        else
        {
            playerController.MoveSpeed = originalMoveSpeed;
            playerController.SprintSpeed = originalSprintSpeed;
            Debug.Log("Photo mode off");
        }
    }

    private void SetViewFinderState(bool isActive)
    {
        if(viewFinderUI == null) return;

        viewFinderUI.alpha = isActive ? 1f : 0f;
        viewFinderUI.interactable = isActive;
        viewFinderUI.blocksRaycasts = isActive;
    }

    IEnumerator CapturePhotoRoutine()
    {
        // Hide the Viewfinder UI from the math pixels
        viewFinderUI.alpha = 0f;
        invUI.alpha = 0f;

        // Wait for the clean 3D frame
        yield return new WaitForEndOfFrame();

        // Capture the actual game view!
        Texture2D rawPhoto = ScreenCapture.CaptureScreenshotAsTexture();
        
        
        // Create the unique ID right now so both the image and JSON share it
        string photoID = "Photo_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        
        // Downsample the 1080p image to 128x128 for lightning-fast math processing
        Texture2D smallPhoto = ImageUtility.DownsampleTexture(rawPhoto, 128, 128);
        
        // Pass the small photo to your math script to get the 0-10 scores
        PhotoMetadata metadata = PhotoAnalyser.AnalysePhoto(smallPhoto, photoID);
        

        // Sound & Flash
        if (cameraAudioSource != null && shutterSound != null)
        {
            cameraAudioSource.PlayOneShot(shutterSound);
        }
        if (screenFlashUI != null) screenFlashUI.alpha = 1f;

        // Restore Viewfinder & Hand BOTH the data and the image to the hard drive
        viewFinderUI.alpha = 1f;
        invUI.alpha = 1f;
        DiskIOWorker.SavePhotoAndMetadata(rawPhoto, metadata);
        
        // Prevent RAM memory leaks by destroying BOTH textures
        Destroy(rawPhoto); 
        Destroy(smallPhoto);

        // Fade the white flash out smoothly over 0.2 seconds
        if (screenFlashUI != null)
        {
            while (screenFlashUI.alpha > 0)
            {
                screenFlashUI.alpha -= Time.deltaTime * 10f;
                yield return null; 
            }
        }

    }


}
