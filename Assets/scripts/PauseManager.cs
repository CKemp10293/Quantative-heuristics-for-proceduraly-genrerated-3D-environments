using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets; // Needed to communicate with the player's camera/cursor

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    public CanvasGroup pauseMenuUI;
    public CanvasGroup galleryPanelUI;

    [Header("State")]
    public bool isPaused = false;

    [Header("Dependencies")]
    [Tooltip("Drag the StarterAssetsInputs component here so we can control the cursor")]
    public StarterAssetsInputs playerInputs;

    private void Start()
    {
        // Ensure UI is hidden on start
        SetCanvasState(pauseMenuUI, false);
        SetCanvasState(galleryPanelUI, false);
    }

    private void Update()
    {
        // Listen for the Escape key to toggle pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freezes all physics and standard updates

        // Unlock the cursor so the player can click buttons
        playerInputs.cursorLocked = false;
        playerInputs.cursorInputForLook = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetCanvasState(pauseMenuUI, true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Restores physics

        // Relock the cursor for first-person gameplay
        playerInputs.cursorLocked = true;
        playerInputs.cursorInputForLook = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hide all pause-related UI
        SetCanvasState(pauseMenuUI, false);
        SetCanvasState(galleryPanelUI, false);
    }

    public void OpenGallery()
    {
        // Hide main pause menu, show gallery
        SetCanvasState(pauseMenuUI, false);
        SetCanvasState(galleryPanelUI, true);
        
        // Tell the GalleryManager to fetch the files!
        GalleryManager.Instance.PopulateGallery();
    }

    public void CloseGallery()
    {
        // Hide gallery, show main pause menu
        SetCanvasState(galleryPanelUI, false);
        SetCanvasState(pauseMenuUI, true);
        
        // Tell GalleryManager to flush RAM
        GalleryManager.Instance.ClearGallery();
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }

    private void SetCanvasState(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;
        canvas.alpha = isActive ? 1f : 0f;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;
    }
}