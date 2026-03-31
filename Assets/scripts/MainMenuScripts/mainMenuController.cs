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

    public static MapConfig selectedBiome;

    private void Start()
    {
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
        SceneManager.LoadScene(("mainScene"));
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void SwitchPanel(CanvasGroup fromPanel,CanvasGroup toPanel)
    {
        fromPanel.alpha =0f;
        fromPanel.interactable = fromPanel.blocksRaycasts = false;

        toPanel.alpha = 1f;
        toPanel.interactable = toPanel.blocksRaycasts = true;
    }

    private void ShowMainMenu()
    {
        mainMenuContainer.alpha = 1f;
        mainMenuContainer.interactable = mainMenuContainer.blocksRaycasts = true;

        biomeSelectContainer.alpha = 0f;
        biomeSelectContainer.interactable = biomeSelectContainer.blocksRaycasts = false;

    }

    private void UpdateMenuVisuals(MapConfig biome)
    {
        backgroundImage.sprite = biome.backgroundImage;
    }

}
