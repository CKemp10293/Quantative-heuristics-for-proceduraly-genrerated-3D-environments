using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class mainMenuController : MonoBehaviour
{
    [Header("panels and containers")]
    public CanvasGroup mainMenuContainer;
    public CanvasGroup biomeSelectContainer;
    public Image backgroundImage;

    [Header("prefabs and assets")]
    public GameObject biomeButtonPrefab;
    public MapConfig[]  allBiomes;

    [Header("transition settings")]
    [Min(0.01f)] public float panelTransitionDuration = 0.3f;
    [Min(0.7f)] public float panelHiddenScale = 0.96f;

    [Header("main menu music")]
    public AudioSource menuMusicSource;
    public AudioClip menuMusicClip;
    [Range(0f, 1f)] public float menuMusicVolume = 0.18f;
    [Min(0f)] public float menuMusicFadeInDuration = 1.5f;
    [Min(0f)] public float menuMusicFadeOutDuration = 0.8f;

    public static MapConfig selectedBiome;
    private Coroutine panelTransitionCoroutine;
    private Coroutine musicFadeCoroutine;

    private void Start()
    {
        SetupMainMenuMusic();

        if(allBiomes == null || allBiomes.Length == 0 ) return;

        selectedBiome = allBiomes[0];
        UpdateMenuVisuals(selectedBiome);
        ShowMainMenu();

        int spawnIndex = 1;

        foreach(MapConfig Biomes in allBiomes)
        {
            MapConfig currentbiome = Biomes;
            GameObject newButton = Instantiate(biomeButtonPrefab, biomeSelectContainer.transform);

            newButton.transform.SetSiblingIndex(spawnIndex);
            spawnIndex++;

            TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = currentbiome.sceneName;
            }

            Button btnComponent = newButton.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.onClick.AddListener(() => OnBiomeClicked(currentbiome));
            }
        }
    }

    public void OpenBiomeSelect()
    {
        SwitchPanel(mainMenuContainer,biomeSelectContainer);
    }

    public void BackToMainMenu()
    {
        SwitchPanel(biomeSelectContainer,mainMenuContainer);
    }

    private void OnBiomeClicked(MapConfig clickedBiome)
    {
        selectedBiome = clickedBiome;
        UpdateMenuVisuals(clickedBiome);
        BackToMainMenu();
    }

    public void StartGame()
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        StartCoroutine(FadeOutMusicAndLoadScene("mainScene"));
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void SwitchPanel(CanvasGroup fromPanel,CanvasGroup toPanel)
    {
        if (panelTransitionCoroutine != null)
        {
            StopCoroutine(panelTransitionCoroutine);
        }

        panelTransitionCoroutine = StartCoroutine(AnimatePanelSwitch(fromPanel, toPanel));
    }

    private void ShowMainMenu()
    {
        mainMenuContainer.alpha = 1f;
        mainMenuContainer.interactable = mainMenuContainer.blocksRaycasts = true;
        mainMenuContainer.transform.localScale = Vector3.one;

        biomeSelectContainer.alpha = 0f;
        biomeSelectContainer.interactable = biomeSelectContainer.blocksRaycasts = false;
        biomeSelectContainer.transform.localScale = Vector3.one * panelHiddenScale;

    }

    private void UpdateMenuVisuals(MapConfig biome)
    {
        backgroundImage.sprite = biome.backgroundImage;
    }

    private void SetupMainMenuMusic()
    {
        if (menuMusicSource == null)
        {
            menuMusicSource = GetComponent<AudioSource>();
            if (menuMusicSource == null)
            {
                menuMusicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (menuMusicClip == null || menuMusicSource == null) return;

        menuMusicSource.clip = menuMusicClip;
        menuMusicSource.loop = true;
        menuMusicSource.playOnAwake = false;
        menuMusicSource.volume = 0f;

        if (!menuMusicSource.isPlaying)
        {
            menuMusicSource.Play();
        }

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeMusicToTarget(menuMusicVolume, menuMusicFadeInDuration));
    }

    private System.Collections.IEnumerator FadeMusicToTarget(float targetVolume, float duration)
    {
        if (menuMusicSource == null) yield break;

        float startVolume = menuMusicSource.volume;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            menuMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        menuMusicSource.volume = targetVolume;
        musicFadeCoroutine = null;
    }

    private System.Collections.IEnumerator FadeOutMusicAndLoadScene(string sceneName)
    {
        if (menuMusicSource != null && menuMusicSource.isPlaying)
        {
            float startVolume = menuMusicSource.volume;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, menuMusicFadeOutDuration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                menuMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            menuMusicSource.volume = 0f;
            menuMusicSource.Stop();
        }

        SceneManager.LoadScene(sceneName);
    }

    private System.Collections.IEnumerator AnimatePanelSwitch(CanvasGroup fromPanel, CanvasGroup toPanel)
    {
        if (fromPanel == null || toPanel == null)
        {
            yield break;
        }

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
            // Smoothstep provides a gentle ease in/out feel.
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
