using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;
using System.Collections;

public class PhotoModeController : MonoBehaviour
{
    public CanvasGroup viewFinderUI;
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
        // 1. Hide the Viewfinder UI from the math pixels
        viewFinderUI.alpha = 0f;

        // 2. Wait for the clean 3D frame (BEFORE the flash happens!)
        yield return new WaitForEndOfFrame();

        // 3. Capture the actual game view!
        Texture2D rawPhoto = ScreenCapture.CaptureScreenshotAsTexture();
        
        // 4. NOW trigger the sensory polish (Sound & Flash)
        if (cameraAudioSource != null && shutterSound != null)
        {
            cameraAudioSource.PlayOneShot(shutterSound);
        }
        if (screenFlashUI != null) screenFlashUI.alpha = 1f;

        // 5. Restore Viewfinder & Hand the data to the hard drive
        viewFinderUI.alpha = 1f;
        DiskIOWorker.SavePhotoToDisk(rawPhoto);
        Destroy(rawPhoto); // Prevent RAM memory leaks

        // 6. Fade the white flash out smoothly over 0.2 seconds
        if (screenFlashUI != null)
        {
            while (screenFlashUI.alpha > 0)
            {
                screenFlashUI.alpha -= Time.deltaTime * 10f;
                yield return null; // Wait for the next frame
            }
        }

    }


}
