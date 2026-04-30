using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StarterAssets; // Needed to communicate with the player's camera/cursor

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    public CanvasGroup pauseMenuUI;
    public CanvasGroup galleryPanelUI;
    public CanvasGroup SettingsUI;

    [Header("State")]
    public bool isPaused = false;

    [Header("Transition Settings")]
    [Min(0.01f)] public float panelTransitionDuration = 0.25f;
    [Min(0.7f)] public float panelHiddenScale = 0.96f;
    public string mainMenuSceneName = "MainMenu";

    [Header("Dependencies")]
    [Tooltip("Drag the StarterAssetsInputs component here so we can control the cursor")]
    public StarterAssetsInputs playerInputs;
    private Coroutine pauseTransitionCoroutine;
    private Coroutine galleryTransitionCoroutine;
    private Coroutine settingTransitionCoroutine;

 

    [Header("New Detail View")]
    public CanvasGroup photoDetailUI;
    private Coroutine detailTransitionCoroutine;

    private void Start()
    {
        // Ensure UI is hidden on start
        SetCanvasState(pauseMenuUI, false);
        SetCanvasState(galleryPanelUI, false);
        SetCanvasState(SettingsUI,false);
        SetCanvasState(photoDetailUI, false);
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

        SetPauseCanvasStateWithTransition(pauseMenuUI, true);
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
        SetPauseCanvasStateWithTransition(pauseMenuUI, false);
        SetGalleryCanvasStateWithTransition(galleryPanelUI, false);
    }

    public void Opensettings()
    {
        SetPauseCanvasStateWithTransition(pauseMenuUI,false);
        SetSettingCanvaStateWithTransition(SettingsUI,true);        
    }

    public void CloseSettings()
    {
        SetSettingCanvaStateWithTransition(SettingsUI,false);
        SetPauseCanvasStateWithTransition(pauseMenuUI,true);
    }

    public void OpenGallery()
    {
        // Hide main pause menu, show gallery
        SetPauseCanvasStateWithTransition(pauseMenuUI, false);
        SetGalleryCanvasStateWithTransition(galleryPanelUI, true);
        
        // Tell the GalleryManager to fetch the files!
        GalleryManager.Instance.PopulateGallery();
    }

    public void CloseGallery()
    {
        // Hide gallery, show main pause menu
        SetGalleryCanvasStateWithTransition(galleryPanelUI, false);
        SetPauseCanvasStateWithTransition(pauseMenuUI, true);
        
        // Tell GalleryManager to flush RAM
        GalleryManager.Instance.ClearGallery();
        GalleryManager.Instance.FlushMemory();
    }

    public void QuitGame()
    {
        // Ensure normal timescale/cursor state before changing scenes.
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerInputs != null)
        {
            playerInputs.cursorLocked = false;
            playerInputs.cursorInputForLook = false;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetCanvasState(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;
        canvas.alpha = isActive ? 1f : 0f;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;
        canvas.transform.localScale = isActive ? Vector3.one : Vector3.one * panelHiddenScale;
    }

    private void SetPauseCanvasStateWithTransition(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;

        if (pauseTransitionCoroutine != null)
        {
            StopCoroutine(pauseTransitionCoroutine);
        }

        pauseTransitionCoroutine = StartCoroutine(FadeCanvas(canvas, isActive, true));
    }

    private void SetGalleryCanvasStateWithTransition(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;

        if (galleryTransitionCoroutine != null)
        {
            StopCoroutine(galleryTransitionCoroutine);
        }

        galleryTransitionCoroutine = StartCoroutine(FadeCanvas(canvas, isActive, false));
    }

    private void SetSettingCanvaStateWithTransition(CanvasGroup canvas,bool isActive)
    {
        if(canvas == null) return;

        if (settingTransitionCoroutine != null)
        {
            StopCoroutine(galleryTransitionCoroutine);
        }

        galleryTransitionCoroutine = StartCoroutine(FadeCanvas(canvas,isActive,false));
    }

    public void ShowPhotoDetail()
    {
        // Fade IN the detail panel (leave gallery open in background)
        if (detailTransitionCoroutine != null) StopCoroutine(detailTransitionCoroutine);
        detailTransitionCoroutine = StartCoroutine(FadeCanvas(photoDetailUI, true, false));
    }

    public void ClosePhotoDetail()
    {
        // Fade OUT the detail panel
        if (detailTransitionCoroutine != null) StopCoroutine(detailTransitionCoroutine);
        detailTransitionCoroutine = StartCoroutine(FadeCanvas(photoDetailUI, false, false));
    
        // Flush the high-res texture from memory
        PhotoScoreManager.Instance.CleanUpMemory();
    }

    private System.Collections.IEnumerator FadeCanvas(CanvasGroup canvas, bool isActive, bool isPausePanel)
    {
        float duration = Mathf.Max(0.01f, panelTransitionDuration);
        float elapsed = 0f;
        float start = canvas.alpha;
        float target = isActive ? 1f : 0f;
        Vector3 startScale = canvas.transform.localScale;
        Vector3 targetScale = isActive ? Vector3.one : Vector3.one * panelHiddenScale;

        // Prevent interaction until we finish.
        canvas.interactable = false;
        canvas.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            canvas.alpha = Mathf.Lerp(start, target, t);
            canvas.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        canvas.alpha = target;
        canvas.transform.localScale = targetScale;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;

        if (isPausePanel)
        {
            pauseTransitionCoroutine = null;
        }
        else
        {
            galleryTransitionCoroutine = null;
        }
    }

    /// <summary>
    /// Logged runs showed gameplay canvases on Constant Pixel Size (not scaling with resolution).
    /// Converts those to Scale With Screen Size for consistent layout across resolutions.
    /// </summary>
    private void ApplyResponsiveCanvasScalersIfConstantPixelSize()
    {
        var scalers = UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
        for (int i = 0; i < scalers.Length; i++)
        {
            CanvasScaler s = scalers[i];
            if (s == null) continue;
            if (s.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize) continue;

            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.5f;
        }
    }
}