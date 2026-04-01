using UnityEngine;

public class mainMenuNav : MonoBehaviour
{
    [Header("Menu Screens")]
    [Tooltip("The Canvas Group holding your Play/Biome buttons")]
    public CanvasGroup mainScreenUI;
    [Tooltip("The Canvas Group holding your Podium")]
    public CanvasGroup highScoreScreenUI;

    private void Start()
    {
        // Ensure the game starts on the correct screen
        SetCanvasState(mainScreenUI, true);
        SetCanvasState(highScoreScreenUI, false);
    }

    public void OpenHighScores()
    {
        SetCanvasState(mainScreenUI, false);
        SetCanvasState(highScoreScreenUI, true);
    }

    public void CloseHighScores()
    {
        SetCanvasState(highScoreScreenUI, false);
        SetCanvasState(mainScreenUI, true);
    }

    private void SetCanvasState(CanvasGroup canvas, bool isActive)
    {
        if (canvas == null) return;
        canvas.alpha = isActive ? 1f : 0f;
        canvas.interactable = isActive;
        canvas.blocksRaycasts = isActive;
    }
}
