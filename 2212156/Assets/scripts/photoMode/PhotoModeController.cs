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

    [Header("Photo Mode UI Styling")]
    [Min(0.01f)] public float uiTransitionDuration = 0.22f;
    [Min(0.7f)] public float hiddenScale = 0.96f;
    public CanvasGroup thirdsGridUI;
    public bool gridEnabledByDefault = true;
    [Range(0.1f, 1f)] public float gridAlpha = 0.35f;
    public Key toggleGridKey = Key.G;

    [Header("Shutter Feel")]
    public RectTransform focusReticle;
    [Range(1f, 1.5f)] public float reticlePulseScale = 1.14f;
    [Min(0.01f)] public float reticlePulseDuration = 0.12f;
    [Min(0f)] public float cameraKickDistance = 0.04f;
    [Min(0.01f)] public float cameraKickDuration = 0.1f;

    private bool isCapturingPhoto = false;
    private bool isGridVisible = true;
    private Coroutine viewfinderTransitionCoroutine;
    private Coroutine inventoryTransitionCoroutine;
    private Coroutine gridTransitionCoroutine;
    private Coroutine reticlePulseCoroutine;
    private Coroutine cameraKickCoroutine;

    private void Start()
    {
        if (viewFinderUI != null) SetCanvasStateImmediate(viewFinderUI, false);
        if (invUI != null) SetCanvasStateImmediate(invUI, true);
        
        if (screenFlashUI != null) screenFlashUI.alpha = 0f;
        if (focusReticle != null) focusReticle.localScale = Vector3.one;
        if (thirdsGridUI != null)
        {
            isGridVisible = gridEnabledByDefault;
            SetGridStateImmediate(isGridVisible && isPhotoModeActive);
        }

        if (playerController != null)
        {
            originalMoveSpeed = playerController.MoveSpeed;
            originalSprintSpeed = playerController.SprintSpeed;
        }
    }

    private void Update()
    {
        if(isPhotoModeActive && !isCapturingPhoto && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(CapturePhotoRoutine());
        }

        if (isPhotoModeActive && Keyboard.current != null && Keyboard.current[toggleGridKey].wasPressedThisFrame)
        {
            ToggleGrid();
        }
    }

    public void TogglePhotoMode()
    {
        isPhotoModeActive = !isPhotoModeActive;
        SetViewFinderState(isPhotoModeActive);
        SetGridStateWithTransition(isPhotoModeActive && isGridVisible);

        if (isPhotoModeActive)
        {
            if (playerController != null)
            {
                playerController.MoveSpeed = 0;
                playerController.SprintSpeed = 0;
            }
            Debug.Log("Photo mode on");
        }
        else
        {
            if (playerController != null)
            {
                playerController.MoveSpeed = originalMoveSpeed;
                playerController.SprintSpeed = originalSprintSpeed;
            }
            Debug.Log("Photo mode off");
        }
    }

    private void SetViewFinderState(bool isActive)
    {
        SetViewfinderCanvasStateWithTransition(viewFinderUI, isActive);
        SetInventoryCanvasStateWithTransition(invUI, !isActive);
    }

    private void ToggleGrid()
    {
        isGridVisible = !isGridVisible;
        SetGridStateWithTransition(isPhotoModeActive && isGridVisible);
    }

    private void SetGridStateWithTransition(bool isActive)
    {
        if (thirdsGridUI == null) return;
        SetGridCanvasStateWithTransition(thirdsGridUI, isActive, gridAlpha);
    }

    private void SetCanvasStateImmediate(CanvasGroup canvas, bool isActive, float targetAlpha = 1f)
    {
        if (canvas == null) return;
        canvas.alpha = isActive ? targetAlpha : 0f;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;
        canvas.transform.localScale = isActive ? Vector3.one : Vector3.one * hiddenScale;
    }

    private void SetGridStateImmediate(bool isActive)
    {
        SetCanvasStateImmediate(thirdsGridUI, isActive, gridAlpha);
    }

    private void SetViewfinderCanvasStateWithTransition(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;
        if (viewfinderTransitionCoroutine != null) StopCoroutine(viewfinderTransitionCoroutine);
        viewfinderTransitionCoroutine = StartCoroutine(AnimateCanvasState(canvas, isActive, 1f, true, false, false));
    }

    private void SetInventoryCanvasStateWithTransition(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;
        if (inventoryTransitionCoroutine != null) StopCoroutine(inventoryTransitionCoroutine);
        inventoryTransitionCoroutine = StartCoroutine(AnimateCanvasState(canvas, isActive, 1f, false, true, false));
    }

    private void SetGridCanvasStateWithTransition(CanvasGroup canvas, bool isActive, float activeAlpha)
    {
        if (canvas == null) return;
        if (gridTransitionCoroutine != null) StopCoroutine(gridTransitionCoroutine);
        gridTransitionCoroutine = StartCoroutine(AnimateCanvasState(canvas, isActive, activeAlpha, false, false, true));
    }

    private IEnumerator AnimateCanvasState(CanvasGroup canvas, bool isActive, float activeAlpha, bool isViewfinder, bool isInventory, bool isGrid)
    {
        float duration = Mathf.Max(0.01f, uiTransitionDuration);
        float elapsed = 0f;
        float startAlpha = canvas.alpha;
        float targetAlpha = isActive ? activeAlpha : 0f;
        Vector3 startScale = canvas.transform.localScale;
        Vector3 targetScale = isActive ? Vector3.one : Vector3.one * hiddenScale;

        canvas.interactable = false;
        canvas.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            canvas.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        canvas.alpha = targetAlpha;
        canvas.transform.localScale = targetScale;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;

        if (isViewfinder) viewfinderTransitionCoroutine = null;
        if (isInventory) inventoryTransitionCoroutine = null;
        if (isGrid) gridTransitionCoroutine = null;
    }

    IEnumerator CapturePhotoRoutine()
    {
        isCapturingPhoto = true;

        // Hide the Viewfinder UI from the math pixels
        if (viewFinderUI != null) viewFinderUI.alpha = 0f;
        if (thirdsGridUI != null) thirdsGridUI.alpha = 0f;

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
        if (viewFinderUI != null) viewFinderUI.alpha = 1f;
        if (thirdsGridUI != null) thirdsGridUI.alpha = isGridVisible ? gridAlpha : 0f;
        TriggerShutterFeedback();
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

        isCapturingPhoto = false;
    }

    private void TriggerShutterFeedback()
    {
        if (reticlePulseCoroutine != null) StopCoroutine(reticlePulseCoroutine);
        reticlePulseCoroutine = StartCoroutine(PulseReticle());

        if (cameraKickCoroutine != null) StopCoroutine(cameraKickCoroutine);
        cameraKickCoroutine = StartCoroutine(DoCameraKick());
    }

    private IEnumerator PulseReticle()
    {
        if (focusReticle == null) yield break;

        float halfDuration = Mathf.Max(0.01f, reticlePulseDuration) * 0.5f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 pulseScale = Vector3.one * reticlePulseScale;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            focusReticle.localScale = Vector3.Lerp(startScale, pulseScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            focusReticle.localScale = Vector3.Lerp(pulseScale, startScale, t);
            yield return null;
        }

        focusReticle.localScale = Vector3.one;
        reticlePulseCoroutine = null;
    }

    private IEnumerator DoCameraKick()
    {
        if (playerController == null || playerController.CinemachineCameraTarget == null || cameraKickDistance <= 0f)
        {
            yield break;
        }

        Transform cameraTarget = playerController.CinemachineCameraTarget.transform;
        Vector3 startPos = cameraTarget.localPosition;
        Vector3 kickPos = startPos + new Vector3(0f, 0f, -cameraKickDistance);
        float halfDuration = Mathf.Max(0.01f, cameraKickDuration) * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            cameraTarget.localPosition = Vector3.Lerp(startPos, kickPos, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            cameraTarget.localPosition = Vector3.Lerp(kickPos, startPos, t);
            yield return null;
        }

        cameraTarget.localPosition = startPos;
        cameraKickCoroutine = null;
    }


}
