using UnityEngine;
using UnityEngine.SceneManagement;

public class mainMenController : MonoBehaviour
{
    public void StartGame()
    {
        // Loads the next scene in the build queue
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenSettings()
    {
        Debug.Log("Settings Menu Opened!");
    }

    public void QuitGame()
    {
        Debug.Log("Game is quitting...");
        Application.Quit();
    }
}
