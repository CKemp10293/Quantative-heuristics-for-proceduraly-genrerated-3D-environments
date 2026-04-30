using UnityEngine;

public class mainMenuNav : MonoBehaviour
{
    [Header("Menu Screens")]
    [Tooltip("The Canvas Group holding your Play/Biome buttons")]
    public CanvasGroup mainScreenUI;
    [Tooltip("The Canvas Group holding your Podium")]
    public CanvasGroup highScoreScreenUI;
	[Tooltip("The Canvas Group holding your Settings tab content")]
	public CanvasGroup settingsScreenUI;

    [Header("Raycast Shields")]
    public CanvasGroup masterPanelGroup;

	[Header("Optional Controllers")]
	[Tooltip("Optional controller for the settings screen (used for future settings content)")]
	public mainMenuSettingsController settingsController;

    [Header("Transition Settings")]
    [Min(0.01f)] public float panelTransitionDuration = 0.3f;
    [Min(0.7f)] public float panelHiddenScale = 0.96f;

    private Coroutine panelTransitionCoroutine;

    private void Start()
    {
        // Ensure the game starts on the correct screen
        SetCanvasState(mainScreenUI, true);
        SetCanvasState(highScoreScreenUI, false);
		SetCanvasState(settingsScreenUI, false);
    }

    public void OpenHighScores()
    {
        SwitchPanel(mainScreenUI, highScoreScreenUI);
        if (masterPanelGroup != null)
        {
            masterPanelGroup.blocksRaycasts = false;
            masterPanelGroup.interactable = false;
        }
    }

    public void CloseHighScores()
    {
        SwitchPanel(highScoreScreenUI, mainScreenUI);
        if (masterPanelGroup != null)
        {
            masterPanelGroup.blocksRaycasts = true;
            masterPanelGroup.interactable = true;
        }
    }

	public void OpenSettings()
	{
		SwitchPanel(mainScreenUI, settingsScreenUI);
		if (settingsController != null) settingsController.OnSettingsOpened();
	}

	public void CloseSettings()
	{
		SwitchPanel(settingsScreenUI, mainScreenUI);
		if (settingsController != null) settingsController.OnSettingsClosed();
	}

    private void SwitchPanel(CanvasGroup fromPanel, CanvasGroup toPanel)
    {
        if (fromPanel == null || toPanel == null) return;

        if (panelTransitionCoroutine != null)
        {
            StopCoroutine(panelTransitionCoroutine);
        }

        panelTransitionCoroutine = StartCoroutine(AnimatePanelSwitch(fromPanel, toPanel));
    }

    private void SetCanvasState(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;
        canvas.alpha = isActive ? 1f : 0f;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;
        canvas.transform.localScale = isActive ? Vector3.one : Vector3.one * panelHiddenScale;
    }

    private System.Collections.IEnumerator AnimatePanelSwitch(CanvasGroup fromPanel, CanvasGroup toPanel)
    {
        // Disable input during transition to prevent rapid double clicks.
        fromPanel.interactable = false;
        fromPanel.blocksRaycasts = false;
        toPanel.interactable = false;
        toPanel.blocksRaycasts = false;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, panelTransitionDuration);
        float fromStart = fromPanel.alpha;
        float toStart = toPanel.alpha;
        Vector3 fromScaleStart = fromPanel.transform.localScale;
        Vector3 toScaleStart = toPanel.transform.localScale;
        Vector3 hiddenScale = Vector3.one * panelHiddenScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            fromPanel.alpha = Mathf.Lerp(fromStart, 0f, t);
            toPanel.alpha = Mathf.Lerp(toStart, 1f, t);
            fromPanel.transform.localScale = Vector3.Lerp(fromScaleStart, hiddenScale, t);
            toPanel.transform.localScale = Vector3.Lerp(toScaleStart, Vector3.one, t);
            yield return null;
        }

        fromPanel.alpha = 0f;
        toPanel.alpha = 1f;
        fromPanel.transform.localScale = hiddenScale;
        toPanel.transform.localScale = Vector3.one;

        toPanel.interactable = true;
        toPanel.blocksRaycasts = true;
        panelTransitionCoroutine = null;
    }
}
