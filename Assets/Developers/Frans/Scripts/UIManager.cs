using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void GoToGame()
    {
        // Load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("FirstDate");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
