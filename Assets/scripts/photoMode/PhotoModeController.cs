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

    private void Start()
    {
        if (viewFinderUI != null)
        {
            SetViewFinderState(false);
        }

        if (playerController != null)
        {
            originalMoveSpeed = playerController.MoveSpeed;
            originalSprintSpeed = playerController.SprintSpeed;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (hasCameraUnlocked)
            {
                TogglePhotoMode();
            }
        }

        if(isPhotoModeActive && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(CapturePhotoRoutine());
        }
    }

    private void TogglePhotoMode()
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
        viewFinderUI.alpha = 0f;
        yield return new WaitForEndOfFrame();

        Texture2D rawPhoto = ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log($"Photo Captured! Memory Resolution: {rawPhoto.width}x{rawPhoto.height}");
        viewFinderUI.alpha = 0f;

        DiskIOWorker.SavePhotoToDisk(rawPhoto);

        Destroy(rawPhoto);

    }


}
